using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Base class for any placed object in the map (furniture, resources, etc.).
    /// Provides common positioning and collision detection functionality.
    /// </summary>
    [Serializable]
    public abstract class PlacedObjectData
    {
        [Header("Identity")]
        [SerializeField] protected string _objectID;
        [SerializeField] protected string _objectType;
        [SerializeField] protected int _roomID;

        [Header("Position")]
        [SerializeField] protected Vector2Int _position;
        [SerializeField] protected Vector2Int _size;

        [Header("Collision")]
        [SerializeField] protected bool _blocksMovement;
        [SerializeField] protected bool _blocksSight;
        [SerializeField] protected LayerMask _collisionLayer;

        // Public Properties
        public string ObjectID => _objectID;
        public string ObjectType => _objectType;
        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public Vector2Int Size => _size;
        public bool BlocksMovement => _blocksMovement;
        public bool BlocksSight => _blocksSight;
        public LayerMask CollisionLayer => _collisionLayer;

        public RectInt Bounds => new RectInt(_position, _size);

        // Constructor
        protected PlacedObjectData(string objectID, string objectType, int roomID, 
                                  Vector2Int position, Vector2Int size)
        {
            _objectID = objectID ?? throw new ArgumentNullException(nameof(objectID));
            _objectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            _roomID = roomID;
            _position = position;
            _size = size;
            _blocksMovement = true;
            _blocksSight = false;
            _collisionLayer = 0;
        }

        // Mutators
        public void SetPosition(Vector2Int position)
        {
            _position = position;
        }

        public void SetSize(Vector2Int size)
        {
            _size = Vector2Int.Max(Vector2Int.one, size);
        }

        public void SetCollisionProperties(bool blocksMovement, bool blocksSight, LayerMask layer = default)
        {
            _blocksMovement = blocksMovement;
            _blocksSight = blocksSight;
            _collisionLayer = layer;
        }

        // Query Methods
        public bool ContainsPoint(Vector2Int point)
        {
            return Bounds.Contains(point);
        }

        public bool OverlapsWith(RectInt otherBounds)
        {
            return Bounds.Overlaps(otherBounds);
        }

        public bool OverlapsWith(PlacedObjectData other)
        {
            return OverlapsWith(other.Bounds);
        }

        public Vector2Int GetCenter()
        {
            return new Vector2Int(_position.x + _size.x / 2, _position.y + _size.y / 2);
        }

        public Vector3 GetWorldPosition(Vector3 tilemapOffset)
        {
            return tilemapOffset + new Vector3(_position.x + _size.x * 0.5f, _position.y + _size.y * 0.5f, 0);
        }

        // Validation
        public virtual bool IsValid()
        {
            return !string.IsNullOrEmpty(_objectID) &&
                   !string.IsNullOrEmpty(_objectType) &&
                   _roomID >= 0 &&
                   _size.x > 0 && _size.y > 0;
        }

        // Abstract Methods
        public abstract PlacedObjectData Clone();

        // Utility Methods
        public override string ToString()
        {
            return $"{_objectType}[{_objectID}] at {_position} ({_size.x}x{_size.y})";
        }

        public override bool Equals(object obj)
        {
            if (obj is PlacedObjectData other)
            {
                return _objectID == other._objectID && _position == other._position;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_objectID, _position);
        }
    }

    /// <summary>
    /// Grid-based collision detection system for placed objects.
    /// Provides efficient spatial queries and collision detection.
    /// </summary>
    public class GridCollisionDetector
    {
        private readonly Dictionary<Vector2Int, List<PlacedObjectData>> _grid;
        private readonly Vector2Int _gridSize;
        private readonly int _cellSize;

        public GridCollisionDetector(Vector2Int gridSize, int cellSize = 1)
        {
            _gridSize = gridSize;
            _cellSize = Mathf.Max(1, cellSize);
            _grid = new Dictionary<Vector2Int, List<PlacedObjectData>>();
        }

        /// <summary>
        /// Adds an object to the collision grid.
        /// </summary>
        public void AddObject(PlacedObjectData obj)
        {
            if (obj == null || !obj.IsValid())
                return;

            var cells = GetOccupiedCells(obj);
            foreach (var cell in cells)
            {
                if (!_grid.ContainsKey(cell))
                    _grid[cell] = new List<PlacedObjectData>();
                
                if (!_grid[cell].Contains(obj))
                    _grid[cell].Add(obj);
            }
        }

        /// <summary>
        /// Removes an object from the collision grid.
        /// </summary>
        public void RemoveObject(PlacedObjectData obj)
        {
            if (obj == null)
                return;

            var cells = GetOccupiedCells(obj);
            foreach (var cell in cells)
            {
                if (_grid.ContainsKey(cell))
                {
                    _grid[cell].Remove(obj);
                    if (_grid[cell].Count == 0)
                        _grid.Remove(cell);
                }
            }
        }

        /// <summary>
        /// Checks if an object would collide with any existing objects.
        /// </summary>
        public bool HasCollision(PlacedObjectData obj, bool checkMovementBlocking = true)
        {
            if (obj == null)
                return false;

            var cells = GetOccupiedCells(obj);
            foreach (var cell in cells)
            {
                if (_grid.ContainsKey(cell))
                {
                    foreach (var other in _grid[cell])
                    {
                        if (other != obj && (!checkMovementBlocking || other.BlocksMovement))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all objects that collide with the given object.
        /// </summary>
        public List<PlacedObjectData> GetCollisions(PlacedObjectData obj, bool checkMovementBlocking = true)
        {
            var collisions = new List<PlacedObjectData>();
            if (obj == null)
                return collisions;

            var cells = GetOccupiedCells(obj);
            var checkedObjects = new HashSet<PlacedObjectData>();

            foreach (var cell in cells)
            {
                if (_grid.ContainsKey(cell))
                {
                    foreach (var other in _grid[cell])
                    {
                        if (other != obj && !checkedObjects.Contains(other) && 
                            (!checkMovementBlocking || other.BlocksMovement))
                        {
                            collisions.Add(other);
                            checkedObjects.Add(other);
                        }
                    }
                }
            }
            return collisions;
        }

        /// <summary>
        /// Gets all objects within a rectangular area.
        /// </summary>
        public List<PlacedObjectData> GetObjectsInArea(RectInt area)
        {
            var objects = new List<PlacedObjectData>();
            var cells = GetCellsInArea(area);
            var checkedObjects = new HashSet<PlacedObjectData>();

            foreach (var cell in cells)
            {
                if (_grid.ContainsKey(cell))
                {
                    foreach (var obj in _grid[cell])
                    {
                        if (!checkedObjects.Contains(obj) && obj.OverlapsWith(area))
                        {
                            objects.Add(obj);
                            checkedObjects.Add(obj);
                        }
                    }
                }
            }
            return objects;
        }

        /// <summary>
        /// Finds valid positions for placing an object in a room.
        /// </summary>
        public List<Vector2Int> FindValidPositions(RoomData room, Vector2Int objectSize, 
                                                 List<PlacedObjectData> existingObjects, 
                                                 int minDistance = 1)
        {
            var validPositions = new List<Vector2Int>();
            var roomBounds = room.Bounds;

            // Shrink room bounds by object size and minimum distance
            var searchBounds = new RectInt(
                roomBounds.x + minDistance,
                roomBounds.y + minDistance,
                Mathf.Max(0, roomBounds.width - objectSize.x - minDistance * 2),
                Mathf.Max(0, roomBounds.height - objectSize.y - minDistance * 2)
            );

            if (searchBounds.width <= 0 || searchBounds.height <= 0)
                return validPositions;

            // Create temporary collision detector with existing objects
            var tempDetector = new GridCollisionDetector(_gridSize, _cellSize);
            foreach (var obj in existingObjects)
            {
                tempDetector.AddObject(obj);
            }

            // Check each possible position
            for (int x = searchBounds.x; x <= searchBounds.xMax - objectSize.x; x++)
            {
                for (int y = searchBounds.y; y <= searchBounds.yMax - objectSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    var tempObj = new TempPlacedObject("temp", "temp", room.RoomID, position, objectSize);
                    
                    if (!tempDetector.HasCollision(tempObj))
                    {
                        validPositions.Add(position);
                    }
                }
            }

            return validPositions;
        }

        /// <summary>
        /// Clears all objects from the collision grid.
        /// </summary>
        public void Clear()
        {
            _grid.Clear();
        }

        /// <summary>
        /// Gets statistics about the collision grid.
        /// </summary>
        public (int totalCells, int occupiedCells, int totalObjects) GetStatistics()
        {
            int totalCells = (_gridSize.x / _cellSize) * (_gridSize.y / _cellSize);
            int occupiedCells = _grid.Count;
            int totalObjects = 0;

            foreach (var kvp in _grid)
            {
                totalObjects += kvp.Value.Count;
            }

            return (totalCells, occupiedCells, totalObjects);
        }

        // Private Helper Methods
        private List<Vector2Int> GetOccupiedCells(PlacedObjectData obj)
        {
            var cells = new List<Vector2Int>();
            var bounds = obj.Bounds;

            for (int x = bounds.x; x < bounds.xMax; x += _cellSize)
            {
                for (int y = bounds.y; y < bounds.yMax; y += _cellSize)
                {
                    cells.Add(new Vector2Int(x / _cellSize, y / _cellSize));
                }
            }

            return cells;
        }

        private List<Vector2Int> GetCellsInArea(RectInt area)
        {
            var cells = new List<Vector2Int>();

            for (int x = area.x; x < area.xMax; x += _cellSize)
            {
                for (int y = area.y; y < area.yMax; y += _cellSize)
                {
                    cells.Add(new Vector2Int(x / _cellSize, y / _cellSize));
                }
            }

            return cells;
        }

        // Temporary class for collision testing
        private class TempPlacedObject : PlacedObjectData
        {
            public TempPlacedObject(string objectID, string objectType, int roomID, 
                                  Vector2Int position, Vector2Int size) 
                : base(objectID, objectType, roomID, position, size)
            {
            }

            public override PlacedObjectData Clone()
            {
                return new TempPlacedObject(_objectID, _objectType, _roomID, _position, _size);
            }
        }
    }
}