using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.PerformanceTesting;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Base;
using OfficeMice.MapGeneration.Factories;

namespace OfficeMice.MapGeneration.Performance
{
    /// <summary>
    /// Performance benchmarking utilities and tests for MapGeneration components.
    /// Validates that all components meet performance requirements and provides regression detection.
    /// </summary>
    [TestFixture]
    public class PerformanceBenchmark : BaseTestFixture
    {
        private struct PerformanceMetrics
        {
            public long ExecutionTimeMs;
            public long MemoryUsedBytes;
            public int TestIterations;
            public double AverageTimeMs;
            public double MinTimeMs;
            public double MaxTimeMs;
            public double StandardDeviationMs;
            public double GcPressureBytes;
            public int FrameCount;
            public double FrameTimeMs;
        }

        private struct BenchmarkTarget
        {
            public string Name;
            public double MaxTimeMs;
            public double MaxMemoryMB;
            public double MaxGcPressureKB;
            public double MaxFrameTimeMs;
        }

        private const int PERFORMANCE_TEST_ITERATIONS = 100;
        private const int WARMUP_ITERATIONS = 10;
        private const long MAX_MEMORY_BYTES = 100 * 1024 * 1024; // 100MB
        private const double MAX_GC_PRESSURE_KB = 500.0; // 500KB per frame
        private const double TARGET_FRAME_TIME_MS = 16.67; // 60 FPS target

        private static readonly Dictionary<string, BenchmarkTarget> BenchmarkTargets = new()
        {
            ["MapGeneration"] = new BenchmarkTarget { Name = "MapGeneration", MaxTimeMs = 3000, MaxMemoryMB = 200, MaxGcPressureKB = 500, MaxFrameTimeMs = 16.67 },
            ["RoomGeneration"] = new BenchmarkTarget { Name = "RoomGeneration", MaxTimeMs = 500, MaxMemoryMB = 50, MaxGcPressureKB = 100, MaxFrameTimeMs = 5.0 },
            ["CorridorGeneration"] = new BenchmarkTarget { Name = "CorridorGeneration", MaxTimeMs = 200, MaxMemoryMB = 30, MaxGcPressureKB = 50, MaxFrameTimeMs = 3.0 },
            ["Pathfinding"] = new BenchmarkTarget { Name = "Pathfinding", MaxTimeMs = 50, MaxMemoryMB = 10, MaxGcPressureKB = 10, MaxFrameTimeMs = 1.0 },
            ["ContentPopulation"] = new BenchmarkTarget { Name = "ContentPopulation", MaxTimeMs = 300, MaxMemoryMB = 40, MaxGcPressureKB = 80, MaxFrameTimeMs = 4.0 },
            ["TileRendering"] = new BenchmarkTarget { Name = "TileRendering", MaxTimeMs = 400, MaxMemoryMB = 60, MaxGcPressureKB = 100, MaxFrameTimeMs = 6.0 },
            ["AssetLoading"] = new BenchmarkTarget { Name = "AssetLoading", MaxTimeMs = 100, MaxMemoryMB = 20, MaxGcPressureKB = 20, MaxFrameTimeMs = 2.0 }
        };

        #region Unity Performance Testing API Integration

        [Test, Performance]
        [Category("Performance")]
        [Category("Benchmark")]
        public void Performance_MapGeneration_UnityBenchmark()
        {
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            Measure.Method(() => generator.GenerateMap(settings))
                .WarmupCount(WARMUP_ITERATIONS)
                .MeasurementCount(PERFORMANCE_TEST_ITERATIONS)
                .SampleGroup("MapGeneration")
                .Definitions()
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Benchmark")]
        public void Performance_RoomGeneration_UnityBenchmark()
        {
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("performance");

            Measure.Method(() => generator.GenerateRooms(settings))
                .WarmupCount(WARMUP_ITERATIONS)
                .MeasurementCount(PERFORMANCE_TEST_ITERATIONS)
                .SampleGroup("RoomGeneration")
                .Definitions()
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Benchmark")]
        public void Performance_CorridorGeneration_UnityBenchmark()
        {
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("performance");
            var rooms = CreateTestMapData("multiple_rooms").Rooms;

            Measure.Method(() => generator.ConnectRooms(rooms, settings))
                .WarmupCount(WARMUP_ITERATIONS)
                .MeasurementCount(PERFORMANCE_TEST_ITERATIONS)
                .SampleGroup("CorridorGeneration")
                .Definitions()
                .Run();
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Benchmark")]
        public void Performance_Pathfinding_UnityBenchmark()
        {
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(100, 100);
            var obstacles = new bool[200, 200];

            Measure.Method(() => pathfinder.FindPath(start, end, obstacles))
                .WarmupCount(WARMUP_ITERATIONS)
                .MeasurementCount(PERFORMANCE_TEST_ITERATIONS)
                .SampleGroup("Pathfinding")
                .Definitions()
                .Run();
        }

        #endregion

        #region Enhanced Performance Test Attributes

        [Test]
        [Category("Performance")]
        [Category("Baseline")]
        public void Performance_MapGeneration_CompletesWithinThreshold()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");
            var target = BenchmarkTargets["MapGeneration"];

            // Act & Assert
            var metrics = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                "MapGeneration", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, target.MaxTimeMs, 
                $"Map generation average time {metrics.AverageTimeMs:F2}ms should be < {target.MaxTimeMs}ms");
            Assert.Less(metrics.MemoryUsedBytes, target.MaxMemoryMB * 1024 * 1024,
                $"Map generation memory usage {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB should be < {target.MaxMemoryMB}MB");
            Assert.Less(metrics.GcPressureBytes, target.MaxGcPressureKB * 1024,
                $"Map generation GC pressure {metrics.GcPressureBytes / 1024:F2}KB should be < {target.MaxGcPressureKB}KB");
        }

        [Test]
        [Category("Performance")]
        [Category("Baseline")]
        public void Performance_RoomGeneration_CompletesWithinThreshold()
        {
            // Arrange
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("performance");
            var target = BenchmarkTargets["RoomGeneration"];

            // Act & Assert
            var metrics = MeasurePerformanceWithGC(() => generator.GenerateRooms(settings), 
                "RoomGeneration", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, target.MaxTimeMs, 
                $"Room generation average time {metrics.AverageTimeMs:F2}ms should be < {target.MaxTimeMs}ms");
            Assert.Less(metrics.MemoryUsedBytes, target.MaxMemoryMB * 1024 * 1024,
                $"Room generation memory usage {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB should be < {target.MaxMemoryMB}MB");
            Assert.Less(metrics.GcPressureBytes, target.MaxGcPressureKB * 1024,
                $"Room generation GC pressure {metrics.GcPressureBytes / 1024:F2}KB should be < {target.MaxGcPressureKB}KB");
        }

        [Test]
        [Category("Performance")]
        public void Performance_CorridorGeneration_CompletesWithinThreshold()
        {
            // Arrange
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("performance");
            var rooms = CreateTestMapData("multiple_rooms").Rooms;

            // Act & Assert
            var metrics = MeasurePerformance(() => generator.ConnectRooms(rooms, settings), 
                "CorridorGeneration", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, 200, 
                $"Corridor generation average time {metrics.AverageTimeMs:F2}ms should be < 200ms");
        }

        [Test]
        [Category("Performance")]
        public void Performance_Pathfinding_CompletesWithinThreshold()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(100, 100);
            var obstacles = new bool[200, 200];

            // Act & Assert
            var metrics = MeasurePerformance(() => pathfinder.FindPath(start, end, obstacles), 
                "Pathfinding", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, 50, 
                $"Pathfinding average time {metrics.AverageTimeMs:F2}ms should be < 50ms");
        }

        [UnityTest]
        [Category("Performance")]
        public IEnumerator Performance_AsyncMapGeneration_CompletesWithinThreshold()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            // Act & Assert
            var metrics = MeasureAsyncPerformance(() => generator.GenerateMapAsync(settings), 
                "AsyncMapGeneration", 10); // Fewer iterations for async tests

            Assert.Less(metrics.AverageTimeMs, 1500, 
                $"Async map generation average time {metrics.AverageTimeMs:F2}ms should be < 1500ms");
            
            yield return null;
        }

        [Test]
        [Category("Performance")]
        public void Performance_ContentPopulation_CompletesWithinThreshold()
        {
            // Arrange
            var populator = new MockContentPopulator();
            var map = CreateTestMapData("complex_layout");
            var biome = MapGenerationTestDataFactory.CreateTestBiomeConfiguration();

            // Act & Assert
            var metrics = MeasurePerformance(() => populator.PopulateContent(map, biome), 
                "ContentPopulation", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, 300, 
                $"Content population average time {metrics.AverageTimeMs:F2}ms should be < 300ms");
        }

        [Test]
        [Category("Performance")]
        public void Performance_TileRendering_CompletesWithinThreshold()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var map = CreateTestMapData("multiple_rooms");
            var tilemaps = new UnityEngine.Tilemaps.Tilemap[2]; // Mock tilemaps

            // Act & Assert
            var metrics = MeasurePerformance(() => renderer.RenderMap(map, tilemaps), 
                "TileRendering", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, 400, 
                $"Tile rendering average time {metrics.AverageTimeMs:F2}ms should be < 400ms");
        }

        [Test]
        [Category("Performance")]
        public void Performance_AssetLoading_CompletesWithinThreshold()
        {
            // Arrange
            var loader = new MockAssetLoader();
            var tileNames = new[] { "ground", "wall", "floor", "door", "window" };

            // Act & Assert
            var metrics = MeasurePerformance(() => 
            {
                foreach (var tileName in tileNames)
                {
                    loader.LoadTile(tileName);
                }
            }, "AssetLoading", PERFORMANCE_TEST_ITERATIONS);

            Assert.Less(metrics.AverageTimeMs, 100, 
                $"Asset loading average time {metrics.AverageTimeMs:F2}ms should be < 100ms");
        }

        #endregion

        #region Stress Tests

        [Test]
        [Category("Performance")]
        [Category("Stress")]
        public void Stress_MultipleMapGenerations_HandlesMemoryCorrectly()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");
            var maps = new List<MapData>();

            // Act
            var metrics = MeasurePerformance(() => 
            {
                var map = generator.GenerateMap(settings);
                maps.Add(map);
            }, "MultipleMapGenerations", 50); // 50 map generations

            // Assert
            Assert.Less(metrics.MemoryUsedBytes, MAX_MEMORY_BYTES * 2,
                $"Multiple map generations memory usage {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB should be < 200MB");
            
            // Cleanup
            maps.Clear();
            System.GC.Collect();
        }

        [Test]
        [Category("Performance")]
        [Category("Stress")]
        public void Stress_LargeMapGeneration_CompletesWithinTimeLimit()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");
            settings.mapWidth = 500;
            settings.mapHeight = 500;
            settings.maxRoomCount = 100;

            // Act & Assert
            var metrics = MeasurePerformance(() => generator.GenerateMap(settings), 
                "LargeMapGeneration", 10); // Fewer iterations for large maps

            Assert.Less(metrics.AverageTimeMs, 5000, 
                $"Large map generation average time {metrics.AverageTimeMs:F2}ms should be < 5000ms");
        }

        #endregion

        #region Regression Tests

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        public void Regression_MapGeneration_PerformanceBaseline()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");
            var baselineTimeMs = 100.0; // Baseline from previous runs

            // Act
            var metrics = MeasurePerformance(() => generator.GenerateMap(settings), 
                "MapGenerationBaseline", PERFORMANCE_TEST_ITERATIONS);

            // Assert - Allow 10% regression tolerance
            var tolerance = baselineTimeMs * 0.1;
            Assert.Less(metrics.AverageTimeMs, baselineTimeMs + tolerance,
                $"Map generation regression: {metrics.AverageTimeMs:F2}ms vs baseline {baselineTimeMs:F2}ms");
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        public void Regression_Pathfinding_PerformanceBaseline()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(50, 50);
            var obstacles = new bool[100, 100];
            var baselineTimeMs = 25.0; // Baseline from previous runs

            // Act
            var metrics = MeasurePerformance(() => pathfinder.FindPath(start, end, obstacles), 
                "PathfindingBaseline", PERFORMANCE_TEST_ITERATIONS);

            // Assert - Allow 10% regression tolerance
            var tolerance = baselineTimeMs * 0.1;
            Assert.Less(metrics.AverageTimeMs, baselineTimeMs + tolerance,
                $"Pathfinding regression: {metrics.AverageTimeMs:F2}ms vs baseline {baselineTimeMs:F2}ms");
        }

        #endregion

        #region Enhanced Performance Measurement Utilities

        private PerformanceMetrics MeasurePerformanceWithGC(Action action, string testName, int iterations)
        {
            var times = new List<long>();
            var gcPressures = new List<long>();
            var memoryBefore = GC.GetTotalMemory(true);
            var gcBefore = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                action();
            }

            // Force GC before measurement
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // Actual measurement
            for (int i = 0; i < iterations; i++)
            {
                var memoryBeforeIteration = GC.GetTotalMemory(false);
                var gcBeforeIteration = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
                
                var stopwatch = Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                
                var memoryAfterIteration = GC.GetTotalMemory(false);
                var gcAfterIteration = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
                
                times.Add(stopwatch.ElapsedMilliseconds);
                gcPressures.Add(memoryAfterIteration - memoryBeforeIteration);
            }

            var memoryAfter = GC.GetTotalMemory(false);
            var gcAfter = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            var memoryUsed = memoryAfter - memoryBefore;
            var totalGcPressure = gcPressures.Count > 0 ? gcPressures.Average() : 0;

            // Calculate statistics
            times.Sort();
            var averageTime = times.Count > 0 ? times.Average() : 0;
            var minTime = times.Count > 0 ? times[0] : 0;
            var maxTime = times.Count > 0 ? times[times.Count - 1] : 0;
            
            var variance = times.Count > 0 ? times.Sum(t => Math.Pow(t - averageTime, 2)) / times.Count : 0;
            var standardDeviation = Math.Sqrt(variance);

            var metrics = new PerformanceMetrics
            {
                ExecutionTimeMs = times.Count > 0 ? times.Sum() : 0,
                MemoryUsedBytes = memoryUsed,
                TestIterations = iterations,
                AverageTimeMs = averageTime,
                MinTimeMs = minTime,
                MaxTimeMs = maxTime,
                StandardDeviationMs = standardDeviation,
                GcPressureBytes = totalGcPressure,
                FrameCount = 1,
                FrameTimeMs = averageTime
            };

            LogEnhancedPerformanceMetrics(testName, metrics);
            return metrics;
        }

        private PerformanceMetrics MeasurePerformance(Action action, string testName, int iterations)
        {
            var times = new List<long>();
            var memoryBefore = GC.GetTotalMemory(true);

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                action();
            }

            // Actual measurement
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            // Calculate statistics
            times.Sort();
            var averageTime = times.Count > 0 ? times.Average() : 0;
            var minTime = times.Count > 0 ? times[0] : 0;
            var maxTime = times.Count > 0 ? times[times.Count - 1] : 0;
            
            var variance = times.Count > 0 ? times.Sum(t => Math.Pow(t - averageTime, 2)) / times.Count : 0;
            var standardDeviation = Math.Sqrt(variance);

            var metrics = new PerformanceMetrics
            {
                ExecutionTimeMs = times.Count > 0 ? times.Sum() : 0,
                MemoryUsedBytes = memoryUsed,
                TestIterations = iterations,
                AverageTimeMs = averageTime,
                MinTimeMs = minTime,
                MaxTimeMs = maxTime,
                StandardDeviationMs = standardDeviation,
                GcPressureBytes = 0,
                FrameCount = 1,
                FrameTimeMs = averageTime
            };

            LogPerformanceMetrics(testName, metrics);
            return metrics;
        }

        private PerformanceMetrics MeasureAsyncPerformance(Func<IEnumerator> asyncAction, string testName, int iterations)
        {
            var times = new List<long>();
            var memoryBefore = GC.GetTotalMemory(true);

            // Warmup
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                var enumerator = asyncAction();
                while (enumerator.MoveNext()) { }
            }

            // Actual measurement
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var enumerator = asyncAction();
                while (enumerator.MoveNext()) { }
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            // Calculate statistics
            times.Sort();
            var averageTime = times.Count > 0 ? times.Average() : 0;
            var minTime = times.Count > 0 ? times[0] : 0;
            var maxTime = times.Count > 0 ? times[times.Count - 1] : 0;
            
            var variance = times.Count > 0 ? times.Sum(t => Math.Pow(t - averageTime, 2)) / times.Count : 0;
            var standardDeviation = Math.Sqrt(variance);

            var metrics = new PerformanceMetrics
            {
                ExecutionTimeMs = times.Count > 0 ? times.Sum() : 0,
                MemoryUsedBytes = memoryUsed,
                TestIterations = iterations,
                AverageTimeMs = averageTime,
                MinTimeMs = minTime,
                MaxTimeMs = maxTime,
                StandardDeviationMs = standardDeviation
            };

            LogPerformanceMetrics(testName, metrics);
            return metrics;
        }

        private void LogEnhancedPerformanceMetrics(string testName, PerformanceMetrics metrics)
        {
            Debug.Log($"📊 Enhanced Performance Metrics for {testName}:");
            Debug.Log($"   Iterations: {metrics.TestIterations}");
            Debug.Log($"   Average Time: {metrics.AverageTimeMs:F2}ms");
            Debug.Log($"   Min Time: {metrics.MinTimeMs:F2}ms");
            Debug.Log($"   Max Time: {metrics.MaxTimeMs:F2}ms");
            Debug.Log($"   Std Dev: {metrics.StandardDeviationMs:F2}ms");
            Debug.Log($"   Memory Used: {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB");
            Debug.Log($"   GC Pressure: {metrics.GcPressureBytes / 1024:F2}KB");
            Debug.Log($"   Frame Time: {metrics.FrameTimeMs:F2}ms");
            Debug.Log($"   Total Time: {metrics.ExecutionTimeMs}ms");
            
            // Check against targets
            if (BenchmarkTargets.TryGetValue(testName, out var target))
            {
                Debug.Log($"   Target Comparison:");
                Debug.Log($"     Time: {metrics.AverageTimeMs:F2}ms / {target.MaxTimeMs}ms {(metrics.AverageTimeMs <= target.MaxTimeMs ? "✅" : "❌")}");
                Debug.Log($"     Memory: {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB / {target.MaxMemoryMB}MB {(metrics.MemoryUsedBytes <= target.MaxMemoryMB * 1024 * 1024 ? "✅" : "❌")}");
                Debug.Log($"     GC Pressure: {metrics.GcPressureBytes / 1024:F2}KB / {target.MaxGcPressureKB}KB {(metrics.GcPressureBytes <= target.MaxGcPressureKB * 1024 ? "✅" : "❌")}");
            }
        }

        private void LogPerformanceMetrics(string testName, PerformanceMetrics metrics)
        {
            Debug.Log($"📊 Performance Metrics for {testName}:");
            Debug.Log($"   Iterations: {metrics.TestIterations}");
            Debug.Log($"   Average Time: {metrics.AverageTimeMs:F2}ms");
            Debug.Log($"   Min Time: {metrics.MinTimeMs:F2}ms");
            Debug.Log($"   Max Time: {metrics.MaxTimeMs:F2}ms");
            Debug.Log($"   Std Dev: {metrics.StandardDeviationMs:F2}ms");
            Debug.Log($"   Memory Used: {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB");
            Debug.Log($"   Total Time: {metrics.ExecutionTimeMs}ms");
        }

        #endregion

        #region Performance Comparison Tests

        [Test]
        [Category("Performance")]
        public void Comparison_SyncVsAsyncGeneration_PerformanceDifference()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var syncMetrics = MeasurePerformance(() => generator.GenerateMap(settings), 
                "SyncMapGeneration", 50);
            var asyncMetrics = MeasureAsyncPerformance(() => generator.GenerateMapAsync(settings), 
                "AsyncMapGeneration", 50);

            // Assert
            Debug.Log($"Sync vs Async Performance Comparison:");
            Debug.Log($"  Sync: {syncMetrics.AverageTimeMs:F2}ms");
            Debug.Log($"  Async: {asyncMetrics.AverageTimeMs:F2}ms");
            Debug.Log($"  Difference: {Math.Abs(syncMetrics.AverageTimeMs - asyncMetrics.AverageTimeMs):F2}ms");

            // Async should not be significantly slower (allow 50% overhead)
            Assert.Less(asyncMetrics.AverageTimeMs, syncMetrics.AverageTimeMs * 1.5,
                "Async generation should not be more than 50% slower than sync generation");
        }

        [Test]
        [Category("Performance")]
        [Category("Scaling")]
        public void Comparison_DifferentMapSizes_ScalesLinearly()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var sizes = new[] { 25, 50, 100, 200 };
            var metrics = new List<(int size, double timeMs, double memoryMB)>();

            // Act
            foreach (var size in sizes)
            {
                var settings = CreateTestSettings("standard");
                settings.mapWidth = size;
                settings.mapHeight = size;
                
                var metric = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                    $"MapSize_{size}", 20);
                metrics.Add((size, metric.AverageTimeMs, metric.MemoryUsedBytes / (1024.0 * 1024.0)));
            }

            // Assert - Check that scaling is roughly linear
            Debug.Log("Map Size Scaling Analysis:");
            foreach (var (size, time, memory) in metrics)
            {
                Debug.Log($"  Size {size}x{size}: {time:F2}ms, {memory:F2}MB");
            }

            // Verify that larger maps take proportionally more time
            for (int i = 1; i < metrics.Count; i++)
            {
                var sizeRatio = (double)metrics[i].size / metrics[i - 1].size;
                var timeRatio = metrics[i].timeMs / metrics[i - 1].timeMs;
                var memoryRatio = metrics[i].memory / metrics[i - 1].memory;
                
                // Allow some tolerance for non-linear factors
                Assert.Less(timeRatio, sizeRatio * 2, 
                    $"Time scaling should be roughly linear (size ratio: {sizeRatio:F2}, time ratio: {timeRatio:F2})");
                Assert.Less(memoryRatio, sizeRatio * 2, 
                    $"Memory scaling should be roughly linear (size ratio: {sizeRatio:F2}, memory ratio: {memoryRatio:F2})");
            }
        }

        [Test]
        [Category("Performance")]
        [Category("Baseline")]
        public void Baseline_100RoomMap_EstablishPerformanceMetrics()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");
            settings.maxRoomCount = 100;
            settings.mapWidth = 200;
            settings.mapHeight = 200;

            // Act
            var metrics = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                "Baseline_100RoomMap", 50);

            // Assert - Establish baseline metrics
            Debug.Log($"🎯 Baseline Performance for 100-room map:");
            Debug.Log($"   Generation Time: {metrics.AverageTimeMs:F2}ms");
            Debug.Log($"   Memory Usage: {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB");
            Debug.Log($"   GC Pressure: {metrics.GcPressureBytes / 1024:F2}KB");
            Debug.Log($"   Std Dev: {metrics.StandardDeviationMs:F2}ms");

            // Save baseline to file for regression detection
            SaveBaselineMetrics("100RoomMap", metrics);

            // Verify against acceptance criteria
            Assert.Less(metrics.AverageTimeMs, 3000, 
                $"100-room map generation should complete in < 3 seconds, took {metrics.AverageTimeMs:F2}ms");
            Assert.Less(metrics.MemoryUsedBytes, 200 * 1024 * 1024, 
                $"100-room map should use < 200MB memory, used {metrics.MemoryUsedBytes / (1024 * 1024):F2}MB");
            Assert.Less(metrics.GcPressureBytes, 500 * 1024, 
                $"100-room map should have < 500KB GC pressure, had {metrics.GcPressureBytes / 1024:F2}KB");
        }

        #endregion

        #region Baseline Management and Regression Detection

        private void SaveBaselineMetrics(string testName, PerformanceMetrics metrics)
        {
            var baselinePath = Path.Combine(Application.persistentDataPath, "PerformanceBaselines");
            Directory.CreateDirectory(baselinePath);
            
            var filePath = Path.Combine(baselinePath, $"{testName}_baseline.json");
            var baseline = new
            {
                TestName = testName,
                Timestamp = DateTime.UtcNow.ToString("O"),
                AverageTimeMs = metrics.AverageTimeMs,
                MemoryUsedBytes = metrics.MemoryUsedBytes,
                GcPressureBytes = metrics.GcPressureBytes,
                StandardDeviationMs = metrics.StandardDeviationMs,
                TestIterations = metrics.TestIterations
            };

            File.WriteAllText(filePath, JsonUtility.ToJson(baseline, true));
            Debug.Log($"💾 Baseline saved to: {filePath}");
        }

        private PerformanceMetrics? LoadBaselineMetrics(string testName)
        {
            var baselinePath = Path.Combine(Application.persistentDataPath, "PerformanceBaselines");
            var filePath = Path.Combine(baselinePath, $"{testName}_baseline.json");

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                var baseline = JsonUtility.FromJson<BaselineData>(json);
                
                return new PerformanceMetrics
                {
                    AverageTimeMs = baseline.AverageTimeMs,
                    MemoryUsedBytes = baseline.MemoryUsedBytes,
                    GcPressureBytes = baseline.GcPressureBytes,
                    StandardDeviationMs = baseline.StandardDeviationMs,
                    TestIterations = baseline.TestIterations
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load baseline for {testName}: {ex.Message}");
                return null;
            }
        }

        [System.Serializable]
        private class BaselineData
        {
            public string TestName;
            public string Timestamp;
            public double AverageTimeMs;
            public long MemoryUsedBytes;
            public double GcPressureBytes;
            public double StandardDeviationMs;
            public int TestIterations;
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        public void Regression_MapGeneration_DetectPerformanceRegression()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");
            var baseline = LoadBaselineMetrics("MapGeneration");

            if (!baseline.HasValue)
            {
                Debug.LogWarning("⚠️ No baseline found for MapGeneration, creating new baseline");
                var metrics = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                    "MapGeneration", PERFORMANCE_TEST_ITERATIONS);
                SaveBaselineMetrics("MapGeneration", metrics);
                Assert.Inconclusive("Created new baseline for MapGeneration");
                return;
            }

            // Act
            var currentMetrics = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                "MapGeneration", PERFORMANCE_TEST_ITERATIONS);

            // Assert - Check for regression (10% tolerance)
            var timeRegression = (currentMetrics.AverageTimeMs - baseline.Value.AverageTimeMs) / baseline.Value.AverageTimeMs;
            var memoryRegression = (double)(currentMetrics.MemoryUsedBytes - baseline.Value.MemoryUsedBytes) / baseline.Value.MemoryUsedBytes;
            var gcRegression = (currentMetrics.GcPressureBytes - baseline.Value.GcPressureBytes) / baseline.Value.GcPressureBytes;

            Debug.Log($"📈 Regression Analysis for MapGeneration:");
            Debug.Log($"   Time Regression: {timeRegression:P2} (baseline: {baseline.Value.AverageTimeMs:F2}ms, current: {currentMetrics.AverageTimeMs:F2}ms)");
            Debug.Log($"   Memory Regression: {memoryRegression:P2} (baseline: {baseline.Value.MemoryUsedBytes / (1024 * 1024):F2}MB, current: {currentMetrics.MemoryUsedBytes / (1024 * 1024):F2}MB)");
            Debug.Log($"   GC Pressure Regression: {gcRegression:P2} (baseline: {baseline.Value.GcPressureBytes / 1024:F2}KB, current: {currentMetrics.GcPressureBytes / 1024:F2}KB)");

            Assert.Less(timeRegression, 0.1, 
                $"Time regression detected: {timeRegression:P2} exceeds 10% tolerance");
            Assert.Less(memoryRegression, 0.1, 
                $"Memory regression detected: {memoryRegression:P2} exceeds 10% tolerance");
            Assert.Less(gcRegression, 0.1, 
                $"GC pressure regression detected: {gcRegression:P2} exceeds 10% tolerance");
        }

        #endregion

        #region Frame Time Budgeting Tests

        [UnityTest]
        [Category("Performance")]
        [Category("FrameTime")]
        public IEnumerator Performance_FrameTimeBudgeting_Maintains60FPS()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");
            var frameBudgetMs = TARGET_FRAME_TIME_MS;

            // Act & Assert - Test frame time budgeting during async generation
            var frameTimes = new List<double>();
            var enumerator = generator.GenerateMapAsync(settings);

            while (enumerator.MoveNext())
            {
                var frameStart = Time.realtimeSinceStartup;
                yield return enumerator.Current;
                var frameEnd = Time.realtimeSinceStartup;
                var frameTime = (frameEnd - frameStart) * 1000; // Convert to ms
                frameTimes.Add(frameTime);

                // Assert frame time budget
                Assert.Less(frameTime, frameBudgetMs, 
                    $"Frame time {frameTime:F2}ms exceeds budget of {frameBudgetMs:F2}ms");
            }

            // Analyze frame time statistics
            var averageFrameTime = frameTimes.Count > 0 ? frameTimes.Average() : 0;
            var maxFrameTime = frameTimes.Count > 0 ? frameTimes.Max() : 0;

            Debug.Log($"🎮 Frame Time Analysis:");
            Debug.Log($"   Average Frame Time: {averageFrameTime:F2}ms");
            Debug.Log($"   Max Frame Time: {maxFrameTime:F2}ms");
            Debug.Log($"   Frame Count: {frameTimes.Count}");
            Debug.Log($"   Budget Compliance: {(maxFrameTime <= frameBudgetMs ? "✅" : "❌")}");

            Assert.Less(averageFrameTime, frameBudgetMs, 
                $"Average frame time {averageFrameTime:F2}ms should be < {frameBudgetMs:F2}ms");
        }

        #endregion
    }
}