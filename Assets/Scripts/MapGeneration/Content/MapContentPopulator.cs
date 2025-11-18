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
    /// Complete content population system implementing IContentPopulator interface.
    /// Integrates furniture placement, spawn points, and resource distribution.
    /// </summary>
    public class MapContentPopulator : IContentPopulator
    {
        #region Private Fields

        private readonly FurniturePlacer _furniturePlacer;
        private readonly SpawnPointManager _spawnPointManager;
        private readonly SpawnPointWaveSpawnerIntegration _waveSpawnerIntegration;
        private readonly ResourceDistributor _resourceDistributor;
        private readonly IAssetLoader _assetLoader;
        private System.Random _random;
        private int _seed;

        // Content caches
        private readonly List<FurnitureData> _placedFurniture;
        private readonly List<SpawnPointData> _placedSpawnPoints;
        private readonly List<ResourceData> _placedResources;

        #endregion

        #region Events (IContentPopulator Implementation)

        public event Action<FurnitureData> OnFurniturePlaced;
        public event Action<SpawnPointData> OnSpawnPointPlaced;
        public event Action<ResourceData> OnResourcePlaced;
        public event Action<MapData, Exception> OnContentPopulationFailed;

        #endregion

        #region Constructor

        public MapContentPopulator(IAssetLoader assetLoader, SpawnTableConfiguration spawnTableConfig = null, int seed = 0)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _seed = seed;
            _random = new System.Random(_seed);
            
            _furniturePlacer = new FurniturePlacer(assetLoader, seed);
            _spawnPointManager = new SpawnPointManager(spawnTableConfig, seed);
            _waveSpawnerIntegration = new SpawnPointWaveSpawnerIntegration(spawnTableConfig);
            _resourceDistributor = new ResourceDistributor(assetLoader, seed);
            
            _placedFurniture = new List<FurnitureData>();
            _placedSpawnPoints = new List<SpawnPointData>();
            _placedResources = new List<ResourceData>();

            // Forward furniture placement events
            _furniturePlacer.OnFurniturePlaced += (furniture) => OnFurniturePlaced?.Invoke(furniture);
            _furniturePlacer.OnFurniturePlacementFailed += (room, error) => 
                OnContentPopulationFailed?.Invoke(null, new InvalidOperationException($"Furniture placement failed in room {room.RoomID}: {error}"));

            // Forward spawn point placement events
            _spawnPointManager.OnSpawnPointPlaced += (spawnPoint) => OnSpawnPointPlaced?.Invoke(spawnPoint);
            _spawnPointManager.OnSpawnPointPlacementFailed += (room, error) => 
                OnContentPopulationFailed?.Invoke(null, new InvalidOperationException($"Spawn point placement failed in room {room.RoomID}: {error}"));

            // Forward resource placement events
            _resourceDistributor.OnResourcePlaced += (resource) => OnResourcePlaced?.Invoke(resource);
            _resourceDistributor.OnResourcePlacementFailed += (room, error) => 
                OnContentPopulationFailed?.Invoke(null, new InvalidOperationException($"Resource placement failed in room {room.RoomID}: {error}"));
        }

        #endregion

        #region IContentPopulator Implementation

        public void PopulateContent(MapData map, BiomeConfiguration biome)
        {
            PopulateContent(map, biome, _seed);
        }

        public void PopulateContent(MapData map, BiomeConfiguration biome, int seed)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (biome == null) throw new ArgumentNullException(nameof(biome));

            try
            {
                _seed = seed;
                _random = new System.Random(_seed);
                _furniturePlacer.SetSeed(_seed);

                // Clear previous content
                ClearContent();

                // Place furniture
                var furniture = PlaceFurniture(map, biome);
                _placedFurniture.AddRange(furniture);

                // Place enemy spawn points
                var spawnPoints = PlaceEnemySpawns(map, biome);
                _placedSpawnPoints.AddRange(spawnPoints);

                // Place resources (default difficulty = 1)
                var resources = PlaceResources(map, biome, 1);
                _placedResources.AddRange(resources);

                // Add content to map data (may need to extend MapData)
                AddContentToMap(map);
            }
            catch (Exception ex)
            {
                OnContentPopulationFailed?.Invoke(map, ex);
                throw;
            }
        }

        public List<FurnitureData> PlaceFurniture(MapData map, BiomeConfiguration biome)
        {
            return _furniturePlacer.PlaceFurniture(map, biome);
        }

        public List<SpawnPointData> PlaceEnemySpawns(MapData map, BiomeConfiguration biome)
        {
            // Use the new SpawnPointManager for strategic placement
            return _spawnPointManager.PlaceSpawnPoints(map, _placedFurniture);
        }

        public List<ResourceData> PlaceResources(MapData map, BiomeConfiguration biome)
        {
            return PlaceResources(map, biome, 1);
        }

        public List<ResourceData> PlaceResources(MapData map, BiomeConfiguration biome, int difficulty)
        {
            return _resourceDistributor.DistributeResources(map, _placedFurniture, difficulty);
        }

        public ValidationResult ValidateContentPlacement(MapData map)
        {
            var result = new ValidationResult();

            // Validate furniture placement
            var furnitureValidation = _furniturePlacer.ValidateFurniturePlacement(map);
            result.Merge(furnitureValidation);

            // Validate spawn points
            foreach (var spawnPoint in _placedSpawnPoints)
            {
                var room = map.GetRoomByID(spawnPoint.RoomID);
                if (room == null)
                {
                    result.AddError($"Spawn point {spawnPoint.Position} references invalid room {spawnPoint.RoomID}");
                }
                else if (!room.ContainsPoint(spawnPoint.Position))
                {
                    result.AddError($"Spawn point {spawnPoint.Position} is outside room {room.RoomID} bounds");
                }
            }

            // Validate resources
            foreach (var resource in _placedResources)
            {
                var room = map.GetRoomByID(resource.RoomID);
                if (room == null)
                {
                    result.AddError($"Resource {resource.Position} references invalid room {resource.RoomID}");
                }
                else if (!room.ContainsPoint(resource.Position))
                {
                    result.AddError($"Resource {resource.Position} is outside room {room.RoomID} bounds");
                }
            }

            return result;
        }

        public MapData OptimizeContentPlacement(MapData map, BiomeConfiguration biome)
        {
            // For now, return the map as-is
            // Future optimization could include:
            // - Balancing enemy density
            // - Optimizing resource distribution
            // - Adjusting furniture for better flow
            return map;
        }

        public ContentDensityStats CalculateContentDensity(MapData map)
        {
            int totalObjects = _placedFurniture.Count + _placedSpawnPoints.Count + _placedResources.Count;
            float averageObjectsPerRoom = map.Rooms.Count > 0 ? (float)totalObjects / map.Rooms.Count : 0f;

            return new ContentDensityStats
            {
                FurnitureDensity = map.Rooms.Count > 0 ? (float)_placedFurniture.Count / map.Rooms.Count : 0f,
                EnemyDensity = map.Rooms.Count > 0 ? (float)_placedSpawnPoints.Count / map.Rooms.Count : 0f,
                ResourceDensity = map.Rooms.Count > 0 ? (float)_placedResources.Count / map.Rooms.Count : 0f,
                TotalObjects = totalObjects,
                AverageObjectsPerRoom = averageObjectsPerRoom
            };
        }

        public List<Vector2Int> FindValidPositions(RoomData room, Vector2Int objectSize, 
                                                   List<PlacedObjectData> existingObjects, int minDistance = 1)
        {
            return _furniturePlacer.FindValidPositions(room, objectSize, existingObjects, minDistance);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets all placed furniture.
        /// </summary>
        public IReadOnlyList<FurnitureData> GetPlacedFurniture()
        {
            return _placedFurniture.AsReadOnly();
        }



        /// <summary>
        /// Gets all placed resources.
        /// </summary>
        public IReadOnlyList<ResourceData> GetPlacedResources()
        {
            return _placedResources.AsReadOnly();
        }

        /// <summary>
        /// Clears all placed content and destroys GameObjects.
        /// </summary>
        public void ClearContent()
        {
            _placedFurniture.Clear();
            _placedSpawnPoints.Clear();
            _placedResources.Clear();
            
            // Destroy spawn point GameObjects
            _waveSpawnerIntegration.DestroyAllSpawnPoints();
        }

        /// <summary>
        /// Sets the random seed for deterministic content placement.
        /// </summary>
        /// <param name="seed">Seed value</param>
        public void SetSeed(int seed)
        {
            _seed = seed;
            _random = new System.Random(_seed);
            _furniturePlacer.SetSeed(seed);
            _spawnPointManager.SetSeed(seed);
            _resourceDistributor.SetSeed(seed);
        }

        /// <summary>
        /// Gets performance statistics from the furniture placer.
        /// </summary>
        public (int roomsProcessed, int furniturePlaced, long totalMs, float avgMsPerRoom) GetFurniturePerformanceStats()
        {
            return _furniturePlacer.GetPerformanceStats();
        }

        /// <summary>
        /// Gets performance statistics from the spawn point manager.
        /// </summary>
        public (int roomsProcessed, int spawnPointsPlaced, long totalMs, float avgMsPerRoom) GetSpawnPointPerformanceStats()
        {
            return _spawnPointManager.GetPerformanceStats();
        }

        /// <summary>
        /// Gets performance statistics from the WaveSpawner integration.
        /// </summary>
        public (int created, int destroyed, float avgCreationTime) GetWaveSpawnerPerformanceStats()
        {
            return _waveSpawnerIntegration.GetPerformanceStats();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates GameObjects for all spawn points with WaveSpawner integration.
        /// </summary>
        /// <param name="tilemapOffset">Offset for world positioning</param>
        /// <returns>List of created spawn point GameObjects</returns>
        public List<GameObject> CreateSpawnPointGameObjects(Vector3 tilemapOffset = default)
        {
            return _waveSpawnerIntegration.CreateSpawnPointGameObjects(_placedSpawnPoints, tilemapOffset);
        }

        /// <summary>
        /// Validates spawn point placement with WaveSpawner integration.
        /// </summary>
        /// <param name="map">Map with spawn points to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateSpawnPointIntegration(MapData map)
        {
            var result = _spawnPointManager.ValidateSpawnPoints(map, _placedSpawnPoints);
            var integrationResult = _waveSpawnerIntegration.ValidateWaveSpawnerIntegration(_placedSpawnPoints);
            result.Merge(integrationResult);
            return result;
        }

        /// <summary>
        /// Places resources in a specific room.
        /// </summary>


        /// <summary>
        /// Calculates the number of spawn points for a room.
        /// </summary>
        private int CalculateSpawnCount(RoomData room)
        {
            int baseCount = room.Classification switch
            {
                RoomClassification.Office => 2,
                RoomClassification.Conference => 3,
                RoomClassification.BreakRoom => 1,
                RoomClassification.Storage => 1,
                RoomClassification.ServerRoom => 2,
                RoomClassification.Lobby => 4,
                _ => 2
            };

            // Adjust based on room size
            float sizeMultiplier = Mathf.Clamp01(room.Area / 50f); // Normalize to 0-1 for 50 tile rooms
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * (0.5f + sizeMultiplier * 0.5f)));
        }

        /// <summary>
        /// Calculates the number of resources for a room.
        /// </summary>
        private int CalculateResourceCount(RoomData room)
        {
            int baseCount = room.Classification switch
            {
                RoomClassification.Office => 3,
                RoomClassification.Conference => 2,
                RoomClassification.BreakRoom => 4,
                RoomClassification.Storage => 5,
                RoomClassification.ServerRoom => 1,
                RoomClassification.Lobby => 3,
                _ => 2
            };

            // Adjust based on room size
            float sizeMultiplier = Mathf.Clamp01(room.Area / 40f);
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * (0.5f + sizeMultiplier * 0.5f)));
        }

        /// <summary>
        /// Selects an enemy type based on biome configuration.
        /// </summary>
        private string SelectEnemyType(BiomeConfiguration biome)
        {
            // For now, return a default enemy type
            // In a full implementation, this would use biome.EnemyTypes
            var enemyTypes = new[] { "BasicEnemy", "FastEnemy", "TankEnemy" };
            return enemyTypes[_random.Next(enemyTypes.Length)];
        }

        /// <summary>
        /// Selects a resource type based on biome configuration.
        /// </summary>
        private string SelectResourceType(BiomeConfiguration biome)
        {
            // For now, return a default resource type
            // In a full implementation, this would use biome.ResourceTypes
            var resourceTypes = new[] { "Health", "Ammo", "PowerUp" };
            return resourceTypes[_random.Next(resourceTypes.Length)];
        }

        /// <summary>
        /// Selects a resource prefab based on resource type.
        /// </summary>
        private string SelectResourcePrefab(BiomeConfiguration biome)
        {
            // Map resource types to existing prefabs
            return SelectResourceType(biome) switch
            {
                "Health" => "Assets/Game/Items/Health.prefab",
                "Ammo" => "Assets/Game/Items/AmmoCrate.prefab",
                "PowerUp" => "Assets/Game/Items/Grenade.prefab",
                _ => "Assets/Game/Items/Health.prefab"
            };
        }

        /// <summary>
        /// Adds placed content to the map data structure.
        /// </summary>
        private void AddContentToMap(MapData map)
        {
            // Add spawn points to map (MapData already supports this)
            foreach (var spawnPoint in _placedSpawnPoints)
            {
                map.AddEnemySpawnPoint(spawnPoint);
            }

            // Note: MapData may need to be extended to support furniture and resources
            // For now, they're tracked in the populator
        }

        #endregion
    }
}