using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Base;
using OfficeMice.MapGeneration.Factories;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive sample tests demonstrating proper testing patterns for all MapGeneration interfaces.
    /// Serves as a reference for implementing additional tests and validates interface contracts.
    /// </summary>
    [TestFixture]
    public class ComprehensiveInterfaceTests : BaseTestFixture
    {
        #region IMapGenerator Tests

        [Test]
        public void IMapGenerator_GenerateMap_WithValidSettings_ReturnsValidMap()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var result = generator.GenerateMap(settings);

            // Assert
            Assert.IsNotNull(result);
            AssertValidMapData(result, settings.mapWidth, settings.mapHeight);
        }

        [Test]
        public void IMapGenerator_GenerateMap_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new MockMapGenerator();

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => generator.GenerateMap(null));
        }

        [Test]
        public void IMapGenerator_GenerateMap_WithSeed_ReturnsDeterministicResults()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");
            var seed = 12345;

            // Act
            var result1 = generator.GenerateMap(settings, seed);
            var result2 = generator.GenerateMap(settings, seed);

            // Assert
            Assert.AreEqual(result1, result2, "Same seed should produce identical results");
        }

        [UnityTest]
        public IEnumerator IMapGenerator_GenerateMapAsync_WithValidSettings_CompletesSuccessfully()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var enumerator = generator.GenerateMapAsync(settings);
            yield return AssertCoroutineCompletes(enumerator);

            // Assert
            Assert.IsTrue(enumerator.MoveNext(), "Enumerator should have a result");
            var result = enumerator.Current;
            Assert.IsNotNull(result);
            AssertValidMapData(result);
        }

        [Test]
        public void IMapGenerator_ValidateSettings_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var result = generator.ValidateSettings(settings);

            // Assert
            AssertValidationSuccess(result);
        }

        [Test]
        public void IMapGenerator_EstimateGenerationTime_WithValidSettings_ReturnsPositiveTime()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var result = generator.EstimateGenerationTime(settings);

            // Assert
            Assert.IsTrue(result > 0, "Generation time should be positive");
        }

        #endregion

        #region IRoomGenerator Tests

        [Test]
        public void IRoomGenerator_GenerateRooms_WithValidSettings_ReturnsRooms()
        {
            // Arrange
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var result = generator.GenerateRooms(settings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Should generate at least one room");
            Assert.IsTrue(result.Count <= settings.maxRoomCount, "Should not exceed max room count");
        }

        [Test]
        public void IRoomGenerator_GenerateRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new MockRoomGenerator();

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => generator.GenerateRooms(null));
        }

        [Test]
        public void IRoomGenerator_GenerateRooms_WithMinimalSettings_ReturnsValidRooms()
        {
            // Arrange
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("minimal");

            // Act
            var result = generator.GenerateRooms(settings);

            // Assert
            Assert.IsNotNull(result);
            foreach (var room in result)
            {
                Assert.IsTrue(room.Bounds.width >= settings.minRoomSize, "Room width should meet minimum size");
                Assert.IsTrue(room.Bounds.height >= settings.minRoomSize, "Room height should meet minimum size");
                Assert.IsTrue(room.Bounds.width <= settings.maxRoomSize, "Room width should not exceed maximum size");
                Assert.IsTrue(room.Bounds.height <= settings.maxRoomSize, "Room height should not exceed maximum size");
            }
        }

        #endregion

        #region ICorridorGenerator Tests

        [Test]
        public void ICorridorGenerator_ConnectRooms_WithValidRooms_ReturnsCorridors()
        {
            // Arrange
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("standard");
            var rooms = CreateTestMapData("multiple_rooms").Rooms;

            // Act
            var result = generator.ConnectRooms(rooms, settings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count >= 0, "Should return non-null corridor list");
        }

        [Test]
        public void ICorridorGenerator_ConnectRooms_WithNullRooms_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("standard");

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => generator.ConnectRooms(null, settings));
        }

        [Test]
        public void ICorridorGenerator_ConnectRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new MockCorridorGenerator();
            var rooms = CreateTestMapData("multiple_rooms").Rooms;

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => generator.ConnectRooms(rooms, null));
        }

        [Test]
        public void ICorridorGenerator_ConnectRooms_WithEmptyRooms_ReturnsEmptyCorridors()
        {
            // Arrange
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("standard");
            var emptyRooms = new System.Collections.Generic.List<RoomData>();

            // Act
            var result = generator.ConnectRooms(emptyRooms, settings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count, "Empty room list should result in empty corridor list");
        }

        #endregion

        #region IContentPopulator Tests

        [Test]
        public void IContentPopulator_PopulateContent_WithValidMap_DoesNotThrow()
        {
            // Arrange
            var populator = new MockContentPopulator();
            var map = CreateTestMapData("multiple_rooms");
            var biome = MapGenerationTestDataFactory.CreateTestBiomeConfiguration();

            // Act & Assert
            Assert.DoesNotThrow(() => populator.PopulateContent(map, biome));
        }

        [Test]
        public void IContentPopulator_PopulateContent_WithNullMap_ThrowsArgumentNullException()
        {
            // Arrange
            var populator = new MockContentPopulator();
            var biome = MapGenerationTestDataFactory.CreateTestBiomeConfiguration();

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => populator.PopulateContent(null, biome));
        }

        [Test]
        public void IContentPopulator_PopulateContent_WithNullBiome_ThrowsArgumentNullException()
        {
            // Arrange
            var populator = new MockContentPopulator();
            var map = CreateTestMapData("multiple_rooms");

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => populator.PopulateContent(map, null));
        }

        #endregion

        #region IPathfinder Tests

        [Test]
        public void IPathfinder_FindPath_WithValidStartEnd_ReturnsPath()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(10, 10);
            var obstacles = new bool[20, 20]; // No obstacles

            // Act
            var result = pathfinder.FindPath(start, end, obstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Path should contain at least one point");
            Assert.AreEqual(start, result[0], "Path should start at the start position");
            Assert.AreEqual(end, result[result.Count - 1], "Path should end at the end position");
        }

        [Test]
        public void IPathfinder_FindPath_WithObstacles_ReturnsValidPath()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(10, 10);
            var obstacles = new bool[20, 20];
            obstacles[5, 5] = true; // Add an obstacle

            // Act
            var result = pathfinder.FindPath(start, end, obstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Path should still be found with obstacles");
        }

        [Test]
        public void IPathfinder_FindPath_WithSameStartEnd_ReturnsSinglePoint()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(5, 5);
            var obstacles = new bool[10, 10];

            // Act
            var result = pathfinder.FindPath(start, end, obstacles);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count, "Path with same start and end should contain one point");
            Assert.AreEqual(start, result[0], "Path point should match start/end position");
        }

        #endregion

        #region ITileRenderer Tests

        [Test]
        public void ITileRenderer_RenderMap_WithValidMap_DoesNotThrow()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var map = CreateTestMapData("single_room");
            var tilemaps = new UnityEngine.Tilemaps.Tilemap[2]; // Mock tilemaps

            // Act & Assert
            Assert.DoesNotThrow(() => renderer.RenderMap(map, tilemaps));
        }

        [Test]
        public void ITileRenderer_RenderMap_WithNullMap_ThrowsArgumentNullException()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var tilemaps = new UnityEngine.Tilemaps.Tilemap[2];

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => renderer.RenderMap(null, tilemaps));
        }

        [Test]
        public void ITileRenderer_RenderMap_WithNullTilemaps_ThrowsArgumentNullException()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var map = CreateTestMapData("single_room");

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => renderer.RenderMap(map, null));
        }

        #endregion

        #region IAssetLoader Tests

        [Test]
        public void IAssetLoader_LoadTile_WithValidName_ReturnsTile()
        {
            // Arrange
            var loader = new MockAssetLoader();
            var tileName = "test_tile";

            // Act
            var result = loader.LoadTile(tileName);

            // Assert
            Assert.IsNotNull(result, "Should return a tile for valid name");
        }

        [Test]
        public void IAssetLoader_LoadTile_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var loader = new MockAssetLoader();

            // Act & Assert
            AssertThrows<ArgumentNullException>(() => loader.LoadTile(null));
        }

        [Test]
        public void IAssetLoader_LoadTile_WithEmptyName_ReturnsNull()
        {
            // Arrange
            var loader = new MockAssetLoader();

            // Act
            var result = loader.LoadTile("");

            // Assert
            Assert.IsNull(result, "Should return null for empty tile name");
        }

        [Test]
        public void IAssetLoader_PreloadAssets_WithValidAssets_DoesNotThrow()
        {
            // Arrange
            var loader = new MockAssetLoader();
            var assetNames = new[] { "tile1", "tile2", "tile3" };

            // Act & Assert
            Assert.DoesNotThrow(() => loader.PreloadAssets(assetNames));
        }

        [Test]
        public void IAssetLoader_ClearCache_DoesNotThrow()
        {
            // Arrange
            var loader = new MockAssetLoader();

            // Act & Assert
            Assert.DoesNotThrow(() => loader.ClearCache());
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_AllInterfaces_WorkTogether()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var roomGenerator = new MockRoomGenerator();
            var corridorGenerator = new MockCorridorGenerator();
            var contentPopulator = new MockContentPopulator();
            var settings = CreateTestSettings("standard");

            // Act
            var rooms = roomGenerator.GenerateRooms(settings);
            var corridors = corridorGenerator.ConnectRooms(rooms, settings);
            var map = mapGenerator.GenerateMap(settings);
            var biome = MapGenerationTestDataFactory.CreateTestBiomeConfiguration();
            contentPopulator.PopulateContent(map, biome);

            // Assert
            Assert.IsNotNull(rooms);
            Assert.IsNotNull(corridors);
            Assert.IsNotNull(map);
            Assert.IsTrue(rooms.Count > 0, "Should have generated rooms");
        }

        [UnityTest]
        public IEnumerator Integration_AsyncGeneration_CompletesSuccessfully()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var enumerator = mapGenerator.GenerateMapAsync(settings);
            yield return AssertCoroutineCompletes(enumerator);

            // Assert
            Assert.IsTrue(enumerator.MoveNext(), "Should have a result");
            var result = enumerator.Current;
            Assert.IsNotNull(result);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void Performance_MapGeneration_CompletesWithinTimeLimit()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            // Act & Assert
            AssertPerformance(() => generator.GenerateMap(settings), 1000f, // 1 second max
                "Map generation should complete within 1 second");
        }

        [Test]
        public void Performance_RoomGeneration_CompletesWithinTimeLimit()
        {
            // Arrange
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("performance");

            // Act & Assert
            AssertPerformance(() => generator.GenerateRooms(settings), 500f, // 0.5 seconds max
                "Room generation should complete within 0.5 seconds");
        }

        [Test]
        public void Performance_Pathfinding_CompletesWithinTimeLimit()
        {
            // Arrange
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(100, 100);
            var obstacles = new bool[200, 200];

            // Act & Assert
            AssertPerformance(() => pathfinder.FindPath(start, end, obstacles), 100f, // 0.1 seconds max
                "Pathfinding should complete within 0.1 seconds");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void EdgeCase_MinimalMapSettings_GeneratesValidMap()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("edge_case");

            // Act
            var result = generator.GenerateMap(settings);

            // Assert
            Assert.IsNotNull(result);
            AssertValidMapData(result, settings.mapWidth, settings.mapHeight);
        }

        [Test]
        public void EdgeCase_ZeroRoomCount_HandlesGracefully()
        {
            // Arrange
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("standard");
            settings.maxRoomCount = 0;

            // Act
            var result = generator.GenerateRooms(settings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count, "Should return empty room list for zero room count");
        }

        [Test]
        public void EdgeCase_LargeMap_HandlesMemoryEfficiently()
        {
            // Arrange
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            // Act & Assert
            AssertMemoryUsage(() => generator.GenerateMap(settings), 50f, // 50MB max
                "Large map generation should use less than 50MB memory");
        }

        #endregion
    }
}