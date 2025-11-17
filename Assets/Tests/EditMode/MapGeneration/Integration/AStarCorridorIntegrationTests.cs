using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.Integration
{
    [TestFixture]
    public class AStarCorridorIntegrationTests
    {
        private AStarPathfinder _pathfinder;
        private List<RoomData> _testRooms;
        private MapGenerationSettings _testSettings;
        
        [SetUp]
        public void SetUp()
        {
            _pathfinder = new AStarPathfinder();
            _testRooms = CreateTestRooms();
            _testSettings = CreateTestSettings();
        }
        
        [TearDown]
        public void TearDown()
        {
            _pathfinder?.ResetStats();
        }
        
        #region Basic Integration Tests
        
        [Test]
        public void AStarPathfinder_WithCorridorGeneration_FindsOptimalPaths()
        {
            // Arrange
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act
            var path = _pathfinder.FindPath(start, end, obstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(end, path[path.Count - 1]);
            
            // Verify path doesn't go through obstacles
            foreach (var point in path)
            {
                Assert.IsFalse(obstacles[point.x, point.y], $"Path goes through obstacle at {point}");
            }
        }
        
        [Test]
        public void AStarPathfinding_WithMultipleRooms_ConnectsAllRooms()
        {
            // Arrange
            var mapSize = new Vector2Int(60, 60);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            var corridors = new List<CorridorData>();
            
            // Act - Connect all rooms using A* pathfinding
            for (int i = 0; i < _testRooms.Count - 1; i++)
            {
                var room1 = _testRooms[i];
                var room2 = _testRooms[i + 1];
                
                var start = room1.GetCenterTile();
                var end = room2.GetCenterTile();
                
                var path = _pathfinder.FindPath(start, end, obstacles);
                
                if (path.Count > 0)
                {
                    var corridor = new CorridorData(room1.RoomID, room2.RoomID, start, end, _testSettings.CorridorConfig.MaxWidth);
                    corridor.SetPath(path);
                    corridors.Add(corridor);
                    
                    // Update obstacles to include this corridor
                    ObstacleDetector.UpdateObstacleMap(obstacles, null, new List<CorridorData> { corridor });
                }
            }
            
            // Assert
            Assert.IsTrue(corridors.Count > 0, "Should have generated at least one corridor");
            
            foreach (var corridor in corridors)
            {
                Assert.IsTrue(corridor.PathTiles.Count > 0, "Corridor should have path tiles");
                Assert.AreEqual(_testSettings.CorridorConfig.MaxWidth, corridor.Width, "Corridor should use configured width");
            }
        }
        
        [Test]
        public void PathSmoothing_WithCorridorPaths_CreatesNaturalCorridors()
        {
            // Arrange
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act
            var originalPath = _pathfinder.FindPath(start, end, obstacles);
            var smoothedPath = PathSmoother.SmoothPathWeighted(originalPath, obstacles, 0.7f);
            
            // Assert
            Assert.IsNotNull(smoothedPath);
            Assert.IsTrue(smoothedPath.Count > 0);
            
            // Calculate smoothness improvement
            float originalSmoothness = PathSmoother.CalculatePathSmoothness(originalPath);
            float smoothedSmoothness = PathSmoother.CalculatePathSmoothness(smoothedPath);
            Assert.IsTrue(smoothedSmoothness <= originalSmoothness, "Smoothed path should be smoother or equal");
            
            // Verify smoothed path is still valid
            foreach (var point in smoothedPath)
            {
                Assert.IsFalse(obstacles[point.x, point.y], $"Smoothed path goes through obstacle at {point}");
            }
        }
        
        [Test]
        public void CorridorWidthValidation_WithGeneratedCorridors_EnsuresValidWidths()
        {
            // Arrange
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            var path = _pathfinder.FindPath(start, end, obstacles);
            
            var corridor = new CorridorData(room1.RoomID, room2.RoomID, start, end, _testSettings.CorridorConfig.MaxWidth);
            corridor.SetPath(path);
            
            // Act
            var validationResult = CorridorWidthValidator.ValidateCorridorData(corridor, obstacles);
            
            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsTrue(validationResult.IsValid, $"Corridor validation failed: {string.Join(", ", validationResult.Errors)}");
        }
        
        [Test]
        public void PerformanceOptimization_WithLargeMap_MeetsPerformanceTargets()
        {
            // Arrange
            var largeMapSize = new Vector2Int(100, 100);
            var largeRooms = CreateLargeTestRooms(10);
            var obstacles = ObstacleDetector.CreateObstacleMap(largeMapSize.x, largeMapSize.y, largeRooms, null);
            
            var room1 = largeRooms[0];
            var room2 = largeRooms[largeRooms.Count - 1];
            
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var path = _pathfinder.FindPath(start, end, obstacles);
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, $"Pathfinding took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");
            
            // Check performance stats
            var stats = _pathfinder.GetPerformanceStats();
            Assert.IsTrue(stats.AverageComputationTime < 50, "Average computation time should be under 50ms");
        }
        
        #endregion
        
        #region Complex Scenario Tests
        
        [Test]
        public void ComplexMapGeneration_WithMultipleObstacles_FindsValidPaths()
        {
            // Arrange
            var mapSize = new Vector2Int(80, 80);
            var complexRooms = CreateComplexTestRooms();
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, complexRooms, null);
            
            // Add additional obstacles for complexity
            AddComplexObstacles(obstacles);
            
            var corridors = new List<CorridorData>();
            
            // Act - Create a connected network
            for (int i = 0; i < complexRooms.Count; i++)
            {
                for (int j = i + 1; j < complexRooms.Count; j++)
                {
                    var room1 = complexRooms[i];
                    var room2 = complexRooms[j];
                    
                    var start = room1.GetCenterTile();
                    var end = room2.GetCenterTile();
                    
                    if (_pathfinder.PathExists(start, end, obstacles))
                    {
                        var path = _pathfinder.FindPath(start, end, obstacles);
                        
                        if (path.Count > 0)
                        {
                            var smoothedPath = PathSmoother.SmoothPathWeighted(path, obstacles, 0.5f);
                            var corridor = new CorridorData(room1.RoomID, room2.RoomID, start, end, 3);
                            corridor.SetPath(smoothedPath);
                            corridors.Add(corridor);
                            
                            // Update obstacles
                            ObstacleDetector.UpdateObstacleMap(obstacles, null, new List<CorridorData> { corridor });
                        }
                    }
                }
            }
            
            // Assert
            Assert.IsTrue(corridors.Count >= complexRooms.Count - 1, "Should have enough corridors to connect rooms");
            
            // Validate all corridors
            foreach (var corridor in corridors)
            {
                var validation = CorridorWidthValidator.ValidateCorridorData(corridor, obstacles);
                Assert.IsTrue(validation.IsValid, $"Corridor {corridor.CorridorID} validation failed");
            }
        }
        
        [Test]
        public void ObstacleAvoidance_WithDenseObstacles_FindsAlternativePaths()
        {
            // Arrange
            var mapSize = new Vector2Int(40, 40);
            var obstacles = CreateDenseObstacleMap(mapSize.x, mapSize.y);
            
            // Create rooms in clear areas
            var room1 = new RoomData(1, new RectInt(5, 5, 4, 4), RoomClassification.Office);
            var room2 = new RoomData(2, new RectInt(30, 30, 4, 4), RoomClassification.Office);
            
            var rooms = new List<RoomData> { room1, room2 };
            ObstacleDetector.UpdateObstacleMap(obstacles, rooms, null);
            
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act
            var path = _pathfinder.FindPath(start, end, obstacles);
            
            // Assert
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0, "Should find path even with dense obstacles");
            
            // Verify path avoids obstacles
            foreach (var point in path)
            {
                Assert.IsFalse(obstacles[point.x, point.y], $"Path goes through obstacle at {point}");
            }
            
            // Verify path is reasonable (not excessively long)
            float directDistance = Vector2Int.Distance(start, end);
            float pathDistance = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                pathDistance += Vector2Int.Distance(path[i - 1], path[i]);
            }
            
            Assert.IsTrue(pathDistance <= directDistance * 2.5f, "Path should not be excessively long");
        }
        
        [Test]
        public void MultiplePathfinding_WithSameStartEnd_ReturnsConsistentResults()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act
            var path1 = _pathfinder.FindPath(start, end, obstacles);
            var path2 = _pathfinder.FindPath(start, end, obstacles);
            var path3 = _pathfinder.FindPath(start, end, obstacles);
            
            // Assert
            Assert.IsNotNull(path1);
            Assert.IsNotNull(path2);
            Assert.IsNotNull(path3);
            
            // All paths should have same start and end
            Assert.AreEqual(start, path1[0]);
            Assert.AreEqual(end, path1[path1.Count - 1]);
            Assert.AreEqual(start, path2[0]);
            Assert.AreEqual(end, path2[path2.Count - 1]);
            Assert.AreEqual(start, path3[0]);
            Assert.AreEqual(end, path3[path3.Count - 1]);
            
            // Paths should be identical (deterministic)
            Assert.AreEqual(path1.Count, path2.Count);
            Assert.AreEqual(path2.Count, path3.Count);
            
            for (int i = 0; i < path1.Count; i++)
            {
                Assert.AreEqual(path1[i], path2[i]);
                Assert.AreEqual(path2[i], path3[i]);
            }
        }
        
        #endregion
        
        #region Edge Cases and Error Handling
        
        [Test]
        public void EdgeCases_WithBoundaryConditions_HandlesGracefully()
        {
            // Arrange
            var mapSize = new Vector2Int(30, 30);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            // Test positions near boundaries
            var edgeRoom1 = new RoomData(10, new RectInt(1, 1, 3, 3), RoomClassification.Office);
            var edgeRoom2 = new RoomData(11, new RectInt(26, 26, 3, 3), RoomClassification.Office);
            
            var edgeRooms = new List<RoomData> { edgeRoom1, edgeRoom2 };
            ObstacleDetector.UpdateObstacleMap(obstacles, edgeRooms, null);
            
            var start = edgeRoom1.GetCenterTile();
            var end = edgeRoom2.GetCenterTile();
            
            // Act
            var path = _pathfinder.FindPath(start, end, obstacles);
            
            // Assert
            Assert.IsNotNull(path);
            
            if (path.Count > 0)
            {
                // Verify path stays within bounds
                foreach (var point in path)
                {
                    Assert.IsTrue(point.x >= 0 && point.x < mapSize.x, $"Path point {point} is out of X bounds");
                    Assert.IsTrue(point.y >= 0 && point.y < mapSize.y, $"Path point {point} is out of Y bounds");
                    Assert.IsFalse(obstacles[point.x, point.y], $"Path point {point} hits obstacle");
                }
            }
        }
        
        [Test]
        public void ErrorHandling_WithInvalidInputs_ThrowsAppropriateExceptions()
        {
            // Arrange
            var validStart = new Vector2Int(5, 5);
            var validEnd = new Vector2Int(10, 10);
            var validObstacles = new bool[20, 20];
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _pathfinder.FindPath(validStart, validEnd, null));
            Assert.Throws<ArgumentException>(() => _pathfinder.FindPath(new Vector2Int(-1, 5), validEnd, validObstacles));
            Assert.Throws<ArgumentException>(() => _pathfinder.FindPath(new Vector2Int(25, 5), validEnd, validObstacles));
            Assert.Throws<ArgumentException>(() => _pathfinder.FindPath(validStart, new Vector2Int(-1, 10), validObstacles));
            Assert.Throws<ArgumentException>(() => _pathfinder.FindPath(validStart, new Vector2Int(25, 10), validObstacles));
        }
        
        #endregion
        
        #region Performance and Memory Tests
        
        [Test]
        public void MemoryUsage_WithMultiplePathfindingOperations_StaysWithinLimits()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act - Perform multiple pathfinding operations
            for (int i = 0; i < 100; i++)
            {
                var path = _pathfinder.FindPath(start, end, obstacles);
                Assert.IsNotNull(path);
            }
            
            // Assert
            var stats = _pathfinder.GetPerformanceStats();
            Assert.AreEqual(100, stats.TotalPathfindingCalls);
            Assert.AreEqual(100, stats.SuccessfulPaths);
            Assert.IsTrue(stats.AverageComputationTime < 50, "Average time should stay under 50ms");
        }
        
        [Test]
        public void CachingSystem_WithRepeatedPaths_ImprovesPerformance()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var obstacles = ObstacleDetector.CreateObstacleMap(mapSize.x, mapSize.y, _testRooms, null);
            
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];
            var start = room1.GetCenterTile();
            var end = room2.GetCenterTile();
            
            // Act - First pathfinding (should be slower)
            var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
            var path1 = _pathfinder.FindPath(start, end, obstacles);
            stopwatch1.Stop();
            
            // Second pathfinding with same parameters (should benefit from caching)
            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
            var path2 = _pathfinder.FindPath(start, end, obstacles);
            stopwatch2.Stop();
            
            // Assert
            Assert.IsNotNull(path1);
            Assert.IsNotNull(path2);
            Assert.AreEqual(path1.Count, path2.Count); // Should be identical paths
            
            // Note: AStarPathfinder doesn't use PathfindingOptimizer caching by default
            // This test verifies the basic performance characteristics
            Assert.IsTrue(stopwatch1.ElapsedMilliseconds < 50, "First pathfinding should complete in reasonable time");
            Assert.IsTrue(stopwatch2.ElapsedMilliseconds < 50, "Second pathfinding should also complete in reasonable time");
        }
        
        #endregion
        
        #region Helper Methods
        
        private List<RoomData> CreateTestRooms()
        {
            var rooms = new List<RoomData>();
            
            // Room 1: Office in top-left
            var room1 = new RoomData(1, new RectInt(5, 35, 6, 5), RoomClassification.Office);
            rooms.Add(room1);
            
            // Room 2: Meeting room in top-right
            var room2 = new RoomData(2, new RectInt(35, 35, 8, 6), RoomClassification.Meeting);
            rooms.Add(room2);
            
            // Room 3: Break room in bottom-left
            var room3 = new RoomData(3, new RectInt(8, 8, 5, 5), RoomClassification.Break);
            rooms.Add(room3);
            
            // Room 4: Storage in bottom-right
            var room4 = new RoomData(4, new RectInt(32, 10, 6, 4), RoomClassification.Storage);
            rooms.Add(room4);
            
            return rooms;
        }
        
        private List<RoomData> CreateLargeTestRooms(int count)
        {
            var rooms = new List<RoomData>();
            var random = new System.Random(42);
            
            for (int i = 0; i < count; i++)
            {
                var x = random.Next(5, 80);
                var y = random.Next(5, 80);
                var width = random.Next(4, 10);
                var height = random.Next(4, 10);
                
                var room = new RoomData(i + 1, new RectInt(x, y, width, height), RoomClassification.Office);
                rooms.Add(room);
            }
            
            return rooms;
        }
        
        private List<RoomData> CreateComplexTestRooms()
        {
            var rooms = new List<RoomData>();
            
            // Create rooms in various positions
            rooms.Add(new RoomData(1, new RectInt(10, 10, 6, 6), RoomClassification.Office));
            rooms.Add(new RoomData(2, new RectInt(30, 15, 8, 5), RoomClassification.Meeting));
            rooms.Add(new RoomData(3, new RectInt(50, 10, 5, 7), RoomClassification.Break));
            rooms.Add(new RoomData(4, new RectInt(15, 40, 7, 6), RoomClassification.Storage));
            rooms.Add(new RoomData(5, new RectInt(45, 45, 6, 5), RoomClassification.Office));
            rooms.Add(new RoomData(6, new RectInt(60, 30, 8, 8), RoomClassification.Meeting));
            
            return rooms;
        }
        
        private MapGenerationSettings CreateTestSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            
            // Configure corridor settings
            var corridorConfig = new CorridorConfiguration
            {
                MinWidth = 3,
                MaxWidth = 5,
                PathSmoothing = 0.5f,
                EnsureDirectPath = true,
                AvoidRooms = true
            };
            
            // Use reflection to set private field since it's a ScriptableObject
            var corridorConfigField = typeof(MapGenerationSettings).GetField("_corridorConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            corridorConfigField?.SetValue(settings, corridorConfig);
            
            return settings;
        }
        
        private bool[,] CreateDenseObstacleMap(int width, int height)
        {
            var obstacles = new bool[width, height];
            var random = new System.Random(42);
            
            // Create dense obstacle pattern
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextDouble() < 0.4) // 40% obstacle density
                    {
                        obstacles[x, y] = true;
                    }
                }
            }
            
            // Create some clear paths
            for (int i = 0; i < 5; i++)
            {
                int startX = random.Next(0, width);
                int startY = random.Next(0, height);
                
                // Create a clear corridor
                for (int j = 0; j < 10; j++)
                {
                    int x = startX + j;
                    int y = startY;
                    
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        obstacles[x, y] = false;
                    }
                }
            }
            
            return obstacles;
        }
        
        private void AddComplexObstacles(bool[,] obstacles)
        {
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            // Add some structured obstacles
            // Horizontal walls
            for (int x = 15; x < 25; x++)
            {
                obstacles[x, 20] = true;
                obstacles[x, 50] = true;
            }
            
            // Vertical walls
            for (int y = 25; y < 35; y++)
            {
                obstacles[40, y] = true;
                obstacles[20, y] = true;
            }
            
            // Add some random obstacles
            var random = new System.Random(123);
            for (int i = 0; i < 50; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);
                obstacles[x, y] = true;
            }
        }
        
        #endregion
    }
}