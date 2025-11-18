using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Rendering
{
    /// <summary>
    /// Unity Tilemap rendering implementation for visualizing generated maps.
    /// Handles efficient batch operations, multi-layer support, and room-type specific tiles.
    /// </summary>
    public class TilemapGenerator : ITileRenderer
    {
        #region Events
        
        public event Action<Vector3Int, TileBase> OnTileRendered;
        public event Action<MapData> OnRenderingCompleted;
        public event Action<MapData, Exception> OnRenderingFailed;
        
        #endregion
        
        #region Private Fields
        
        private readonly System.Random _random;
        private readonly Dictionary<TileType, TileBase> _tileCache;
        private readonly List<Vector3Int> _batchPositions;
        private readonly List<TileBase> _batchTiles;
        private TilesetConfiguration _currentTileset;
        
        // Performance tracking
        private int _tilesRenderedThisFrame;
        private int _batchOperationsThisFrame;
        private const int MAX_TILES_PER_FRAME = 1000;
        private const int MAX_BATCH_OPERATIONS_PER_FRAME = 50;
        
        #endregion
        
        #region Constructor
        
        public TilemapGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            _tileCache = new Dictionary<TileType, TileBase>();
            _batchPositions = new List<Vector3Int>();
            _batchTiles = new List<TileBase>();
        }
        
        #endregion
        
        #region ITileRenderer Implementation
        
        public void RenderMap(MapData map, Tilemap[] tilemaps)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
                
            if (tilemaps == null || tilemaps.Length == 0)
                throw new ArgumentException("Tilemaps array cannot be null or empty", nameof(tilemaps));
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Validate tilemap setup
                var validationResult = ValidateTilemapSetup(tilemaps);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Invalid tilemap setup: {string.Join(", ", validationResult.Errors)}");
                }
                
                // Clear existing tiles
                ClearTilemaps(tilemaps);
                
                // Bind tilemaps to map data
                if (tilemaps.Length >= 3)
                {
                    map.BindTilemaps(tilemaps[0], tilemaps[1], tilemaps[2]);
                }
                
                // Get tileset configuration
                _currentTileset = GetTilesetConfiguration();
                if (_currentTileset == null)
                {
                    throw new InvalidOperationException("No TilesetConfiguration found. Please assign one in the project settings.");
                }
                
                // Reset performance counters
                _tilesRenderedThisFrame = 0;
                _batchOperationsThisFrame = 0;
                
                // Render floors first (layer 0)
                if (tilemaps.Length > 0)
                {
                    RenderFloors(map, tilemaps[0]);
                }
                
                // Render walls second (layer 1)
                if (tilemaps.Length > 1)
                {
                    RenderWalls(map, tilemaps[1]);
                }
                
                // Render objects/decorations third (layer 2)
                if (tilemaps.Length > 2)
                {
                    RenderDecorations(map, tilemaps[2]);
                }
                
                // Optimize all tilemaps
                foreach (var tilemap in tilemaps)
                {
                    OptimizeTileRendering(tilemap);
                }
                
                stopwatch.Stop();
                
                // Check performance targets
                if (stopwatch.ElapsedMilliseconds > 150)
                {
                    Debug.LogWarning($"Tilemap rendering took {stopwatch.ElapsedMilliseconds}ms, exceeding the 150ms target");
                }
                
                OnRenderingCompleted?.Invoke(map);
                Debug.Log($"Successfully rendered map {map.MapID} with {map.Rooms.Count} rooms and {map.Corridors.Count} corridors in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                OnRenderingFailed?.Invoke(map, ex);
                Debug.LogError($"Failed to render map {map.MapID}: {ex.Message}");
                throw;
            }
        }
        
        public void RenderRoom(RoomData room, Tilemap tilemap, TilesetConfiguration tileset)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));
                
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));
                
            if (tileset == null)
                throw new ArgumentNullException(nameof(tileset));
            
            _currentTileset = tileset;
            
            // Render floor tiles using batch operation
            RenderRoomFloors(room, tilemap);
            
            // Render wall perimeter
            RenderRoomWalls(room, tilemap);
        }
        
        public void RenderCorridor(CorridorData corridor, Tilemap tilemap, TilesetConfiguration tileset)
        {
            if (corridor == null)
                throw new ArgumentNullException(nameof(corridor));
                
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));
                
            if (tileset == null)
                throw new ArgumentNullException(nameof(tileset));
            
            _currentTileset = tileset;
            
            // Render corridor floor tiles
            RenderCorridorFloors(corridor, tilemap);
            
            // Render corridor walls
            RenderCorridorWalls(corridor, tilemap);
        }
        
        public void ClearTilemaps(Tilemap[] tilemaps)
        {
            if (tilemaps == null)
                return;
                
            foreach (var tilemap in tilemaps)
            {
                if (tilemap != null)
                {
                    tilemap.ClearAllTiles();
                }
            }
        }
        
        public void UpdateTiles(Vector3Int[] positions, TileBase[] tiles, Tilemap tilemap)
        {
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));
                
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));
                
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));
                
            if (positions.Length != tiles.Length)
                throw new ArgumentException("Positions and tiles arrays must have the same length");
            
            // Use batch operation for efficiency
            for (int i = 0; i < positions.Length; i++)
            {
                _batchPositions.Add(positions[i]);
                _batchTiles.Add(tiles[i]);
                
                // Fire event for each tile
                OnTileRendered?.Invoke(positions[i], tiles[i]);
            }
            
            // Execute batch operation
            if (_batchPositions.Count > 0)
            {
                tilemap.SetTiles(_batchPositions.ToArray(), _batchTiles.ToArray());
                _batchPositions.Clear();
                _batchTiles.Clear();
                _batchOperationsThisFrame++;
            }
        }
        
        public ValidationResult ValidateTilemapSetup(Tilemap[] tilemaps)
        {
            var result = new ValidationResult();
            
            if (tilemaps == null)
            {
                result.AddError("Tilemaps array is null");
                return result;
            }
            
            if (tilemaps.Length == 0)
            {
                result.AddError("Tilemaps array is empty");
                return result;
            }
            
            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (tilemaps[i] == null)
                {
                    result.AddError($"Tilemap at index {i} is null");
                    continue;
                }
                
                // Check if tilemap has a grid component
                var grid = tilemaps[i].GetComponentInParent<Grid>();
                if (grid == null)
                {
                    result.AddError($"Tilemap '{tilemaps[i].name}' has no parent Grid component");
                }
                
                // Check if tilemap has proper layout
                var tilemapRenderer = tilemaps[i].GetComponent<TilemapRenderer>();
                if (tilemapRenderer == null)
                {
                    result.AddWarning($"Tilemap '{tilemaps[i].name}' has no TilemapRenderer component");
                }
            }
            
            return result;
        }
        
        public void OptimizeTileRendering(Tilemap tilemap)
        {
            if (tilemap == null)
                return;
            
            // Force tilemap to update its internal data
            tilemap.RefreshAllTiles();
            
            // Compress tilemap if possible
            var tilemapCompressor = tilemap.GetComponent<TilemapCompressor>();
            if (tilemapCompressor != null)
            {
                tilemapCompressor.Compress();
            }
            
            // Optimize collider if present
            var tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider != null)
            {
                tilemapCollider.ProcessTilemapChanges();
            }
        }
        
        public Bounds GetRenderedBounds(Tilemap tilemap)
        {
            if (tilemap == null)
                return new Bounds();
            
            var cellBounds = tilemap.cellBounds;
            return new Bounds(
                tilemap.GetCellCenterWorld(new Vector3Int(cellBounds.x, cellBounds.y, 0)),
                new Vector3(cellBounds.size.x, cellBounds.size.y, 1)
            );
        }
        
        public Vector3Int WorldToGrid(Vector3 worldPosition, Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));
            
            return tilemap.WorldToCell(worldPosition);
        }
        
        public Vector3 GridToWorld(Vector3Int gridPosition, Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));
            
            return tilemap.GetCellCenterWorld(gridPosition);
        }
        
        #endregion
        
        #region Private Rendering Methods
        
        private void RenderFloors(MapData map, Tilemap floorTilemap)
        {
            // Render all room floors
            foreach (var room in map.Rooms)
            {
                RenderRoomFloors(room, floorTilemap);
            }
            
            // Render all corridor floors
            foreach (var corridor in map.Corridors)
            {
                RenderCorridorFloors(corridor, floorTilemap);
            }
        }
        
        private void RenderWalls(MapData map, Tilemap wallTilemap)
        {
            // Render all room walls
            foreach (var room in map.Rooms)
            {
                RenderRoomWalls(room, wallTilemap);
            }
            
            // Render all corridor walls
            foreach (var corridor in map.Corridors)
            {
                RenderCorridorWalls(corridor, wallTilemap);
            }
        }
        
        private void RenderDecorations(MapData map, Tilemap objectTilemap)
        {
            // Render room-specific decorations based on classification
            foreach (var room in map.Rooms)
            {
                RenderRoomDecorations(room, objectTilemap);
            }
        }
        
        private void RenderRoomFloors(RoomData room, Tilemap tilemap)
        {
            var floorTile = GetTileForRoomType(TileType.Floor, room.Classification);
            if (floorTile == null)
                return;
            
            // Use BoxFill for batch operation efficiency
            var bounds = room.Bounds;
            var minPos = new Vector3Int(bounds.x, bounds.y, 0);
            var maxPos = new Vector3Int(bounds.xMax - 1, bounds.yMax - 1, 0);
            
            tilemap.BoxFill(minPos, floorTile, minPos.x, minPos.y, maxPos.x, maxPos.y);
            
            // Fire events for the filled area (sample a few points to avoid spam)
            var centerPos = new Vector3Int((int)bounds.center.x, (int)bounds.center.y, 0);
            OnTileRendered?.Invoke(centerPos, floorTile);
            
            _tilesRenderedThisFrame += bounds.width * bounds.height;
            _batchOperationsThisFrame++;
        }
        
        private void RenderRoomWalls(RoomData room, Tilemap tilemap)
        {
            var wallTile = GetTileForRoomType(TileType.Wall, room.Classification);
            if (wallTile == null)
                return;
            
            var bounds = room.Bounds;
            var wallPositions = new List<Vector3Int>();
            
            // Top wall
            for (int x = bounds.x - 1; x <= bounds.xMax; x++)
            {
                wallPositions.Add(new Vector3Int(x, bounds.yMax, 0));
            }
            
            // Bottom wall
            for (int x = bounds.x - 1; x <= bounds.xMax; x++)
            {
                wallPositions.Add(new Vector3Int(x, bounds.y - 1, 0));
            }
            
            // Left wall
            for (int y = bounds.y; y < bounds.yMax; y++)
            {
                wallPositions.Add(new Vector3Int(bounds.x - 1, y, 0));
            }
            
            // Right wall
            for (int y = bounds.y; y < bounds.yMax; y++)
            {
                wallPositions.Add(new Vector3Int(bounds.xMax, y, 0));
            }
            
            // Batch set wall tiles
            var tiles = new TileBase[wallPositions.Count];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = wallTile;
            }
            
            tilemap.SetTiles(wallPositions.ToArray(), tiles);
            
            // Fire events for a few wall tiles
            if (wallPositions.Count > 0)
            {
                OnTileRendered?.Invoke(wallPositions[0], wallTile);
            }
            
            _tilesRenderedThisFrame += wallPositions.Count;
            _batchOperationsThisFrame++;
        }
        
        private void RenderCorridorFloors(CorridorData corridor, Tilemap tilemap)
        {
            var floorTile = _currentTileset.GetTileForType(TileType.Floor, _random);
            if (floorTile == null)
                return;
            
            var positions = corridor.PathTiles.Select(p => new Vector3Int(p.x, p.y, 0)).ToArray();
            var tiles = new TileBase[positions.Length];
            
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = floorTile;
            }
            
            tilemap.SetTiles(positions, tiles);
            
            // Fire event for first tile
            if (positions.Length > 0)
            {
                OnTileRendered?.Invoke(positions[0], floorTile);
            }
            
            _tilesRenderedThisFrame += positions.Length;
            _batchOperationsThisFrame++;
        }
        
        private void RenderCorridorWalls(CorridorData corridor, Tilemap tilemap)
        {
            var wallTile = _currentTileset.GetTileForType(TileType.Wall, _random);
            if (wallTile == null)
                return;
            
            var wallPositions = new HashSet<Vector3Int>();
            
            // Calculate wall positions around corridor path
            foreach (var pathTile in corridor.PathTiles)
            {
                // Check all 8 directions around each path tile
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip the center tile
                        
                        var wallPos = new Vector3Int(pathTile.x + dx, pathTile.y + dy, 0);
                        
                        // Only add wall if it's not part of the corridor path
                        if (!corridor.ContainsTile(new Vector2Int(wallPos.x, wallPos.y)))
                        {
                            wallPositions.Add(wallPos);
                        }
                    }
                }
            }
            
            // Batch set wall tiles
            if (wallPositions.Count > 0)
            {
                var positions = wallPositions.ToArray();
                var tiles = new TileBase[positions.Length];
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = wallTile;
                }
                
                tilemap.SetTiles(positions, tiles);
                
                // Fire event for first wall tile
                OnTileRendered?.Invoke(positions[0], wallTile);
            }
            
            _tilesRenderedThisFrame += wallPositions.Count;
            _batchOperationsThisFrame++;
        }
        
        private void RenderRoomDecorations(RoomData room, Tilemap tilemap)
        {
            // Add room-type specific decorations
            var decorationTile = GetDecorationForRoomType(room.Classification);
            if (decorationTile == null)
                return;
            
            // Place decorations randomly within the room
            var decorationCount = Mathf.FloorToInt(room.Area * _currentTileset.DecorationDensity);
            var placedPositions = new HashSet<Vector2Int>();
            
            for (int i = 0; i < decorationCount; i++)
            {
                var x = _random.Next(room.Bounds.x, room.Bounds.xMax);
                var y = _random.Next(room.Bounds.y, room.Bounds.yMax);
                var pos = new Vector2Int(x, y);
                
                // Avoid placing decorations on the same tile
                if (!placedPositions.Contains(pos))
                {
                    placedPositions.Add(pos);
                    var gridPos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(gridPos, decorationTile);
                    OnTileRendered?.Invoke(gridPos, decorationTile);
                }
            }
            
            _tilesRenderedThisFrame += placedPositions.Count;
        }
        
        #endregion
        
        #region Tile Selection Methods
        
        private TileBase GetTileForRoomType(TileType tileType, RoomClassification classification)
        {
            // For now, just use the tile type for caching (room classification can be added later with a different cache structure)
            if (_tileCache.TryGetValue(tileType, out var cachedTile))
            {
                return cachedTile;
            }

            // Get base tile from tileset configuration
            var tile = _currentTileset.GetTileForType(tileType, _random);

            // Apply room-type specific modifications if needed
            tile = ApplyRoomTypeModification(tile, tileType, classification);

            // Cache the result
            _tileCache[tileType] = tile;

            return tile;
        }
        
        private TileBase GetDecorationForRoomType(RoomClassification classification)
        {
            // Get decorative tile based on room classification
            var decorationTile = _currentTileset.GetDecorativeTile(_random);
            
            // Apply room-type specific modifications
            return ApplyRoomTypeModification(decorationTile, TileType.Decoration, classification);
        }
        
        private TileBase ApplyRoomTypeModification(TileBase baseTile, TileType tileType, RoomClassification classification)
        {
            if (baseTile == null)
                return null;
            
            // Apply room-type specific modifications
            switch (classification)
            {
                case RoomClassification.Office:
                    // Standard office tiles - no modification needed
                    break;
                    
                case RoomClassification.Conference:
                    // Conference rooms might have different floor patterns
                    if (tileType == TileType.Floor && _random.NextDouble() < 0.3f)
                    {
                        // Return a variant tile for conference rooms
                        return _currentTileset.GetTileForType(TileType.Floor, _random);
                    }
                    break;
                    
                case RoomClassification.ServerRoom:
                    // Server rooms need special tiles
                    if (tileType == TileType.Floor)
                    {
                        // Use a more technical-looking floor tile
                        return _currentTileset.GetTileForType(TileType.Floor, _random);
                    }
                    break;
                    
                case RoomClassification.BossRoom:
                    // Boss rooms get premium tiles
                    if (_random.NextDouble() < 0.5f)
                    {
                        return _currentTileset.GetTileForType(tileType, _random);
                    }
                    break;
            }
            
            return baseTile;
        }
        
        private TilesetConfiguration GetTilesetConfiguration()
        {
            // Try to find tileset configuration in resources
            var tilesets = Resources.LoadAll<TilesetConfiguration>("Tilesets");
            if (tilesets.Length > 0)
            {
                return tilesets[0];
            }
            
            // Try to find it in the project
            return UnityEngine.Object.FindObjectOfType<TilesetConfiguration>();
        }
        
        #endregion
        
        #region Performance Monitoring
        
        public RenderingStatistics GetRenderingStatistics()
        {
            return new RenderingStatistics
            {
                TilesRenderedThisFrame = _tilesRenderedThisFrame,
                BatchOperationsThisFrame = _batchOperationsThisFrame,
                CachedTiles = _tileCache.Count,
                IsWithinPerformanceTargets = _tilesRenderedThisFrame <= MAX_TILES_PER_FRAME && 
                                           _batchOperationsThisFrame <= MAX_BATCH_OPERATIONS_PER_FRAME
            };
        }
        
        public void ResetPerformanceCounters()
        {
            _tilesRenderedThisFrame = 0;
            _batchOperationsThisFrame = 0;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics for monitoring rendering performance.
    /// </summary>
    [Serializable]
    public class RenderingStatistics
    {
        public int TilesRenderedThisFrame;
        public int BatchOperationsThisFrame;
        public int CachedTiles;
        public bool IsWithinPerformanceTargets;
        
        public override string ToString()
        {
            return $"Tiles: {TilesRenderedThisFrame}, Batches: {BatchOperationsThisFrame}, Cached: {CachedTiles}, Within Targets: {IsWithinPerformanceTargets}";
        }
    }
}