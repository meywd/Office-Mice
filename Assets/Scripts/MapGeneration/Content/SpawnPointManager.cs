using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Content
{
    /// <summary>
    /// Strategic spawn point generation system with intelligent positioning algorithms.
    /// Prioritizes corners, doorways, and cover positions while avoiding obstacles.
    /// Integrates with existing WaveSpawner system using "Spawn Point" tags.
    /// </summary>
    public class SpawnPointManager
    {
        #region Private Fields

        // Configuration and rules
        private readonly Dictionary<RoomClassification, SpawnDensityRule> _densityRules;
        private readonly SpawnTableConfiguration _spawnTableConfig;
        private readonly GridCollisionDetector _collisionDetector;

        // Performance tracking
        private readonly System.Diagnostics.Stopwatch _placementStopwatch;
        private int _roomsProcessed;
        private int _spawnPointsPlaced;
        private long _totalPlacementTime;

        // Random generation
        private System.Random _random;
        private int _seed;

        // Strategic positioning
        private readonly Dictionary<RoomClassification, List<SpawnPositionType>> _positionPriorities;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a spawn point is successfully placed.
        /// </summary>
        public event Action<SpawnPointData> OnSpawnPointPlaced;

        /// <summary>
        /// Fired when spawn point placement fails.
        /// </summary>
        public event Action<RoomData, string> OnSpawnPointPlacementFailed;

        #endregion

        #region Constructor

        public SpawnPointManager(SpawnTableConfiguration spawnTableConfig, int seed = 0)
        {
            _spawnTableConfig = spawnTableConfig ?? throw new ArgumentNullException(nameof(spawnTableConfig));
            _seed = seed;
            _random = new System.Random(_seed);
            
            _densityRules = new Dictionary<RoomClassification, SpawnDensityRule>();
            _positionPriorities = new Dictionary<RoomClassification, List<SpawnPositionType>>();
            
            _placementStopwatch = new System.Diagnostics.Stopwatch();
            _roomsProcessed = 0;
            _spawnPointsPlaced = 0;
            _totalPlacementTime = 0;

            InitializeDefaultRules();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Places spawn points in all rooms of the map according to strategic rules.
        /// </summary>
        /// <param name="map">Map data to populate with spawn points</param>
        /// <param name="existingFurniture">Already placed furniture to avoid</param>
        /// <returns>List of all placed spawn points</returns>
        public List<SpawnPointData> PlaceSpawnPoints(MapData map, List<FurnitureData> existingFurniture)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (existingFurniture == null) throw new ArgumentNullException(nameof(existingFurniture));

            _placementStopwatch.Restart();
            var placedSpawnPoints = new List<SpawnPointData>();

            try
            {
                // Initialize collision detector with existing furniture
                _collisionDetector = new GridCollisionDetector(map.MapSize);
                foreach (var furniture in existingFurniture)
                {
                    _collisionDetector.AddObject(furniture);
                }

                // Process each room
                foreach (var room in map.Rooms)
                {
                    var roomSpawnPoints = PlaceSpawnPointsInRoom(room, existingFurniture);
                    placedSpawnPoints.AddRange(roomSpawnPoints);
                    _roomsProcessed++;
                }

                _spawnPointsPlaced = placedSpawnPoints.Count;
                return placedSpawnPoints;
            }
            finally
            {
                _placementStopwatch.Stop();
                _totalPlacementTime = _placementStopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Places spawn points in a single room.
        /// </summary>
        /// <param name="room">Room to populate with spawn points</param>
        /// <param name="existingFurniture">Already placed furniture to avoid</param>
        /// <returns>List of spawn points placed in the room</returns>
        public List<SpawnPointData> PlaceSpawnPointsInRoom(RoomData room, List<FurnitureData> existingFurniture)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (existingFurniture == null) throw new ArgumentNullException(nameof(existingFurniture));

            var spawnPoints = new List<SpawnPointData>();

            // Get density rule for this room type
            if (!_densityRules.TryGetValue(room.Classification, out var densityRule))
            {
                Debug.LogWarning($"No spawn density rule found for room type: {room.Classification}");
                return spawnPoints;
            }

            // Calculate spawn count for this room
            int spawnCount = CalculateSpawnCount(room, densityRule);
            if (spawnCount <= 0)
                return spawnPoints;

            // Get strategic position priorities
            var positionPriorities = _positionPriorities.GetValueOrDefault(room.Classification, 
                new List<SpawnPositionType> { SpawnPositionType.Corner, SpawnPositionType.NearDoorway, SpawnPositionType.Cover });

            // Generate strategic positions
            var strategicPositions = GenerateStrategicPositions(room, positionPriorities, existingFurniture, spawnCount);

            // Place spawn points at valid positions
            foreach (var position in strategicPositions.Take(spawnCount))
            {
                var spawnPoint = CreateSpawnPoint(room, position);
                if (spawnPoint != null)
                {
                    spawnPoints.Add(spawnPoint);
                    OnSpawnPointPlaced?.Invoke(spawnPoint);
                }
            }

            return spawnPoints;
        }

        /// <summary>
        /// Validates that spawn points are properly placed and accessible.
        /// </summary>
        /// <param name="map">Map with spawn points to validate</param>
        /// <param name="spawnPoints">Spawn points to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        public ValidationResult ValidateSpawnPoints(MapData map, List<SpawnPointData> spawnPoints)
        {
            var result = new ValidationResult();

            if (map == null)
            {
                result.AddError("Map is null");
                return result;
            }

            if (spawnPoints == null)
            {
                result.AddError("Spawn points list is null");
                return result;
            }

            // Validate each spawn point
            foreach (var spawnPoint in spawnPoints)
            {
                // Check room validity
                var room = map.GetRoom(spawnPoint.RoomID);
                if (room == null)
                {
                    result.AddError($"Spawn point {spawnPoint.Position} references invalid room {spawnPoint.RoomID}");
                    continue;
                }

                // Check position is within room bounds
                if (!room.ContainsPoint(spawnPoint.Position))
                {
                    result.AddError($"Spawn point {spawnPoint.Position} is outside room {room.RoomID} bounds");
                }

                // Check for obstacles
                if (_collisionDetector != null)
                {
                    var tempObj = new TempSpawnObject(spawnPoint.RoomID, spawnPoint.Position);
                    if (_collisionDetector.HasCollision(tempObj))
                    {
                        result.AddWarning($"Spawn point {spawnPoint.Position} may have obstacles nearby");
                    }
                }

                // Check enemy type validity
                if (string.IsNullOrEmpty(spawnPoint.EnemyType))
                {
                    result.AddWarning($"Spawn point {spawnPoint.Position} has no enemy type specified");
                }

                // Validate spawn delay
                if (spawnPoint.SpawnDelay < 0)
                {
                    result.AddError($"Spawn point {spawnPoint.Position} has negative spawn delay: {spawnPoint.SpawnDelay}");
                }
            }

            // Check for spawn point clustering
            ValidateSpawnClustering(spawnPoints, result);

            // Check room capacity limits
            ValidateRoomCapacity(map, spawnPoints, result);

            return result;
        }

        /// <summary>
        /// Sets the random seed for deterministic spawn point placement.
        /// </summary>
        /// <param name="seed">Seed value</param>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new System.Random(_seed);
        }

        /// <summary>
        /// Adds a spawn density rule for a room type.
        /// </summary>
        /// <param name="roomType">Room classification</param>
        /// <param name="rule">Density rule</param>
        public void AddDensityRule(RoomClassification roomType, SpawnDensityRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _densityRules[roomType] = rule;
        }

        /// <summary>
        /// Sets position priorities for a room type.
        /// </summary>
        /// <param name="roomType">Room classification</param>
        /// <param name="priorities">List of position types in priority order</param>
        public void SetPositionPriorities(RoomClassification roomType, List<SpawnPositionType> priorities)
        {
            if (priorities == null) throw new ArgumentNullException(nameof(priorities));
            _positionPriorities[roomType] = new List<SpawnPositionType>(priorities);
        }

        /// <summary>
        /// Gets performance statistics for the spawn point placement system.
        /// </summary>
        /// <returns>Performance statistics</returns>
        public (int roomsProcessed, int spawnPointsPlaced, long totalMs, float avgMsPerRoom) GetPerformanceStats()
        {
            float avgMsPerRoom = _roomsProcessed > 0 ? (float)_totalPlacementTime / _roomsProcessed : 0f;
            return (_roomsProcessed, _spawnPointsPlaced, _totalPlacementTime, avgMsPerRoom);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the number of spawn points for a room based on density rules.
        /// </summary>
        private int CalculateSpawnCount(RoomData room, SpawnDensityRule rule)
        {
            // Base count from room size
            float areaRatio = (float)room.Area / rule.ReferenceArea;
            int baseCount = Mathf.RoundToInt(rule.BaseDensity * areaRatio);

            // Apply room type modifier
            float roomTypeModifier = GetRoomTypeModifier(room.Classification);
            int modifiedCount = Mathf.RoundToInt(baseCount * roomTypeModifier);

            // Apply random variance
            float variance = (float)(_random.NextDouble() * 2 - 1) * rule.Variance;
            int finalCount = Mathf.RoundToInt(modifiedCount * (1f + variance));

            // Clamp to min/max bounds
            return Mathf.Clamp(finalCount, rule.MinPerRoom, rule.MaxPerRoom);
        }

        /// <summary>
        /// Generates strategic positions for spawn points in a room.
        /// </summary>
        private List<Vector2Int> GenerateStrategicPositions(RoomData room, List<SpawnPositionType> priorities, 
                                                        List<FurnitureData> existingFurniture, int targetCount)
        {
            var positions = new List<Vector2Int>();
            var roomFurniture = existingFurniture.Where(f => f.RoomID == room.RoomID).ToList();

            // Generate positions by priority
            foreach (var priority in priorities)
            {
                if (positions.Count >= targetCount)
                    break;

                var priorityPositions = priority switch
                {
                    SpawnPositionType.Corner => GenerateCornerPositions(room, roomFurniture),
                    SpawnPositionType.NearDoorway => GenerateDoorwayPositions(room, roomFurniture),
                    SpawnPositionType.Cover => GenerateCoverPositions(room, roomFurniture),
                    SpawnPositionType.Center => GenerateCenterPositions(room, roomFurniture),
                    SpawnPositionType.Perimeter => GeneratePerimeterPositions(room, roomFurniture),
                    _ => GenerateRandomPositions(room, roomFurniture, targetCount - positions.Count)
                };

                // Shuffle and add positions
                priorityPositions = priorityPositions.OrderBy(x => _random.Next()).ToList();
                positions.AddRange(priorityPositions);

                // Remove duplicates
                positions = positions.Distinct().ToList();
            }

            // Limit to target count
            return positions.Take(targetCount).ToList();
        }

        /// <summary>
        /// Generates corner positions in a room.
        /// </summary>
        private List<Vector2Int> GenerateCornerPositions(RoomData room, List<FurnitureData> furniture)
        {
            var positions = new List<Vector2Int>();
            var bounds = room.Bounds;
            int offset = 1; // Offset from walls

            // Four corners
            var corners = new[]
            {
                new Vector2Int(bounds.x + offset, bounds.y + offset),
                new Vector2Int(bounds.xMax - offset - 1, bounds.y + offset),
                new Vector2Int(bounds.x + offset, bounds.yMax - offset - 1),
                new Vector2Int(bounds.xMax - offset - 1, bounds.yMax - offset - 1)
            };

            foreach (var corner in corners)
            {
                if (IsValidSpawnPosition(corner, room, furniture))
                {
                    positions.Add(corner);
                }
            }

            return positions;
        }

        /// <summary>
        /// Generates positions near doorways.
        /// </summary>
        private List<Vector2Int> GenerateDoorwayPositions(RoomData room, List<FurnitureData> furniture)
        {
            var positions = new List<Vector2Int>();

            foreach (var doorway in room.Doorways)
            {
                // Generate positions around doorway
                for (int offset = 2; offset <= 4; offset++)
                {
                    var positionsAroundDoorway = new[]
                    {
                        doorway.position + new Vector2Int(offset, 0),
                        doorway.position + new Vector2Int(-offset, 0),
                        doorway.position + new Vector2Int(0, offset),
                        doorway.position + new Vector2Int(0, -offset)
                    };

                    foreach (var pos in positionsAroundDoorway)
                    {
                        if (room.ContainsPoint(pos) && IsValidSpawnPosition(pos, room, furniture))
                        {
                            positions.Add(pos);
                        }
                    }
                }
            }

            return positions.Distinct().ToList();
        }

        /// <summary>
        /// Generates cover positions behind furniture.
        /// </summary>
        private List<Vector2Int> GenerateCoverPositions(RoomData room, List<FurnitureData> furniture)
        {
            var positions = new List<Vector2Int>();

            foreach (var furn in furniture.Where(f => f.BlocksSight))
            {
                // Generate positions behind furniture for cover
                var coverOffsets = new[]
                {
                    new Vector2Int(furn.Size.x + 1, 0),
                    new Vector2Int(-furn.Size.x - 1, 0),
                    new Vector2Int(0, furn.Size.y + 1),
                    new Vector2Int(0, -furn.Size.y - 1)
                };

                foreach (var offset in coverOffsets)
                {
                    var coverPos = furn.Position + offset;
                    if (room.ContainsPoint(coverPos) && IsValidSpawnPosition(coverPos, room, furniture))
                    {
                        positions.Add(coverPos);
                    }
                }
            }

            return positions.Distinct().ToList();
        }

        /// <summary>
        /// Generates center positions in a room.
        /// </summary>
        private List<Vector2Int> GenerateCenterPositions(RoomData room, List<FurnitureData> furniture)
        {
            var positions = new List<Vector2Int>();
            var center = room.Center;

            // Generate positions in a small grid around the center
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    var pos = center + new Vector2Int(x, y);
                    if (room.ContainsPoint(pos) && IsValidSpawnPosition(pos, room, furniture))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Generates perimeter positions along room walls.
        /// </summary>
        private List<Vector2Int> GeneratePerimeterPositions(RoomData room, List<FurnitureData> furniture)
        {
            var positions = new List<Vector2Int>();
            var bounds = room.Bounds;
            int offset = 1;

            // Top and bottom walls
            for (int x = bounds.x + offset; x < bounds.xMax - offset; x += 2)
            {
                var topPos = new Vector2Int(x, bounds.y + offset);
                var bottomPos = new Vector2Int(x, bounds.yMax - offset - 1);

                if (IsValidSpawnPosition(topPos, room, furniture))
                    positions.Add(topPos);
                if (IsValidSpawnPosition(bottomPos, room, furniture))
                    positions.Add(bottomPos);
            }

            // Left and right walls
            for (int y = bounds.y + offset; y < bounds.yMax - offset; y += 2)
            {
                var leftPos = new Vector2Int(bounds.x + offset, y);
                var rightPos = new Vector2Int(bounds.xMax - offset - 1, y);

                if (IsValidSpawnPosition(leftPos, room, furniture))
                    positions.Add(leftPos);
                if (IsValidSpawnPosition(rightPos, room, furniture))
                    positions.Add(rightPos);
            }

            return positions;
        }

        /// <summary>
        /// Generates random positions in a room.
        /// </summary>
        private List<Vector2Int> GenerateRandomPositions(RoomData room, List<FurnitureData> furniture, int count)
        {
            var positions = new List<Vector2Int>();
            var attempts = 0;
            var maxAttempts = count * 10;

            while (positions.Count < count && attempts < maxAttempts)
            {
                var pos = room.GetRandomPoint(_random);
                if (IsValidSpawnPosition(pos, room, furniture))
                {
                    positions.Add(pos);
                }
                attempts++;
            }

            return positions;
        }

        /// <summary>
        /// Validates if a position is suitable for spawning.
        /// </summary>
        private bool IsValidSpawnPosition(Vector2Int position, RoomData room, List<FurnitureData> furniture)
        {
            // Check if position is within room bounds
            if (!room.ContainsPoint(position))
                return false;

            // Check if position is not too close to doorways (to avoid blocking)
            foreach (var doorway in room.Doorways)
            {
                if (Vector2Int.Distance(position, doorway.position) < 2)
                    return false;
            }

            // Check collision with furniture
            if (_collisionDetector != null)
            {
                var tempObj = new TempSpawnObject(room.RoomID, position);
                if (_collisionDetector.HasCollision(tempObj))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a spawn point at the specified position.
        /// </summary>
        private SpawnPointData CreateSpawnPoint(RoomData room, Vector2Int position)
        {
            // Select enemy type from spawn table configuration
            string enemyType = SelectEnemyType(room);
            
            // Calculate spawn delay
            float spawnDelay = CalculateSpawnDelay(room);

            return new SpawnPointData(room.RoomID, position, enemyType, spawnDelay);
        }

        /// <summary>
        /// Selects an appropriate enemy type for the room.
        /// </summary>
        private string SelectEnemyType(RoomData room)
        {
            if (_spawnTableConfig?.SpawnEntries == null || _spawnTableConfig.SpawnEntries.Count == 0)
            {
                // Fallback to default enemy types
                var defaultEnemies = new[] { "Mouse", "Rat", "Bug" };
                return defaultEnemies[_random.Next(defaultEnemies.Length)];
            }

            // Use spawn table configuration
            var spawnEntry = _spawnTableConfig.GetRandomSpawnEntry(_random);
            return spawnEntry?.EnemyType ?? "Mouse";
        }

        /// <summary>
        /// Calculates spawn delay based on room type and configuration.
        /// </summary>
        private float CalculateSpawnDelay(RoomData room)
        {
            // Base delay with room type modification
            float baseDelay = room.Classification switch
            {
                RoomClassification.Office => 1.0f,
                RoomClassification.Conference => 1.5f,
                RoomClassification.BreakRoom => 0.8f,
                RoomClassification.Storage => 0.5f,
                RoomClassification.ServerRoom => 1.2f,
                _ => 1.0f
            };

            // Add random variance
            float variance = (float)(_random.NextDouble() * 0.5 - 0.25); // ±0.25 seconds
            return Mathf.Max(0f, baseDelay + variance);
        }

        /// <summary>
        /// Gets room type modifier for spawn density.
        /// </summary>
        private float GetRoomTypeModifier(RoomClassification roomType)
        {
            return roomType switch
            {
                RoomClassification.Office => 1.0f,
                RoomClassification.Conference => 1.2f,
                RoomClassification.BreakRoom => 0.8f,
                RoomClassification.Storage => 0.6f,
                RoomClassification.ServerRoom => 1.1f,
                RoomClassification.Lobby => 1.5f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Validates spawn point clustering.
        /// </summary>
        private void ValidateSpawnClustering(List<SpawnPointData> spawnPoints, ValidationResult result)
        {
            const float minDistance = 3f;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                for (int j = i + 1; j < spawnPoints.Count; j++)
                {
                    var distance = Vector2Int.Distance(spawnPoints[i].Position, spawnPoints[j].Position);
                    if (distance < minDistance)
                    {
                        result.AddWarning($"Spawn points {spawnPoints[i].Position} and {spawnPoints[j].Position} are too close ({distance:F1} < {minDistance})");
                    }
                }
            }
        }

        /// <summary>
        /// Validates room capacity limits.
        /// </summary>
        private void ValidateRoomCapacity(MapData map, List<SpawnPointData> spawnPoints, ValidationResult result)
        {
            var spawnPointsByRoom = spawnPoints.GroupBy(sp => sp.RoomID);

            foreach (var group in spawnPointsByRoom)
            {
                var room = map.GetRoom(group.Key);
                if (room == null)
                    continue;

                int maxCapacity = room.Classification switch
                {
                    RoomClassification.Office => 8,
                    RoomClassification.Conference => 12,
                    RoomClassification.BreakRoom => 6,
                    RoomClassification.Storage => 4,
                    RoomClassification.ServerRoom => 5,
                    RoomClassification.Lobby => 15,
                    _ => 8
                };

                if (group.Count() > maxCapacity)
                {
                    result.AddWarning($"Room {room.RoomID} ({room.Classification}) has {group.Count()} spawn points, exceeding recommended capacity of {maxCapacity}");
                }
            }
        }

        /// <summary>
        /// Initializes default spawn density rules and position priorities.
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Default density rules
            _densityRules[RoomClassification.Office] = new SpawnDensityRule(1.2f, 50, 0.3f, 1, 6);
            _densityRules[RoomClassification.Conference] = new SpawnDensityRule(1.5f, 80, 0.2f, 2, 8);
            _densityRules[RoomClassification.BreakRoom] = new SpawnDensityRule(0.8f, 40, 0.4f, 1, 4);
            _densityRules[RoomClassification.Storage] = new SpawnDensityRule(0.6f, 30, 0.5f, 1, 3);
            _densityRules[RoomClassification.ServerRoom] = new SpawnDensityRule(1.0f, 35, 0.3f, 1, 5);
            _densityRules[RoomClassification.Lobby] = new SpawnDensityRule(2.0f, 100, 0.2f, 3, 12);

            // Default position priorities
            _positionPriorities[RoomClassification.Office] = new List<SpawnPositionType>
            { SpawnPositionType.Corner, SpawnPositionType.Cover, SpawnPositionType.NearDoorway };
            
            _positionPriorities[RoomClassification.Conference] = new List<SpawnPositionType>
            { SpawnPositionType.Perimeter, SpawnPositionType.Corner, SpawnPositionType.NearDoorway };
            
            _positionPriorities[RoomClassification.BreakRoom] = new List<SpawnPositionType>
            { SpawnPositionType.Cover, SpawnPositionType.Corner, SpawnPositionType.Center };
            
            _positionPriorities[RoomClassification.Storage] = new List<SpawnPositionType>
            { SpawnPositionType.Cover, SpawnPositionType.Perimeter, SpawnPositionType.Corner };
            
            _positionPriorities[RoomClassification.ServerRoom] = new List<SpawnPositionType>
            { SpawnPositionType.Perimeter, SpawnPositionType.Corner, SpawnPositionType.Cover };
            
            _positionPriorities[RoomClassification.Lobby] = new List<SpawnPositionType>
            { SpawnPositionType.Corner, SpawnPositionType.NearDoorway, SpawnPositionType.Perimeter };
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Temporary spawn object for collision detection.
        /// </summary>
        private class TempSpawnObject : PlacedObjectData
        {
            public TempSpawnObject(int roomID, Vector2Int position) 
                : base("temp_spawn", "temp", roomID, position, Vector2Int.one)
            {
                SetCollisionProperties(false, false);
            }

            public override PlacedObjectData Clone()
            {
                return new TempSpawnObject(RoomID, Position);
            }
        }

        #endregion
    }

    /// <summary>
    /// Configuration for spawn point density in rooms.
    /// </summary>
    [Serializable]
    public class SpawnDensityRule
    {
        [Header("Density Settings")]
        [SerializeField] private float _baseDensity;
        [SerializeField] private int _referenceArea;
        [SerializeField, Range(0f, 1f)] private float _variance;
        [SerializeField] private int _minPerRoom;
        [SerializeField] private int _maxPerRoom;

        public float BaseDensity => _baseDensity;
        public int ReferenceArea => _referenceArea;
        public float Variance => _variance;
        public int MinPerRoom => _minPerRoom;
        public int MaxPerRoom => _maxPerRoom;

        public SpawnDensityRule(float baseDensity, int referenceArea, float variance, int minPerRoom, int maxPerRoom)
        {
            _baseDensity = baseDensity;
            _referenceArea = referenceArea;
            _variance = Mathf.Clamp01(variance);
            _minPerRoom = Mathf.Max(0, minPerRoom);
            _maxPerRoom = Mathf.Max(_minPerRoom, maxPerRoom);
        }
    }

    /// <summary>
    /// Types of strategic positions for spawn points.
    /// </summary>
    public enum SpawnPositionType
    {
        Corner,
        NearDoorway,
        Cover,
        Center,
        Perimeter,
        Random
    }
}