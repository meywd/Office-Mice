using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;
using System.IO;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class ConfigurationAssetTests
    {
        private string _testAssetPath;
        
        [SetUp]
        public void SetUp()
        {
            _testAssetPath = "Assets/Tests/TestAssets";
            
            // Create test directory if it doesn't exist
            if (!Directory.Exists(_testAssetPath))
            {
                Directory.CreateDirectory(_testAssetPath);
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test assets
            if (Directory.Exists(_testAssetPath))
            {
                Directory.Delete(_testAssetPath, true);
                
                // Refresh AssetDatabase to recognize deletion
                AssetDatabase.Refresh();
            }
        }
        
        [Test]
        public void CreateRoomTemplateAsset_CreatesValidAsset()
        {
            // Arrange
            var assetPath = Path.Combine(_testAssetPath, "TestRoomTemplate.asset");
            
            // Act
            var roomTemplate = ScriptableObject.CreateInstance<RoomTemplate>();
            roomTemplate.SetPrivateField("_templateID", "test_room_template");
            roomTemplate.SetPrivateField("_templateName", "Test Room Template");
            roomTemplate.SetPrivateField("_description", "A test room template for unit testing");
            roomTemplate.SetPrivateField("_minWidth", 5);
            roomTemplate.SetPrivateField("_minHeight", 5);
            roomTemplate.SetPrivateField("_maxWidth", 10);
            roomTemplate.SetPrivateField("_maxHeight", 10);
            roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Office);
            
            // Create floor pattern
            var floorPattern = new TilePattern();
            floorPattern.SetPrivateField("_patternName", "TestFloor");
            floorPattern.SetPrivateField("_probability", 1.0f);
            roomTemplate.SetPrivateField("_floorPattern", floorPattern);
            
            // Create wall pattern
            var wallPattern = new TilePattern();
            wallPattern.SetPrivateField("_patternName", "TestWall");
            wallPattern.SetPrivateField("_probability", 1.0f);
            roomTemplate.SetPrivateField("_wallPattern", wallPattern);
            
            AssetDatabase.CreateAsset(roomTemplate, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Load the asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<RoomTemplate>(assetPath);
            
            // Assert
            Assert.IsNotNull(loadedAsset, "Room template asset should be created and loadable");
            Assert.AreEqual("test_room_template", loadedAsset.TemplateID);
            Assert.AreEqual("Test Room Template", loadedAsset.TemplateName);
            Assert.AreEqual(RoomClassification.Office, loadedAsset.RequiredClassification);
            
            // Validate the loaded asset
            var validationResult = loadedAsset.Validate();
            Assert.IsTrue(validationResult.IsValid, "Loaded room template should be valid");
        }
        
        [Test]
        public void CreateBiomeConfigurationAsset_CreatesValidAsset()
        {
            // Arrange
            var assetPath = Path.Combine(_testAssetPath, "TestBiomeConfiguration.asset");
            
            // Act
            var biomeConfig = ScriptableObject.CreateInstance<BiomeConfiguration>();
            biomeConfig.SetPrivateField("_biomeID", "test_biome");
            biomeConfig.SetPrivateField("_biomeName", "Test Biome");
            biomeConfig.SetPrivateField("_description", "A test biome configuration for unit testing");
            biomeConfig.SetPrivateField("_biomeType", BiomeType.Office);
            biomeConfig.SetPrivateField("_secondaryTilesetChance", 0.3f);
            biomeConfig.SetPrivateField("_applyColorTinting", true);
            biomeConfig.SetPrivateField("_colorVariation", 0.2f);
            biomeConfig.SetPrivateField("_ambientLightIntensity", 1.0f);
            biomeConfig.SetPrivateField("_ambientLightColor", Color.white);
            biomeConfig.SetPrivateField("_musicVolume", 0.5f);
            biomeConfig.SetPrivateField("_soundVolume", 0.3f);
            biomeConfig.SetPrivateField("_rareResourceChance", 0.1f);
            
            // Create generation rules
            var generationRules = new BiomeGenerationRules();
            generationRules.SetPrivateField("_roomSizeRange", new Vector2Int(5, 15));
            generationRules.SetPrivateField("_maxRooms", 10);
            generationRules.SetPrivateField("_roomDensity", 0.6f);
            biomeConfig.SetPrivateField("_generationRules", generationRules);
            
            // Create color palette
            var colorPalette = new BiomeColorPalette();
            colorPalette.SetPrivateField("_paletteName", "TestPalette");
            colorPalette.SetPrivateField("_primaryColor", Color.blue);
            colorPalette.SetPrivateField("_secondaryColor", Color.gray);
            colorPalette.SetPrivateField("_accentColor", Color.yellow);
            biomeConfig.SetPrivateField("_colorPalette", colorPalette);
            
            AssetDatabase.CreateAsset(biomeConfig, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Load the asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<BiomeConfiguration>(assetPath);
            
            // Assert
            Assert.IsNotNull(loadedAsset, "Biome configuration asset should be created and loadable");
            Assert.AreEqual("test_biome", loadedAsset.BiomeID);
            Assert.AreEqual("Test Biome", loadedAsset.BiomeName);
            Assert.AreEqual(BiomeType.Office, loadedAsset.BiomeType);
            Assert.AreEqual(0.3f, loadedAsset.SecondaryTilesetChance);
            
            // Validate the loaded asset
            var validationResult = loadedAsset.Validate();
            // Note: This will fail validation due to missing primary tileset, which is expected
            Assert.IsFalse(validationResult.IsValid, "Biome config without primary tileset should fail validation");
        }
        
        [Test]
        public void CreateSpawnTableConfigurationAsset_CreatesValidAsset()
        {
            // Arrange
            var assetPath = Path.Combine(_testAssetPath, "TestSpawnTableConfiguration.asset");
            
            // Act
            var spawnTable = ScriptableObject.CreateInstance<SpawnTableConfiguration>();
            spawnTable.SetPrivateField("_spawnTableID", "test_spawn_table");
            spawnTable.SetPrivateField("_tableName", "Test Spawn Table");
            spawnTable.SetPrivateField("_description", "A test spawn table for unit testing");
            spawnTable.SetPrivateField("_tableType", SpawnTableType.Standard);
            spawnTable.SetPrivateField("_ensureMinimumSpawns", true);
            spawnTable.SetPrivateField("_minimumSpawns", 1);
            spawnTable.SetPrivateField("_wavePattern", WavePattern.Sequential);
            spawnTable.SetPrivateField("_maxConcurrentEnemies", 5);
            spawnTable.SetPrivateField("_spawnDelayBetweenWaves", 2f);
            spawnTable.SetPrivateField("_scaleWithPlayerProgress", true);
            spawnTable.SetPrivateField("_difficultyMultiplier", 1f);
            spawnTable.SetPrivateField("_respectExistingSpawnPoints", true);
            spawnTable.SetPrivateField("_useWaveSpawnerSystem", true);
            spawnTable.SetPrivateField("_spawnPointTag", "Spawn Point");
            spawnTable.SetPrivateField("_useObjectPooling", true);
            spawnTable.SetPrivateField("_spawnRadius", 10f);
            
            // Create spawn entries
            var spawnEntries = new List<SpawnEntry>();
            var entry1 = new SpawnEntry();
            entry1.SetPrivateField("_enemyType", "Guard");
            entry1.SetPrivateField("_weight", 2.0f);
            entry1.SetPrivateField("_minCount", 1);
            entry1.SetPrivateField("_maxCount", 2);
            entry1.SetPrivateField("_spawnDelay", 0.5f);
            entry1.SetPrivateField("_healthMultiplier", 1.0f);
            entry1.SetPrivateField("_damageMultiplier", 1.0f);
            spawnEntries.Add(entry1);
            
            var entry2 = new SpawnEntry();
            entry2.SetPrivateField("_enemyType", "EliteGuard");
            entry2.SetPrivateField("_weight", 1.0f);
            entry2.SetPrivateField("_minCount", 1);
            entry2.SetPrivateField("_maxCount", 1);
            entry2.SetPrivateField("_spawnDelay", 1.0f);
            entry2.SetPrivateField("_healthMultiplier", 2.0f);
            entry2.SetPrivateField("_damageMultiplier", 1.5f);
            spawnEntries.Add(entry2);
            
            spawnTable.SetPrivateField("_spawnEntries", spawnEntries);
            
            // Create difficulty scaling
            var difficultyScaling = new DifficultyScaling();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 2f);
            difficultyScaling.SetPrivateField("_scalingCurve", curve);
            difficultyScaling.SetPrivateField("_maxScaling", 5f);
            difficultyScaling.SetPrivateField("_scaleEnemyCount", true);
            difficultyScaling.SetPrivateField("_scaleEnemyHealth", true);
            difficultyScaling.SetPrivateField("_scaleEnemyDamage", true);
            spawnTable.SetPrivateField("_difficultyScaling", difficultyScaling);
            
            // Create spawn rules
            var spawnRules = new SpawnRules();
            spawnRules.SetPrivateField("_avoidPlayerLineOfSight", true);
            spawnRules.SetPrivateField("_minDistanceFromPlayer", 5f);
            spawnRules.SetPrivateField("_maxDistanceFromPlayer", 20f);
            spawnRules.SetPrivateField("_avoidClustering", true);
            spawnRules.SetPrivateField("_minDistanceBetweenSpawns", 3f);
            spawnRules.SetPrivateField("_respectRoomCapacity", true);
            spawnRules.SetPrivateField("_maxEnemiesPerRoom", 10);
            spawnTable.SetPrivateField("_spawnRules", spawnRules);
            
            AssetDatabase.CreateAsset(spawnTable, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Load the asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<SpawnTableConfiguration>(assetPath);
            
            // Assert
            Assert.IsNotNull(loadedAsset, "Spawn table configuration asset should be created and loadable");
            Assert.AreEqual("test_spawn_table", loadedAsset.SpawnTableID);
            Assert.AreEqual("Test Spawn Table", loadedAsset.TableName);
            Assert.AreEqual(SpawnTableType.Standard, loadedAsset.TableType);
            Assert.AreEqual(2, loadedAsset.SpawnEntries.Count);
            Assert.AreEqual(5, loadedAsset.MaxConcurrentEnemies);
            
            // Validate the loaded asset
            var validationResult = loadedAsset.Validate();
            // Note: This will fail validation due to missing enemy prefabs, which is expected
            Assert.IsFalse(validationResult.IsValid, "Spawn table without enemy prefabs should fail validation");
        }
        
        [Test]
        public void CreateTilesetConfigurationAsset_CreatesValidAsset()
        {
            // Arrange
            var assetPath = Path.Combine(_testAssetPath, "TestTilesetConfiguration.asset");
            
            // Act
            var tilesetConfig = ScriptableObject.CreateInstance<TilesetConfiguration>();
            tilesetConfig.SetPrivateField("_tilesetID", "test_tileset");
            tilesetConfig.SetPrivateField("_tilesetName", "Test Tileset");
            tilesetConfig.SetPrivateField("_description", "A test tileset configuration for unit testing");
            tilesetConfig.SetPrivateField("_theme", TilesetTheme.Office);
            tilesetConfig.SetPrivateField("_decorationDensity", 0.2f);
            tilesetConfig.SetPrivateField("_randomizeDecorations", true);
            tilesetConfig.SetPrivateField("_variationChance", 0.3f);
            tilesetConfig.SetPrivateField("_useVariationsForFloors", true);
            tilesetConfig.SetPrivateField("_useVariationsForWalls", true);
            tilesetConfig.SetPrivateField("_applyRulesAutomatically", true);
            tilesetConfig.SetPrivateField("_tileSize", new Vector2Int(16, 16));
            tilesetConfig.SetPrivateField("_tilesPerRow", 16);
            tilesetConfig.SetPrivateField("_enableTileColliders", true);
            tilesetConfig.SetPrivateField("_useTilemapCollider2D", true);
            
            // Create floor tiles mapping
            var floorMapping = new TileMapping();
            floorMapping.SetPrivateField("_mappingName", "FloorTiles");
            floorMapping.SetPrivateField("_useRandomSelection", true);
            floorMapping.SetPrivateField("_defaultIndex", 0);
            
            var floorTiles = new List<TileEntry>();
            var floorTile = new TileEntry();
            floorTile.SetPrivateField("_weight", 1.0f);
            floorTile.SetPrivateField("_isWalkable", true);
            floorTile.SetPrivateField("_hasCollision", false);
            floorTiles.Add(floorTile);
            floorMapping.SetPrivateField("_tiles", floorTiles);
            
            tilesetConfig.SetPrivateField("_floorTiles", floorMapping);
            
            // Create wall tiles mapping
            var wallMapping = new TileMapping();
            wallMapping.SetPrivateField("_mappingName", "WallTiles");
            wallMapping.SetPrivateField("_useRandomSelection", true);
            wallMapping.SetPrivateField("_defaultIndex", 0);
            
            var wallTiles = new List<TileEntry>();
            var wallTile = new TileEntry();
            wallTile.SetPrivateField("_weight", 1.0f);
            wallTile.SetPrivateField("_isWalkable", false);
            wallTile.SetPrivateField("_hasCollision", true);
            wallTiles.Add(wallTile);
            wallMapping.SetPrivateField("_tiles", wallTiles);
            
            tilesetConfig.SetPrivateField("_wallTiles", wallMapping);
            
            AssetDatabase.CreateAsset(tilesetConfig, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Load the asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<TilesetConfiguration>(assetPath);
            
            // Assert
            Assert.IsNotNull(loadedAsset, "Tileset configuration asset should be created and loadable");
            Assert.AreEqual("test_tileset", loadedAsset.TilesetID);
            Assert.AreEqual("Test Tileset", loadedAsset.TilesetName);
            Assert.AreEqual(TilesetTheme.Office, loadedAsset.Theme);
            Assert.AreEqual(0.2f, loadedAsset.DecorationDensity);
            
            // Validate the loaded asset
            var validationResult = loadedAsset.Validate();
            // Note: This will fail validation due to missing tile assets, which is expected
            Assert.IsFalse(validationResult.IsValid, "Tileset without tile assets should fail validation");
        }
        
        [Test]
        public void CreateMapGenerationSettingsAsset_CreatesValidAsset()
        {
            // Arrange
            var assetPath = Path.Combine(_testAssetPath, "TestMapGenerationSettings.asset");
            
            // Act
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.SetPrivateField("_settingsID", "test_settings");
            settings.SetPrivateField("_settingsName", "Test Settings");
            settings.SetPrivateField("_description", "A test map generation settings for unit testing");
            settings.SetPrivateField("_profile", GenerationProfile.Development);
            settings.SetPrivateField("_allowRuntimeModification", false);
            
            // Create map configuration
            var mapConfig = new MapConfiguration();
            mapConfig.SetPrivateField("_mapSizeRange", new Vector2Int(50, 100));
            mapConfig.SetPrivateField("_roomSizeRange", new Vector2Int(5, 15));
            mapConfig.SetPrivateField("_minRooms", 5);
            mapConfig.SetPrivateField("_maxRooms", 20);
            mapConfig.SetPrivateField("_roomDensity", 0.6f);
            mapConfig.SetPrivateField("_useRandomSeed", true);
            mapConfig.SetPrivateField("_saveSeed", true);
            settings.SetPrivateField("_mapConfig", mapConfig);
            
            // Create BSP configuration
            var bspConfig = new BSPConfiguration();
            bspConfig.SetPrivateField("_minPartitionSize", 10);
            bspConfig.SetPrivateField("_splitPositionVariation", 0.3f);
            bspConfig.SetPrivateField("_allowHorizontalSplits", true);
            bspConfig.SetPrivateField("_allowVerticalSplits", true);
            bspConfig.SetPrivateField("_maxDepth", 10);
            bspConfig.SetPrivateField("_stopSplittingChance", 0.1f);
            bspConfig.SetPrivateField("_balanceTree", true);
            bspConfig.SetPrivateField("_roomSizeRatio", 0.8f);
            bspConfig.SetPrivateField("_roomPositionVariation", 0.1f);
            settings.SetPrivateField("_bspConfig", bspConfig);
            
            // Create corridor configuration
            var corridorConfig = new CorridorConfiguration();
            corridorConfig.SetPrivateField("_corridorType", CorridorType.LShaped);
            corridorConfig.SetPrivateField("_minWidth", 1);
            corridorConfig.SetPrivateField("_maxWidth", 3);
            corridorConfig.SetPrivateField("_ensureDirectPath", true);
            corridorConfig.SetPrivateField("_avoidRooms", true);
            corridorConfig.SetPrivateField("_pathSmoothing", 0.3f);
            settings.SetPrivateField("_corridorConfig", corridorConfig);
            
            // Create generation rules
            var generationRules = new GenerationRules();
            generationRules.SetPrivateField("_ensureAllRoomsReachable", true);
            generationRules.SetPrivateField("_createLoops", false);
            generationRules.SetPrivateField("_loopChance", 0.2f);
            generationRules.SetPrivateField("_secretRoomChance", 0.1f);
            generationRules.SetPrivateField("_treasureRoomChance", 0.05f);
            generationRules.SetPrivateField("_bossRoomChance", 0.1f);
            generationRules.SetPrivateField("_balanceRoomDistribution", true);
            generationRules.SetPrivateField("_clusterSimilarRooms", false);
            generationRules.SetPrivateField("_randomnessFactor", 0.5f);
            settings.SetPrivateField("_generationRules", generationRules);
            
            // Create validation rules
            var validationRules = new ValidationRules();
            validationRules.SetPrivateField("_validateConnectivity", true);
            validationRules.SetPrivateField("_validateRoomSizes", true);
            validationRules.SetPrivateField("_validateCorridorWidths", true);
            validationRules.SetPrivateField("_rejectInvalidMaps", true);
            validationRules.SetPrivateField("_maxRetryAttempts", 3);
            validationRules.SetPrivateField("_logValidationDetails", true);
            validationRules.SetPrivateField("_minWalkableRatio", 0.3f);
            validationRules.SetPrivateField("_minRoomCount", 3);
            validationRules.SetPrivateField("_maxDeadEndRatio", 0.5f);
            settings.SetPrivateField("_validationRules", validationRules);
            
            // Create performance settings
            var performanceSettings = new PerformanceSettings();
            performanceSettings.SetPrivateField("_enableMultithreading", false);
            performanceSettings.SetPrivateField("_generationTimeoutMs", 10000);
            performanceSettings.SetPrivateField("_useIncrementalGeneration", false);
            performanceSettings.SetPrivateField("_poolObjects", true);
            performanceSettings.SetPrivateField("_reuseTilemaps", false);
            performanceSettings.SetPrivateField("_maxPoolSize", 100);
            performanceSettings.SetPrivateField("_enableLOD", false);
            performanceSettings.SetPrivateField("_lodLevels", 3);
            performanceSettings.SetPrivateField("_lodDistance", 50f);
            settings.SetPrivateField("_performanceSettings", performanceSettings);
            
            // Create runtime configuration
            var runtimeConfig = new RuntimeConfiguration();
            runtimeConfig.SetPrivateField("_generateOnStart", false);
            runtimeConfig.SetPrivateField("_generateAsync", true);
            runtimeConfig.SetPrivateField("_generationDelay", 0f);
            runtimeConfig.SetPrivateField("_autoSaveMaps", false);
            runtimeConfig.SetPrivateField("_allowMapSaving", true);
            runtimeConfig.SetPrivateField("_allowMapLoading", true);
            runtimeConfig.SetPrivateField("_allowRuntimeRegeneration", false);
            runtimeConfig.SetPrivateField("_allowParameterModification", false);
            runtimeConfig.SetPrivateField("_requireAdminPrivileges", true);
            settings.SetPrivateField("_runtimeConfig", runtimeConfig);
            
            // Create debug settings
            var debugSettings = new DebugSettings();
            debugSettings.SetPrivateField("_showGizmos", true);
            debugSettings.SetPrivateField("_showRoomLabels", false);
            debugSettings.SetPrivateField("_showConnectivity", false);
            debugSettings.SetPrivateField("_colorizeRooms", false);
            debugSettings.SetPrivateField("_enableLogging", true);
            debugSettings.SetPrivateField("_logGenerationSteps", false);
            debugSettings.SetPrivateField("_logPerformanceMetrics", false);
            debugSettings.SetPrivateField("_logValidationResults", true);
            debugSettings.SetPrivateField("_enableTestMode", false);
            debugSettings.SetPrivateField("_testSeed", 12345);
            debugSettings.SetPrivateField("_runValidationTests", true);
            settings.SetPrivateField("_debugSettings", debugSettings);
            
            // Create quality settings
            var qualitySettings = new QualitySettings();
            qualitySettings.SetPrivateField("_quality", GenerationQuality.Medium);
            qualitySettings.SetPrivateField("_adaptiveQuality", false);
            qualitySettings.SetPrivateField("_qualityThreshold", 60f);
            qualitySettings.SetPrivateField("_decorationQuality", 0.5f);
            qualitySettings.SetPrivateField("_lightingQuality", 0.7f);
            qualitySettings.SetPrivateField("_effectsQuality", 0.3f);
            qualitySettings.SetPrivateField("_enableOcclusionCulling", true);
            qualitySettings.SetPrivateField("_enableFrustumCulling", true);
            qualitySettings.SetPrivateField("_batchTileOperations", true);
            settings.SetPrivateField("_qualitySettings", qualitySettings);
            
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Load the asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(assetPath);
            
            // Assert
            Assert.IsNotNull(loadedAsset, "Map generation settings asset should be created and loadable");
            Assert.AreEqual("test_settings", loadedAsset.SettingsID);
            Assert.AreEqual("Test Settings", loadedAsset.SettingsName);
            Assert.AreEqual(GenerationProfile.Development, loadedAsset.Profile);
            Assert.IsFalse(loadedAsset.AllowRuntimeModification);
            
            // Validate the loaded asset
            var validationResult = loadedAsset.Validate();
            // Note: This will fail validation due to missing room templates and tilesets, which is expected
            Assert.IsFalse(validationResult.IsValid, "Settings without room templates and tilesets should fail validation");
        }
        
        [Test]
        public void AllConfigurationAssets_HaveCreateAssetMenuAttributes()
        {
            // Arrange & Act
            var roomTemplateType = typeof(RoomTemplate);
            var biomeConfigType = typeof(BiomeConfiguration);
            var spawnTableType = typeof(SpawnTableConfiguration);
            var tilesetType = typeof(TilesetConfiguration);
            var settingsType = typeof(MapGenerationSettings);
            
            // Assert
            Assert.IsTrue(HasCreateAssetMenuAttribute(roomTemplateType), 
                "RoomTemplate should have CreateAssetMenu attribute");
            Assert.IsTrue(HasCreateAssetMenuAttribute(biomeConfigType), 
                "BiomeConfiguration should have CreateAssetMenu attribute");
            Assert.IsTrue(HasCreateAssetMenuAttribute(spawnTableType), 
                "SpawnTableConfiguration should have CreateAssetMenu attribute");
            Assert.IsTrue(HasCreateAssetMenuAttribute(tilesetType), 
                "TilesetConfiguration should have CreateAssetMenu attribute");
            Assert.IsTrue(HasCreateAssetMenuAttribute(settingsType), 
                "MapGenerationSettings should have CreateAssetMenu attribute");
        }
        
        private bool HasCreateAssetMenuAttribute(System.Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            return attributes.Length > 0;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class ConfigurationAssetTestExtensions
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