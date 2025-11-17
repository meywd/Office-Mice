# Office-Mice Procedural Map Generation - System Architecture

**Document Version:** 1.0
**Created:** 2025-11-17
**Author:** BMAD Master Executor
**Status:** Architectural Blueprint - Ready for Implementation
**Scope:** Complete system architecture for procedural generation

---

## Executive Summary

This document defines the complete system architecture for Office-Mice's procedural map generation system. The architecture follows a **layered, modular design** that separates concerns, enables testing, and supports future extensibility while maintaining compatibility with existing Unity systems.

**Core Architectural Principles:**
- **Separation of Concerns:** Clear boundaries between generation phases
- **Interface-Driven Design:** Loose coupling through well-defined contracts
- **Data-Driven Configuration:** ScriptableObject-based content system
- **Performance First:** Object pooling, coroutines, efficient algorithms
- **Unity Native:** Leveraging Unity's strengths (Tilemap, NavMesh, serialization)

---

## System Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  (Unity Editor UI, Runtime Debug Visualization, Loading UI) │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                   Orchestration Layer                        │
│       (MapGenerationController - Coordinates Phases)         │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┴────────────────┐
           │                                │
┌──────────▼─────────┐          ┌──────────▼──────────────┐
│   Phase 1: BSP     │          │  Phase 2: Content       │
│   Generation       │──────────▶  Population             │
│                    │          │                         │
│ - BSPGenerator     │          │ - SpawnPointManager     │
│ - RoomCreator      │          │ - ResourceDistributor   │
│ - CorridorBuilder  │          │ - SpecialRoomManager    │
│ - TilemapRenderer  │          │ - BiomeManager          │
└────────────────────┘          └─────────────────────────┘
           │                                │
           │                                │
┌──────────▼────────────────────────────────▼──────────────┐
│                     Data Layer                            │
│  (ScriptableObjects: Templates, Configs, Spawn Tables)    │
└───────────────────────────────────────────────────────────┘
           │
┌──────────▼────────────────────────────────────────────────┐
│                   Integration Layer                        │
│  (Unity APIs: Tilemap, NavMesh, Object Pooling, Physics)  │
└───────────────────────────────────────────────────────────┘
```

### Architectural Layers

#### 1. Presentation Layer
**Responsibility:** User interface and visualization
**Components:**
- MapGeneratorWindow (Editor UI)
- GizmoVisualization (Debug display)
- LoadingUI (Runtime progress)
- PerformanceMonitor (Analytics display)

#### 2. Orchestration Layer
**Responsibility:** Coordinate generation phases and manage flow
**Components:**
- MapGenerationController (Main orchestrator)
- GenerationPipeline (Phase management)
- ProgressTracker (Status reporting)
- ValidationSystem (Quality assurance)

#### 3. Generation Layer
**Responsibility:** Core generation algorithms and content population
**Components:**
- BSPGenerator (Room layout)
- CorridorGenerator (Connectivity)
- TilemapRenderer (Visual output)
- ContentPopulator (Asset integration)

#### 4. Data Layer
**Responsibility:** Configuration and template management
**Components:**
- ScriptableObject configurations
- Template definitions
- Spawn tables and rules
- Biome and theme data

#### 5. Integration Layer
**Responsibility:** Unity engine integration and platform abstraction
**Components:**
- Unity Tilemap API
- NavMeshPlus integration
- Object pooling system
- Physics and collision detection

---

## Core System Components

### 1. Map Generation Controller

```csharp
public class MapGenerationController : MonoBehaviour
{
    [Header("Configuration")]
    public MapGenerationSettings settings;
    public BiomeConfiguration biome;
    
    [Header("Components")]
    private IMapGenerator mapGenerator;
    private IContentPopulator contentPopulator;
    private NavMeshManager navMeshManager;
    private ProgressTracker progressTracker;
    
    public IEnumerator GenerateMapCoroutine(int seed = 0)
    {
        progressTracker.StartGeneration();
        
        // Phase 1: Core Generation
        yield return StartCoroutine(GenerateCoreCoroutine(seed));
        
        // Phase 2: Content Population
        yield return StartCoroutine(PopulateContentCoroutine());
        
        // Phase 3: Finalization
        yield return StartCoroutine(FinalizeMapCoroutine());
        
        progressTracker.CompleteGeneration();
    }
}
```

**Responsibilities:**
- Coordinate generation phases
- Manage coroutines and progress
- Handle errors and recovery
- Provide public API for external systems

### 2. BSP Generation System

```csharp
public class BSPGenerator : IRoomGenerator
{
    public List<RoomData> GenerateRooms(MapGenerationSettings settings)
    {
        var root = new BSPNode(settings.mapBounds);
        root.Split(settings.minRoomSize, settings.maxRoomSize, settings.seed);
        
        var leaves = root.GetLeaves();
        var rooms = new List<RoomData>();
        
        foreach (var leaf in leaves)
        {
            var room = CreateRoomFromLeaf(leaf, settings);
            rooms.Add(room);
        }
        
        return rooms;
    }
    
    private RoomData CreateRoomFromLeaf(BSPNode leaf, MapGenerationSettings settings)
    {
        // Create room smaller than partition with margins
        var margin = settings.roomMargin;
        var roomBounds = new RectInt(
            leaf.partition.x + margin,
            leaf.partition.y + margin,
            leaf.partition.width - (margin * 2),
            leaf.partition.height - (margin * 2)
        );
        
        return new RoomData
        {
            bounds = roomBounds,
            type = ClassifyRoom(roomBounds, leaf.depth),
            depth = leaf.depth,
            doorways = CalculateDoorways(roomBounds)
        };
    }
}
```

**Key Features:**
- Recursive space partitioning
- Configurable room size ranges
- Deterministic seed-based generation
- Automatic room classification
- Margin system for corridor space

### 3. Corridor Generation System

```csharp
public class CorridorGenerator : ICorridorGenerator
{
    public List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings)
    {
        // Two-pass corridor system
        var primaryCorridors = GeneratePrimaryCorridors(rooms, settings);
        var secondaryCorridors = GenerateSecondaryCorridors(rooms, primaryCorridors, settings);
        
        return primaryCorridors.Concat(secondaryCorridors).ToList();
    }
    
    private List<CorridorData> GeneratePrimaryCorridors(List<RoomData> rooms, MapGenerationSettings settings)
    {
        // Identify core rooms (largest from each major partition)
        var coreRooms = IdentifyCoreRooms(rooms);
        
        // Connect core rooms using MST for optimal connectivity
        return BuildMSTConnections(coreRooms, settings);
    }
    
    private CorridorData BuildCorridor(RoomData start, RoomData end, int width)
    {
        var pathfinder = new AStarPathfinder();
        var path = pathfinder.FindPath(start.Center, end.Center, GetObstacleMap());
        
        return new CorridorData
        {
            startRoom = start,
            endRoom = end,
            width = width,
            tiles = SmoothPath(path),
            pathType = CorridorType.Primary
        };
    }
}
```

**Key Features:**
- Two-pass hierarchical system
- A* pathfinding with Manhattan heuristic
- MST optimization for minimal corridor length
- Path smoothing for natural appearance
- Configurable corridor widths

### 4. Content Population System

```csharp
public class ContentPopulator : IContentPopulator
{
    public void PopulateContent(MapData map, BiomeConfiguration biome)
    {
        PopulateFurniture(map, biome);
        PopulateSpawnPoints(map, biome);
        PopulateResources(map, biome);
        ApplyBiomeTheme(map, biome);
    }
    
    private void PopulateFurniture(MapData map, BiomeConfiguration biome)
    {
        foreach (var room in map.rooms)
        {
            var template = biome.GetRoomTemplate(room.type);
            var furniture = template.GetFurnitureForRoom(room);
            
            foreach (var furnitureItem in furniture)
            {
                var position = CalculateFurniturePosition(room, furnitureItem);
                if (IsValidPosition(position, furnitureItem.minDistanceFromWalls))
                {
                    PlaceFurniture(furnitureItem.prefab, position, furnitureItem.rotation);
                }
            }
        }
    }
}
```

**Key Features:**
- Template-based furniture placement
- Room-type specific content rules
- Collision detection and validation
- Biome theme application
- Resource distribution balancing

---

## Data Architecture

### Core Data Models

```csharp
[System.Serializable]
public class MapData
{
    public List<RoomData> rooms;
    public List<CorridorData> corridors;
    public Vector2Int mapSize;
    public int generationSeed;
    public BiomeConfiguration biome;
    public float generationTime;
    
    public bool Validate()
    {
        return rooms.All(r => r.Validate()) && 
               corridors.All(c => c.Validate()) &&
               ValidateConnectivity();
    }
}

[System.Serializable]
public struct RoomData
{
    public RectInt bounds;
    public RoomType type;
    public int depth;
    public Vector2Int[] doorways;
    public List<FurnitureInstance> furniture;
    public List<SpawnPoint> spawnPoints;
    
    public Vector2Int Center => new Vector2Int(
        bounds.x + bounds.width / 2,
        bounds.y + bounds.height / 2
    );
    
    public bool Validate()
    {
        return bounds.width > 0 && bounds.height > 0 &&
               bounds.width >= GetMinSize(type) &&
               bounds.height >= GetMinSize(type);
    }
}

[System.Serializable]
public class CorridorData
{
    public RoomData startRoom;
    public RoomData endRoom;
    public List<Vector2Int> tiles;
    public int width;
    public CorridorType pathType;
    
    public bool Validate()
    {
        return tiles != null && tiles.Count > 0 && 
               startRoom != null && endRoom != null &&
               width >= 3; // Minimum corridor width
    }
}
```

### Configuration System

```csharp
[CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "OfficeMice/Generation Settings")]
public class MapGenerationSettings : ScriptableObject
{
    [Header("Map Parameters")]
    public Vector2Int mapSize = new Vector2Int(100, 100);
    public int roomCount = 15;
    public int minRoomSize = 8;
    public int maxRoomSize = 20;
    public int roomMargin = 2;
    
    [Header("Corridor Settings")]
    public int primaryCorridorWidth = 5;
    public int secondaryCorridorWidth = 3;
    public bool enablePathSmoothing = true;
    
    [Header("Generation Settings")]
    public bool deterministicGeneration = true;
    public int defaultSeed = 0;
    public bool enableTwoPassCorridors = true;
    
    public bool Validate()
    {
        return mapSize.x > 0 && mapSize.y > 0 &&
               roomCount > 0 && roomCount <= 500 &&
               minRoomSize > 0 && maxRoomSize > minRoomSize &&
               roomMargin >= 0 &&
               primaryCorridorWidth >= 3 && secondaryCorridorWidth >= 3;
    }
}
```

---

## Interface Contracts

### Core Interfaces

```csharp
public interface IMapGenerator
{
    IEnumerator<MapData> GenerateMapAsync(MapGenerationSettings settings, int seed = 0);
    MapData GenerateMap(MapGenerationSettings settings, int seed = 0);
    bool ValidateSettings(MapGenerationSettings settings);
}

public interface IRoomGenerator
{
    List<RoomData> GenerateRooms(MapGenerationSettings settings);
    RoomData GenerateRoom(RectInt bounds, RoomType type, int seed);
    bool ValidateRoom(RoomData room);
}

public interface ICorridorGenerator
{
    List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings);
    CorridorData BuildCorridor(RoomData start, RoomData end, int width);
    bool ValidateConnectivity(List<RoomData> rooms, List<CorridorData> corridors);
}

public interface IContentPopulator
{
    void PopulateContent(MapData map, BiomeConfiguration biome);
    void PopulateFurniture(MapData map, BiomeConfiguration biome);
    void PopulateSpawnPoints(MapData map, BiomeConfiguration biome);
    void PopulateResources(MapData map, BiomeConfiguration biome);
}
```

### Supporting Interfaces

```csharp
public interface IPathfinder
{
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool[,] obstacles);
    bool IsPathValid(List<Vector2Int> path);
    float CalculatePathCost(List<Vector2Int> path);
}

public interface ITileRenderer
{
    void RenderMap(MapData map, Tilemap[] tilemaps);
    void RenderRoom(RoomData room, Tilemap floorTilemap, Tilemap wallTilemap);
    void RenderCorridor(CorridorData corridor, Tilemap floorTilemap);
    void ClearTilemaps(Tilemap[] tilemaps);
}

public interface IAssetLoader
{
    TileBase LoadTile(string tileName);
    GameObject LoadPrefab(string prefabPath);
    T LoadAsset<T>(string assetPath) where T : Object;
    void PreloadAssets(string[] assetPaths);
    void UnloadUnusedAssets();
}
```

---

## Performance Architecture

### Object Pooling System

```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly Func<T> createFunc;
    private readonly Action<T> resetAction;
    private readonly int maxCapacity;
    
    public ObjectPool(Func<T> createFunc = null, Action<T> resetAction = null, int maxCapacity = 100)
    {
        this.createFunc = createFunc ?? (() => new T());
        this.resetAction = resetAction;
        this.maxCapacity = maxCapacity;
    }
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            var item = pool.Dequeue();
            resetAction?.Invoke(item);
            return item;
        }
        return createFunc();
    }
    
    public void Return(T item)
    {
        if (pool.Count < maxCapacity)
        {
            pool.Enqueue(item);
        }
    }
    
    public void Clear()
    {
        pool.Clear();
    }
}

// Specialized pools for common objects
public class AStarNodePool : ObjectPool<AStarNode>
{
    public AStarNodePool() : base(
        createFunc: () => new AStarNode(),
        resetAction: node => node.Reset(),
        maxCapacity: 1000
    ) { }
}

public class FurniturePool : ObjectPool<GameObject>
{
    public FurniturePool() : base(
        createFunc: () => new GameObject("PooledFurniture"),
        resetAction: go => {
            go.SetActive(false);
            go.transform.SetParent(null);
        },
        maxCapacity: 500
    ) { }
}
```

### Coroutine-Based Generation

```csharp
public class CoroutineGenerationPipeline
{
    private readonly FrameTimeBudgeter budgeter;
    private readonly ProgressTracker progress;
    
    public IEnumerator GenerateMapCoroutine(MapGenerationSettings settings)
    {
        budgeter.Reset();
        progress.StartGeneration();
        
        // Phase 1: BSP Generation (budget: 5ms)
        yield return StartCoroutine(BSPGenerationCoroutine(settings));
        
        // Phase 2: Corridor Generation (budget: 10ms)
        yield return StartCoroutine(CorridorGenerationCoroutine(settings));
        
        // Phase 3: Content Population (budget: 15ms)
        yield return StartCoroutine(ContentPopulationCoroutine(settings));
        
        // Phase 4: Finalization (budget: 5ms)
        yield return StartCoroutine(FinalizationCoroutine(settings));
        
        progress.CompleteGeneration();
    }
    
    private IEnumerator BSPGenerationCoroutine(MapGenerationSettings settings)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Process BSP in chunks
        while (!bspGenerationComplete)
        {
            ProcessBSPChunk();
            
            if (stopwatch.ElapsedMilliseconds > budgeter.RemainingBudget)
            {
                progress.UpdateProgress(0.25f, "Generating rooms...");
                yield return null;
                stopwatch.Restart();
            }
        }
        
        progress.UpdateProgress(0.5f, "Rooms generated");
    }
}
```

---

## Testing Architecture

### Test Structure

```
Assets/Tests/
├── EditMode/
│   ├── MapGeneration.EditMode.Tests.asmdef
│   ├── Core/
│   │   ├── BSPNodeTests.cs
│   │   ├── RoomDataTests.cs
│   │   ├── CorridorDataTests.cs
│   │   └── MapDataTests.cs
│   ├── Generators/
│   │   ├── BSPGeneratorTests.cs
│   │   ├── CorridorGeneratorTests.cs
│   │   └── TilemapRendererTests.cs
│   └── Utilities/
│       ├── PathfindingTests.cs
│       ├── ObjectPoolTests.cs
│       └── SerializationTests.cs
└── PlayMode/
    ├── MapGeneration.PlayMode.Tests.asmdef
    ├── Integration/
    │   ├── FullGenerationTests.cs
    │   ├── NavMeshTests.cs
    │   └── AssetLoadingTests.cs
    └── EndToEnd/
        ├── GameplayTests.cs
        └── PerformanceTests.cs
```

### Test Data Factory

```csharp
public static class MapGenerationTestDataFactory
{
    public static MapGenerationSettings CreateStandardSettings()
    {
        return new MapGenerationSettings
        {
            mapSize = new Vector2Int(100, 100),
            roomCount = 15,
            minRoomSize = 8,
            maxRoomSize = 20,
            roomMargin = 2
        };
    }
    
    public static RoomData CreateTestRoom(RoomType type, int width = 10, int height = 10)
    {
        return new RoomData
        {
            bounds = new RectInt(0, 0, width, height),
            type = type,
            depth = 1,
            doorways = new Vector2Int[] { new Vector2Int(width/2, 0) }
        };
    }
    
    public static List<RoomData> CreateTestRoomSet(int count)
    {
        var rooms = new List<RoomData>();
        for (int i = 0; i < count; i++)
        {
            rooms.Add(CreateTestRoom(RoomType.Office, 8 + i, 8 + i));
        }
        return rooms;
    }
}
```

---

## Integration Architecture

### Unity System Integration

#### NavMesh Integration
```csharp
public class NavMeshManager : MonoBehaviour
{
    private NavMeshSurface2d navMeshSurface;
    private const float MIN_COVERAGE = 0.95f;
    
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
        
        if (coverage < MIN_COVERAGE)
        {
            Debug.LogWarning($"NavMesh coverage {coverage:P} below threshold {MIN_COVERAGE:P}");
            yield return StartCoroutine(FixNavMeshGaps());
        }
        
        Debug.Log($"NavMesh built with {coverage:P} coverage");
    }
    
    private IEnumerator FixNavMeshGaps()
    {
        // Attempt to fix common NavMesh issues
        navMeshSurface.minRegionArea = 1f;
        navMeshSurface.voxelSize = 0.15f;
        navMeshSurface.BuildNavMesh();
        yield return new WaitForEndOfFrame();
    }
}
```

#### Tilemap Integration
```csharp
public class TilemapRenderer : ITileRenderer
{
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap decorTilemap;
    
    private TileAssetLoader tileLoader;
    
    public void RenderMap(MapData map, BiomeConfiguration biome)
    {
        ClearTilemaps();
        
        // Render rooms
        foreach (var room in map.rooms)
        {
            RenderRoom(room, biome);
        }
        
        // Render corridors
        foreach (var corridor in map.corridors)
        {
            RenderCorridor(corridor, biome);
        }
        
        // Optimize tilemap batching
        OptimizeTilemaps();
    }
    
    private void RenderRoom(RoomData room, BiomeConfiguration biome)
    {
        var template = biome.GetRoomTemplate(room.type);
        
        // Batch fill floor tiles
        floorTilemap.BoxFill(
            new Vector3Int(room.bounds.x, room.bounds.y, 0),
            template.GetRandomFloorTile(),
            room.bounds.xMin, room.bounds.yMin,
            room.bounds.xMax, room.bounds.yMax
        );
        
        // Render wall tiles
        RenderWalls(room.bounds, template.GetWallTile());
    }
}
```

---

## Deployment Architecture

### CI/CD Pipeline

```yaml
# .github/workflows/map-generation-build.yml
name: Map Generation Build & Deploy

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup Unity
        uses: game-ci/unity-test-runner@v2
        with:
          unityVersion: 2022.3.0f1
          
      - name: Run Tests
        uses: game-ci/unity-test-runner@v2
        with:
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          
  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build WebGL
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: WebGL
          
      - name: Deploy to Cloudflare
        uses: cloudflare/wrangler-action@v1
        with:
          apiToken: ${{ secrets.CLOUDFLARE_API_TOKEN }}
```

### Performance Monitoring

```csharp
public class ProductionMonitor : MonoBehaviour
{
    [Header("Monitoring")]
    private bool enableMonitoring = true;
    private float reportInterval = 60f; // seconds
    
    private PerformanceMetrics metrics;
    
    private void Start()
    {
        if (enableMonitoring)
        {
            metrics = new PerformanceMetrics();
            StartCoroutine(MonitoringCoroutine());
        }
    }
    
    private IEnumerator MonitoringCoroutine()
    {
        while (true)
        {
            metrics.CollectFrameData();
            
            if (Time.time >= reportInterval)
            {
                ReportMetrics();
                yield return new WaitForSeconds(reportInterval);
            }
            else
            {
                yield return null;
            }
        }
    }
    
    private void ReportMetrics()
    {
        var report = new AnalyticsEvent
        {
            eventName = "performance_report",
            parameters = new Dictionary<string, object>
            {
                ["avg_fps"] = metrics.AverageFPS,
                ["frame_time_ms"] = metrics.AverageFrameTime * 1000,
                ["memory_mb"] = metrics.MemoryUsageMB,
                ["gc_allocations"] = metrics.GCAllocations,
                ["generation_time_ms"] = metrics.LastGenerationTime * 1000
            }
        };
        
        Analytics.SendEvent(report);
        metrics.Reset();
    }
}
```

---

## Future Extensibility

### Plugin Architecture
```csharp
public interface IMapGeneratorPlugin
{
    string Name { get; }
    string Version { get; }
    bool IsCompatible(string unityVersion);
    
    void Initialize(MapGenerationContext context);
    void OnGenerationStart(MapGenerationSettings settings);
    void OnRoomGenerated(RoomData room);
    void OnCorridorGenerated(CorridorData corridor);
    void OnGenerationComplete(MapData map);
    void Cleanup();
}

public class PluginManager
{
    private List<IMapGeneratorPlugin> plugins = new List<IMapGeneratorPlugin>();
    
    public void LoadPlugins()
    {
        // Load plugins from Assets/MapGeneration/Plugins/
        var pluginPaths = Directory.GetFiles("Assets/MapGeneration/Plugins/", "*.dll");
        
        foreach (var path in pluginPaths)
        {
            var assembly = Assembly.LoadFrom(path);
            var types = assembly.GetTypes()
                .Where(t => typeof(IMapGeneratorPlugin).IsAssignableFrom(t));
                
            foreach (var type in types)
            {
                var plugin = Activator.CreateInstance(type) as IMapGeneratorPlugin;
                if (plugin != null && plugin.IsCompatible(Application.unityVersion))
                {
                    plugins.Add(plugin);
                    plugin.Initialize(new MapGenerationContext());
                }
            }
        }
    }
}
```

---

## Architecture Decision Records (ADRs)

### ADR-001: BSP Algorithm Selection
**Status:** Accepted
**Date:** 2025-11-17
**Decision:** Use Binary Space Partitioning for room generation

**Options Considered:**
- BSP: Excellent for rectangular office layouts
- Cellular Automata: Better for organic caves
- WFC: Most flexible but complex
- Graph-based: Good for flow but needs BSP foundation

**Decision Rationale:**
- Office environments naturally rectangular
- BSP guarantees non-overlapping rooms
- O(n log n) performance is predictable
- Easy to debug and visualize

---

### ADR-002: Two-Pass Corridor System
**Status:** Accepted
**Date:** 2025-11-17
**Decision:** Implement hierarchical corridor generation

**Options Considered:**
- Direct MST: Minimal corridors, maze-like
- Two-pass: Realistic office flow
- Manual: Designer-specified connections

**Decision Rationale:**
- Creates believable office layouts
- Main corridors provide clear navigation
- Secondary corridors add exploration
- Maintains 100% connectivity guarantee

---

### ADR-003: ScriptableObject Configuration
**Status:** Accepted
**Date:** 2025-11-17
**Decision:** Use ScriptableObjects for all configuration

**Options Considered:**
- ScriptableObjects: Unity-native, inspector-friendly
- JSON files: External editing possible
- Hard-coded: Fastest but inflexible
- Custom asset format: Maximum flexibility

**Decision Rationale:**
- Designer-friendly workflow
- Unity serialization support
- Runtime asset loading
- Easy validation and debugging

---

## Conclusion

This architecture provides a solid foundation for implementing Office-Mice's procedural map generation system. The modular design enables:

- **Incremental Development:** Each component can be developed and tested independently
- **Performance Optimization:** Clear separation allows targeted optimization
- **Future Extensibility:** Plugin system supports custom generators
- **Maintainability:** Interface-driven design enables easy modifications
- **Quality Assurance:** Comprehensive testing architecture ensures reliability

The architecture balances technical excellence with practical implementation considerations, ensuring the system can be delivered within the 6-week timeline while meeting all performance and quality targets.

---

**Document Status:** Complete and Ready for Implementation
**Next Phase:** Epic Breakdown and Story Creation
**Review Date:** 2025-11-24 (1 week after creation)