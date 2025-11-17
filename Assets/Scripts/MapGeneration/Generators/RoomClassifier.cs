using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Generators
{
    /// <summary>
    /// Room classification system that assigns room types based on size, position, and depth.
    /// Implements rule-based logic with configurable distribution tables and designer overrides.
    /// </summary>
    public class RoomClassifier
    {
        #region Private Fields
        private System.Random _random;
        private RoomClassificationSettings _classificationSettings;
        private Dictionary<RoomClassification, RoomTypeRule> _typeRules;
        private Dictionary<int, RoomClassification> _designerOverrides;
        #endregion

        #region Constructor
        public RoomClassifier(RoomClassificationSettings settings, int seed = 0)
        {
            _classificationSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new System.Random(seed);
            _designerOverrides = new Dictionary<int, RoomClassification>();
            InitializeTypeRules();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Classifies a list of rooms based on their properties and configured rules.
        /// </summary>
        /// <param name="rooms">List of rooms to classify</param>
        /// <param name="mapBounds">Overall map boundaries for position-based calculations</param>
        /// <returns>Classified rooms with assigned types</returns>
        public List<RoomData> ClassifyRooms(List<RoomData> rooms, RectInt mapBounds)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));

            var classifiedRooms = new List<RoomData>();
            var distributionTracker = new Dictionary<RoomClassification, int>();

            // First pass: apply designer overrides and calculate room properties
            foreach (var room in rooms)
            {
                var roomClone = room.Clone();
                
                // Apply designer override if present
                if (_designerOverrides.TryGetValue(room.RoomID, out var overrideType))
                {
                    roomClone.SetClassification(overrideType);
                    TrackDistribution(distributionTracker, overrideType);
                    classifiedRooms.Add(roomClone);
                    continue;
                }

                // Calculate room properties for classification
                var properties = CalculateRoomProperties(roomClone, mapBounds);
                roomClone.SetDistanceFromPlayerSpawn(properties.DistanceFromCenter);
                
                classifiedRooms.Add(roomClone);
            }

            // Second pass: automatic classification based on rules and distribution
            var roomsToClassify = classifiedRooms
                .Where(r => r.Classification == RoomClassification.Unassigned)
                .OrderByDescending(r => r.Area) // Classify larger rooms first
                .ToList();

            foreach (var room in roomsToClassify)
            {
                var classification = ClassifyRoom(room, mapBounds, distributionTracker);
                room.SetClassification(classification);
                TrackDistribution(distributionTracker, classification);
            }

            // Third pass: validation and adjustments
            ValidateAndAdjustClassifications(classifiedRooms, distributionTracker);

            return classifiedRooms;
        }

        /// <summary>
        /// Sets a designer override for a specific room.
        /// </summary>
        /// <param name="roomID">ID of the room to override</param>
        /// <param name="classification">Classification to assign</param>
        public void SetDesignerOverride(int roomID, RoomClassification classification)
        {
            if (!IsOfficeRoomType(classification))
                throw new ArgumentException($"Classification {classification} is not a valid office room type");

            _designerOverrides[roomID] = classification;
        }

        /// <summary>
        /// Removes a designer override for a specific room.
        /// </summary>
        /// <param name="roomID">ID of the room to remove override from</param>
        /// <returns>True if override was removed, false if not found</returns>
        public bool RemoveDesignerOverride(int roomID)
        {
            return _designerOverrides.Remove(roomID);
        }

        /// <summary>
        /// Clears all designer overrides.
        /// </summary>
        public void ClearDesignerOverrides()
        {
            _designerOverrides.Clear();
        }

        /// <summary>
        /// Validates room classification configuration.
        /// </summary>
        /// <returns>Validation result with any errors or warnings</returns>
        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            if (_classificationSettings == null)
                result.AddError("Classification settings are null");

            if (_typeRules == null || _typeRules.Count == 0)
                result.AddError("No type rules configured");

            // Validate distribution rules
            var totalDistribution = _classificationSettings.DistributionRules
                .Sum(dr => dr.Percentage);

            if (Math.Abs(totalDistribution - 100.0f) > 0.01f)
                result.AddWarning($"Distribution rules sum to {totalDistribution}%, expected 100%");

            // Validate minimum size requirements
            foreach (var rule in _typeRules.Values)
            {
                if (rule.MinSize.x < 3 || rule.MinSize.y < 3)
                    result.AddError($"Room type {rule.Type} has minimum size smaller than 3x3");

                if (rule.MaxSize.x < rule.MinSize.x || rule.MaxSize.y < rule.MinSize.y)
                    result.AddError($"Room type {rule.Type} has invalid size range");
            }

            return result;
        }
        #endregion

        #region Private Methods
        private void InitializeTypeRules()
        {
            _typeRules = new Dictionary<RoomClassification, RoomTypeRule>();

            foreach (var officeType in GetOfficeRoomTypes())
            {
                var rule = _classificationSettings.GetTypeRule(officeType);
                if (rule != null)
                {
                    _typeRules[officeType] = rule;
                }
                else
                {
                    // Create default rule if not configured
                    _typeRules[officeType] = CreateDefaultRule(officeType);
                }
            }
        }

        private RoomTypeRule CreateDefaultRule(RoomClassification type)
        {
            return type switch
            {
                RoomClassification.Office => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(4, 4),
                    MaxSize = new Vector2Int(12, 12),
                    PreferredSize = new Vector2Int(6, 8),
                    PositionPreference = PositionPreference.Any,
                    DepthPreference = DepthPreference.Any,
                    Priority = 1
                },
                RoomClassification.Conference => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(8, 8),
                    MaxSize = new Vector2Int(20, 20),
                    PreferredSize = new Vector2Int(12, 12),
                    PositionPreference = PositionPreference.Center,
                    DepthPreference = DepthPreference.Shallow,
                    Priority = 2
                },
                RoomClassification.BreakRoom => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(6, 6),
                    MaxSize = new Vector2Int(15, 15),
                    PreferredSize = new Vector2Int(8, 10),
                    PositionPreference = PositionPreference.Any,
                    DepthPreference = DepthPreference.Any,
                    Priority = 1
                },
                RoomClassification.Storage => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(3, 3),
                    MaxSize = new Vector2Int(8, 8),
                    PreferredSize = new Vector2Int(4, 6),
                    PositionPreference = PositionPreference.Edge,
                    DepthPreference = DepthPreference.Deep,
                    Priority = 0
                },
                RoomClassification.Lobby => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(10, 10),
                    MaxSize = new Vector2Int(25, 25),
                    PreferredSize = new Vector2Int(15, 15),
                    PositionPreference = PositionPreference.Center,
                    DepthPreference = DepthPreference.Shallow,
                    Priority = 3
                },
                RoomClassification.ServerRoom => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(4, 4),
                    MaxSize = new Vector2Int(10, 10),
                    PreferredSize = new Vector2Int(6, 6),
                    PositionPreference = PositionPreference.Edge,
                    DepthPreference = DepthPreference.Deep,
                    Priority = 2
                },
                RoomClassification.Security => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(3, 3),
                    MaxSize = new Vector2Int(8, 8),
                    PreferredSize = new Vector2Int(5, 5),
                    PositionPreference = PositionPreference.Edge,
                    DepthPreference = DepthPreference.Shallow,
                    Priority = 2
                },
                RoomClassification.BossOffice => new RoomTypeRule
                {
                    Type = type,
                    MinSize = new Vector2Int(10, 10),
                    MaxSize = new Vector2Int(20, 20),
                    PreferredSize = new Vector2Int(15, 12),
                    PositionPreference = PositionPreference.Center,
                    DepthPreference = DepthPreference.Shallow,
                    Priority = 4
                },
                _ => throw new ArgumentException($"Unknown office room type: {type}")
            };
        }

        private RoomProperties CalculateRoomProperties(RoomData room, RectInt mapBounds)
        {
            var mapCenter = new Vector2(
                mapBounds.x + mapBounds.width / 2f,
                mapBounds.y + mapBounds.height / 2f
            );

            var roomCenter = new Vector2(
                room.Center.x,
                room.Center.y
            );

            var distanceFromCenter = Vector2.Distance(roomCenter, mapCenter);
            var maxDistance = Mathf.Sqrt(mapBounds.width * mapBounds.width + mapBounds.height * mapBounds.height) / 2f;
            var normalizedDistance = distanceFromCenter / maxDistance;

            return new RoomProperties
            {
                Area = room.Area,
                Width = room.Bounds.width,
                Height = room.Bounds.height,
                DistanceFromCenter = normalizedDistance,
                AspectRatio = (float)room.Bounds.width / room.Bounds.height
            };
        }

        private RoomClassification ClassifyRoom(RoomData room, RectInt mapBounds, Dictionary<RoomClassification, int> distributionTracker)
        {
            var properties = CalculateRoomProperties(room, mapBounds);
            var candidates = new List<ClassificationCandidate>();

            // Find all compatible room types
            foreach (var rule in _typeRules.Values)
            {
                if (IsRoomCompatible(room, properties, rule, distributionTracker))
                {
                    var score = CalculateClassificationScore(room, properties, rule, distributionTracker);
                    candidates.Add(new ClassificationCandidate
                    {
                        Type = rule.Type,
                        Score = score,
                        Rule = rule
                    });
                }
            }

            if (candidates.Count == 0)
            {
                // Fallback to Office type if no candidates found
                return RoomClassification.Office;
            }

            // Sort by score and apply some randomness for variety
            candidates = candidates.OrderByDescending(c => c.Score).ToList();

            // Apply weighted random selection from top candidates
            var topCandidates = candidates.Take(3).ToList();
            if (topCandidates.Count == 1)
                return topCandidates[0].Type;

            var weights = topCandidates.Select(c => c.Score).ToList();
            var totalWeight = weights.Sum();
            var randomValue = _random.NextDouble() * totalWeight;

            var cumulativeWeight = 0.0;
            for (int i = 0; i < topCandidates.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    return topCandidates[i].Type;
                }
            }

            return topCandidates[0].Type;
        }

        private bool IsRoomCompatible(RoomData room, RoomProperties properties, RoomTypeRule rule, Dictionary<RoomClassification, int> distributionTracker)
        {
            // Check size compatibility
            if (room.Bounds.width < rule.MinSize.x || room.Bounds.height < rule.MinSize.y)
                return false;

            if (room.Bounds.width > rule.MaxSize.x || room.Bounds.height > rule.MaxSize.y)
                return false;

            // Check distribution limits
            var currentCount = distributionTracker.GetValueOrDefault(rule.Type, 0);
            var distributionRule = _classificationSettings.DistributionRules
                .FirstOrDefault(dr => dr.Type == rule.Type);

            if (distributionRule != null)
            {
                var totalRooms = distributionTracker.Values.Sum() + 1; // +1 for current room
                var maxAllowed = Mathf.CeilToInt(totalRooms * distributionRule.Percentage / 100f);

                if (currentCount >= maxAllowed)
                    return false;
            }

            return true;
        }

        private float CalculateClassificationScore(RoomData room, RoomProperties properties, RoomTypeRule rule, Dictionary<RoomClassification, int> distributionTracker)
        {
            float score = rule.Priority * 100f; // Base priority score

            // Size compatibility score
            var sizeScore = CalculateSizeScore(properties, rule);
            score += sizeScore * 50f;

            // Position preference score
            var positionScore = CalculatePositionScore(properties, rule);
            score += positionScore * 30f;

            // Distribution balance score
            var distributionScore = CalculateDistributionScore(rule.Type, distributionTracker);
            score += distributionScore * 20f;

            return score;
        }

        private float CalculateSizeScore(RoomProperties properties, RoomTypeRule rule)
        {
            var widthScore = 1f - Mathf.Abs(properties.Width - rule.PreferredSize.x) / 
                           Mathf.Max(rule.MaxSize.x - rule.MinSize.x, 1f);
            var heightScore = 1f - Mathf.Abs(properties.Height - rule.PreferredSize.y) / 
                            Mathf.Max(rule.MaxSize.y - rule.MinSize.y, 1f);

            return (widthScore + heightScore) / 2f;
        }

        private float CalculatePositionScore(RoomProperties properties, RoomTypeRule rule)
        {
            return rule.PositionPreference switch
            {
                PositionPreference.Center => 1f - properties.DistanceFromCenter,
                PositionPreference.Edge => properties.DistanceFromCenter,
                PositionPreference.Any => 0.5f,
                _ => 0.5f
            };
        }

        private float CalculateDistributionScore(RoomClassification type, Dictionary<RoomClassification, int> distributionTracker)
        {
            var currentCount = distributionTracker.GetValueOrDefault(type, 0);
            var distributionRule = _classificationSettings.DistributionRules
                .FirstOrDefault(dr => dr.Type == type);

            if (distributionRule == null)
                return 0.5f; // Neutral score if no distribution rule

            var totalRooms = distributionTracker.Values.Sum();
            if (totalRooms == 0)
                return 1f; // First room of this type gets bonus

            var currentPercentage = (float)currentCount / totalRooms * 100f;
            var targetPercentage = distributionRule.Percentage;

            // Score decreases as we exceed the target percentage
            if (currentPercentage < targetPercentage)
                return 1f;
            else
                return Mathf.Max(0f, 1f - (currentPercentage - targetPercentage) / targetPercentage);
        }

        private void ValidateAndAdjustClassifications(List<RoomData> rooms, Dictionary<RoomClassification, int> distributionTracker)
        {
            // Ensure at least one of each required room type
            var requiredTypes = new[] { RoomClassification.Office, RoomClassification.Lobby };
            
            foreach (var requiredType in requiredTypes)
            {
                if (!distributionTracker.ContainsKey(requiredType) || distributionTracker[requiredType] == 0)
                {
                    // Find the best candidate room to reclassify
                    var candidate = rooms
                        .Where(r => IsOfficeRoomType(r.Classification))
                        .OrderByDescending(r => r.Area)
                        .FirstOrDefault();

                    if (candidate != null)
                    {
                        distributionTracker[candidate.Classification]--;
                        candidate.SetClassification(requiredType);
                        distributionTracker[requiredType]++;
                    }
                }
            }
        }

        private void TrackDistribution(Dictionary<RoomClassification, int> tracker, RoomClassification type)
        {
            if (!tracker.ContainsKey(type))
                tracker[type] = 0;
            tracker[type]++;
        }

        private static IEnumerable<RoomClassification> GetOfficeRoomTypes()
        {
            return new[]
            {
                RoomClassification.Office,
                RoomClassification.Conference,
                RoomClassification.BreakRoom,
                RoomClassification.Storage,
                RoomClassification.Lobby,
                RoomClassification.ServerRoom,
                RoomClassification.Security,
                RoomClassification.BossOffice
            };
        }

        private static bool IsOfficeRoomType(RoomClassification type)
        {
            return type >= RoomClassification.Office && type <= RoomClassification.BossOffice;
        }
        #endregion

        #region Helper Classes
        private class RoomProperties
        {
            public int Area { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float DistanceFromCenter { get; set; }
            public float AspectRatio { get; set; }
        }

        private class ClassificationCandidate
        {
            public RoomClassification Type { get; set; }
            public float Score { get; set; }
            public RoomTypeRule Rule { get; set; }
        }
        #endregion
    }
}