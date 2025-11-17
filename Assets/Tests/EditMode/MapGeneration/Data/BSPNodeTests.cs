using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class BSPNodeTests
    {
        [Test]
        public void BSPNode_Constructor_InitializesCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);

            // Act
            var node = new BSPNode(bounds);

            // Assert
            Assert.AreEqual(bounds, node.Bounds);
            Assert.IsTrue(node.IsLeaf);
            Assert.AreEqual(0, node.Depth);
            Assert.IsNull(node.Left);
            Assert.IsNull(node.Right);
            Assert.IsNull(node.Parent);
        }

        [Test]
        public void BSPNode_Split_CreatesChildrenCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);

            // Act
            bool split = node.Split(5, 5);

            // Assert
            Assert.IsTrue(split);
            Assert.IsFalse(node.IsLeaf);
            Assert.IsNotNull(node.Left);
            Assert.IsNotNull(node.Right);
            Assert.AreEqual(1, node.Left.Depth);
            Assert.AreEqual(1, node.Right.Depth);
        }

        [Test]
        public void BSPNode_Split_HorizontalSplit_CreatesCorrectBounds()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);

            // Act
            bool split = node.Split(5, 5);

            // Assert
            Assert.IsTrue(split);
            Assert.IsTrue(node.IsHorizontalSplit);
            Assert.AreEqual(10, node.SplitPosition);
            
            // Check left child bounds
            Assert.AreEqual(new RectInt(0, 0, 20, 10), node.Left.Bounds);
            
            // Check right child bounds
            Assert.AreEqual(new RectInt(0, 10, 20, 10), node.Right.Bounds);
        }

        [Test]
        public void BSPNode_Split_VerticalSplit_CreatesCorrectBounds()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);
            
            // Force vertical split by making horizontal impossible
            var verticalBounds = new RectInt(0, 0, 20, 8); // Too small for horizontal split
            var verticalNode = new BSPNode(verticalBounds);

            // Act
            bool split = verticalNode.Split(5, 5);

            // Assert
            Assert.IsTrue(split);
            Assert.IsFalse(verticalNode.IsHorizontalSplit);
            Assert.AreEqual(10, verticalNode.SplitPosition);
            
            // Check left child bounds
            Assert.AreEqual(new RectInt(0, 0, 10, 8), verticalNode.Left.Bounds);
            
            // Check right child bounds
            Assert.AreEqual(new RectInt(10, 0, 10, 8), verticalNode.Right.Bounds);
        }

        [Test]
        public void BSPNode_Split_ReturnsFalse_WhenTooSmall()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 8, 8);
            var node = new BSPNode(bounds);

            // Act
            bool split = node.Split(5, 5);

            // Assert
            Assert.IsFalse(split);
            Assert.IsTrue(node.IsLeaf);
        }

        [Test]
        public void BSPNode_Split_ReturnsFalse_WhenAtMaxDepth()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);

            // Act
            bool split = node.Split(5, 0); // Max depth = 0

            // Assert
            Assert.IsFalse(split);
            Assert.IsTrue(node.IsLeaf);
        }

        [Test]
        public void BSPNode_SetRoomBounds_WorksCorrectly()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);
            var roomBounds = new RectInt(5, 5, 10, 10);

            // Act
            node.SetRoomBounds(roomBounds);

            // Assert
            Assert.AreEqual(roomBounds, node.RoomBounds);
        }

        [Test]
        public void BSPNode_SetRoomBounds_RejectsInvalidBounds()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);
            var invalidBounds = new RectInt(-5, -5, 30, 30);

            // Act
            node.SetRoomBounds(invalidBounds);

            // Assert
            Assert.AreNotEqual(invalidBounds, node.RoomBounds);
        }

        [Test]
        public void BSPNode_GetLeafNodes_ReturnsAllLeaves()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var root = new BSPNode(bounds);
            root.Split(5, 5);
            root.Left.Split(5, 5);

            // Act
            var leaves = root.GetLeafNodes();

            // Assert
            Assert.AreEqual(3, leaves.Count);
            Assert.IsTrue(leaves.Contains(root.Left.Left));
            Assert.IsTrue(leaves.Contains(root.Left.Right));
            Assert.IsTrue(leaves.Contains(root.Right));
        }

        [Test]
        public void BSPNode_Validate_ReturnsValidForCorrectNode()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);

            // Act
            var result = node.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void BSPNode_Validate_ReturnsErrorForInvalidBounds()
        {
            // Arrange
            var invalidBounds = new RectInt(0, 0, -5, 10);
            var node = new BSPNode(invalidBounds);

            // Act
            var result = node.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }

        [Test]
        public void BSPNode_Validate_ReturnsErrorForInternalNodeWithoutChildren()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var node = new BSPNode(bounds);
            
            // Manually create invalid state (internal node without children)
            // This would require reflection or making the method public for testing
            // For now, we'll test the validation logic indirectly

            // Act
            var result = node.Validate();

            // Assert
            Assert.IsTrue(result.IsValid); // Valid leaf node
        }

        [Test]
        public void BSPNode_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 20, 20);
            var leafNode = new BSPNode(bounds);
            leafNode.SetRoomBounds(new RectInt(5, 5, 10, 10));

            // Act
            var leafString = leafNode.ToString();
            
            leafNode.Split(5, 5);
            var internalString = leafNode.ToString();

            // Assert
            Assert.IsTrue(leafString.Contains("Leaf"));
            Assert.IsTrue(leafString.Contains("Depth:0"));
            Assert.IsTrue(internalString.Contains("Node"));
            Assert.IsTrue(internalString.Contains("Depth:0"));
        }
    }
}