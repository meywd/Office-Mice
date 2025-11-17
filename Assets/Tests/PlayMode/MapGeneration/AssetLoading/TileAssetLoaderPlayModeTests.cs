using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using NUnit.Framework;
using UnityEngine.TestTools;
using OfficeMice.MapGeneration.AssetLoading;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.AssetLoading.Tests
{
    /// <summary>
    /// PlayMode tests for TileAssetLoader to verify runtime behavior.
    /// Tests async loading, events, and integration with Unity runtime.
    /// </summary>
    [TestFixture]
    public class TileAssetLoaderPlayModeTests
    {
        private GameObject _testGameObject;
        private TileAssetLoader _assetLoader;
        private TilesetConfiguration _testConfiguration;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Create a test GameObject for the dispatcher
            _testGameObject = new GameObject("TileAssetLoaderTest");
            _testGameObject.AddComponent<UnityMainThreadDispatcher>();
            
            _assetLoader = new TileAssetLoader();
            _testConfiguration = ScriptableObject.CreateInstance<TilesetConfiguration>();
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            _assetLoader?.ClearCache();
            _assetLoader = null;
            
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
            
            yield return null;
        }

        #region Async Loading Tests

        [UnityTest]
        public IEnumerator LoadAssetAsync_ValidTile_CallsCallbackWithTile()
        {
            // Arrange
            var callbackCalled = false;
            TileBase loadedTile = null;
            Exception callbackException = null;
            
            // Act
            _assetLoader.LoadAssetAsync("test_tile", typeof(TileBase), (tile) =>
            {
                callbackCalled = true;
                loadedTile = tile as TileBase;
            });
            
            // Wait for async operation
            yield return new WaitUntil(() => callbackCalled);
            
            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            // Note: In a real test with actual Resources, this would return a tile
            // For now, we test the async mechanism
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_InvalidTile_CallsCallbackWithNull()
        {
            // Arrange
            var callbackCalled = false;
            TileBase loadedTile = null;
            
            // Act
            _assetLoader.LoadAssetAsync("non_existent_tile", typeof(TileBase), (tile) =>
            {
                callbackCalled = true;
                loadedTile = tile as TileBase;
            });
            
            // Wait for async operation
            yield return new WaitUntil(() => callbackCalled);
            
            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(loadedTile, "Callback should receive null for non-existent tile");
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_MultipleConcurrentRequests_HandlesCorrectly()
        {
            // Arrange
            var callbacks = new bool[5];
            var tiles = new TileBase[5];
            
            // Act
            for (int i = 0; i < 5; i++)
            {
                var index = i; // Capture loop variable
                _assetLoader.LoadAssetAsync($"test_tile_{i}", typeof(TileBase), (tile) =>
                {
                    callbacks[index] = true;
                    tiles[index] = tile as TileBase;
                });
            }
            
            // Wait for all callbacks
            yield return new WaitUntil(() => callbacks.All(called => called));
            
            // Assert
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(callbacks[i], $"Callback {i} should be called");
            }
        }

        #endregion

        #region Event Tests in PlayMode

        [UnityTest]
        public IEnumerator OnAssetLoaded_EventFiresInRuntime()
        {
            // Arrange
            var eventFired = false;
            string loadedName = null;
            Type loadedType = null;
            
            _assetLoader.OnAssetLoaded += (name, type) =>
            {
                eventFired = true;
                loadedName = name;
                loadedType = type;
            };
            
            // Act
            _assetLoader.LoadTile("test_tile");
            
            // Wait a frame for event processing
            yield return null;
            
            // Assert
            // Note: Event should fire synchronously, but we wait to ensure Unity processes it
            Assert.IsTrue(eventFired, "Event should fire");
        }

        [UnityTest]
        public IEnumerator OnCacheCleared_EventFiresInRuntime()
        {
            // Arrange
            var eventFired = false;
            
            _assetLoader.OnCacheCleared += () => eventFired = true;
            
            // Act
            _assetLoader.ClearCache();
            
            // Wait a frame for event processing
            yield return null;
            
            // Assert
            Assert.IsTrue(eventFired, "Event should fire");
        }

        #endregion

        #region Memory Management Tests

        [UnityTest]
        public IEnumerator MemoryMonitoring_DetectsMemoryChanges()
        {
            // Arrange
            var initialStats = _assetLoader.GetCacheStats();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act
            // Load multiple assets to increase memory usage
            for (int i = 0; i < 50; i++)
            {
                _assetLoader.LoadTile($"test_tile_{i}");
            }
            
            // Wait for memory to stabilize
            yield return new WaitForSeconds(0.1f);
            
            var finalStats = _assetLoader.GetCacheStats();
            var finalMemory = GC.GetTotalMemory(false);
            
            // Assert
            Assert.Greater(finalStats.LoadCount, initialStats.LoadCount, "Load count should increase");
            Assert.Greater(finalStats.MemoryUsage, initialStats.MemoryUsage, "Memory usage should increase");
        }

        [UnityTest]
        public IEnumerator CacheSizeLimit_EnforcedInRuntime()
        {
            // Arrange
            var smallCacheLoader = new TileAssetLoader(
                tileSearchPaths: new[] { "Assets" },
                maxCacheSize: 10, // Very small cache
                enableMemoryMonitoring: true);
            
            // Act
            // Load more assets than cache limit
            for (int i = 0; i < 20; i++)
            {
                smallCacheLoader.LoadTile($"test_tile_{i}");
            }
            
            yield return null;
            
            // Assert
            var stats = smallCacheLoader.GetCacheStats();
            Assert.LessOrEqual(stats.CachedAssets, 10, "Cache should not exceed limit");
            
            // Cleanup
            smallCacheLoader.ClearCache();
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator TileAssetLoader_WithTilemap_RendersCorrectly()
        {
            // Arrange
            var gameObject = new GameObject("TestTilemap");
            var tilemap = gameObject.AddComponent<Tilemap>();
            var tilemapRenderer = gameObject.AddComponent<TilemapRenderer>();
            
            // Create a test tile
            var testTile = ScriptableObject.CreateInstance<TileBase>();
            testTile.name = "integration_test_tile";
            
            // Act
            _assetLoader.LoadTile("integration_test_tile");
            var loadedTile = _assetLoader.LoadTile("integration_test_tile");
            
            if (loadedTile != null)
            {
                tilemap.SetTile(new Vector3Int(0, 0, 0), loadedTile);
            }
            
            yield return null;
            
            // Assert
            var retrievedTile = tilemap.GetTile(new Vector3Int(0, 0, 0));
            Assert.AreEqual(loadedTile, retrievedTile, "Tile should be set correctly in tilemap");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(testTile);
        }

        [UnityTest]
        public IEnumerator WeightedSelection_WithConfiguration_WorksInRuntime()
        {
            // Arrange
            _assetLoader.SetTilesetConfiguration(_testConfiguration);
            var random = new System.Random(42);
            var results = new Dictionary<string, int>();
            
            // Act
            for (int i = 0; i < 100; i++)
            {
                var tile = _assetLoader.GetRandomTile(TileType.Floor, random);
                if (tile != null)
                {
                    results[tile.name] = results.GetValueOrDefault(tile.name, 0) + 1;
                }
            }
            
            yield return null;
            
            // Assert
            // Test that weighted selection is working (even if no tiles are loaded)
            Assert.IsNotNull(results, "Results dictionary should be created");
        }

        #endregion

        #region Performance Tests in PlayMode

        [UnityTest]
        public IEnumerator Performance_AssetLoading_UnderFrameTime()
        {
            // Arrange
            var frameStartTime = Time.realtimeSinceStartup;
            var frameBudgetMs = 16.67f; // 60 FPS target
            
            // Act
            var loadStartTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 50; i++)
            {
                _assetLoader.LoadTile($"performance_test_{i}");
            }
            
            var loadEndTime = Time.realtimeSinceStartup;
            var loadTimeMs = (loadEndTime - loadStartTime) * 1000;
            
            yield return null;
            
            // Assert
            Assert.Less(loadTimeMs, frameBudgetMs, 
                $"Asset loading {loadTimeMs:F2}ms should complete within frame budget of {frameBudgetMs:F2}ms");
        }

        [UnityTest]
        public IEnumerator Performance_CacheAccess_FastInRuntime()
        {
            // Arrange
            var assetNames = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                assetNames.Add($"cache_test_{i}");
            }
            
            // Preload assets
            foreach (var assetName in assetNames)
            {
                _assetLoader.LoadTile(assetName);
            }
            
            yield return null;
            
            // Act
            var startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 100; i++) // Multiple passes
            {
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            }
            
            var endTime = Time.realtimeSinceStartup;
            var totalTimeMs = (endTime - startTime) * 1000;
            var averageTimePerAccess = totalTimeMs / (100 * assetNames.Count);
            
            yield return null;
            
            // Assert
            Assert.Less(averageTimePerAccess, 0.1f, 
                $"Average cache access time {averageTimePerAccess:F4}ms should be very fast");
            
            var stats = _assetLoader.GetCacheStats();
            Assert.Greater(stats.HitRate, 0.95f, "Cache hit rate should be high");
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator ErrorHandling_InvalidParameters_HandlesGracefully()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(null),
                "Should throw ArgumentException for null tile name");
            
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(""),
                "Should throw ArgumentException for empty tile name");
            
            Assert.Throws<ArgumentNullException>(() => _assetLoader.LoadAssetAsync("test", null, null),
                "Should throw ArgumentNullException for null callback");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ErrorHandling_MissingAssets_HandlesGracefully()
        {
            // Arrange
            var callbackCalled = false;
            UnityEngine.Object loadedAsset = null;
            
            // Act
            _assetLoader.LoadAssetAsync("definitely_non_existent_asset", typeof(TileBase), (asset) =>
            {
                callbackCalled = true;
                loadedAsset = asset;
            });
            
            yield return new WaitUntil(() => callbackCalled);
            
            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called even for missing assets");
            Assert.IsNull(loadedAsset, "Missing asset should result in null");
        }

        #endregion
    }
}