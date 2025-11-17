using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Base
{
    /// <summary>
    /// Base class for PlayMode tests that require Unity runtime systems.
    /// Provides setup for GameObjects, Tilemaps, and other Unity components.
    /// </summary>
    public abstract class PlayModeTestBase : BaseTestFixture
    {
        protected GameObject _testGameObject;
        protected GameObject _tilemapGameObject;
        protected UnityEngine.Tilemaps.Tilemap _groundTilemap;
        protected UnityEngine.Tilemaps.Tilemap _wallTilemap;
        protected UnityEngine.Tilemaps.TilemapRenderer _groundRenderer;
        protected UnityEngine.Tilemaps.TilemapRenderer _wallRenderer;

        [UnitySetUp]
        public virtual IEnumerator UnitySetUp()
        {
            // Create test GameObject
            _testGameObject = new GameObject("TestGameObject");
            _testGameObject.transform.position = Vector3.zero;

            // Setup Tilemap hierarchy
            yield return SetupTilemapHierarchy();

            // Wait for one frame to ensure Unity systems are initialized
            yield return null;
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            // Cleanup Tilemap objects
            if (_tilemapGameObject != null)
            {
                Object.DestroyImmediate(_tilemapGameObject);
                _tilemapGameObject = null;
            }

            // Cleanup test GameObject
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
                _testGameObject = null;
            }

            // Reset references
            _groundTilemap = null;
            _wallTilemap = null;
            _groundRenderer = null;
            _wallRenderer = null;

            // Wait for one frame to ensure cleanup is complete
            yield return null;
        }

        /// <summary>
        /// Sets up the Tilemap hierarchy for testing.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator SetupTilemapHierarchy()
        {
            _tilemapGameObject = new GameObject("Tilemaps");
            
            // Create Grid component
            var grid = _tilemapGameObject.AddComponent<UnityEngine.Tilemaps.Grid>();
            grid.cellSize = Vector3.one;

            // Create Ground Tilemap
            var groundGameObject = new GameObject("GroundTilemap");
            groundGameObject.transform.SetParent(_tilemapGameObject.transform);
            _groundTilemap = groundGameObject.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            _groundRenderer = groundGameObject.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            
            var groundTilemapCollider = groundGameObject.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            groundTilemapCollider.usedByComposite = false;

            // Create Wall Tilemap
            var wallGameObject = new GameObject("WallTilemap");
            wallGameObject.transform.SetParent(_tilemapGameObject.transform);
            _wallTilemap = wallGameObject.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            _wallRenderer = wallGameObject.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            
            var wallTilemapCollider = wallGameObject.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            wallTilemapCollider.usedByComposite = true;

            var compositeCollider2D = wallGameObject.AddComponent<UnityEngine.CompositeCollider2D>();
            wallTilemapCollider.usedByComposite = true;
            compositeCollider2D.geometryType = UnityEngine.CompositeCollider2D.GeometryType.Polygons;

            yield return null;
        }

        /// <summary>
        /// Clears all tiles from the tilemaps.
        /// </summary>
        protected void ClearTilemaps()
        {
            if (_groundTilemap != null)
                _groundTilemap.ClearAllTiles();
            
            if (_wallTilemap != null)
                _wallTilemap.ClearAllTiles();
        }

        /// <summary>
        /// Gets the tilemap array for rendering tests.
        /// </summary>
        /// <returns>Array containing ground and wall tilemaps</returns>
        protected UnityEngine.Tilemaps.Tilemap[] GetTilemapArray()
        {
            return new UnityEngine.Tilemaps.Tilemap[] { _groundTilemap, _wallTilemap };
        }

        /// <summary>
        /// Asserts that tilemaps are properly initialized.
        /// </summary>
        protected void AssertTilemapsInitialized()
        {
            Assert.IsNotNull(_groundTilemap, "Ground tilemap should be initialized");
            Assert.IsNotNull(_wallTilemap, "Wall tilemap should be initialized");
            Assert.IsNotNull(_groundRenderer, "Ground renderer should be initialized");
            Assert.IsNotNull(_wallRenderer, "Wall renderer should be initialized");
        }

        /// <summary>
        /// Asserts that a map can be rendered to tilemaps without errors.
        /// </summary>
        /// <param name="mapData">Map data to render</param>
        /// <param name="renderer">Tile renderer implementation</param>
        /// <returns>IEnumerator for coroutine</returns>
        protected IEnumerator AssertMapRendersToTilemaps(MapData mapData, ITileRenderer renderer)
        {
            AssertTilemapsInitialized();
            Assert.IsNotNull(mapData, "Map data should not be null");
            Assert.IsNotNull(renderer, "Tile renderer should not be null");

            ClearTilemaps();

            // Render the map
            var tilemaps = GetTilemapArray();
            renderer.RenderMap(mapData, tilemaps);

            // Wait for one frame to allow Unity to process the changes
            yield return null;

            // Basic validation - check that tiles were placed
            Assert.IsTrue(_groundTilemap.GetUsedTilesCount() > 0 || _wallTilemap.GetUsedTilesCount() > 0, 
                "At least one tilemap should have tiles after rendering");
        }

        /// <summary>
        /// Creates a simple test tile for rendering tests.
        /// </summary>
        /// <param name="name">Tile name</param>
        /// <param name="color">Tile color</param>
        /// <returns>Test tile</returns>
        protected UnityEngine.Tilemaps.TileBase CreateTestTile(string name, Color color)
        {
            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            tile.name = name;
            
            // Create a simple sprite for the tile
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            tile.sprite = sprite;
            
            return tile;
        }

        /// <summary>
        /// Sets up basic test tiles for rendering.
        /// </summary>
        /// <returns>Dictionary of tile names to tile instances</returns>
        protected Dictionary<string, UnityEngine.Tilemaps.TileBase> SetupTestTiles()
        {
            var tiles = new Dictionary<string, UnityEngine.Tilemaps.TileBase>();
            
            tiles["ground"] = CreateTestTile("GroundTile", Color.gray);
            tiles["wall"] = CreateTestTile("WallTile", Color.red);
            tiles["floor"] = CreateTestTile("FloorTile", Color.white);
            
            return tiles;
        }
    }
}