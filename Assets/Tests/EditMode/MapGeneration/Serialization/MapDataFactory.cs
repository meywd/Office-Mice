using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Tests
{
    /// <summary>
    /// Factory for creating test map data with various sizes and complexities.
    /// Used for performance and functionality testing.
    /// </summary>
    public static class MapDataFactory
    {
        /// <summary>
        /// Creates a small map with 10 rooms for basic testing.
        /// </summary>
        public static MapData CreateSmallMap()
        {
            return CreateMap(10, new Vector2Int(50, 50), "small-test-map");
        }
        
        /// <summary>
        /// Creates a medium map with 50 rooms for performance testing.
        /// </summary>
        public static MapData CreateMediumMap()
        {
            return CreateMap(50, new Vector2Int(100, 100), "medium-test-map");
        }
        
        /// <summary>
        /// Creates a large map with 100 rooms for stress testing.
        /// </summary>
        public static MapData CreateLargeMap()
        {
            return CreateMap(100, new Vector2Int(200, 200), "large-test-map");
        }
        
        /// <summary>
        /// Creates a map with specified parameters.
        /// </summary>
        private static MapData CreateMap(int roomCount, Vector2Int mapSize, string mapId)
        {
            var map = new MapData();
            
            // Set basic properties using reflection or public setters if available
            var seedProperty = typeof(MapData).GetProperty("Seed");
            var mapIdProperty = typeof(MapData).GetProperty("MapID");
            var mapSizeProperty = typeof(MapData).GetProperty("MapSize");
            var mapBoundsProperty = typeof(MapData).GetProperty("MapBounds");
            
            if (seedProperty != null && seedProperty.CanWrite)
                seedProperty.SetValue(map, UnityEngine.Random.Range(1, 1000000));
                
            if (mapIdProperty != null && mapIdProperty.CanWrite)
                mapIdProperty.SetValue(map, $"{mapId}-{DateTime.Now:yyyyMMdd-HHmmss}");
                
            if (mapSizeProperty != null && mapSizeProperty.CanWrite)
                mapSizeProperty.SetValue(map, mapSize);
                
            if (mapBoundsProperty != null && mapBoundsProperty.CanWrite)
                mapBoundsProperty.SetValue(map, new RectInt(0, 0, mapSize.x, mapSize.y));
            
            // Create rooms
            var rooms = CreateRooms(roomCount, mapSize);
            var roomsField = typeof(MapData).GetField("_rooms", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (roomsField != null)
                roomsField.SetValue(map, rooms);
            
            // Create corridors (connect rooms in a simple pattern)
            var corridors = CreateCorridors(rooms);
            var corridorsField = typeof(MapData).GetField("_corridors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (corridorsField != null)
                corridorsField.SetValue(map, corridors);
            
            // Create BSP tree
            var bspRoot = CreateBSPTree(rooms, mapSize);
            var bspField = typeof(MapData).GetField("_rootNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bspField != null)
                bspField.SetValue(map, bspRoot);
            
            // Set player spawn position
            var playerSpawnProperty = typeof(MapData).GetProperty("PlayerSpawnPosition");
            if (playerSpawnProperty != null && playerSpawnProperty.CanWrite && rooms.Count > 0)
            {
                var firstRoom = rooms[0];
                var spawnPos = new Vector2Int(
                    firstRoom.Bounds.x + firstRoom.Bounds.width / 2,
                    firstRoom.Bounds.y + firstRoom.Bounds.height / 2
                );
                playerSpawnProperty.SetValue(map, spawnPos);
            }
            
            // Create enemy spawn points
            var enemySpawns = CreateEnemySpawnPoints(rooms, roomCount / 5);
            var enemySpawnsField = typeof(MapData).GetField("_enemySpawnPoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (enemySpawnsField != null)
                enemySpawnsField.SetValue(map, enemySpawns);
            
            // Create resources
            var resources = CreateResources(rooms, roomCount / 3);
            var resourcesField = typeof(MapData).GetField("_resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (resourcesField != null)
                resourcesField.SetValue(map, resources);
            
            // Create metadata
            var metadata = CreateMapMetadata();
            var metadataField = typeof(MapData).GetField("_metadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (metadataField != null)
                metadataField.SetValue(map, metadata);
            
            return map;
        }
        
        /// <summary>
        /// Creates a collection of rooms.
        /// </summary>
        private static List<RoomData> CreateRooms(int count, Vector2Int mapSize)
        {
            var rooms = new List<RoomData>();
            var usedPositions = new HashSet<Vector2Int>();
            
            for (int i = 0; i < count; i++)
            {
                var room = new RoomData();
                
                // Random room size
                var roomWidth = UnityEngine.Random.Range(4, 12);
                var roomHeight = UnityEngine.Random.Range(4, 12);
                
                // Find a position that doesn't overlap
                var position = FindNonOverlappingPosition(usedPositions, roomWidth, roomHeight, mapSize);
                
                // Set room properties using reflection
                var roomIdField = typeof(RoomData).GetField("_roomID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var boundsField = typeof(RoomData).GetField("_bounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (roomIdField != null)
                    roomIdField.SetValue(room, i + 1);
                    
                if (boundsField != null)
                    boundsField.SetValue(room, new RectInt(position.x, position.y, roomWidth, roomHeight));
                
                rooms.Add(room);
                
                // Mark this area as used
                for (int x = position.x; x < position.x + roomWidth; x++)
                {
                    for (int y = position.y; y < position.y + roomHeight; y++)
                    {
                        usedPositions.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return rooms;
        }
        
        /// <summary>
        /// Finds a non-overlapping position for a room.
        /// </summary>
        private static Vector2Int FindNonOverlappingPosition(HashSet<Vector2Int> usedPositions, int width, int height, Vector2Int mapSize)
        {
            int attempts = 0;
            const int maxAttempts = 100;
            
            while (attempts < maxAttempts)
            {
                var x = UnityEngine.Random.Range(1, mapSize.x - width - 1);
                var y = UnityEngine.Random.Range(1, mapSize.y - height - 1);
                var position = new Vector2Int(x, y);
                
                bool overlaps = false;
                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        if (usedPositions.Contains(new Vector2Int(x + dx, y + dy)))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (overlaps) break;
                }
                
                if (!overlaps)
                    return position;
                    
                attempts++;
            }
            
            // Fallback: return position even if it overlaps
            return new Vector2Int(1, 1);
        }
        
        /// <summary>
        /// Creates corridors connecting rooms.
        /// </summary>
        private static List<CorridorData> CreateCorridors(List<RoomData> rooms)
        {
            var corridors = new List<CorridorData>();
            
            // Simple corridor pattern: connect each room to the next one
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                var room1 = rooms[i];
                var room2 = rooms[i + 1];
                
                var corridor = new CorridorData();
                
                // Set corridor properties using reflection
                var corridorIdField = typeof(CorridorData).GetField("_corridorID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var startField = typeof(CorridorData).GetField("_startRoomID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var endField = typeof(CorridorData).GetField("_endRoomID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (corridorIdField != null)
                    corridorIdField.SetValue(corridor, i + 1);
                    
                if (startField != null)
                    startField.SetValue(corridor, room1.RoomID);
                    
                if (endField != null)
                    endField.SetValue(corridor, room2.RoomID);
                
                corridors.Add(corridor);
            }
            
            return corridors;
        }
        
        /// <summary>
        /// Creates a simple BSP tree structure.
        /// </summary>
        private static BSPNode CreateBSPTree(List<RoomData> rooms, Vector2Int mapSize)
        {
            var root = new BSPNode();
            
            // Set root properties using reflection
            var nodeIdField = typeof(BSPNode).GetField("_nodeID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var boundsField = typeof(BSPNode).GetField("_bounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isLeafField = typeof(BSPNode).GetField("_isLeaf", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nodeIdField != null)
                nodeIdField.SetValue(root, 1);
                
            if (boundsField != null)
                boundsField.SetValue(root, new RectInt(0, 0, mapSize.x, mapSize.y));
                
            if (isLeafField != null)
                isLeafField.SetValue(root, false);
            
            // Create a simple binary tree structure
            if (rooms.Count > 2)
            {
                var leftChild = CreateBSPLeaf(rooms.GetRange(0, rooms.Count / 2), 2);
                var rightChild = CreateBSPLeaf(rooms.GetRange(rooms.Count / 2, rooms.Count - rooms.Count / 2), 3);
                
                var leftChildField = typeof(BSPNode).GetField("_leftChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rightChildField = typeof(BSPNode).GetField("_rightChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (leftChildField != null)
                    leftChildField.SetValue(root, leftChild);
                    
                if (rightChildField != null)
                    rightChildField.SetValue(root, rightChild);
            }
            
            return root;
        }
        
        /// <summary>
        /// Creates a leaf BSP node.
        /// </summary>
        private static BSPNode CreateBSPLeaf(List<RoomData> rooms, int nodeId)
        {
            var leaf = new BSPNode();
            
            var nodeIdField = typeof(BSPNode).GetField("_nodeID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isLeafField = typeof(BSPNode).GetField("_isLeaf", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nodeIdField != null)
                nodeIdField.SetValue(leaf, nodeId);
                
            if (isLeafField != null)
                isLeafField.SetValue(leaf, true);
            
            return leaf;
        }
        
        /// <summary>
        /// Creates enemy spawn points.
        /// </summary>
        private static List<SpawnPointData> CreateEnemySpawnPoints(List<RoomData> rooms, int count)
        {
            var spawnPoints = new List<SpawnPointData>();
            
            for (int i = 0; i < count && i < rooms.Count; i++)
            {
                var room = rooms[i];
                var spawnPoint = new SpawnPointData();
                
                // Set spawn point properties using reflection
                var positionField = typeof(SpawnPointData).GetField("_position", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var typeField = typeof(SpawnPointData).GetField("_enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (positionField != null)
                {
                    var position = new Vector2Int(
                        room.Bounds.x + room.Bounds.width / 2,
                        room.Bounds.y + room.Bounds.height / 2
                    );
                    positionField.SetValue(spawnPoint, position);
                }
                
                if (typeField != null)
                    typeField.SetValue(spawnPoint, $"EnemyType_{i % 5}");
                
                spawnPoints.Add(spawnPoint);
            }
            
            return spawnPoints;
        }
        
        /// <summary>
        /// Creates resource placements.
        /// </summary>
        private static List<ResourcePlacementData> CreateResources(List<RoomData> rooms, int count)
        {
            var resources = new List<ResourcePlacementData>();
            
            for (int i = 0; i < count; i++)
            {
                var room = rooms[i % rooms.Count];
                var resource = new ResourcePlacementData();
                
                // Set resource properties using reflection
                var positionField = typeof(ResourcePlacementData).GetField("_position", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var typeField = typeof(ResourcePlacementData).GetField("_resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (positionField != null)
                {
                    var position = new Vector2Int(
                        room.Bounds.x + UnityEngine.Random.Range(1, room.Bounds.width - 1),
                        room.Bounds.y + UnityEngine.Random.Range(1, room.Bounds.height - 1)
                    );
                    positionField.SetValue(resource, position);
                }
                
                if (typeField != null)
                    typeField.SetValue(resource, $"ResourceType_{i % 8}");
                
                resources.Add(resource);
            }
            
            return resources;
        }
        
        /// <summary>
        /// Creates map metadata.
        /// </summary>
        private static MapMetadata CreateMapMetadata()
        {
            var metadata = new MapMetadata();
            
            // Set metadata properties using reflection
            var algorithmField = typeof(MapMetadata).GetField("_generationAlgorithm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var versionField = typeof(MapMetadata).GetField("_algorithmVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeField = typeof(MapMetadata).GetField("_generationTimeMs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (algorithmField != null)
                algorithmField.SetValue(metadata, "TestFactory");
                
            if (versionField != null)
                versionField.SetValue(metadata, "1.0.0");
                
            if (timeField != null)
                timeField.SetValue(metadata, UnityEngine.Random.Range(100f, 1000f));
            
            return metadata;
        }
    }
}