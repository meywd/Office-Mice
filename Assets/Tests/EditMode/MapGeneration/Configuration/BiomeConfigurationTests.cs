using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class BiomeConfigurationTests
    {
        private BiomeConfiguration _biomeConfig;
        private GameObject _testGameObject;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestBiomeConfiguration");
            _biomeConfig = _testGameObject.AddComponent<BiomeConfiguration>();
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
        public void BiomeConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            
            // Act
            var result = _biomeConfig.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid biome configuration should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid biome configuration should have no errors");
        }
        
        [Test]
        public void BiomeConfiguration_WithMissingID_FailsValidation()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            _biomeConfig.SetPrivateField("_biomeID", "");
            
            // Act
            var result = _biomeConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Biome configuration with missing ID should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Biome ID is required")), 
                "Should have error about missing biome ID");
        }
        
        [Test]
        public void BiomeConfiguration_WithMissingPrimaryTileset_FailsValidation()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            _biomeConfig.SetPrivateField("_primaryTileset", null);
            
            // Act
            var result = _biomeConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Biome configuration with missing primary tileset should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Primary tileset is required")), 
                "Should have error about missing primary tileset");
        }
        
        [Test]
        public void BiomeConfiguration_WithInvalidSecondaryTilesetChance_FailsValidation()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            _biomeConfig.SetPrivateField("_secondaryTilesetChance", 1.5f); // Invalid > 1.0
            
            // Act
            var result = _biomeConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Biome configuration with invalid secondary tileset chance should fail validation");
        }
        
        [Test]
        public void GetRandomTileset_WithValidChance_ReturnsSecondaryTileset()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            var primaryTileset = CreateTestTileset("Primary");
            var secondaryTileset = CreateTestTileset("Secondary");
            _biomeConfig.SetPrivateField("_primaryTileset", primaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTileset", secondaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTilesetChance", 1.0f); // Always secondary
            
            // Act
            var result = _biomeConfig.GetRandomTileset(new System.Random(1)); // Fixed seed for predictable result
            
            // Assert
            Assert.AreEqual(secondaryTileset, result, "Should return secondary tileset when chance is 100%");
        }
        
        [Test]
        public void GetRandomTileset_WithZeroChance_ReturnsPrimaryTileset()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            var primaryTileset = CreateTestTileset("Primary");
            var secondaryTileset = CreateTestTileset("Secondary");
            _biomeConfig.SetPrivateField("_primaryTileset", primaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTileset", secondaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTilesetChance", 0.0f); // Never secondary
            
            // Act
            var result = _biomeConfig.GetRandomTileset();
            
            // Assert
            Assert.AreEqual(primaryTileset, result, "Should return primary tileset when chance is 0%");
        }
        
        [Test]
        public void GetRandomTileset_WithNoSecondaryTileset_ReturnsPrimaryTileset()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            var primaryTileset = CreateTestTileset("Primary");
            _biomeConfig.SetPrivateField("_primaryTileset", primaryTileset);
            _biomeConfig.SetPrivateField("_secondaryTileset", null);
            
            // Act
            var result = _biomeConfig.GetRandomTileset();
            
            // Assert
            Assert.AreEqual(primaryTileset, result, "Should return primary tileset when no secondary tileset exists");
        }
        
        [Test]
        public void GetAvailableResources_WithRareResourceChance_ReturnsCommonAndRareResources()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            var commonResources = new List<BiomeResource>
            {
                CreateTestBiomeResource("CommonResource1"),
                CreateTestBiomeResource("CommonResource2")
            };
            var rareResources = new List<BiomeResource>
            {
                CreateTestBiomeResource("RareResource1")
            };
            _biomeConfig.SetPrivateField("_commonResources", commonResources);
            _biomeConfig.SetPrivateField("_rareResources", rareResources);
            _biomeConfig.SetPrivateField("_rareResourceChance", 1.0f); // Always include rare
            
            // Act
            var result = _biomeConfig.GetAvailableResources(new System.Random(1));
            
            // Assert
            Assert.AreEqual(3, result.Count, "Should return all resources when rare chance is 100%");
        }
        
        [Test]
        public void GetAvailableResources_WithZeroRareResourceChance_ReturnsOnlyCommonResources()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            var commonResources = new List<BiomeResource>
            {
                CreateTestBiomeResource("CommonResource1"),
                CreateTestBiomeResource("CommonResource2")
            };
            var rareResources = new List<BiomeResource>
            {
                CreateTestBiomeResource("RareResource1")
            };
            _biomeConfig.SetPrivateField("_commonResources", commonResources);
            _biomeConfig.SetPrivateField("_rareResources", rareResources);
            _biomeConfig.SetPrivateField("_rareResourceChance", 0.0f); // Never include rare
            
            // Act
            var result = _biomeConfig.GetAvailableResources();
            
            // Assert
            Assert.AreEqual(2, result.Count, "Should return only common resources when rare chance is 0%");
        }
        
        [Test]
        public void GetRandomColorVariation_WithTintingDisabled_ReturnsOriginalColor()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            _biomeConfig.SetPrivateField("_applyColorTinting", false);
            var originalColor = Color.red;
            
            // Act
            var result = _biomeConfig.GetRandomColorVariation(originalColor);
            
            // Assert
            Assert.AreEqual(originalColor, result, "Should return original color when tinting is disabled");
        }
        
        [Test]
        public void GetRandomColorVariation_WithTintingEnabled_ReturnsModifiedColor()
        {
            // Arrange
            SetupValidBiomeConfiguration();
            _biomeConfig.SetPrivateField("_applyColorTinting", true);
            _biomeConfig.SetPrivateField("_colorVariation", 0.5f);
            var originalColor = Color.red;
            
            // Act
            var result = _biomeConfig.GetRandomColorVariation(originalColor, new System.Random(1));
            
            // Assert
            Assert.AreNotEqual(originalColor, result, "Should return modified color when tinting is enabled");
        }
        
        [Test]
        public void BiomeColorPalette_WithValidData_PassesValidation()
        {
            // Arrange
            var palette = new BiomeColorPalette();
            palette.SetPrivateField("_paletteName", "TestPalette");
            palette.SetPrivateField("_primaryColor", Color.red);
            palette.SetPrivateField("_secondaryColor", Color.blue);
            
            // Act
            var result = palette.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid color palette should pass validation");
        }
        
        [Test]
        public void BiomeColorPalette_WithMissingName_FailsValidation()
        {
            // Arrange
            var palette = new BiomeColorPalette();
            palette.SetPrivateField("_paletteName", "");
            
            // Act
            var result = palette.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Color palette with missing name should fail validation");
        }
        
        [Test]
        public void BiomeColorPalette_GetRandomColor_ReturnsValidColor()
        {
            // Arrange
            var palette = new BiomeColorPalette();
            palette.SetPrivateField("_paletteName", "TestPalette");
            palette.SetPrivateField("_primaryColor", Color.red);
            palette.SetPrivateField("_secondaryColor", Color.blue);
            palette.SetPrivateField("_accentColor", Color.green);
            
            // Act
            var result = palette.GetRandomColor(new System.Random(1));
            
            // Assert
            Assert.IsTrue(result == Color.red || result == Color.blue || result == Color.green, 
                "Should return one of the defined colors");
        }
        
        [Test]
        public void EnvironmentalEffect_WithValidData_PassesValidation()
        {
            // Arrange
            var effect = new EnvironmentalEffect();
            effect.SetPrivateField("_effectName", "Rain");
            effect.SetPrivateField("_effectType", EffectType.Rain);
            effect.SetPrivateField("_intensity", 0.7f);
            effect.SetPrivateField("_frequencyRange", new Vector2(1f, 5f));
            
            // Act
            var result = effect.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid environmental effect should pass validation");
        }
        
        [Test]
        public void EnvironmentalEffect_WithInvalidIntensity_FailsValidation()
        {
            // Arrange
            var effect = new EnvironmentalEffect();
            effect.SetPrivateField("_effectName", "Rain");
            effect.SetPrivateField("_intensity", -1.0f); // Negative intensity
            
            // Act
            var result = effect.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Environmental effect with negative intensity should fail validation");
        }
        
        [Test]
        public void BiomeGenerationRules_WithValidData_PassesValidation()
        {
            // Arrange
            var rules = new BiomeGenerationRules();
            rules.SetPrivateField("_roomSizeRange", new Vector2Int(5, 15));
            rules.SetPrivateField("_maxRooms", 10);
            rules.SetPrivateField("_roomDensity", 0.6f);
            rules.SetPrivateField("_minCorridorWidth", 1);
            rules.SetPrivateField("_maxCorridorWidth", 3);
            
            // Act
            var result = rules.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid biome generation rules should pass validation");
        }
        
        [Test]
        public void BiomeGenerationRules_WithInvalidRoomSizeRange_FailsValidation()
        {
            // Arrange
            var rules = new BiomeGenerationRules();
            rules.SetPrivateField("_roomSizeRange", new Vector2Int(10, 5)); // Min > Max
            
            // Act
            var result = rules.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Biome generation rules with invalid room size range should fail validation");
        }
        
        [Test]
        public void BiomeModifier_WithValidData_PassesValidation()
        {
            // Arrange
            var modifier = new BiomeModifier();
            modifier.SetPrivateField("_modifierName", "SpeedBoost");
            modifier.SetPrivateField("_modifierType", ModifierType.MovementSpeed);
            modifier.SetPrivateField("_value", 1.5f);
            modifier.SetPrivateField("_chance", 0.3f);
            
            // Act
            var result = modifier.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid biome modifier should pass validation");
        }
        
        [Test]
        public void BiomeResource_WithValidData_PassesValidation()
        {
            // Arrange
            var resource = new BiomeResource();
            resource.SetPrivateField("_resourceType", "HealthPack");
            resource.SetPrivateField("_resourcePrefab", new GameObject("HealthPack"));
            resource.SetPrivateField("_spawnChance", 0.5f);
            resource.SetPrivateField("_quantityRange", new Vector2Int(1, 3));
            
            // Act
            var result = resource.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid biome resource should pass validation");
        }
        
        [Test]
        public void BiomeResource_WithMissingPrefab_FailsValidation()
        {
            // Arrange
            var resource = new BiomeResource();
            resource.SetPrivateField("_resourceType", "HealthPack");
            resource.SetPrivateField("_resourcePrefab", null); // Missing prefab
            
            // Act
            var result = resource.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Biome resource with missing prefab should fail validation");
        }
        
        private void SetupValidBiomeConfiguration()
        {
            _biomeConfig.SetPrivateField("_biomeID", "test_biome");
            _biomeConfig.SetPrivateField("_biomeName", "Test Biome");
            _biomeConfig.SetPrivateField("_description", "A test biome configuration");
            _biomeConfig.SetPrivateField("_biomeType", BiomeType.Office);
            _biomeConfig.SetPrivateField("_primaryTileset", CreateTestTileset("Primary"));
            _biomeConfig.SetPrivateField("_secondaryTilesetChance", 0.3f);
            _biomeConfig.SetPrivateField("_applyColorTinting", true);
            _biomeConfig.SetPrivateField("_colorVariation", 0.2f);
            _biomeConfig.SetPrivateField("_ambientLightIntensity", 1.0f);
            _biomeConfig.SetPrivateField("_ambientLightColor", Color.white);
            _biomeConfig.SetPrivateField("_generationRules", new BiomeGenerationRules());
            _biomeConfig.SetPrivateField("_musicVolume", 0.5f);
            _biomeConfig.SetPrivateField("_soundVolume", 0.3f);
            _biomeConfig.SetPrivateField("_rareResourceChance", 0.1f);
        }
        
        private TilesetConfiguration CreateTestTileset(string name)
        {
            var gameObject = new GameObject(name);
            var tileset = gameObject.AddComponent<TilesetConfiguration>();
            tileset.SetPrivateField("_tilesetID", name.ToLower());
            tileset.SetPrivateField("_tilesetName", name);
            return tileset;
        }
        
        private BiomeResource CreateTestBiomeResource(string name)
        {
            var resource = new BiomeResource();
            resource.SetPrivateField("_resourceType", name);
            resource.SetPrivateField("_resourcePrefab", new GameObject(name));
            resource.SetPrivateField("_spawnChance", 0.5f);
            resource.SetPrivateField("_quantityRange", new Vector2Int(1, 1));
            return resource;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class BiomeTestExtensions
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