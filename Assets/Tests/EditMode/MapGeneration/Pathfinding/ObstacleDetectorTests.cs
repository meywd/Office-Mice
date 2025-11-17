using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Tests.Pathfinding
{
    [TestFixture]
    public class ObstacleDetectorTests
    {
        private List<RoomData> _testRooms;
        private List<CorridorData> _testCorridors;
        
        [SetUp]
        public void SetUp()
        {
            _testRooms = CreateTestRooms();
            _testCorridors = CreateTestCorridors();
        }
        
        #region Obstacle Map Creation Tests
        
        [Test]
        public void CreateObstacleMap_WithValidParameters_ReturnsCorrectMap()
        {
            // Arrange
            int width = 20;
            int height = 20;
            
            // Act
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, _testCorridors);
            
            // Assert
            Assert.IsNotNull(obstacles);
            Assert.AreEqual(width, obstacles.GetLength(0));
            Assert.AreEqual(height, obstacles.GetLength(1));
            
            // Check boundaries are marked as obstacles
            for (int x = 0; x < width; x++)
            {
                Assert.IsTrue(obstacles[x, 0], $"Top boundary at x={x} should be obstacle");
                Assert.IsTrue(obstacles[x, height - 1], $"Bottom boundary at x={x} should be obstacle");
            }
            
            for (int y = 0; y < height; y++)
            {
                Assert.IsTrue(obstacles[0, y], $"Left boundary at y={y} should be obstacle");
                Assert.IsTrue(obstacles[width - 1, y], $"Right boundary at y={y} should be obstacle");
            }
        }
        
        [Test]
        public void CreateObstacleMap_WithRooms_MarksRoomsAsObstacles()
        {
            // Arrange
            int width = 20;
            int height = 20;
            
            // Act
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, null);
            
            // Assert
            foreach (var room in _testRooms)
            {
                var bounds = room.GetBounds();
                for (int x = bounds.xMin; x <= bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y <= bounds.yMax; y++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Assert.IsTrue(obstacles[x, y], $"Room tile at ({x},{y}) should be obstacle");
                        }
                    }
                }
            }
        }
        
        [Test]
        public void CreateObstacleMap_WithCorridors_MarksCorridorsAsObstacles()
        {
            // Arrange
            int width = 20;
            int height = 20;
            
            // Act
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, _testCorridors);
            
            // Assert
            foreach (var corridor in _testCorridors)
            {
                foreach (var tile in corridor.PathTiles)
                {
                    if (tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height)
                    {
                        Assert.IsTrue(obstacles[tile.x, tile.y], $"Corridor tile at {tile} should be obstacle");
                    }
                }
            }
        }
        
        [Test]
        public void CreateObstacleMap_InvalidDimensions_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ObstacleDetector.CreateObstacleMap(0, 20, _testRooms, _testCorridors));
            Assert.Throws<ArgumentException>(() => ObstacleDetector.CreateObstacleMap(20, 0, _testRooms, _testCorridors));
            Assert.Throws<ArgumentException>(() => ObstacleDetector.CreateObstacleMap(-5, 20, _testRooms, _testCorridors));
        }
        
        #endregion
        
        #region Obstacle Update Tests
        
        [Test]
        public void UpdateObstacleMap_WithNewRooms_UpdatesCorrectly()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var newRooms = new List<RoomData> { _testRooms[0] };
            
            // Act
            ObstacleDetector.UpdateObstacleMap(obstacles, newRooms, null);
            
            // Assert
            var room = _testRooms[0];
            var bounds = room.GetBounds();
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y <= bounds.yMax; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        Assert.IsTrue(obstacles[x, y], $"Updated room tile at ({x},{y}) should be obstacle");
                    }
                }
            }
        }
        
        [Test]
        public void UpdateObstacleMap_WithNewCorridors_UpdatesCorrectly()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var newCorridors = new List<CorridorData> { _testCorridors[0] };
            
            // Act
            ObstacleDetector.UpdateObstacleMap(obstacles, null, newCorridors);
            
            // Assert
            var corridor = _testCorridors[0];
            foreach (var tile in corridor.PathTiles)
            {
                if (tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height)
                {
                    Assert.IsTrue(obstacles[tile.x, tile.y], $"Updated corridor tile at {tile} should be obstacle");
                }
            }
        }
        
        #endregion
        
        #region Path Clearing Tests
        
        [Test]
        public void ClearPathFromObstacles_WithValidPath_ClearsCorrectTiles()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, _testCorridors);
            var path = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5) };
            
            // Act
            ObstacleDetector.ClearPathFromObstacles(obstacles, path);
            
            // Assert
            foreach (var tile in path)
            {
                Assert.IsFalse(obstacles[tile.x, tile.y], $"Path tile at {tile} should be cleared");
            }
        }
        
        [Test]
        public void ClearTileFromObstacles_WithValidTile_ClearsTileAndSurroundings()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, _testCorridors);
            var tile = new Vector2Int(10, 10);
            int corridorWidth = 3;
            
            // Act
            ObstacleDetector.ClearTileFromObstacles(obstacles, tile, corridorWidth);
            
            // Assert
            int radius = corridorWidth / 2;
            for (int x = tile.x - radius; x <= tile.x + radius; x++)
            {
                for (int y = tile.y - radius; y <= tile.y + radius; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        Assert.IsFalse(obstacles[x, y], $"Cleared area tile at ({x},{y}) should be clear");
                    }
                }
            }
        }
        
        #endregion
        
        #region Position Validation Tests
        
        [Test]
        public void IsValidPosition_WithValidPosition_ReturnsTrue()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var validPosition = new Vector2Int(10, 10);
            
            // Act
            var isValid = ObstacleDetector.IsValidPosition(validPosition, obstacles);
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void IsValidPosition_WithObstacle_ReturnsFalse()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, null);
            var roomCenter = new Vector2Int(5, 5); // Inside first room
            
            // Act
            var isValid = ObstacleDetector.IsValidPosition(roomCenter, obstacles);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void IsValidPosition_WithOutOfBoundsPosition_ReturnsFalse()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var outOfBoundsPosition = new Vector2Int(-1, 10);
            
            // Act
            var isValid = ObstacleDetector.IsValidPosition(outOfBoundsPosition, obstacles);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void IsValidPosition_WithNullObstacles_ReturnsFalse()
        {
            // Arrange
            var position = new Vector2Int(10, 10);
            
            // Act
            var isValid = ObstacleDetector.IsValidPosition(position, null);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        #endregion
        
        #region Corridor Path Validation Tests
        
        [Test]
        public void IsValidCorridorPath_WithValidPath_ReturnsTrue()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var path = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5) };
            
            // Act
            var isValid = ObstacleDetector.IsValidCorridorPath(path, obstacles);
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void IsValidCorridorPath_WithObstacleInPath_ReturnsFalse()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, _testRooms, null);
            var path = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5) };
            
            // Act
            var isValid = ObstacleDetector.IsValidCorridorPath(path, obstacles);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void IsValidCorridorPath_WithNullPath_ReturnsFalse()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            
            // Act
            var isValid = ObstacleDetector.IsValidCorridorPath(null, obstacles);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        #endregion
        
        #region Doorway Detection Tests
        
        [Test]
        public void FindValidDoorwayPositions_WithRoom_ReturnsValidPositions()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            var room = _testRooms[0];
            
            // Act
            var doorways = ObstacleDetector.FindValidDoorwayPositions(room, obstacles);
            
            // Assert
            Assert.IsNotNull(doorways);
            Assert.IsTrue(doorways.Count > 0);
            
            foreach (var doorway in doorways)
            {
                Assert.IsTrue(ObstacleDetector.IsValidPosition(doorway, obstacles), 
                    $"Doorway position {doorway} should be valid");
            }
        }
        
        [Test]
        public void FindValidDoorwayPositions_WithNullRoom_ReturnsEmptyList()
        {
            // Arrange
            int width = 20;
            int height = 20;
            var obstacles = ObstacleDetector.CreateObstacleMap(width, height, null, null);
            
            // Act
            var doorways = ObstacleDetector.FindValidDoorwayPositions(null, obstacles);
            
            // Assert
            Assert.IsNotNull(doorways);
            Assert.AreEqual(0, doorways.Count);
        }
        
        #endregion
        
        #region Obstacle Expansion Tests
        
        [Test]
        public void ExpandObstacles_WithValidObstacles_ExpandsCorrectly()
        {
            // Arrange
            int width = 10;
            int height = 10;
            var obstacles = new bool[width, height];
            obstacles[5, 5] = true; // Single obstacle
            int expansionRadius = 1;
            
            // Act
            var expanded = ObstacleDetector.ExpandObstacles(obstacles, expansionRadius);
            
            // Assert
            Assert.IsNotNull(expanded);
            Assert.AreEqual(width, expanded.GetLength(0));
            Assert.AreEqual(height, expanded.GetLength(1));
            
            // Check that original obstacle is still there
            Assert.IsTrue(expanded[5, 5]);
            
            // Check that surrounding tiles are now obstacles
            for (int x = 4; x <= 6; x++)
            {
                for (int y = 4; y <= 6; y++)
                {
                    Assert.IsTrue(expanded[x, y], $"Expanded obstacle at ({x},{y}) should be true");
                }
            }
        }
        
        #endregion
        
        #region Utility Tests
        
        [Test]
        public void CountObstacles_WithValidMap_ReturnsCorrectCount()
        {
            // Arrange
            int width = 10;
            int height = 10;
            var obstacles = new bool[width, height];
            obstacles[1, 1] = true;
            obstacles[2, 2] = true;
            obstacles[3, 3] = true;
            
            // Act
            var count = ObstacleDetector.CountObstacles(obstacles);
            
            // Assert
            Assert.AreEqual(3, count);
        }
        
        [Test]
        public void GetObstacleDensity_WithValidMap_ReturnsCorrectDensity()
        {
            // Arrange
            int width = 10;
            int height = 10;
            var obstacles = new bool[width, height];
            obstacles[1, 1] = true;
            obstacles[2, 2] = true;
            obstacles[3, 3] = true;
            
            // Act
            var density = ObstacleDetector.GetObstacleDensity(obstacles);
            
            // Assert
            Assert.AreEqual(0.03f, density, 0.001f); // 3 obstacles out of 100 tiles
        }
        
        [Test]
        public void VisualizeObstacleMap_WithValidMap_ReturnsVisualization()
        {
            // Arrange
            int width = 5;
            int height = 5;
            var obstacles = new bool[width, height];
            obstacles[2, 2] = true;
            
            // Act
            var visualization = ObstacleDetector.VisualizeObstacleMap(obstacles);
            
            // Assert
            Assert.IsNotNull(visualization);
            Assert.IsTrue(visualization.Length > 0);
            Assert.IsTrue(visualization.Contains("█")); // Should contain obstacle character
            Assert.IsTrue(visualization.Contains("·")); // Should contain empty character
        }
        
        [Test]
        public void VisualizeObstacleMap_WithNullMap_ReturnsErrorMessage()
        {
            // Act
            var visualization = ObstacleDetector.VisualizeObstacleMap(null);
            
            // Assert
            Assert.IsNotNull(visualization);
            Assert.IsTrue(visualization.Contains("null"));
        }
        
        #endregion
        
        #region Helper Methods
        
        private List<RoomData> CreateTestRooms()
        {
            var rooms = new List<RoomData>();
            
            // Room 1: 4x4 room at (3,3)
            var room1 = new RoomData(1, new RectInt(3, 3, 4, 4), RoomClassification.Office);
            rooms.Add(room1);
            
            // Room 2: 3x5 room at (12, 8)
            var room2 = new RoomData(2, new RectInt(12, 8, 3, 5), RoomClassification.Meeting);
            rooms.Add(room2);
            
            return rooms;
        }
        
        private List<CorridorData> CreateTestCorridors()
        {
            var corridors = new List<CorridorData>();
            
            // Corridor 1: L-shaped corridor
            var corridor1 = new CorridorData(1, 2, new Vector2Int(7, 5), new Vector2Int(12, 10), 3);
            var path1 = new List<Vector2Int>
            {
                new Vector2Int(7, 5),
                new Vector2Int(8, 5),
                new Vector2Int(9, 5),
                new Vector2Int(10, 5),
                new Vector2Int(10, 6),
                new Vector2Int(10, 7),
                new Vector2Int(10, 8),
                new Vector2Int(10, 9),
                new Vector2Int(10, 10),
                new Vector2Int(11, 10),
                new Vector2Int(12, 10)
            };
            corridor1.SetPath(path1);
            corridors.Add(corridor1);
            
            return corridors;
        }
        
        #endregion
    }
}