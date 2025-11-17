using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;

namespace OfficeMice.MapGeneration.Tests.Serialization
{
    /// <summary>
    /// Tests for the SerializationValidator system.
    /// Validates comprehensive round-trip integrity checking.
    /// </summary>
    [TestFixture]
    public class SerializationValidatorTests
    {
        private SerializationValidator _validator;
        private MapSerializer _serializer;
        
        [SetUp]
        public void SetUp()
        {
            _serializer = new MapSerializer();
            _validator = new SerializationValidator(_serializer);
        }
        
        [TearDown]
        public void TearDown()
        {
            _validator = null;
            _serializer = null;
        }
        
        [Test]
        public void ValidateRoundTrip_ValidMap_ReturnsSuccess()
        {
            // Arrange
            var testMap = MapDataFactory.CreateSmallMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNotNull(result.JsonRoundTripResult);
            Assert.IsNotNull(result.BinaryRoundTripResult);
            Assert.IsNotNull(result.CompressionResult);
            Assert.IsNotNull(result.PerformanceResult);
        }
        
        [Test]
        public void ValidateRoundTrip_NullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.ValidateRoundTrip(null));
        }
        
        [Test]
        public void ValidateRoundTrip_ComplexMap_AllValidationRulesPass()
        {
            // Arrange
            var complexMap = MapDataFactory.CreateLargeMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(complexMap);
            
            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.JsonRoundTripResult.ValidationErrors.Count);
            Assert.AreEqual(0, result.BinaryRoundTripResult.ValidationErrors.Count);
        }
        
        [Test]
        public void JsonRoundTripResult_ValidMap_NoValidationErrors()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert
            Assert.IsTrue(result.JsonRoundTripResult.IsSuccess);
            Assert.AreEqual("JSON", result.JsonRoundTripResult.Format);
            Assert.Greater(result.JsonRoundTripResult.SerializedSize, 0);
            Assert.Greater(result.JsonRoundTripResult.SerializationTime.TotalMilliseconds, 0);
            Assert.Greater(result.JsonRoundTripResult.DeserializationTime.TotalMilliseconds, 0);
            Assert.AreEqual(0, result.JsonRoundTripResult.ValidationErrors.Count);
        }
        
        [Test]
        public void BinaryRoundTripResult_ValidMap_NoValidationErrors()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert
            Assert.IsTrue(result.BinaryRoundTripResult.IsSuccess);
            Assert.AreEqual("Binary", result.BinaryRoundTripResult.Format);
            Assert.Greater(result.BinaryRoundTripResult.SerializedSize, 0);
            Assert.Greater(result.BinaryRoundTripResult.SerializationTime.TotalMilliseconds, 0);
            Assert.Greater(result.BinaryRoundTripResult.DeserializationTime.TotalMilliseconds, 0);
            Assert.AreEqual(0, result.BinaryRoundTripResult.ValidationErrors.Count);
        }
        
        [Test]
        public void CompressionResult_ValidMap_EffectiveCompression()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert
            Assert.IsTrue(result.CompressionResult.IsSuccess);
            Assert.Greater(result.CompressionResult.SpaceSaved, 0);
            Assert.Less(result.CompressionResult.CompressionRatio, 1.0f);
            Assert.Greater(result.CompressionResult.CompressionTime.TotalMilliseconds, 0);
            Assert.Greater(result.CompressionResult.DecompressionTime.TotalMilliseconds, 0);
        }
        
        [Test]
        public void PerformanceResult_ValidMap_MeetsTargets()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert
            Assert.IsTrue(result.PerformanceResult.IsSuccess);
            Assert.Greater(result.PerformanceResult.AverageJsonTime.TotalMilliseconds, 0);
            Assert.Greater(result.PerformanceResult.AverageBinaryTime.TotalMilliseconds, 0);
        }
        
        [Test]
        public void ValidationTiming_AccurateMeasurement()
        {
            // Arrange
            var testMap = MapDataFactory.CreateSmallMap();
            var startTime = DateTime.UtcNow;
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            var endTime = DateTime.UtcNow;
            
            // Assert
            Assert.LessOrEqual(result.ValidationStartTime, startTime.AddSeconds(1));
            Assert.GreaterOrEqual(result.ValidationEndTime, endTime.AddSeconds(-1));
            Assert.Greater(result.TotalValidationTime.TotalMilliseconds, 0);
        }
        
        [Test]
        public void BasicPropertiesValidationRule_IdenticalMaps_NoErrors()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            var rule = new BasicPropertiesValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void BasicPropertiesValidationRule_DifferentSeed_ReturnsError()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            
            // Modify the seed using reflection (since it's read-only)
            var seedField = typeof(MapData).GetField("_seed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (seedField != null)
            {
                seedField.SetValue(deserialized, original.Seed + 1);
            }
            
            var rule = new BasicPropertiesValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(1, errors.Count);
            Assert.IsTrue(errors[0].Contains("Seed mismatch"));
        }
        
        [Test]
        public void CollectionIntegrityValidationRule_IdenticalMaps_NoErrors()
        {
            // Arrange
            var original = MapDataFactory.CreateMediumMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            var rule = new CollectionIntegrityValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void CollectionIntegrityValidationRule_DifferentRoomCount_ReturnsError()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var deserialized = MapDataFactory.CreateMediumMap(); // Different room count
            var rule = new CollectionIntegrityValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.Greater(errors.Count, 0);
            Assert.IsTrue(errors.Any(e => e.Contains("Rooms count mismatch")));
        }
        
        [Test]
        public void SpatialDataValidationRule_IdenticalMaps_NoErrors()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            var rule = new SpatialDataValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void GameplayDataValidationRule_IdenticalMaps_NoErrors()
        {
            // Arrange
            var original = MapDataFactory.CreateMediumMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            var rule = new GameplayDataValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void MetadataValidationRule_IdenticalMaps_NoErrors()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var deserialized = _serializer.DeserializeFromJson(_serializer.SerializeToJson(original));
            var rule = new MetadataValidationRule();
            
            // Act
            var errors = rule.Validate(original, deserialized);
            
            // Assert
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void ValidationErrorHandling_InvalidData_GracefulFailure()
        {
            // Arrange
            var original = MapDataFactory.CreateSmallMap();
            var corruptedData = "{ invalid json }";
            
            // Act & Assert
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromJson(corruptedData));
        }
        
        [Test]
        public void MultipleValidations_DifferentMaps_IndependentResults()
        {
            // Arrange
            var smallMap = MapDataFactory.CreateSmallMap();
            var mediumMap = MapDataFactory.CreateMediumMap();
            
            // Act
            var result1 = _validator.ValidateRoundTrip(smallMap);
            var result2 = _validator.ValidateRoundTrip(mediumMap);
            
            // Assert
            Assert.IsTrue(result1.IsSuccess);
            Assert.IsTrue(result2.IsSuccess);
            Assert.AreNotEqual(result1.JsonRoundTripResult.SerializedSize, result2.JsonRoundTripResult.SerializedSize);
        }
        
        [Test]
        public void ValidationComprehensive_AllRulesExecuted()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            
            // Act
            var result = _validator.ValidateRoundTrip(testMap);
            
            // Assert - Verify all validation components are present
            Assert.IsNotNull(result.JsonRoundTripResult);
            Assert.IsNotNull(result.BinaryRoundTripResult);
            Assert.IsNotNull(result.CompressionResult);
            Assert.IsNotNull(result.PerformanceResult);
            
            // Verify timing information is recorded
            Assert.Greater(result.TotalValidationTime.TotalMilliseconds, 0);
            Assert.IsNotNull(result.ValidationStartTime);
            Assert.IsNotNull(result.ValidationEndTime);
        }
    }
}