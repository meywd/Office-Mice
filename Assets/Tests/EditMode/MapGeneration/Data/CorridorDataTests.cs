using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;
using System.Linq;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class CorridorDataTests
    {
        [Test]
        public void CorridorData_Constructor_InitializesCorrectly()
        {
            // Arrange
            int roomA = 1;
            int roomB = 2;
            var start = new Vector2Int(5, 10);
            var end = new Vector2Int(15, 20);
            int width = 3;

            // Act
            var corridor = new CorridorData(roomA, roomB, start, end, width);

            // Assert
            Assert.AreEqual(roomA, corridor.RoomA_ID);
            Assert.AreEqual(roomB, corridor.RoomB_ID);
            Assert.AreEqual(start, corridor.StartPosition);
            Assert.AreEqual(end, corridor.EndPosition);
            Assert.AreEqual(width, corridor.Width);
            Assert.AreEqual(0, corridor.Length);
            Assert.AreEqual(CorridorShape.Point, corridor.Shape);
            Assert.AreEqual(0, corridor.PathTiles.Count);
        }

        [Test]
        public void CorridorData_Constructor_ClampsWidth()
        {
            // Arrange
            var start = Vector2Int.zero;
            var end = Vector2Int.one;

            // Act
            var corridorMin = new CorridorData(1, 2, start, end, 0);
            var corridorMax = new CorridorData(1, 2, start, end, 10);

            // Assert
            Assert.AreEqual(1, corridorMin.Width, "Width should be clamped to minimum 1");
            Assert.AreEqual(5, corridorMax.Width, "Width should be clamped to maximum 5");
        }

        [Test]
        public void CorridorData_SetPath_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
                new Vector2Int(4, 0),
                new Vector2Int(5, 0)
            };

            // Act
            corridor.SetPath(path);

            // Assert
            Assert.AreEqual(path.Count, corridor.PathTiles.Count);
            Assert.AreEqual(CorridorShape.Straight, corridor.Shape);
            Assert.AreEqual(path.Count, corridor.Length);
            
            for (int i = 0; i < path.Count; i++)
            {
                Assert.AreEqual(path[i], corridor.PathTiles[i]);
            }
        }

        [Test]
        public void CorridorData_SetPath_DetectsShapesCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 5));

            // Act - Straight path
            var straightPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)
            };
            corridor.SetPath(straightPath);
            Assert.AreEqual(CorridorShape.Straight, corridor.Shape);

            // Act - L-shaped path
            var lPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)
            };
            corridor.SetPath(lPath);
            Assert.AreEqual(CorridorShape.L_Shaped, corridor.Shape);

            // Act - Z-shaped path
            var zPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), 
                new Vector2Int(2, 1), new Vector2Int(2, 2)
            };
            corridor.SetPath(zPath);
            Assert.AreEqual(CorridorShape.Z_Shaped, corridor.Shape);

            // Act - Complex path
            var complexPath = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1),
                new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(3, 2)
            };
            corridor.SetPath(complexPath);
            Assert.AreEqual(CorridorShape.Complex, corridor.Shape);
        }

        [Test]
        public void CorridorData_ConnectsRoom_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, Vector2Int.one);

            // Act & Assert
            Assert.IsTrue(corridor.ConnectsRoom(1));
            Assert.IsTrue(corridor.ConnectsRoom(2));
            Assert.IsFalse(corridor.ConnectsRoom(3));
        }

        [Test]
        public void CorridorData_GetOtherRoomID_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, Vector2Int.one);

            // Act & Assert
            Assert.AreEqual(2, corridor.GetOtherRoomID(1));
            Assert.AreEqual(1, corridor.GetOtherRoomID(2));
            Assert.AreEqual(-1, corridor.GetOtherRoomID(3));
        }

        [Test]
        public void CorridorData_ContainsTile_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(2, 0));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)
            };
            corridor.SetPath(path);

            // Act & Assert
            Assert.IsTrue(corridor.ContainsTile(new Vector2Int(0, 0)));
            Assert.IsTrue(corridor.ContainsTile(new Vector2Int(1, 0)));
            Assert.IsTrue(corridor.ContainsTile(new Vector2Int(2, 0)));
            Assert.IsFalse(corridor.ContainsTile(new Vector2Int(3, 0)));
            Assert.IsFalse(corridor.ContainsTile(new Vector2Int(1, 1)));
        }

        [Test]
        public void CorridorData_GetBounds_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 3));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(5, 2)
            };
            corridor.SetPath(path);

            // Act
            var bounds = corridor.GetBounds();

            // Assert
            Assert.AreEqual(new RectInt(0, 0, 6, 3), bounds);
        }

        [Test]
        public void CorridorData_GetExpandedTiles_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(2, 0));
            var path = new List<Vector2Int>
            {
                new Vector2Int(1, 1), new Vector2Int(2, 1)
            };
            corridor.SetPath(path);

            // Act
            var expandedTiles = corridor.GetExpandedTiles(1);

            // Assert
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(1, 1)));
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(2, 1)));
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(0, 1))); // Left expansion
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(3, 1))); // Right expansion
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(1, 0))); // Top expansion
            Assert.IsTrue(expandedTiles.Contains(new Vector2Int(1, 2))); // Bottom expansion
        }

        [Test]
        public void CorridorData_GetDistance_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(3, 4));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1),
                new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(3, 2),
                new Vector2Int(3, 3), new Vector2Int(3, 4)
            };
            corridor.SetPath(path);

            // Act
            float distance = corridor.GetDistance();

            // Assert
            Assert.AreEqual(8f, distance); // 8 steps of distance 1 each
        }

        [Test]
        public void CorridorData_Validate_ReturnsValidForCorrectCorridor()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(5, 0)
            };
            corridor.SetPath(path);

            // Act
            var result = corridor.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void CorridorData_Validate_ReturnsErrorForNoPath()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0));

            // Act
            var result = corridor.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("no path tiles")));
        }

        [Test]
        public void CorridorData_Validate_ReturnsErrorForSameRoomConnection()
        {
            // Arrange
            var corridor = new CorridorData(1, 1, Vector2Int.zero, new Vector2Int(5, 0));
            var path = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(5, 0) };
            corridor.SetPath(path);

            // Act
            var result = corridor.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("connects room to itself")));
        }

        [Test]
        public void CorridorData_Validate_ReturnsErrorForPathGap()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0));
            var pathWithGap = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(3, 0) // Gap at (2,0)
            };
            corridor.SetPath(pathWithGap);

            // Act
            var result = corridor.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("gap between tiles")));
        }

        [Test]
        public void CorridorData_Validate_ReturnsWarningForNarrowCorridor()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0), 2); // Width 2
            var path = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(5, 0) };
            corridor.SetPath(path);

            // Act
            var result = corridor.Validate();

            // Assert
            Assert.IsTrue(result.HasWarnings);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("narrow")));
        }

        [Test]
        public void CorridorData_Clone_CreatesExactCopy()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 3), 4);
            corridor.CorridorID = 7;
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(5, 2)
            };
            corridor.SetPath(path);

            // Act
            var clone = corridor.Clone();

            // Assert
            Assert.AreEqual(corridor.CorridorID, clone.CorridorID);
            Assert.AreEqual(corridor.RoomA_ID, clone.RoomA_ID);
            Assert.AreEqual(corridor.RoomB_ID, clone.RoomB_ID);
            Assert.AreEqual(corridor.StartPosition, clone.StartPosition);
            Assert.AreEqual(corridor.EndPosition, clone.EndPosition);
            Assert.AreEqual(corridor.Width, clone.Width);
            Assert.AreEqual(corridor.Shape, clone.Shape);
            Assert.AreEqual(corridor.PathTiles.Count, clone.PathTiles.Count);
            
            for (int i = 0; i < corridor.PathTiles.Count; i++)
            {
                Assert.AreEqual(corridor.PathTiles[i], clone.PathTiles[i]);
            }
        }

        [Test]
        public void CorridorData_Reverse_WorksCorrectly()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, new Vector2Int(0, 0), new Vector2Int(5, 0));
            var path = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(5, 0)
            };
            corridor.SetPath(path);

            // Act
            corridor.Reverse();

            // Assert
            Assert.AreEqual(2, corridor.RoomA_ID);
            Assert.AreEqual(1, corridor.RoomB_ID);
            Assert.AreEqual(new Vector2Int(5, 0), corridor.StartPosition);
            Assert.AreEqual(new Vector2Int(0, 0), corridor.EndPosition);
            
            // Check path is reversed
            Assert.AreEqual(new Vector2Int(5, 0), corridor.PathTiles[0]);
            Assert.AreEqual(new Vector2Int(0, 0), corridor.PathTiles[corridor.PathTiles.Count - 1]);
        }

        [Test]
        public void CorridorData_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var corridor = new CorridorData(1, 2, Vector2Int.zero, new Vector2Int(5, 0), 3);
            corridor.CorridorID = 7;
            var path = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(5, 0) };
            corridor.SetPath(path);

            // Act
            var result = corridor.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Corridor[7]"));
            Assert.IsTrue(result.Contains("Room1<->Room2"));
            Assert.IsTrue(result.Contains("Straight"));
            Assert.IsTrue(result.Contains("width:3"));
        }
    }
}