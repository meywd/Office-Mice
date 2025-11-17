using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class TilesetConfigurationTests
    {
        private TilesetConfiguration _tilesetConfig;
        private GameObject _testGameObject;
        private Tilemap _testTilemap;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestTilesetConfiguration");
            _tilesetConfig = _testGameObject.AddComponent<TilesetConfiguration>();
            
            // Create test tilemap
            var tilemapGO = new GameObject("TestTilemap");
            _testTilemap = tilemapGO.AddComponent<Tilemap>();
            var grid = tilemapGO.AddComponent<Grid>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            if (_testTilemap != null && _testTilemap.gameObject != null)
            {
                Object.DestroyImmediate(_testTilemap.gameObject);
            }
        }
        
        [Test]
        public void TilesetConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            
            // Act
            var result = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tileset configuration should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid tileset configuration should have no errors");
        }
        
        [Test]
        public void TilesetConfiguration_WithMissingID_FailsValidation()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_tilesetID", "");
            
            // Act
            var result = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tileset configuration with missing ID should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Tileset ID is required")), 
                "Should have error about missing tileset ID");
        }
        
        [Test]
        public void TilesetConfiguration_WithMissingFloorTiles_FailsValidation()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_floorTiles", null);
            
            // Act
            var result = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tileset configuration with missing floor tiles should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Floor tiles mapping is required")), 
                "Should have error about missing floor tiles");
        }
        
        [Test]
        public void TilesetConfiguration_WithMissingWallTiles_FailsValidation()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_wallTiles", null);
            
            // Act
            var result = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tileset configuration with missing wall tiles should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Wall tiles mapping is required")), 
                "Should have error about missing wall tiles");
        }
        
        [Test]
        public void TilesetConfiguration_WithInvalidDecorationDensity_FailsValidation()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_decorationDensity", 1.5f); // Invalid > 1.0
            
            // Act
            var result = _tilesetConfig.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tileset configuration with invalid decoration density should fail validation");
        }
        
        [Test]
        public void GetTileForType_WithFloorType_ReturnsFloorTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            var floorMapping = CreateTestTileMapping("Floor", CreateTestTile("FloorTile"));
            _tilesetConfig.SetPrivateField("_floorTiles", floorMapping);
            
            // Act
            var result = _tilesetConfig.GetTileForType(TileType.Floor);
            
            // Assert
            Assert.IsNotNull(result, "Should return a floor tile");
        }
        
        [Test]
        public void GetTileForType_WithWallType_ReturnsWallTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            var wallMapping = CreateTestTileMapping("Wall", CreateTestTile("WallTile"));
            _tilesetConfig.SetPrivateField("_wallTiles", wallMapping);
            
            // Act
            var result = _tilesetConfig.GetTileForType(TileType.Wall);
            
            // Assert
            Assert.IsNotNull(result, "Should return a wall tile");
        }
        
        [Test]
        public void GetTileForType_WithUnknownType_ReturnsFallbackTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            var fallbackTile = CreateTestTile("FallbackTile");
            _tilesetConfig.SetPrivateField("_fallbackTile", fallbackTile);
            
            // Act
            var result = _tilesetConfig.GetTileForType(TileType.Objective); // Not configured
            
            // Assert
            Assert.AreEqual(fallbackTile, result, "Should return fallback tile for unknown type");
        }
        
        [Test]
        public void GetTileForType_WithVariationsEnabled_ReturnsVariantTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            var floorMapping = CreateTestTileMapping("Floor", CreateTestTile("FloorTile"));
            _tilesetConfig.SetPrivateField("_floorTiles", floorMapping);
            _tilesetConfig.SetPrivateField("_useVariationsForFloors", true);
            _tilesetConfig.SetPrivateField("_variationChance", 1.0f); // Always apply variations
            
            var variation = new TileVariation();
            variation.SetPrivateField("_variationName", "TestVariation");
            variation.SetPrivateField("_applicableTypes", new List<TileType> { TileType.Floor });
            variation.SetPrivateField("_variationType", VariationType.Replacement);
            variation.SetPrivateField("_replacementTile", CreateTestTile("VariantTile"));
            _tilesetConfig.SetPrivateField("_tileVariations", new List<TileVariation> { variation });
            
            // Act
            var result = _tilesetConfig.GetTileForType(TileType.Floor, new System.Random(1));
            
            // Assert
            Assert.IsNotNull(result, "Should return a variant tile");
        }
        
        [Test]
        public void GetDecorativeTile_WithDecorativeTiles_ReturnsRandomTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            var decorativeMapping = CreateTestTileMapping("Decorative", CreateTestTile("DecorativeTile"));
            _tilesetConfig.SetPrivateField("_decorativeTiles", new List<TileMapping> { decorativeMapping });
            
            // Act
            var result = _tilesetConfig.GetDecorativeTile(new System.Random(1));
            
            // Assert
            Assert.IsNotNull(result, "Should return a decorative tile");
        }
        
        [Test]
        public void GetDecorativeTile_WithNoDecorativeTiles_ReturnsNull()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_decorativeTiles", new List<TileMapping>());
            
            // Act
            var result = _tilesetConfig.GetDecorativeTile();
            
            // Assert
            Assert.IsNull(result, "Should return null when no decorative tiles exist");
        }
        
        [Test]
        public void ApplyTileRules_WithDisabledRules_DoesNothing()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_applyRulesAutomatically", false);
            var position = new Vector3Int(0, 0, 0);
            var originalTile = CreateTestTile("OriginalTile");
            _testTilemap.SetTile(position, originalTile);
            
            // Act
            _tilesetConfig.ApplyTileRules(_testTilemap, position, originalTile);
            
            // Assert
            Assert.AreEqual(originalTile, _testTilemap.GetTile(position), "Should not modify tile when rules are disabled");
        }
        
        [Test]
        public void ApplyTileRules_WithEnabledRules_ModifiesTile()
        {
            // Arrange
            SetupValidTilesetConfiguration();
            _tilesetConfig.SetPrivateField("_applyRulesAutomatically", true);
            
            var rule = new TileRule();
            rule.SetPrivateField("_ruleName", "TestRule");
            rule.SetPrivateField("_condition", TileRuleCondition.Always);
            rule.SetPrivateField("_action", TileRuleAction.Replace);
            rule.SetPrivateField("_replacementTile", CreateTestTile("ReplacementTile"));
            rule.SetPrivateField("_chance", 1.0f);
            _tilesetConfig.SetPrivateField("_tileRules", new List<TileRule> { rule });
            
            var position = new Vector3Int(0, 0, 0);
            var originalTile = CreateTestTile("OriginalTile");
            _testTilemap.SetTile(position, originalTile);
            
            // Act
            _tilesetConfig.ApplyTileRules(_testTilemap, position, originalTile);
            
            // Assert
            var newTile = _testTilemap.GetTile(position);
            Assert.AreNotEqual(originalTile, newTile, "Should modify tile when rules are enabled");
        }
        
        [Test]
        public void TileMapping_WithValidData_PassesValidation()
        {
            // Arrange
            var mapping = new TileMapping();
            mapping.SetPrivateField("_mappingName", "TestMapping");
            mapping.SetPrivateField("_tiles", new List<TileEntry> { CreateTestTileEntry("TestTile", 1.0f) });
            mapping.SetPrivateField("_useRandomSelection", true);
            mapping.SetPrivateField("_defaultIndex", 0);
            
            // Act
            var result = mapping.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tile mapping should pass validation");
        }
        
        [Test]
        public void TileMapping_WithNoTiles_FailsValidation()
        {
            // Arrange
            var mapping = new TileMapping();
            mapping.SetPrivateField("_mappingName", "TestMapping");
            mapping.SetPrivateField("_tiles", new List<TileEntry>());
            
            // Act
            var result = mapping.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile mapping with no tiles should fail validation");
        }
        
        [Test]
        public void TileMapping_GetRandomTile_WithRandomSelection_ReturnsRandomTile()
        {
            // Arrange
            var mapping = new TileMapping();
            var tile1 = CreateTestTile("Tile1");
            var tile2 = CreateTestTile("Tile2");
            mapping.SetPrivateField("_tiles", new List<TileEntry> 
            { 
                CreateTestTileEntry("Tile1", 1.0f, tile1),
                CreateTestTileEntry("Tile2", 1.0f, tile2)
            });
            mapping.SetPrivateField("_useRandomSelection", true);
            
            // Act
            var result = mapping.GetRandomTile(new System.Random(1));
            
            // Assert
            Assert.IsTrue(result == tile1 || result == tile2, "Should return one of the tiles");
        }
        
        [Test]
        public void TileMapping_GetRandomTile_WithoutRandomSelection_ReturnsDefaultTile()
        {
            // Arrange
            var mapping = new TileMapping();
            var tile1 = CreateTestTile("Tile1");
            var tile2 = CreateTestTile("Tile2");
            mapping.SetPrivateField("_tiles", new List<TileEntry> 
            { 
                CreateTestTileEntry("Tile1", 1.0f, tile1),
                CreateTestTileEntry("Tile2", 1.0f, tile2)
            });
            mapping.SetPrivateField("_useRandomSelection", false);
            mapping.SetPrivateField("_defaultIndex", 1);
            
            // Act
            var result = mapping.GetRandomTile();
            
            // Assert
            Assert.AreEqual(tile2, result, "Should return default tile when random selection is disabled");
        }
        
        [Test]
        public void TileEntry_WithValidData_PassesValidation()
        {
            // Arrange
            var entry = CreateTestTileEntry("TestTile", 1.0f);
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tile entry should pass validation");
        }
        
        [Test]
        public void TileEntry_WithMissingTile_FailsValidation()
        {
            // Arrange
            var entry = new TileEntry();
            entry.SetPrivateField("_tile", null);
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile entry with missing tile should fail validation");
        }
        
        [Test]
        public void TileEntry_WithNegativeWeight_FailsValidation()
        {
            // Arrange
            var entry = CreateTestTileEntry("TestTile", -1.0f); // Negative weight
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile entry with negative weight should fail validation");
        }
        
        [Test]
        public void TileVariation_WithValidData_PassesValidation()
        {
            // Arrange
            var variation = new TileVariation();
            variation.SetPrivateField("_variationName", "TestVariation");
            variation.SetPrivateField("_applicableTypes", new List<TileType> { TileType.Floor });
            variation.SetPrivateField("_variationType", VariationType.Tint);
            variation.SetPrivateField("_intensity", 0.5f);
            
            // Act
            var result = variation.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tile variation should pass validation");
        }
        
        [Test]
        public void TileVariation_WithNoApplicableTypes_FailsValidation()
        {
            // Arrange
            var variation = new TileVariation();
            variation.SetPrivateField("_variationName", "TestVariation");
            variation.SetPrivateField("_applicableTypes", new List<TileType>());
            
            // Act
            var result = variation.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile variation with no applicable types should fail validation");
        }
        
        [Test]
        public void TileVariation_ApplicableToType_ReturnsCorrectResult()
        {
            // Arrange
            var variation = new TileVariation();
            variation.SetPrivateField("_applicableTypes", new List<TileType> { TileType.Floor, TileType.Wall });
            
            // Act & Assert
            Assert.IsTrue(variation.ApplicableToType(TileType.Floor), "Should be applicable to floor type");
            Assert.IsTrue(variation.ApplicableToType(TileType.Wall), "Should be applicable to wall type");
            Assert.IsFalse(variation.ApplicableToType(TileType.Door), "Should not be applicable to door type");
        }
        
        [Test]
        public void TileRule_WithValidData_PassesValidation()
        {
            // Arrange
            var rule = new TileRule();
            rule.SetPrivateField("_ruleName", "TestRule");
            rule.SetPrivateField("_condition", TileRuleCondition.Always);
            rule.SetPrivateField("_action", TileRuleAction.Keep);
            rule.SetPrivateField("_chance", 0.5f);
            
            // Act
            var result = rule.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tile rule should pass validation");
        }
        
        [Test]
        public void TileRule_WithInvalidChance_FailsValidation()
        {
            // Arrange
            var rule = new TileRule();
            rule.SetPrivateField("_ruleName", "TestRule");
            rule.SetPrivateField("_chance", 1.5f); // Invalid > 1.0
            
            // Act
            var result = rule.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile rule with invalid chance should fail validation");
        }
        
        [Test]
        public void TileRule_ShouldApply_WithAlwaysCondition_ReturnsTrue()
        {
            // Arrange
            var rule = new TileRule();
            rule.SetPrivateField("_condition", TileRuleCondition.Always);
            rule.SetPrivateField("_chance", 1.0f);
            
            // Act
            var result = rule.ShouldApply(_testTilemap, Vector3Int.zero, null);
            
            // Assert
            Assert.IsTrue(result, "Always condition should always return true");
        }
        
        [Test]
        public void TileRule_ShouldApply_WithLowChance_ReturnsFalse()
        {
            // Arrange
            var rule = new TileRule();
            rule.SetPrivateField("_condition", TileRuleCondition.Always);
            rule.SetPrivateField("_chance", 0.0f); // Never apply
            
            // Act
            var result = rule.ShouldApply(_testTilemap, Vector3Int.zero, null);
            
            // Assert
            Assert.IsFalse(result, "Rule with 0% chance should never apply");
        }
        
        private void SetupValidTilesetConfiguration()
        {
            _tilesetConfig.SetPrivateField("_tilesetID", "test_tileset");
            _tilesetConfig.SetPrivateField("_tilesetName", "Test Tileset");
            _tilesetConfig.SetPrivateField("_description", "A test tileset configuration");
            _tilesetConfig.SetPrivateField("_theme", TilesetTheme.Office);
            _tilesetConfig.SetPrivateField("_floorTiles", CreateTestTileMapping("Floor", CreateTestTile("FloorTile")));
            _tilesetConfig.SetPrivateField("_wallTiles", CreateTestTileMapping("Wall", CreateTestTile("WallTile")));
            _tilesetConfig.SetPrivateField("_decorationDensity", 0.2f);
            _tilesetConfig.SetPrivateField("_randomizeDecorations", true);
            _tilesetConfig.SetPrivateField("_variationChance", 0.3f);
            _tilesetConfig.SetPrivateField("_useVariationsForFloors", true);
            _tilesetConfig.SetPrivateField("_useVariationsForWalls", true);
            _tilesetConfig.SetPrivateField("_applyRulesAutomatically", true);
            _tilesetConfig.SetPrivateField("_fallbackTile", CreateTestTile("FallbackTile"));
            _tilesetConfig.SetPrivateField("_tileSize", new Vector2Int(16, 16));
            _tilesetConfig.SetPrivateField("_tilesPerRow", 16);
            _tilesetConfig.SetPrivateField("_enableTileColliders", true);
            _tilesetConfig.SetPrivateField("_useTilemapCollider2D", true);
        }
        
        private TileMapping CreateTestTileMapping(string name, TileBase tile)
        {
            var mapping = new TileMapping();
            mapping.SetPrivateField("_mappingName", name);
            mapping.SetPrivateField("_tiles", new List<TileEntry> { CreateTestTileEntry(name, 1.0f, tile) });
            mapping.SetPrivateField("_useRandomSelection", true);
            mapping.SetPrivateField("_defaultIndex", 0);
            return mapping;
        }
        
        private TileEntry CreateTestTileEntry(string name, float weight, TileBase tile = null)
        {
            var entry = new TileEntry();
            entry.SetPrivateField("_tile", tile ?? CreateTestTile(name));
            entry.SetPrivateField("_weight", weight);
            entry.SetPrivateField("_isWalkable", true);
            entry.SetPrivateField("_hasCollision", false);
            return entry;
        }
        
        private TileBase CreateTestTile(string name)
        {
            var gameObject = new GameObject(name);
            var tile = gameObject.AddComponent<TileBase>();
            return tile;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class TilesetTestExtensions
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