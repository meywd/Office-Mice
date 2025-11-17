using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Serialization snapshot of MapData.
    /// Contains only serializable data, no Unity object references.
    /// </summary>
    [Serializable]
    public class MapDataSnapshot
    {
        [Header("Identity")]
        public int seed;
        public string mapID;
        public long generatedTimestamp; // DateTime.Ticks

        [Header("Spatial Properties")]
        public Vector2Int mapSize;
        public RectInt mapBounds;

        [Header("Structural Data")]
        public RoomDataSnapshot[] rooms;
        public CorridorDataSnapshot[] corridors;
        public BSPNodeSnapshot bspRoot;

        [Header("Gameplay Data")]
        public Vector2Int playerSpawnPosition;
        public SpawnPointSnapshot[] enemySpawnPoints;
        public ResourceSnapshot[] resources;

        [Header("Metadata")]
        public MapMetadataSnapshot metadata;
        
        [Header("Serialization Metadata")]
        public string serializationVersion;
        public long serializationTimestamp; // DateTime.Ticks

        // Default constructor for serialization
        public MapDataSnapshot()
        {
        }

        // Constructor from MapData
        public MapDataSnapshot(MapData source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Copy basic properties
            seed = source.Seed;
            mapID = source.MapID;
            generatedTimestamp = source.GeneratedTimestamp.Ticks;
            mapSize = source.MapSize;
            mapBounds = source.MapBounds;
            playerSpawnPosition = source.PlayerSpawnPosition;

            // Convert rooms to snapshots
            rooms = new RoomDataSnapshot[source.Rooms.Count];
            for (int i = 0; i < source.Rooms.Count; i++)
            {
                rooms[i] = new RoomDataSnapshot(source.Rooms[i]);
            }

            // Convert corridors to snapshots
            corridors = new CorridorDataSnapshot[source.Corridors.Count];
            for (int i = 0; i < source.Corridors.Count; i++)
            {
                corridors[i] = new CorridorDataSnapshot(source.Corridors[i]);
            }

            // Convert BSP tree to snapshot
            bspRoot = source.RootNode != null ? new BSPNodeSnapshot(source.RootNode) : null;

            // Convert spawn points
            enemySpawnPoints = new SpawnPointSnapshot[source.EnemySpawnPoints.Count];
            for (int i = 0; i < source.EnemySpawnPoints.Count; i++)
            {
                enemySpawnPoints[i] = new SpawnPointSnapshot(source.EnemySpawnPoints[i]);
            }

            // Convert resources
            resources = new ResourceSnapshot[source.Resources.Count];
            for (int i = 0; i < source.Resources.Count; i++)
            {
                resources[i] = new ResourceSnapshot(source.Resources[i]);
            }

            // Convert metadata
            metadata = new MapMetadataSnapshot(source.Metadata);
        }

        // Reconstruction to MapData
        public MapData ToMapData()
        {
            var mapData = new MapData(seed, mapSize);
            
            // Restore identity
            mapData.SetPlayerSpawn(playerSpawnPosition);

            // Rebuild rooms
            foreach (var roomSnapshot in rooms)
            {
                var room = roomSnapshot.ToRoomData();
                mapData.AddRoom(room);
            }

            // Rebuild corridors
            foreach (var corridorSnapshot in corridors)
            {
                var corridor = corridorSnapshot.ToCorridorData();
                mapData.AddCorridor(corridor);
            }

            // Rebuild BSP tree
            if (bspRoot != null)
            {
                var bspNode = bspRoot.ToBSPNode();
                mapData.SetBSPRoot(bspNode);
            }

            // Rebuild spawn points
            foreach (var spawnSnapshot in enemySpawnPoints)
            {
                var spawnPoint = spawnSnapshot.ToSpawnPointData();
                mapData.AddEnemySpawnPoint(spawnPoint);
            }

            // Rebuild resources
            foreach (var resourceSnapshot in resources)
            {
                var resource = resourceSnapshot.ToResourcePlacementData();
                mapData.AddResource(resource);
            }

            return mapData;
        }

        // Validation
        public bool IsValid()
        {
            if (rooms == null || rooms.Length == 0)
                return false;

            if (corridors == null)
                return false;

            if (string.IsNullOrEmpty(mapID))
                return false;

            if (mapSize.x <= 0 || mapSize.y <= 0)
                return false;

            return true;
        }
    }

    [Serializable]
    public class RoomDataSnapshot
    {
        public int roomID;
        public RectInt bounds;
        public Vector2Int center;
        public int area;
        public int[] connectedRoomIDs;
        public DoorwayPositionSnapshot[] doorways;
        public RoomClassification classification;
        public bool isOnCriticalPath;
        public float distanceFromPlayerSpawn;
        public string assignedTemplateID;

        public RoomDataSnapshot()
        {
        }

        public RoomDataSnapshot(RoomData source)
        {
            roomID = source.RoomID;
            bounds = source.Bounds;
            center = source.Center;
            area = source.Area;
            connectedRoomIDs = source.ConnectedRoomIDs.ToArray();
            classification = source.Classification;
            isOnCriticalPath = source.IsOnCriticalPath;
            distanceFromPlayerSpawn = source.DistanceFromPlayerSpawn;
            assignedTemplateID = source.AssignedTemplateID;

            // Convert doorways
            doorways = new DoorwayPositionSnapshot[source.Doorways.Count];
            for (int i = 0; i < source.Doorways.Count; i++)
            {
                doorways[i] = new DoorwayPositionSnapshot(source.Doorways[i]);
            }
        }

        public RoomData ToRoomData()
        {
            var room = new RoomData(bounds);
            room.RoomID = roomID;
            room.SetClassification(classification);
            room.SetOnCriticalPath(isOnCriticalPath);
            room.SetDistanceFromPlayerSpawn(distanceFromPlayerSpawn);
            room.AssignTemplate(assignedTemplateID);

            // Restore connections
            foreach (var id in connectedRoomIDs)
            {
                room.ConnectToRoom(id);
            }

            // Restore doorways
            foreach (var doorwaySnapshot in doorways)
            {
                var doorway = doorwaySnapshot.ToDoorwayPosition();
                room.AddDoorway(doorway);
            }

            return room;
        }
    }

    [Serializable]
    public class CorridorDataSnapshot
    {
        public int corridorID;
        public int roomA_ID;
        public int roomB_ID;
        public Vector2Int startPosition;
        public Vector2Int endPosition;
        public Vector2Int[] pathTiles;
        public int width;
        public int length;
        public CorridorShape shape;

        public CorridorDataSnapshot()
        {
        }

        public CorridorDataSnapshot(CorridorData source)
        {
            corridorID = source.CorridorID;
            roomA_ID = source.RoomA_ID;
            roomB_ID = source.RoomB_ID;
            startPosition = source.StartPosition;
            endPosition = source.EndPosition;
            pathTiles = source.PathTiles.ToArray();
            width = source.Width;
            length = source.Length;
            shape = source.Shape;
        }

        public CorridorData ToCorridorData()
        {
            var corridor = new CorridorData(roomA_ID, roomB_ID, startPosition, endPosition, width);
            corridor.CorridorID = corridorID;
            corridor.SetPath(new List<Vector2Int>(pathTiles));
            return corridor;
        }
    }

    [Serializable]
    public class BSPNodeSnapshot
    {
        public RectInt bounds;
        public bool isLeaf;
        public int depth;
        public bool isHorizontalSplit;
        public int splitPosition;
        public BSPNodeSnapshot left;
        public BSPNodeSnapshot right;
        public RectInt roomBounds;

        public BSPNodeSnapshot()
        {
        }

        public BSPNodeSnapshot(BSPNode source)
        {
            bounds = source.Bounds;
            isLeaf = source.IsLeaf;
            depth = source.Depth;
            isHorizontalSplit = source.IsHorizontalSplit;
            splitPosition = source.SplitPosition;
            roomBounds = source.RoomBounds;

            if (!isLeaf)
            {
                left = source.Left != null ? new BSPNodeSnapshot(source.Left) : null;
                right = source.Right != null ? new BSPNodeSnapshot(source.Right) : null;
            }
        }

        public BSPNode ToBSPNode()
        {
            var node = new BSPNode(bounds);
            
            // Use reflection to set private fields since BSPNode doesn't expose public setters
            var nodeType = typeof(BSPNode);
            var isLeafField = nodeType.GetField("_isLeaf", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var depthField = nodeType.GetField("_depth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isHorizontalSplitField = nodeType.GetField("_isHorizontalSplit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var splitPositionField = nodeType.GetField("_splitPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var roomBoundsField = nodeType.GetField("_roomBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            isLeafField?.SetValue(node, isLeaf);
            depthField?.SetValue(node, depth);
            isHorizontalSplitField?.SetValue(node, isHorizontalSplit);
            splitPositionField?.SetValue(node, splitPosition);
            roomBoundsField?.SetValue(node, roomBounds);

            if (!isLeaf)
            {
                // This is a simplified approach - in practice, you might want to add public methods to BSPNode
                // for setting children after creation
                if (left != null || right != null)
                {
                    Debug.LogWarning("BSPNodeSnapshot reconstruction of internal nodes is not fully supported");
                }
            }
            else if (roomBounds.width > 0 && roomBounds.height > 0)
            {
                node.SetRoomBounds(roomBounds);
            }

            return node;
        }
    }

    [Serializable]
    public class DoorwayPositionSnapshot
    {
        public Vector2Int position;
        public DoorwayDirection direction;
        public int width;

        public DoorwayPositionSnapshot()
        {
        }

        public DoorwayPositionSnapshot(DoorwayPosition source)
        {
            position = source.position;
            direction = source.direction;
            width = source.width;
        }

        public DoorwayPosition ToDoorwayPosition()
        {
            return new DoorwayPosition(position, direction, width);
        }
    }

    [Serializable]
    public class SpawnPointSnapshot
    {
        public int roomID;
        public Vector2Int position;
        public string enemyType;
        public float spawnDelay;

        public SpawnPointSnapshot()
        {
        }

        public SpawnPointSnapshot(SpawnPointData source)
        {
            roomID = source.RoomID;
            position = source.Position;
            enemyType = source.EnemyType;
            spawnDelay = source.SpawnDelay;
        }

        public SpawnPointData ToSpawnPointData()
        {
            return new SpawnPointData(roomID, position, enemyType, spawnDelay);
        }
    }

    [Serializable]
    public class ResourceSnapshot
    {
        public int roomID;
        public Vector2Int position;
        public string resourceType;
        public int quantity;

        public ResourceSnapshot()
        {
        }

        public ResourceSnapshot(ResourcePlacementData source)
        {
            roomID = source.RoomID;
            position = source.Position;
            resourceType = source.ResourceType;
            quantity = source.Quantity;
        }

        public ResourcePlacementData ToResourcePlacementData()
        {
            return new ResourcePlacementData(roomID, position, resourceType, quantity);
        }
    }

    [Serializable]
    public class MapMetadataSnapshot
    {
        public string version;
        public string generatorType;
        public float generationTime;
        public string[] customPropertyKeys;
        public string[] customPropertyValues;

        public MapMetadataSnapshot()
        {
        }

        public MapMetadataSnapshot(MapMetadata source)
        {
            version = source.Version;
            generatorType = source.GeneratorType;
            generationTime = source.GenerationTime;

            if (source.CustomProperties != null)
            {
                customPropertyKeys = source.CustomProperties.Keys.ToArray();
                customPropertyValues = source.CustomProperties.Values.ToArray();
            }
        }

        public MapMetadata ToMapMetadata()
        {
            var metadata = new MapMetadata();
            
            // Use reflection to set private fields
            var metadataType = typeof(MapMetadata);
            var versionField = metadataType.GetField("_version", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generatorTypeField = metadataType.GetField("_generatorType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generationTimeField = metadataType.GetField("_generationTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            versionField?.SetValue(metadata, version);
            generatorTypeField?.SetValue(metadata, generatorType);
            generationTimeField?.SetValue(metadata, generationTime);

            // Restore custom properties
            if (customPropertyKeys != null && customPropertyValues != null)
            {
                for (int i = 0; i < Math.Min(customPropertyKeys.Length, customPropertyValues.Length); i++)
                {
                    metadata.CustomProperties[customPropertyKeys[i]] = customPropertyValues[i];
                }
            }

            return metadata;
        }
    }
}