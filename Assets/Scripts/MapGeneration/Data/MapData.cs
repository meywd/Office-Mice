using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Root data structure representing a complete procedurally generated map.
    /// This is the canonical output of Phase 1 (BSP Generation) and input to Phase 2 (Content Population).
    /// </summary>
    [Serializable]
    public class MapData
    {
        [Header("Identity")]
        [SerializeField] private int _seed;
        [SerializeField] private string _mapID;
        [SerializeField] private DateTime _generatedTimestamp;

        [Header("Spatial Properties")]
        [SerializeField] private Vector2Int _mapSize;
        [SerializeField] private RectInt _mapBounds;

        [Header("Structural Data")]
        [SerializeField] private List<RoomData> _rooms;
        [SerializeField] private List<CorridorData> _corridors;
        [SerializeField] private BSPNode _rootNode; // Phase 1 BSP tree

        [Header("Gameplay Data (populated in Phase 2)")]
        [SerializeField] private Vector2Int _playerSpawnPosition;
        [SerializeField] private List<SpawnPointData> _enemySpawnPoints;
        [SerializeField] private List<ResourcePlacementData> _resources;

        [Header("Metadata")]
        [SerializeField] private MapMetadata _metadata;

        // Runtime-only references (not serialized)
        private Tilemap _floorTilemap;
        private Tilemap _wallTilemap;
        private Tilemap _objectTilemap;
        private Dictionary<int, RoomData> _roomLookup;
        private Dictionary<int, CorridorData> _corridorLookup;

        // Public Properties
        public int Seed => _seed;
        public string MapID => _mapID;
        public DateTime GeneratedTimestamp => _generatedTimestamp;
        public Vector2Int MapSize => _mapSize;
        public RectInt MapBounds => _mapBounds;
        public IReadOnlyList<RoomData> Rooms => _rooms.AsReadOnly();
        public IReadOnlyList<CorridorData> Corridors => _corridors.AsReadOnly();
        public BSPNode RootNode => _rootNode;
        public Vector2Int PlayerSpawnPosition => _playerSpawnPosition;
        public IReadOnlyList<SpawnPointData> EnemySpawnPoints => _enemySpawnPoints.AsReadOnly();
        public IReadOnlyList<ResourcePlacementData> Resources => _resources.AsReadOnly();
        public MapMetadata Metadata => _metadata;

        // Tilemap access (runtime only)
        public Tilemap FloorTilemap => _floorTilemap;
        public Tilemap WallTilemap => _wallTilemap;
        public Tilemap ObjectTilemap => _objectTilemap;

        // Constructor
        public MapData(int seed, Vector2Int mapSize)
        {
            _seed = seed;
            _mapSize = mapSize;
            _mapBounds = new RectInt(0, 0, mapSize.x, mapSize.y);
            _mapID = Guid.NewGuid().ToString();
            _generatedTimestamp = DateTime.UtcNow;

            _rooms = new List<RoomData>();
            _corridors = new List<CorridorData>();
            _enemySpawnPoints = new List<SpawnPointData>();
            _resources = new List<ResourcePlacementData>();
            _metadata = new MapMetadata();

            // Initialize runtime lookup tables
            _roomLookup = new Dictionary<int, RoomData>();
            _corridorLookup = new Dictionary<int, CorridorData>();
        }

        // Mutators (Phase 1: Structure)
        public void AddRoom(RoomData room)
        {
            if (room == null)
            {
                Debug.LogError("Attempted to add null room to map");
                return;
            }

            room.RoomID = _rooms.Count;
            _rooms.Add(room);
            _roomLookup[room.RoomID] = room;
        }

        public void RemoveRoom(RoomData room)
        {
            if (room != null && _rooms.Contains(room))
            {
                _rooms.Remove(room);
                _roomLookup.Remove(room.RoomID);
                
                // Remove connected corridors
                var connectedCorridors = _corridors.Where(c => c.ConnectsRoom(room.RoomID)).ToList();
                foreach (var corridor in connectedCorridors)
                {
                    RemoveCorridor(corridor);
                }
            }
        }

        public void AddCorridor(CorridorData corridor)
        {
            if (corridor == null)
            {
                Debug.LogError("Attempted to add null corridor to map");
                return;
            }

            corridor.CorridorID = _corridors.Count;
            _corridors.Add(corridor);
            _corridorLookup[corridor.CorridorID] = corridor;

            // Update room connections
            var roomA = GetRoomByID(corridor.RoomA_ID);
            var roomB = GetRoomByID(corridor.RoomB_ID);
            
            roomA?.ConnectToRoom(corridor.RoomB_ID);
            roomB?.ConnectToRoom(corridor.RoomA_ID);
        }

        public void RemoveCorridor(CorridorData corridor)
        {
            if (corridor != null && _corridors.Contains(corridor))
            {
                _corridors.Remove(corridor);
                _corridorLookup.Remove(corridor.CorridorID);

                // Update room connections
                var roomA = GetRoomByID(corridor.RoomA_ID);
                var roomB = GetRoomByID(corridor.RoomB_ID);
                
                roomA?.DisconnectFromRoom(corridor.RoomB_ID);
                roomB?.DisconnectFromRoom(corridor.RoomA_ID);
            }
        }

        public void SetBSPRoot(BSPNode root)
        {
            _rootNode = root;
        }

        // Mutators (Phase 2: Content)
        public void SetPlayerSpawn(Vector2Int position)
        {
            if (!_mapBounds.Contains(position))
            {
                Debug.LogWarning($"Player spawn position {position} is outside map bounds {_mapBounds}");
            }
            _playerSpawnPosition = position;
        }

        public void AddEnemySpawnPoint(SpawnPointData spawnPoint)
        {
            if (spawnPoint != null)
            {
                _enemySpawnPoints.Add(spawnPoint);
            }
        }

        public void AddResource(ResourcePlacementData resource)
        {
            if (resource != null)
            {
                _resources.Add(resource);
            }
        }

        // Tilemap Binding (Runtime Only)
        public void BindTilemaps(Tilemap floor, Tilemap wall, Tilemap objects)
        {
            _floorTilemap = floor;
            _wallTilemap = wall;
            _objectTilemap = objects;
        }

        // Query Methods
        public RoomData GetRoomByID(int roomID)
        {
            return _roomLookup.TryGetValue(roomID, out var room) ? room : null;
        }

        public CorridorData GetCorridorByID(int corridorID)
        {
            return _corridorLookup.TryGetValue(corridorID, out var corridor) ? corridor : null;
        }

        public RoomData GetRoomContainingPoint(Vector2Int point)
        {
            return _rooms.FirstOrDefault(room => room.ContainsPoint(point));
        }

        public List<RoomData> GetRoomsOfClassification(RoomClassification classification)
        {
            return _rooms.Where(room => room.Classification == classification).ToList();
        }

        public List<CorridorData> GetCorridorsConnectingRoom(int roomID)
        {
            return _corridors.Where(corridor => corridor.ConnectsRoom(roomID)).ToList();
        }

        public CorridorData GetCorridorConnectingRooms(int roomA, int roomB)
        {
            return _corridors.FirstOrDefault(corridor => 
                (corridor.RoomA_ID == roomA && corridor.RoomB_ID == roomB) ||
                (corridor.RoomA_ID == roomB && corridor.RoomB_ID == roomA));
        }

        public bool IsPointInCorridor(Vector2Int point)
        {
            return _corridors.Any(corridor => corridor.ContainsTile(point));
        }

        public bool IsPointWalkable(Vector2Int point)
        {
            if (!_mapBounds.Contains(point))
                return false;

            return GetRoomContainingPoint(point) != null || IsPointInCorridor(point);
        }

        public List<Vector2Int> GetWalkableTiles()
        {
            var walkableTiles = new List<Vector2Int>();

            // Add room tiles
            foreach (var room in _rooms)
            {
                for (int x = room.Bounds.x; x < room.Bounds.xMax; x++)
                {
                    for (int y = room.Bounds.y; y < room.Bounds.yMax; y++)
                    {
                        walkableTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Add corridor tiles
            foreach (var corridor in _corridors)
            {
                walkableTiles.AddRange(corridor.PathTiles);
            }

            // Remove duplicates
            return walkableTiles.Distinct().ToList();
        }

        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate basic properties
            if (_mapSize.x <= 0 || _mapSize.y <= 0)
                result.AddError($"Map has invalid size: {_mapSize}");

            if (_rooms.Count == 0)
                result.AddError("Map contains no rooms");

            if (string.IsNullOrEmpty(_mapID))
                result.AddError("Map has no ID");

            // Validate rooms
            foreach (var room in _rooms)
            {
                var roomResult = room.Validate();
                result.Merge(roomResult);

                // Check if room is within map bounds
                if (!_mapBounds.Contains(room.Bounds.min) || !_mapBounds.Contains(room.Bounds.max - Vector2Int.one))
                    result.AddError($"Room {room.RoomID} exceeds map bounds");
            }

            // Validate corridors
            foreach (var corridor in _corridors)
            {
                var corridorResult = corridor.Validate();
                result.Merge(corridorResult);

                // Check if corridor connects valid rooms
                if (GetRoomByID(corridor.RoomA_ID) == null)
                    result.AddError($"Corridor {corridor.CorridorID} connects to non-existent room {corridor.RoomA_ID}");

                if (GetRoomByID(corridor.RoomB_ID) == null)
                    result.AddError($"Corridor {corridor.CorridorID} connects to non-existent room {corridor.RoomB_ID}");
            }

            // Validate BSP tree
            if (_rootNode != null)
            {
                var bspResult = _rootNode.Validate();
                result.Merge(bspResult);
            }

            // Validate player spawn
            if (_playerSpawnPosition == Vector2Int.zero)
                result.AddWarning("Player spawn not set");
            else if (!IsPointWalkable(_playerSpawnPosition))
                result.AddError($"Player spawn position {_playerSpawnPosition} is not walkable");

            // Validate room IDs are unique
            var roomIDs = new HashSet<int>();
            foreach (var room in _rooms)
            {
                if (!roomIDs.Add(room.RoomID))
                    result.AddError($"Duplicate room ID: {room.RoomID}");
            }

            // Validate corridor IDs are unique
            var corridorIDs = new HashSet<int>();
            foreach (var corridor in _corridors)
            {
                if (!corridorIDs.Add(corridor.CorridorID))
                    result.AddError($"Duplicate corridor ID: {corridor.CorridorID}");
            }

            // Validate connectivity
            if (_rooms.Count > 1)
            {
                var connectedRooms = GetConnectedRoomsFromSpawn();
                if (connectedRooms.Count < _rooms.Count)
                {
                    result.AddError($"Only {connectedRooms.Count}/{_rooms.Count} rooms are reachable from player spawn");
                }
            }

            return result;
        }

        private HashSet<RoomData> GetConnectedRoomsFromSpawn()
        {
            var connected = new HashSet<RoomData>();
            var startRoom = GetRoomContainingPoint(_playerSpawnPosition);
            
            if (startRoom == null)
                return connected;

            var queue = new Queue<RoomData>();
            queue.Enqueue(startRoom);
            connected.Add(startRoom);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var connectedRoomID in current.ConnectedRoomIDs)
                {
                    var connectedRoom = GetRoomByID(connectedRoomID);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                    {
                        connected.Add(connectedRoom);
                        queue.Enqueue(connectedRoom);
                    }
                }
            }

            return connected;
        }

        // Serialization Support
        public MapDataSnapshot CreateSnapshot()
        {
            return new MapDataSnapshot(this);
        }

        // Statistics
        public MapStatistics GetStatistics()
        {
            return new MapStatistics
            {
                TotalRooms = _rooms.Count,
                TotalCorridors = _corridors.Count,
                TotalWalkableTiles = GetWalkableTiles().Count,
                MapSize = _mapSize,
                AverageRoomSize = _rooms.Count > 0 ? (float)_rooms.Average(r => r.Area) : 0f,
                TotalEnemySpawnPoints = _enemySpawnPoints.Count,
                TotalResources = _resources.Count,
                BSPDepth = _rootNode?.GetLeafNodes().Max(n => n.Depth) ?? 0
            };
        }

        // Utility Methods
        public void RebuildLookupTables()
        {
            _roomLookup.Clear();
            _corridorLookup.Clear();

            foreach (var room in _rooms)
            {
                _roomLookup[room.RoomID] = room;
            }

            foreach (var corridor in _corridors)
            {
                _corridorLookup[corridor.CorridorID] = corridor;
            }
        }

        public override string ToString()
        {
            return $"Map[{_mapID}] Seed:{_seed} Size:{_mapSize} Rooms:{_rooms.Count} Corridors:{_corridors.Count}";
        }
    }

    // Supporting classes for MapData
    [Serializable]
    public class SpawnPointData
    {
        [SerializeField] private int _roomID;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private string _enemyType;
        [SerializeField] private float _spawnDelay;

        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public string EnemyType => _enemyType;
        public float SpawnDelay => _spawnDelay;

        public SpawnPointData(int roomID, Vector2Int position, string enemyType, float spawnDelay = 0f)
        {
            _roomID = roomID;
            _position = position;
            _enemyType = enemyType;
            _spawnDelay = spawnDelay;
        }
    }

    [Serializable]
    public class ResourcePlacementData
    {
        [SerializeField] private int _roomID;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private string _resourceType;
        [SerializeField] private int _quantity;

        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public string ResourceType => _resourceType;
        public int Quantity => _quantity;

        public ResourcePlacementData(int roomID, Vector2Int position, string resourceType, int quantity = 1)
        {
            _roomID = roomID;
            _position = position;
            _resourceType = resourceType;
            _quantity = quantity;
        }
    }

    [Serializable]
    public class MapMetadata
    {
        [SerializeField] private string _version;
        [SerializeField] private string _generatorType;
        [SerializeField] private float _generationTime;
        [SerializeField] private Dictionary<string, string> _customProperties;

        public string Version => _version;
        public string GeneratorType => _generatorType;
        public float GenerationTime => _generationTime;
        public Dictionary<string, string> CustomProperties => _customProperties;

        public MapMetadata()
        {
            _version = "1.0";
            _generatorType = "BSP";
            _generationTime = 0f;
            _customProperties = new Dictionary<string, string>();
        }
    }

    [Serializable]
    public class MapStatistics
    {
        public int TotalRooms;
        public int TotalCorridors;
        public int TotalWalkableTiles;
        public Vector2Int MapSize;
        public float AverageRoomSize;
        public int TotalEnemySpawnPoints;
        public int TotalResources;
        public int BSPDepth;

        public float GetWalkablePercentage()
        {
            int totalTiles = MapSize.x * MapSize.y;
            return totalTiles > 0 ? (float)TotalWalkableTiles / totalTiles * 100f : 0f;
        }
    }
}