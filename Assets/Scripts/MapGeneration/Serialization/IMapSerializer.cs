using System;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Interface for map serialization operations supporting both JSON and binary formats with validation.
    /// </summary>
    public interface IMapSerializer
    {
        /// <summary>
        /// Serializes map data to JSON format for development and debugging.
        /// </summary>
        /// <param name="map">The map data to serialize</param>
        /// <returns>JSON string representation of the map</returns>
        string SerializeToJson(MapData map);
        
        /// <summary>
        /// Serializes map data to binary format for production efficiency.
        /// </summary>
        /// <param name="map">The map data to serialize</param>
        /// <returns>Binary representation of the map</returns>
        byte[] SerializeToBinary(MapData map);
        
        /// <summary>
        /// Deserializes map data from JSON format.
        /// </summary>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Deserialized map data</returns>
        MapData DeserializeFromJson(string json);
        
        /// <summary>
        /// Deserializes map data from binary format.
        /// </summary>
        /// <param name="data">Binary data to deserialize</param>
        /// <returns>Deserialized map data</returns>
        MapData DeserializeFromBinary(byte[] data);
        
        /// <summary>
        /// Validates that serialization round-trip maintains 100% data integrity.
        /// </summary>
        /// <param name="map">The map data to validate</param>
        /// <returns>True if round-trip validation passes, false otherwise</returns>
        bool ValidateRoundTrip(MapData map);
    }
}