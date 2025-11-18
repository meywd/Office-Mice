using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of IPathfinder for testing purposes.
    /// Provides configurable pathfinding behavior for unit testing.
    /// </summary>
    public class MockPathfinder : IPathfinder
    {
        private List<Vector2Int> _mockPath;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;
        private PathfindingStats _mockStats;
        private Func<Vector2Int, Vector2Int, float> _heuristic;

        public event Action<Vector2Int, Vector2Int> OnPathfindingStarted;
        public event Action<Vector2Int, Vector2Int, List<Vector2Int>> OnPathfindingCompleted;
        public event Action<Vector2Int, Vector2Int, Exception> OnPathfindingFailed;

        public MockPathfinder()
        {
            _mockPath = CreateDefaultMockPath();
            _shouldThrowException = false;
            _exceptionToThrow = new InvalidOperationException("Mock pathfinding failed");
            _mockValidationResult = ValidationResult.Success();
            _mockStats = new PathfindingStats
            {
                TotalPathfindingCalls = 10,
                SuccessfulPaths = 8,
                FailedPaths = 2,
                AveragePathLength = 15.5f,
                AverageComputationTime = 2.3f,
                TotalNodesExplored = 150,
                AverageNodesExplored = 15f
            };
            _heuristic = (start, end) => Vector2Int.Distance(start, end);
        }

        /// <summary>
        /// Sets the mock path to return from pathfinding methods.
        /// </summary>
        public void SetMockPath(List<Vector2Int> path)
        {
            _mockPath = path ?? new List<Vector2Int>();
        }

        /// <summary>
        /// Sets the mock pathfinding statistics.
        /// </summary>
        public void SetMockStats(PathfindingStats stats)
        {
            _mockStats = stats;
        }

        /// <summary>
        /// Configures the mock to throw an exception during pathfinding.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock pathfinding failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            return FindPath(start, end, obstacles, null);
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles, float[,] movementCosts)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            OnPathfindingStarted?.Invoke(start, end);

            if (_shouldThrowException)
            {
                OnPathfindingFailed?.Invoke(start, end, _exceptionToThrow);
                throw _exceptionToThrow;
            }

            // Validate bounds
            if (start.x < 0 || start.x >= obstacles.GetLength(0) || start.y < 0 || start.y >= obstacles.GetLength(1))
                throw new ArgumentException("Start position is out of bounds");
            if (end.x < 0 || end.x >= obstacles.GetLength(0) || end.y < 0 || end.y >= obstacles.GetLength(1))
                throw new ArgumentException("End position is out of bounds");

            // Check if start or end is blocked
            if (obstacles[start.x, start.y] || obstacles[end.x, end.y])
            {
                OnPathfindingCompleted?.Invoke(start, end, new List<Vector2Int>());
                return new List<Vector2Int>();
            }

            OnPathfindingCompleted?.Invoke(start, end, new List<Vector2Int>(_mockPath));
            return new List<Vector2Int>(_mockPath);
        }

        public Dictionary<Vector2Int, List<Vector2Int>> FindMultiplePaths(Vector2Int start, List<Vector2Int> ends, bool[,] obstacles)
        {
            if (ends == null)
                throw new ArgumentNullException(nameof(ends));
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            var paths = new Dictionary<Vector2Int, List<Vector2Int>>();
            
            foreach (var end in ends)
            {
                paths[end] = FindPath(start, end, obstacles);
            }

            return paths;
        }

        public bool PathExists(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            var path = FindPath(start, end, obstacles);
            return path.Count > 0;
        }

        public HashSet<Vector2Int> GetReachablePositions(Vector2Int start, bool[,] obstacles, int maxDistance = -1)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            var reachable = new HashSet<Vector2Int>();
            
            // Mock implementation - return some positions around start
            for (int x = Math.Max(0, start.x - 2); x <= Math.Min(obstacles.GetLength(0) - 1, start.x + 2); x++)
            {
                for (int y = Math.Max(0, start.y - 2); y <= Math.Min(obstacles.GetLength(1) - 1, start.y + 2); y++)
                {
                    if (!obstacles[x, y])
                    {
                        var pos = new Vector2Int(x, y);
                        var distance = Vector2Int.Distance(start, pos);
                        if (maxDistance == -1 || distance <= maxDistance)
                        {
                            reachable.Add(pos);
                        }
                    }
                }
            }

            return reachable;
        }

        public List<Vector2Int> OptimizePath(List<Vector2Int> path, bool[,] obstacles)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));

            // Mock optimization - return every other point
            var optimized = new List<Vector2Int>();
            for (int i = 0; i < path.Count; i += 2)
            {
                optimized.Add(path[i]);
            }

            // Ensure the end point is included
            if (optimized.Count > 0 && optimized[optimized.Count - 1] != path[path.Count - 1])
            {
                optimized.Add(path[path.Count - 1]);
            }

            return optimized;
        }

        public float CalculatePathCost(List<Vector2Int> path, float[,] movementCosts = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            float cost = 0f;
            
            for (int i = 1; i < path.Count; i++)
            {
                var stepCost = 1f; // Default movement cost
                
                if (movementCosts != null)
                {
                    var pos = path[i];
                    if (pos.x >= 0 && pos.x < movementCosts.GetLength(0) && 
                        pos.y >= 0 && pos.y < movementCosts.GetLength(1))
                    {
                        stepCost = movementCosts[pos.x, pos.y];
                    }
                }
                
                cost += stepCost;
            }

            return cost;
        }

        public ValidationResult ValidatePathfindingParameters(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            if (obstacles == null)
                return ValidationResult.Failure("Obstacles array cannot be null");

            if (start.x < 0 || start.x >= obstacles.GetLength(0) || start.y < 0 || start.y >= obstacles.GetLength(1))
                return ValidationResult.Failure("Start position is out of bounds");

            if (end.x < 0 || end.x >= obstacles.GetLength(0) || end.y < 0 || end.y >= obstacles.GetLength(1))
                return ValidationResult.Failure("End position is out of bounds");

            if (obstacles[start.x, start.y])
                return ValidationResult.Failure("Start position is blocked");

            if (obstacles[end.x, end.y])
                return ValidationResult.Failure("End position is blocked");

            return _mockValidationResult;
        }

        public void SetHeuristic(Func<Vector2Int, Vector2Int, float> heuristic)
        {
            _heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
        }

        public PathfindingStats GetPerformanceStats()
        {
            return _mockStats;
        }

        public void ResetStats()
        {
            _mockStats = new PathfindingStats();
        }

        private List<Vector2Int> CreateDefaultMockPath()
        {
            return new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(4, 2),
                new Vector2Int(5, 2)
            };
        }
    }
}