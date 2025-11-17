using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of IAssetLoader for testing purposes.
    /// Provides configurable asset loading behavior for unit testing.
    /// </summary>
    public class MockAssetLoader : IAssetLoader
    {
        private Dictionary<string, TileBase> _mockTiles;
        private Dictionary<string, GameObject> _mockPrefabs;
        private Dictionary<string, ScriptableObject> _mockScriptableObjects;
        private Dictionary<Type, Dictionary<string, UnityEngine.Object>> _cache;
        private CacheStats _mockCacheStats;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;

        public event Action<string, Type> OnAssetLoaded;
        public event Action<string, Type, Exception> OnAssetLoadFailed;
        public event Action OnCacheCleared;

        public MockAssetLoader()
        {
            _mockTiles = new Dictionary<string, TileBase>();
            _mockPrefabs = new Dictionary<string, GameObject>();
            _mockScriptableObjects = new Dictionary<string, ScriptableObject>();
            _cache = new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
            _mockCacheStats = new CacheStats
            {
                TotalAssets = 10,
                CachedAssets = 8,
                HitRate = 0.8f,
                MemoryUsage = 1024,
                LoadCount = 20,
                MissCount = 5
            };
            _shouldThrowException = false;
            _exceptionToThrow = new InvalidOperationException("Mock asset loading failed");

            InitializeMockAssets();
        }

        /// <summary>
        /// Adds a mock tile to the loader.
        /// </summary>
        public void AddMockTile(string name, TileBase tile)
        {
            _mockTiles[name] = tile;
        }

        /// <summary>
        /// Adds a mock prefab to the loader.
        /// </summary>
        public void AddMockPrefab(string name, GameObject prefab)
        {
            _mockPrefabs[name] = prefab;
        }

        /// <summary>
        /// Adds a mock ScriptableObject to the loader.
        /// </summary>
        public void AddMockScriptableObject(string name, ScriptableObject obj)
        {
            _mockScriptableObjects[name] = obj;
        }

        /// <summary>
        /// Sets the mock cache statistics.
        /// </summary>
        public void SetMockCacheStats(CacheStats stats)
        {
            _mockCacheStats = stats;
        }

        /// <summary>
        /// Configures the mock to throw an exception during asset loading.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock asset loading failed");
        }

        public TileBase LoadTile(string tileName)
        {
            if (string.IsNullOrEmpty(tileName))
                throw new ArgumentException("Tile name cannot be null or empty");

            if (_shouldThrowException)
            {
                OnAssetLoadFailed?.Invoke(tileName, typeof(TileBase), _exceptionToThrow);
                throw _exceptionToThrow;
            }

            if (_mockTiles.TryGetValue(tileName, out var tile))
            {
                CacheAsset(tileName, tile);
                OnAssetLoaded?.Invoke(tileName, typeof(TileBase));
                return tile;
            }

            OnAssetLoadFailed?.Invoke(tileName, typeof(TileBase), new KeyNotFoundException($"Tile '{tileName}' not found"));
            return null;
        }

        public GameObject LoadPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                throw new ArgumentException("Prefab name cannot be null or empty");

            if (_shouldThrowException)
            {
                OnAssetLoadFailed?.Invoke(prefabName, typeof(GameObject), _exceptionToThrow);
                throw _exceptionToThrow;
            }

            if (_mockPrefabs.TryGetValue(prefabName, out var prefab))
            {
                CacheAsset(prefabName, prefab);
                OnAssetLoaded?.Invoke(prefabName, typeof(GameObject));
                return prefab;
            }

            OnAssetLoadFailed?.Invoke(prefabName, typeof(GameObject), new KeyNotFoundException($"Prefab '{prefabName}' not found"));
            return null;
        }

        public T LoadScriptableObject<T>(string assetName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException("Asset name cannot be null or empty");

            if (_shouldThrowException)
            {
                OnAssetLoadFailed?.Invoke(assetName, typeof(T), _exceptionToThrow);
                throw _exceptionToThrow;
            }

            if (_mockScriptableObjects.TryGetValue(assetName, out var obj) && obj is T typedObj)
            {
                CacheAsset(assetName, obj);
                OnAssetLoaded?.Invoke(assetName, typeof(T));
                return typedObj;
            }

            OnAssetLoadFailed?.Invoke(assetName, typeof(T), new KeyNotFoundException($"ScriptableObject '{assetName}' not found"));
            return null;
        }

        public void PreloadAssets(List<string> assetNames, Type assetType)
        {
            if (assetNames == null)
                throw new ArgumentNullException(nameof(assetNames));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));

            // Mock preloading - just cache the assets if they exist
            foreach (var assetName in assetNames)
            {
                if (assetType == typeof(TileBase) && _mockTiles.ContainsKey(assetName))
                {
                    CacheAsset(assetName, _mockTiles[assetName]);
                }
                else if (assetType == typeof(GameObject) && _mockPrefabs.ContainsKey(assetName))
                {
                    CacheAsset(assetName, _mockPrefabs[assetName]);
                }
                else if (typeof(ScriptableObject).IsAssignableFrom(assetType) && _mockScriptableObjects.ContainsKey(assetName))
                {
                    CacheAsset(assetName, _mockScriptableObjects[assetName]);
                }
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            OnCacheCleared?.Invoke();
        }

        public CacheStats GetCacheStats()
        {
            return _mockCacheStats;
        }

        public bool IsAssetCached(string assetName, Type assetType)
        {
            if (string.IsNullOrEmpty(assetName))
                return false;
            if (assetType == null)
                return false;

            return _cache.ContainsKey(assetType) && _cache[assetType].ContainsKey(assetName);
        }

        public T[] LoadAllAssets<T>() where T : UnityEngine.Object
        {
            var assets = new List<T>();

            if (typeof(T) == typeof(TileBase))
            {
                foreach (var tile in _mockTiles.Values)
                {
                    if (tile is T typedTile)
                        assets.Add(typedTile);
                }
            }
            else if (typeof(T) == typeof(GameObject))
            {
                foreach (var prefab in _mockPrefabs.Values)
                {
                    if (prefab is T typedPrefab)
                        assets.Add(typedPrefab);
                }
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                foreach (var obj in _mockScriptableObjects.Values)
                {
                    if (obj is T typedObj)
                        assets.Add(typedObj);
                }
            }

            return assets.ToArray();
        }

        public void LoadAssetAsync(string assetName, Type assetType, Action<UnityEngine.Object> callback)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException("Asset name cannot be null or empty");
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            // Mock async loading - just call the callback immediately
            try
            {
                UnityEngine.Object asset = null;
                
                if (assetType == typeof(TileBase))
                {
                    asset = LoadTile(assetName);
                }
                else if (assetType == typeof(GameObject))
                {
                    asset = LoadPrefab(assetName);
                }
                else if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                {
                    asset = LoadScriptableObject(assetName);
                }

                callback(asset);
            }
            catch (Exception ex)
            {
                callback(null);
            }
        }

        public ValidationResult ValidateRequiredAssets(List<string> requiredAssets, Type assetType)
        {
            if (requiredAssets == null)
                return ValidationResult.Failure("Required assets list cannot be null");
            if (assetType == null)
                return ValidationResult.Failure("Asset type cannot be null");

            var missingAssets = new List<string>();
            
            foreach (var assetName in requiredAssets)
            {
                bool exists = false;
                
                if (assetType == typeof(TileBase))
                {
                    exists = _mockTiles.ContainsKey(assetName);
                }
                else if (assetType == typeof(GameObject))
                {
                    exists = _mockPrefabs.ContainsKey(assetName);
                }
                else if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                {
                    exists = _mockScriptableObjects.ContainsKey(assetName);
                }

                if (!exists)
                    missingAssets.Add(assetName);
            }

            if (missingAssets.Count > 0)
            {
                return ValidationResult.Failure($"Missing assets: {string.Join(", ", missingAssets)}");
            }

            return ValidationResult.Success();
        }

        private void InitializeMockAssets()
        {
            // Create some default mock assets
            _mockTiles["floor"] = ScriptableObject.CreateInstance<TileBase>();
            _mockTiles["wall"] = ScriptableObject.CreateInstance<TileBase>();
            _mockTiles["door"] = ScriptableObject.CreateInstance<TileBase>();

            var mockPrefab = new GameObject("MockPrefab");
            _mockPrefabs["enemy"] = mockPrefab;
            _mockPrefabs["furniture"] = mockPrefab;

            var mockScriptableObj = ScriptableObject.CreateInstance<ScriptableObject>();
            _mockScriptableObjects["config"] = mockScriptableObj;
        }

        private void CacheAsset(string assetName, UnityEngine.Object asset)
        {
            var assetType = asset.GetType();
            
            if (!_cache.ContainsKey(assetType))
            {
                _cache[assetType] = new Dictionary<string, UnityEngine.Object>();
            }
            
            _cache[assetType][assetName] = asset;
        }
    }
}