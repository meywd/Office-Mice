using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Pathfinding
{
    [TestFixture]
    public class CorridorWidthValidatorTests
    {
        private List<RoomData> _testRooms;
        private List<CorridorData> _testCorridors;
        private bool[,] _testObstacles;
        
        [SetUp]
        public void SetUp()
        {
            _testRooms = CreateTestRooms();
            _testCorridors = CreateTestCorridors();
            _testObstacles = CreateTestObstacleMap();
        }
        
        #region Width Validation Tests
        
        [Test]
        public void ValidateCorridorWidth_WithValidWidth_ReturnsSuccess()
        {
            // Arrange
            int validWidth = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(validWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }
        
        [Test]
        public void ValidateCorridorWidth_WithMinimumWidth_ReturnsSuccess()
        {
            // Arrange
            int minWidth = CorridorWidthValidator.MIN_CORRIDOR_WIDTH;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(minWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateCorridorWidth_WithMaximumWidth_ReturnsSuccess()
        {
            // Arrange
            int maxWidth = CorridorWidthValidator.MAX_CORRIDOR_WIDTH;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(maxWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateCorridorWidth_WithBelowMinimumWidth_ReturnsError()
        {
            // Arrange
            int invalidWidth = CorridorWidthValidator.MIN_CORRIDOR_WIDTH - 1;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(invalidWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors[0].Contains("below minimum"));
        }
        
        [Test]
        public void ValidateCorridorWidth_WithAboveMaximumWidth_ReturnsError()
        {
            // Arrange
            int invalidWidth = CorridorWidthValidator.MAX_CORRIDOR_WIDTH + 1;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(invalidWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors[0].Contains("exceeds maximum"));
        }
        
        [Test]
        public void ValidateCorridorWidth_WithBelowRecommendedWidth_ReturnsWarning()
        {
            // Arrange
            int narrowWidth = CorridorWidthValidator.RECOMMENDED_MIN_WIDTH - 1;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorWidth(narrowWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid); // Still valid, but with warning
            Assert.IsTrue(result.Warnings.Count > 0);
            Assert.IsTrue(result.Warnings[0].Contains("below recommended"));
        }
        
        #endregion
        
        #region Path Validation Tests
        
        [Test]
        public void ValidateCorridorPath_WithValidPath_ReturnsSuccess()
        {
            // Arrange
            var validPath = new List<Vector2Int>
            {
                new Vector2Int(5, 5),
                new Vector2Int(6, 5),
                new Vector2Int(7, 5),
                new Vector2Int(8, 5)
            };
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(validPath, width, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateCorridorPath_WithNullPath_ReturnsError()
        {
            // Arrange
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(null, width, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorPath_WithEmptyPath_ReturnsError()
        {
            // Arrange
            var emptyPath = new List<Vector2Int>();
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(emptyPath, width, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorPath_WithNullObstacles_ReturnsError()
        {
            // Arrange
            var path = new List<Vector2Int> { new Vector2Int(5, 5) };
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(path, width, null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorPath_WithPathHittingObstacles_ReturnsError()
        {
            // Arrange
            var obstaclePath = new List<Vector2Int>
            {
                new Vector2Int(10, 10), // This hits an obstacle in our test map
                new Vector2Int(11, 10)
            };
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(obstaclePath, width, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorPath_WithPathExceedingBoundaries_ReturnsError()
        {
            // Arrange
            var outOfBoundsPath = new List<Vector2Int>
            {
                new Vector2Int(-1, 5), // Out of bounds
                new Vector2Int(0, 5)
            };
            int width = 3;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorPath(outOfBoundsPath, width, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        #endregion
        
        #region Corridor Data Validation Tests
        
        [Test]
        public void ValidateCorridorData_WithValidCorridor_ReturnsSuccess()
        {
            // Arrange
            var corridor = _testCorridors[0];
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorData(corridor, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateCorridorData_WithNullCorridor_ReturnsError()
        {
            // Act
            var result = CorridorWidthValidator.ValidateCorridorData(null, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorData_WithEmptyPath_ReturnsError()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, new Vector2Int(5, 5), new Vector2Int(10, 10), 3);
            // Don't set path, so it's empty
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorData(corridor, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorData_WithMismatchedStartEnd_ReturnsWarning()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, new Vector2Int(5, 5), new Vector2Int(10, 10), 3);
            var path = new List<Vector2Int>
            {
                new Vector2Int(6, 6), // Different from start
                new Vector2Int(7, 7),
                new Vector2Int(8, 8)  // Different from end
            };
            corridor.SetPath(path);
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorData(corridor, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            // Should still be valid but with warnings
            Assert.IsTrue(result.Warnings.Count > 0);
        }
        
        #endregion
        
        #region Corridor Collection Validation Tests
        
        [Test]
        public void ValidateCorridorCollection_WithValidCollection_ReturnsSuccess()
        {
            // Arrange
            bool allowVariableWidth = false;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorCollection(_testCorridors, _testObstacles, allowVariableWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateCorridorCollection_WithNullCollection_ReturnsError()
        {
            // Act
            var result = CorridorWidthValidator.ValidateCorridorCollection(null, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorCollection_WithEmptyCollection_ReturnsWarning()
        {
            // Arrange
            var emptyCollection = new List<CorridorData>();
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorCollection(emptyCollection, _testObstacles);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid); // Valid but with warning
            Assert.IsTrue(result.Warnings.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorCollection_WithVariableWidthsAndDisallowed_ReturnsWarning()
        {
            // Arrange
            var variableWidthCorridors = new List<CorridorData>
            {
                new CorridorData(1, 2, new Vector2Int(5, 5), new Vector2Int(10, 10), 3),
                new CorridorData(3, 4, new Vector2Int(15, 5), new Vector2Int(20, 10), 5) // Different width
            };
            bool allowVariableWidth = false;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorCollection(variableWidthCorridors, _testObstacles, allowVariableWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid); // Valid but with warning
            Assert.IsTrue(result.Warnings.Count > 0);
        }
        
        [Test]
        public void ValidateCorridorCollection_WithVariableWidthsAndAllowed_ReturnsSuccess()
        {
            // Arrange
            var variableWidthCorridors = new List<CorridorData>
            {
                new CorridorData(1, 2, new Vector2Int(5, 5), new Vector2Int(10, 10), 3),
                new CorridorData(3, 4, new Vector2Int(15, 5), new Vector2Int(20, 10), 5) // Different width
            };
            bool allowVariableWidth = true;
            
            // Act
            var result = CorridorWidthValidator.ValidateCorridorCollection(variableWidthCorridors, _testObstacles, allowVariableWidth);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Warnings.Count); // No warnings about width inconsistency
        }
        
        #endregion
        
        #region Width Calculation Tests
        
        [Test]
        public void CalculateOptimalCorridorWidth_WithSmallMap_ReturnsAdjustedWidth()
        {
            // Arrange
            var smallMapSize = new Vector2Int(30, 30); // 900 tiles
            int preferredWidth = 4;
            
            // Act
            var optimalWidth = CorridorWidthValidator.CalculateOptimalCorridorWidth(_testRooms, smallMapSize, preferredWidth);
            
            // Assert
            Assert.IsTrue(optimalWidth <= 3, "Small map should have reduced corridor width");
            Assert.IsTrue(optimalWidth >= CorridorWidthValidator.MIN_CORRIDOR_WIDTH);
        }
        
        [Test]
        public void CalculateOptimalCorridorWidth_WithLargeMap_ReturnsAdjustedWidth()
        {
            // Arrange
            var largeMapSize = new Vector2Int(150, 150); // 22500 tiles
            int preferredWidth = 3;
            
            // Act
            var optimalWidth = CorridorWidthValidator.CalculateOptimalCorridorWidth(_testRooms, largeMapSize, preferredWidth);
            
            // Assert
            Assert.IsTrue(optimalWidth >= 4, "Large map should have increased corridor width");
            Assert.IsTrue(optimalWidth <= CorridorWidthValidator.MAX_CORRIDOR_WIDTH);
        }
        
        [Test]
        public void CalculateOptimalCorridorWidth_WithLargeRooms_ReturnsIncreasedWidth()
        {
            // Arrange
            var largeRooms = new List<RoomData>
            {
                new RoomData(1, new RectInt(10, 10, 20, 20), RoomClassification.Office), // Large room
                new RoomData(2, new RectInt(40, 40, 15, 15), RoomClassification.Meeting) // Large room
            };
            var mapSize = new Vector2Int(100, 100);
            int preferredWidth = 3;
            
            // Act
            var optimalWidth = CorridorWidthValidator.CalculateOptimalCorridorWidth(largeRooms, mapSize, preferredWidth);
            
            // Assert
            Assert.IsTrue(optimalWidth >= preferredWidth, "Large rooms should support wider corridors");
        }
        
        [Test]
        public void CalculateOptimalCorridorWidth_WithSmallRooms_ReturnsDecreasedWidth()
        {
            // Arrange
            var smallRooms = new List<RoomData>
            {
                new RoomData(1, new RectInt(10, 10, 3, 3), RoomClassification.Office), // Small room
                new RoomData(2, new RectInt(20, 20, 4, 4), RoomClassification.Meeting) // Small room
            };
            var mapSize = new Vector2Int(50, 50);
            int preferredWidth = 4;
            
            // Act
            var optimalWidth = CorridorWidthValidator.CalculateOptimalCorridorWidth(smallRooms, mapSize, preferredWidth);
            
            // Assert
            Assert.IsTrue(optimalWidth <= preferredWidth, "Small rooms should have narrower corridors");
        }
        
        #endregion
        
        #region Utility Tests
        
        [Test]
        public void GetRecommendedMinimumWidth_WithSmallMap_ReturnsAppropriateWidth()
        {
            // Arrange
            var smallMapSize = new Vector2Int(30, 30);
            bool hasDiagonalMovement = false;
            
            // Act
            var minWidth = CorridorWidthValidator.GetRecommendedMinimumWidth(smallMapSize, hasDiagonalMovement);
            
            // Assert
            Assert.IsTrue(minWidth >= CorridorWidthValidator.MIN_CORRIDOR_WIDTH);
            Assert.IsTrue(minWidth <= CorridorWidthValidator.RECOMMENDED_MIN_WIDTH);
        }
        
        [Test]
        public void GetRecommendedMinimumWidth_WithDiagonalMovement_ReturnsIncreasedWidth()
        {
            // Arrange
            var mapSize = new Vector2Int(100, 100);
            bool hasDiagonalMovement = true;
            
            // Act
            var minWidth = CorridorWidthValidator.GetRecommendedMinimumWidth(mapSize, hasDiagonalMovement);
            
            // Assert
            Assert.IsTrue(minWidth >= 4, "Diagonal movement should require wider corridors");
        }
        
        [Test]
        public void GetEffectiveWidthAtPosition_WithValidCorridor_ReturnsCorrectWidth()
        {
            // Arrange
            var corridor = _testCorridors[0];
            var position = corridor.PathTiles[0];
            
            // Act
            var effectiveWidth = CorridorWidthValidator.GetEffectiveWidthAtPosition(corridor, position);
            
            // Assert
            Assert.AreEqual(corridor.Width, effectiveWidth);
        }
        
        [Test]
        public void GetEffectiveWidthAtPosition_WithInvalidPosition_ReturnsZero()
        {
            // Arrange
            var corridor = _testCorridors[0];
            var invalidPosition = new Vector2Int(999, 999);
            
            // Act
            var effectiveWidth = CorridorWidthValidator.GetEffectiveWidthAtPosition(corridor, invalidPosition);
            
            // Assert
            Assert.AreEqual(0, effectiveWidth);
        }
        
        [Test]
        public void CreateWidthReport_WithValidCorridors_ReturnsDetailedReport()
        {
            // Arrange
            var corridors = _testCorridors;
            
            // Act
            var report = CorridorWidthValidator.CreateWidthReport(corridors);
            
            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("Corridor Width Report"));
            Assert.IsTrue(report.Contains("Total corridors"));
            Assert.IsTrue(report.Contains("Width distribution"));
            Assert.IsTrue(report.Contains("Total corridor tiles"));
        }
        
        [Test]
        public void CreateWidthReport_WithNullCorridors_ReturnsErrorMessage()
        {
            // Act
            var report = CorridorWidthValidator.CreateWidthReport(null);
            
            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("null"));
        }
        
        [Test]
        public void ValidatePathContinuity_WithContinuousPath_ReturnsSuccess()
        {
            // Arrange
            var continuousPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new VectorInt(2, 1),
                new Vector2Int(2, 2)
            };
            
            // Act
            var result = CorridorWidthValidator.ValidatePathContinuity(continuousPath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidatePathContinuity_WithGappedPath_ReturnsError()
        {
            // Arrange
            var gappedPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(3, 0), // Gap of 2
                new Vector2Int(4, 0)
            };
            
            // Act
            var result = CorridorWidthValidator.ValidatePathContinuity(gappedPath);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        #endregion
        
        #region Helper Methods
        
        private List<RoomData> CreateTestRooms()
        {
            var rooms = new List<RoomData>();
            
            // Room 1: Medium office
            var room1 = new RoomData(1, new RectInt(5, 5, 8, 6), RoomClassification.Office);
            rooms.Add(room1);
            
            // Room 2: Small meeting room
            var room2 = new RoomData(2, new RectInt(20, 8, 5, 4), RoomClassification.Meeting);
            rooms.Add(room2);
            
            return rooms;
        }
        
        private List<CorridorData> CreateTestCorridors()
        {
            var corridors = new List<CorridorData>();
            
            // Corridor 1: 3-width corridor
            var corridor1 = new CorridorData(1, 2, new Vector2Int(13, 10), new Vector2Int(20, 10), 3);
            var path1 = new List<Vector2Int>
            {
                new Vector2Int(13, 10),
                new Vector2Int(14, 10),
                new Vector2Int(15, 10),
                new Vector2Int(16, 10),
                new Vector2Int(17, 10),
                new Vector2Int(18, 10),
                new Vector2Int(19, 10),
                new Vector2Int(20, 10)
            };
            corridor1.SetPath(path1);
            corridors.Add(corridor1);
            
            // Corridor 2: 4-width corridor
            var corridor2 = new CorridorData(3, 4, new Vector2Int(10, 11), new Vector2Int(10, 20), 4);
            var path2 = new List<Vector2Int>
            {
                new Vector2Int(10, 11),
                new Vector2Int(10, 12),
                new Vector2Int(10, 13),
                new Vector2Int(10, 14),
                new Vector2Int(10, 15),
                new Vector2Int(10, 16),
                new Vector2Int(10, 17),
                new Vector2Int(10, 18),
                new Vector2Int(10, 19),
                new Vector2Int(10, 20)
            };
            corridor2.SetPath(path2);
            corridors.Add(corridor2);
            
            return corridors;
        }
        
        private bool[,] CreateTestObstacleMap()
        {
            var obstacles = new bool[30, 30];
            
            // Add some obstacles for testing
            obstacles[10, 10] = true;
            obstacles[11, 10] = true;
            obstacles[12, 10] = true;
            
            obstacles[15, 15] = true;
            obstacles[15, 16] = true;
            obstacles[15, 17] = true;
            
            return obstacles;
        }
        
        #endregion
    }
}