using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Configuration for enemy spawn tables including wave definitions, spawn rules,
    * difficulty scaling, and integration with the existing WaveSpawner system.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTableConfiguration", menuName = "Office Mice/Map Generation/Spawn Table Configuration")]
    [Serializable]
    public class SpawnTableConfiguration : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _spawnTableID;
        [SerializeField] private string _tableName;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private SpawnTableType _tableType;
        
        [Header("Spawn Entries")]
        [SerializeField] private List<SpawnEntry> _spawnEntries = new List<SpawnEntry>();
        [SerializeField] private bool _ensureMinimumSpawns = true;
        [SerializeField, Min(1)] private int _minimumSpawns = 1;
        
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveDefinition> _waveDefinitions = new List<WaveDefinition>();
        [SerializeField] private WavePattern _wavePattern = WavePattern.Sequential;
        [SerializeField, Min(1)] private int _maxConcurrentEnemies = 5;
        [SerializeField] private float _spawnDelayBetweenWaves = 2f;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private DifficultyScaling _difficultyScaling;
        [SerializeField] private bool _scaleWithPlayerProgress = true;
        [SerializeField, Range(0.1f, 5f)] private float _difficultyMultiplier = 1f;
        
        [Header("Spawn Rules")]
        [SerializeField] private SpawnRules _spawnRules;
        [SerializeField] private List<SpawnCondition> _spawnConditions = new List<SpawnCondition>();
        [SerializeField] private bool _respectExistingSpawnPoints = true;
        
        [Header("Integration Settings")]
        [SerializeField] private bool _useWaveSpawnerSystem = true;
        [SerializeField] private string _spawnPointTag = "Spawn Point";
        [SerializeField] private bool _useObjectPooling = true;
        [SerializeField] private float _spawnRadius = 10f;
        
        // Public Properties
        public string SpawnTableID => _spawnTableID;
        public string TableName => _tableName;
        public string Description => _description;
        public SpawnTableType TableType => _tableType;
        public IReadOnlyList<SpawnEntry> SpawnEntries => _spawnEntries.AsReadOnly();
        public bool EnsureMinimumSpawns => _ensureMinimumSpawns;
        public int MinimumSpawns => _minimumSpawns;
        public IReadOnlyList<WaveDefinition> WaveDefinitions => _waveDefinitions.AsReadOnly();
        public WavePattern WavePattern => _wavePattern;
        public int MaxConcurrentEnemies => _maxConcurrentEnemies;
        public float SpawnDelayBetweenWaves => _spawnDelayBetweenWaves;
        public DifficultyScaling DifficultyScaling => _difficultyScaling;
        public bool ScaleWithPlayerProgress => _scaleWithPlayerProgress;
        public float DifficultyMultiplier => _difficultyMultiplier;
        public SpawnRules SpawnRules => _spawnRules;
        public IReadOnlyList<SpawnCondition> SpawnConditions => _spawnConditions.AsReadOnly();
        public bool RespectExistingSpawnPoints => _respectExistingSpawnPoints;
        public bool UseWaveSpawnerSystem => _useWaveSpawnerSystem;
        public string SpawnPointTag => _spawnPointTag;
        public bool UseObjectPooling => _useObjectPooling;
        public float SpawnRadius => _spawnRadius;
        
        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate identity
            if (string.IsNullOrEmpty(_spawnTableID))
                result.AddError("Spawn table ID is required");
            else if (_spawnTableID.Contains(" "))
                result.AddError("Spawn table ID cannot contain spaces");
                
            if (string.IsNullOrEmpty(_tableName))
                result.AddWarning("Spawn table name is not set");
            
            // Validate spawn entries
            if (_spawnEntries.Count == 0)
                result.AddError("Spawn table must have at least one spawn entry");
            else
            {
                float totalWeight = _spawnEntries.Sum(e => e.Weight);
                if (totalWeight <= 0f)
                    result.AddError("Total spawn entry weight must be greater than 0");
                
                foreach (var entry in _spawnEntries)
                {
                    if (entry != null)
                        result.Merge(entry.Validate());
                }
            }
            
            // Validate wave definitions
            if (_waveDefinitions.Count > 0)
            {
                foreach (var wave in _waveDefinitions)
                {
                    if (wave != null)
                        result.Merge(wave.Validate());
                }
            }
            
            // Validate difficulty scaling
            if (_difficultyScaling == null)
                result.AddError("Difficulty scaling configuration is required");
            else
                result.Merge(_difficultyScaling.Validate());
            
            // Validate spawn rules
            if (_spawnRules == null)
                result.AddError("Spawn rules configuration is required");
            else
                result.Merge(_spawnRules.Validate());
            
            // Validate spawn conditions
            foreach (var condition in _spawnConditions)
            {
                if (condition != null)
                    result.Merge(condition.Validate());
            }
            
            // Validate numeric values
            if (_maxConcurrentEnemies < 1)
                result.AddError("Max concurrent enemies must be at least 1");
                
            if (_spawnDelayBetweenWaves < 0f)
                result.AddError("Spawn delay between waves cannot be negative");
                
            if (_difficultyMultiplier <= 0f)
                result.AddError("Difficulty multiplier must be greater than 0");
                
            if (_spawnRadius <= 0f)
                result.AddError("Spawn radius must be greater than 0");
            
            return result;
        }
        
        // Utility Methods
        public SpawnEntry GetRandomSpawnEntry(System.Random random = null)
        {
            random = random ?? new System.Random();
            
            if (_spawnEntries.Count == 0)
                return null;
            
            float totalWeight = _spawnEntries.Sum(e => e.Weight);
            float randomValue = (float)(random.NextDouble() * totalWeight);
            
            float currentWeight = 0f;
            foreach (var entry in _spawnEntries)
            {
                currentWeight += entry.Weight;
                if (randomValue <= currentWeight)
                    return entry;
            }
            
            return _spawnEntries[_spawnEntries.Count - 1];
        }
        
        public List<SpawnEntry> GetSpawnEntriesForWave(int waveIndex)
        {
            if (_waveDefinitions.Count == 0 || waveIndex >= _waveDefinitions.Count)
                return new List<SpawnEntry>(_spawnEntries);
            
            var waveDef = _waveDefinitions[waveIndex];
            var entries = new List<SpawnEntry>();
            
            foreach (var entryRef in waveDef.Entries)
            {
                var entry = _spawnEntries.FirstOrDefault(e => e.EnemyType == entryRef.EnemyType);
                if (entry != null)
                {
                    // Create a copy with modified count
                    var modifiedEntry = new SpawnEntry
                    {
                        EnemyType = entry.EnemyType,
                        EnemyPrefab = entry.EnemyPrefab,
                        Weight = entry.Weight,
                        MinCount = entryRef.Count,
                        MaxCount = entryRef.Count,
                        SpawnDelay = entry.SpawnDelay,
                        HealthMultiplier = entry.HealthMultiplier * entryRef.HealthMultiplier,
                        DamageMultiplier = entry.DamageMultiplier * entryRef.DamageMultiplier
                    };
                    entries.Add(modifiedEntry);
                }
            }
            
            return entries.Count > 0 ? entries : new List<SpawnEntry>(_spawnEntries);
        }
        
        public int GetScaledEnemyCount(int baseCount, float playerProgress = 0f)
        {
            if (!_scaleWithPlayerProgress)
                return baseCount;
            
            float scaling = _difficultyScaling.GetScalingFactor(playerProgress) * _difficultyMultiplier;
            return Mathf.RoundToInt(baseCount * scaling);
        }
        
        public bool CanSpawnAtPosition(Vector2Int position, MapData mapData)
        {
            if (mapData == null)
                return false;
            
            // Check if position is walkable
            if (!mapData.IsPointWalkable(position))
                return false;
            
            // Check spawn conditions
            foreach (var condition in _spawnConditions)
            {
                if (!condition.IsMet(position, mapData))
                    return false;
            }
            
            return true;
        }
        
        private void OnValidate()
        {
            // Auto-generate spawn table ID if empty
            if (string.IsNullOrEmpty(_spawnTableID) && !string.IsNullOrEmpty(_tableName))
            {
                _spawnTableID = System.Text.RegularExpressions.Regex.Replace(_tableName.ToLower(), @"[^a-z0-9]", "_");
            }
            
            // Clamp values to valid ranges
            _minimumSpawns = Mathf.Max(1, _minimumSpawns);
            _maxConcurrentEnemies = Mathf.Max(1, _maxConcurrentEnemies);
            _spawnDelayBetweenWaves = Mathf.Max(0f, _spawnDelayBetweenWaves);
            _difficultyMultiplier = Mathf.Max(0.1f, _difficultyMultiplier);
            _spawnRadius = Mathf.Max(0.1f, _spawnRadius);
        }
    }
    
    /// <summary>
    /// Individual spawn entry with enemy configuration.
    /// </summary>
    [Serializable]
    public class SpawnEntry
    {
        [SerializeField] private string _enemyType;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField, Range(0f, 1f)] private float _weight = 1f;
        [SerializeField, Min(1)] private int _minCount = 1;
        [SerializeField, Min(1)] private int _maxCount = 3;
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField, Range(0.1f, 5f)] private float _healthMultiplier = 1f;
        [SerializeField, Range(0.1f, 5f)] private float _damageMultiplier = 1f;
        
        public string EnemyType => _enemyType;
        public GameObject EnemyPrefab => _enemyPrefab;
        public float Weight => _weight;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
        public float SpawnDelay => _spawnDelay;
        public float HealthMultiplier => _healthMultiplier;
        public float DamageMultiplier => _damageMultiplier;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_enemyType))
                result.AddError("Enemy type is required");
                
            if (_enemyPrefab == null)
                result.AddError($"Enemy '{_enemyType}' has no prefab assigned");
                
            if (_weight < 0f)
                result.AddError($"Enemy '{_enemyType}' has negative weight");
                
            if (_minCount > _maxCount)
                result.AddError($"Enemy '{_enemyType}' min count ({_minCount}) is greater than max count ({_maxCount})");
                
            if (_spawnDelay < 0f)
                result.AddError($"Enemy '{_enemyType}' has negative spawn delay");
                
            if (_healthMultiplier <= 0f)
                result.AddError($"Enemy '{_enemyType}' has invalid health multiplier");
                
            if (_damageMultiplier <= 0f)
                result.AddError($"Enemy '{_enemyType}' has invalid damage multiplier");
            
            return result;
        }
        
        public int GetRandomCount(System.Random random = null)
        {
            random = random ?? new System.Random();
            return random.Next(_minCount, _maxCount + 1);
        }
    }
    
    /// <summary>
    /// Wave definition for structured enemy spawning.
    /// </summary>
    [Serializable]
    public class WaveDefinition
    {
        [SerializeField] private string _waveName;
        [SerializeField] private List<WaveEntry> _entries = new List<WaveEntry>();
        [SerializeField] private float _waveDelay = 2f;
        [SerializeField] private bool _waitForCompletion = true;
        
        public string WaveName => _waveName;
        public List<WaveEntry> Entries => _entries;
        public float WaveDelay => _waveDelay;
        public bool WaitForCompletion => _waitForCompletion;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_waveName))
                result.AddError("Wave name is required");
                
            if (_entries.Count == 0)
                result.AddError($"Wave '{_waveName}' has no entries");
                
            if (_waveDelay < 0f)
                result.AddError($"Wave '{_waveName}' has negative delay");
            
            foreach (var entry in _entries)
            {
                if (entry != null)
                    result.Merge(entry.Validate());
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Individual entry within a wave.
    /// </summary>
    [Serializable]
    public class WaveEntry
    {
        [SerializeField] private string _enemyType;
        [SerializeField, Min(1)] private int _count = 1;
        [SerializeField, Range(0.1f, 5f)] private float _healthMultiplier = 1f;
        [SerializeField, Range(0.1f, 5f)] private float _damageMultiplier = 1f;
        [SerializeField] private float _spawnDelay = 0.5f;
        
        public string EnemyType => _enemyType;
        public int Count => _count;
        public float HealthMultiplier => _healthMultiplier;
        public float DamageMultiplier => _damageMultiplier;
        public float SpawnDelay => _spawnDelay;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_enemyType))
                result.AddError("Enemy type is required");
                
            if (_count < 1)
                result.AddError($"Wave entry for '{_enemyType}' has invalid count");
                
            if (_healthMultiplier <= 0f)
                result.AddError($"Wave entry for '{_enemyType}' has invalid health multiplier");
                
            if (_damageMultiplier <= 0f)
                result.AddError($"Wave entry for '{_enemyType}' has invalid damage multiplier");
                
            if (_spawnDelay < 0f)
                result.AddError($"Wave entry for '{_enemyType}' has negative spawn delay");
            
            return result;
        }
    }
    
    /// <summary>
    /// Difficulty scaling configuration.
    /// </summary>
    [Serializable]
    public class DifficultyScaling
    {
        [SerializeField] private AnimationCurve _scalingCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        [SerializeField] private float _maxScaling = 5f;
        [SerializeField] private bool _scaleEnemyCount = true;
        [SerializeField] private bool _scaleEnemyHealth = true;
        [SerializeField] private bool _scaleEnemyDamage = true;
        [SerializeField] private float _scalingStep = 0.1f;
        
        public AnimationCurve ScalingCurve => _scalingCurve;
        public float MaxScaling => _maxScaling;
        public bool ScaleEnemyCount => _scaleEnemyCount;
        public bool ScaleEnemyHealth => _scaleEnemyHealth;
        public bool ScaleEnemyDamage => _scaleEnemyDamage;
        public float ScalingStep => _scalingStep;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_maxScaling <= 1f)
                result.AddError("Max scaling must be greater than 1");
                
            if (_scalingStep <= 0f)
                result.AddError("Scaling step must be greater than 0");
            
            return result;
        }
        
        public float GetScalingFactor(float progress)
        {
            progress = Mathf.Clamp01(progress);
            float scaling = _scalingCurve.Evaluate(progress);
            return Mathf.Min(scaling, _maxScaling);
        }
    }
    
    /// <summary>
    /// Rules governing enemy spawning behavior.
    /// </summary>
    [Serializable]
    public class SpawnRules
    {
        [SerializeField] private bool _avoidPlayerLineOfSight = true;
        [SerializeField] private float _minDistanceFromPlayer = 5f;
        [SerializeField] private float _maxDistanceFromPlayer = 20f;
        [SerializeField] private bool _avoidClustering = true;
        [SerializeField] private float _minDistanceBetweenSpawns = 3f;
        [SerializeField] private bool _respectRoomCapacity = true;
        [SerializeField, Min(1)] private int _maxEnemiesPerRoom = 10;
        
        public bool AvoidPlayerLineOfSight => _avoidPlayerLineOfSight;
        public float MinDistanceFromPlayer => _minDistanceFromPlayer;
        public float MaxDistanceFromPlayer => _maxDistanceFromPlayer;
        public bool AvoidClustering => _avoidClustering;
        public float MinDistanceBetweenSpawns => _minDistanceBetweenSpawns;
        public bool RespectRoomCapacity => _respectRoomCapacity;
        public int MaxEnemiesPerRoom => _maxEnemiesPerRoom;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (_minDistanceFromPlayer < 0f)
                result.AddError("Min distance from player cannot be negative");
                
            if (_maxDistanceFromPlayer <= _minDistanceFromPlayer)
                result.AddError("Max distance from player must be greater than min distance");
                
            if (_minDistanceBetweenSpawns < 0f)
                result.AddError("Min distance between spawns cannot be negative");
                
            if (_maxEnemiesPerRoom < 1)
                result.AddError("Max enemies per room must be at least 1");
            
            return result;
        }
    }
    
    /// <summary>
    /// Condition that must be met for spawning to occur.
    /// </summary>
    [Serializable]
    public class SpawnCondition
    {
        [SerializeField] private string _conditionName;
        [SerializeField] private ConditionType _conditionType;
        [SerializeField] private string _parameter;
        [SerializeField] private float _threshold = 0f;
        [SerializeField] private ComparisonOperator _operator = ComparisonOperator.GreaterThan;
        
        public string ConditionName => _conditionName;
        public ConditionType ConditionType => _conditionType;
        public string Parameter => _parameter;
        public float Threshold => _threshold;
        public ComparisonOperator Operator => _operator;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(_conditionName))
                result.AddError("Condition name is required");
                
            if (string.IsNullOrEmpty(_parameter) && _conditionType != ConditionType.Always)
                result.AddError($"Condition '{_conditionName}' requires a parameter");
            
            return result;
        }
        
        public bool IsMet(Vector2Int position, MapData mapData)
        {
            switch (_conditionType)
            {
                case ConditionType.Always:
                    return true;
                    
                case ConditionType.RoomType:
                    var room = mapData.GetRoomContainingPoint(position);
                    if (room != null && !string.IsNullOrEmpty(_parameter))
                    {
                        return CompareValue(room.Classification.ToString(), _parameter);
                    }
                    return false;
                    
                case ConditionType.DistanceFromSpawn:
                    var distance = Vector2Int.Distance(position, mapData.PlayerSpawnPosition);
                    return CompareValue(distance, _threshold);
                    
                case ConditionType.RoomSize:
                    var targetRoom = mapData.GetRoomContainingPoint(position);
                    if (targetRoom != null)
                    {
                        return CompareValue(targetRoom.Area, _threshold);
                    }
                    return false;
                    
                default:
                    return true;
            }
        }
        
        private bool CompareValue(float value, float threshold)
        {
            switch (_operator)
            {
                case ComparisonOperator.Equal:
                    return Mathf.Approximately(value, threshold);
                case ComparisonOperator.NotEqual:
                    return !Mathf.Approximately(value, threshold);
                case ComparisonOperator.GreaterThan:
                    return value > threshold;
                case ComparisonOperator.LessThan:
                    return value < threshold;
                case ComparisonOperator.GreaterThanOrEqual:
                    return value >= threshold;
                case ComparisonOperator.LessThanOrEqual:
                    return value <= threshold;
                default:
                    return false;
            }
        }
        
        private bool CompareValue(string value, string target)
        {
            switch (_operator)
            {
                case ComparisonOperator.Equal:
                    return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
                case ComparisonOperator.NotEqual:
                    return !string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
    
    // Enums
    public enum SpawnTableType
    {
        Standard,
        Boss,
        Elite,
        Swarm,
        Sniper,
        Tank,
        Custom
    }
    
    public enum WavePattern
    {
        Sequential,
        Random,
        Loop,
        Adaptive
    }
    
    public enum ConditionType
    {
        Always,
        RoomType,
        DistanceFromSpawn,
        RoomSize,
        TimeOfDay,
        PlayerHealth,
        Custom
    }
    
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }
}