using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// A* pathfinding abstraction for corridor generation and navigation.
    /// Provides optimal path finding while avoiding obstacles and supporting various heuristics.
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// Finds the shortest path between two positions using A* algorithm.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Destination position</param>
        /// <param name="obstacles">2D array indicating obstacle positions (true = obstacle)</param>
        /// <returns>List of positions forming the shortest path, or empty list if no path exists</returns>
        /// <exception cref="ArgumentException">Thrown when start or end is out of bounds</exception>
        /// <exception cref="ArgumentNullException">Thrown when obstacles is null</exception>
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles);

        /// <summary>
        /// Finds path with custom movement costs for different terrain types.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Destination position</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <param name="movementCosts">2D array of movement costs for each position</param>
        /// <returns>List of positions forming the optimal path</returns>
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles, float[,] movementCosts);

        /// <summary>
        /// Finds multiple paths from start to multiple end positions.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="ends">Multiple destination positions</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <returns>Dictionary mapping end positions to their paths</returns>
        Dictionary<Vector2Int, List<Vector2Int>> FindMultiplePaths(Vector2Int start, List<Vector2Int> ends, bool[,] obstacles);

        /// <summary>
        /// Checks if a path exists between two positions.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Destination position</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <returns>True if path exists, false otherwise</returns>
        bool PathExists(Vector2Int start, Vector2Int end, bool[,] obstacles);

        /// <summary>
        /// Gets all reachable positions from a starting point.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <param name="maxDistance">Maximum distance to search (optional)</param>
        /// <returns>Set of reachable positions</returns>
        HashSet<Vector2Int> GetReachablePositions(Vector2Int start, bool[,] obstacles, int maxDistance = -1);

        /// <summary>
        /// Optimizes an existing path to remove unnecessary waypoints.
        /// </summary>
        /// <param name="path">Original path to optimize</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <returns>Optimized path with fewer waypoints</returns>
        List<Vector2Int> OptimizePath(List<Vector2Int> path, bool[,] obstacles);

        /// <summary>
        /// Calculates the total cost of a path.
        /// </summary>
        /// <param name="path">Path to calculate cost for</param>
        /// <param name="movementCosts">Movement costs for each position (optional)</param>
        /// <returns>Total path cost</returns>
        float CalculatePathCost(List<Vector2Int> path, float[,] movementCosts = null);

        /// <summary>
        /// Validates pathfinding parameters and obstacle map.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Destination position</param>
        /// <param name="obstacles">2D array indicating obstacle positions</param>
        /// <returns>Validation result with detailed error information</returns>
        ValidationResult ValidatePathfindingParameters(Vector2Int start, Vector2Int end, bool[,] obstacles);

        /// <summary>
        /// Sets the heuristic function for pathfinding.
        /// </summary>
        /// <param name="heuristic">Heuristic function to use</param>
        void SetHeuristic(Func<Vector2Int, Vector2Int, float> heuristic);

        /// <summary>
        /// Gets performance statistics for pathfinding operations.
        /// </summary>
        /// <returns>Pathfinding performance statistics</returns>
        PathfindingStats GetPerformanceStats();

        /// <summary>
        /// Resets performance statistics.
        /// </summary>
        void ResetStats();

        /// <summary>
        /// Event fired when pathfinding starts.
        /// </summary>
        event Action<Vector2Int, Vector2Int> OnPathfindingStarted;

        /// <summary>
        /// Event fired when pathfinding completes.
        /// </summary>
        event Action<Vector2Int, Vector2Int, List<Vector2Int>> OnPathfindingCompleted;

        /// <summary>
        /// Event fired when pathfinding fails.
        /// </summary>
        event Action<Vector2Int, Vector2Int, Exception> OnPathfindingFailed;
    }

    /// <summary>
    /// Performance statistics for pathfinding operations.
    /// </summary>
    public struct PathfindingStats
    {
        public int TotalPathfindingCalls;
        public int SuccessfulPaths;
        public int FailedPaths;
        public float AveragePathLength;
        public float AverageComputationTime;
        public long TotalNodesExplored;
        public float AverageNodesExplored;
    }
}