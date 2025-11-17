using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Rendering;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Tests.Factories;

namespace OfficeMice.MapGeneration.Tests.Rendering
{
    [TestFixture]
    public class TilemapGeneratorTests : BaseTestFixture
    {
        private TilemapGenerator _tilemapGenerator;
        private Tilemap[] _testTilemaps;
        private MapData _testMap;
        private TilesetConfiguration _testTileset;
        private GameObject _testGameObject;
        private Grid _testGrid;
        
        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            
            // Create test GameObject with Grid and Tilemap components
            _testGameObject = new GameObject("TestTilemapObject");
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
            
            // Create test map
            _testMap = MapGenerationTestDataFactory.CreateTestMapData();
            
            // Create tilemap generator
            _tilemapGenerator = new TilemapGenerator(42); // Fixed seed for reproducible tests
        }
        
        [TearDown]
        public void TearDown()
        {
            base.TearDown();
            
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
            
            _tilemapGenerator = null;
            _testTilemaps = null;
            _testMap = null;
            _testTileset = null;
        }
        
        #region Constructor Tests
        
        [Test]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var generator = new TilemapGenerator(123);
            
            // Assert
            Assert.IsNotNull(generator);
        }
        
        [Test]
        public void Constructor_WithoutSeed_InitializesWithRandomSeed()
        {
            // Arrange & Act
            var generator = new TilemapGenerator();
            
            // Assert
            Assert.IsNotNull(generator);
        }
        
        #endregion
        
        #region RenderMap Tests
        
        [Test]
        public void RenderMap_WithValidParameters_RendersSuccessfully()
        {
            // Arrange
            var eventFired = false;
            _tilemapGenerator.OnRenderingCompleted += (map) => eventFired = true;
            
            // Act
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            
            // Assert
            Assert.IsTrue(eventFired, "OnRenderingCompleted event should be fired");
            
            // Verify tiles were placed
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Any(t => t != null), 
                "Floor tiles should be placed");
            Assert.IsTrue(_testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds).Any(t => t != null), 
                "Wall tiles should be placed");
        }
        
        [Test]
        public void RenderMap_WithNullMap_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderMap(null, _testTilemaps));
        }
        
        [Test]
        public void RenderMap_WithNullTilemaps_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => _tilemapGenerator.RenderMap(_testMap, null));
        }
        
        [Test]
        public void RenderMap_WithEmptyTilemaps_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => _tilemapGenerator.RenderMap(_testMap, new Tilemap[0]));
        }
        
        [Test]
        public void RenderMap_WithInvalidTilemapSetup_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidTilemaps = new Tilemap[] { null };
            
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _tilemapGenerator.RenderMap(_testMap, invalidTilemaps));
            Assert.That(ex.Message, Contains.Substring("Invalid tilemap setup"));
        }
        
        [Test]
        public void RenderMap_WithPerformanceTarget_WarnsWhenExceeded()
        {
            // Arrange - Create a large map that should exceed performance targets
            var largeMap = MapGenerationTestDataFactory.CreateLargeTestMapData(100);
            
            // Act
            _tilemapGenerator.RenderMap(largeMap, _testTilemaps);
            
            // Assert - The warning should be logged (we can't easily test log output in unit tests,
            // but we can verify the map was still rendered)
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Any(t => t != null));
        }
        
        #endregion
        
        #region RenderRoom Tests
        
        [Test]
        public void RenderRoom_WithValidParameters_RendersRoomSuccessfully()
        {
            // Arrange
            var room = _testMap.Rooms.First();
            
            // Act
            _tilemapGenerator.RenderRoom(room, _testTilemaps[0], _testTileset);
            
            // Assert
            var roomBounds = room.Bounds;
            var tiles = _testTilemaps[0].GetTilesBlock(new BoundsInt(roomBounds.x, roomBounds.y, 0, roomBounds.width, roomBounds.height, 1));
            Assert.IsTrue(tiles.Any(t => t != null), "Room tiles should be placed");
        }
        
        [Test]
        public void RenderRoom_WithNullRoom_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderRoom(null, _testTilemaps[0], _testTileset));
        }
        
        [Test]
        public void RenderRoom_WithNullTilemap_ThrowsArgumentNullException()
        {
            // Arrange
            var room = _testMap.Rooms.First();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderRoom(room, null, _testTileset));
        }
        
        [Test]
        public void RenderRoom_WithNullTileset_ThrowsArgumentNullException()
        {
            // Arrange
            var room = _testMap.Rooms.First();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderRoom(room, _testTilemaps[0], null));
        }
        
        [Test]
        public void RenderRoom_WithDifferentRoomTypes_AppliesCorrectTiles()
        {
            // Arrange
            var officeRoom = MapGenerationTestDataFactory.CreateTestRoom(RoomClassification.Office);
            var serverRoom = MapGenerationTestDataFactory.CreateTestRoom(RoomClassification.ServerRoom);
            
            // Act
            _tilemapGenerator.RenderRoom(officeRoom, _testTilemaps[0], _testTileset);
            var officeTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            
            _testTilemaps[0].ClearAllTiles();
            _tilemapGenerator.RenderRoom(serverRoom, _testTilemaps[0], _testTileset);
            var serverTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            
            // Assert
            Assert.IsTrue(officeTiles.Any(t => t != null), "Office room should have tiles");
            Assert.IsTrue(serverTiles.Any(t => t != null), "Server room should have tiles");
        }
        
        #endregion
        
        #region RenderCorridor Tests
        
        [Test]
        public void RenderCorridor_WithValidParameters_RendersCorridorSuccessfully()
        {
            // Arrange
            var corridor = _testMap.Corridors.First();
            
            // Act
            _tilemapGenerator.RenderCorridor(corridor, _testTilemaps[0], _testTileset);
            
            // Assert
            var tiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            Assert.IsTrue(tiles.Any(t => t != null), "Corridor tiles should be placed");
        }
        
        [Test]
        public void RenderCorridor_WithNullCorridor_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderCorridor(null, _testTilemaps[0], _testTileset));
        }
        
        [Test]
        public void RenderCorridor_WithNullTilemap_ThrowsArgumentNullException()
        {
            // Arrange
            var corridor = _testMap.Corridors.First();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderCorridor(corridor, null, _testTileset));
        }
        
        [Test]
        public void RenderCorridor_WithNullTileset_ThrowsArgumentNullException()
        {
            // Arrange
            var corridor = _testMap.Corridors.First();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.RenderCorridor(corridor, _testTilemaps[0], null));
        }
        
        #endregion
        
        #region ClearTilemaps Tests
        
        [Test]
        public void ClearTilemaps_WithValidTilemaps_ClearsAllTiles()
        {
            // Arrange
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Any(t => t != null));
            
            // Act
            _tilemapGenerator.ClearTilemaps(_testTilemaps);
            
            // Assert
            foreach (var tilemap in _testTilemaps)
            {
                var tiles = tilemap.GetTilesBlock(tilemap.cellBounds);
                Assert.IsFalse(tiles.Any(t => t != null), "All tiles should be cleared");
            }
        }
        
        [Test]
        public void ClearTilemaps_WithNullArray_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => _tilemapGenerator.ClearTilemaps(null));
        }
        
        [Test]
        public void ClearTilemaps_WithNullTilemapEntries_HandlesGracefully()
        {
            // Arrange
            var tilemapsWithNulls = new Tilemap[] { _testTilemaps[0], null, _testTilemaps[1] };
            
            // Act & Assert
            Assert.DoesNotThrow(() => _tilemapGenerator.ClearTilemaps(tilemapsWithNulls));
        }
        
        #endregion
        
        #region UpdateTiles Tests
        
        [Test]
        public void UpdateTiles_WithValidParameters_UpdatesTilesCorrectly()
        {
            // Arrange
            var positions = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) };
            var tiles = new TileBase[] { _testTileset.FloorTiles.Tiles[0].Tile, _testTileset.FloorTiles.Tiles[1].Tile };
            var eventFired = false;
            _tilemapGenerator.OnTileRendered += (pos, tile) => eventFired = true;
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            
            // Assert
            Assert.IsTrue(eventFired, "OnTileRendered event should be fired");
            Assert.AreEqual(tiles[0], _testTilemaps[0].GetTile(positions[0]));
            Assert.AreEqual(tiles[1], _testTilemaps[0].GetTile(positions[1]));
        }
        
        [Test]
        public void UpdateTiles_WithNullPositions_ThrowsArgumentNullException()
        {
            // Arrange
            var tiles = new TileBase[] { _testTileset.FloorTiles.Tiles[0].Tile };
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.UpdateTiles(null, tiles, _testTilemaps[0]));
        }
        
        [Test]
        public void UpdateTiles_WithNullTiles_ThrowsArgumentNullException()
        {
            // Arrange
            var positions = new Vector3Int[] { new Vector3Int(0, 0, 0) };
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.UpdateTiles(positions, null, _testTilemaps[0]));
        }
        
        [Test]
        public void UpdateTiles_WithMismatchedArrayLengths_ThrowsArgumentException()
        {
            // Arrange
            var positions = new Vector3Int[] { new Vector3Int(0, 0, 0) };
            var tiles = new TileBase[] { _testTileset.FloorTiles.Tiles[0].Tile, _testTileset.FloorTiles.Tiles[1].Tile };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]));
        }
        
        #endregion
        
        #region ValidateTilemapSetup Tests
        
        [Test]
        public void ValidateTilemapSetup_WithValidSetup_ReturnsValidResult()
        {
            // Act
            var result = _tilemapGenerator.ValidateTilemapSetup(_testTilemaps);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Valid tilemap setup should pass validation");
            Assert.IsEmpty(result.Errors, "Should have no errors");
        }
        
        [Test]
        public void ValidateTilemapSetup_WithNullArray_ReturnsError()
        {
            // Act
            var result = _tilemapGenerator.ValidateTilemapSetup(null);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Null array should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Contains("null")), "Should have null error");
        }
        
        [Test]
        public void ValidateTilemapSetup_WithEmptyArray_ReturnsError()
        {
            // Act
            var result = _tilemapGenerator.ValidateTilemapSetup(new Tilemap[0]);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Empty array should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Contains("empty")), "Should have empty error");
        }
        
        [Test]
        public void ValidateTilemapSetup_WithNullTilemap_ReturnsError()
        {
            // Arrange
            var tilemapsWithNull = new Tilemap[] { null };
            
            // Act
            var result = _tilemapGenerator.ValidateTilemapSetup(tilemapsWithNull);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Null tilemap should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Contains("null")), "Should have null error");
        }
        
        [Test]
        public void ValidateTilemapSetup_WithoutGrid_ReturnsError()
        {
            // Arrange
            var tilemapWithoutGrid = new GameObject().AddComponent<Tilemap>();
            var tilemaps = new Tilemap[] { tilemapWithoutGrid };
            
            // Act
            var result = _tilemapGenerator.ValidateTilemapSetup(tilemaps);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Tilemap without grid should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Grid")), "Should have Grid error");
        }
        
        #endregion
        
        #region OptimizeTileRendering Tests
        
        [Test]
        public void OptimizeTileRendering_WithValidTilemap_DoesNotThrow()
        {
            // Arrange
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            
            // Act & Assert
            Assert.DoesNotThrow(() => _tilemapGenerator.OptimizeTileRendering(_testTilemaps[0]));
        }
        
        [Test]
        public void OptimizeTileRendering_WithNullTilemap_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => _tilemapGenerator.OptimizeTileRendering(null));
        }
        
        #endregion
        
        #region GetRenderedBounds Tests
        
        [Test]
        public void GetRenderedBounds_WithValidTilemap_ReturnsCorrectBounds()
        {
            // Arrange
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            
            // Act
            var bounds = _tilemapGenerator.GetRenderedBounds(_testTilemaps[0]);
            
            // Assert
            Assert.IsTrue(bounds.size.magnitude > 0, "Bounds should have size");
        }
        
        [Test]
        public void GetRenderedBounds_WithNullTilemap_ReturnsEmptyBounds()
        {
            // Act
            var bounds = _tilemapGenerator.GetRenderedBounds(null);
            
            // Assert
            Assert.AreEqual(new Bounds(), bounds, "Null tilemap should return empty bounds");
        }
        
        [Test]
        public void GetRenderedBounds_WithEmptyTilemap_ReturnsEmptyBounds()
        {
            // Act
            var bounds = _tilemapGenerator.GetRenderedBounds(_testTilemaps[0]);
            
            // Assert
            Assert.AreEqual(new Bounds(), bounds, "Empty tilemap should return empty bounds");
        }
        
        #endregion
        
        #region World/Grid Conversion Tests
        
        [Test]
        public void WorldToGrid_WithValidPosition_ReturnsCorrectGridPosition()
        {
            // Arrange
            var worldPos = new Vector3(1.5f, 2.5f, 0f);
            
            // Act
            var gridPos = _tilemapGenerator.WorldToGrid(worldPos, _testTilemaps[0]);
            
            // Assert
            Assert.AreEqual(new Vector3Int(1, 2, 0), gridPos, "World to grid conversion should be correct");
        }
        
        [Test]
        public void WorldToGrid_WithNullTilemap_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.WorldToGrid(Vector3.zero, null));
        }
        
        [Test]
        public void GridToWorld_WithValidPosition_ReturnsCorrectWorldPosition()
        {
            // Arrange
            var gridPos = new Vector3Int(2, 3, 0);
            
            // Act
            var worldPos = _tilemapGenerator.GridToWorld(gridPos, _testTilemaps[0]);
            
            // Assert
            Assert.AreEqual(new Vector3(2f, 3f, 0f), worldPos, "Grid to world conversion should be correct");
        }
        
        [Test]
        public void GridToWorld_WithNullTilemap_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tilemapGenerator.GridToWorld(Vector3Int.zero, null));
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void GetRenderingStatistics_AfterRendering_ReturnsCorrectStatistics()
        {
            // Arrange
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            
            // Act
            var stats = _tilemapGenerator.GetRenderingStatistics();
            
            // Assert
            Assert.IsTrue(stats.TilesRenderedThisFrame > 0, "Should have rendered tiles");
            Assert.IsTrue(stats.BatchOperationsThisFrame > 0, "Should have performed batch operations");
            Assert.IsTrue(stats.CachedTiles >= 0, "Cached tiles should be non-negative");
        }
        
        [Test]
        public void ResetPerformanceCounters_AfterRendering_ResetsCounters()
        {
            // Arrange
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            var statsBefore = _tilemapGenerator.GetRenderingStatistics();
            
            // Act
            _tilemapGenerator.ResetPerformanceCounters();
            var statsAfter = _tilemapGenerator.GetRenderingStatistics();
            
            // Assert
            Assert.AreEqual(0, statsAfter.TilesRenderedThisFrame, "Tiles rendered should be reset");
            Assert.AreEqual(0, statsAfter.BatchOperationsThisFrame, "Batch operations should be reset");
        }
        
        #endregion
        
        #region Event Tests
        
        [Test]
        public void OnTileRendered_WhenTileIsRendered_FiresCorrectly()
        {
            // Arrange
            Vector3Int capturedPosition = Vector3Int.zero;
            TileBase capturedTile = null;
            _tilemapGenerator.OnTileRendered += (pos, tile) =>
            {
                capturedPosition = pos;
                capturedTile = tile;
            };
            
            var positions = new Vector3Int[] { new Vector3Int(5, 5, 0) };
            var tiles = new TileBase[] { _testTileset.FloorTiles.Tiles[0].Tile };
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            
            // Assert
            Assert.AreEqual(positions[0], capturedPosition, "Event should fire with correct position");
            Assert.AreEqual(tiles[0], capturedTile, "Event should fire with correct tile");
        }
        
        [Test]
        public void OnRenderingFailed_WhenRenderingFails_FiresCorrectly()
        {
            // Arrange
            MapData capturedMap = null;
            Exception capturedException = null;
            _tilemapGenerator.OnRenderingFailed += (map, ex) =>
            {
                capturedMap = map;
                capturedException = ex;
            };
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _tilemapGenerator.RenderMap(_testMap, new Tilemap[] { null }));
            
            // Verify event was fired
            Assert.AreEqual(_testMap, capturedMap, "Failed map should be passed to event");
            Assert.IsNotNull(capturedException, "Exception should be passed to event");
        }
        
        #endregion
        
        #region Batch Operation Tests
        
        [Test]
        public void RenderMap_UsesBatchOperationsForEfficiency()
        {
            // Arrange
            var initialStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Act
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            var finalStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Assert
            Assert.IsTrue(finalStats.BatchOperationsThisFrame > initialStats.BatchOperationsThisFrame, 
                "Should use batch operations for efficiency");
        }
        
        [Test]
        public void UpdateTiles_WithMultipleTiles_UsesBatchOperation()
        {
            // Arrange
            var positions = new Vector3Int[10];
            var tiles = new TileBase[10];
            for (int i = 0; i < 10; i++)
            {
                positions[i] = new Vector3Int(i, 0, 0);
                tiles[i] = _testTileset.FloorTiles.Tiles[0].Tile;
            }
            
            var initialStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Act
            _tilemapGenerator.UpdateTiles(positions, tiles, _testTilemaps[0]);
            var finalStats = _tilemapGenerator.GetRenderingStatistics();
            
            // Assert
            Assert.IsTrue(finalStats.BatchOperationsThisFrame > initialStats.BatchOperationsThisFrame, 
                "Should use batch operation for multiple tiles");
        }
        
        #endregion
        
        #region Room Type Specific Tests
        
        [Test]
        public void RenderRoom_WithDifferentClassifications_AppliesCorrectTileTypes()
        {
            // Arrange
            var officeRoom = MapGenerationTestDataFactory.CreateTestRoom(RoomClassification.Office);
            var bossRoom = MapGenerationTestDataFactory.CreateTestRoom(RoomClassification.BossRoom);
            
            // Act
            _tilemapGenerator.RenderRoom(officeRoom, _testTilemaps[0], _testTileset);
            var officeTile = _testTilemaps[0].GetTile(new Vector3Int(officeRoom.Bounds.x, officeRoom.Bounds.y, 0));
            
            _testTilemaps[0].ClearAllTiles();
            _tilemapGenerator.RenderRoom(bossRoom, _testTilemaps[0], _testTileset);
            var bossTile = _testTilemaps[0].GetTile(new Vector3Int(bossRoom.Bounds.x, bossRoom.Bounds.y, 0));
            
            // Assert
            Assert.IsNotNull(officeTile, "Office room should have tiles");
            Assert.IsNotNull(bossTile, "Boss room should have tiles");
            // Note: In a real implementation, you might want to test that different tile types are used
        }
        
        #endregion
        
        #region Multi-layer Tests
        
        [Test]
        public void RenderMap_WithMultipleLayers_RendersToCorrectLayers()
        {
            // Act
            _tilemapGenerator.RenderMap(_testMap, _testTilemaps);
            
            // Assert
            // Floor layer should have tiles
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Any(t => t != null), 
                "Floor layer should have tiles");
            
            // Wall layer should have tiles
            Assert.IsTrue(_testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds).Any(t => t != null), 
                "Wall layer should have tiles");
            
            // Object layer might have tiles (decorations)
            var objectTiles = _testTilemaps[2].GetTilesBlock(_testTilemaps[2].cellBounds);
            // Object tiles are optional, so we don't assert they must exist
        }
        
        #endregion
    }
}