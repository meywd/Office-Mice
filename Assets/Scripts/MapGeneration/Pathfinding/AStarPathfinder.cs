using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// A* pathfinding implementation for corridor generation.
    /// Provides optimal path finding with Manhattan distance heuristic and obstacle avoidance.
    /// </summary>
    public class AStarPathfinder : IPathfinder
    {
        #region Private Fields
        
        // Core algorithm components
        private PriorityQueue<AStarNode> _openSet;
        private HashSet<Vector2Int> _closedSet;
        private Dictionary<Vector2Int, AStarNode> _allNodes;
        private Func<Vector2Int, Vector2Int, float> _heuristic;
        
        // Performance tracking
        private PathfindingStats _stats;
        private System.Diagnostics.Stopwatch _stopwatch;
        
        // Object pooling for nodes
        private Stack<AStarNode> _nodePool;
        private const int MAX_POOL_SIZE = 1000;
        
        // Configuration
        private const float DEFAULT_MOVEMENT_COST = 1f;
        private const float DIAGONAL_MOVEMENT_COST = 1.414f; // sqrt(2)
        
        #endregion
        
        #region Events
        
        public event Action<Vector2Int, Vector2Int> OnPathfindingStarted;
        public event Action<Vector2Int, Vector2Int, List<Vector2Int>> OnPathfindingCompleted;
        public event Action<Vector2Int, Vector2Int, Exception> OnPathfindingFailed;
        
        #endregion
        
        #region Constructor
        
        public AStarPathfinder()
        {
            InitializeComponents();
            SetHeuristic(ManhattanDistance);
        }
        
        #endregion
        
        #region IPathfinder Implementation
        
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            return FindPath(start, end, obstacles, null);
        }
        
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles, float[,] movementCosts)
        {
            var startTime = System.DateTime.UtcNow;
            
            try
            {
                // Update statistics
                _stats.TotalPathfindingCalls++;
                OnPathfindingStarted?.Invoke(start, end);
                
                // Validate parameters
                var validation = ValidatePathfindingParameters(start, end, obstacles);
                if (!validation.IsValid)
                {
                    _stats.FailedPaths++;
                    OnPathfindingFailed?.Invoke(start, end, new ArgumentException(validation.Errors[0]));
                    return new List<Vector2Int>();
                }
                
                // Check if start equals end
                if (start == end)
                {
                    _stats.SuccessfulPaths++;
                    OnPathfindingCompleted?.Invoke(start, end, new List<Vector2Int> { start });
                    return new List<Vector2Int> { start };
                }
                
                // Perform A* pathfinding
                var path = FindPathInternal(start, end, obstacles, movementCosts);
                
                // Update statistics
                var elapsed = (float)(System.DateTime.UtcNow - startTime).TotalMilliseconds;
                UpdatePerformanceStats(path, elapsed);
                
                // Fire events
                if (path.Count > 0)
                {
                    _stats.SuccessfulPaths++;
                    OnPathfindingCompleted?.Invoke(start, end, path);
                }
                else
                {
                    _stats.FailedPaths++;
                    OnPathfindingFailed?.Invoke(start, end, new InvalidOperationException("No path found"));
                }
                
                return path;
            }
            catch (Exception ex)
            {
                _stats.FailedPaths++;
                var elapsed = (float)(System.DateTime.UtcNow - startTime).TotalMilliseconds;
                UpdatePerformanceStats(new List<Vector2Int>(), elapsed);
                OnPathfindingFailed?.Invoke(start, end, ex);
                throw;
            }
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
            
            var validation = ValidatePathfindingParameters(start, end, obstacles);
            if (!validation.IsValid)
                return false;
            
            // Quick path existence check using BFS for better performance
            return PathExistsBFS(start, end, obstacles);
        }
        
        public HashSet<Vector2Int> GetReachablePositions(Vector2Int start, bool[,] obstacles, int maxDistance = -1)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            var reachable = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                reachable.Add(current);
                
                // Check distance limit
                if (maxDistance > 0 && Vector2Int.Distance(start, current) > maxDistance)
                    continue;
                
                // Explore neighbors
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!IsValidPosition(neighbor, obstacles) || visited.Contains(neighbor))
                        continue;
                    
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
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
            
            if (path.Count <= 2)
                return new List<Vector2Int>(path);
            
            var optimized = new List<Vector2Int> { path[0] };
            
            int currentIndex = 0;
            while (currentIndex < path.Count - 1)
            {
                int furthestVisible = currentIndex + 1;
                
                // Find the furthest point we can reach directly
                for (int i = currentIndex + 2; i < path.Count; i++)
                {
                    if (HasLineOfSight(path[currentIndex], path[i], obstacles))
                    {
                        furthestVisible = i;
                    }
                    else
                    {
                        break;
                    }
                }
                
                optimized.Add(path[furthestVisible]);
                currentIndex = furthestVisible;
            }
            
            return optimized;
        }
        
        public float CalculatePathCost(List<Vector2Int> path, float[,] movementCosts = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            
            if (path.Count <= 1)
                return 0f;
            
            float totalCost = 0f;
            
            for (int i = 1; i < path.Count; i++)
            {
                var from = path[i - 1];
                var to = path[i];
                
                // Determine movement cost
                float stepCost = DEFAULT_MOVEMENT_COST;
                
                // Check for diagonal movement
                if (Mathf.Abs(from.x - to.x) == 1 && Mathf.Abs(from.y - to.y) == 1)
                    stepCost = DIAGONAL_MOVEMENT_COST;
                
                // Apply custom movement costs if provided
                if (movementCosts != null)
                {
                    var pos = to;
                    if (pos.x >= 0 && pos.x < movementCosts.GetLength(0) && 
                        pos.y >= 0 && pos.y < movementCosts.GetLength(1))
                    {
                        stepCost *= movementCosts[pos.x, pos.y];
                    }
                }
                
                totalCost += stepCost;
            }
            
            return totalCost;
        }
        
        public ValidationResult ValidatePathfindingParameters(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            var result = new ValidationResult();
            
            if (obstacles == null)
            {
                result.AddError("Obstacles array cannot be null");
                return result;
            }
            
            // Validate array dimensions
            if (obstacles.GetLength(0) == 0 || obstacles.GetLength(1) == 0)
            {
                result.AddError("Obstacles array must have positive dimensions");
                return result;
            }
            
            // Validate start position
            if (!IsValidPosition(start, obstacles))
            {
                result.AddError($"Start position {start} is out of bounds or invalid");
            }
            else if (obstacles[start.x, start.y])
            {
                result.AddError($"Start position {start} is blocked by an obstacle");
            }
            
            // Validate end position
            if (!IsValidPosition(end, obstacles))
            {
                result.AddError($"End position {end} is out of bounds or invalid");
            }
            else if (obstacles[end.x, end.y])
            {
                result.AddError($"End position {end} is blocked by an obstacle");
            }
            
            return result;
        }
        
        public void SetHeuristic(Func<Vector2Int, Vector2Int, float> heuristic)
        {
            _heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
        }
        
        public PathfindingStats GetPerformanceStats()
        {
            return _stats;
        }
        
        public void ResetStats()
        {
            _stats = new PathfindingStats();
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeComponents()
        {
            _openSet = new PriorityQueue<AStarNode>();
            _closedSet = new HashSet<Vector2Int>();
            _allNodes = new Dictionary<Vector2Int, AStarNode>();
            _nodePool = new Stack<AStarNode>();
            _stats = new PathfindingStats();
            _stopwatch = new System.Diagnostics.Stopwatch();
        }
        
        private List<Vector2Int> FindPathInternal(Vector2Int start, Vector2Int end, bool[,] obstacles, float[,] movementCosts)
        {
            ResetPathfindingState();
            
            // Initialize start node
            var startNode = GetOrCreateNode(start);
            startNode.GCost = 0;
            startNode.HCost = _heuristic(start, end);
            startNode.FCost = startNode.GCost + startNode.HCost;
            
            _openSet.Enqueue(startNode);
            _allNodes[start] = startNode;
            
            while (_openSet.Count > 0)
            {
                // Get node with lowest F cost
                var currentNode = _openSet.Dequeue();
                _stats.TotalNodesExplored++;
                
                // Check if we reached the target
                if (currentNode.Position == end)
                {
                    return ReconstructPath(currentNode);
                }
                
                _closedSet.Add(currentNode.Position);
                
                // Explore neighbors
                foreach (var neighborPos in GetNeighbors(currentNode.Position))
                {
                    if (!IsValidPosition(neighborPos, obstacles) || _closedSet.Contains(neighborPos))
                        continue;
                    
                    var neighborNode = GetOrCreateNode(neighborPos);
                    
                    // Calculate movement cost
                    float movementCost = GetMovementCost(currentNode.Position, neighborPos, movementCosts);
                    float tentativeGCost = currentNode.GCost + movementCost;
                    
                    if (tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.HCost = _heuristic(neighborPos, end);
                        neighborNode.FCost = neighborNode.GCost + neighborNode.HCost;
                        
                        if (!_openSet.Contains(neighborNode))
                        {
                            _openSet.Enqueue(neighborNode);
                        }
                        else
                        {
                            _openSet.UpdatePriority(neighborNode);
                        }
                    }
                }
            }
            
            // No path found
            return new List<Vector2Int>();
        }
        
        private void ResetPathfindingState()
        {
            _openSet.Clear();
            _closedSet.Clear();
            
            // Return nodes to pool
            foreach (var kvp in _allNodes)
            {
                ReturnNodeToPool(kvp.Value);
            }
            _allNodes.Clear();
        }
        
        private AStarNode GetOrCreateNode(Vector2Int position)
        {
            if (_allNodes.TryGetValue(position, out var existingNode))
                return existingNode;
            
            var newNode = GetNodeFromPool();
            newNode.Position = position;
            newNode.Reset();
            _allNodes[position] = newNode;
            return newNode;
        }
        
        private AStarNode GetNodeFromPool()
        {
            if (_nodePool.Count > 0)
                return _nodePool.Pop();
            
            return new AStarNode();
        }
        
        private void ReturnNodeToPool(AStarNode node)
        {
            if (_nodePool.Count < MAX_POOL_SIZE)
            {
                node.Reset();
                _nodePool.Push(node);
            }
        }
        
        private List<Vector2Int> GetNeighbors(Vector2Int position)
        {
            var neighbors = new List<Vector2Int>(8);
            
            // Cardinal directions
            neighbors.Add(position + Vector2Int.up);
            neighbors.Add(position + Vector2Int.down);
            neighbors.Add(position + Vector2Int.left);
            neighbors.Add(position + Vector2Int.right);
            
            // Diagonal directions
            neighbors.Add(position + new Vector2Int(1, 1));
            neighbors.Add(position + new Vector2Int(1, -1));
            neighbors.Add(position + new Vector2Int(-1, 1));
            neighbors.Add(position + new Vector2Int(-1, -1));
            
            return neighbors;
        }
        
        private bool IsValidPosition(Vector2Int position, bool[,] obstacles)
        {
            return position.x >= 0 && 
                   position.x < obstacles.GetLength(0) && 
                   position.y >= 0 && 
                   position.y < obstacles.GetLength(1);
        }
        
        private float GetMovementCost(Vector2Int from, Vector2Int to, float[,] movementCosts)
        {
            float baseCost = DEFAULT_MOVEMENT_COST;
            
            // Check for diagonal movement
            if (Mathf.Abs(from.x - to.x) == 1 && Mathf.Abs(from.y - to.y) == 1)
                baseCost = DIAGONAL_MOVEMENT_COST;
            
            // Apply custom movement costs if provided
            if (movementCosts != null)
            {
                if (to.x >= 0 && to.x < movementCosts.GetLength(0) && 
                    to.y >= 0 && to.y < movementCosts.GetLength(1))
                {
                    baseCost *= movementCosts[to.x, to.y];
                }
            }
            
            return baseCost;
        }
        
        private List<Vector2Int> ReconstructPath(AStarNode endNode)
        {
            var path = new List<Vector2Int>();
            var current = endNode;
            
            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }
            
            path.Reverse();
            return path;
        }
        
        private bool PathExistsBFS(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current == end)
                    return true;
                
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!IsValidPosition(neighbor, obstacles) || 
                        obstacles[neighbor.x, neighbor.y] || 
                        visited.Contains(neighbor))
                        continue;
                    
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            
            return false;
        }
        
        private bool HasLineOfSight(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            // Bresenham's line algorithm
            int x0 = start.x, y0 = start.y;
            int x1 = end.x, y1 = end.y;
            
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            
            int err = dx - dy;
            
            while (true)
            {
                // Check current position
                if (!IsValidPosition(new Vector2Int(x0, y0), obstacles) || obstacles[x0, y0])
                    return false;
                
                // Check if we've reached the end
                if (x0 == x1 && y0 == y1)
                    break;
                
                int e2 = 2 * err;
                
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
            
            return true;
        }
        
        private void UpdatePerformanceStats(List<Vector2Int> path, float elapsedMs)
        {
            // Update average computation time
            float totalCalls = _stats.TotalPathfindingCalls;
            float currentAvg = _stats.AverageComputationTime;
            _stats.AverageComputationTime = (currentAvg * (totalCalls - 1) + elapsedMs) / totalCalls;
            
            // Update average path length
            if (path.Count > 0)
            {
                float currentAvgLength = _stats.AveragePathLength;
                _stats.AveragePathLength = (currentAvgLength * (_stats.SuccessfulPaths - 1) + path.Count) / _stats.SuccessfulPaths;
            }
            
            // Update average nodes explored
            float currentAvgNodes = _stats.AverageNodesExplored;
            _stats.AverageNodesExplored = (currentAvgNodes * (totalCalls - 1) + _stats.TotalNodesExplored) / totalCalls;
        }
        
        #endregion
        
        #region Heuristic Functions
        
        public static float ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        public static float EuclideanDistance(Vector2Int a, Vector2Int b)
        {
            return Vector2Int.Distance(a, b);
        }
        
        public static float ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
        
        #endregion
    }
}