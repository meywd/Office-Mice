using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Default implementation of version migration system for map data.
    /// Handles backward compatibility and data structure evolution.
    /// </summary>
    public class DefaultVersionMigrator : IVersionMigrator
    {
        private readonly Dictionary<string, Version> _supportedVersions;
        private readonly Dictionary<string, IMigrationStep> _migrationSteps;
        
        public DefaultVersionMigrator()
        {
            _supportedVersions = new Dictionary<string, Version>
            {
                { "1.0.0", new Version(1, 0, 0) }
                // Add future versions here as they are introduced
            };
            
            _migrationSteps = new Dictionary<string, IMigrationStep>();
            RegisterMigrationSteps();
        }
        
        /// <summary>
        /// Migrates map data from one version to another.
        /// </summary>
        public MapDataSnapshot Migrate(MapDataSnapshot data, string fromVersion, string toVersion)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
                
            if (string.IsNullOrEmpty(fromVersion))
                throw new ArgumentException("Source version cannot be null or empty", nameof(fromVersion));
                
            if (string.IsNullOrEmpty(toVersion))
                throw new ArgumentException("Target version cannot be null or empty", nameof(toVersion));
                
            // If versions are the same, no migration needed
            if (fromVersion == toVersion)
                return data;
                
            // Validate versions
            if (!_supportedVersions.ContainsKey(fromVersion))
                throw new UnsupportedVersionException($"Source version '{fromVersion}' is not supported");
                
            if (!_supportedVersions.ContainsKey(toVersion))
                throw new UnsupportedVersionException($"Target version '{toVersion}' is not supported");
                
            Version from = _supportedVersions[fromVersion];
            Version to = _supportedVersions[toVersion];
            
            // Perform migration step by step
            MapDataSnapshot current = data;
            Version currentVersion = from;
            
            while (currentVersion < to)
            {
                Version nextVersion = GetNextVersion(currentVersion);
                string migrationKey = $"{currentVersion}->{nextVersion}";
                
                if (!_migrationSteps.ContainsKey(migrationKey))
                {
                    throw new MigrationException($"No migration path found from {currentVersion} to {nextVersion}");
                }
                
                current = _migrationSteps[migrationKey].Migrate(current);
                currentVersion = nextVersion;
            }
            
            // Update version metadata
            current.serializationVersion = toVersion;
            current.serializationTimestamp = DateTime.UtcNow.Ticks;
            
            return current;
        }
        
        /// <summary>
        /// Checks if migration between versions is supported.
        /// </summary>
        public bool CanMigrate(string fromVersion, string toVersion)
        {
            if (string.IsNullOrEmpty(fromVersion) || string.IsNullOrEmpty(toVersion))
                return false;
                
            if (!_supportedVersions.ContainsKey(fromVersion) || !_supportedVersions.ContainsKey(toVersion))
                return false;
                
            Version from = _supportedVersions[fromVersion];
            Version to = _supportedVersions[toVersion];
            
            // Check if there's a migration path
            Version current = from;
            while (current < to)
            {
                Version next = GetNextVersion(current);
                string migrationKey = $"{current}->{next}";
                
                if (!_migrationSteps.ContainsKey(migrationKey))
                    return false;
                    
                current = next;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets all supported versions for migration.
        /// </summary>
        public string[] GetSupportedVersions()
        {
            var versions = new string[_supportedVersions.Count];
            _supportedVersions.Keys.CopyTo(versions, 0);
            Array.Sort(versions);
            return versions;
        }
        
        /// <summary>
        /// Registers all available migration steps.
        /// </summary>
        private void RegisterMigrationSteps()
        {
            // Register migration steps as they are needed
            // Example: _migrationSteps["1.0.0->1.1.0"] = new Migration_1_0_0_To_1_1_0();
            
            // For now, we only have version 1.0.0, so no migration steps needed yet
        }
        
        /// <summary>
        /// Gets the next version in the sequence.
        /// </summary>
        private Version GetNextVersion(Version current)
        {
            // Simple increment logic - can be made more sophisticated
            if (current.Revision >= 0)
                return new Version(current.Major, current.Minor, current.Revision + 1);
            else if (current.Minor >= 0)
                return new Version(current.Major, current.Minor + 1, 0);
            else
                return new Version(current.Major + 1, 0, 0);
        }
    }
    
    /// <summary>
    /// Interface for individual migration steps.
    /// </summary>
    internal interface IMigrationStep
    {
        MapDataSnapshot Migrate(MapDataSnapshot data);
    }
    
    /// <summary>
    /// Exception thrown when a version is not supported for migration.
    /// </summary>
    public class UnsupportedVersionException : Exception
    {
        public UnsupportedVersionException(string message) : base(message) { }
        public UnsupportedVersionException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Exception thrown when migration fails.
    /// </summary>
    public class MigrationException : Exception
    {
        public MigrationException(string message) : base(message) { }
        public MigrationException(string message, Exception innerException) : base(message, innerException) { }
    }
}