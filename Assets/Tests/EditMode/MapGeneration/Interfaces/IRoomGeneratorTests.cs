using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive unit tests for IRoomGenerator interface and mock implementation.
    /// Tests all interface methods, events, and edge cases.
    /// </summary>
    [TestFixture]
    public class IRoomGeneratorTests
    {
        private MockRoomGenerator _roomGenerator;
        private MapGenerationSettings _testSettings;
        private List<RoomData> _testRooms;

        [SetUp]
        public void SetUp()
        {
            _roomGenerator = new MockRoomGenerator();
            _testSettings = new MapGenerationSettings();
            _testRooms = CreateTestRooms();
        }

        [Test]
        public void GenerateRooms_WithValidSettings_ReturnsRooms()
        {
            // Arrange
            _roomGenerator.SetMockRooms(_testRooms);

            // Act
            var result = _roomGenerator.GenerateRooms(_testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testRooms.Count, result.Count);
        }

        [Test]
        public void GenerateRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.GenerateRooms(null));
        }

        [Test]
        public void GenerateRooms_WithSeed_ReturnsDeterministicRooms()
        {
            // Arrange
            _roomGenerator.SetMockRooms(_testRooms);

            // Act
            var result1 = _roomGenerator.GenerateRooms(_testSettings, 123);
            var result2 = _roomGenerator.GenerateRooms(_testSettings, 123);

            // Assert
            Assert.AreEqual(result1.Count, result2.Count);
        }

        [Test]
        public void GenerateRooms_WithException_ThrowsAndFiresEvent()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            _roomGenerator.SetThrowException(true, expectedException);
            RoomData failedRoom = null;
            Exception firedException = null;
            _roomGenerator.OnRoomGenerationFailed += (room, ex) => { failedRoom = room; firedException = ex; };

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _roomGenerator.GenerateRooms(_testSettings));
            Assert.AreEqual(expectedException, thrownException);
            Assert.AreEqual(expectedException, firedException);
        }

        [Test]
        public void ValidateRoomPlacement_WithValidData_ReturnsSuccess()
        {
            // Arrange
            _roomGenerator.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _roomGenerator.ValidateRoomPlacement(_testRooms, _testSettings);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateRoomPlacement_WithNullRooms_ReturnsFailure()
        {
            // Act
            var result = _roomGenerator.ValidateRoomPlacement(null, _testSettings);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Rooms list cannot be null"));
        }

        [Test]
        public void ValidateRoomPlacement_WithNullSettings_ReturnsFailure()
        {
            // Act
            var result = _roomGenerator.ValidateRoomPlacement(_testRooms, null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Settings cannot be null"));
        }

        [Test]
        public void OptimizeRoomLayout_WithValidData_ReturnsOptimizedRooms()
        {
            // Act
            var result = _roomGenerator.OptimizeRoomLayout(_testRooms, _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testRooms.Count, result.Count);
            // Should be a copy, not the same reference
            Assert.AreNotEqual(_testRooms, result);
        }

        [Test]
        public void OptimizeRoomLayout_WithNullRooms_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.OptimizeRoomLayout(null, _testSettings));
        }

        [Test]
        public void OptimizeRoomLayout_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.OptimizeRoomLayout(_testRooms, null));
        }

        [Test]
        public void ClassifyRooms_WithValidData_ReturnsClassifiedRooms()
        {
            // Arrange
            var unclassifiedRooms = new List<RoomData>
            {
                new RoomData { Classification = RoomClassification.Undefined },
                new RoomData { Classification = RoomClassification.Office }
            };

            // Act
            var result = _roomGenerator.ClassifyRooms(unclassifiedRooms, _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(unclassifiedRooms.Count, result.Count);
            // All rooms should have classifications now
            foreach (var room in result)
            {
                Assert.AreNotEqual(RoomClassification.Undefined, room.Classification);
            }
        }

        [Test]
        public void CalculateTotalRoomArea_WithValidRooms_ReturnsArea()
        {
            // Arrange
            var expectedArea = 500f;
            _roomGenerator.SetMockTotalArea(expectedArea);

            // Act
            var result = _roomGenerator.CalculateTotalRoomArea(_testRooms);

            // Assert
            Assert.AreEqual(expectedArea, result);
        }

        [Test]
        public void CalculateTotalRoomArea_WithNullRooms_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.CalculateTotalRoomArea(null));
        }

        [Test]
        public void FindOptimalRoomPosition_WithValidData_ReturnsPosition()
        {
            // Act
            var result = _roomGenerator.FindOptimalRoomPosition(_testRooms, new Vector2Int(5, 5), _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasValue);
        }

        [Test]
        public void FindOptimalRoomPosition_WithNullRooms_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _roomGenerator.FindOptimalRoomPosition(null, new Vector2Int(5, 5), _testSettings));
        }

        [Test]
        public void FindOptimalRoomPosition_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _roomGenerator.FindOptimalRoomPosition(_testRooms, new Vector2Int(5, 5), null));
        }

        [Test]
        public void OnRoomGenerated_EventFired_WhenRoomsGenerated()
        {
            // Arrange
            var generatedRooms = new List<RoomData>();
            _roomGenerator.OnRoomGenerated += (room) => generatedRooms.Add(room);
            _roomGenerator.SetMockRooms(_testRooms);

            // Act
            var result = _roomGenerator.GenerateRooms(_testSettings);

            // Assert
            Assert.AreEqual(_testRooms.Count, generatedRooms.Count);
        }

        [Test]
        public void Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockRoomGenerator properly implements IRoomGenerator
            Assert.IsInstanceOf<IRoomGenerator>(_roomGenerator);
            
            // Verify all required methods exist and are callable
            var roomGen = (IRoomGenerator)_roomGenerator;
            
            Assert.DoesNotThrow(() => roomGen.ValidateRoomPlacement(_testRooms, _testSettings));
            Assert.DoesNotThrow(() => roomGen.OptimizeRoomLayout(_testRooms, _testSettings));
            Assert.DoesNotThrow(() => roomGen.ClassifyRooms(_testRooms, _testSettings));
            Assert.DoesNotThrow(() => roomGen.CalculateTotalRoomArea(_testRooms));
            Assert.DoesNotThrow(() => roomGen.FindOptimalRoomPosition(_testRooms, new Vector2Int(5, 5), _testSettings));
        }

        [Test]
        public void Events_AllEvents_WorkCorrectly()
        {
            // Arrange
            var generatedRooms = new List<RoomData>();
            var failedRooms = new List<(RoomData room, Exception ex)>();
            
            _roomGenerator.OnRoomGenerated += (room) => generatedRooms.Add(room);
            _roomGenerator.OnRoomGenerationFailed += (room, ex) => failedRooms.Add((room, ex));

            // Act - Successful generation
            _roomGenerator.SetMockRooms(_testRooms);
            _roomGenerator.GenerateRooms(_testSettings);
            
            // Assert - Success case
            Assert.AreEqual(_testRooms.Count, generatedRooms.Count, "OnRoomGenerated should fire for each room");
            Assert.AreEqual(0, failedRooms.Count, "OnRoomGenerationFailed should not fire on success");
            
            // Reset
            generatedRooms.Clear();
            failedRooms.Clear();
            
            // Act - Failed generation
            _roomGenerator.SetThrowException(true);
            try { _roomGenerator.GenerateRooms(_testSettings); } catch { }
            
            // Assert - Failure case
            Assert.AreEqual(0, generatedRooms.Count, "OnRoomGenerated should not fire on failure");
            Assert.AreEqual(1, failedRooms.Count, "OnRoomGenerationFailed should fire on failure");
        }

        [Test]
        public void GenerateRooms_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            _roomGenerator.SetMockRooms(new List<RoomData>());

            // Act
            var result = _roomGenerator.GenerateRooms(_testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ClassifyRooms_WithNullList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.ClassifyRooms(null, _testSettings));
        }

        [Test]
        public void ClassifyRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _roomGenerator.ClassifyRooms(_testRooms, null));
        }

        private List<RoomData> CreateTestRooms()
        {
            return new List<RoomData>
            {
                new RoomData { Classification = RoomClassification.Office },
                new RoomData { Classification = RoomClassification.BreakRoom },
                new RoomData { Classification = RoomClassification.MeetingRoom }
            };
        }
    }
}