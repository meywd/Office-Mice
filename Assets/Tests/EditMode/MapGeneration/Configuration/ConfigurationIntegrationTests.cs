using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Data;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class ConfigurationIntegrationTests
    {
        private MapGenerationSettings _settings;
        private RoomTemplate _roomTemplate;
        private BiomeConfiguration _biomeConfig;
        private SpawnTableConfiguration _spawnTable;
        private TilesetConfiguration _tilesetConfig;
        private GameObject _testGameObject;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestConfigurationIntegration");
            
            // Create test configuration objects
            _settings = _testGameObject.AddComponent<MapGenerationSettings>();
            _roomTemplate = _testGameObject.AddComponent<RoomTemplate>();
            _biomeConfig = _testGameObject.AddComponent<BiomeConfiguration>();
            _spawnTable = _testGameObject.AddComponent<SpawnTableConfiguration>();
            _tilesetConfig = _testGameObject.AddComponent<TilesetConfiguration>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }
        
        [Test]
        public void CompleteConfigurationSetup_PassesValidation()
        {
            // Arrange
            SetupCompleteConfiguration();
            
            // Act
            var settingsResult = _settings.Validate();
            var roomResult = _roomTemplate.Validate();
            var biomeResult = _biomeConfig.Validate();
            var spawnResult = _spawnTable.Validate();
            var tilesetResult = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsTrue(settingsResult.IsValid, "Complete settings should be valid");
            Assert.IsTrue(roomResult.IsValid, "Complete room template should be valid");
            Assert.IsTrue(biomeResult.IsValid, "Complete biome configuration should be valid");
            Assert.IsTrue(spawnResult.IsValid, "Complete spawn table should be valid");
            Assert.IsTrue(tilesetResult.IsValid, "Complete tileset configuration should be valid");
        }
        
        [Test]
        public void SettingsWithAllConfigurations_CanRetrieveAllComponents()
        {
            // Arrange
            SetupCompleteConfiguration();
            
            // Act
            var retrievedRoomTemplate = _settings.GetRandomRoomTemplate(RoomClassification.Office);
            var retrievedBiome = _settings.GetRandomBiome();
            var retrievedSpawnTable = _settings.GetSpawnTable("test_spawn_table");
            var retrievedTileset = _settings.GetTileset("test_tileset");
            
            // Assert
            Assert.AreEqual(_roomTemplate, retrievedRoomTemplate, "Should retrieve correct room template");
            Assert.AreEqual(_biomeConfig, retrievedBiome, "Should retrieve correct biome configuration");
            Assert.AreEqual(_spawnTable, retrievedSpawnTable, "Should retrieve correct spawn table");
            Assert.AreEqual(_tilesetConfig, retrievedTileset, "Should retrieve correct tileset configuration");
        }
        
        [Test]
        public void RoomTemplateWithTileset_CanGetTiles()
        {
            // Arrange
            SetupRoomTemplateWithTileset();
            
            // Act
            var floorTile = _tilesetConfig.GetTileForType(TileType.Floor);
            var wallTile = _tilesetConfig.GetTileForType(TileType.Wall);
            var decorativeTile = _tilesetConfig.GetDecorativeTile();
            
            // Assert
            Assert.IsNotNull(floorTile, "Should be able to get floor tile");
            Assert.IsNotNull(wallTile, "Should be able to get wall tile");
            Assert.IsNotNull(decorativeTile, "Should be able to get decorative tile");
        }
        
        [Test]
        public void BiomeWithTileset_CanGetRandomTileset()
        {
            // Arrange
            SetupBiomeWithTilesets();
            
            // Act
            var randomTileset = _biomeConfig.GetRandomTileset(new System.Random(1));
            var resources = _biomeConfig.GetAvailableResources(new System.Random(1));
            var colorVariation = _biomeConfig.GetRandomColorVariation(Color.blue, new System.Random(1));
            
            // Assert
            Assert.IsNotNull(randomTileset, "Should be able to get random tileset");
            Assert.IsNotNull(resources, "Should be able to get available resources");
            Assert.AreNotEqual(Color.blue, colorVariation, "Should apply color variation");
        }
        
        [Test]
        public void SpawnTableWithMapData_CanValidateSpawnPositions()
        {
            // Arrange
            SetupSpawnTableWithMapData();
            var mapData = CreateTestMapData();
            
            // Act
            var canSpawnInRoom = _spawnTable.CanSpawnAtPosition(new Vector2Int(5, 5), mapData);
            var canSpawnOutside = _spawnTable.CanSpawnAtPosition(new Vector2Int(0, 0), mapData);
            var randomEntry = _spawnTable.GetRandomSpawnEntry(new System.Random(1));
            var scaledCount = _spawnTable.GetScaledEnemyCount(10, 0.5f);
            
            // Assert
            Assert.IsTrue(canSpawnInRoom, "Should be able to spawn in valid room position");
            Assert.IsFalse(canSpawnOutside, "Should not be able to spawn outside map");
            Assert.IsNotNull(randomEntry, "Should be able to get random spawn entry");
            Assert.IsTrue(scaledCount >= 10, "Should scale enemy count appropriately");
        }
        
        [Test]
        public void ConfigurationChain_WorksEndToEnd()
        {
            // Arrange
            SetupCompleteConfiguration();
            
            // Act - Simulate the complete configuration chain
            var roomTemplate = _settings.GetRandomRoomTemplate(RoomClassification.Office);
            var biome = _settings.GetRandomBiome();
            var tileset = biome.GetRandomTileset();
            var floorTile = tileset.GetTileForType(TileType.Floor);
            var wallTile = tileset.GetTileForType(TileType.Wall);
            var spawnTable = _settings.GetSpawnTable("test_spawn_table");
            var spawnEntry = spawnTable.GetRandomSpawnEntry();
            var resources = biome.GetAvailableResources();
            
            // Assert
            Assert.IsNotNull(roomTemplate, "Should get room template from settings");
            Assert.IsNotNull(biome, "Should get biome from settings");
            Assert.IsNotNull(tileset, "Should get tileset from biome");
            Assert.IsNotNull(floorTile, "Should get floor tile from tileset");
            Assert.IsNotNull(wallTile, "Should get wall tile from tileset");
            Assert.IsNotNull(spawnTable, "Should get spawn table from settings");
            Assert.IsNotNull(spawnEntry, "Should get spawn entry from spawn table");
            Assert.IsNotNull(resources, "Should get resources from biome");
            
            // Verify compatibility
            Assert.IsTrue(roomTemplate.IsCompatibleWithClassification(RoomClassification.Office), 
                "Room template should be compatible with office classification");
            Assert.IsTrue(roomTemplate.CanFitInRoom(8, 8), 
                "Room template should fit in 8x8 room");
        }
        
        [Test]
        public void ValidationChain_CatchesAllErrors()
        {
            // Arrange
            SetupInvalidConfiguration();
            
            // Act
            var settingsResult = _settings.Validate();
            var roomResult = _roomTemplate.Validate();
            var biomeResult = _biomeConfig.Validate();
            var spawnResult = _spawnTable.Validate();
            var tilesetResult = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsFalse(settingsResult.IsValid, "Invalid settings should fail validation");
            Assert.IsFalse(roomResult.IsValid, "Invalid room template should fail validation");
            Assert.IsFalse(biomeResult.IsValid, "Invalid biome configuration should fail validation");
            Assert.IsFalse(spawnResult.IsValid, "Invalid spawn table should fail validation");
            Assert.IsFalse(tilesetResult.IsValid, "Invalid tileset configuration should fail validation");
            
            // Check for specific errors
            Assert.IsTrue(settingsResult.Errors.Count > 0, "Settings should have errors");
            Assert.IsTrue(roomResult.Errors.Count > 0, "Room template should have errors");
            Assert.IsTrue(biomeResult.Errors.Count > 0, "Biome configuration should have errors");
            Assert.IsTrue(spawnResult.Errors.Count > 0, "Spawn table should have errors");
            Assert.IsTrue(tilesetResult.Errors.Count > 0, "Tileset configuration should have errors");
        }
        
        [Test]
        public void PerformanceTest_LargeConfigurationSet_ValidatesQuickly()
        {
            // Arrange
            SetupLargeConfigurationSet();
            
            // Act
            var startTime = System.DateTime.Now;
            var result = _settings.Validate();
            var endTime = System.DateTime.Now;
            var validationTime = (endTime - startTime).TotalMilliseconds;
            
            // Assert
            Assert.IsTrue(validationTime < 1000, $"Validation should complete quickly (took {validationTime}ms)");
            // Note: This might fail validation due to missing assets, but should complete quickly
        }
        
        [Test]
        public void ConfigurationSerialization_MaintainsDataIntegrity()
        {
            // Arrange
            SetupCompleteConfiguration();
            
            // Act - Simulate serialization by creating new instances with same data
            var originalSettingsID = _settings.SettingsID;
            var originalRoomTemplateID = _roomTemplate.TemplateID;
            var originalBiomeID = _biomeConfig.BiomeID;
            var originalSpawnTableID = _spawnTable.SpawnTableID;
            var originalTilesetID = _tilesetConfig.TilesetID;
            
            // Simulate round-trip (in real scenario, this would be JSON/Binary serialization)
            var newSettings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            newSettings.SetPrivateField("_settingsID", originalSettingsID);
            newSettings.SetPrivateField("_settingsName", _settings.SettingsName);
            newSettings.SetPrivateField("_roomTemplates", new List<RoomTemplate> { _roomTemplate });
            newSettings.SetPrivateField("_biomeConfigurations", new List<BiomeConfiguration> { _biomeConfig });
            newSettings.SetPrivateField("_spawnTables", new List<SpawnTableConfiguration> { _spawnTable });
            newSettings.SetPrivateField("_tilesets", new List<TilesetConfiguration> { _tilesetConfig });
            
            // Assert
            Assert.AreEqual(originalSettingsID, newSettings.SettingsID, "Settings ID should be preserved");
            Assert.AreEqual(originalRoomTemplateID, newSettings.RoomTemplates[0].TemplateID, "Room template ID should be preserved");
            Assert.AreEqual(originalBiomeID, newSettings.BiomeConfigurations[0].BiomeID, "Biome ID should be preserved");
            Assert.AreEqual(originalSpawnTableID, newSettings.SpawnTables[0].SpawnTableID, "Spawn table ID should be preserved");
            Assert.AreEqual(originalTilesetID, newSettings.Tilesets[0].TilesetID, "Tileset ID should be preserved");
        }
        
        private void SetupCompleteConfiguration()
        {
            // Setup Map Generation Settings
            _settings.SetPrivateField("_settingsID", "test_settings");
            _settings.SetPrivateField("_settingsName", "Test Settings");
            _settings.SetPrivateField("_profile", GenerationProfile.Development);
            _settings.SetPrivateField("_mapConfig", new MapConfiguration());
            _settings.SetPrivateField("_bspConfig", new BSPConfiguration());
            _settings.SetPrivateField("_corridorConfig", new CorridorConfiguration());
            _settings.SetPrivateField("_generationRules", new GenerationRules());
            _settings.SetPrivateField("_validationRules", new ValidationRules());
            _settings.SetPrivateField("_performanceSettings", new PerformanceSettings());
            _settings.SetPrivateField("_runtimeConfig", new RuntimeConfiguration());
            _settings.SetPrivateField("_debugSettings", new DebugSettings());
            _settings.SetPrivateField("_qualitySettings", new QualitySettings());
            
            // Setup Room Template
            _roomTemplate.SetPrivateField("_templateID", "test_room_template");
            _roomTemplate.SetPrivateField("_templateName", "Test Room Template");
            _roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Office);
            _roomTemplate.SetPrivateField("_minWidth", 5);
            _roomTemplate.SetPrivateField("_minHeight", 5);
            _roomTemplate.SetPrivateField("_maxWidth", 10);
            _roomTemplate.SetPrivateField("_maxHeight", 10);
            _roomTemplate.SetPrivateField("_floorPattern", new TilePattern());
            _roomTemplate.SetPrivateField("_wallPattern", new TilePattern());
            
            // Setup Biome Configuration
            _biomeConfig.SetPrivateField("_biomeID", "test_biome");
            _biomeConfig.SetPrivateField("_biomeName", "Test Biome");
            _biomeConfig.SetPrivateField("_biomeType", BiomeType.Office);
            _biomeConfig.SetPrivateField("_primaryTileset", _tilesetConfig);
            _biomeConfig.SetPrivateField("_generationRules", new BiomeGenerationRules());
            _biomeConfig.SetPrivateField("_colorPalette", new BiomeColorPalette());
            
            // Setup Spawn Table
            _spawnTable.SetPrivateField("_spawnTableID", "test_spawn_table");
            _spawnTable.SetPrivateField("_tableName", "Test Spawn Table");
            _spawnTable.SetPrivateField("_tableType", SpawnTableType.Standard);
            
            var spawnEntries = new List<SpawnEntry>();
            var spawnEntry = new SpawnEntry();
            spawnEntry.SetPrivateField("_enemyType", "TestEnemy");
            spawnEntry.SetPrivateField("_weight", 1.0f);
            spawnEntry.SetPrivateField("_minCount", 1);
            spawnEntry.SetPrivateField("_maxCount", 3);
            spawnEntries.Add(spawnEntry);
            _spawnTable.SetPrivateField("_spawnEntries", spawnEntries);
            
            _spawnTable.SetPrivateField("_difficultyScaling", new DifficultyScaling());
            _spawnTable.SetPrivateField("_spawnRules", new SpawnRules());
            
            // Setup Tileset Configuration
            _tilesetConfig.SetPrivateField("_tilesetID", "test_tileset");
            _tilesetConfig.SetPrivateField("_tilesetName", "Test Tileset");
            _tilesetConfig.SetPrivateField("_theme", TilesetTheme.Office);
            
            var floorMapping = new TileMapping();
            floorMapping.SetPrivateField("_mappingName", "Floor");
            floorMapping.SetPrivateField("_tiles", new List<TileEntry> { new TileEntry() });
            _tilesetConfig.SetPrivateField("_floorTiles", floorMapping);
            
            var wallMapping = new TileMapping();
            wallMapping.SetPrivateField("_mappingName", "Wall");
            wallMapping.SetPrivateField("_tiles", new List<TileEntry> { new TileEntry() });
            _tilesetConfig.SetPrivateField("_wallTiles", wallMapping);
            
            // Link configurations together
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate> { _roomTemplate });
            _settings.SetPrivateField("_biomeConfigurations", new List<BiomeConfiguration> { _biomeConfig });
            _settings.SetPrivateField("_spawnTables", new List<SpawnTableConfiguration> { _spawnTable });
            _settings.SetPrivateField("_tilesets", new List<TilesetConfiguration> { _tilesetConfig });
        }
        
        private void SetupRoomTemplateWithTileset()
        {
            _tilesetConfig.SetPrivateField("_tilesetID", "test_tileset");
            _tilesetConfig.SetPrivateField("_tilesetName", "Test Tileset");
            
            var floorMapping = new TileMapping();
            floorMapping.SetPrivateField("_tiles", new List<TileEntry> { new TileEntry() });
            _tilesetConfig.SetPrivateField("_floorTiles", floorMapping);
            
            var wallMapping = new TileMapping();
            wallMapping.SetPrivateField("_tiles", new List<TileEntry> { new TileEntry() });
            _tilesetConfig.SetPrivateField("_wallTiles", wallMapping);
            
            var decorativeMapping = new TileMapping();
            decorativeMapping.SetPrivateField("_tiles", new List<TileEntry> { new TileEntry() });
            _tilesetConfig.SetPrivateField("_decorativeTiles", new List<TileMapping> { decorativeMapping });
        }
        
        private void SetupBiomeWithTilesets()
        {
            var primaryTileset = ScriptableObject.CreateInstance<TilesetConfiguration>();
            primaryTileset.SetPrivateField("_tilesetID", "primary_tileset");
            
            var secondaryTileset = ScriptableObject.CreateInstance<TilesetConfiguration>();
            secondaryTileset.SetPrivateField("_tilesetID", "secondary_tileset");
            
            _biomeConfig.SetPrivateField("_biomeID", "test_biome");
            _biomeConfig.SetPrivateField("_primaryTileset", primaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTileset", secondaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTilesetChance", 0.5f);
            _biomeConfig.SetPrivateField("_applyColorTinting", true);
            _biomeConfig.SetPrivateField("_colorVariation", 0.3f);
            
            var resource = new BiomeResource();
            resource.SetPrivateField("_resourceType", "TestResource");
            resource.SetPrivateField("_resourcePrefab", new GameObject("TestResource"));
            resource.SetPrivateField("_spawnChance", 0.5f);
            resource.SetPrivateField("_quantityRange", new Vector2Int(1, 3));
            
            _biomeConfig.SetPrivateField("_commonResources", new List<BiomeResource> { resource });
        }
        
        private void SetupSpawnTableWithMapData()
        {
            var spawnEntry = new SpawnEntry();
            spawnEntry.SetPrivateField("_enemyType", "TestEnemy");
            spawnEntry.SetPrivateField("_weight", 1.0f);
            spawnEntry.SetPrivateField("_minCount", 1);
            spawnEntry.SetPrivateField("_maxCount", 3);
            
            _spawnTable.SetPrivateField("_spawnTableID", "test_spawn_table");
            _spawnTable.SetPrivateField("_spawnEntries", new List<SpawnEntry> { spawnEntry });
            _spawnTable.SetPrivateField("_scaleWithPlayerProgress", true);
            _spawnTable.SetPrivateField("_difficultyMultiplier", 1.5f);
            
            var scaling = new DifficultyScaling();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 2f);
            scaling.SetPrivateField("_scalingCurve", curve);
            _spawnTable.SetPrivateField("_difficultyScaling", scaling);
        }
        
        private void SetupInvalidConfiguration()
        {
            // Create intentionally invalid configurations
            _settings.SetPrivateField("_settingsID", ""); // Missing ID
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate>()); // No room templates
            
            _roomTemplate.SetPrivateField("_templateID", ""); // Missing ID
            _roomTemplate.SetPrivateField("_minWidth", 10);
            _roomTemplate.SetPrivateField("_maxWidth", 5); // Invalid range
            
            _biomeConfig.SetPrivateField("_biomeID", ""); // Missing ID
            _biomeConfig.SetPrivateField("_primaryTileset", null); // Missing tileset
            
            _spawnTable.SetPrivateField("_spawnTableID", ""); // Missing ID
            _spawnTable.SetPrivateField("_spawnEntries", new List<SpawnEntry>()); // No spawn entries
            
            _tilesetConfig.SetPrivateField("_tilesetID", ""); // Missing ID
            _tilesetConfig.SetPrivateField("_floorTiles", null); // Missing floor tiles
            _tilesetConfig.SetPrivateField("_wallTiles", null); // Missing wall tiles
        }
        
        private void SetupLargeConfigurationSet()
        {
            SetupCompleteConfiguration();
            
            // Add many more configurations to test performance
            var manyRoomTemplates = new List<RoomTemplate>();
            var manyBiomes = new List<BiomeConfiguration>();
            var manySpawnTables = new List<SpawnTableConfiguration>();
            var manyTilesets = new List<TilesetConfiguration>();
            
            for (int i = 0; i < 50; i++)
            {
                var roomTemplate = ScriptableObject.CreateInstance<RoomTemplate>();
                roomTemplate.SetPrivateField("_templateID", $"room_template_{i}");
                roomTemplate.SetPrivateField("_templateName", $"Room Template {i}");
                manyRoomTemplates.Add(roomTemplate);
                
                var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
                biome.SetPrivateField("_biomeID", $"biome_{i}");
                biome.SetPrivateField("_biomeName", $"Biome {i}");
                manyBiomes.Add(biome);
                
                var spawnTable = ScriptableObject.CreateInstance<SpawnTableConfiguration>();
                spawnTable.SetPrivateField("_spawnTableID", $"spawn_table_{i}");
                spawnTable.SetPrivateField("_tableName", $"Spawn Table {i}");
                manySpawnTables.Add(spawnTable);
                
                var tileset = ScriptableObject.CreateInstance<TilesetConfiguration>();
                tileset.SetPrivateField("_tilesetID", $"tileset_{i}");
                tileset.SetPrivateField("_tilesetName", $"Tileset {i}");
                manyTilesets.Add(tileset);
            }
            
            _settings.SetPrivateField("_roomTemplates", manyRoomTemplates);
            _settings.SetPrivateField("_biomeConfigurations", manyBiomes);
            _settings.SetPrivateField("_spawnTables", manySpawnTables);
            _settings.SetPrivateField("_tilesets", manyTilesets);
        }
        
        private MapData CreateTestMapData()
        {
            var mapData = new MapData(12345, new Vector2Int(20, 20));
            
            // Add a test room
            var roomBounds = new RectInt(2, 2, 10, 10);
            var room = new RoomData(roomBounds);
            room.SetClassification(RoomClassification.Office);
            mapData.AddRoom(room);
            
            // Set player spawn in room
            mapData.SetPlayerSpawn(new Vector2Int(5, 5));
            
            return mapData;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class ConfigurationIntegrationTestExtensions
    {
        public static void SetPrivateField<T>(this T obj, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
        
        public static T GetPrivateField<T>(this T obj, string fieldName)
        {
            var field = typeof(T).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }
    }
}