using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class MapGenerationSettingsTests
    {
        private MapGenerationSettings _settings;
        private GameObject _testGameObject;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestMapGenerationSettings");
            _settings = _testGameObject.AddComponent<MapGenerationSettings>();
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
        public void MapGenerationSettings_WithValidData_PassesValidation()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            
            // Act
            var result = _settings.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid map generation settings should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid map generation settings should have no errors");
        }
        
        [Test]
        public void MapGenerationSettings_WithMissingID_FailsValidation()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_settingsID", "");
            
            // Act
            var result = _settings.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Map generation settings with missing ID should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Settings ID is required")), 
                "Should have error about missing settings ID");
        }
        
        [Test]
        public void MapGenerationSettings_WithMissingMapConfig_FailsValidation()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_mapConfig", null);
            
            // Act
            var result = _settings.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Map generation settings with missing map config should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Map configuration is required")), 
                "Should have error about missing map configuration");
        }
        
        [Test]
        public void MapGenerationSettings_WithNoRoomTemplates_FailsValidation()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate>());
            
            // Act
            var result = _settings.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Map generation settings with no room templates should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("at least one room template")), 
                "Should have error about missing room templates");
        }
        
        [Test]
        public void MapGenerationSettings_WithNoTilesets_FailsValidation()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_tilesets", new List<TilesetConfiguration>());
            
            // Act
            var result = _settings.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Map generation settings with no tilesets should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("at least one tileset")), 
                "Should have error about missing tilesets");
        }
        
        [Test]
        public void GetRandomRoomTemplate_WithMatchingClassification_ReturnsCompatibleTemplate()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var officeTemplate = CreateTestRoomTemplate("OfficeTemplate", RoomClassification.Office);
            var storageTemplate = CreateTestRoomTemplate("StorageTemplate", RoomClassification.Storage);
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate> { officeTemplate, storageTemplate });
            
            // Act
            var result = _settings.GetRandomRoomTemplate(RoomClassification.Office, new System.Random(1));
            
            // Assert
            Assert.AreEqual(officeTemplate, result, "Should return office template for office classification");
        }
        
        [Test]
        public void GetRandomRoomTemplate_WithNoMatchingClassification_ReturnsAnyTemplate()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var officeTemplate = CreateTestRoomTemplate("OfficeTemplate", RoomClassification.Office);
            var storageTemplate = CreateTestRoomTemplate("StorageTemplate", RoomClassification.Storage);
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate> { officeTemplate, storageTemplate });
            
            // Act
            var result = _settings.GetRandomRoomTemplate(RoomClassification.Boss, new System.Random(1));
            
            // Assert
            Assert.IsTrue(result == officeTemplate || result == storageTemplate, 
                "Should return any template when no compatible template exists");
        }
        
        [Test]
        public void GetRandomRoomTemplate_WithNoTemplates_ReturnsNull()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate>());
            
            // Act
            var result = _settings.GetRandomRoomTemplate(RoomClassification.Office);
            
            // Assert
            Assert.IsNull(result, "Should return null when no room templates exist");
        }
        
        [Test]
        public void GetRandomBiome_WithBiomes_ReturnsRandomBiome()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var officeBiome = CreateTestBiomeConfiguration("OfficeBiome");
            var storageBiome = CreateTestBiomeConfiguration("StorageBiome");
            _settings.SetPrivateField("_biomeConfigurations", new List<BiomeConfiguration> { officeBiome, storageBiome });
            
            // Act
            var result = _settings.GetRandomBiome(new System.Random(1));
            
            // Assert
            Assert.IsTrue(result == officeBiome || result == storageBiome, 
                "Should return one of the available biomes");
        }
        
        [Test]
        public void GetRandomBiome_WithNoBiomes_ReturnsNull()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_biomeConfigurations", new List<BiomeConfiguration>());
            
            // Act
            var result = _settings.GetRandomBiome();
            
            // Assert
            Assert.IsNull(result, "Should return null when no biomes exist");
        }
        
        [Test]
        public void GetSpawnTable_WithExistingID_ReturnsCorrectTable()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var spawnTable = CreateTestSpawnTableConfiguration("TestSpawnTable");
            _settings.SetPrivateField("_spawnTables", new List<SpawnTableConfiguration> { spawnTable });
            
            // Act
            var result = _settings.GetSpawnTable("TestSpawnTable");
            
            // Assert
            Assert.AreEqual(spawnTable, result, "Should return correct spawn table by ID");
        }
        
        [Test]
        public void GetSpawnTable_WithNonExistingID_ReturnsNull()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var spawnTable = CreateTestSpawnTableConfiguration("TestSpawnTable");
            _settings.SetPrivateField("_spawnTables", new List<SpawnTableConfiguration> { spawnTable });
            
            // Act
            var result = _settings.GetSpawnTable("NonExistingTable");
            
            // Assert
            Assert.IsNull(result, "Should return null for non-existing spawn table ID");
        }
        
        [Test]
        public void GetTileset_WithExistingID_ReturnsCorrectTileset()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var tileset = CreateTestTilesetConfiguration("TestTileset");
            _settings.SetPrivateField("_tilesets", new List<TilesetConfiguration> { tileset });
            
            // Act
            var result = _settings.GetTileset("TestTileset");
            
            // Assert
            Assert.AreEqual(tileset, result, "Should return correct tileset by ID");
        }
        
        [Test]
        public void GetTileset_WithNonExistingID_ReturnsNull()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            var tileset = CreateTestTilesetConfiguration("TestTileset");
            _settings.SetPrivateField("_tilesets", new List<TilesetConfiguration> { tileset });
            
            // Act
            var result = _settings.GetTileset("NonExistingTileset");
            
            // Assert
            Assert.IsNull(result, "Should return null for non-existing tileset ID");
        }
        
        [Test]
        public void IsValidForProfile_WithMatchingProfile_ReturnsTrue()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_profile", GenerationProfile.Development);
            
            // Act
            var result = _settings.IsValidForProfile(GenerationProfile.Development);
            
            // Assert
            Assert.IsTrue(result, "Should be valid for matching profile");
        }
        
        [Test]
        public void IsValidForProfile_WithAnyProfile_ReturnsTrue()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_profile", GenerationProfile.Any);
            
            // Act
            var result = _settings.IsValidForProfile(GenerationProfile.Production);
            
            // Assert
            Assert.IsTrue(result, "Should be valid for any profile when set to Any");
        }
        
        [Test]
        public void IsValidForProfile_WithNonMatchingProfile_ReturnsFalse()
        {
            // Arrange
            SetupValidMapGenerationSettings();
            _settings.SetPrivateField("_profile", GenerationProfile.Development);
            
            // Act
            var result = _settings.IsValidForProfile(GenerationProfile.Production);
            
            // Assert
            Assert.IsFalse(result, "Should not be valid for non-matching profile");
        }
        
        [Test]
        public void MapConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            var config = new MapConfiguration();
            config.SetPrivateField("_mapSizeRange", new Vector2Int(50, 100));
            config.SetPrivateField("_roomSizeRange", new Vector2Int(5, 15));
            config.SetPrivateField("_minRooms", 5);
            config.SetPrivateField("_maxRooms", 20);
            config.SetPrivateField("_roomDensity", 0.6f);
            config.SetPrivateField("_useRandomSeed", true);
            config.SetPrivateField("_saveSeed", true);
            
            // Act
            var result = config.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid map configuration should pass validation");
        }
        
        [Test]
        public void MapConfiguration_WithInvalidSizeRange_FailsValidation()
        {
            // Arrange
            var config = new MapConfiguration();
            config.SetPrivateField("_mapSizeRange", new Vector2Int(100, 50)); // Min > Max
            
            // Act
            var result = config.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Map configuration with invalid size range should fail validation");
        }
        
        [Test]
        public void MapConfiguration_GetMapSize_WithFixedSize_ReturnsFixedSize()
        {
            // Arrange
            var config = new MapConfiguration();
            config.SetPrivateField("_useFixedSize", true);
            config.SetPrivateField("_fixedSize", new Vector2Int(75, 75));
            
            // Act
            var result = config.GetMapSize();
            
            // Assert
            Assert.AreEqual(new Vector2Int(75, 75), result, "Should return fixed size when use fixed size is enabled");
        }
        
        [Test]
        public void MapConfiguration_GetMapSize_WithVariableSize_ReturnsRandomSize()
        {
            // Arrange
            var config = new MapConfiguration();
            config.SetPrivateField("_useFixedSize", false);
            config.SetPrivateField("_mapSizeRange", new Vector2Int(50, 100));
            
            // Act
            var result = config.GetMapSize(new System.Random(1));
            
            // Assert
            Assert.IsTrue(result.x >= 50 && result.x <= 100, "X should be within range");
            Assert.IsTrue(result.y >= 50 && result.y <= 100, "Y should be within range");
        }
        
        [Test]
        public void BSPConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            var config = new BSPConfiguration();
            config.SetPrivateField("_minPartitionSize", 10);
            config.SetPrivateField("_splitPositionVariation", 0.3f);
            config.SetPrivateField("_allowHorizontalSplits", true);
            config.SetPrivateField("_allowVerticalSplits", true);
            config.SetPrivateField("_maxDepth", 10);
            config.SetPrivateField("_stopSplittingChance", 0.1f);
            config.SetPrivateField("_balanceTree", true);
            config.SetPrivateField("_roomSizeRatio", 0.8f);
            config.SetPrivateField("_roomPositionVariation", 0.1f);
            config.SetPrivateField("_centerRooms", false);
            
            // Act
            var result = config.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid BSP configuration should pass validation");
        }
        
        [Test]
        public void CorridorConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            var config = new CorridorConfiguration();
            config.SetPrivateField("_corridorType", CorridorType.LShaped);
            config.SetPrivateField("_minWidth", 1);
            config.SetPrivateField("_maxWidth", 3);
            config.SetPrivateField("_ensureDirectPath", true);
            config.SetPrivateField("_avoidRooms", true);
            config.SetPrivateField("_pathSmoothing", 0.3f);
            config.SetPrivateField("_addDecorations", false);
            config.SetPrivateField("_decorationDensity", 0.1f);
            config.SetPrivateField("_useCurvedCorridors", false);
            
            // Act
            var result = config.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid corridor configuration should pass validation");
        }
        
        [Test]
        public void GenerationRules_WithValidData_PassesValidation()
        {
            // Arrange
            var rules = new GenerationRules();
            rules.SetPrivateField("_ensureAllRoomsReachable", true);
            rules.SetPrivateField("_createLoops", false);
            rules.SetPrivateField("_loopChance", 0.2f);
            rules.SetPrivateField("_secretRoomChance", 0.1f);
            rules.SetPrivateField("_treasureRoomChance", 0.05f);
            rules.SetPrivateField("_bossRoomChance", 0.1f);
            rules.SetPrivateField("_balanceRoomDistribution", true);
            rules.SetPrivateField("_clusterSimilarRooms", false);
            rules.SetPrivateField("_randomnessFactor", 0.5f);
            
            // Act
            var result = rules.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid generation rules should pass validation");
        }
        
        [Test]
        public void ValidationRules_WithValidData_PassesValidation()
        {
            // Arrange
            var rules = new ValidationRules();
            rules.SetPrivateField("_validateConnectivity", true);
            rules.SetPrivateField("_validateRoomSizes", true);
            rules.SetPrivateField("_validateCorridorWidths", true);
            rules.SetPrivateField("_rejectInvalidMaps", true);
            rules.SetPrivateField("_maxRetryAttempts", 3);
            rules.SetPrivateField("_logValidationDetails", true);
            rules.SetPrivateField("_minWalkableRatio", 0.3f);
            rules.SetPrivateField("_minRoomCount", 3);
            rules.SetPrivateField("_maxDeadEndRatio", 0.5f);
            
            // Act
            var result = rules.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid validation rules should pass validation");
        }
        
        [Test]
        public void PerformanceSettings_WithValidData_PassesValidation()
        {
            // Arrange
            var settings = new PerformanceSettings();
            settings.SetPrivateField("_enableMultithreading", false);
            settings.SetPrivateField("_generationTimeoutMs", 10000);
            settings.SetPrivateField("_useIncrementalGeneration", false);
            settings.SetPrivateField("_poolObjects", true);
            settings.SetPrivateField("_reuseTilemaps", false);
            settings.SetPrivateField("_maxPoolSize", 100);
            settings.SetPrivateField("_enableLOD", false);
            settings.SetPrivateField("_lodLevels", 3);
            settings.SetPrivateField("_lodDistance", 50f);
            
            // Act
            var result = settings.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid performance settings should pass validation");
        }
        
        [Test]
        public void RuntimeConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            var config = new RuntimeConfiguration();
            config.SetPrivateField("_generateOnStart", false);
            config.SetPrivateField("_generateAsync", true);
            config.SetPrivateField("_generationDelay", 0f);
            config.SetPrivateField("_autoSaveMaps", false);
            config.SetPrivateField("_allowMapSaving", true);
            config.SetPrivateField("_allowMapLoading", true);
            config.SetPrivateField("_allowRuntimeRegeneration", false);
            config.SetPrivateField("_allowParameterModification", false);
            config.SetPrivateField("_requireAdminPrivileges", true);
            
            // Act
            var result = config.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid runtime configuration should pass validation");
        }
        
        [Test]
        public void DebugSettings_WithValidData_PassesValidation()
        {
            // Arrange
            var settings = new DebugSettings();
            settings.SetPrivateField("_showGizmos", true);
            settings.SetPrivateField("_showRoomLabels", false);
            settings.SetPrivateField("_showConnectivity", false);
            settings.SetPrivateField("_colorizeRooms", false);
            settings.SetPrivateField("_enableLogging", true);
            settings.SetPrivateField("_logGenerationSteps", false);
            settings.SetPrivateField("_logPerformanceMetrics", false);
            settings.SetPrivateField("_logValidationResults", true);
            settings.SetPrivateField("_enableTestMode", false);
            settings.SetPrivateField("_testSeed", 12345);
            settings.SetPrivateField("_runValidationTests", true);
            
            // Act
            var result = settings.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid debug settings should pass validation");
        }
        
        [Test]
        public void QualitySettings_WithValidData_PassesValidation()
        {
            // Arrange
            var settings = new QualitySettings();
            settings.SetPrivateField("_quality", GenerationQuality.Medium);
            settings.SetPrivateField("_adaptiveQuality", false);
            settings.SetPrivateField("_qualityThreshold", 60f);
            settings.SetPrivateField("_decorationQuality", 0.5f);
            settings.SetPrivateField("_lightingQuality", 0.7f);
            settings.SetPrivateField("_effectsQuality", 0.3f);
            settings.SetPrivateField("_enableOcclusionCulling", true);
            settings.SetPrivateField("_enableFrustumCulling", true);
            settings.SetPrivateField("_batchTileOperations", true);
            
            // Act
            var result = settings.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid quality settings should pass validation");
        }
        
        private void SetupValidMapGenerationSettings()
        {
            _settings.SetPrivateField("_settingsID", "test_settings");
            _settings.SetPrivateField("_settingsName", "Test Settings");
            _settings.SetPrivateField("_description", "A test map generation settings");
            _settings.SetPrivateField("_profile", GenerationProfile.Any);
            _settings.SetPrivateField("_mapConfig", new MapConfiguration());
            _settings.SetPrivateField("_bspConfig", new BSPConfiguration());
            _settings.SetPrivateField("_corridorConfig", new CorridorConfiguration());
            _settings.SetPrivateField("_roomTemplates", new List<RoomTemplate> { CreateTestRoomTemplate("TestTemplate") });
            _settings.SetPrivateField("_biomeConfigurations", new List<BiomeConfiguration>());
            _settings.SetPrivateField("_spawnTables", new List<SpawnTableConfiguration>());
            _settings.SetPrivateField("_tilesets", new List<TilesetConfiguration> { CreateTestTilesetConfiguration("TestTileset") });
            _settings.SetPrivateField("_generationRules", new GenerationRules());
            _settings.SetPrivateField("_validationRules", new ValidationRules());
            _settings.SetPrivateField("_performanceSettings", new PerformanceSettings());
            _settings.SetPrivateField("_runtimeConfig", new RuntimeConfiguration());
            _settings.SetPrivateField("_debugSettings", new DebugSettings());
            _settings.SetPrivateField("_qualitySettings", new QualitySettings());
            _settings.SetPrivateField("_allowRuntimeModification", false);
        }
        
        private RoomTemplate CreateTestRoomTemplate(string name, RoomClassification classification = RoomClassification.Unassigned)
        {
            var gameObject = new GameObject(name);
            var template = gameObject.AddComponent<RoomTemplate>();
            template.SetPrivateField("_templateID", name.ToLower());
            template.SetPrivateField("_templateName", name);
            template.SetPrivateField("_requiredClassification", classification);
            return template;
        }
        
        private BiomeConfiguration CreateTestBiomeConfiguration(string name)
        {
            var gameObject = new GameObject(name);
            var biome = gameObject.AddComponent<BiomeConfiguration>();
            biome.SetPrivateField("_biomeID", name.ToLower());
            biome.SetPrivateField("_biomeName", name);
            return biome;
        }
        
        private SpawnTableConfiguration CreateTestSpawnTableConfiguration(string name)
        {
            var gameObject = new GameObject(name);
            var spawnTable = gameObject.AddComponent<SpawnTableConfiguration>();
            spawnTable.SetPrivateField("_spawnTableID", name.ToLower());
            spawnTable.SetPrivateField("_tableName", name);
            return spawnTable;
        }
        
        private TilesetConfiguration CreateTestTilesetConfiguration(string name)
        {
            var gameObject = new GameObject(name);
            var tileset = gameObject.AddComponent<TilesetConfiguration>();
            tileset.SetPrivateField("_tilesetID", name.ToLower());
            tileset.SetPrivateField("_tilesetName", name);
            return tileset;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class MapGenerationSettingsTestExtensions
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