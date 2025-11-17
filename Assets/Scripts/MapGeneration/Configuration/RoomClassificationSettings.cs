using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration
{
    /// <summary>
    /// Configuration settings for room classification system.
    /// Defines rules, distribution tables, and constraints for automatic room type assignment.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomClassificationSettings", menuName = "Office Mice/Map Generation/Room Classification Settings")]
    [Serializable]
    public class RoomClassificationSettings : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _settingsID;
        [SerializeField] private string _settingsName;
        [SerializeField, TextArea(3, 5)] private string _description;
        
        [Header("Classification Rules")]
        [SerializeField] private List<RoomTypeRule> _typeRules = new List<RoomTypeRule>();
        [SerializeField] private List<DistributionRule> _distributionRules = new List<DistributionRule>();
        
        [Header("Classification Behavior")]
        [SerializeField, Range(0f, 1f)] private float _randomnessFactor = 0.3f;
        [SerializeField] private bool _enforceDistributionStrictly = false;
        [SerializeField] private bool _allowDesignerOverrides = true;
        
        [Header("Performance")]
        [SerializeField] private bool _enableCaching = true;
        [SerializeField, Min(1)] private int _maxCacheSize = 1000;
        
        // Public Properties
        public string SettingsID => _settingsID;
        public string SettingsName => _settingsName;
        public string Description => _description;
        public IReadOnlyList<RoomTypeRule> TypeRules => _typeRules.AsReadOnly();
        public IReadOnlyList<DistributionRule> DistributionRules => _distributionRules.AsReadOnly();
        public float RandomnessFactor => _randomnessFactor;
        public bool EnforceDistributionStrictly => _enforceDistributionStrictly;
        public bool AllowDesignerOverrides => _allowDesignerOverrides;
        public bool EnableCaching => _enableCaching;
        public int MaxCacheSize => _maxCacheSize;
        
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
            
            // Validate type rules
            if (_typeRules.Count == 0)
                result.AddError("At least one type rule is required");
            else
            {
                var duplicateTypes = _typeRules
                    .GroupBy(r => r.Type)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                    
                foreach (var duplicate in duplicateTypes)
                {
                    result.AddError($"Duplicate type rule for {duplicate}");
                }
                
                foreach (var rule in _typeRules)
                {
                    result.Merge(rule.Validate());
                }
            }
            
            // Validate distribution rules
            if (_distributionRules.Count == 0)
                result.AddWarning("No distribution rules configured - will use default distribution");
            else
            {
                var totalPercentage = _distributionRules.Sum(dr => dr.Percentage);
                if (Math.Abs(totalPercentage - 100.0f) > 0.01f)
                {
                    if (_enforceDistributionStrictly)
                        result.AddError($"Distribution rules must sum to 100%, currently {totalPercentage}%");
                    else
                        result.AddWarning($"Distribution rules sum to {totalPercentage}%, expected 100%");
                }
                
                foreach (var rule in _distributionRules)
                {
                    result.Merge(rule.Validate());
                }
            }
            
            // Validate behavior settings
            if (_randomnessFactor < 0f || _randomnessFactor > 1f)
                result.AddError("Randomness factor must be between 0 and 1");
                
            if (_maxCacheSize < 1)
                result.AddError("Max cache size must be at least 1");
            
            return result;
        }
        
        // Utility Methods
        public RoomTypeRule GetTypeRule(RoomClassification type)
        {
            return _typeRules.FirstOrDefault(r => r.Type == type);
        }
        
        public DistributionRule GetDistributionRule(RoomClassification type)
        {
            return _distributionRules.FirstOrDefault(r => r.Type == type);
        }
        
        public void AddTypeRule(RoomTypeRule rule)
        {
            if (rule != null && !_typeRules.Any(r => r.Type == rule.Type))
            {
                _typeRules.Add(rule);
            }
        }
        
        public void RemoveTypeRule(RoomClassification type)
        {
            _typeRules.RemoveAll(r => r.Type == type);
        }
        
        public void AddDistributionRule(DistributionRule rule)
        {
            if (rule != null && !_distributionRules.Any(r => r.Type == rule.Type))
            {
                _distributionRules.Add(rule);
            }
        }
        
        public void RemoveDistributionRule(RoomClassification type)
        {
            _distributionRules.RemoveAll(r => r.Type == type);
        }
        
        /// <summary>
        /// Creates default configuration for office environment.
        /// </summary>
        public void CreateDefaultConfiguration()
        {
            _typeRules.Clear();
            _distributionRules.Clear();
            
            // Default type rules
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(12, 12),
                PreferredSize = new Vector2Int(6, 8),
                PositionPreference = PositionPreference.Any,
                DepthPreference = DepthPreference.Any,
                Priority = 1
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.Conference,
                MinSize = new Vector2Int(8, 8),
                MaxSize = new Vector2Int(20, 20),
                PreferredSize = new Vector2Int(12, 12),
                PositionPreference = PositionPreference.Center,
                DepthPreference = DepthPreference.Shallow,
                Priority = 2
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.BreakRoom,
                MinSize = new Vector2Int(6, 6),
                MaxSize = new Vector2Int(15, 15),
                PreferredSize = new Vector2Int(8, 10),
                PositionPreference = PositionPreference.Any,
                DepthPreference = DepthPreference.Any,
                Priority = 1
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.Storage,
                MinSize = new Vector2Int(3, 3),
                MaxSize = new Vector2Int(8, 8),
                PreferredSize = new Vector2Int(4, 6),
                PositionPreference = PositionPreference.Edge,
                DepthPreference = DepthPreference.Deep,
                Priority = 0
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.Lobby,
                MinSize = new Vector2Int(10, 10),
                MaxSize = new Vector2Int(25, 25),
                PreferredSize = new Vector2Int(15, 15),
                PositionPreference = PositionPreference.Center,
                DepthPreference = DepthPreference.Shallow,
                Priority = 3
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.ServerRoom,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(10, 10),
                PreferredSize = new Vector2Int(6, 6),
                PositionPreference = PositionPreference.Edge,
                DepthPreference = DepthPreference.Deep,
                Priority = 2
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.Security,
                MinSize = new Vector2Int(3, 3),
                MaxSize = new Vector2Int(8, 8),
                PreferredSize = new Vector2Int(5, 5),
                PositionPreference = PositionPreference.Edge,
                DepthPreference = DepthPreference.Shallow,
                Priority = 2
            });
            
            _typeRules.Add(new RoomTypeRule
            {
                Type = RoomClassification.BossOffice,
                MinSize = new Vector2Int(10, 10),
                MaxSize = new Vector2Int(20, 20),
                PreferredSize = new Vector2Int(15, 12),
                PositionPreference = PositionPreference.Center,
                DepthPreference = DepthPreference.Shallow,
                Priority = 4
            });
            
            // Default distribution rules
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 40f,
                MinCount = 1,
                MaxCount = 20
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Conference,
                Percentage = 10f,
                MinCount = 0,
                MaxCount = 3
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.BreakRoom,
                Percentage = 10f,
                MinCount = 0,
                MaxCount = 2
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Storage,
                Percentage = 15f,
                MinCount = 0,
                MaxCount = 5
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Lobby,
                Percentage = 5f,
                MinCount = 1,
                MaxCount = 1
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.ServerRoom,
                Percentage = 5f,
                MinCount = 0,
                MaxCount = 2
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Security,
                Percentage = 10f,
                MinCount = 0,
                MaxCount = 3
            });
            
            _distributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.BossOffice,
                Percentage = 5f,
                MinCount = 0,
                MaxCount = 1
            });
        }
        
        private void OnValidate()
        {
            // Auto-generate settings ID if empty
            if (string.IsNullOrEmpty(_settingsID) && !string.IsNullOrEmpty(_settingsName))
            {
                _settingsID = System.Text.RegularExpressions.Regex.Replace(_settingsName.ToLower(), @"[^a-z0-9]", "_");
            }
            
            // Clamp values to valid ranges
            _randomnessFactor = Mathf.Clamp01(_randomnessFactor);
            _maxCacheSize = Mathf.Max(1, _maxCacheSize);
        }
    }
    
    /// <summary>
    /// Defines rules for a specific room type including size constraints and preferences.
    /// </summary>
    [Serializable]
    public class RoomTypeRule
    {
        [Header("Room Type")]
        [SerializeField] private RoomClassification _type;
        
        [Header("Size Constraints")]
        [SerializeField] private Vector2Int _minSize = new Vector2Int(3, 3);
        [SerializeField] private Vector2Int _maxSize = new Vector2Int(20, 20);
        [SerializeField] private Vector2Int _preferredSize = new Vector2Int(8, 8);
        
        [Header("Position Preferences")]
        [SerializeField] private PositionPreference _positionPreference = PositionPreference.Any;
        [SerializeField] private DepthPreference _depthPreference = DepthPreference.Any;
        
        [Header("Classification Priority")]
        [SerializeField, Min(0)] private int _priority = 1;
        [SerializeField, TextArea(2, 3)] private string _notes;
        
        // Public Properties
        public RoomClassification Type => _type;
        public Vector2Int MinSize => _minSize;
        public Vector2Int MaxSize => _maxSize;
        public Vector2Int PreferredSize => _preferredSize;
        public PositionPreference PositionPreference => _positionPreference;
        public DepthPreference DepthPreference => _depthPreference;
        public int Priority => _priority;
        public string Notes => _notes;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate room type
            if (!IsOfficeRoomType(_type))
                result.AddError($"Room type {_type} is not a valid office room type");
            
            // Validate size constraints
            if (_minSize.x < 3 || _minSize.y < 3)
                result.AddError($"Minimum size for {_type} must be at least 3x3");
                
            if (_maxSize.x < _minSize.x || _maxSize.y < _minSize.y)
                result.AddError($"Maximum size for {_type} must be greater than or equal to minimum size");
                
            if (_preferredSize.x < _minSize.x || _preferredSize.x > _maxSize.x ||
                _preferredSize.y < _minSize.y || _preferredSize.y > _maxSize.y)
                result.AddError($"Preferred size for {_type} must be within min-max range");
            
            // Validate priority
            if (_priority < 0)
                result.AddError($"Priority for {_type} cannot be negative");
            
            return result;
        }
        
        private static bool IsOfficeRoomType(RoomClassification type)
        {
            return type >= RoomClassification.Office && type <= RoomClassification.BossOffice;
        }
    }
    
    /// <summary>
    /// Defines distribution rules for room types to ensure balanced map generation.
    /// </summary>
    [Serializable]
    public class DistributionRule
    {
        [Header("Room Type")]
        [SerializeField] private RoomClassification _type;
        
        [Header("Distribution")]
        [SerializeField, Range(0f, 100f)] private float _percentage = 10f;
        [SerializeField, Min(0)] private int _minCount = 0;
        [SerializeField, Min(0)] private int _maxCount = 10;
        
        [Header("Constraints")]
        [SerializeField] private bool _enforceMinCount = true;
        [SerializeField] private bool _enforceMaxCount = true;
        [SerializeField, TextArea(2, 3)] private string _notes;
        
        // Public Properties
        public RoomClassification Type => _type;
        public float Percentage => _percentage;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
        public bool EnforceMinCount => _enforceMinCount;
        public bool EnforceMaxCount => _enforceMaxCount;
        public string Notes => _notes;
        
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate room type
            if (!IsOfficeRoomType(_type))
                result.AddError($"Room type {_type} is not a valid office room type");
            
            // Validate distribution
            if (_percentage < 0f || _percentage > 100f)
                result.AddError($"Percentage for {_type} must be between 0 and 100");
                
            if (_minCount < 0)
                result.AddError($"Min count for {_type} cannot be negative");
                
            if (_maxCount < 0)
                result.AddError($"Max count for {_type} cannot be negative");
                
            if (_maxCount < _minCount)
                result.AddError($"Max count for {_type} must be greater than or equal to min count");
            
            return result;
        }
        
        private static bool IsOfficeRoomType(RoomClassification type)
        {
            return type >= RoomClassification.Office && type <= RoomClassification.BossOffice;
        }
    }
    
    // Enums for classification preferences
    public enum PositionPreference
    {
        Any,        // No position preference
        Center,     // Prefer center of map
        Edge,       // Prefer edges of map
        Corner      // Prefer corners of map
    }
    
    public enum DepthPreference
    {
        Any,        // No depth preference
        Shallow,    // Prefer shallow BSP depth
        Medium,     // Prefer medium BSP depth
        Deep        // Prefer deep BSP depth
    }
}