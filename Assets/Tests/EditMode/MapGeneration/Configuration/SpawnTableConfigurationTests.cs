using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Data;
using System.Collections.Generic;
using System.Linq;

namespace OfficeMice.MapGeneration.Configuration.Tests
{
    [TestFixture]
    public class SpawnTableConfigurationTests
    {
        private SpawnTableConfiguration _spawnTable;
        private GameObject _testGameObject;
        private MapData _testMapData;
        
        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestSpawnTable");
            _spawnTable = _testGameObject.AddComponent<SpawnTableConfiguration>();
            
            // Create test map data
            _testMapData = CreateTestMapData();
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
        public void SpawnTableConfiguration_WithValidData_PassesValidation()
        {
            // Arrange
            SetupValidSpawnTable();
            
            // Act
            var result = _spawnTable.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid spawn table should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid spawn table should have no errors");
        }
        
        [Test]
        public void SpawnTableConfiguration_WithMissingID_FailsValidation()
        {
            // Arrange
            SetupValidSpawnTable();
            _spawnTable.SetPrivateField("_spawnTableID", "");
            
            // Act
            var result = _spawnTable.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Spawn table with missing ID should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Spawn table ID is required")), 
                "Should have error about missing spawn table ID");
        }
        
        [Test]
        public void SpawnTableConfiguration_WithNoSpawnEntries_FailsValidation()
        {
            // Arrange
            SetupValidSpawnTable();
            _spawnTable.SetPrivateField("_spawnEntries", new List<SpawnEntry>());
            
            // Act
            var result = _spawnTable.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Spawn table with no spawn entries should fail validation");
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("at least one spawn entry")), 
                "Should have error about missing spawn entries");
        }
        
        [Test]
        public void SpawnTableConfiguration_WithZeroTotalWeight_FailsValidation()
        {
            // Arrange
            SetupValidSpawnTable();
            var entries = new List<SpawnEntry>
            {
                CreateTestSpawnEntry("Enemy1", 0f) // Zero weight
            };
            _spawnTable.SetPrivateField("_spawnEntries", entries);
            
            // Act
            var result = _spawnTable.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Spawn table with zero total weight should fail validation");
        }
        
        [Test]
        public void GetRandomSpawnEntry_WithValidEntries_ReturnsEntry()
        {
            // Arrange
            SetupValidSpawnTable();
            var entries = new List<SpawnEntry>
            {
                CreateTestSpawnEntry("Enemy1", 1.0f),
                CreateTestSpawnEntry("Enemy2", 1.0f)
            };
            _spawnTable.SetPrivateField("_spawnEntries", entries);
            
            // Act
            var result = _spawnTable.GetRandomSpawnEntry(new System.Random(1)); // Fixed seed
            
            // Assert
            Assert.IsNotNull(result, "Should return a spawn entry");
            Assert.IsTrue(result.EnemyType == "Enemy1" || result.EnemyType == "Enemy2", 
                "Should return one of the defined enemy types");
        }
        
        [Test]
        public void GetRandomSpawnEntry_WithNoEntries_ReturnsNull()
        {
            // Arrange
            SetupValidSpawnTable();
            _spawnTable.SetPrivateField("_spawnEntries", new List<SpawnEntry>());
            
            // Act
            var result = _spawnTable.GetRandomSpawnEntry();
            
            // Assert
            Assert.IsNull(result, "Should return null when no spawn entries exist");
        }
        
        [Test]
        public void GetRandomSpawnEntry_RespectsWeights()
        {
            // Arrange
            SetupValidSpawnTable();
            var entries = new List<SpawnEntry>
            {
                CreateTestSpawnEntry("LightEnemy", 9.0f), // 90% chance
                CreateTestSpawnEntry("HeavyEnemy", 1.0f)  // 10% chance
            };
            _spawnTable.SetPrivateField("_spawnEntries", entries);
            
            // Act
            var lightCount = 0;
            var heavyCount = 0;
            var random = new System.Random(1);
            
            for (int i = 0; i < 100; i++)
            {
                var result = _spawnTable.GetRandomSpawnEntry(random);
                if (result.EnemyType == "LightEnemy")
                    lightCount++;
                else if (result.EnemyType == "HeavyEnemy")
                    heavyCount++;
            }
            
            // Assert
            Assert.IsTrue(lightCount > heavyCount, "Light enemy should be selected more often due to higher weight");
            Assert.IsTrue(lightCount > 80, "Light enemy should be selected around 90% of the time");
            Assert.IsTrue(heavyCount < 20, "Heavy enemy should be selected around 10% of the time");
        }
        
        [Test]
        public void GetSpawnEntriesForWave_WithNoWaveDefinitions_ReturnsAllEntries()
        {
            // Arrange
            SetupValidSpawnTable();
            var entries = new List<SpawnEntry>
            {
                CreateTestSpawnEntry("Enemy1", 1.0f),
                CreateTestSpawnEntry("Enemy2", 1.0f)
            };
            _spawnTable.SetPrivateField("_spawnEntries", entries);
            _spawnTable.SetPrivateField("_waveDefinitions", new List<WaveDefinition>());
            
            // Act
            var result = _spawnTable.GetSpawnEntriesForWave(0);
            
            // Assert
            Assert.AreEqual(2, result.Count, "Should return all spawn entries when no wave definitions exist");
        }
        
        [Test]
        public void GetSpawnEntriesForWave_WithWaveDefinition_ReturnsModifiedEntries()
        {
            // Arrange
            SetupValidSpawnTable();
            var entries = new List<SpawnEntry>
            {
                CreateTestSpawnEntry("Enemy1", 1.0f)
            };
            var waveEntries = new List<WaveEntry>
            {
                CreateTestWaveEntry("Enemy1", 5, 2.0f, 1.5f)
            };
            var waveDef = new WaveDefinition();
            waveDef.SetPrivateField("_entries", waveEntries);
            var waveDefs = new List<WaveDefinition> { waveDef };
            
            _spawnTable.SetPrivateField("_spawnEntries", entries);
            _spawnTable.SetPrivateField("_waveDefinitions", waveDefs);
            
            // Act
            var result = _spawnTable.GetSpawnEntriesForWave(0);
            
            // Assert
            Assert.AreEqual(1, result.Count, "Should return one entry");
            Assert.AreEqual(5, result[0].MinCount, "Should use wave count");
            Assert.AreEqual(5, result[0].MaxCount, "Should use wave count");
            Assert.AreEqual(2.0f, result[0].HealthMultiplier, "Should apply wave health multiplier");
            Assert.AreEqual(1.5f, result[0].DamageMultiplier, "Should apply wave damage multiplier");
        }
        
        [Test]
        public void GetScaledEnemyCount_WithScalingEnabled_ReturnsScaledCount()
        {
            // Arrange
            SetupValidSpawnTable();
            _spawnTable.SetPrivateField("_scaleWithPlayerProgress", true);
            _spawnTable.SetPrivateField("_difficultyMultiplier", 2.0f);
            
            var difficultyScaling = new DifficultyScaling();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 2f);
            difficultyScaling.SetPrivateField("_scalingCurve", curve);
            _spawnTable.SetPrivateField("_difficultyScaling", difficultyScaling);
            
            // Act
            var result = _spawnTable.GetScaledEnemyCount(10, 0.5f); // 50% progress
            
            // Assert
            Assert.AreEqual(20, result, "Should scale enemy count based on progress and multiplier");
        }
        
        [Test]
        public void GetScaledEnemyCount_WithScalingDisabled_ReturnsOriginalCount()
        {
            // Arrange
            SetupValidSpawnTable();
            _spawnTable.SetPrivateField("_scaleWithPlayerProgress", false);
            
            // Act
            var result = _spawnTable.GetScaledEnemyCount(10, 0.5f);
            
            // Assert
            Assert.AreEqual(10, result, "Should return original count when scaling is disabled");
        }
        
        [Test]
        public void CanSpawnAtPosition_WithWalkablePosition_ReturnsTrue()
        {
            // Arrange
            SetupValidSpawnTable();
            var position = new Vector2Int(5, 5); // Center of test room
            
            // Act
            var result = _spawnTable.CanSpawnAtPosition(position, _testMapData);
            
            // Assert
            Assert.IsTrue(result, "Should allow spawning at walkable position");
        }
        
        [Test]
        public void CanSpawnAtPosition_WithNonWalkablePosition_ReturnsFalse()
        {
            // Arrange
            SetupValidSpawnTable();
            var position = new Vector2Int(0, 0); // Outside test room
            
            // Act
            var result = _spawnTable.CanSpawnAtPosition(position, _testMapData);
            
            // Assert
            Assert.IsFalse(result, "Should not allow spawning at non-walkable position");
        }
        
        [Test]
        public void CanSpawnAtPosition_WithNullMapData_ReturnsFalse()
        {
            // Arrange
            SetupValidSpawnTable();
            var position = new Vector2Int(5, 5);
            
            // Act
            var result = _spawnTable.CanSpawnAtPosition(position, null);
            
            // Assert
            Assert.IsFalse(result, "Should not allow spawning with null map data");
        }
        
        [Test]
        public void SpawnEntry_WithValidData_PassesValidation()
        {
            // Arrange
            var entry = CreateTestSpawnEntry("TestEnemy", 1.0f);
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid spawn entry should pass validation");
        }
        
        [Test]
        public void SpawnEntry_WithMissingEnemyType_FailsValidation()
        {
            // Arrange
            var entry = CreateTestSpawnEntry("", 1.0f); // Empty enemy type
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Spawn entry with missing enemy type should fail validation");
        }
        
        [Test]
        public void SpawnEntry_WithInvalidCountRange_FailsValidation()
        {
            // Arrange
            var entry = CreateTestSpawnEntry("TestEnemy", 1.0f);
            entry.SetPrivateField("_minCount", 5);
            entry.SetPrivateField("_maxCount", 3); // Max < Min
            
            // Act
            var result = entry.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Spawn entry with invalid count range should fail validation");
        }
        
        [Test]
        public void SpawnEntry_GetRandomCount_ReturnsValidRange()
        {
            // Arrange
            var entry = CreateTestSpawnEntry("TestEnemy", 1.0f);
            entry.SetPrivateField("_minCount", 2);
            entry.SetPrivateField("_maxCount", 5);
            
            // Act
            var results = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                results.Add(entry.GetRandomCount(new System.Random(i)));
            }
            
            // Assert
            Assert.IsTrue(results.All(c => c >= 2 && c <= 5), "All counts should be within range");
            Assert.IsTrue(results.Distinct().Count() > 1, "Should generate variety of counts");
        }
        
        [Test]
        public void WaveDefinition_WithValidData_PassesValidation()
        {
            // Arrange
            var waveDef = new WaveDefinition();
            waveDef.SetPrivateField("_waveName", "TestWave");
            waveDef.SetPrivateField("_entries", new List<WaveEntry> { CreateTestWaveEntry("Enemy1", 3) });
            waveDef.SetPrivateField("_waveDelay", 2.0f);
            
            // Act
            var result = waveDef.Validate();
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid wave definition should pass validation");
        }
        
        [Test]
        public void WaveDefinition_WithNoEntries_FailsValidation()
        {
            // Arrange
            var waveDef = new WaveDefinition();
            waveDef.SetPrivateField("_waveName", "TestWave");
            waveDef.SetPrivateField("_entries", new List<WaveEntry>());
            
            // Act
            var result = waveDef.Validate();
            
            // Assert
            Assert.IsFalse(result.IsValid, "Wave definition with no entries should fail validation");
        }
        
        [Test]
        public void DifficultyScaling_GetScalingFactor_ReturnsCorrectValue()
        {
            // Arrange
            var scaling = new DifficultyScaling();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(0.5f, 1.5f);
            curve.AddKey(1f, 2f);
            scaling.SetPrivateField("_scalingCurve", curve);
            scaling.SetPrivateField("_maxScaling", 3f);
            
            // Act
            var result = scaling.GetScalingFactor(0.5f);
            
            // Assert
            Assert.AreEqual(1.5f, result, "Should return correct scaling factor at 50% progress");
        }
        
        [Test]
        public void DifficultyScaling_GetScalingFactor_ClampsToMax()
        {
            // Arrange
            var scaling = new DifficultyScaling();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 5f); // Above max
            scaling.SetPrivateField("_scalingCurve", curve);
            scaling.SetPrivateField("_maxScaling", 3f);
            
            // Act
            var result = scaling.GetScalingFactor(1f);
            
            // Assert
            Assert.AreEqual(3f, result, "Should clamp scaling factor to maximum");
        }
        
        [Test]
        public void SpawnCondition_WithAlwaysType_ReturnsTrue()
        {
            // Arrange
            var condition = new SpawnCondition();
            condition.SetPrivateField("_conditionType", ConditionType.Always);
            
            // Act
            var result = condition.IsMet(Vector2Int.zero, _testMapData);
            
            // Assert
            Assert.IsTrue(result, "Always condition should always return true");
        }
        
        [Test]
        public void SpawnCondition_WithRoomTypeCondition_ReturnsCorrectResult()
        {
            // Arrange
            var condition = new SpawnCondition();
            condition.SetPrivateField("_conditionType", ConditionType.RoomType);
            condition.SetPrivateField("_parameter", "Office");
            
            // Act
            var result = condition.IsMet(new Vector2Int(5, 5), _testMapData); // Inside test room
            
            // Assert
            Assert.IsTrue(result, "Should return true for matching room type");
        }
        
        private void SetupValidSpawnTable()
        {
            _spawnTable.SetPrivateField("_spawnTableID", "test_spawn_table");
            _spawnTable.SetPrivateField("_tableName", "Test Spawn Table");
            _spawnTable.SetPrivateField("_description", "A test spawn table");
            _spawnTable.SetPrivateField("_tableType", SpawnTableType.Standard);
            _spawnTable.SetPrivateField("_spawnEntries", new List<SpawnEntry> { CreateTestSpawnEntry("TestEnemy", 1.0f) });
            _spawnTable.SetPrivateField("_ensureMinimumSpawns", true);
            _spawnTable.SetPrivateField("_minimumSpawns", 1);
            _spawnTable.SetPrivateField("_wavePattern", WavePattern.Sequential);
            _spawnTable.SetPrivateField("_maxConcurrentEnemies", 5);
            _spawnTable.SetPrivateField("_spawnDelayBetweenWaves", 2f);
            _spawnTable.SetPrivateField("_difficultyScaling", new DifficultyScaling());
            _spawnTable.SetPrivateField("_scaleWithPlayerProgress", true);
            _spawnTable.SetPrivateField("_difficultyMultiplier", 1f);
            _spawnTable.SetPrivateField("_spawnRules", new SpawnRules());
            _spawnTable.SetPrivateField("_spawnConditions", new List<SpawnCondition>());
            _spawnTable.SetPrivateField("_respectExistingSpawnPoints", true);
            _spawnTable.SetPrivateField("_useWaveSpawnerSystem", true);
            _spawnTable.SetPrivateField("_spawnPointTag", "Spawn Point");
            _spawnTable.SetPrivateField("_useObjectPooling", true);
            _spawnTable.SetPrivateField("_spawnRadius", 10f);
        }
        
        private SpawnEntry CreateTestSpawnEntry(string enemyType, float weight)
        {
            var entry = new SpawnEntry();
            entry.SetPrivateField("_enemyType", enemyType);
            entry.SetPrivateField("_enemyPrefab", new GameObject(enemyType));
            entry.SetPrivateField("_weight", weight);
            entry.SetPrivateField("_minCount", 1);
            entry.SetPrivateField("_maxCount", 3);
            entry.SetPrivateField("_spawnDelay", 0.5f);
            entry.SetPrivateField("_healthMultiplier", 1f);
            entry.SetPrivateField("_damageMultiplier", 1f);
            return entry;
        }
        
        private WaveEntry CreateTestWaveEntry(string enemyType, int count, float healthMultiplier = 1f, float damageMultiplier = 1f)
        {
            var entry = new WaveEntry();
            entry.SetPrivateField("_enemyType", enemyType);
            entry.SetPrivateField("_count", count);
            entry.SetPrivateField("_healthMultiplier", healthMultiplier);
            entry.SetPrivateField("_damageMultiplier", damageMultiplier);
            entry.SetPrivateField("_spawnDelay", 0.5f);
            return entry;
        }
        
        private MapData CreateTestMapData()
        {
            var mapData = new MapData(12345, new Vector2Int(20, 20));
            
            // Add a test room
            var roomBounds = new RectInt(2, 2, 10, 10);
            var room = new RoomData(roomBounds);
            room.SetClassification(RoomClassification.Office);
            mapData.AddRoom(room);
            
            // Set player spawn in the room
            mapData.SetPlayerSpawn(new Vector2Int(5, 5));
            
            return mapData;
        }
    }
    
    /// <summary>
    /// Helper extension methods for testing private fields
    /// </summary>
    public static class SpawnTableTestExtensions
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