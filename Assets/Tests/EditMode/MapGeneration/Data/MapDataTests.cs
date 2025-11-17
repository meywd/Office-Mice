using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;
using System.Linq;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class MapDataTests
    {
        private MapData _mapData;
        private Tilemap _mockFloorTilemap;
        private Tilemap _mockWallTilemap;
        private Tilemap _mockObjectTilemap;

        [SetUp]
        public void SetUp()
        {
            _mapData = new MapData(42, new Vector2Int(100, 100));
            
            // Create mock tilemaps (in real tests, these would be properly mocked)
            _mockFloorTilemap = new GameObject("FloorTilemap").AddComponent<Tilemap>();
            _mockWallTilemap = new GameObject("WallTilemap").AddComponent<Tilemap>();
            _mockObjectTilemap = new GameObject("ObjectTilemap").AddComponent<Tilemap>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_mockFloorTilemap != null) Object.DestroyImmediate(_mockFloorTilemap.gameObject);
            if (_mockWallTilemap != null) Object.DestroyImmediate(_mockWallTilemap.gameObject);
            if (_mockObjectTilemap != null) Object.DestroyImmediate(_mockObjectTilemap.gameObject);
        }

        [Test]
        public void MapData_Constructor_InitializesCorrectly()
        {
            // Arrange
            int seed = 123;
            var mapSize = new Vector2Int(50, 75);

            // Act
            var map = new MapData(seed, mapSize);

            // Assert
            Assert.AreEqual(seed, map.Seed);
            Assert.AreEqual(mapSize, map.MapSize);
            Assert.AreEqual(new RectInt(0, 0, 50, 75), map.MapBounds);
            Assert.IsNotEmpty(map.MapID);
            Assert.AreEqual(0, map.Rooms.Count);
            Assert.AreEqual(0, map.Corridors.Count);
            Assert.AreEqual(0, map.EnemySpawnPoints.Count);
            Assert.AreEqual(0, map.Resources.Count);
        }

        [Test]
        public void MapData_AddRoom_WorksCorrectly()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 15, 20));

            // Act
            _mapData.AddRoom(room);

            // Assert
            Assert.AreEqual(1, _mapData.Rooms.Count);
            Assert.AreEqual(room, _mapData.Rooms[0]);
            Assert.AreEqual(0, room.RoomID); // First room gets ID 0
            Assert.AreEqual(room, _mapData.GetRoomByID(0));
        }

        [Test]
        public void MapData_AddRoom_AssignsSequentialIDs()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var room3 = new RoomData(new RectInt(50, 50, 12, 12));

            // Act
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddRoom(room3);

            // Assert
            Assert.AreEqual(0, room1.RoomID);
            Assert.AreEqual(1, room2.RoomID);
            Assert.AreEqual(2, room3.RoomID);
        }

        [Test]
        public void MapData_RemoveRoom_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var corridor = new CorridorData(0, 1, Vector2Int.zero, Vector2Int.one);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);

            // Act
            _mapData.RemoveRoom(room1);

            // Assert
            Assert.AreEqual(1, _mapData.Rooms.Count);
            Assert.IsFalse(_mapData.Rooms.Contains(room1));
            Assert.IsTrue(_mapData.Rooms.Contains(room2));
            Assert.AreEqual(0, _mapData.Corridors.Count); // Connected corridor should be removed
        }

        [Test]
        public void MapData_AddCorridor_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var corridor = new CorridorData(0, 1, Vector2Int.zero, Vector2Int.one);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);

            // Act
            _mapData.AddCorridor(corridor);

            // Assert
            Assert.AreEqual(1, _mapData.Corridors.Count);
            Assert.AreEqual(corridor, _mapData.Corridors[0]);
            Assert.AreEqual(0, corridor.CorridorID);
            Assert.IsTrue(room1.IsConnectedTo(1));
            Assert.IsTrue(room2.IsConnectedTo(0));
        }

        [Test]
        public void MapData_RemoveCorridor_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var corridor = new CorridorData(0, 1, Vector2Int.zero, Vector2Int.one);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);

            // Act
            _mapData.RemoveCorridor(corridor);

            // Assert
            Assert.AreEqual(0, _mapData.Corridors.Count);
            Assert.IsFalse(room1.IsConnectedTo(1));
            Assert.IsFalse(room2.IsConnectedTo(0));
        }

        [Test]
        public void MapData_SetPlayerSpawn_WorksCorrectly()
        {
            // Arrange
            var spawnPos = new Vector2Int(25, 30);

            // Act
            _mapData.SetPlayerSpawn(spawnPos);

            // Assert
            Assert.AreEqual(spawnPos, _mapData.PlayerSpawnPosition);
        }

        [Test]
        public void MapData_BindTilemaps_WorksCorrectly()
        {
            // Act
            _mapData.BindTilemaps(_mockFloorTilemap, _mockWallTilemap, _mockObjectTilemap);

            // Assert
            Assert.AreEqual(_mockFloorTilemap, _mapData.FloorTilemap);
            Assert.AreEqual(_mockWallTilemap, _mapData.WallTilemap);
            Assert.AreEqual(_mockObjectTilemap, _mapData.ObjectTilemap);
        }

        [Test]
        public void MapData_GetRoomContainingPoint_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);

            // Act & Assert
            Assert.AreEqual(room1, _mapData.GetRoomContainingPoint(new Vector2Int(15, 15)));
            Assert.AreEqual(room2, _mapData.GetRoomContainingPoint(new Vector2Int(35, 35)));
            Assert.IsNull(_mapData.GetRoomContainingPoint(new Vector2Int(5, 5)));
        }

        [Test]
        public void MapData_GetRoomsOfClassification_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var room3 = new RoomData(new RectInt(50, 50, 12, 12));
            
            room1.SetClassification(RoomClassification.BossRoom);
            room2.SetClassification(RoomClassification.StandardRoom);
            room3.SetClassification(RoomClassification.BossRoom);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddRoom(room3);

            // Act
            var bossRooms = _mapData.GetRoomsOfClassification(RoomClassification.BossRoom);
            var standardRooms = _mapData.GetRoomsOfClassification(RoomClassification.StandardRoom);
            var safeRooms = _mapData.GetRoomsOfClassification(RoomClassification.SafeRoom);

            // Assert
            Assert.AreEqual(2, bossRooms.Count);
            Assert.IsTrue(bossRooms.Contains(room1));
            Assert.IsTrue(bossRooms.Contains(room3));
            
            Assert.AreEqual(1, standardRooms.Count);
            Assert.AreEqual(room2, standardRooms[0]);
            
            Assert.AreEqual(0, safeRooms.Count);
        }

        [Test]
        public void MapData_GetCorridorsConnectingRoom_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var room3 = new RoomData(new RectInt(50, 50, 12, 12));
            
            var corridor1 = new CorridorData(0, 1, Vector2Int.zero, Vector2Int.one);
            var corridor2 = new CorridorData(1, 2, Vector2Int.one, Vector2Int.right);
            var corridor3 = new CorridorData(0, 2, Vector2Int.right, Vector2Int.up);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddRoom(room3);
            _mapData.AddCorridor(corridor1);
            _mapData.AddCorridor(corridor2);
            _mapData.AddCorridor(corridor3);

            // Act
            var room1Corridors = _mapData.GetCorridorsConnectingRoom(0);
            var room2Corridors = _mapData.GetCorridorsConnectingRoom(1);
            var room3Corridors = _mapData.GetCorridorsConnectingRoom(2);

            // Assert
            Assert.AreEqual(2, room1Corridors.Count);
            Assert.IsTrue(room1Corridors.Contains(corridor1));
            Assert.IsTrue(room1Corridors.Contains(corridor3));
            
            Assert.AreEqual(2, room2Corridors.Count);
            Assert.IsTrue(room2Corridors.Contains(corridor1));
            Assert.IsTrue(room2Corridors.Contains(corridor2));
            
            Assert.AreEqual(2, room3Corridors.Count);
            Assert.IsTrue(room3Corridors.Contains(corridor2));
            Assert.IsTrue(room3Corridors.Contains(corridor3));
        }

        [Test]
        public void MapData_IsPointWalkable_WorksCorrectly()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 15, 20));
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(35, 20));
            var path = new List<Vector2Int>
            {
                new Vector2Int(25, 20), new Vector2Int(26, 20), new Vector2Int(27, 20),
                new Vector2Int(28, 20), new Vector2Int(29, 20), new Vector2Int(30, 20),
                new Vector2Int(31, 20), new Vector2Int(32, 20), new Vector2Int(33, 20),
                new Vector2Int(34, 20), new Vector2Int(35, 20)
            };
            corridor.SetPath(path);
            
            _mapData.AddRoom(room);
            _mapData.AddCorridor(corridor);

            // Act & Assert
            Assert.IsTrue(_mapData.IsPointWalkable(new Vector2Int(15, 15))); // In room
            Assert.IsTrue(_mapData.IsPointWalkable(new Vector2Int(30, 20))); // In corridor
            Assert.IsFalse(_mapData.IsPointWalkable(new Vector2Int(5, 5))); // Outside both
            Assert.IsFalse(_mapData.IsPointWalkable(new Vector2Int(-1, 15))); // Outside map bounds
        }

        [Test]
        public void MapData_GetWalkableTiles_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 5, 5)); // 25 tiles
            var room2 = new RoomData(new RectInt(20, 20, 3, 3)); // 9 tiles
            var corridor = new CorridorData(0, 1, new Vector2Int(14, 12), new Vector2Int(20, 22));
            var path = new List<Vector2Int>
            {
                new Vector2Int(14, 12), new Vector2Int(15, 12), new Vector2Int(16, 12),
                new Vector2Int(17, 12), new Vector2Int(18, 12), new Vector2Int(19, 12),
                new Vector2Int(20, 12), new Vector2Int(20, 13), new Vector2Int(20, 14),
                new Vector2Int(20, 15), new Vector2Int(20, 16), new Vector2Int(20, 17),
                new Vector2Int(20, 18), new Vector2Int(20, 19), new Vector2Int(20, 20),
                new Vector2Int(20, 21), new Vector2Int(20, 22)
            };
            corridor.SetPath(path);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);

            // Act
            var walkableTiles = _mapData.GetWalkableTiles();

            // Assert
            Assert.AreEqual(25 + 9 + 17, walkableTiles.Count); // Room1 + Room2 + Corridor
            
            // Check some specific tiles
            Assert.IsTrue(walkableTiles.Contains(new Vector2Int(12, 12))); // Room1
            Assert.IsTrue(walkableTiles.Contains(new Vector2Int(21, 21))); // Room2
            Assert.IsTrue(walkableTiles.Contains(new Vector2Int(17, 12))); // Corridor
            Assert.IsFalse(walkableTiles.Contains(new Vector2Int(5, 5))); // Empty space
        }

        [Test]
        public void MapData_Validate_ReturnsValidForCorrectMap()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            var path = new List<Vector2Int> { new Vector2Int(25, 20), new Vector2Int(30, 30) };
            corridor.SetPath(path);
            
            room1.AddDoorway(new DoorwayPosition(new Vector2Int(25, 20), DoorwayDirection.East, 2));
            room2.AddDoorway(new DoorwayPosition(new Vector2Int(30, 30), DoorwayDirection.West, 2));
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);
            _mapData.SetPlayerSpawn(new Vector2Int(15, 15));

            // Act
            var result = _mapData.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void MapData_Validate_ReturnsErrorForNoRooms()
        {
            // Arrange - map with no rooms
            var emptyMap = new MapData(42, new Vector2Int(50, 50));

            // Act
            var result = emptyMap.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("no rooms")));
        }

        [Test]
        public void MapData_Validate_ReturnsErrorForUnreachableRooms()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var room3 = new RoomData(new RectInt(50, 50, 12, 12)); // Not connected
            
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            var path = new List<Vector2Int> { new Vector2Int(25, 20), new Vector2Int(30, 30) };
            corridor.SetPath(path);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddRoom(room3);
            _mapData.AddCorridor(corridor);
            _mapData.SetPlayerSpawn(new Vector2Int(15, 15)); // In room1

            // Act
            var result = _mapData.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("reachable from player spawn")));
        }

        [Test]
        public void MapData_CreateSnapshot_WorksCorrectly()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 15, 20));
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            var path = new List<Vector2Int> { new Vector2Int(25, 20), new Vector2Int(30, 30) };
            corridor.SetPath(path);
            
            _mapData.AddRoom(room);
            _mapData.AddCorridor(corridor);
            _mapData.SetPlayerSpawn(new Vector2Int(15, 15));

            // Act
            var snapshot = _mapData.CreateSnapshot();

            // Assert
            Assert.AreEqual(_mapData.Seed, snapshot.seed);
            Assert.AreEqual(_mapData.MapID, snapshot.mapID);
            Assert.AreEqual(_mapData.MapSize, snapshot.mapSize);
            Assert.AreEqual(1, snapshot.rooms.Length);
            Assert.AreEqual(1, snapshot.corridors.Length);
            Assert.AreEqual(_mapData.PlayerSpawnPosition, snapshot.playerSpawnPosition);
        }

        [Test]
        public void MapData_GetStatistics_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20)); // 300 tiles
            var room2 = new RoomData(new RectInt(30, 30, 10, 10)); // 100 tiles
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            var path = new List<Vector2Int> { new Vector2Int(25, 20), new Vector2Int(30, 30) };
            corridor.SetPath(path);
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);
            _mapData.AddEnemySpawnPoint(new SpawnPointData(0, Vector2Int.zero, "basic_enemy"));
            _mapData.AddResource(new ResourcePlacementData(1, Vector2Int.one, "health", 1));

            // Act
            var stats = _mapData.GetStatistics();

            // Assert
            Assert.AreEqual(2, stats.TotalRooms);
            Assert.AreEqual(1, stats.TotalCorridors);
            Assert.AreEqual(402, stats.TotalWalkableTiles); // 300 + 100 + 2 corridor tiles
            Assert.AreEqual(_mapData.MapSize, stats.MapSize);
            Assert.AreEqual(200f, stats.AverageRoomSize); // (300 + 100) / 2
            Assert.AreEqual(1, stats.TotalEnemySpawnPoints);
            Assert.AreEqual(1, stats.TotalResources);
        }

        [Test]
        public void MapData_RebuildLookupTables_WorksCorrectly()
        {
            // Arrange
            var room1 = new RoomData(new RectInt(10, 10, 15, 20));
            var room2 = new RoomData(new RectInt(30, 30, 10, 10));
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            
            _mapData.AddRoom(room1);
            _mapData.AddRoom(room2);
            _mapData.AddCorridor(corridor);

            // Act
            _mapData.RebuildLookupTables();

            // Assert
            Assert.AreEqual(room1, _mapData.GetRoomByID(0));
            Assert.AreEqual(room2, _mapData.GetRoomByID(1));
            Assert.AreEqual(corridor, _mapData.GetCorridorByID(0));
        }

        [Test]
        public void MapData_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 15, 20));
            var corridor = new CorridorData(0, 1, new Vector2Int(25, 20), new Vector2Int(30, 30));
            
            _mapData.AddRoom(room);
            _mapData.AddCorridor(corridor);

            // Act
            var result = _mapData.ToString();

            // Assert
            Assert.IsTrue(result.Contains($"Map[{_mapData.MapID}]"));
            Assert.IsTrue(result.Contains($"Seed:{_mapData.Seed}"));
            Assert.IsTrue(result.Contains($"Size:{_mapData.MapSize}"));
            Assert.IsTrue(result.Contains("Rooms:1"));
            Assert.IsTrue(result.Contains("Corridors:1"));
        }
    }
}