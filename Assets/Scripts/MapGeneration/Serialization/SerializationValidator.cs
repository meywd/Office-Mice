using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Comprehensive validation system for serialization round-trip integrity.
    /// Ensures 100% data accuracy during serialization/deserialization cycles.
    /// </summary>
    public class SerializationValidator
    {
        private readonly IMapSerializer _serializer;
        private readonly List<IValidationRule> _validationRules;
        
        public SerializationValidator(IMapSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _validationRules = new List<IValidationRule>
            {
                new BasicPropertiesValidationRule(),
                new CollectionIntegrityValidationRule(),
                new SpatialDataValidationRule(),
                new GameplayDataValidationRule(),
                new MetadataValidationRule()
            };
        }
        
        /// <summary>
        /// Performs comprehensive round-trip validation of map serialization.
        /// </summary>
        /// <param name="originalMap">The original map to validate</param>
        /// <returns>Detailed validation result</returns>
        public SerializationValidationResult ValidateRoundTrip(MapData originalMap)
        {
            if (originalMap == null)
                throw new ArgumentNullException(nameof(originalMap));
                
            var result = new SerializationValidationResult
            {
                OriginalMap = originalMap,
                ValidationStartTime = DateTime.UtcNow
            };
            
            try
            {
                // Test JSON round-trip
                result.JsonRoundTripResult = ValidateJsonRoundTrip(originalMap);
                
                // Test binary round-trip
                result.BinaryRoundTripResult = ValidateBinaryRoundTrip(originalMap);
                
                // Test compression integrity
                result.CompressionResult = ValidateCompression(originalMap);
                
                // Test performance
                result.PerformanceResult = ValidatePerformance(originalMap);
                
                // Overall success
                result.IsSuccess = result.JsonRoundTripResult.IsSuccess &&
                                  result.BinaryRoundTripResult.IsSuccess &&
                                  result.CompressionResult.IsSuccess;
                                  
                result.ValidationEndTime = DateTime.UtcNow;
                result.TotalValidationTime = result.ValidationEndTime - result.ValidationStartTime;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ValidationEndTime = DateTime.UtcNow;
                result.TotalValidationTime = result.ValidationEndTime - result.ValidationStartTime;
                return result;
            }
        }
        
        /// <summary>
        /// Validates JSON serialization round-trip.
        /// </summary>
        private RoundTripValidationResult ValidateJsonRoundTrip(MapData originalMap)
        {
            var result = new RoundTripValidationResult { Format = "JSON" };
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Serialize to JSON
                string json = _serializer.SerializeToJson(originalMap);
                result.SerializedSize = json.Length;
                result.SerializationTime = stopwatch.Elapsed;
                
                // Deserialize from JSON
                stopwatch.Restart();
                MapData deserializedMap = _serializer.DeserializeFromJson(json);
                result.DeserializationTime = stopwatch.Elapsed;
                
                // Validate integrity
                result.ValidationErrors = ValidateMapIntegrity(originalMap, deserializedMap);
                result.IsSuccess = result.ValidationErrors.Count == 0;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validates binary serialization round-trip.
        /// </summary>
        private RoundTripValidationResult ValidateBinaryRoundTrip(MapData originalMap)
        {
            var result = new RoundTripValidationResult { Format = "Binary" };
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Serialize to binary
                byte[] binary = _serializer.SerializeToBinary(originalMap);
                result.SerializedSize = binary.Length;
                result.SerializationTime = stopwatch.Elapsed;
                
                // Deserialize from binary
                stopwatch.Restart();
                MapData deserializedMap = _serializer.DeserializeFromBinary(binary);
                result.DeserializationTime = stopwatch.Elapsed;
                
                // Validate integrity
                result.ValidationErrors = ValidateMapIntegrity(originalMap, deserializedMap);
                result.IsSuccess = result.ValidationErrors.Count == 0;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validates compression effectiveness and integrity.
        /// </summary>
        private CompressionValidationResult ValidateCompression(MapData originalMap)
        {
            var result = new CompressionValidationResult();
            
            try
            {
                // Test with compression enabled
                var settingsWithCompression = new SerializationSettings { EnableCompression = true };
                var serializerWithCompression = new MapSerializer(settings: settingsWithCompression);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                byte[] compressedBinary = serializerWithCompression.SerializeToBinary(originalMap);
                result.CompressionTime = stopwatch.Elapsed;
                
                // Test decompression
                stopwatch.Restart();
                MapData decompressedMap = serializerWithCompression.DeserializeFromBinary(compressedBinary);
                result.DecompressionTime = stopwatch.Elapsed;
                
                // Calculate compression ratio
                var serializerWithoutCompression = new MapSerializer(settings: new SerializationSettings { EnableCompression = false });
                byte[] uncompressedBinary = serializerWithoutCompression.SerializeToBinary(originalMap);
                
                result.CompressionRatio = (float)compressedBinary.Length / uncompressedBinary.Length;
                result.SpaceSaved = uncompressedBinary.Length - compressedBinary.Length;
                
                // Validate decompression integrity
                var decompressionErrors = ValidateMapIntegrity(originalMap, decompressedMap);
                result.IsSuccess = decompressionErrors.Count == 0;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validates serialization performance against targets.
        /// </summary>
        private PerformanceValidationResult ValidatePerformance(MapData originalMap)
        {
            var result = new PerformanceValidationResult();
            
            try
            {
                const int iterations = 10;
                var jsonTimes = new List<TimeSpan>();
                var binaryTimes = new List<TimeSpan>();
                
                // Performance test for JSON
                for (int i = 0; i < iterations; i++)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    string json = _serializer.SerializeToJson(originalMap);
                    MapData deserialized = _serializer.DeserializeFromJson(json);
                    stopwatch.Stop();
                    jsonTimes.Add(stopwatch.Elapsed);
                }
                
                // Performance test for binary
                for (int i = 0; i < iterations; i++)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    byte[] binary = _serializer.SerializeToBinary(originalMap);
                    MapData deserialized = _serializer.DeserializeFromBinary(binary);
                    stopwatch.Stop();
                    binaryTimes.Add(stopwatch.Elapsed);
                }
                
                // Calculate averages
                result.AverageJsonTime = TimeSpan.FromTicks((long)jsonTimes.Average(t => t.Ticks));
                result.AverageBinaryTime = TimeSpan.FromTicks((long)binaryTimes.Average(t => t.Ticks));
                
                // Check against performance targets (from context: 3000ms total generation budget)
                var targetTime = TimeSpan.FromMilliseconds(500); // Allocate 500ms for serialization
                result.MeetsPerformanceTarget = result.AverageJsonTime <= targetTime && 
                                               result.AverageBinaryTime <= targetTime;
                
                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Validates map integrity using all registered validation rules.
        /// </summary>
        private List<string> ValidateMapIntegrity(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            foreach (var rule in _validationRules)
            {
                try
                {
                    var ruleErrors = rule.Validate(original, deserialized);
                    errors.AddRange(ruleErrors);
                }
                catch (Exception ex)
                {
                    errors.Add($"Validation rule '{rule.GetType().Name}' failed: {ex.Message}");
                }
            }
            
            return errors;
        }
    }
    
    /// <summary>
    /// Comprehensive result of serialization validation.
    /// </summary>
    public class SerializationValidationResult
    {
        public MapData OriginalMap { get; set; }
        public DateTime ValidationStartTime { get; set; }
        public DateTime ValidationEndTime { get; set; }
        public TimeSpan TotalValidationTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public RoundTripValidationResult JsonRoundTripResult { get; set; }
        public RoundTripValidationResult BinaryRoundTripResult { get; set; }
        public CompressionValidationResult CompressionResult { get; set; }
        public PerformanceValidationResult PerformanceResult { get; set; }
    }
    
    /// <summary>
    /// Result of a round-trip validation test.
    /// </summary>
    public class RoundTripValidationResult
    {
        public string Format { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int SerializedSize { get; set; }
        public TimeSpan SerializationTime { get; set; }
        public TimeSpan DeserializationTime { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Result of compression validation.
    /// </summary>
    public class CompressionValidationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public float CompressionRatio { get; set; }
        public int SpaceSaved { get; set; }
        public TimeSpan CompressionTime { get; set; }
        public TimeSpan DecompressionTime { get; set; }
    }
    
    /// <summary>
    /// Result of performance validation.
    /// </summary>
    public class PerformanceValidationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan AverageJsonTime { get; set; }
        public TimeSpan AverageBinaryTime { get; set; }
        public bool MeetsPerformanceTarget { get; set; }
    }
    
    /// <summary>
    /// Interface for validation rules used in round-trip validation.
    /// </summary>
    public interface IValidationRule
    {
        List<string> Validate(MapData original, MapData deserialized);
    }
}