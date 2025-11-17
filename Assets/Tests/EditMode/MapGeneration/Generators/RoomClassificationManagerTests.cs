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
    public class RoomClassificationManagerTests
    {
        private RoomClassificationSettings _defaultSettings;
        private RoomClassificationManager _manager;
        private List<RoomData> _testRooms;
        private RectInt _testMapBounds;

        [SetUp]
        public void SetUp()
        {
            _defaultSettings = CreateDefaultSettings();
            _manager = new RoomClassificationManager(_defaultSettings, 12345);
            _testMapBounds = new RectInt(0, 0, 100, 100);
            _testRooms = CreateTestRooms();
        }

        [Test]
        public void Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange & Act
            var manager = new RoomClassificationManager(_defaultSettings, 54321);

            // Assert
            Assert.IsNotNull(manager);
            Assert.AreEqual(54321, manager.GetCurrentSeed());
        }

        [Test]
        public void Constructor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new RoomClassificationManager(null, 12345));
        }

        [Test]
        public void ClassifyRooms_WithValidRooms_ReturnsClassifiedRooms()
        {
            // Arrange
            var rooms = _testRooms.Take(5).ToList();

            // Act
            var result = _manager.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(rooms.Count, result.Count);
            
            foreach (var room in result)
            {
                Assert.AreNotEqual(RoomClassification.Unassigned, room.Classification);
            }
        }

        [Test]
        public void SetDesignerOverride_WithValidType_SetsOverride()
        {
            // Arrange
            var roomID = 1;
            var overrideType = RoomClassification.Lobby;

            // Act
            _manager.SetDesignerOverride(roomID, overrideType);

            // Assert
            var retrievedOverride = _manager.GetDesignerOverride(roomID);
            Assert.IsNotNull(retrievedOverride);
            Assert.AreEqual(overrideType, retrievedOverride.Value);
        }

        [Test]
        public void SetDesignerOverride_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var roomID = 1;
            var invalidType = RoomClassification.PlayerStart; // Not an office room type

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _manager.SetDesignerOverride(roomID, invalidType));
        }

        [Test]
        public void RemoveDesignerOverride_WithExistingOverride_ReturnsTrue()
        {
            // Arrange
            var roomID = 1;
            _manager.SetDesignerOverride(roomID, RoomClassification.Lobby);

            // Act
            var result = _manager.RemoveDesignerOverride(roomID);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(_manager.GetDesignerOverride(roomID));
        }

        [Test]
        public void RemoveDesignerOverride_WithNonExistingOverride_ReturnsFalse()
        {
            // Arrange
            var roomID = 999;

            // Act
            var result = _manager.RemoveDesignerOverride(roomID);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetAllDesignerOverrides_WithMultipleOverrides_ReturnsAll()
        {
            // Arrange
            _manager.SetDesignerOverride(1, RoomClassification.Lobby);
            _manager.SetDesignerOverride(2, RoomClassification.Conference);
            _manager.SetDesignerOverride(3, RoomClassification.Storage);

            // Act
            var overrides = _manager.GetAllDesignerOverrides();

            // Assert
            Assert.AreEqual(3, overrides.Count);
            Assert.IsTrue(overrides.ContainsKey(1));
            Assert.IsTrue(overrides.ContainsKey(2));
            Assert.IsTrue(overrides.ContainsKey(3));
            Assert.AreEqual(RoomClassification.Lobby, overrides[1]);
            Assert.AreEqual(RoomClassification.Conference, overrides[2]);
            Assert.AreEqual(RoomClassification.Storage, overrides[3]);
        }

        [Test]
        public void ClearAllDesignerOverrides_WithExistingOverrides_ClearsAll()
        {
            // Arrange
            _manager.SetDesignerOverride(1, RoomClassification.Lobby);
            _manager.SetDesignerOverride(2, RoomClassification.Conference);

            // Act
            _manager.ClearAllDesignerOverrides();

            // Assert
            var overrides = _manager.GetAllDesignerOverrides();
            Assert.AreEqual(0, overrides.Count);
        }

        [Test]
        public void ValidateRoomClassification_WithValidRoomAndType_ReturnsNoErrors()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 8, 8)) { RoomID = 1 };
            var classification = RoomClassification.Office;

            // Act
            var result = _manager.ValidateRoomClassification(room, classification);

            // Assert
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void ValidateRoomClassification_WithRoomTooSmall_ReturnsError()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 2, 2)) { RoomID = 1 }; // Too small for most types
            var classification = RoomClassification.Conference; // Requires minimum 8x8

            // Act
            var result = _manager.ValidateRoomClassification(room, classification);

            // Assert
            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void ValidateRoomClassification_WithInvalidType_ReturnsError()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 8, 8)) { RoomID = 1 };
            var invalidType = RoomClassification.PlayerStart; // Not an office room type

            // Act
            var result = _manager.ValidateRoomClassification(room, invalidType);

            // Assert
            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void GetClassificationSuggestions_WithValidRoom_ReturnsSuggestions()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 6, 6)) { RoomID = 1 };

            // Act
            var suggestions = _manager.GetClassificationSuggestions(room, _testMapBounds);

            // Assert
            Assert.IsNotNull(suggestions);
            Assert.IsTrue(suggestions.Count > 0);
            
            foreach (var suggestion in suggestions)
            {
                Assert.IsTrue(suggestion.Confidence >= 0f && suggestion.Confidence <= 1f);
                Assert.IsNotNull(suggestion.Reason);
            }
        }

        [Test]
        public void GetClassificationSuggestions_WithMaxSuggestions_LimitsResults()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 6, 6)) { RoomID = 1 };
            var maxSuggestions = 3;

            // Act
            var suggestions = _manager.GetClassificationSuggestions(room, _testMapBounds, maxSuggestions);

            // Assert
            Assert.IsTrue(suggestions.Count <= maxSuggestions);
        }

        [Test]
        public void GetClassificationSuggestions_WithNullRoom_ReturnsEmptyList()
        {
            // Arrange & Act
            var suggestions = _manager.GetClassificationSuggestions(null, _testMapBounds);

            // Assert
            Assert.IsNotNull(suggestions);
            Assert.AreEqual(0, suggestions.Count);
        }

        [Test]
        public void ValidateConfiguration_WithValidSettings_ReturnsNoErrors()
        {
            // Arrange & Act
            var result = _manager.ValidateConfiguration();

            // Assert
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void UpdateSeed_WithNewSeed_UpdatesSeed()
        {
            // Arrange
            var newSeed = 99999;

            // Act
            _manager.UpdateSeed(newSeed);

            // Assert
            Assert.AreEqual(newSeed, _manager.GetCurrentSeed());
        }

        [Test]
        public void UpdateSeed_WithExistingOverrides_PreservesOverrides()
        {
            // Arrange
            _manager.SetDesignerOverride(1, RoomClassification.Lobby);
            _manager.SetDesignerOverride(2, RoomClassification.Conference);
            var newSeed = 99999;

            // Act
            _manager.UpdateSeed(newSeed);

            // Assert
            Assert.AreEqual(newSeed, _manager.GetCurrentSeed());
            var overrides = _manager.GetAllDesignerOverrides();
            Assert.AreEqual(2, overrides.Count);
            Assert.AreEqual(RoomClassification.Lobby, overrides[1]);
            Assert.AreEqual(RoomClassification.Conference, overrides[2]);
        }

        [Test]
        public void ExportDesignerOverrides_WithExistingOverrides_ReturnsData()
        {
            // Arrange
            _manager.SetDesignerOverride(1, RoomClassification.Lobby);
            _manager.SetDesignerOverride(2, RoomClassification.Conference);

            // Act
            var exportData = _manager.ExportDesignerOverrides();

            // Assert
            Assert.IsNotNull(exportData);
            Assert.AreEqual(2, exportData.Overrides.Count);
            Assert.IsTrue(exportData.ExportTimestamp > DateTime.MinValue);
            Assert.AreEqual(12345, exportData.Seed);
        }

        [Test]
        public void ImportDesignerOverrides_WithValidData_ImportsOverrides()
        {
            // Arrange
            var exportData = new DesignerOverrideData
            {
                Overrides = new List<KeyValuePair<int, RoomClassification>>
                {
                    new KeyValuePair<int, RoomClassification>(10, RoomClassification.Lobby),
                    new KeyValuePair<int, RoomClassification>(20, RoomClassification.Conference)
                },
                ExportTimestamp = DateTime.Now,
                Seed = 54321
            };

            // Act
            _manager.ImportDesignerOverrides(exportData);

            // Assert
            Assert.AreEqual(54321, _manager.GetCurrentSeed());
            var overrides = _manager.GetAllDesignerOverrides();
            Assert.AreEqual(2, overrides.Count);
            Assert.AreEqual(RoomClassification.Lobby, overrides[10]);
            Assert.AreEqual(RoomClassification.Conference, overrides[20]);
        }

        [Test]
        public void ImportDesignerOverrides_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _manager.ImportDesignerOverrides(null));
        }

        [Test]
        public void ImportDesignerOverrides_WithInvalidType_SkipsInvalid()
        {
            // Arrange
            var exportData = new DesignerOverrideData
            {
                Overrides = new List<KeyValuePair<int, RoomClassification>>
                {
                    new KeyValuePair<int, RoomClassification>(10, RoomClassification.Lobby),
                    new KeyValuePair<int, RoomClassification>(20, RoomClassification.PlayerStart) // Invalid type
                },
                ExportTimestamp = DateTime.Now,
                Seed = 54321
            };

            // Act
            _manager.ImportDesignerOverrides(exportData);

            // Assert
            var overrides = _manager.GetAllDesignerOverrides();
            Assert.AreEqual(1, overrides.Count); // Only the valid one should be imported
            Assert.AreEqual(RoomClassification.Lobby, overrides[10]);
        }

        [Test]
        public void ClassifyRooms_WithDesignerOverride_UsesOverrideInClassification()
        {
            // Arrange
            var rooms = _testRooms.Take(3).ToList();
            var targetRoom = rooms[0];
            var overrideType = RoomClassification.Lobby;

            // Act
            _manager.SetDesignerOverride(targetRoom.RoomID, overrideType);
            var result = _manager.ClassifyRooms(rooms, _testMapBounds);

            // Assert
            var classifiedRoom = result.FirstOrDefault(r => r.RoomID == targetRoom.RoomID);
            Assert.IsNotNull(classifiedRoom);
            Assert.AreEqual(overrideType, classifiedRoom.Classification);
        }

        [Test]
        public void GetClassificationSuggestions_SortedByConfidence_ReturnsInOrder()
        {
            // Arrange
            var room = new RoomData(new RectInt(45, 45, 12, 12)) { RoomID = 1 }; // Large center room

            // Act
            var suggestions = _manager.GetClassificationSuggestions(room, _testMapBounds, 5);

            // Assert
            Assert.IsTrue(suggestions.Count > 1);
            
            // Check that suggestions are sorted by confidence (descending)
            for (int i = 0; i < suggestions.Count - 1; i++)
            {
                Assert.IsTrue(suggestions[i].Confidence >= suggestions[i + 1].Confidence);
            }
        }

        #region Helper Methods
        private RoomClassificationSettings CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            settings.CreateDefaultConfiguration();
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