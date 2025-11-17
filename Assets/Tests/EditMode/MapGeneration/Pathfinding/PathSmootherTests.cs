using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;

namespace OfficeMice.MapGeneration.Tests.Pathfinding
{
    [TestFixture]
    public class PathSmootherTests
    {
        private bool[,] _emptyObstacles;
        private bool[,] _simpleObstacles;
        
        [SetUp]
        public void SetUp()
        {
            _emptyObstacles = CreateEmptyObstacleMap(20, 20);
            _simpleObstacles = CreateSimpleObstacleMap(20, 20);
        }
        
        #region Line of Sight Smoothing Tests
        
        [Test]
        public void SmoothPathLineOfSight_WithZigzagPath_RemovesUnnecessaryPoints()
        {
            // Arrange
            var zigzagPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new VectorInt(3, 0),
                new VectorInt(3, 1),
                new VectorInt(3, 2),
                new VectorInt(3, 3),
                new VectorInt(3, 4),
                new VectorInt(4, 4),
                new VectorInt(5, 4)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathLineOfSight(zigzagPath, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.IsTrue(smoothed.Count < zigzagPath.Count);
            Assert.AreEqual(zigzagPath[0], smoothed[0]);
            Assert.AreEqual(zigzagPath[zigzagPath.Count - 1], smoothed[smoothed.Count - 1]);
            
            // Verify path is still valid (no obstacles)
            foreach (var point in smoothed)
            {
                Assert.IsTrue(IsValidPosition(point, _emptyObstacles));
            }
        }
        
        [Test]
        public void SmoothPathLineOfSight_WithObstacles_RespectsObstacles()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(2, 10),
                new Vector2Int(10, 10),
                new Vector2Int(18, 10)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathLineOfSight(path, _simpleObstacles);
            
            // Assert
            Assert.IsNotNull(smoothed);
            foreach (var point in smoothed)
            {
                Assert.IsFalse(_simpleObstacles[point.x, point.y], $"Smoothed path goes through obstacle at {point}");
            }
        }
        
        [Test]
        public void SmoothPathLineOfSight_WithEmptyPath_ReturnsEmptyList()
        {
            // Arrange
            var emptyPath = new List<Vector2Int>();
            
            // Act
            var smoothed = PathSmoother.SmoothPathLineOfSight(emptyPath, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.AreEqual(0, smoothed.Count);
        }
        
        [Test]
        public void SmoothPathLineOfSight_WithSinglePoint_ReturnsSinglePoint()
        {
            // Arrange
            var singlePointPath = new List<Vector2Int> { new Vector2Int(5, 5) };
            
            // Act
            var smoothed = PathSmoother.SmoothPathLineOfSight(singlePointPath, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.AreEqual(1, smoothed.Count);
            Assert.AreEqual(singlePointPath[0], smoothed[0]);
        }
        
        [Test]
        public void SmoothPathLineOfSight_WithNullObstacles_ThrowsArgumentNullException()
        {
            // Arrange
            var path = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(5, 5) };
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PathSmoother.SmoothPathLineOfSight(path, null));
        }
        
        #endregion
        
        #region Spline Smoothing Tests
        
        [Test]
        public void SmoothPathSpline_WithStraightPath_CreatesSmoothCurve()
        {
            // Arrange
            var straightPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0),
                new Vector2Int(10, 0),
                new Vector2Int(15, 0)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathSpline(straightPath, _emptyObstacles, 2);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.IsTrue(smoothed.Count >= straightPath.Count);
            Assert.AreEqual(straightPath[0], smoothed[0]);
            Assert.AreEqual(straightPath[straightPath.Count - 1], smoothed[smoothed.Count - 1]);
            
            // Verify path is still valid
            foreach (var point in smoothed)
            {
                Assert.IsTrue(IsValidPosition(point, _emptyObstacles));
            }
        }
        
        [Test]
        public void SmoothPathSpline_WithLShapedPath_CreatesSmoothCorner()
        {
            // Arrange
            var lShapedPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0),
                new Vector2Int(5, 5),
                new Vector2Int(10, 5)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathSpline(lShapedPath, _emptyObstacles, 3);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.IsTrue(smoothed.Count >= lShapedPath.Count);
            Assert.AreEqual(lShapedPath[0], smoothed[0]);
            Assert.AreEqual(lShapedPath[lShapedPath.Count - 1], smoothed[smoothed.Count - 1]);
            
            // Verify path is still valid
            foreach (var point in smoothed)
            {
                Assert.IsTrue(IsValidPosition(point, _emptyObstacles));
            }
        }
        
        #endregion
        
        #region Angular Smoothing Tests
        
        [Test]
        public void SmoothPathAngles_WithSharpTurns_ReducesAngles()
        {
            // Arrange
            var sharpTurnPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0),
                new VectorInt(5, 5),
                new VectorInt(0, 5),
                new VectorInt(0, 10)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathAngles(sharpTurnPath, _emptyObstacles, 90f);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.AreEqual(sharpTurnPath[0], smoothed[0]);
            Assert.AreEqual(sharpTurnPath[sharpTurnPath.Count - 1], smoothed[smoothed.Count - 1]);
            
            // Calculate smoothness improvement
            float originalSmoothness = PathSmoother.CalculatePathSmoothness(sharpTurnPath);
            float smoothedSmoothness = PathSmoother.CalculatePathSmoothness(smoothed);
            Assert.IsTrue(smoothedSmoothness <= originalSmoothness, "Smoothed path should have equal or better smoothness");
        }
        
        [Test]
        public void SmoothPathAngles_WithGentleTurns_KeepsPathSimilar()
        {
            // Arrange
            var gentlePath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(3, 1),
                new VectorInt(6, 2),
                new VectorInt(9, 3)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathAngles(gentlePath, _emptyObstacles, 45f);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.AreEqual(gentlePath[0], smoothed[0]);
            Assert.AreEqual(gentlePath[gentlePath.Count - 1], smoothed[smoothed.Count - 1]);
        }
        
        #endregion
        
        #region Weighted Smoothing Tests
        
        [Test]
        public void SmoothPathWeighted_WithLowSmoothness_UsesLineOfSight()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(2, 0),
                new VectorInt(4, 0),
                new VectorInt(6, 0),
                new VectorInt(8, 0),
                new VectorInt(8, 2),
                new VectorInt(8, 4),
                new VectorInt(8, 6)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathWeighted(path, _emptyObstacles, 0.2f);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.IsTrue(smoothed.Count <= path.Count); // Should be shorter due to line-of-sight
        }
        
        [Test]
        public void SmoothPathWeighted_WithHighSmoothness_UsesMultipleTechniques()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(2, 0),
                new VectorInt(4, 0),
                new VectorInt(6, 0),
                new VectorInt(8, 0),
                new VectorInt(8, 2),
                new VectorInt(8, 4),
                new VectorInt(8, 6)
            };
            
            // Act
            var smoothed = PathSmoother.SmoothPathWeighted(path, _emptyObstacles, 0.8f);
            
            // Assert
            Assert.IsNotNull(smoothed);
            Assert.AreEqual(path[0], smoothed[0]);
            Assert.AreEqual(path[path.Count - 1], smoothed[smoothed.Count - 1]);
        }
        
        [Test]
        public void SmoothPathWeighted_WithClampedSmoothness_HandlesBoundaryValues()
        {
            // Arrange
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0),
                new VectorInt(10, 0)
            };
            
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathWeighted(path, _emptyObstacles, -0.5f));
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathWeighted(path, _emptyObstacles, 1.5f));
        }
        
        #endregion
        
        #region Path Optimization Tests
        
        [Test]
        public void OptimizePath_WithRedundantPoints_RemovesRedundantPoints()
        {
            // Arrange
            var redundantPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2),
                new Vector2Int(0, 3),
                new Vector2Int(0, 4),
                new Vector2Int(0, 5)
            };
            
            // Act
            var optimized = PathSmoother.OptimizePath(redundantPath, 2f);
            
            // Assert
            Assert.IsNotNull(optimized);
            Assert.IsTrue(optimized.Count < redundantPath.Count);
            Assert.AreEqual(redundantPath[0], optimized[0]);
            Assert.AreEqual(redundantPath[redundantPath.Count - 1], optimized[optimized.Count - 1]);
        }
        
        [Test]
        public void EnsurePathContinuity_WithGappedPath_FillsGaps()
        {
            // Arrange
            var gappedPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0), // Gap
                new Vector2Int(10, 0) // Gap
            };
            
            // Act
            var continuous = PathSmoother.EnsurePathContinuity(gappedPath, _emptyObstacles);
            
            // Assert
            Assert.IsNotNull(continuous);
            Assert.IsTrue(continuous.Count >= gappedPath.Count);
            Assert.AreEqual(gappedPath[0], continuous[0]);
            Assert.AreEqual(gappedPath[gappedPath.Count - 1], continuous[continuous.Count - 1]);
            
            // Verify continuity
            for (int i = 1; i < continuous.Count; i++)
            {
                var distance = Vector2Int.Distance(continuous[i - 1], continuous[i]);
                Assert.IsTrue(distance <= 1.5f, $"Gap between {continuous[i - 1]} and {continuous[i]} is too large");
            }
        }
        
        #endregion
        
        #region Utility Tests
        
        [Test]
        public void CalculatePathSmoothness_WithStraightPath_ReturnsLowValue()
        {
            // Arrange
            var straightPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0)
            };
            
            // Act
            var smoothness = PathSmoother.CalculatePathSmoothness(straightPath);
            
            // Assert
            Assert.IsTrue(smoothness < 0.1f, "Straight path should have very low smoothness value");
        }
        
        [Test]
        public void CalculatePathSmoothness_WithZigzagPath_ReturnsHighValue()
        {
            // Arrange
            var zigzagPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2)
            };
            
            // Act
            var smoothness = PathSmoother.CalculatePathSmoothness(zigzagPath);
            
            // Assert
            Assert.IsTrue(smoothness > 0.5f, "Zigzag path should have high smoothness value");
        }
        
        [Test]
        public void CalculatePathEfficiency_WithOptimizedPath_ReturnsGoodEfficiency()
        {
            // Arrange
            var originalPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
                new Vector2Int(4, 0)
            };
            
            var optimizedPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(4, 0)
            };
            
            // Act
            var efficiency = PathSmoother.CalculatePathEfficiency(originalPath, optimizedPath);
            
            // Assert
            Assert.IsTrue(efficiency <= 1.0f, "Optimized path should be equal or shorter");
            Assert.IsTrue(efficiency > 0f, "Efficiency should be positive");
        }
        
        [Test]
        public void CalculatePathEfficiency_WithNullPaths_ReturnsZero()
        {
            // Arrange
            var validPath = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(5, 5) };
            
            // Act
            var efficiency1 = PathSmoother.CalculatePathEfficiency(null, validPath);
            var efficiency2 = PathSmoother.CalculatePathEfficiency(validPath, null);
            var efficiency3 = PathSmoother.CalculatePathEfficiency(null, null);
            
            // Assert
            Assert.AreEqual(0f, efficiency1);
            Assert.AreEqual(0f, efficiency2);
            Assert.AreEqual(0f, efficiency3);
        }
        
        #endregion
        
        #region Edge Cases Tests
        
        [Test]
        public void AllSmoothingMethods_WithNullPath_HandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathLineOfSight(null, _emptyObstacles));
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathSpline(null, _emptyObstacles));
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathAngles(null, _emptyObstacles));
            Assert.DoesNotThrow(() => PathSmoother.SmoothPathWeighted(null, _emptyObstacles));
            Assert.DoesNotThrow(() => PathSmoother.OptimizePath(null));
            Assert.DoesNotThrow(() => PathSmoother.EnsurePathContinuity(null, _emptyObstacles));
        }
        
        [Test]
        public void CalculatePathSmoothness_WithNullPath_ReturnsZero()
        {
            // Act
            var smoothness = PathSmoother.CalculatePathSmoothness(null);
            
            // Assert
            Assert.AreEqual(0f, smoothness);
        }
        
        [Test]
        public void CalculatePathSmoothness_WithShortPath_ReturnsZero()
        {
            // Arrange
            var shortPath = new List<Vector2Int> { new Vector2Int(0, 0) };
            
            // Act
            var smoothness = PathSmoother.CalculatePathSmoothness(shortPath);
            
            // Assert
            Assert.AreEqual(0f, smoothness);
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
            
            // Create a horizontal wall
            for (int x = 3; x < 8; x++)
            {
                obstacles[x, 10] = true;
            }
            
            return obstacles;
        }
        
        private bool IsValidPosition(Vector2Int position, bool[,] obstacles)
        {
            if (obstacles == null)
                return false;
            
            return position.x >= 0 && position.x < obstacles.GetLength(0) && 
                   position.y >= 0 && position.y < obstacles.GetLength(1) && 
                   !obstacles[position.x, position.y];
        }
        
        #endregion
    }
}