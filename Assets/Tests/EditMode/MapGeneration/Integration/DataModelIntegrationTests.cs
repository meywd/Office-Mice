using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class DataModelIntegrationTests
    {
        [Test]
        public void CompleteMapData_Workflow_WorksCorrectly()
        {
            // Arrange
            var mapData = new MapData(12345, new Vector2Int(100, 100));
            
            // Create BSP tree
            var rootNode = new BSPNode(new RectInt(0, 0, 100, 100));
            rootNode.Split(8, 3); // Split the root
            
            // Create rooms from leaf nodes
            var leafNodes = rootNode.GetLeafNodes();
            var rooms = new List<RoomData>();
            
            foreach (var leaf in leafNodes)
            {
                // Create room bounds smaller than leaf bounds
                var roomBounds = new RectInt(
                    leaf.Bounds.x + 2,
                    leaf.Bounds.y + 2,
                    leaf.Bounds.width - 4,
                    leaf.Bounds.height - 4
                );
                
                if (roomBounds.width >= 3 && roomBounds.height >= 3)
                {
                    var room = new RoomData(roomBounds);
                    rooms.Add(room);
                    mapData.AddRoom(room);
                }
            }

            // Create corridors between rooms
            if (rooms.Count >= 2)
            {
                for (int i = 0; i < rooms.Count - 1; i++)
                {
                    var roomA = rooms[i];
                    var roomB = rooms[i + 1];
                    
                    var startPos = roomA.GetRandomEdgePoint();
                    var endPos = roomB.GetRandomEdgePoint();
                    
                    var corridor = new CorridorData(roomA.RoomID, roomB.RoomID, startPos, endPos);
                    
                    // Create simple L-shaped path
                    var path = new List<Vector2Int>();
                    var current = startPos;
                    
                    // Move horizontally first
                    while (current.x != endPos.x)
                    {
                        path.Add(current);
                        current.x += current.x < endPos.x ? 1 : -1;
                    }
                    
                    // Then move vertically
                    while (current.y != endPos.y)
                    {
                        path.Add(current);
                        current.y += current.y < endPos.y ? 1 : -1;
                    }
                    path.Add(endPos);
                    
                    corridor.SetPath(path);
                    mapData.AddCorridor(corridor);
                    
                    // Add doorways to rooms
                    roomA.AddDoorway(new DoorwayPosition(startPos, GetDirectionFromVector(startPos - roomA.Center)));
                    roomB.AddDoorway(new DoorwayPosition(endPos, GetDirectionFromVector(endPos - roomB.Center)));
                }
            }

            // Set player spawn in first room
            if (rooms.Count > 0)
            {
                mapData.SetPlayerSpawn(rooms[0].GetRandomPoint());
                
                // Classify rooms
                rooms[0].SetClassification(RoomClassification.PlayerStart);
                for (int i = 1; i < rooms.Count - 1; i++)
                {
                    rooms[i].SetClassification(RoomClassification.StandardRoom);
                }
                if (rooms.Count > 1)
                {
                    rooms[rooms.Count - 1].SetClassification(RoomClassification.BossRoom);
                }
            }

            // Set BSP root
            mapData.SetBSPRoot(rootNode);

            // Act - Validate the complete map
            var validationResult = mapData.Validate();

            // Assert
            Assert.IsTrue(validationResult.IsValid, 
                $"Map validation failed: {string.Join(", ", validationResult.Errors)}");
            
            Assert.IsTrue(mapData.Rooms.Count > 0, "Map should have rooms");
            Assert.IsTrue(mapData.Corridors.Count > 0, "Map should have corridors");
            Assert.IsTrue(mapData.PlayerSpawnPosition != Vector2Int.zero, "Map should have player spawn");
            
            // Test connectivity
            var walkableTiles = mapData.GetWalkableTiles();
            Assert.IsTrue(walkableTiles.Count > 0, "Map should have walkable tiles");
            
            // Test statistics
            var stats = mapData.GetStatistics();
            Assert.IsTrue(stats.TotalRooms > 0, "Statistics should show rooms");
            Assert.IsTrue(stats.TotalCorridors > 0, "Statistics should show corridors");
            Assert.IsTrue(stats.TotalWalkableTiles > 0, "Statistics should show walkable tiles");
            
            // Test serialization
            var snapshot = mapData.CreateSnapshot();
            Assert.IsTrue(snapshot.IsValid(), "Snapshot should be valid");
            
            var reconstructedMap = snapshot.ToMapData();
            Assert.AreEqual(mapData.Seed, reconstructedMap.Seed);
            Assert.AreEqual(mapData.Rooms.Count, reconstructedMap.Rooms.Count);
            Assert.AreEqual(mapData.Corridors.Count, reconstructedMap.Corridors.Count);
        }

        [Test]
        public void MapDataSnapshot_RoundTrip_WorksCorrectly()
        {
            // Arrange
            var originalMap = new MapData(42, new Vector2Int(50, 50));
            
            var room = new RoomData(new RectInt(10, 10, 15, 20));
            room.SetClassification(RoomClassification.BossRoom);
            room.AddDoorway(new DoorwayPosition(new Vector2Int(15, 10), DoorwayDirection.North, 2));
            
            var corridor = new CorridorData(0, 1, new Vector2Int(15, 10), new Vector2Int(25, 30));
            corridor.SetPath(new List<Vector2Int>
            {
                new Vector2Int(15, 10), new Vector2Int(15, 20), 
                new Vector2Int(25, 20), new Vector2Int(25, 30)
            });
            
            originalMap.AddRoom(room);
            originalMap.AddCorridor(corridor);
            originalMap.SetPlayerSpawn(new Vector2Int(12, 12));
            originalMap.AddEnemySpawnPoint(new SpawnPointData(0, new Vector2Int(18, 18), "basic_enemy"));
            originalMap.AddResource(new ResourcePlacementData(0, new Vector2Int(14, 14), "health", 1));

            // Act
            var snapshot = originalMap.CreateSnapshot();
            var reconstructedMap = snapshot.ToMapData();

            // Assert
            Assert.AreEqual(originalMap.Seed, reconstructedMap.Seed);
            Assert.AreEqual(originalMap.MapSize, reconstructedMap.MapSize);
            Assert.AreEqual(originalMap.PlayerSpawnPosition, reconstructedMap.PlayerSpawnPosition);
            Assert.AreEqual(originalMap.Rooms.Count, reconstructedMap.Rooms.Count);
            Assert.AreEqual(originalMap.Corridors.Count, reconstructedMap.Corridors.Count);
            Assert.AreEqual(originalMap.EnemySpawnPoints.Count, reconstructedMap.EnemySpawnPoints.Count);
            Assert.AreEqual(originalMap.Resources.Count, reconstructedMap.Resources.Count);
            
            // Validate reconstructed map
            var validationResult = reconstructedMap.Validate();
            Assert.IsTrue(validationResult.IsValid, 
                $"Reconstructed map validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        private DoorwayDirection GetDirectionFromVector(Vector2Int direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return direction.x > 0 ? DoorwayDirection.East : DoorwayDirection.West;
            }
            else
            {
                return direction.y > 0 ? DoorwayDirection.North : DoorwayDirection.South;
            }
        }
    }
}