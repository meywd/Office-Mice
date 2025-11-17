using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Interfaces
{
    /// <summary>
    /// Comprehensive unit tests for IMapGenerator interface and mock implementation.
    /// Tests all interface methods, events, and edge cases.
    /// </summary>
    [TestFixture]
    public class IMapGeneratorTests
    {
        private MockMapGenerator _mapGenerator;
        private MapGenerationSettings _testSettings;

        [SetUp]
        public void SetUp()
        {
            _mapGenerator = new MockMapGenerator();
            _testSettings = new MapGenerationSettings();
        }

        [Test]
        public void GenerateMap_WithValidSettings_ReturnsMapData()
        {
            // Arrange
            var expectedMap = new MapData();
            _mapGenerator.SetMockMapData(expectedMap);

            // Act
            var result = _mapGenerator.GenerateMap(_testSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedMap, result);
        }

        [Test]
        public void GenerateMap_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapGenerator.GenerateMap(null));
        }

        [Test]
        public void GenerateMap_WithSeed_ReturnsDeterministicMap()
        {
            // Arrange
            var expectedMap = new MapData();
            _mapGenerator.SetMockMapData(expectedMap);

            // Act
            var result1 = _mapGenerator.GenerateMap(_testSettings, 123);
            var result2 = _mapGenerator.GenerateMap(_testSettings, 123);

            // Assert
            Assert.AreEqual(result1, result2);
        }

        [Test]
        public void GenerateMap_WithException_ThrowsAndFiresEvent()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            _mapGenerator.SetThrowException(true, expectedException);
            Exception firedException = null;
            _mapGenerator.OnGenerationFailed += (ex) => firedException = ex;

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _mapGenerator.GenerateMap(_testSettings));
            Assert.AreEqual(expectedException, thrownException);
            Assert.AreEqual(expectedException, firedException);
        }

        [UnityTest]
        public IEnumerator GenerateMapAsync_WithValidSettings_ReturnsMapData()
        {
            // Arrange
            var expectedMap = new MapData();
            _mapGenerator.SetMockMapData(expectedMap);

            // Act
            var enumerator = _mapGenerator.GenerateMapAsync(_testSettings);
            Assert.IsTrue(enumerator.MoveNext());
            var result = enumerator.Current;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedMap, result);
            yield break;
        }

        [UnityTest]
        public IEnumerator GenerateMapAsync_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
            {
                var enumerator = _mapGenerator.GenerateMapAsync(null);
                enumerator.MoveNext();
            });
            yield break;
        }

        [Test]
        public void ValidateSettings_WithValidSettings_ReturnsSuccess()
        {
            // Arrange
            _mapGenerator.SetMockValidationResult(ValidationResult.Success());

            // Act
            var result = _mapGenerator.ValidateSettings(_testSettings);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSettings_WithNullSettings_ReturnsFailure()
        {
            // Act
            var result = _mapGenerator.ValidateSettings(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Settings cannot be null"));
        }

        [Test]
        public void EstimateGenerationTime_WithValidSettings_ReturnsTime()
        {
            // Arrange
            var expectedTime = 150f;
            _mapGenerator.SetMockGenerationTime(expectedTime);

            // Act
            var result = _mapGenerator.EstimateGenerationTime(_testSettings);

            // Assert
            Assert.AreEqual(expectedTime, result);
        }

        [Test]
        public void EstimateGenerationTime_WithNullSettings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapGenerator.EstimateGenerationTime(null));
        }

        [Test]
        public void OnGenerationCompleted_EventFired_WhenMapGenerated()
        {
            // Arrange
            MapData firedMap = null;
            _mapGenerator.OnGenerationCompleted += (map) => firedMap = map;
            var expectedMap = new MapData();
            _mapGenerator.SetMockMapData(expectedMap);

            // Act
            _mapGenerator.GenerateMap(_testSettings);

            // Assert
            Assert.AreEqual(expectedMap, firedMap);
        }

        [Test]
        public void OnProgressUpdated_EventFired_DuringAsyncGeneration()
        {
            // Arrange
            var progressUpdates = new System.Collections.Generic.List<(float progress, string phase)>();
            _mapGenerator.OnProgressUpdated += (progress, phase) => progressUpdates.Add((progress, phase));

            // Act
            var enumerator = _mapGenerator.GenerateMapAsync(_testSettings);
            while (enumerator.MoveNext()) { }

            // Assert
            Assert.IsTrue(progressUpdates.Count > 0);
            Assert.IsTrue(progressUpdates[0].progress > 0);
            Assert.IsNotNull(progressUpdates[0].phase);
        }

        [Test]
        public void Interface_Contract_AllMethodsImplemented()
        {
            // Verify that MockMapGenerator properly implements IMapGenerator
            Assert.IsInstanceOf<IMapGenerator>(_mapGenerator);
            
            // Verify all required methods exist and are callable
            var mapGen = (IMapGenerator)_mapGenerator;
            
            Assert.DoesNotThrow(() => mapGen.ValidateSettings(_testSettings));
            Assert.DoesNotThrow(() => mapGen.EstimateGenerationTime(_testSettings));
            
            var asyncEnum = mapGen.GenerateMapAsync(_testSettings);
            Assert.IsNotNull(asyncEnum);
        }

        [Test]
        public void GenerateMap_WithDifferentSeeds_HandlesCorrectly()
        {
            // Arrange
            var expectedMap = new MapData();
            _mapGenerator.SetMockMapData(expectedMap);

            // Act
            var result1 = _mapGenerator.GenerateMap(_testSettings, 123);
            var result2 = _mapGenerator.GenerateMap(_testSettings, 456);

            // Assert
            // In mock implementation, seed doesn't affect the result, but method should handle it
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
        }

        [Test]
        public void Events_AllEvents_WorkCorrectly()
        {
            // Arrange
            bool completedFired = false;
            bool failedFired = false;
            bool progressFired = false;
            
            _mapGenerator.OnGenerationCompleted += (map) => completedFired = true;
            _mapGenerator.OnGenerationFailed += (ex) => failedFired = true;
            _mapGenerator.OnProgressUpdated += (progress, phase) => progressFired = true;

            // Act - Successful generation
            _mapGenerator.GenerateMap(_testSettings);
            
            // Assert - Success case
            Assert.IsTrue(completedFired, "OnGenerationCompleted should fire on success");
            Assert.IsFalse(failedFired, "OnGenerationFailed should not fire on success");
            
            // Reset
            completedFired = false;
            failedFired = false;
            
            // Act - Failed generation
            _mapGenerator.SetThrowException(true);
            try { _mapGenerator.GenerateMap(_testSettings); } catch { }
            
            // Assert - Failure case
            Assert.IsFalse(completedFired, "OnGenerationCompleted should not fire on failure");
            Assert.IsTrue(failedFired, "OnGenerationFailed should fire on failure");
            
            // Test progress event
            var enumerator = _mapGenerator.GenerateMapAsync(_testSettings);
            while (enumerator.MoveNext()) { }
            Assert.IsTrue(progressFired, "OnProgressUpdated should fire during async generation");
        }
    }
}