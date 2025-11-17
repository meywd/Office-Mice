using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive unit tests for IContentPopulator interface and mock implementation.
    /// Tests all interface methods, events, and edge cases.
    /// </summary>
    [TestFixture]
    public class IContentPopulatorTests
    {
        private MockContentPopulator _contentPopulator;
        private MapData _testMap;
        private BiomeConfiguration _testBiome;
        private List<FurnitureData> _testFurniture;
        private List<SpawnPointData> _testSpawnPoints;
        private List<ResourceData> _testResources;

        [SetUp]
        public void SetUp()
        {
            _contentPopulator = new MockContentPopulator();
            _testMap = CreateTestMap();
            _testBiome = new BiomeConfiguration();
            _testFurniture = CreateTestFurniture();
            _testSpawnPoints = CreateTestSpawnPoints();
            _testResources = CreateTestResources();
        }

        [Test]
        public void PopulateContent_WithValidData_PopulatesSuccessfully()
        {
            // Arrange
            _contentPopulator.SetMockFurniture(_testFurniture);
            _contentPopulator.SetMockSpawnPoints(_testSpawnPoints);
            _contentPopulator.SetMockResources(_testResources);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _contentPopulator.PopulateContent(_testMap, _testBiome));
        }

        [Test]
        public void PopulateContent_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PopulateContent(null, _testBiome));
        }

        [Test]
        public void PopulateContent_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PopulateContent(_testMap, null));
        }

        [Test]
        public void PopulateContent_WithSeed_PopulatesSuccessfully()
        {
            // Arrange
            _contentPopulator.SetMockFurniture(_testFurniture);
            _contentPopulator.SetMockSpawnPoints(_testSpawnPoints);
            _contentPopulator.SetMockResources(_testResources);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _contentPopulator.PopulateContent(_testMap, _testBiome, 123));
        }

        [Test]
        public void PopulateContent_WithException_ThrowsAndFiresEvent()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            _contentPopulator.SetThrowException(true, expectedException);
            Exception firedException = null;
            _contentPopulator.OnContentPopulationFailed += (map, ex) => firedException = ex;

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => 
                _contentPopulator.PopulateContent(_testMap, _testBiome));
            Assert.AreEqual(expectedException, thrownException);
            Assert.AreEqual(expectedException, firedException);
        }

        [Test]
        public void PlaceFurniture_WithValidData_ReturnsFurniture()
        {
            // Arrange
            _contentPopulator.SetMockFurniture(_testFurniture);

            // Act
            var result = _contentPopulator.PlaceFurniture(_testMap, _testBiome);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testFurniture.Count, result.Count);
        }

        [Test]
        public void PlaceFurniture_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceFurniture(null, _testBiome));
        }

        [Test]
        public void PlaceFurniture_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceFurniture(_testMap, null));
        }

        [Test]
        public void PlaceEnemySpawns_WithValidData_ReturnsSpawnPoints()
        {
            // Arrange
            _contentPopulator.SetMockSpawnPoints(_testSpawnPoints);

            // Act
            var result = _contentPopulator.PlaceEnemySpawns(_testMap, _testBiome);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testSpawnPoints.Count, result.Count);
        }

        [Test]
        public void PlaceEnemySpawns_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceEnemySpawns(null, _testBiome));
        }

        [Test]
        public void PlaceEnemySpawns_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceEnemySpawns(_testMap, null));
        }

        [Test]
        public void PlaceResources_WithValidData_ReturnsResources()
        {
            // Arrange
            _contentPopulator.SetMockResources(_testResources);

            // Act
            var result = _contentPopulator.PlaceResources(_testMap, _testBiome);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testResources.Count, result.Count);
        }

        [Test]
        public void PlaceResources_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceResources(null, _testBiome));
        }

        [Test]
        public void PlaceResources_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.PlaceResources(_testMap, null));
        }

        [Test]
        public void ValidateContentPlacement_WithValidMap_ReturnsSuccess()
        {
            // Arrange
            _contentPopulator.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _contentPopulator.ValidateContentPlacement(_testMap);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateContentPlacement_WithNullMap_ReturnsFailure()
        {
            // Act
            var result = _contentPopulator.ValidateContentPlacement(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Map cannot be null"));
        }

        [Test]
        public void OptimizeContentPlacement_WithValidData_ReturnsOptimizedMap()
        {
            // Act
            var result = _contentPopulator.OptimizeContentPlacement(_testMap, _testBiome);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testMap, result); // Mock returns same map
        }

        [Test]
        public void OptimizeContentPlacement_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _contentPopulator.OptimizeContentPlacement(null, _testBiome));
        }

        [Test]
        public void OptimizeContentPlacement_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _contentPopulator.OptimizeContentPlacement(_testMap, null));
        }

        [Test]
        public void CalculateContentDensity_WithValidMap_ReturnsStats()
        {
            // Arrange
            var expectedStats = new ContentDensityStats
            {
                FurnitureDensity = 0.6f,
                EnemyDensity = 0.3f,
                ResourceDensity = 0.4f,
                TotalObjects = 15,
                AverageObjectsPerRoom = 5f
            };
            _contentPopulator.SetMockDensityStats(expectedStats);

            // Act
            var result = _contentPopulator.CalculateContentDensity(_testMap);

            // Assert
            Assert.AreEqual(expectedStats.FurnitureDensity, result.FurnitureDensity);
            Assert.AreEqual(expectedStats.EnemyDensity, result.EnemyDensity);
            Assert.AreEqual(expectedStats.ResourceDensity, result.ResourceDensity);
            Assert.AreEqual(expectedStats.TotalObjects, result.TotalObjects);
            Assert.AreEqual(expectedStats.AverageObjectsPerRoom, result.AverageObjectsPerRoom);
        }

        [Test]
        public void CalculateContentDensity_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contentPopulator.CalculateContentDensity(null));
        }

        [Test]
        public void FindValidPositions_WithValidData_ReturnsPositions()
        {
            // Arrange
            var room = _testMap.Rooms[0];
            var objectSize = new Vector2Int(2, 2);
            var existingObjects = new List<PlacedObjectData>();

            // Act
            var result = _contentPopulator.FindValidPositions(room, objectSize, existingObjects);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void FindValidPositions_WithNullRoom_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _contentPopulator.FindValidPositions(null, new Vector2Int(2, 2), new List<PlacedObjectData>()));
        }

        [Test]
        public void FindValidPositions_WithNullExistingObjects_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _contentPopulator.FindValidPositions(_testMap.Rooms[0], new Vector2Int(2, 2), null));
        }

        [Test]
        public void OnFurniturePlaced_EventFired_WhenFurniturePlaced()
        {
            // Arrange
            var placedFurniture = new List<FurnitureData>();
            _contentPopulator.OnFurniturePlaced += (furniture) => placedFurniture.Add(furniture);
            _contentPopulator.SetMockFurniture(_testFurniture);

            // Act
            _contentPopulator.PlaceFurniture(_testMap, _testBiome);

            // Assert
            Assert.AreEqual(_testFurniture.Count, placedFurniture.Count);
        }

        [Test]
        public void OnSpawnPointPlaced_EventFired_WhenSpawnPointsPlaced()
        {
            // Arrange
            var placedSpawnPoints = new List<SpawnPointData>();
            _contentPopulator.OnSpawnPointPlaced += (spawnPoint) => placedSpawnPoints.Add(spawnPoint);
            _contentPopulator.SetMockSpawnPoints(_testSpawnPoints);

            // Act
            _contentPopulator.PlaceEnemySpawns(_testMap, _testBiome);

            // Assert
            Assert.AreEqual(_testSpawnPoints.Count, placedSpawnPoints.Count);
        }

        [Test]
        public void OnResourcePlaced_EventFired_WhenResourcesPlaced()
        {
            // Arrange
            var placedResources = new List<ResourceData>();
            _contentPopulator.OnResourcePlaced += (resource) => placedResources.Add(resource);
            _contentPopulator.SetMockResources(_testResources);

            // Act
            _contentPopulator.PlaceResources(_testMap, _testBiome);

            // Assert
            Assert.AreEqual(_testResources.Count, placedResources.Count);
        }

        [Test]
        public void Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockContentPopulator properly implements IContentPopulator
            Assert.IsInstanceOf<IContentPopulator>(_contentPopulator);
            
            // Verify all required methods exist and are callable
            var contentPop = (IContentPopulator)_contentPopulator;
            
            Assert.DoesNotThrow(() => contentPop.ValidateContentPlacement(_testMap));
            Assert.DoesNotThrow(() => contentPop.OptimizeContentPlacement(_testMap, _testBiome));
            Assert.DoesNotThrow(() => contentPop.CalculateContentDensity(_testMap));
            Assert.DoesNotThrow(() => contentPop.FindValidPositions(_testMap.Rooms[0], new Vector2Int(2, 2), new List<PlacedObjectData>()));
        }

        [Test]
        public void Events_AllEvents_WorkCorrectly()
        {
            // Arrange
            var placedFurniture = new List<FurnitureData>();
            var placedSpawnPoints = new List<SpawnPointData>();
            var placedResources = new List<ResourceData>();
            var failedPopulations = new List<(MapData map, Exception ex)>();
            
            _contentPopulator.OnFurniturePlaced += (furniture) => placedFurniture.Add(furniture);
            _contentPopulator.OnSpawnPointPlaced += (spawnPoint) => placedSpawnPoints.Add(spawnPoint);
            _contentPopulator.OnResourcePlaced += (resource) => placedResources.Add(resource);
            _contentPopulator.OnContentPopulationFailed += (map, ex) => failedPopulations.Add((map, ex));

            // Act - Successful population
            _contentPopulator.SetMockFurniture(_testFurniture);
            _contentPopulator.SetMockSpawnPoints(_testSpawnPoints);
            _contentPopulator.SetMockResources(_testResources);
            _contentPopulator.PopulateContent(_testMap, _testBiome);
            
            // Assert - Success case
            Assert.AreEqual(_testFurniture.Count, placedFurniture.Count, "OnFurniturePlaced should fire for each furniture");
            Assert.AreEqual(_testSpawnPoints.Count, placedSpawnPoints.Count, "OnSpawnPointPlaced should fire for each spawn point");
            Assert.AreEqual(_testResources.Count, placedResources.Count, "OnResourcePlaced should fire for each resource");
            Assert.AreEqual(0, failedPopulations.Count, "OnContentPopulationFailed should not fire on success");
            
            // Reset
            placedFurniture.Clear();
            placedSpawnPoints.Clear();
            placedResources.Clear();
            failedPopulations.Clear();
            
            // Act - Failed population
            _contentPopulator.SetThrowException(true);
            try { _contentPopulator.PopulateContent(_testMap, _testBiome); } catch { }
            
            // Assert - Failure case
            Assert.AreEqual(0, placedFurniture.Count, "OnFurniturePlaced should not fire on failure");
            Assert.AreEqual(0, placedSpawnPoints.Count, "OnSpawnPointPlaced should not fire on failure");
            Assert.AreEqual(0, placedResources.Count, "OnResourcePlaced should not fire on failure");
            Assert.AreEqual(1, failedPopulations.Count, "OnContentPopulationFailed should fire on failure");
        }

        [Test]
        public void FindValidPositions_WithMinDistance_ReturnsValidPositions()
        {
            // Arrange
            var room = _testMap.Rooms[0];
            var objectSize = new Vector2Int(2, 2);
            var existingObjects = new List<PlacedObjectData>();
            var minDistance = 2;

            // Act
            var result = _contentPopulator.FindValidPositions(room, objectSize, existingObjects, minDistance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        private MapData CreateTestMap()
        {
            var map = new MapData();
            map.SetDimensions(50, 50);
            map.SetSeed(12345);

            var room1 = new RoomData();
            room1.SetBounds(new Rect(5, 5, 10, 10));
            room1.Classification = RoomClassification.Office;
            map.AddRoom(room1);

            var room2 = new RoomData();
            room2.SetBounds(new Rect(20, 20, 8, 8));
            room2.Classification = RoomClassification.BreakRoom;
            map.AddRoom(room2);

            return map;
        }

        private List<FurnitureData> CreateTestFurniture()
        {
            return new List<FurnitureData>
            {
                new FurnitureData { Position = new Vector2Int(7, 7), Type = "Desk" },
                new FurnitureData { Position = new Vector2Int(10, 8), Type = "Chair" },
                new FurnitureData { Position = new Vector2Int(22, 22), Type = "Table" }
            };
        }

        private List<SpawnPointData> CreateTestSpawnPoints()
        {
            return new List<SpawnPointData>
            {
                new SpawnPointData { Position = new Vector2Int(8, 8), EnemyType = "Mouse", Difficulty = 1 },
                new SpawnPointData { Position = new Vector2Int(23, 23), EnemyType = "Rat", Difficulty = 2 }
            };
        }

        private List<ResourceData> CreateTestResources()
        {
            return new List<ResourceData>
            {
                new ResourceData { Position = new Vector2Int(9, 9), ResourceType = "Cheese", Amount = 1 },
                new ResourceData { Position = new Vector2Int(24, 24), ResourceType = "Cheese", Amount = 2 }
            };
        }
    }
}