using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive unit tests for ICorridorGenerator interface and mock implementation.
    /// Tests all interface methods, events, and edge cases.
    /// </summary>
    [TestFixture]
    public class ICorridorGeneratorTests
    {
        private MockCorridorGenerator _corridorGenerator;
        private MapGenerationSettings _testSettings;
        private List<RoomData> _testRooms;
        private List<CorridorData> _testCorridors;

        [SetUp]
        public void SetUp()
        {
            _corridorGenerator = new MockCorridorGenerator();
            _testSettings = new MapGenerationSettings();
            _testRooms = CreateTestRooms();
            _testCorridors = CreateTestCorridors();
        }

        [Test]
        public void ConnectRooms_WithValidData_ReturnsCorridors()
        {
            // Arrange
            _corridorGenerator.SetMockCorridors(_testCorridors);

            // Act
            var result = _corridorGenerator.ConnectRooms(_testRooms, _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void ConnectRooms_WithNullRooms_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _corridorGenerator.ConnectRooms(null, _testSettings));
        }

        [Test]
        public void ConnectRooms_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _corridorGenerator.ConnectRooms(_testRooms, null));
        }

        [Test]
        public void ConnectRooms_WithSeed_ReturnsDeterministicCorridors()
        {
            // Arrange
            _corridorGenerator.SetMockCorridors(_testCorridors);

            // Act
            var result1 = _corridorGenerator.ConnectRooms(_testRooms, _testSettings, 123);
            var result2 = _corridorGenerator.ConnectRooms(_testRooms, _testSettings, 123);

            // Assert
            Assert.AreEqual(result1.Count, result2.Count);
        }

        [Test]
        public void ConnectRooms_SingleConnection_WithValidData_ReturnsCorridor()
        {
            // Arrange
            var room1 = _testRooms[0];
            var room2 = _testRooms[1];

            // Act
            var result = _corridorGenerator.ConnectRooms(room1, room2, _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasValue);
        }

        [Test]
        public void ConnectRooms_SingleConnection_WithNullRoom1_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.ConnectRooms(null, _testRooms[0], _testSettings));
        }

        [Test]
        public void ConnectRooms_SingleConnection_WithNullRoom2_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.ConnectRooms(_testRooms[0], null, _testSettings));
        }

        [Test]
        public void ConnectRooms_SingleConnection_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.ConnectRooms(_testRooms[0], _testRooms[1], null));
        }

        [Test]
        public void ConnectRooms_WithException_ThrowsAndFiresEvent()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            _corridorGenerator.SetThrowException(true, expectedException);
            Exception firedException = null;
            _corridorGenerator.OnCorridorGenerationFailed += (room1, room2, ex) => firedException = ex;

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => 
                _corridorGenerator.ConnectRooms(_testRooms, _testSettings));
            Assert.AreEqual(expectedException, thrownException);
            Assert.AreEqual(expectedException, firedException);
        }

        [Test]
        public void ValidateConnectivity_WithValidData_ReturnsSuccess()
        {
            // Arrange
            _corridorGenerator.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _corridorGenerator.ValidateConnectivity(_testRooms, _testCorridors);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateConnectivity_WithNullRooms_ReturnsFailure()
        {
            // Act
            var result = _corridorGenerator.ValidateConnectivity(null, _testCorridors);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Rooms list cannot be null"));
        }

        [Test]
        public void ValidateConnectivity_WithNullCorridors_ReturnsFailure()
        {
            // Act
            var result = _corridorGenerator.ValidateConnectivity(_testRooms, null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Corridors list cannot be null"));
        }

        [Test]
        public void OptimizeCorridors_WithValidData_ReturnsOptimizedCorridors()
        {
            // Act
            var result = _corridorGenerator.OptimizeCorridors(_testCorridors, _testRooms, _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testCorridors.Count, result.Count);
            // Should be a copy, not the same reference
            Assert.AreNotEqual(_testCorridors, result);
        }

        [Test]
        public void OptimizeCorridors_WithNullCorridors_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.OptimizeCorridors(null, _testRooms, _testSettings));
        }

        [Test]
        public void OptimizeCorridors_WithNullRooms_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.OptimizeCorridors(_testCorridors, null, _testSettings));
        }

        [Test]
        public void OptimizeCorridors_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.OptimizeCorridors(_testCorridors, _testRooms, null));
        }

        [Test]
        public void ResolveIntersections_WithValidData_ReturnsResolvedCorridors()
        {
            // Act
            var result = _corridorGenerator.ResolveIntersections(_testCorridors);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testCorridors.Count, result.Count);
            // Should be a copy, not the same reference
            Assert.AreNotEqual(_testCorridors, result);
        }

        [Test]
        public void ResolveIntersections_WithNullCorridors_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _corridorGenerator.ResolveIntersections(null));
        }

        [Test]
        public void CalculateTotalCorridorLength_WithValidCorridors_ReturnsLength()
        {
            // Arrange
            var expectedLength = 75f;
            _corridorGenerator.SetMockTotalLength(expectedLength);

            // Act
            var result = _corridorGenerator.CalculateTotalCorridorLength(_testCorridors);

            // Assert
            Assert.AreEqual(expectedLength, result);
        }

        [Test]
        public void CalculateTotalCorridorLength_WithNullCorridors_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _corridorGenerator.CalculateTotalCorridorLength(null));
        }

        [Test]
        public void FindShortestPath_WithValidData_ReturnsPath()
        {
            // Arrange
            var startRoom = _testRooms[0];
            var endRoom = _testRooms[1];

            // Act
            var result = _corridorGenerator.FindShortestPath(startRoom, endRoom, _testCorridors);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public void FindShortestPath_WithNullStartRoom_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.FindShortestPath(null, _testRooms[1], _testCorridors));
        }

        [Test]
        public void FindShortestPath_WithNullEndRoom_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.FindShortestPath(_testRooms[0], null, _testCorridors));
        }

        [Test]
        public void FindShortestPath_WithNullCorridors_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _corridorGenerator.FindShortestPath(_testRooms[0], _testRooms[1], null));
        }

        [Test]
        public void OnCorridorGenerated_EventFired_WhenCorridorsGenerated()
        {
            // Arrange
            var generatedCorridors = new List<CorridorData>();
            _corridorGenerator.OnCorridorGenerated += (corridor) => generatedCorridors.Add(corridor);

            // Act
            var result = _corridorGenerator.ConnectRooms(_testRooms, _testSettings);

            // Assert
            Assert.IsTrue(generatedCorridors.Count > 0);
        }

        [Test]
        public void Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockCorridorGenerator properly implements ICorridorGenerator
            Assert.IsInstanceOf<ICorridorGenerator>(_corridorGenerator);
            
            // Verify all required methods exist and are callable
            var corridorGen = (ICorridorGenerator)_corridorGenerator;
            
            Assert.DoesNotThrow(() => corridorGen.ValidateConnectivity(_testRooms, _testCorridors));
            Assert.DoesNotThrow(() => corridorGen.OptimizeCorridors(_testCorridors, _testRooms, _testSettings));
            Assert.DoesNotThrow(() => corridorGen.ResolveIntersections(_testCorridors));
            Assert.DoesNotThrow(() => corridorGen.CalculateTotalCorridorLength(_testCorridors));
            Assert.DoesNotThrow(() => corridorGen.FindShortestPath(_testRooms[0], _testRooms[1], _testCorridors));
        }

        [Test]
        public void Events_AllEvents_WorkCorrectly()
        {
            // Arrange
            var generatedCorridors = new List<CorridorData>();
            var failedConnections = new List<(RoomData room1, RoomData room2, Exception ex)>();
            
            _corridorGenerator.OnCorridorGenerated += (corridor) => generatedCorridors.Add(corridor);
            _corridorGenerator.OnCorridorGenerationFailed += (room1, room2, ex) => failedConnections.Add((room1, room2, ex));

            // Act - Successful generation
            _corridorGenerator.ConnectRooms(_testRooms, _testSettings);
            
            // Assert - Success case
            Assert.IsTrue(generatedCorridors.Count > 0, "OnCorridorGenerated should fire for each corridor");
            Assert.AreEqual(0, failedConnections.Count, "OnCorridorGenerationFailed should not fire on success");
            
            // Reset
            generatedCorridors.Clear();
            failedConnections.Clear();
            
            // Act - Failed generation
            _corridorGenerator.SetThrowException(true);
            try { _corridorGenerator.ConnectRooms(_testRooms, _testSettings); } catch { }
            
            // Assert - Failure case
            Assert.AreEqual(0, generatedCorridors.Count, "OnCorridorGenerated should not fire on failure");
            Assert.AreEqual(1, failedConnections.Count, "OnCorridorGenerationFailed should fire on failure");
        }

        [Test]
        public void ConnectRooms_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            _corridorGenerator.SetMockCorridors(new List<CorridorData>());

            // Act
            var result = _corridorGenerator.ConnectRooms(new List<RoomData>(), _testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FindShortestPath_WithEmptyCorridors_ReturnsEmptyList()
        {
            // Act
            var result = _corridorGenerator.FindShortestPath(_testRooms[0], _testRooms[1], new List<CorridorData>());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        private List<RoomData> CreateTestRooms()
        {
            var rooms = new List<RoomData>();
            
            var room1 = new RoomData();
            room1.SetBounds(new Rect(5, 5, 10, 10));
            room1.Classification = RoomClassification.Office;
            rooms.Add(room1);

            var room2 = new RoomData();
            room2.SetBounds(new Rect(20, 8, 8, 10));
            room2.Classification = RoomClassification.BreakRoom;
            rooms.Add(room2);

            var room3 = new RoomData();
            room3.SetBounds(new Rect(35, 15, 10, 8));
            room3.Classification = RoomClassification.MeetingRoom;
            rooms.Add(room3);

            return rooms;
        }

        private List<CorridorData> CreateTestCorridors()
        {
            var corridors = new List<CorridorData>();
            
            var corridor1 = new CorridorData();
            corridor1.SetRooms(CreateTestRooms()[0], CreateTestRooms()[1]);
            corridor1.SetPath(new List<Vector2Int> { new Vector2Int(15, 10), new Vector2Int(20, 10) });
            corridors.Add(corridor1);

            var corridor2 = new CorridorData();
            corridor2.SetRooms(CreateTestRooms()[1], CreateTestRooms()[2]);
            corridor2.SetPath(new List<Vector2Int> { new Vector2Int(28, 13), new Vector2Int(35, 15) });
            corridors.Add(corridor2);

            return corridors;
        }
    }
}