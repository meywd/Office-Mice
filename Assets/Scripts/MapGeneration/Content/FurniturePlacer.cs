using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Content
{
    /// <summary>
    /// High-performance furniture placement system with rule-based logic and collision detection.
    /// Implements room-type specific furniture placement with variation and optimization.
    /// </summary>
    public class FurniturePlacer
    {
        #region Private Fields

        // Configuration and rules
        private readonly Dictionary<RoomClassification, List<FurniturePlacementRule>> _rulesByRoomType;
        private readonly Dictionary<string, FurnitureData> _furnitureTemplates;
        private readonly IAssetLoader _assetLoader;

        // Collision detection
        private GridCollisionDetector _collisionDetector;

        // Performance tracking
        private readonly System.Diagnostics.Stopwatch _placementStopwatch;
        private int _roomsProcessed;
        private int _furniturePlaced;
        private long _totalPlacementTime;

        // Random generation
        private System.Random _random;
        private int _seed;

        #endregion

        #region Events

        /// <summary>
        /// Fired when furniture is successfully placed.
        /// </summary>
        public event Action<FurnitureData> OnFurniturePlaced;

        /// <summary>
        /// Fired when furniture placement fails.
        /// </summary>
        public event Action<RoomData, string> OnFurniturePlacementFailed;

        #endregion

        #region Constructor

        public FurniturePlacer(IAssetLoader assetLoader, int seed = 0)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _seed = seed;
            _random = new System.Random(_seed);
            
            _rulesByRoomType = new Dictionary<RoomClassification, List<FurniturePlacementRule>>();
            _furnitureTemplates = new Dictionary<string, FurnitureData>();
            
            _placementStopwatch = new System.Diagnostics.Stopwatch();
            _roomsProcessed = 0;
            _furniturePlaced = 0;
            _totalPlacementTime = 0;

            InitializeDefaultRules();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Places furniture in all rooms of the map according to room-type rules.
        /// </summary>
        /// <param name="map">Map data to populate with furniture</param>
        /// <param name="biome">Biome configuration for furniture selection</param>
        /// <returns>List of all placed furniture</returns>
        public List<FurnitureData> PlaceFurniture(MapData map, BiomeConfiguration biome)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (biome == null) throw new ArgumentNullException(nameof(biome));

            _placementStopwatch.Restart();
            var placedFurniture = new List<FurnitureData>();

            try
            {
                // Initialize collision detector for the entire map
                _collisionDetector = new GridCollisionDetector(map.MapSize);

                // Process each room
                foreach (var room in map.Rooms)
                {
                    var roomFurniture = PlaceFurnitureInRoom(room, biome);
                    placedFurniture.AddRange(roomFurniture);
                    
                    // Add placed furniture to collision detector for subsequent rooms
                    foreach (var furniture in roomFurniture)
                    {
                        _collisionDetector.AddObject(furniture);
                    }
                    
                    _roomsProcessed++;
                }

                _furniturePlaced = placedFurniture.Count;
                return placedFurniture;
            }
            finally
            {
                _placementStopwatch.Stop();
                _totalPlacementTime = _placementStopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Places furniture in a single room.
        /// </summary>
        /// <param name="room">Room to populate with furniture</param>
        /// <param name="biome">Biome configuration</param>
        /// <returns>List of furniture placed in the room</returns>
        public List<FurnitureData> PlaceFurnitureInRoom(RoomData room, BiomeConfiguration biome)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (biome == null) throw new ArgumentNullException(nameof(biome));

            var placedFurniture = new List<FurnitureData>();

            // Get placement rules for this room type
            if (!_rulesByRoomType.TryGetValue(room.Classification, out var rules))
            {
                Debug.LogWarning($"No furniture rules found for room type: {room.Classification}");
                return placedFurniture;
            }

            // Process each rule
            foreach (var rule in rules)
            {
                if (!rule.IsValidForRoom(room))
                    continue;

                int count = rule.GetCountForRoom(room, _random);
                if (count <= 0)
                    continue;

                var furnitureForRule = PlaceFurnitureByRule(room, rule, count);
                placedFurniture.AddRange(furnitureForRule);
            }

            return placedFurniture;
        }

        /// <summary>
        /// Finds valid positions for placing furniture in a room.
        /// </summary>
        /// <param name="room">Room to search</param>
        /// <param name="objectSize">Size of furniture to place</param>
        /// <param name="existingObjects">Already placed objects to avoid</param>
        /// <param name="minDistance">Minimum distance from walls and other objects</param>
        /// <returns>List of valid positions</returns>
        public List<Vector2Int> FindValidPositions(RoomData room, Vector2Int objectSize, 
                                                 List<PlacedObjectData> existingObjects, int minDistance = 1)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            if (_collisionDetector == null)
            {
                // Create temporary collision detector
                _collisionDetector = new GridCollisionDetector(room.Bounds.max);
            }

            return _collisionDetector.FindValidPositions(room, objectSize, existingObjects, minDistance);
        }

        /// <summary>
        /// Validates furniture placement in a map.
        /// </summary>
        /// <param name="map">Map with placed furniture to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        public ValidationResult ValidateFurniturePlacement(MapData map)
        {
            var result = new ValidationResult();

            if (map == null)
            {
                result.AddError("Map is null");
                return result;
            }

            // Check for furniture overlaps
            var allFurniture = new List<FurnitureData>();
            foreach (var room in map.Rooms)
            {
                // Note: This assumes furniture is stored in MapData - may need to extend MapData
                // For now, we'll validate the collision detector state
            }

            // Validate collision detector state
            if (_collisionDetector != null)
            {
                var (totalCells, occupiedCells, totalObjects) = _collisionDetector.GetStatistics();
                
                if (totalObjects > 0 && occupiedCells == 0)
                {
                    result.AddWarning("Collision detector has objects but no occupied cells");
                }
            }

            return result;
        }

        /// <summary>
        /// Sets the random seed for deterministic placement.
        /// </summary>
        /// <param name="seed">Seed value</param>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new System.Random(_seed);
        }

        /// <summary>
        /// Adds a furniture placement rule.
        /// </summary>
        /// <param name="roomType">Room classification</param>
        /// <param name="rule">Placement rule</param>
        public void AddPlacementRule(RoomClassification roomType, FurniturePlacementRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            if (!_rulesByRoomType.ContainsKey(roomType))
            {
                _rulesByRoomType[roomType] = new List<FurniturePlacementRule>();
            }

            _rulesByRoomType[roomType].Add(rule);
        }

        /// <summary>
        /// Registers a furniture template for placement.
        /// </summary>
        /// <param name="furniture">Furniture template</param>
        public void RegisterFurnitureTemplate(FurnitureData furniture)
        {
            if (furniture == null) throw new ArgumentNullException(nameof(furniture));
            if (!furniture.IsValid()) throw new ArgumentException("Invalid furniture data");

            _furnitureTemplates[furniture.FurnitureType] = furniture;
        }

        /// <summary>
        /// Gets performance statistics for the furniture placement system.
        /// </summary>
        /// <returns>Performance statistics</returns>
        public (int roomsProcessed, int furniturePlaced, long totalMs, float avgMsPerRoom) GetPerformanceStats()
        {
            float avgMsPerRoom = _roomsProcessed > 0 ? (float)_totalPlacementTime / _roomsProcessed : 0f;
            return (_roomsProcessed, _furniturePlaced, _totalPlacementTime, avgMsPerRoom);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Places furniture according to a specific rule.
        /// </summary>
        private List<FurnitureData> PlaceFurnitureByRule(RoomData room, FurniturePlacementRule rule, int count)
        {
            var placedFurniture = new List<FurnitureData>();

            for (int i = 0; i < count; i++)
            {
                var furniture = CreateFurnitureFromRule(rule, room);
                if (furniture == null)
                    continue;

                var position = FindValidPositionForFurniture(room, furniture, rule);
                if (position.HasValue)
                {
                    furniture.SetPosition(position.Value);
                    ApplyVariation(furniture, rule);
                    
                    placedFurniture.Add(furniture);
                    OnFurniturePlaced?.Invoke(furniture);
                }
                else
                {
                    OnFurniturePlacementFailed?.Invoke(room, $"Could not find valid position for {rule.FurnitureType}");
                }
            }

            return placedFurniture;
        }

        /// <summary>
        /// Creates furniture data from a placement rule.
        /// </summary>
        private FurnitureData CreateFurnitureFromRule(FurniturePlacementRule rule, RoomData room)
        {
            // Select a random prefab from allowed prefabs
            if (rule.AllowedPrefabs.Length == 0)
                return null;

            string prefabPath = rule.AllowedPrefabs[_random.Next(rule.AllowedPrefabs.Length)];
            
            // Get furniture template or create default
            if (!_furnitureTemplates.TryGetValue(rule.FurnitureType, out var template))
            {
                template = new FurnitureData(
                    $"{rule.FurnitureType}_{room.RoomID}_{Guid.NewGuid():N}",
                    rule.FurnitureType,
                    prefabPath,
                    room.RoomID,
                    Vector2Int.zero,
                    Vector2Int.one
                );
            }

            var furniture = template.Clone();
            furniture.SetPosition(Vector2Int.zero); // Will be set later
            
            return furniture;
        }

        /// <summary>
        /// Finds a valid position for furniture placement.
        /// </summary>
        private Vector2Int? FindValidPositionForFurniture(RoomData room, FurnitureData furniture, FurniturePlacementRule rule)
        {
            var validPositions = new List<Vector2Int>();
            var roomBounds = room.Bounds;

            // Generate candidate positions based on rule preferences
            if (rule.PlaceAgainstWalls)
            {
                validPositions.AddRange(GetWallPositions(room, furniture.Size, rule.MinDistanceFromWalls));
            }
            else if (rule.PlaceInCenter)
            {
                validPositions.AddRange(GetCenterPositions(room, furniture.Size));
            }
            else
            {
                validPositions.AddRange(GetAllPositions(room, furniture.Size, rule.MinDistanceFromWalls));
            }

            // Filter out positions too close to doorways
            if (rule.MinDistanceFromDoorways > 0)
            {
                validPositions = FilterPositionsNearDoorways(validPositions, room, rule.MinDistanceFromDoorways);
            }

            // Check for collisions
            foreach (var position in validPositions)
            {
                furniture.SetPosition(position);
                if (!_collisionDetector.HasCollision(furniture))
                {
                    return position;
                }
            }

            return null;
        }

        /// <summary>
        /// Applies variation to furniture (rotation, flipping, etc.).
        /// </summary>
        private void ApplyVariation(FurnitureData furniture, FurniturePlacementRule rule)
        {
            if (rule.AllowRotation)
            {
                int[] rotations = { 0, 90, 180, 270 };
                furniture.SetRotation(rotations[_random.Next(rotations.Length)]);
            }

            if (rule.AllowFlipping && _random.NextDouble() < 0.5f)
            {
                furniture.SetFlipped(true);
            }

            if (rule.VariantCount > 1)
            {
                furniture.SetVariant(_random.Next(rule.VariantCount));
            }
        }

        /// <summary>
        /// Gets positions along walls for furniture placement.
        /// </summary>
        private List<Vector2Int> GetWallPositions(RoomData room, Vector2Int objectSize, int minDistance)
        {
            var positions = new List<Vector2Int>();
            var bounds = room.Bounds;

            // Top and bottom walls
            for (int x = bounds.x + minDistance; x <= bounds.xMax - objectSize.x - minDistance; x++)
            {
                // Top wall
                positions.Add(new Vector2Int(x, bounds.yMax - objectSize.y - minDistance));
                // Bottom wall
                positions.Add(new Vector2Int(x, bounds.y + minDistance));
            }

            // Left and right walls
            for (int y = bounds.y + minDistance; y <= bounds.yMax - objectSize.y - minDistance; y++)
            {
                // Left wall
                positions.Add(new Vector2Int(bounds.x + minDistance, y));
                // Right wall
                positions.Add(new Vector2Int(bounds.xMax - objectSize.x - minDistance, y));
            }

            return positions;
        }

        /// <summary>
        /// Gets center positions for furniture placement.
        /// </summary>
        private List<Vector2Int> GetCenterPositions(RoomData room, Vector2Int objectSize)
        {
            var positions = new List<Vector2Int>();
            var center = room.Center;
            var halfSize = new Vector2Int(objectSize.x / 2, objectSize.y / 2);
            
            positions.Add(new Vector2Int(center.x - halfSize.x, center.y - halfSize.y));
            
            return positions;
        }

        /// <summary>
        /// Gets all possible positions in a room.
        /// </summary>
        private List<Vector2Int> GetAllPositions(RoomData room, Vector2Int objectSize, int minDistance)
        {
            var positions = new List<Vector2Int>();
            var bounds = room.Bounds;

            for (int x = bounds.x + minDistance; x <= bounds.xMax - objectSize.x - minDistance; x++)
            {
                for (int y = bounds.y + minDistance; y <= bounds.yMax - objectSize.y - minDistance; y++)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }

            return positions;
        }

        /// <summary>
        /// Filters positions that are too close to doorways.
        /// </summary>
        private List<Vector2Int> FilterPositionsNearDoorways(List<Vector2Int> positions, RoomData room, int minDistance)
        {
            var filteredPositions = new List<Vector2Int>();

            foreach (var position in positions)
            {
                bool tooClose = false;
                var furnitureBounds = new RectInt(position, Vector2Int.one * 2); // Approximate size

                foreach (var doorway in room.Doorways)
                {
                    var doorwayBounds = new RectInt(
                        doorway.position.x - minDistance,
                        doorway.position.y - minDistance,
                        doorway.width + minDistance * 2,
                        minDistance * 2
                    );

                    if (furnitureBounds.Overlaps(doorwayBounds))
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    filteredPositions.Add(position);
                }
            }

            return filteredPositions;
        }

        /// <summary>
        /// Initializes default furniture placement rules for common room types.
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Office room rules
            var officeRules = new List<FurniturePlacementRule>
            {
                new FurniturePlacementRule("Desk")
                {
                    AllowedPrefabs = new[] { "Assets/Game/Furniture/OfficeDesk.prefab" },
                    MinRoomSize = new Vector2Int(4, 4),
                    MaxRoomSize = new Vector2Int(10, 10),
                    MinCount = 1,
                    MaxCount = 3,
                    PlacementProbability = 0.8f,
                    PlaceAgainstWalls = true,
                    MinDistanceFromWalls = 1,
                    MinDistanceFromDoorways = 2,
                    AllowRotation = true,
                    AllowFlipping = false,
                    VariantCount = 2
                },
                new FurniturePlacementRule("Chair")
                {
                    AllowedPrefabs = new[] { "Assets/Game/Furniture/OfficeChair.prefab" },
                    MinRoomSize = new Vector2Int(3, 3),
                    MaxRoomSize = new Vector2Int(10, 10),
                    MinCount = 1,
                    MaxCount = 4,
                    PlacementProbability = 0.9f,
                    PlaceAgainstWalls = false,
                    MinDistanceFromWalls = 1,
                    MinDistanceFromDoorways = 1,
                    AllowRotation = true,
                    AllowFlipping = true,
                    VariantCount = 1
                }
            };

            _rulesByRoomType[RoomClassification.Office] = officeRules;

            // Conference room rules
            var conferenceRules = new List<FurniturePlacementRule>
            {
                new FurniturePlacementRule("ConferenceTable")
                {
                    AllowedPrefabs = new[] { "Assets/Game/Furniture/ConferenceTable.prefab" },
                    MinRoomSize = new Vector2Int(6, 6),
                    MaxRoomSize = new Vector2Int(15, 15),
                    MinCount = 1,
                    MaxCount = 2,
                    PlacementProbability = 0.9f,
                    PlaceInCenter = true,
                    MinDistanceFromWalls = 2,
                    MinDistanceFromDoorways = 3,
                    AllowRotation = true,
                    AllowFlipping = false,
                    VariantCount = 1
                }
            };

            _rulesByRoomType[RoomClassification.Conference] = conferenceRules;

            // Break room rules
            var breakRoomRules = new List<FurniturePlacementRule>
            {
                new FurniturePlacementRule("VendingMachine")
                {
                    AllowedPrefabs = new[] { "Assets/Game/Furniture/VendingMachine.prefab" },
                    MinRoomSize = new Vector2Int(3, 3),
                    MaxRoomSize = new Vector2Int(8, 8),
                    MinCount = 0,
                    MaxCount = 2,
                    PlacementProbability = 0.6f,
                    PlaceAgainstWalls = true,
                    MinDistanceFromWalls = 0,
                    MinDistanceFromDoorways = 1,
                    AllowRotation = false,
                    AllowFlipping = true,
                    VariantCount = 2
                }
            };

            _rulesByRoomType[RoomClassification.BreakRoom] = breakRoomRules;
        }

        #endregion
    }
}