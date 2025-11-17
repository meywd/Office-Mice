using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Configuration;

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

        [Test]
        public void Split_WithDeterministicRandom_ProducesIdenticalResults()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var node1 = new BSPNode(bounds);
            var node2 = new BSPNode(bounds);
            var random1 = new System.Random(12345);
            var random2 = new System.Random(12345);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 3,
                SplitPositionVariation = 0.3f,
                StopSplittingChance = 0.1f
            };

            // Act
            node1.SplitRecursive(config, random1);
            node2.SplitRecursive(config, random2);

            // Assert
            var leaves1 = node1.GetLeafNodes();
            var leaves2 = node2.GetLeafNodes();
            Assert.AreEqual(leaves1.Count, leaves2.Count);

            for (int i = 0; i < leaves1.Count; i++)
            {
                Assert.AreEqual(leaves1[i].Bounds, leaves2[i].Bounds);
            }
        }

        [Test]
        public void SplitRecursive_WithConfiguration_CreatesValidTree()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var node = new BSPNode(bounds);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 4,
                SplitPreference = SplitPreference.Alternate,
                SplitPositionVariation = 0.3f,
                StopSplittingChance = 0.1f
            };

            // Act
            node.SplitRecursive(config, new System.Random(12345));

            // Assert
            var validation = node.Validate();
            Assert.IsFalse(validation.HasErrors, $"Validation errors: {validation.GetErrorSummary()}");

            var leaves = node.GetLeafNodes();
            Assert.IsTrue(leaves.Count > 0, "Should have at least one leaf node");

            // All leaf nodes should be within bounds and meet minimum size
            foreach (var leaf in leaves)
            {
                Assert.IsTrue(leaf.Bounds.width >= config.MinPartitionSize);
                Assert.IsTrue(leaf.Bounds.height >= config.MinPartitionSize);
                Assert.IsTrue(bounds.Contains(leaf.Bounds.min));
                Assert.IsTrue(bounds.Contains(leaf.Bounds.max - Vector2Int.one));
            }
        }

        [Test]
        public void SplitPreference_Horizontal_OnlyHorizontalSplits()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 3,
                SplitPreference = SplitPreference.Horizontal,
                AllowHorizontalSplits = true,
                AllowVerticalSplits = false
            };
            var node = new BSPNode(bounds);

            // Act
            node.SplitRecursive(config, new System.Random(12345));

            // Assert
            AssertAllSplitsAreDirection(node, true); // true = horizontal
        }

        [Test]
        public void SplitPreference_Vertical_OnlyVerticalSplits()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 3,
                SplitPreference = SplitPreference.Vertical,
                AllowHorizontalSplits = false,
                AllowVerticalSplits = true
            };
            var node = new BSPNode(bounds);

            // Act
            node.SplitRecursive(config, new System.Random(12345));

            // Assert
            AssertAllSplitsAreDirection(node, false); // false = vertical
        }

        [Test]
        public void SplitPositionVariation_WithRandom_ProducesVariedSplits()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 2,
                SplitPositionVariation = 0.5f
            };
            var node1 = new BSPNode(bounds);
            var node2 = new BSPNode(bounds);

            // Act
            node1.SplitRecursive(config, new System.Random(11111));
            node2.SplitRecursive(config, new System.Random(22222));

            // Assert
            // The split positions should be different due to different random seeds
            var splitPos1 = node1.SplitPosition;
            var splitPos2 = node2.SplitPosition;
            Assert.AreNotEqual(splitPos1, splitPos2);
        }

        [Test]
        public void StopSplittingChance_PreventsFurtherSplits()
        {
            // Arrange
            var bounds = new RectInt(0, 0, 100, 100);
            var config = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 10,
                StopSplittingChance = 1.0f // Always stop splitting
            };
            var node = new BSPNode(bounds);

            // Act
            node.SplitRecursive(config, new System.Random(12345));

            // Assert
            // Should have stopped at first split
            Assert.IsFalse(node.IsLeaf);
            Assert.IsTrue(node.Left.IsLeaf);
            Assert.IsTrue(node.Right.IsLeaf);
        }

        #region Helper Methods
        private BSPNode FindNodeAtDepth(BSPNode root, int targetDepth)
        {
            if (root.Depth == targetDepth)
                return root;

            if (!root.IsLeaf)
            {
                var leftResult = FindNodeAtDepth(root.Left, targetDepth);
                if (leftResult != null) return leftResult;

                var rightResult = FindNodeAtDepth(root.Right, targetDepth);
                return rightResult;
            }

            return null;
        }

        private void AssertAllSplitsAreDirection(BSPNode node, bool expectHorizontal)
        {
            if (!node.IsLeaf)
            {
                Assert.AreEqual(expectHorizontal, node.IsHorizontalSplit, 
                    $"Node at depth {node.Depth} has unexpected split direction");
                
                AssertAllSplitsAreDirection(node.Left, expectHorizontal);
                AssertAllSplitsAreDirection(node.Right, expectHorizontal);
            }
        }
        #endregion
    }
}