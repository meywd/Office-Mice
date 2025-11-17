using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class RoomTemplateTests
    {
        private RoomTemplate _roomTemplate;
        private GameObject _testGameObject;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestRoomTemplate");
            _roomTemplate = _testGameObject.AddComponent<RoomTemplate>();
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
        public void RoomTemplate_WithValidData_PassesValidation()
        {
            // Arrange
            SetupValidRoomTemplate();
            
            // Act
            var result = _roomTemplate.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid room template should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid room template should have no errors");
        }
        
        [Test]
        public void RoomTemplate_WithMissingID_FailsValidation()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_templateID", "");
            
            // Act
            var result = _roomTemplate.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Room template with missing ID should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Template ID is required")), 
                "Should have error about missing template ID");
        }
        
        [Test]
        public void RoomTemplate_WithInvalidSizeRange_FailsValidation()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_minWidth", 10);
            _roomTemplate.SetPrivateField("_maxWidth", 5); // Max < Min
            
            // Act
            var result = _roomTemplate.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Room template with invalid size range should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Min width")), 
                "Should have error about invalid width range");
        }
        
        [Test]
        public void RoomTemplate_WithMissingFloorPattern_FailsValidation()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_floorPattern", null);
            
            // Act
            var result = _roomTemplate.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Room template with missing floor pattern should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Floor pattern is required")), 
                "Should have error about missing floor pattern");
        }
        
        [Test]
        public void CanFitInRoom_WithValidDimensions_ReturnsTrue()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_minWidth", 5);
            _roomTemplate.SetPrivateField("_maxWidth", 10);
            _roomTemplate.SetPrivateField("_minHeight", 5);
            _roomTemplate.SetPrivateField("_maxHeight", 10);
            
            // Act
            var canFit = _roomTemplate.CanFitInRoom(7, 8);
            
            // Assert
            Assert.IsTrue(canFit, "Should fit in room with valid dimensions");
        }
        
        [Test]
        public void CanFitInRoom_WithTooSmallDimensions_ReturnsFalse()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_minWidth", 5);
            _roomTemplate.SetPrivateField("_minHeight", 5);
            
            // Act
            var canFit = _roomTemplate.CanFitInRoom(3, 4);
            
            // Assert
            Assert.IsFalse(canFit, "Should not fit in room with too small dimensions");
        }
        
        [Test]
        public void CanFitInRoom_WithTooLargeDimensions_ReturnsFalse()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_maxWidth", 10);
            _roomTemplate.SetPrivateField("_maxHeight", 10);
            
            // Act
            var canFit = _roomTemplate.CanFitInRoom(12, 15);
            
            // Assert
            Assert.IsFalse(canFit, "Should not fit in room with too large dimensions");
        }
        
        [Test]
        public void IsCompatibleWithClassification_WithMatchingClassification_ReturnsTrue()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Office);
            
            // Act
            var isCompatible = _roomTemplate.IsCompatibleWithClassification(RoomClassification.Office);
            
            // Assert
            Assert.IsTrue(isCompatible, "Should be compatible with matching classification");
        }
        
        [Test]
        public void IsCompatibleWithClassification_WithUnassignedClassification_ReturnsTrue()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Unassigned);
            
            // Act
            var isCompatible = _roomTemplate.IsCompatibleWithClassification(RoomClassification.Storage);
            
            // Assert
            Assert.IsTrue(isCompatible, "Should be compatible with any classification when unassigned");
        }
        
        [Test]
        public void IsCompatibleWithClassification_WithNonMatchingClassification_ReturnsFalse()
        {
            // Arrange
            SetupValidRoomTemplate();
            _roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Office);
            
            // Act
            var isCompatible = _roomTemplate.IsCompatibleWithClassification(RoomClassification.Storage);
            
            // Assert
            Assert.IsFalse(isCompatible, "Should not be compatible with non-matching classification");
        }
        
        [Test]
        public void GetValidSpawnPositions_WithRelativeSpawnPoints_ReturnsValidPositions()
        {
            // Arrange
            SetupValidRoomTemplate();
            var spawnPoints = new List<EnemySpawnPoint>
            {
                new EnemySpawnPoint { Position = new Vector2Int(2, 2), IsRelative = true }
            };
            _roomTemplate.SetPrivateField("_enemySpawnPoints", spawnPoints);
            
            // Act
            var positions = _roomTemplate.GetValidSpawnPositions(5, 5);
            
            // Assert
            Assert.AreEqual(1, positions.Count, "Should return one valid spawn position");
            Assert.AreEqual(new Vector2Int(2, 2), positions[0], "Should return the correct spawn position");
        }
        
        [Test]
        public void GetValidSpawnPositions_WithOutOfBoundsSpawnPoints_ReturnsEmptyList()
        {
            // Arrange
            SetupValidRoomTemplate();
            var spawnPoints = new List<EnemySpawnPoint>
            {
                new EnemySpawnPoint { Position = new Vector2Int(10, 10), IsRelative = true }
            };
            _roomTemplate.SetPrivateField("_enemySpawnPoints", spawnPoints);
            
            // Act
            var positions = _roomTemplate.GetValidSpawnPositions(5, 5);
            
            // Assert
            Assert.AreEqual(0, positions.Count, "Should return no positions for out-of-bounds spawn points");
        }
        
        [Test]
        public void TilePattern_WithValidData_PassesValidation()
        {
            // Arrange
            var tilePattern = new TilePattern();
            tilePattern.SetPrivateField("_patternName", "TestPattern");
            tilePattern.SetPrivateField("_tile", CreateTestTile());
            tilePattern.SetPrivateField("_probability", 0.5f);
            
            // Act
            var result = tilePattern.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tile pattern should pass validation");
        }
        
        [Test]
        public void TilePattern_WithMissingTile_FailsValidation()
        {
            // Arrange
            var tilePattern = new TilePattern();
            tilePattern.SetPrivateField("_patternName", "TestPattern");
            tilePattern.SetPrivateField("_tile", null);
            
            // Act
            var result = tilePattern.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tile pattern with missing tile should fail validation");
        }
        
        [Test]
        public void FurniturePlacement_WithValidData_PassesValidation()
        {
            // Arrange
            var furniture = new FurniturePlacement();
            furniture.SetPrivateField("_furnitureType", "Desk");
            furniture.SetPrivateField("_position", new Vector2Int(1, 1));
            furniture.SetPrivateField("_size", new Vector2Int(2, 1));
            furniture.SetPrivateField("_spawnProbability", 0.8f);
            
            // Act
            var result = furniture.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid furniture placement should pass validation");
        }
        
        [Test]
        public void FurniturePlacement_WithInvalidSize_FailsValidation()
        {
            // Arrange
            var furniture = new FurniturePlacement();
            furniture.SetPrivateField("_furnitureType", "Desk");
            furniture.SetPrivateField("_size", new Vector2Int(0, 1)); // Invalid size
            
            // Act
            var result = furniture.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Furniture placement with invalid size should fail validation");
        }
        
        [Test]
        public void EnemySpawnPoint_WithValidData_PassesValidation()
        {
            // Arrange
            var spawnPoint = new EnemySpawnPoint();
            spawnPoint.SetPrivateField("_enemyType", "Guard");
            spawnPoint.SetPrivateField("_position", new Vector2Int(2, 2));
            spawnPoint.SetPrivateField("_spawnDelay", 1.0f);
            spawnPoint.SetPrivateField("_spawnProbability", 0.7f);
            
            // Act
            var result = spawnPoint.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid enemy spawn point should pass validation");
        }
        
        [Test]
        public void EnemySpawnPoint_WithNegativeDelay_FailsValidation()
        {
            // Arrange
            var spawnPoint = new EnemySpawnPoint();
            spawnPoint.SetPrivateField("_enemyType", "Guard");
            spawnPoint.SetPrivateField("_spawnDelay", -1.0f); // Negative delay
            
            // Act
            var result = spawnPoint.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Enemy spawn point with negative delay should fail validation");
        }
        
        [Test]
        public void DoorwayTemplate_WithValidData_PassesValidation()
        {
            // Arrange
            var doorway = new DoorwayTemplate();
            doorway.SetPrivateField("_position", new Vector2Int(0, 0));
            doorway.SetPrivateField("_orientation", DoorOrientation.Horizontal);
            doorway.SetPrivateField("_width", 2);
            doorway.SetPrivateField("_priority", 0.8f);
            
            // Act
            var result = doorway.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid doorway template should pass validation");
        }
        
        [Test]
        public void DoorwayTemplate_WithInvalidWidth_FailsValidation()
        {
            // Arrange
            var doorway = new DoorwayTemplate();
            doorway.SetPrivateField("_width", 0); // Invalid width
            
            // Act
            var result = doorway.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Doorway template with invalid width should fail validation");
        }
        
        private void SetupValidRoomTemplate()
        {
            _roomTemplate.SetPrivateField("_templateID", "test_template");
            _roomTemplate.SetPrivateField("_templateName", "Test Template");
            _roomTemplate.SetPrivateField("_description", "A test room template");
            _roomTemplate.SetPrivateField("_minWidth", 3);
            _roomTemplate.SetPrivateField("_minHeight", 3);
            _roomTemplate.SetPrivateField("_maxWidth", 10);
            _roomTemplate.SetPrivateField("_maxHeight", 10);
            _roomTemplate.SetPrivateField("_requiredClassification", RoomClassification.Unassigned);
            _roomTemplate.SetPrivateField("_floorPattern", new TilePattern());
            _roomTemplate.SetPrivateField("_wallPattern", new TilePattern());
            _roomTemplate.SetPrivateField("_furnitureDensity", 0.3f);
            _roomTemplate.SetPrivateField("_ensurePathing", true);
            _roomTemplate.SetPrivateField("_autoPlacePlayerSpawn", true);
            _roomTemplate.SetPrivateField("_minDoorways", 1);
            _roomTemplate.SetPrivateField("_maxDoorways", 4);
        }
        
        private TileBase CreateTestTile()
        {
            // Create a simple test tile
            var gameObject = new GameObject("TestTile");
            var tile = gameObject.AddComponent<TileBase>();
            return tile;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class TestExtensions
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