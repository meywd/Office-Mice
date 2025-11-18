using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Main map serializer implementing JSON and binary serialization with compression support.
    /// </summary>
    public class MapSerializer : IMapSerializer
    {
        private readonly IVersionMigrator _versionMigrator;
        private readonly ICompressionProvider _compressionProvider;
        private readonly SerializationSettings _settings;
        
        // Current serialization format version
        public const string CURRENT_VERSION = "1.0.0";
        
        public MapSerializer(
            IVersionMigrator versionMigrator = null,
            ICompressionProvider compressionProvider = null,
            SerializationSettings settings = null)
        {
            _versionMigrator = versionMigrator ?? new DefaultVersionMigrator();
            _compressionProvider = compressionProvider ?? new GzipCompressionProvider();
            _settings = settings ?? new SerializationSettings();
        }
        
        /// <summary>
        /// Serializes map data to JSON format for development and debugging.
        /// </summary>
        public string SerializeToJson(MapData map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
                
            try
            {
                var snapshot = map.CreateSnapshot();
                snapshot.serializationVersion = CURRENT_VERSION;
                snapshot.serializationTimestamp = DateTime.UtcNow.Ticks;
                
                string json = JsonUtility.ToJson(snapshot, _settings.PrettyPrintJson);
                
                if (_settings.EnableCompression && _settings.CompressJson)
                {
                    byte[] compressed = _compressionProvider.Compress(System.Text.Encoding.UTF8.GetBytes(json));
                    return Convert.ToBase64String(compressed);
                }
                
                return json;
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to serialize map to JSON", ex);
            }
        }
        
        /// <summary>
        /// Serializes map data to binary format for production efficiency.
        /// </summary>
        public byte[] SerializeToBinary(MapData map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
                
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Write header
                    using (var writer = new BinaryWriter(memoryStream))
                    {
                        writer.Write("OMAP"); // Magic number
                        writer.Write(CURRENT_VERSION);
                        writer.Write(DateTime.UtcNow.Ticks);
                    }
                    
                    // Serialize snapshot to JSON first, then to binary
                    var snapshot = map.CreateSnapshot();
                    snapshot.serializationVersion = CURRENT_VERSION;
                    snapshot.serializationTimestamp = DateTime.UtcNow.Ticks;
                    
                    string json = JsonUtility.ToJson(snapshot, false);
                    byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);
                    
                    // Compress if enabled
                    if (_settings.EnableCompression)
                    {
                        jsonData = _compressionProvider.Compress(jsonData);
                    }
                    
                    // Write length and data
                    using (var writer = new BinaryWriter(memoryStream))
                    {
                        writer.Write(jsonData.Length);
                        writer.Write(jsonData);
                    }
                    
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to serialize map to binary", ex);
            }
        }
        
        /// <summary>
        /// Deserializes map data from JSON format.
        /// </summary>
        public MapData DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
                
            try
            {
                // Check if this is compressed JSON (Base64 encoded)
                if (_settings.EnableCompression && _settings.CompressJson && IsBase64String(json))
                {
                    byte[] compressed = Convert.FromBase64String(json);
                    byte[] decompressed = _compressionProvider.Decompress(compressed);
                    json = System.Text.Encoding.UTF8.GetString(decompressed);
                }
                
                var snapshot = JsonUtility.FromJson<MapDataSnapshot>(json);
                if (snapshot == null)
                    throw new SerializationException("Failed to parse JSON data");
                    
                // Handle version migration
                if (!string.IsNullOrEmpty(snapshot.serializationVersion) && 
                    snapshot.serializationVersion != CURRENT_VERSION)
                {
                    snapshot = _versionMigrator.Migrate(snapshot, snapshot.serializationVersion, CURRENT_VERSION);
                }
                
                return snapshot.ToMapData();
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to deserialize map from JSON", ex);
            }
        }
        
        /// <summary>
        /// Deserializes map data from binary format.
        /// </summary>
        public MapData DeserializeFromBinary(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Binary data cannot be null or empty", nameof(data));
                
            try
            {
                using (var memoryStream = new MemoryStream(data))
                using (var reader = new BinaryReader(memoryStream))
                {
                    // Read and validate header
                    string magic = reader.ReadString();
                    if (magic != "OMAP")
                        throw new SerializationException("Invalid binary format - wrong magic number");
                        
                    string version = reader.ReadString();
                    long timestamp = reader.ReadInt64();
                    
                    // Read JSON data
                    int jsonLength = reader.ReadInt32();
                    byte[] jsonData = reader.ReadBytes(jsonLength);
                    
                    // Decompress if needed
                    if (_settings.EnableCompression)
                    {
                        jsonData = _compressionProvider.Decompress(jsonData);
                    }
                    
                    string json = System.Text.Encoding.UTF8.GetString(jsonData);
                    var snapshot = JsonUtility.FromJson<MapDataSnapshot>(json);
                    
                    if (snapshot == null)
                        throw new SerializationException("Failed to parse binary data");
                        
                    // Handle version migration
                    if (!string.IsNullOrEmpty(version) && version != CURRENT_VERSION)
                    {
                        snapshot = _versionMigrator.Migrate(snapshot, version, CURRENT_VERSION);
                    }
                    
                    return snapshot.ToMapData();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to deserialize map from binary", ex);
            }
        }
        
        /// <summary>
        /// Validates that serialization round-trip maintains 100% data integrity.
        /// </summary>
        public bool ValidateRoundTrip(MapData map)
        {
            if (map == null)
                return false;
                
            try
            {
                // Test JSON round-trip
                string json = SerializeToJson(map);
                MapData jsonResult = DeserializeFromJson(json);
                
                if (!AreMapsEqual(map, jsonResult))
                    return false;
                    
                // Test binary round-trip
                byte[] binary = SerializeToBinary(map);
                MapData binaryResult = DeserializeFromBinary(binary);
                
                return AreMapsEqual(map, binaryResult);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Compares two maps for equality (deep comparison).
        /// </summary>
        private bool AreMapsEqual(MapData map1, MapData map2)
        {
            if (map1 == null && map2 == null) return true;
            if (map1 == null || map2 == null) return false;
            
            // Compare basic properties
            if (map1.Seed != map2.Seed ||
                map1.MapID != map2.MapID ||
                map1.MapSize != map2.MapSize ||
                map1.PlayerSpawnPosition != map2.PlayerSpawnPosition)
                return false;
                
            // Compare collections
            if (map1.Rooms.Count != map2.Rooms.Count ||
                map1.Corridors.Count != map2.Corridors.Count ||
                map1.EnemySpawnPoints.Count != map2.EnemySpawnPoints.Count ||
                map2.Resources.Count != map2.Resources.Count)
                return false;
                
            // Deep comparison would require comparing individual items
            // For now, compare snapshots which should be comprehensive
            var snapshot1 = map1.CreateSnapshot();
            var snapshot2 = map2.CreateSnapshot();
            
            return JsonUtility.ToJson(snapshot1) == JsonUtility.ToJson(snapshot2);
        }
        
        /// <summary>
        /// Checks if a string is Base64 encoded.
        /// </summary>
        private bool IsBase64String(string base64String)
        {
            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Configuration settings for serialization behavior.
    /// </summary>
    [Serializable]
    public class SerializationSettings
    {
        public bool EnableCompression = true;
        public bool CompressJson = false; // Usually keep JSON readable for debugging
        public bool PrettyPrintJson = true;
        public int CompressionLevel = 6; // 1-9, default 6
    }
    
    /// <summary>
    /// Custom exception for serialization errors.
    /// </summary>
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message) { }
        public SerializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}