using System;
using System.Collections;
using UnityEngine;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Main generation pipeline abstraction coordinating all phases of map generation.
    /// Provides coroutine-based async operations to maintain frame-time budgeting.
    /// </summary>
    public interface IMapGenerator
    {
        /// <summary>
        /// Generates a complete map asynchronously using coroutines for frame-time budgeting.
        /// </summary>
        /// <param name="settings">Configuration settings for map generation</param>
        /// <param name="seed">Optional seed for deterministic generation (default: 0)</param>
        /// <returns>Coroutine that yields the generated MapData</returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when generation fails validation</exception>
        IEnumerator<MapData> GenerateMapAsync(MapGenerationSettings settings, int seed = 0);

        /// <summary>
        /// Synchronous version of map generation for testing and simple use cases.
        /// </summary>
        /// <param name="settings">Configuration settings for map generation</param>
        /// <param name="seed">Optional seed for deterministic generation (default: 0)</param>
        /// <returns>Generated MapData</returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when generation fails validation</exception>
        MapData GenerateMap(MapGenerationSettings settings, int seed = 0);

        /// <summary>
        /// Validates generation settings before starting the generation process.
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        /// <returns>Validation result indicating success or failure with detailed errors</returns>
        ValidationResult ValidateSettings(MapGenerationSettings settings);

        /// <summary>
        /// Estimates the time required to generate a map with the given settings.
        /// Useful for progress tracking and performance optimization.
        /// </summary>
        /// <param name="settings">Generation settings to estimate</param>
        /// <returns>Estimated generation time in milliseconds</returns>
        float EstimateGenerationTime(MapGenerationSettings settings);

        /// <summary>
        /// Event fired when generation progress updates.
        /// Provides progress percentage and current phase information.
        /// </summary>
        event Action<float, string> OnProgressUpdated;

        /// <summary>
        /// Event fired when map generation completes successfully.
        /// </summary>
        event Action<MapData> OnGenerationCompleted;

        /// <summary>
        /// Event fired when map generation fails.
        /// </summary>
        event Action<Exception> OnGenerationFailed;
    }
}