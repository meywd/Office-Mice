using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Configuration for tilesets including tile mappings, variations,
    /// rules, and asset references for map generation.
    /// </summary>
    [CreateAssetMenu(fileName = "TilesetConfiguration", menuName = "Office Mice/Map Generation/Tileset Configuration")]
    [Serializable]
    public class TilesetConfiguration : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _tilesetID;
        [SerializeField] private string _tilesetName;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private TilesetTheme _theme;
        
        [Header("Core Tiles")]
        [SerializeField] private TileMapping _floorTiles;
        [SerializeField] private TileMapping _wallTiles;
        [SerializeField] private TileMapping _ceilingTiles;
        [SerializeField] private TileMapping _doorTiles;
        [SerializeField] private TileMapping _windowTiles;
        
        [Header("Decorative Tiles")]
        [SerializeField] private List<TileMapping> _decorativeTiles = new List<TileMapping>();
        [SerializeField, Range(0f, 1f)] private float _decorationDensity = 0.2f;
        [SerializeField] private bool _randomizeDecorations = true;
        
        [Header("Special Tiles")]
        [SerializeField] private TileMapping _hazardTiles;
        [SerializeField] private TileMapping _interactiveTiles;
        [SerializeField] private TileMapping _spawnTiles;
        [SerializeField] private TileMapping _objectiveTiles;
        
        [Header("Tile Variations")]
        [SerializeField] private List<TileVariation> _tileVariations = new List<TileVariation>();
        [SerializeField, Range(0f, 1f)] private float _variationChance = 0.3f;
        [SerializeField] private bool _useVariationsForFloors = true;
        [SerializeField] private bool _useVariationsForWalls = true;
        
        [Header("Tile Rules")]
        [SerializeField] private List<TileRule> _tileRules = new List<TileRule>();
        [SerializeField] private bool _applyRulesAutomatically = true;
        
        [Header("Asset References")]
        [SerializeField] private TileBase _fallbackTile;
        [SerializeField] private Texture2D _tilesetTexture;
        [SerializeField] private Vector2Int _tileSize = new Vector2Int(16, 16);
        [SerializeField] private int _tilesPerRow = 16;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableTileColliders = true;
        [SerializeField] private PhysicsMaterial2D _tilePhysicsMaterial;
        [SerializeField] private bool _useTilemapCollider2D = true;
        
        // Public Properties
        public string TilesetID => _tilesetID;
        public string TilesetName => _tilesetName;
        public string Description => _description;
        public TilesetTheme Theme => _theme;
        public TileMapping FloorTiles => _floorTiles;
        public TileMapping WallTiles => _wallTiles;
        public TileMapping CeilingTiles => _ceilingTiles;
        public TileMapping DoorTiles => _doorTiles;
        public TileMapping WindowTiles => _windowTiles;
        public IReadOnlyList<TileMapping> DecorativeTiles => _decorativeTiles.AsReadOnly();
        public float DecorationDensity => _decorationDensity;
        public bool RandomizeDecorations => _randomizeDecorations;
        public TileMapping HazardTiles => _hazardTiles;
        public TileMapping InteractiveTiles => _interactiveTiles;
        public TileMapping SpawnTiles => _spawnTiles;
        public TileMapping ObjectiveTiles => _objectiveTiles;
        public IReadOnlyList<TileVariation> TileVariations => _tileVariations.AsReadOnly();
        public float VariationChance => _variationChance;
        public bool UseVariationsForFloors => _useVariationsForFloors;
        public bool UseVariationsForWalls => _useVariationsForWalls;
        public IReadOnlyList<TileRule> TileRules => _tileRules.AsReadOnly();
        public bool ApplyRulesAutomatically => _applyRulesAutomatically;
        public TileBase FallbackTile => _fallbackTile;
        public Texture2D TilesetTexture => _tilesetTexture;
        public Vector2Int TileSize => _tileSize;
        public int TilesPerRow => _tilesPerRow;
        public bool EnableTileColliders => _enableTileColliders;
        public PhysicsMaterial2D TilePhysicsMaterial => _tilePhysicsMaterial;
        public bool UseTilemapCollider2D => _useTilemapCollider2D;
        
        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate identity
            if (string.IsNullOrEmpty(_tilesetID))
                result.AddError("Tileset ID is required");
            else if (_tilesetID.Contains(" "))
                result.AddError("Tileset ID cannot contain spaces");
                
            if (string.IsNullOrEmpty(_tilesetName))
                result.AddWarning("Tileset name is not set");
            
            // Validate core tiles
            if (_floorTiles == null)
                result.AddError("Floor tiles mapping is required");
            else
                result.Merge(_floorTiles.Validate());
            
            if (_wallTiles == null)
                result.AddError("Wall tiles mapping is required");
            else
                result.Merge(_wallTiles.Validate());
            
            // Validate optional core tiles
            if (_ceilingTiles != null)
                result.Merge(_ceilingTiles.Validate());
            if (_doorTiles != null)
                result.Merge(_doorTiles.Validate());
            if (_windowTiles != null)
                result.Merge(_windowTiles.Validate());
            
            // Validate decorative tiles
            foreach (var tile in _decorativeTiles)
            {
                if (tile != null)
                    result.Merge(tile.Validate());
            }
            
            // Validate special tiles
            if (_hazardTiles != null)
                result.Merge(_hazardTiles.Validate());
            if (_interactiveTiles != null)
                result.Merge(_interactiveTiles.Validate());
            if (_spawnTiles != null)
                result.Merge(_spawnTiles.Validate());
            if (_objectiveTiles != null)
                result.Merge(_objectiveTiles.Validate());
            
            // Validate tile variations
            foreach (var variation in _tileVariations)
            {
                if (variation != null)
                    result.Merge(variation.Validate());
            }
            
            // Validate tile rules
            foreach (var rule in _tileRules)
            {
                if (rule != null)
                    result.Merge(rule.Validate());
            }
            
            // Validate numeric values
            if (_decorationDensity < 0f || _decorationDensity > 1f)
                result.AddError($"Invalid decoration density: {_decorationDensity}");
                
            if (_variationChance < 0f || _variationChance > 1f)
                result.AddError($"Invalid variation chance: {_variationChance}");
                
            if (_tileSize.x <= 0 || _tileSize.y <= 0)
                result.AddError($"Invalid tile size: {_tileSize}");
                
            if (_tilesPerRow <= 0)
                result.AddError($"Invalid tiles per row: {_tilesPerRow}");
            
            return result;
        }
        
        // Utility Methods
        public TileBase GetTileForType(TileType tileType, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            TileMapping mapping = null;
            switch (tileType)
            {
                case TileType.Floor:
                    mapping = _floorTiles;
                    break;
                case TileType.Wall:
                    mapping = _wallTiles;
                    break;
                case TileType.Ceiling:
                    mapping = _ceilingTiles;
                    break;
                case TileType.Door:
                    mapping = _doorTiles;
                    break;
                case TileType.Window:
                    mapping = _windowTiles;
                    break;
                case TileType.Hazard:
                    mapping = _hazardTiles;
                    break;
                case TileType.Interactive:
                    mapping = _interactiveTiles;
                    break;
                case TileType.Spawn:
                    mapping = _spawnTiles;
                    break;
                case TileType.Objective:
                    mapping = _objectiveTiles;
                    break;
            }
            
            if (mapping == null)
                return _fallbackTile;
            
            var baseTile = mapping.GetRandomTile(random);
            
            // Apply variations if enabled
            if (ShouldApplyVariation(tileType, random))
            {
                var variation = GetRandomVariation(tileType, random);
                if (variation != null)
                {
                    return variation.GetVariantTile(baseTile);
                }
            }
            
            return baseTile ?? _fallbackTile;
        }
        
        public TileBase GetDecorativeTile(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (_decorativeTiles.Count == 0)
                return null;
            
            var mapping = _decorativeTiles[random.Next(_decorativeTiles.Count)];
            return mapping.GetRandomTile(random);
        }
        
        public void ApplyTileRules(Tilemap tilemap, Vector3Int position, TileBase tile)
        {
            if (!_applyRulesAutomatically || _tileRules.Count == 0)
                return;
            
            foreach (var rule in _tileRules)
            {
                if (rule.ShouldApply(tilemap, position, tile))
                {
                    var newTile = rule.ApplyRule(tilemap, position, tile);
                    if (newTile != null)
                    {
                        tilemap.SetTile(position, newTile);
                    }
                }
            }
        }
        
        private bool ShouldApplyVariation(TileType tileType, System.Random random)
        {
            if (random.NextDouble() > _variationChance)
                return false;
            
            switch (tileType)
            {
                case TileType.Floor:
                    return _useVariationsForFloors;
                case TileType.Wall:
                    return _useVariationsForWalls;
                default:
                    return false;
            }
        }
        
        private TileVariation GetRandomVariation(TileType tileType, System.Random random)
        {
            var applicableVariations = _tileVariations.FindAll(v => v.ApplicableToType(tileType));
            if (applicableVariations.Count == 0)
                return null;
            
            return applicableVariations[random.Next(applicableVariations.Count)];
        }
        
        private void OnValidate()
        {
            // Auto-generate tileset ID if empty
            if (string.IsNullOrEmpty(_tilesetID) && !string.IsNullOrEmpty(_tilesetName))
            {
                _tilesetID = System.Text.RegularExpressions.Regex.Replace(_tilesetName.ToLower(), @"[^a-z0-9]", "_");
            }
            
            // Clamp values to valid ranges
            _decorationDensity = Mathf.Clamp01(_decorationDensity);
            _variationChance = Mathf.Clamp01(_variationChance);
            _tileSize = new Vector2Int(Mathf.Max(1, _tileSize.x), Mathf.Max(1, _tileSize.y));
            _tilesPerRow = Mathf.Max(1, _tilesPerRow);
        }
    }
    
    /// <summary>
    /// Maps tile types to actual tile assets with weights and variations.
    /// </summary>
    [Serializable]
    public class TileMapping
    {
        [SerializeField] private string _mappingName;
        [SerializeField] private List<TileEntry> _tiles = new List<TileEntry>();
        [SerializeField] private bool _useRandomSelection = true;
        [SerializeField] private int _defaultIndex = 0;
        
        public string MappingName => _mappingName;
        public List<TileEntry> Tiles => _tiles;
        public bool UseRandomSelection => _useRandomSelection;
        public int DefaultIndex => _defaultIndex;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_mappingName))
                result.AddError("Mapping name is required");
            
            if (_tiles.Count == 0)
                result.AddError($"Tile mapping '{_mappingName}' has no tiles");
            else
            {
                float totalWeight = _tiles.Sum(t => t.Weight);
                if (totalWeight <= 0f && _useRandomSelection)
                    result.AddError($"Tile mapping '{_mappingName}' has zero total weight");
                
                for (int i = 0; i < _tiles.Count; i++)
                {
                    var tileResult = _tiles[i].Validate();
                    if (!tileResult.IsValid)
                    {
                        result.AddError($"Tile mapping '{_mappingName}' entry {i}: {string.Join(", ", tileResult.Errors)}");
                    }
                }
            }
            
            if (_defaultIndex < 0 || _defaultIndex >= _tiles.Count)
                result.AddError($"Tile mapping '{_mappingName}' has invalid default index");
            
            return result;
        }
        
        public TileBase GetRandomTile(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (_tiles.Count == 0)
                return null;
            
            if (!_useRandomSelection)
            {
                return _defaultIndex >= 0 && _defaultIndex < _tiles.Count ? _tiles[_defaultIndex].Tile : null;
            }
            
            float totalWeight = _tiles.Sum(t => t.Weight);
            if (totalWeight <= 0f)
                return _tiles[0].Tile;
            
            float randomValue = (float)(random.NextDouble() * totalWeight);
            float currentWeight = 0f;
            
            foreach (var tileEntry in _tiles)
            {
                currentWeight += tileEntry.Weight;
                if (randomValue <= currentWeight)
                    return tileEntry.Tile;
            }
            
            return _tiles[_tiles.Count - 1].Tile;
        }
    }
    
    /// <summary>
    /// Individual tile entry with weight and properties.
    /// </summary>
    [Serializable]
    public class TileEntry
    {
        [SerializeField] private TileBase _tile;
        [SerializeField, Range(0f, 1f)] private float _weight = 1f;
        [SerializeField] private bool _isWalkable = true;
        [SerializeField] private bool _hasCollision = false;
        
        public TileBase Tile => _tile;
        public float Weight => _weight;
        public bool IsWalkable => _isWalkable;
        public bool HasCollision => _hasCollision;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_tile == null)
                result.AddError("Tile entry has no tile assigned");
                
            if (_weight < 0f)
                result.AddError("Tile entry has negative weight");
            
            return result;
        }
    }
    
    /// <summary>
    /// Defines variations that can be applied to tiles.
    /// </summary>
    [Serializable]
    public class TileVariation
    {
        [SerializeField] private string _variationName;
        [SerializeField] private List<TileType> _applicableTypes = new List<TileType>();
        [SerializeField] private VariationType _variationType;
        [SerializeField, Range(0f, 1f)] private float _intensity = 0.5f;
        [SerializeField] private Color _tintColor = Color.white;
        [SerializeField] private Texture2D _overlayTexture;
        [SerializeField] private TileBase _replacementTile;
        
        public string VariationName => _variationName;
        public List<TileType> ApplicableTypes => _applicableTypes;
        public VariationType VariationType => _variationType;
        public float Intensity => _intensity;
        public Color TintColor => _tintColor;
        public Texture2D OverlayTexture => _overlayTexture;
        public TileBase ReplacementTile => _replacementTile;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_variationName))
                result.AddError("Variation name is required");
                
            if (_applicableTypes.Count == 0)
                result.AddError($"Variation '{_variationName}' has no applicable types");
                
            if (_intensity < 0f || _intensity > 1f)
                result.AddError($"Variation '{_variationName}' has invalid intensity");
            
            return result;
        }
        
        public bool ApplicableToType(TileType tileType)
        {
            return _applicableTypes.Contains(tileType);
        }
        
        public TileBase GetVariantTile(TileBase originalTile)
        {
            switch (_variationType)
            {
                case VariationType.Replacement:
                    return _replacementTile;
                case VariationType.Tint:
                    // In a real implementation, you would create a tinted version of the tile
                    return originalTile;
                case VariationType.Overlay:
                    // In a real implementation, you would apply an overlay to the tile
                    return originalTile;
                default:
                    return originalTile;
            }
        }
    }
    
    /// <summary>
    /// Rule for modifying tile placement based on conditions.
    /// </summary>
    [Serializable]
    public class TileRule
    {
        [SerializeField] private string _ruleName;
        [SerializeField] private TileRuleCondition _condition;
        [SerializeField] private TileRuleAction _action;
        [SerializeField] private TileBase _replacementTile;
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;
        
        public string RuleName => _ruleName;
        public TileRuleCondition Condition => _condition;
        public TileRuleAction Action => _action;
        public TileBase ReplacementTile => _replacementTile;
        public float Chance => _chance;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_ruleName))
                result.AddError("Rule name is required");
                
            if (_chance < 0f || _chance > 1f)
                result.AddError($"Rule '{_ruleName}' has invalid chance");
                
            if (_action == TileRuleAction.Replace && _replacementTile == null)
                result.AddError($"Rule '{_ruleName}' requires replacement tile for replace action");
            
            return result;
        }
        
        public bool ShouldApply(Tilemap tilemap, Vector3Int position, TileBase tile)
        {
            if (UnityEngine.Random.value > _chance)
                return false;
            
            switch (_condition)
            {
                case TileRuleCondition.Always:
                    return true;
                case TileRuleCondition.Edge:
                    return IsEdgeTile(tilemap, position);
                case TileRuleCondition.Corner:
                    return IsCornerTile(tilemap, position);
                case TileRuleCondition.Isolated:
                    return IsIsolatedTile(tilemap, position);
                case TileRuleCondition.Surrounded:
                    return IsSurroundedTile(tilemap, position);
                default:
                    return false;
            }
        }
        
        public TileBase ApplyRule(Tilemap tilemap, Vector3Int position, TileBase tile)
        {
            switch (_action)
            {
                case TileRuleAction.Replace:
                    return _replacementTile;
                case TileRuleAction.Remove:
                    return null;
                case TileRuleAction.Keep:
                    return tile;
                default:
                    return tile;
            }
        }
        
        private bool IsEdgeTile(Tilemap tilemap, Vector3Int position)
        {
            // Check if tile is on the edge of a tilemap region
            var neighbors = GetNeighborTiles(tilemap, position);
            return neighbors.Count < 4;
        }
        
        private bool IsCornerTile(Tilemap tilemap, Vector3Int position)
        {
            // Check if tile is at a corner
            var neighbors = GetNeighborTiles(tilemap, position);
            return neighbors.Count == 2;
        }
        
        private bool IsIsolatedTile(Tilemap tilemap, Vector3Int position)
        {
            // Check if tile has no neighbors of the same type
            var neighbors = GetNeighborTiles(tilemap, position);
            return neighbors.Count == 0;
        }
        
        private bool IsSurroundedTile(Tilemap tilemap, Vector3Int position)
        {
            // Check if tile is completely surrounded
            var neighbors = GetNeighborTiles(tilemap, position);
            return neighbors.Count == 4;
        }
        
        private List<TileBase> GetNeighborTiles(Tilemap tilemap, Vector3Int position)
        {
            var neighbors = new List<TileBase>();
            var directions = new Vector3Int[]
            {
                Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
            };
            
            foreach (var dir in directions)
            {
                var neighborPos = position + dir;
                var neighborTile = tilemap.GetTile(neighborPos);
                if (neighborTile != null)
                    neighbors.Add(neighborTile);
            }
            
            return neighbors;
        }
    }
    
    // Enums
    public enum TilesetTheme
    {
        Office,
        Industrial,
        Laboratory,
        ServerRoom,
        Cafeteria,
        Storage,
        Executive,
        Basement,
        Custom
    }
    
    public enum TileType
    {
        Floor,
        Wall,
        Ceiling,
        Door,
        Window,
        Hazard,
        Interactive,
        Spawn,
        Objective,
        Decoration
    }
    
    public enum VariationType
    {
        Replacement,
        Tint,
        Overlay,
        Rotation,
        Scale
    }
    
    public enum TileRuleCondition
    {
        Always,
        Edge,
        Corner,
        Isolated,
        Surrounded,
        Random
    }
    
    public enum TileRuleAction
    {
        Replace,
        Remove,
        Keep,
        Modify
    }
}