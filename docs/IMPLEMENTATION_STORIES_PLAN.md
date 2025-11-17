# Office-Mice Procedural Generation - Implementation Stories & Plan

**Document Version:** 1.0
**Created:** 2025-11-17
**Author:** BMAD Master Executor
**Status:** Ready for Implementation
**Total Stories:** 29 across 4 phases
**Estimated Timeline:** 15 days

---

## Executive Summary

This document contains **29 detailed user stories** organized into 4 implementation phases for Office-Mice's procedural map generation system. Each story includes acceptance criteria, technical specifications, and success metrics derived from comprehensive architectural analysis.

**Implementation Approach:** Test-Driven Development (TDD) with 90%+ coverage target
**Performance Targets:** <3s generation, 60 FPS gameplay, 95% NavMesh coverage
**Asset Integration:** 691 tiles, existing prefabs, WaveSpawner compatibility

---

## 🏗️ PHASE 0: FOUNDATION & SETUP (Days 1-2)
**Focus:** Core data models, testing infrastructure, configuration system

### Epic: Data Architecture Foundation

#### Story 0.1: Core Data Models
**As a developer**, I want well-defined data structures so that I can reliably represent map data

**Acceptance Criteria:**
- [ ] MapData class with room/corridor collections
- [ ] RoomData struct with position, size, type
- [ ] CorridorData class with tile path and width
- [ ] BSPNode class for tree structure
- [ ] All models support Unity serialization

**Technical Specifications:**
```csharp
public class MapData
{
    public List<RoomData> rooms { get; set; }
    public List<CorridorData> corridors { get; set; }
    public Vector2Int mapSize { get; set; }
    public int generationSeed { get; set; }
}

public struct RoomData
{
    public RectInt bounds;
    public RoomType type;
    public Vector2Int[] doorways;
    public int depth;
}

public class CorridorData
{
    public List<Vector2Int> tiles;
    public int width;
    public RoomData startRoom;
    public RoomData endRoom;
}
```

**Success Metrics:**
- Memory usage: ~115KB for 100-room map
- Serialization time: <50ms
- Validation: 100% data integrity

---

#### Story 0.2: ScriptableObject Configuration System
**As a designer**, I want configuration assets so that I can tweak generation without code changes

**Acceptance Criteria:**
- [ ] RoomTemplate ScriptableObject with tile/furniture configs
- [ ] BiomeConfiguration ScriptableObject for theming
- [ ] SpawnTableConfiguration for enemy waves
- [ ] TilesetConfiguration for asset mapping
- [ ] All editable in Unity Inspector

**Technical Specifications:**
```csharp
[CreateAssetMenu(fileName = "RoomTemplate", menuName = "OfficeMice/Room Template")]
public class RoomTemplate : ScriptableObject
{
    public RoomType roomType;
    public Vector2Int minSize = new Vector2Int(5, 5);
    public Vector2Int maxSize = new Vector2Int(10, 10);
    public TilesetConfiguration tileset;
    public FurnitureSpawn[] furniture;
    public PickupSpawn[] pickups;
}
```

**Success Metrics:**
- Designer can create new room template in <5 minutes
- Zero code changes required for configuration updates
- Validation prevents invalid configurations

---

#### Story 0.3: Interface Contracts
**As a system architect**, I want clear interfaces so that components remain decoupled

**Acceptance Criteria:**
- [ ] IMapGenerator interface for generation pipeline
- [ ] IRoomGenerator interface for room creation
- [ ] ICorridorGenerator interface for pathfinding
- [ ] IContentPopulator interface for furniture/spawns
- [ ] All interfaces have mock implementations

**Technical Specifications:**
```csharp
public interface IMapGenerator
{
    MapData GenerateMap(MapGenerationSettings settings);
    bool ValidateMap(MapData map);
}

public interface IRoomGenerator
{
    List<RoomData> GenerateRooms(MapGenerationSettings settings);
    RoomData GenerateRoom(RectInt bounds, RoomType type);
}

public interface ICorridorGenerator
{
    List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings);
    bool ValidateConnectivity(List<RoomData> rooms, List<CorridorData> corridors);
}
```

**Success Metrics:**
- 100% interface coverage for core systems
- Mock implementations enable isolated unit testing
- Dependency injection works seamlessly

---

### Epic: Testing Infrastructure

#### Story 0.4: Test Framework Setup
**As a developer**, I want automated testing so that I can refactor confidently

**Acceptance Criteria:**
- [ ] Unity Test Framework integration
- [ ] Separate EditMode and PlayMode test assemblies
- [ ] Test data factories for reproducible tests
- [ ] Mock implementations for all interfaces
- [ ] CI/CD pipeline with test automation

**Technical Specifications:**
```
Assets/
├── Tests/
│   ├── EditMode/
│   │   ├── MapGeneration.EditMode.Tests.asmdef
│   │   ├── Core/
│   │   │   ├── BSPNodeTests.cs
│   │   │   ├── RoomDataTests.cs
│   │   │   └── CorridorDataTests.cs
│   │   └── Generators/
│   │       ├── RoomGeneratorTests.cs
│   │       └── CorridorGeneratorTests.cs
│   └── PlayMode/
│       ├── MapGeneration.PlayMode.Tests.asmdef
│       ├── Integration/
│       │   ├── FullGenerationTests.cs
│       │   └── NavMeshTests.cs
│       └── EndToEnd/
│           └── GameplayTests.cs
```

**Success Metrics:**
- 90%+ code coverage on core systems
- All tests run in <2 minutes
- Zero flaky tests
- CI/CD pipeline passes 100%

---

#### Story 0.5: Performance Benchmarking
**As a performance engineer**, I want benchmarks so that I can prevent regressions

**Acceptance Criteria:**
- [ ] Baseline performance metrics for 100-room maps
- [ ] Automated performance tests
- [ ] Memory usage tracking
- [ ] Generation time limits (<3 seconds)
- [ ] GC pressure monitoring (<500KB/frame)

**Technical Specifications:**
```csharp
[Test, Performance]
public void Test_MapGeneration_Performance()
{
    var settings = new MapGenerationSettings { roomCount = 100 };
    
    Measure.Method(() => generator.GenerateMap(settings))
        .WarmupCount(10)
        .MeasurementCount(100)
        .SampleCount(10)
        .GC()
        .Run();
}
```

**Success Metrics:**
- BSP Generation: <30ms for 100 rooms
- Corridor Generation: <1.5s for full connectivity
- Layout Optimization: <250ms
- Total Generation: <3s
- Memory Usage: <200MB runtime

---

## 🏛️ PHASE 1: CORE GENERATION (Days 3-7)
**Focus:** BSP algorithm, A* pathfinding, room connectivity, layout optimization

### Epic: BSP Room Generation

#### Story 1.1: BSP Algorithm Implementation
**As a player**, I want varied room layouts so that each map feels unique

**Acceptance Criteria:**
- [ ] Recursive space partitioning algorithm
- [ ] Configurable min/max room sizes
- [ ] Rectangular room generation
- [ ] Tree structure visualization with Gizmos
- [ ] Deterministic generation with seed support

**Technical Specifications:**
```csharp
public class BSPNode
{
    public RectInt partition;
    public RectInt room;
    public BSPNode parent;
    public BSPNode leftChild;
    public BSPNode rightChild;
    public bool splitHorizontally;
    public int splitPosition;
    public RoomType roomType;
    public int depth;
    
    public bool IsLeaf => leftChild == null && rightChild == null;
    public Vector2Int Center => new Vector2Int(
        room.x + room.width / 2,
        room.y + room.height / 2
    );
}
```

**Success Metrics:**
- O(n log n) time complexity
- Generates 10-100 rooms reliably
- Deterministic output for same seed
- No overlapping rooms
- Tree depth balanced (max depth: log2(n))

---

#### Story 1.2: Room Classification System
**As a designer**, I want room types so that maps have purposeful areas

**Acceptance Criteria:**
- [ ] RoomType enum (Office, Conference, BreakRoom, etc.)
- [ ] Automatic room classification based on size/position
- [ ] Room type distribution configuration
- [ ] Visual differentiation between room types
- [ ] Room type validation (min sizes, requirements)

**Technical Specifications:**
```csharp
public enum RoomType
{
    Office,          // 5x8 to 8x12
    Conference,      // 10x10 to 15x15
    BreakRoom,       // 8x8 to 12x12
    Storage,         // 4x6 to 8x10
    Lobby,           // 12x12 to 20x20
    ServerRoom,      // 6x8 to 10x12
    Security,        // 5x7 to 8x10
    BossRoom,        // 15x15 to 20x20
}

public class RoomClassifier
{
    public RoomType ClassifyRoom(RectInt room, int depth, Vector2Int mapSize)
    {
        // Classification logic based on size, position, depth
    }
}
```

**Success Metrics:**
- 100% rooms classified
- Distribution matches configuration
- Room types placed logically (boss room central, etc.)
- Minimum size requirements enforced

---

#### Story 1.3: Room Template Integration
**As a content creator**, I want detailed room templates so that rooms have character

**Acceptance Criteria:**
- [ ] Template-based room decoration
- [ ] Furniture placement within templates
- [ ] Doorway position specification
- [ ] Template rotation and mirroring
- [ ] Template validation and error handling

**Technical Specifications:**
```csharp
public class RoomTemplate : ScriptableObject
{
    [System.Serializable]
    public class FurnitureSpawn
    {
        public GameObject prefab;
        public Vector2 relativePosition; // 0-1 normalized
        public float rotation;
        public float spawnProbability = 1f;
        public int maxCount = 1;
        public float minDistanceFromWalls = 1f;
    }
    
    public FurnitureSpawn[] furniture;
    public Vector2Int[] potentialDoorways;
    public TileBase[] floorTiles;
    public TileBase[] wallTiles;
}
```

**Success Metrics:**
- Templates apply correctly 100% of time
- Furniture doesn't block doorways
- Templates can be rotated/mirrored
- Validation catches invalid templates

---

### Epic: Corridor Connectivity

#### Story 1.4: A* Pathfinding Implementation
**As a player**, I want connected rooms so that I can navigate the entire map

**Acceptance Criteria:**
- [ ] A* algorithm for corridor pathfinding
- [ ] Manhattan distance heuristic
- [ ] Obstacle avoidance (rooms, existing corridors)
- [ ] Path smoothing for natural corridors
- [ ] Configurable corridor width (3-5 tiles)

**Technical Specifications:**
```csharp
public class AStarPathfinder
{
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles)
    {
        var openSet = new PriorityQueue<AStarNode>();
        var closedSet = new HashSet<Vector2Int>();
        
        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            
            if (current.Position == end)
                return ReconstructPath(current);
                
            // Explore neighbors with Manhattan heuristic
            foreach (var neighbor in GetNeighbors(current.Position))
            {
                if (!IsValid(neighbor, obstacles)) continue;
                
                float gScore = current.GScore + 1;
                float hScore = ManhattanDistance(neighbor, end);
                float fScore = gScore + hScore;
                
                // Add to open set if better path found
            }
        }
        
        return null; // No path found
    }
}
```

**Success Metrics:**
- 100% room connectivity achieved
- Path length within 20% of optimal
- Corridor width consistent
- No corridor overlaps
- Path smoothing reduces jaggedness by 80%

---

#### Story 1.5: Two-Pass Corridor System
**As a designer**, I want realistic office flow so that maps feel believable

**Acceptance Criteria:**
- [ ] Primary pass: Connect core rooms (main hallways)
- [ ] Secondary pass: Connect minor rooms to main arteries
- [ ] Hierarchical corridor structure
- [ ] Corridor width variation (main vs secondary)
- [ ] 100% room connectivity guarantee

**Technical Specifications:**
```csharp
public class TwoPassCorridorGenerator
{
    public List<CorridorData> GenerateCorridors(List<RoomData> rooms)
    {
        // Pass 1: Identify and connect core rooms
        var coreRooms = IdentifyCoreRooms(rooms);
        var primaryCorridors = ConnectCoreRooms(coreRooms);
        
        // Pass 2: Connect remaining rooms to primary network
        var remainingRooms = rooms.Except(coreRooms).ToList();
        var secondaryCorridors = ConnectRemainingRooms(remainingRooms, primaryCorridors);
        
        return primaryCorridors.Concat(secondaryCorridors).ToList();
    }
}
```

**Success Metrics:**
- Main corridors form recognizable "spine"
- Secondary corridors branch logically
- Corridor width: 5 tiles (main), 3 tiles (secondary)
- 100% room connectivity
- Realistic office flow patterns

---

#### Story 1.6: MST Connection Optimization
**As a performance engineer**, I want efficient connections so that generation is fast

**Acceptance Criteria:**
- [ ] Prim's Minimum Spanning Tree algorithm
- [ ] Redundant corridor addition (15% for realism)
- [ ] Connection point optimization
- [ ] Loop creation for variety
- [ ] O(E log V) performance guarantee

**Technical Specifications:**
```csharp
public class MSTConnector
{
    public List<CorridorData> BuildMST(List<RoomData> rooms)
    {
        var mst = new List<CorridorData>();
        var visited = new HashSet<RoomData>();
        var edges = new PriorityQueue<CorridorEdge>();
        
        // Prim's algorithm implementation
        visited.Add(rooms[0]);
        AddEdges(rooms[0], edges);
        
        while (visited.Count < rooms.Count)
        {
            var edge = edges.Dequeue();
            if (!visited.Contains(edge.Target))
            {
                mst.Add(CreateCorridor(edge));
                visited.Add(edge.Target);
                AddEdges(edge.Target, edges);
            }
        }
        
        // Add 15% redundant corridors for loops
        AddRedundantCorridors(mst, rooms);
        
        return mst;
    }
}
```

**Success Metrics:**
- O(E log V) time complexity
- Total corridor length minimized
- 15% redundant corridors added
- Loop creation for navigation variety
- Performance: <50ms for 100 rooms

---

### Epic: Layout Optimization

#### Story 1.7: Force-Directed Layout
**As a designer**, I want optimized room positions so that layouts look natural

**Acceptance Criteria:**
- [ ] Force-directed layout algorithm
- [ ] Room spacing optimization
- [ ] Alignment and grid snapping
- [ ] Multi-criteria scoring function
- [ ] Convergence detection and stopping

**Technical Specifications:**
```csharp
public class ForceDirectedOptimizer
{
    public void OptimizeLayout(List<RoomData> rooms, int maxIterations = 50)
    {
        for (int i = 0; i < maxIterations; i++)
        {
            // Calculate repulsive forces between rooms
            var forces = CalculateRepulsiveForces(rooms);
            
            // Calculate attractive forces for connected rooms
            forces = CombineWithAttractiveForces(forces, rooms);
            
            // Apply forces with damping
            ApplyForces(rooms, forces, 0.1f);
            
            // Check convergence
            if (HasConverged(forces)) break;
        }
        
        // Snap to grid
        SnapToGrid(rooms);
    }
}
```

**Success Metrics:**
- Layout optimization: <250ms
- Room spacing: minimum 2 tiles
- Alignment: 90% rooms grid-aligned
- Convergence: <50 iterations typical
- Visual quality: natural office layouts

---

#### Story 1.8: Serialization System
**As a developer**, I want save/load functionality so that maps can be persisted

**Acceptance Criteria:**
- [ ] JSON serialization for development
- [ ] Binary serialization for production
- [ ] Version migration support
- [ ] Round-trip data integrity
- [ ] Compression for file size optimization

**Technical Specifications:**
```csharp
public class MapSerializer
{
    public string SerializeToJson(MapData map)
    {
        return JsonUtility.ToJson(map, true);
    }
    
    public byte[] SerializeToBinary(MapData map)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            // Custom binary format for efficiency
            WriteMapData(writer, map);
            return stream.ToArray();
        }
    }
    
    public MapData DeserializeFromJson(string json)
    {
        return JsonUtility.FromJson<MapData>(json);
    }
}
```

**Success Metrics:**
- JSON size: ~250KB for 100-room map
- Binary size: ~80KB (3x compression)
- Serialization time: <50ms
- Deserialization time: <50ms
- 100% round-trip accuracy

---

## 🎨 PHASE 2: CONTENT & FEATURES (Days 8-12)
**Focus:** Asset integration, spawn system, biome variation, content population

### Epic: Asset Integration

#### Story 2.1: Tile Asset Loading
**As a system**, I want efficient tile loading so that generation is fast

**Acceptance Criteria:**
- [ ] TileAssetLoader with caching
- [ ] Support for 691 existing tiles
- [ ] Tile grouping by type (floors, walls, decor)
- [ ] Weighted random tile selection
- [ ] Memory-efficient tile management

**Technical Specifications:**
```csharp
public class TileAssetLoader : MonoBehaviour
{
    private Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();
    private const string TILE_PATH = "Game/Layout/Palette_Assets/";
    
    public TileBase LoadTile(string tileName)
    {
        if (tileCache.ContainsKey(tileName))
            return tileCache[tileName];
            
        TileBase tile = Resources.Load<TileBase>(TILE_PATH + tileName);
        if (tile != null)
            tileCache[tileName] = tile;
            
        return tile;
    }
    
    public TileBase GetRandomTile(int[] indices, string prefix = "tile_")
    {
        int randomIndex = indices[Random.Range(0, indices.Length)];
        return LoadTile($"{prefix}{randomIndex}");
    }
}
```

**Success Metrics:**
- All 691 tiles loadable
- Cache hit rate: >95%
- Loading time: <100ms for all tiles
- Memory usage: <50MB for tile cache
- Weighted selection works correctly

---

#### Story 2.2: Furniture Placement System
**As a player**, I want realistic furniture so that offices feel lived-in

**Acceptance Criteria:**
- [ ] Procedural furniture placement
- [ ] Room-type specific furniture rules
- [ ] Collision detection and spacing
- [ ] Furniture rotation and variation
- [ ] Integration with existing prefabs

**Technical Specifications:**
```csharp
public class FurniturePlacer
{
    public void PlaceFurniture(RoomData room, RoomTemplate template)
    {
        foreach (var furnitureSpawn in template.furniture)
        {
            if (Random.value > furnitureSpawn.spawnProbability)
                continue;
                
            Vector3 worldPos = CalculateWorldPosition(room, furnitureSpawn);
            
            if (IsValidPosition(worldPos, furnitureSpawn.minDistanceFromWalls))
            {
                GameObject furniture = Instantiate(furnitureSpawn.prefab, worldPos, 
                    Quaternion.Euler(0, 0, furnitureSpawn.rotation));
                    
                ConfigureFurniture(furniture, room.type);
            }
        }
    }
}
```

**Success Metrics:**
- Furniture places without blocking paths
- Room-type rules followed 100%
- Collision detection prevents overlaps
- Rotation and variation applied correctly
- Integration with existing prefabs seamless

---

#### Story 2.3: Prefab Management
**As a content creator**, I want prefab management so that assets are organized

**Acceptance Criteria:**
- [ ] PrefabManager with caching
- [ ] Support for furniture, pickups, weapons
- [ ] Layer and sorting configuration
- [ ] Prefab validation and error handling
- [ ] Runtime prefab instantiation

**Technical Specifications:**
```csharp
public class PrefabManager : MonoBehaviour
{
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
    
    public GameObject LoadPrefab(string path)
    {
        if (prefabCache.ContainsKey(path))
            return prefabCache[path];
            
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
            prefabCache[path] = prefab;
            
        return prefab;
    }
    
    public GameObject SpawnPrefab(string path, Vector3 position, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(path);
        if (prefab == null) return null;
        
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);
        ConfigureLayers(instance);
        return instance;
    }
}
```

**Success Metrics:**
- Prefab cache hit rate: >90%
- All prefabs load without errors
- Layers and sorting configured correctly
- Runtime instantiation works
- Error handling prevents crashes

---

### Epic: Spawn System Integration

#### Story 2.4: Spawn Point Generation
**As the game**, I want spawn points so that enemies can appear

**Acceptance Criteria:**
- [ ] Automatic spawn point creation
- [ ] "Spawn Point" tag compatibility with WaveSpawner
- [ ] Strategic spawn placement (corners, doorways)
- [ ] Spawn point validation (no obstacles)
- [ ] Configurable spawn density per room

**Technical Specifications:**
```csharp
public class SpawnPointManager : MonoBehaviour
{
    public void CreateSpawnPointsInRoom(RoomData room)
    {
        Vector3[] positions = GetSpawnPositions(room);
        
        foreach (Vector3 pos in positions)
        {
            if (IsValidSpawnPosition(pos))
            {
                GameObject spawnPoint = new GameObject($"Spawn_{room.type}_{spawnCount++}");
                spawnPoint.transform.position = pos;
                spawnPoint.tag = "Spawn Point"; // Critical for WaveSpawner
                spawnPoints.Add(spawnPoint);
            }
        }
    }
    
    private Vector3[] GetSpawnPositions(RoomData room)
    {
        // Corners (offset from walls)
        // Doorways
        // Strategic positions based on room type
    }
}
```

**Success Metrics:**
- 100% compatibility with WaveSpawner
- Spawn points placed strategically
- No obstacles blocking spawns
- Configurable density works
- Minimum 2 spawn points per combat room

---

#### Story 2.5: Resource Distribution
**As a player**, I want balanced resources so that gameplay is fair

**Acceptance Criteria:**
- [ ] Health pickup placement (scarce)
- [ ] Ammo crate distribution (moderate)
- [ ] Food item placement (break rooms)
- [ ] Weapon spawning (loot rooms)
- [ ] Difficulty-based resource scaling

**Technical Specifications:**
```csharp
public class ResourceDistributor
{
    public void DistributeResources(List<RoomData> rooms, float difficulty)
    {
        foreach (var room in rooms)
        {
            var resources = GetResourcesForRoomType(room.type);
            
            foreach (var resource in resources)
            {
                int count = CalculateResourceCount(resource, difficulty, room);
                
                for (int i = 0; i < count; i++)
                {
                    Vector3 position = GetRandomPositionInRoom(room);
                    PlaceResource(resource.prefab, position);
                }
            }
        }
    }
}
```

**Success Metrics:**
- Resource distribution balanced
- Health pickups scarce (10-15% of rooms)
- Ammo moderate (30-40% of rooms)
- Food abundant in break rooms (80%)
- Difficulty scaling works correctly

---

#### Story 2.6: Special Room System
**As a player**, I want special rooms so that maps have landmarks

**Acceptance Criteria:**
- [ ] Boss room generation (largest, central)
- [ ] Break room with high resources
- [ ] Server room with tech theme
- [ ] Security room with enemy spawns
- [ ] Special room validation and placement

**Technical Specifications:**
```csharp
public class SpecialRoomGenerator
{
    public RoomData GenerateBossRoom(List<RoomData> rooms)
    {
        // Find most central, largest room
        var candidate = rooms.OrderByDescending(r => r.bounds.width * r.bounds.height)
                         .ThenBy(r => DistanceToCenter(r, mapSize))
                         .First();
        
        candidate.type = RoomType.BossRoom;
        ConfigureBossRoom(candidate);
        return candidate;
    }
}
```

**Success Metrics:**
- Boss room always present and central
- Break room has high resource density
- Server room has tech theming
- Security room has strategic spawn points
- Special rooms validated correctly

---

### Epic: Biome System

#### Story 2.7: Biome Configuration
**As a designer**, I want biome variation so that maps have different themes

**Acceptance Criteria:**
- [ ] BiomeConfiguration ScriptableObjects
- [ ] Multiple biome types (Modern, Industrial, etc.)
- [ ] Biome-specific tilesets and furniture
- [ ] Environmental settings (lighting, fog)
- [ ] Biome transition handling

**Technical Specifications:**
```csharp
[CreateAssetMenu(fileName = "BiomeConfiguration", menuName = "OfficeMice/Biome")]
public class BiomeConfiguration : ScriptableObject
{
    public enum BiomeType { ModernOffice, Industrial, HighTech, OldOffice }
    
    public BiomeType biomeType;
    public string biomeName;
    public TilesetConfiguration tileset;
    public RoomDistribution[] roomDistribution;
    public Color fogColor;
    public float fogDensity;
    public AudioClip ambientSound;
}
```

**Success Metrics:**
- Multiple biomes configurable
- Biome-specific assets applied correctly
- Environmental settings work
- Transitions handled smoothly
- Designer can create new biome in <10 minutes

---

#### Story 2.8: Dynamic Content Population
**As a player**, I want varied content so that each playthrough is unique

**Acceptance Criteria:**
- [ ] Probability-based content spawning
- [ ] Wave progression integration
- [ ] Enemy type variation by biome
- [ ] Pickup scarcity curves
- [ ] Content validation and balancing

**Technical Specifications:**
```csharp
public class ContentPopulator : IContentPopulator
{
    public void PopulateContent(MapData map, BiomeConfiguration biome)
    {
        var rng = new Random(map.generationSeed);
        
        // Populate based on biome probabilities
        foreach (var room in map.rooms)
        {
            PopulateRoom(room, biome, rng);
        }
        
        // Validate and balance
        ValidateContentBalance(map);
    }
}
```

**Success Metrics:**
- Content varies between seeds
- Probability curves work correctly
- Wave progression integrated
- Enemy types vary by biome
- Content balanced for gameplay

---

## ⚡ PHASE 3: POLISH & INTEGRATION (Days 13-15)
**Focus:** Performance optimization, editor tools, production deployment

### Epic: Performance Optimization

#### Story 3.1: Object Pooling Implementation
**As a performance engineer**, I want object pooling so that GC pressure is reduced

**Acceptance Criteria:**
- [ ] A* node pooling (95% reduction)
- [ ] Furniture object pooling
- [ ] Tile batch rendering
- [ ] Memory usage optimization
- [ ] GC pressure monitoring

**Technical Specifications:**
```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly Func<T> createFunc;
    private readonly Action<T> resetAction;
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            var item = pool.Dequeue();
            resetAction?.Invoke(item);
            return item;
        }
        return createFunc != null ? createFunc() : new T();
    }
    
    public void Return(T item)
    {
        pool.Enqueue(item);
    }
}
```

**Success Metrics:**
- GC allocation reduction: 95%
- Memory usage stable during generation
- Frame time consistent
- No memory leaks after 100 generations
- Object pool hit rate: >90%

---

#### Story 3.2: Coroutine-Based Generation
**As a player**, I want smooth generation so that the game doesn't freeze

**Acceptance Criteria:**
- [ ] Coroutine-based generation pipeline
- [ ] Loading bar with progress indication
- [ ] Frame-time budgeting (16.67ms)
- [ ] Generation step yielding
- [ ] Cancellation support

**Technical Specifications:**
```csharp
public class CoroutineMapGenerator : MonoBehaviour
{
    public IEnumerator GenerateMapCoroutine(MapGenerationSettings settings)
    {
        float startTime = Time.realtimeSinceStartup;
        
        // Phase 1: BSP Generation
        yield return StartCoroutine(GenerateBSPCoroutine(settings));
        UpdateProgress(0.25f);
        
        // Phase 2: Corridor Generation
        yield return StartCoroutine(GenerateCorridorsCoroutine(settings));
        UpdateProgress(0.5f);
        
        // Phase 3: Content Population
        yield return StartCoroutine(PopulateContentCoroutine(settings));
        UpdateProgress(0.75f);
        
        // Phase 4: Finalization
        yield return StartCoroutine(FinalizeMapCoroutine(settings));
        UpdateProgress(1.0f);
        
        Debug.Log($"Map generated in {Time.realtimeSinceStartup - startTime:F2} seconds");
    }
}
```

**Success Metrics:**
- Generation doesn't freeze UI
- Progress bar updates smoothly
- Frame time never exceeds 16.67ms
- Generation can be cancelled
- Total time: <3 seconds

---

#### Story 3.3: NavMesh Integration
**As an AI enemy**, I want NavMesh coverage so that I can navigate

**Acceptance Criteria:**
- [ ] Automatic NavMesh baking after generation
- [ ] 95%+ coverage validation
- [ ] NavMesh gap detection and fixing
- [ ] NavMeshPlus integration
- [ ] Multi-floor support preparation

**Technical Specifications:**
```csharp
public class NavMeshManager : MonoBehaviour
{
    public IEnumerator BuildNavMeshWithValidation()
    {
        yield return new WaitForEndOfFrame(); // Ensure geometry placed
        
        // Calculate expected walkable area
        float expectedArea = CalculateWalkableArea();
        
        // Build NavMesh
        navMeshSurface.BuildNavMesh();
        yield return new WaitForEndOfFrame();
        
        // Validate coverage
        float actualArea = CalculateNavMeshArea();
        float coverage = actualArea / expectedArea;
        
        if (coverage < 0.95f)
        {
            Debug.LogWarning($"NavMesh coverage: {coverage:P} (below 95%)");
            AttemptNavMeshFix();
        }
    }
}
```

**Success Metrics:**
- NavMesh coverage: >95%
- Automatic baking works
- Gap detection and fixing
- NavMeshPlus integration
- No navigation errors

---

### Epic: Editor Tools

#### Story 3.4: Custom Editor Window
**As a designer**, I want editor tools so that I can create and test maps

**Acceptance Criteria:**
- [ ] MapGeneratorWindow with tabbed interface
- [ ] Real-time parameter adjustment
- [ ] Seed-based reproducible generation
- [ ] Map preview and validation
- [ ] Export/import functionality

**Technical Specifications:**
```csharp
public class MapGeneratorWindow : EditorWindow
{
    [MenuItem("Tools/Map Generator")]
    static void ShowWindow() => GetWindow<MapGeneratorWindow>("Map Generator");
    
    void OnGUI()
    {
        // Parameters tab
        if (selectedTab == Tab.Parameters)
        {
            DrawParameterControls();
        }
        
        // Preview tab
        if (selectedTab == Tab.Preview)
        {
            DrawPreviewControls();
        }
        
        // Export tab
        if (selectedTab == Tab.Export)
        {
            DrawExportControls();
        }
    }
}
```

**Success Metrics:**
- Designer can generate map in <5 clicks
- Real-time parameter adjustment works
- Preview updates instantly
- Export/import functional
- Validation catches errors

---

#### Story 3.5: Gizmo Visualization
**As a developer**, I want visual debugging so that I can understand generation

**Acceptance Criteria:**
- [ ] BSP tree visualization
- [ ] Room boundary display
- [ ] Corridor path visualization
- [ ] Spawn point indicators
- [ ] Interactive gizmo controls

**Technical Specifications:**
```csharp
public class MapGenerator : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (showBSPNodes)
        {
            DrawBSPNodes();
        }
        
        if (showRooms)
        {
            DrawRoomBoundaries();
        }
        
        if (showCorridors)
        {
            DrawCorridorPaths();
        }
        
        if (showSpawnPoints)
        {
            DrawSpawnPoints();
        }
    }
}
```

**Success Metrics:**
- BSP tree clearly visible
- Room boundaries accurate
- Corridor paths traceable
- Spawn points identifiable
- Gizmos don't impact performance

---

#### Story 3.6: Asset Creation Workflows
**As a content creator**, I want creation tools so that I can make content efficiently

**Acceptance Criteria:**
- [ ] Room template creation wizard
- [ ] Biome configuration editor
- [ ] Tileset organization tools
- [ ] Batch asset operations
- [ ] Validation and error reporting

**Technical Specifications:**
```csharp
public class RoomTemplateWizard : ScriptableWizard
{
    [MenuItem("Assets/Create/OfficeMice/Room Template Wizard")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<RoomTemplateWizard>("Create Room Template");
    }
    
    void OnWizardCreate()
    {
        var template = CreateInstance<RoomTemplate>();
        template.roomType = selectedRoomType;
        template.minSize = minSize;
        template.maxSize = maxSize;
        
        AssetDatabase.CreateAsset(template, $"Assets/RoomTemplates/{templateName}.asset");
        AssetDatabase.SaveAssets();
    }
}
```

**Success Metrics:**
- Template creation: <2 minutes
- Configuration editor intuitive
- Batch operations work
- Validation prevents errors
- Error reporting helpful

---

### Epic: Production Deployment

#### Story 3.7: CI/CD Pipeline
**As a devops engineer**, I want automated deployment so that updates are reliable

**Acceptance Criteria:**
- [ ] GitHub Actions workflow
- [ ] Unity WebGL build automation
- [ ] Cloudflare Workers deployment
- [ ] Performance benchmarking
- [ ] Rollback capability

**Technical Specifications:**
```yaml
# .github/workflows/build-deploy.yml
name: Build and Deploy
on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build Unity WebGL
        uses: game-ci/unity-builder@v2
      - name: Deploy to Cloudflare
        uses: cloudflare/wrangler-action@v1
```

**Success Metrics:**
- Automated builds work
- Deployment successful 95%+ of time
- Performance benchmarks pass
- Rollback works within 5 minutes
- CI/CD pipeline completes in <10 minutes

---

#### Story 3.8: Production Monitoring
**As a producer**, I want analytics so that I can understand player behavior

**Acceptance Criteria:**
- [ ] Generation performance tracking
- [ ] Error reporting and logging
- [ ] Player telemetry integration
- [ ] A/B testing framework
- [ ] Remote configuration support

**Technical Specifications:**
```csharp
public class ProductionMonitor : MonoBehaviour
{
    public void TrackGenerationPerformance(float generationTime, int roomCount)
    {
        var analytics = new AnalyticsEvent
        {
            eventName = "map_generation",
            parameters = new Dictionary<string, object>
            {
                ["generation_time"] = generationTime,
                ["room_count"] = roomCount,
                ["seed"] = currentSeed
            }
        };
        
        Analytics.SendEvent(analytics);
    }
}
```

**Success Metrics:**
- Performance data collected
- Errors reported automatically
- Player telemetry useful
- A/B tests functional
- Remote configuration works

---

## 📊 IMPLEMENTATION SUMMARY

### Story Distribution by Phase
| Phase | Stories | Focus | Estimated Days |
|-------|---------|--------|----------------|
| **Phase 0** | 5 | Foundation & Testing | 2 days |
| **Phase 1** | 8 | Core Generation | 5 days |
| **Phase 2** | 8 | Content & Features | 5 days |
| **Phase 3** | 8 | Polish & Deployment | 3 days |
| **Total** | **29** | **Complete System** | **15 days** |

### Priority Matrix
| Priority | Stories | Focus |
|----------|---------|--------|
| **P0 (Critical)** | 8 | Core foundation (0.1, 0.2, 0.3, 1.1, 1.4, 2.1, 2.4, 3.3) |
| **P1 (High)** | 10 | Essential features (0.4, 0.5, 1.2, 1.5, 1.6, 1.7, 1.8, 2.2, 2.5, 3.1) |
| **P2 (Medium)** | 7 | Quality improvements (1.3, 2.3, 2.6, 2.7, 2.8, 3.2, 3.4) |
| **P3 (Low)** | 4 | Nice-to-have (3.5, 3.6, 3.7, 3.8) |

### Success Criteria by Phase

#### Phase 0 Success
- [ ] All data models implemented and unit tested
- [ ] ScriptableObject system functional
- [ ] Test framework with 90%+ coverage
- [ ] Performance baselines established

#### Phase 1 Success
- [ ] BSP generates 10-100 rooms in <500ms
- [ ] A* connects all rooms with 100% success
- [ ] Layout optimization completes in <250ms
- [ ] Serialization round-trip 100% accurate

#### Phase 2 Success
- [ ] All 691 tiles loadable and categorized
- [ ] Furniture places without blocking paths
- [ ] Spawn points compatible with WaveSpawner
- [ ] Resource distribution balanced across difficulties

#### Phase 3 Success
- [ ] Generation completes in <3 seconds total
- [ ] Gameplay maintains 60 FPS
- [ ] GC pressure <500KB per frame
- [ ] NavMesh coverage >95%
- [ ] Editor tools fully functional

---

## 🚀 NEXT STEPS

1. **Begin Phase 0 Implementation** - Start with P0 stories (0.1, 0.2, 0.3)
2. **Set Up TDD Environment** - Configure test framework before coding
3. **Follow Acceptance Criteria** - Each story must meet all criteria
4. **Validate Performance** - Check against benchmarks after each phase
5. **Iterate Based on Testing** - Use test results to refine implementation

---

**Document Status:** ✅ Complete and Ready for Implementation
**Last Updated:** 2025-11-17
**Total Implementation Stories:** 29
**Estimated Timeline:** 15 working days
**Success Rate Target:** 100% of acceptance criteria met