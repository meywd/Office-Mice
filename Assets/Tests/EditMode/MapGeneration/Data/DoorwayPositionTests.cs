using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class DoorwayPositionTests
    {
        [Test]
        public void DoorwayPosition_Constructor_InitializesCorrectly()
        {
            // Arrange
            var position = new Vector2Int(5, 10);
            var direction = DoorwayDirection.North;
            int width = 2;

            // Act
            var doorway = new DoorwayPosition(position, direction, width);

            // Assert
            Assert.AreEqual(position, doorway.position);
            Assert.AreEqual(direction, doorway.direction);
            Assert.AreEqual(width, doorway.width);
        }

        [Test]
        public void DoorwayPosition_Constructor_ClampsWidth()
        {
            // Arrange
            var position = new Vector2Int(0, 0);
            var direction = DoorwayDirection.East;

            // Act
            var doorwayMin = new DoorwayPosition(position, direction, 0);
            var doorwayMax = new DoorwayPosition(position, direction, 5);

            // Assert
            Assert.AreEqual(1, doorwayMin.width, "Width should be clamped to minimum 1");
            Assert.AreEqual(3, doorwayMax.width, "Width should be clamped to maximum 3");
        }

        [Test]
        public void GetDirectionVector_ReturnsCorrectVectors()
        {
            // Arrange
            var position = Vector2Int.zero;

            // Act & Assert
            Assert.AreEqual(Vector2Int.up, new DoorwayPosition(position, DoorwayDirection.North).GetDirectionVector());
            Assert.AreEqual(Vector2Int.down, new DoorwayPosition(position, DoorwayDirection.South).GetDirectionVector());
            Assert.AreEqual(Vector2Int.right, new DoorwayPosition(position, DoorwayDirection.East).GetDirectionVector());
            Assert.AreEqual(Vector2Int.left, new DoorwayPosition(position, DoorwayDirection.West).GetDirectionVector());
        }

        [Test]
        public void DoorwayPosition_Equals_WorksCorrectly()
        {
            // Arrange
            var doorway1 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.North, 2);
            var doorway2 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.North, 2);
            var doorway3 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.South, 2);

            // Act & Assert
            Assert.IsTrue(doorway1.Equals(doorway2));
            Assert.IsFalse(doorway1.Equals(doorway3));
            Assert.IsFalse(doorway1.Equals(null));
        }

        [Test]
        public void DoorwayPosition_Operators_WorksCorrectly()
        {
            // Arrange
            var doorway1 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.North, 2);
            var doorway2 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.North, 2);
            var doorway3 = new DoorwayPosition(new Vector2Int(1, 2), DoorwayDirection.South, 2);

            // Act & Assert
            Assert.IsTrue(doorway1 == doorway2);
            Assert.IsFalse(doorway1 == doorway3);
            Assert.IsTrue(doorway1 != doorway3);
        }

        [Test]
        public void DoorwayPosition_GetHashCode_ReturnsConsistentValue()
        {
            // Arrange
            var doorway = new DoorwayPosition(new Vector2Int(5, 10), DoorwayDirection.East, 2);

            // Act
            int hash1 = doorway.GetHashCode();
            int hash2 = doorway.GetHashCode();

            // Assert
            Assert.AreEqual(hash1, hash2);
        }
    }
}