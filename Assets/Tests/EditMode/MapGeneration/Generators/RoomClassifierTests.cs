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
    public class RoomClassifierTests
    {
        private RoomClassificationSettings _defaultSettings;
        private RoomClassifier _classifier;
        private List<RoomData> _testRooms;
        private RectInt _testMapBounds;

        [SetUp]
        public void SetUp()
        {
            _defaultSettings = CreateDefaultSettings();
            _classifier = new RoomClassifier(_defaultSettings, 12345);
            _testMapBounds = new RectInt(0, 0, 100, 100);
            _testRooms = CreateTestRooms();
        }

        [Test]
        public void Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange & Act
            var classifier = new RoomClassifier(_defaultSettings, 54321);

            // Assert
            Assert.IsNotNull(classifier);
        }

        [Test]
        public void Constructor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new RoomClassifier(null, 12345));
        }

        [Test]
        public void ClassifyRooms_WithValidRooms_ReturnsClassifiedRooms()
        {
            // Arrange
            var rooms = _testRooms.Take(5).ToList();

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(rooms.Count, result.Count);
            
            foreach (var room in result)
            {
                Assert.AreNotEqual(RoomClassification.Unassigned, room.Classification);
            }
        }

        [Test]
        public void ClassifyRooms_WithNullRooms_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _classifier.ClassifyRooms(null, _testMapBounds));
        }

        [Test]
        public void ClassifyRooms_WithDesignerOverride_UsesOverride()
        {
            // Arrange
            var rooms = _testRooms.Take(3).ToList();
            var targetRoom = rooms[0];
            var overrideType = RoomClassification.Lobby;

            // Act
            _classifier.SetDesignerOverride(targetRoom.RoomID, overrideType);
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            var classifiedRoom = result.FirstOrDefault(r => r.RoomID == targetRoom.RoomID);
            Assert.IsNotNull(classifiedRoom);
            Assert.AreEqual(overrideType, classifiedRoom.Classification);
        }

        [Test]
        public void SetDesignerOverride_WithValidType_SetsOverride()
        {
            // Arrange
            var roomID = 1;
            var overrideType = RoomClassification.Conference;

            // Act
            _classifier.SetDesignerOverride(roomID, overrideType);

            // Assert - No exception thrown
        }

        [Test]
        public void SetDesignerOverride_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var roomID = 1;
            var invalidType = RoomClassification.PlayerStart; // Not an office room type

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _classifier.SetDesignerOverride(roomID, invalidType));
        }

        [Test]
        public void RemoveDesignerOverride_WithExistingOverride_ReturnsTrue()
        {
            // Arrange
            var roomID = 1;
            _classifier.SetDesignerOverride(roomID, RoomClassification.Lobby);

            // Act
            var result = _classifier.RemoveDesignerOverride(roomID);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void RemoveDesignerOverride_WithNonExistingOverride_ReturnsFalse()
        {
            // Arrange
            var roomID = 999;

            // Act
            var result = _classifier.RemoveDesignerOverride(roomID);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ClearDesignerOverrides_WithExistingOverrides_ClearsAll()
        {
            // Arrange
            _classifier.SetDesignerOverride(1, RoomClassification.Lobby);
            _classifier.SetDesignerOverride(2, RoomClassification.Conference);

            // Act
            _classifier.ClearDesignerOverrides();

            // Assert - No exception thrown, overrides should be cleared
        }

        [Test]
        public void ClassifyRooms_SmallRooms_ClassifiesAsStorage()
        {
            // Arrange
            var smallRoom = new RoomData(new RectInt(10, 10, 3, 3)) { RoomID = 100 };
            var rooms = new List<RoomData> { smallRoom };

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            Assert.AreEqual(RoomClassification.Storage, result[0].Classification);
        }

        [Test]
        public void ClassifyRooms_LargeRooms_ClassifiesAsConferenceOrLobby()
        {
            // Arrange
            var largeRoom = new RoomData(new RectInt(10, 10, 15, 15)) { RoomID = 101 };
            var rooms = new List<RoomData> { largeRoom };

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            Assert.IsTrue(result[0].Classification == RoomClassification.Conference || 
                         result[0].Classification == RoomClassification.Lobby);
        }

        [Test]
        public void ClassifyRooms_CenterRooms_PrefersCenterTypes()
        {
            // Arrange
            var centerRoom = new RoomData(new RectInt(45, 45, 10, 10)) { RoomID = 102 };
            var rooms = new List<RoomData> { centerRoom };

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            // Center rooms should prefer Lobby, Conference, or BossOffice
            var centerTypes = new[] { RoomClassification.Lobby, RoomClassification.Conference, RoomClassification.BossOffice };
            Assert.Contains(result[0].Classification, centerTypes);
        }

        [Test]
        public void ClassifyRooms_EdgeRooms_PrefersEdgeTypes()
        {
            // Arrange
            var edgeRoom = new RoomData(new RectInt(1, 1, 5, 5)) { RoomID = 103 };
            var rooms = new List<RoomData> { edgeRoom };

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            // Edge rooms should prefer Storage, ServerRoom, or Security
            var edgeTypes = new[] { RoomClassification.Storage, RoomClassification.ServerRoom, RoomClassification.Security };
            Assert.Contains(result[0].Classification, edgeTypes);
        }

        [Test]
        public void ClassifyRooms_WithDistributionRules_FollowsDistribution()
        {
            // Arrange
            var settings = CreateSettingsWithStrictDistribution();
            var classifier = new RoomClassifier(settings, 12345);
            var rooms = CreateTestRooms(20); // More rooms for distribution testing

            // Act
            var result = classifier.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            var distribution = result.GroupBy(r => r.Classification)
                                   .ToDictionary(g => g.Key, g => g.Count());

            // Check that distribution roughly follows rules (within tolerance)
            var totalRooms = result.Count;
            foreach (var distRule in settings.DistributionRules)
            {
                var expectedCount = Mathf.RoundToInt(totalRooms * distRule.Percentage / 100f);
                var actualCount = distribution.GetValueOrDefault(distRule.Type, 0);

                // Allow 50% tolerance for randomness
                var tolerance = expectedCount * 0.5f;
                Assert.IsTrue(Mathf.Abs(actualCount - expectedCount) <= tolerance,
                    $"Distribution for {distRule.Type}: expected ~{expectedCount}, got {actualCount}");
            }
        }

        [Test]
        public void ValidateConfiguration_WithValidSettings_ReturnsNoErrors()
        {
            // Arrange & Act
            var result = _classifier.ValidateConfiguration();

            // Assert
            Assert.IsFalse(result.HasErrors, $"Validation errors: {result.GetErrorSummary()}");
        }

        [Test]
        public void ValidateConfiguration_WithInvalidDistribution_ReturnsWarning()
        {
            // Arrange
            var settings = CreateSettingsWithInvalidDistribution();
            var classifier = new RoomClassifier(settings, 12345);

            // Act
            var result = classifier.ValidateConfiguration();

            // Assert
            Assert.IsTrue(result.HasWarnings);
        }

        [Test]
        public void ClassifyRooms_WithSameSeed_IsDeterministic()
        {
            // Arrange
            var rooms = _testRooms.Take(10).ToList();
            var seed = 54321;

            // Act
            var classifier1 = new RoomClassifier(_defaultSettings, seed);
            var result1 = classifier1.ClassifyRooms(rooms, _testMapBounds);

            var classifier2 = new RoomClassifier(_defaultSettings, seed);
            var result2 = classifier2.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            Assert.AreEqual(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.AreEqual(result1[i].Classification, result2[i].Classification);
            }
        }

        [Test]
        public void ClassifyRooms_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Arrange
            var rooms = _testRooms.Take(10).ToList();

            // Act
            var classifier1 = new RoomClassifier(_defaultSettings, 11111);
            var result1 = classifier1.ClassifyRooms(rooms, _testMapBounds);

            var classifier2 = new RoomClassifier(_defaultSettings, 22222);
            var result2 = classifier2.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            // Results should be different (though there's a small chance they could be the same)
            bool hasDifference = false;
            for (int i = 0; i < result1.Count; i++)
            {
                if (result1[i].Classification != result2[i].Classification)
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Different seeds should produce different classifications");
        }

        [Test]
        public void ClassifyRooms_PerformanceTest_CompletesWithinTimeLimit()
        {
            // Arrange
            var rooms = CreateTestRooms(100); // Performance test with 100 rooms
            var startTime = System.DateTime.Now;

            // Act
            var result = _classifier.ClassifyRooms(rooms, _testMapBounds);
            var duration = System.DateTime.Now - startTime;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(rooms.Count, result.Count);
            Assert.IsTrue(duration.TotalMilliseconds < 50, 
                $"Classification took {duration.TotalMilliseconds}ms, should be under 50ms for 100 rooms");
        }

        #region Helper Methods
        private RoomClassificationSettings CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            settings.CreateDefaultConfiguration();
            return settings;
        }

        private RoomClassificationSettings CreateSettingsWithStrictDistribution()
        {
            var settings = CreateDefaultSettings();
            settings.EnforceDistributionStrictly = true;
            settings.RandomnessFactor = 0.1f; // Low randomness for predictable distribution
            return settings;
        }

        private RoomClassificationSettings CreateSettingsWithInvalidDistribution()
        {
            var settings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            settings.SettingsID = "invalid_distribution";
            settings.SettingsName = "Invalid Distribution Test";
            
            // Create distribution rules that don't sum to 100%
            settings.DistributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 30f,
                MinCount = 1,
                MaxCount = 10
            });
            
            settings.DistributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Conference,
                Percentage = 20f,
                MinCount = 0,
                MaxCount = 3
            });
            
            // Total is only 50%, not 100%
            return settings;
        }

        private List<RoomData> CreateTestRooms(int count = 10)
        {
            var rooms = new List<RoomData>();
            var random = new System.Random(12345);

            for (int i = 0; i < count; i++)
            {
                var width = random.Next(3, 15);
                var height = random.Next(3, 15);
                var x = random.Next(0, _testMapBounds.width - width);
                var y = random.Next(0, _testMapBounds.height - height);

                var room = new RoomData(new RectInt(x, y, width, height)) { RoomID = i };
                rooms.Add(room);
            }

            return rooms;
        }
        #endregion
    }
}