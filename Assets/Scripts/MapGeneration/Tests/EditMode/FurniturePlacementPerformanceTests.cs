using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Tests.EditMode;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class FurniturePlacementPerformanceTests
    {
        private FurniturePlacer _furniturePlacer;
        private MockAssetLoader _mockAssetLoader;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _furniturePlacer = new FurniturePlacer(_mockAssetLoader, 42);
        }

        [TearDown]
        public void TearDown()
        {
            _furniturePlacer = null;
            _mockAssetLoader = null;
        }

        [Test]
        [Category("Performance")]
        public void PlaceFurniture_SmallMap_PerformanceTarget()
        {
            // Arrange
            var smallMap = CreateMapWithRooms(10); // 10 rooms
            var biome = CreateTestBiome();
            var targetTimeMs = 50; // Target: <50ms for small maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var furniture = _furniturePlacer.PlaceFurniture(smallMap, biome);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Small map furniture placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(furniture.Count, 0, "Should place furniture in small map");
            
            var stats = _furniturePlacer.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 10, 
                $"Average time per room should be under 10ms for small maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void PlaceFurniture_MediumMap_PerformanceTarget()
        {
            // Arrange
            var mediumMap = CreateMapWithRooms(50); // 50 rooms
            var biome = CreateTestBiome();
            var targetTimeMs = 150; // Target: <150ms for medium maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var furniture = _furniturePlacer.PlaceFurniture(mediumMap, biome);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Medium map furniture placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(furniture.Count, 0, "Should place furniture in medium map");
            
            var stats = _furniturePlacer.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 5, 
                $"Average time per room should be under 5ms for medium maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void PlaceFurniture_LargeMap_PerformanceTarget()
        {
            // Arrange
            var largeMap = CreateMapWithRooms(100); // 100 rooms
            var biome = CreateTestBiome();
            var targetTimeMs = 200; // Target: <200ms for large maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var furniture = _furniturePlacer.PlaceFurniture(largeMap, biome);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Large map furniture placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(furniture.Count, 0, "Should place furniture in large map");
            
            var stats = _furniturePlacer.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 3, 
                $"Average time per room should be under 3ms for large maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void FindValidPositions_PerformanceTarget()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 20, 20));
            var objectSize = new Vector2Int(2, 2);
            var existingObjects = CreateExistingObjects(50); // 50 existing objects
            var targetTimeMs = 10; // Target: <10ms for position finding

            // Act
            var stopwatch = Stopwatch.StartNew();
            var validPositions = _furniturePlacer.FindValidPositions(room, objectSize, existingObjects, 1);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Finding valid positions should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(validPositions.Count, 0, "Should find valid positions even with many existing objects");
        }

        [Test]
        [Category("Performance")]
        public void CollisionDetection_PerformanceTarget()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 15, 15));
            var objectSize = new Vector2Int(1, 1);
            var existingObjects = CreateExistingObjects(100); // 100 existing objects
            var targetTimeMs = 5; // Target: <5ms for collision detection

            // Act
            var stopwatch = Stopwatch.StartNew();
            var validPositions = _furniturePlacer.FindValidPositions(room, objectSize, existingObjects, 0);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Collision detection should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        [Category("Performance")]
        public void MemoryUsage_FurniturePlacement_WithinLimits()
        {
            // Arrange
            var map = CreateMapWithRooms(50);
            var biome = CreateTestBiome();
            
            // Force garbage collection to get baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var furniture = _furniturePlacer.PlaceFurniture(map, biome);
            
            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.Less(memoryIncrease, 10 * 1024 * 1024, // 10MB limit
                $"Furniture placement should use less than 10MB memory, used {memoryIncrease / (1024f * 1024f):F2}MB");
            
            // Memory per furniture should be reasonable
            var memoryPerFurniture = furniture.Count > 0 ? memoryIncrease / furniture.Count : 0;
            Assert.Less(memoryPerFurniture, 1024, // 1KB per furniture
                $"Memory per furniture should be less than 1KB, was {memoryPerFurniture} bytes");
        }

        [Test]
        [Category("Performance")]
        public void Scalability_RoomCount_LinearPerformance()
        {
            // Arrange
            var roomCounts = new[] { 10, 25, 50, 75, 100 };
            var timesPerRoom = new List<float>();

            // Act
            foreach (var roomCount in roomCounts)
            {
                var map = CreateMapWithRooms(roomCount);
                var biome = CreateTestBiome();
                
                var stopwatch = Stopwatch.StartNew();
                _furniturePlacer.PlaceFurniture(map, biome);
                stopwatch.Stop();
                
                var timePerRoom = (float)stopwatch.ElapsedMilliseconds / roomCount;
                timesPerRoom.Add(timePerRoom);
            }

            // Assert - Check that performance scales reasonably (not exponentially)
            for (int i = 1; i < timesPerRoom.Count; i++)
            {
                var ratio = timesPerRoom[i] / timesPerRoom[0];
                Assert.Less(ratio, 3.0f, 
                    $"Time per room should not increase by more than 3x from {roomCounts[0]} to {roomCounts[i]} rooms. " +
                    $"Was {timesPerRoom[0]:F2}ms -> {timesPerRoom[i]:F2}ms (ratio: {ratio:F2})");
            }
        }

        [Test]
        [Category("Performance")]
        public void RepeatedPlacement_ConsistentPerformance()
        {
            // Arrange
            var map = CreateMapWithRooms(25);
            var biome = CreateTestBiome();
            var iterations = 10;
            var times = new List<long>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                _furniturePlacer.SetSeed(42 + i); // Different seed each time
                var stopwatch = Stopwatch.StartNew();
                _furniturePlacer.PlaceFurniture(map, biome);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var averageTime = times.Count > 0 ? times.Average() : 0;
            var maxTime = times.Count > 0 ? times.Max() : 0;
            var minTime = times.Count > 0 ? times.Min() : 0;

            Assert.Less(averageTime, 100, "Average placement time should be under 100ms");
            Assert.Less(maxTime - minTime, 50, "Performance variance should be under 50ms");
            
            // Performance should be consistent (low standard deviation)
            var variance = times.Count > 1 ? times.Sum(t => (t - averageTime) * (t - averageTime)) / (times.Count - 1) : 0;
            var stdDev = System.Math.Sqrt(variance);
            Assert.Less(stdDev, 15, "Standard deviation should be under 15ms");
        }

        #region Helper Methods

        private MapData CreateMapWithRooms(int roomCount)
        {
            var map = new MapData(200, 200, 12345);
            var roomId = 1;

            for (int i = 0; i < roomCount; i++)
            {
                var x = (i % 10) * 20;
                var y = (i / 10) * 20;
                var width = UnityEngine.Random.Range(5, 12);
                var height = UnityEngine.Random.Range(5, 12);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.RoomID = roomId++;
                
                // Assign random classification
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

            return map;
        }

        private BiomeConfiguration CreateTestBiome()
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            return biome;
        }

        private List<PlacedObjectData> CreateExistingObjects(int count)
        {
            var objects = new List<PlacedObjectData>();
            
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2Int(
                    UnityEngine.Random.Range(0, 50),
                    UnityEngine.Random.Range(0, 50)
                );
                var size = new Vector2Int(
                    UnityEngine.Random.Range(1, 3),
                    UnityEngine.Random.Range(1, 3)
                );
                
                objects.Add(new TestPlacedObjectData($"obj_{i}", "Test", 0, position, size));
            }
            
            return objects;
        }

        #endregion

        #region Test Helper Classes

        private class TestPlacedObjectData : PlacedObjectData
        {
            public TestPlacedObjectData(string objectID, string objectType, int roomID, 
                                       Vector2Int position, Vector2Int size) 
                : base(objectID, objectType, roomID, position, size)
            {
            }

            public override PlacedObjectData Clone()
            {
                return new TestPlacedObjectData(ObjectID, ObjectType, RoomID, Position, Size);
            }
        }

        #endregion
    }
}