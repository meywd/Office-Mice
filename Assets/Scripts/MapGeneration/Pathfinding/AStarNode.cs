using System;
using UnityEngine;

namespace OfficeMice.MapGeneration.Pathfinding
{
    /// <summary>
    /// Node used in A* pathfinding algorithm.
    /// Represents a position in the pathfinding grid with associated costs.
    /// </summary>
    public class AStarNode : IComparable<AStarNode>
    {
        #region Properties
        
        public Vector2Int Position { get; set; }
        public float GCost { get; set; }  // Cost from start to this node
        public float HCost { get; set; }  // Heuristic cost from this node to end
        public float FCost => GCost + HCost;  // Total cost
        public AStarNode Parent { get; set; }
        
        #endregion
        
        #region Constructor
        
        public AStarNode()
        {
            Reset();
        }
        
        #endregion
        
        #region Public Methods
        
        public void Reset()
        {
            Position = Vector2Int.zero;
            GCost = float.MaxValue;
            HCost = 0f;
            Parent = null;
        }
        
        public int CompareTo(AStarNode other)
        {
            if (other == null)
                return 1;
            
            // Compare by F cost first, then by H cost for tie-breaking
            int fCostComparison = FCost.CompareTo(other.FCost);
            if (fCostComparison != 0)
                return fCostComparison;
            
            // Tie-breaker: prefer nodes with lower H cost (closer to goal)
            return HCost.CompareTo(other.HCost);
        }
        
        #endregion
        
        #region Object Overrides
        
        public override bool Equals(object obj)
        {
            if (obj is AStarNode other)
                return Position.Equals(other.Position);
            return false;
        }
        
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
        
        public override string ToString()
        {
            return $"AStarNode[{Position}] G:{GCost:F2} H:{HCost:F2} F:{FCost:F2}";
        }
        
        #endregion
    }
}