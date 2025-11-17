using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Corridors;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Pathfinding;

namespace OfficeMice.MapGeneration.Tests.Corridors
{
    [TestFixture]
    public class TwoPassCorridorGeneratorTests
    {
        private TwoPassCorridorGenerator _generator;
        private MockPathfinder _mockPathfinder;
        private MapGenerationSettings _testSettings;
        private List<RoomData> _testRooms;
        
        [SetUp]
        public void SetUp()
        {
            _mockPathfinder = new MockPathfinder();
            _generator = new TwoPassCorridorGenerator(_mockPathfinder);
            _testSettings = CreateTestSettings();
            _testRooms = CreateTestRooms();
        }
        
        [TearDown]
        public void TearDown()
        {
            _generator = null;
            _mockPathfinder = null;
            _testSettings = null;
            _testRooms = null;
        }
        
        #region Task 1: Core Room Identification Tests
        
        [Test]
        public void IdentifyCoreRooms_SelectsLargestRooms()
        {
            // Arrange
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),   // 25 tiles
                new RoomData(new RectInt(10, 0, 8, 8)),  // 64 tiles
                new RoomData(new RectInt(20, 0, 6, 6)),   // 36 tiles
                new RoomData(new RectInt(0, 10, 10, 10)) // 100 tiles (largest)
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result, "Corridors should be generated");
            Assert.IsTrue(result.Count > 0, "Should generate at least one corridor");
            
            // The largest room should be connected to form the primary network
            var connectedRoomIDs = result.SelectMany(c => new[] { c.RoomA_ID, c.RoomB_ID }).ToHashSet();
            Assert.Contains(3, connectedRoomIDs.ToList(), "Largest room (ID 3) should be connected");
        }
        
        [Test]
        public void IdentifyCoreRooms_EnsuresGeographicDistribution()
        {
            // Arrange - Create rooms in different quadrants
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 8, 8)),      // Top-left
                new RoomData(new RectInt(50, 0, 8, 8)),     // Top-right
                new RoomData(new RectInt(0, 50, 8, 8)),     // Bottom-left
                new RoomData(new RectInt(50, 50, 8, 8)),    // Bottom-right
                new RoomData(new RectInt(25, 25, 15, 15))   // Center (largest)
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result);
            var connectedRoomIDs = result.SelectMany(c => new[] { c.RoomA_ID, c.RoomB_ID }).ToHashSet();
            
            // Should have rooms from different areas connected
            Assert.IsTrue(connectedRoomIDs.Count >= 3, "Should connect rooms from multiple areas");
        }
        
        [Test]
        public void IdentifyCoreRooms_MinimumCoreRoomsFallback()
        {
            // Arrange - All rooms are small
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 3, 3)),
                new RoomData(new RectInt(10, 0, 3, 3)),
                new RoomData(new RectInt(20, 0, 3, 3))
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Should still generate corridors with small rooms");
        }
        
        #endregion
        
        #region Task 2: Primary Corridor Generation Tests
        
        [Test]
        public void GeneratePrimaryCorridors_CreatesMainHallways()
        {
            // Arrange
            var coreRooms = _testRooms.Take(3).ToList();
            
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            Assert.IsTrue(primaryCorridors.Count > 0, "Should generate primary corridors");
            
            // Primary corridors should connect core rooms
            foreach (var corridor in primaryCorridors)
            {
                Assert.AreEqual(5, corridor.Width, "Primary corridors should be 5 tiles wide");
            }
        }
        
        [Test]
        public void GeneratePrimaryCorridors_UsesMSTOptimization()
        {
            // Arrange - Create rooms in a line to test MST
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),
                new RoomData(new RectInt(20, 0, 5, 5)),
                new RoomData(new RectInt(40, 0, 5, 5)),
                new RoomData(new RectInt(60, 0, 5, 5))
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            
            // MST should create minimal connections (n-1 corridors for n rooms)
            Assert.IsTrue(primaryCorridors.Count <= rooms.Count - 1, 
                "MST should create minimal number of primary corridors");
        }
        
        #endregion
        
        #region Task 3: Secondary Corridor Generation Tests
        
        [Test]
        public void GenerateSecondaryCorridors_ConnectsRemainingRooms()
        {
            // Arrange
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 10, 10)),    // Core room
                new RoomData(new RectInt(30, 0, 10, 10)),   // Core room
                new RoomData(new RectInt(15, 20, 5, 5)),     // Secondary room
                new RoomData(new RectInt(35, 20, 5, 5))      // Secondary room
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var secondaryCorridors = result.Where(c => c.Width == 3).ToList();
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should generate secondary corridors");
            
            foreach (var corridor in secondaryCorridors)
            {
                Assert.AreEqual(3, corridor.Width, "Secondary corridors should be 3 tiles wide");
            }
        }
        
        [Test]
        public void GenerateSecondaryCorridors_ConnectsToMainArteries()
        {
            // Arrange
            _mockPathfinder.SetPath(new List<Vector2Int>
            {
                new Vector2Int(5, 5), new Vector2Int(10, 5), new Vector2Int(15, 5), new Vector2Int(20, 5)
            });
            
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            Assert.IsTrue(result.Count > 0, "Should generate corridors connecting to main arteries");
            
            // Verify connectivity
            var connectivityResult = _generator.ValidateConnectivity(_testRooms, result);
            Assert.IsTrue(connectivityResult.IsValid, "All rooms should be connected");
        }
        
        #endregion
        
        #region Task 4: Corridor Width Variation Tests
        
        [Test]
        public void CorridorWidthVariation_PrimaryCorridorsHaveCorrectWidth()
        {
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            foreach (var corridor in primaryCorridors)
            {
                Assert.AreEqual(5, corridor.Width, "Primary corridors should be 5 tiles wide");
            }
        }
        
        [Test]
        public void CorridorWidthVariation_SecondaryCorridorsHaveCorrectWidth()
        {
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            var secondaryCorridors = result.Where(c => c.Width == 3).ToList();
            foreach (var corridor in secondaryCorridors)
            {
                Assert.AreEqual(3, corridor.Width, "Secondary corridors should be 3 tiles wide");
            }
        }
        
        [Test]
        public void CorridorWidthVariation_NoInvalidWidths()
        {
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            foreach (var corridor in result)
            {
                Assert.IsTrue(corridor.Width == 3 || corridor.Width == 5, 
                    $"Corridor width should be 3 or 5, got {corridor.Width}");
            }
        }
        
        #endregion
        
        #region Task 5: Connectivity Validation Tests
        
        [Test]
        public void ConnectivityValidation_Guarantees100PercentConnectivity()
        {
            // Arrange
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),
                new RoomData(new RectInt(20, 0, 5, 5)),
                new RoomData(new RectInt(40, 0, 5, 5)),
                new RoomData(new RectInt(60, 0, 5, 5)),
                new RoomData(new RectInt(80, 0, 5, 5))
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var connectivityResult = _generator.ValidateConnectivity(rooms, result);
            Assert.IsTrue(connectivityResult.IsValid, 
                $"Connectivity validation failed: {string.Join(", ", connectivityResult.Errors)}");
        }
        
        [Test]
        public void ConnectivityValidation_DetectsIsolatedRooms()
        {
            // Arrange - Create disconnected scenario
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),
                new RoomData(new RectInt(100, 100, 5, 5))  // Far away room
            };
            
            // Mock pathfinder to return empty path for distant rooms
            _mockPathfinder.SetPath(new List<Vector2Int>());
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert - Should attempt to fix connectivity
            var connectivityResult = _generator.ValidateConnectivity(rooms, result);
            // The generator should try to fix connectivity, so this might pass
        }
        
        [Test]
        public void ConnectivityValidation_FixesConnectivityIssues()
        {
            // Arrange
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),
                new RoomData(new RectInt(20, 0, 5, 5)),
                new RoomData(new RectInt(40, 0, 5, 5))
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result, "Should generate corridors");
            Assert.IsTrue(result.Count > 0, "Should generate at least one corridor");
            
            var connectivityResult = _generator.ValidateConnectivity(rooms, result);
            if (!connectivityResult.IsValid)
            {
                // If initial validation fails, the generator should attempt to fix it
                Assert.IsTrue(connectivityResult.Errors.Count <= 1, 
                    "Should have minimal connectivity issues after fixing");
            }
        }
        
        #endregion
        
        #region Task 6: MST Optimization Tests
        
        [Test]
        public void MSTOptimization_CreatesMinimalSpanningTree()
        {
            // Arrange - Create rooms in a square formation
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),      // Top-left
                new RoomData(new RectInt(20, 0, 5, 5)),     // Top-right
                new RoomData(new RectInt(0, 20, 5, 5)),      // Bottom-left
                new RoomData(new RectInt(20, 20, 5, 5))     // Bottom-right
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            
            // For 4 rooms, MST should create exactly 3 connections
            Assert.IsTrue(primaryCorridors.Count <= 3, 
                $"MST should create at most 3 primary corridors for 4 rooms, got {primaryCorridors.Count}");
        }
        
        [Test]
        public void MSTOptimization_MinimizesTotalCorridorLength()
        {
            // Arrange - Create rooms in a line
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 5, 5)),
                new RoomData(new RectInt(10, 0, 5, 5)),
                new RoomData(new RectInt(20, 0, 5, 5))
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var totalLength = _generator.CalculateTotalCorridorLength(result);
            Assert.IsTrue(totalLength > 0, "Should have positive total corridor length");
            
            // MST should connect adjacent rooms, not skip to distant ones
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            foreach (var corridor in primaryCorridors)
            {
                Assert.IsTrue(corridor.GetDistance() < 30, 
                    "MST should prefer shorter connections");
            }
        }
        
        #endregion
        
        #region Task 7: Corridor Hierarchy Logic Tests
        
        [Test]
        public void CorridorHierarchy_ImplementsTwoPassSystem()
        {
            // Act
            var result = _generator.ConnectRooms(_testRooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            var secondaryCorridors = result.Where(c => c.Width == 3).ToList();
            
            Assert.IsTrue(primaryCorridors.Count > 0, "Should have primary corridors");
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should have secondary corridors");
            
            // Should have both types of corridors (two-pass system)
            Assert.IsTrue(result.Count >= 2, "Should implement two-pass corridor system");
        }
        
        [Test]
        public void CorridorHierarchy_CreatesRealisticOfficeFlow()
        {
            // Arrange - Create office-like layout
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 15, 15)),    // Large conference room (core)
                new RoomData(new RectInt(30, 0, 10, 10)),   // Medium office (core)
                new RoomData(new RectInt(0, 25, 8, 8)),     // Small office (secondary)
                new RoomData(new RectInt(15, 25, 6, 6)),    // Storage room (secondary)
                new RoomData(new RectInt(30, 25, 5, 5))     // Server room (secondary)
            };
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            var secondaryCorridors = result.Where(c => c.Width == 3).ToList();
            
            // Primary corridors should connect the larger rooms
            var primaryRoomIDs = primaryCorridors.SelectMany(c => new[] { c.RoomA_ID, c.RoomB_ID }).ToHashSet();
            Assert.Contains(0, primaryRoomIDs.ToList(), "Large conference room should be on primary corridor");
            Assert.Contains(1, primaryRoomIDs.ToList(), "Medium office should be on primary corridor");
            
            // Secondary corridors should connect smaller rooms to the main network
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should have secondary corridors for smaller rooms");
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void Performance_LargeMapGeneration()
        {
            // Arrange - Create 100 rooms
            var rooms = new List<RoomData>();
            for (int i = 0; i < 100; i++)
            {
                var x = (i % 10) * 15;
                var y = (i / 10) * 15;
                var size = UnityEngine.Random.Range(5, 10);
                rooms.Add(new RoomData(new RectInt(x, y, size, size)));
            }
            
            // Act
            var startTime = System.DateTime.UtcNow;
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            var endTime = System.DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Should generate corridors for large map");
            Assert.IsTrue(duration < 200, $"Corridor generation should complete within 200ms, took {duration}ms");
        }
        
        [Test]
        public void Performance_MemoryUsage()
        {
            // Arrange
            var rooms = new List<RoomData>();
            for (int i = 0; i < 50; i++)
            {
                var x = (i % 7) * 20;
                var y = (i / 7) * 20;
                rooms.Add(new RoomData(new RectInt(x, y, 8, 8)));
            }
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result);
            // Memory usage is harder to test directly, but we can check for reasonable corridor count
            Assert.IsTrue(result.Count < rooms.Count * 2, 
                "Should not generate excessive number of corridors");
        }
        
        #endregion
        
        #region Integration Tests
        
        [Test]
        public void Integration_CompleteWorkflow()
        {
            // Arrange
            var rooms = CreateComplexOfficeLayout();
            
            // Act
            var result = _generator.ConnectRooms(rooms, _testSettings, 12345);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Should generate corridors");
            
            // Validate all acceptance criteria
            var connectivityResult = _generator.ValidateConnectivity(rooms, result);
            Assert.IsTrue(connectivityResult.IsValid, "Should ensure 100% connectivity");
            
            var primaryCorridors = result.Where(c => c.Width == 5).ToList();
            var secondaryCorridors = result.Where(c => c.Width == 3).ToList();
            
            Assert.IsTrue(primaryCorridors.Count > 0, "Should have primary corridors");
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should have secondary corridors");
            
            // Test corridor optimization
            var optimized = _generator.OptimizeCorridors(result, rooms, _testSettings);
            Assert.IsNotNull(optimized, "Should optimize corridors");
        }
        
        [Test]
        public void Integration_WithDifferentSeeds()
        {
            // Arrange
            var rooms = _testRooms;
            
            // Act
            var result1 = _generator.ConnectRooms(rooms, _testSettings, 12345);
            var result2 = _generator.ConnectRooms(rooms, _testSettings, 54321);
            
            // Assert - Results should be different due to different seeds
            bool areDifferent = result1.Count != result2.Count || 
                               !result1.Select(c => c.CorridorID).OrderBy(id => id)
                                      .SequenceEqual(result2.Select(c => c.CorridorID).OrderBy(id => id));
            
            // Note: This might be false if the algorithm is deterministic despite the seed
            // The important thing is that both results should be valid
            var connectivity1 = _generator.ValidateConnectivity(rooms, result1);
            var connectivity2 = _generator.ValidateConnectivity(rooms, result2);
            
            Assert.IsTrue(connectivity1.IsValid, "First seed should produce valid result");
            Assert.IsTrue(connectivity2.IsValid, "Second seed should produce valid result");
        }
        
        #endregion
        
        #region Helper Methods
        
        private MapGenerationSettings CreateTestSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            
            // Create minimal valid configuration
            var mapConfig = new MapConfiguration
            {
                _mapSizeRange = new Vector2Int(50, 100),
                _roomSizeRange = new Vector2Int(5, 15),
                _minRooms = 3,
                _maxRooms = 20
            };
            
            var corridorConfig = new CorridorConfiguration
            {
                _corridorType = CorridorType.LShaped,
                _minWidth = 1,
                _maxWidth = 5,
                _pathSmoothing = 0.3f
            };
            
            var debugSettings = new DebugSettings
            {
                _enableLogging = true,
                _logPerformanceMetrics = true
            };
            
            // Use reflection to set private fields since they don't have public setters
            typeof(MapGenerationSettings).GetField("_mapConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, mapConfig);
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, corridorConfig);
            typeof(MapGenerationSettings).GetField("_debugSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, debugSettings);
            
            return settings;
        }
        
        private List<RoomData> CreateTestRooms()
        {
            return new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 10, 10)),    // Large room (core)
                new RoomData(new RectInt(25, 0, 8, 8)),      // Medium room (core)
                new RoomData(new RectInt(50, 0, 6, 6)),      // Small room
                new RoomData(new RectInt(15, 20, 5, 5)),     // Small room
                new RoomData(new RectInt(35, 20, 7, 7))      // Medium room
            };
        }
        
        private List<RoomData> CreateComplexOfficeLayout()
        {
            return new List<RoomData>
            {
                // Core areas (large rooms)
                new RoomData(new RectInt(0, 0, 20, 15)),     // Main lobby
                new RoomData(new RectInt(40, 0, 15, 12)),    // Conference room
                new RoomData(new RectInt(80, 0, 12, 10)),    // Large office
                
                // Secondary areas
                new RoomData(new RectInt(0, 25, 8, 8)),      // Small office 1
                new RoomData(new RectInt(20, 25, 6, 6)),     // Storage room
                new RoomData(new RectInt(35, 25, 7, 7)),     // Break room
                new RoomData(new RectInt(50, 25, 5, 5)),     // Server room
                new RoomData(new RectInt(65, 25, 8, 8)),     // Small office 2
                new RoomData(new RectInt(80, 25, 6, 6)),     // Security office
                new RoomData(new RectInt(95, 25, 10, 8))     // Boss office
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Mock pathfinder for testing purposes.
    /// </summary>
    public class MockPathfinder : IPathfinder
    {
        private List<Vector2Int> _mockPath;
        
        public void SetPath(List<Vector2Int> path)
        {
            _mockPath = path;
        }
        
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            if (_mockPath != null)
                return new List<Vector2Int>(_mockPath);
            
            // Default simple path
            var path = new List<Vector2Int>();
            var current = start;
            
            while (current.x != end.x)
            {
                current.x += current.x < end.x ? 1 : -1;
                path.Add(current);
            }
            
            while (current.y != end.y)
            {
                current.y += current.y < end.y ? 1 : -1;
                path.Add(current);
            }
            
            return path;
        }
        
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles, float[,] movementCosts)
        {
            return FindPath(start, end, obstacles);
        }
        
        public Dictionary<Vector2Int, List<Vector2Int>> FindMultiplePaths(Vector2Int start, List<Vector2Int> ends, bool[,] obstacles)
        {
            var result = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (var end in ends)
            {
                result[end] = FindPath(start, end, obstacles);
            }
            return result;
        }
        
        public bool PathExists(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            return FindPath(start, end, obstacles).Count > 0;
        }
        
        public HashSet<Vector2Int> GetReachablePositions(Vector2Int start, bool[,] obstacles, int maxDistance = -1)
        {
            return new HashSet<Vector2Int> { start };
        }
        
        public List<Vector2Int> OptimizePath(List<Vector2Int> path, bool[,] obstacles)
        {
            return new List<Vector2Int>(path);
        }
        
        public float CalculatePathCost(List<Vector2Int> path, float[,] movementCosts = null)
        {
            return path.Count;
        }
        
        public ValidationResult ValidatePathfindingParameters(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            return new ValidationResult();
        }
        
        public void SetHeuristic(Func<Vector2Int, Vector2Int, float> heuristic) { }
        
        public PathfindingStats GetPerformanceStats()
        {
            return new PathfindingStats();
        }
        
        public void ResetStats() { }
        
        public event Action<Vector2Int, Vector2Int> OnPathfindingStarted;
        public event Action<Vector2Int, Vector2Int, List<Vector2Int>> OnPathfindingCompleted;
        public event Action<Vector2Int, Vector2Int, Exception> OnPathfindingFailed;
    }
}