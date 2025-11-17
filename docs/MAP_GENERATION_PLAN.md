# Map Generation Plan for Office-Mice

## Executive Summary

**Recommended Approach:** Hybrid BSP + Room Templates

After deep analysis, the optimal solution for Office-Mice is a hybrid approach combining:
- **Binary Space Partitioning (BSP)** for layout structure
- **Room Templates** for detailed office design
- **Procedural corridors** connecting rooms

---

## Why This Approach?

✅ **Perfect for office environments** - BSP creates rectangular rooms ideal for cubicles, meeting rooms, hallways
✅ **Balances variety & control** - Procedural structure + hand-crafted room details
✅ **Works with existing setup** - NavMeshPlus integration, tilemaps already configured
✅ **Incremental implementation** - Can build piece by piece without breaking current game
✅ **Fast generation** - <2 seconds for 100x100 maps

---

## Algorithm Comparison

| Algorithm | Suitability | Complexity | Best For |
|-----------|-------------|------------|----------|
| **BSP** ⭐ | Excellent | Medium | Office layouts, guaranteed connectivity |
| **WFC** | Excellent | High | Pattern-based, advanced control |
| **Room Templates** | Good | Low | Quick prototyping, art quality |
| **Cellular Automata** | Poor | Low | Organic caves (not offices) |
| **Graph-based** | Good | High | Complex flow design |

### Detailed Algorithm Analysis

#### 1. Binary Space Partitioning (BSP) - HIGHLY SUITABLE
- Creates rectangular rooms by recursively dividing space
- Perfect for office cubicles, meeting rooms, hallways
- Easy to implement with Unity Tilemaps
- Can control room sizes, corridor widths
- Example: Divide 100x100 grid → create rooms → connect with corridors

#### 2. Wave Function Collapse (WFC) - EXCELLENT
- Generates maps based on input patterns/rules
- Can learn from manually-created office layouts
- Very flexible, handles constraints well
- More complex to implement
- Unity package: WaveFunctionCollapse by mxgmn

#### 3. Room Template Systems - SIMPLE & EFFECTIVE
- Pre-made room prefabs placed procedurally
- Easy to art-direct and control quality
- Fast implementation
- Less variety than true procedural
- Good for prototyping

#### 4. Cellular Automata - MODERATE
- Creates organic cave-like structures
- Less suitable for structured office layouts
- Could work for "destroyed/chaotic" office theme
- Requires post-processing for playability

#### 5. Graph-Based Generation - ADVANCED
- Defines rooms as nodes, connections as edges
- Great for ensuring connectivity, gameplay flow
- Can enforce design patterns (boss at end, etc.)
- More programming complexity

---

## Implementation Roadmap (3 Weeks)

### Week 1: Core BSP Generator

#### Day 1-2: BSP Algorithm
- Build recursive space partitioning
- **Validation:** Use `OnDrawGizmos` to visualize partitions (no tiles yet!)
- Tune parameters: min/max room sizes, split orientation

**Key Point:** Start with abstract data visualization. Don't touch tiles until the partition logic is solid.

#### Day 3: Abstract Room & Corridor Generation
- Inside each leaf partition, define a smaller "room" rectangle
- Implement two-pass corridor system:
  - **Primary Pass:** Connect large "core" rooms (creates main hallway)
  - **Secondary Pass:** Connect smaller rooms to main artery
- **Validation:** Draw room rectangles and corridor paths with Gizmos

**Why Two-Pass?** This creates realistic office flow vs dungeon maze. Offices have main circulation spines with branches.

#### Day 4-5: Tilemap Rendering
- Create `TilemapGenerator` class
- Paint floor tiles in rooms/corridors
- Add wall tiles around perimeters
- **Validation:** Complete visual level generated at runtime

### Week 2: Room Templates

#### Day 1-2: Template System
Create the `RoomTemplate` ScriptableObject:

```csharp
public class RoomTemplate : ScriptableObject
{
    public Vector2Int size;
    public TileBase[] tiles; // Flattened 1D array of tiles
    public List<Vector2Int> potentialDoorways; // Local coordinates within template
    public RoomType type; // Conference, Cubicle, BreakRoom, etc.
}
```

Create 3-5 test room templates manually.

#### Day 3-4: Furniture Placement
- Reuse existing desk/chair prefabs from `Assets/Game/Layout/`
- Procedurally place based on room type
- Add spawn points, health pickups, ammo based on gameplay rules
- **Validation:** Rooms contain appropriate furniture without blocking paths

#### Day 5: Template Integration
- Modify `TilemapGenerator` to place template instances
- **Critical:** Corridors connect to `potentialDoorways` (not random walls!)
- Ensures clean connections without breaking room designs
- **Validation:** Generated map includes detailed pre-designed rooms connected by procedural corridors

### Week 3: Editor Tools & Polish

#### Day 1-2: Custom Editor Window
```csharp
Assets/Scripts/MapGeneration/Editor/MapGeneratorEditor.cs
```

Features:
- Generate button
- Regenerate button (with same seed)
- Clear button
- Seed input field (for reproducible maps)
- Size parameters (map width/height)
- Preview before applying

#### Day 3: NavMesh Integration
- Integrate `NavMeshSurface.BuildNavMesh()` after generation
- Ensure corridor width ≥3 tiles (prevents NavMesh gaps)
- Add padding around furniture
- **Validation:** Verify 95%+ NavMesh coverage, agents can navigate between any two rooms

#### Day 4-5: Performance & Polish
- Implement generation as coroutine with loading bar
- Yield after each major step (BSP, Corridors, Tiling)
- Handle large maps (up to 500x500 tiles)
- Bug fixes and edge case handling

---

## Critical Implementation Details

### 1. Two-Pass Corridor Generation

**Problem:** Standard BSP creates dungeon-like mazes, not realistic offices.

**Solution:**
1. **Primary Corridor Pass:** Identify "core" rooms (e.g., largest from each major partition)
2. Connect these core rooms first to form main circulation spine
3. **Secondary Connection Pass:** Connect remaining "minor" rooms to this main artery
4. Results in hierarchical, believable office layout

### 2. Pre-Defined Doorways

**Problem:** Random corridor connections can punch holes in carefully designed walls.

**Solution:**
- Room templates include `List<Vector2Int> potentialDoorways`
- Corridor algorithm queries both room templates
- Finds two closest doorways between rooms
- Pathfinds between these specific points
- Preserves room design integrity

### 3. NavMesh Gap Prevention

**Risks:**
- Corridors too narrow
- Furniture blocking critical paths
- Disconnected NavMesh regions

**Solutions:**
- Corridors must be ≥3 tiles wide
- Add padding around furniture placement
- Verify connectivity after generation
- Remove furniture if it blocks NavMesh

### 4. Phased Validation Strategy

**Don't:** Build everything then test
**Do:** Validate at each step

1. **Step 1:** Gizmos show BSP partitions
2. **Step 2:** Gizmos show rooms + corridor paths
3. **Step 3:** Tiles render correctly
4. **Step 4:** Templates integrate cleanly
5. **Step 5:** NavMesh generates and connects

---

## Unity API Usage

### Core Generation APIs

```csharp
// Setting individual tiles
Tilemap.SetTile(Vector3Int position, TileBase tile)

// Efficient rectangular fills
Tilemap.BoxFill(Vector3Int position, TileBase tile, int xMin, int yMin, int xMax, int yMax)

// Flood fill for connected regions
Tilemap.FloodFill(Vector3Int position, TileBase tile)

// NavMesh baking (NavMeshPlus)
NavMeshSurface.BuildNavMesh()
```

### Editor Tools

```csharp
// Custom Editor window
public class MapGeneratorEditor : EditorWindow
{
    [MenuItem("Tools/Map Generator")]
    static void ShowWindow() { ... }
}

// Gizmos for visualization
void OnDrawGizmos()
{
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(roomCenter, roomSize);
}
```

---

## File Structure

```
Assets/
├── Scripts/
│   └── MapGeneration/
│       ├── BSPNode.cs              // Tree structure for space partitioning
│       ├── RoomTemplate.cs         // ScriptableObject for room definitions
│       ├── MapGenerator.cs         // Main controller
│       ├── CorridorGenerator.cs    // Two-pass corridor pathfinding
│       ├── FurniturePlacer.cs      // Procedural desk/chair spawning
│       ├── TilemapGenerator.cs     // Renders abstract data to Tilemap
│       └── Editor/
│           └── MapGeneratorEditor.cs // Custom Editor window
│
├── Resources/
│   └── RoomTemplates/
│       ├── ConferenceRoom.asset
│       ├── Cubicles.asset
│       ├── BreakRoom.asset
│       ├── Reception.asset
│       └── ServerRoom.asset
│
└── Game/
    ├── GameScene.unity             // Add MapGenerator component here
    └── Layout/                     // Existing tile/furniture assets
```

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Generation Time** | <2 seconds | 100x100 tile map |
| **NavMesh Coverage** | >95% | Walkable floor area |
| **Room Variety** | ≥5 templates | Different room types |
| **Connectivity** | 100% | Zero unreachable rooms |
| **Performance** | 60 FPS | During gameplay after generation |

---

## Integration Points

### GameScene.unity
- Add `MapGenerator` component to scene
- Configure in Inspector:
  - Map size (width, height)
  - Seed (optional, for reproducibility)
  - Reference to Tilemap objects
  - Reference to NavMeshSurface

### NavMesh Integration
- Auto-bake after generation completes
- Use existing NavMeshPlus package (already in project)
- Configure NavMeshSurface component:
  - Agent Type: 2D
  - Collect Objects: All
  - Layermask: Default + whatever your walkable layer is

### Existing Assets
- Desk prefabs in `Assets/Game/Layout/`
- Chair prefabs
- Tile assets from Tile Palette
- All reusable in procedural system

### Game Manager
- Hook generation to:
  - Level start (first load)
  - Level restart (new seed)
  - New game (different layout each time)

---

## Edge Cases & Solutions

### 1. Unreachable Rooms
**Problem:** BSP may create isolated rooms
**Solution:** Corridor algorithm ensures all leaves are connected

### 2. Too Small Rooms
**Problem:** Partitioning creates tiny unusable spaces
**Solution:** Set minimum room size (e.g., 8x8 tiles) in BSP parameters

### 3. NavMesh Gaps
**Problem:** Narrow corridors or furniture creates disconnected NavMesh
**Solution:**
- Corridor width ≥3 tiles
- Padding around furniture
- Post-generation verification

### 4. Performance Spikes
**Problem:** Large maps cause frame drops during generation
**Solution:**
- Generate in coroutine
- Yield after each major step
- Show loading bar for user feedback

### 5. Desks Blocking Paths
**Problem:** Furniture placement blocks critical walkways
**Solution:**
- Check NavMesh after furniture placement
- Remove furniture if it creates gaps
- Use `NavMesh.SamplePosition()` to verify walkability

---

## Technical Constraints

| Constraint | Limit | Reason |
|------------|-------|--------|
| **Tilemap Size** | 500x500 tiles | Unity performance limit |
| **Room Templates** | ~20 variants | Build size, memory |
| **Generation Time** | <5 seconds | User experience |
| **Cache Size** | Managed by GitHub | 10GB limit |

### Git LFS Considerations
- Large tilemap files use Git LFS (already configured)
- Room template assets should use LFS
- Monitor `.git/lfs` size in caching

---

## Available Unity Assets (Optional)

### Commercial
- **Dungeon Architect** ($45, Asset Store)
  - Visual graph editor
  - Pre-built 2D tilemap support
  - Good for rapid prototyping

### Free/Open Source
- **WaveFunctionCollapse Unity** (GitHub)
  - Pattern-based generation
  - Can learn from existing layouts
  - More complex setup

### Built-in Unity
- **Tile Palette Brushes** (Unity's Brush API)
  - Custom painting tools
  - Batch operations
  - Editor workflow enhancement

---

## Fallback Options

If BSP + Templates doesn't meet requirements:

### Option 1: Pure Template System
- **Approach:** Room prefabs + manual connection rules
- **Pros:** Simplest, full art control
- **Cons:** Less variety, more authoring
- **Effort:** Low

### Option 2: Wave Function Collapse
- **Approach:** Pattern-based procedural generation
- **Pros:** Learns from examples, very flexible
- **Cons:** Complex, slower generation
- **Effort:** High

### Option 3: Enhanced Manual Tools
- **Approach:** Better editor tools for manual creation
- **Pros:** Immediate value, no algorithm risk
- **Cons:** Still manual work, no runtime variety
- **Effort:** Low-Medium

---

## Implementation Pseudocode

### BSP Core

```csharp
public class BSPNode
{
    public Rect rect;
    public BSPNode left, right;
    public bool isLeaf => left == null && right == null;

    public void Split(int minRoomSize)
    {
        if (rect.width < minRoomSize * 2 && rect.height < minRoomSize * 2)
            return; // Too small to split

        bool splitHorizontal = Random.value > 0.5f;

        if (splitHorizontal)
        {
            // Split horizontally
            float splitY = Random.Range(minRoomSize, rect.height - minRoomSize);
            left = new BSPNode(new Rect(rect.x, rect.y, rect.width, splitY));
            right = new BSPNode(new Rect(rect.x, rect.y + splitY, rect.width, rect.height - splitY));
        }
        else
        {
            // Split vertically
            float splitX = Random.Range(minRoomSize, rect.width - minRoomSize);
            left = new BSPNode(new Rect(rect.x, rect.y, splitX, rect.height));
            right = new BSPNode(new Rect(rect.x + splitX, rect.y, rect.width - splitX, rect.height));
        }

        left.Split(minRoomSize);
        right.Split(minRoomSize);
    }

    public List<BSPNode> GetLeaves()
    {
        if (isLeaf) return new List<BSPNode> { this };
        return left.GetLeaves().Concat(right.GetLeaves()).ToList();
    }
}
```

### Map Generator

```csharp
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int mapWidth = 100;
    [SerializeField] private int mapHeight = 100;
    [SerializeField] private int minRoomSize = 8;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    public void GenerateMap(int seed = 0)
    {
        Random.InitState(seed == 0 ? System.DateTime.Now.Millisecond : seed);

        // 1. Create BSP tree
        BSPNode root = new BSPNode(new Rect(0, 0, mapWidth, mapHeight));
        root.Split(minRoomSize);

        // 2. Get all leaf rooms
        List<BSPNode> rooms = root.GetLeaves();

        // 3. Generate corridors (two-pass)
        List<Vector2Int> corridorTiles = GenerateCorridors(rooms);

        // 4. Render to tilemap
        RenderToTilemap(rooms, corridorTiles);

        // 5. Bake NavMesh
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    private List<Vector2Int> GenerateCorridors(List<BSPNode> rooms)
    {
        // Implementation: Primary + Secondary pass
        // Returns list of all corridor tile positions
    }

    private void RenderToTilemap(List<BSPNode> rooms, List<Vector2Int> corridors)
    {
        // Paint floors
        foreach (var room in rooms)
        {
            floorTilemap.BoxFill(
                new Vector3Int((int)room.rect.x, (int)room.rect.y, 0),
                floorTile,
                (int)room.rect.xMin, (int)room.rect.yMin,
                (int)room.rect.xMax, (int)room.rect.yMax
            );
        }

        // Paint corridor floors
        foreach (var tile in corridors)
            floorTilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), floorTile);

        // Paint walls (around all floors)
        // Implementation: Check adjacent tiles, add walls where needed
    }
}
```

---

## Testing Strategy

### Unit Tests
- BSP splitting logic
- Corridor pathfinding
- Room template validation
- Doorway connection logic

### Integration Tests
- Full generation pipeline
- NavMesh baking
- Furniture placement
- Connectivity verification

### Playtest Checklist
- [ ] All rooms are reachable
- [ ] NavMesh covers >95% of floors
- [ ] No furniture blocks critical paths
- [ ] Office layout feels believable
- [ ] Performance acceptable (<5s generation, 60 FPS gameplay)
- [ ] Same seed produces same layout
- [ ] Different seeds produce varied layouts

---

## Performance Optimization

### Generation Phase
- Use `yield return null` in coroutine after each major step
- Batch tilemap operations (use BoxFill instead of SetTile loops)
- Profile with Unity Profiler to identify bottlenecks

### Runtime Phase
- Bake NavMesh once, don't update per frame
- Use object pooling for furniture prefabs
- Combine static tilemaps for better batching

---

## Future Enhancements

### Phase 4: Advanced Features (Post-Launch)
- **Multiple floors** - Stairs, elevators between levels
- **Destructible walls** - Allow players to modify layout
- **Dynamic events** - Fire spreads, water leaks affect layout
- **Biome variants** - Tech startup vs corporate vs government office themes
- **Save/Load layouts** - Serialize generated maps for reuse

### Community Features
- **Map sharing** - Players share seed codes
- **Level editor mode** - Hybrid procedural + manual editing
- **Workshop support** - Custom room templates from community

---

## References & Resources

### Documentation
- Unity Tilemap API: https://docs.unity3d.com/Manual/Tilemap.html
- NavMeshPlus: https://github.com/h8man/NavMeshPlus
- BSP Algorithm: http://www.roguebasin.com/index.php?title=Basic_BSP_Dungeon_generation

### Example Projects
- Unity ProBuilder (for editor tools patterns)
- Dungeon Architect examples
- Binding of Isaac (room template system)

### Papers & Articles
- "Procedural Generation in Game Design" (Tanya X. Short)
- Wave Function Collapse: https://github.com/mxgmn/WaveFunctionCollapse

---

## Appendix: Code Snippets

### Room Template ScriptableObject

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Room Template", menuName = "Map Generation/Room Template")]
public class RoomTemplate : ScriptableObject
{
    [Header("Dimensions")]
    public Vector2Int size;

    [Header("Tiles")]
    public TileBase[] floorTiles;  // Flattened 2D array (size.x * size.y)
    public TileBase[] wallTiles;

    [Header("Connection Points")]
    public List<Vector2Int> northDoorways;
    public List<Vector2Int> southDoorways;
    public List<Vector2Int> eastDoorways;
    public List<Vector2Int> westDoorways;

    [Header("Furniture")]
    public List<FurnitureSpawnPoint> furniturePoints;

    [Header("Metadata")]
    public RoomType type;
    public int minRoomSize = 10;
    public int maxRoomSize = 20;
}

public enum RoomType
{
    Generic,
    Conference,
    Cubicles,
    BreakRoom,
    Reception,
    ServerRoom,
    Storage,
    Bathroom
}

[System.Serializable]
public class FurnitureSpawnPoint
{
    public Vector2Int position;
    public GameObject prefab;
    public float rotationDegrees;
}
```

### Editor Window

```csharp
using UnityEngine;
using UnityEditor;

public class MapGeneratorEditor : EditorWindow
{
    private int seed = 0;
    private int width = 100;
    private int height = 100;
    private MapGenerator generator;

    [MenuItem("Tools/Map Generator")]
    static void ShowWindow()
    {
        GetWindow<MapGeneratorEditor>("Map Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Map Generation Settings", EditorStyles.boldLabel);

        seed = EditorGUILayout.IntField("Seed (0 = random)", seed);
        width = EditorGUILayout.IntSlider("Width", width, 50, 500);
        height = EditorGUILayout.IntSlider("Height", height, 50, 500);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate New Map"))
        {
            FindGenerator();
            if (generator != null)
            {
                generator.GenerateMap(seed);
                EditorUtility.SetDirty(generator);
            }
        }

        if (GUILayout.Button("Regenerate (Same Seed)"))
        {
            FindGenerator();
            if (generator != null && seed != 0)
            {
                generator.GenerateMap(seed);
            }
        }

        if (GUILayout.Button("Clear Map"))
        {
            FindGenerator();
            if (generator != null)
            {
                generator.ClearMap();
            }
        }
    }

    void FindGenerator()
    {
        if (generator == null)
            generator = FindObjectOfType<MapGenerator>();
    }
}
```

---

## Conclusion

This plan provides a complete roadmap for implementing procedural map generation in Office-Mice. The hybrid BSP + Room Templates approach balances:
- **Efficiency** - Fast generation with minimal code
- **Quality** - Hand-crafted room details where it matters
- **Variety** - Procedural structure for replayability
- **Control** - Predictable, debuggable, tunable

**Start with Week 1** to build the foundation. Each phase adds value independently, allowing for incremental releases and testing.

---

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Author:** AI Analysis + Expert Validation
**Status:** Ready for Implementation
