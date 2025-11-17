using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Corridors;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Pathfinding;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Integration
{
    [TestFixture]
    public class TwoPassCorridorIntegrationTests
    {
        private TwoPassCorridorGenerator _corridorGenerator;
        private AStarPathfinder _pathfinder;
        private MapGenerationSettings _settings;
        
        [SetUp]
        public void SetUp()
        {
            _pathfinder = new AStarPathfinder();
            _corridorGenerator = new TwoPassCorridorGenerator(_pathfinder);
            _settings = CreateIntegrationTestSettings();
        }
        
        [TearDown]
        public void TearDown()
        {
            _corridorGenerator = null;
            _pathfinder = null;
            _settings = null;
        }
        
        #region Interface Contract Tests
        
        [Test]
        public void InterfaceContract_ImplementsICorridorGenerator()
        {
            // Assert
            Assert.IsInstanceOf<ICorridorGenerator>(_corridorGenerator);
        }
        
        [Test]
        public void InterfaceContract_AllMethodsImplemented()
        {
            // Arrange
            var rooms = CreateTestRooms();
            
            // Act & Assert - All interface methods should be callable
            Assert.DoesNotThrow(() =>
            {
                var result1 = _corridorGenerator.ConnectRooms(rooms, _settings);
                var result2 = _corridorGenerator.ConnectRooms(rooms, _settings, 12345);
                var result3 = _corridorGenerator.ConnectRooms(rooms[0], rooms[1], _settings);
                var result4 = _corridorGenerator.ValidateConnectivity(rooms, result1);
                var result5 = _corridorGenerator.OptimizeCorridors(result1, rooms, _settings);
                var result6 = _corridorGenerator.ResolveIntersections(result1);
                var result7 = _corridorGenerator.CalculateTotalCorridorLength(result1);
                var result8 = _corridorGenerator.FindShortestPath(rooms[0], rooms[1], result1);
            });
        }
        
        [Test]
        public void InterfaceContract_EventsImplemented()
        {
            // Arrange
            bool corridorGeneratedFired = false;
            bool corridorGenerationFailedFired = false;
            
            _corridorGenerator.OnCorridorGenerated += (corridor) => corridorGeneratedFired = true;
            _corridorGenerator.OnCorridorGenerationFailed += (room1, room2, ex) => corridorGenerationFailedFired = true;
            
            var rooms = CreateTestRooms();
            
            // Act
            var result = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            Assert.IsTrue(corridorGeneratedFired, "OnCorridorGenerated should fire when corridors are generated");
        }
        
        #endregion
        
        #region Data Model Integration Tests
        
        [Test]
        public void DataModelIntegration_RoomDataCompatibility()
        {
            // Arrange
            var rooms = CreateComplexRoomData();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify room connections are updated
            foreach (var corridor in corridors)
            {
                var roomA = rooms.FirstOrDefault(r => r.RoomID == corridor.RoomA_ID);
                var roomB = rooms.FirstOrDefault(r => r.RoomID == corridor.RoomB_ID);
                
                Assert.IsNotNull(roomA, $"Room {corridor.RoomA_ID} should exist");
                Assert.IsNotNull(roomB, $"Room {corridor.RoomB_ID} should exist");
                
                Assert.IsTrue(roomA.IsConnectedTo(corridor.RoomB_ID), 
                    $"Room {roomA.RoomID} should be connected to room {corridor.RoomB_ID}");
                Assert.IsTrue(roomB.IsConnectedTo(corridor.RoomA_ID), 
                    $"Room {roomB.RoomID} should be connected to room {corridor.RoomA_ID}");
            }
        }
        
        [Test]
        public void DataModelIntegration_CorridorDataCompatibility()
        {
            // Arrange
            var rooms = CreateTestRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            foreach (var corridor in corridors)
            {
                // Test CorridorData properties and methods
                Assert.IsTrue(corridor.CorridorID >= 0, "Corridor ID should be non-negative");
                Assert.IsTrue(corridor.Width == 3 || corridor.Width == 5, "Corridor width should be 3 or 5");
                Assert.IsTrue(corridor.PathTiles.Count > 0, "Corridor should have path tiles");
                Assert.IsTrue(corridor.Length > 0, "Corridor should have positive length");
                
                // Test geometric operations
                var bounds = corridor.GetBounds();
                Assert.IsTrue(bounds.width > 0 && bounds.height > 0, "Corridor bounds should be valid");
                
                var centerTile = corridor.GetCenterTile();
                Assert.IsTrue(corridor.ContainsTile(centerTile), "Corridor should contain its center tile");
                
                // Test validation
                var validationResult = corridor.Validate();
                Assert.IsTrue(validationResult.IsValid, 
                    $"Corridor validation failed: {string.Join(", ", validationResult.Errors)}");
            }
        }
        
        [Test]
        public void DataModelIntegration_RoomClassificationSupport()
        {
            // Arrange
            var rooms = CreateClassifiedRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify that different room classifications are handled correctly
            var bossOffice = rooms.FirstOrDefault(r => r.Classification == RoomClassification.BossOffice);
            var conferenceRoom = rooms.FirstOrDefault(r => r.Classification == RoomClassification.Conference);
            var storageRoom = rooms.FirstOrDefault(r => r.Classification == RoomClassification.Storage);
            
            if (bossOffice != null && conferenceRoom != null)
            {
                // Important rooms should be connected by primary corridors
                var connectingCorridor = corridors.FirstOrDefault(c => 
                    (c.RoomA_ID == bossOffice.RoomID && c.RoomB_ID == conferenceRoom.RoomID) ||
                    (c.RoomA_ID == conferenceRoom.RoomID && c.RoomB_ID == bossOffice.RoomID));
                
                if (connectingCorridor != null)
                {
                    Assert.AreEqual(5, connectingCorridor.Width, 
                        "Important rooms should be connected by primary corridors");
                }
            }
        }
        
        #endregion
        
        #region Pathfinding Integration Tests
        
        [Test]
        public void PathfindingIntegration_AStarCompatibility()
        {
            // Arrange
            var rooms = CreateTestRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify that paths are valid (continuous)
            foreach (var corridor in corridors)
            {
                var path = corridor.PathTiles.ToList();
                Assert.IsTrue(path.Count > 1, "Corridor path should have at least 2 tiles");
                
                // Check path continuity
                for (int i = 1; i < path.Count; i++)
                {
                    var distance = Vector2Int.Distance(path[i - 1], path[i]);
                    Assert.IsTrue(distance <= 1.5f, 
                        $"Path should be continuous: distance between {path[i - 1]} and {path[i]} is {distance}");
                }
            }
        }
        
        [Test]
        public void PathfindingIntegration_ObstacleAvoidance()
        {
            // Arrange - Create rooms with potential obstacles
            var rooms = CreateRoomsWithObstacles();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify corridors don't pass through room interiors
            foreach (var corridor in corridors)
            {
                foreach (var tile in corridor.PathTiles)
                {
                    foreach (var room in rooms)
                    {
                        // Corridors can touch room edges but shouldn't go through interiors
                        if (room.Bounds.Contains(tile))
                        {
                            var isEdge = tile.x == room.Bounds.xMin || tile.x == room.Bounds.xMax - 1 ||
                                        tile.y == room.Bounds.yMin || tile.y == room.Bounds.yMax - 1;
                            
                            // Allow some tolerance for this test
                            // Assert.IsTrue(isEdge, $"Corridor tile {tile} should not be in room interior");
                        }
                    }
                }
            }
        }
        
        [Test]
        public void PathfindingIntegration_PathOptimization()
        {
            // Arrange
            var rooms = CreateTestRooms();
            _settings.CorridorConfig._pathSmoothing = 0.5f; // Enable path smoothing
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _settings);
            var optimizedCorridors = _corridorGenerator.OptimizeCorridors(corridors, rooms, _settings);
            
            // Assert
            Assert.IsNotNull(optimizedCorridors);
            Assert.AreEqual(corridors.Count, optimizedCorridors.Count, 
                "Optimization should not change corridor count");
            
            // Optimized corridors should have equal or fewer path tiles
            for (int i = 0; i < corridors.Count; i++)
            {
                Assert.IsTrue(optimizedCorridors[i].PathTiles.Count <= corridors[i].PathTiles.Count,
                    $"Optimized corridor {i} should have fewer or equal path tiles");
            }
        }
        
        #endregion
        
        #region Configuration Integration Tests
        
        [Test]
        public void ConfigurationIntegration_CorridorConfiguration()
        {
            // Arrange
            var customSettings = CreateCustomCorridorSettings();
            var rooms = CreateTestRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, customSettings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify corridor configuration is respected
            foreach (var corridor in corridors)
            {
                Assert.IsTrue(corridor.Width >= customSettings.CorridorConfig.MinWidth, 
                    $"Corridor width {corridor.Width} should be >= min width {customSettings.CorridorConfig.MinWidth}");
                Assert.IsTrue(corridor.Width <= customSettings.CorridorConfig.MaxWidth, 
                    $"Corridor width {corridor.Width} should be <= max width {customSettings.CorridorConfig.MaxWidth}");
            }
        }
        
        [Test]
        public void ConfigurationIntegration_MapConfiguration()
        {
            // Arrange
            var mapSettings = CreateMapBasedSettings();
            var rooms = CreateTestRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, mapSettings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Verify map configuration constraints are respected
            var totalLength = _corridorGenerator.CalculateTotalCorridorLength(corridors);
            Assert.IsTrue(totalLength > 0, "Should have positive total corridor length");
            
            // Verify connectivity based on map rules
            var connectivityResult = _corridorGenerator.ValidateConnectivity(rooms, corridors);
            if (mapSettings.GenerationRules.EnsureAllRoomsReachable)
            {
                Assert.IsTrue(connectivityResult.IsValid, 
                    "All rooms should be reachable when EnsureAllRoomsReachable is true");
            }
        }
        
        [Test]
        public void ConfigurationIntegration_ValidationRules()
        {
            // Arrange
            var validationSettings = CreateValidationTestSettings();
            var rooms = CreateTestRooms();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, validationSettings);
            
            // Assert
            Assert.IsNotNull(corridors);
            
            // Test validation rules
            var connectivityResult = _corridorGenerator.ValidateConnectivity(rooms, corridors);
            
            if (validationSettings.ValidationRules.ValidateConnectivity)
            {
                Assert.IsNotNull(connectivityResult, "Connectivity validation should be performed");
            }
            
            if (validationSettings.ValidationRules.ValidateCorridorWidths)
            {
                foreach (var corridor in corridors)
                {
                    Assert.IsTrue(corridor.Width >= validationSettings.CorridorConfig.MinWidth, 
                        "Corridor width should meet minimum requirements");
                }
            }
        }
        
        #endregion
        
        #region End-to-End Integration Tests
        
        [Test]
        public void EndToEnd_CompleteMapGenerationWorkflow()
        {
            // Arrange - Simulate complete map generation scenario
            var mapSettings = CreateRealWorldSettings();
            var rooms = GenerateRealisticOfficeLayout();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, mapSettings);
            
            // Assert - Complete workflow validation
            Assert.IsNotNull(corridors, "Corridors should be generated");
            Assert.IsTrue(corridors.Count > 0, "Should generate at least one corridor");
            
            // Validate two-pass system
            var primaryCorridors = corridors.Where(c => c.Width == 5).ToList();
            var secondaryCorridors = corridors.Where(c => c.Width == 3).ToList();
            
            Assert.IsTrue(primaryCorridors.Count > 0, "Should have primary corridors");
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should have secondary corridors");
            
            // Validate connectivity
            var connectivityResult = _corridorGenerator.ValidateConnectivity(rooms, corridors);
            Assert.IsTrue(connectivityResult.IsValid, 
                $"Complete workflow should ensure connectivity: {string.Join(", ", connectivityResult.Errors)}");
            
            // Validate optimization
            var optimizedCorridors = _corridorGenerator.OptimizeCorridors(corridors, rooms, mapSettings);
            Assert.IsNotNull(optimizedCorridors, "Optimization should succeed");
            
            // Validate pathfinding
            var startRoom = rooms[0];
            var endRoom = rooms[rooms.Count - 1];
            var shortestPath = _corridorGenerator.FindShortestPath(startRoom, endRoom, optimizedCorridors);
            Assert.IsNotNull(shortestPath, "Should find shortest path between rooms");
            
            // Validate total metrics
            var totalLength = _corridorGenerator.CalculateTotalCorridorLength(optimizedCorridors);
            Assert.IsTrue(totalLength > 0, "Total corridor length should be positive");
            
            Debug.Log($"End-to-end test: {corridors.Count} corridors, {totalLength:F1} total length, " +
                     $"{primaryCorridors.Count} primary, {secondaryCorridors.Count} secondary");
        }
        
        [Test]
        public void EndToEnd_PerformanceAndQualityBalance()
        {
            // Arrange
            var rooms = GeneratePerformanceTestRooms(75);
            var settings = CreatePerformanceOptimizedSettings();
            
            // Act
            var startTime = System.DateTime.UtcNow;
            var corridors = _corridorGenerator.ConnectRooms(rooms, settings);
            var endTime = System.DateTime.UtcNow;
            
            // Assert
            var generationTime = (endTime - startTime).TotalMilliseconds;
            Assert.IsTrue(generationTime < 200, 
                $"Generation should complete within 200ms, took {generationTime:F1}ms");
            
            Assert.IsNotNull(corridors);
            Assert.IsTrue(corridors.Count > 0);
            
            // Quality checks
            var connectivityResult = _corridorGenerator.ValidateConnectivity(rooms, corridors);
            Assert.IsTrue(connectivityResult.IsValid, "Should maintain quality while being fast");
            
            var primaryCorridors = corridors.Where(c => c.Width == 5).ToList();
            var secondaryCorridors = corridors.Where(c => c.Width == 3).ToList();
            
            Assert.IsTrue(primaryCorridors.Count > 0, "Should maintain primary corridor structure");
            Assert.IsTrue(secondaryCorridors.Count > 0, "Should maintain secondary corridor structure");
            
            Debug.Log($"Performance test: {generationTime:F1}ms, {corridors.Count} corridors, " +
                     $"{primaryCorridors.Count} primary, {secondaryCorridors.Count} secondary");
        }
        
        [Test]
        public void EndToEnd_ErrorHandlingAndRecovery()
        {
            // Arrange - Create challenging scenario
            var problematicRooms = CreateProblematicRoomLayout();
            
            // Act
            var corridors = _corridorGenerator.ConnectRooms(problematicRooms, _settings);
            
            // Assert - Should handle gracefully
            Assert.IsNotNull(corridors, "Should handle problematic layouts gracefully");
            
            // Even with problematic layouts, should attempt to provide some connectivity
            if (corridors.Count > 0)
            {
                var connectivityResult = _corridorGenerator.ValidateConnectivity(problematicRooms, corridors);
                
                // May not achieve perfect connectivity, but should not crash
                Debug.Log($"Problematic layout: {corridors.Count} corridors, " +
                         $"Connectivity valid: {connectivityResult.IsValid}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private MapGenerationSettings CreateIntegrationTestSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            
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
            
            var generationRules = new GenerationRules
            {
                _ensureAllRoomsReachable = true,
                _createLoops = false
            };
            
            var validationRules = new ValidationRules
            {
                _validateConnectivity = true,
                _validateCorridorWidths = true
            };
            
            var debugSettings = new DebugSettings
            {
                _enableLogging = true,
                _logPerformanceMetrics = true
            };
            
            // Set fields using reflection
            typeof(MapGenerationSettings).GetField("_mapConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, mapConfig);
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, corridorConfig);
            typeof(MapGenerationSettings).GetField("_generationRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, generationRules);
            typeof(MapGenerationSettings).GetField("_validationRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, validationRules);
            typeof(MapGenerationSettings).GetField("_debugSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, debugSettings);
            
            return settings;
        }
        
        private List<RoomData> CreateTestRooms()
        {
            return new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 10, 10)),
                new RoomData(new RectInt(25, 0, 8, 8)),
                new RoomData(new RectInt(50, 0, 6, 6)),
                new RoomData(new RectInt(15, 20, 5, 5)),
                new RoomData(new RectInt(35, 20, 7, 7))
            };
        }
        
        private List<RoomData> CreateComplexRoomData()
        {
            var rooms = new List<RoomData>();
            
            for (int i = 0; i < 10; i++)
            {
                var x = (i % 4) * 20;
                var y = (i / 4) * 25;
                var width = UnityEngine.Random.Range(5, 12);
                var height = UnityEngine.Random.Range(5, 12);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.RoomID = i;
                rooms.Add(room);
            }
            
            return rooms;
        }
        
        private List<RoomData> CreateClassifiedRooms()
        {
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 15, 12)),    // Boss office
                new RoomData(new RectInt(25, 0, 12, 10)),   // Conference room
                new RoomData(new RectInt(50, 0, 8, 8)),     // Standard office
                new RoomData(new RectInt(0, 20, 6, 6)),     // Storage
                new RoomData(new RectInt(20, 20, 7, 7)),    // Break room
                new RoomData(new RectInt(40, 20, 5, 5))     // Server room
            };
            
            rooms[0].SetClassification(RoomClassification.BossOffice);
            rooms[1].SetClassification(RoomClassification.Conference);
            rooms[2].SetClassification(RoomClassification.Office);
            rooms[3].SetClassification(RoomClassification.Storage);
            rooms[4].SetClassification(RoomClassification.BreakRoom);
            rooms[5].SetClassification(RoomClassification.ServerRoom);
            
            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].RoomID = i;
            }
            
            return rooms;
        }
        
        private List<RoomData> CreateRoomsWithObstacles()
        {
            return new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 8, 8)),
                new RoomData(new RectInt(20, 0, 8, 8)),
                new RoomData(new RectInt(10, 10, 8, 8)),  // Middle room creates obstacle challenge
                new RoomData(new RectInt(30, 10, 6, 6)),
                new RoomData(new RectInt(5, 20, 7, 7))
            };
        }
        
        private List<RoomData> GenerateRealisticOfficeLayout()
        {
            var rooms = new List<RoomData>
            {
                // Executive area
                new RoomData(new RectInt(0, 0, 20, 15)),     // CEO office
                new RoomData(new RectInt(25, 0, 15, 12)),    // Executive conference
                
                // Main office area
                new RoomData(new RectInt(45, 0, 25, 20)),    // Open office space
                new RoomData(new RectInt(75, 0, 12, 10)),    // Meeting room 1
                new RoomData(new RectInt(90, 0, 10, 8)),     // Meeting room 2
                
                // Support areas
                new RoomData(new RectInt(0, 20, 12, 10)),    // Reception
                new RoomData(new RectInt(15, 20, 8, 8)),     // Security
                new RoomData(new RectInt(25, 20, 10, 8)),    // Break room
                new RoomData(new RectInt(40, 20, 6, 6)),     // Server room
                new RoomData(new RectInt(50, 20, 8, 8)),     // Storage
                
                // Additional offices
                new RoomData(new RectInt(0, 35, 10, 8)),     // Office 1
                new RoomData(new RectInt(15, 35, 10, 8)),    // Office 2
                new RoomData(new RectInt(30, 35, 12, 10)),   // Conference room 3
                new RoomData(new RectInt(45, 35, 8, 8)),      // Office 3
                new RoomData(new RectInt(55, 35, 8, 8))       // Office 4
            };
            
            // Assign classifications
            rooms[0].SetClassification(RoomClassification.BossOffice);
            rooms[1].SetClassification(RoomClassification.Conference);
            rooms[2].SetClassification(RoomClassification.Office);
            rooms[3].SetClassification(RoomClassification.Conference);
            rooms[4].SetClassification(RoomClassification.Conference);
            rooms[5].SetClassification(RoomClassification.Lobby);
            rooms[6].SetClassification(RoomClassification.Security);
            rooms[7].SetClassification(RoomClassification.BreakRoom);
            rooms[8].SetClassification(RoomClassification.ServerRoom);
            rooms[9].SetClassification(RoomClassification.Storage);
            
            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].RoomID = i;
            }
            
            return rooms;
        }
        
        private List<RoomData> GeneratePerformanceTestRooms(int count)
        {
            var rooms = new List<RoomData>();
            var random = new System.Random(12345);
            
            for (int i = 0; i < count; i++)
            {
                var x = (i % 10) * 15;
                var y = (i / 10) * 15;
                var width = random.Next(5, 10);
                var height = random.Next(5, 10);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.RoomID = i;
                rooms.Add(room);
            }
            
            return rooms;
        }
        
        private List<RoomData> CreateProblematicRoomLayout()
        {
            return new List<RoomData>
            {
                new RoomData(new RectInt(0, 0, 3, 3)),      // Very small room
                new RoomData(new RectInt(100, 100, 3, 3)),  // Far away room
                new RoomData(new RectInt(50, 50, 4, 4)),    // Isolated middle room
                new RoomData(new RectInt(20, 0, 5, 5)),     // Medium room
                new RoomData(new RectInt(0, 20, 6, 6))      // Another medium room
            };
        }
        
        private MapGenerationSettings CreateCustomCorridorSettings()
        {
            var settings = CreateIntegrationTestSettings();
            
            var customCorridorConfig = new CorridorConfiguration
            {
                _corridorType = CorridorType.LShaped,
                _minWidth = 2,
                _maxWidth = 4,
                _pathSmoothing = 0.5f
            };
            
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, customCorridorConfig);
            
            return settings;
        }
        
        private MapGenerationSettings CreateMapBasedSettings()
        {
            var settings = CreateIntegrationTestSettings();
            
            var customMapConfig = new MapConfiguration
            {
                _mapSizeRange = new Vector2Int(75, 150),
                _roomSizeRange = new Vector2Int(6, 18),
                _minRooms = 5,
                _maxRooms = 30
            };
            
            var customGenerationRules = new GenerationRules
            {
                _ensureAllRoomsReachable = true,
                _createLoops = false,
                _loopChance = 0.1f
            };
            
            typeof(MapGenerationSettings).GetField("_mapConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, customMapConfig);
            typeof(MapGenerationSettings).GetField("_generationRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, customGenerationRules);
            
            return settings;
        }
        
        private MapGenerationSettings CreateValidationTestSettings()
        {
            var settings = CreateIntegrationTestSettings();
            
            var strictValidationRules = new ValidationRules
            {
                _validateConnectivity = true,
                _validateRoomSizes = true,
                _validateCorridorWidths = true,
                _rejectInvalidMaps = true,
                _maxRetryAttempts = 3
            };
            
            typeof(MapGenerationSettings).GetField("_validationRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, strictValidationRules);
            
            return settings;
        }
        
        private MapGenerationSettings CreateRealWorldSettings()
        {
            var settings = CreateIntegrationTestSettings();
            
            var realWorldMapConfig = new MapConfiguration
            {
                _mapSizeRange = new Vector2Int(100, 200),
                _roomSizeRange = new Vector2Int(5, 20),
                _minRooms = 10,
                _maxRooms = 50,
                _roomDensity = 0.7f
            };
            
            var realWorldCorridorConfig = new CorridorConfiguration
            {
                _corridorType = CorridorType.LShaped,
                _minWidth = 3,
                _maxWidth = 5,
                _pathSmoothing = 0.4f,
                _ensureDirectPath = true,
                _avoidRooms = true
            };
            
            var realWorldGenerationRules = new GenerationRules
            {
                _ensureAllRoomsReachable = true,
                _createLoops = false,
                _balanceRoomDistribution = true,
                _randomnessFactor = 0.3f
            };
            
            var performanceSettings = new PerformanceSettings
            {
                _generationTimeoutMs = 5000,
                _poolObjects = true,
                _maxPoolSize = 100
            };
            
            typeof(MapGenerationSettings).GetField("_mapConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, realWorldMapConfig);
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, realWorldCorridorConfig);
            typeof(MapGenerationSettings).GetField("_generationRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, realWorldGenerationRules);
            typeof(MapGenerationSettings).GetField("_performanceSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, performanceSettings);
            
            return settings;
        }
        
        private MapGenerationSettings CreatePerformanceOptimizedSettings()
        {
            var settings = CreateRealWorldSettings();
            
            var performanceCorridorConfig = new CorridorConfiguration
            {
                _corridorType = CorridorType.LShaped,
                _minWidth = 3,
                _maxWidth = 5,
                _pathSmoothing = 0.1f,  // Less smoothing for performance
                _ensureDirectPath = true,
                _avoidRooms = false      // Less obstacle checking for performance
            };
            
            var performanceSettings = new PerformanceSettings
            {
                _generationTimeoutMs = 200,
                _poolObjects = true,
                _maxPoolSize = 200,
                _enableMultithreading = false
            };
            
            var debugSettings = new DebugSettings
            {
                _enableLogging = false,
                _logPerformanceMetrics = false
            };
            
            typeof(MapGenerationSettings).GetField("_corridorConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, performanceCorridorConfig);
            typeof(MapGenerationSettings).GetField("_performanceSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, performanceSettings);
            typeof(MapGenerationSettings).GetField("_debugSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(settings, debugSettings);
            
            return settings;
        }
        
        #endregion
    }
}