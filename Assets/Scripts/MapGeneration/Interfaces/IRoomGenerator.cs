using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Room creation abstraction using Binary Space Partitioning (BSP) algorithm.
    /// Generates non-overlapping rooms with proper spatial distribution and classification.
    /// </summary>
    public interface IRoomGenerator
    {
        /// <summary>
        /// Generates a list of rooms using BSP algorithm based on the provided settings.
        /// </summary>
        /// <param name="settings">Map generation settings containing room parameters</param>
        /// <returns>List of generated RoomData objects</returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when room generation fails constraints</exception>
        List<RoomData> GenerateRooms(MapGenerationSettings settings);

        /// <summary>
        /// Generates rooms with a specific seed for deterministic output.
        /// </summary>
        /// <param name="settings">Map generation settings containing room parameters</param>
        /// <param name="seed">Seed for deterministic room generation</param>
        /// <returns>List of generated RoomData objects</returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when room generation fails constraints</exception>
        List<RoomData> GenerateRooms(MapGenerationSettings settings, int seed);

        /// <summary>
        /// Validates room placement and ensures no overlaps occur.
        /// </summary>
        /// <param name="rooms">List of rooms to validate</param>
        /// <param name="settings">Settings containing validation constraints</param>
        /// <returns>Validation result with detailed error information</returns>
        ValidationResult ValidateRoomPlacement(List<RoomData> rooms, MapGenerationSettings settings);

        /// <summary>
        /// Optimizes room layout for better connectivity and space utilization.
        /// </summary>
        /// <param name="rooms">Initial room layout to optimize</param>
        /// <param name="settings">Settings containing optimization parameters</param>
        /// <returns>Optimized room layout</returns>
        List<RoomData> OptimizeRoomLayout(List<RoomData> rooms, MapGenerationSettings settings);

        /// <summary>
        /// Assigns classifications to rooms based on their properties and position.
        /// </summary>
        /// <param name="rooms">Rooms to classify</param>
        /// <param name="settings">Settings containing classification rules</param>
        /// <returns>Rooms with assigned classifications</returns>
        List<RoomData> ClassifyRooms(List<RoomData> rooms, MapGenerationSettings settings);

        /// <summary>
        /// Calculates the total area occupied by rooms.
        /// </summary>
        /// <param name="rooms">List of rooms</param>
        /// <returns>Total area in square units</returns>
        float CalculateTotalRoomArea(List<RoomData> rooms);

        /// <summary>
        /// Finds the optimal room placement for a new room within existing layout.
        /// </summary>
        /// <param name="existingRooms">Current room layout</param>
        /// <param name="newRoomSize">Size of the room to place</param>
        /// <param name="settings">Generation settings</param>
        /// <returns>Position for the new room, or null if no valid position exists</returns>
        Vector2Int? FindOptimalRoomPosition(List<RoomData> existingRooms, Vector2Int newRoomSize, MapGenerationSettings settings);

        /// <summary>
        /// Event fired when a room is successfully generated.
        /// </summary>
        event Action<RoomData> OnRoomGenerated;

        /// <summary>
        /// Event fired when room generation fails for a specific room.
        /// </summary>
        event Action<RoomData, Exception> OnRoomGenerationFailed;
    }
}