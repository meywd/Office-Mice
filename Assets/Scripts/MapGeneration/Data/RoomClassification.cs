namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Room classification for content population.
    /// Determines spawn density, resource placement, template selection.
    /// Extended for office environment with specific room types.
    /// </summary>
    public enum RoomClassification
    {
        Unassigned,       // Not yet classified
        
        // Original game-oriented classifications
        PlayerStart,      // Player spawns here, safe zone
        SafeRoom,         // No enemy spawns, resources only
        StandardRoom,     // Normal combat area
        ArenaRoom,        // Large combat space, higher spawn density
        StorageRoom,      // Resource-heavy, few spawns
        BossRoom,         // End-of-floor special encounter
        SecretRoom,       // Hidden, high-value loot
        TransitionRoom,   // Connects major areas, moderate spawns
        AmbushRoom,       // Trap room, sudden spawn burst
        
        // Office environment classifications (Story 2.2)
        Office,           // Standard workspace, medium size
        Conference,       // Meeting space, large size
        BreakRoom,        // Employee break area, medium size
        Storage,          // Supply storage, small to medium size
        Lobby,            // Entrance/reception area, large size
        ServerRoom,       // IT infrastructure, small size, secure
        Security,         // Security office, small size, strategic position
        BossOffice        // Executive office, large size, premium location
    }
}