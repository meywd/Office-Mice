using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Configuration
{
    [TestFixture]
    public class RoomClassificationSettingsTests
    {
        private RoomClassificationSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            _settings.SettingsID = "test_settings";
            _settings.SettingsName = "Test Settings";
        }

        [Test]
        public void Validate_WithValidSettings_ReturnsNoErrors()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsFalse(result.HasErrors, $"Validation errors: {result.GetErrorSummary()}");
        }

        [Test]
        public void Validate_WithMissingID_ReturnsError()
        {
            // Arrange
            _settings.SettingsID = "";

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("Settings ID is required"));
        }

        [Test]
        public void Validate_WithIDContainingSpaces_ReturnsError()
        {
            // Arrange
            _settings.SettingsID = "test settings";

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("Settings ID cannot contain spaces"));
        }

        [Test]
        public void Validate_WithNoTypeRules_ReturnsError()
        {
            // Arrange
            _settings.TypeRules.Clear();

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("At least one type rule is required"));
        }

        [Test]
        public void Validate_WithDuplicateTypeRules_ReturnsError()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();
            _settings.AddTypeRule(new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(8, 8),
                PreferredSize = new Vector2Int(6, 6),
                Priority = 1
            });

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("Duplicate type rule"));
        }

        [Test]
        public void Validate_WithDistributionNotSummingTo100_ReturnsWarning()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();
            _settings.DistributionRules.Clear();
            _settings.DistributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 50f,
                MinCount = 1,
                MaxCount = 10
            });
            _settings.DistributionRules.Add(new DistributionRule
            {
                Type = RoomClassification.Conference,
                Percentage = 30f,
                MinCount = 0,
                MaxCount = 3
            });
            // Total is 80%, not 100%

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasWarnings);
            Assert.IsTrue(result.GetWarningSummary().Contains("80%"));
        }

        [Test]
        public void Validate_WithInvalidRandomnessFactor_ReturnsError()
        {
            // Arrange
            _settings.RandomnessFactor = 1.5f; // Invalid, should be 0-1

            // Act
            var result = _settings.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("Randomness factor must be between 0 and 1"));
        }

        [Test]
        public void GetTypeRule_WithExistingType_ReturnsRule()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            var rule = _settings.GetTypeRule(RoomClassification.Office);

            // Assert
            Assert.IsNotNull(rule);
            Assert.AreEqual(RoomClassification.Office, rule.Type);
        }

        [Test]
        public void GetTypeRule_WithNonExistingType_ReturnsNull()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            var rule = _settings.GetTypeRule(RoomClassification.PlayerStart); // Not in office types

            // Assert
            Assert.IsNull(rule);
        }

        [Test]
        public void GetDistributionRule_WithExistingType_ReturnsRule()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            var rule = _settings.GetDistributionRule(RoomClassification.Office);

            // Assert
            Assert.IsNotNull(rule);
            Assert.AreEqual(RoomClassification.Office, rule.Type);
        }

        [Test]
        public void GetDistributionRule_WithNonExistingType_ReturnsNull()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            var rule = _settings.GetDistributionRule(RoomClassification.PlayerStart); // Not in office types

            // Assert
            Assert.IsNull(rule);
        }

        [Test]
        public void AddTypeRule_WithNewRule_AddsRule()
        {
            // Arrange
            var newRule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(5, 5),
                MaxSize = new Vector2Int(10, 10),
                PreferredSize = new Vector2Int(7, 7),
                Priority = 2
            };

            // Act
            _settings.AddTypeRule(newRule);

            // Assert
            var retrievedRule = _settings.GetTypeRule(RoomClassification.Office);
            Assert.IsNotNull(retrievedRule);
            Assert.AreEqual(5, retrievedRule.MinSize.x);
            Assert.AreEqual(5, retrievedRule.MinSize.y);
        }

        [Test]
        public void AddTypeRule_WithDuplicateType_DoesNotAdd()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();
            var duplicateRule = new RoomTypeRule
            {
                Type = RoomClassification.Office, // Already exists
                MinSize = new Vector2Int(5, 5),
                MaxSize = new Vector2Int(10, 10),
                PreferredSize = new Vector2Int(7, 7),
                Priority = 2
            };

            // Act
            _settings.AddTypeRule(duplicateRule);

            // Assert
            var rules = _settings.TypeRules.Where(r => r.Type == RoomClassification.Office).ToList();
            Assert.AreEqual(1, rules.Count); // Should still only have one
        }

        [Test]
        public void RemoveTypeRule_WithExistingType_RemovesRule()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            _settings.RemoveTypeRule(RoomClassification.Office);

            // Assert
            var rule = _settings.GetTypeRule(RoomClassification.Office);
            Assert.IsNull(rule);
        }

        [Test]
        public void AddDistributionRule_WithNewRule_AddsRule()
        {
            // Arrange
            var newRule = new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 25f,
                MinCount = 1,
                MaxCount = 5
            };

            // Act
            _settings.AddDistributionRule(newRule);

            // Assert
            var retrievedRule = _settings.GetDistributionRule(RoomClassification.Office);
            Assert.IsNotNull(retrievedRule);
            Assert.AreEqual(25f, retrievedRule.Percentage);
        }

        [Test]
        public void RemoveDistributionRule_WithExistingType_RemovesRule()
        {
            // Arrange
            _settings.CreateDefaultConfiguration();

            // Act
            _settings.RemoveDistributionRule(RoomClassification.Office);

            // Assert
            var rule = _settings.GetDistributionRule(RoomClassification.Office);
            Assert.IsNull(rule);
        }

        [Test]
        public void CreateDefaultConfiguration_CreatesAllOfficeTypes()
        {
            // Arrange & Act
            _settings.CreateDefaultConfiguration();

            // Assert
            var officeTypes = new[]
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

            foreach (var type in officeTypes)
            {
                var typeRule = _settings.GetTypeRule(type);
                Assert.IsNotNull(typeRule, $"Missing type rule for {type}");
                
                var distributionRule = _settings.GetDistributionRule(type);
                Assert.IsNotNull(distributionRule, $"Missing distribution rule for {type}");
            }
        }

        [Test]
        public void CreateDefaultConfiguration_DistributionSumsTo100()
        {
            // Arrange & Act
            _settings.CreateDefaultConfiguration();

            // Assert
            var totalPercentage = _settings.DistributionRules.Sum(dr => dr.Percentage);
            Assert.AreEqual(100f, totalPercentage, 0.01f);
        }

        [Test]
        public void OnValidate_WithEmptyNameAndID_GeneratesID()
        {
            // Arrange
            _settings.SettingsID = "";
            _settings.SettingsName = "Test Configuration";

            // Act
            _settings.OnValidate();

            // Assert
            Assert.AreEqual("test_configuration", _settings.SettingsID);
        }

        [Test]
        public void OnValidate_WithInvalidValues_ClampsValues()
        {
            // Arrange
            _settings.RandomnessFactor = 1.5f;
            _settings.MaxCacheSize = 0;

            // Act
            _settings.OnValidate();

            // Assert
            Assert.AreEqual(1f, _settings.RandomnessFactor);
            Assert.AreEqual(1, _settings.MaxCacheSize);
        }
    }

    [TestFixture]
    public class RoomTypeRuleTests
    {
        [Test]
        public void Validate_WithValidRule_ReturnsNoErrors()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(12, 12),
                PreferredSize = new Vector2Int(6, 8),
                PositionPreference = PositionPreference.Any,
                DepthPreference = DepthPreference.Any,
                Priority = 1
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void Validate_WithInvalidRoomType_ReturnsError()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.PlayerStart, // Not an office room type
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(12, 12),
                PreferredSize = new Vector2Int(6, 8),
                Priority = 1
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("not a valid office room type"));
        }

        [Test]
        public void Validate_WithMinSizeTooSmall_ReturnsError()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(2, 4), // Too small (minimum 3x3)
                MaxSize = new Vector2Int(12, 12),
                PreferredSize = new Vector2Int(6, 8),
                Priority = 1
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("must be at least 3x3"));
        }

        [Test]
        public void Validate_WithMaxSizeSmallerThanMin_ReturnsError()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(8, 8),
                MaxSize = new Vector2Int(6, 6), // Smaller than min
                PreferredSize = new Vector2Int(7, 7),
                Priority = 1
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("must be greater than or equal to minimum size"));
        }

        [Test]
        public void Validate_WithPreferredSizeOutsideRange_ReturnsError()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(8, 8),
                PreferredSize = new Vector2Int(10, 10), // Outside range
                Priority = 1
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("must be within min-max range"));
        }

        [Test]
        public void Validate_WithNegativePriority_ReturnsError()
        {
            // Arrange
            var rule = new RoomTypeRule
            {
                Type = RoomClassification.Office,
                MinSize = new Vector2Int(4, 4),
                MaxSize = new Vector2Int(12, 12),
                PreferredSize = new Vector2Int(6, 8),
                Priority = -1 // Negative
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("cannot be negative"));
        }
    }

    [TestFixture]
    public class DistributionRuleTests
    {
        [Test]
        public void Validate_WithValidRule_ReturnsNoErrors()
        {
            // Arrange
            var rule = new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 25f,
                MinCount = 1,
                MaxCount = 10
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void Validate_WithInvalidRoomType_ReturnsError()
        {
            // Arrange
            var rule = new DistributionRule
            {
                Type = RoomClassification.PlayerStart, // Not an office room type
                Percentage = 25f,
                MinCount = 1,
                MaxCount = 10
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("not a valid office room type"));
        }

        [Test]
        public void Validate_WithPercentageOutOfRange_ReturnsError()
        {
            // Arrange
            var rule = new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 150f, // Over 100%
                MinCount = 1,
                MaxCount = 10
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("must be between 0 and 100"));
        }

        [Test]
        public void Validate_WithNegativeMinCount_ReturnsError()
        {
            // Arrange
            var rule = new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 25f,
                MinCount = -1, // Negative
                MaxCount = 10
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("cannot be negative"));
        }

        [Test]
        public void Validate_WithMaxCountSmallerThanMin_ReturnsError()
        {
            // Arrange
            var rule = new DistributionRule
            {
                Type = RoomClassification.Office,
                Percentage = 25f,
                MinCount = 10,
                MaxCount = 5 // Smaller than min
            };

            // Act
            var result = rule.Validate();

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("must be greater than or equal to min count"));
        }
    }
}