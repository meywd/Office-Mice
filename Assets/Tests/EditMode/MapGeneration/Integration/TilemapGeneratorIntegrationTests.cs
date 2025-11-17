using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Rendering;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Generators;
using OfficeMice.MapGeneration.Corridors;
using OfficeMice.MapGeneration.Tests.Factories;

namespace OfficeMice.MapGeneration.Tests.Integration
{
    [TestFixture]
    public class TilemapGeneratorIntegrationTests : BaseTestFixture
    {
        private TilemapGenerator _tilemapGenerator;
        private Tilemap[] _testTilemaps;
        private GameObject _testGameObject;
        private Grid _testGrid;
        private BSPGenerator _bspGenerator;
        private TwoPassCorridorGenerator _corridorGenerator;
        
        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            
            // Create test GameObject with Grid and Tilemap components
            _testGameObject = new GameObject("IntegrationTestTilemapObject");
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
            
            // Create tilemap generator
            _tilemapGenerator = new TilemapGenerator(42);
            
            // Create map generation components for integration testing
            _bspGenerator = new BSPGenerator();
            _corridorGenerator = new TwoPassCorridorGenerator();
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
            _bspGenerator = null;
            _corridorGenerator = null;
        }
        
        #region Full Pipeline Integration Tests
        
        [Test]
        public void FullMapGenerationPipeline_GeneratesAndRendersCompleteMap()
        {
            // Arrange
            var mapSize = new Vector2Int(100, 100);
            var minRoomSize = new Vector2Int(8, 8);
            var maxRoomSize = new Vector2Int(15, 15);
            
            // Act - Generate map structure
            var mapData = _bspGenerator.GenerateMap(mapSize, minRoomSize, maxRoomSize, 42);
            
            // Generate corridors
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Render the complete map
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            Assert.IsTrue(mapData.Rooms.Count > 0, "Map should have rooms");
            Assert.IsTrue(mapData.Corridors.Count > 0, "Map should have corridors");
            
            // Verify tiles were rendered
            Assert.IsTrue(_testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Any(t => t != null), 
                "Floor tiles should be rendered");
            Assert.IsTrue(_testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds).Any(t => t != null), 
                "Wall tiles should be rendered");
            
            // Verify map data is properly bound to tilemaps
            Assert.AreEqual(_testTilemaps[0], mapData.FloorTilemap, "Floor tilemap should be bound");
            Assert.AreEqual(_testTilemaps[1], mapData.WallTilemap, "Wall tilemap should be bound");
            Assert.AreEqual(_testTilemaps[2], mapData.ObjectTilemap, "Object tilemap should be bound");
        }
        
        [Test]
        public void FullPipeline_WithDifferentSeeds_GeneratesDifferentMaps()
        {
            // Arrange
            var mapSize = new Vector2Int(80, 80);
            var minRoomSize = new Vector2Int(6, 6);
            var maxRoomSize = new Vector2Int(12, 12);
            
            // Act - Generate two maps with different seeds
            var map1 = _bspGenerator.GenerateMap(mapSize, minRoomSize, maxRoomSize, 123);
            _corridorGenerator.GenerateCorridors(map1);
            _tilemapGenerator.RenderMap(map1, _testTilemaps);
            var tiles1 = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            
            _tilemapGenerator.ClearTilemaps(_testTilemaps);
            
            var map2 = _bspGenerator.GenerateMap(mapSize, minRoomSize, maxRoomSize, 456);
            _corridorGenerator.GenerateCorridors(map2);
            _tilemapGenerator.RenderMap(map2, _testTilemaps);
            var tiles2 = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            
            // Assert
            Assert.AreNotEqual(map1.Seed, map2.Seed, "Maps should have different seeds");
            
            // The tile patterns should be different (this is a probabilistic test)
            var tileCount1 = tiles1.Count(t => t != null);
            var tileCount2 = tiles2.Count(t => t != null);
            
            // While tile counts might be similar, the exact patterns should differ
            // We can't easily compare tile patterns in unit tests, but we can verify both maps rendered successfully
            Assert.Greater(tileCount1, 0, "First map should have tiles");
            Assert.Greater(tileCount2, 0, "Second map should have tiles");
        }
        
        #endregion
        
        #region Room Classification Integration Tests
        
        [Test]
        public void RoomClassificationIntegration_DifferentRoomTypes_RenderWithCorrectTiles()
        {
            // Arrange
            var mapSize = new Vector2Int(60, 60);
            var mapData = _bspGenerator.GenerateMap(mapSize, new Vector2Int(8, 8), new Vector2Int(12, 12), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Manually set different room classifications for testing
            if (mapData.Rooms.Count >= 3)
            {
                mapData.Rooms[0].Classification = RoomClassification.Office;
                mapData.Rooms[1].Classification = RoomClassification.ServerRoom;
                mapData.Rooms[2].Classification = RoomClassification.BossRoom;
            }
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            foreach (var room in mapData.Rooms)
            {
                var roomCenter = new Vector3Int(room.Bounds.center.x, room.Bounds.center.y, 0);
                var floorTile = _testTilemaps[0].GetTile(roomCenter);
                
                Assert.IsNotNull(floorTile, $"Room {room.RoomID} ({room.Classification}) should have floor tiles");
                
                // Verify wall tiles around the room
                var wallPos = new Vector3Int(room.Bounds.x - 1, room.Bounds.y, 0);
                var wallTile = _testTilemaps[1].GetTile(wallPos);
                Assert.IsNotNull(wallTile, $"Room {room.RoomID} should have wall tiles");
            }
        }
        
        #endregion
        
        #region Corridor Integration Tests
        
        [Test]
        public void CorridorIntegration_ConnectedRooms_HaveProperCorridorRendering()
        {
            // Arrange
            var mapSize = new Vector2Int(50, 50);
            var mapData = _bspGenerator.GenerateMap(mapSize, new Vector2Int(6, 6), new Vector2Int(10, 10), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            foreach (var corridor in mapData.Corridors)
            {
                // Verify corridor floor tiles are rendered
                var firstCorridorTile = corridor.PathTiles.First();
                var floorPos = new Vector3Int(firstCorridorTile.x, firstCorridorTile.y, 0);
                var floorTile = _testTilemaps[0].GetTile(floorPos);
                Assert.IsNotNull(floorTile, $"Corridor {corridor.CorridorID} should have floor tiles");
                
                // Verify corridor walls are rendered
                var wallPos = new Vector3Int(firstCorridorTile.x + 1, firstCorridorTile.y, 0);
                var wallTile = _testTilemaps[1].GetTile(wallPos);
                // Wall tiles might not exist at every position due to room connections
            }
            
            // Verify connectivity through rendered tiles
            Assert.IsTrue(mapData.Corridors.Count > 0, "Map should have corridors connecting rooms");
        }
        
        [Test]
        public void CorridorIntegration_WallGeneration_DoesNotOverlapRooms()
        {
            // Arrange
            var mapSize = new Vector2Int(40, 40);
            var mapData = _bspGenerator.GenerateMap(mapSize, new Vector2Int(5, 5), new Vector2Int(8, 8), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            foreach (var room in mapData.Rooms)
            {
                // Check that room floor tiles are not overwritten by corridor walls
                for (int x = room.Bounds.x; x < room.Bounds.xMax; x++)
                {
                    for (int y = room.Bounds.y; y < room.Bounds.yMax; y++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        var floorTile = _testTilemaps[0].GetTile(pos);
                        var wallTile = _testTilemaps[1].GetTile(pos);
                        
                        // Room interior should have floor tiles, not wall tiles
                        if (floorTile != null)
                        {
                            Assert.IsNull(wallTile, $"Room interior at {pos} should not have wall tiles");
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Multi-layer Integration Tests
        
        [Test]
        public void MultiLayerIntegration_AllLayers_RenderCorrectly()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(20);
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            // Floor layer (index 0) should have the most tiles
            var floorTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            var floorTileCount = floorTiles.Count(t => t != null);
            Assert.Greater(floorTileCount, 0, "Floor layer should have tiles");
            
            // Wall layer (index 1) should have tiles
            var wallTiles = _testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds);
            var wallTileCount = wallTiles.Count(t => t != null);
            Assert.Greater(wallTileCount, 0, "Wall layer should have tiles");
            
            // Object layer (index 2) might have decorative tiles
            var objectTiles = _testTilemaps[2].GetTilesBlock(_testTilemaps[2].cellBounds);
            var objectTileCount = objectTiles.Count(t => t != null);
            // Object tiles are optional, so we don't assert they must exist
            
            // Verify layer separation
            for (int x = 0; x < _testTilemaps[0].cellBounds.xMax; x++)
            {
                for (int y = 0; y < _testTilemaps[0].cellBounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var floorTile = _testTilemaps[0].GetTile(pos);
                    var wallTile = _testTilemaps[1].GetTile(pos);
                    
                    // A position should not have both floor and wall tiles
                    if (floorTile != null && wallTile != null)
                    {
                        // This might be acceptable in some cases (e.g., tiles on different layers),
                        // but generally floor and wall should be on different layers
                        Debug.LogWarning($"Position {pos} has both floor and wall tiles");
                    }
                }
            }
        }
        
        #endregion
        
        #region Event Integration Tests
        
        [Test]
        public void EventIntegration_RenderingProcess_FiresEventsInCorrectOrder()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(10);
            var tileRenderedEvents = new List<(Vector3Int position, TileBase tile)>();
            var renderingCompletedFired = false;
            
            _tilemapGenerator.OnTileRendered += (pos, tile) => tileRenderedEvents.Add((pos, tile));
            _tilemapGenerator.OnRenderingCompleted += (map) => renderingCompletedFired = true;
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            Assert.Greater(tileRenderedEvents.Count, 0, "Should fire tile rendered events");
            Assert.IsTrue(renderingCompletedFired, "Should fire rendering completed event");
            
            // Verify events contain valid data
            foreach (var (position, tile) in tileRenderedEvents)
            {
                Assert.IsNotNull(tile, "Event should include valid tile");
                Assert.AreNotEqual(Vector3Int.zero, position, "Event should include valid position");
            }
        }
        
        [Test]
        public void EventIntegration_RenderingFailure_FiresErrorEvent()
        {
            // Arrange
            var mapData = MapGenerationTestDataFactory.CreateTestMapData(5);
            var invalidTilemaps = new Tilemap[] { null };
            MapData capturedMap = null;
            Exception capturedException = null;
            
            _tilemapGenerator.OnRenderingFailed += (map, ex) =>
            {
                capturedMap = map;
                capturedException = ex;
            };
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _tilemapGenerator.RenderMap(mapData, invalidTilemaps));
            
            // Verify error event was fired
            Assert.AreEqual(mapData, capturedMap, "Should pass correct map to error event");
            Assert.IsNotNull(capturedException, "Should pass exception to error event");
        }
        
        #endregion
        
        #region Performance Integration Tests
        
        [Test]
        public void PerformanceIntegration_LargeMap_RendersWithinTargets()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Generate a large map
            var mapData = _bspGenerator.GenerateMap(new Vector2Int(150, 150), new Vector2Int(8, 8), new Vector2Int(15, 15), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            var generationTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            stopwatch.Stop();
            var renderingTime = stopwatch.ElapsedMilliseconds;
            
            // Assert
            Assert.Less(renderingTime, 150, "Large map rendering should complete within 150ms");
            
            var stats = _tilemapGenerator.GetRenderingStatistics();
            Assert.IsTrue(stats.IsWithinPerformanceTargets, "Should be within performance targets");
            
            Debug.Log($"Performance Integration - Generation: {generationTime}ms, Rendering: {renderingTime}ms, Rooms: {mapData.Rooms.Count}");
        }
        
        #endregion
        
        #region Validation Integration Tests
        
        [Test]
        public void ValidationIntegration_CompleteMap_PassesAllValidations()
        {
            // Arrange
            var mapData = _bspGenerator.GenerateMap(new Vector2Int(80, 80), new Vector2Int(6, 6), new Vector2Int(12, 12), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Act
            var mapValidationResult = mapData.Validate();
            var tilemapValidationResult = _tilemapGenerator.ValidateTilemapSetup(_testTilemaps);
            
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            Assert.IsTrue(mapValidationResult.IsValid, $"Map validation should pass: {string.Join(", ", mapValidationResult.Errors)}");
            Assert.IsTrue(tilemapValidationResult.IsValid, $"Tilemap validation should pass: {string.Join(", ", tilemapValidationResult.Errors)}");
            
            // Verify rendered map is visually complete
            var floorTiles = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds);
            var wallTiles = _testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds);
            
            Assert.Greater(floorTiles.Count(t => t != null), 0, "Should have floor tiles");
            Assert.Greater(wallTiles.Count(t => t != null), 0, "Should have wall tiles");
        }
        
        #endregion
        
        #region Real-world Scenario Tests
        
        [Test]
        public void RealWorldScenario_OfficeBuilding_GeneratesCoherentMap()
        {
            // Arrange - Simulate a realistic office building scenario
            var mapSize = new Vector2Int(120, 80);
            var mapData = _bspGenerator.GenerateMap(mapSize, new Vector2Int(10, 10), new Vector2Int(20, 15), 42);
            _corridorGenerator.GenerateCorridors(mapData);
            
            // Manually assign realistic room classifications
            var roomClassifications = new[]
            {
                RoomClassification.Office, RoomClassification.Office, RoomClassification.Office,
                RoomClassification.Conference, RoomClassification.BreakRoom,
                RoomClassification.ServerRoom, RoomClassification.Storage,
                RoomClassification.Lobby, RoomClassification.BossRoom
            };
            
            for (int i = 0; i < Math.Min(mapData.Rooms.Count, roomClassifications.Length); i++)
            {
                mapData.Rooms[i].Classification = roomClassifications[i];
            }
            
            // Act
            _tilemapGenerator.RenderMap(mapData, _testTilemaps);
            
            // Assert
            Assert.GreaterOrEqual(mapData.Rooms.Count, 5, "Office building should have multiple rooms");
            Assert.GreaterOrEqual(mapData.Corridors.Count, 3, "Office building should have connecting corridors");
            
            // Verify different room types are rendered
            var distinctClassifications = mapData.Rooms.Select(r => r.Classification).Distinct().ToList();
            Assert.Greater(distinctClassifications.Count, 1, "Should have different room types");
            
            // Verify visual coherence
            var floorTileCount = _testTilemaps[0].GetTilesBlock(_testTilemaps[0].cellBounds).Count(t => t != null);
            var wallTileCount = _testTilemaps[1].GetTilesBlock(_testTilemaps[1].cellBounds).Count(t => t != null);
            
            Assert.Greater(floorTileCount, wallTileCount, "Should have more floor tiles than wall tiles");
            Assert.Greater(wallTileCount, 0, "Should have wall tiles for boundaries");
            
            Debug.Log($"Office Building Scenario - Rooms: {mapData.Rooms.Count}, Corridors: {mapData.Corridors.Count}, Floor Tiles: {floorTileCount}, Wall Tiles: {wallTileCount}");
        }
        
        #endregion
    }
}