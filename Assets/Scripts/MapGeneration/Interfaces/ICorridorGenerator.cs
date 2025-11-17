using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Corridor generation abstraction using A* pathfinding algorithm.
    /// Connects rooms with optimal paths while avoiding obstacles and maintaining connectivity.
    /// </summary>
    public interface ICorridorGenerator
    {
        /// <summary>
        /// Connects all rooms using optimal corridor paths.
        /// Ensures 100% connectivity between all rooms in the map.
        /// </summary>
        /// <param name="rooms">List of rooms to connect</param>
        /// <param name="settings">Map generation settings containing corridor parameters</param>
        /// <returns>List of generated CorridorData objects</returns>
        /// <exception cref="ArgumentNullException">Thrown when rooms or settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when connectivity cannot be achieved</exception>
        List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings);

        /// <summary>
        /// Connects rooms with a specific seed for deterministic corridor generation.
        /// </summary>
        /// <param name="rooms">List of rooms to connect</param>
        /// <param name="settings">Map generation settings containing corridor parameters</param>
        /// <param name="seed">Seed for deterministic corridor generation</param>
        /// <returns>List of generated CorridorData objects</returns>
        /// <exception cref="ArgumentNullException">Thrown when rooms or settings is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when connectivity cannot be achieved</exception>
        List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings, int seed);

        /// <summary>
        /// Generates a single corridor between two specific rooms.
        /// </summary>
        /// <param name="room1">First room to connect</param>
        /// <param name="room2">Second room to connect</param>
        /// <param name="settings">Generation settings</param>
        /// <returns>CorridorData representing the connection, or null if connection fails</returns>
        CorridorData? ConnectRooms(RoomData room1, RoomData room2, MapGenerationSettings settings);

        /// <summary>
        /// Validates that all rooms are connected through the corridor network.
        /// </summary>
        /// <param name="rooms">List of rooms to check</param>
        /// <param name="corridors">List of corridors to validate</param>
        /// <returns>Validation result indicating connectivity status</returns>
        ValidationResult ValidateConnectivity(List<RoomData> rooms, List<CorridorData> corridors);

        /// <summary>
        /// Optimizes corridor paths to reduce length and improve layout efficiency.
        /// </summary>
        /// <param name="corridors">Initial corridor paths to optimize</param>
        /// <param name="rooms">Room layout for reference</param>
        /// <param name="settings">Settings containing optimization parameters</param>
        /// <returns>Optimized corridor paths</returns>
        List<CorridorData> OptimizeCorridors(List<CorridorData> corridors, List<RoomData> rooms, MapGenerationSettings settings);

        /// <summary>
        /// Detects and resolves corridor intersections and overlaps.
        /// </summary>
        /// <param name="corridors">List of corridors to check for intersections</param>
        /// <returns>Corridors with resolved intersections</returns>
        List<CorridorData> ResolveIntersections(List<CorridorData> corridors);

        /// <summary>
        /// Calculates the total length of all corridors.
        /// </summary>
        /// <param name="corridors">List of corridors</param>
        /// <returns>Total corridor length in units</returns>
        float CalculateTotalCorridorLength(List<CorridorData> corridors);

        /// <summary>
        /// Finds the shortest path between two rooms using existing corridors.
        /// </summary>
        /// <param name="startRoom">Starting room</param>
        /// <param name="endRoom">Destination room</param>
        /// <param name="corridors">Available corridor network</param>
        /// <returns>List of corridors forming the shortest path, or empty list if no path exists</returns>
        List<CorridorData> FindShortestPath(RoomData startRoom, RoomData endRoom, List<CorridorData> corridors);

        /// <summary>
        /// Event fired when a corridor is successfully generated.
        /// </summary>
        event Action<CorridorData> OnCorridorGenerated;

        /// <summary>
        /// Event fired when corridor generation fails.
        /// </summary>
        event Action<RoomData, RoomData, Exception> OnCorridorGenerationFailed;
    }
}