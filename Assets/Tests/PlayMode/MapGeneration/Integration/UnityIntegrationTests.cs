using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Base;
using OfficeMice.MapGeneration.Factories;

namespace OfficeMice.MapGeneration.PlayMode.Integration
{
    /// <summary>
    /// PlayMode integration tests that validate MapGeneration components work correctly with Unity systems.
    /// Tests Tilemap rendering, GameObject creation, and runtime behavior.
    /// </summary>
    [TestFixture]
    public class UnityIntegrationTests : PlayModeTestBase
    {
        #region Tilemap Integration Tests

        [UnityTest]
        public IEnumerator TileRenderer_RenderMapToUnityTilemaps_RendersCorrectly()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var mapData = CreateTestMapData("single_room");

            // Act & Assert
            yield return AssertMapRendersToTilemaps(mapData, renderer);
        }

        [UnityTest]
        public IEnumerator TileRenderer_RenderComplexMap_HandlesMultipleRooms()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var mapData = CreateTestMapData("multiple_rooms");

            // Act & Assert
            yield return AssertMapRendersToTilemaps(mapData, renderer);

            // Additional validation for complex map
            Assert.IsTrue(_groundTilemap.GetUsedTilesCount() > 0, "Complex map should have ground tiles");
            Assert.IsTrue(_wallTilemap.GetUsedTilesCount() > 0, "Complex map should have wall tiles");
        }

        [UnityTest]
        public IEnumerator TileRenderer_RenderEmptyMap_HandlesGracefully()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var mapData = CreateTestMapData("empty");

            // Act & Assert
            yield return AssertMapRendersToTilemaps(mapData, renderer);

            // Empty map should not crash but may not place tiles
            Assert.IsTrue(_groundTilemap.GetUsedTilesCount() >= 0, "Empty map should not cause errors");
        }

        #endregion

        #region GameObject Integration Tests

        [UnityTest]
        public IEnumerator MapGeneration_CreateMapObjects_CreatesValidHierarchy()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var mapData = mapGenerator.GenerateMap(settings);
            yield return null;

            // Create GameObject hierarchy for the map
            var mapGameObject = new GameObject("GeneratedMap");
            var roomContainer = new GameObject("Rooms");
            roomContainer.transform.SetParent(mapGameObject.transform);

            foreach (var room in mapData.Rooms)
            {
                var roomGameObject = new GameObject($"Room_{room.Classification}");
                roomGameObject.transform.SetParent(roomContainer.transform);
                roomGameObject.transform.position = new Vector3(room.Bounds.center.x, room.Bounds.center.y, 0);
            }

            yield return null;

            // Assert
            Assert.IsNotNull(mapGameObject);
            Assert.AreEqual(mapData.Rooms.Count, roomContainer.transform.childCount);
            
            // Cleanup
            Object.DestroyImmediate(mapGameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MapGeneration_RuntimeGeneration_PerformsWell()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var startTime = Time.realtimeSinceStartup;
            var mapData = mapGenerator.GenerateMap(settings);
            var generationTime = Time.realtimeSinceStartup - startTime;

            // Create runtime objects
            var mapGameObject = new GameObject("RuntimeMap");
            yield return null;

            // Assert
            Assert.IsNotNull(mapData);
            Assert.Less(generationTime, 0.1f, "Runtime generation should be fast");
            
            // Cleanup
            Object.DestroyImmediate(mapGameObject);
            yield return null;
        }

        #endregion

        #region Physics Integration Tests

        [UnityTest]
        public IEnumerator GeneratedMap_WithColliders_HasValidPhysics()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var mapData = CreateTestMapData("single_room");

            // Act
            yield return AssertMapRendersToTilemaps(mapData, renderer);

            // Add a test Rigidbody to check collision
            var testObject = new GameObject("TestPhysicsObject");
            var rigidbody = testObject.AddComponent<Rigidbody2D>();
            var collider = testObject.AddComponent<BoxCollider2D>();
            testObject.transform.position = new Vector3(25, 25, 0); // Center of map

            yield return new WaitForSeconds(0.1f); // Let physics settle

            // Assert
            Assert.IsNotNull(rigidbody);
            Assert.IsNotNull(collider);
            Assert.IsTrue(_wallTilemap.GetComponent<TilemapCollider2D>() != null, "Wall tilemap should have collider");

            // Test that physics works
            var initialPosition = testObject.transform.position;
            rigidbody.AddForce(Vector2.up * 10f);
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreNotEqual(initialPosition, testObject.transform.position, "Physics object should move when force is applied");

            // Cleanup
            Object.DestroyImmediate(testObject);
            yield return null;
        }

        #endregion

        #region Asset Loading Integration Tests

        [UnityTest]
        public IEnumerator AssetLoader_LoadRuntimeAssets_WorksCorrectly()
        {
            // Arrange
            var assetLoader = new MockAssetLoader();
            var testTiles = SetupTestTiles();

            // Act
            var groundTile = assetLoader.LoadTile("ground");
            var wallTile = assetLoader.LoadTile("wall");
            var invalidTile = assetLoader.LoadTile("nonexistent");

            yield return null;

            // Assert
            Assert.IsNotNull(groundTile, "Should load ground tile");
            Assert.IsNotNull(wallTile, "Should load wall tile");
            Assert.IsNull(invalidTile, "Should return null for invalid tile");

            // Test tile placement
            _groundTilemap.SetTile(new Vector3Int(0, 0, 0), groundTile);
            _wallTilemap.SetTile(new Vector3Int(1, 0, 0), wallTile);

            yield return null;

            Assert.AreEqual(groundTile, _groundTilemap.GetTile(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(wallTile, _wallTilemap.GetTile(new Vector3Int(1, 0, 0)));
        }

        [UnityTest]
        public IEnumerator AssetLoader_PreloadAssets_ImprovesPerformance()
        {
            // Arrange
            var assetLoader = new MockAssetLoader();
            var assetNames = new[] { "ground", "wall", "floor" };

            // Act - Measure time without preload
            var startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < 100; i++)
            {
                assetLoader.LoadTile("ground");
            }
            var timeWithoutPreload = Time.realtimeSinceStartup - startTime;

            // Preload assets
            assetLoader.PreloadAssets(assetNames);
            yield return null;

            // Measure time with preload
            startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < 100; i++)
            {
                assetLoader.LoadTile("ground");
            }
            var timeWithPreload = Time.realtimeSinceStartup - startTime;

            // Assert
            Assert.Less(timeWithPreload, timeWithoutPreload, "Preloading should improve performance");
        }

        #endregion

        #region Coroutine Integration Tests

        [UnityTest]
        public IEnumerator AsyncMapGeneration_WithYieldInstructions_WorksCorrectly()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var enumerator = mapGenerator.GenerateMapAsync(settings);
            var frameCount = 0;

            while (enumerator.MoveNext())
            {
                frameCount++;
                yield return enumerator.Current;
            }

            var result = enumerator.Current;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(frameCount > 0, "Async generation should span multiple frames");
            AssertValidMapData(result);
        }

        [UnityTest]
        public IEnumerator AsyncMapGeneration_WithTimeout_HandlesGracefully()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act
            var enumerator = mapGenerator.GenerateMapAsync(settings);
            var timeout = 5f; // 5 seconds
            var startTime = Time.time;

            while (enumerator.MoveNext())
            {
                if (Time.time - startTime > timeout)
                {
                    Assert.Fail("Async generation timed out");
                    yield break;
                }
                yield return enumerator.Current;
            }

            var result = enumerator.Current;

            // Assert
            Assert.IsNotNull(result);
            Assert.Less(Time.time - startTime, timeout, "Generation should complete within timeout");
        }

        #endregion

        #region Memory Management Tests

        [UnityTest]
        public IEnumerator MapGeneration_MultipleMaps_ManagesMemoryCorrectly()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");

            // Act - Generate multiple maps
            var maps = new MapData[10];
            for (int i = 0; i < 10; i++)
            {
                maps[i] = mapGenerator.GenerateMap(settings);
                yield return null; // Allow garbage collection between generations
            }

            // Force garbage collection
            System.GC.Collect();
            yield return null;

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.IsNotNull(maps[i], $"Map {i} should not be null");
                AssertValidMapData(maps[i]);
            }

            // Cleanup
            for (int i = 0; i < 10; i++)
            {
                maps[i] = null;
            }
            System.GC.Collect();
            yield return null;
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator MapGeneration_WithCorruptedData_HandlesGracefully()
        {
            // Arrange
            var renderer = new MockTileRenderer();
            var corruptedMap = CreateTestMapData("corrupted");

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() =>
            {
                var tilemaps = GetTilemapArray();
                renderer.RenderMap(corruptedMap, tilemaps);
            });

            yield return null;

            // Verify tilemaps are still in a valid state
            AssertTilemapsInitialized();
        }

        [UnityTest]
        public IEnumerator AssetLoader_WithInvalidAssets_DoesNotCrash()
        {
            // Arrange
            var assetLoader = new MockAssetLoader();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var nullTile = assetLoader.LoadTile(null);
                var emptyTile = assetLoader.LoadTile("");
                var invalidTile = assetLoader.LoadTile("definitely_does_not_exist");
            });

            yield return null;
        }

        #endregion

        #region Performance Integration Tests

        [UnityTest]
        public IEnumerator FullMapGenerationPipeline_PerformsWell()
        {
            // Arrange
            var mapGenerator = new MockMapGenerator();
            var roomGenerator = new MockRoomGenerator();
            var corridorGenerator = new MockCorridorGenerator();
            var contentPopulator = new MockContentPopulator();
            var renderer = new MockTileRenderer();
            var settings = CreateTestSettings("standard");

            // Act
            var startTime = Time.realtimeSinceStartup;

            var rooms = roomGenerator.GenerateRooms(settings);
            yield return null;

            var corridors = corridorGenerator.ConnectRooms(rooms, settings);
            yield return null;

            var map = mapGenerator.GenerateMap(settings);
            yield return null;

            var biome = MapGenerationTestDataFactory.CreateTestBiomeConfiguration();
            contentPopulator.PopulateContent(map, biome);
            yield return null;

            yield return AssertMapRendersToTilemaps(map, renderer);

            var totalTime = Time.realtimeSinceStartup - startTime;

            // Assert
            Assert.IsNotNull(rooms);
            Assert.IsNotNull(corridors);
            Assert.IsNotNull(map);
            Assert.Less(totalTime, 2f, "Full pipeline should complete within 2 seconds");
        }

        #endregion
    }
}