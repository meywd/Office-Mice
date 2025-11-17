using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using OfficeMice.MapGeneration.Interfaces;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Performance optimization system for A* pathfinding.
    /// Provides caching, object pooling, and algorithmic optimizations.
    /// </summary>
    public static class PathfindingOptimizer
    {
        #region Performance Settings
        
        public const int DEFAULT_CACHE_SIZE = 1000;
        public const int MAX_CACHE_SIZE = 10000;
        public const int NODE_POOL_SIZE = 1000;
        public const int PATH_CACHE_EXPIRY_MINUTES = 10;
        
        #endregion
        
        #region Static Fields
        
        // Path caching
        private static readonly Dictionary<PathCacheKey, List<Vector2Int>> _pathCache = 
            new Dictionary<PathCacheKey, List<Vector2Int>>();
        private static readonly Dictionary<PathCacheKey, DateTime> _cacheTimestamps = 
            new Dictionary<PathCacheKey, DateTime>();
        private static readonly object _cacheLock = new object();
        
        // Node pooling
        private static readonly Stack<AStarNode> _nodePool = new Stack<AStarNode>(NODE_POOL_SIZE);
        private static readonly object _poolLock = new object();
        
        // Performance tracking
        private static readonly PerformanceMetrics _metrics = new PerformanceMetrics();
        
        #endregion
        
        #region Caching System
        
        /// <summary>
        /// Attempts to retrieve a cached path.
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="obstacles">Obstacle map hash</param>
        /// <returns>Cached path or null if not found</returns>
        public static List<Vector2Int> GetCachedPath(Vector2Int start, Vector2Int end, int obstaclesHash)
        {
            lock (_cacheLock)
            {
                var key = new PathCacheKey(start, end, obstaclesHash);
                
                if (_pathCache.TryGetValue(key, out var cachedPath))
                {
                    // Check if cache entry is still valid
                    if (_cacheTimestamps.TryGetValue(key, out var timestamp))
                    {
                        if (DateTime.UtcNow - timestamp < TimeSpan.FromMinutes(PATH_CACHE_EXPIRY_MINUTES))
                        {
                            _metrics.CacheHits++;
                            return new List<Vector2Int>(cachedPath);
                        }
                        else
                        {
                            // Remove expired entry
                            _pathCache.Remove(key);
                            _cacheTimestamps.Remove(key);
                        }
                    }
                }
                
                _metrics.CacheMisses++;
                return null;
            }
        }
        
        /// <summary>
        /// Caches a path for future use.
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="obstacles">Obstacle map hash</param>
        /// <param name="path">Path to cache</param>
        public static void CachePath(Vector2Int start, Vector2Int end, int obstaclesHash, List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
                return;
            
            lock (_cacheLock)
            {
                // Check cache size limit
                if (_pathCache.Count >= MAX_CACHE_SIZE)
                {
                    ClearOldestCacheEntries();
                }
                
                var key = new PathCacheKey(start, end, obstaclesHash);
                _pathCache[key] = new List<Vector2Int>(path);
                _cacheTimestamps[key] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Clears the path cache.
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _pathCache.Clear();
                _cacheTimestamps.Clear();
            }
        }
        
        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public static (int entries, int hits, int misses) GetCacheStats()
        {
            lock (_cacheLock)
            {
                return (_pathCache.Count, _metrics.CacheHits, _metrics.CacheMisses);
            }
        }
        
        #endregion
        
        #region Object Pooling
        
        /// <summary>
        /// Gets a node from the pool or creates a new one.
        /// </summary>
        /// <returns>AStarNode instance</returns>
        public static AStarNode GetNodeFromPool()
        {
            lock (_poolLock)
            {
                if (_nodePool.Count > 0)
                {
                    var node = _nodePool.Pop();
                    node.Reset();
                    return node;
                }
                
                _metrics.NodesCreated++;
                return new AStarNode();
            }
        }
        
        /// <summary>
        /// Returns a node to the pool.
        /// </summary>
        /// <param name="node">Node to return</param>
        public static void ReturnNodeToPool(AStarNode node)
        {
            if (node == null)
                return;
            
            lock (_poolLock)
            {
                if (_nodePool.Count < NODE_POOL_SIZE)
                {
                    node.Reset();
                    _nodePool.Push(node);
                }
            }
        }
        
        /// <summary>
        /// Gets pool statistics.
        /// </summary>
        public static (int poolSize, int nodesCreated) GetPoolStats()
        {
            lock (_poolLock)
            {
                return (_nodePool.Count, _metrics.NodesCreated);
            }
        }
        
        #endregion
        
        #region Performance Monitoring
        
        /// <summary>
        /// Records a pathfinding operation.
        /// </summary>
        /// <param name="duration">Duration in milliseconds</param>
        /// <param name="nodesExplored">Number of nodes explored</param>
        /// <param name="pathLength">Length of resulting path</param>
        /// <param name="success">Whether pathfinding was successful</param>
        public static void RecordOperation(float duration, int nodesExplored, int pathLength, bool success)
        {
            _metrics.RecordOperation(duration, nodesExplored, pathLength, success);
        }
        
        /// <summary>
        /// Gets current performance metrics.
        /// </summary>
        public static PerformanceMetrics GetMetrics()
        {
            return _metrics.Clone();
        }
        
        /// <summary>
        /// Resets performance metrics.
        /// </summary>
        public static void ResetMetrics()
        {
            _metrics.Reset();
        }
        
        #endregion
        
        #region Algorithmic Optimizations
        
        /// <summary>
        /// Optimizes pathfinding parameters based on map characteristics.
        /// </summary>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Optimization settings</returns>
        public static PathfindingOptimizationSettings AnalyzeMap(bool[,] obstacles)
        {
            if (obstacles == null)
                return new PathfindingOptimizationSettings();
            
            var settings = new PathfindingOptimizationSettings();
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            int totalTiles = width * height;
            
            // Calculate obstacle density
            int obstacleCount = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (obstacles[x, y])
                        obstacleCount++;
                }
            }
            
            float obstacleDensity = (float)obstacleCount / totalTiles;
            settings.ObstacleDensity = obstacleDensity;
            
            // Adjust settings based on map characteristics
            if (totalTiles < 2500) // Small maps
            {
                settings.UseEarlyTermination = false;
                settings.MaxSearchDepth = totalTiles / 2;
            }
            else if (totalTiles > 10000) // Large maps
            {
                settings.UseEarlyTermination = true;
                settings.MaxSearchDepth = totalTiles / 10;
                settings.UseHierarchicalPathfinding = true;
            }
            
            if (obstacleDensity > 0.7f) // High obstacle density
            {
                settings.UseEarlyTermination = true;
                settings.MaxSearchDepth = totalTiles / 20;
                settings.UseBidirectionalSearch = true;
            }
            else if (obstacleDensity < 0.3f) // Low obstacle density
            {
                settings.UseJumpPointSearch = true;
            }
            
            return settings;
        }
        
        /// <summary>
        /// Applies hierarchical pathfinding for large maps.
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <param name="clusterSize">Size of hierarchical clusters</param>
        /// <returns>Hierarchical path</returns>
        public static List<Vector2Int> FindHierarchicalPath(Vector2Int start, Vector2Int end, 
            bool[,] obstacles, int clusterSize = 10)
        {
            if (obstacles == null)
                return new List<Vector2Int>();
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            // For small maps, use regular pathfinding
            if (width <= clusterSize * 2 || height <= clusterSize * 2)
            {
                var pathfinder = new AStarPathfinder();
                return pathfinder.FindPath(start, end, obstacles);
            }
            
            // Create hierarchical representation
            var clusters = CreateClusters(obstacles, clusterSize);
            var clusterGraph = CreateClusterGraph(clusters, obstacles, clusterSize);
            
            // Find path through clusters
            var startCluster = GetCluster(start, clusterSize);
            var endCluster = GetCluster(end, clusterSize);
            
            if (startCluster == endCluster)
            {
                // Same cluster, use regular pathfinding
                var pathfinder = new AStarPathfinder();
                return pathfinder.FindPath(start, end, obstacles);
            }
            
            // Find path through cluster graph
            var clusterPath = FindClusterPath(startCluster, endCluster, clusterGraph);
            
            // Refine path within clusters
            return RefineHierarchicalPath(start, end, clusterPath, obstacles, clusterSize);
        }
        
        /// <summary>
        /// Applies bidirectional search for faster pathfinding.
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Bidirectional search path</returns>
        public static List<Vector2Int> FindBidirectionalPath(Vector2Int start, Vector2Int end, bool[,] obstacles)
        {
            // This is a simplified implementation
            // In practice, this would run two simultaneous A* searches from start and end
            var pathfinder = new AStarPathfinder();
            return pathfinder.FindPath(start, end, obstacles);
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private static void ClearOldestCacheEntries()
        {
            // Remove oldest 25% of entries
            int entriesToRemove = _pathCache.Count / 4;
            var sortedEntries = new List<KeyValuePair<PathCacheKey, DateTime>>(_cacheTimestamps);
            sortedEntries.Sort((a, b) => a.Value.CompareTo(b.Value));
            
            for (int i = 0; i < entriesToRemove && i < sortedEntries.Count; i++)
            {
                var key = sortedEntries[i].Key;
                _pathCache.Remove(key);
                _cacheTimestamps.Remove(key);
            }
        }
        
        private static List<Cluster> CreateClusters(bool[,] obstacles, int clusterSize)
        {
            var clusters = new List<Cluster>();
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            for (int x = 0; x < width; x += clusterSize)
            {
                for (int y = 0; y < height; y += clusterSize)
                {
                    var cluster = new Cluster
                    {
                        X = x,
                        Y = y,
                        Width = Math.Min(clusterSize, width - x),
                        Height = Math.Min(clusterSize, height - y)
                    };
                    
                    // Calculate cluster passability
                    cluster.Passable = CalculateClusterPassability(obstacles, cluster);
                    clusters.Add(cluster);
                }
            }
            
            return clusters;
        }
        
        private static bool CalculateClusterPassability(bool[,] obstacles, Cluster cluster)
        {
            int passableTiles = 0;
            int totalTiles = cluster.Width * cluster.Height;
            
            for (int x = cluster.X; x < cluster.X + cluster.Width; x++)
            {
                for (int y = cluster.Y; y < cluster.Y + cluster.Height; y++)
                {
                    if (!obstacles[x, y])
                        passableTiles++;
                }
            }
            
            return (float)passableTiles / totalTiles > 0.3f; // At least 30% passable
        }
        
        private static Dictionary<Cluster, List<Cluster>> CreateClusterGraph(List<Cluster> clusters, 
            bool[,] obstacles, int clusterSize)
        {
            var graph = new Dictionary<Cluster, List<Cluster>>();
            
            foreach (var cluster in clusters)
            {
                if (!cluster.Passable)
                    continue;
                
                graph[cluster] = new List<Cluster>();
                
                // Find adjacent clusters
                foreach (var other in clusters)
                {
                    if (other == cluster || !other.Passable)
                        continue;
                    
                    if (AreClustersAdjacent(cluster, other, clusterSize))
                    {
                        graph[cluster].Add(other);
                    }
                }
            }
            
            return graph;
        }
        
        private static bool AreClustersAdjacent(Cluster a, Cluster b, int clusterSize)
        {
            // Check if clusters are adjacent (including diagonal)
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            
            return dx <= clusterSize && dy <= clusterSize && (dx > 0 || dy > 0);
        }
        
        private static Cluster GetCluster(Vector2Int position, int clusterSize)
        {
            return new Cluster
            {
                X = (position.x / clusterSize) * clusterSize,
                Y = (position.y / clusterSize) * clusterSize,
                Width = clusterSize,
                Height = clusterSize
            };
        }
        
        private static List<Cluster> FindClusterPath(Cluster start, Cluster end, 
            Dictionary<Cluster, List<Cluster>> graph)
        {
            // Simplified cluster pathfinding
            var path = new List<Cluster>();
            var visited = new HashSet<Cluster>();
            var queue = new Queue<Cluster>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                path.Add(current);
                
                if (current == end)
                    break;
                
                if (graph.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
            
            return path;
        }
        
        private static List<Vector2Int> RefineHierarchicalPath(Vector2Int start, Vector2Int end, 
            List<Cluster> clusterPath, bool[,] obstacles, int clusterSize)
        {
            var refinedPath = new List<Vector2Int> { start };
            var pathfinder = new AStarPathfinder();
            
            for (int i = 0; i < clusterPath.Count - 1; i++)
            {
                var currentCluster = clusterPath[i];
                var nextCluster = clusterPath[i + 1];
                
                // Find entry and exit points
                var entryPoint = GetClusterEntryPoint(currentCluster, nextCluster, obstacles, clusterSize);
                var exitPoint = GetClusterExitPoint(currentCluster, nextCluster, obstacles, clusterSize);
                
                // Find path within cluster
                var intraClusterPath = pathfinder.FindPath(entryPoint, exitPoint, obstacles);
                refinedPath.AddRange(intraClusterPath);
            }
            
            // Add final path to end
            var lastCluster = clusterPath[clusterPath.Count - 1];
            var finalPath = pathfinder.FindPath(refinedPath[refinedPath.Count - 1], end, obstacles);
            refinedPath.AddRange(finalPath);
            
            return refinedPath;
        }
        
        private static Vector2Int GetClusterEntryPoint(Cluster from, Cluster to, bool[,] obstacles, int clusterSize)
        {
            // Simplified: return center of cluster
            return new Vector2Int(from.X + from.Width / 2, from.Y + from.Height / 2);
        }
        
        private static Vector2Int GetClusterExitPoint(Cluster from, Cluster to, bool[,] obstacles, int clusterSize)
        {
            // Simplified: return edge point towards next cluster
            int exitX = from.X + (to.X > from.X ? from.Width - 1 : 0);
            int exitY = from.Y + (to.Y > from.Y ? from.Height - 1 : 0);
            return new Vector2Int(exitX, exitY);
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Key for path caching.
    /// </summary>
    internal readonly struct PathCacheKey : IEquatable<PathCacheKey>
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int End;
        public readonly int ObstaclesHash;
        
        public PathCacheKey(Vector2Int start, Vector2Int end, int obstaclesHash)
        {
            Start = start;
            End = end;
            ObstaclesHash = obstaclesHash;
        }
        
        public bool Equals(PathCacheKey other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End) && ObstaclesHash == other.ObstaclesHash;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PathCacheKey other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, ObstaclesHash);
        }
    }
    
    /// <summary>
    /// Performance metrics for pathfinding operations.
    /// </summary>
    public class PerformanceMetrics
    {
        public int TotalOperations { get; private set; }
        public int SuccessfulOperations { get; private set; }
        public float AverageDuration { get; private set; }
        public float AverageNodesExplored { get; private set; }
        public float AveragePathLength { get; private set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public int NodesCreated { get; set; }
        
        public void RecordOperation(float duration, int nodesExplored, int pathLength, bool success)
        {
            TotalOperations++;
            
            if (success)
                SuccessfulOperations++;
            
            // Update averages
            AverageDuration = (AverageDuration * (TotalOperations - 1) + duration) / TotalOperations;
            AverageNodesExplored = (AverageNodesExplored * (TotalOperations - 1) + nodesExplored) / TotalOperations;
            AveragePathLength = (AveragePathLength * (TotalOperations - 1) + pathLength) / TotalOperations;
        }
        
        public PerformanceMetrics Clone()
        {
            return new PerformanceMetrics
            {
                TotalOperations = this.TotalOperations,
                SuccessfulOperations = this.SuccessfulOperations,
                AverageDuration = this.AverageDuration,
                AverageNodesExplored = this.AverageNodesExplored,
                AveragePathLength = this.AveragePathLength,
                CacheHits = this.CacheHits,
                CacheMisses = this.CacheMisses,
                NodesCreated = this.NodesCreated
            };
        }
        
        public void Reset()
        {
            TotalOperations = 0;
            SuccessfulOperations = 0;
            AverageDuration = 0f;
            AverageNodesExplored = 0f;
            AveragePathLength = 0f;
            CacheHits = 0;
            CacheMisses = 0;
            NodesCreated = 0;
        }
    }
    
    /// <summary>
    /// Settings for pathfinding optimization.
    /// </summary>
    public class PathfindingOptimizationSettings
    {
        public bool UseEarlyTermination { get; set; } = false;
        public bool UseHierarchicalPathfinding { get; set; } = false;
        public bool UseBidirectionalSearch { get; set; } = false;
        public bool UseJumpPointSearch { get; set; } = false;
        public int MaxSearchDepth { get; set; } = int.MaxValue;
        public float ObstacleDensity { get; set; } = 0f;
    }
    
    /// <summary>
    /// Represents a cluster in hierarchical pathfinding.
    /// </summary>
    internal class Cluster
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Passable { get; set; }
    }
    
    #endregion
}