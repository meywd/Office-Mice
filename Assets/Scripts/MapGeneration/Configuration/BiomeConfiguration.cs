using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Configuration for biome theming including tilesets, color palettes,
    /// environmental effects, and biome-specific generation rules.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeConfiguration", menuName = "Office Mice/Map Generation/Biome Configuration")]
    [Serializable]
    public class BiomeConfiguration : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _biomeID;
        [SerializeField] private string _biomeName;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private BiomeType _biomeType;
        
        [Header("Tileset Configuration")]
        [SerializeField] private TilesetConfiguration _primaryTileset;
        [SerializeField] private TilesetConfiguration _secondaryTileset;
        [SerializeField] private List<TilesetConfiguration> _decorativeTilesets = new List<TilesetConfiguration>();
        [SerializeField, Range(0f, 1f)] private float _secondaryTilesetChance = 0.3f;
        
        [Header("Color Palette")]
        [SerializeField] private BiomeColorPalette _colorPalette;
        [SerializeField] private bool _applyColorTinting = true;
        [SerializeField, Range(0f, 1f)] private float _colorVariation = 0.2f;
        
        [Header("Environmental Effects")]
        [SerializeField] private List<EnvironmentalEffect> _environmentalEffects = new List<EnvironmentalEffect>();
        [SerializeField] private float _ambientLightIntensity = 1f;
        [SerializeField] private Color _ambientLightColor = Color.white;
        
        [Header("Generation Rules")]
        [SerializeField] private BiomeGenerationRules _generationRules;
        [SerializeField] private List<BiomeModifier> _biomeModifiers = new List<BiomeModifier>();
        
        [Header("Audio Configuration")]
        [SerializeField] private AudioClip _ambientMusic;
        [SerializeField] private List<AudioClip> _ambientSounds = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _soundVolume = 0.3f;
        
        [Header("Resource Configuration")]
        [SerializeField] private List<BiomeResource> _commonResources = new List<BiomeResource>();
        [SerializeField] private List<BiomeResource> _rareResources = new List<BiomeResource>();
        [SerializeField, Range(0f, 1f)] private float _rareResourceChance = 0.1f;
        
        // Public Properties
        public string BiomeID => _biomeID;
        public string BiomeName => _biomeName;
        public string Description => _description;
        public BiomeType BiomeType => _biomeType;
        public TilesetConfiguration PrimaryTileset => _primaryTileset;
        public TilesetConfiguration SecondaryTileset => _secondaryTileset;
        public IReadOnlyList<TilesetConfiguration> DecorativeTilesets => _decorativeTilesets.AsReadOnly();
        public float SecondaryTilesetChance => _secondaryTilesetChance;
        public BiomeColorPalette ColorPalette => _colorPalette;
        public bool ApplyColorTinting => _applyColorTinting;
        public float ColorVariation => _colorVariation;
        public IReadOnlyList<EnvironmentalEffect> EnvironmentalEffects => _environmentalEffects.AsReadOnly();
        public float AmbientLightIntensity => _ambientLightIntensity;
        public Color AmbientLightColor => _ambientLightColor;
        public BiomeGenerationRules GenerationRules => _generationRules;
        public IReadOnlyList<BiomeModifier> BiomeModifiers => _biomeModifiers.AsReadOnly();
        public AudioClip AmbientMusic => _ambientMusic;
        public IReadOnlyList<AudioClip> AmbientSounds => _ambientSounds.AsReadOnly();
        public float MusicVolume => _musicVolume;
        public float SoundVolume => _soundVolume;
        public IReadOnlyList<BiomeResource> CommonResources => _commonResources.AsReadOnly();
        public IReadOnlyList<BiomeResource> RareResources => _rareResources.AsReadOnly();
        public float RareResourceChance => _rareResourceChance;
        
        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate identity
            if (string.IsNullOrEmpty(_biomeID))
                result.AddError("Biome ID is required");
            else if (_biomeID.Contains(" "))
                result.AddError("Biome ID cannot contain spaces");
                
            if (string.IsNullOrEmpty(_biomeName))
                result.AddWarning("Biome name is not set");
            
            // Validate tilesets
            if (_primaryTileset == null)
                result.AddError("Primary tileset is required");
            else
                result.Merge(_primaryTileset.Validate());
            
            if (_secondaryTileset != null)
                result.Merge(_secondaryTileset.Validate());
            
            foreach (var tileset in _decorativeTilesets)
            {
                if (tileset != null)
                    result.Merge(tileset.Validate());
            }
            
            // Validate color palette
            if (_colorPalette == null)
                result.AddWarning("No color palette assigned");
            else
                result.Merge(_colorPalette.Validate());
            
            // Validate environmental effects
            foreach (var effect in _environmentalEffects)
            {
                if (effect != null)
                    result.Merge(effect.Validate());
            }
            
            // Validate generation rules
            if (_generationRules == null)
                result.AddError("Generation rules are required");
            else
                result.Merge(_generationRules.Validate());
            
            // Validate biome modifiers
            foreach (var modifier in _biomeModifiers)
            {
                if (modifier != null)
                    result.Merge(modifier.Validate());
            }
            
            // Validate audio configuration
            if (_musicVolume < 0f || _musicVolume > 1f)
                result.AddError($"Invalid music volume: {_musicVolume}");
                
            if (_soundVolume < 0f || _soundVolume > 1f)
                result.AddError($"Invalid sound volume: {_soundVolume}");
            
            // Validate resources
            foreach (var resource in _commonResources)
            {
                if (resource != null)
                    result.Merge(resource.Validate());
            }
            
            foreach (var resource in _rareResources)
            {
                if (resource != null)
                    result.Merge(resource.Validate());
            }
            
            if (_rareResourceChance < 0f || _rareResourceChance > 1f)
                result.AddError($"Invalid rare resource chance: {_rareResourceChance}");
            
            return result;
        }
        
        // Utility Methods
        public TilesetConfiguration GetRandomTileset(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (random.NextDouble() < _secondaryTilesetChance && _secondaryTileset != null)
            {
                return _secondaryTileset;
            }
            
            return _primaryTileset;
        }
        
        public List<BiomeResource> GetAvailableResources(System.Random random = null)
        {
            random = random ?? new System.Random();
            var resources = new List<BiomeResource>(_commonResources);
            
            // Add rare resources based on chance
            if (random.NextDouble() < _rareResourceChance)
            {
                resources.AddRange(_rareResources);
            }
            
            return resources;
        }
        
        public Color GetRandomColorVariation(Color baseColor, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (!_applyColorTinting || _colorVariation <= 0f)
                return baseColor;
            
            float variation = (float)(random.NextDouble() * 2 - 1) * _colorVariation;
            return Color.Lerp(baseColor, Color.white, variation);
        }
        
        private void OnValidate()
        {
            // Auto-generate biome ID if empty
            if (string.IsNullOrEmpty(_biomeID) && !string.IsNullOrEmpty(_biomeName))
            {
                _biomeID = System.Text.RegularExpressions.Regex.Replace(_biomeName.ToLower(), @"[^a-z0-9]", "_");
            }
            
            // Clamp values to valid ranges
            _secondaryTilesetChance = Mathf.Clamp01(_secondaryTilesetChance);
            _colorVariation = Mathf.Clamp01(_colorVariation);
            _ambientLightIntensity = Mathf.Max(0f, _ambientLightIntensity);
            _musicVolume = Mathf.Clamp01(_musicVolume);
            _soundVolume = Mathf.Clamp01(_soundVolume);
            _rareResourceChance = Mathf.Clamp01(_rareResourceChance);
        }
    }
    
    /// <summary>
    /// Color palette configuration for biome theming.
    /// </summary>
    [Serializable]
    public class BiomeColorPalette
    {
        [SerializeField] private string _paletteName;
        [SerializeField] private Color _primaryColor = Color.white;
        [SerializeField] private Color _secondaryColor = Color.gray;
        [SerializeField] private Color _accentColor = Color.blue;
        [SerializeField] private Color _shadowColor = Color.black;
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private List<Color> _additionalColors = new List<Color>();
        
        public string PaletteName => _paletteName;
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public Color AccentColor => _accentColor;
        public Color ShadowColor => _shadowColor;
        public Color HighlightColor => _highlightColor;
        public IReadOnlyList<Color> AdditionalColors => _additionalColors.AsReadOnly();
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_paletteName))
                result.AddError("Palette name is required");
            
            return result;
        }
        
        public Color GetRandomColor(System.Random random = null)
        {
            random = random ?? new System.Random();
            var allColors = new List<Color> { _primaryColor, _secondaryColor, _accentColor };
            allColors.AddRange(_additionalColors);
            
            if (allColors.Count == 0)
                return Color.white;
            
            return allColors[random.Next(allColors.Count)];
        }
    }
    
    /// <summary>
    /// Environmental effect configuration for biomes.
    /// </summary>
    [Serializable]
    public class EnvironmentalEffect
    {
        [SerializeField] private string _effectName;
        [SerializeField] private EffectType _effectType;
        [SerializeField] private ParticleSystem _particlePrefab;
        [SerializeField] private float _intensity = 1f;
        [SerializeField] private Vector2 _frequencyRange = new Vector2(1f, 5f);
        [SerializeField] private bool _affectsLighting = false;
        
        public string EffectName => _effectName;
        public EffectType EffectType => _effectType;
        public ParticleSystem ParticlePrefab => _particlePrefab;
        public float Intensity => _intensity;
        public Vector2 FrequencyRange => _frequencyRange;
        public bool AffectsLighting => _affectsLighting;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_effectName))
                result.AddError("Effect name is required");
                
            if (_intensity < 0f)
                result.AddError($"Effect '{_effectName}' has negative intensity");
                
            if (_frequencyRange.x <= 0f || _frequencyRange.y <= 0f)
                result.AddError($"Effect '{_effectName}' has invalid frequency range");
                
            if (_frequencyRange.x > _frequencyRange.y)
                result.AddError($"Effect '{_effectName}' frequency range is inverted");
            
            return result;
        }
    }
    
    /// <summary>
    /// Generation rules specific to this biome.
    /// </summary>
    [Serializable]
    public class BiomeGenerationRules
    {
        [Header("Room Generation")]
        [SerializeField, Vector2IntRange(3, 50)] private Vector2Int _roomSizeRange = new Vector2Int(5, 15);
        [SerializeField, Range(1, 20)] private int _maxRooms = 10;
        [SerializeField, Range(0f, 1f)] private float _roomDensity = 0.5f;
        
        [Header("Corridor Generation")]
        [SerializeField, Min(1)] private int _minCorridorWidth = 1;
        [SerializeField, Min(1)] private int _maxCorridorWidth = 3;
        [SerializeField] private CorridorStyle _corridorStyle = CorridorStyle.Straight;
        
        [Header("Special Features")]
        [SerializeField, Range(0f, 1f)] private float _secretRoomChance = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _treasureRoomChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _hazardChance = 0.15f;
        
        public Vector2Int RoomSizeRange => _roomSizeRange;
        public int MaxRooms => _maxRooms;
        public float RoomDensity => _roomDensity;
        public int MinCorridorWidth => _minCorridorWidth;
        public int MaxCorridorWidth => _maxCorridorWidth;
        public CorridorStyle CorridorStyle => _corridorStyle;
        public float SecretRoomChance => _secretRoomChance;
        public float TreasureRoomChance => _treasureRoomChance;
        public float HazardChance => _hazardChance;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_roomSizeRange.x < 3 || _roomSizeRange.y < 3)
                result.AddError("Room size range minimum must be at least 3");
                
            if (_roomSizeRange.x > _roomSizeRange.y)
                result.AddError("Room size range is inverted");
                
            if (_maxRooms < 1)
                result.AddError("Max rooms must be at least 1");
                
            if (_minCorridorWidth > _maxCorridorWidth)
                result.AddError("Corridor width range is inverted");
            
            return result;
        }
    }
    
    /// <summary>
    /// Modifier that affects biome generation parameters.
    /// </summary>
    [Serializable]
    public class BiomeModifier
    {
        [SerializeField] private string _modifierName;
        [SerializeField] private ModifierType _modifierType;
        [SerializeField] private float _value = 1f;
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;
        [SerializeField] private bool _stackable = false;
        
        public string ModifierName => _modifierName;
        public ModifierType ModifierType => _modifierType;
        public float Value => _value;
        public float Chance => _chance;
        public bool Stackable => _stackable;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_modifierName))
                result.AddError("Modifier name is required");
                
            if (_chance < 0f || _chance > 1f)
                result.AddError($"Modifier '{_modifierName}' has invalid chance: {_chance}");
            
            return result;
        }
    }
    
    /// <summary>
    /// Resource configuration specific to biomes.
    /// </summary>
    [Serializable]
    public class BiomeResource
    {
        [SerializeField] private string _resourceType;
        [SerializeField] private GameObject _resourcePrefab;
        [SerializeField, Range(0f, 1f)] private float _spawnChance = 0.5f;
        [SerializeField] private Vector2Int _quantityRange = new Vector2Int(1, 3);
        [SerializeField] private bool _requiresSpecificTile = false;
        [SerializeField] private string _requiredTileType;
        
        public string ResourceType => _resourceType;
        public GameObject ResourcePrefab => _resourcePrefab;
        public float SpawnChance => _spawnChance;
        public Vector2Int QuantityRange => _quantityRange;
        public bool RequiresSpecificTile => _requiresSpecificTile;
        public string RequiredTileType => _requiredTileType;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_resourceType))
                result.AddError("Resource type is required");
                
            if (_resourcePrefab == null)
                result.AddError($"Resource '{_resourceType}' has no prefab assigned");
                
            if (_spawnChance < 0f || _spawnChance > 1f)
                result.AddError($"Resource '{_resourceType}' has invalid spawn chance: {_spawnChance}");
                
            if (_quantityRange.x <= 0 || _quantityRange.y <= 0)
                result.AddError($"Resource '{_resourceType}' has invalid quantity range");
                
            if (_quantityRange.x > _quantityRange.y)
                result.AddError($"Resource '{_resourceType}' quantity range is inverted");
            
            return result;
        }
    }
    
    // Enums
    public enum BiomeType
    {
        Office,
        ServerRoom,
        Cafeteria,
        Storage,
        Laboratory,
        Executive,
        Basement,
        Rooftop,
        Custom
    }
    
    public enum EffectType
    {
        Rain,
        Snow,
        Fog,
        Dust,
        Lightning,
        Wind,
        Steam,
        Radiation,
        Custom
    }
    
    public enum CorridorStyle
    {
        Straight,
        LShaped,
        Zigzag,
        Spiral,
        Random
    }
    
    public enum ModifierType
    {
        RoomSizeMultiplier,
        CorridorWidthMultiplier,
        EnemySpawnRate,
        ResourceSpawnRate,
        LightIntensity,
        MovementSpeed,
        DamageMultiplier,
        Custom
    }
    
    // Custom property attribute for Vector2Int range
    [AttributeUsage(AttributeTargets.Field)]
    public class Vector2IntRangeAttribute : PropertyAttribute
    {
        public int min;
        public int max;
        
        public Vector2IntRangeAttribute(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }
}