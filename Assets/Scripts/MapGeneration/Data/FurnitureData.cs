using System;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Represents a placed furniture object in the map.
    /// Contains position, rotation, type, and variant information.
    /// </summary>
    [Serializable]
    public class FurnitureData
    {
        [Header("Identity")]
        [SerializeField] private string _furnitureID;
        [SerializeField] private string _furnitureType;
        [SerializeField] private string _prefabPath;

        [Header("Position")]
        [SerializeField] private int _roomID;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private Vector2Int _size;
        [SerializeField] private int _rotation; // 0, 90, 180, 270 degrees

        [Header("Variation")]
        [SerializeField] private int _variantIndex;
        [SerializeField] private bool _isFlipped;

        [Header("Properties")]
        [SerializeField] private bool _blocksMovement;
        [SerializeField] private bool _blocksSight;
        [SerializeField] private float _health;

        // Public Properties
        public string FurnitureID => _furnitureID;
        public string FurnitureType => _furnitureType;
        public string PrefabPath => _prefabPath;
        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public Vector2Int Size => _size;
        public int Rotation => _rotation;
        public int VariantIndex => _variantIndex;
        public bool IsFlipped => _isFlipped;
        public bool BlocksMovement => _blocksMovement;
        public bool BlocksSight => _blocksSight;
        public float Health => _health;

        public RectInt Bounds => new RectInt(_position, _size);

        // Constructor
        public FurnitureData(string furnitureID, string furnitureType, string prefabPath, 
                           int roomID, Vector2Int position, Vector2Int size)
        {
            _furnitureID = furnitureID ?? throw new ArgumentNullException(nameof(furnitureID));
            _furnitureType = furnitureType ?? throw new ArgumentNullException(nameof(furnitureType));
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _roomID = roomID;
            _position = position;
            _size = size;
            _rotation = 0;
            _variantIndex = 0;
            _isFlipped = false;
            _blocksMovement = true;
            _blocksSight = false;
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

        public void SetBlockingProperties(bool blocksMovement, bool blocksSight)
        {
            _blocksMovement = blocksMovement;
            _blocksSight = blocksSight;
        }

        public void SetHealth(float health)
        {
            _health = Mathf.Max(0f, health);
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

        public Vector3 GetWorldPosition(Vector3 tilemapOffset)
        {
            return tilemapOffset + new Vector3(_position.x + _size.x * 0.5f, _position.y + _size.y * 0.5f, 0);
        }

        // Validation
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_furnitureID) &&
                   !string.IsNullOrEmpty(_furnitureType) &&
                   !string.IsNullOrEmpty(_prefabPath) &&
                   _roomID >= 0 &&
                   _size.x > 0 && _size.y > 0 &&
                   _rotation % 90 == 0;
        }

        // Clone
        public FurnitureData Clone()
        {
            var clone = new FurnitureData(_furnitureID, _furnitureType, _prefabPath, _roomID, _position, _size);
            clone._rotation = _rotation;
            clone._variantIndex = _variantIndex;
            clone._isFlipped = _isFlipped;
            clone._blocksMovement = _blocksMovement;
            clone._blocksSight = _blocksSight;
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
        public string[] AllowedPrefabs => _allowedPrefabs;
        public Vector2Int MinRoomSize => _minRoomSize;
        public Vector2Int MaxRoomSize => _maxRoomSize;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
        public float PlacementProbability => _placementProbability;
        public bool PlaceAgainstWalls => _placeAgainstWalls;
        public bool PlaceInCenter => _placeInCenter;
        public int MinDistanceFromWalls => _minDistanceFromWalls;
        public int MinDistanceFromDoorways => _minDistanceFromDoorways;
        public bool AllowRotation => _allowRotation;
        public bool AllowFlipping => _allowFlipping;
        public int VariantCount => _variantCount;

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