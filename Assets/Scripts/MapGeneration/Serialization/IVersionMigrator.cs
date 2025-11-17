using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Interface for handling version migration of serialized map data between different data structure versions.
    /// </summary>
    public interface IVersionMigrator
    {
        /// <summary>
        /// Migrates map data from one version to another.
        /// </summary>
        /// <param name="data">The map data snapshot to migrate</param>
        /// <param name="fromVersion">Source version</param>
        /// <param name="toVersion">Target version</param>
        /// <returns>Migrated map data snapshot</returns>
        MapDataSnapshot Migrate(MapDataSnapshot data, string fromVersion, string toVersion);
        
        /// <summary>
        /// Checks if migration between versions is supported.
        /// </summary>
        /// <param name="fromVersion">Source version</param>
        /// <param name="toVersion">Target version</param>
        /// <returns>True if migration is supported, false otherwise</returns>
        bool CanMigrate(string fromVersion, string toVersion);
        
        /// <summary>
        /// Gets all supported versions for migration.
        /// </summary>
        /// <returns>Array of supported version strings</returns>
        string[] GetSupportedVersions();
    }
}