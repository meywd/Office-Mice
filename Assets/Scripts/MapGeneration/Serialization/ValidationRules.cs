using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Collection of validation rules for serialization integrity checking.
    /// </summary>
    
    /// <summary>
    /// Validates basic map properties during round-trip serialization.
    /// </summary>
    public class BasicPropertiesValidationRule : IValidationRule
    {
        public List<string> Validate(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            if (original.Seed != deserialized.Seed)
                errors.Add($"Seed mismatch: original={original.Seed}, deserialized={deserialized.Seed}");
                
            if (original.MapID != deserialized.MapID)
                errors.Add($"MapID mismatch: original={original.MapID}, deserialized={deserialized.MapID}");
                
            if (original.MapSize != deserialized.MapSize)
                errors.Add($"MapSize mismatch: original={original.MapSize}, deserialized={deserialized.MapSize}");
                
            if (original.MapBounds != deserialized.MapBounds)
                errors.Add($"MapBounds mismatch: original={original.MapBounds}, deserialized={deserialized.MapBounds}");
                
            if (Math.Abs((original.GeneratedTimestamp - deserialized.GeneratedTimestamp).TotalSeconds) > 1)
                errors.Add($"GeneratedTimestamp mismatch: original={original.GeneratedTimestamp}, deserialized={deserialized.GeneratedTimestamp}");
                
            return errors;
        }
    }
    
    /// <summary>
    /// Validates collection integrity during round-trip serialization.
    /// </summary>
    public class CollectionIntegrityValidationRule : IValidationRule
    {
        public List<string> Validate(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            // Validate rooms collection
            if (original.Rooms.Count != deserialized.Rooms.Count)
                errors.Add($"Rooms count mismatch: original={original.Rooms.Count}, deserialized={deserialized.Rooms.Count}");
            else
            {
                for (int i = 0; i < original.Rooms.Count; i++)
                {
                    var originalRoom = original.Rooms[i];
                    var deserializedRoom = deserialized.Rooms[i];
                    
                    if (originalRoom.RoomID != deserializedRoom.RoomID)
                        errors.Add($"Room[{i}].RoomID mismatch: original={originalRoom.RoomID}, deserialized={deserializedRoom.RoomID}");
                        
                    if (originalRoom.Bounds != deserializedRoom.Bounds)
                        errors.Add($"Room[{i}].Bounds mismatch: original={originalRoom.Bounds}, deserialized={deserializedRoom.Bounds}");
                }
            }
            
            // Validate corridors collection
            if (original.Corridors.Count != deserialized.Corridors.Count)
                errors.Add($"Corridors count mismatch: original={original.Corridors.Count}, deserialized={deserialized.Corridors.Count}");
            else
            {
                for (int i = 0; i < original.Corridors.Count; i++)
                {
                    var originalCorridor = original.Corridors[i];
                    var deserializedCorridor = deserialized.Corridors[i];
                    
                    if (originalCorridor.CorridorID != deserializedCorridor.CorridorID)
                        errors.Add($"Corridor[{i}].CorridorID mismatch: original={originalCorridor.CorridorID}, deserialized={deserializedCorridor.CorridorID}");
                }
            }
            
            // Validate spawn points collection
            if (original.EnemySpawnPoints.Count != deserialized.EnemySpawnPoints.Count)
                errors.Add($"EnemySpawnPoints count mismatch: original={original.EnemySpawnPoints.Count}, deserialized={deserialized.EnemySpawnPoints.Count}");
                
            // Validate resources collection
            if (original.Resources.Count != deserialized.Resources.Count)
                errors.Add($"Resources count mismatch: original={original.Resources.Count}, deserialized={deserialized.Resources.Count}");
                
            return errors;
        }
    }
    
    /// <summary>
    /// Validates spatial data integrity during round-trip serialization.
    /// </summary>
    public class SpatialDataValidationRule : IValidationRule
    {
        public List<string> Validate(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            // Validate player spawn position
            if (original.PlayerSpawnPosition != deserialized.PlayerSpawnPosition)
                errors.Add($"PlayerSpawnPosition mismatch: original={original.PlayerSpawnPosition}, deserialized={deserialized.PlayerSpawnPosition}");
            
            // Validate BSP tree structure
            if (original.RootNode != null && deserialized.RootNode != null)
            {
                var bspErrors = ValidateBSPNode(original.RootNode, deserialized.RootNode, "RootNode");
                errors.AddRange(bspErrors);
            }
            else if (original.RootNode != deserialized.RootNode)
            {
                errors.Add($"BSP RootNode mismatch: one is null, the other is not");
            }
            
            return errors;
        }
        
        private List<string> ValidateBSPNode(BSPNode original, BSPNode deserialized, string path)
        {
            var errors = new List<string>();
            
            if (original == null && deserialized == null)
                return errors;
                
            if (original == null || deserialized == null)
            {
                errors.Add($"{path}: one node is null, the other is not");
                return errors;
            }
            
            if (original.NodeID != deserialized.NodeID)
                errors.Add($"{path}.NodeID mismatch: original={original.NodeID}, deserialized={deserialized.NodeID}");
                
            if (original.Bounds != deserialized.Bounds)
                errors.Add($"{path}.Bounds mismatch: original={original.Bounds}, deserialized={deserialized.Bounds}");
                
            if (original.IsLeaf != deserialized.IsLeaf)
                errors.Add($"{path}.IsLeaf mismatch: original={original.IsLeaf}, deserialized={deserialized.IsLeaf}");
            
            if (!original.IsLeaf)
            {
                if (original.LeftChild == null && deserialized.LeftChild != null)
                    errors.Add($"{path}.LeftChild: original is null, deserialized is not");
                else if (original.LeftChild != null && deserialized.LeftChild == null)
                    errors.Add($"{path}.LeftChild: original is not null, deserialized is null");
                else if (original.LeftChild != null && deserialized.LeftChild != null)
                {
                    var leftErrors = ValidateBSPNode(original.LeftChild, deserialized.LeftChild, $"{path}.LeftChild");
                    errors.AddRange(leftErrors);
                }
                
                if (original.RightChild == null && deserialized.RightChild != null)
                    errors.Add($"{path}.RightChild: original is null, deserialized is not");
                else if (original.RightChild != null && deserialized.RightChild == null)
                    errors.Add($"{path}.RightChild: original is not null, deserialized is null");
                else if (original.RightChild != null && deserialized.RightChild != null)
                {
                    var rightErrors = ValidateBSPNode(original.RightChild, deserialized.RightChild, $"{path}.RightChild");
                    errors.AddRange(rightErrors);
                }
            }
            
            return errors;
        }
    }
    
    /// <summary>
    /// Validates gameplay data integrity during round-trip serialization.
    /// </summary>
    public class GameplayDataValidationRule : IValidationRule
    {
        public List<string> Validate(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            // Validate enemy spawn points
            for (int i = 0; i < Math.Min(original.EnemySpawnPoints.Count, deserialized.EnemySpawnPoints.Count); i++)
            {
                var originalSpawn = original.EnemySpawnPoints[i];
                var deserializedSpawn = deserialized.EnemySpawnPoints[i];
                
                if (originalSpawn.Position != deserializedSpawn.Position)
                    errors.Add($"EnemySpawnPoints[{i}].Position mismatch: original={originalSpawn.Position}, deserialized={deserializedSpawn.Position}");
                    
                if (originalSpawn.EnemyType != deserializedSpawn.EnemyType)
                    errors.Add($"EnemySpawnPoints[{i}].EnemyType mismatch: original={originalSpawn.EnemyType}, deserialized={deserializedSpawn.EnemyType}");
            }
            
            // Validate resources
            for (int i = 0; i < Math.Min(original.Resources.Count, deserialized.Resources.Count); i++)
            {
                var originalResource = original.Resources[i];
                var deserializedResource = deserialized.Resources[i];
                
                if (originalResource.Position != deserializedResource.Position)
                    errors.Add($"Resources[{i}].Position mismatch: original={originalResource.Position}, deserialized={deserializedResource.Position}");
                    
                if (originalResource.ResourceType != deserializedResource.ResourceType)
                    errors.Add($"Resources[{i}].ResourceType mismatch: original={originalResource.ResourceType}, deserialized={deserializedResource.ResourceType}");
            }
            
            return errors;
        }
    }
    
    /// <summary>
    /// Validates metadata integrity during round-trip serialization.
    /// </summary>
    public class MetadataValidationRule : IValidationRule
    {
        public List<string> Validate(MapData original, MapData deserialized)
        {
            var errors = new List<string>();
            
            if (original.Metadata == null && deserialized.Metadata == null)
                return errors;
                
            if (original.Metadata == null || deserialized.Metadata == null)
            {
                errors.Add($"Metadata mismatch: one is null, the other is not");
                return errors;
            }
            
            if (original.Metadata.GenerationAlgorithm != deserialized.Metadata.GenerationAlgorithm)
                errors.Add($"Metadata.GenerationAlgorithm mismatch: original={original.Metadata.GenerationAlgorithm}, deserialized={deserialized.Metadata.GenerationAlgorithm}");
                
            if (original.Metadata.AlgorithmVersion != deserialized.Metadata.AlgorithmVersion)
                errors.Add($"Metadata.AlgorithmVersion mismatch: original={original.Metadata.AlgorithmVersion}, deserialized={deserialized.Metadata.AlgorithmVersion}");
                
            if (Math.Abs(original.Metadata.GenerationTimeMs - deserialized.Metadata.GenerationTimeMs) > 0.1f)
                errors.Add($"Metadata.GenerationTimeMs mismatch: original={original.Metadata.GenerationTimeMs}, deserialized={deserialized.Metadata.GenerationTimeMs}");
            
            return errors;
        }
    }
}