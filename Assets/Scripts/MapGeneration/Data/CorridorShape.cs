namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Shape classification for corridors based on path geometry.
    /// Used for visual variation and gameplay considerations.
    /// </summary>
    public enum CorridorShape
    {
        Point,      // Single tile (degenerate case)
        Straight,   // Horizontal or vertical line
        L_Shaped,   // One 90° turn
        Z_Shaped,   // Two 90° turns
        Complex     // Three or more turns
    }
}