using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.AssetLoading
{
    /// <summary>
    /// High-performance asset loader with caching, categorization, and weighted selection.
    /// Implements IAssetLoader interface for efficient tile, prefab, and ScriptableObject loading.
    /// </summary>
    public class TileAssetLoader : IAssetLoader
    {
        #region Private Fields
        
        // Asset caches by type
        private readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> _assetCache;
        
        // Tile categorization by type
        private readonly Dictionary<TileType, List<TileBase>> _tilesByType;
        private readonly Dictionary<TileType, List<TileEntry>> _tileEntriesByType;
        
        // Weighted selection caches
        private readonly Dictionary<TileType, float> _totalWeights;
        private readonly Dictionary<TileType, List<float>> _cumulativeWeights;
        
        // Performance tracking
        private readonly CacheStats _cacheStats;
        private int _loadCount;
        private int _hitCount;
        private long _memoryUsage;
        
        // Configuration
        private TilesetConfiguration _tilesetConfig;
        private readonly string[] _tileSearchPaths;
        private readonly int _maxCacheSize;
        private readonly bool _enableMemoryMonitoring;
        
        // Memory monitoring
        private readonly System.Diagnostics.Stopwatch _memoryMonitorStopwatch;
        private long _lastMemoryCheck;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Event fired when an asset is loaded.
        /// </summary>
        public event Action<string, Type> OnAssetLoaded;
        
        /// <summary>
        /// Event fired when asset loading fails.
        /// </summary>
        public event Action<string, Type, Exception> OnAssetLoadFailed;
        
        /// <summary>
        /// Event fired when cache is cleared.
        /// </summary>
        public event Action OnCacheCleared;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new TileAssetLoader with default configuration.
        /// </summary>
        public TileAssetLoader() : this(
            tileSearchPaths: new[] { "Assets/Game/Layout/Palette_Assets" },
            maxCacheSize: 1000,
            enableMemoryMonitoring: true)
        {
        }
        
        /// <summary>
        /// Creates a new TileAssetLoader with custom configuration.
        /// </summary>
        /// <param name="tileSearchPaths">Paths to search for tile assets</param>
        /// <param name="maxCacheSize">Maximum number of assets to cache</param>
        /// <param name="enableMemoryMonitoring">Whether to enable memory monitoring</param>
        public TileAssetLoader(string[] tileSearchPaths, int maxCacheSize = 1000, bool enableMemoryMonitoring = true)
        {
            _assetCache = new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
            _tilesByType = new Dictionary<TileType, List<TileBase>>();
            _tileEntriesByType = new Dictionary<TileType, List<TileEntry>>();
            _totalWeights = new Dictionary<TileType, float>();
            _cumulativeWeights = new Dictionary<TileType, List<float>>();
            
            _tileSearchPaths = tileSearchPaths ?? new[] { "Assets" };
            _maxCacheSize = maxCacheSize;
            _enableMemoryMonitoring = enableMemoryMonitoring;
            
            _cacheStats = new CacheStats();
            _memoryMonitorStopwatch = new System.Diagnostics.Stopwatch();
            
            InitializeTileCategories();
            
            if (_enableMemoryMonitoring)
            {
                _memoryMonitorStopwatch.Start();
                _lastMemoryCheck = GC.GetTotalMemory(false);
            }
        }
        
        #endregion

        #region IAssetLoader Implementation
        
        /// <summary>
        /// Loads a tile by name with caching.
        /// </summary>
        /// <param name="tileName">Name of the tile to load</param>
        /// <returns>Loaded TileBase, or null if not found</returns>
        public TileBase LoadTile(string tileName)
        {
            if (string.IsNullOrEmpty(tileName))
                throw new ArgumentException("Tile name cannot be null or empty", nameof(tileName));
            
            _loadCount++;
            
            // Check cache first
            if (IsAssetCached(tileName, typeof(TileBase)))
            {
                _hitCount++;
                UpdateCacheStats();
                return (TileBase)_assetCache[typeof(TileBase)][tileName];
            }
            
            try
            {
                var tile = LoadTileInternal(tileName);
                if (tile != null)
                {
                    CacheAsset(tileName, tile);
                    CategorizeTile(tile);
                    OnAssetLoaded?.Invoke(tileName, typeof(TileBase));
                }
                else
                {
                    OnAssetLoadFailed?.Invoke(tileName, typeof(TileBase), 
                        new KeyNotFoundException($"Tile '{tileName}' not found"));
                }
                
                UpdateCacheStats();
                MonitorMemoryUsage();
                
                return tile;
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(tileName, typeof(TileBase), ex);
                throw;
            }
        }
        
        /// <summary>
        /// Loads a prefab by name with caching.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to load</param>
        /// <returns>Loaded GameObject prefab, or null if not found</returns>
        public GameObject LoadPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                throw new ArgumentException("Prefab name cannot be null or empty", nameof(prefabName));
            
            _loadCount++;
            
            // Check cache first
            if (IsAssetCached(prefabName, typeof(GameObject)))
            {
                _hitCount++;
                UpdateCacheStats();
                return (GameObject)_assetCache[typeof(GameObject)][prefabName];
            }
            
            try
            {
                var prefab = LoadPrefabInternal(prefabName);
                if (prefab != null)
                {
                    CacheAsset(prefabName, prefab);
                    OnAssetLoaded?.Invoke(prefabName, typeof(GameObject));
                }
                else
                {
                    OnAssetLoadFailed?.Invoke(prefabName, typeof(GameObject), 
                        new KeyNotFoundException($"Prefab '{prefabName}' not found"));
                }
                
                UpdateCacheStats();
                MonitorMemoryUsage();
                
                return prefab;
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(prefabName, typeof(GameObject), ex);
                throw;
            }
        }
        
        /// <summary>
        /// Loads a ScriptableObject by type and name with caching.
        /// </summary>
        /// <typeparam name="T">Type of ScriptableObject to load</typeparam>
        /// <param name="assetName">Name of the asset to load</param>
        /// <returns>Loaded ScriptableObject, or null if not found</returns>
        public T LoadScriptableObject<T>(string assetName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException("Asset name cannot be null or empty", nameof(assetName));
            
            _loadCount++;
            var assetType = typeof(T);
            
            // Check cache first
            if (IsAssetCached(assetName, assetType))
            {
                _hitCount++;
                UpdateCacheStats();
                return (T)_assetCache[assetType][assetName];
            }
            
            try
            {
                var asset = LoadScriptableObjectInternal<T>(assetName);
                if (asset != null)
                {
                    CacheAsset(assetName, asset);
                    OnAssetLoaded?.Invoke(assetName, assetType);
                }
                else
                {
                    OnAssetLoadFailed?.Invoke(assetName, assetType, 
                        new KeyNotFoundException($"ScriptableObject '{assetName}' not found"));
                }
                
                UpdateCacheStats();
                MonitorMemoryUsage();
                
                return asset;
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(assetName, assetType, ex);
                throw;
            }
        }
        
        /// <summary>
        /// Preloads a set of assets into cache for faster access.
        /// </summary>
        /// <param name="assetNames">List of asset names to preload</param>
        /// <param name="assetType">Type of assets to preload</param>
        public void PreloadAssets(List<string> assetNames, Type assetType)
        {
            if (assetNames == null)
                throw new ArgumentNullException(nameof(assetNames));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
            
            Debug.Log($"Preloading {assetNames.Count} assets of type {assetType.Name}");
            
            foreach (var assetName in assetNames)
            {
                try
                {
                    if (assetType == typeof(TileBase))
                    {
                        LoadTile(assetName);
                    }
                    else if (assetType == typeof(GameObject))
                    {
                        LoadPrefab(assetName);
                    }
                    else if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                    {
                        // Use reflection to call the generic method
                        var method = typeof(TileAssetLoader).GetMethod(nameof(LoadScriptableObject), 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var genericMethod = method?.MakeGenericMethod(assetType);
                        genericMethod?.Invoke(this, new object[] { assetName });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to preload asset '{assetName}': {ex.Message}");
                }
            }
            
            Debug.Log($"Preloading completed. Cache stats: {GetCacheStats()}");
        }
        
        /// <summary>
        /// Clears the asset cache.
        /// </summary>
        public void ClearCache()
        {
            var totalAssets = _assetCache.Values.Sum(cache => cache.Count);
            _assetCache.Clear();
            
            // Clear tile categorization
            _tilesByType.Clear();
            _tileEntriesByType.Clear();
            _totalWeights.Clear();
            _cumulativeWeights.Clear();
            
            // Reset stats
            _loadCount = 0;
            _hitCount = 0;
            _memoryUsage = 0;
            
            OnCacheCleared?.Invoke();
            Debug.Log($"Cache cleared. Removed {totalAssets} assets.");
        }
        
        /// <summary>
        /// Gets cache statistics for performance monitoring.
        /// </summary>
        /// <returns>Cache statistics including hit rate and memory usage</returns>
        public CacheStats GetCacheStats()
        {
            var totalAssets = _assetCache.Values.Sum(cache => cache.Count);
            var hitRate = _loadCount > 0 ? (float)_hitCount / _loadCount : 0f;
            
            return new CacheStats
            {
                TotalAssets = totalAssets,
                CachedAssets = totalAssets,
                HitRate = hitRate,
                MemoryUsage = _memoryUsage,
                LoadCount = _loadCount,
                MissCount = _loadCount - _hitCount
            };
        }
        
        /// <summary>
        /// Checks if an asset is currently cached.
        /// </summary>
        /// <param name="assetName">Name of the asset to check</param>
        /// <param name="assetType">Type of the asset</param>
        /// <returns>True if asset is cached, false otherwise</returns>
        public bool IsAssetCached(string assetName, Type assetType)
        {
            if (string.IsNullOrEmpty(assetName) || assetType == null)
                return false;
            
            return _assetCache.ContainsKey(assetType) && _assetCache[assetType].ContainsKey(assetName);
        }
        
        /// <summary>
        /// Loads all assets of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <returns>Array of loaded assets</returns>
        public T[] LoadAllAssets<T>() where T : UnityEngine.Object
        {
            var assetType = typeof(T);
            var assets = new List<T>();
            
            if (assetType == typeof(TileBase))
            {
                // Load all tiles from search paths
                foreach (var path in _tileSearchPaths)
                {
                    var tileAssets = Resources.LoadAll<TileBase>(path.Replace("Assets/", ""));
                    assets.AddRange(tileAssets.Cast<T>());
                }
            }
            else if (assetType == typeof(GameObject))
            {
                // Load all prefabs from search paths
                foreach (var path in _tileSearchPaths)
                {
                    var prefabAssets = Resources.LoadAll<GameObject>(path.Replace("Assets/", ""));
                    assets.AddRange(prefabAssets.Cast<T>());
                }
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(assetType))
            {
                // Load all ScriptableObjects from search paths
                foreach (var path in _tileSearchPaths)
                {
                    var scriptableAssets = Resources.LoadAll<ScriptableObject>(path.Replace("Assets/", ""));
                    assets.AddRange(scriptableAssets.Where(obj => obj is T).Cast<T>());
                }
            }
            
            // Cache all loaded assets
            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    var assetName = asset.name;
                    CacheAsset(assetName, asset);
                    
                    if (asset is TileBase tile)
                    {
                        CategorizeTile(tile);
                    }
                }
            }
            
            return assets.ToArray();
        }
        
        /// <summary>
        /// Loads assets asynchronously for better performance.
        /// </summary>
        /// <param name="assetName">Name of the asset to load</param>
        /// <param name="assetType">Type of the asset</param>
        /// <param name="callback">Callback called when loading completes</param>
        public void LoadAssetAsync(string assetName, Type assetType, Action<UnityEngine.Object> callback)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException("Asset name cannot be null or empty", nameof(assetName));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            // For now, implement async loading using a simple coroutine-like approach
            // In a real implementation, you might use Unity's ResourceRequest API
            System.Threading.Tasks.Task.Run(() =>
            {
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
                        // Use reflection for generic method call
                        var method = typeof(TileAssetLoader).GetMethod(nameof(LoadScriptableObject), 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var genericMethod = method?.MakeGenericMethod(assetType);
                        asset = genericMethod?.Invoke(this, new object[] { assetName }) as UnityEngine.Object;
                    }
                    
                    // Execute callback on main thread
                    UnityMainThreadDispatcher.Instance.Enqueue(() => callback(asset));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Async asset loading failed for '{assetName}': {ex.Message}");
                    UnityMainThreadDispatcher.Instance.Enqueue(() => callback(null));
                }
            });
        }
        
        /// <summary>
        /// Validates that required assets are available.
        /// </summary>
        /// <param name="requiredAssets">List of required asset names</param>
        /// <param name="assetType">Type of required assets</param>
        /// <returns>Validation result indicating missing assets</returns>
        public ValidationResult ValidateRequiredAssets(List<string> requiredAssets, Type assetType)
        {
            var result = new ValidationResult();
            
            if (requiredAssets == null)
            {
                result.AddError("Required assets list cannot be null");
                return result;
            }
            
            if (assetType == null)
            {
                result.AddError("Asset type cannot be null");
                return result;
            }
            
            var missingAssets = new List<string>();
            
            foreach (var assetName in requiredAssets)
            {
                if (string.IsNullOrEmpty(assetName))
                {
                    missingAssets.Add("(null or empty)");
                    continue;
                }
                
                bool exists = false;
                
                try
                {
                    if (assetType == typeof(TileBase))
                    {
                        exists = LoadTile(assetName) != null;
                    }
                    else if (assetType == typeof(GameObject))
                    {
                        exists = LoadPrefab(assetName) != null;
                    }
                    else if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                    {
                        // Use reflection to check existence
                        var method = typeof(TileAssetLoader).GetMethod(nameof(LoadScriptableObject), 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var genericMethod = method?.MakeGenericMethod(assetType);
                        var asset = genericMethod?.Invoke(this, new object[] { assetName });
                        exists = asset != null;
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Failed to validate asset '{assetName}': {ex.Message}");
                }
                
                if (!exists)
                {
                    missingAssets.Add(assetName);
                }
            }
            
            if (missingAssets.Count > 0)
            {
                result.AddError($"Missing {assetType.Name} assets: {string.Join(", ", missingAssets)}");
            }
            else
            {
                Debug.Log($"All {requiredAssets.Count} required {assetType.Name} assets are available");
            }
            
            return result;
        }
        
        #endregion

        #region Tile Categorization and Weighted Selection
        
        /// <summary>
        /// Gets a random tile of the specified type using weighted selection.
        /// </summary>
        /// <param name="tileType">Type of tile to get</param>
        /// <param name="random">Random number generator (optional)</param>
        /// <returns>Randomly selected tile, or null if no tiles of that type exist</returns>
        public TileBase GetRandomTile(TileType tileType, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (!_tilesByType.ContainsKey(tileType) || _tilesByType[tileType].Count == 0)
                return null;
            
            // Use weighted selection if we have weights
            if (_totalWeights.ContainsKey(tileType) && _totalWeights[tileType] > 0)
            {
                return GetWeightedRandomTile(tileType, random);
            }
            
            // Fall back to uniform random selection
            var tiles = _tilesByType[tileType];
            return tiles[random.Next(tiles.Count)];
        }
        
        /// <summary>
        /// Gets all tiles of a specific type.
        /// </summary>
        /// <param name="tileType">Type of tiles to get</param>
        /// <returns>List of tiles of the specified type</returns>
        public IReadOnlyList<TileBase> GetTilesByType(TileType tileType)
        {
            if (!_tilesByType.ContainsKey(tileType))
                return new List<TileBase>().AsReadOnly();
            
            return _tilesByType[tileType].AsReadOnly();
        }
        
        /// <summary>
        /// Sets the tileset configuration for categorization and weighted selection.
        /// </summary>
        /// <param name="configuration">Tileset configuration to use</param>
        public void SetTilesetConfiguration(TilesetConfiguration configuration)
        {
            _tilesetConfig = configuration;
            
            if (configuration != null)
            {
                // Load and categorize tiles from configuration
                LoadTilesFromConfiguration();
            }
        }
        
        #endregion

        #region Private Methods
        
        private void InitializeTileCategories()
        {
            foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
            {
                _tilesByType[tileType] = new List<TileBase>();
                _tileEntriesByType[tileType] = new List<TileEntry>();
                _totalWeights[tileType] = 0f;
                _cumulativeWeights[tileType] = new List<float>();
            }
        }
        
        private TileBase LoadTileInternal(string tileName)
        {
            // Try to load from Resources first
            var tile = Resources.Load<TileBase>(tileName);
            if (tile != null)
                return tile;
            
            // Try to load from AssetDatabase (Editor only)
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:TileBase {tileName}");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            #endif
            
            return null;
        }
        
        private GameObject LoadPrefabInternal(string prefabName)
        {
            // Try to load from Resources first
            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab != null)
                return prefab;
            
            // Try to load from AssetDatabase (Editor only)
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab {prefabName}");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            #endif
            
            return null;
        }
        
        private T LoadScriptableObjectInternal<T>(string assetName) where T : ScriptableObject
        {
            // Try to load from Resources first
            var asset = Resources.Load<T>(assetName);
            if (asset != null)
                return asset;
            
            // Try to load from AssetDatabase (Editor only)
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name} {assetName}");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            }
            #endif
            
            return null;
        }
        
        private void CacheAsset(string assetName, UnityEngine.Object asset)
        {
            var assetType = asset.GetType();
            
            if (!_assetCache.ContainsKey(assetType))
            {
                _assetCache[assetType] = new Dictionary<string, UnityEngine.Object>();
            }
            
            // Check cache size limit
            if (_assetCache[assetType].Count >= _maxCacheSize)
            {
                // Remove oldest asset (simple LRU simulation)
                var oldestKey = _assetCache[assetType].Keys.First();
                _assetCache[assetType].Remove(oldestKey);
                Debug.LogWarning($"Cache limit reached, removed oldest asset: {oldestKey}");
            }
            
            _assetCache[assetType][assetName] = asset;
            
            // Estimate memory usage (rough approximation)
            _memoryUsage += EstimateAssetSize(asset);
        }
        
        private void CategorizeTile(TileBase tile)
        {
            if (tile == null)
                return;
            
            // Try to determine tile type from name or configuration
            var tileType = DetermineTileType(tile);
            
            if (!_tilesByType.ContainsKey(tileType))
            {
                _tilesByType[tileType] = new List<TileBase>();
            }
            
            _tilesByType[tileType].Add(tile);
            
            // Update weighted selection data if we have configuration
            UpdateWeightedSelection(tileType, tile);
        }
        
        private TileType DetermineTileType(TileBase tile)
        {
            if (_tilesetConfig != null)
            {
                // Use configuration to determine type
                var tileName = tile.name.ToLower();
                
                if (tileName.Contains("floor") || tileName.Contains("ground"))
                    return TileType.Floor;
                if (tileName.Contains("wall"))
                    return TileType.Wall;
                if (tileName.Contains("door"))
                    return TileType.Door;
                if (tileName.Contains("window"))
                    return TileType.Window;
                if (tileName.Contains("ceiling"))
                    return TileType.Ceiling;
                if (tileName.Contains("hazard") || tileName.Contains("spike") || tileName.Contains("trap"))
                    return TileType.Hazard;
                if (tileName.Contains("spawn") || tileName.Contains("start"))
                    return TileType.Spawn;
                if (tileName.Contains("objective") || tileName.Contains("goal") || tileName.Contains("exit"))
                    return TileType.Objective;
                if (tileName.Contains("decor") || tileName.Contains("prop") || tileName.Contains("furniture"))
                    return TileType.Decoration;
            }
            
            // Default to floor if we can't determine the type
            return TileType.Floor;
        }
        
        private void UpdateWeightedSelection(TileType tileType, TileBase tile)
        {
            if (_tilesetConfig == null)
                return;
            
            // Get tile mapping from configuration
            TileMapping mapping = null;
            switch (tileType)
            {
                case TileType.Floor:
                    mapping = _tilesetConfig.FloorTiles;
                    break;
                case TileType.Wall:
                    mapping = _tilesetConfig.WallTiles;
                    break;
                case TileType.Door:
                    mapping = _tilesetConfig.DoorTiles;
                    break;
                case TileType.Window:
                    mapping = _tilesetConfig.WindowTiles;
                    break;
                case TileType.Ceiling:
                    mapping = _tilesetConfig.CeilingTiles;
                    break;
                case TileType.Hazard:
                    mapping = _tilesetConfig.HazardTiles;
                    break;
                case TileType.Spawn:
                    mapping = _tilesetConfig.SpawnTiles;
                    break;
                case TileType.Objective:
                    mapping = _tilesetConfig.ObjectiveTiles;
                    break;
            }
            
            if (mapping?.Tiles != null)
            {
                // Update weighted selection data
                var tileEntry = mapping.Tiles.FirstOrDefault(t => t.Tile == tile);
                if (tileEntry != null)
                {
                    if (!_tileEntriesByType.ContainsKey(tileType))
                    {
                        _tileEntriesByType[tileType] = new List<TileEntry>();
                    }
                    
                    _tileEntriesByType[tileType].Add(tileEntry);
                    _totalWeights[tileType] += tileEntry.Weight;
                    
                    // Update cumulative weights for efficient weighted selection
                    UpdateCumulativeWeights(tileType);
                }
            }
        }
        
        private void UpdateCumulativeWeights(TileType tileType)
        {
            if (!_tileEntriesByType.ContainsKey(tileType))
                return;
            
            var entries = _tileEntriesByType[tileType];
            var cumulativeWeights = new List<float>();
            var currentWeight = 0f;
            
            foreach (var entry in entries)
            {
                currentWeight += entry.Weight;
                cumulativeWeights.Add(currentWeight);
            }
            
            _cumulativeWeights[tileType] = cumulativeWeights;
        }
        
        private TileBase GetWeightedRandomTile(TileType tileType, System.Random random)
        {
            if (!_cumulativeWeights.ContainsKey(tileType) || _cumulativeWeights[tileType].Count == 0)
                return null;
            
            var totalWeight = _totalWeights[tileType];
            if (totalWeight <= 0)
                return null;
            
            var randomValue = (float)(random.NextDouble() * totalWeight);
            var cumulativeWeights = _cumulativeWeights[tileType];
            var entries = _tileEntriesByType[tileType];
            
            for (int i = 0; i < cumulativeWeights.Count; i++)
            {
                if (randomValue <= cumulativeWeights[i])
                {
                    return entries[i].Tile;
                }
            }
            
            return entries[entries.Count - 1].Tile;
        }
        
        private void LoadTilesFromConfiguration()
        {
            if (_tilesetConfig == null)
                return;
            
            // Load all tiles from configuration mappings
            var mappings = new[]
            {
                _tilesetConfig.FloorTiles,
                _tilesetConfig.WallTiles,
                _tilesetConfig.DoorTiles,
                _tilesetConfig.WindowTiles,
                _tilesetConfig.CeilingTiles,
                _tilesetConfig.HazardTiles,
                _tilesetConfig.SpawnTiles,
                _tilesetConfig.ObjectiveTiles
            };
            
            foreach (var mapping in mappings)
            {
                if (mapping?.Tiles != null)
                {
                    foreach (var tileEntry in mapping.Tiles)
                    {
                        if (tileEntry.Tile != null)
                        {
                            var tileName = tileEntry.Tile.name;
                            CacheAsset(tileName, tileEntry.Tile);
                            CategorizeTile(tileEntry.Tile);
                        }
                    }
                }
            }
            
            // Load decorative tiles
            foreach (var decorativeMapping in _tilesetConfig.DecorativeTiles)
            {
                if (decorativeMapping?.Tiles != null)
                {
                    foreach (var tileEntry in decorativeMapping.Tiles)
                    {
                        if (tileEntry.Tile != null)
                        {
                            var tileName = tileEntry.Tile.name;
                            CacheAsset(tileName, tileEntry.Tile);
                            CategorizeTile(tileEntry.Tile);
                        }
                    }
                }
            }
        }
        
        private void UpdateCacheStats()
        {
            // Cache stats are calculated on-demand in GetCacheStats()
        }
        
        private void MonitorMemoryUsage()
        {
            if (!_enableMemoryMonitoring)
                return;
            
            // Check memory usage every 5 seconds
            if (_memoryMonitorStopwatch.ElapsedMilliseconds > 5000)
            {
                var currentMemory = GC.GetTotalMemory(false);
                var memoryDelta = currentMemory - _lastMemoryCheck;
                
                if (memoryDelta > 10 * 1024 * 1024) // 10MB increase
                {
                    Debug.LogWarning($"High memory usage detected: {memoryDelta / (1024 * 1024):F2}MB increase");
                    
                    // Consider cache cleanup if memory is too high
                    var stats = GetCacheStats();
                    if (stats.MemoryUsage > 50 * 1024 * 1024) // 50MB cache
                    {
                        Debug.LogWarning("Cache memory usage is high, consider clearing cache");
                    }
                }
                
                _lastMemoryCheck = currentMemory;
                _memoryMonitorStopwatch.Restart();
            }
        }
        
        private long EstimateAssetSize(UnityEngine.Object asset)
        {
            // Rough estimation of asset size in bytes
            if (asset is TileBase)
                return 1024; // 1KB per tile
            if (asset is GameObject)
                return 4096; // 4KB per prefab
            if (asset is ScriptableObject)
                return 2048; // 2KB per ScriptableObject
            
            return 512; // Default 512B
        }
        
        #endregion
    }
}