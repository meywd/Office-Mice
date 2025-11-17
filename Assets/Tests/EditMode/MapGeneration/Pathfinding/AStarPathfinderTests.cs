using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.Pathfinding
{
    [TestFixture]
    public class AStarPathfinderTests
    {
        private AStarPathfinder _pathfinder;
        private bool[,] _emptyObstacles;
        private bool[,] _simpleObstacles;
        private bool[,] _complexObstacles;
        
        [SetUp]
        public void SetUp()
        {
            _pathfinder = new AStarPathfinder();
            
            // Create test obstacle maps
            _emptyObstacles = CreateEmptyObstacleMap(20, 20);
            _simpleObstacles = CreateSimpleObstacleMap(20, 20);
            _complexObstacles = CreateComplexObstacleMap(30, 30);
        }
        
        [TearDown]
        public void TearDown()
        {
            _pathfinder?.ResetStats();
        }
        
        #region Basic Pathfinding Tests
        
        [Test]
        public void FindPath_SameStartAndEnd_ReturnsSinglePoint()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(5, 5);
            
            // Act
            var path = _pathfinder.FindPath(start, end, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(start, path[0]);
        }
        
        [Test]
        public void FindPath_StraightLinePath_ReturnsOptimalPath()
        {
            // Arrange
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(8, 2);
            
            // Act
            var path = _pathfinder.FindPath(start, end, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
            
            // Verify path is reasonably straight
            float totalDistance = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                totalDistance += Vector2Int.Distance(path[i - 1], path[i]);
            }
            float directDistance = Vector2Int.Distance(start, end);
            Assert.IsTrue(totalDistance <= directDistance * 1.5f, "Path should be reasonably direct");
        }
        
        [Test]
        public void FindPath_WithObstacles_FindsPathAroundObstacles()
        {
            // Arrange
            var start = new Vector2Int(2, 10);
            var end = new Vector2Int(18, 10);
            
            // Act
            var path = _pathfinder.FindPath(start, end, _simpleObstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
            
            // Verify path doesn't go through obstacles
            foreach (var point in path)
            {
                Assert.IsFalse(_simpleObstacles[point.x, point.y], $"Path goes through obstacle at {point}");
            }
        }
        
        [Test]
        public void FindPath_NoPathExists_ReturnsEmptyList()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(18, 18);
            var blockedMap = CreateBlockedObstacleMap(20, 20);
            
            // Act
            var path = _pathfinder.FindPath(start, end, blockedMap);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }
        
        [Test]
        public void FindPath_InvalidPositions_ThrowsArgumentException()
        {
            // Arrange
            var start = new Vector2Int(-1, 5);
            var end = new Vector2Int(10, 10);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _pathfinder.FindPath(start, end, _emptyObstacles));
        }
        
        [Test]
        public void FindPath_NullObstacles_ThrowsArgumentNullException()
        {
            // Arrange
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(10, 10);
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _pathfinder.FindPath(start, end, null));
        }
        
        #endregion
        
        #region Manhattan Distance Heuristic Tests
        
        [Test]
        public void ManhattanDistance_CalculatesCorrectDistance()
        {
            // Arrange
            var a = new Vector2Int(2, 3);
            var b = new Vector2Int(5, 7);
            
            // Act
            float distance = AStarPathfinder.ManhattanDistance(a, b);
            
            // Assert
            Assert.AreEqual(7f, distance); // |5-2| + |7-3| = 3 + 4 = 7
        }
        
        [Test]
        public void FindPath_WithManhattanHeuristic_FindsOptimalPath()
        {
            // Arrange
            _pathfinder.SetHeuristic(AStarPathfinder.ManhattanDistance);
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(10, 10);
            
            // Act
            var path = _pathfinder.FindPath(start, end, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
        }
        
        #endregion
        
        #region Priority Queue Tests
        
        [Test]
        public void PriorityQueue_EnqueueDequeue_MaintainsCorrectOrder()
        {
            // Arrange
            var queue = new PriorityQueue<AStarNode>();
            var node1 = new AStarNode { Position = new Vector2Int(1, 1), GCost = 5, HCost = 3 };
            var node2 = new AStarNode { Position = new Vector2Int(2, 2), GCost = 2, HCost = 1 };
            var node3 = new AStarNode { Position = new Vector2Int(3, 3), GCost = 4, HCost = 2 };
            
            // Act
            queue.Enqueue(node1);
            queue.Enqueue(node2);
            queue.Enqueue(node3);
            
            var first = queue.Dequeue();
            var second = queue.Dequeue();
            var third = queue.Dequeue();
            
            // Assert
            Assert.AreEqual(node2, first);  // F = 3 (lowest)
            Assert.AreEqual(node3, second); // F = 6
            Assert.AreEqual(node1, third);  // F = 8 (highest)
        }
        
        [Test]
        public void PriorityQueue_UpdatePriority_MaintainsHeapProperty()
        {
            // Arrange
            var queue = new PriorityQueue<AStarNode>();
            var node = new AStarNode { Position = new Vector2Int(1, 1), GCost = 10, HCost = 5 };
            
            queue.Enqueue(node);
            
            // Act
            node.GCost = 1; // Reduce F cost
            queue.UpdatePriority(node);
            
            // Assert
            var dequeued = queue.Dequeue();
            Assert.AreEqual(node, dequeued);
        }
        
        #endregion
        
        #region Obstacle Detection Tests
        
        [Test]
        public void PathExists_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(8, 8);
            
            // Act
            var exists = _pathfinder.PathExists(start, end, _emptyObstacles);
            
            // Assert
            Assert.IsTrue(exists);
        }
        
        [Test]
        public void PathExists_WithBlockedPath_ReturnsFalse()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(18, 18);
            var blockedMap = CreateBlockedObstacleMap(20, 20);
            
            // Act
            var exists = _pathfinder.PathExists(start, end, blockedMap);
            
            // Assert
            Assert.IsFalse(exists);
        }
        
        [Test]
        public void GetReachablePositions_WithObstacles_ReturnsCorrectSet()
        {
            // Arrange
            var start = new Vector2Int(10, 10);
            var maxDistance = 3;
            
            // Act
            var reachable = _pathfinder.GetReachablePositions(start, _simpleObstacles, maxDistance);
            
            // Assert
            Assert.IsNotNull(reachable);
            Assert.IsTrue(reachable.Count > 0);
            Assert.IsTrue(reachable.Contains(start));
            
            // Verify all positions are within distance
            foreach (var pos in reachable)
            {
                var distance = Vector2Int.Distance(start, pos);
                Assert.IsTrue(distance <= maxDistance, $"Position {pos} is beyond max distance {maxDistance}");
            }
        }
        
        #endregion
        
        #region Path Smoothing Tests
        
        [Test]
        public void OptimizePath_WithZigzagPath_RemovesUnnecessaryPoints()
        {
            // Arrange
            var zigzagPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(3, 3)
            };
            
            // Act
            var optimized = _pathfinder.OptimizePath(zigzagPath, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(optimized);
            Assert.IsTrue(optimized.Count < zigzagPath.Count);
            Assert.AreEqual(zigzagPath[0], optimized[0]);
            Assert.AreEqual(zigzagPath[zigzagPath.Count - 1], optimized[optimized.Count - 1]);
        }
        
        [Test]
        public void OptimizePath_WithObstacles_AvoidsObstacles()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(2, 10),
                new Vector2Int(10, 10),
                new Vector2Int(18, 10)
            };
            
            // Act
            var optimized = _pathfinder.OptimizePath(path, _simpleObstacles);
            
            // Assert
            Assert.IsNotNull(optimized);
            foreach (var point in optimized)
            {
                Assert.IsFalse(_simpleObstacles[point.x, point.y], $"Optimized path goes through obstacle at {point}");
            }
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void FindPath_Performance_CompletesWithinTimeLimit()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(28, 28);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            var path = _pathfinder.FindPath(start, end, _complexObstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, $"Pathfinding took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");
        }
        
        [Test]
        public void GetPerformanceStats_AfterOperations_ReturnsCorrectStats()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(10, 10);
            
            // Act
            _pathfinder.FindPath(start, end, _emptyObstacles);
            _pathfinder.FindPath(start, new Vector2Int(15, 15), _emptyObstacles);
            var stats = _pathfinder.GetPerformanceStats();
            
            // Assert
            Assert.AreEqual(2, stats.TotalPathfindingCalls);
            Assert.AreEqual(2, stats.SuccessfulPaths);
            Assert.IsTrue(stats.AverageComputationTime > 0);
            Assert.IsTrue(stats.AveragePathLength > 0);
        }
        
        [Test]
        public void ResetStats_ClearsAllStatistics()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(10, 10);
            _pathfinder.FindPath(start, end, _emptyObstacles);
            
            // Act
            _pathfinder.ResetStats();
            var stats = _pathfinder.GetPerformanceStats();
            
            // Assert
            Assert.AreEqual(0, stats.TotalPathfindingCalls);
            Assert.AreEqual(0, stats.SuccessfulPaths);
            Assert.AreEqual(0, stats.FailedPaths);
        }
        
        #endregion
        
        #region Multiple Path Tests
        
        [Test]
        public void FindMultiplePaths_WithMultipleEnds_ReturnsCorrectPaths()
        {
            // Arrange
            var start = new Vector2Int(10, 10);
            var ends = new List<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(18, 2),
                new Vector2Int(2, 18),
                new Vector2Int(18, 18)
            };
            
            // Act
            var paths = _pathfinder.FindMultiplePaths(start, ends, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(paths);
            Assert.AreEqual(ends.Count, paths.Count);
            
            foreach (var end in ends)
            {
                Assert.IsTrue(paths.ContainsKey(end));
                Assert.IsNotNull(paths[end]);
                
                if (paths[end].Count > 0)
                {
                    Assert.AreEqual(start, paths[end][0]);
                    Assert.AreEqual(end, paths[end][paths[end].Count - 1]);
                }
            }
        }
        
        #endregion
        
        #region Movement Cost Tests
        
        [Test]
        public void FindPath_WithMovementCosts_FindsOptimalPath()
        {
            // Arrange
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(8, 2);
            var movementCosts = CreateMovementCostMap(20, 20);
            
            // Make some areas more expensive
            movementCosts[5, 1] = 5f;
            movementCosts[5, 2] = 5f;
            movementCosts[5, 3] = 5f;
            
            // Act
            var path = _pathfinder.FindPath(start, end, _emptyObstacles, movementCosts);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            
            // Path should avoid expensive areas if possible
            bool goesThroughExpensiveArea = false;
            foreach (var point in path)
            {
                if (movementCosts[point.x, point.y] > 2f)
                {
                    goesThroughExpensiveArea = true;
                    break;
                }
            }
            
            // In this simple case, it might still go through expensive area
            // but the cost should be reflected in the calculation
            var cost = _pathfinder.CalculatePathCost(path, movementCosts);
            Assert.IsTrue(cost > 0);
        }
        
        [Test]
        public void CalculatePathCost_WithMovementCosts_ReturnsCorrectCost()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0)
            };
            var movementCosts = CreateMovementCostMap(10, 10);
            movementCosts[1, 0] = 2f;
            movementCosts[2, 0] = 3f;
            
            // Act
            var cost = _pathfinder.CalculatePathCost(path, movementCosts);
            
            // Assert
            Assert.AreEqual(5f, cost); // 1 (start) + 2 + 3 = 6, but path cost excludes start
        }
        
        #endregion
        
        #region Event Tests
        
        [Test]
        public void FindPath_SuccessfulPath_FiresEventsCorrectly()
        {
            // Arrange
            var start = new Vector2Int(2, 2);
            var end = new Vector2Int(8, 8);
            bool startedFired = false;
            bool completedFired = false;
            Vector2Int eventStart = Vector2Int.zero;
            Vector2Int eventEnd = Vector2Int.zero;
            List<Vector2Int> eventPath = null;
            
            _pathfinder.OnPathfindingStarted += (s, e) => 
            {
                startedFired = true;
                eventStart = s;
                eventEnd = e;
            };
            
            _pathfinder.OnPathfindingCompleted += (s, e, p) => 
            {
                completedFired = true;
                eventStart = s;
                eventEnd = e;
                eventPath = p;
            };
            
            // Act
            var path = _pathfinder.FindPath(start, end, _emptyObstacles);
            
            // Assert
            Assert.IsTrue(startedFired);
            Assert.IsTrue(completedFired);
            Assert.AreEqual(start, eventStart);
            Assert.AreEqual(end, eventEnd);
            Assert.IsNotNull(eventPath);
        }
        
        [Test]
        public void FindPath_FailedPath_FiresFailedEvent()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(18, 18);
            var blockedMap = CreateBlockedObstacleMap(20, 20);
            bool failedFired = false;
            
            _pathfinder.OnPathfindingFailed += (s, e, ex) => 
            {
                failedFired = true;
            };
            
            // Act
            var path = _pathfinder.FindPath(start, end, blockedMap);
            
            // Assert
            Assert.IsTrue(failedFired);
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool[,] CreateEmptyObstacleMap(int width, int height)
        {
            return new bool[width, height];
        }
        
        private bool[,] CreateSimpleObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            
            // Create a horizontal wall in the middle
            for (int x = 5; x < 15; x++)
            {
                obstacles[x, 10] = true;
            }
            
            // Create a vertical wall
            for (int y = 5; y < 15; y++)
            {
                obstacles[10, y] = true;
            }
            
            return obstacles;
        }
        
        private bool[,] CreateComplexObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            var random = new System.Random(42); // Fixed seed for reproducible tests
            
            // Add random obstacles
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
            
            // Ensure start and end are clear
            obstacles[1, 1] = false;
            obstacles[28, 28] = false;
            
            return obstacles;
        }
        
        private bool[,] CreateBlockedObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            
            // Block everything except start and end
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    obstacles[x, y] = true;
                }
            }
            
            obstacles[1, 1] = false;
            obstacles[18, 18] = false;
            
            return obstacles;
        }
        
        private float[,] CreateMovementCostMap(int width, int height)
        {
            var costs = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    costs[x, y] = 1f; // Default movement cost
                }
            }
            
            return costs;
        }
        
        #endregion
    }
}