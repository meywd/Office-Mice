using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Content population abstraction for furniture, spawn points, and resource placement.
    /// Populates generated maps with interactive elements based on biome configuration.
    /// </summary>
    public interface IContentPopulator
    {
        /// <summary>
        /// Populates a map with content based on the specified biome configuration.
        /// Places furniture, enemy spawn points, resources, and interactive elements.
        /// </summary>
        /// <param name="map">Map data to populate with content</param>
        /// <param name="biome">Biome configuration containing content rules</param>
        /// <exception cref="ArgumentNullException">Thrown when map or biome is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when content placement fails validation</exception>
        void PopulateContent(MapData map, BiomeConfiguration biome);

        /// <summary>
        /// Populates content with a specific seed for deterministic placement.
        /// </summary>
        /// <param name="map">Map data to populate with content</param>
        /// <param name="biome">Biome configuration containing content rules</param>
        /// <param name="seed">Seed for deterministic content placement</param>
        /// <exception cref="ArgumentNullException">Thrown when map or biome is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when content placement fails validation</exception>
        void PopulateContent(MapData map, BiomeConfiguration biome, int seed);

        /// <summary>
        /// Places furniture in rooms based on room classification and biome rules.
        /// </summary>
        /// <param name="map">Map data to modify</param>
        /// <param name="biome">Biome configuration for furniture selection</param>
        /// <returns>List of placed furniture objects</returns>
        List<FurnitureData> PlaceFurniture(MapData map, BiomeConfiguration biome);

        /// <summary>
        /// Places enemy spawn points according to difficulty and room types.
        /// </summary>
        /// <param name="map">Map data to modify</param>
        /// <param name="biome">Biome configuration for enemy types</param>
        /// <returns>List of placed spawn points</returns>
        List<SpawnPointData> PlaceEnemySpawns(MapData map, BiomeConfiguration biome);

        /// <summary>
        /// Places resources and collectibles throughout the map.
        /// </summary>
        /// <param name="map">Map data to modify</param>
        /// <param name="biome">Biome configuration for resource types</param>
        /// <returns>List of placed resources</returns>
        List<ResourceData> PlaceResources(MapData map, BiomeConfiguration biome);

        /// <summary>
        /// Validates that all placed content is valid and doesn't conflict.
        /// </summary>
        /// <param name="map">Map with placed content to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        ValidationResult ValidateContentPlacement(MapData map);

        /// <summary>
        /// Optimizes content placement for better gameplay flow and balance.
        /// </summary>
        /// <param name="map">Map with content to optimize</param>
        /// <param name="biome">Biome configuration for optimization rules</param>
        /// <returns>Optimized content placement</returns>
        MapData OptimizeContentPlacement(MapData map, BiomeConfiguration biome);

        /// <summary>
        /// Calculates content density statistics for the map.
        /// </summary>
        /// <param name="map">Map to analyze</param>
        /// <returns>Content density statistics</returns>
        ContentDensityStats CalculateContentDensity(MapData map);

        /// <summary>
        /// Finds valid positions for placing objects within a room.
        /// </summary>
        /// <param name="room">Room to search for positions</param>
        /// <param name="objectSize">Size of the object to place</param>
        /// <param name="existingObjects">Already placed objects to avoid</param>
        /// <param name="minDistance">Minimum distance from walls and other objects</param>
        /// <returns>List of valid positions</returns>
        List<Vector2Int> FindValidPositions(RoomData room, Vector2Int objectSize, List<PlacedObjectData> existingObjects, int minDistance = 1);

        /// <summary>
        /// Event fired when furniture is placed.
        /// </summary>
        event Action<FurnitureData> OnFurniturePlaced;

        /// <summary>
        /// Event fired when an enemy spawn point is placed.
        /// </summary>
        event Action<SpawnPointData> OnSpawnPointPlaced;

        /// <summary>
        /// Event fired when a resource is placed.
        /// </summary>
        event Action<ResourceData> OnResourcePlaced;

        /// <summary>
        /// Event fired when content population fails.
        /// </summary>
        event Action<MapData, Exception> OnContentPopulationFailed;
    }

    /// <summary>
    /// Statistics about content density in a map.
    /// </summary>
    public struct ContentDensityStats
    {
        public float FurnitureDensity;
        public float EnemyDensity;
        public float ResourceDensity;
        public int TotalObjects;
        public float AverageObjectsPerRoom;
    }
}