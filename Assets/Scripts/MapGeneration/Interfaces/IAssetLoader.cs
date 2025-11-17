using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Asset loading and caching abstraction for map generation resources.
    /// Provides efficient loading and caching of tiles, prefabs, and other assets.
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// Loads a tile by name from the configured tileset.
        /// </summary>
        /// <param name="tileName">Name of the tile to load</param>
        /// <returns>Loaded TileBase, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when tileName is null or empty</exception>
        TileBase LoadTile(string tileName);

        /// <summary>
        /// Loads a prefab by name.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to load</param>
        /// <returns>Loaded GameObject prefab, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when prefabName is null or empty</exception>
        GameObject LoadPrefab(string prefabName);

        /// <summary>
        /// Loads a scriptable object by type and name.
        /// </summary>
        /// <typeparam name="T">Type of ScriptableObject to load</typeparam>
        /// <param name="assetName">Name of the asset to load</param>
        /// <returns>Loaded ScriptableObject, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when assetName is null or empty</exception>
        T LoadScriptableObject<T>(string assetName) where T : ScriptableObject;

        /// <summary>
        /// Preloads a set of assets into cache for faster access.
        /// </summary>
        /// <param name="assetNames">List of asset names to preload</param>
        /// <param name="assetType">Type of assets to preload</param>
        void PreloadAssets(List<string> assetNames, Type assetType);

        /// <summary>
        /// Clears the asset cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets cache statistics for performance monitoring.
        /// </summary>
        /// <returns>Cache statistics including hit rate and memory usage</returns>
        CacheStats GetCacheStats();

        /// <summary>
        /// Checks if an asset is currently cached.
        /// </summary>
        /// <param name="assetName">Name of the asset to check</param>
        /// <param name="assetType">Type of the asset</param>
        /// <returns>True if asset is cached, false otherwise</returns>
        bool IsAssetCached(string assetName, Type assetType);

        /// <summary>
        /// Loads all assets of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <returns>Array of loaded assets</returns>
        T[] LoadAllAssets<T>() where T : UnityEngine.Object;

        /// <summary>
        /// Loads assets asynchronously for better performance.
        /// </summary>
        /// <param name="assetName">Name of the asset to load</param>
        /// <param name="assetType">Type of the asset</param>
        /// <param name="callback">Callback called when loading completes</param>
        void LoadAssetAsync(string assetName, Type assetType, Action<UnityEngine.Object> callback);

        /// <summary>
        /// Validates that required assets are available.
        /// </summary>
        /// <param name="requiredAssets">List of required asset names</param>
        /// <param name="assetType">Type of required assets</param>
        /// <returns>Validation result indicating missing assets</returns>
        ValidationResult ValidateRequiredAssets(List<string> requiredAssets, Type assetType);

        /// <summary>
        /// Event fired when an asset is loaded.
        /// </summary>
        event Action<string, Type> OnAssetLoaded;

        /// <summary>
        /// Event fired when asset loading fails.
        /// </summary>
        event Action<string, Type, Exception> OnAssetLoadFailed;

        /// <summary>
        /// Event fired when cache is cleared.
        /// </summary>
        event Action OnCacheCleared;
    }

    /// <summary>
    /// Statistics for asset cache performance monitoring.
    /// </summary>
    public struct CacheStats
    {
        public int TotalAssets;
        public int CachedAssets;
        public float HitRate;
        public long MemoryUsage;
        public int LoadCount;
        public int MissCount;
    }
}