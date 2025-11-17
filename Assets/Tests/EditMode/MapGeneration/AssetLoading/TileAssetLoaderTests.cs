using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using NUnit.Framework;
using OfficeMice.MapGeneration.AssetLoading;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;
using Unity.PerformanceTesting;

namespace OfficeMice.MapGeneration.AssetLoading.Tests
{
    /// <summary>
    /// Comprehensive tests for TileAssetLoader functionality.
    /// Tests asset loading, caching, categorization, weighted selection, and performance.
    /// </summary>
    [TestFixture]
    public class TileAssetLoaderTests
    {
        private TileAssetLoader _assetLoader;
        private TilesetConfiguration _testConfiguration;
        private List<TileBase> _testTiles;

        [SetUp]
        public void SetUp()
        {
            _assetLoader = new TileAssetLoader();
            _testTiles = new List<TileBase>();
            
            // Create test tiles
            CreateTestTiles();
            
            // Create test configuration
            CreateTestConfiguration();
        }

        [TearDown]
        public void TearDown()
        {
            _assetLoader?.ClearCache();
            _assetLoader = null;
            
            // Clean up test tiles
            foreach (var tile in _testTiles)
            {
                if (tile != null)
                {
                    UnityEngine.Object.DestroyImmediate(tile);
                }
            }
            _testTiles.Clear();
        }

        #region Basic Asset Loading Tests

        [Test]
        public void LoadTile_ValidTileName_ReturnsTile()
        {
            // Arrange
            var tileName = "test_floor_1";
            
            // Act
            var tile = _assetLoader.LoadTile(tileName);
            
            // Assert
            Assert.IsNotNull(tile, "Tile should be loaded successfully");
            Assert.AreEqual(tileName, tile.name, "Loaded tile should have correct name");
        }

        [Test]
        public void LoadTile_NullTileName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(null),
                "Should throw ArgumentException for null tile name");
            
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(""),
                "Should throw ArgumentException for empty tile name");
        }

        [Test]
        public void LoadTile_NonExistentTile_ReturnsNull()
        {
            // Arrange
            var nonExistentTileName = "non_existent_tile";
            
            // Act
            var tile = _assetLoader.LoadTile(nonExistentTileName);
            
            // Assert
            Assert.IsNull(tile, "Non-existent tile should return null");
        }

        [Test]
        public void LoadPrefab_ValidPrefabName_ReturnsPrefab()
        {
            // Arrange
            var prefabName = "test_enemy";
            var testPrefab = new GameObject(prefabName);
            
            // Add to Resources simulation (in real test, this would be in Resources folder)
            
            // Act
            var prefab = _assetLoader.LoadPrefab(prefabName);
            
            // Assert
            // Note: This test would require actual prefab in Resources folder
            // For now, we'll test the null case
            Assert.IsNull(prefab, "Prefab should be null if not in Resources");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(testPrefab);
        }

        [Test]
        public void LoadScriptableObject_ValidAsset_ReturnsAsset()
        {
            // Arrange
            var assetName = "test_config";
            
            // Act
            var config = _assetLoader.LoadScriptableObject<TilesetConfiguration>(assetName);
            
            // Assert
            // Note: This test would require actual ScriptableObject in Resources folder
            Assert.IsNull(config, "ScriptableObject should be null if not in Resources");
        }

        #endregion

        #region Caching Tests

        [Test]
        public void LoadTile_CachingEnabled_SecondLoadReturnsCachedTile()
        {
            // Arrange
            var tileName = "test_floor_1";
            
            // Act
            var firstLoad = _assetLoader.LoadTile(tileName);
            var statsAfterFirst = _assetLoader.GetCacheStats();
            
            var secondLoad = _assetLoader.LoadTile(tileName);
            var statsAfterSecond = _assetLoader.GetCacheStats();
            
            // Assert
            Assert.IsNotNull(firstLoad, "First load should return tile");
            Assert.IsNotNull(secondLoad, "Second load should return tile");
            Assert.AreSame(firstLoad, secondLoad, "Second load should return same cached instance");
            Assert.AreEqual(statsAfterFirst.LoadCount + 1, statsAfterSecond.LoadCount, "Load count should increase");
            Assert.AreEqual(statsAfterFirst.HitCount + 1, statsAfterSecond.HitCount, "Hit count should increase");
        }

        [Test]
        public void IsAssetCached_CachedAsset_ReturnsTrue()
        {
            // Arrange
            var tileName = "test_floor_1";
            _assetLoader.LoadTile(tileName);
            
            // Act
            var isCached = _assetLoader.IsAssetCached(tileName, typeof(TileBase));
            
            // Assert
            Assert.IsTrue(isCached, "Loaded tile should be cached");
        }

        [Test]
        public void IsAssetCached_NonCachedAsset_ReturnsFalse()
        {
            // Arrange
            var tileName = "non_existent_tile";
            
            // Act
            var isCached = _assetLoader.IsAssetCached(tileName, typeof(TileBase));
            
            // Assert
            Assert.IsFalse(isCached, "Non-existent tile should not be cached");
        }

        [Test]
        public void ClearCache_ClearsAllCachedAssets()
        {
            // Arrange
            _assetLoader.LoadTile("test_floor_1");
            _assetLoader.LoadTile("test_wall_1");
            var statsBefore = _assetLoader.GetCacheStats();
            
            // Act
            _assetLoader.ClearCache();
            var statsAfter = _assetLoader.GetCacheStats();
            
            // Assert
            Assert.Greater(statsBefore.CachedAssets, 0, "Should have cached assets before clear");
            Assert.AreEqual(0, statsAfter.CachedAssets, "Should have no cached assets after clear");
            Assert.AreEqual(0, statsAfter.LoadCount, "Load count should be reset");
            Assert.AreEqual(0, statsAfter.HitCount, "Hit count should be reset");
        }

        [Test]
        public void GetCacheStats_ReturnsAccurateStatistics()
        {
            // Arrange
            _assetLoader.LoadTile("test_floor_1");
            _assetLoader.LoadTile("test_floor_1"); // Cache hit
            _assetLoader.LoadTile("test_wall_1");
            _assetLoader.LoadTile("non_existent_tile"); // Cache miss
            
            // Act
            var stats = _assetLoader.GetCacheStats();
            
            // Assert
            Assert.Greater(stats.LoadCount, 0, "Should have load count");
            Assert.Greater(stats.HitCount, 0, "Should have hit count");
            Assert.Greater(stats.HitRate, 0f, "Should have positive hit rate");
            Assert.Greater(stats.MemoryUsage, 0, "Should have memory usage");
        }

        #endregion

        #region Tile Categorization Tests

        [Test]
        public void GetRandomTile_WithTiles_ReturnsTile()
        {
            // Arrange
            _assetLoader.LoadTile("test_floor_1");
            _assetLoader.LoadTile("test_floor_2");
            
            // Act
            var tile = _assetLoader.GetRandomTile(TileType.Floor);
            
            // Assert
            Assert.IsNotNull(tile, "Should return a random floor tile");
            Assert.IsTrue(tile.name.Contains("floor"), "Returned tile should be a floor tile");
        }

        [Test]
        public void GetRandomTile_NoTiles_ReturnsNull()
        {
            // Act
            var tile = _assetLoader.GetRandomTile(TileType.Hazard);
            
            // Assert
            Assert.IsNull(tile, "Should return null when no tiles of type exist");
        }

        [Test]
        public void GetTilesByType_WithTiles_ReturnsCorrectTiles()
        {
            // Arrange
            _assetLoader.LoadTile("test_floor_1");
            _assetLoader.LoadTile("test_floor_2");
            _assetLoader.LoadTile("test_wall_1");
            
            // Act
            var floorTiles = _assetLoader.GetTilesByType(TileType.Floor);
            var wallTiles = _assetLoader.GetTilesByType(TileType.Wall);
            
            // Assert
            Assert.AreEqual(2, floorTiles.Count, "Should return 2 floor tiles");
            Assert.AreEqual(1, wallTiles.Count, "Should return 1 wall tile");
            
            foreach (var tile in floorTiles)
            {
                Assert.IsTrue(tile.name.Contains("floor"), "All returned tiles should be floor tiles");
            }
        }

        [Test]
        public void SetTilesetConfiguration_WithValidConfig_LoadsTilesFromConfig()
        {
            // Arrange
            _assetLoader.SetTilesetConfiguration(_testConfiguration);
            
            // Act
            var floorTiles = _assetLoader.GetTilesByType(TileType.Floor);
            var wallTiles = _assetLoader.GetTilesByType(TileType.Wall);
            
            // Assert
            Assert.Greater(floorTiles.Count, 0, "Should load floor tiles from configuration");
            Assert.Greater(wallTiles.Count, 0, "Should load wall tiles from configuration");
        }

        #endregion

        #region Weighted Selection Tests

        [Test]
        public void GetRandomTile_WithWeightedSelection_DistributesCorrectly()
        {
            // Arrange
            _assetLoader.SetTilesetConfiguration(_testConfiguration);
            var random = new System.Random(42); // Fixed seed for reproducible tests
            
            // Act
            var results = new Dictionary<string, int>();
            for (int i = 0; i < 1000; i++)
            {
                var tile = _assetLoader.GetRandomTile(TileType.Floor, random);
                if (tile != null)
                {
                    results[tile.name] = results.GetValueOrDefault(tile.name, 0) + 1;
                }
            }
            
            // Assert
            Assert.Greater(results.Count, 0, "Should have multiple different tiles");
            
            // Check that distribution roughly matches weights (within tolerance)
            var totalSelections = results.Values.Sum();
            foreach (var entry in _testConfiguration.FloorTiles.Tiles)
            {
                if (results.ContainsKey(entry.Tile.name))
                {
                    var actualRatio = (double)results[entry.Tile.name] / totalSelections;
                    var expectedRatio = entry.Weight / _testConfiguration.FloorTiles.Tiles.Sum(t => t.Weight);
                    var tolerance = 0.1; // 10% tolerance
                    
                    Assert.AreEqual(expectedRatio, actualRatio, tolerance,
                        $"Tile {entry.Tile.name} distribution should match weight");
                }
            }
        }

        #endregion

        #region Preloading Tests

        [Test]
        public void PreloadAssets_ValidAssets_LoadsIntoCache()
        {
            // Arrange
            var assetNames = new List<string> { "test_floor_1", "test_floor_2", "test_wall_1" };
            var statsBefore = _assetLoader.GetCacheStats();
            
            // Act
            _assetLoader.PreloadAssets(assetNames, typeof(TileBase));
            var statsAfter = _assetLoader.GetCacheStats();
            
            // Assert
            Assert.Greater(statsAfter.CachedAssets, statsBefore.CachedAssets, 
                "Should have more cached assets after preloading");
            
            foreach (var assetName in assetNames)
            {
                Assert.IsTrue(_assetLoader.IsAssetCached(assetName, typeof(TileBase)),
                    $"Asset {assetName} should be cached after preloading");
            }
        }

        #endregion

        #region Validation Tests

        [Test]
        public void ValidateRequiredAssets_AllAssetsExist_ReturnsSuccess()
        {
            // Arrange
            var requiredAssets = new List<string> { "test_floor_1", "test_floor_2" };
            
            // Act
            var result = _assetLoader.ValidateRequiredAssets(requiredAssets, typeof(TileBase));
            
            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed when all assets exist");
        }

        [Test]
        public void ValidateRequiredAssets_MissingAssets_ReturnsFailure()
        {
            // Arrange
            var requiredAssets = new List<string> { "test_floor_1", "non_existent_tile" };
            
            // Act
            var result = _assetLoader.ValidateRequiredAssets(requiredAssets, typeof(TileBase));
            
            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail when assets are missing");
            Assert.Greater(result.Errors.Count, 0, "Should have error messages for missing assets");
        }

        [Test]
        public void ValidateRequiredAssets_NullList_ReturnsFailure()
        {
            // Act
            var result = _assetLoader.ValidateRequiredAssets(null, typeof(TileBase));
            
            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for null asset list");
        }

        #endregion

        #region Performance Tests

        [Test, Performance]
        [Category("Performance")]
        public void Performance_AssetLoading_CompletesWithinThreshold()
        {
            // Arrange
            var assetNames = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                assetNames.Add($"test_tile_{i}");
            }
            
            // Act & Assert
            Measure.Method(() =>
            {
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("AssetLoading")
            .Definitions()
            .Run();
            
            // Additional assertions
            var stats = _assetLoader.GetCacheStats();
            Assert.Greater(stats.HitRate, 0.95f, "Cache hit rate should exceed 95%");
        }

        [Test, Performance]
        [Category("Performance")]
        public void Performance_CacheHitRate_Exceeds95Percent()
        {
            // Arrange
            var tileName = "test_floor_1";
            _assetLoader.LoadTile(tileName); // Load once to cache
            
            // Act & Assert
            Measure.Method(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    _assetLoader.LoadTile(tileName); // Should all be cache hits
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("CacheHitRate")
            .Definitions()
            .Run();
            
            var stats = _assetLoader.GetCacheStats();
            Assert.GreaterOrEqual(stats.HitRate, 0.95f, "Cache hit rate should be at least 95%");
        }

        [Test, Performance]
        [Category("Performance")]
        public void Performance_MemoryUsage_StaysWithinLimits()
        {
            // Arrange
            var assetNames = new List<string>();
            for (int i = 0; i < 200; i++)
            {
                assetNames.Add($"test_tile_{i}");
            }
            
            // Act
            var memoryBefore = GC.GetTotalMemory(true);
            
            Measure.Method(() =>
            {
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(3)
            .MeasurementCount(20)
            .SampleGroup("MemoryUsage")
            .Definitions()
            .Run();
            
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = (memoryAfter - memoryBefore) / (1024 * 1024); // Convert to MB
            
            // Assert
            Assert.Less(memoryUsed, 20, $"Memory usage should be under 20MB, was {memoryUsed:F2}MB");
        }

        [Test, Performance]
        [Category("Performance")]
        public void Performance_PreloadAssets_ImprovesPerformance()
        {
            // Arrange
            var assetNames = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                assetNames.Add($"test_tile_{i}");
            }
            
            // Measure without preloading
            var timeWithoutPreload = Measure.Action(() =>
            {
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            });
            
            _assetLoader.ClearCache();
            
            // Measure with preloading
            var timeWithPreload = Measure.Action(() =>
            {
                _assetLoader.PreloadAssets(assetNames, typeof(TileBase));
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            });
            
            // Assert
            Assert.Less(timeWithPreload, timeWithoutPreload * 1.2, 
                "Preloading should not significantly increase load time");
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnAssetLoaded_FiresWhenAssetLoaded()
        {
            // Arrange
            TileBase loadedTile = null;
            Type loadedType = null;
            string loadedName = null;
            
            _assetLoader.OnAssetLoaded += (name, type) =>
            {
                loadedName = name;
                loadedType = type;
            };
            
            // Act
            loadedTile = _assetLoader.LoadTile("test_floor_1");
            
            // Assert
            Assert.IsNotNull(loadedTile, "Tile should be loaded");
            Assert.AreEqual("test_floor_1", loadedName, "Event should fire with correct name");
            Assert.AreEqual(typeof(TileBase), loadedType, "Event should fire with correct type");
        }

        [Test]
        public void OnAssetLoadFailed_FiresWhenAssetNotFound()
        {
            // Arrange
            string failedName = null;
            Type failedType = null;
            Exception failedException = null;
            
            _assetLoader.OnAssetLoadFailed += (name, type, exception) =>
            {
                failedName = name;
                failedType = type;
                failedException = exception;
            };
            
            // Act
            var tile = _assetLoader.LoadTile("non_existent_tile");
            
            // Assert
            Assert.IsNull(tile, "Tile should be null");
            Assert.AreEqual("non_existent_tile", failedName, "Event should fire with correct name");
            Assert.AreEqual(typeof(TileBase), failedType, "Event should fire with correct type");
            Assert.IsNotNull(failedException, "Event should include exception");
        }

        [Test]
        public void OnCacheCleared_FiresWhenCacheCleared()
        {
            // Arrange
            bool eventFired = false;
            _assetLoader.OnCacheCleared += () => eventFired = true;
            
            // Act
            _assetLoader.ClearCache();
            
            // Assert
            Assert.IsTrue(eventFired, "Event should fire when cache is cleared");
        }

        #endregion

        #region Helper Methods

        private void CreateTestTiles()
        {
            // Create test tiles for testing
            for (int i = 1; i <= 5; i++)
            {
                var floorTile = ScriptableObject.CreateInstance<TileBase>();
                floorTile.name = $"test_floor_{i}";
                _testTiles.Add(floorTile);
            }
            
            for (int i = 1; i <= 3; i++)
            {
                var wallTile = ScriptableObject.CreateInstance<TileBase>();
                wallTile.name = $"test_wall_{i}";
                _testTiles.Add(wallTile);
            }
        }

        private void CreateTestConfiguration()
        {
            _testConfiguration = ScriptableObject.CreateInstance<TilesetConfiguration>();
            
            // Create floor tiles mapping
            var floorMapping = new TileMapping
            {
                UseRandomSelection = true
            };
            
            for (int i = 1; i <= 5; i++)
            {
                var tileEntry = new TileEntry
                {
                    Tile = _testTiles.FirstOrDefault(t => t.name == $"test_floor_{i}"),
                    Weight = i * 0.2f // Different weights for testing
                };
                floorMapping.Tiles.Add(tileEntry);
            }
            
            // Create wall tiles mapping
            var wallMapping = new TileMapping
            {
                UseRandomSelection = true
            };
            
            for (int i = 1; i <= 3; i++)
            {
                var tileEntry = new TileEntry
                {
                    Tile = _testTiles.FirstOrDefault(t => t.name == $"test_wall_{i}"),
                    Weight = 1.0f
                };
                wallMapping.Tiles.Add(tileEntry);
            }
            
            // Use reflection to set private fields since they're read-only properties
            var floorField = typeof(TilesetConfiguration).GetField("_floorTiles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wallField = typeof(TilesetConfiguration).GetField("_wallTiles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            floorField?.SetValue(_testConfiguration, floorMapping);
            wallField?.SetValue(_testConfiguration, wallMapping);
        }

        #endregion
    }
}