using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Content
{
    /// <summary>
    /// Handles balanced resource distribution throughout generated maps.
    /// Implements probability-based placement with room-type specific rules and difficulty scaling.
    /// </summary>
    public class ResourceDistributor
    {
        #region Private Fields

        private readonly IAssetLoader _assetLoader;
        private readonly System.Random _random;
        private int _seed;
        
        // Resource distribution rules
        private readonly Dictionary<RoomClassification, List<ResourcePlacementRule>> _roomTypeRules;
        private readonly Dictionary<string, string[]> _resourcePrefabs;
        private readonly Dictionary<int, DifficultyScaling> _difficultyScaling;

        // Performance tracking
        private int _collisionChecks;
        private int _placementsAttempted;
        private int _placementsSuccessful;

        #endregion

        #region Events

        public event Action<ResourceData> OnResourcePlaced;
        public event Action<RoomData, string> OnResourcePlacementFailed;

        #endregion

        #region Constructor

        public ResourceDistributor(IAssetLoader assetLoader, int seed = 0)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _seed = seed;
            _random = new System.Random(_seed);
            
            _roomTypeRules = new Dictionary<RoomClassification, List<ResourcePlacementRule>>();
            _resourcePrefabs = new Dictionary<string, string[]>();
            _difficultyScaling = new Dictionary<int, DifficultyScaling>();
            
            InitializeDefaultRules();
            InitializeResourcePrefabs();
            InitializeDifficultyScaling();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Places resources throughout all rooms in the map
        /// </summary>
        public List<ResourceData> DistributeResources(MapData map, List<FurnitureData> furniture, int difficulty = 1)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (furniture == null) throw new ArgumentNullException(nameof(furniture));
            
            var resources = new List<ResourceData>();
            _collisionChecks = 0;
            _placementsAttempted = 0;
            _placementsSuccessful = 0;

            // Get difficulty scaling
            var scaling = GetDifficultyScaling(difficulty);

            foreach (var room in map.Rooms)
            {
                var roomResources = PlaceResourcesInRoom(room, furniture, scaling);
                resources.AddRange(roomResources);
            }

            return resources;
        }

        /// <summary>
        /// Places resources in a specific room
        /// </summary>
        public List<ResourceData> PlaceResourcesInRoom(RoomData room, List<FurnitureData> furniture, DifficultyScaling scaling = null)
        {
            var resources = new List<ResourceData>();
            
            if (!_roomTypeRules.TryGetValue(room.Classification, out var rules))
                return resources;

            scaling = scaling ?? GetDifficultyScaling(1);

            foreach (var rule in rules)
            {
                if (!rule.IsValidForRoom(room, 1)) // Difficulty handled by scaling
                    continue;

                // Apply difficulty scaling to spawn probability
                float scaledProbability = ApplyDifficultyScaling(rule.SpawnProbability, rule.ResourceType, scaling);
                
                if (_random.NextDouble() > scaledProbability)
                    continue;

                int quantity = rule.GetQuantityForRoom(room, _random);
                
                for (int i = 0; i < quantity; i++)
                {
                    var resource = TryPlaceResource(room, furniture, rule, scaling);
                    if (resource != null)
                    {
                        resources.Add(resource);
                        OnResourcePlaced?.Invoke(resource);
                    }
                }
            }

            return resources;
        }

        /// <summary>
        /// Updates the random seed for reproducible generation
        /// </summary>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new System.Random(_seed);
        }

        /// <summary>
        /// Gets performance metrics for the last distribution operation
        /// </summary>
        public ResourceDistributionMetrics GetMetrics()
        {
            return new ResourceDistributionMetrics
            {
                CollisionChecks = _collisionChecks,
                PlacementsAttempted = _placementsAttempted,
                PlacementsSuccessful = _placementsSuccessful,
                SuccessRate = _placementsAttempted > 0 ? (float)_placementsSuccessful / _placementsAttempted : 0f
            };
        }

        #endregion

        #region Private Methods

        private ResourceData TryPlaceResource(RoomData room, List<FurnitureData> furniture, ResourcePlacementRule rule, DifficultyScaling scaling)
        {
            _placementsAttempted++;

            // Get available positions in the room
            var availablePositions = GetAvailablePositions(room, furniture, rule);
            
            if (availablePositions.Count == 0)
            {
                OnResourcePlacementFailed?.Invoke(room, $"No available positions for {rule.ResourceType}");
                return null;
            }

            // Select position based on preferences
            var position = SelectPosition(availablePositions, rule);
            
            // Select prefab
            var prefabPath = SelectPrefab(rule);
            
            // Create resource data
            var resourceID = $"{rule.ResourceType}_{room.RoomID}_{_placementsAttempted}";
            var resource = new ResourceData(resourceID, rule.ResourceType, prefabPath, room.RoomID, position);
            
            // Apply difficulty scaling to value
            float scaledValue = ApplyDifficultyScaling(rule.GetValue(_random), rule.ResourceType, scaling);
            resource.SetValue(scaledValue);
            
            // Set respawn properties
            if (rule.RespawnAllowed)
            {
                resource.SetRespawnTime(rule.RespawnTime);
                resource.SetConsumable(true);
            }

            _placementsSuccessful++;
            return resource;
        }

        private List<Vector2Int> GetAvailablePositions(RoomData room, List<FurnitureData> furniture, ResourcePlacementRule rule)
        {
            var availablePositions = new List<Vector2Int>();
            var roomFurniture = furniture.Where(f => f.RoomID == room.RoomID).ToList();
            
            // Get all floor tiles in the room
            for (int x = room.Bounds.xMin; x <= room.Bounds.xMax; x++)
            {
                for (int y = room.Bounds.yMin; y <= room.Bounds.yMax; y++)
                {
                    var pos = new Vector2Int(x, y);
                    
                    if (!IsPositionAvailable(pos, room, roomFurniture, rule))
                        continue;
                        
                    availablePositions.Add(pos);
                }
            }

            return availablePositions;
        }

        private bool IsPositionAvailable(Vector2Int position, RoomData room, List<FurnitureData> furniture, ResourcePlacementRule rule)
        {
            _collisionChecks++;

            // Check if position is within room bounds
            if (!room.Bounds.Contains(position))
                return false;

            // Check distance from doorways
            if (rule.AvoidDoorways)
            {
                foreach (var doorway in room.Doorways)
                {
                    float distance = Vector2Int.Distance(position, doorway.Position);
                    if (distance < rule.MinDistanceFromDoorways)
                        return false;
                }
            }

            // Check collision with furniture
            foreach (var furniturePiece in furniture)
            {
                if (furniturePiece.OccupiedTiles.Contains(position))
                    return false;
            }

            return true;
        }

        private Vector2Int SelectPosition(List<Vector2Int> availablePositions, ResourcePlacementRule rule)
        {
            if (availablePositions.Count == 0)
                throw new ArgumentException("No available positions");

            // Apply positioning preferences
            if (rule.PreferCorners)
            {
                var cornerPositions = availablePositions.Where(pos => IsCornerPosition(pos, availablePositions)).ToList();
                if (cornerPositions.Count > 0)
                    return cornerPositions[_random.Next(cornerPositions.Count)];
            }

            if (rule.PreferCenter)
            {
                var center = GetCenterPosition(availablePositions);
                var centerPositions = availablePositions.OrderBy(pos => Vector2Int.Distance(pos, center)).Take(3).ToList();
                if (centerPositions.Count > 0)
                    return centerPositions[_random.Next(centerPositions.Count)];
            }

            // Random selection
            return availablePositions[_random.Next(availablePositions.Count)];
        }

        private bool IsCornerPosition(Vector2Int position, List<Vector2Int> availablePositions)
        {
            int adjacentCount = 0;
            var directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                if (availablePositions.Contains(position + dir))
                    adjacentCount++;
            }

            return adjacentCount <= 2; // Corner positions have fewer adjacent positions
        }

        private Vector2Int GetCenterPosition(List<Vector2Int> positions)
        {
            if (positions.Count == 0)
                return Vector2Int.zero;

            int sumX = positions.Sum(pos => pos.x);
            int sumY = positions.Sum(pos => pos.y);
            
            return new Vector2Int(sumX / positions.Count, sumY / positions.Count);
        }

        private string SelectPrefab(ResourcePlacementRule rule)
        {
            if (rule.AllowedPrefabs.Length == 0)
                return string.Empty;

            return rule.AllowedPrefabs[_random.Next(rule.AllowedPrefabs.Length)];
        }

        private float ApplyDifficultyScaling(float baseValue, string resourceType, DifficultyScaling scaling)
        {
            return resourceType.ToLower() switch
            {
                "health" => baseValue * scaling.HealthModifier,
                "ammo" => baseValue * scaling.AmmoModifier,
                "weapon" => baseValue * scaling.WeaponModifier,
                _ => baseValue
            };
        }

        private DifficultyScaling GetDifficultyScaling(int difficulty)
        {
            return _difficultyScaling.TryGetValue(difficulty, out var scaling) ? scaling : _difficultyScaling[1];
        }

        #endregion

        #region Initialization

        private void InitializeDefaultRules()
        {
            // Office room rules
            _roomTypeRules[RoomClassification.Office] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("Health") { SpawnProbability = 0.1f, MinQuantity = 1, MaxQuantity = 1 },
                new ResourcePlacementRule("Ammo") { SpawnProbability = 0.3f, MinQuantity = 1, MaxQuantity = 2 },
                new ResourcePlacementRule("Food") { SpawnProbability = 0.2f, MinQuantity = 1, MaxQuantity = 1 }
            };

            // Conference room rules
            _roomTypeRules[RoomClassification.Conference] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("Health") { SpawnProbability = 0.15f, MinQuantity = 1, MaxQuantity = 2 },
                new ResourcePlacementRule("Ammo") { SpawnProbability = 0.4f, MinQuantity = 2, MaxQuantity = 3 },
                new ResourcePlacementRule("Weapon") { SpawnProbability = 0.1f, MinQuantity = 1, MaxQuantity = 1 }
            };

            // Break room rules
            _roomTypeRules[RoomClassification.BreakRoom] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("Health") { SpawnProbability = 0.2f, MinQuantity = 1, MaxQuantity = 2 },
                new ResourcePlacementRule("Food") { SpawnProbability = 0.8f, MinQuantity = 3, MaxQuantity = 5 },
                new ResourcePlacementRule("PowerUp") { SpawnProbability = 0.3f, MinQuantity = 1, MaxQuantity = 2 }
            };

            // Storage room rules
            _roomTypeRules[RoomClassification.Storage] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("Ammo") { SpawnProbability = 0.5f, MinQuantity = 2, MaxQuantity = 4 },
                new ResourcePlacementRule("Health") { SpawnProbability = 0.25f, MinQuantity = 1, MaxQuantity = 2 },
                new ResourcePlacementRule("Weapon") { SpawnProbability = 0.15f, MinQuantity = 1, MaxQuantity = 1 }
            };

            // Server room rules
            _roomTypeRules[RoomClassification.ServerRoom] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("PowerUp") { SpawnProbability = 0.4f, MinQuantity = 1, MaxQuantity = 2 },
                new ResourcePlacementRule("Health") { SpawnProbability = 0.1f, MinQuantity = 1, MaxQuantity = 1 }
            };

            // Lobby rules
            _roomTypeRules[RoomClassification.Lobby] = new List<ResourcePlacementRule>
            {
                new ResourcePlacementRule("Health") { SpawnProbability = 0.3f, MinQuantity = 2, MaxQuantity = 3 },
                new ResourcePlacementRule("Ammo") { SpawnProbability = 0.35f, MinQuantity = 2, MaxQuantity = 3 },
                new ResourcePlacementRule("Weapon") { SpawnProbability = 0.2f, MinQuantity = 1, MaxQuantity = 2 }
            };
        }

        private void InitializeResourcePrefabs()
        {
            _resourcePrefabs["Health"] = new[] { "Assets/Game/Items/Health.prefab" };
            _resourcePrefabs["Ammo"] = new[] { "Assets/Game/Items/AmmoCrate.prefab" };
            _resourcePrefabs["Weapon"] = new[] { 
                "Assets/Game/Items/Guns/BasicGun.prefab",
                "Assets/Game/Items/Guns/MachineGun.prefab",
                "Assets/Game/Items/Guns/HeavyGun.prefab"
            };
            _resourcePrefabs["Food"] = new[] {
                "Assets/Game/Items/cake.png",
                "Assets/Game/Items/chips.png",
                "Assets/Game/Items/coldbrew.png",
                "Assets/Game/Items/popcorn.png",
                "Assets/Game/Items/cookies.png"
            };
            _resourcePrefabs["PowerUp"] = new[] { "Assets/Game/Items/UpgradeCrate.prefab" };

            // Apply prefabs to rules
            foreach (var roomRules in _roomTypeRules.Values)
            {
                foreach (var rule in roomRules)
                {
                    if (_resourcePrefabs.TryGetValue(rule.ResourceType, out var prefabs))
                    {
                        rule.AllowedPrefabs = prefabs;
                    }
                }
            }
        }

        private void InitializeDifficultyScaling()
        {
            _difficultyScaling[1] = new DifficultyScaling(1.0f, 1.0f, 0.8f);
            _difficultyScaling[3] = new DifficultyScaling(0.9f, 1.1f, 1.0f);
            _difficultyScaling[5] = new DifficultyScaling(0.8f, 1.2f, 1.2f);
            _difficultyScaling[7] = new DifficultyScaling(0.7f, 1.3f, 1.4f);
            _difficultyScaling[10] = new DifficultyScaling(0.6f, 1.5f, 1.6f);
        }

        #endregion
    }

    /// <summary>
    /// Difficulty scaling parameters for resource distribution
    /// </summary>
    [Serializable]
    public class DifficultyScaling
    {
        public float HealthModifier { get; }
        public float AmmoModifier { get; }
        public float WeaponModifier { get; }

        public DifficultyScaling(float healthModifier, float ammoModifier, float weaponModifier)
        {
            HealthModifier = healthModifier;
            AmmoModifier = ammoModifier;
            WeaponModifier = weaponModifier;
        }
    }

    /// <summary>
    /// Performance metrics for resource distribution operations
    /// </summary>
    [Serializable]
    public class ResourceDistributionMetrics
    {
        public int CollisionChecks { get; set; }
        public int PlacementsAttempted { get; set; }
        public int PlacementsSuccessful { get; set; }
        public float SuccessRate { get; set; }
        public float AverageCollisionChecksPerRoom => PlacementsAttempted > 0 ? (float)CollisionChecks / PlacementsAttempted : 0f;
    }
}