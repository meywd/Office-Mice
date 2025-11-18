using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Master configuration for map generation including all parameters,
    /// references to other configurations, and generation rules.
    /// </summary>
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "Office Mice/Map Generation/Map Generation Settings")]
    [Serializable]
    public class MapGenerationSettings : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _settingsID;
        [SerializeField] private string _settingsName;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private GenerationProfile _profile;
        
        [Header("Map Configuration")]
        [SerializeField] private MapConfiguration _mapConfig;
        [SerializeField] private BSPConfiguration _bspConfig;
        [SerializeField] private CorridorConfiguration _corridorConfig;
        
        [Header("Content Configuration")]
        [SerializeField] private List<RoomTemplate> _roomTemplates = new List<RoomTemplate>();
        [SerializeField] private List<BiomeConfiguration> _biomeConfigurations = new List<BiomeConfiguration>();
        [SerializeField] private List<SpawnTableConfiguration> _spawnTables = new List<SpawnTableConfiguration>();
        [SerializeField] private List<TilesetConfiguration> _tilesets = new List<TilesetConfiguration>();
        
        [Header("Room Classification")]
        [SerializeField] private RoomClassificationSettings _classificationSettings;
        
        [Header("Generation Rules")]
        [SerializeField] private GenerationRules _generationRules;
        [SerializeField] private ValidationRules _validationRules;
        [SerializeField] private PerformanceSettings _performanceSettings;
        
        [Header("Runtime Settings")]
        [SerializeField] private RuntimeConfiguration _runtimeConfig;
        [SerializeField] private DebugSettings _debugSettings;
        
        [Header("Quality Settings")]
        [SerializeField] private QualitySettings _qualitySettings;
        [SerializeField] private bool _allowRuntimeModification = false;
        
        // Public Properties
        public string SettingsID => _settingsID;
        public string SettingsName => _settingsName;
        public string Description => _description;
        public GenerationProfile Profile => _profile;
        public MapConfiguration MapConfig => _mapConfig;
        public BSPConfiguration BSPConfig => _bspConfig;
        public CorridorConfiguration CorridorConfig => _corridorConfig;
        public IReadOnlyList<RoomTemplate> RoomTemplates => _roomTemplates.AsReadOnly();
        public IReadOnlyList<BiomeConfiguration> BiomeConfigurations => _biomeConfigurations.AsReadOnly();
        public IReadOnlyList<SpawnTableConfiguration> SpawnTables => _spawnTables.AsReadOnly();
        public IReadOnlyList<TilesetConfiguration> Tilesets => _tilesets.AsReadOnly();
        public RoomClassificationSettings ClassificationSettings => _classificationSettings;
        public GenerationRules GenerationRules => _generationRules;
        public ValidationRules ValidationRules => _validationRules;
        public PerformanceSettings PerformanceSettings => _performanceSettings;
        public RuntimeConfiguration RuntimeConfig => _runtimeConfig;
        public DebugSettings DebugSettings => _debugSettings;
        public QualitySettings QualitySettings => _qualitySettings;
        public bool AllowRuntimeModification => _allowRuntimeModification;

        // Helper method for map bounds
        public RectInt GetMapBounds()
        {
            var size = _mapConfig.UseFixedSize ? _mapConfig.FixedSize : _mapConfig.MapSizeRange;
            return new RectInt(0, 0, size.x, size.y);
        }

        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate identity
            if (string.IsNullOrEmpty(_settingsID))
                result.AddError("Settings ID is required");
            else if (_settingsID.Contains(" "))
                result.AddError("Settings ID cannot contain spaces");
                
            if (string.IsNullOrEmpty(_settingsName))
                result.AddWarning("Settings name is not set");
            
            // Validate core configurations
            if (_mapConfig == null)
                result.AddError("Map configuration is required");
            else
                result.Merge(_mapConfig.Validate());
            
            if (_bspConfig == null)
                result.AddError("BSP configuration is required");
            else
                result.Merge(_bspConfig.Validate());
            
            if (_corridorConfig == null)
                result.AddError("Corridor configuration is required");
            else
                result.Merge(_corridorConfig.Validate());
            
            // Validate content configurations
            if (_roomTemplates.Count == 0)
                result.AddError("At least one room template is required");
            else
            {
                foreach (var template in _roomTemplates)
                {
                    if (template != null)
                        result.Merge(template.Validate());
                }
            }
            
            if (_biomeConfigurations.Count == 0)
                result.AddWarning("No biome configurations assigned");
            else
            {
                foreach (var biome in _biomeConfigurations)
                {
                    if (biome != null)
                        result.Merge(biome.Validate());
                }
            }
            
            if (_spawnTables.Count == 0)
                result.AddWarning("No spawn tables assigned");
            else
            {
                foreach (var spawnTable in _spawnTables)
                {
                    if (spawnTable != null)
                        result.Merge(spawnTable.Validate());
                }
            }
            
            if (_tilesets.Count == 0)
                result.AddError("At least one tileset configuration is required");
            else
            {
                foreach (var tileset in _tilesets)
                {
                    if (tileset != null)
                        result.Merge(tileset.Validate());
                }
            }
            
            // Validate classification settings
            if (_classificationSettings == null)
                result.AddWarning("No classification settings configured - will use default room classification");
            else
                result.Merge(_classificationSettings.Validate());
            
            // Validate rules and settings
            if (_generationRules == null)
                result.AddError("Generation rules are required");
            else
                result.Merge(_generationRules.Validate());
            
            if (_validationRules == null)
                result.AddError("Validation rules are required");
            else
                result.Merge(_validationRules.Validate());
            
            if (_performanceSettings == null)
                result.AddError("Performance settings are required");
            else
                result.Merge(_performanceSettings.Validate());
            
            if (_runtimeConfig == null)
                result.AddError("Runtime configuration is required");
            else
                result.Merge(_runtimeConfig.Validate());
            
            if (_debugSettings == null)
                result.AddError("Debug settings are required");
            else
                result.Merge(_debugSettings.Validate());
            
            if (_qualitySettings == null)
                result.AddError("Quality settings are required");
            else
                result.Merge(_qualitySettings.Validate());
            
            return result;
        }
        
        // Utility Methods
        public RoomTemplate GetRandomRoomTemplate(RoomClassification classification, System.Random random = null)
        {
            random = random ?? new System.Random();
            
            var compatibleTemplates = _roomTemplates.FindAll(t => t.IsCompatibleWithClassification(classification));
            if (compatibleTemplates.Count == 0)
                return _roomTemplates.Count > 0 ? _roomTemplates[random.Next(_roomTemplates.Count)] : null;
            
            return compatibleTemplates[random.Next(compatibleTemplates.Count)];
        }
        
        public BiomeConfiguration GetRandomBiome(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (_biomeConfigurations.Count == 0)
                return null;
            
            return _biomeConfigurations[random.Next(_biomeConfigurations.Count)];
        }
        
        public SpawnTableConfiguration GetSpawnTable(string tableID)
        {
            return _spawnTables.Find(t => t.SpawnTableID == tableID);
        }
        
        public TilesetConfiguration GetTileset(string tilesetID)
        {
            return _tilesets.Find(t => t.TilesetID == tilesetID);
        }
        
        public bool IsValidForProfile(GenerationProfile profile)
        {
            return _profile == profile || _profile == GenerationProfile.Any;
        }
        
        private void OnValidate()
        {
            // Auto-generate settings ID if empty
            if (string.IsNullOrEmpty(_settingsID) && !string.IsNullOrEmpty(_settingsName))
            {
                _settingsID = System.Text.RegularExpressions.Regex.Replace(_settingsName.ToLower(), @"[^a-z0-9]", "_");
            }
        }
    }
    
    /// <summary>
    /// Core map configuration parameters.
    /// </summary>
    [Serializable]
    public class MapConfiguration
    {
        [Header("Size Settings")]
        [SerializeField, Vector2IntRange(10, 200)] private Vector2Int _mapSizeRange = new Vector2Int(50, 100);
        [SerializeField] private bool _useFixedSize = false;
        [SerializeField] private Vector2Int _fixedSize = new Vector2Int(75, 75);
        
        [Header("Room Settings")]
        [SerializeField, Vector2IntRange(3, 50)] private Vector2Int _roomSizeRange = new Vector2Int(5, 15);
        [SerializeField, Min(2)] private int _minRooms = 5;
        [SerializeField, Min(2)] private int _maxRooms = 20;
        [SerializeField, Range(0.1f, 1f)] private float _roomDensity = 0.6f;
        
        [Header("Seed Settings")]
        [SerializeField] private bool _useRandomSeed = true;
        [SerializeField] private int _fixedSeed = 12345;
        [SerializeField] private bool _saveSeed = true;
        
        public Vector2Int MapSizeRange { get => _mapSizeRange; set => _mapSizeRange = value; }
        public bool UseFixedSize { get => _useFixedSize; set => _useFixedSize = value; }
        public Vector2Int FixedSize { get => _fixedSize; set => _fixedSize = value; }
        public Vector2Int RoomSizeRange { get => _roomSizeRange; set => _roomSizeRange = value; }
        public int MinRooms { get => _minRooms; set => _minRooms = value; }
        public int MaxRooms { get => _maxRooms; set => _maxRooms = value; }
        public float RoomDensity { get => _roomDensity; set => _roomDensity = value; }
        public bool UseRandomSeed { get => _useRandomSeed; set => _useRandomSeed = value; }
        public int FixedSeed { get => _fixedSeed; set => _fixedSeed = value; }
        public bool SaveSeed { get => _saveSeed; set => _saveSeed = value; }
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_mapSizeRange.x < 20 || _mapSizeRange.y < 20)
                result.AddError("Map size range minimum must be at least 20x20");
                
            if (_mapSizeRange.x > _mapSizeRange.y)
                result.AddError("Map size range is inverted");
                
            if (_fixedSize.x < 20 || _fixedSize.y < 20)
                result.AddError("Fixed map size must be at least 20x20");
                
            if (_roomSizeRange.x < 3 || _roomSizeRange.y < 3)
                result.AddError("Room size range minimum must be at least 3x3");
                
            if (_roomSizeRange.x > _roomSizeRange.y)
                result.AddError("Room size range is inverted");
                
            if (_minRooms > _maxRooms)
                result.AddError("Min rooms cannot be greater than max rooms");
                
            if (_roomDensity <= 0f || _roomDensity > 1f)
                result.AddError("Room density must be between 0 and 1");
            
            return result;
        }
        
        public Vector2Int GetMapSize(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (_useFixedSize)
                return _fixedSize;
            
            return new Vector2Int(
                random.Next(_mapSizeRange.x, _mapSizeRange.y + 1),
                random.Next(_mapSizeRange.x, _mapSizeRange.y + 1)
            );
        }
        
        public int GetSeed(System.Random random = null)
        {
            if (_useRandomSeed)
            {
                random = random ?? new System.Random();
                return random.Next();
            }
            
            return _fixedSeed;
        }
    }
    
    /// <summary>
    /// BSP tree generation configuration.
    /// </summary>
    [Serializable]
    public class BSPConfiguration
    {
        [Header("Splitting Rules")]
        [SerializeField, Min(2)] private int _minPartitionSize = 10;
        [SerializeField, Range(0.1f, 0.9f)] private float _splitPositionVariation = 0.3f;
        [SerializeField] private bool _allowHorizontalSplits = true;
        [SerializeField] private bool _allowVerticalSplits = true;
        [SerializeField] private SplitPreference _splitPreference = SplitPreference.Alternate;
        
        [Header("Tree Settings")]
        [SerializeField, Min(1)] private int _maxDepth = 10;
        [SerializeField, Range(0f, 1f)] private float _stopSplittingChance = 0.1f;
        [SerializeField] private bool _balanceTree = true;
        
        [Header("Room Generation")]
        [SerializeField, Range(0.5f, 0.95f)] private float _roomSizeRatio = 0.8f;
        [SerializeField, Range(0f, 0.5f)] private float _roomPositionVariation = 0.1f;
        [SerializeField] private bool _centerRooms = false;
        
        public int MinPartitionSize => _minPartitionSize;
        public float SplitPositionVariation => _splitPositionVariation;
        public bool AllowHorizontalSplits => _allowHorizontalSplits;
        public bool AllowVerticalSplits => _allowVerticalSplits;
        public SplitPreference SplitPreference => _splitPreference;
        public int MaxDepth => _maxDepth;
        public float StopSplittingChance => _stopSplittingChance;
        public bool BalanceTree => _balanceTree;
        public float RoomSizeRatio => _roomSizeRatio;
        public float RoomPositionVariation => _roomPositionVariation;
        public bool CenterRooms => _centerRooms;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_minPartitionSize < 6)
                result.AddError("Min partition size must be at least 6");
                
            if (_maxDepth < 1)
                result.AddError("Max depth must be at least 1");
                
            if (_roomSizeRatio <= 0f || _roomSizeRatio > 1f)
                result.AddError("Room size ratio must be between 0 and 1");
                
            if (_roomPositionVariation < 0f || _roomPositionVariation > 0.5f)
                result.AddError("Room position variation must be between 0 and 0.5");
            
            return result;
        }
    }
    
    /// <summary>
    /// Corridor generation configuration.
    /// </summary>
    [Serializable]
    public class CorridorConfiguration
    {
        [Header("Corridor Style")]
        [SerializeField] private CorridorType _corridorType = CorridorType.LShaped;
        [SerializeField, Min(1)] private int _minWidth = 1;
        [SerializeField, Min(1)] private int _maxWidth = 3;
        [SerializeField] private bool _allowVariableWidth = false;
        
        [Header("Pathing")]
        [SerializeField] private bool _ensureDirectPath = true;
        [SerializeField] private bool _avoidRooms = true;
        [SerializeField, Range(0f, 1f)] private float _pathSmoothing = 0.3f;
        
        [Header("Aesthetics")]
        [SerializeField] private bool _addDecorations = false;
        [SerializeField, Range(0f, 1f)] private float _decorationDensity = 0.1f;
        [SerializeField] private bool _useCurvedCorridors = false;
        
        public CorridorType CorridorType => _corridorType;
        public int MinWidth => _minWidth;
        public int MaxWidth => _maxWidth;
        public bool AllowVariableWidth => _allowVariableWidth;
        public bool EnsureDirectPath => _ensureDirectPath;
        public bool AvoidRooms => _avoidRooms;
        public float PathSmoothing => _pathSmoothing;
        public bool AddDecorations => _addDecorations;
        public float DecorationDensity => _decorationDensity;
        public bool UseCurvedCorridors => _useCurvedCorridors;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_minWidth < 1)
                result.AddError("Min corridor width must be at least 1");
                
            if (_maxWidth < _minWidth)
                result.AddError("Max corridor width must be greater than or equal to min width");
                
            if (_pathSmoothing < 0f || _pathSmoothing > 1f)
                result.AddError("Path smoothing must be between 0 and 1");
                
            if (_decorationDensity < 0f || _decorationDensity > 1f)
                result.AddError("Decoration density must be between 0 and 1");
            
            return result;
        }
    }
    
    /// <summary>
    /// High-level generation rules and constraints.
    /// </summary>
    [Serializable]
    public class GenerationRules
    {
        [Header("Connectivity")]
        [SerializeField] private bool _ensureAllRoomsReachable = true;
        [SerializeField] private bool _createLoops = false;
        [SerializeField, Range(0f, 1f)] private float _loopChance = 0.2f;
        
        [Header("Special Rooms")]
        [SerializeField, Range(0f, 1f)] private float _secretRoomChance = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _treasureRoomChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _bossRoomChance = 0.1f;
        
        [Header("Distribution")]
        [SerializeField] private bool _balanceRoomDistribution = true;
        [SerializeField] private bool _clusterSimilarRooms = false;
        [SerializeField, Range(0f, 1f)] private float _randomnessFactor = 0.5f;
        
        public bool EnsureAllRoomsReachable => _ensureAllRoomsReachable;
        public bool CreateLoops => _createLoops;
        public float LoopChance => _loopChance;
        public float SecretRoomChance => _secretRoomChance;
        public float TreasureRoomChance => _treasureRoomChance;
        public float BossRoomChance => _bossRoomChance;
        public bool BalanceRoomDistribution => _balanceRoomDistribution;
        public bool ClusterSimilarRooms => _clusterSimilarRooms;
        public float RandomnessFactor => _randomnessFactor;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_loopChance < 0f || _loopChance > 1f)
                result.AddError("Loop chance must be between 0 and 1");
                
            if (_secretRoomChance < 0f || _secretRoomChance > 1f)
                result.AddError("Secret room chance must be between 0 and 1");
                
            if (_treasureRoomChance < 0f || _treasureRoomChance > 1f)
                result.AddError("Treasure room chance must be between 0 and 1");
                
            if (_bossRoomChance < 0f || _bossRoomChance > 1f)
                result.AddError("Boss room chance must be between 0 and 1");
                
            if (_randomnessFactor < 0f || _randomnessFactor > 1f)
                result.AddError("Randomness factor must be between 0 and 1");
            
            return result;
        }
    }
    
    /// <summary>
    /// Validation rules for generated maps.
    /// </summary>
    [Serializable]
    public class ValidationRules
    {
        [Header("Basic Validation")]
        [SerializeField] private bool _validateConnectivity = true;
        [SerializeField] private bool _validateRoomSizes = true;
        [SerializeField] private bool _validateCorridorWidths = true;
        
        [Header("Strict Validation")]
        [SerializeField] private bool _rejectInvalidMaps = true;
        [SerializeField, Min(1)] private int _maxRetryAttempts = 3;
        [SerializeField] private bool _logValidationDetails = true;
        
        [Header("Quality Checks")]
        [SerializeField, Min(0.1f)] private float _minWalkableRatio = 0.3f;
        [SerializeField, Min(1)] private int _minRoomCount = 3;
        [SerializeField, Min(0.1f)] private float _maxDeadEndRatio = 0.5f;
        
        public bool ValidateConnectivity => _validateConnectivity;
        public bool ValidateRoomSizes => _validateRoomSizes;
        public bool ValidateCorridorWidths => _validateCorridorWidths;
        public bool RejectInvalidMaps { get => _rejectInvalidMaps; set => _rejectInvalidMaps = value; }
        public int MaxRetryAttempts { get => _maxRetryAttempts; set => _maxRetryAttempts = value; }
        public bool LogValidationDetails => _logValidationDetails;
        public float MinWalkableRatio => _minWalkableRatio;
        public int MinRoomCount => _minRoomCount;
        public float MaxDeadEndRatio => _maxDeadEndRatio;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_maxRetryAttempts < 1)
                result.AddError("Max retry attempts must be at least 1");
                
            if (_minWalkableRatio <= 0f || _minWalkableRatio > 1f)
                result.AddError("Min walkable ratio must be between 0 and 1");
                
            if (_minRoomCount < 1)
                result.AddError("Min room count must be at least 1");
                
            if (_maxDeadEndRatio < 0f || _maxDeadEndRatio > 1f)
                result.AddError("Max dead end ratio must be between 0 and 1");
            
            return result;
        }
    }
    
    /// <summary>
    /// Performance optimization settings.
    /// </summary>
    [Serializable]
    public class PerformanceSettings
    {
        [Header("Generation Performance")]
        [SerializeField] private bool _enableMultithreading = false;
        [SerializeField, Min(10)] private int _generationTimeoutMs = 10000;
        [SerializeField] private bool _useIncrementalGeneration = false;
        
        [Header("Memory Management")]
        [SerializeField] private bool _poolObjects = true;
        [SerializeField] private bool _reuseTilemaps = false;
        [SerializeField, Min(10)] private int _maxPoolSize = 100;
        
        [Header("LOD Settings")]
        [SerializeField] private bool _enableLOD = false;
        [SerializeField, Min(1)] private int _lodLevels = 3;
        [SerializeField] private float _lodDistance = 50f;
        
        public bool EnableMultithreading { get => _enableMultithreading; set => _enableMultithreading = value; }
        public int GenerationTimeoutMs { get => _generationTimeoutMs; set => _generationTimeoutMs = value; }
        public bool UseIncrementalGeneration { get => _useIncrementalGeneration; set => _useIncrementalGeneration = value; }
        public bool PoolObjects { get => _poolObjects; set => _poolObjects = value; }
        public bool ReuseTilemaps { get => _reuseTilemaps; set => _reuseTilemaps = value; }
        public int MaxPoolSize { get => _maxPoolSize; set => _maxPoolSize = value; }
        public bool EnableLOD { get => _enableLOD; set => _enableLOD = value; }
        public int LODLevels { get => _lodLevels; set => _lodLevels = value; }
        public float LODDistance { get => _lodDistance; set => _lodDistance = value; }
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_generationTimeoutMs < 100)
                result.AddError("Generation timeout must be at least 100ms");
                
            if (_maxPoolSize < 1)
                result.AddError("Max pool size must be at least 1");
                
            if (_lodLevels < 1)
                result.AddError("LOD levels must be at least 1");
                
            if (_lodDistance <= 0f)
                result.AddError("LOD distance must be greater than 0");
            
            return result;
        }
    }
    
    /// <summary>
    /// Runtime configuration settings.
    /// </summary>
    [Serializable]
    public class RuntimeConfiguration
    {
        [Header("Generation Timing")]
        [SerializeField] private bool _generateOnStart = false;
        [SerializeField] private bool _generateAsync = true;
        [SerializeField] private float _generationDelay = 0f;
        
        [Header("Save/Load")]
        [SerializeField] private bool _autoSaveMaps = false;
        [SerializeField] private bool _allowMapSaving = true;
        [SerializeField] private bool _allowMapLoading = true;
        
        [Header("Runtime Modification")]
        [SerializeField] private bool _allowRuntimeRegeneration = false;
        [SerializeField] private bool _allowParameterModification = false;
        [SerializeField] private bool _requireAdminPrivileges = true;
        
        public bool GenerateOnStart => _generateOnStart;
        public bool GenerateAsync => _generateAsync;
        public float GenerationDelay => _generationDelay;
        public bool AutoSaveMaps => _autoSaveMaps;
        public bool AllowMapSaving => _allowMapSaving;
        public bool AllowMapLoading => _allowMapLoading;
        public bool AllowRuntimeRegeneration => _allowRuntimeRegeneration;
        public bool AllowParameterModification => _allowParameterModification;
        public bool RequireAdminPrivileges => _requireAdminPrivileges;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_generationDelay < 0f)
                result.AddError("Generation delay cannot be negative");
            
            return result;
        }
    }
    
    /// <summary>
    /// Debug and development settings.
    /// </summary>
    [Serializable]
    public class DebugSettings
    {
        [Header("Visualization")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private bool _showRoomLabels = false;
        [SerializeField] private bool _showConnectivity = false;
        [SerializeField] private bool _colorizeRooms = false;
        
        [Header("Logging")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _logGenerationSteps = false;
        [SerializeField] private bool _logPerformanceMetrics = false;
        [SerializeField] private bool _logValidationResults = true;
        
        [Header("Testing")]
        [SerializeField] private bool _enableTestMode = false;
        [SerializeField] private int _testSeed = 12345;
        [SerializeField] private bool _runValidationTests = true;
        
        public bool ShowGizmos { get => _showGizmos; set => _showGizmos = value; }
        public bool ShowRoomLabels { get => _showRoomLabels; set => _showRoomLabels = value; }
        public bool ShowConnectivity { get => _showConnectivity; set => _showConnectivity = value; }
        public bool ColorizeRooms { get => _colorizeRooms; set => _colorizeRooms = value; }
        public bool EnableLogging { get => _enableLogging; set => _enableLogging = value; }
        public bool LogGenerationSteps { get => _logGenerationSteps; set => _logGenerationSteps = value; }
        public bool LogPerformanceMetrics => _logPerformanceMetrics;
        public bool LogValidationResults => _logValidationResults;
        public bool EnableTestMode { get => _enableTestMode; set => _enableTestMode = value; }
        public int TestSeed => _testSeed;
        public bool RunValidationTests => _runValidationTests;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            // No validation errors for debug settings
            return result;
        }
    }
    
    /// <summary>
    /// Quality and optimization settings.
    /// </summary>
    [Serializable]
    public class QualitySettings
    {
        [Header("Generation Quality")]
        [SerializeField] private GenerationQuality _quality = GenerationQuality.Medium;
        [SerializeField] private bool _adaptiveQuality = false;
        [SerializeField] private float _qualityThreshold = 60f; // FPS threshold
        
        [Header("Detail Levels")]
        [SerializeField, Range(0f, 1f)] private float _decorationQuality = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _lightingQuality = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _effectsQuality = 0.3f;
        
        [Header("Optimization")]
        [SerializeField] private bool _enableOcclusionCulling = true;
        [SerializeField] private bool _enableFrustumCulling = true;
        [SerializeField] private bool _batchTileOperations = true;
        
        public GenerationQuality Quality { get => _quality; set => _quality = value; }
        public bool AdaptiveQuality { get => _adaptiveQuality; set => _adaptiveQuality = value; }
        public float QualityThreshold => _qualityThreshold;
        public float DecorationQuality => _decorationQuality;
        public float LightingQuality => _lightingQuality;
        public float EffectsQuality => _effectsQuality;
        public bool EnableOcclusionCulling => _enableOcclusionCulling;
        public bool EnableFrustumCulling => _enableFrustumCulling;
        public bool BatchTileOperations => _batchTileOperations;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_qualityThreshold <= 0f)
                result.AddError("Quality threshold must be greater than 0");
            
            return result;
        }
    }
    
    // Enums
    public enum GenerationProfile
    {
        Any,
        Development,
        Testing,
        Production,
        Mobile,
        Desktop
    }
    
    public enum CorridorType
    {
        Straight,
        LShaped,
        ZShaped,
        Curved,
        Random
    }
    
    public enum SplitPreference
    {
        Horizontal,
        Vertical,
        Alternate,
        Random,
        Balanced
    }
    
    public enum GenerationQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
}