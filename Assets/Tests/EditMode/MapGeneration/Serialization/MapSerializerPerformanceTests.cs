using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;
using UnityEngine.PerformanceTesting;

namespace OfficeMice.MapGeneration.Tests.Serialization
{
    /// <summary>
    /// Performance tests for map serialization system.
    /// Validates that serialization meets performance targets defined in the context.
    /// </summary>
    [TestFixture]
    public class MapSerializerPerformanceTests
    {
        private MapSerializer _serializer;
        private MapData[] _testMaps;
        
        [SetUp]
        public void SetUp()
        {
            _serializer = new MapSerializer();
            _testMaps = CreateTestMaps();
        }
        
        [TearDown]
        public void TearDown()
        {
            _serializer = null;
            _testMaps = null;
        }
        
        [Test]
        [Performance]
        public void SerializeToJson_SmallMap_PerformanceTarget()
        {
            var smallMap = _testMaps[0]; // 10 rooms
            
            Measure.Method(() => _serializer.SerializeToJson(smallMap))
                .WarmupCount(5)
                .MeasurementCount(20)
                .SampleGroup("JSON_Serialization_SmallMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(50)); // Target: 50ms for small map
        }
        
        [Test]
        [Performance]
        public void DeserializeFromJson_SmallMap_PerformanceTarget()
        {
            var smallMap = _testMaps[0]; // 10 rooms
            string json = _serializer.SerializeToJson(smallMap);
            
            Measure.Method(() => _serializer.DeserializeFromJson(json))
                .WarmupCount(5)
                .MeasurementCount(20)
                .SampleGroup("JSON_Deserialization_SmallMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(30)); // Target: 30ms for small map
        }
        
        [Test]
        [Performance]
        public void SerializeToBinary_SmallMap_PerformanceTarget()
        {
            var smallMap = _testMaps[0]; // 10 rooms
            
            Measure.Method(() => _serializer.SerializeToBinary(smallMap))
                .WarmupCount(5)
                .MeasurementCount(20)
                .SampleGroup("Binary_Serialization_SmallMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(40)); // Target: 40ms for small map
        }
        
        [Test]
        [Performance]
        public void DeserializeFromBinary_SmallMap_PerformanceTarget()
        {
            var smallMap = _testMaps[0]; // 10 rooms
            byte[] binary = _serializer.SerializeToBinary(smallMap);
            
            Measure.Method(() => _serializer.DeserializeFromBinary(binary))
                .WarmupCount(5)
                .MeasurementCount(20)
                .SampleGroup("Binary_Deserialization_SmallMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(25)); // Target: 25ms for small map
        }
        
        [Test]
        [Performance]
        public void SerializeToJson_MediumMap_PerformanceTarget()
        {
            var mediumMap = _testMaps[1]; // 50 rooms
            
            Measure.Method(() => _serializer.SerializeToJson(mediumMap))
                .WarmupCount(3)
                .MeasurementCount(10)
                .SampleGroup("JSON_Serialization_MediumMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(200)); // Target: 200ms for medium map
        }
        
        [Test]
        [Performance]
        public void SerializeToBinary_MediumMap_PerformanceTarget()
        {
            var mediumMap = _testMaps[1]; // 50 rooms
            
            Measure.Method(() => _serializer.SerializeToBinary(mediumMap))
                .WarmupCount(3)
                .MeasurementCount(10)
                .SampleGroup("Binary_Serialization_MediumMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(150)); // Target: 150ms for medium map
        }
        
        [Test]
        [Performance]
        public void SerializeToJson_LargeMap_PerformanceTarget()
        {
            var largeMap = _testMaps[2]; // 100 rooms
            
            Measure.Method(() => _serializer.SerializeToJson(largeMap))
                .WarmupCount(2)
                .MeasurementCount(5)
                .SampleGroup("JSON_Serialization_LargeMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(400)); // Target: 400ms for large map
        }
        
        [Test]
        [Performance]
        public void SerializeToBinary_LargeMap_PerformanceTarget()
        {
            var largeMap = _testMaps[2]; // 100 rooms
            
            Measure.Method(() => _serializer.SerializeToBinary(largeMap))
                .WarmupCount(2)
                .MeasurementCount(5)
                .SampleGroup("Binary_Serialization_LargeMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(300)); // Target: 300ms for large map
        }
        
        [Test]
        [Performance]
        public void ValidateRoundTrip_SmallMap_PerformanceTarget()
        {
            var smallMap = _testMaps[0]; // 10 rooms
            
            Measure.Method(() => _serializer.ValidateRoundTrip(smallMap))
                .WarmupCount(3)
                .MeasurementCount(10)
                .SampleGroup("RoundTrip_Validation_SmallMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(100)); // Target: 100ms for small map
        }
        
        [Test]
        [Performance]
        public void Compression_LargeMap_PerformanceTarget()
        {
            var largeMap = _testMaps[2]; // 100 rooms
            var settings = new SerializationSettings { EnableCompression = true };
            var serializerWithCompression = new MapSerializer(settings: settings);
            
            Measure.Method(() => serializerWithCompression.SerializeToBinary(largeMap))
                .WarmupCount(2)
                .MeasurementCount(5)
                .SampleGroup("Compression_LargeMap")
                .GC()
                .Define()
                .AssertAverageTime(TimeSpan.FromMilliseconds(350)); // Target: 350ms for large map with compression
        }
        
        [Test]
        public void MemoryUsage_Serialization_WithinBudget()
        {
            var largeMap = _testMaps[2]; // 100 rooms
            
            // Measure memory before serialization
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memoryBefore = GC.GetTotalMemory(false);
            
            // Perform serialization
            string json = _serializer.SerializeToJson(largeMap);
            byte[] binary = _serializer.SerializeToBinary(largeMap);
            
            // Measure memory after serialization
            long memoryAfter = GC.GetTotalMemory(false);
            long memoryUsed = memoryAfter - memoryBefore;
            
            // Memory budget from context: under 200MB total, GC pressure under 500KB per frame
            Assert.Less(memoryUsed, 50 * 1024 * 1024, $"Serialization used {memoryUsed / (1024 * 1024)}MB, should be under 50MB");
            
            // Clean up
            json = null;
            binary = null;
            GC.Collect();
        }
        
        [Test]
        public void FileSize_Compression_Effective()
        {
            var largeMap = _testMaps[2]; // 100 rooms
            
            // Serialize without compression
            var serializerWithoutCompression = new MapSerializer(settings: new SerializationSettings { EnableCompression = false });
            byte[] uncompressedData = serializerWithoutCompression.SerializeToBinary(largeMap);
            
            // Serialize with compression
            var serializerWithCompression = new MapSerializer(settings: new SerializationSettings { EnableCompression = true });
            byte[] compressedData = serializerWithCompression.SerializeToBinary(largeMap);
            
            // Calculate compression ratio
            float compressionRatio = (float)compressedData.Length / uncompressedData.Length;
            
            // Assert compression is effective (should reduce size by at least 20%)
            Assert.Less(compressionRatio, 0.8f, $"Compression ratio {compressionRatio:P1} should be better than 80%");
            
            Debug.Log($"Uncompressed size: {uncompressedData.Length:N0} bytes");
            Debug.Log($"Compressed size: {compressedData.Length:N0} bytes");
            Debug.Log($"Compression ratio: {compressionRatio:P1}");
        }
        
        [Test]
        public void SerializationSpeed_BinaryVsJson_Comparison()
        {
            var mediumMap = _testMaps[1]; // 50 rooms
            const int iterations = 50;
            
            // Measure JSON serialization speed
            var jsonStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string json = _serializer.SerializeToJson(mediumMap);
                MapData deserialized = _serializer.DeserializeFromJson(json);
            }
            jsonStopwatch.Stop();
            
            // Measure binary serialization speed
            var binaryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                byte[] binary = _serializer.SerializeToBinary(mediumMap);
                MapData deserialized = _serializer.DeserializeFromBinary(binary);
            }
            binaryStopwatch.Stop();
            
            // Binary should be faster than JSON
            Assert.Less(binaryStopwatch.ElapsedMilliseconds, jsonStopwatch.ElapsedMilliseconds, 
                "Binary serialization should be faster than JSON serialization");
            
            Debug.Log($"JSON round-trip time: {jsonStopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
            Debug.Log($"Binary round-trip time: {binaryStopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
            Debug.Log($"Binary speedup: {(float)jsonStopwatch.ElapsedMilliseconds / binaryStopwatch.ElapsedMilliseconds:F2}x");
        }
        
        /// <summary>
        /// Creates test maps of different sizes for performance testing.
        /// </summary>
        private MapData[] CreateTestMaps()
        {
            return new[]
            {
                MapDataFactory.CreateSmallMap(), // 10 rooms
                MapDataFactory.CreateMediumMap(), // 50 rooms
                MapDataFactory.CreateLargeMap()  // 100 rooms
            };
        }
    }
}