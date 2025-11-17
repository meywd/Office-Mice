using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using OfficeMice.MapGeneration.Corridors;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Pathfinding;

namespace OfficeMice.MapGeneration.Tests.Performance
{
    [TestFixture]
    public class TwoPassCorridorPerformanceTests
    {
        private TwoPassCorridorGenerator _generator;
        private MapGenerationSettings _testSettings;
        
        [SetUp]
        public void SetUp()
        {
            _generator = new TwoPassCorridorGenerator();
            _testSettings = CreatePerformanceTestSettings();
        }
        
        [TearDown]
        public void TearDown()
        {
            _generator = null;
            _testSettings = null;
        }
        
        [Test, Performance]
        public void Performance_SmallMap_10Rooms()
        {
            var rooms = GenerateTestRooms(10, 50, 50);
            
            Measure.Method(() =>
            {
                var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
                Assert.IsNotNull(result);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .SampleGroup("SmallMap_10Rooms")
            .GC()
            .Run();
        }
        
        [Test, Performance]
        public void Performance_MediumMap_50Rooms()
        {
            var rooms = GenerateTestRooms(50, 100, 100);
            
            Measure.Method(() =>
            {
                var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
                Assert.IsNotNull(result);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .SampleGroup("MediumMap_50Rooms")
            .GC()
            .Run();
        }
        
        [Test, Performance]
        public void Performance_LargeMap_100Rooms()
        {
            var rooms = GenerateTestRooms(100, 150, 150);
            
            Measure.Method(() =>
            {
                var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
                Assert.IsNotNull(result);
            })
            .WarmupCount(2)
            .MeasurementCount(5)
            .SampleGroup("LargeMap_100Rooms")
            .GC()
            .Run();
        }
        
        [Test, Performance]
        public void Performance_VeryLargeMap_200Rooms()
        {
            var rooms = GenerateTestRooms(200, 200, 200);
            
            Measure.Method(() =>
            {
                var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
                Assert.IsNotNull(result);
            })
            .WarmupCount(1)
            .MeasurementCount(3)
            .SampleGroup("VeryLargeMap_200Rooms")
            .GC()
            .Run();
        }
        
        [Test]
        public void Performance_MemoryUsage_100Rooms()
        {
            // Arrange
            var rooms = GenerateTestRooms(100, 150, 150);
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Force garbage collection to get accurate memory measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;
            var memoryUsedMB = memoryUsed / (1024f * 1024f);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(memoryUsedMB < 30f, 
                $"Memory usage should be under 30MB for 100 rooms, used {memoryUsedMB:F2}MB");
            
            Debug.Log($"Memory usage for 100 rooms: {memoryUsedMB:F2}MB");
        }
        
        [Test]
        public void Performance_ConnectivityValidation_Speed()
        {
            // Arrange
            var rooms = GenerateTestRooms(100, 150, 150);
            var corridors = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < 100; i++)
            {
                var result = _generator.ValidateConnectivity(rooms, corridors);
                Assert.IsNotNull(result);
            }
            
            stopwatch.Stop();
            
            // Assert
            var averageTime = stopwatch.ElapsedMilliseconds / 100f;
            Assert.IsTrue(averageTime < 5f, 
                $"Connectivity validation should average under 5ms, took {averageTime:F2}ms");
            
            Debug.Log($"Average connectivity validation time: {averageTime:F2}ms");
        }
        
        [Test]
        public void Performance_CorridorOptimization_Speed()
        {
            // Arrange
            var rooms = GenerateTestRooms(50, 100, 100);
            var corridors = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < 50; i++)
            {
                var result = _generator.OptimizeCorridors(corridors, rooms, _testSettings);
                Assert.IsNotNull(result);
            }
            
            stopwatch.Stop();
            
            // Assert
            var averageTime = stopwatch.ElapsedMilliseconds / 50f;
            Assert.IsTrue(averageTime < 10f, 
                $"Corridor optimization should average under 10ms, took {averageTime:F2}ms");
            
            Debug.Log($"Average corridor optimization time: {averageTime:F2}ms");
        }
        
        [Test]
        public void Performance_TotalCorridorLength_Calculation()
        {
            // Arrange
            var rooms = GenerateTestRooms(100, 150, 150);
            var corridors = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < 1000; i++)
            {
                var totalLength = _generator.CalculateTotalCorridorLength(corridors);
                Assert.IsTrue(totalLength > 0);
            }
            
            stopwatch.Stop();
            
            // Assert
            var averageTime = stopwatch.ElapsedMilliseconds / 1000f;
            Assert.IsTrue(averageTime < 0.1f, 
                $"Total length calculation should average under 0.1ms, took {averageTime:F3}ms");
            
            Debug.Log($"Average total length calculation time: {averageTime:F3}ms");
        }
        
        [Test]
        public void Performance_ShortestPath_Finding()
        {
            // Arrange
            var rooms = GenerateTestRooms(50, 100, 100);
            var corridors = _generator.ConnectRooms(rooms, _testSettings, 12345);
            var startRoom = rooms[0];
            var endRoom = rooms[rooms.Count - 1];
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            for (int i = 0; i < 100; i++)
            {
                var path = _generator.FindShortestPath(startRoom, endRoom, corridors);
                Assert.IsNotNull(path);
            }
            
            stopwatch.Stop();
            
            // Assert
            var averageTime = stopwatch.ElapsedMilliseconds / 100f;
            Assert.IsTrue(averageTime < 2f, 
                $"Shortest path finding should average under 2ms, took {averageTime:F2}ms");
            
            Debug.Log($"Average shortest path finding time: {averageTime:F2}ms");
        }
        
        [Test]
        public void Performance_Regressions_CompareWithBaseline()
        {
            // Arrange
            var rooms = GenerateTestRooms(50, 100, 100);
            
            // Baseline measurements (these would be stored from previous runs)
            var baselineTimeMs = 100f; // Example baseline
            var baselineMemoryMB = 15f; // Example baseline
            
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            stopwatch.Stop();
            
            var currentTime = stopwatch.ElapsedMilliseconds;
            var currentMemory = (finalMemory - initialMemory) / (1024f * 1024f);
            
            // Assert - Allow 20% time regression and 25% memory regression
            Assert.IsTrue(currentTime < baselineTimeMs * 1.2f, 
                $"Time regression: {currentTime}ms vs baseline {baselineTimeMs}ms");
            Assert.IsTrue(currentMemory < baselineMemoryMB * 1.25f, 
                $"Memory regression: {currentMemory:F2}MB vs baseline {baselineMemoryMB:F2}MB");
            
            Debug.Log($"Performance: {currentTime}ms (baseline: {baselineTimeMs}ms), " +
                     $"Memory: {currentMemory:F2}MB (baseline: {baselineMemoryMB:F2}MB)");
        }
        
        [Test]
        public void Performance_ConcurrentGeneration_ThreadSafety()
        {
            // Arrange
            var rooms = GenerateTestRooms(25, 75, 75);
            var results = new List<List<CorridorData>>();
            var exceptions = new List<Exception>();
            
            // Act - Generate corridors on multiple threads
            var tasks = new List<System.Threading.Tasks.Task>();
            
            for (int i = 0; i < 4; i++)
            {
                var seed = 12345 + i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var generator = new TwoPassCorridorGenerator();
                        var result = generator.ConnectRooms(rooms, _testSettings, seed);
                        lock (results)
                        {
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }
            
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            
            // Assert
            Assert.AreEqual(0, exceptions.Count, $"No exceptions should occur: {string.Join(", ", exceptions.Select(e => e.Message))}");
            Assert.AreEqual(4, results.Count, "Should generate results for all threads");
            
            foreach (var result in results)
            {
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Count > 0, "Each result should have corridors");
            }
        }
        
        [Test]
        public void Performance_GarbageCollection_Pressure()
        {
            // Arrange
            var rooms = GenerateTestRooms(25, 75, 75);
            var initialGCCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            
            // Act - Generate many corridor sets to test GC pressure
            for (int i = 0; i < 50; i++)
            {
                var result = _generator.ConnectRooms(rooms, _testSettings, 12345 + i);
                Assert.IsNotNull(result);
            }
            
            // Force final GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalGCCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            var gcIncrease = finalGCCount - initialGCCount;
            
            // Assert
            Assert.IsTrue(gcIncrease < 10, 
                $"GC pressure should be minimal, GC collections increased by {gcIncrease}");
            
            Debug.Log($"GC collections increased by {gcIncrease} over 50 generations");
        }
        
        #region Helper Methods
        
        private MapGenerationSettings CreatePerformanceTestSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            
            var mapConfig = new MapConfiguration
            {
                _mapSizeRange = new Vector2Int(50, 200),
                _roomSizeRange = new Vector2Int(5, 15),
                _minRooms = 5,
                _maxRooms = 200
            };
            
            var corridorConfig = new CorridorConfiguration
            {
                _corridorType = CorridorType.LShaped,
                _minWidth = 1,
                _maxWidth = 5,
                _pathSmoothing = 0.1f // Lower smoothing for performance tests
            };
            
            var debugSettings = new DebugSettings
            {
                _enableLogging = false, // Disable logging for performance tests
                _logPerformanceMetrics = false
            };
            
            var performanceSettings = new PerformanceSettings
            {
                _generationTimeoutMs = 30000, // Longer timeout for performance tests
                _poolObjects = true,
                _maxPoolSize = 200
            };
            
            // Use reflection to set private fields
            typeof(MapGenerationSettings).GetField("_mapConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, mapConfig);
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, corridorConfig);
            typeof(MapGenerationSettings).GetField("_debugSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, debugSettings);
            typeof(MapGenerationSettings).GetField("_performanceSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, performanceSettings);
            
            return settings;
        }
        
        private List<RoomData> GenerateTestRooms(int count, int mapWidth, int mapHeight)
        {
            var rooms = new List<RoomData>();
            var random = new System.Random(12345); // Fixed seed for reproducible tests
            var usedPositions = new HashSet<RectInt>();
            
            for (int i = 0; i < count; i++)
            {
                int attempts = 0;
                while (attempts < 100)
                {
                    var width = random.Next(5, 12);
                    var height = random.Next(5, 12);
                    var x = random.Next(0, mapWidth - width);
                    var y = random.Next(0, mapHeight - height);
                    
                    var bounds = new RectInt(x, y, width, height);
                    
                    // Check for overlaps
                    bool overlaps = false;
                    foreach (var used in usedPositions)
                    {
                        if (bounds.Overlaps(used))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    
                    if (!overlaps)
                    {
                        var room = new RoomData(bounds);
                        room.RoomID = i;
                        rooms.Add(room);
                        usedPositions.Add(bounds);
                        break;
                    }
                    
                    attempts++;
                }
                
                // If we couldn't place a room without overlap, place it anyway (for test purposes)
                if (rooms.Count <= i)
                {
                    var width = random.Next(5, 8);
                    var height = random.Next(5, 8);
                    var x = (i % 10) * 15;
                    var y = (i / 10) * 15;
                    
                    var room = new RoomData(new RectInt(x, y, width, height));
                    room.RoomID = i;
                    rooms.Add(room);
                }
            }
            
            return rooms;
        }
        
        #endregion
    }
}