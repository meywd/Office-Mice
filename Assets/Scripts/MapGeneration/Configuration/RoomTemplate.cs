using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Configuration template for room generation including tile patterns, furniture placement,
    /// and spawn point configurations. Used by designers to create reusable room layouts.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "Office Mice/Map Generation/Room Template")]
    [Serializable]
    public class RoomTemplate : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _templateID;
        [SerializeField] private string _templateName;
        [SerializeField, TextArea(3, 5)] private string _description;
        
        [Header("Room Requirements")]
        [SerializeField, Min(3)] private int _minWidth = 3;
        [SerializeField, Min(3)] private int _minHeight = 3;
        [SerializeField, Min(3)] private int _maxWidth = 20;
        [SerializeField, Min(3)] private int _maxHeight = 20;
        [SerializeField] private RoomClassification _requiredClassification = RoomClassification.Unassigned;
        
        [Header("Tile Configuration")]
        [SerializeField] private TilePattern _floorPattern;
        [SerializeField] private TilePattern _wallPattern;
        [SerializeField] private List<TilePattern> _decorativePatterns = new List<TilePattern>();
        
        [Header("Furniture Configuration")]
        [SerializeField] private List<FurniturePlacement> _furniturePlacements = new List<FurniturePlacement>();
        [SerializeField, Range(0f, 1f)] private float _furnitureDensity = 0.3f;
        [SerializeField] private bool _ensurePathing = true;
        
        [Header("Spawn Configuration")]
        [SerializeField] private List<EnemySpawnPoint> _enemySpawnPoints = new List<EnemySpawnPoint>();
        [SerializeField] private List<ResourceSpawnPoint> _resourceSpawnPoints = new List<ResourceSpawnPoint>();
        [SerializeField] private bool _autoPlacePlayerSpawn = true;
        
        [Header("Doorway Configuration")]
        [SerializeField] private List<DoorwayTemplate> _preferredDoorways = new List<DoorwayTemplate>();
        [SerializeField, Min(1)] private int _minDoorways = 1;
        [SerializeField, Min(1)] private int _maxDoorways = 4;
        
        // Public Properties
        public string TemplateID => _templateID;
        public string TemplateName => _templateName;
        public string Description => _description;
        public int MinWidth => _minWidth;
        public int MinHeight => _minHeight;
        public int MaxWidth => _maxWidth;
        public int MaxHeight => _maxHeight;
        public RoomClassification RequiredClassification => _requiredClassification;
        public TilePattern FloorPattern => _floorPattern;
        public TilePattern WallPattern => _wallPattern;
        public IReadOnlyList<TilePattern> DecorativePatterns => _decorativePatterns.AsReadOnly();
        public IReadOnlyList<FurniturePlacement> FurniturePlacements => _furniturePlacements.AsReadOnly();
        public float FurnitureDensity => _furnitureDensity;
        public bool EnsurePathing => _ensurePathing;
        public IReadOnlyList<EnemySpawnPoint> EnemySpawnPoints => _enemySpawnPoints.AsReadOnly();
        public IReadOnlyList<ResourceSpawnPoint> ResourceSpawnPoints => _resourceSpawnPoints.AsReadOnly();
        public bool AutoPlacePlayerSpawn => _autoPlacePlayerSpawn;
        public IReadOnlyList<DoorwayTemplate> PreferredDoorways => _preferredDoorways.AsReadOnly();
        public int MinDoorways => _minDoorways;
        public int MaxDoorways => _maxDoorways;
        
        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate identity
            if (string.IsNullOrEmpty(_templateID))
                result.AddError("Template ID is required");
            else if (_templateID.Contains(" "))
                result.AddError("Template ID cannot contain spaces");
                
            if (string.IsNullOrEmpty(_templateName))
                result.AddWarning("Template name is not set");
            
            // Validate size constraints
            if (_minWidth > _maxWidth)
                result.AddError($"Min width ({_minWidth}) cannot be greater than max width ({_maxWidth})");
                
            if (_minHeight > _maxHeight)
                result.AddError($"Min height ({_minHeight}) cannot be greater than max height ({_maxHeight})");
                
            if (_minWidth < 3 || _minHeight < 3)
                result.AddError("Minimum room dimensions must be at least 3x3");
                
            if (_maxWidth > 50 || _maxHeight > 50)
                result.AddWarning("Large room dimensions may impact performance");
            
            // Validate tile patterns
            if (_floorPattern == null)
                result.AddError("Floor pattern is required");
            else
                result.Merge(_floorPattern.Validate());
                
            if (_wallPattern == null)
                result.AddError("Wall pattern is required");
            else
                result.Merge(_wallPattern.Validate());
            
            // Validate decorative patterns
            foreach (var pattern in _decorativePatterns)
            {
                if (pattern != null)
                    result.Merge(pattern.Validate());
            }
            
            // Validate furniture placements
            if (_furnitureDensity < 0f || _furnitureDensity > 1f)
                result.AddError("Furniture density must be between 0 and 1");
                
            foreach (var furniture in _furniturePlacements)
            {
                if (furniture != null)
                    result.Merge(furniture.Validate());
            }
            
            // Validate spawn points
            foreach (var spawn in _enemySpawnPoints)
            {
                if (spawn != null)
                    result.Merge(spawn.Validate());
            }
            
            foreach (var spawn in _resourceSpawnPoints)
            {
                if (spawn != null)
                    result.Merge(spawn.Validate());
            }
            
            // Validate doorway configuration
            if (_minDoorways > _maxDoorways)
                result.AddError($"Min doorways ({_minDoorways}) cannot be greater than max doorways ({_maxDoorways})");
                
            if (_minDoorways < 1)
                result.AddError("Room must have at least 1 doorway");
                
            foreach (var doorway in _preferredDoorways)
            {
                if (doorway != null)
                    result.Merge(doorway.Validate());
            }
            
            return result;
        }
        
        // Utility Methods
        public bool CanFitInRoom(int width, int height)
        {
            return width >= _minWidth && width <= _maxWidth &&
                   height >= _minHeight && height <= _maxHeight;
        }
        
        public bool IsCompatibleWithClassification(RoomClassification classification)
        {
            return _requiredClassification == RoomClassification.Unassigned ||
                   _requiredClassification == classification;
        }
        
        public List<Vector2Int> GetValidSpawnPositions(int roomWidth, int roomHeight)
        {
            var validPositions = new List<Vector2Int>();
            
            // Add predefined spawn points
            foreach (var spawn in _enemySpawnPoints)
            {
                if (spawn.IsRelative && spawn.Position.x < roomWidth && spawn.Position.y < roomHeight)
                {
                    validPositions.Add(spawn.Position);
                }
            }
            
            return validPositions;
        }
        
        private void OnValidate()
        {
            // Auto-generate template ID if empty
            if (string.IsNullOrEmpty(_templateID) && !string.IsNullOrEmpty(_templateName))
            {
                _templateID = System.Text.RegularExpressions.Regex.Replace(_templateName.ToLower(), @"[^a-z0-9]", "_");
            }
            
            // Clamp values to valid ranges
            _minWidth = Mathf.Max(3, _minWidth);
            _minHeight = Mathf.Max(3, _minHeight);
            _maxWidth = Mathf.Max(_minWidth, _maxWidth);
            _maxHeight = Mathf.Max(_minHeight, _maxHeight);
            _furnitureDensity = Mathf.Clamp01(_furnitureDensity);
            _minDoorways = Mathf.Max(1, _minDoorways);
            _maxDoorways = Mathf.Max(_minDoorways, _maxDoorways);
        }
    }
    
    /// <summary>
    /// Defines a tile pattern with specific tile types and placement rules.
    /// </summary>
    [Serializable]
    public class TilePattern
    {
        [SerializeField] private string _patternName;
        [SerializeField] private TileBase _tile;
        [SerializeField] private TilePatternType _patternType;
        [SerializeField, Range(0f, 1f)] private float _probability = 1f;
        [SerializeField] private Vector2Int _offset;
        [SerializeField] private bool _allowRotation;
        
        public string PatternName => _patternName;
        public TileBase Tile => _tile;
        public TilePatternType PatternType => _patternType;
        public float Probability => _probability;
        public Vector2Int Offset => _offset;
        public bool AllowRotation => _allowRotation;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_patternName))
                result.AddError("Pattern name is required");
                
            if (_tile == null)
                result.AddError($"Tile pattern '{_patternName}' has no tile assigned");
                
            if (_probability < 0f || _probability > 1f)
                result.AddError($"Pattern '{_patternName}' has invalid probability: {_probability}");
            
            return result;
        }
    }
    
    /// <summary>
    /// Defines furniture placement with position, type, and constraints.
    /// </summary>
    [Serializable]
    public class FurniturePlacement
    {
        [SerializeField] private string _furnitureType;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private Vector2Int _size = Vector2Int.one;
        [SerializeField] private bool _isRelative = true;
        [SerializeField] private bool _blocksPathing = true;
        [SerializeField, Range(0f, 1f)] private float _spawnProbability = 1f;
        
        public string FurnitureType => _furnitureType;
        public Vector2Int Position => _position;
        public Vector2Int Size => _size;
        public bool IsRelative => _isRelative;
        public bool BlocksPathing => _blocksPathing;
        public float SpawnProbability => _spawnProbability;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_furnitureType))
                result.AddError("Furniture type is required");
                
            if (_size.x <= 0 || _size.y <= 0)
                result.AddError($"Furniture '{_furnitureType}' has invalid size: {_size}");
                
            if (_spawnProbability < 0f || _spawnProbability > 1f)
                result.AddError($"Furniture '{_furnitureType}' has invalid spawn probability: {_spawnProbability}");
            
            return result;
        }
    }
    
    /// <summary>
    /// Defines enemy spawn point configuration.
    /// </summary>
    [Serializable]
    public class EnemySpawnPoint
    {
        [SerializeField] private string _enemyType;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private bool _isRelative = true;
        [SerializeField] private float _spawnDelay = 0f;
        [SerializeField, Range(0f, 1f)] private float _spawnProbability = 1f;
        
        public string EnemyType => _enemyType;
        public Vector2Int Position => _position;
        public bool IsRelative => _isRelative;
        public float SpawnDelay => _spawnDelay;
        public float SpawnProbability => _spawnProbability;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_enemyType))
                result.AddError("Enemy type is required");
                
            if (_spawnDelay < 0f)
                result.AddError($"Spawn point for '{_enemyType}' has negative delay: {_spawnDelay}");
                
            if (_spawnProbability < 0f || _spawnProbability > 1f)
                result.AddError($"Spawn point for '{_enemyType}' has invalid probability: {_spawnProbability}");
            
            return result;
        }
    }
    
    /// <summary>
    /// Defines resource spawn point configuration.
    /// </summary>
    [Serializable]
    public class ResourceSpawnPoint
    {
        [SerializeField] private string _resourceType;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private bool _isRelative = true;
        [SerializeField] private int _quantity = 1;
        [SerializeField, Range(0f, 1f)] private float _spawnProbability = 1f;
        
        public string ResourceType => _resourceType;
        public Vector2Int Position => _position;
        public bool IsRelative => _isRelative;
        public int Quantity => _quantity;
        public float SpawnProbability => _spawnProbability;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_resourceType))
                result.AddError("Resource type is required");
                
            if (_quantity <= 0)
                result.AddError($"Resource '{_resourceType}' has invalid quantity: {_quantity}");
                
            if (_spawnProbability < 0f || _spawnProbability > 1f)
                result.AddError($"Resource '{_resourceType}' has invalid spawn probability: {_spawnProbability}");
            
            return result;
        }
    }
    
    /// <summary>
    /// Defines preferred doorway placement for the room template.
    /// </summary>
    [Serializable]
    public class DoorwayTemplate
    {
        [SerializeField] private Vector2Int _position;
        [SerializeField] private bool _isRelative = true;
        [SerializeField] private DoorOrientation _orientation;
        [SerializeField, Range(1, 3)] private int _width = 1;
        [SerializeField, Range(0f, 1f)] private float _priority = 1f;
        
        public Vector2Int Position => _position;
        public bool IsRelative => _isRelative;
        public DoorOrientation Orientation => _orientation;
        public int Width => _width;
        public float Priority => _priority;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_width < 1 || _width > 3)
                result.AddError($"Doorway has invalid width: {_width}");
                
            if (_priority < 0f || _priority > 1f)
                result.AddError($"Doorway has invalid priority: {_priority}");
            
            return result;
        }
    }
    
    // Enums
    public enum TilePatternType
    {
        Solid,          // Fill entire area
        Checkerboard,   // Alternating pattern
        Random,         // Random placement based on probability
        Border,         // Only on edges
        Diagonal,       // Diagonal lines
        Custom          // Custom algorithm
    }
    
    public enum DoorOrientation
    {
        Horizontal,
        Vertical,
        Automatic
    }
}