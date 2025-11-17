using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of IContentPopulator for testing purposes.
    /// Provides configurable content population behavior for unit testing.
    /// </summary>
    public class MockContentPopulator : IContentPopulator
    {
        private List<FurnitureData> _mockFurniture;
        private List<SpawnPointData> _mockSpawnPoints;
        private List<ResourceData> _mockResources;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;
        private ContentDensityStats _mockDensityStats;

        public event Action<FurnitureData> OnFurniturePlaced;
        public event Action<SpawnPointData> OnSpawnPointPlaced;
        public event Action<ResourceData> OnResourcePlaced;
        public event Action<MapData, Exception> OnContentPopulationFailed;

        public MockContentPopulator()
        {
            _mockFurniture = CreateDefaultMockFurniture();
            _mockSpawnPoints = CreateDefaultMockSpawnPoints();
            _mockResources = CreateDefaultMockResources();
            _shouldThrowException = false;
            _mockValidationResult = ValidationResult.Success();
            _mockDensityStats = new ContentDensityStats
            {
                FurnitureDensity = 0.5f,
                EnemyDensity = 0.2f,
                ResourceDensity = 0.3f,
                TotalObjects = 10,
                AverageObjectsPerRoom = 3.3f
            };
        }

        /// <summary>
        /// Sets the mock furniture to return from placement methods.
        /// </summary>
        public void SetMockFurniture(List<FurnitureData> furniture)
        {
            _mockFurniture = furniture ?? new List<FurnitureData>();
        }

        /// <summary>
        /// Sets the mock spawn points to return from placement methods.
        /// </summary>
        public void SetMockSpawnPoints(List<SpawnPointData> spawnPoints)
        {
            _mockSpawnPoints = spawnPoints ?? new List<SpawnPointData>();
        }

        /// <summary>
        /// Sets the mock resources to return from placement methods.
        /// </summary>
        public void SetMockResources(List<ResourceData> resources)
        {
            _mockResources = resources ?? new List<ResourceData>();
        }

        /// <summary>
        /// Sets the mock content density statistics.
        /// </summary>
        public void SetMockDensityStats(ContentDensityStats stats)
        {
            _mockDensityStats = stats;
        }

        /// <summary>
        /// Configures the mock to throw an exception during content population.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock content population failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        public void PopulateContent(MapData map, BiomeConfiguration biome)
        {
            PopulateContent(map, biome, 0);
        }

        public void PopulateContent(MapData map, BiomeConfiguration biome, int seed)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (biome == null)
                throw new ArgumentNullException(nameof(biome));

            if (_shouldThrowException)
            {
                OnContentPopulationFailed?.Invoke(map, _exceptionToThrow);
                throw _exceptionToThrow;
            }

            // Simulate content placement
            PlaceFurniture(map, biome);
            PlaceEnemySpawns(map, biome);
            PlaceResources(map, biome);
        }

        public List<FurnitureData> PlaceFurniture(MapData map, BiomeConfiguration biome)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (biome == null)
                throw new ArgumentNullException(nameof(biome));

            foreach (var furniture in _mockFurniture)
            {
                OnFurniturePlaced?.Invoke(furniture);
            }

            return new List<FurnitureData>(_mockFurniture);
        }

        public List<SpawnPointData> PlaceEnemySpawns(MapData map, BiomeConfiguration biome)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (biome == null)
                throw new ArgumentNullException(nameof(biome));

            foreach (var spawnPoint in _mockSpawnPoints)
            {
                OnSpawnPointPlaced?.Invoke(spawnPoint);
            }

            return new List<SpawnPointData>(_mockSpawnPoints);
        }

        public List<ResourceData> PlaceResources(MapData map, BiomeConfiguration biome)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (biome == null)
                throw new ArgumentNullException(nameof(biome));

            foreach (var resource in _mockResources)
            {
                OnResourcePlaced?.Invoke(resource);
            }

            return new List<ResourceData>(_mockResources);
        }

        public ValidationResult ValidateContentPlacement(MapData map)
        {
            if (map == null)
                return ValidationResult.Failure("Map cannot be null");

            return _mockValidationResult;
        }

        public MapData OptimizeContentPlacement(MapData map, BiomeConfiguration biome)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (biome == null)
                throw new ArgumentNullException(nameof(biome));

            // Return the same map to simulate optimization
            return map;
        }

        public ContentDensityStats CalculateContentDensity(MapData map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            return _mockDensityStats;
        }

        public List<Vector2Int> FindValidPositions(RoomData room, Vector2Int objectSize, List<PlacedObjectData> existingObjects, int minDistance = 1)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));
            if (existingObjects == null)
                throw new ArgumentNullException(nameof(existingObjects));

            // Return mock valid positions
            return new List<Vector2Int>
            {
                new Vector2Int(room.Bounds.x + 2, room.Bounds.y + 2),
                new Vector2Int(room.Bounds.x + 5, room.Bounds.y + 5)
            };
        }

        private List<FurnitureData> CreateDefaultMockFurniture()
        {
            return new List<FurnitureData>
            {
                new FurnitureData { Position = new Vector2Int(7, 7), Type = "Desk" },
                new FurnitureData { Position = new Vector2Int(10, 8), Type = "Chair" },
                new FurnitureData { Position = new Vector2Int(22, 10), Type = "Table" }
            };
        }

        private List<SpawnPointData> CreateDefaultMockSpawnPoints()
        {
            return new List<SpawnPointData>
            {
                new SpawnPointData { Position = new Vector2Int(8, 8), EnemyType = "Mouse", Difficulty = 1 },
                new SpawnPointData { Position = new Vector2Int(25, 12), EnemyType = "Rat", Difficulty = 2 }
            };
        }

        private List<ResourceData> CreateDefaultMockResources()
        {
            return new List<ResourceData>
            {
                new ResourceData { Position = new Vector2Int(9, 9), ResourceType = "Cheese", Amount = 1 },
                new ResourceData { Position = new Vector2Int(23, 13), ResourceType = "Cheese", Amount = 2 }
            };
        }
    }

    // Mock data structures for testing
    public struct FurnitureData
    {
        public Vector2Int Position;
        public string Type;
    }

    public struct SpawnPointData
    {
        public Vector2Int Position;
        public string EnemyType;
        public int Difficulty;
    }

    public struct ResourceData
    {
        public Vector2Int Position;
        public string ResourceType;
        public int Amount;
    }

    public struct PlacedObjectData
    {
        public Vector2Int Position;
        public Vector2Int Size;
    }
}