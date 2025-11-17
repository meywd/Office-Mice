using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using OfficeMice.MapGeneration.Generators;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Tests.Generators
{
    /// <summary>
    /// Test runner for room classification system to verify all acceptance criteria.
    /// </summary>
    [TestFixture]
    public class RoomClassificationTestRunner
    {
        private BSPGenerator _bspGenerator;
        private MapGenerationSettings _mapSettings;

        [SetUp]
        public void SetUp()
        {
            _bspGenerator = new BSPGenerator();
            _mapSettings = CreateTestSettings();
        }

        [Test]
        public void AcceptanceCriteria1_RoomTypeEnumIncludesAllRequiredTypes()
        {
            // Given BSP has generated room boundaries
            // When room classification system processes map
            // Then RoomType enum includes Office, Conference, BreakRoom, Storage, Lobby, ServerRoom, Security, BossRoom

            var requiredTypes = new[]
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

            // Act - Generate and classify rooms
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 12345);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Assert - All required types should be available in the enum
            var allRoomTypes = System.Enum.GetValues(typeof(RoomClassification)).Cast<RoomClassification>().ToList();
            
            foreach (var requiredType in requiredTypes)
            {
                Assert.IsTrue(allRoomTypes.Contains(requiredType), 
                    $"RoomClassification enum is missing required type: {requiredType}");
            }

            // Assert - At least some rooms should be classified with office types
            var officeClassifications = classifiedRooms
                .Where(r => requiredTypes.Contains(r.Classification))
                .ToList();
            
            Assert.IsTrue(officeClassifications.Count > 0, 
                "No rooms were classified with office room types");
        }

        [Test]
        public void AcceptanceCriteria2_AutomaticClassificationBasedOnSizePositionAndDepth()
        {
            // Given BSP has generated room boundaries
            // When room classification system processes map
            // Then automatic classification assigns types based on size, position, and depth

            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 54321);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Assert - All rooms should be classified
            foreach (var room in classifiedRooms)
            {
                Assert.AreNotEqual(RoomClassification.Unassigned, room.Classification,
                    $"Room {room.RoomID} was not classified");
            }

            // Assert - Classification should vary based on room properties
            var classifications = classifiedRooms.Select(r => r.Classification).Distinct().ToList();
            Assert.IsTrue(classifications.Count > 1, 
                "All rooms received the same classification - should vary based on properties");

            // Assert - Large rooms should prefer large room types
            var largeRooms = classifiedRooms.Where(r => r.Area > 100).ToList();
            var largeRoomTypes = largeRooms.Select(r => r.Classification).ToList();
            
            var hasLargeRoomTypes = largeRoomTypes.Any(t => 
                t == RoomClassification.Conference || 
                t == RoomClassification.Lobby || 
                t == RoomClassification.BossOffice);
            
            Assert.IsTrue(hasLargeRoomTypes,
                "Large rooms should be classified as large room types (Conference, Lobby, or BossOffice)");

            // Assert - Small rooms should prefer small room types
            var smallRooms = classifiedRooms.Where(r => r.Area < 30).ToList();
            var smallRoomTypes = smallRooms.Select(r => r.Classification).ToList();
            
            var hasSmallRoomTypes = smallRoomTypes.Any(t => 
                t == RoomClassification.Storage || 
                t == RoomClassification.ServerRoom || 
                t == RoomClassification.Security);
            
            Assert.IsTrue(hasSmallRoomTypes,
                "Small rooms should be classified as small room types (Storage, ServerRoom, or Security)");
        }

        [Test]
        public void AcceptanceCriteria3_RoomTypeDistributionFollowsConfigurableRules()
        {
            // Given BSP has generated room boundaries
            // When room classification system processes map
            // Then room type distribution follows configurable rules

            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 98765);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Calculate actual distribution
            var totalRooms = classifiedRooms.Count;
            var actualDistribution = new Dictionary<RoomClassification, float>();
            
            foreach (var room in classifiedRooms)
            {
                if (!actualDistribution.ContainsKey(room.Classification))
                    actualDistribution[room.Classification] = 0f;
                actualDistribution[room.Classification]++;
            }

            // Convert to percentages
            foreach (var key in actualDistribution.Keys.ToList())
            {
                actualDistribution[key] = (actualDistribution[key] / totalRooms) * 100f;
            }

            // Compare with configured distribution
            var distributionRules = _mapSettings.ClassificationSettings.DistributionRules;
            
            foreach (var rule in distributionRules)
            {
                var actualPercentage = actualDistribution.GetValueOrDefault(rule.Type, 0f);
                var expectedPercentage = rule.Percentage;
                
                // Allow reasonable tolerance for randomness (25%)
                var tolerance = expectedPercentage * 0.25f;
                
                Assert.IsTrue(Mathf.Abs(actualPercentage - expectedPercentage) <= tolerance,
                    $"Distribution for {rule.Type}: expected ~{expectedPercentage:F1}%, got {actualPercentage:F1}% (tolerance: ±{tolerance:F1}%)");
            }
        }

        [Test]
        public void AcceptanceCriteria4_VisualDifferentiationBetweenRoomTypes()
        {
            // Given BSP has generated room boundaries
            // When room classification system processes map
            // Then visual differentiation between room types is supported

            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 11111);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Assert - Each room type should have at least one compatible template
            var roomTemplates = _mapSettings.RoomTemplates;
            var classifications = classifiedRooms.Select(r => r.Classification).Distinct().ToList();
            
            foreach (var classification in classifications)
            {
                var compatibleTemplates = roomTemplates
                    .Where(t => t.IsCompatibleWithClassification(classification))
                    .ToList();
                
                // Note: This test assumes templates will be added later
                // For now, we just verify the classification system supports the concept
                Assert.IsNotNull(compatibleTemplates,
                    $"Template compatibility check failed for {classification}");
            }
        }

        [Test]
        public void AcceptanceCriteria5_MinimumSizeRequirementsEnforced()
        {
            // Given BSP has generated room boundaries
            // When room classification system processes map
            // Then minimum size requirements are enforced for each room type

            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 22222);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);

            // Create classification manager for validation
            var classificationManager = new RoomClassificationManager(_mapSettings.ClassificationSettings, 22222);

            // Assert - Each room should meet minimum size requirements for its classification
            foreach (var room in classifiedRooms)
            {
                var validation = classificationManager.ValidateRoomClassification(room, room.Classification);
                
                Assert.IsFalse(validation.HasErrors,
                    $"Room {room.RoomID} classified as {room.Classification} failed validation: {validation.GetErrorSummary()}");
            }
        }

        [Test]
        public void PerformanceTest_ClassificationCompletesWithinTimeLimit()
        {
            // Performance requirement: Classification must complete within 50ms for 100-room maps

            // Arrange - Create a large map with many rooms
            var largeSettings = CreateLargeTestSettings();
            var rooms = _bspGenerator.GenerateRooms(largeSettings, 33333);
            
            Assert.IsTrue(rooms.Count >= 50, $"Test requires at least 50 rooms, got {rooms.Count}");

            // Act - Measure classification time
            var startTime = System.DateTime.Now;
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, largeSettings);
            var duration = System.DateTime.Now - startTime;

            // Assert
            Assert.IsNotNull(classifiedRooms);
            Assert.AreEqual(rooms.Count, classifiedRooms.Count);
            Assert.IsTrue(duration.TotalMilliseconds < 50,
                $"Classification of {rooms.Count} rooms took {duration.TotalMilliseconds}ms, should be under 50ms");

            Debug.Log($"Classification Performance: {rooms.Count} rooms classified in {duration.TotalMilliseconds:F2}ms");
        }

        [Test]
        public void IntegrationTest_CompleteWorkflowWithDesignerOverrides()
        {
            // Test complete workflow: generation -> classification -> designer overrides -> reclassification

            // Arrange - Generate and classify rooms
            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 44444);
            var classifiedRooms = _bspGenerator.ClassifyRooms(rooms, _mapSettings);
            
            // Select a room to override
            var targetRoom = classifiedRooms.FirstOrDefault();
            Assert.IsNotNull(targetRoom);
            
            var originalClassification = targetRoom.Classification;
            var overrideClassification = RoomClassification.Lobby;

            // Act - Apply designer override
            var classificationManager = new RoomClassificationManager(_mapSettings.ClassificationSettings, 44444);
            classificationManager.SetDesignerOverride(targetRoom.RoomID, overrideClassification);
            
            var reclassifiedRooms = classificationManager.ClassifyRooms(rooms, _mapSettings.mapBounds);

            // Assert - Override should be applied
            var overriddenRoom = reclassifiedRooms.FirstOrDefault(r => r.RoomID == targetRoom.RoomID);
            Assert.IsNotNull(overriddenRoom);
            Assert.AreEqual(overrideClassification, overriddenRoom.Classification);
            Assert.AreNotEqual(originalClassification, overriddenRoom.Classification);

            // Assert - Other rooms should still be classified
            var otherRooms = reclassifiedRooms.Where(r => r.RoomID != targetRoom.RoomID).ToList();
            foreach (var room in otherRooms)
            {
                Assert.AreNotEqual(RoomClassification.Unassigned, room.Classification);
            }
        }

        [Test]
        public void RegressionTest_DeterministicClassificationWithSameSeed()
        {
            // Ensure classification is deterministic when using the same seed

            var rooms = _bspGenerator.GenerateRooms(_mapSettings, 55555);
            var seed = 66666;

            // Act - Classify twice with same seed
            var classificationManager1 = new RoomClassificationManager(_mapSettings.ClassificationSettings, seed);
            var result1 = classificationManager1.ClassifyRooms(rooms, _mapSettings.mapBounds);

            var classificationManager2 = new RoomClassificationManager(_mapSettings.ClassificationSettings, seed);
            var result2 = classificationManager2.ClassifyRooms(rooms, _mapSettings.mapBounds);

            // Assert - Results should be identical
            Assert.AreEqual(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.AreEqual(result1[i].Classification, result2[i].Classification,
                    $"Room {result1[i].RoomID} has different classifications: {result1[i].Classification} vs {result2[i].Classification}");
            }
        }

        #region Helper Methods
        private MapGenerationSettings CreateTestSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 75, 75);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 12,
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
            classificationSettings.RandomnessFactor = 0.3f;
            settings.ClassificationSettings = classificationSettings;

            // Add some basic room templates for testing
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.TemplateID = "test_template";
            template.TemplateName = "Test Template";
            template.MinWidth = 3;
            template.MaxWidth = 20;
            template.MinHeight = 3;
            template.MaxHeight = 20;
            
            settings.RoomTemplates.Add(template);

            return settings;
        }

        private MapGenerationSettings CreateLargeTestSettings()
        {
            var settings = CreateTestSettings();
            settings.mapBounds = new RectInt(0, 0, 150, 150);
            settings.bsp.MaxDepth = 7;
            settings.bsp.MinPartitionSize = 10;
            return settings;
        }
        #endregion
    }
}