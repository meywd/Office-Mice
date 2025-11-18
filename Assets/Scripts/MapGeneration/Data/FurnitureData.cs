using System;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Represents a placed furniture object in the map.
    /// Contains position, rotation, type, and variant information.
    /// </summary>
    [Serializable]
    public class FurnitureData : PlacedObjectData
    {
        [Header("Furniture Specific")]
        [SerializeField] private string _furnitureID;
        [SerializeField] private string _furnitureType;
        [SerializeField] private string _prefabPath;
        [SerializeField] private int _rotation; // 0, 90, 180, 270 degrees

        [Header("Variation")]
        [SerializeField] private int _variantIndex;
        [SerializeField] private bool _isFlipped;

        [Header("Properties")]
        [SerializeField] private float _health;

        // Public Properties
        public string FurnitureID => _furnitureID;
        public string FurnitureType => _furnitureType;
        public string PrefabPath => _prefabPath;
        public int Rotation => _rotation;
        public int VariantIndex => _variantIndex;
        public bool IsFlipped => _isFlipped;
        public float Health => _health;

        // Constructor
        public FurnitureData(string furnitureID, string furnitureType, string prefabPath,
                           int roomID, Vector2Int position, Vector2Int size)
            : base(furnitureID, furnitureType, roomID, position, size)
        {
            _furnitureID = furnitureID ?? throw new ArgumentNullException(nameof(furnitureID));
            _furnitureType = furnitureType ?? throw new ArgumentNullException(nameof(furnitureType));
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _rotation = 0;
            _variantIndex = 0;
            _isFlipped = false;
            _health = 100f;
        }

        // Mutators
        public void SetRotation(int rotation)
        {
            _rotation = ((rotation % 360) + 360) % 360; // Normalize to 0-359
            if (_rotation % 90 != 0)
                _rotation = Mathf.RoundToInt(_rotation / 90f) * 90; // Snap to 90-degree increments
        }

        public void SetVariant(int variantIndex)
        {
            _variantIndex = Mathf.Max(0, variantIndex);
        }

        public void SetFlipped(bool flipped)
        {
            _isFlipped = flipped;
        }

        public void SetHealth(float health)
        {
            _health = Mathf.Max(0f, health);
        }

        // Query Methods
        public bool OverlapsWith(FurnitureData other)
        {
            return OverlapsWith(other.Bounds);
        }

        public Vector2Int GetRotatedSize()
        {
            if (_rotation == 90 || _rotation == 270)
                return new Vector2Int(_size.y, _size.x);
            return _size;
        }

        public Quaternion GetRotationQuaternion()
        {
            return Quaternion.Euler(0, 0, _rotation);
        }

        public new Vector3 GetWorldPosition(Vector3 tilemapOffset)
        {
            return tilemapOffset + new Vector3(_position.x + _size.x * 0.5f, _position.y + _size.y * 0.5f, 0);
        }

        // Validation
        public new bool IsValid()
        {
            return !string.IsNullOrEmpty(_furnitureID) &&
                   !string.IsNullOrEmpty(_furnitureType) &&
                   !string.IsNullOrEmpty(_prefabPath) &&
                   _roomID >= 0 &&
                   _size.x > 0 && _size.y > 0 &&
                   _rotation % 90 == 0;
        }

        // Clone
        public override PlacedObjectData Clone()
        {
            var clone = new FurnitureData(_furnitureID, _furnitureType, _prefabPath, _roomID, _position, _size);
            clone._rotation = _rotation;
            clone._variantIndex = _variantIndex;
            clone._isFlipped = _isFlipped;
            clone.SetCollisionProperties(_blocksMovement, _blocksSight, _collisionLayer);
            clone._health = _health;
            return clone;
        }

        public override string ToString()
        {
            return $"Furniture[{_furnitureID}] {_furnitureType} at {_position} (rot:{_rotation}, var:{_variantIndex})";
        }

        public override bool Equals(object obj)
        {
            if (obj is FurnitureData other)
            {
                return _furnitureID == other._furnitureID && _position == other._position;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_furnitureID, _position);
        }
    }

    /// <summary>
    /// Defines furniture placement rules for a specific room type.
    /// </summary>
    [Serializable]
    public class FurniturePlacementRule
    {
        [Header("Furniture Type")]
        [SerializeField] private string _furnitureType;
        [SerializeField] private string[] _allowedPrefabs;

        [Header("Placement Constraints")]
        [SerializeField] private Vector2Int _minRoomSize;
        [SerializeField] private Vector2Int _maxRoomSize;
        [SerializeField] private int _minCount;
        [SerializeField] private int _maxCount;
        [SerializeField] private float _placementProbability;

        [Header("Positioning")]
        [SerializeField] private bool _placeAgainstWalls;
        [SerializeField] private bool _placeInCenter;
        [SerializeField] private int _minDistanceFromWalls;
        [SerializeField] private int _minDistanceFromDoorways;

        [Header("Variation")]
        [SerializeField] private bool _allowRotation;
        [SerializeField] private bool _allowFlipping;
        [SerializeField] private int _variantCount;

        // Public Properties
        public string FurnitureType => _furnitureType;
        public string[] AllowedPrefabs { get => _allowedPrefabs; set => _allowedPrefabs = value; }
        public Vector2Int MinRoomSize { get => _minRoomSize; set => _minRoomSize = value; }
        public Vector2Int MaxRoomSize { get => _maxRoomSize; set => _maxRoomSize = value; }
        public int MinCount { get => _minCount; set => _minCount = value; }
        public int MaxCount { get => _maxCount; set => _maxCount = value; }
        public float PlacementProbability { get => _placementProbability; set => _placementProbability = value; }
        public bool PlaceAgainstWalls { get => _placeAgainstWalls; set => _placeAgainstWalls = value; }
        public bool PlaceInCenter { get => _placeInCenter; set => _placeInCenter = value; }
        public int MinDistanceFromWalls { get => _minDistanceFromWalls; set => _minDistanceFromWalls = value; }
        public int MinDistanceFromDoorways { get => _minDistanceFromDoorways; set => _minDistanceFromDoorways = value; }
        public bool AllowRotation { get => _allowRotation; set => _allowRotation = value; }
        public bool AllowFlipping { get => _allowFlipping; set => _allowFlipping = value; }
        public int VariantCount { get => _variantCount; set => _variantCount = value; }

        // Constructor
        public FurniturePlacementRule(string furnitureType)
        {
            _furnitureType = furnitureType ?? throw new ArgumentNullException(nameof(furnitureType));
            _allowedPrefabs = new string[0];
            _minRoomSize = Vector2Int.one;
            _maxRoomSize = new Vector2Int(20, 20);
            _minCount = 0;
            _maxCount = 5;
            _placementProbability = 0.5f;
            _placeAgainstWalls = false;
            _placeInCenter = false;
            _minDistanceFromWalls = 1;
            _minDistanceFromDoorways = 2;
            _allowRotation = true;
            _allowFlipping = true;
            _variantCount = 1;
        }

        // Query Methods
        public bool IsValidForRoom(RoomData room)
        {
            var roomSize = new Vector2Int(room.Bounds.width, room.Bounds.height);
            return roomSize.x >= _minRoomSize.x && roomSize.y >= _minRoomSize.y &&
                   roomSize.x <= _maxRoomSize.x && roomSize.y <= _maxRoomSize.y;
        }

        public int GetCountForRoom(RoomData room, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (!IsValidForRoom(room) || random.NextDouble() > _placementProbability)
                return 0;

            return random.Next(_minCount, _maxCount + 1);
        }
    }
}