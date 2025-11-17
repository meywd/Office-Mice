using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive unit tests for supporting interfaces (ITileRenderer, IAssetLoader, IPathfinder).
    /// Tests all interface methods, events, and edge cases.
    /// </summary>
    [TestFixture]
    public class SupportingInterfacesTests
    {
        private MockTileRenderer _tileRenderer;
        private MockAssetLoader _assetLoader;
        private MockPathfinder _pathfinder;
        private MapData _testMap;
        private Tilemap _testTilemap;
        private bool[,] _testObstacles;

        [SetUp]
        public void SetUp()
        {
            _tileRenderer = new MockTileRenderer();
            _assetLoader = new MockAssetLoader();
            _pathfinder = new MockPathfinder();
            _testMap = CreateTestMap();
            _testTilemap = CreateTestTilemap();
            _testObstacles = CreateTestObstacles();
        }

        #region ITileRenderer Tests

        [Test]
        public void TileRenderer_RenderMap_WithValidData_RendersSuccessfully()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _tileRenderer.RenderMap(_testMap, new Tilemap[] { _testTilemap }));
        }

        [Test]
        public void TileRenderer_RenderMap_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tileRenderer.RenderMap(null, new Tilemap[] { _testTilemap }));
        }

        [Test]
        public void TileRenderer_RenderMap_WithNullTilemaps_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tileRenderer.RenderMap(_testMap, null));
        }

        [Test]
        public void TileRenderer_RenderMap_WithEmptyTilemaps_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _tileRenderer.RenderMap(_testMap, new Tilemap[0]));
        }

        [Test]
        public void TileRenderer_RenderRoom_WithValidData_RendersSuccessfully()
        {
            // Arrange
            var room = _testMap.Rooms[0];

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _tileRenderer.RenderRoom(room, _testTilemap, null));
        }

        [Test]
        public void TileRenderer_RenderRoom_WithNullRoom_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tileRenderer.RenderRoom(null, _testTilemap, null));
        }

        [Test]
        public void TileRenderer_RenderRoom_WithNullTilemap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _tileRenderer.RenderRoom(_testMap.Rooms[0], null, null));
        }

        [Test]
        public void TileRenderer_ValidateTilemapSetup_WithValidData_ReturnsSuccess()
        {
            // Arrange
            _tileRenderer.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _tileRenderer.ValidateTilemapSetup(new Tilemap[] { _testTilemap });

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void TileRenderer_ValidateTilemapSetup_WithNullTilemaps_ReturnsFailure()
        {
            // Act
            var result = _tileRenderer.ValidateTilemapSetup(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Tilemaps array cannot be null"));
        }

        [Test]
        public void TileRenderer_OnTileRendered_EventFired_WhenTilesRendered()
        {
            // Arrange
            var renderedTiles = new List<(Vector3Int position, TileBase tile)>();
            _tileRenderer.OnTileRendered += (pos, tile) => renderedTiles.Add((pos, tile));

            // Act
            _tileRenderer.RenderMap(_testMap, new Tilemap[] { _testTilemap });

            // Assert
            Assert.IsTrue(renderedTiles.Count > 0);
        }

        [Test]
        public void TileRenderer_Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockTileRenderer properly implements ITileRenderer
            Assert.IsInstanceOf<ITileRenderer>(_tileRenderer);
            
            // Verify all required methods exist and are callable
            var renderer = (ITileRenderer)_tileRenderer;
            
            Assert.DoesNotThrow(() => renderer.ValidateTilemapSetup(new Tilemap[] { _testTilemap }));
            Assert.DoesNotThrow(() => renderer.GetRenderedBounds(_testTilemap));
            Assert.DoesNotThrow(() => renderer.WorldToGrid(Vector3.zero, _testTilemap));
            Assert.DoesNotThrow(() => renderer.GridToWorld(Vector3Int.zero, _testTilemap));
        }

        #endregion

        #region IAssetLoader Tests

        [Test]
        public void AssetLoader_LoadTile_WithValidName_ReturnsTile()
        {
            // Arrange
            var expectedTile = ScriptableObject.CreateInstance<TileBase>();
            _assetLoader.AddMockTile("testTile", expectedTile);

            // Act
            var result = _assetLoader.LoadTile("testTile");

            // Assert
            Assert.AreEqual(expectedTile, result);
        }

        [Test]
        public void AssetLoader_LoadTile_WithNullName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(null));
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadTile(""));
        }

        [Test]
        public void AssetLoader_LoadTile_WithNonExistentTile_ReturnsNull()
        {
            // Act
            var result = _assetLoader.LoadTile("nonExistentTile");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void AssetLoader_LoadPrefab_WithValidName_ReturnsPrefab()
        {
            // Arrange
            var expectedPrefab = new GameObject("TestPrefab");
            _assetLoader.AddMockPrefab("testPrefab", expectedPrefab);

            // Act
            var result = _assetLoader.LoadPrefab("testPrefab");

            // Assert
            Assert.AreEqual(expectedPrefab, result);
        }

        [Test]
        public void AssetLoader_LoadPrefab_WithNullName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadPrefab(null));
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadPrefab(""));
        }

        [Test]
        public void AssetLoader_LoadScriptableObject_WithValidName_ReturnsObject()
        {
            // Arrange
            var expectedObj = ScriptableObject.CreateInstance<ScriptableObject>();
            _assetLoader.AddMockScriptableObject("testObj", expectedObj);

            // Act
            var result = _assetLoader.LoadScriptableObject<ScriptableObject>("testObj");

            // Assert
            Assert.AreEqual(expectedObj, result);
        }

        [Test]
        public void AssetLoader_LoadScriptableObject_WithNullName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadScriptableObject<ScriptableObject>(null));
            Assert.Throws<ArgumentException>(() => _assetLoader.LoadScriptableObject<ScriptableObject>(""));
        }

        [Test]
        public void AssetLoader_ValidateRequiredAssets_WithValidAssets_ReturnsSuccess()
        {
            // Arrange
            _assetLoader.AddMockTile("tile1", ScriptableObject.CreateInstance<TileBase>());
            _assetLoader.AddMockTile("tile2", ScriptableObject.CreateInstance<TileBase>());

            // Act
            var result = _assetLoader.ValidateRequiredAssets(new List<string> { "tile1", "tile2" }, typeof(TileBase));

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void AssetLoader_ValidateRequiredAssets_WithMissingAssets_ReturnsFailure()
        {
            // Act
            var result = _assetLoader.ValidateRequiredAssets(new List<string> { "missingTile" }, typeof(TileBase));

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Missing assets: missingTile"));
        }

        [Test]
        public void AssetLoader_OnAssetLoaded_EventFired_WhenAssetLoaded()
        {
            // Arrange
            var loadedAssets = new List<(string name, Type type)>();
            _assetLoader.OnAssetLoaded += (name, type) => loadedAssets.Add((name, type));
            var tile = ScriptableObject.CreateInstance<TileBase>();
            _assetLoader.AddMockTile("testTile", tile);

            // Act
            _assetLoader.LoadTile("testTile");

            // Assert
            Assert.AreEqual(1, loadedAssets.Count);
            Assert.AreEqual("testTile", loadedAssets[0].name);
            Assert.AreEqual(typeof(TileBase), loadedAssets[0].type);
        }

        [Test]
        public void AssetLoader_Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockAssetLoader properly implements IAssetLoader
            Assert.IsInstanceOf<IAssetLoader>(_assetLoader);
            
            // Verify all required methods exist and are callable
            var loader = (IAssetLoader)_assetLoader;
            
            Assert.DoesNotThrow(() => loader.PreloadAssets(new List<string> { "test" }, typeof(TileBase)));
            Assert.DoesNotThrow(() => loader.ClearCache());
            Assert.DoesNotThrow(() => loader.GetCacheStats());
            Assert.DoesNotThrow(() => loader.IsAssetCached("test", typeof(TileBase)));
            Assert.DoesNotThrow(() => loader.LoadAllAssets<TileBase>());
        }

        #endregion

        #region IPathfinder Tests

        [Test]
        public void Pathfinder_FindPath_WithValidData_ReturnsPath()
        {
            // Arrange
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(5, 5);
            var expectedPath = new List<Vector2Int> { start, new Vector2Int(2, 2), end };
            _pathfinder.SetMockPath(expectedPath);

            // Act
            var result = _pathfinder.FindPath(start, end, _testObstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedPath.Count, result.Count);
        }

        [Test]
        public void Pathfinder_FindPath_WithNullObstacles_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _pathfinder.FindPath(Vector2Int.zero, Vector2Int.one, null));
        }

        [Test]
        public void Pathfinder_FindPath_WithOutOfBoundsStart_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _pathfinder.FindPath(new Vector2Int(-1, 0), Vector2Int.one, _testObstacles));
        }

        [Test]
        public void Pathfinder_FindPath_WithOutOfBoundsEnd_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _pathfinder.FindPath(Vector2Int.zero, new Vector2Int(100, 100), _testObstacles));
        }

        [Test]
        public void Pathfinder_FindPath_WithBlockedStart_ReturnsEmptyPath()
        {
            // Arrange
            var start = new Vector2Int(1, 1);
            var end = new Vector2Int(5, 5);
            _testObstacles[start.x, start.y] = true; // Block start

            // Act
            var result = _pathfinder.FindPath(start, end, _testObstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Pathfinder_FindPath_WithBlockedEnd_ReturnsEmptyPath()
        {
            // Arrange
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(5, 5);
            _testObstacles[end.x, end.y] = true; // Block end

            // Act
            var result = _pathfinder.FindPath(start, end, _testObstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Pathfinder_PathExists_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(5, 5);
            _pathfinder.SetMockPath(new List<Vector2Int> { start, end });

            // Act
            var result = _pathfinder.PathExists(start, end, _testObstacles);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Pathfinder_PathExists_WithNoPath_ReturnsFalse()
        {
            // Arrange
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(5, 5);
            _pathfinder.SetMockPath(new List<Vector2Int>()); // Empty path

            // Act
            var result = _pathfinder.PathExists(start, end, _testObstacles);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Pathfinder_GetReachablePositions_WithValidData_ReturnsPositions()
        {
            // Arrange
            var start = new Vector2Int(2, 2);

            // Act
            var result = _pathfinder.GetReachablePositions(start, _testObstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void Pathfinder_OptimizePath_WithValidPath_ReturnsOptimizedPath()
        {
            // Arrange
            var originalPath = new List<Vector2Int> 
            { 
                Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0), 
                new Vector2Int(2, 1), new Vector2Int(2, 2) 
            };

            // Act
            var result = _pathfinder.OptimizePath(originalPath, _testObstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= originalPath.Count);
        }

        [Test]
        public void Pathfinder_CalculatePathCost_WithValidPath_ReturnsCost()
        {
            // Arrange
            var path = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0) };

            // Act
            var result = _pathfinder.CalculatePathCost(path);

            // Assert
            Assert.IsTrue(result > 0);
        }

        [Test]
        public void Pathfinder_ValidatePathfindingParameters_WithValidData_ReturnsSuccess()
        {
            // Arrange
            _pathfinder.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _pathfinder.ValidatePathfindingParameters(Vector2Int.zero, Vector2Int.one, _testObstacles);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Pathfinder_OnPathfindingCompleted_EventFired_WhenPathFound()
        {
            // Arrange
            var completedPaths = new List<(Vector2Int start, Vector2Int end, List<Vector2Int> path)>();
            _pathfinder.OnPathfindingCompleted += (start, end, path) => completedPaths.Add((start, end, path));
            var start = Vector2Int.zero;
            var end = Vector2Int.one;
            var expectedPath = new List<Vector2Int> { start, end };
            _pathfinder.SetMockPath(expectedPath);

            // Act
            _pathfinder.FindPath(start, end, _testObstacles);

            // Assert
            Assert.AreEqual(1, completedPaths.Count);
            Assert.AreEqual(start, completedPaths[0].start);
            Assert.AreEqual(end, completedPaths[0].end);
            Assert.AreEqual(expectedPath.Count, completedPaths[0].path.Count);
        }

        [Test]
        public void Pathfinder_Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockPathfinder properly implements IPathfinder
            Assert.IsInstanceOf<IPathfinder>(_pathfinder);
            
            // Verify all required methods exist and are callable
            var pathfinder = (IPathfinder)_pathfinder;
            
            Assert.DoesNotThrow(() => pathfinder.FindMultiplePaths(Vector2Int.zero, new List<Vector2Int> { Vector2Int.one }, _testObstacles));
            Assert.DoesNotThrow(() => pathfinder.GetReachablePositions(Vector2Int.zero, _testObstacles));
            Assert.DoesNotThrow(() => pathfinder.OptimizePath(new List<Vector2Int> { Vector2Int.zero }, _testObstacles));
            Assert.DoesNotThrow(() => pathfinder.CalculatePathCost(new List<Vector2Int> { Vector2Int.zero }));
            Assert.DoesNotThrow(() => pathfinder.ValidatePathfindingParameters(Vector2Int.zero, Vector2Int.one, _testObstacles));
            Assert.DoesNotThrow(() => pathfinder.SetHeuristic((s, e) => 1f));
            Assert.DoesNotThrow(() => pathfinder.GetPerformanceStats());
            Assert.DoesNotThrow(() => pathfinder.ResetStats());
        }

        #endregion

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData();
            map.SetDimensions(10, 10);
            map.SetSeed(12345);

            var room = new RoomData();
            room.SetBounds(new Rect(2, 2, 6, 6));
            room.Classification = RoomClassification.Office;
            map.AddRoom(room);

            return map;
        }

        private Tilemap CreateTestTilemap()
        {
            var gameObject = new GameObject("TestTilemap");
            return gameObject.AddComponent<Tilemap>();
        }

        private bool[,] CreateTestObstacles()
        {
            return new bool[10, 10]; // All false (no obstacles)
        }

        #endregion
    }
}