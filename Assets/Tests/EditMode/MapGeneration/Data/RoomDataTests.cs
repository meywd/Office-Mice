using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;
using System.Linq;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class RoomDataTests
    {
        [Test]
        public void RoomData_Constructor_InitializesCorrectly()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);

            // Act
            var room = new RoomData(bounds);

            // Assert
            Assert.AreEqual(bounds, room.Bounds);
            Assert.AreEqual(new Vector2Int(17, 32), room.Center); // (10+7, 20+12)
            Assert.AreEqual(375, room.Area); // 15 * 25
            Assert.AreEqual(RoomClassification.Unassigned, room.Classification);
            Assert.IsFalse(room.IsOnCriticalPath);
            Assert.AreEqual(-1f, room.DistanceFromPlayerSpawn);
            Assert.AreEqual(0, room.ConnectedRoomIDs.Count);
            Assert.AreEqual(0, room.Doorways.Count);
        }

        [Test]
        public void RoomData_ConnectToRoom_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);

            // Act
            room.ConnectToRoom(5);
            room.ConnectToRoom(7);
            room.ConnectToRoom(5); // Duplicate

            // Assert
            Assert.AreEqual(2, room.ConnectedRoomIDs.Count);
            Assert.IsTrue(room.IsConnectedTo(5));
            Assert.IsTrue(room.IsConnectedTo(7));
            Assert.IsFalse(room.IsConnectedTo(9));
        }

        [Test]
        public void RoomData_DisconnectFromRoom_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);
            room.ConnectToRoom(5);
            room.ConnectToRoom(7);

            // Act
            room.DisconnectFromRoom(5);

            // Assert
            Assert.AreEqual(1, room.ConnectedRoomIDs.Count);
            Assert.IsFalse(room.IsConnectedTo(5));
            Assert.IsTrue(room.IsConnectedTo(7));
        }

        [Test]
        public void RoomData_DoorwayOperations_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);
            var doorway1 = new DoorwayPosition(new Vector2Int(5, 0), DoorwayDirection.North, 2);
            var doorway2 = new DoorwayPosition(new Vector2Int(0, 5), DoorwayDirection.West, 1);

            // Act
            room.AddDoorway(doorway1);
            room.AddDoorway(doorway2);
            room.AddDoorway(doorway1); // Duplicate

            // Assert
            Assert.AreEqual(2, room.Doorways.Count);
            Assert.IsTrue(room.HasDoorwayAt(new Vector2Int(5, 0)));
            Assert.IsTrue(room.HasDoorwayAt(new Vector2Int(0, 5)));
            Assert.IsFalse(room.HasDoorwayAt(new Vector2Int(10, 5)));

            var foundDoorway = room.GetDoorwayAt(new Vector2Int(5, 0));
            Assert.IsNotNull(foundDoorway);
            Assert.AreEqual(doorway1, foundDoorway.Value);

            // Act - Remove doorway
            room.RemoveDoorway(doorway1);

            // Assert
            Assert.AreEqual(1, room.Doorways.Count);
            Assert.IsFalse(room.HasDoorwayAt(new Vector2Int(5, 0)));
        }

        [Test]
        public void RoomData_ClassificationOperations_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);

            // Act
            room.Classification = RoomClassification.BossRoom;
            room.SetOnCriticalPath(true);
            room.SetDistanceFromPlayerSpawn(25.5f);
            room.AssignTemplate("boss_room_template_001");

            // Assert
            Assert.AreEqual(RoomClassification.BossRoom, room.Classification);
            Assert.IsTrue(room.IsOnCriticalPath);
            Assert.AreEqual(25.5f, room.DistanceFromPlayerSpawn);
            Assert.AreEqual("boss_room_template_001", room.AssignedTemplateID);
        }

        [Test]
        public void RoomData_ContainsPoint_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);
            var room = new RoomData(bounds);

            // Act & Assert
            Assert.IsTrue(room.ContainsPoint(new Vector2Int(10, 20))); // Corner
            Assert.IsTrue(room.ContainsPoint(new Vector2Int(24, 44))); // Opposite corner - 1
            Assert.IsTrue(room.ContainsPoint(new Vector2Int(17, 32))); // Center
            Assert.IsFalse(room.ContainsPoint(new Vector2Int(9, 20))); // Outside left
            Assert.IsFalse(room.ContainsPoint(new Vector2Int(10, 19))); // Outside top
            Assert.IsFalse(room.ContainsPoint(new Vector2Int(25, 32))); // Outside right
            Assert.IsFalse(room.ContainsPoint(new Vector2Int(17, 45))); // Outside bottom
        }

        [Test]
        public void RoomData_OverlapsWith_WorksCorrectly()
        {
            // Arrange
            var bounds1 = new RectInt(0, 0, 10, 10);
            var bounds2 = new RectInt(5, 5, 10, 10);
            var bounds3 = new RectInt(20, 20, 10, 10);
            var room1 = new RoomData(bounds1);
            var room2 = new RoomData(bounds2);
            var room3 = new RoomData(bounds3);

            // Act & Assert
            Assert.IsTrue(room1.OverlapsWith(bounds2));
            Assert.IsFalse(room1.OverlapsWith(bounds3));
        }

        [Test]
        public void RoomData_GetRandomPoint_ReturnsValidPoint()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);
            var room = new RoomData(bounds);
            var random = new System.Random(42); // Fixed seed for reproducible test

            // Act
            var point1 = room.GetRandomPoint(random);
            var point2 = room.GetRandomPoint(random);

            // Assert
            Assert.IsTrue(room.ContainsPoint(point1));
            Assert.IsTrue(room.ContainsPoint(point2));
            Assert.AreNotEqual(point1, point2); // Should be different with fixed seed
        }

        [Test]
        public void RoomData_GetRandomEdgePoint_ReturnsValidPoint()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);
            var room = new RoomData(bounds);
            var random = new System.Random(42);

            // Act
            var edgePoints = new List<Vector2Int>();
            for (int i = 0; i < 20; i++)
            {
                edgePoints.Add(room.GetRandomEdgePoint(random));
            }

            // Assert
            foreach (var point in edgePoints)
            {
                Assert.IsTrue(room.ContainsPoint(point), $"Point {point} should be within room bounds");
                
                // Check if point is actually on an edge
                bool onEdge = point.x == bounds.x || point.x == bounds.xMax - 1 ||
                              point.y == bounds.y || point.y == bounds.yMax - 1;
                Assert.IsTrue(onEdge, $"Point {point} should be on room edge");
            }
        }

        [Test]
        public void RoomData_Validate_ReturnsValidForCorrectRoom()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);
            room.AddDoorway(new DoorwayPosition(new Vector2Int(5, 0), DoorwayDirection.North, 2));
            room.ConnectToRoom(1);
            room.SetClassification(RoomClassification.StandardRoom);

            // Act
            var result = room.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void RoomData_Validate_ReturnsErrorForTooSmallRoom()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 2, 2); // Too small
            var room = new RoomData(bounds);

            // Act
            var result = room.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("too small")));
        }

        [Test]
        public void RoomData_Validate_ReturnsWarningForNoDoorways()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);

            // Act
            var result = room.Validate();

            // Assert
            Assert.IsTrue(result.HasWarnings);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("no doorways")));
        }

        [Test]
        public void RoomData_Validate_ReturnsErrorForInvalidDoorway()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);
            var room = new RoomData(bounds);
            var invalidDoorway = new DoorwayPosition(new Vector2Int(5, 5), DoorwayDirection.North, 2); // Outside bounds

            // Act
            room.AddDoorway(invalidDoorway);
            var result = room.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("outside room bounds")));
        }

        [Test]
        public void RoomData_Clone_CreatesExactCopy()
        {
            // Arrange
            var bounds = new RectInt(10, 20, 15, 25);
            var room = new RoomData(bounds);
            room.RoomID = 5;
            room.Classification = RoomClassification.BossRoom;
            room.SetOnCriticalPath(true);
            room.SetDistanceFromPlayerSpawn(50.0f);
            room.AssignTemplate("test_template");
            room.ConnectToRoom(1);
            room.ConnectToRoom(2);
            room.AddDoorway(new DoorwayPosition(new Vector2Int(15, 25), DoorwayDirection.East, 2));

            // Act
            var clone = room.Clone();

            // Assert
            Assert.AreEqual(room.RoomID, clone.RoomID);
            Assert.AreEqual(room.Bounds, clone.Bounds);
            Assert.AreEqual(room.Classification, clone.Classification);
            Assert.AreEqual(room.IsOnCriticalPath, clone.IsOnCriticalPath);
            Assert.AreEqual(room.DistanceFromPlayerSpawn, clone.DistanceFromPlayerSpawn);
            Assert.AreEqual(room.AssignedTemplateID, clone.AssignedTemplateID);
            Assert.AreEqual(room.ConnectedRoomIDs.Count, clone.ConnectedRoomIDs.Count);
            Assert.AreEqual(room.Doorways.Count, clone.Doorways.Count);
            
            // Verify collections are copied, not referenced
            clone.ConnectToRoom(3);
            Assert.AreEqual(2, room.ConnectedRoomIDs.Count);
            Assert.AreEqual(3, clone.ConnectedRoomIDs.Count);
        }

        [Test]
        public void RoomData_Equals_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room1 = new RoomData(bounds);
            var room2 = new RoomData(bounds);
            var room3 = new RoomData(new RectInt(5, 5, 10, 10));

            room1.RoomID = 1;
            room2.RoomID = 1;
            room3.RoomID = 2;

            // Act & Assert
            Assert.IsTrue(room1.Equals(room2));
            Assert.IsFalse(room1.Equals(room3));
            Assert.IsFalse(room1.Equals(null));
        }

        [Test]
        public void RoomData_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 10, 10);
            var room = new RoomData(bounds);
            room.RoomID = 5;
            room.Classification = RoomClassification.BossRoom;
            room.ConnectToRoom(1);
            room.ConnectToRoom(2);
            room.AddDoorway(new DoorwayPosition(new Vector2Int(5, 0), DoorwayDirection.North, 2));

            // Act
            var result = room.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Room[5]"));
            Assert.IsTrue(result.Contains("(0, 0, 10, 10)"));
            Assert.IsTrue(result.Contains("BossRoom"));
            Assert.IsTrue(result.Contains("2 connections"));
            Assert.IsTrue(result.Contains("1 doorways"));
        }
    }
}