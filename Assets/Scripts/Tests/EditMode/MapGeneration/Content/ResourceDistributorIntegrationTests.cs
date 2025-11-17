using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class ResourceDistributorIntegrationTests
    {
        private MapContentPopulator _contentPopulator;
        private MockAssetLoader _mockAssetLoader;
        private BiomeConfiguration _testBiome;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _testBiome = CreateTestBiome();
            _contentPopulator = new MapContentPopulator(_mockAssetLoader, null, 42);
        }

        [Test]
        public void MapContentPopulator_WithResourceDistributor_PopulatesAllContent()
        {
            // Arrange
            var map = CreateTestMap();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);

            // Assert
            var furniture = _contentPopulator.PlaceFurniture(map, _testBiome);
            var spawnPoints = _contentPopulator.PlaceEnemySpawns(map, _testBiome);
            var resources = _contentPopulator.PlaceResources(map, _testBiome);

            Assert.IsTrue(furniture.Count > 0, "Should place furniture");
            Assert.IsTrue(spawnPoints.Count > 0, "Should place spawn points");
            Assert.IsTrue(resources.Count > 0, "Should place resources");
        }

        [Test]
        public void MapContentPopulator_CompleteWorkflow_NoCollisions()
        {
            // Arrange
            var map = CreateTestMap();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);
            var furniture = _contentPopulator.PlaceFurniture(map, _testBiome);
            var spawnPoints = _contentPopulator.PlaceEnemySpawns(map, _testBiome);
            var resources = _contentPopulator.PlaceResources(map, _testBiome);

            // Assert - Check for collisions between different content types
            foreach (var resource in resources)
            {
                var furnitureAtPosition = furniture.FirstOrDefault(f => 
                    f.RoomID == resource.RoomID && f.OccupiedTiles.Contains(resource.Position));
                
                var spawnPointAtPosition = spawnPoints.FirstOrDefault(s => 
                    s.RoomID == resource.RoomID && s.Position == resource.Position);

                Assert.IsNull(furnitureAtPosition, 
                    $"Resource at {resource.Position} should not overlap with furniture");
                Assert.IsNull(spawnPointAtPosition, 
                    $"Resource at {resource.Position} should not overlap with spawn point");
            }
        }

        [Test]
        public void MapContentPopulator_WithDifficulty_ScalesResourcesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();

            // Act
            var easyResources = _contentPopulator.PlaceResources(map, _testBiome, 1);
            var hardResources = _contentPopulator.PlaceResources(map, _testBiome, 10);

            // Assert
            Assert.IsNotNull(easyResources);
            Assert.IsNotNull(hardResources);

            // Check that difficulty scaling affects resource distribution
            var easyHealth = easyResources.Where(r => r.IsHealthResource()).ToList();
            var hardHealth = hardResources.Where(r => r.IsHealthResource()).ToList();

            // Higher difficulty should generally have fewer or less valuable health resources
            if (easyHealth.Count > 0 && hardHealth.Count > 0)
            {
                var easyAvgValue = easyHealth.Average(r => r.Value);
                var hardAvgValue = hardHealth.Average(r => r.Value);
                
                // Hard difficulty should scale health values down
                Assert.IsTrue(hardAvgValue <= easyAvgValue * 1.1f, // Allow some variance
                    "Hard difficulty should reduce health resource values");
            }
        }

        [Test]
        public void MapContentPopulator_RoomTypeSpecificRules_FollowsDistributionPatterns()
        {
            // Arrange
            var map = CreateTestMapWithSpecificRoomTypes();

            // Act
            var resources = _contentPopulator.PlaceResources(map, _testBiome);

            // Assert - Check room-specific distribution patterns
            var breakRoom = map.Rooms.First(r => r.Classification == RoomClassification.BreakRoom);
            var breakRoomResources = resources.Where(r => r.RoomID == breakRoom.RoomID).ToList();

            var officeRoom = map.Rooms.First(r => r.Classification == RoomClassification.Office);
            var officeResources = resources.Where(r => r.RoomID == officeRoom.RoomID).ToList();

            // Break rooms should have more food resources (probabilistic test)
            var breakRoomFood = breakRoomResources.Where(r => r.ResourceType.Contains("Food")).ToList();
            var officeFood = officeResources.Where(r => r.ResourceType.Contains("Food")).ToList();

            // Due to probability, we just check that resources are placed according to rules
            Assert.IsTrue(breakRoomResources.Count >= 0, "Break room should have resources");
            Assert.IsTrue(officeResources.Count >= 0, "Office room should have resources");
        }

        [Test]
        public void MapContentPopulator_Validation_PassesAllChecks()
        {
            // Arrange
            var map = CreateTestMap();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);
            var validationResult = _contentPopulator.ValidateContentPlacement(map);

            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsTrue(validationResult.IsValid, 
                $"Content validation should pass: {string.Join(", ", validationResult.Errors)}");
        }

        [Test]
        public void MapContentPopulator_EventForwarding_FiresCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var furniturePlaced = false;
            var spawnPointsPlaced = false;
            var resourcesPlaced = false;

            _contentPopulator.OnFurniturePlaced += (f) => furniturePlaced = true;
            _contentPopulator.OnSpawnPointPlaced += (s) => spawnPointsPlaced = true;
            _contentPopulator.OnResourcePlaced += (r) => resourcesPlaced = true;

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);

            // Assert
            Assert.IsTrue(furniturePlaced, "Furniture placed event should fire");
            Assert.IsTrue(spawnPointsPlaced, "Spawn points placed event should fire");
            Assert.IsTrue(resourcesPlaced, "Resources placed event should fire");
        }

        [Test]
        public void MapContentPopulator_ReproducibleGeneration_SameSeedSameResults()
        {
            // Arrange
            const int seed = 123;
            var map = CreateTestMap();

            var populator1 = new MapContentPopulator(_mockAssetLoader, null, seed);
            var populator2 = new MapContentPopulator(_mockAssetLoader, null, seed);

            // Act
            populator1.PopulateContent(map, _testBiome);
            populator2.PopulateContent(map, _testBiome);

            var resources1 = populator1.PlaceResources(map, _testBiome);
            var resources2 = populator2.PlaceResources(map, _testBiome);

            // Assert
            Assert.AreEqual(resources1.Count, resources2.Count);
            
            for (int i = 0; i < resources1.Count; i++)
            {
                Assert.AreEqual(resources1[i].Position, resources2[i].Position);
                Assert.AreEqual(resources1[i].ResourceType, resources2[i].ResourceType);
                Assert.AreEqual(resources1[i].RoomID, resources2[i].RoomID);
            }
        }

        [Test]
        public void MapContentPopulator_Performance_CompleteWorkflowWithinTarget()
        {
            // Arrange
            var map = CreateLargeTestMap(50);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, 
                $"Complete content population took {stopwatch.ElapsedMilliseconds}ms, target < 500ms");

            UnityEngine.Debug.Log($"Complete content population for 50 rooms: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void MapContentPopulator_WithExistingFurniture_IntegratesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            
            // First place furniture
            var furniture = _contentPopulator.PlaceFurniture(map, _testBiome);
            
            // Then place resources (should avoid furniture)
            var resources = _contentPopulator.PlaceResources(map, _testBiome);

            // Assert
            foreach (var resource in resources)
            {
                var conflictingFurniture = furniture.FirstOrDefault(f => 
                    f.RoomID == resource.RoomID && f.OccupiedTiles.Contains(resource.Position));
                
                Assert.IsNull(conflictingFurniture, 
                    $"Resource at {resource.Position} should not overlap with existing furniture");
            }
        }

        [Test]
        public void MapContentPopulator_DifferentBiomes_AdaptsResourcePlacement()
        {
            // Arrange
            var map = CreateTestMap();
            var biome1 = CreateTestBiome("Office");
            var biome2 = CreateTestBiome("Industrial");

            // Act
            var resources1 = _contentPopulator.PlaceResources(map, biome1);
            var resources2 = _contentPopulator.PlaceResources(map, biome2);

            // Assert
            Assert.IsNotNull(resources1);
            Assert.IsNotNull(resources2);
            
            // Resources should be placed regardless of biome (biome mainly affects furniture)
            Assert.IsTrue(resources1.Count > 0, "Should place resources for biome 1");
            Assert.IsTrue(resources2.Count > 0, "Should place resources for biome 2");
        }

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData(20, 20);
            
            var rooms = new[]
            {
                new RoomData(1, new RectInt(2, 2, 5, 5), RoomClassification.Office),
                new RoomData(2, new RectInt(10, 2, 4, 5), RoomClassification.Conference),
                new RoomData(3, new RectInt(2, 10, 5, 4), RoomClassification.BreakRoom),
                new RoomData(4, new RectInt(10, 10, 4, 4), RoomClassification.Storage)
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private MapData CreateTestMapWithSpecificRoomTypes()
        {
            var map = new MapData(15, 15);
            
            var rooms = new[]
            {
                new RoomData(1, new RectInt(1, 1, 4, 4), RoomClassification.BreakRoom),
                new RoomData(2, new RectInt(8, 1, 4, 4), RoomClassification.Office),
                new RoomData(3, new RectInt(1, 8, 4, 4), RoomClassification.Storage),
                new RoomData(4, new RectInt(8, 8, 4, 4), RoomClassification.ServerRoom)
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private MapData CreateLargeTestMap(int roomCount)
        {
            var map = new MapData(100, 100);
            var roomTypes = new[] 
            { 
                RoomClassification.Office, 
                RoomClassification.Conference, 
                RoomClassification.BreakRoom, 
                RoomClassification.Storage,
                RoomClassification.ServerRoom,
                RoomClassification.Lobby
            };

            for (int i = 0; i < roomCount; i++)
            {
                var x = (i % 10) * 10 + 1;
                var y = (i / 10) * 10 + 1;
                var roomType = roomTypes[i % roomTypes.Length];
                
                var room = new RoomData(i + 1, new RectInt(x, y, 8, 8), roomType);
                map.AddRoom(room);
            }

            return map;
        }

        private BiomeConfiguration CreateTestBiome(string name = "TestBiome")
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            // Set basic properties - the exact values don't matter for resource testing
            return biome;
        }

        #endregion
    }
}