using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using NUnit.Framework;
using OfficeMice.MapGeneration.Rendering;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Tests.Factories;

namespace OfficeMice.MapGeneration.Tests.PlayMode
{
    [TestFixture]
    public class TilemapGeneratorPlayModeTests
    {
        private GameObject _testGameObject;
        private Grid _testGrid;
        private Tilemap[] _testTilemaps;
        private TilemapGenerator _tilemapGenerator;
        private TilesetConfiguration _testTileset;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with Grid and Tilemap components
            _testGameObject = new GameObject("PlayModeTestTilemapObject");
            _testGrid = _testGameObject.AddComponent<Grid>();
            _testGrid.cellSize = Vector3.one;
            
            // Create test tilemaps
            _testTilemaps = new Tilemap[3];
            for (int i = 0; i < 3; i++)
            {
                var tilemapGO = new GameObject($"Tilemap_{i}");
                tilemapGO.transform.SetParent(_testGameObject.transform);
                
                var tilemap = tilemapGO.AddComponent<Tilemap>();
                var renderer = tilemapGO.AddComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Individual;
                
                _testTilemaps[i] = tilemap;
            }
            
            // Create test tileset
            _testTileset = MapGenerationTestDataFactory.CreateTestTilesetConfiguration();
            
            // Create tilemap generator
            _tilemapGenerator = new TilemapGenerator(42);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            
            _tilemapGenerator = null;
            _testTilemaps = null;
            _testTileset = null;
        }
        
        [UnityTest]
        public IEnumerator RenderMap_InPlayMode_RendersTilesCorrectly()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(5);
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for one frame to ensure Unity processes the tile changes
            yield return null;
            
            // Assert
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Length > 0, 
                "Floor tiles should be rendered");
            Assert.IsTrue(_testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds).Length > 0, 
                "Wall tiles should be rendered");
            
            // Verify tiles are actually visible in the scene
            var floorRenderer = _testTilemaps[0].GetComponent<TilemapRenderer>();
            var wallRenderer = _testTilemaps[1].GetComponent<TilemapRenderer>();
            
            Assert.IsNotNull(floorRenderer, "Floor tilemap should have renderer");
            Assert.IsNotNull(wallRenderer, "Wall tilemap should have renderer");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator UpdateTiles_InPlayMode_UpdatesTilesVisually()
        {
            // Arrange
            var positions = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) };
            var tiles = new TileBase[] { _testTileset.FloorTiles.Tiles[0].Tile, _testTileset.FloorTiles.Tiles[1].Tile };
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            
            // Wait for Unity to process the changes
            yield return null;
            
            // Assert
            Assert.AreEqual(tiles[0], _testTilemaps[0].GetTile(positions[0]), 
                "First tile should be updated correctly");
            Assert.AreEqual(tiles[1], _testTilemaps[0].GetTile(positions[1]), 
                "Second tile should be updated correctly");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator ClearTilemaps_InPlayMode_ClearsAllVisualTiles()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(3);
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering to complete
            yield return null;
            
            // Verify tiles exist
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Length > 0, 
                "Tiles should exist before clearing");
            
            // Act
            _tilemapGenerator.ClearTilemaps(_testTilemaps);
            
            // Wait for Unity to process the changes
            yield return null;
            
            // Assert
            foreach (var tilemap in _testTilemaps)
            {
                var tiles = tilemap.GetTilesBlock(tilemap.cellBounds);
                foreach (var tile in tiles)
                {
                    Assert.IsNull(tile, "All tiles should be cleared");
                }
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator WorldToGrid_InPlayMode_ConvertsCoordinatesCorrectly()
        {
            // Arrange
            var worldPosition = new Vector3(2.5f, 3.7f, 0f);
            
            // Act
            var gridPosition = _tilemapGenerator.WorldToGrid(worldPosition, _testTilemaps[0]);
            
            // Wait for any Unity processing
            yield return null;
            
            // Assert
            Assert.AreEqual(new Vector3Int(2, 3, 0), gridPosition, 
                "World to grid conversion should work correctly");
            
            // Test reverse conversion
            var convertedBack = _tilemapGenerator.GridToWorld(gridPosition, _testTilemaps[0]);
            Assert.AreEqual(new Vector3(2f, 3f, 0f), convertedBack, 
                "Grid to world conversion should work correctly");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator GetRenderedBounds_InPlayMode_ReturnsCorrectBounds()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(3);
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering to complete
            yield return null;
            
            // Act
            var bounds = _tilemapGenerator.GetRenderedBounds(_testTilemaps[0]);
            
            // Assert
            Assert.IsTrue(bounds.size.magnitude > 0, "Rendered bounds should have size");
            Assert.IsTrue(bounds.center != Vector3.zero, "Rendered bounds should have center");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator OptimizeTileRendering_InPlayMode_OptimizesCorrectly()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(10);
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering to complete
            yield return null;
            
            var initialStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Act
            _tilemapGenerator.OptimizeTileRendering(_testTilemaps[0]);
            
            // Wait for optimization to complete
            yield return null;
            
            // Assert
            var optimizedStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Optimization should not break the rendering
            var tiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            Assert.IsTrue(tiles.Length > 0, "Tiles should still exist after optimization");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator TilemapCompressor_InPlayMode_CompressesTilesCorrectly()
        {
            // Arrange
            var compressor = _testTilemaps[0].gameObject.AddComponent<TilemapCompressor>();
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(5);
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering to complete
            yield return null;
            
            var initialTileCount = 0;
            var initialTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            foreach (var tile in initialTiles)
            {
                if (tile != null) initialTileCount++;
            }
            
            // Act
            compressor.Compress();
            
            // Wait for compression to complete
            yield return null;
            
            // Assert
            var finalTileCount = 0;
            var finalTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            foreach (var tile in finalTiles)
            {
                if (tile != null) finalTileCount++;
            }
            
            Assert.IsTrue(finalTileCount > 0, "Tiles should still exist after compression");
            Assert.IsTrue(compressor.CompressionRatio > 0, "Compression ratio should be calculated");
            
            Debug.Log($"Compression: {initialTileCount} -> {finalTileCount} tiles (Ratio: {compressor.CompressionRatio:P1})");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Events_InPlayMode_FireCorrectly()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(3);
            var tileRenderedFired = false;
            var renderingCompletedFired = false;
            
            _tilemapGenerator.OnTileRendered += (pos, tile) => tileRenderedFired = true;
            _tilemapGenerator.OnRenderingCompleted += (map) => renderingCompletedFired = true;
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering and event processing
            yield return null;
            
            // Assert
            Assert.IsTrue(tileRenderedFired, "Tile rendered event should fire");
            Assert.IsTrue(renderingCompletedFired, "Rendering completed event should fire");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Performance_InPlayMode_MeetsTargets()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateLargeTestMapData(50);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            stopwatch.Stop();
            
            // Wait for rendering to complete
            yield return null;
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 150, 
                "Large map rendering should complete within 150ms in play mode");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.IsTrue(stats.IsWithinPerformanceTargets, 
                "Should be within performance targets");
            
            Debug.Log($"Play Mode Performance - Time: {stopwatch.ElapsedMilliseconds}ms, Stats: {stats}");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator MultipleLayers_InPlayMode_RenderCorrectly()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(5);
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Wait for rendering to complete
            yield return null;
            
            // Assert
            // Check that different layers have different content
            var floorTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            var wallTiles = _testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds);
            var objectTiles = _testTilemaps[2].GetTilesBlock(_testTilemaps[2].cellBounds);
            
            var floorCount = 0;
            var wallCount = 0;
            var objectCount = 0;
            
            foreach (var tile in floorTiles) if (tile != null) floorCount++;
            foreach (var tile in wallTiles) if (tile != null) wallCount++;
            foreach (var tile in objectTiles) if (tile != null) objectCount++;
            
            Assert.Greater(floorCount, 0, "Floor layer should have tiles");
            Assert.Greater(wallCount, 0, "Wall layer should have tiles");
            // Object tiles are optional
            
            // Verify layer ordering in the hierarchy
            Assert.IsTrue(_testTilemaps[0].transform.GetSiblingIndex() < _testTilemaps[1].transform.GetSiblingIndex(), 
                "Floor layer should be below wall layer");
            Assert.IsTrue(_testTilemaps[1].transform.GetSiblingIndex() < _testTilemaps[2].transform.GetSiblingIndex(), 
                "Wall layer should be below object layer");
            
            Debug.Log($"Layer Counts - Floor: {floorCount}, Wall: {wallCount}, Object: {objectCount}");
            
            yield return null;
        }
    }
}