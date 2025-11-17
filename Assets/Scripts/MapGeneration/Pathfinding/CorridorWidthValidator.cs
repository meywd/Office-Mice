using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Corridor width validation system for ensuring proper corridor dimensions.
    /// Validates corridor width constraints and ensures compatibility with pathfinding.
    /// </summary>
    public static class CorridorWidthValidator
    {
        #region Constants
        
        public const int MIN_CORRIDOR_WIDTH = 1;
        public const int MAX_CORRIDOR_WIDTH = 5;
        public const int DEFAULT_CORRIDOR_WIDTH = 3;
        public const int RECOMMENDED_MIN_WIDTH = 3;
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>
        /// Validates corridor width against allowed constraints.
        /// </summary>
        /// <param name="width">Width to validate</param>
        /// <returns>Validation result with detailed information</returns>
        public static ValidationResult ValidateCorridorWidth(int width)
        {
            var result = new ValidationResult();
            
            if (width < MIN_CORRIDOR_WIDTH)
            {
                result.AddError($"Corridor width {width} is below minimum allowed width of {MIN_CORRIDOR_WIDTH}");
            }
            else if (width < RECOMMENDED_MIN_WIDTH)
            {
                result.AddWarning($"Corridor width {width} is below recommended minimum width of {RECOMMENDED_MIN_WIDTH}. May cause navigation issues.");
            }
            
            if (width > MAX_CORRIDOR_WIDTH)
            {
                result.AddError($"Corridor width {width} exceeds maximum allowed width of {MAX_CORRIDOR_WIDTH}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates that a corridor path fits within the specified width constraints.
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <param name="width">Corridor width</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateCorridorPath(List<Vector2Int> path, int width, bool[,] obstacles)
        {
            var result = ValidateCorridorWidth(width);
            
            if (path == null)
            {
                result.AddError("Corridor path cannot be null");
                return result;
            }
            
            if (obstacles == null)
            {
                result.AddError("Obstacle map cannot be null");
                return result;
            }
            
            if (path.Count == 0)
            {
                result.AddError("Corridor path cannot be empty");
                return result;
            }
            
            // Validate each tile in the path
            int radius = width / 2;
            int mapWidth = obstacles.GetLength(0);
            int mapHeight = obstacles.GetLength(1);
            
            for (int i = 0; i < path.Count; i++)
            {
                var tile = path[i];
                
                // Check if the corridor width fits at this position
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int x = tile.x + dx;
                        int y = tile.y + dy;
                        
                        // Check map boundaries
                        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                        {
                            result.AddError($"Corridor width {width} exceeds map boundaries at position {tile} (tile {x},{y})");
                            return result;
                        }
                        
                        // Check for obstacles
                        if (obstacles[x, y])
                        {
                            result.AddError($"Corridor width {width} collides with obstacle at position {tile} (tile {x},{y})");
                            return result;
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates a complete CorridorData object.
        /// </summary>
        /// <param name="corridor">Corridor to validate</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateCorridorData(CorridorData corridor, bool[,] obstacles)
        {
            var result = new ValidationResult();
            
            if (corridor == null)
            {
                result.AddError("Corridor data cannot be null");
                return result;
            }
            
            // Validate corridor width
            var widthValidation = ValidateCorridorWidth(corridor.Width);
            result.Merge(widthValidation);
            
            // Validate path
            if (corridor.PathTiles.Count == 0)
            {
                result.AddError("Corridor has no path tiles");
            }
            else
            {
                // Validate path fits within width constraints
                if (obstacles != null)
                {
                    var pathValidation = ValidateCorridorPath(
                        new List<Vector2Int>(corridor.PathTiles), 
                        corridor.Width, 
                        obstacles
                    );
                    result.Merge(pathValidation);
                }
                
                // Validate path continuity
                var continuityValidation = ValidatePathContinuity(corridor.PathTiles);
                result.Merge(continuityValidation);
            }
            
            // Validate start and end positions
            if (corridor.PathTiles.Count > 0)
            {
                var firstTile = corridor.PathTiles[0];
                var lastTile = corridor.PathTiles[corridor.PathTiles.Count - 1];
                
                if (firstTile != corridor.StartPosition)
                {
                    result.AddWarning($"Corridor start position {corridor.StartPosition} doesn't match first path tile {firstTile}");
                }
                
                if (lastTile != corridor.EndPosition)
                {
                    result.AddWarning($"Corridor end position {corridor.EndPosition} doesn't match last path tile {lastTile}");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates multiple corridors for width consistency and conflicts.
        /// </summary>
        /// <param name="corridors">List of corridors to validate</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <param name="allowVariableWidth">Whether corridors can have different widths</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateCorridorCollection(List<CorridorData> corridors, bool[,] obstacles, bool allowVariableWidth = false)
        {
            var result = new ValidationResult();
            
            if (corridors == null)
            {
                result.AddError("Corridor collection cannot be null");
                return result;
            }
            
            if (corridors.Count == 0)
            {
                result.AddWarning("Corridor collection is empty");
                return result;
            }
            
            // Validate individual corridors
            foreach (var corridor in corridors)
            {
                var corridorValidation = ValidateCorridorData(corridor, obstacles);
                result.Merge(corridorValidation);
            }
            
            // Check width consistency
            if (!allowVariableWidth)
            {
                int firstWidth = corridors[0].Width;
                for (int i = 1; i < corridors.Count; i++)
                {
                    if (corridors[i].Width != firstWidth)
                    {
                        result.AddWarning($"Corridor width inconsistency: corridor {corridors[i].CorridorID} has width {corridors[i].Width}, expected {firstWidth}");
                    }
                }
            }
            
            // Check for corridor intersections
            if (obstacles != null)
            {
                var intersectionValidation = ValidateCorridorIntersections(corridors, obstacles);
                result.Merge(intersectionValidation);
            }
            
            return result;
        }
        
        #endregion
        
        #region Width Calculation Methods
        
        /// <summary>
        /// Calculates the optimal corridor width based on room sizes and map constraints.
        /// </summary>
        /// <param name="rooms">List of rooms in the map</param>
        /// <param name="mapSize">Size of the map</param>
        /// <param name="preferredWidth">Preferred corridor width</param>
        /// <returns>Optimal corridor width</returns>
        public static int CalculateOptimalCorridorWidth(List<RoomData> rooms, Vector2Int mapSize, int preferredWidth = DEFAULT_CORRIDOR_WIDTH)
        {
            // Start with preferred width
            int optimalWidth = Mathf.Clamp(preferredWidth, MIN_CORRIDOR_WIDTH, MAX_CORRIDOR_WIDTH);
            
            // Adjust based on map size
            float mapArea = mapSize.x * mapSize.y;
            if (mapArea < 2500) // Small maps (< 50x50)
            {
                optimalWidth = Mathf.Min(optimalWidth, 2);
            }
            else if (mapArea > 10000) // Large maps (> 100x100)
            {
                optimalWidth = Mathf.Max(optimalWidth, 4);
            }
            
            // Adjust based on room density
            if (rooms != null && rooms.Count > 0)
            {
                float avgRoomSize = 0f;
                foreach (var room in rooms)
                {
                    var bounds = room.GetBounds();
                    avgRoomSize += bounds.width * bounds.height;
                }
                avgRoomSize /= rooms.Count;
                
                // Larger rooms can support wider corridors
                if (avgRoomSize > 100)
                {
                    optimalWidth = Mathf.Min(optimalWidth + 1, MAX_CORRIDOR_WIDTH);
                }
                else if (avgRoomSize < 25)
                {
                    optimalWidth = Mathf.Max(optimalWidth - 1, MIN_CORRIDOR_WIDTH);
                }
            }
            
            return optimalWidth;
        }
        
        /// <summary>
        /// Gets the effective width of a corridor at a specific position.
        /// </summary>
        /// <param name="corridor">Corridor to check</param>
        /// <param name="position">Position to check</param>
        /// <returns>Effective width at the position</returns>
        public static int GetEffectiveWidthAtPosition(CorridorData corridor, Vector2Int position)
        {
            if (corridor == null || !corridor.ContainsTile(position))
                return 0;
            
            // For now, return the corridor's base width
            // In the future, this could account for variable width corridors
            return corridor.Width;
        }
        
        #endregion
        
        #region Path Validation Methods
        
        /// <summary>
        /// Validates that a path has proper continuity (no gaps between consecutive tiles).
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidatePathContinuity(IReadOnlyList<Vector2Int> path)
        {
            var result = new ValidationResult();
            
            if (path == null || path.Count <= 1)
                return result;
            
            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var curr = path[i];
                
                // Calculate Manhattan distance
                int distance = Mathf.Abs(curr.x - prev.x) + Mathf.Abs(curr.y - prev.y);
                
                if (distance > 1)
                {
                    result.AddError($"Path continuity broken: gap between tiles {prev} and {curr} (distance: {distance})");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates that corridors don't intersect in problematic ways.
        /// </summary>
        /// <param name="corridors">List of corridors to check</param>
        /// <param name="obstacles">Obstacle map</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateCorridorIntersections(List<CorridorData> corridors, bool[,] obstacles)
        {
            var result = new ValidationResult();
            
            if (corridors == null || corridors.Count <= 1)
                return result;
            
            // Create a map of corridor tiles
            var corridorMap = new Dictionary<Vector2Int, List<int>>();
            
            for (int i = 0; i < corridors.Count; i++)
            {
                var corridor = corridors[i];
                foreach (var tile in corridor.PathTiles)
                {
                    if (!corridorMap.ContainsKey(tile))
                    {
                        corridorMap[tile] = new List<int>();
                    }
                    corridorMap[tile].Add(corridor.CorridorID);
                }
            }
            
            // Check for intersections
            foreach (var kvp in corridorMap)
            {
                if (kvp.Value.Count > 1)
                {
                    // Multiple corridors use this tile
                    string corridorIds = string.Join(", ", kvp.Value);
                    result.AddWarning($"Corridor intersection detected at {kvp.Key}: corridors {corridorIds}");
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Gets the minimum recommended corridor width for a given map configuration.
        /// </summary>
        /// <param name="mapSize">Size of the map</param>
        /// <param name="hasDiagonalMovement">Whether the map supports diagonal movement</param>
        /// <returns>Recommended minimum width</returns>
        public static int GetRecommendedMinimumWidth(Vector2Int mapSize, bool hasDiagonalMovement = false)
        {
            // Base recommendation
            int minWidth = RECOMMENDED_MIN_WIDTH;
            
            // Adjust for diagonal movement (wider corridors help with diagonal paths)
            if (hasDiagonalMovement)
            {
                minWidth = Mathf.Max(minWidth, 4);
            }
            
            // Adjust for map size
            float mapArea = mapSize.x * mapSize.y;
            if (mapArea > 10000) // Large maps
            {
                minWidth = Mathf.Max(minWidth, 4);
            }
            
            return minWidth;
        }
        
        /// <summary>
        /// Creates a width validation report for debugging.
        /// </summary>
        /// <param name="corridors">Corridors to analyze</param>
        /// <returns>Detailed width report</returns>
        public static string CreateWidthReport(List<CorridorData> corridors)
        {
            if (corridors == null)
                return "Corridor collection is null";
            
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Corridor Width Report ===");
            report.AppendLine($"Total corridors: {corridors.Count}");
            
            if (corridors.Count == 0)
                return report.ToString();
            
            var widthCounts = new Dictionary<int, int>();
            int totalTiles = 0;
            
            foreach (var corridor in corridors)
            {
                int width = corridor.Width;
                if (!widthCounts.ContainsKey(width))
                {
                    widthCounts[width] = 0;
                }
                widthCounts[width]++;
                totalTiles += corridor.PathTiles.Count;
            }
            
            report.AppendLine("\nWidth distribution:");
            foreach (var kvp in widthCounts.OrderBy(x => x.Key))
            {
                float percentage = (float)kvp.Value / corridors.Count * 100f;
                report.AppendLine($"  Width {kvp.Value}: {kvp.Value} corridors ({percentage:F1}%)");
            }
            
            report.AppendLine($"\nTotal corridor tiles: {totalTiles}");
            report.AppendLine($"Average tiles per corridor: {(float)totalTiles / corridors.Count:F1}");
            
            return report.ToString();
        }
        
        #endregion
    }
}