using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Obstacle detection system for A* pathfinding.
    /// Identifies and manages obstacles including rooms, existing corridors, and map boundaries.
    /// </summary>
    public static class ObstacleDetector
    {
        #region Obstacle Detection Methods
        
        /// <summary>
        /// Creates an obstacle map from rooms and existing corridors.
        /// </summary>
        /// <param name="mapWidth">Width of the map</param>
        /// <param name="mapHeight">Height of the map</param>
        /// <param name="rooms">List of rooms to mark as obstacles</param>
        /// <param name="existingCorridors">List of existing corridors to mark as obstacles</param>
        /// <param name="corridorWidth">Width of corridors to expand for obstacle detection</param>
        /// <returns>2D boolean array where true indicates an obstacle</returns>
        public static bool[,] CreateObstacleMap(int mapWidth, int mapHeight, 
            List<RoomData> rooms, List<CorridorData> existingCorridors, int corridorWidth = 3)
        {
            if (mapWidth <= 0 || mapHeight <= 0)
                throw new ArgumentException("Map dimensions must be positive");
            
            var obstacles = new bool[mapWidth, mapHeight];
            
            // Mark map boundaries as obstacles
            MarkMapBoundaries(obstacles);
            
            // Mark rooms as obstacles
            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    MarkRoomAsObstacle(obstacles, room);
                }
            }
            
            // Mark existing corridors as obstacles
            if (existingCorridors != null)
            {
                foreach (var corridor in existingCorridors)
                {
                    MarkCorridorAsObstacle(obstacles, corridor, corridorWidth);
                }
            }
            
            return obstacles;
        }
        
        /// <summary>
        /// Updates an existing obstacle map with new rooms and corridors.
        /// </summary>
        public static void UpdateObstacleMap(bool[,] obstacles, List<RoomData> newRooms, 
            List<CorridorData> newCorridors, int corridorWidth = 3)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            // Mark new rooms as obstacles
            if (newRooms != null)
            {
                foreach (var room in newRooms)
                {
                    MarkRoomAsObstacle(obstacles, room);
                }
            }
            
            // Mark new corridors as obstacles
            if (newCorridors != null)
            {
                foreach (var corridor in newCorridors)
                {
                    MarkCorridorAsObstacle(obstacles, corridor, corridorWidth);
                }
            }
        }
        
        /// <summary>
        /// Clears a path from the obstacle map, making it walkable.
        /// </summary>
        public static void ClearPathFromObstacles(bool[,] obstacles, List<Vector2Int> path, int corridorWidth = 3)
        {
            if (obstacles == null || path == null)
                return;
            
            foreach (var tile in path)
            {
                ClearTileFromObstacles(obstacles, tile, corridorWidth);
            }
        }
        
        /// <summary>
        /// Clears a single tile and its surrounding area from obstacles.
        /// </summary>
        public static void ClearTileFromObstacles(bool[,] obstacles, Vector2Int tile, int corridorWidth = 3)
        {
            if (obstacles == null)
                return;
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            int radius = corridorWidth / 2;
            
            for (int x = tile.x - radius; x <= tile.x + radius; x++)
            {
                for (int y = tile.y - radius; y <= tile.y + radius; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        obstacles[x, y] = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if a position is valid and not an obstacle.
        /// </summary>
        public static bool IsValidPosition(Vector2Int position, bool[,] obstacles)
        {
            if (obstacles == null)
                return false;
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            return position.x >= 0 && position.x < width && 
                   position.y >= 0 && position.y < height && 
                   !obstacles[position.x, position.y];
        }
        
        /// <summary>
        /// Checks if a corridor path is valid (doesn't intersect obstacles).
        /// </summary>
        public static bool IsValidCorridorPath(List<Vector2Int> path, bool[,] obstacles, int corridorWidth = 3)
        {
            if (path == null || obstacles == null)
                return false;
            
            foreach (var tile in path)
            {
                if (!IsValidCorridorTile(tile, obstacles, corridorWidth))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Finds valid doorway positions around a room.
        /// </summary>
        public static List<Vector2Int> FindValidDoorwayPositions(RoomData room, bool[,] obstacles, int corridorWidth = 3)
        {
            var doorways = new List<Vector2Int>();
            
            if (room == null || obstacles == null)
                return doorways;
            
            var bounds = room.Bounds;
            
            // Check each edge of the room for potential doorway positions
            // Top edge
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                var pos = new Vector2Int(x, bounds.yMax + 1);
                if (IsValidCorridorTile(pos, obstacles, corridorWidth))
                    doorways.Add(pos);
            }
            
            // Bottom edge
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                var pos = new Vector2Int(x, bounds.yMin - 1);
                if (IsValidCorridorTile(pos, obstacles, corridorWidth))
                    doorways.Add(pos);
            }
            
            // Right edge
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                var pos = new Vector2Int(bounds.xMax + 1, y);
                if (IsValidCorridorTile(pos, obstacles, corridorWidth))
                    doorways.Add(pos);
            }
            
            // Left edge
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                var pos = new Vector2Int(bounds.xMin - 1, y);
                if (IsValidCorridorTile(pos, obstacles, corridorWidth))
                    doorways.Add(pos);
            }
            
            return doorways;
        }
        
        /// <summary>
        /// Expands obstacles to account for corridor width.
        /// </summary>
        public static bool[,] ExpandObstacles(bool[,] obstacles, int expansionRadius)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            var expanded = new bool[width, height];
            
            // Copy original obstacles
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    expanded[x, y] = obstacles[x, y];
                }
            }
            
            // Expand obstacles
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (obstacles[x, y])
                    {
                        // Mark surrounding area as obstacles
                        for (int dx = -expansionRadius; dx <= expansionRadius; dx++)
                        {
                            for (int dy = -expansionRadius; dy <= expansionRadius; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    expanded[nx, ny] = true;
                                }
                            }
                        }
                    }
                }
            }
            
            return expanded;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private static void MarkMapBoundaries(bool[,] obstacles)
        {
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            // Mark top and bottom boundaries
            for (int x = 0; x < width; x++)
            {
                obstacles[x, 0] = true;
                obstacles[x, height - 1] = true;
            }
            
            // Mark left and right boundaries
            for (int y = 0; y < height; y++)
            {
                obstacles[0, y] = true;
                obstacles[width - 1, y] = true;
            }
        }
        
        private static void MarkRoomAsObstacle(bool[,] obstacles, RoomData room)
        {
            if (room == null || obstacles == null)
                return;
            
            var bounds = room.Bounds;
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y <= bounds.yMax; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        obstacles[x, y] = true;
                    }
                }
            }
        }
        
        private static void MarkCorridorAsObstacle(bool[,] obstacles, CorridorData corridor, int corridorWidth)
        {
            if (corridor == null || obstacles == null)
                return;
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            // Get expanded tiles for the corridor
            var expandedTiles = corridor.GetExpandedTiles(corridorWidth / 2);
            
            foreach (var tile in expandedTiles)
            {
                if (tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height)
                {
                    obstacles[tile.x, tile.y] = true;
                }
            }
        }
        
        private static bool IsValidCorridorTile(Vector2Int tile, bool[,] obstacles, int corridorWidth)
        {
            if (obstacles == null)
                return false;
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            int radius = corridorWidth / 2;
            
            // Check if the entire corridor width fits at this position
            for (int x = tile.x - radius; x <= tile.x + radius; x++)
            {
                for (int y = tile.y - radius; y <= tile.y + radius; y++)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height || obstacles[x, y])
                        return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Counts the total number of obstacles in the map.
        /// </summary>
        public static int CountObstacles(bool[,] obstacles)
        {
            if (obstacles == null)
                return 0;
            
            int count = 0;
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (obstacles[x, y])
                        count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Gets the obstacle density (percentage of map that is obstacles).
        /// </summary>
        public static float GetObstacleDensity(bool[,] obstacles)
        {
            if (obstacles == null)
                return 0f;
            
            int totalTiles = obstacles.GetLength(0) * obstacles.GetLength(1);
            int obstacleCount = CountObstacles(obstacles);
            
            return (float)obstacleCount / totalTiles;
        }
        
        /// <summary>
        /// Creates a visual representation of the obstacle map for debugging.
        /// </summary>
        public static string VisualizeObstacleMap(bool[,] obstacles)
        {
            if (obstacles == null)
                return "Obstacle map is null";
            
            int width = obstacles.GetLength(0);
            int height = obstacles.GetLength(1);
            
            var visualization = new System.Text.StringBuilder();
            
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    visualization.Append(obstacles[x, y] ? "█" : "·");
                }
                visualization.AppendLine();
            }
            
            return visualization.ToString();
        }
        
        #endregion
    }
}