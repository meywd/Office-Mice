using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.EditMode
{
    [TestFixture]
    public class FurniturePlacerTests
    {
        private FurniturePlacer _furniturePlacer;
        private MockAssetLoader _mockAssetLoader;
        private MapData _testMap;
        private BiomeConfiguration _testBiome;

        [SetUp]
        public void SetUp()
        {
            _mockAssetLoader = new MockAssetLoader();
            _furniturePlacer = new FurniturePlacer(_mockAssetLoader, 42); // Fixed seed for deterministic tests
            
            _testMap = CreateTestMap();
            _testBiome = CreateTestBiome();
        }

        [TearDown]
        public void TearDown()
        {
            _furniturePlacer = null;
            _mockAssetLoader = null;
            _testMap = null;
            _testBiome = null;
        }

        [Test]
        public void Constructor_WithNullAssetLoader_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FurniturePlacer(null));
        }

        [Test]
        public void PlaceFurniture_WithValidMap_ReturnsFurnitureList()
        {
            // Arrange
            var expectedRoomCount = _testMap.Rooms.Count;

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(_testMap, _testBiome);

            // Assert
            Assert.IsNotNull(placedFurniture);
            Assert.IsTrue(placedFurniture.Count > 0, "Should place at least some furniture");
            
            // Verify all furniture is in valid rooms
            foreach (var furniture in placedFurniture)
            {
                var room = _testMap.GetRoomByID(furniture.RoomID);
                Assert.IsNotNull(room, $"Furniture {furniture.FurnitureID} references invalid room {furniture.RoomID}");
                Assert.IsTrue(room.ContainsPoint(furniture.Position),
                    $"Furniture {furniture.FurnitureID} at {furniture.Position} is outside room bounds");
            }
        }

        [Test]
        public void PlaceFurniture_WithNullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _furniturePlacer.PlaceFurniture(null, _testBiome));
        }

        [Test]
        public void PlaceFurniture_WithNullBiome_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _furniturePlacer.PlaceFurniture(_testMap, null));
        }

        [Test]
        public void PlaceFurnitureInRoom_WithOfficeRoom_PlacesOfficeFurniture()
        {
            // Arrange
            var officeRoom = _testMap.Rooms.FirstOrDefault(r => r.Classification == RoomClassification.Office);
            Assert.IsNotNull(officeRoom, "Test map should have an office room");

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurnitureInRoom(officeRoom, _testBiome);

            // Assert
            Assert.IsNotNull(placedFurniture);
            
            // Should place office-type furniture
            var hasDesk = placedFurniture.Exists(f => f.FurnitureType.Contains("Desk"));
            var hasChair = placedFurniture.Exists(f => f.FurnitureType.Contains("Chair"));
            
            Assert.IsTrue(hasDesk || hasChair, "Should place office furniture (desk or chair)");
        }

        [Test]
        public void PlaceFurnitureInRoom_WithConferenceRoom_PlacesConferenceFurniture()
        {
            // Arrange
            var conferenceRoom = _testMap.Rooms.FirstOrDefault(r => r.Classification == RoomClassification.Conference);
            Assert.IsNotNull(conferenceRoom, "Test map should have a conference room");

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurnitureInRoom(conferenceRoom, _testBiome);

            // Assert
            Assert.IsNotNull(placedFurniture);
            
            // Should place conference-type furniture
            var hasTable = placedFurniture.Exists(f => f.FurnitureType.Contains("ConferenceTable"));
            Assert.IsTrue(hasTable, "Should place conference table in conference room");
        }

        [Test]
        public void FindValidPositions_WithEmptyRoom_ReturnsValidPositions()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 10, 10));
            var objectSize = new Vector2Int(2, 2);
            var existingObjects = new List<PlacedObjectData>();

            // Act
            var validPositions = _furniturePlacer.FindValidPositions(room, objectSize, existingObjects, 1);

            // Assert
            Assert.IsNotNull(validPositions);
            Assert.IsTrue(validPositions.Count > 0, "Should find valid positions in empty room");
            
            // Verify all positions are valid
            foreach (var position in validPositions)
            {
                var objectBounds = new RectInt(position, objectSize);
                Assert.IsTrue(room.Bounds.Contains(objectBounds.min), 
                    $"Position {position} should be inside room bounds");
                Assert.IsTrue(room.Bounds.Contains(objectBounds.max - Vector2Int.one), 
                    $"Position {position} should fit completely inside room");
            }
        }

        [Test]
        public void FindValidPositions_WithExistingObjects_AvoidsCollisions()
        {
            // Arrange
            var room = new RoomData(new RectInt(0, 0, 10, 10));
            var objectSize = new Vector2Int(2, 2);
            var existingObjects = new List<PlacedObjectData>
            {
                new TestFurnitureData("existing1", "Test", 0, new Vector2Int(2, 2), objectSize)
            };

            // Act
            var validPositions = _furniturePlacer.FindValidPositions(room, objectSize, existingObjects, 0);

            // Assert
            Assert.IsNotNull(validPositions);
            
            // Verify no valid positions overlap with existing objects
            foreach (var position in validPositions)
            {
                var newBounds = new RectInt(position, objectSize);
                var existingBounds = new RectInt(existingObjects[0].Position, existingObjects[0].Size);
                Assert.IsFalse(newBounds.Overlaps(existingBounds), 
                    $"Position {position} should not overlap with existing object");
            }
        }

        [Test]
        public void ValidateFurniturePlacement_WithValidMap_ReturnsSuccess()
        {
            // Arrange
            _furniturePlacer.PlaceFurniture(_testMap, _testBiome);

            // Act
            var validationResult = _furniturePlacer.ValidateFurniturePlacement(_testMap);

            // Assert
            Assert.IsNotNull(validationResult);
            Assert.IsTrue(validationResult.IsValid, "Map with properly placed furniture should be valid");
        }

        [Test]
        public void SetSeed_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Arrange
            var seed1 = 123;
            var seed2 = 456;

            // Act
            _furniturePlacer.SetSeed(seed1);
            var furniture1 = _furniturePlacer.PlaceFurniture(_testMap, _testBiome);
            
            _furniturePlacer.SetSeed(seed2);
            var furniture2 = _furniturePlacer.PlaceFurniture(_testMap, _testBiome);

            // Assert
            Assert.AreNotEqual(furniture1.Count, furniture2.Count, 
                "Different seeds should produce different furniture counts or positions");
        }

        [Test]
        public void AddPlacementRule_WithValidRule_AddsToRules()
        {
            // Arrange
            var roomType = RoomClassification.Office;
            var rule = new FurniturePlacementRule("TestFurniture");

            // Act
            _furniturePlacer.AddPlacementRule(roomType, rule);

            // Verify by placing furniture - should use the new rule
            var officeRoom = _testMap.Rooms.FirstOrDefault(r => r.Classification == RoomClassification.Office);
            var placedFurniture = _furniturePlacer.PlaceFurnitureInRoom(officeRoom, _testBiome);

            // Assert
            Assert.IsNotNull(placedFurniture);
            // The exact verification depends on the rule implementation
        }

        [Test]
        public void RegisterFurnitureTemplate_WithValidTemplate_RegistersTemplate()
        {
            // Arrange
            var template = new FurnitureData("test_template", "TestType", "TestPrefab", 0, Vector2Int.zero, Vector2Int.one);

            // Act
            _furniturePlacer.RegisterFurnitureTemplate(template);

            // Assert - no exception should be thrown
            Assert.Pass("Template registration should succeed");
        }

        [Test]
        public void GetPerformanceStats_AfterPlacement_ReturnsValidStats()
        {
            // Arrange
            _furniturePlacer.PlaceFurniture(_testMap, _testBiome);

            // Act
            var stats = _furniturePlacer.GetPerformanceStats();

            // Assert
            Assert.IsTrue(stats.roomsProcessed > 0, "Should have processed rooms");
            Assert.IsTrue(stats.furniturePlaced >= 0, "Furniture count should be non-negative");
            Assert.IsTrue(stats.totalMs >= 0, "Total time should be non-negative");
            Assert.IsTrue(stats.avgMsPerRoom >= 0, "Average time per room should be non-negative");
        }

        [Test]
        public void FurniturePlacementEvents_WhenFurniturePlaced_FiresEvents()
        {
            // Arrange
            bool eventFired = false;
            FurnitureData placedFurniture = null;
            
            _furniturePlacer.OnFurniturePlaced += (furniture) =>
            {
                eventFired = true;
                placedFurniture = furniture;
            };

            // Act
            var furnitureList = _furniturePlacer.PlaceFurniture(_testMap, _testBiome);

            // Assert
            Assert.IsTrue(eventFired, "OnFurniturePlaced event should be fired");
            Assert.IsNotNull(placedFurniture, "Event should provide furniture data");
            Assert.IsTrue(furnitureList.Contains(placedFurniture), "Event furniture should be in returned list");
        }

        [Test]
        public void PlaceFurniture_Performance_TargetsMet()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var placedFurniture = _furniturePlacer.PlaceFurniture(_testMap, _testBiome);
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200, 
                $"Furniture placement should complete within 200ms, took {stopwatch.ElapsedMilliseconds}ms");
            
            var stats = _furniturePlacer.GetPerformanceStats();
            Assert.IsTrue(stats.avgMsPerRoom < 50, 
                $"Average time per room should be under 50ms, was {stats.avgMsPerRoom}ms");
        }

        #region Helper Methods

        private MapData CreateTestMap()
        {
            var map = new MapData(100, 100, 12345);
            
            // Add test rooms
            var rooms = new List<RoomData>
            {
                new RoomData(new RectInt(10, 10, 8, 6)) { RoomID = 1, SetClassification(RoomClassification.Office) },
                new RoomData(new RectInt(30, 10, 12, 8)) { RoomID = 2, SetClassification(RoomClassification.Conference) },
                new RoomData(new RectInt(10, 30, 6, 6)) { RoomID = 3, SetClassification(RoomClassification.BreakRoom) },
                new RoomData(new RectInt(30, 30, 8, 8)) { RoomID = 4, SetClassification(RoomClassification.Storage) }
            };

            foreach (var room in rooms)
            {
                map.AddRoom(room);
            }

            return map;
        }

        private BiomeConfiguration CreateTestBiome()
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            // Set basic properties - the exact configuration depends on BiomeConfiguration implementation
            return biome;
        }

        #endregion

        #region Mock Classes

        private class TestFurnitureData : PlacedObjectData
        {
            public TestFurnitureData(string objectID, string objectType, int roomID,
                                    Vector2Int position, Vector2Int size)
                : base(objectID, objectType, roomID, position, size)
            {
            }

            public override PlacedObjectData Clone()
            {
                return new TestFurnitureData(ObjectID, ObjectType, RoomID, Position, Size);
            }
        }

        #endregion
    }

    /// <summary>
    /// Mock asset loader for testing purposes. Shared across test classes.
    /// </summary>
    internal class MockAssetLoader : IAssetLoader
    {
        public event Action<string, Type> OnAssetLoaded;
        public event Action<string, Type, Exception> OnAssetLoadFailed;
        public event Action OnCacheCleared;

        public TileBase LoadTile(string tileName)
        {
            return null; // Mock implementation
        }

        public GameObject LoadPrefab(string prefabName)
        {
            var go = new GameObject("MockFurniture");
            return go;
        }

        public T LoadScriptableObject<T>(string assetName) where T : ScriptableObject
        {
            return null; // Mock implementation
        }

        public void PreloadAssets(List<string> assetNames, Type assetType)
        {
            // Mock implementation
        }

        public void ClearCache()
        {
            // Mock implementation
            OnCacheCleared?.Invoke();
        }

        public CacheStats GetCacheStats()
        {
            return new CacheStats();
        }

        public bool IsAssetCached(string assetName, Type assetType)
        {
            return true; // Mock implementation
        }

        public T[] LoadAllAssets<T>() where T : UnityEngine.Object
        {
            return new T[0];
        }

        public void LoadAssetAsync(string assetName, Type assetType, Action<UnityEngine.Object> callback)
        {
            callback?.Invoke(null); // Mock implementation
        }

        public ValidationResult ValidateRequiredAssets(List<string> requiredAssets, Type assetType)
        {
            return new ValidationResult(); // Mock implementation - all valid
        }
    }
}