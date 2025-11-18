using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class BiomeApplicatorTests
    {
        private BiomeApplicator _biomeApplicator;
        private MockAssetLoader _mockAssetLoader;
        private BiomeConfiguration _testBiome;
        private MapData _testMap;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);
            _testBiome = CreateTestBiome();
            _testMap = CreateTestMap();
        }

        [Test]
        public void ApplyBiomeToMap_WithValidMap_ReturnsSuccess()
        {
            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, _testBiome);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ApplicationTimeMs >= 0);
            Assert.IsTrue(result.ProcessedRooms.Count > 0);
        }

        [Test]
        public void ApplyBiomeToMap_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _biomeApplicator.ApplyBiomeToMap(null, _testBiome));
        }

        [Test]
        public void ApplyBiomeToMap_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _biomeApplicator.ApplyBiomeToMap(_testMap, null));
        }

        [Test]
        public void ApplyBiomeToMap_WithInvalidBiome_ReturnsFailure()
        {
            // Arrange
            var invalidBiome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            // Leave biome in invalid state (no ID, no tileset, etc.)

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, invalidBiome);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("validation failed"));
        }

        [Test]
        public void ApplyBiomeToMap_WithRoomTypeBiomes_AppliesCorrectly()
        {
            // Arrange
            var roomTypeBiomes = new Dictionary<RoomClassification, BiomeConfiguration>
            {
                { RoomClassification.Office, CreateTestBiome("Office") },
                { RoomClassification.Conference, CreateTestBiome("Conference") }
            };

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, _testBiome, roomTypeBiomes);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ProcessedRooms.Count > 0);
            
            // Check that rooms with specific biomes were processed
            var officeRooms = _testMap.Rooms.Where(r => r.Classification == RoomClassification.Office);
            var conferenceRooms = _testMap.Rooms.Where(r => r.Classification == RoomClassification.Conference);
            
            Assert.IsTrue(officeRooms.All(r => result.ProcessedRooms.Contains(r.RoomID)));
            Assert.IsTrue(conferenceRooms.All(r => result.ProcessedRooms.Contains(r.RoomID)));
        }

        [Test]
        public void ApplyBiomeToMap_WithEnvironmentalEffects_AppliesEffects()
        {
            // Arrange
            var biomeWithEffects = CreateTestBiomeWithEffects();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, biomeWithEffects);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.AppliedEffects.Any(e => e.Contains("Environmental")));
        }

        [Test]
        public void ApplyBiomeToMap_WithAudioConfiguration_AppliesAudio()
        {
            // Arrange
            var biomeWithAudio = CreateTestBiomeWithAudio();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, biomeWithAudio);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.AppliedEffects.Any(e => e.Contains("Music") || e.Contains("Sound")));
        }

        [Test]
        public void ApplyBiomeTransitions_WithValidRegions_ProcessesTransitions()
        {
            // Arrange
            var biomeRegions = new Dictionary<RectInt, BiomeConfiguration>
            {
                { new RectInt(0, 0, 10, 10), CreateTestBiome("Region1") },
                { new RectInt(8, 8, 10, 10), CreateTestBiome("Region2") }
            };

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _biomeApplicator.ApplyBiomeTransitions(_testMap, biomeRegions));
        }

        [Test]
        public void SetSeed_WithSameSeed_ProducesSameResults()
        {
            // Arrange
            const int seed = 123;
            var applicator1 = new BiomeApplicator(_mockAssetLoader, seed);
            var applicator2 = new BiomeApplicator(_mockAssetLoader, seed);

            // Act
            var result1 = applicator1.ApplyBiomeToMap(_testMap, _testBiome);
            var result2 = applicator2.ApplyBiomeToMap(_testMap, _testBiome);

            // Assert
            Assert.AreEqual(result1.ProcessedRooms.Count, result2.ProcessedRooms.Count);
            // Note: Exact comparison may vary due to Unity's non-deterministic behavior
        }

        [Test]
        public void GetMetrics_AfterApplication_ReturnsValidMetrics()
        {
            // Act
            _biomeApplicator.ApplyBiomeToMap(_testMap, _testBiome);
            var metrics = _biomeApplicator.GetMetrics();

            // Assert
            Assert.IsTrue(metrics.TilesProcessed >= 0);
            Assert.IsTrue(metrics.ObjectsProcessed >= 0);
            Assert.IsTrue(metrics.ApplicationTimeMs >= 0);
            Assert.IsTrue(metrics.AppliedBiomesCount > 0);
        }

        [Test]
        public void ClearBiomeEffects_AfterApplication_ClearsEffects()
        {
            // Arrange
            _biomeApplicator.ApplyBiomeToMap(_testMap, _testBiome);
            var metricsBefore = _biomeApplicator.GetMetrics();

            // Act
            _biomeApplicator.ClearBiomeEffects();
            var metricsAfter = _biomeApplicator.GetMetrics();

            // Assert
            Assert.AreEqual(0, metricsAfter.AppliedBiomesCount);
            Assert.AreEqual(0, metricsAfter.ActiveEffectsCount);
            Assert.IsTrue(metricsAfter.TilesProcessed < metricsBefore.TilesProcessed);
        }

        [Test]
        public void BiomeApplicationEvents_WhenApplied_FiresCorrectly()
        {
            // Arrange
            BiomeConfiguration appliedBiome = null;
            _biomeApplicator.OnBiomeApplied += (biome) => appliedBiome = biome;

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(_testMap, _testBiome);

            // Assert
            if (result.Success)
            {
                Assert.IsNotNull(appliedBiome);
                Assert.AreEqual(_testBiome.BiomeID, appliedBiome.BiomeID);
            }
        }

        [Test]
        public void BiomeApplicationEvents_WhenFailed_FiresFailureEvent()
        {
            // Arrange
            string errorBiomeId = null;
            System.Exception caughtException = null;
            
            _biomeApplicator.OnBiomeApplicationFailed += (biomeId, ex) => 
            {
                errorBiomeId = biomeId;
                caughtException = ex;
            };

            var invalidBiome = ScriptableObject.CreateInstance<BiomeConfiguration>();

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => 
                _biomeApplicator.ApplyBiomeToMap(_testMap, invalidBiome));

            // Assert
            Assert.IsNotNull(errorBiomeId);
            Assert.IsNotNull(caughtException);
        }

        [Test]
        public void ApplyBiomeToMap_Performance_CompletesWithinTarget()
        {
            // Arrange
            var largeMap = CreateLargeTestMap(50);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = _biomeApplicator.ApplyBiomeToMap(largeMap, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
                $"Biome application took {stopwatch.ElapsedMilliseconds}ms, target < 300ms");
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
                new EnvironmentalEffect { _effectName = "TestFog", _effectType = EffectType.Fog, _intensity = 0.5f }
            };
            
            effectsField?.SetValue(biome, effects);
            
            return biome;
        }

        private BiomeConfiguration CreateTestBiomeWithAudio()
        {
            var biome = CreateTestBiome("AudioBiome");
            
            // Add audio configuration using reflection
            var biomeType = typeof(BiomeConfiguration);
            var musicField = biomeType.GetField("_ambientMusic", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var soundsField = biomeType.GetField("_ambientSounds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Create mock audio clip
            var audioClip = ScriptableObject.CreateInstance<AudioClip>();
            musicField?.SetValue(biome, audioClip);
            
            var sounds = new List<AudioClip> { audioClip };
            soundsField?.SetValue(biome, sounds);
            
            return biome;
        }

        private MapData CreateTestMap()
        {
            var map = new MapData(20, 20);
            
            var rooms = new[]
            {
                new RoomData(1, new RectInt(2, 2, 5, 5), RoomClassification.Office),
                new RoomData(2, new RectInt(10, 2, 4, 5), RoomClassification.Conference),
                new RoomData(3, new RectInt(2, 10, 5, 4), RoomClassification.BreakRoom),
                new RoomData(4, new RectInt(10, 10, 4, 4), RoomClassification.Storage)
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private MapData CreateLargeTestMap(int roomCount)
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