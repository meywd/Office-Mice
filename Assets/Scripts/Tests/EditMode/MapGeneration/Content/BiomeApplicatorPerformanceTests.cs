using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class BiomeApplicatorPerformanceTests
    {
        private BiomeApplicator _biomeApplicator;
        private MockAssetLoader _mockAssetLoader;
        private BiomeConfiguration _testBiome;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _biomeApplicator = new BiomeApplicator(_mockAssetLoader);
            _testBiome = CreateTestBiome();
        }

        [Test]
        [Performance]
        public void ApplyBiomeToMap_10Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(10);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Biome application should succeed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Biome application for 10 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 100ms");
            
            UnityEngine.Debug.Log($"Biome application for 10 rooms: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.ProcessedRooms.Count} rooms processed");
        }

        [Test]
        [Performance]
        public void ApplyBiomeToMap_50Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Biome application should succeed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200, 
                $"Biome application for 50 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 200ms");
            
            UnityEngine.Debug.Log($"Biome application for 50 rooms: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.ProcessedRooms.Count} rooms processed");
        }

        [Test]
        [Performance]
        public void ApplyBiomeToMap_100Rooms_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(100);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Biome application should succeed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
                $"Biome application for 100 rooms took {stopwatch.ElapsedMilliseconds}ms, target < 300ms");
            
            UnityEngine.Debug.Log($"Biome application for 100 rooms: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.ProcessedRooms.Count} rooms processed");
        }

        [Test]
        [Performance]
        public void ApplyBiomeToMap_WithRoomTypeBiomes_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            var roomTypeBiomes = CreateRoomTypeBiomes();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome, roomTypeBiomes);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Biome application with room types should succeed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 250, 
                $"Biome application with room types took {stopwatch.ElapsedMilliseconds}ms, target < 250ms");
            
            UnityEngine.Debug.Log($"Biome application with room types: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.ProcessedRooms.Count} rooms processed");
        }

        [Test]
        [Performance]
        public void ApplyBiomeToMap_WithEnvironmentalEffects_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            var biomeWithEffects = CreateTestBiomeWithEffects();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, biomeWithEffects);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Biome application with effects should succeed");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
                $"Biome application with effects took {stopwatch.ElapsedMilliseconds}ms, target < 300ms");
            
            UnityEngine.Debug.Log($"Biome application with effects: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.AppliedEffects.Count} effects applied");
        }

        [Test]
        public void ApplyBiomeToMap_MemoryUsage_StaysWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(100);
            
            // Force garbage collection to get clean baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            
            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryUsed = (finalMemory - initialMemory) / (1024f * 1024f); // Convert to MB

            // Assert
            Assert.IsTrue(result.Success, "Biome application should succeed");
            Assert.IsTrue(memoryUsed < 10f, 
                $"Biome application used {memoryUsed:F2}MB memory, target < 10MB");
            
            UnityEngine.Debug.Log($"Biome application memory usage: {memoryUsed:F2}MB for {result.ProcessedRooms.Count} rooms");
        }

        [Test]
        public void GetMetrics_AfterApplication_ReturnsAccurateMetrics()
        {
            // Arrange
            var map = CreateTestMap(25);

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            var metrics = _biomeApplicator.GetMetrics();

            // Assert
            Assert.IsTrue(result.Success, "Biome application should succeed");
            Assert.IsTrue(metrics.TilesProcessed >= 0, "Should track tiles processed");
            Assert.IsTrue(metrics.ObjectsProcessed >= 0, "Should track objects processed");
            Assert.IsTrue(metrics.ApplicationTimeMs >= 0, "Should track application time");
            Assert.IsTrue(metrics.AppliedBiomesCount > 0, "Should track applied biomes");
            
            // Performance validation
            Assert.IsTrue(metrics.AverageTilesPerMs >= 0, "Average tiles per ms should be valid");
            
            UnityEngine.Debug.Log($"Biome application metrics: " +
                                 $"{metrics.TilesProcessed} tiles, " +
                                 $"{metrics.ObjectsProcessed} objects, " +
                                 $"{metrics.ApplicationTimeMs}ms, " +
                                 $"{metrics.AppliedBiomesCount} biomes");
        }

        [Test]
        public void ApplyBiomeTransitions_MultipleRegions_PerformsWithinTarget()
        {
            // Arrange
            var map = CreateTestMap(50);
            var biomeRegions = CreateBiomeRegions(5);
            var stopwatch = Stopwatch.StartNew();

            // Act
            _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            _biomeApplicator.ApplyBiomeTransitions(map, biomeRegions);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Biome transitions took {stopwatch.ElapsedMilliseconds}ms, target < 100ms");
            
            UnityEngine.Debug.Log($"Biome transitions for 5 regions: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void ApplyBiomeToMap_ScalingPerformance_LinearGrowth()
        {
            // Arrange
            var roomCounts = new[] { 10, 25, 50, 100 };
            var performanceData = new List<(int rooms, long ms)>();

            // Act
            foreach (var roomCount in roomCounts)
            {
                var map = CreateTestMap(roomCount);
                var stopwatch = Stopwatch.StartNew();
                
                var result = _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
                
                stopwatch.Stop();
                performanceData.Add((roomCount, stopwatch.ElapsedMilliseconds));
                
                UnityEngine.Debug.Log($"Biome application {roomCount} rooms: {stopwatch.ElapsedMilliseconds}ms, " +
                                     $"{result.ProcessedRooms.Count} processed");
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
        public void ApplyBiomeToMap_MultipleRuns_ConsistentPerformance()
        {
            // Arrange
            var map = CreateTestMap(50);
            var runTimes = new List<long>();

            // Act - Run multiple times to check consistency
            for (int i = 0; i < 5; i++)
            {
                var applicator = new BiomeApplicator(_mockAssetLoader);
                var stopwatch = Stopwatch.StartNew();
                
                var result = applicator.ApplyBiomeToMap(map, _testBiome);
                
                stopwatch.Stop();
                runTimes.Add(stopwatch.ElapsedMilliseconds);
                
                Assert.IsTrue(result.Success, $"Run {i + 1} should succeed");
            }

            // Assert
            var avgTime = runTimes.Average();
            var maxTime = runTimes.Max();
            var minTime = runTimes.Min();
            
            Assert.IsTrue(avgTime < 200, $"Average time {avgTime:F2}ms should be < 200ms");
            Assert.IsTrue(maxTime - minTime < avgTime * 0.5f, 
                $"Performance variance too high: min={minTime}ms, max={maxTime}ms, avg={avgTime:F2}ms");
            
            UnityEngine.Debug.Log($"Biome application consistency: avg={avgTime:F2}ms, " +
                                 $"min={minTime}ms, max={maxTime}ms");
        }

        [Test]
        public void ClearBiomeEffects_Performance_CleansUpEfficiently()
        {
            // Arrange
            var map = CreateTestMap(50);
            _biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            var stopwatch = Stopwatch.StartNew();

            // Act
            _biomeApplicator.ClearBiomeEffects();
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, 
                $"Clear biome effects took {stopwatch.ElapsedMilliseconds}ms, target < 50ms");
            
            var metrics = _biomeApplicator.GetMetrics();
            Assert.AreEqual(0, metrics.AppliedBiomesCount, "Should clear applied biomes");
            Assert.AreEqual(0, metrics.ActiveEffectsCount, "Should clear active effects");
            
            UnityEngine.Debug.Log($"Clear biome effects: {stopwatch.ElapsedMilliseconds}ms");
        }

        #region Helper Methods

        private BiomeConfiguration CreateTestBiome(string biomeName = "TestBiome")
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            
            // Use reflection to set private fields for testing
            var biomeType = typeof(BiomeConfiguration);
            var biomeIdField = biomeType.GetField("_biomeID", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var biomeNameField = biomeType.GetField("_biomeName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var primaryTilesetField = biomeType.GetField("_primaryTileset", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var generationRulesField = biomeType.GetField("_generationRules", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            biomeIdField?.SetValue(biome, biomeName.ToLower().Replace(" ", "_"));
            biomeNameField?.SetValue(biome, biomeName);
            
            // Create mock tileset
            var tileset = ScriptableObject.CreateInstance<TilesetConfiguration>();
            primaryTilesetField?.SetValue(biome, tileset);
            
            // Create mock generation rules
            var generationRules = ScriptableObject.CreateInstance<BiomeGenerationRules>();
            generationRulesField?.SetValue(biome, generationRules);

            return biome;
        }

        private BiomeConfiguration CreateTestBiomeWithEffects()
        {
            var biome = CreateTestBiome("EffectsBiome");
            
            // Add environmental effects using reflection
            var biomeType = typeof(BiomeConfiguration);
            var effectsField = biomeType.GetField("_environmentalEffects", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var effects = new List<EnvironmentalEffect>
            {
                new EnvironmentalEffect { _effectName = "TestFog", _effectType = EffectType.Fog, _intensity = 0.5f },
                new EnvironmentalEffect { _effectName = "TestDust", _effectType = EffectType.Dust, _intensity = 0.3f }
            };
            
            effectsField?.SetValue(biome, effects);
            
            return biome;
        }

        private Dictionary<RoomClassification, BiomeConfiguration> CreateRoomTypeBiomes()
        {
            return new Dictionary<RoomClassification, BiomeConfiguration>
            {
                { RoomClassification.Office, CreateTestBiome("Office") },
                { RoomClassification.Conference, CreateTestBiome("Conference") },
                { RoomClassification.BreakRoom, CreateTestBiome("BreakRoom") },
                { RoomClassification.Storage, CreateTestBiome("Storage") },
                { RoomClassification.ServerRoom, CreateTestBiome("ServerRoom") },
                { RoomClassification.Lobby, CreateTestBiome("Lobby") }
            };
        }

        private Dictionary<RectInt, BiomeConfiguration> CreateBiomeRegions(int regionCount)
        {
            var regions = new Dictionary<RectInt, BiomeConfiguration>();
            
            for (int i = 0; i < regionCount; i++)
            {
                var x = (i % 3) * 30 + 5;
                var y = (i / 3) * 30 + 5;
                var region = new RectInt(x, y, 20, 20);
                var biome = CreateTestBiome($"Region{i}");
                
                regions[region] = biome;
            }
            
            return regions;
        }

        private MapData CreateTestMap(int roomCount)
        {
            var map = new MapData(100, 100);
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
                var x = (i % 10) * 10 + 1;
                var y = (i / 10) * 10 + 1;
                var roomType = roomTypes[i % roomTypes.Length];
                
                var room = new RoomData(i + 1, new RectInt(x, y, 8, 8), roomType);
                map.AddRoom(room);
            }

            return map;
        }

        #endregion
    }
}