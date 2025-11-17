using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of ITileRenderer for testing purposes.
    /// Provides configurable tile rendering behavior for unit testing.
    /// </summary>
    public class MockTileRenderer : ITileRenderer
    {
        private List<Vector3Int> _renderedTiles;
        private List<TileBase> _renderedTileTypes;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;
        private Bounds _mockBounds;

        public event Action<Vector3Int, TileBase> OnTileRendered;
        public event Action<MapData> OnRenderingCompleted;
        public event Action<MapData, Exception> OnRenderingFailed;

        public MockTileRenderer()
        {
            _renderedTiles = new List<Vector3Int>();
            _renderedTileTypes = new List<TileBase>();
            _shouldThrowException = false;
            _exceptionToThrow = new InvalidOperationException("Mock tile rendering failed");
            _mockValidationResult = ValidationResult.Success();
            _mockBounds = new Bounds(Vector3.zero, Vector3.one * 50);
        }

        /// <summary>
        /// Gets the list of rendered tile positions.
        /// </summary>
        public List<Vector3Int> RenderedTiles => new List<Vector3Int>(_renderedTiles);

        /// <summary>
        /// Gets the list of rendered tile types.
        /// </summary>
        public List<TileBase> RenderedTileTypes => new List<TileBase>(_renderedTileTypes);

        /// <summary>
        /// Sets the mock bounds to return from GetRenderedBounds.
        /// </summary>
        public void SetMockBounds(Bounds bounds)
        {
            _mockBounds = bounds;
        }

        /// <summary>
        /// Configures the mock to throw an exception during rendering.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock tile rendering failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        /// <summary>
        /// Clears the recorded rendering data.
        /// </summary>
        public void ClearRenderData()
        {
            _renderedTiles.Clear();
            _renderedTileTypes.Clear();
        }

        public void RenderMap(MapData map, Tilemap[] tilemaps)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (tilemaps == null)
                throw new ArgumentNullException(nameof(tilemaps));
            if (tilemaps.Length == 0)
                throw new ArgumentException("Tilemaps array cannot be empty");

            if (_shouldThrowException)
            {
                OnRenderingFailed?.Invoke(map, _exceptionToThrow);
                throw _exceptionToThrow;
            }

            // Simulate rendering tiles for each room
            foreach (var room in map.Rooms)
            {
                RenderRoom(room, tilemaps[0], null); // Use null for tileset in mock
            }

            OnRenderingCompleted?.Invoke(map);
        }

        public void RenderRoom(RoomData room, Tilemap tilemap, TilesetConfiguration tileset)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            if (_shouldThrowException)
            {
                throw _exceptionToThrow;
            }

            // Simulate rendering room tiles
            for (int x = room.Bounds.x; x < room.Bounds.x + room.Bounds.width; x++)
            {
                for (int y = room.Bounds.y; y < room.Bounds.y + room.Bounds.height; y++)
                {
                    var position = new Vector3Int(x, y, 0);
                    var tile = CreateMockTile();
                    
                    _renderedTiles.Add(position);
                    _renderedTileTypes.Add(tile);
                    OnTileRendered?.Invoke(position, tile);
                }
            }
        }

        public void RenderCorridor(CorridorData corridor, Tilemap tilemap, TilesetConfiguration tileset)
        {
            if (corridor == null)
                throw new ArgumentNullException(nameof(corridor));
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            if (_shouldThrowException)
            {
                throw _exceptionToThrow;
            }

            // Simulate rendering corridor tiles
            foreach (var pathTile in corridor.PathTiles)
            {
                var tilePos = new Vector3Int(pathTile.x, pathTile.y, 0);
                var tile = CreateMockTile();
                
                _renderedTiles.Add(tilePos);
                _renderedTileTypes.Add(tile);
                OnTileRendered?.Invoke(tilePos, tile);
            }
        }

        public void ClearTilemaps(Tilemap[] tilemaps)
        {
            if (tilemaps == null)
                throw new ArgumentNullException(nameof(tilemaps));

            ClearRenderData();
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

            if (_shouldThrowException)
            {
                throw _exceptionToThrow;
            }

            for (int i = 0; i < positions.Length; i++)
            {
                _renderedTiles.Add(positions[i]);
                _renderedTileTypes.Add(tiles[i]);
                OnTileRendered?.Invoke(positions[i], tiles[i]);
            }
        }

        public ValidationResult ValidateTilemapSetup(Tilemap[] tilemaps)
        {
            if (tilemaps == null)
                return ValidationResult.Failure("Tilemaps array cannot be null");
            if (tilemaps.Length == 0)
                return ValidationResult.Failure("Tilemaps array cannot be empty");

            return _mockValidationResult;
        }

        public void OptimizeTileRendering(Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            // Mock optimization - no actual implementation needed
        }

        public Bounds GetRenderedBounds(Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            return _mockBounds;
        }

        public Vector3Int WorldToGrid(Vector3 worldPosition, Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            // Mock conversion
            return Vector3Int.RoundToInt(worldPosition);
        }

        public Vector3 GridToWorld(Vector3Int gridPosition, Tilemap tilemap)
        {
            if (tilemap == null)
                throw new ArgumentNullException(nameof(tilemap));

            // Mock conversion
            return gridPosition;
        }

        private TileBase CreateMockTile()
        {
            // Create a simple mock tile for testing
            return ScriptableObject.CreateInstance<TileBase>();
        }
    }
}