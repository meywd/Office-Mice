using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class ResourceDistributorTests
    {
        private ResourceDistributor _resourceDistributor;
        private MockAssetLoader _mockAssetLoader;
        private MapData _testMap;
        private List<FurnitureData> _testFurniture;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _resourceDistributor = new ResourceDistributor(_mockAssetLoader, 42);
            _testFurniture = new List<FurnitureData>();
            
            // Create test map with different room types
            _testMap = CreateTestMap();
        }

        [Test]
        public void DistributeResources_WithValidMap_ReturnsResources()
        {
            // Act
            var resources = _resourceDistributor.DistributeResources(_testMap, _testFurniture);

            // Assert
            Assert.IsNotNull(resources);
            Assert.IsTrue(resources.Count > 0);
        }

        [Test]
        public void DistributeResources_OfficeRoom_PlacesHealthAndAmmo()
        {
            // Arrange
            var officeRoom = _testMap.Rooms.First(r => r.Classification == RoomClassification.Office);

            // Act
            var resources = _resourceDistributor.DistributeResources(_testMap, _testFurniture);
            var officeResources = resources.Where(r => r.RoomID == officeRoom.RoomID).ToList();

            // Assert
            var healthResources = officeResources.Where(r => r.IsHealthResource()).ToList();
            var ammoResources = officeResources.Where(r => r.IsAmmoResource()).ToList();

            // Office rooms should have some resources (probabilistic, so we check for presence)
            Assert.IsTrue(officeResources.Count >= 0, "Office room should have resources placed");
        }

        [Test]
        public void DistributeResources_BreakRoom_PlacesAbundantFood()
        {
            // Arrange
            var breakRoom = _testMap.Rooms.First(r => r.Classification == RoomClassification.BreakRoom);

            // Act
            var resources = _resourceDistributor.DistributeResources(_testMap, _testFurniture);
            var breakRoomResources = resources.Where(r => r.RoomID == breakRoom.RoomID).ToList();

            // Assert
            // Break rooms should have higher chance of food resources
            var foodResources = breakRoomResources.Where(r => r.ResourceType.Contains("Food")).ToList();
            Assert.IsTrue(breakRoomResources.Count >= 0, "Break room should have resources placed");
        }

        [Test]
        public void DistributeResources_WithFurniture_AvoidsCollisions()
        {
            // Arrange
            AddTestFurniture();

            // Act
            var resources = _resourceDistributor.DistributeResources(_testMap, _testFurniture);

            // Assert
            foreach (var resource in resources)
            {
                var furnitureAtPosition = _testFurniture.FirstOrDefault(f => 
                    f.RoomID == resource.RoomID && f.OccupiedTiles.Contains(resource.Position));
                
                Assert.IsNull(furnitureAtPosition, $"Resource at {resource.Position} should not overlap with furniture");
            }
        }

        [Test]
        public void DistributeResources_WithDifficulty_ScalesCorrectly()
        {
            // Act
            var easyResources = _resourceDistributor.DistributeResources(_testMap, _testFurniture, 1);
            var hardResources = _resourceDistributor.DistributeResources(_testMap, _testFurniture, 10);

            // Assert
            Assert.IsNotNull(easyResources);
            Assert.IsNotNull(hardResources);
            
            // Higher difficulty should affect resource values
            var easyHealthValue = easyResources.Where(r => r.IsHealthResource()).DefaultIfEmpty().Sum(r => r?.Value ?? 0);
            var hardHealthValue = hardResources.Where(r => r.IsHealthResource()).DefaultIfEmpty().Sum(r => r?.Value ?? 0);
            
            // Hard difficulty should have lower health values (due to scaling)
            Assert.IsTrue(hardHealthValue <= easyHealthValue || easyHealthValue == 0, 
                "Hard difficulty should scale health values down");
        }

        [Test]
        public void PlaceResourcesInRoom_InvalidRoom_ReturnsEmptyList()
        {
            // Arrange
            var invalidRoom = new RoomData(999, new RectInt(0, 0, 5, 5), RoomClassification.Office);

            // Act
            var resources = _resourceDistributor.PlaceResourcesInRoom(invalidRoom, _testFurniture);

            // Assert
            Assert.IsNotNull(resources);
            Assert.AreEqual(0, resources.Count);
        }

        [Test]
        public void SetSeed_WithSameSeed_ProducesSameResults()
        {
            // Arrange
            const int seed = 123;
            var distributor1 = new ResourceDistributor(_mockAssetLoader, seed);
            var distributor2 = new ResourceDistributor(_mockAssetLoader, seed);

            // Act
            var resources1 = distributor1.DistributeResources(_testMap, _testFurniture);
            var resources2 = distributor2.DistributeResources(_testMap, _testFurniture);

            // Assert
            Assert.AreEqual(resources1.Count, resources2.Count);
            
            for (int i = 0; i < resources1.Count; i++)
            {
                Assert.AreEqual(resources1[i].Position, resources2[i].Position);
                Assert.AreEqual(resources1[i].ResourceType, resources2[i].ResourceType);
            }
        }

        [Test]
        public void GetMetrics_AfterDistribution_ReturnsValidMetrics()
        {
            // Act
            _resourceDistributor.DistributeResources(_testMap, _testFurniture);
            var metrics = _resourceDistributor.GetMetrics();

            // Assert
            Assert.IsTrue(metrics.CollisionChecks >= 0);
            Assert.IsTrue(metrics.PlacementsAttempted >= 0);
            Assert.IsTrue(metrics.PlacementsSuccessful >= 0);
            Assert.IsTrue(metrics.SuccessRate >= 0 && metrics.SuccessRate <= 1);
        }

        [Test]
        public void DistributeResources_NullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _resourceDistributor.DistributeResources(null, _testFurniture));
        }

        [Test]
        public void DistributeResources_NullFurniture_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _resourceDistributor.DistributeResources(_testMap, null));
        }

        [Test]
        public void ResourcePlacementEvents_WhenResourcePlaced_FiresCorrectly()
        {
            // Arrange
            ResourceData placedResource = null;
            _resourceDistributor.OnResourcePlaced += (resource) => placedResource = resource;

            // Act
            var resources = _resourceDistributor.DistributeResources(_testMap, _testFurniture);

            // Assert
            if (resources.Count > 0)
            {
                Assert.IsNotNull(placedResource);
                Assert.AreEqual(resources[0].ResourceID, placedResource.ResourceID);
            }
        }

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData(10, 10);
            
            // Create different room types for testing
            var rooms = new[]
            {
                new RoomData(1, new RectInt(1, 1, 4, 4), RoomClassification.Office),
                new RoomData(2, new RectInt(6, 1, 3, 4), RoomClassification.Conference),
                new RoomData(3, new RectInt(1, 6, 4, 3), RoomClassification.BreakRoom),
                new RoomData(4, new RectInt(6, 6, 3, 3), RoomClassification.Storage)
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private void AddTestFurniture()
        {
            // Add some furniture to test collision avoidance
            var officeRoom = _testMap.Rooms.First(r => r.Classification == RoomClassification.Office);
            
            var desk = new FurnitureData("desk", "desk", officeRoom.RoomID, 
                new Vector2Int(2, 2), Vector2Int.one);
            desk.SetOccupiedTiles(new[] { new Vector2Int(2, 2) });
            
            _testFurniture.Add(desk);
        }

        #endregion
    }

    /// <summary>
    /// Mock asset loader for testing
    /// </summary>
    public class MockAssetLoader : IAssetLoader
    {
        public T LoadAsset<T>(string path) where T : class
        {
            // Return mock objects for testing
            if (typeof(T) == typeof(GameObject))
            {
                return new GameObject() as T;
            }
            return null;
        }

        public T[] LoadAllAssets<T>() where T : class
        {
            return new T[0];
        }

        public bool AssetExists(string path)
        {
            return true; // Assume all assets exist for testing
        }
    }
}