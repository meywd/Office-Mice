namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Room classification for content population.
    /// Determines spawn density, resource placement, template selection.
    /// </summary>
    public enum RoomClassification
    {
        Unassigned,       // Not yet classified
        PlayerStart,      // Player spawns here, safe zone
        SafeRoom,         // No enemy spawns, resources only
        StandardRoom,     // Normal combat area
        ArenaRoom,        // Large combat space, higher spawn density
        StorageRoom,      // Resource-heavy, few spawns
        BossRoom,         // End-of-floor special encounter
        SecretRoom,       // Hidden, high-value loot
        TransitionRoom,   // Connects major areas, moderate spawns
        AmbushRoom        // Trap room, sudden spawn burst
    }
}