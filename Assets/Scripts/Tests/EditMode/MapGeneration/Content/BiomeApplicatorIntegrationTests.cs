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
    public class BiomeApplicatorIntegrationTests
    {
        private MapContentPopulator _contentPopulator;
        private MockAssetLoader _mockAssetLoader;
        private BiomeConfiguration _testBiome;
        private BiomeConfiguration _officeBiome;
        private BiomeConfiguration _serverRoomBiome;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _testBiome = CreateTestBiome("Default");
            _officeBiome = CreateTestBiome("Office");
            _serverRoomBiome = CreateTestBiome("ServerRoom");
            _contentPopulator = new MapContentPopulator(_mockAssetLoader, null, 42);
        }

        [Test]
        public void MapContentPopulator_WithBiomeApplicator_CompletesWorkflow()
        {
            // Arrange
            var map = CreateTestMap();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);

            // Assert
            var furniture = _contentPopulator.PlaceFurniture(map, _testBiome);
            var spawnPoints = _contentPopulator.PlaceEnemySpawns(map, _testBiome);
            var resources = _contentPopulator.PlaceResources(map, _testBiome);
            var biomeResult = _contentPopulator.ApplyBiome(map, _testBiome);

            Assert.IsTrue(furniture.Count > 0, "Should place furniture");
            Assert.IsTrue(spawnPoints.Count > 0, "Should place spawn points");
            Assert.IsTrue(resources.Count > 0, "Should place resources");
            Assert.IsTrue(biomeResult.Success, "Should apply biome successfully");
        }

        [Test]
        public void MapContentPopulator_WithRoomTypeBiomes_AppliesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var roomTypeBiomes = new Dictionary<RoomClassification, BiomeConfiguration>
            {
                { RoomClassification.Office, _officeBiome },
                { RoomClassification.ServerRoom, _serverRoomBiome }
            };

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);
            var biomeResult = _contentPopulator.ApplyBiome(map, _testBiome, roomTypeBiomes);

            // Assert
            Assert.IsTrue(biomeResult.Success, "Should apply room-type biomes successfully");
            Assert.IsTrue(biomeResult.ProcessedRooms.Count > 0, "Should process rooms");
        }

        [Test]
        public void MapContentPopulator_CompleteWorkflow_NoErrors()
        {
            // Arrange
            var map = CreateLargeTestMap(25);

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);

            // Assert - Should not throw any exceptions
            Assert.IsTrue(true, "Complete workflow should complete without errors");
        }

        [Test]
        public void MapContentPopulator_BiomeEvents_FireCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeApplied = false;

            _contentPopulator.OnBiomeApplied += (biome) => biomeApplied = true;

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);

            // Assert
            Assert.IsTrue(biomeApplied, "Biome applied event should fire");
        }

        [Test]
        public void MapContentPopulator_ReproducibleGeneration_SameBiomeResults()
        {
            // Arrange
            const int seed = 123;
            var map = CreateTestMap();

            var populator1 = new MapContentPopulator(_mockAssetLoader, null, seed);
            var populator2 = new MapContentPopulator(_mockAssetLoader, null, seed);

            // Act
            populator1.PopulateContent(map, _testBiome);
            populator2.PopulateContent(map, _testBiome);

            var biomeResult1 = populator1.ApplyBiome(map, _testBiome);
            var biomeResult2 = populator2.ApplyBiome(map, _testBiome);

            // Assert
            Assert.IsTrue(biomeResult1.Success);
            Assert.IsTrue(biomeResult2.Success);
            Assert.AreEqual(biomeResult1.ProcessedRooms.Count, biomeResult2.ProcessedRooms.Count);
        }

        [Test]
        public void MapContentPopulator_Performance_CompleteWorkflowWithinTarget()
        {
            // Arrange
            var map = CreateLargeTestMap(50);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            _contentPopulator.PopulateContent(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Complete workflow took {stopwatch.ElapsedMilliseconds}ms, target < 1000ms");

            UnityEngine.Debug.Log($"Complete workflow for 50 rooms: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void BiomeApplicator_WithExistingContent_IntegratesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);

            // First place content
            var furniture = _contentPopulator.PlaceFurniture(map, _testBiome);
            var spawnPoints = _contentPopulator.PlaceEnemySpawns(map, _testBiome);
            var resources = _contentPopulator.PlaceResources(map, _testBiome);

            // Then apply biome
            var biomeResult = biomeApplicator.ApplyBiomeToMap(map, _testBiome);

            // Assert
            Assert.IsTrue(furniture.Count > 0, "Should have furniture");
            Assert.IsTrue(spawnPoints.Count > 0, "Should have spawn points");
            Assert.IsTrue(resources.Count > 0, "Should have resources");
            Assert.IsTrue(biomeResult.Success, "Should apply biome successfully");
        }

        [Test]
        public void BiomeApplicator_MultipleBiomes_TransitionsWork()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);
            
            var biomeRegions = new Dictionary<RectInt, BiomeConfiguration>
            {
                { new RectInt(0, 0, 10, 10), _officeBiome },
                { new RectInt(8, 8, 10, 10), _serverRoomBiome }
            };

            // Act
            var biomeResult = biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            biomeApplicator.ApplyBiomeTransitions(map, biomeRegions);

            // Assert
            Assert.IsTrue(biomeResult.Success, "Should apply primary biome successfully");
            // Should not throw during transitions
            Assert.IsTrue(true, "Biome transitions should complete without errors");
        }

        [Test]
        public void BiomeApplicator_WithEnvironmentalEffects_AppliesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeWithEffects = CreateTestBiomeWithEffects();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);

            // Act
            var biomeResult = biomeApplicator.ApplyBiomeToMap(map, biomeWithEffects);

            // Assert
            Assert.IsTrue(biomeResult.Success, "Should apply biome with effects successfully");
            Assert.IsTrue(biomeResult.AppliedEffects.Any(e => e.Contains("Environmental")), 
                "Should apply environmental effects");
        }

        [Test]
        public void BiomeApplicator_WithAudioConfiguration_AppliesCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeWithAudio = CreateTestBiomeWithAudio();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);

            // Act
            var biomeResult = biomeApplicator.ApplyBiomeToMap(map, biomeWithAudio);

            // Assert
            Assert.IsTrue(biomeResult.Success, "Should apply biome with audio successfully");
            Assert.IsTrue(biomeResult.AppliedEffects.Any(e => e.Contains("Music") || e.Contains("Sound")), 
                "Should apply audio effects");
        }

        [Test]
        public void BiomeApplicator_ClearEffects_RestoresCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);

            // Apply biome
            var biomeResult = biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            var metricsBefore = biomeApplicator.GetMetrics();

            // Clear effects
            biomeApplicator.ClearBiomeEffects();
            var metricsAfter = biomeApplicator.GetMetrics();

            // Assert
            Assert.IsTrue(biomeResult.Success, "Should apply biome successfully");
            Assert.AreEqual(0, metricsAfter.AppliedBiomesCount, "Should clear applied biomes");
            Assert.AreEqual(0, metricsAfter.ActiveEffectsCount, "Should clear active effects");
            Assert.IsTrue(metricsAfter.AppliedBiomesCount < metricsBefore.AppliedBiomesCount, 
                "Should reduce applied biome count");
        }

        [Test]
        public void BiomeApplicator_Validation_InvalidBiomeFailsGracefully()
        {
            // Arrange
            var map = CreateTestMap();
            var invalidBiome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);

            // Act & Assert
            var result = biomeApplicator.ApplyBiomeToMap(map, invalidBiome);
            
            Assert.IsFalse(result.Success, "Should fail with invalid biome");
            Assert.IsNotNull(result.ErrorMessage, "Should provide error message");
            Assert.IsTrue(result.ErrorMessage.Contains("validation failed"), 
                "Error message should mention validation failure");
        }

        [Test]
        public void BiomeApplicator_Performance_LargeMapWithinTarget()
        {
            // Arrange
            var map = CreateLargeTestMap(100);
            var biomeApplicator = new BiomeApplicator(_mockAssetLoader, 42);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = biomeApplicator.ApplyBiomeToMap(map, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(result.Success, "Should apply biome to large map successfully");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
                $"Large map biome application took {stopwatch.ElapsedMilliseconds}ms, target < 300ms");

            UnityEngine.Debug.Log($"Large map biome application: {stopwatch.ElapsedMilliseconds}ms, " +
                                 $"{result.ProcessedRooms.Count} rooms processed");
        }

        [Test]
        public void MapContentPopulator_DifferentBiomes_AdaptsCorrectly()
        {
            // Arrange
            var map = CreateTestMap();
            var officeBiome = CreateTestBiome("OfficeTheme");
            var serverBiome = CreateTestBiome("ServerTheme");

            // Act
            _contentPopulator.PopulateContent(map, officeBiome);
            var officeResult = _contentPopulator.ApplyBiome(map, officeBiome);

            // Clear and apply different biome
            _contentPopulator.ClearContent();
            _contentPopulator.PopulateContent(map, serverBiome);
            var serverResult = _contentPopulator.ApplyBiome(map, serverBiome);

            // Assert
            Assert.IsTrue(officeResult.Success, "Should apply office biome");
            Assert.IsTrue(serverResult.Success, "Should apply server biome");
            Assert.AreNotEqual(officeResult.AppliedEffects.Count, serverResult.AppliedEffects.Count, 
                "Different biomes should have different effects");
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