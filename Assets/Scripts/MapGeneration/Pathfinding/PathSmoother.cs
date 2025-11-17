using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Path smoothing algorithms for creating natural-looking corridors.
    /// Reduces jagged edges and optimizes paths for better visual appearance.
    /// </summary>
    public static class PathSmoother
    {
        #region Smoothing Algorithms
        
        /// <summary>
        /// Applies line-of-sight based smoothing to remove unnecessary waypoints.
        /// </summary>
        /// <param name="path">Original path to smooth</param>
        /// <param name="obstacles">Obstacle map for collision detection</param>
        /// <returns>Smoothed path with fewer waypoints</returns>
        public static List<Vector2Int> SmoothPathLineOfSight(List<Vector2Int> path, bool[,] obstacles)
        {
            if (path == null || path.Count <= 2)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            var smoothed = new List<Vector2Int> { path[0] };
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
                
                smoothed.Add(path[furthestVisible]);
                currentIndex = furthestVisible;
            }
            
            return smoothed;
        }
        
        /// <summary>
        /// Applies Catmull-Rom spline smoothing for curved corridors.
        /// </summary>
        /// <param name="path">Original path</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <param name="resolution">Number of interpolated points between original points</param>
        /// <returns>Smoothed curved path</returns>
        public static List<Vector2Int> SmoothPathSpline(List<Vector2Int> path, bool[,] obstacles, int resolution = 3)
        {
            if (path == null || path.Count <= 2)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            var smoothed = new List<Vector2Int>();
            
            // Add the first point
            smoothed.Add(path[0]);
            
            // Apply Catmull-Rom spline between points
            for (int i = 0; i < path.Count - 1; i++)
            {
                var p0 = i > 0 ? path[i - 1] : path[i];
                var p1 = path[i];
                var p2 = path[i + 1];
                var p3 = i < path.Count - 2 ? path[i + 2] : path[i + 1];
                
                // Generate interpolated points
                for (int j = 1; j <= resolution; j++)
                {
                    float t = (float)j / (resolution + 1);
                    var interpolatedPoint = CatmullRomPoint(p0, p1, p2, p3, t);
                    var gridPoint = new Vector2Int(
                        Mathf.RoundToInt(interpolatedPoint.x),
                        Mathf.RoundToInt(interpolatedPoint.y)
                    );
                    
                    // Only add if it's valid and not a duplicate
                    if (IsValidPosition(gridPoint, obstacles) && 
                        (smoothed.Count == 0 || smoothed[smoothed.Count - 1] != gridPoint))
                    {
                        smoothed.Add(gridPoint);
                    }
                }
                
                // Add the next original point
                if (i < path.Count - 2)
                {
                    smoothed.Add(path[i + 1]);
                }
            }
            
            // Add the last point
            smoothed.Add(path[path.Count - 1]);
            
            return smoothed;
        }
        
        /// <summary>
        /// Applies angular smoothing to reduce sharp turns.
        /// </summary>
        /// <param name="path">Original path</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <param name="maxAngle">Maximum allowed turn angle in degrees</param>
        /// <returns>Path with smoothed angles</returns>
        public static List<Vector2Int> SmoothPathAngles(List<Vector2Int> path, bool[,] obstacles, float maxAngle = 45f)
        {
            if (path == null || path.Count <= 2)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            var smoothed = new List<Vector2Int> { path[0] };
            float maxAngleRad = maxAngle * Mathf.Deg2Rad;
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                var next = path[i + 1];
                
                // Calculate turn angle
                var dir1 = (current - prev).normalized;
                var dir2 = (next - current).normalized;
                float angle = Mathf.Acos(Vector2.Dot(dir1, dir2));
                
                if (angle > maxAngleRad)
                {
                    // Try to find a smoother intermediate point
                    var smoothedPoint = FindSmoothedPoint(prev, current, next, obstacles, maxAngleRad);
                    if (smoothedPoint.HasValue && smoothedPoint.Value != current)
                    {
                        smoothed.Add(smoothedPoint.Value);
                    }
                    else
                    {
                        smoothed.Add(current);
                    }
                }
                else
                {
                    smoothed.Add(current);
                }
            }
            
            smoothed.Add(path[path.Count - 1]);
            return smoothed;
        }
        
        /// <summary>
        /// Applies weighted smoothing that balances between directness and smoothness.
        /// </summary>
        /// <param name="path">Original path</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <param name="smoothness">Smoothness factor (0-1, higher = smoother)</param>
        /// <returns>Weighted smoothed path</returns>
        public static List<Vector2Int> SmoothPathWeighted(List<Vector2Int> path, bool[,] obstacles, float smoothness = 0.5f)
        {
            if (path == null || path.Count <= 2)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            smoothness = Mathf.Clamp01(smoothness);
            
            // First pass: line-of-sight smoothing
            var losSmoothed = SmoothPathLineOfSight(path, obstacles);
            
            // Second pass: angular smoothing based on smoothness factor
            if (smoothness > 0.3f)
            {
                var angleSmoothed = SmoothPathAngles(losSmoothed, obstacles, 45f * smoothness);
                
                // Third pass: spline smoothing for high smoothness values
                if (smoothness > 0.7f)
                {
                    return SmoothPathSpline(angleSmoothed, obstacles, Mathf.RoundToInt(3 * smoothness));
                }
                
                return angleSmoothed;
            }
            
            return losSmoothed;
        }
        
        #endregion
        
        #region Path Optimization
        
        /// <summary>
        /// Optimizes path by removing redundant points and ensuring minimum distance between waypoints.
        /// </summary>
        /// <param name="path">Path to optimize</param>
        /// <param name="minDistance">Minimum distance between consecutive waypoints</param>
        /// <returns>Optimized path</returns>
        public static List<Vector2Int> OptimizePath(List<Vector2Int> path, float minDistance = 1f)
        {
            if (path == null || path.Count <= 2)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            var optimized = new List<Vector2Int> { path[0] };
            
            for (int i = 1; i < path.Count; i++)
            {
                var lastAdded = optimized[optimized.Count - 1];
                var current = path[i];
                
                // Only add if it's far enough from the last added point
                if (Vector2Int.Distance(lastAdded, current) >= minDistance)
                {
                    optimized.Add(current);
                }
            }
            
            // Ensure the last point is included
            if (optimized[optimized.Count - 1] != path[path.Count - 1])
            {
                optimized.Add(path[path.Count - 1]);
            }
            
            return optimized;
        }
        
        /// <summary>
        /// Ensures path continuity by filling gaps between consecutive points.
        /// </summary>
        /// <param name="path">Path to ensure continuity for</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Continuous path</returns>
        public static List<Vector2Int> EnsurePathContinuity(List<Vector2Int> path, bool[,] obstacles)
        {
            if (path == null || path.Count <= 1)
                return new List<Vector2Int>(path ?? new List<Vector2Int>());
            
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            var continuous = new List<Vector2Int> { path[0] };
            
            for (int i = 1; i < path.Count; i++)
            {
                var from = path[i - 1];
                var to = path[i];
                
                // Check if there's a gap
                var diff = to - from;
                float distance = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);
                
                if (distance > 1)
                {
                    // Fill the gap with intermediate points
                    var intermediatePoints = GetIntermediatePoints(from, to, obstacles);
                    continuous.AddRange(intermediatePoints);
                }
                
                continuous.Add(to);
            }
            
            return continuous;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private static bool HasLineOfSight(Vector2Int start, Vector2Int end, bool[,] obstacles)
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
                if (!IsValidPosition(new Vector2Int(x0, y0), obstacles))
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
        
        private static Vector2 CatmullRomPoint(Vector2Int p0, Vector2Int p1, Vector2Int p2, Vector2Int p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        
        private static Vector2Int? FindSmoothedPoint(Vector2Int prev, Vector2Int current, Vector2Int next, 
            bool[,] obstacles, float maxAngleRad)
        {
            var dir1 = (current - prev).normalized;
            var dir2 = (next - current).normalized;
            
            // Calculate the bisector direction
            var bisector = (dir1 + dir2).normalized;
            
            // Try points along the bisector
            for (int distance = 1; distance <= 3; distance++)
            {
                var candidate = current + Vector2Int.RoundToInt(bisector * distance);
                
                if (IsValidPosition(candidate, obstacles))
                {
                    // Check if this creates a smoother angle
                    var newDir1 = (candidate - prev).normalized;
                    var newDir2 = (next - candidate).normalized;
                    float newAngle = Mathf.Acos(Vector2.Dot(newDir1, newDir2));
                    
                    if (newAngle <= maxAngleRad)
                    {
                        return candidate;
                    }
                }
            }
            
            return null;
        }
        
        private static List<Vector2Int> GetIntermediatePoints(Vector2Int from, Vector2Int to, bool[,] obstacles)
        {
            var points = new List<Vector2Int>();
            
            // Use A* to find a path between the points
            var pathfinder = new AStarPathfinder();
            var path = pathfinder.FindPath(from, to, obstacles);
            
            // Skip the first and last points as they're already in the main path
            for (int i = 1; i < path.Count - 1; i++)
            {
                points.Add(path[i]);
            }
            
            return points;
        }
        
        private static bool IsValidPosition(Vector2Int position, bool[,] obstacles)
        {
            if (obstacles == null)
                return false;
            
            return position.x >= 0 && position.x < obstacles.GetLength(0) && 
                   position.y >= 0 && position.y < obstacles.GetLength(1) && 
                   !obstacles[position.x, position.y];
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Calculates the smoothness score of a path (lower is smoother).
        /// </summary>
        public static float CalculatePathSmoothness(List<Vector2Int> path)
        {
            if (path == null || path.Count <= 2)
                return 0f;
            
            float totalAngleChange = 0f;
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                var next = path[i + 1];
                
                var dir1 = (current - prev).normalized;
                var dir2 = (next - current).normalized;
                
                float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(dir1, dir2), -1f, 1f));
                totalAngleChange += angle;
            }
            
            return totalAngleChange / (path.Count - 2);
        }
        
        /// <summary>
        /// Calculates the efficiency ratio of a smoothed path compared to the original.
        /// </summary>
        public static float CalculatePathEfficiency(List<Vector2Int> originalPath, List<Vector2Int> smoothedPath)
        {
            if (originalPath == null || smoothedPath == null)
                return 0f;
            
            if (originalPath.Count == 0)
                return 1f;
            
            float originalLength = CalculatePathLength(originalPath);
            float smoothedLength = CalculatePathLength(smoothedPath);
            
            return smoothedLength / originalLength;
        }
        
        private static float CalculatePathLength(List<Vector2Int> path)
        {
            if (path == null || path.Count <= 1)
                return 0f;
            
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                length += Vector2Int.Distance(path[i - 1], path[i]);
            }
            return length;
        }
        
        #endregion
    }
}