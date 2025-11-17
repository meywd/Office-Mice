using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;

namespace OfficeMice.MapGeneration.Tests.Serialization
{
    /// <summary>
    /// Comprehensive tests for the MapSerializer system.
    /// Validates all acceptance criteria for Story 2.6.
    /// </summary>
    [TestFixture]
    public class MapSerializerTests
    {
        private MapSerializer _serializer;
        private MapData _testMap;
        
        [SetUp]
        public void SetUp()
        {
            _serializer = new MapSerializer();
            _testMap = MapDataFactory.CreateMediumMap();
        }
        
        [TearDown]
        public void TearDown()
        {
            _serializer = null;
            _testMap = null;
        }
        
        [Test]
        public void SerializeToJson_ValidMap_ReturnsJsonString()
        {
            // Act
            string json = _serializer.SerializeToJson(_testMap);
            
            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.StartsWith("{"));
            Assert.IsTrue(json.EndsWith("}"));
        }
        
        [Test]
        public void SerializeToJson_NullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _serializer.SerializeToJson(null));
        }
        
        [Test]
        public void DeserializeFromJson_ValidJson_ReturnsMapData()
        {
            // Arrange
            string json = _serializer.SerializeToJson(_testMap);
            
            // Act
            MapData deserialized = _serializer.DeserializeFromJson(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(_testMap.Seed, deserialized.Seed);
            Assert.AreEqual(_testMap.MapID, deserialized.MapID);
            Assert.AreEqual(_testMap.MapSize, deserialized.MapSize);
        }
        
        [Test]
        public void DeserializeFromJson_InvalidJson_ThrowsSerializationException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";
            
            // Act & Assert
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromJson(invalidJson));
        }
        
        [Test]
        public void DeserializeFromJson_NullOrEmptyJson_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(null));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson(""));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromJson("   "));
        }
        
        [Test]
        public void SerializeToBinary_ValidMap_ReturnsByteArray()
        {
            // Act
            byte[] binary = _serializer.SerializeToBinary(_testMap);
            
            // Assert
            Assert.IsNotNull(binary);
            Assert.Greater(binary.Length, 0);
            
            // Verify binary format header
            string header = System.Text.Encoding.ASCII.GetString(binary, 0, 4);
            Assert.AreEqual("OMAP", header);
        }
        
        [Test]
        public void SerializeToBinary_NullMap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _serializer.SerializeToBinary(null));
        }
        
        [Test]
        public void DeserializeFromBinary_ValidBinary_ReturnsMapData()
        {
            // Arrange
            byte[] binary = _serializer.SerializeToBinary(_testMap);
            
            // Act
            MapData deserialized = _serializer.DeserializeFromBinary(binary);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(_testMap.Seed, deserialized.Seed);
            Assert.AreEqual(_testMap.MapID, deserialized.MapID);
            Assert.AreEqual(_testMap.MapSize, deserialized.MapSize);
        }
        
        [Test]
        public void DeserializeFromBinary_InvalidHeader_ThrowsSerializationException()
        {
            // Arrange
            byte[] invalidBinary = System.Text.Encoding.ASCII.GetBytes("INVALID");
            
            // Act & Assert
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromBinary(invalidBinary));
        }
        
        [Test]
        public void DeserializeFromBinary_NullOrEmptyData_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromBinary(null));
            Assert.Throws<ArgumentException>(() => _serializer.DeserializeFromBinary(new byte[0]));
        }
        
        [Test]
        public void ValidateRoundTrip_ValidMap_ReturnsTrue()
        {
            // Act
            bool isValid = _serializer.ValidateRoundTrip(_testMap);
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void ValidateRoundTrip_NullMap_ReturnsFalse()
        {
            // Act
            bool isValid = _serializer.ValidateRoundTrip(null);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void JsonRoundTrip_ComplexMap_MaintainsDataIntegrity()
        {
            // Arrange
            var complexMap = MapDataFactory.CreateLargeMap();
            
            // Act
            string json = _serializer.SerializeToJson(complexMap);
            MapData deserialized = _serializer.DeserializeFromJson(json);
            
            // Assert - Verify all major data structures
            Assert.AreEqual(complexMap.Seed, deserialized.Seed);
            Assert.AreEqual(complexMap.MapID, deserialized.MapID);
            Assert.AreEqual(complexMap.MapSize, deserialized.MapSize);
            Assert.AreEqual(complexMap.Rooms.Count, deserialized.Rooms.Count);
            Assert.AreEqual(complexMap.Corridors.Count, deserialized.Corridors.Count);
            Assert.AreEqual(complexMap.EnemySpawnPoints.Count, deserialized.EnemySpawnPoints.Count);
            Assert.AreEqual(complexMap.Resources.Count, deserialized.Resources.Count);
            
            // Verify room data integrity
            for (int i = 0; i < complexMap.Rooms.Count; i++)
            {
                Assert.AreEqual(complexMap.Rooms[i].RoomID, deserialized.Rooms[i].RoomID);
                Assert.AreEqual(complexMap.Rooms[i].Bounds, deserialized.Rooms[i].Bounds);
            }
        }
        
        [Test]
        public void BinaryRoundTrip_ComplexMap_MaintainsDataIntegrity()
        {
            // Arrange
            var complexMap = MapDataFactory.CreateLargeMap();
            
            // Act
            byte[] binary = _serializer.SerializeToBinary(complexMap);
            MapData deserialized = _serializer.DeserializeFromBinary(binary);
            
            // Assert - Verify all major data structures
            Assert.AreEqual(complexMap.Seed, deserialized.Seed);
            Assert.AreEqual(complexMap.MapID, deserialized.MapID);
            Assert.AreEqual(complexMap.MapSize, deserialized.MapSize);
            Assert.AreEqual(complexMap.Rooms.Count, deserialized.Rooms.Count);
            Assert.AreEqual(complexMap.Corridors.Count, deserialized.Corridors.Count);
            Assert.AreEqual(complexMap.EnemySpawnPoints.Count, deserialized.EnemySpawnPoints.Count);
            Assert.AreEqual(complexMap.Resources.Count, deserialized.Resources.Count);
        }
        
        [Test]
        public void Compression_Enabled_ReducesFileSize()
        {
            // Arrange
            var settingsWithoutCompression = new SerializationSettings { EnableCompression = false };
            var settingsWithCompression = new SerializationSettings { EnableCompression = true };
            
            var serializerWithoutCompression = new MapSerializer(settings: settingsWithoutCompression);
            var serializerWithCompression = new MapSerializer(settings: settingsWithCompression);
            
            // Act
            byte[] uncompressed = serializerWithoutCompression.SerializeToBinary(_testMap);
            byte[] compressed = serializerWithCompression.SerializeToBinary(_testMap);
            
            // Assert
            Assert.Less(compressed.Length, uncompressed.Length);
            float compressionRatio = (float)compressed.Length / uncompressed.Length;
            Assert.Less(compressionRatio, 0.9f); // Should reduce size by at least 10%
        }
        
        [Test]
        public void Compression_RoundTrip_MaintainsDataIntegrity()
        {
            // Arrange
            var settings = new SerializationSettings { EnableCompression = true };
            var serializer = new MapSerializer(settings: settings);
            
            // Act
            byte[] compressed = serializer.SerializeToBinary(_testMap);
            MapData deserialized = serializer.DeserializeFromBinary(compressed);
            
            // Assert
            Assert.AreEqual(_testMap.Seed, deserialized.Seed);
            Assert.AreEqual(_testMap.MapID, deserialized.MapID);
            Assert.AreEqual(_testMap.Rooms.Count, deserialized.Rooms.Count);
        }
        
        [Test]
        public void VersionMigration_SameVersion_NoMigrationNeeded()
        {
            // Arrange
            var migrator = new DefaultVersionMigrator();
            var snapshot = _testMap.CreateSnapshot();
            snapshot.serializationVersion = "1.0.0";
            
            // Act
            var migrated = migrator.Migrate(snapshot, "1.0.0", "1.0.0");
            
            // Assert
            Assert.AreEqual(snapshot, migrated);
        }
        
        [Test]
        public void VersionMigration_UnsupportedVersion_ThrowsException()
        {
            // Arrange
            var migrator = new DefaultVersionMigrator();
            var snapshot = _testMap.CreateSnapshot();
            snapshot.serializationVersion = "0.9.0"; // Unsupported version
            
            // Act & Assert
            Assert.Throws<UnsupportedVersionException>(() => 
                migrator.Migrate(snapshot, "0.9.0", "1.0.0"));
        }
        
        [Test]
        public void SerializationSettings_CustomSettings_AppliedCorrectly()
        {
            // Arrange
            var settings = new SerializationSettings
            {
                EnableCompression = false,
                PrettyPrintJson = false
            };
            var serializer = new MapSerializer(settings: settings);
            
            // Act
            string json = serializer.SerializeToJson(_testMap);
            
            // Assert
            Assert.IsFalse(json.Contains("\n")); // Should not be pretty printed
            Assert.IsFalse(json.Contains("  ")); // Should not have indentation
        }
        
        [Test]
        public void JsonWithCompression_Base64Encoded_CorrectlyDecoded()
        {
            // Arrange
            var settings = new SerializationSettings 
            { 
                EnableCompression = true, 
                CompressJson = true 
            };
            var serializer = new MapSerializer(settings: settings);
            
            // Act
            string compressedJson = serializer.SerializeToJson(_testMap);
            MapData deserialized = serializer.DeserializeFromJson(compressedJson);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(_testMap.Seed, deserialized.Seed);
            
            // Verify it's Base64 encoded
            Assert.DoesNotThrow(() => Convert.FromBase64String(compressedJson));
        }
        
        [Test]
        public void BinaryFormat_Header_CorrectlyWritten()
        {
            // Act
            byte[] binary = _serializer.SerializeToBinary(_testMap);
            
            // Assert
            Assert.GreaterOrEqual(binary.Length, 12); // Minimum header size
            
            // Check magic number
            string magic = System.Text.Encoding.ASCII.GetString(binary, 0, 4);
            Assert.AreEqual("OMAP", magic);
            
            // Check version string exists
            using (var stream = new MemoryStream(binary))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadString(); // Magic number
                string version = reader.ReadString();
                Assert.AreEqual("1.0.0", version);
            }
        }
        
        [Test]
        public void ErrorHandling_CorruptedData_GracefulFailure()
        {
            // Arrange
            byte[] corruptedData = new byte[] { 0x4F, 0x4D, 0x41, 0x50 }; // Valid header but no data
            
            // Act & Assert
            Assert.Throws<SerializationException>(() => _serializer.DeserializeFromBinary(corruptedData));
        }
        
        [Test]
        public void MultipleSerializations_DifferentMaps_IndependentResults()
        {
            // Arrange
            var map1 = MapDataFactory.CreateSmallMap();
            var map2 = MapDataFactory.CreateMediumMap();
            
            // Act
            string json1 = _serializer.SerializeToJson(map1);
            string json2 = _serializer.SerializeToJson(map2);
            
            MapData deserialized1 = _serializer.DeserializeFromJson(json1);
            MapData deserialized2 = _serializer.DeserializeFromJson(json2);
            
            // Assert
            Assert.AreNotEqual(map1.Seed, map2.Seed);
            Assert.AreEqual(map1.Seed, deserialized1.Seed);
            Assert.AreEqual(map2.Seed, deserialized2.Seed);
            Assert.AreNotEqual(deserialized1.Seed, deserialized2.Seed);
        }
    }
}