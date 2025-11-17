using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;
using OfficeMice.MapGeneration.Rendering;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Tests.Factories;
using OfficeMice.MapGeneration.Tests.Performance;

namespace OfficeMice.MapGeneration.Tests.Performance
{
    [TestFixture]
    public class TilemapGeneratorPerformanceTests : PerformanceBenchmark
    {
        private TilemapGenerator _tilemapGenerator;
        private Tilemap[] _testTilemaps;
        private TilesetConfiguration _testTileset;
        private GameObject _testGameObject;
        private Grid _testGrid;
        
        // Performance targets from the requirements
        private const int MAX_RENDERING_TIME_MS = 150;
        private const int MAX_MEMORY_USAGE_MB = 25;
        private const int MAX_GC_PRESSURE_KB = 40;
        private const int MAX_TILES_PER_FRAME = 1000;
        private const int MAX_BATCH_OPERATIONS_PER_FRAME = 50;
        
        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            
            // Create test GameObject with Grid and Tilemap components
            _testGameObject = new GameObject("PerformanceTestTilemapObject");
            _testGrid = _testGameObject.AddComponent<Grid>();
            _testGrid.cellSize = Vector3.one;
            
            // Create test tilemaps
            _testTilemaps = new Tilemap[3];
            for (int i = 0; i < 3; i++)
            {
                var tilemapGO = new GameObject($"Tilemap_{i}");
                tilemapGO.transform.SetParent(_testGameObject.transform);
                
                var tilemap = tilemapGO.AddComponent<Tilemap>();
                var renderer = tilemapGO.AddComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Individual;
                
                _testTilemaps[i] = tilemap;
            }
            
            // Create test tileset
            _testTileset = MapGenerationTestDataFactory.CreateTestTilesetConfiguration();
            
            // Create tilemap generator
            _tilemapGenerator = new TilemapGenerator(42); // Fixed seed for reproducible tests
        }
        
        [TearDown]
        public void TearDown()
        {
            base.TearDown();
            
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
            
            _tilemapGenerator = null;
            _testTilemaps = null;
            _testTileset = null;
        }
        
        #region Rendering Performance Tests
        
        [Test]
        [Performance]
        public void RenderMap_SmallMap_CompletesWithinPerformanceTargets()
        {
            // Arrange
            var smallMap = MapGenerationTestDataFactory.CreateTestMapData(10); // 10 rooms
            var memoryTracker = new MemoryTracker();
            var gcMonitor = new GcPressureMonitor();
            
            memoryTracker.StartTracking();
            gcMonitor.StartMonitoring();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.RenderMap(smallMap, _testTilemaps);
            
            stopwatch.Stop();
            var memoryUsed = memoryTracker.StopTracking();
            var gcPressure = gcMonitor.StopMonitoring();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS, 
                $"Small map rendering should complete within {MAX_RENDERING_TIME_MS}ms");
            Assert.Less(memoryUsed, MAX_MEMORY_USAGE_MB * 1024 * 1024, 
                $"Memory usage should be under {MAX_MEMORY_USAGE_MB}MB");
            Assert.Less(gcPressure, MAX_GC_PRESSURE_KB * 1024, 
                $"GC pressure should be under {MAX_GC_PRESSURE_KB}KB");
            
            // Log performance metrics
            Debug.Log($"Small Map Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed / 1024 / 1024}MB, GC: {gcPressure / 1024}KB");
        }
        
        [Test]
        [Performance]
        public void RenderMap_MediumMap_CompletesWithinPerformanceTargets()
        {
            // Arrange
            var mediumMap = MapGenerationTestDataFactory.CreateTestMapData(50); // 50 rooms
            var memoryTracker = new MemoryTracker();
            var gcMonitor = new GcPressureMonitor();
            
            memoryTracker.StartTracking();
            gcMonitor.StartMonitoring();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.RenderMap(mediumMap, _testTilemaps);
            
            stopwatch.Stop();
            var memoryUsed = memoryTracker.StopTracking();
            var gcPressure = gcMonitor.StopMonitoring();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS, 
                $"Medium map rendering should complete within {MAX_RENDERING_TIME_MS}ms");
            Assert.Less(memoryUsed, MAX_MEMORY_USAGE_MB * 1024 * 1024, 
                $"Memory usage should be under {MAX_MEMORY_USAGE_MB}MB");
            Assert.Less(gcPressure, MAX_GC_PRESSURE_KB * 1024, 
                $"GC pressure should be under {MAX_GC_PRESSURE_KB}KB");
            
            // Log performance metrics
            Debug.Log($"Medium Map Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed / 1024 / 1024}MB, GC: {gcPressure / 1024}KB");
        }
        
        [Test]
        [Performance]
        public void RenderMap_LargeMap_CompletesWithinPerformanceTargets()
        {
            // Arrange
            var largeMap = MapGenerationTestDataFactory.CreateTestMapData(100); // 100 rooms
            var memoryTracker = new MemoryTracker();
            var gcMonitor = new GcPressureMonitor();
            
            memoryTracker.StartTracking();
            gcMonitor.StartMonitoring();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.RenderMap(largeMap, _testTilemaps);
            
            stopwatch.Stop();
            var memoryUsed = memoryTracker.StopTracking();
            var gcPressure = gcMonitor.StopMonitoring();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS, 
                $"Large map rendering should complete within {MAX_RENDERING_TIME_MS}ms");
            Assert.Less(memoryUsed, MAX_MEMORY_USAGE_MB * 1024 * 1024, 
                $"Memory usage should be under {MAX_MEMORY_USAGE_MB}MB");
            Assert.Less(gcPressure, MAX_GC_PRESSURE_KB * 1024, 
                $"GC pressure should be under {MAX_GC_PRESSURE_KB}KB");
            
            // Log performance metrics
            Debug.Log($"Large Map Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed / 1024 / 1024}MB, GC: {gcPressure / 1024}KB");
        }
        
        #endregion
        
        #region Batch Operation Performance Tests
        
        [Test]
        [Performance]
        public void UpdateTiles_WithLargeBatch_CompletesWithinPerformanceTargets()
        {
            // Arrange
            var positions = new Vector3Int[500];
            var tiles = new TileBase[500];
            
            for (int i = 0; i < 500; i++)
            {
                positions[i] = new Vector3Int(i % 50, i / 50, 0);
                tiles[i] = _testTileset.FloorTiles.Tiles[0].Tile;
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS / 2, 
                "Large batch update should complete quickly");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.LessOrEqual(stats.BatchOperationsThisFrame, MAX_BATCH_OPERATIONS_PER_FRAME, 
                $"Should not exceed {MAX_BATCH_OPERATIONS_PER_FRAME} batch operations per frame");
            
            Debug.Log($"Batch Update Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Batches: {stats.BatchOperationsThisFrame}");
        }
        
        [Test]
        [Performance]
        public void RenderRoom_UsesBoxFillEfficiently()
        {
            // Arrange
            var largeRoom = MapGenerationTestDataFactory.CreateTestRoom(RoomClassification.Office, 20, 20);
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.RenderRoom(largeRoom, _testTilemaps[0], _testTileset);
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, 
                "Large room rendering should use efficient BoxFill operation");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.Less(stats.BatchOperationsThisFrame, 5, 
                "Large room should use minimal batch operations with BoxFill");
            
            Debug.Log($"BoxFill Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Batches: {stats.BatchOperationsThisFrame}");
        }
        
        #endregion
        
        #region Memory Management Tests
        
        [Test]
        [Performance]
        public void RenderMap_MultipleMaps_DoesNotLeakMemory()
        {
            // Arrange
            var memoryTracker = new MemoryTracker();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Render multiple maps
            for (int i = 0; i < 10; i++)
            {
                var map = MapGenerationTestDataFactory.CreateTestMapData(20);
                _tilemapGenerator.RenderMap(map, _testTilemaps);
                _tilemapGenerator.ClearTilemaps(_testTilemaps);
            }
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert
            Assert.Less(memoryIncrease, MAX_MEMORY_USAGE_MB * 1024 * 1024, 
                $"Memory increase should be under {MAX_MEMORY_USAGE_MB}MB after multiple renders");
            
            Debug.Log($"Memory Management - Initial: {initialMemory / 1024 / 1024}MB, Final: {finalMemory / 1024 / 1024}MB, Increase: {memoryIncrease / 1024 / 1024}MB");
        }
        
        [Test]
        [Performance]
        public void TileCache_ImprovesPerformanceOnRepeatedRenders()
        {
            // Arrange
            var map = MapGenerationTestDataFactory.CreateTestMapData(30);
            
            // First render (cache miss)
            var stopwatch1 = Stopwatch.StartNew();
            _tilemapGenerator.RenderMap(map, _testTilemaps);
            stopwatch1.Stop();
            
            _tilemapGenerator.ClearTilemaps(_testTilemaps);
            
            // Second render (cache hit)
            var stopwatch2 = Stopwatch.StartNew();
            _tilemapGenerator.RenderMap(map, _testTilemaps);
            stopwatch2.Stop();
            
            // Assert
            Assert.Less(stopwatch2.ElapsedMilliseconds, stopwatch1.ElapsedMilliseconds, 
                "Second render should be faster due to tile caching");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.Greater(stats.CachedTiles, 0, "Should have cached tiles");
            
            Debug.Log($"Cache Performance - First: {stopwatch1.ElapsedMilliseconds}ms, Second: {stopwatch2.ElapsedMilliseconds}ms, Cached: {stats.CachedTiles}");
        }
        
        #endregion
        
        #region Scalability Tests
        
        [Test]
        [Performance]
        public void RenderMap_ScalabilityTest_PerformanceScalesLinearly()
        {
            // Arrange
            var performanceData = new List<(int rooms, long timeMs)>();
            
            // Test with different map sizes
            for (int roomCount = 10; roomCount <= 100; roomCount += 10)
            {
                var map = MapGenerationTestDataFactory.CreateTestMapData(roomCount);
                
                var stopwatch = Stopwatch.StartNew();
                _tilemapGenerator.RenderMap(map, _testTilemaps);
                stopwatch.Stop();
                
                performanceData.Add((roomCount, stopwatch.ElapsedMilliseconds));
                
                _tilemapGenerator.ClearTilemaps(_testTilemaps);
            }
            
            // Assert - Check that performance scales reasonably (not exponentially)
            var firstTime = performanceData.First().timeMs;
            var lastTime = performanceData.Last().timeMs;
            var roomRatio = (double)performanceData.Last().rooms / performanceData.First().rooms;
            var timeRatio = (double)lastTime / firstTime;
            
            Assert.Less(timeRatio, roomRatio * 1.5, 
                "Performance should scale close to linearly with room count");
            
            // Log scalability data
            Debug.Log("Scalability Test Results:");
            foreach (var (rooms, time) in performanceData)
            {
                Debug.Log($"  {rooms} rooms: {time}ms");
            }
        }
        
        #endregion
        
        #region Stress Tests
        
        [Test]
        [Performance]
        public void RenderMap_StressTest_HandlesExtremeMapSize()
        {
            // Arrange
            var extremeMap = MapGenerationTestDataFactory.CreateTestMapData(200); // Very large map
            var memoryTracker = new MemoryTracker();
            
            memoryTracker.StartTracking();
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            try
            {
                _tilemapGenerator.RenderMap(extremeMap, _testTilemaps);
                stopwatch.Stop();
                var memoryUsed = memoryTracker.StopTracking();
                
                // Assert - Should complete without crashing
                Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS * 3, 
                    "Even extreme maps should complete within reasonable time");
                
                Debug.Log($"Stress Test - Time: {stopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                // It's acceptable for extreme maps to fail gracefully
                Assert.IsInstanceOf<OutOfMemoryException>(ex, "Should fail gracefully with OutOfMemoryException for extreme maps");
                Debug.LogWarning($"Stress test failed gracefully: {ex.Message}");
            }
        }
        
        [Test]
        [Performance]
        public void UpdateTiles_StressTest_HandlesLargeTileCount()
        {
            // Arrange
            var positions = new Vector3Int[2000];
            var tiles = new TileBase[2000];
            
            for (int i = 0; i < 2000; i++)
            {
                positions[i] = new Vector3Int(i % 100, i / 100, 0);
                tiles[i] = _testTileset.FloorTiles.Tiles[0].Tile;
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, MAX_RENDERING_TIME_MS, 
                "Large tile updates should complete within performance targets");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.Greater(stats.TilesRenderedThisFrame, MAX_TILES_PER_FRAME, 
                "Should handle more than maximum tiles per frame in stress test");
            
            Debug.Log($"Stress Update - Time: {stopwatch.ElapsedMilliseconds}ms, Tiles: {stats.TilesRenderedThisFrame}");
        }
        
        #endregion
        
        #region Regression Tests
        
        [Test]
        [Performance]
        public void RenderMap_PerformanceRegression_NoDegradationOverTime()
        {
            // Arrange
            var regressionData = new List<long>();
            var iterations = 20;
            
            // Act - Run multiple iterations to check for performance regression
            for (int i = 0; i < iterations; i++)
            {
                var map = MapGenerationTestDataFactory.CreateTestMapData(50);
                
                var stopwatch = Stopwatch.StartNew();
                _tilemapGenerator.RenderMap(map, _testTilemaps);
                stopwatch.Stop();
                
                regressionData.Add(stopwatch.ElapsedMilliseconds);
                
                _tilemapGenerator.ClearTilemaps(_testTilemaps);
                _tilemapGenerator.ResetPerformanceCounters();
            }
            
            // Assert - Check for performance regression
            var averageTime = regressionData.Average();
            var maxTime = regressionData.Max();
            var minTime = regressionData.Min();
            
            Assert.Less(averageTime, MAX_RENDERING_TIME_MS, 
                $"Average time should be under {MAX_RENDERING_TIME_MS}ms");
            Assert.Less(maxTime - minTime, averageTime * 0.5, 
                "Performance variance should be within 50% of average");
            
            Debug.Log($"Regression Test - Average: {averageTime}ms, Min: {minTime}ms, Max: {maxTime}ms");
        }
        
        #endregion
        
        #region Optimization Tests
        
        [Test]
        [Performance]
        public void OptimizeTileRendering_ImprovesPerformance()
        {
            // Arrange
            var map = MapGenerationTestDataFactory.CreateTestMapData(50);
            
            // Render without optimization
            var stopwatch1 = Stopwatch.StartNew();
            _tilemapGenerator.RenderMap(map, _testTilemaps);
            stopwatch1.Stop();
            
            _tilemapGenerator.ClearTilemaps(_testTilemaps);
            
            // Render with optimization
            var stopwatch2 = Stopwatch.StartNew();
            _tilemapGenerator.RenderMap(map, _testTilemaps);
            foreach (var tilemap in _testTilemaps)
            {
                _tilemapGenerator.OptimizeTileRendering(tilemap);
            }
            stopwatch2.Stop();
            
            // Assert - Optimization should not significantly increase render time
            Assert.Less(stopwatch2.ElapsedMilliseconds, stopwatch1.ElapsedMilliseconds * 1.2, 
                "Optimization should not increase render time by more than 20%");
            
            Debug.Log($"Optimization Test - Without: {stopwatch1.ElapsedMilliseconds}ms, With: {stopwatch2.ElapsedMilliseconds}ms");
        }
        
        #endregion
    }
}