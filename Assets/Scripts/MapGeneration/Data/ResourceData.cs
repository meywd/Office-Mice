using System;
using UnityEngine;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Represents a resource or collectible placed in the map.
    /// Includes health, ammo, power-ups, and other game resources.
    /// </summary>
    [Serializable]
    public class ResourceData
    {
        [Header("Identity")]
        [SerializeField] private string _resourceID;
        [SerializeField] private string _resourceType;
        [SerializeField] private string _prefabPath;

        [Header("Position")]
        [SerializeField] private int _roomID;
        [SerializeField] private Vector2Int _position;

        [Header("Resource Properties")]
        [SerializeField] private int _quantity;
        [SerializeField] private float _respawnTime;
        [SerializeField] private bool _isConsumable;

        [Header("Gameplay")]
        [SerializeField] private float _value;
        [SerializeField] private string _effectType;
        [SerializeField] private float _duration;

        // Public Properties
        public string ResourceID => _resourceID;
        public string ResourceType => _resourceType;
        public string PrefabPath => _prefabPath;
        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public int Quantity => _quantity;
        public float RespawnTime => _respawnTime;
        public bool IsConsumable => _isConsumable;
        public float Value => _value;
        public string EffectType => _effectType;
        public float Duration => _duration;

        // Constructor
        public ResourceData(string resourceID, string resourceType, string prefabPath, 
                          int roomID, Vector2Int position, int quantity = 1)
        {
            _resourceID = resourceID ?? throw new ArgumentNullException(nameof(resourceID));
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _roomID = roomID;
            _position = position;
            _quantity = Mathf.Max(1, quantity);
            _respawnTime = 0f;
            _isConsumable = true;
            _value = 1f;
            _effectType = string.Empty;
            _duration = 0f;
        }

        // Mutators
        public void SetRespawnTime(float respawnTime)
        {
            _respawnTime = Mathf.Max(0f, respawnTime);
        }

        public void SetConsumable(bool consumable)
        {
            _isConsumable = consumable;
        }

        public void SetValue(float value)
        {
            _value = Mathf.Max(0f, value);
        }

        public void SetEffect(string effectType, float duration = 0f)
        {
            _effectType = effectType ?? string.Empty;
            _duration = Mathf.Max(0f, duration);
        }

        // Query Methods
        public bool IsHealthResource()
        {
            return _resourceType.ToLower().Contains("health") || _effectType.ToLower().Contains("heal");
        }

        public bool IsAmmoResource()
        {
            return _resourceType.ToLower().Contains("ammo") || _resourceType.ToLower().Contains("bullet");
        }

        public bool IsPowerUp()
        {
            return _resourceType.ToLower().Contains("power") || _resourceType.ToLower().Contains("upgrade") || 
                   !string.IsNullOrEmpty(_effectType);
        }

        public Vector3 GetWorldPosition(Vector3 tilemapOffset)
        {
            return tilemapOffset + new Vector3(_position.x + 0.5f, _position.y + 0.5f, 0);
        }

        // Validation
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_resourceID) &&
                   !string.IsNullOrEmpty(_resourceType) &&
                   !string.IsNullOrEmpty(_prefabPath) &&
                   _roomID >= 0 &&
                   _quantity > 0;
        }

        // Clone
        public ResourceData Clone()
        {
            var clone = new ResourceData(_resourceID, _resourceType, _prefabPath, _roomID, _position, _quantity);
            clone._respawnTime = _respawnTime;
            clone._isConsumable = _isConsumable;
            clone._value = _value;
            clone._effectType = _effectType;
            clone._duration = _duration;
            return clone;
        }

        public override string ToString()
        {
            return $"Resource[{_resourceID}] {_resourceType} at {_position} (qty:{_quantity}, val:{_value})";
        }

        public override bool Equals(object obj)
        {
            if (obj is ResourceData other)
            {
                return _resourceID == other._resourceID && _position == other._position;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_resourceID, _position);
        }
    }

    /// <summary>
    /// Defines resource placement rules for different room types and difficulty levels.
    /// </summary>
    [Serializable]
    public class ResourcePlacementRule
    {
        [Header("Resource Type")]
        [SerializeField] private string _resourceType;
        [SerializeField] private string[] _allowedPrefabs;

        [Header("Placement Constraints")]
        [SerializeField] private RoomClassification[] _allowedRoomTypes;
        [SerializeField] private int _minDifficulty;
        [SerializeField] private int _maxDifficulty;
        [SerializeField] private float _spawnProbability;

        [Header("Quantity")]
        [SerializeField] private int _minQuantity;
        [SerializeField] private int _maxQuantity;
        [SerializeField] private float _quantityPerRoomArea;

        [Header("Positioning")]
        [SerializeField] private bool _avoidDoorways;
        [SerializeField] private int _minDistanceFromDoorways;
        [SerializeField] private bool _preferCorners;
        [SerializeField] private bool _preferCenter;

        [Header("Gameplay")]
        [SerializeField] private float _baseValue;
        [SerializeField] private float _valueVariance;
        [SerializeField] private bool _respawnAllowed;
        [SerializeField] private float _respawnTime;

        // Public Properties
        public string ResourceType => _resourceType;
        public string[] AllowedPrefabs => _allowedPrefabs;
        public RoomClassification[] AllowedRoomTypes => _allowedRoomTypes;
        public int MinDifficulty => _minDifficulty;
        public int MaxDifficulty => _maxDifficulty;
        public float SpawnProbability => _spawnProbability;
        public int MinQuantity => _minQuantity;
        public int MaxQuantity => _maxQuantity;
        public float QuantityPerRoomArea => _quantityPerRoomArea;
        public bool AvoidDoorways => _avoidDoorways;
        public int MinDistanceFromDoorways => _minDistanceFromDoorways;
        public bool PreferCorners => _preferCorners;
        public bool PreferCenter => _preferCenter;
        public float BaseValue => _baseValue;
        public float ValueVariance => _valueVariance;
        public bool RespawnAllowed => _respawnAllowed;
        public float RespawnTime => _respawnTime;

        // Constructor
        public ResourcePlacementRule(string resourceType)
        {
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _allowedPrefabs = new string[0];
            _allowedRoomTypes = new RoomClassification[0];
            _minDifficulty = 1;
            _maxDifficulty = 10;
            _spawnProbability = 0.3f;
            _minQuantity = 1;
            _maxQuantity = 3;
            _quantityPerRoomArea = 0.01f;
            _avoidDoorways = true;
            _minDistanceFromDoorways = 2;
            _preferCorners = false;
            _preferCenter = false;
            _baseValue = 1f;
            _valueVariance = 0.2f;
            _respawnAllowed = false;
            _respawnTime = 30f;
        }

        // Query Methods
        public bool IsValidForRoom(RoomData room, int difficulty)
        {
            if (difficulty < _minDifficulty || difficulty > _maxDifficulty)
                return false;

            if (_allowedRoomTypes.Length > 0)
            {
                bool roomTypeAllowed = false;
                foreach (var allowedType in _allowedRoomTypes)
                {
                    if (room.Classification == allowedType)
                    {
                        roomTypeAllowed = true;
                        break;
                    }
                }
                if (!roomTypeAllowed)
                    return false;
            }

            return true;
        }

        public int GetQuantityForRoom(RoomData room, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            int baseQuantity = random.Next(_minQuantity, _maxQuantity + 1);
            int areaBasedQuantity = Mathf.FloorToInt(room.Area * _quantityPerRoomArea);
            
            return Mathf.Max(baseQuantity, areaBasedQuantity);
        }

        public float GetValue(System.Random random = null)
        {
            random = random ?? new System.Random();
            float variance = (float)(random.NextDouble() * 2 - 1) * _valueVariance;
            return Mathf.Max(0.1f, _baseValue + _baseValue * variance);
        }
    }
}