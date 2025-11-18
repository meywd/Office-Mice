using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Configuration;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class SpawnPointPerformanceTests
    {
        private SpawnPointManager _spawnPointManager;
        private SpawnTableConfiguration _testSpawnTableConfig;

        [SetUp]
        public void SetUp()
        {
            _testSpawnTableConfig = CreateTestSpawnTableConfig();
            _spawnPointManager = new SpawnPointManager(_testSpawnTableConfig, 42);
        }

        [TearDown]
        public void TearDown()
        {
            _spawnPointManager = null;
            _testSpawnTableConfig = null;
        }

        [Test]
        [Category("Performance")]
        public void PlaceSpawnPoints_SmallMap_PerformanceTarget()
        {
            // Arrange
            var smallMap = CreateMapWithRooms(10); // 10 rooms
            var furniture = CreateTestFurniture(smallMap);
            var targetTimeMs = 30; // Target: <30ms for small maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(smallMap, furniture);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Small map spawn point placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(spawnPoints.Count, 0, "Should place spawn points in small map");
            
            var stats = _spawnPointManager.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 5, 
                $"Average time per room should be under 5ms for small maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void PlaceSpawnPoints_MediumMap_PerformanceTarget()
        {
            // Arrange
            var mediumMap = CreateMapWithRooms(50); // 50 rooms
            var furniture = CreateTestFurniture(mediumMap);
            var targetTimeMs = 100; // Target: <100ms for medium maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(mediumMap, furniture);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Medium map spawn point placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(spawnPoints.Count, 0, "Should place spawn points in medium map");
            
            var stats = _spawnPointManager.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 3, 
                $"Average time per room should be under 3ms for medium maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void PlaceSpawnPoints_LargeMap_PerformanceTarget()
        {
            // Arrange
            var largeMap = CreateMapWithRooms(100); // 100 rooms
            var furniture = CreateTestFurniture(largeMap);
            var targetTimeMs = 150; // Target: <150ms for large maps

            // Act
            var stopwatch = Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(largeMap, furniture);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Large map spawn point placement should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(spawnPoints.Count, 0, "Should place spawn points in large map");
            
            var stats = _spawnPointManager.GetPerformanceStats();
            Assert.Less(stats.avgMsPerRoom, 2, 
                $"Average time per room should be under 2ms for large maps, was {stats.avgMsPerRoom}ms");
        }

        [Test]
        [Category("Performance")]
        public void StrategicPositionGeneration_PerformanceTarget()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 20, 20));
            var furniture = CreateTestFurnitureForRoom(room);
            var targetTimeMs = 5; // Target: <5ms for strategic position generation

            // Act
            var stopwatch = Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Strategic position generation should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(spawnPoints.Count, 0, "Should generate strategic positions");
        }

        [Test]
        [Category("Performance")]
        public void SpawnPointValidation_PerformanceTarget()
        {
            // Arrange
            var map = CreateMapWithRooms(50);
            var furniture = CreateTestFurniture(map);
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);
            var targetTimeMs = 20; // Target: <20ms for validation

            // Act
            var stopwatch = Stopwatch.StartNew();
            var validationResult = _spawnPointManager.ValidateSpawnPoints(map, spawnPoints);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Spawn point validation should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsNotNull(validationResult, "Should return validation result");
        }

        [Test]
        [Category("Performance")]
        public void MemoryUsage_SpawnPointPlacement_WithinLimits()
        {
            // Arrange
            var map = CreateMapWithRooms(50);
            var furniture = CreateTestFurniture(map);
            
            // Force garbage collection to get baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);
            
            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.Less(memoryIncrease, 5 * 1024 * 1024, // 5MB limit
                $"Spawn point placement should use less than 5MB memory, used {memoryIncrease / (1024f * 1024f):F2}MB");
            
            // Memory per spawn point should be reasonable
            var memoryPerSpawnPoint = spawnPoints.Count > 0 ? memoryIncrease / spawnPoints.Count : 0;
            Assert.Less(memoryPerSpawnPoint, 512, // 512 bytes per spawn point
                $"Memory per spawn point should be less than 512 bytes, was {memoryPerSpawnPoint} bytes");
        }

        [Test]
        [Category("Performance")]
        public void Scalability_RoomCount_LinearPerformance()
        {
            // Arrange
            var roomCounts = new[] { 10, 25, 50, 75, 100 };
            var timesPerRoom = new List<float>();

            // Act
            foreach (var roomCount in roomCounts)
            {
                var map = CreateMapWithRooms(roomCount);
                var furniture = CreateTestFurniture(map);
                
                var stopwatch = Stopwatch.StartNew();
                _spawnPointManager.PlaceSpawnPoints(map, furniture);
                stopwatch.Stop();
                
                var timePerRoom = (float)stopwatch.ElapsedMilliseconds / roomCount;
                timesPerRoom.Add(timePerRoom);
            }

            // Assert - Check that performance scales reasonably (not exponentially)
            for (int i = 1; i < timesPerRoom.Count; i++)
            {
                var ratio = timesPerRoom[i] / timesPerRoom[0];
                Assert.Less(ratio, 2.5f, 
                    $"Time per room should not increase by more than 2.5x from {roomCounts[0]} to {roomCounts[i]} rooms. " +
                    $"Was {timesPerRoom[0]:F2}ms -> {timesPerRoom[i]:F2}ms (ratio: {ratio:F2})");
            }
        }

        [Test]
        [Category("Performance")]
        public void RepeatedPlacement_ConsistentPerformance()
        {
            // Arrange
            var map = CreateMapWithRooms(25);
            var furniture = CreateTestFurniture(map);
            var iterations = 10;
            var times = new List<long>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                _spawnPointManager.SetSeed(42 + i); // Different seed each time
                var stopwatch = Stopwatch.StartNew();
                _spawnPointManager.PlaceSpawnPoints(map, furniture);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var averageTime = times.Count > 0 ? times.Average() : 0;
            var maxTime = times.Count > 0 ? times.Max() : 0;
            var minTime = times.Count > 0 ? times.Min() : 0;

            Assert.Less(averageTime, 50, "Average placement time should be under 50ms");
            Assert.Less(maxTime - minTime, 25, "Performance variance should be under 25ms");
            
            // Performance should be consistent (low standard deviation)
            var variance = times.Count > 1 ? times.Sum(t => (t - averageTime) * (t - averageTime)) / (times.Count - 1) : 0;
            var stdDev = System.Math.Sqrt(variance);
            Assert.Less(stdDev, 8, "Standard deviation should be under 8ms");
        }

        [Test]
        [Category("Performance")]
        public void CollisionDetection_WithFurniture_PerformanceTarget()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 15, 15));
            var furniture = CreateDenseFurnitureForRoom(room); // Many furniture pieces
            var targetTimeMs = 8; // Target: <8ms for collision detection

            // Act
            var stopwatch = Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"Collision detection should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Greater(spawnPoints.Count, 0, "Should find valid spawn points even with dense furniture");
        }

        [Test]
        [Category("Performance")]
        public void StrategicPositionTypes_AllTypes_PerformanceTarget()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 12, 12));
            var furniture = CreateTestFurnitureForRoom(room);
            
            // Test each position type
            var positionTypes = new[] 
            { 
                SpawnPositionType.Corner, 
                SpawnPositionType.NearDoorway, 
                SpawnPositionType.Cover,
                SpawnPositionType.Center,
                SpawnPositionType.Perimeter
            };
            
            var targetTimePerType = 3; // Target: <3ms per position type

            // Act & Assert
            foreach (var positionType in positionTypes)
            {
                _spawnPointManager.SetPositionPriorities(RoomClassification.Office, 
                    new List<SpawnPositionType> { positionType });
                
                var stopwatch = Stopwatch.StartNew();
                var spawnPoints = _spawnPointManager.PlaceSpawnPointsInRoom(room, furniture);
                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, targetTimePerType, 
                    $"Position type {positionType} should complete within {targetTimePerType}ms, took {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        #region Helper Methods

        private MapData CreateMapWithRooms(int roomCount)
        {
            var map = new MapData(200, 200);
            map.SetSeed(12345);
            var roomId = 1;

            for (int i = 0; i < roomCount; i++)
            {
                var x = (i % 10) * 20;
                var y = (i / 10) * 20;
                var width = UnityEngine.Random.Range(5, 12);
                var height = UnityEngine.Random.Range(5, 12);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.RoomID = roomId++;
                
                // Assign random classification
                var classifications = new[] 
                { 
                    RoomClassification.Office, 
                    RoomClassification.Conference, 
                    RoomClassification.BreakRoom,
                    RoomClassification.Storage,
                    RoomClassification.ServerRoom
                };
                room.Classification = classifications[UnityEngine.Random.Range(0, classifications.Length)];
                
                map.AddRoom(room);
            }

            return map;
        }

        private List<FurnitureData> CreateTestFurniture(MapData map)
        {
            var furniture = new List<FurnitureData>();
            
            foreach (var room in map.Rooms)
            {
                var roomFurniture = CreateTestFurnitureForRoom(room);
                furniture.AddRange(roomFurniture);
            }
            
            return furniture;
        }

        private List<FurnitureData> CreateTestFurnitureForRoom(RoomData room)
        {
            var furniture = new List<FurnitureData>();
            var bounds = room.Bounds;
            
            // Add 1-3 pieces of furniture per room
            var furnitureCount = UnityEngine.Random.Range(1, 4);
            
            for (int i = 0; i < furnitureCount; i++)
            {
                var x = UnityEngine.Random.Range(bounds.x + 1, bounds.xMax - 3);
                var y = UnityEngine.Random.Range(bounds.y + 1, bounds.yMax - 3);
                var width = UnityEngine.Random.Range(1, 3);
                var height = UnityEngine.Random.Range(1, 3);
                
                var furn = new FurnitureData($"furn_{room.RoomID}_{i}", "Desk", "path", 
                    room.RoomID, new Vector2Int(x, y), new Vector2Int(width, height));
                furn.SetBlockingProperties(true, UnityEngine.Random.value < 0.5f);
                
                furniture.Add(furn);
            }
            
            return furniture;
        }

        private List<FurnitureData> CreateDenseFurnitureForRoom(RoomData room)
        {
            var furniture = new List<FurnitureData>();
            var bounds = room.Bounds;
            
            // Create dense furniture layout for performance testing
            for (int x = bounds.x + 1; x < bounds.xMax - 2; x += 2)
            {
                for (int y = bounds.y + 1; y < bounds.yMax - 2; y += 2)
                {
                    var furn = new FurnitureData($"dense_furn_{x}_{y}", "Desk", "path", 
                        room.RoomID, new Vector2Int(x, y), new Vector2Int(1, 1));
                    furn.SetBlockingProperties(true, true);
                    
                    furniture.Add(furn);
                }
            }
            
            return furniture;
        }

        private SpawnTableConfiguration CreateTestSpawnTableConfig()
        {
            var config = ScriptableObject.CreateInstance<SpawnTableConfiguration>();
            return config;
        }

        #endregion
    }
}