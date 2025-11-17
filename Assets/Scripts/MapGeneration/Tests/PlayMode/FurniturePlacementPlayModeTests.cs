using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.AssetLoading;

namespace OfficeMice.MapGeneration.Tests.PlayMode
{
    [TestFixture]
    public class FurniturePlacementPlayModeTests
    {
        private FurniturePlacer _furniturePlacer;
        private TileAssetLoader _assetLoader;
        private GameObject _testParent;

        [SetUp]
        public void SetUp()
        {
            _testParent = new GameObject("TestParent");
            _assetLoader = new TileAssetLoader();
            _furniturePlacer = new FurniturePlacer(_assetLoader, 42);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testParent != null)
            {
                Object.DestroyImmediate(_testParent);
            }
            
            _furniturePlacer = null;
            _assetLoader = null;
        }

        [UnityTest]
        public IEnumerator PlaceFurniture_WithRealAssets_CreatesGameObjects()
        {
            // Arrange
            var map = CreateTestMap();
            var biome = CreateTestBiome();

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);

            // Wait for potential async operations
            yield return null;

            // Assert
            Assert.IsNotNull(placedFurniture, "Should return furniture list");
            Assert.IsTrue(placedFurniture.Count > 0, "Should place some furniture");

            // Verify furniture properties
            foreach (var furniture in placedFurniture)
            {
                Assert.IsTrue(furniture.IsValid(), "All placed furniture should be valid");
                Assert.IsTrue(map.GetRoom(furniture.RoomID) != null, "Furniture should reference valid room");
                
                // Check position is within room bounds
                var room = map.GetRoom(furniture.RoomID);
                Assert.IsTrue(room.ContainsPoint(furniture.Position), 
                    $"Furniture at {furniture.Position} should be within room bounds");
            }
        }

        [UnityTest]
        public IEnumerator PlaceFurniture_WithDifferentSeeds_ProducesDifferentLayouts()
        {
            // Arrange
            var map = CreateTestMap();
            var biome = CreateTestBiome();

            // Act
            _furniturePlacer.SetSeed(123);
            var furniture1 = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            _furniturePlacer.SetSeed(456);
            var furniture2 = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            // Assert
            Assert.AreNotEqual(furniture1.Count, furniture2.Count, 
                "Different seeds should produce different furniture counts or positions");
        }

        [UnityTest]
        public IEnumerator PlaceFurniture_Performance_InPlayMode()
        {
            // Arrange
            var map = CreateLargeTestMap();
            var biome = CreateTestBiome();
            var targetTimeMs = 200;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);
            stopwatch.Stop();

            yield return null;

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"PlayMode furniture placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(placedFurniture.Count > 0, "Should place furniture in large map");
        }

        [UnityTest]
        public IEnumerator FurniturePlacementEvents_FireCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biome = CreateTestBiome();
            var eventCount = 0;
            FurnitureData lastFurniture = null;

            _furniturePlacer.OnFurniturePlaced += (furniture) =>
            {
                eventCount++;
                lastFurniture = furniture;
            };

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            // Assert
            Assert.IsTrue(eventCount > 0, "Should fire furniture placement events");
            Assert.IsNotNull(lastFurniture, "Last event should have furniture data");
            Assert.AreEqual(eventCount, placedFurniture.Count, "Event count should match placed furniture count");
        }

        [UnityTest]
        public IEnumerator CollisionDetection_PreventsOverlappingFurniture()
        {
            // Arrange
            var map = CreateSmallTestMap(); // Small map to force collisions
            var biome = CreateTestBiome();

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            // Assert
            // Check for overlapping furniture
            for (int i = 0; i < placedFurniture.Count; i++)
            {
                for (int j = i + 1; j < placedFurniture.Count; j++)
                {
                    var furniture1 = placedFurniture[i];
                    var furniture2 = placedFurniture[j];
                    
                    if (furniture1.RoomID == furniture2.RoomID)
                    {
                        Assert.IsFalse(furniture1.OverlapsWith(furniture2), 
                            $"Furniture {i} and {j} should not overlap in room {furniture1.RoomID}");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator FurnitureRotation_VariesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biome = CreateTestBiome();

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            // Assert
            var rotations = new System.Collections.Generic.HashSet<int>();
            foreach (var furniture in placedFurniture)
            {
                rotations.Add(furniture.Rotation);
                
                // All rotations should be multiples of 90 degrees
                Assert.IsTrue(furniture.Rotation % 90 == 0, 
                    $"Furniture rotation should be multiple of 90, was {furniture.Rotation}");
            }

            // Should have some variety in rotations (unless all furniture is the same type)
            if (placedFurniture.Count > 5)
            {
                Assert.IsTrue(rotations.Count > 1, "Should have variety in furniture rotations");
            }
        }

        [UnityTest]
        public IEnumerator MemoryUsage_StaysWithinLimits()
        {
            // Arrange
            var map = CreateLargeTestMap();
            var biome = CreateTestBiome();

            // Force garbage collection to get baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(map, biome);

            yield return null;

            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.Less(memoryIncrease, 15 * 1024 * 1024, // 15MB limit for PlayMode
                $"PlayMode furniture placement should use less than 15MB memory, used {memoryIncrease / (1024f * 1024f):F2}MB");
        }

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData(100, 100, 12345);
            
            var rooms = new[]
            {
                new RoomData(new RectInt(10, 10, 8, 6)) { RoomID = 1, SetClassification(RoomClassification.Office) },
                new RoomData(new RectInt(30, 10, 12, 8)) { RoomID = 2, SetClassification(RoomClassification.Conference) },
                new RoomData(new RectInt(10, 30, 6, 6)) { RoomID = 3, SetClassification(RoomClassification.BreakRoom) },
                new RoomData(new RectInt(30, 30, 8, 8)) { RoomID = 4, SetClassification(RoomClassification.Storage) },
                new RoomData(new RectInt(50, 10, 10, 10)) { RoomID = 5, SetClassification(RoomClassification.ServerRoom) }
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private MapData CreateSmallTestMap()
        {
            var map = new MapData(50, 50, 12345);
            
            var rooms = new[]
            {
                new RoomData(new RectInt(5, 5, 6, 6)) { RoomID = 1, SetClassification(RoomClassification.Office) },
                new RoomData(new RectInt(20, 5, 8, 8)) { RoomID = 2, SetClassification(RoomClassification.Office) }
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private MapData CreateLargeTestMap()
        {
            var map = new MapData(200, 200, 12345);
            var roomId = 1;

            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var x = i * 15;
                    var y = j * 15;
                    var width = UnityEngine.Random.Range(6, 12);
                    var height = UnityEngine.Random.Range(6, 12);
                    
                    var room = new RoomData(new RectInt(x, y, width, height));
                    room.RoomID = roomId++;
                    
                    var classifications = new[] 
                    { 
                        RoomClassification.Office, 
                        RoomClassification.Conference, 
                        RoomClassification.BreakRoom,
                        RoomClassification.Storage,
                        RoomClassification.ServerRoom
                    };
                    room.SetClassification(classifications[UnityEngine.Random.Range(0, classifications.Length)]);
                    
                    map.AddRoom(room);
                }
            }

            return map;
        }

        private BiomeConfiguration CreateTestBiome()
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            return biome;
        }

        #endregion
    }
}