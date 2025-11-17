using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Unity Tilemap rendering abstraction for visualizing generated maps.
    /// Handles tile placement, layer management, and visual updates.
    /// </summary>
    public interface ITileRenderer
    {
        /// <summary>
        /// Renders a complete map onto the provided tilemaps.
        /// </summary>
        /// <param name="map">Map data to render</param>
        /// <param name="tilemaps">Array of tilemaps for different layers</param>
        /// <exception cref="ArgumentNullException">Thrown when map or tilemaps is null</exception>
        /// <exception cref="ArgumentException">Thrown when tilemaps array is empty</exception>
        void RenderMap(MapData map, Tilemap[] tilemaps);

        /// <summary>
        /// Renders a single room onto the specified tilemap.
        /// </summary>
        /// <param name="room">Room data to render</param>
        /// <param name="tilemap">Target tilemap</param>
        /// <param name="tileset">Tileset configuration for rendering</param>
        void RenderRoom(RoomData room, Tilemap tilemap, TilesetConfiguration tileset);

        /// <summary>
        /// Renders a corridor onto the specified tilemap.
        /// </summary>
        /// <param name="corridor">Corridor data to render</param>
        /// <param name="tilemap">Target tilemap</param>
        /// <param name="tileset">Tileset configuration for rendering</param>
        void RenderCorridor(CorridorData corridor, Tilemap tilemap, TilesetConfiguration tileset);

        /// <summary>
        /// Clears all tiles from the provided tilemaps.
        /// </summary>
        /// <param name="tilemaps">Tilemaps to clear</param>
        void ClearTilemaps(Tilemap[] tilemaps);

        /// <summary>
        /// Updates specific tiles that have changed.
        /// </summary>
        /// <param name="positions">Positions of tiles to update</param>
        /// <param name="tiles">New tiles to place</param>
        /// <param name="tilemap">Target tilemap</param>
        void UpdateTiles(Vector3Int[] positions, TileBase[] tiles, Tilemap tilemap);

        /// <summary>
        /// Validates that the tilemap setup is correct for rendering.
        /// </summary>
        /// <param name="tilemaps">Tilemaps to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        ValidationResult ValidateTilemapSetup(Tilemap[] tilemaps);

        /// <summary>
        /// Optimizes tile rendering by combining adjacent tiles of the same type.
        /// </summary>
        /// <param name="tilemap">Tilemap to optimize</param>
        void OptimizeTileRendering(Tilemap tilemap);

        /// <summary>
        /// Gets the bounds of the rendered area.
        /// </summary>
        /// <param name="tilemap">Tilemap to check</param>
        /// <returns>Bounds of the rendered content</returns>
        Bounds GetRenderedBounds(Tilemap tilemap);

        /// <summary>
        /// Converts world position to tilemap grid position.
        /// </summary>
        /// <param name="worldPosition">World position to convert</param>
        /// <param name="tilemap">Target tilemap</param>
        /// <returns>Grid position on the tilemap</returns>
        Vector3Int WorldToGrid(Vector3 worldPosition, Tilemap tilemap);

        /// <summary>
        /// Converts tilemap grid position to world position.
        /// </summary>
        /// <param name="gridPosition">Grid position to convert</param>
        /// <param name="tilemap">Source tilemap</param>
        /// <returns>World position</returns>
        Vector3 GridToWorld(Vector3Int gridPosition, Tilemap tilemap);

        /// <summary>
        /// Event fired when a tile is rendered.
        /// </summary>
        event Action<Vector3Int, TileBase> OnTileRendered;

        /// <summary>
        /// Event fired when rendering completes.
        /// </summary>
        event Action<MapData> OnRenderingCompleted;

        /// <summary>
        /// Event fired when rendering fails.
        /// </summary>
        event Action<MapData, Exception> OnRenderingFailed;
    }
}