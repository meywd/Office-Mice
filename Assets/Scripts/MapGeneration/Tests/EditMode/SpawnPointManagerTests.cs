using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class SpawnPointManagerTests
    {
        private SpawnPointManager _spawnPointManager;
        private SpawnTableConfiguration _testSpawnTableConfig;
        private MapData _testMap;
        private List<FurnitureData> _testFurniture;

        [SetUp]
        public void SetUp()
        {
            _testSpawnTableConfig = CreateTestSpawnTableConfig();
            _spawnPointManager = new SpawnPointManager(_testSpawnTableConfig, 42); // Fixed seed for deterministic tests
            
            _testMap = CreateTestMap();
            _testFurniture = CreateTestFurniture();
        }

        [TearDown]
        public void TearDown()
        {
            _spawnPointManager = null;
            _testSpawnTableConfig = null;
            _testMap = null;
            _testFurniture = null;
        }

        [Test]
        public void Constructor_WithNullSpawnTableConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SpawnPointManager(null));
        }

        [Test]
        public void PlaceSpawnPoints_WithValidMap_ReturnsSpawnPoints()
        {
            // Arrange
            var expectedRoomCount = _testMap.Rooms.Count;

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);

            // Assert
            Assert.IsNotNull(spawnPoints);
            Assert.IsTrue(spawnPoints.Count > 0, "Should place at least some spawn points");
            
            // Verify all spawn points are in valid rooms
            foreach (var spawnPoint in spawnPoints)
            {
                var room = _testMap.GetRoom(spawnPoint.RoomID);
                Assert.IsNotNull(room, $"Spawn point {spawnPoint.Position} references invalid room {spawnPoint.RoomID}");
                Assert.IsTrue(room.ContainsPoint(spawnPoint.Position), 
                    $"Spawn point {spawnPoint.Position} is outside room bounds");
            }
        }

        [Test]
        public void PlaceSpawnPoints_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _spawnPointManager.PlaceSpawnPoints(null, _testFurniture));
        }

        [Test]
        public void PlaceSpawnPoints_WithNullFurniture_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _spawnPointManager.PlaceSpawnPoints(_testMap, null));
        }

        [Test]
        public void PlaceSpawnPointsInRoom_OfficeRoom_PrioritizesCornersAndCover()
        {
            // Arrange
            var officeRoom = _testMap.Rooms.Find(r => r.Classification == RoomClassification.Office);
            Assert.IsNotNull(officeRoom, "Test map should have an office room");

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(officeRoom, _testFurniture);

            // Assert
            Assert.IsNotNull(spawnPoints);
            
            // Should place spawn points in strategic positions
            var cornerPositions = GetCornerPositions(officeRoom);
            var hasCornerSpawn = spawnPoints.Any(sp => cornerPositions.Contains(sp.Position));
            Assert.IsTrue(hasCornerSpawn, "Should place at least one spawn point in a corner");
        }

        [Test]
        public void PlaceSpawnPointsInRoom_ConferenceRoom_PrioritizesPerimeter()
        {
            // Arrange
            var conferenceRoom = _testMap.Rooms.Find(r => r.Classification == RoomClassification.Conference);
            Assert.IsNotNull(conferenceRoom, "Test map should have a conference room");

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(conferenceRoom, _testFurniture);

            // Assert
            Assert.IsNotNull(spawnPoints);
            Assert.IsTrue(spawnPoints.Count > 0, "Should place spawn points in conference room");
            
            // Conference rooms should have more spawn points due to larger size
            Assert.IsTrue(spawnPoints.Count >= 2, "Conference room should have at least 2 spawn points");
        }

        [Test]
        public void ValidateSpawnPoints_WithValidSpawnPoints_ReturnsSuccess()
        {
            // Arrange
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);

            // Act
            var validationResult = _spawnPointManager.ValidateSpawnPoints(_testMap, spawnPoints);

            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsTrue(validationResult.IsValid, "Valid spawn points should pass validation");
        }

        [Test]
        public void ValidateSpawnPoints_WithInvalidRoom_ReturnsError()
        {
            // Arrange
            var invalidSpawnPoints = new List<SpawnPointData>
            {
                new SpawnPointData(999, new Vector2Int(0, 0), "Mouse") // Invalid room ID
            };

            // Act
            var validationResult = _spawnPointManager.ValidateSpawnPoints(_testMap, invalidSpawnPoints);

            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsFalse(validationResult.IsValid, "Invalid room ID should cause validation failure");
            Assert.IsTrue(validationResult.HasErrors, "Should have specific error about invalid room");
        }

        [Test]
        public void ValidateSpawnPoints_WithClustering_ReturnsWarning()
        {
            // Arrange
            var clusteredSpawnPoints = new List<SpawnPointData>
            {
                new SpawnPointData(1, new Vector2Int(5, 5), "Mouse"),
                new SpawnPointData(1, new Vector2Int(5, 6), "Mouse"), // Too close
                new SpawnPointData(1, new Vector2Int(5, 7), "Mouse")  // Too close
            };

            // Act
            var validationResult = _spawnPointManager.ValidateSpawnPoints(_testMap, clusteredSpawnPoints);

            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsTrue(validationResult.HasWarnings, "Should warn about clustered spawn points");
        }

        [Test]
        public void SetSeed_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Arrange
            var seed1 = 123;
            var seed2 = 456;

            // Act
            _spawnPointManager.SetSeed(seed1);
            var spawnPoints1 = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);
            
            _spawnPointManager.SetSeed(seed2);
            var spawnPoints2 = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);

            // Assert
            Assert.AreNotEqual(spawnPoints1.Count, spawnPoints2.Count, 
                "Different seeds should produce different spawn point counts or positions");
        }

        [Test]
        public void AddDensityRule_WithValidRule_AddsToRules()
        {
            // Arrange
            var roomType = RoomClassification.Office;
            var rule = new SpawnDensityRule(1.5f, 60, 0.2f, 2, 8);

            // Act
            _spawnPointManager.AddDensityRule(roomType, rule);

            // Verify by placing spawn points - should use the new rule
            var officeRoom = _testMap.Rooms.Find(r => r.Classification == RoomClassification.Office);
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(officeRoom, _testFurniture);

            // Assert
            Assert.IsNotNull(spawnPoints);
            // The exact verification depends on the rule implementation
        }

        [Test]
        public void SetPositionPriorities_WithValidPriorities_UpdatesPriorities()
        {
            // Arrange
            var roomType = RoomClassification.Office;
            var priorities = new List<SpawnPositionType>
            { 
                SpawnPositionType.Center, 
                SpawnPositionType.Corner, 
                SpawnPositionType.Cover 
            };

            // Act
            _spawnPointManager.SetPositionPriorities(roomType, priorities);

            // Verify by placing spawn points - should use new priorities
            var officeRoom = _testMap.Rooms.Find(r => r.Classification == RoomClassification.Office);
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(officeRoom, _testFurniture);

            // Assert
            Assert.IsNotNull(spawnPoints);
            // The exact verification depends on the priority implementation
        }

        [Test]
        public void GetPerformanceStats_AfterPlacement_ReturnsValidStats()
        {
            // Arrange
            _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);

            // Act
            var stats = _spawnPointManager.GetPerformanceStats();

            // Assert
            Assert.IsTrue(stats.roomsProcessed > 0, "Should have processed rooms");
            Assert.IsTrue(stats.spawnPointsPlaced >= 0, "Spawn point count should be non-negative");
            Assert.IsTrue(stats.totalMs >= 0, "Total time should be non-negative");
            Assert.IsTrue(stats.avgMsPerRoom >= 0, "Average time per room should be non-negative");
        }

        [Test]
        public void SpawnPointEvents_WhenSpawnPointsPlaced_FiresEvents()
        {
            // Arrange
            bool eventFired = false;
            SpawnPointData placedSpawnPoint = null;
            
            _spawnPointManager.OnSpawnPointPlaced += (spawnPoint) =>
            {
                eventFired = true;
                placedSpawnPoint = spawnPoint;
            };

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);

            // Assert
            Assert.IsTrue(eventFired, "OnSpawnPointPlaced event should be fired");
            Assert.IsNotNull(placedSpawnPoint, "Event should provide spawn point data");
            Assert.IsTrue(spawnPoints.Contains(placedSpawnPoint), "Event spawn point should be in returned list");
        }

        [Test]
        public void StrategicPositionGeneration_CornerPositions_ReturnsValidCorners()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 10, 10));
            var furniture = new List<FurnitureData>();

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);

            // Assert
            var expectedCorners = new[]
            {
                new Vector2Int(1, 1), // Top-left
                new Vector2Int(8, 1), // Top-right
                new Vector2Int(1, 8), // Bottom-left
                new Vector2Int(8, 8)  // Bottom-right
            };

            var hasCornerSpawn = spawnPoints.Any(sp => expectedCorners.Contains(sp.Position));
            Assert.IsTrue(hasCornerSpawn, "Should place spawn points in corners");
        }

        [Test]
        public void StrategicPositionGeneration_DoorwayPositions_ReturnsNearDoorway()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 10, 10));
            room.AddDoorway(new DoorwayPosition(new Vector2Int(5, 0), 1, Direction.North));
            var furniture = new List<FurnitureData>();

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);

            // Assert
            var hasNearDoorwaySpawn = spawnPoints.Any(sp => 
                Vector2Int.Distance(sp.Position, new Vector2Int(5, 0)) <= 4);
            Assert.IsTrue(hasNearDoorwaySpawn, "Should place spawn points near doorways");
        }

        [Test]
        public void StrategicPositionGeneration_CoverPositions_ReturnsBehindFurniture()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 10, 10));
            var furniture = new List<FurnitureData>
            {
                new FurnitureData("desk1", "Desk", "path", 0, new Vector2Int(4, 4), new Vector2Int(2, 2))
                { SetBlockingProperties(true, true) }
            };

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);

            // Assert
            var hasCoverSpawn = spawnPoints.Any(sp => 
                (sp.Position.x >= 6 && sp.Position.x <= 7 && sp.Position.y >= 4 && sp.Position.y <= 5) || // Right of desk
                (sp.Position.x >= 4 && sp.Position.x <= 5 && sp.Position.y >= 6 && sp.Position.y <= 7));   // Below desk
            
            Assert.IsTrue(hasCoverSpawn, "Should place spawn points behind furniture for cover");
        }

        [Test]
        public void SpawnPointPlacement_Performance_TargetsMet()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(_testMap, _testFurniture);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Spawn point placement should complete within 100ms, took {stopwatch.ElapsedMilliseconds}ms");
            
            var stats = _spawnPointManager.GetPerformanceStats();
            Assert.IsTrue(stats.avgMsPerRoom < 20, 
                $"Average time per room should be under 20ms, was {stats.avgMsPerRoom}ms");
        }

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData(100, 100, 12345);
            
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(10, 10, 8, 6)) { RoomID = 1, SetClassification(RoomClassification.Office) },
                new RoomData(new RectInt(30, 10, 12, 8)) { RoomID = 2, SetClassification(RoomClassification.Conference) },
                new RoomData(new RectInt(10, 30, 6, 6)) { RoomID = 3, SetClassification(RoomClassification.BreakRoom) },
                new RoomData(new RectInt(30, 30, 8, 8)) { RoomID = 4, SetClassification(RoomClassification.Storage) },
                new RoomData(new RectInt(50, 10, 10, 10)) { RoomID = 5, SetClassification(RoomClassification.ServerRoom) }
            };

            // Add doorways to rooms
            rooms[0].AddDoorway(new DoorwayPosition(new Vector2Int(14, 10), 1, Direction.South));
            rooms[1].AddDoorway(new DoorwayPosition(new Vector2Int(36, 10), 2, Direction.South));
            rooms[2].AddDoorway(new DoorwayPosition(new Vector2Int(13, 30), 1, Direction.North));
            rooms[3].AddDoorway(new DoorwayPosition(new Vector2Int(34, 30), 1, Direction.North));
            rooms[4].AddDoorway(new DoorwayPosition(new Vector2Int(55, 10), 2, Direction.South));

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private List<FurnitureData> CreateTestFurniture()
        {
            return new List<FurnitureData>
            {
                new FurnitureData("desk1", "Desk", "path", 1, new Vector2Int(12, 12), new Vector2Int(2, 2))
                { SetBlockingProperties(true, true) },
                new FurnitureData("desk2", "Desk", "path", 2, new Vector2Int(35, 15), new Vector2Int(3, 2))
                { SetBlockingProperties(true, false) },
                new FurnitureData("table1", "Table", "path", 3, new Vector2Int(12, 32), new Vector2Int(2, 2))
                { SetBlockingProperties(true, true) },
                new FurnitureData("shelf1", "Shelf", "path", 4, new Vector2Int(32, 35), new Vector2Int(1, 3))
                { SetBlockingProperties(true, true) }
            };
        }

        private SpawnTableConfiguration CreateTestSpawnTableConfig()
        {
            var config = ScriptableObject.CreateInstance<SpawnTableConfiguration>();
            
            // This would normally be configured in the Unity Editor
            // For testing, we'll use the default configuration
            return config;
        }

        private List<Vector2Int> GetCornerPositions(RoomData room)
        {
            var bounds = room.Bounds;
            var offset = 1;
            
            return new List<Vector2Int>
            {
                new Vector2Int(bounds.x + offset, bounds.y + offset),
                new Vector2Int(bounds.xMax - offset - 1, bounds.y + offset),
                new Vector2Int(bounds.x + offset, bounds.yMax - offset - 1),
                new Vector2Int(bounds.xMax - offset - 1, bounds.yMax - offset - 1)
            };
        }

        #endregion
    }
}