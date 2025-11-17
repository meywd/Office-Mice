using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using OfficeMice.MapGeneration.Generators;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Generators
{
    [TestFixture]
    public class BSPGeneratorTests
    {
        private BSPGenerator _generator;
        private MapGenerationSettings _defaultSettings;

        [SetUp]
        public void SetUp()
        {
            _generator = new BSPGenerator();
            _defaultSettings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 100, 100),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 10,
                    MaxDepth = 5,
                    SplitPreference = SplitPreference.Alternate,
                    SplitPositionVariation = 0.3f,
                    StopSplittingChance = 0.1f,
                    RoomSizeRatio = 0.8f,
                    RoomPositionVariation = 0.1f
                }
            };
        }

        [Test]
        public void GenerateRooms_WithValidSettings_ReturnsRooms()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            var rooms = _generator.GenerateRooms(settings);

            // Assert
            Assert.IsNotNull(rooms);
            Assert.IsTrue(rooms.Count > 0);
        }

        [Test]
        public void GenerateRooms_WithSeed_IsDeterministic()
        {
            // Arrange
            var settings = _defaultSettings;
            int seed = 12345;

            // Act
            var rooms1 = _generator.GenerateRooms(settings, seed);
            var rooms2 = _generator.GenerateRooms(settings, seed);

            // Assert
            Assert.AreEqual(rooms1.Count, rooms2.Count);
            
            for (int i = 0; i < rooms1.Count; i++)
            {
                Assert.AreEqual(rooms1[i].Bounds, rooms2[i].Bounds);
                Assert.AreEqual(rooms1[i].RoomID, rooms2[i].RoomID);
            }
        }

        [Test]
        public void GenerateRooms_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            var rooms1 = _generator.GenerateRooms(settings, 11111);
            var rooms2 = _generator.GenerateRooms(settings, 22222);

            // Assert
            // Results should be different (though there's a small chance they could be the same)
            bool hasDifference = false;
            if (rooms1.Count != rooms2.Count)
            {
                hasDifference = true;
            }
            else
            {
                for (int i = 0; i < rooms1.Count; i++)
                {
                    if (rooms1[i].Bounds != rooms2[i].Bounds)
                    {
                        hasDifference = true;
                        break;
                    }
                }
            }
            
            Assert.IsTrue(hasDifference, "Different seeds should produce different room layouts");
        }

        [Test]
        public void GenerateRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            MapGenerationSettings nullSettings = null;

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _generator.GenerateRooms(nullSettings));
        }

        [Test]
        public void GenerateRooms_RoomsWithinBounds()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            var rooms = _generator.GenerateRooms(settings);

            // Assert
            foreach (var room in rooms)
            {
                Assert.IsTrue(settings.mapBounds.Contains(room.Bounds.min));
                Assert.IsTrue(settings.mapBounds.Contains(room.Bounds.max - Vector2Int.one));
            }
        }

        [Test]
        public void GenerateRooms_RoomsMeetMinimumSize()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            var rooms = _generator.GenerateRooms(settings);

            // Assert
            foreach (var room in rooms)
            {
                Assert.IsTrue(room.Bounds.width >= 3, "Room width should be at least 3");
                Assert.IsTrue(room.Bounds.height >= 3, "Room height should be at least 3");
            }
        }

        [Test]
        public void GenerateRooms_RoomsDoNotOverlap()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            var rooms = _generator.GenerateRooms(settings);

            // Assert
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    Assert.IsFalse(rooms[i].OverlapsWith(rooms[j].Bounds), 
                        $"Room {rooms[i].RoomID} overlaps with Room {rooms[j].RoomID}");
                }
            }
        }

        [Test]
        public void ValidateRoomPlacement_WithValidRooms_ReturnsNoErrors()
        {
            // Arrange
            var settings = _defaultSettings;
            var rooms = _generator.GenerateRooms(settings);

            // Act
            var result = _generator.ValidateRoomPlacement(rooms, settings);

            // Assert
            Assert.IsFalse(result.HasErrors, $"Validation errors: {result.GetErrorSummary()}");
        }

        [Test]
        public void ValidateRoomPlacement_WithNullRooms_ReturnsError()
        {
            // Arrange
            List<RoomData> nullRooms = null;

            // Act
            var result = _generator.ValidateRoomPlacement(nullRooms, _defaultSettings);

            // Assert
            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void CalculateTotalRoomArea_ReturnsCorrectSum()
        {
            // Arrange
            var settings = _defaultSettings;
            var rooms = _generator.GenerateRooms(settings);

            // Act
            var totalArea = _generator.CalculateTotalRoomArea(rooms);
            var expectedArea = rooms.Sum(room => room.Area);

            // Assert
            Assert.AreEqual(expectedArea, totalArea);
        }

        [Test]
        public void GetRootNode_AfterGeneration_ReturnsValidNode()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            _generator.GenerateRooms(settings);
            var rootNode = _generator.GetRootNode();

            // Assert
            Assert.IsNotNull(rootNode);
            Assert.AreEqual(settings.mapBounds, rootNode.Bounds);
        }

        [Test]
        public void GetLastSeed_AfterGeneration_ReturnsCorrectSeed()
        {
            // Arrange
            var settings = _defaultSettings;
            int expectedSeed = 54321;

            // Act
            _generator.GenerateRooms(settings, expectedSeed);
            var actualSeed = _generator.GetLastSeed();

            // Assert
            Assert.AreEqual(expectedSeed, actualSeed);
        }

        [Test]
        public void GetStatistics_AfterGeneration_ReturnsValidStats()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            _generator.GenerateRooms(settings);
            var stats = _generator.GetStatistics();

            // Assert
            Assert.IsTrue(stats.TotalNodes > 0);
            Assert.IsTrue(stats.LeafNodes > 0);
            Assert.IsTrue(stats.RoomsGenerated > 0);
            Assert.IsTrue(stats.MaxDepth >= 0);
            Assert.IsTrue(stats.TotalRoomArea > 0);
        }

        [Test]
        public void ValidateBSPStructure_AfterGeneration_ReturnsNoErrors()
        {
            // Arrange
            var settings = _defaultSettings;

            // Act
            _generator.GenerateRooms(settings);
            var result = _generator.ValidateBSPStructure();

            // Assert
            Assert.IsFalse(result.HasErrors, $"BSP validation errors: {result.GetErrorSummary()}");
        }

        [Test]
        public void GenerateRooms_WithSmallBounds_HandlesCorrectly()
        {
            // Arrange
            var smallSettings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 20, 20),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 6,
                    MaxDepth = 3,
                    RoomSizeRatio = 0.7f
                }
            };

            // Act
            var rooms = _generator.GenerateRooms(smallSettings);

            // Assert
            Assert.IsNotNull(rooms);
            // Should still generate some rooms even with small bounds
            Assert.IsTrue(rooms.Count > 0);
        }

        [Test]
        public void GenerateRooms_WithLargeMap_PerformsWell()
        {
            // Arrange
            var largeSettings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 500, 500),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 15,
                    MaxDepth = 8,
                    RoomSizeRatio = 0.8f
                }
            };

            // Act
            var startTime = System.DateTime.Now;
            var rooms = _generator.GenerateRooms(largeSettings);
            var duration = System.DateTime.Now - startTime;

            // Assert
            Assert.IsNotNull(rooms);
            Assert.IsTrue(rooms.Count > 0);
            // Should complete within reasonable time (adjust threshold as needed)
            Assert.IsTrue(duration.TotalSeconds < 5.0, $"Generation took {duration.TotalSeconds} seconds");
        }

        [Test]
        public void OnRoomGenerated_EventFiredForEachRoom()
        {
            // Arrange
            var settings = _defaultSettings;
            var generatedRooms = new List<RoomData>();
            _generator.OnRoomGenerated += room => generatedRooms.Add(room);

            // Act
            var rooms = _generator.GenerateRooms(settings);

            // Assert
            Assert.AreEqual(rooms.Count, generatedRooms.Count);
            for (int i = 0; i < rooms.Count; i++)
            {
                Assert.AreEqual(rooms[i].RoomID, generatedRooms[i].RoomID);
            }
        }

        [Test]
        public void OptimizeRoomLayout_ReturnsSameRooms()
        {
            // Arrange
            var settings = _defaultSettings;
            var rooms = _generator.GenerateRooms(settings);

            // Act
            var optimizedRooms = _generator.OptimizeRoomLayout(rooms, settings);

            // Assert
            Assert.AreEqual(rooms.Count, optimizedRooms.Count);
            for (int i = 0; i < rooms.Count; i++)
            {
                Assert.AreEqual(rooms[i].Bounds, optimizedRooms[i].Bounds);
            }
        }

        [Test]
        public void ClassifyRooms_SetsUnassignedClassification()
        {
            // Arrange
            var settings = _defaultSettings;
            var rooms = _generator.GenerateRooms(settings);

            // Act
            var classifiedRooms = _generator.ClassifyRooms(rooms, settings);

            // Assert
            foreach (var room in classifiedRooms)
            {
                Assert.AreEqual(RoomClassification.Unassigned, room.Classification);
            }
        }

        [Test]
        public void FindOptimalRoomPosition_ReturnsNull()
        {
            // Arrange
            var settings = _defaultSettings;
            var existingRooms = _generator.GenerateRooms(settings);
            var newRoomSize = new Vector2Int(10, 10);

            // Act
            var position = _generator.FindOptimalRoomPosition(existingRooms, newRoomSize, settings);

            // Assert
            Assert.IsNull(position); // BSP generator doesn't implement this feature
        }
    }
}