using System;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

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
            return Split(minRoomSize, maxDepth, null, SplitPreference.Alternate, 0.3f, 0.1f);
        }

        /// <summary>
        /// Splits this node into two child nodes with full configuration.
        /// </summary>
        /// <param name="minRoomSize">Minimum size for leaf nodes</param>
        /// <param name="maxDepth">Maximum recursion depth</param>
        /// <param name="random">Random number generator for deterministic splitting</param>
        /// <param name="splitPreference">Preferred split direction strategy</param>
        /// <param name="splitPositionVariation">Variation in split position (0-1)</param>
        /// <param name="stopSplittingChance">Chance to stop splitting (0-1)</param>
        /// <returns>True if split was successful</returns>
        public bool Split(int minRoomSize, int maxDepth, System.Random random, 
            SplitPreference splitPreference = SplitPreference.Alternate, 
            float splitPositionVariation = 0.3f, 
            float stopSplittingChance = 0.1f)
        {
            // Can't split if already split or at max depth
            if (!_isLeaf || _depth >= maxDepth)
                return false;

            // Check stop splitting chance
            if (random != null && random.NextDouble() < stopSplittingChance)
                return false;

            // Can't split if too small
            if (_bounds.width < minRoomSize * 2 || _bounds.height < minRoomSize * 2)
                return false;

            // Determine split direction based on preference
            bool preferHorizontal = DetermineSplitPreference(splitPreference);
            bool canSplitHorizontal = _bounds.height >= minRoomSize * 2;
            bool canSplitVertical = _bounds.width >= minRoomSize * 2;

            if (!canSplitHorizontal && !canSplitVertical)
                return false;

            // Choose split direction
            bool useHorizontalSplit;
            if (canSplitHorizontal && !canSplitVertical)
                useHorizontalSplit = true;
            else if (canSplitVertical && !canSplitHorizontal)
                useHorizontalSplit = false;
            else
                useHorizontalSplit = preferHorizontal;

            // Calculate split position with variation
            int splitPos = CalculateSplitPosition(useHorizontalSplit, minRoomSize, splitPositionVariation, random);

            // Create child bounds
            RectInt leftBounds, rightBounds;
            if (useHorizontalSplit)
            {
                leftBounds = new RectInt(_bounds.x, _bounds.y, _bounds.width, splitPos - _bounds.y);
                rightBounds = new RectInt(_bounds.x, splitPos, _bounds.width, _bounds.yMax - splitPos);
            }
            else
            {
                leftBounds = new RectInt(_bounds.x, _bounds.y, splitPos - _bounds.x, _bounds.height);
                rightBounds = new RectInt(splitPos, _bounds.y, _bounds.xMax - splitPos, _bounds.height);
            }

            // Validate child bounds meet minimum size requirements
            if (leftBounds.width < minRoomSize || leftBounds.height < minRoomSize ||
                rightBounds.width < minRoomSize || rightBounds.height < minRoomSize)
                return false;

            // Create child nodes
            _isHorizontalSplit = useHorizontalSplit;
            _splitPosition = splitPos;
            _left = new BSPNode(leftBounds, true, _depth + 1, this);
            _right = new BSPNode(rightBounds, true, _depth + 1, this);

            _isLeaf = false;
            return true;
        }

        /// <summary>
        /// Determines split direction based on preference strategy.
        /// </summary>
        private bool DetermineSplitPreference(SplitPreference preference)
        {
            switch (preference)
            {
                case SplitPreference.Horizontal:
                    return true;
                case SplitPreference.Vertical:
                    return false;
                case SplitPreference.Alternate:
                    return _depth % 2 == 0;
                case SplitPreference.Random:
                    return UnityEngine.Random.value < 0.5f;
                case SplitPreference.Balanced:
                    // Prefer splitting along the longer axis
                    return _bounds.height > _bounds.width;
                default:
                    return _depth % 2 == 0; // Default to alternate
            }
        }

        /// <summary>
        /// Calculates split position with optional variation.
        /// </summary>
        private int CalculateSplitPosition(bool isHorizontal, int minRoomSize, float variation, System.Random random)
        {
            int availableSpace;
            int minSplitPos, maxSplitPos;
            
            if (isHorizontal)
            {
                availableSpace = _bounds.height;
                minSplitPos = _bounds.y + minRoomSize;
                maxSplitPos = _bounds.yMax - minRoomSize;
            }
            else
            {
                availableSpace = _bounds.width;
                minSplitPos = _bounds.x + minRoomSize;
                maxSplitPos = _bounds.xMax - minRoomSize;
            }

            // Default to middle split
            int splitPos = (minSplitPos + maxSplitPos) / 2;

            // Apply variation if random is provided and variation > 0
            if (random != null && variation > 0f)
            {
                int variationRange = Mathf.FloorToInt((maxSplitPos - minSplitPos) * variation);
                if (variationRange > 0)
                {
                    int offset = random.Next(-variationRange, variationRange + 1);
                    splitPos = Mathf.Clamp(splitPos + offset, minSplitPos, maxSplitPos);
                }
            }

            return splitPos;
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
        /// Recursively splits this node and all its children.
        /// </summary>
        /// <param name="config">BSP configuration settings</param>
        /// <param name="random">Random number generator for deterministic splitting</param>
        public void SplitRecursive(BSPConfiguration config, System.Random random = null)
        {
            if (Split(config.MinPartitionSize, config.MaxDepth, random, 
                config.SplitPreference, config.SplitPositionVariation, config.StopSplittingChance))
            {
                // Successfully split, now recursively split children
                _left?.SplitRecursive(config, random);
                _right?.SplitRecursive(config, random);
            }
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
        /// Draws Gizmo visualization for this node and its children.
        /// </summary>
        /// <param name="showRooms">Whether to show room bounds for leaf nodes</param>
        /// <param name="showSplits">Whether to show split lines for internal nodes</param>
        public void DrawGizmos(bool showRooms = true, bool showSplits = true)
        {
            // Set color based on depth
            Gizmos.color = GetDepthColor();
            
            // Draw node bounds
            Gizmos.DrawLine(
                new Vector3(_bounds.x, _bounds.y, 0),
                new Vector3(_bounds.xMax, _bounds.y, 0)
            );
            Gizmos.DrawLine(
                new Vector3(_bounds.xMax, _bounds.y, 0),
                new Vector3(_bounds.xMax, _bounds.yMax, 0)
            );
            Gizmos.DrawLine(
                new Vector3(_bounds.xMax, _bounds.yMax, 0),
                new Vector3(_bounds.x, _bounds.yMax, 0)
            );
            Gizmos.DrawLine(
                new Vector3(_bounds.x, _bounds.yMax, 0),
                new Vector3(_bounds.x, _bounds.y, 0)
            );

            // Draw split line for internal nodes
            if (!_isLeaf && showSplits)
            {
                Gizmos.color = Color.red;
                if (_isHorizontalSplit)
                {
                    Gizmos.DrawLine(
                        new Vector3(_bounds.x, _splitPosition, 0),
                        new Vector3(_bounds.xMax, _splitPosition, 0)
                    );
                }
                else
                {
                    Gizmos.DrawLine(
                        new Vector3(_splitPosition, _bounds.y, 0),
                        new Vector3(_splitPosition, _bounds.yMax, 0)
                    );
                }
            }

            // Draw room bounds for leaf nodes
            if (_isLeaf && showRooms && _roomBounds.width > 0 && _roomBounds.height > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    new Vector3(_roomBounds.x, _roomBounds.y, 0),
                    new Vector3(_roomBounds.xMax, _roomBounds.y, 0)
                );
                Gizmos.DrawLine(
                    new Vector3(_roomBounds.xMax, _roomBounds.y, 0),
                    new Vector3(_roomBounds.xMax, _roomBounds.yMax, 0)
                );
                Gizmos.DrawLine(
                    new Vector3(_roomBounds.xMax, _roomBounds.yMax, 0),
                    new Vector3(_roomBounds.x, _roomBounds.yMax, 0)
                );
                Gizmos.DrawLine(
                    new Vector3(_roomBounds.x, _roomBounds.yMax, 0),
                    new Vector3(_roomBounds.x, _roomBounds.y, 0)
                );
            }

            // Recursively draw children
            _left?.DrawGizmos(showRooms, showSplits);
            _right?.DrawGizmos(showRooms, showSplits);
        }

        /// <summary>
        /// Gets a color based on node depth for visualization.
        /// </summary>
        private Color GetDepthColor()
        {
            // Gradient from blue (shallow) to yellow (deep)
            float t = Mathf.Clamp01((float)_depth / 10f);
            return Color.Lerp(Color.blue, Color.yellow, t);
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