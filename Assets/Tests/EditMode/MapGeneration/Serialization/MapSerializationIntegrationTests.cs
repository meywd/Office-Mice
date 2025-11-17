using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;

namespace OfficeMice.MapGeneration.Tests.Serialization
{
    /// <summary>
    /// Integration tests for the complete Map Serialization System.
    /// Validates all acceptance criteria for Story 2.6 are met.
    /// </summary>
    [TestFixture]
    public class MapSerializationIntegrationTests
    {
        private MapSerializer _serializer;
        private SerializationValidator _validator;
        private string _testDirectory;
        
        [SetUp]
        public void SetUp()
        {
            _serializer = new MapSerializer();
            _validator = new SerializationValidator(_serializer);
            _testDirectory = Path.Combine(Application.temporaryCachePath, "MapSerializationTests");
            
            if (!Directory.Exists(_testDirectory))
            {
                Directory.CreateDirectory(_testDirectory);
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        
        [Test]
        public void AcceptanceCriteria_JsonSerialization_AvailableForDevelopment()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            
            // Act
            string json = _serializer.SerializeToJson(testMap);
            
            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            
            // Verify JSON is human-readable (development requirement)
            Assert.IsTrue(json.Contains("\n"), "JSON should be formatted for readability");
            Assert.IsTrue(json.Contains("  "), "JSON should have indentation");
            
            // Verify it can be deserialized
            var deserialized = _serializer.DeserializeFromJson(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(testMap.Seed, deserialized.Seed);
        }
        
        [Test]
        public void AcceptanceCriteria_BinarySerialization_AvailableForProduction()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            
            // Act
            byte[] binary = _serializer.SerializeToBinary(testMap);
            
            // Assert
            Assert.IsNotNull(binary);
            Assert.Greater(binary.Length, 0);
            
            // Verify binary format is more compact than JSON
            string json = _serializer.SerializeToJson(testMap);
            Assert.Less(binary.Length, json.Length, "Binary should be more compact than JSON");
            
            // Verify it can be deserialized
            var deserialized = _serializer.DeserializeFromBinary(binary);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(testMap.Seed, deserialized.Seed);
        }
        
        [Test]
        public void AcceptanceCriteria_VersionMigration_HandlesDataStructureChanges()
        {
            // Arrange
            var migrator = new DefaultVersionMigrator();
            var testMap = MapDataFactory.CreateSmallMap();
            var snapshot = testMap.CreateSnapshot();
            
            // Act & Assert - Current version migration
            Assert.IsTrue(migrator.CanMigrate("1.0.0", "1.0.0"));
            var migrated = migrator.Migrate(snapshot, "1.0.0", "1.0.0");
            Assert.IsNotNull(migrated);
            
            // Assert - Unsupported version handling
            Assert.IsFalse(migrator.CanMigrate("0.9.0", "1.0.0"));
            Assert.Throws<UnsupportedVersionException>(() => 
                migrator.Migrate(snapshot, "0.9.0", "1.0.0"));
        }
        
        [Test]
        public void AcceptanceCriteria_RoundTripDataIntegrity_OneHundredPercentAccurate()
        {
            // Arrange
            var testMaps = new[]
            {
                MapDataFactory.CreateSmallMap(),
                MapDataFactory.CreateMediumMap(),
                MapDataFactory.CreateLargeMap()
            };
            
            // Act & Assert - Test each map
            foreach (var testMap in testMaps)
            {
                // JSON round-trip
                string json = _serializer.SerializeToJson(testMap);
                var jsonDeserialized = _serializer.DeserializeFromJson(json);
                
                Assert.AreEqual(testMap.Seed, jsonDeserialized.Seed, "JSON round-trip failed for seed");
                Assert.AreEqual(testMap.MapID, jsonDeserialized.MapID, "JSON round-trip failed for MapID");
                Assert.AreEqual(testMap.MapSize, jsonDeserialized.MapSize, "JSON round-trip failed for MapSize");
                Assert.AreEqual(testMap.Rooms.Count, jsonDeserialized.Rooms.Count, "JSON round-trip failed for rooms count");
                
                // Binary round-trip
                byte[] binary = _serializer.SerializeToBinary(testMap);
                var binaryDeserialized = _serializer.DeserializeFromBinary(binary);
                
                Assert.AreEqual(testMap.Seed, binaryDeserialized.Seed, "Binary round-trip failed for seed");
                Assert.AreEqual(testMap.MapID, binaryDeserialized.MapID, "Binary round-trip failed for MapID");
                Assert.AreEqual(testMap.MapSize, binaryDeserialized.MapSize, "Binary round-trip failed for MapSize");
                Assert.AreEqual(testMap.Rooms.Count, binaryDeserialized.Rooms.Count, "Binary round-trip failed for rooms count");
                
                // Comprehensive validation
                bool isValid = _serializer.ValidateRoundTrip(testMap);
                Assert.IsTrue(isValid, $"Round-trip validation failed for map with {testMap.Rooms.Count} rooms");
            }
        }
        
        [Test]
        public void AcceptanceCriteria_Compression_ReducesFileSize()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            var settingsWithoutCompression = new SerializationSettings { EnableCompression = false };
            var settingsWithCompression = new SerializationSettings { EnableCompression = true };
            
            var serializerWithoutCompression = new MapSerializer(settings: settingsWithoutCompression);
            var serializerWithCompression = new MapSerializer(settings: settingsWithCompression);
            
            // Act
            byte[] uncompressed = serializerWithoutCompression.SerializeToBinary(testMap);
            byte[] compressed = serializerWithCompression.SerializeToBinary(testMap);
            
            // Assert
            Assert.Less(compressed.Length, uncompressed.Length, "Compression should reduce file size");
            
            float compressionRatio = (float)compressed.Length / uncompressed.Length;
            Assert.Less(compressionRatio, 0.9f, "Compression should reduce size by at least 10%");
            
            // Verify compressed data can be decompressed correctly
            var decompressed = serializerWithCompression.DeserializeFromBinary(compressed);
            Assert.AreEqual(testMap.Seed, decompressed.Seed);
            Assert.AreEqual(testMap.Rooms.Count, decompressed.Rooms.Count);
        }
        
        [Test]
        public void Integration_FileBasedSaveLoad_CompleteWorkflow()
        {
            // Arrange
            var originalMap = MapDataFactory.CreateMediumMap();
            string fileName = "IntegrationTest_" + DateTime.Now.Ticks;
            string jsonFilePath = Path.Combine(_testDirectory, fileName + ".json");
            string binaryFilePath = Path.Combine(_testDirectory, fileName + ".binary");
            
            // Act - JSON save/load
            string json = _serializer.SerializeToJson(originalMap);
            File.WriteAllText(jsonFilePath, json);
            
            string loadedJson = File.ReadAllText(jsonFilePath);
            var jsonLoadedMap = _serializer.DeserializeFromJson(loadedJson);
            
            // Act - Binary save/load
            byte[] binary = _serializer.SerializeToBinary(originalMap);
            File.WriteAllBytes(binaryFilePath, binary);
            
            byte[] loadedBinary = File.ReadAllBytes(binaryFilePath);
            var binaryLoadedMap = _serializer.DeserializeFromBinary(loadedBinary);
            
            // Assert
            Assert.AreEqual(originalMap.Seed, jsonLoadedMap.Seed);
            Assert.AreEqual(originalMap.Seed, binaryLoadedMap.Seed);
            Assert.AreEqual(originalMap.Rooms.Count, jsonLoadedMap.Rooms.Count);
            Assert.AreEqual(originalMap.Rooms.Count, binaryLoadedMap.Rooms.Count);
            
            // Verify files exist and have reasonable sizes
            Assert.IsTrue(File.Exists(jsonFilePath));
            Assert.IsTrue(File.Exists(binaryFilePath));
            Assert.Greater(new FileInfo(jsonFilePath).Length, 0);
            Assert.Greater(new FileInfo(binaryFilePath).Length, 0);
        }
        
        [Test]
        public void Performance_AllSerializationMethods_WithinBudget()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            const int iterations = 10;
            
            // Act - Measure JSON performance
            var jsonStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                string json = _serializer.SerializeToJson(testMap);
                var deserialized = _serializer.DeserializeFromJson(json);
            }
            jsonStopwatch.Stop();
            
            // Act - Measure Binary performance
            var binaryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                byte[] binary = _serializer.SerializeToBinary(testMap);
                var deserialized = _serializer.DeserializeFromBinary(binary);
            }
            binaryStopwatch.Stop();
            
            // Assert - Performance targets from context: 3000ms total generation budget
            // Allocate 500ms for serialization as a reasonable target
            var targetTime = TimeSpan.FromMilliseconds(500);
            
            Assert.Less(jsonStopwatch.ElapsedMilliseconds / iterations, targetTime.TotalMilliseconds, 
                "JSON serialization should complete within performance target");
            Assert.Less(binaryStopwatch.ElapsedMilliseconds / iterations, targetTime.TotalMilliseconds, 
                "Binary serialization should complete within performance target");
            
            // Binary should be faster than JSON
            Assert.Less(binaryStopwatch.ElapsedMilliseconds, jsonStopwatch.ElapsedMilliseconds,
                "Binary serialization should be faster than JSON");
        }
        
        [Test]
        public void ErrorHandling_InvalidData_GracefulFailure()
        {
            // Test null inputs
            Assert.Throws<ArgumentNullException>(() => _serializer.SerializeToJson(null));
            Assert.Throws<ArgumentNullException>(() => _serializer.SerializeToBinary(null));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(null));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(""));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromBinary(null));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromBinary(new byte[0]));
            
            // Test invalid JSON
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromJson("{ invalid }"));
            
            // Test invalid binary
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromBinary(new byte[] { 0x00, 0x01, 0x02, 0x03 }));
        }
        
        [Test]
        public void ComprehensiveValidation_AllMapTypes_PassesValidation()
        {
            // Arrange
            var testMaps = new[]
            {
                MapDataFactory.CreateSmallMap(),
                MapDataFactory.CreateMediumMap(),
                MapDataFactory.CreateLargeMap()
            };
            
            // Act & Assert
            foreach (var testMap in testMaps)
            {
                var validationResult = _validator.ValidateRoundTrip(testMap);
                
                Assert.IsTrue(validationResult.IsSuccess, 
                    $"Validation failed for map with {testMap.Rooms.Count} rooms: {string.Join(", ", validationResult.JsonRoundTripResult.ValidationErrors)}");
                
                Assert.IsNotNull(validationResult.JsonRoundTripResult);
                Assert.IsNotNull(validationResult.BinaryRoundTripResult);
                Assert.IsNotNull(validationResult.CompressionResult);
                Assert.IsNotNull(validationResult.PerformanceResult);
                
                Assert.IsTrue(validationResult.JsonRoundTripResult.IsSuccess);
                Assert.IsTrue(validationResult.BinaryRoundTripResult.IsSuccess);
                Assert.IsTrue(validationResult.CompressionResult.IsSuccess);
                Assert.IsTrue(validationResult.PerformanceResult.IsSuccess);
            }
        }
        
        [Test]
        public void MemoryUsage_LargeMaps_WithinBudget()
        {
            // Arrange
            var largeMap = MapDataFactory.CreateLargeMap();
            
            // Measure memory before serialization
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memoryBefore = GC.GetTotalMemory(false);
            
            // Act - Perform serialization operations
            string json = _serializer.SerializeToJson(largeMap);
            byte[] binary = _serializer.SerializeToBinary(largeMap);
            var jsonDeserialized = _serializer.DeserializeFromJson(json);
            var binaryDeserialized = _serializer.DeserializeFromBinary(binary);
            
            // Measure memory after serialization
            long memoryAfter = GC.GetTotalMemory(false);
            long memoryUsed = memoryAfter - memoryBefore;
            
            // Assert - Memory budget from context: under 200MB total
            Assert.Less(memoryUsed, 50 * 1024 * 1024, 
                $"Serialization used {memoryUsed / (1024 * 1024)}MB, should be under 50MB for large maps");
            
            // Clean up
            json = null;
            binary = null;
            jsonDeserialized = null;
            binaryDeserialized = null;
            GC.Collect();
        }
    }
}