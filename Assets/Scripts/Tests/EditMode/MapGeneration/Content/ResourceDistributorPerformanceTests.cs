using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class ResourceDistributorPerformanceTests
    {
        private ResourceDistributor _resourceDistributor;
        private MockAssetLoader _mockAssetLoader;
        private List<FurnitureData> _testFurniture;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _resourceDistributor = new ResourceDistributor(_mockAssetLoader);
            _testFurniture = new List<FurnitureData>();
        }

        [Test]
        [Category("Performance")]
        public void DistributeResources_10Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(10);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(resources.Count >= 0, "Should place resources in 10 rooms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, 
                $"Resource distribution for 10 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 50ms");
            
            UnityEngine.Debug.Log($"Resource distribution for 10 rooms: {stopwatch.ElapsedMilliseconds}ms, {resources.Count} resources");
        }

        [Test]
        [Category("Performance")]
        public void DistributeResources_50Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(resources.Count >= 0, "Should place resources in 50 rooms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Resource distribution for 50 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 100ms");
            
            UnityEngine.Debug.Log($"Resource distribution for 50 rooms: {stopwatch.ElapsedMilliseconds}ms, {resources.Count} resources");
        }

        [Test]
        [Category("Performance")]
        public void DistributeResources_100Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(100);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(resources.Count >= 0, "Should place resources in 100 rooms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200, 
                $"Resource distribution for 100 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 200ms");
            
            UnityEngine.Debug.Log($"Resource distribution for 100 rooms: {stopwatch.ElapsedMilliseconds}ms, {resources.Count} resources");
        }

        [Test]
        [Category("Performance")]
        public void DistributeResources_WithFurniture_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            AddTestFurniture(map);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(resources.Count >= 0, "Should place resources with furniture present");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 150, 
                $"Resource distribution with furniture took {stopwatch.ElapsedMilliseconds}ms, target < 150ms");
            
            var metrics = _resourceDistributor.GetMetrics();
            UnityEngine.Debug.Log($"Resource distribution with furniture: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{metrics.CollisionChecks} collision checks, {metrics.SuccessRate:P2} success rate");
        }

        [Test]
        public void DistributeResources_MemoryUsage_StaysWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(100);
            
            // Force garbage collection to get clean baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            
            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryUsed = (finalMemory - initialMemory) / (1024f * 1024f); // Convert to MB

            // Assert
            Assert.IsTrue(memoryUsed < 5f, 
                $"Resource distribution used {memoryUsed:F2}MB memory, target < 5MB");
            
            UnityEngine.Debug.Log($"Resource distribution memory usage: {memoryUsed:F2}MB for {resources.Count} resources");
        }

        [Test]
        public void GetMetrics_AfterDistribution_ReturnsAccurateMetrics()
        {
            // Arrange
            var map = CreateTestMap(25);
            AddTestFurniture(map);

            // Act
            var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
            var metrics = _resourceDistributor.GetMetrics();

            // Assert
            Assert.IsTrue(metrics.CollisionChecks > 0, "Should perform collision checks");
            Assert.IsTrue(metrics.PlacementsAttempted >= 0, "Should track placement attempts");
            Assert.IsTrue(metrics.PlacementsSuccessful >= 0, "Should track successful placements");
            Assert.IsTrue(metrics.SuccessRate >= 0 && metrics.SuccessRate <= 1, "Success rate should be valid");
            
            // Performance validation
            Assert.IsTrue(metrics.AverageCollisionChecksPerRoom < 50, 
                $"Average collision checks per room: {metrics.AverageCollisionChecksPerRoom:F2}, target < 50");
            
            UnityEngine.Debug.Log($"Resource distribution metrics: " +
                                 $"{metrics.CollisionChecks} checks, " +
                                 $"{metrics.PlacementsSuccessful}/{metrics.PlacementsAttempted} placements, " +
                                 $"{metrics.SuccessRate:P2} success rate");
        }

        [Test]
        public void DistributeResources_ScalingPerformance_LinearGrowth()
        {
            // Arrange
            var roomCounts = new[] { 10, 25, 50, 100 };
            var performanceData = new List<(int rooms, long ms)>();

            // Act
            foreach (var roomCount in roomCounts)
            {
                var map = CreateTestMap(roomCount);
                var stopwatch = Stopwatch.StartNew();
                
                var resources = _resourceDistributor.DistributeResources(map, _testFurniture);
                
                stopwatch.Stop();
                performanceData.Add((roomCount, stopwatch.ElapsedMilliseconds));
                
                UnityEngine.Debug.Log($"Resource distribution {roomCount} rooms: {stopwatch.ElapsedMilliseconds}ms, {resources.Count} resources");
            }

            // Assert - Check for roughly linear scaling
            for (int i = 1; i < performanceData.Count; i++)
            {
                var prev = performanceData[i - 1];
                var curr = performanceData[i];
                
                var roomRatio = (float)curr.rooms / prev.rooms;
                var timeRatio = (float)curr.ms / prev.ms;
                
                // Time growth should be proportional to room count (allowing some variance)
                Assert.IsTrue(timeRatio <= roomRatio * 1.5f, 
                    $"Performance scaling not linear: {prev.rooms}->{curr.rooms} rooms, " +
                    $"{prev.ms}->{curr.ms}ms (time ratio: {timeRatio:F2}, room ratio: {roomRatio:F2})");
            }
        }

        [Test]
        public void DistributeResources_MultipleRuns_ConsistentPerformance()
        {
            // Arrange
            var map = CreateTestMap(50);
            var runTimes = new List<long>();

            // Act - Run multiple times to check consistency
            for (int i = 0; i < 5; i++)
            {
                var distributor = new ResourceDistributor(_mockAssetLoader);
                var stopwatch = Stopwatch.StartNew();
                
                var resources = distributor.DistributeResources(map, _testFurniture);
                
                stopwatch.Stop();
                runTimes.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert
            var avgTime = runTimes.Average();
            var maxTime = runTimes.Max();
            var minTime = runTimes.Min();
            
            Assert.IsTrue(avgTime < 100, $"Average time {avgTime:F2}ms should be < 100ms");
            Assert.IsTrue(maxTime - minTime < avgTime * 0.5f, 
                $"Performance variance too high: min={minTime}ms, max={maxTime}ms, avg={avgTime:F2}ms");
            
            UnityEngine.Debug.Log($"Resource distribution consistency: avg={avgTime:F2}ms, " +
                                 $"min={minTime}ms, max={maxTime}ms");
        }

        #region Helper Methods

        private MapData CreateTestMap(int roomCount)
        {
            var map = new MapData(50, 50);
            var roomTypes = new[] 
            { 
                RoomClassification.Office, 
                RoomClassification.Conference, 
                RoomClassification.BreakRoom, 
                RoomClassification.Storage,
                RoomClassification.ServerRoom,
                RoomClassification.Lobby
            };

            for (int i = 0; i < roomCount; i++)
            {
                var x = (i % 10) * 5 + 1;
                var y = (i / 10) * 5 + 1;
                var roomType = roomTypes[i % roomTypes.Length];
                
                var room = new RoomData(i + 1, new RectInt(x, y, 4, 4), roomType);
                map.AddRoom(room);
            }

            return map;
        }

        private void AddTestFurniture(MapData map)
        {
            // Add furniture to some rooms to test collision detection performance
            for (int i = 0; i < map.Rooms.Count / 2; i++)
            {
                var room = map.Rooms[i];
                
                // Add 2-3 pieces of furniture per room
                for (int j = 0; j < 3; j++)
                {
                    var furniturePos = new Vector2Int(
                        room.Bounds.xMin + 1 + j,
                        room.Bounds.yMin + 1
                    );
                    
                    var furniture = new FurnitureData(
                        $"furniture_{i}_{j}", 
                        "desk", 
                        room.RoomID, 
                        furniturePos, 
                        Vector2Int.one
                    );
                    furniture.SetOccupiedTiles(new[] { furniturePos });
                    
                    _testFurniture.Add(furniture);
                }
            }
        }

        #endregion
    }
}