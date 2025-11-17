using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Tests.Performance
{
    [TestFixture]
    public class AStarPerformanceTests
    {
        private AStarPathfinder _pathfinder;
        private const int PERFORMANCE_TARGET_MS = 50;
        private const int MEMORY_TARGET_MB = 10;
        private const int GC_PRESSURE_TARGET_KB = 10;
        
        [SetUp]
        public void SetUp()
        {
            _pathfinder = new AStarPathfinder();
        }
        
        [TearDown]
        public void TearDown()
        {
            _pathfinder?.ResetStats();
            PathfindingOptimizer.ClearCache();
            PathfindingOptimizer.ResetMetrics();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        
        #region Performance Target Tests
        
        [Test]
        public void FindPath_SmallMap_CompletesWithinPerformanceTarget()
        {
            // Arrange
            var mapSize = new Vector2Int(30, 30);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.2f);
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(27, 27);
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= PERFORMANCE_TARGET_MS, 
                $"Small map pathfinding took {stopwatch.ElapsedMilliseconds}ms, target is {PERFORMANCE_TARGET_MS}ms");
            
            var stats = _pathfinder.GetPerformanceStats();
            Assert.IsTrue(stats.AverageComputationTime <= PERFORMANCE_TARGET_MS, 
                $"Average computation time {stats.AverageComputationTime}ms exceeds target {PERFORMANCE_TARGET_MS}ms");
        }
        
        [Test]
        public void FindPath_MediumMap_CompletesWithinPerformanceTarget()
        {
            // Arrange
            var mapSize = new Vector2Int(75, 75);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.3f);
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(70, 70);
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= PERFORMANCE_TARGET_MS, 
                $"Medium map pathfinding took {stopwatch.ElapsedMilliseconds}ms, target is {PERFORMANCE_TARGET_MS}ms");
        }
        
        [Test]
        public void FindPath_LargeMap_CompletesWithinPerformanceTarget()
        {
            // Arrange
            var mapSize = new Vector2Int(150, 150);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.25f);
            var start = new Vector2Int(10, 10);
            var end = new Vector2Int(140, 140);
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= PERFORMANCE_TARGET_MS, 
                $"Large map pathfinding took {stopwatch.ElapsedMilliseconds}ms, target is {PERFORMANCE_TARGET_MS}ms");
        }
        
        #endregion
        
        #region Memory Usage Tests
        
        [Test]
        public void FindPath_MultipleOperations_UsesMemoryWithinTarget()
        {
            // Arrange
            var mapSize = new Vector2Int(100, 100);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.3f);
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(95, 95);
            
            // Measure initial memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(false);
            
            // Act - Perform multiple pathfinding operations
            for (int i = 0; i < 50; i++)
            {
                var path = _pathfinder.FindPath(start, end, obstacles);
                Assert.IsNotNull(path);
            }
            
            // Measure final memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long finalMemory = GC.GetTotalMemory(false);
            
            // Calculate memory usage
            long memoryUsed = (finalMemory - initialMemory) / (1024 * 1024); // Convert to MB
            
            // Assert
            Assert.IsTrue(memoryUsed <= MEMORY_TARGET_MB, 
                $"Memory usage {memoryUsed}MB exceeds target {MEMORY_TARGET_MB}MB");
        }
        
        [Test]
        public void FindPath_ObjectPooling_MinimizesGCPressure()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.2f);
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(45, 45);
            
            // Measure initial GC collections
            var initialGen0 = GC.CollectionCount(0);
            var initialGen1 = GC.CollectionCount(1);
            var initialGen2 = GC.CollectionCount(2);
            
            // Act - Perform many pathfinding operations
            for (int i = 0; i < 100; i++)
            {
                var path = _pathfinder.FindPath(start, end, obstacles);
                Assert.IsNotNull(path);
            }
            
            // Measure final GC collections
            var finalGen0 = GC.CollectionCount(0);
            var finalGen1 = GC.CollectionCount(1);
            var finalGen2 = GC.CollectionCount(2);
            
            // Calculate GC pressure
            int gen0Collections = finalGen0 - initialGen0;
            int gen1Collections = finalGen1 - initialGen1;
            int gen2Collections = finalGen2 - initialGen2;
            
            // Assert - Should minimize GC pressure
            Assert.IsTrue(gen0Collections <= 5, $"Too many Gen 0 collections: {gen0Collections}");
            Assert.IsTrue(gen1Collections <= 2, $"Too many Gen 1 collections: {gen1Collections}");
            Assert.IsTrue(gen2Collections <= 1, $"Too many Gen 2 collections: {gen2Collections}");
        }
        
        #endregion
        
        #region Scalability Tests
        
        [Test]
        public void FindPath_ScalabilityTest_PerformanceScalesLinearly()
        {
            // Arrange
            var mapSizes = new[]
            {
                new Vector2Int(25, 25),
                new Vector2Int(50, 50),
                new Vector2Int(100, 100),
                new Vector2Int(150, 150)
            };
            
            var performanceData = new List<(Vector2Int size, long timeMs)>();
            
            // Act - Test each map size
            foreach (var mapSize in mapSizes)
            {
                var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.25f);
                var start = new Vector2Int(2, 2);
                var end = new Vector2Int(mapSize.x - 3, mapSize.y - 3);
                
                var stopwatch = Stopwatch.StartNew();
                var path = _pathfinder.FindPath(start, end, obstacles);
                stopwatch.Stop();
                
                Assert.IsNotNull(path);
                performanceData.Add((mapSize, stopwatch.ElapsedMilliseconds));
                
                Debug.Log($"Map size {mapSize.x}x{mapSize.y}: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            // Assert - Performance should scale reasonably
            for (int i = 1; i < performanceData.Count; i++)
            {
                var prev = performanceData[i - 1];
                var curr = performanceData[i];
                
                double areaRatio = (curr.size.x * curr.size.y) / (double)(prev.size.x * prev.size.y);
                double timeRatio = curr.timeMs / (double)prev.timeMs;
                
                // Time should scale sub-quadratically
                Assert.IsTrue(timeRatio <= areaRatio * 1.5, 
                    $"Performance scaling too aggressive: area ratio {areaRatio:F2}, time ratio {timeRatio:F2}");
            }
        }
        
        [Test]
        public void FindMultiplePaths_ConcurrentRequests_HandlesEfficiently()
        {
            // Arrange
            var mapSize = new Vector2Int(75, 75);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.3f);
            var start = new Vector2Int(10, 10);
            var ends = new List<Vector2Int>();
            
            // Create multiple end points
            for (int i = 0; i < 10; i++)
            {
                ends.Add(new Vector2Int(60 + i, 60 + i));
            }
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            var paths = _pathfinder.FindMultiplePaths(start, ends, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(paths);
            Assert.AreEqual(ends.Count, paths.Count);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= PERFORMANCE_TARGET_MS * 2, 
                $"Multiple pathfinding took {stopwatch.ElapsedMilliseconds}ms, should be reasonable");
            
            // Verify all paths are valid
            foreach (var kvp in paths)
            {
                Assert.IsNotNull(kvp.Value);
                if (kvp.Value.Count > 0)
                {
                    Assert.AreEqual(start, kvp.Value[0]);
                    Assert.AreEqual(kvp.Key, kvp.Value[kvp.Value.Count - 1]);
                }
            }
        }
        
        #endregion
        
        #region Stress Tests
        
        [Test]
        public void FindPath_HighObstacleDensity_PerformsWell()
        {
            // Arrange
            var mapSize = new Vector2Int(100, 100);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.7f); // 70% obstacles
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(95, 95);
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= PERFORMANCE_TARGET_MS * 2, 
                $"High obstacle density pathfinding took {stopwatch.ElapsedMilliseconds}ms");
            
            // If path exists, verify it's valid
            if (path.Count > 0)
            {
                foreach (var point in path)
                {
                    Assert.IsFalse(obstacles[point.x, point.y], $"Path goes through obstacle at {point}");
                }
            }
        }
        
        [Test]
        public void FindPath_ContinuousLoad_MaintainsPerformance()
        {
            // Arrange
            var mapSize = new Vector2Int(75, 75);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.3f);
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(70, 70);
            
            var times = new List<long>();
            
            // Act - Perform continuous pathfinding
            for (int i = 0; i < 20; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var path = _pathfinder.FindPath(start, end, obstacles);
                stopwatch.Stop();
                
                Assert.IsNotNull(path);
                times.Add(stopwatch.ElapsedMilliseconds);
            }
            
            // Assert
            var averageTime = times.Average();
            var maxTime = times.Max();
            
            Assert.IsTrue(averageTime <= PERFORMANCE_TARGET_MS, 
                $"Average time {averageTime:F2}ms exceeds target {PERFORMANCE_TARGET_MS}ms");
            Assert.IsTrue(maxTime <= PERFORMANCE_TARGET_MS * 2, 
                $"Max time {maxTime}ms exceeds reasonable limit");
            
            // Performance should be stable (low variance)
            var variance = times.Select(t => Math.Pow(t - averageTime, 2)).Average();
            Assert.IsTrue(variance <= 100, $"Performance variance too high: {variance:F2}");
        }
        
        #endregion
        
        #region Optimization Tests
        
        [Test]
        public void PathfindingOptimizer_CachingSystem_ImprovesPerformance()
        {
            // Arrange
            var mapSize = new Vector2Int(100, 100);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.25f);
            var start = new Vector2Int(10, 10);
            var end = new Vector2Int(90, 90);
            
            // Calculate obstacles hash for caching
            int obstaclesHash = CalculateObstaclesHash(obstacles);
            
            // Act - First pathfinding (no cache)
            var stopwatch1 = Stopwatch.StartNew();
            var path1 = _pathfinder.FindPath(start, end, obstacles);
            stopwatch1.Stop();
            
            // Cache the path
            PathfindingOptimizer.CachePath(start, end, obstaclesHash, path1);
            
            // Second pathfinding (with cache)
            var stopwatch2 = Stopwatch.StartNew();
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, obstaclesHash);
            stopwatch2.Stop();
            
            // Assert
            Assert.IsNotNull(path1);
            Assert.IsNotNull(cachedPath);
            Assert.AreEqual(path1.Count, cachedPath.Count);
            
            // Cache lookup should be much faster
            Assert.IsTrue(stopwatch2.ElapsedMilliseconds < stopwatch1.ElapsedMilliseconds / 10, 
                "Cache lookup should be significantly faster");
            
            var cacheStats = PathfindingOptimizer.GetCacheStats();
            Assert.IsTrue(cacheStats.hits > 0, "Should have cache hits");
        }
        
        [Test]
        public void PathfindingOptimizer_ObjectPooling_ReducesMemoryAllocations()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var obstacles = CreateObstacleMap(mapSize.x, mapSize.y, 0.2f);
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(45, 45);
            
            // Measure initial pool stats
            var initialPoolStats = PathfindingOptimizer.GetPoolStats();
            
            // Act - Perform multiple pathfinding operations
            for (int i = 0; i < 50; i++)
            {
                var path = _pathfinder.FindPath(start, end, obstacles);
                Assert.IsNotNull(path);
            }
            
            // Measure final pool stats
            var finalPoolStats = PathfindingOptimizer.GetPoolStats();
            
            // Assert
            Assert.IsTrue(finalPoolStats.nodesCreated > initialPoolStats.nodesCreated, 
                "Should have created some nodes");
            
            // Pool should have grown to accommodate reuse
            Assert.IsTrue(finalPoolStats.poolSize >= initialPoolStats.poolSize, 
                "Pool should have grown or maintained size");
        }
        
        #endregion
        
        #region Regression Tests
        
        [Test]
        public void PerformanceRegression_ComparedToBaseline_MeetsTargets()
        {
            // Arrange - Baseline performance targets
            var baselineTargets = new
            {
                SmallMapTime = 10,    // ms
                MediumMapTime = 25,   // ms
                LargeMapTime = 45,    // ms
                MemoryUsage = 8,       // MB
                GCCollections = 3      // Max Gen 0 collections
            };
            
            // Act & Assert - Small map
            var smallMapTime = MeasurePathfindingTime(30, 30, 0.2f);
            Assert.IsTrue(smallMapTime <= baselineTargets.SmallMapTime, 
                $"Small map regression: {smallMapTime}ms > {baselineTargets.SmallMapTime}ms");
            
            // Medium map
            var mediumMapTime = MeasurePathfindingTime(75, 75, 0.3f);
            Assert.IsTrue(mediumMapTime <= baselineTargets.MediumMapTime, 
                $"Medium map regression: {mediumMapTime}ms > {baselineTargets.MediumMapTime}ms");
            
            // Large map
            var largeMapTime = MeasurePathfindingTime(150, 150, 0.25f);
            Assert.IsTrue(largeMapTime <= baselineTargets.LargeMapTime, 
                $"Large map regression: {largeMapTime}ms > {baselineTargets.LargeMapTime}ms");
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool[,] CreateObstacleMap(int width, int height, float obstacleDensity)
        {
            var obstacles = new bool[width, height];
            var random = new System.Random(42); // Fixed seed for reproducible tests
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextDouble() < obstacleDensity)
                    {
                        obstacles[x, y] = true;
                    }
                }
            }
            
            // Ensure start and end are clear
            obstacles[2, 2] = false;
            obstacles[width - 3, height - 3] = false;
            
            return obstacles;
        }
        
        private long MeasurePathfindingTime(int mapWidth, int mapHeight, float obstacleDensity)
        {
            var obstacles = CreateObstacleMap(mapWidth, mapHeight, obstacleDensity);
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(mapWidth - 3, mapHeight - 3);
            
            var stopwatch = Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            Assert.IsNotNull(path);
            return stopwatch.ElapsedMilliseconds;
        }
        
        private int CalculateObstaclesHash(bool[,] obstacles)
        {
            if (obstacles == null)
                return 0;
            
            int hash = 17;
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    hash = hash * 31 + (obstacles[x, y] ? 1 : 0);
                }
            }
            
            return hash;
        }
        
        #endregion
    }
}