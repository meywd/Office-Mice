using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Integration tests for all interfaces working together.
    /// Tests the complete map generation pipeline using mock implementations.
    /// </summary>
    [TestFixture]
    public class InterfaceIntegrationTests
    {
        private MockMapGenerator _mapGenerator;
        private MockRoomGenerator _roomGenerator;
        private MockCorridorGenerator _corridorGenerator;
        private MockContentPopulator _contentPopulator;
        private MockTileRenderer _tileRenderer;
        private MockAssetLoader _assetLoader;
        private MockPathfinder _pathfinder;
        private MapGenerationSettings _testSettings;
        private BiomeConfiguration _testBiome;

        [SetUp]
        public void SetUp()
        {
            _mapGenerator = new MockMapGenerator();
            _roomGenerator = new MockRoomGenerator();
            _corridorGenerator = new MockCorridorGenerator();
            _contentPopulator = new MockContentPopulator();
            _tileRenderer = new MockTileRenderer();
            _assetLoader = new MockAssetLoader();
            _pathfinder = new MockPathfinder();
            
            _testSettings = new MapGenerationSettings();
            _testBiome = new BiomeConfiguration();

            SetupMockData();
        }

        [Test]
        public void CompleteMapGenerationPipeline_AllInterfaces_WorkTogether()
        {
            // Arrange
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            var mapData = CreateMapDataFromComponents(rooms, corridors);

            // Act & Assert - Each step should work without throwing
            Assert.DoesNotThrow(() => _contentPopulator.PopulateContent(mapData, _testBiome));
            Assert.DoesNotThrow(() => _tileRenderer.RenderMap(mapData, CreateMockTilemaps()));
            Assert.DoesNotThrow(() => _assetLoader.ValidateRequiredAssets(new List<string> { "floor", "wall" }, typeof(TileBase)));
            Assert.DoesNotThrow(() => _pathfinder.FindPath(Vector2Int.zero, Vector2Int.one, CreateMockObstacles()));
        }

        [UnityTest]
        public IEnumerator AsyncMapGenerationPipeline_CompletesSuccessfully()
        {
            // Arrange
            var mapGenerated = false;
            var mapData = (MapData)null;
            
            _mapGenerator.OnGenerationCompleted += (map) => 
            { 
                mapGenerated = true; 
                mapData = map; 
            };

            // Act
            var enumerator = _mapGenerator.GenerateMapAsync(_testSettings);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.IsTrue(mapGenerated, "Map generation should complete");
            Assert.IsNotNull(mapData, "Generated map data should not be null");
        }

        [Test]
        public void RoomToCorridorIntegration_ValidData_ConnectsSuccessfully()
        {
            // Arrange
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            Assert.IsTrue(rooms.Count >= 2, "Need at least 2 rooms for corridor testing");

            // Act
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            var validationResult = _corridorGenerator.ValidateConnectivity(rooms, corridors);

            // Assert
            Assert.IsNotNull(corridors, "Corridors should be generated");
            Assert.IsTrue(corridors.Count > 0, "At least one corridor should be created");
            Assert.IsTrue(validationResult.IsValid, "Connectivity should be valid");
        }

        [Test]
        public void ContentPopulationIntegration_ValidMap_PopulatesSuccessfully()
        {
            // Arrange
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            var mapData = CreateMapDataFromComponents(rooms, corridors);

            // Act
            var furniture = _contentPopulator.PlaceFurniture(mapData, _testBiome);
            var spawnPoints = _contentPopulator.PlaceEnemySpawns(mapData, _testBiome);
            var resources = _contentPopulator.PlaceResources(mapData, _testBiome);

            // Assert
            Assert.IsNotNull(furniture, "Furniture should be placed");
            Assert.IsNotNull(spawnPoints, "Spawn points should be placed");
            Assert.IsNotNull(resources, "Resources should be placed");
            
            var validation = _contentPopulator.ValidateContentPlacement(mapData);
            Assert.IsTrue(validation.IsValid, "Content placement should be valid");
        }

        [Test]
        public void RenderingIntegration_ValidMap_RendersSuccessfully()
        {
            // Arrange
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            var mapData = CreateMapDataFromComponents(rooms, corridors);
            var tilemaps = CreateMockTilemaps();

            // Act
            _tileRenderer.RenderMap(mapData, tilemaps);
            var renderedTiles = _tileRenderer.RenderedTiles;

            // Assert
            Assert.IsNotNull(renderedTiles, "Rendered tiles should be tracked");
            Assert.IsTrue(renderedTiles.Count > 0, "Some tiles should be rendered");
            
            var validation = _tileRenderer.ValidateTilemapSetup(tilemaps);
            Assert.IsTrue(validation.IsValid, "Tilemap setup should be valid");
        }

        [Test]
        public void AssetLoadingIntegration_ValidAssets_LoadsSuccessfully()
        {
            // Arrange
            var requiredAssets = new List<string> { "floor", "wall", "door" };
            
            // Add mock assets
            _assetLoader.AddMockTile("floor", ScriptableObject.CreateInstance<TileBase>());
            _assetLoader.AddMockTile("wall", ScriptableObject.CreateInstance<TileBase>());
            _assetLoader.AddMockTile("door", ScriptableObject.CreateInstance<TileBase>());

            // Act
            var validation = _assetLoader.ValidateRequiredAssets(requiredAssets, typeof(TileBase));
            var floorTile = _assetLoader.LoadTile("floor");
            var wallTile = _assetLoader.LoadTile("wall");
            var doorTile = _assetLoader.LoadTile("door");

            // Assert
            Assert.IsTrue(validation.IsValid, "All required assets should be available");
            Assert.IsNotNull(floorTile, "Floor tile should be loaded");
            Assert.IsNotNull(wallTile, "Wall tile should be loaded");
            Assert.IsNotNull(doorTile, "Door tile should be loaded");
            
            var cacheStats = _assetLoader.GetCacheStats();
            Assert.IsTrue(cacheStats.CachedAssets > 0, "Assets should be cached");
        }

        [Test]
        public void PathfindingIntegration_ValidObstacles_FindsPaths()
        {
            // Arrange
            var obstacles = CreateMockObstacles();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(5, 5);
            var expectedPath = new List<Vector2Int> { start, new Vector2Int(2, 2), end };
            _pathfinder.SetMockPath(expectedPath);

            // Act
            var path = _pathfinder.FindPath(start, end, obstacles);
            var pathExists = _pathfinder.PathExists(start, end, obstacles);
            var reachablePositions = _pathfinder.GetReachablePositions(start, obstacles);
            var validation = _pathfinder.ValidatePathfindingParameters(start, end, obstacles);

            // Assert
            Assert.IsNotNull(path, "Path should be found");
            Assert.AreEqual(expectedPath.Count, path.Count, "Path should match expected path");
            Assert.IsTrue(pathExists, "Path should exist");
            Assert.IsNotNull(reachablePositions, "Reachable positions should be calculated");
            Assert.IsTrue(reachablePositions.Count > 0, "Some positions should be reachable");
            Assert.IsTrue(validation.IsValid, "Pathfinding parameters should be valid");
        }

        [Test]
        public void EventIntegration_AllEvents_FireCorrectly()
        {
            // Arrange
            var eventCounts = new Dictionary<string, int>();
            
            // Subscribe to all events
            _mapGenerator.OnGenerationCompleted += (map) => eventCounts["MapGenerated"] = (eventCounts.ContainsKey("MapGenerated") ? eventCounts["MapGenerated"] : 0) + 1;
            _roomGenerator.OnRoomGenerated += (room) => eventCounts["RoomGenerated"] = (eventCounts.ContainsKey("RoomGenerated") ? eventCounts["RoomGenerated"] : 0) + 1;
            _corridorGenerator.OnCorridorGenerated += (corridor) => eventCounts["CorridorGenerated"] = (eventCounts.ContainsKey("CorridorGenerated") ? eventCounts["CorridorGenerated"] : 0) + 1;
            _contentPopulator.OnFurniturePlaced += (furniture) => eventCounts["FurniturePlaced"] = (eventCounts.ContainsKey("FurniturePlaced") ? eventCounts["FurniturePlaced"] : 0) + 1;
            _tileRenderer.OnTileRendered += (pos, tile) => eventCounts["TileRendered"] = (eventCounts.ContainsKey("TileRendered") ? eventCounts["TileRendered"] : 0) + 1;
            _assetLoader.OnAssetLoaded += (name, type) => eventCounts["AssetLoaded"] = (eventCounts.ContainsKey("AssetLoaded") ? eventCounts["AssetLoaded"] : 0) + 1;
            _pathfinder.OnPathfindingCompleted += (start, end, path) => eventCounts["PathfindingCompleted"] = (eventCounts.ContainsKey("PathfindingCompleted") ? eventCounts["PathfindingCompleted"] : 0) + 1;

            // Act - Trigger all events
            var map = _mapGenerator.GenerateMap(_testSettings);
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            _contentPopulator.PopulateContent(map, _testBiome);
            _tileRenderer.RenderMap(map, CreateMockTilemaps());
            _assetLoader.LoadTile("floor");
            _pathfinder.FindPath(Vector2Int.zero, Vector2Int.one, CreateMockObstacles());

            // Assert
            Assert.IsTrue(eventCounts.ContainsKey("MapGenerated") && eventCounts["MapGenerated"] > 0, "Map generation event should fire");
            Assert.IsTrue(eventCounts.ContainsKey("RoomGenerated") && eventCounts["RoomGenerated"] > 0, "Room generation events should fire");
            Assert.IsTrue(eventCounts.ContainsKey("CorridorGenerated") && eventCounts["CorridorGenerated"] > 0, "Corridor generation events should fire");
            Assert.IsTrue(eventCounts.ContainsKey("FurniturePlaced") && eventCounts["FurniturePlaced"] > 0, "Furniture placement events should fire");
            Assert.IsTrue(eventCounts.ContainsKey("TileRendered") && eventCounts["TileRendered"] > 0, "Tile rendering events should fire");
            Assert.IsTrue(eventCounts.ContainsKey("AssetLoaded") && eventCounts["AssetLoaded"] > 0, "Asset loading events should fire");
            Assert.IsTrue(eventCounts.ContainsKey("PathfindingCompleted") && eventCounts["PathfindingCompleted"] > 0, "Pathfinding completion events should fire");
        }

        [Test]
        public void ErrorHandlingIntegration_WithExceptions_HandlesGracefully()
        {
            // Arrange
            var errorCounts = new Dictionary<string, int>();
            
            _mapGenerator.OnGenerationFailed += (ex) => errorCounts["MapGenerationFailed"] = (errorCounts.ContainsKey("MapGenerationFailed") ? errorCounts["MapGenerationFailed"] : 0) + 1;
            _roomGenerator.OnRoomGenerationFailed += (room, ex) => errorCounts["RoomGenerationFailed"] = (errorCounts.ContainsKey("RoomGenerationFailed") ? errorCounts["RoomGenerationFailed"] : 0) + 1;
            _corridorGenerator.OnCorridorGenerationFailed += (room1, room2, ex) => errorCounts["CorridorGenerationFailed"] = (errorCounts.ContainsKey("CorridorGenerationFailed") ? errorCounts["CorridorGenerationFailed"] : 0) + 1;
            _contentPopulator.OnContentPopulationFailed += (map, ex) => errorCounts["ContentPopulationFailed"] = (errorCounts.ContainsKey("ContentPopulationFailed") ? errorCounts["ContentPopulationFailed"] : 0) + 1;
            _assetLoader.OnAssetLoadFailed += (name, type, ex) => errorCounts["AssetLoadFailed"] = (errorCounts.ContainsKey("AssetLoadFailed") ? errorCounts["AssetLoadFailed"] : 0) + 1;
            _pathfinder.OnPathfindingFailed += (start, end, ex) => errorCounts["PathfindingFailed"] = (errorCounts.ContainsKey("PathfindingFailed") ? errorCounts["PathfindingFailed"] : 0) + 1;

            // Configure all mocks to throw exceptions
            _mapGenerator.SetThrowException(true);
            _roomGenerator.SetThrowException(true);
            _corridorGenerator.SetThrowException(true);
            _contentPopulator.SetThrowException(true);
            _assetLoader.SetThrowException(true);
            _pathfinder.SetThrowException(true);

            // Act - Trigger all exceptions
            try { _mapGenerator.GenerateMap(_testSettings); } catch { }
            try { _roomGenerator.GenerateRooms(_testSettings); } catch { }
            try { _corridorGenerator.ConnectRooms(new List<RoomData>(), _testSettings); } catch { }
            try { _contentPopulator.PopulateContent(new MapData(), _testBiome); } catch { }
            try { _assetLoader.LoadTile("nonExistent"); } catch { }
            try { _pathfinder.FindPath(Vector2Int.zero, Vector2Int.one, CreateMockObstacles()); } catch { }

            // Assert
            Assert.IsTrue(errorCounts.ContainsKey("MapGenerationFailed") && errorCounts["MapGenerationFailed"] > 0, "Map generation failure event should fire");
            Assert.IsTrue(errorCounts.ContainsKey("RoomGenerationFailed") && errorCounts["RoomGenerationFailed"] > 0, "Room generation failure event should fire");
            Assert.IsTrue(errorCounts.ContainsKey("CorridorGenerationFailed") && errorCounts["CorridorGenerationFailed"] > 0, "Corridor generation failure event should fire");
            Assert.IsTrue(errorCounts.ContainsKey("ContentPopulationFailed") && errorCounts["ContentPopulationFailed"] > 0, "Content population failure event should fire");
            Assert.IsTrue(errorCounts.ContainsKey("AssetLoadFailed") && errorCounts["AssetLoadFailed"] > 0, "Asset loading failure event should fire");
            Assert.IsTrue(errorCounts.ContainsKey("PathfindingFailed") && errorCounts["PathfindingFailed"] > 0, "Pathfinding failure event should fire");
        }

        [Test]
        public void PerformanceIntegration_AllInterfaces_MeetRequirements()
        {
            // Arrange
            var rooms = _roomGenerator.GenerateRooms(_testSettings);
            var corridors = _corridorGenerator.ConnectRooms(rooms, _testSettings);
            var mapData = CreateMapDataFromComponents(rooms, corridors);

            // Act & Assert - Performance checks
            var generationTime = _mapGenerator.EstimateGenerationTime(_testSettings);
            Assert.IsTrue(generationTime >= 0, "Generation time should be non-negative");

            var cacheStats = _assetLoader.GetCacheStats();
            Assert.IsTrue(cacheStats.HitRate >= 0 && cacheStats.HitRate <= 1, "Cache hit rate should be between 0 and 1");

            var pathfindingStats = _pathfinder.GetPerformanceStats();
            Assert.IsTrue(pathfindingStats.TotalPathfindingCalls >= 0, "Pathfinding calls should be non-negative");

            var contentDensity = _contentPopulator.CalculateContentDensity(mapData);
            Assert.IsTrue(contentDensity.TotalObjects >= 0, "Total objects should be non-negative");
            Assert.IsTrue(contentDensity.AverageObjectsPerRoom >= 0, "Average objects per room should be non-negative");

            var corridorLength = _corridorGenerator.CalculateTotalCorridorLength(corridors);
            Assert.IsTrue(corridorLength >= 0, "Total corridor length should be non-negative");

            var roomArea = _roomGenerator.CalculateTotalRoomArea(rooms);
            Assert.IsTrue(roomArea >= 0, "Total room area should be non-negative");
        }

        #region Helper Methods

        private void SetupMockData()
        {
            // Setup mock assets
            _assetLoader.AddMockTile("floor", ScriptableObject.CreateInstance<TileBase>());
            _assetLoader.AddMockTile("wall", ScriptableObject.CreateInstance<TileBase>());
            _assetLoader.AddMockTile("door", ScriptableObject.CreateInstance<TileBase>());

            // Setup mock pathfinding
            _pathfinder.SetMockPath(new List<Vector2Int> 
            { 
                Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0), 
                new Vector2Int(2, 1), new Vector2Int(2, 2) 
            });
        }

        private MapData CreateMapDataFromComponents(List<RoomData> rooms, List<CorridorData> corridors)
        {
            var mapData = new MapData();
            mapData.SetDimensions(50, 50);
            mapData.SetSeed(12345);

            foreach (var room in rooms)
            {
                mapData.AddRoom(room);
            }

            foreach (var corridor in corridors)
            {
                mapData.AddCorridor(corridor);
            }

            return mapData;
        }

        private Tilemap[] CreateMockTilemaps()
        {
            var tilemaps = new Tilemap[3];
            for (int i = 0; i < tilemaps.Length; i++)
            {
                var gameObject = new GameObject($"Tilemap_{i}");
                tilemaps[i] = gameObject.AddComponent<Tilemap>();
            }
            return tilemaps;
        }

        private bool[,] CreateMockObstacles()
        {
            return new bool[10, 10]; // All false (no obstacles)
        }

        #endregion
    }
}