using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;

namespace OfficeMice.MapGeneration.Tests.Pathfinding
{
    [TestFixture]
    public class PathfindingOptimizerTests
    {
        private bool[,] _testObstacles;
        private int _testObstaclesHash;
        
        [SetUp]
        public void SetUp()
        {
            _testObstacles = CreateTestObstacleMap(20, 20);
            _testObstaclesHash = CalculateObstaclesHash(_testObstacles);
            PathfindingOptimizer.ClearCache(); // Clear cache before each test
            PathfindingOptimizer.ResetMetrics();
        }
        
        [TearDown]
        public void TearDown()
        {
            PathfindingOptimizer.ClearCache();
            PathfindingOptimizer.ResetMetrics();
        }
        
        #region Caching Tests
        
        [Test]
        public void GetCachedPath_WithNonExistentPath_ReturnsNull()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            
            // Act
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, _testObstaclesHash);
            
            // Assert
            Assert.IsNull(cachedPath);
        }
        
        [Test]
        public void CachePath_WithValidPath_StoresPath()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            var path = new List<Vector2Int> { start, new Vector2Int(7, 7), end };
            
            // Act
            PathfindingOptimizer.CachePath(start, end, _testObstaclesHash, path);
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, _testObstaclesHash);
            
            // Assert
            Assert.IsNotNull(cachedPath);
            Assert.AreEqual(path.Count, cachedPath.Count);
            for (int i = 0; i < path.Count; i++)
            {
                Assert.AreEqual(path[i], cachedPath[i]);
            }
        }
        
        [Test]
        public void GetCachedPath_WithExistingPath_ReturnsCachedPath()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            var originalPath = new List<Vector2Int> { start, new Vector2Int(7, 7), end };
            PathfindingOptimizer.CachePath(start, end, _testObstaclesHash, originalPath);
            
            // Act
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, _testObstaclesHash);
            
            // Assert
            Assert.IsNotNull(cachedPath);
            Assert.AreEqual(originalPath.Count, cachedPath.Count);
            
            // Verify it's a copy, not the same reference
            Assert.AreNotSame(originalPath, cachedPath);
        }
        
        [Test]
        public void CachePath_WithEmptyPath_DoesNotCache()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            var emptyPath = new List<Vector2Int>();
            
            // Act
            PathfindingOptimizer.CachePath(start, end, _testObstaclesHash, emptyPath);
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, _testObstaclesHash);
            
            // Assert
            Assert.IsNull(cachedPath);
        }
        
        [Test]
        public void CachePath_WithNullPath_DoesNotCache()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            
            // Act
            PathfindingOptimizer.CachePath(start, end, _testObstaclesHash, null);
            var cachedPath = PathfindingOptimizer.GetCachedPath(start, end, _testObstaclesHash);
            
            // Assert
            Assert.IsNull(cachedPath);
        }
        
        [Test]
        public void ClearCache_WithCachedPaths_ClearsAllPaths()
        {
            // Arrange
            var start1 = new Vector2Int(5, 5);
            var end1 = new Vector2Int(10, 10);
            var start2 = new Vector2Int(2, 2);
            var end2 = new Vector2Int(8, 8);
            var path1 = new List<Vector2Int> { start1, end1 };
            var path2 = new List<Vector2Int> { start2, end2 };
            
            PathfindingOptimizer.CachePath(start1, end1, _testObstaclesHash, path1);
            PathfindingOptimizer.CachePath(start2, end2, _testObstaclesHash, path2);
            
            // Verify paths are cached
            Assert.IsNotNull(PathfindingOptimizer.GetCachedPath(start1, end1, _testObstaclesHash));
            Assert.IsNotNull(PathfindingOptimizer.GetCachedPath(start2, end2, _testObstaclesHash));
            
            // Act
            PathfindingOptimizer.ClearCache();
            
            // Assert
            Assert.IsNull(PathfindingOptimizer.GetCachedPath(start1, end1, _testObstaclesHash));
            Assert.IsNull(PathfindingOptimizer.GetCachedPath(start2, end2, _testObstaclesHash));
        }
        
        [Test]
        public void GetCacheStats_WithCachedPaths_ReturnsCorrectStats()
        {
            // Arrange
            var start1 = new Vector2Int(5, 5);
            var end1 = new Vector2Int(10, 10);
            var start2 = new Vector2Int(2, 2);
            var end2 = new Vector2Int(8, 8);
            var path1 = new List<Vector2Int> { start1, end1 };
            var path2 = new List<Vector2Int> { start2, end2 };
            
            PathfindingOptimizer.CachePath(start1, end1, _testObstaclesHash, path1);
            PathfindingOptimizer.CachePath(start2, end2, _testObstaclesHash, path2);
            
            // Act
            var (entries, hits, misses) = PathfindingOptimizer.GetCacheStats();
            
            // Assert
            Assert.AreEqual(2, entries);
            Assert.AreEqual(0, hits); // No hits yet
            Assert.AreEqual(0, misses); // No misses yet
            
            // Test hit/miss tracking
            PathfindingOptimizer.GetCachedPath(start1, end1, _testObstaclesHash); // Hit
            PathfindingOptimizer.GetCachedPath(new Vector2Int(1, 1), new Vector2Int(2, 2), _testObstaclesHash); // Miss
            
            var (entries2, hits2, misses2) = PathfindingOptimizer.GetCacheStats();
            Assert.AreEqual(2, entries2); // Same number of entries
            Assert.AreEqual(1, hits2); // One hit
            Assert.AreEqual(1, misses2); // One miss
        }
        
        #endregion
        
        #region Object Pooling Tests
        
        [Test]
        public void GetNodeFromPool_WithEmptyPool_CreatesNewNode()
        {
            // Arrange
            var initialStats = PathfindingOptimizer.GetPoolStats();
            
            // Act
            var node = PathfindingOptimizer.GetNodeFromPool();
            
            // Assert
            Assert.IsNotNull(node);
            var finalStats = PathfindingOptimizer.GetPoolStats();
            Assert.IsTrue(finalStats.nodesCreated > initialStats.nodesCreated);
        }
        
        [Test]
        public void ReturnNodeToPool_WithValidNode_ReturnsToPool()
        {
            // Arrange
            var node = PathfindingOptimizer.GetNodeFromPool();
            var initialStats = PathfindingOptimizer.GetPoolStats();
            
            // Act
            PathfindingOptimizer.ReturnNodeToPool(node);
            var finalStats = PathfindingOptimizer.GetPoolStats();
            
            // Assert
            Assert.IsTrue(finalStats.poolSize >= initialStats.poolSize);
        }
        
        [Test]
        public void ReturnNodeToPool_WithNullNode_DoesNotThrow()
        {
            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => PathfindingOptimizer.ReturnNodeToPool(null));
        }
        
        [Test]
        public void GetPoolStats_AfterOperations_ReturnsCorrectStats()
        {
            // Arrange
            var initialStats = PathfindingOptimizer.GetPoolStats();
            
            // Act
            var node1 = PathfindingOptimizer.GetNodeFromPool();
            var node2 = PathfindingOptimizer.GetNodeFromPool();
            PathfindingOptimizer.ReturnNodeToPool(node1);
            
            var finalStats = PathfindingOptimizer.GetPoolStats();
            
            // Assert
            Assert.IsTrue(finalStats.nodesCreated > initialStats.nodesCreated);
            Assert.IsTrue(finalStats.poolSize >= initialStats.poolSize);
        }
        
        #endregion
        
        #region Performance Monitoring Tests
        
        [Test]
        public void RecordOperation_WithValidData_UpdatesMetrics()
        {
            // Arrange
            var initialMetrics = PathfindingOptimizer.GetMetrics();
            
            // Act
            PathfindingOptimizer.RecordOperation(25.5f, 150, 12, true);
            var updatedMetrics = PathfindingOptimizer.GetMetrics();
            
            // Assert
            Assert.AreEqual(initialMetrics.TotalOperations + 1, updatedMetrics.TotalOperations);
            Assert.AreEqual(initialMetrics.SuccessfulOperations + 1, updatedMetrics.SuccessfulOperations);
            Assert.IsTrue(updatedMetrics.AverageDuration > 0);
            Assert.IsTrue(updatedMetrics.AverageNodesExplored > 0);
            Assert.IsTrue(updatedMetrics.AveragePathLength > 0);
        }
        
        [Test]
        public void RecordOperation_WithFailedOperation_UpdatesFailureCount()
        {
            // Arrange
            var initialMetrics = PathfindingOptimizer.GetMetrics();
            
            // Act
            PathfindingOptimizer.RecordOperation(15.0f, 50, 0, false);
            var updatedMetrics = PathfindingOptimizer.GetMetrics();
            
            // Assert
            Assert.AreEqual(initialMetrics.TotalOperations + 1, updatedMetrics.TotalOperations);
            Assert.AreEqual(initialMetrics.SuccessfulOperations, updatedMetrics.SuccessfulOperations); // No increase
        }
        
        [Test]
        public void GetMetrics_AfterOperations_ReturnsCorrectMetrics()
        {
            // Arrange
            PathfindingOptimizer.RecordOperation(10.0f, 100, 8, true);
            PathfindingOptimizer.RecordOperation(20.0f, 200, 15, true);
            PathfindingOptimizer.RecordOperation(5.0f, 50, 0, false);
            
            // Act
            var metrics = PathfindingOptimizer.GetMetrics();
            
            // Assert
            Assert.AreEqual(3, metrics.TotalOperations);
            Assert.AreEqual(2, metrics.SuccessfulOperations);
            Assert.IsTrue(metrics.AverageDuration > 0);
            Assert.IsTrue(metrics.AverageNodesExplored > 0);
            Assert.IsTrue(metrics.AveragePathLength > 0);
        }
        
        [Test]
        public void ResetMetrics_ClearsAllStatistics()
        {
            // Arrange
            PathfindingOptimizer.RecordOperation(10.0f, 100, 8, true);
            PathfindingOptimizer.RecordOperation(20.0f, 200, 15, true);
            
            // Act
            PathfindingOptimizer.ResetMetrics();
            var metrics = PathfindingOptimizer.GetMetrics();
            
            // Assert
            Assert.AreEqual(0, metrics.TotalOperations);
            Assert.AreEqual(0, metrics.SuccessfulOperations);
            Assert.AreEqual(0, metrics.AverageDuration);
            Assert.AreEqual(0, metrics.AverageNodesExplored);
            Assert.AreEqual(0, metrics.AveragePathLength);
        }
        
        [Test]
        public void GetMetrics_Clone_ReturnsIndependentCopy()
        {
            // Arrange
            PathfindingOptimizer.RecordOperation(10.0f, 100, 8, true);
            var metrics1 = PathfindingOptimizer.GetMetrics();
            
            // Act
            var metrics2 = PathfindingOptimizer.GetMetrics();
            
            // Assert
            Assert.AreEqual(metrics1.TotalOperations, metrics2.TotalOperations);
            Assert.AreEqual(metrics1.SuccessfulOperations, metrics2.SuccessfulOperations);
            Assert.AreEqual(metrics1.AverageDuration, metrics2.AverageDuration);
            
            // Verify they're independent objects
            Assert.AreNotSame(metrics1, metrics2);
        }
        
        #endregion
        
        #region Map Analysis Tests
        
        [Test]
        public void AnalyzeMap_WithEmptyMap_ReturnsDefaultSettings()
        {
            // Arrange
            var emptyMap = new bool[10, 10];
            
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(emptyMap);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(0f, settings.ObstacleDensity);
            Assert.IsFalse(settings.UseEarlyTermination);
            Assert.IsFalse(settings.UseHierarchicalPathfinding);
            Assert.IsFalse(settings.UseBidirectionalSearch);
            Assert.IsFalse(settings.UseJumpPointSearch);
        }
        
        [Test]
        public void AnalyzeMap_WithHighObstacleDensity_ReturnsOptimizedSettings()
        {
            // Arrange
            var denseMap = CreateDenseObstacleMap(20, 20, 0.8f); // 80% obstacles
            
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(denseMap);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.ObstacleDensity > 0.7f);
            Assert.IsTrue(settings.UseEarlyTermination);
            Assert.IsTrue(settings.UseBidirectionalSearch);
        }
        
        [Test]
        public void AnalyzeMap_WithLowObstacleDensity_ReturnsOptimizedSettings()
        {
            // Arrange
            var sparseMap = CreateDenseObstacleMap(20, 20, 0.1f); // 10% obstacles
            
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(sparseMap);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.ObstacleDensity < 0.3f);
            Assert.IsTrue(settings.UseJumpPointSearch);
        }
        
        [Test]
        public void AnalyzeMap_WithLargeMap_ReturnsHierarchicalSettings()
        {
            // Arrange
            var largeMap = new bool[150, 150]; // Large map
            
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(largeMap);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.UseHierarchicalPathfinding);
            Assert.IsTrue(settings.UseEarlyTermination);
        }
        
        [Test]
        public void AnalyzeMap_WithSmallMap_ReturnsBasicSettings()
        {
            // Arrange
            var smallMap = new bool[20, 20]; // Small map
            
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(smallMap);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.IsFalse(settings.UseHierarchicalPathfinding);
            Assert.IsFalse(settings.UseEarlyTermination);
        }
        
        [Test]
        public void AnalyzeMap_WithNullMap_ReturnsDefaultSettings()
        {
            // Act
            var settings = PathfindingOptimizer.AnalyzeMap(null);
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(0f, settings.ObstacleDensity);
        }
        
        #endregion
        
        #region Hierarchical Pathfinding Tests
        
        [Test]
        public void FindHierarchicalPath_WithSmallMap_UsesRegularPathfinding()
        {
            // Arrange
            var smallMap = new bool[15, 15]; // Small map
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(12, 12);
            
            // Act
            var path = PathfindingOptimizer.FindHierarchicalPath(start, end, smallMap);
            
            // Assert
            Assert.IsNotNull(path);
            if (path.Count > 0)
            {
                Assert.AreEqual(start, path[0]);
                Assert.AreEqual(end, path[path.Count - 1]);
            }
        }
        
        [Test]
        public void FindHierarchicalPath_WithLargeMap_UsesHierarchicalApproach()
        {
            // Arrange
            var largeMap = new bool[50, 50]; // Large map
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(45, 45);
            
            // Act
            var path = PathfindingOptimizer.FindHierarchicalPath(start, end, largeMap);
            
            // Assert
            Assert.IsNotNull(path);
            if (path.Count > 0)
            {
                Assert.AreEqual(start, path[0]);
                Assert.AreEqual(end, path[path.Count - 1]);
            }
        }
        
        [Test]
        public void FindHierarchicalPath_WithNullMap_ReturnsEmptyPath()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            
            // Act
            var path = PathfindingOptimizer.FindHierarchicalPath(start, end, null);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }
        
        #endregion
        
        #region Bidirectional Search Tests
        
        [Test]
        public void FindBidirectionalPath_WithValidMap_ReturnsPath()
        {
            // Arrange
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(17, 17);
            
            // Act
            var path = PathfindingOptimizer.FindBidirectionalPath(start, end, _testObstacles);
            
            // Assert
            Assert.IsNotNull(path);
            if (path.Count > 0)
            {
                Assert.AreEqual(start, path[0]);
                Assert.AreEqual(end, path[path.Count - 1]);
            }
        }
        
        [Test]
        public void FindBidirectionalPath_WithNoPath_ReturnsEmptyPath()
        {
            // Arrange
            var blockedMap = CreateBlockedObstacleMap(20, 20);
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(18, 18);
            
            // Act
            var path = PathfindingOptimizer.FindBidirectionalPath(start, end, blockedMap);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }
        
        #endregion
        
        #region Edge Cases Tests
        
        [Test]
        public void AllMethods_WithNullParameters_HandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => PathfindingOptimizer.GetCachedPath(Vector2Int.zero, Vector2Int.one, 0));
            Assert.DoesNotThrow(() => PathfindingOptimizer.CachePath(Vector2Int.zero, Vector2Int.one, 0, null));
            Assert.DoesNotThrow(() => PathfindingOptimizer.ClearCache());
            Assert.DoesNotThrow(() => PathfindingOptimizer.GetCacheStats());
            Assert.DoesNotThrow(() => PathfindingOptimizer.GetNodeFromPool());
            Assert.DoesNotThrow(() => PathfindingOptimizer.ReturnNodeToPool(null));
            Assert.DoesNotThrow(() => PathfindingOptimizer.GetPoolStats());
            Assert.DoesNotThrow(() => PathfindingOptimizer.RecordOperation(0f, 0, 0, false));
            Assert.DoesNotThrow(() => PathfindingOptimizer.GetMetrics());
            Assert.DoesNotThrow(() => PathfindingOptimizer.ResetMetrics());
            Assert.DoesNotThrow(() => PathfindingOptimizer.AnalyzeMap(null));
            Assert.DoesNotThrow(() => PathfindingOptimizer.FindHierarchicalPath(Vector2Int.zero, Vector2Int.one, null));
            Assert.DoesNotThrow(() => PathfindingOptimizer.FindBidirectionalPath(Vector2Int.zero, Vector2Int.one, null));
        }
        
        [Test]
        public void PerformanceMetrics_Clone_ReturnsIndependentCopy()
        {
            // Arrange
            PathfindingOptimizer.RecordOperation(10.0f, 100, 8, true);
            var original = PathfindingOptimizer.GetMetrics();
            
            // Act
            var clone = original.Clone();
            
            // Assert
            Assert.AreEqual(original.TotalOperations, clone.TotalOperations);
            Assert.AreEqual(original.SuccessfulOperations, clone.SuccessfulOperations);
            Assert.AreEqual(original.AverageDuration, clone.AverageDuration);
            Assert.AreNotSame(original, clone);
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool[,] CreateTestObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            var random = new System.Random(42); // Fixed seed for reproducible tests
            
            // Add some random obstacles
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextDouble() < 0.2) // 20% obstacle density
                    {
                        obstacles[x, y] = true;
                    }
                }
            }
            
            return obstacles;
        }
        
        private bool[,] CreateDenseObstacleMap(int width, int height, float density)
        {
            var obstacles = new bool[width, height];
            var random = new System.Random(42);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextDouble() < density)
                    {
                        obstacles[x, y] = true;
                    }
                }
            }
            
            return obstacles;
        }
        
        private bool[,] CreateBlockedObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            
            // Block everything
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    obstacles[x, y] = true;
                }
            }
            
            // Leave start and end clear
            obstacles[1, 1] = false;
            obstacles[18, 18] = false;
            
            return obstacles;
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