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
    /// Manager class for room classification that provides designer override functionality
    /// and coordinates between different generation systems.
    /// </summary>
    public class RoomClassificationManager
    {
        #region Private Fields
        private RoomClassifier _classifier;
        private RoomClassificationSettings _settings;
        private Dictionary<int, RoomClassification> _designerOverrides;
        private int _lastSeed;
        #endregion

        #region Constructor
        public RoomClassificationManager(RoomClassificationSettings settings, int seed = 0)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _classifier = new RoomClassifier(settings, seed);
            _designerOverrides = new Dictionary<int, RoomClassification>();
            _lastSeed = seed;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Classifies rooms using the configured rules and designer overrides.
        /// </summary>
        /// <param name="rooms">List of rooms to classify</param>
        /// <param name="mapBounds">Map boundaries for position-based calculations</param>
        /// <returns>Classified rooms</returns>
        public List<RoomData> ClassifyRooms(List<RoomData> rooms, RectInt mapBounds)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));

            // Apply designer overrides to classifier
            foreach (var overridePair in _designerOverrides)
            {
                _classifier.SetDesignerOverride(overridePair.Key, overridePair.Value);
            }

            return _classifier.ClassifyRooms(rooms, mapBounds);
        }

        /// <summary>
        /// Sets a designer override for a specific room.
        /// </summary>
        /// <param name="roomID">ID of the room to override</param>
        /// <param name="classification">Classification to assign</param>
        public void SetDesignerOverride(int roomID, RoomClassification classification)
        {
            if (!IsValidOfficeRoomType(classification))
                throw new ArgumentException($"Classification {classification} is not a valid office room type for override");

            _designerOverrides[roomID] = classification;
            _classifier.SetDesignerOverride(roomID, classification);
        }

        /// <summary>
        /// Removes a designer override for a specific room.
        /// </summary>
        /// <param name="roomID">ID of the room to remove override from</param>
        /// <returns>True if override was removed, false if not found</returns>
        public bool RemoveDesignerOverride(int roomID)
        {
            var removed = _designerOverrides.Remove(roomID);
            if (removed)
            {
                _classifier.RemoveDesignerOverride(roomID);
            }
            return removed;
        }

        /// <summary>
        /// Gets the current designer override for a room.
        /// </summary>
        /// <param name="roomID">ID of the room</param>
        /// <returns>Override classification, or null if no override exists</returns>
        public RoomClassification? GetDesignerOverride(int roomID)
        {
            return _designerOverrides.TryGetValue(roomID, out var classification) ? classification : null;
        }

        /// <summary>
        /// Gets all current designer overrides.
        /// </summary>
        /// <returns>Dictionary of room IDs to their override classifications</returns>
        public IReadOnlyDictionary<int, RoomClassification> GetAllDesignerOverrides()
        {
            return _designerOverrides.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Clears all designer overrides.
        /// </summary>
        public void ClearAllDesignerOverrides()
        {
            _designerOverrides.Clear();
            _classifier.ClearDesignerOverrides();
        }

        /// <summary>
        /// Validates that a room can accept the specified classification.
        /// </summary>
        /// <param name="room">Room to validate</param>
        /// <param name="classification">Classification to validate</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateRoomClassification(RoomData room, RoomClassification classification)
        {
            var result = new ValidationResult();

            if (room == null)
            {
                result.AddError("Room is null");
                return result;
            }

            if (!IsValidOfficeRoomType(classification))
            {
                result.AddError($"Classification {classification} is not a valid office room type");
                return result;
            }

            // Get the type rule for validation
            var typeRule = _settings.GetTypeRule(classification);
            if (typeRule == null)
            {
                result.AddWarning($"No type rule found for {classification}, using default validation");
                return result;
            }

            // Validate size constraints
            if (room.Bounds.width < typeRule.MinSize.x || room.Bounds.height < typeRule.MinSize.y)
            {
                result.AddError($"Room size {room.Bounds.width}x{room.Bounds.height} is below minimum {typeRule.MinSize} for {classification}");
            }

            if (room.Bounds.width > typeRule.MaxSize.x || room.Bounds.height > typeRule.MaxSize.y)
            {
                result.AddError($"Room size {room.Bounds.width}x{room.Bounds.height} exceeds maximum {typeRule.MaxSize} for {classification}");
            }

            return result;
        }

        /// <summary>
        /// Gets suggested classifications for a room based on its properties.
        /// </summary>
        /// <param name="room">Room to analyze</param>
        /// <param name="mapBounds">Map boundaries for position analysis</param>
        /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
        /// <returns>List of suggested classifications with confidence scores</returns>
        public List<ClassificationSuggestion> GetClassificationSuggestions(RoomData room, RectInt mapBounds, int maxSuggestions = 5)
        {
            var suggestions = new List<ClassificationSuggestion>();

            if (room == null)
                return suggestions;

            var officeTypes = GetValidOfficeRoomTypes();
            var mapCenter = new Vector2(mapBounds.x + mapBounds.width / 2f, mapBounds.y + mapBounds.height / 2f);
            var roomCenter = new Vector2(room.Center.x, room.Center.y);
            var distanceFromCenter = Vector2.Distance(roomCenter, mapCenter);
            var maxDistance = Mathf.Sqrt(mapBounds.width * mapBounds.width + mapBounds.height * mapBounds.height) / 2f;
            var normalizedDistance = distanceFromCenter / maxDistance;

            foreach (var type in officeTypes)
            {
                var typeRule = _settings.GetTypeRule(type);
                if (typeRule == null) continue;

                var confidence = CalculateSuggestionConfidence(room, typeRule, normalizedDistance);
                if (confidence > 0f)
                {
                    suggestions.Add(new ClassificationSuggestion
                    {
                        Classification = type,
                        Confidence = confidence,
                        Reason = GetSuggestionReason(room, typeRule, normalizedDistance)
                    });
                }
            }

            return suggestions.OrderByDescending(s => s.Confidence).Take(maxSuggestions).ToList();
        }

        /// <summary>
        /// Validates the current classification configuration.
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            if (_settings == null)
                result.AddError("Classification settings are null");
            else
                result.Merge(_settings.Validate());

            if (_classifier == null)
                result.AddError("Room classifier is null");
            else
                result.Merge(_classifier.ValidateConfiguration());

            return result;
        }

        /// <summary>
        /// Updates the seed used for classification.
        /// </summary>
        /// <param name="seed">New seed to use</param>
        public void UpdateSeed(int seed)
        {
            _lastSeed = seed;
            _classifier = new RoomClassifier(_settings, seed);
            
            // Re-apply designer overrides to new classifier
            foreach (var overridePair in _designerOverrides)
            {
                _classifier.SetDesignerOverride(overridePair.Key, overridePair.Value);
            }
        }

        /// <summary>
        /// Gets the current seed being used.
        /// </summary>
        /// <returns>Current seed</returns>
        public int GetCurrentSeed()
        {
            return _lastSeed;
        }

        /// <summary>
        /// Exports current designer overrides to a serializable format.
        /// </summary>
        /// <returns>Export data for designer overrides</returns>
        public DesignerOverrideData ExportDesignerOverrides()
        {
            return new DesignerOverrideData
            {
                Overrides = _designerOverrides.ToList(),
                ExportTimestamp = DateTime.Now,
                Seed = _lastSeed
            };
        }

        /// <summary>
        /// Imports designer overrides from exported data.
        /// </summary>
        /// <param name="data">Export data to import</param>
        public void ImportDesignerOverrides(DesignerOverrideData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ClearAllDesignerOverrides();

            foreach (var overridePair in data.Overrides)
            {
                if (IsValidOfficeRoomType(overridePair.Value))
                {
                    SetDesignerOverride(overridePair.Key, overridePair.Value);
                }
            }

            if (data.Seed != 0)
            {
                UpdateSeed(data.Seed);
            }
        }
        #endregion

        #region Private Methods
        private static bool IsValidOfficeRoomType(RoomClassification type)
        {
            return type >= RoomClassification.Office && type <= RoomClassification.BossOffice;
        }

        private static IEnumerable<RoomClassification> GetValidOfficeRoomTypes()
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

        private float CalculateSuggestionConfidence(RoomData room, RoomTypeRule rule, float normalizedDistance)
        {
            float confidence = 0f;

            // Size compatibility
            var widthScore = 1f - Mathf.Abs(room.Bounds.width - rule.PreferredSize.x) / 
                           Mathf.Max(rule.MaxSize.x - rule.MinSize.x, 1f);
            var heightScore = 1f - Mathf.Abs(room.Bounds.height - rule.PreferredSize.y) / 
                            Mathf.Max(rule.MaxSize.y - rule.MinSize.y, 1f);
            var sizeScore = (widthScore + heightScore) / 2f;

            // Position compatibility
            var positionScore = rule.PositionPreference switch
            {
                PositionPreference.Center => 1f - normalizedDistance,
                PositionPreference.Edge => normalizedDistance,
                PositionPreference.Any => 0.5f,
                PositionPreference.Corner => normalizedDistance > 0.7f ? 1f : 0.3f,
                _ => 0.5f
            };

            // Priority bonus
            var priorityBonus = rule.Priority * 0.1f;

            confidence = (sizeScore * 0.5f) + (positionScore * 0.3f) + priorityBonus;

            return Mathf.Clamp01(confidence);
        }

        private string GetSuggestionReason(RoomData room, RoomTypeRule rule, float normalizedDistance)
        {
            var reasons = new List<string>();

            // Size reasons
            if (room.Bounds.width >= rule.MinSize.x && room.Bounds.height >= rule.MinSize.y &&
                room.Bounds.width <= rule.MaxSize.x && room.Bounds.height <= rule.MaxSize.y)
            {
                reasons.Add("Size fits requirements");
            }

            // Position reasons
            if (rule.PositionPreference == PositionPreference.Center && normalizedDistance < 0.3f)
                reasons.Add("Good central location");
            else if (rule.PositionPreference == PositionPreference.Edge && normalizedDistance > 0.6f)
                reasons.Add("Good edge location");

            // Priority reasons
            if (rule.Priority >= 3)
                reasons.Add("High priority room type");

            return reasons.Count > 0 ? string.Join(", ", reasons) : "General compatibility";
        }
        #endregion
    }

    /// <summary>
    /// Represents a classification suggestion with confidence score and reasoning.
    /// </summary>
    [Serializable]
    public class ClassificationSuggestion
    {
        public RoomClassification Classification;
        public float Confidence;
        public string Reason;
    }

    /// <summary>
    /// Serializable data for designer overrides.
    /// </summary>
    [Serializable]
    public class DesignerOverrideData
    {
        public List<KeyValuePair<int, RoomClassification>> Overrides;
        public DateTime ExportTimestamp;
        public int Seed;
    }
}