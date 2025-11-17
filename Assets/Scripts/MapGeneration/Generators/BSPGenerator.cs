using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Generators
{
    /// <summary>
    /// Binary Space Partitioning room generator.
    /// Implements deterministic BSP algorithm with configurable parameters.
    /// </summary>
    public class BSPGenerator : IRoomGenerator
    {
        #region Events
        public event Action<RoomData> OnRoomGenerated;
        public event Action<RoomData, Exception> OnRoomGenerationFailed;
        #endregion

        #region Private Fields
        private System.Random _random;
        private int _lastSeed;
        private BSPNode _rootNode;
        private List<RoomData> _generatedRooms;
        #endregion

        #region IRoomGenerator Implementation
        public List<RoomData> GenerateRooms(MapGenerationSettings settings)
        {
            return GenerateRooms(settings, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public List<RoomData> GenerateRooms(MapGenerationSettings settings, int seed)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                // Initialize deterministic random number generator
                _random = new System.Random(seed);
                _lastSeed = seed;
                _generatedRooms = new List<RoomData>();

                // Create root BSP node
                _rootNode = new BSPNode(settings.mapBounds);

                // Generate BSP tree
                _rootNode.SplitRecursive(settings.bsp, _random);

                // Create rooms from leaf nodes
                var leafNodes = _rootNode.GetLeafNodes();
                int roomID = 0;

                foreach (var leaf in leafNodes)
                {
                    var room = CreateRoomFromLeaf(leaf, settings, roomID++);
                    if (room != null)
                    {
                        _generatedRooms.Add(room);
                        OnRoomGenerated?.Invoke(room);
                    }
                }

                return _generatedRooms;
            }
            catch (Exception ex)
            {
                OnRoomGenerationFailed?.Invoke(null, ex);
                throw new InvalidOperationException($"BSP room generation failed: {ex.Message}", ex);
            }
        }

        public ValidationResult ValidateRoomPlacement(List<RoomData> rooms, MapGenerationSettings settings)
        {
            var result = new ValidationResult();

            if (rooms == null)
            {
                result.AddError("Room list is null");
                return result;
            }

            // Check for overlapping rooms
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (rooms[i].OverlapsWith(rooms[j].Bounds))
                    {
                        result.AddError($"Room {rooms[i].RoomID} overlaps with Room {rooms[j].RoomID}");
                    }
                }
            }

            // Check room bounds
            foreach (var room in rooms)
            {
                if (!settings.mapBounds.Contains(room.Bounds.min) || 
                    !settings.mapBounds.Contains(room.Bounds.max - Vector2Int.one))
                {
                    result.AddError($"Room {room.RoomID} bounds exceed map bounds");
                }

                // Validate room size
                if (room.Bounds.width < settings.bsp.MinPartitionSize || 
                    room.Bounds.height < settings.bsp.MinPartitionSize)
                {
                    result.AddError($"Room {room.RoomID} is smaller than minimum size");
                }
            }

            return result;
        }

        public List<RoomData> OptimizeRoomLayout(List<RoomData> rooms, MapGenerationSettings settings)
        {
            // BSP algorithm naturally produces optimal layouts
            // This is a placeholder for future optimizations
            return rooms?.ToList() ?? new List<RoomData>();
        }

        public List<RoomData> ClassifyRooms(List<RoomData> rooms, MapGenerationSettings settings)
        {
            if (rooms == null) 
                return new List<RoomData>();

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                // Use RoomClassifier for automatic classification
                var classificationSettings = settings.ClassificationSettings;
                if (classificationSettings == null)
                {
                    // Fallback to unassigned classification if no settings configured
                    foreach (var room in rooms)
                    {
                        room.SetClassification(RoomClassification.Unassigned);
                    }
                    return rooms;
                }

                var classifier = new RoomClassifier(classificationSettings, _lastSeed);
                var classifiedRooms = classifier.ClassifyRooms(rooms, settings.mapBounds);

                return classifiedRooms;
            }
            catch (Exception ex)
            {
                OnRoomGenerationFailed?.Invoke(null, ex);
                throw new InvalidOperationException($"Room classification failed: {ex.Message}", ex);
            }
        }

        public float CalculateTotalRoomArea(List<RoomData> rooms)
        {
            if (rooms == null) return 0f;
            return rooms.Sum(room => room.Area);
        }

        public Vector2Int? FindOptimalRoomPosition(List<RoomData> existingRooms, Vector2Int newRoomSize, MapGenerationSettings settings)
        {
            // BSP algorithm handles room positioning
            // This is a placeholder for future enhancements
            return null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates a RoomData from a BSP leaf node.
        /// </summary>
        private RoomData CreateRoomFromLeaf(BSPNode leaf, MapGenerationSettings settings, int roomID)
        {
            if (leaf == null || !leaf.IsLeaf)
                return null;

            try
            {
                // Calculate room bounds within the leaf node
                RectInt roomBounds = CalculateRoomBounds(leaf.Bounds, settings.bsp);
                
                // Create room data
                var room = new RoomData(roomBounds);
                room.RoomID = roomID;

                // Set room bounds in the BSP node for visualization
                leaf.SetRoomBounds(roomBounds);

                return room;
            }
            catch (Exception ex)
            {
                OnRoomGenerationFailed?.Invoke(null, ex);
                return null;
            }
        }

        /// <summary>
        /// Calculates room bounds within a BSP leaf node.
        /// </summary>
        private RectInt CalculateRoomBounds(RectInt leafBounds, BSPConfiguration config)
        {
            int roomWidth = Mathf.Max(3, Mathf.FloorToInt(leafBounds.width * config.RoomSizeRatio));
            int roomHeight = Mathf.Max(3, Mathf.FloorToInt(leafBounds.height * config.RoomSizeRatio));

            int roomX, roomY;

            if (config.CenterRooms)
            {
                // Center the room in the leaf
                roomX = leafBounds.x + (leafBounds.width - roomWidth) / 2;
                roomY = leafBounds.y + (leafBounds.height - roomHeight) / 2;
            }
            else
            {
                // Add some variation to room position
                int maxX = leafBounds.xMax - roomWidth;
                int maxY = leafBounds.yMax - roomHeight;
                
                if (config.RoomPositionVariation > 0f && _random != null)
                {
                    int variationX = Mathf.FloorToInt((maxX - leafBounds.x) * config.RoomPositionVariation);
                    int variationY = Mathf.FloorToInt((maxY - leafBounds.y) * config.RoomPositionVariation);
                    
                    roomX = leafBounds.x + _random.Next(0, variationX + 1);
                    roomY = leafBounds.y + _random.Next(0, variationY + 1);
                }
                else
                {
                    roomX = leafBounds.x;
                    roomY = leafBounds.y;
                }

                // Ensure room stays within bounds
                roomX = Mathf.Clamp(roomX, leafBounds.x, maxX);
                roomY = Mathf.Clamp(roomY, leafBounds.y, maxY);
            }

            return new RectInt(roomX, roomY, roomWidth, roomHeight);
        }
        #endregion

        #region Public Utility Methods
        /// <summary>
        /// Gets the BSP tree root node for visualization/debugging.
        /// </summary>
        public BSPNode GetRootNode() => _rootNode;

        /// <summary>
        /// Gets the last used seed for deterministic generation.
        /// </summary>
        public int GetLastSeed() => _lastSeed;

        /// <summary>
        /// Draws Gizmo visualization of the BSP tree.
        /// Call this from a MonoBehaviour's OnDrawGizmos method.
        /// </summary>
        public void DrawGizmos(bool showRooms = true, bool showSplits = true)
        {
            _rootNode?.DrawGizmos(showRooms, showSplits);
        }

        /// <summary>
        /// Validates the BSP tree structure.
        /// </summary>
        public ValidationResult ValidateBSPStructure()
        {
            return _rootNode?.Validate() ?? new ValidationResult().AddError("BSP root node is null");
        }

        /// <summary>
        /// Gets statistics about the generated BSP tree.
        /// </summary>
        public BSPStatistics GetStatistics()
        {
            if (_rootNode == null)
                return new BSPStatistics();

            var stats = new BSPStatistics();
            CalculateStatistics(_rootNode, stats);
            return stats;
        }

        private void CalculateStatistics(BSPNode node, BSPStatistics stats)
        {
            stats.TotalNodes++;
            stats.MaxDepth = Mathf.Max(stats.MaxDepth, node.Depth);

            if (node.IsLeaf)
            {
                stats.LeafNodes++;
                if (node.RoomBounds.width > 0 && node.RoomBounds.height > 0)
                {
                    stats.RoomsGenerated++;
                    stats.TotalRoomArea += node.RoomBounds.width * node.RoomBounds.height;
                }
            }
            else
            {
                stats.InternalNodes++;
                if (node.IsHorizontalSplit)
                    stats.HorizontalSplits++;
                else
                    stats.VerticalSplits++;

                node.Left?.CalculateStatistics(node.Left, stats);
                node.Right?.CalculateStatistics(node.Right, stats);
            }
        }
        #endregion
    }

    /// <summary>
    /// Statistics about the generated BSP tree.
    /// </summary>
    [Serializable]
    public class BSPStatistics
    {
        public int TotalNodes;
        public int InternalNodes;
        public int LeafNodes;
        public int RoomsGenerated;
        public int MaxDepth;
        public int HorizontalSplits;
        public int VerticalSplits;
        public int TotalRoomArea;

        public override string ToString()
        {
            return $"BSP Stats: {TotalNodes} nodes ({InternalNodes} internal, {LeafNodes} leaf), " +
                   $"{RoomsGenerated} rooms, Max depth: {MaxDepth}, " +
                   $"Splits: {HorizontalSplits}H/{VerticalSplits}V, " +
                   $"Total room area: {TotalRoomArea}";
        }
    }
}