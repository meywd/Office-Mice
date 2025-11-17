using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using OfficeMice.MapGeneration.Generators;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Integration
{
    [TestFixture]
    public class RoomClassificationIntegrationTests
    {
        private BSPGenerator _bspGenerator;
        private MapGenerationSettings _mapSettings;
        private RoomClassificationManager _classificationManager;

        [SetUp]
        public void SetUp()
        {
            _bspGenerator = new BSPGenerator();
            _mapSettings = CreateMapSettings();
            _classificationManager = new RoomClassificationManager(_mapSettings.ClassificationSettings, 12345);
        }

        [Test]
        public void BSPGenerator_WithClassificationSettings_ClassifiesRoomsCorrectly()
        {
            // Arrange
            var seed = 54321;

            // Act
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, seed);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Assert
            Assert.IsNotNull(classifiedRooms);
            Assert.AreEqual(rooms.Count, classifiedRooms.Count);
            
            foreach (var room in classifiedRooms)
            {
                Assert.AreNotEqual(RoomClassification.Unassigned, room.Classification);
                Assert.IsTrue(IsOfficeRoomType(room.Classification));
            }
        }

        [Test]
        public void CompleteWorkflow_GenerateAndClassifyRooms_ProducesValidMap()
        {
            // Arrange
            var seed = 98765;

            // Act
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, seed);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Assert
            // Validate room placement
            var placementValidation = _bspGenerator.ValidateRoomPlacement(classifiedRooms, _mapSettings);
            Assert.IsFalse(placementValidation.HasErrors, $"Room placement errors: {placementValidation.GetErrorSummary()}");

            // Validate classifications
            var classifications = classifiedRooms.GroupBy(r => r.Classification).ToDictionary(g => g.Key, g => g.Count());
            Assert.IsTrue(classifications.Count > 0, "No room classifications assigned");

            // Validate distribution roughly follows rules
            var totalRooms = classifiedRooms.Count;
            foreach (var distRule in _mapSettings.ClassificationSettings.DistributionRules)
            {
                var expectedCount = Mathf.RoundToInt(totalRooms * distRule.Percentage / 100f);
                var actualCount = classifications.GetValueOrDefault(distRule.Type, 0);

                // Allow reasonable tolerance for randomness
                var tolerance = Mathf.Max(1, expectedCount * 0.5f);
                Assert.IsTrue(Mathf.Abs(actualCount - expectedCount) <= tolerance,
                    $"Distribution for {distRule.Type}: expected ~{expectedCount}, got {actualCount}");
            }
        }

        [Test]
        public void ClassificationManager_WithDesignerOverrides_OverridesAutomaticClassification()
        {
            // Arrange
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 11111);
            var targetRoom = rooms.FirstOrDefault();
            Assert.IsNotNull(targetRoom);

            var overrideType = RoomClassification.Lobby;

            // Act
            _classificationManager.SetDesignerOverride(targetRoom.RoomID, overrideType);
            var classifiedRooms = _classificationManager.ClassifyRooms(rooms, _mapSettings.mapBounds);

            // Assert
            var overriddenRoom = classifiedRooms.FirstOrDefault(r => r.RoomID == targetRoom.RoomID);
            Assert.IsNotNull(overriddenRoom);
            Assert.AreEqual(overrideType, overriddenRoom.Classification);
        }

        [Test]
        public void ClassificationManager_ExportImportDesignerOverrides_PreservesOverrides()
        {
            // Arrange
            _classificationManager.SetDesignerOverride(1, RoomClassification.Lobby);
            _classificationManager.SetDesignerOverride(2, RoomClassification.Conference);
            _classificationManager.SetDesignerOverride(3, RoomClassification.Storage);

            // Act
            var exportData = _classificationManager.ExportDesignerOverrides();
            var newManager = new RoomClassificationManager(_mapSettings.ClassificationSettings, 54321);
            newManager.ImportDesignerOverrides(exportData);

            // Assert
            var importedOverrides = newManager.GetAllDesignerOverrides();
            Assert.AreEqual(3, importedOverrides.Count);
            Assert.AreEqual(RoomClassification.Lobby, importedOverrides[1]);
            Assert.AreEqual(RoomClassification.Conference, importedOverrides[2]);
            Assert.AreEqual(RoomClassification.Storage, importedOverrides[3]);
            Assert.AreEqual(12345, newManager.GetCurrentSeed()); // Seed should be imported
        }

        [Test]
        public void RoomClassifier_WithPerformanceTest_CompletesWithinTimeLimit()
        {
            // Arrange
            var largeMapSettings = CreateLargeMapSettings();
            var rooms = _bspGenerator.GenerateRooms(largeMapSettings, 22222);
            var startTime = System.DateTime.Now;

            // Act
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, largeMapSettings);
            var duration = System.DateTime.Now - startTime;

            // Assert
            Assert.IsNotNull(classifiedRooms);
            Assert.AreEqual(rooms.Count, classifiedRooms.Count);
            Assert.IsTrue(duration.TotalMilliseconds < 50, 
                $"Classification took {duration.TotalMilliseconds}ms, should be under 50ms");
        }

        [Test]
        public void ClassificationSystem_WithDifferentSeeds_ProducesDifferentClassifications()
        {
            // Arrange
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 33333);

            // Act
            var classifier1 = new RoomClassificationManager(_mapSettings.ClassificationSettings, 11111);
            var result1 = classifier1.ClassifyRooms(rooms, _mapSettings.mapBounds);

            var classifier2 = new RoomClassificationManager(_mapSettings.ClassificationSettings, 22222);
            var result2 = classifier2.ClassifyRooms(rooms, _mapSettings.mapBounds);

            // Assert
            bool hasDifference = false;
            for (int i = 0; i < result1.Count; i++)
            {
                if (result1[i].Classification != result2[i].Classification)
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Different seeds should produce different classifications");
        }

        [Test]
        public void ClassificationSystem_WithValidation_AllowsValidRoomTypes()
        {
            // Arrange
            var validRoom = new RoomData(new RectInt(10, 10, 8, 8)) { RoomID = 1 };
            var validTypes = new[]
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

            // Act & Assert
            foreach (var type in validTypes)
            {
                var result = _classificationManager.ValidateRoomClassification(validRoom, type);
                Assert.IsFalse(result.HasErrors, $"Validation failed for valid type {type}: {result.GetErrorSummary()}");
            }
        }

        [Test]
        public void ClassificationSystem_WithValidation_RejectsInvalidRoomTypes()
        {
            // Arrange
            var room = new RoomData(new RectInt(10, 10, 8, 8)) { RoomID = 1 };
            var invalidTypes = new[]
            {
                RoomClassification.PlayerStart,
                RoomClassification.SafeRoom,
                RoomClassification.StandardRoom,
                RoomClassification.ArenaRoom,
                RoomClassification.BossRoom,
                RoomClassification.SecretRoom,
                RoomClassification.TransitionRoom,
                RoomClassification.AmbushRoom
            };

            // Act & Assert
            foreach (var type in invalidTypes)
            {
                var result = _classificationManager.ValidateRoomClassification(room, type);
                Assert.IsTrue(result.HasErrors, $"Validation should fail for invalid type {type}");
            }
        }

        [Test]
        public void ClassificationSystem_WithSizeValidation_EnforcesMinimumSizes()
        {
            // Arrange
            var smallRoom = new RoomData(new RectInt(10, 10, 2, 2)) { RoomID = 1 }; // Too small for most types

            // Act
            var result = _classificationManager.ValidateRoomClassification(smallRoom, RoomClassification.Conference);

            // Assert
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.GetErrorSummary().Contains("below minimum"));
        }

        [Test]
        public void ClassificationSystem_WithSuggestions_ProvidesReasonableSuggestions()
        {
            // Arrange
            var largeCenterRoom = new RoomData(new RectInt(45, 45, 15, 15)) { RoomID = 1 };
            var smallEdgeRoom = new RoomData(new RectInt(1, 1, 4, 4)) { RoomID = 2 };

            // Act
            var suggestions1 = _classificationManager.GetClassificationSuggestions(largeCenterRoom, _mapSettings.mapBounds);
            var suggestions2 = _classificationManager.GetClassificationSuggestions(smallEdgeRoom, _mapSettings.mapBounds);

            // Assert
            Assert.IsTrue(suggestions1.Count > 0);
            Assert.IsTrue(suggestions2.Count > 0);

            // Large center room should suggest large, center-preferred types
            var largeCenterTypes = suggestions1
                .Where(s => s.Confidence > 0.5f)
                .Select(s => s.Classification)
                .ToList();
            Assert.IsTrue(largeCenterTypes.Contains(RoomClassification.Lobby) || 
                         largeCenterTypes.Contains(RoomClassification.Conference));

            // Small edge room should suggest small, edge-preferred types
            var smallEdgeTypes = suggestions2
                .Where(s => s.Confidence > 0.5f)
                .Select(s => s.Classification)
                .ToList();
            Assert.IsTrue(smallEdgeTypes.Contains(RoomClassification.Storage) || 
                         smallEdgeTypes.Contains(RoomClassification.Security));
        }

        [Test]
        public void ClassificationSystem_WithMultipleRuns_MaintainsDistributionBalance()
        {
            // Arrange
            var runs = 10;
            var distributionTotals = new Dictionary<RoomClassification, int>();

            // Act
            for (int i = 0; i < runs; i++)
            {
                var rooms = _bspGenerator.GenerateRooms(_mapSettings, i * 1000);
                var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

                foreach (var room in classifiedRooms)
                {
                    if (!distributionTotals.ContainsKey(room.Classification))
                        distributionTotals[room.Classification] = 0;
                    distributionTotals[room.Classification]++;
                }
            }

            // Assert
            var totalRooms = distributionTotals.Values.Sum();
            foreach (var distRule in _mapSettings.ClassificationSettings.DistributionRules)
            {
                var actualPercentage = (float)distributionTotals.GetValueOrDefault(distRule.Type, 0) / totalRooms * 100f;
                var expectedPercentage = distRule.Percentage;

                // Allow 20% tolerance across multiple runs
                var tolerance = expectedPercentage * 0.2f;
                Assert.IsTrue(Mathf.Abs(actualPercentage - expectedPercentage) <= tolerance,
                    $"Distribution for {distRule.Type}: expected ~{expectedPercentage}%, got {actualPercentage:F1}% across {runs} runs");
            }
        }

        #region Helper Methods
        private MapGenerationSettings CreateMapSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 100, 100);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 5,
                SplitPreference = SplitPreference.Alternate,
                SplitPositionVariation = 0.3f,
                StopSplittingChance = 0.1f,
                RoomSizeRatio = 0.8f,
                RoomPositionVariation = 0.1f
            };

            // Create classification settings
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            settings.ClassificationSettings = classificationSettings;

            return settings;
        }

        private MapGenerationSettings CreateLargeMapSettings()
        {
            var settings = CreateMapSettings();
            settings.mapBounds = new RectInt(0, 0, 200, 200);
            settings.bsp.MaxDepth = 7;
            return settings;
        }

        private static bool IsOfficeRoomType(RoomClassification type)
        {
            return type >= RoomClassification.Office && type <= RoomClassification.BossOffice;
        }
        #endregion
    }
}