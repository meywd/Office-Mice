using System;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Lightweight struct representing a doorway position and orientation.
    /// Used extensively in pathfinding - value type for performance.
    /// </summary>
    [Serializable]
    public struct DoorwayPosition
    {
        public Vector2Int position;
        public DoorwayDirection direction;
        public int width; // Number of tiles wide (1-3)

        public DoorwayPosition(Vector2Int pos, DoorwayDirection dir, int w = 1)
        {
            position = pos;
            direction = dir;
            width = Mathf.Clamp(w, 1, 3);
        }

        public Vector2Int GetDirectionVector()
        {
            switch (direction)
            {
                case DoorwayDirection.North: return Vector2Int.up;
                case DoorwayDirection.South: return Vector2Int.down;
                case DoorwayDirection.East: return Vector2Int.right;
                case DoorwayDirection.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is DoorwayPosition other)
            {
                return position == other.position && direction == other.direction && width == other.width;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(position, (int)direction, width);
        }

        public static bool operator ==(DoorwayPosition left, DoorwayPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DoorwayPosition left, DoorwayPosition right)
        {
            return !left.Equals(right);
        }
    }

    public enum DoorwayDirection
    {
        North,
        South,
        East,
        West
    }
}