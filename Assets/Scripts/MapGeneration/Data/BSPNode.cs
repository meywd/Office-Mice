using System;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Binary Space Partitioning node for recursive room generation.
    /// Represents a partition in the BSP tree structure.
    /// </summary>
    [Serializable]
    public class BSPNode
    {
        [Header("Node Properties")]
        [SerializeField] private RectInt _bounds;
        [SerializeField] private bool _isLeaf;
        [SerializeField] private int _depth;
        
        [Header("Partition Data")]
        [SerializeField] private bool _isHorizontalSplit;
        [SerializeField] private int _splitPosition;
        
        [Header("Tree Structure")]
        [SerializeField] private BSPNode _left;
        [SerializeField] private BSPNode _right;
        [SerializeField] private BSPNode _parent;
        
        [Header("Room Data (Leaf Nodes Only)")]
        [SerializeField] private RectInt _roomBounds;

        // Public Properties
        public RectInt Bounds => _bounds;
        public bool IsLeaf => _isLeaf;
        public int Depth => _depth;
        public bool IsHorizontalSplit => _isHorizontalSplit;
        public int SplitPosition => _splitPosition;
        public BSPNode Left => _left;
        public BSPNode Right => _right;
        public BSPNode Parent => _parent;
        public RectInt RoomBounds => _roomBounds;

        // Constructor for root node
        public BSPNode(RectInt bounds)
        {
            _bounds = bounds;
            _isLeaf = true;
            _depth = 0;
            _left = null;
            _right = null;
            _parent = null;
            _roomBounds = new RectInt();
        }

        // Constructor for child nodes
        private BSPNode(RectInt bounds, bool isLeaf, int depth, BSPNode parent)
        {
            _bounds = bounds;
            _isLeaf = isLeaf;
            _depth = depth;
            _parent = parent;
            _left = null;
            _right = null;
            _roomBounds = new RectInt();
        }

        /// <summary>
        /// Splits this node into two child nodes.
        /// </summary>
        /// <param name="minRoomSize">Minimum size for leaf nodes</param>
        /// <param name="maxDepth">Maximum recursion depth</param>
        /// <returns>True if split was successful</returns>
        public bool Split(int minRoomSize, int maxDepth)
        {
            // Can't split if already split or at max depth
            if (!_isLeaf || _depth >= maxDepth)
                return false;

            // Can't split if too small
            if (_bounds.width < minRoomSize * 2 || _bounds.height < minRoomSize * 2)
                return false;

            // Determine split direction (alternate based on depth, but consider aspect ratio)
            bool preferHorizontal = _depth % 2 == 0;
            bool canSplitHorizontal = _bounds.height >= minRoomSize * 2;
            bool canSplitVertical = _bounds.width >= minRoomSize * 2;

            if (!canSplitHorizontal && !canSplitVertical)
                return false;

            // Choose split direction
            if (canSplitHorizontal && (!canSplitVertical || preferHorizontal))
            {
                _isHorizontalSplit = true;
                _splitPosition = _bounds.y + _bounds.height / 2;
                
                // Create child bounds
                RectInt leftBounds = new RectInt(_bounds.x, _bounds.y, _bounds.width, _splitPosition - _bounds.y);
                RectInt rightBounds = new RectInt(_bounds.x, _splitPosition, _bounds.width, _bounds.yMax - _splitPosition);
                
                _left = new BSPNode(leftBounds, true, _depth + 1, this);
                _right = new BSPNode(rightBounds, true, _depth + 1, this);
            }
            else
            {
                _isHorizontalSplit = false;
                _splitPosition = _bounds.x + _bounds.width / 2;
                
                // Create child bounds
                RectInt leftBounds = new RectInt(_bounds.x, _bounds.y, _splitPosition - _bounds.x, _bounds.height);
                RectInt rightBounds = new RectInt(_splitPosition, _bounds.y, _bounds.xMax - _splitPosition, _bounds.height);
                
                _left = new BSPNode(leftBounds, true, _depth + 1, this);
                _right = new BSPNode(rightBounds, true, _depth + 1, this);
            }

            _isLeaf = false;
            return true;
        }

        /// <summary>
        /// Sets room bounds for this leaf node.
        /// </summary>
        /// <param name="roomBounds">Room bounds (must be within node bounds)</param>
        public void SetRoomBounds(RectInt roomBounds)
        {
            if (!_isLeaf)
            {
                Debug.LogWarning($"Attempted to set room bounds on non-leaf node at depth {_depth}");
                return;
            }

            if (!_bounds.Contains(roomBounds.min) || !_bounds.Contains(roomBounds.max - Vector2Int.one))
            {
                Debug.LogWarning($"Room bounds {roomBounds} exceed node bounds {_bounds}");
                return;
            }

            _roomBounds = roomBounds;
        }

        /// <summary>
        /// Gets all leaf nodes in the subtree rooted at this node.
        /// </summary>
        /// <returns>List of leaf nodes</returns>
        public System.Collections.Generic.List<BSPNode> GetLeafNodes()
        {
            var leaves = new System.Collections.Generic.List<BSPNode>();
            GetLeafNodesRecursive(leaves);
            return leaves;
        }

        private void GetLeafNodesRecursive(System.Collections.Generic.List<BSPNode> leaves)
        {
            if (_isLeaf)
            {
                leaves.Add(this);
            }
            else
            {
                _left?.GetLeafNodesRecursive(leaves);
                _right?.GetLeafNodesRecursive(leaves);
            }
        }

        /// <summary>
        /// Validates the BSP tree structure.
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate bounds
            if (_bounds.width <= 0 || _bounds.height <= 0)
                result.AddError($"Node has invalid bounds: {_bounds}");

            // Validate depth
            if (_depth < 0)
                result.AddError($"Node has negative depth: {_depth}");

            // Validate leaf node consistency
            if (_isLeaf)
            {
                if (_left != null || _right != null)
                    result.AddError($"Leaf node has children at depth {_depth}");

                // Validate room bounds if set
                if (_roomBounds.width > 0 || _roomBounds.height > 0)
                {
                    if (!_bounds.Contains(_roomBounds.min) || !_bounds.Contains(_roomBounds.max - Vector2Int.one))
                        result.AddError($"Room bounds {_roomBounds} exceed node bounds {_bounds}");
                }
            }
            else
            {
                // Internal node must have children
                if (_left == null || _right == null)
                    result.AddError($"Internal node missing children at depth {_depth}");

                // Validate split position
                if (_isHorizontalSplit)
                {
                    if (_splitPosition <= _bounds.y || _splitPosition >= _bounds.yMax)
                        result.AddError($"Invalid horizontal split position: {_splitPosition} in bounds {_bounds}");
                }
                else
                {
                    if (_splitPosition <= _bounds.x || _splitPosition >= _bounds.xMax)
                        result.AddError($"Invalid vertical split position: {_splitPosition} in bounds {_bounds}");
                }

                // Validate children
                _left?.Validate().Merge(result);
                _right?.Validate().Merge(result);
            }

            return result;
        }

        /// <summary>
        /// Gets a string representation of the node for debugging.
        /// </summary>
        public override string ToString()
        {
            if (_isLeaf)
            {
                return $"Leaf[Depth:{_depth}, Bounds:{_bounds}, Room:{_roomBounds}]";
            }
            else
            {
                return $"Node[Depth:{_depth}, Bounds:{_bounds}, Split:{(_isHorizontalSplit ? "H" : "V")}@{_splitPosition}]";
            }
        }
    }
}