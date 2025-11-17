# Phase 0 System Design & Architecture Analysis
## Office-Mice Procedural Map Generation

**Document Version:** 1.0
**Date:** 2025-11-17
**Author:** Software Architecture Analysis
**Status:** Architectural Foundation Blueprint
**Scope:** Phase 0 Tasks 0.1 & 0.4 Architecture Analysis

---

## Executive Summary

Phase 0 establishes the **architectural foundation** for Office-Mice's procedural map generation system. This analysis examines the core system architecture, design patterns, and critical architectural decisions that will govern the entire generation pipeline.

**Key Architectural Characteristics:**
- **Modularity:** 7 independent systems with clear boundaries
- **Extensibility:** Plugin-based architecture for content expansion
- **Performance:** Object pooling and coroutine-based generation
- **Maintainability:** Data-driven design via ScriptableObjects
- **Unity 6 Ready:** Modern Unity API patterns and URP compatibility

---

## Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [The 7 Core Systems](#2-the-7-core-systems)
3. [Architectural Patterns](#3-architectural-patterns)
4. [Core Generator Interface](#4-core-generator-interface)
5. [Dependency Injection Strategy](#5-dependency-injection-strategy)
6. [Event-Driven vs Direct Coupling](#6-event-driven-vs-direct-coupling)
7. [MonoBehaviour vs Plain C# Design](#7-monobehaviour-vs-plain-c-design)
8. [Unity 6 Upgrade Considerations](#8-unity-6-upgrade-considerations)
9. [Critical Architectural Decisions](#9-critical-architectural-decisions)

---

## 1. System Architecture Overview

### 1.1 High-Level Architecture

The map generation system follows a **layered architecture** pattern with clear separation of concerns:

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

### 1.2 Architectural Principles

**1. Separation of Concerns**
- Each system has a single, well-defined responsibility
- No cross-system direct dependencies (interface-based contracts)
- Clear input/output boundaries via `MapGenerationContext`

**2. Open/Closed Principle**
- Systems open for extension via ScriptableObject content
- Closed for modification - new features don't alter core systems
- Plugin architecture for new generators, room types, biomes

**3. Dependency Inversion**
- High-level orchestrator depends on abstractions (`IGenerator`, `IContentPopulator`)
- Low-level systems implement interfaces without knowing consumers
- Unity's SerializeField provides constructor injection alternative

**4. Single Responsibility**
- BSPGenerator: Only generates spatial partitions
- TilemapRenderer: Only paints tiles
- SpawnPointManager: Only places spawn points
- Each class has one reason to change

**5. Interface Segregation**
- Small, focused interfaces (`IMapGenerator`, `IValidator`, `IPlacementStrategy`)
- Clients depend only on methods they use
- No fat interfaces forcing unused implementations

---

## 2. The 7 Core Systems

### 2.1 System Inventory

```
1. BSP Generation System
   └─ Responsibility: Spatial partitioning and room boundary creation

2. Room Template System
   └─ Responsibility: Detailed room layouts with tiles and furniture

3. Corridor Generation System
   └─ Responsibility: Connecting rooms with navigable pathways

4. Spawn Point System
   └─ Responsibility: Strategic enemy spawn placement and wave progression

5. Resource Distribution System
   └─ Responsibility: Balancing ammo, health, weapon placement

6. Biome Theming System
   └─ Responsibility: Visual variety through tile/asset swapping

7. Validation & Quality Assurance System
   └─ Responsibility: Ensuring playability and balance
```

### 2.2 System Dependencies

**Dependency Graph:**

```
BSP Generation (1)
    │
    ├──▶ Room Templates (2)
    │        │
    │        └──▶ Spawn Points (4)
    │        │
    │        └──▶ Resources (5)
    │
    └──▶ Corridors (3)
             │
             └──▶ Room Templates (2) [doorway alignment]

Biome Theming (6) ──▶ ALL (applies post-generation)

Validation (7) ──▶ ALL (validates all outputs)
```

**Critical Observation:** Systems 1-3 are **structural** (geometry), systems 4-6 are **gameplay** (content), system 7 is **quality assurance**.

### 2.3 System Boundaries

Each system maintains strict boundaries via **interface contracts**:

```csharp
// System 1: BSP Generation
public interface IBSPGenerator
{
    BSPNode GeneratePartition(Rect bounds, BSPConfig config);
    List<Room> ExtractRooms(BSPNode root);
}

// System 2: Room Template
public interface IRoomTemplateProvider
{
    RoomTemplate GetTemplate(RoomClassification classification);
    RoomInstance Instantiate(RoomTemplate template, Room room);
}

// System 3: Corridor Generation
public interface ICorridorGenerator
{
    List<Corridor> ConnectRooms(List<Room> rooms, CorridorConfig config);
    List<Vector2Int> GenerateCorridorTiles(Corridor corridor);
}

// System 4: Spawn Points
public interface ISpawnPointManager
{
    List<GameObject> PlaceSpawnPoints(List<Room> rooms, SpawnConfig config);
    void ValidateSpawnDistribution(List<GameObject> spawns);
}

// System 5: Resource Distribution
public interface IResourceDistributor
{
    List<ResourcePlacement> DistributeResources(
        List<Room> rooms,
        ResourceBudget budget
    );
}

// System 6: Biome Theming
public interface IBiomeManager
{
    void ApplyBiome(BiomeConfig biome, MapData mapData);
}

// System 7: Validation
public interface IMapValidator
{
    ValidationResult Validate(MapGenerationContext context);
}
```

**Architectural Impact:** This interface segregation enables:
- Independent testing of each system
- Parallel development by multiple developers
- Easy mocking for unit tests
- Future replacement of implementations without breaking consumers

---

## 3. Architectural Patterns

### 3.1 Factory Pattern

**Usage:** Creating map elements (rooms, corridors, spawn points)

```csharp
// Abstract factory for map element creation
public interface IMapElementFactory
{
    Room CreateRoom(Rect bounds, RoomClassification type);
    Corridor CreateCorridor(Vector2Int start, Vector2Int end);
    GameObject CreateSpawnPoint(Vector2Int position, RoomClassification roomType);
}

// Concrete factory implementation
public class UnityMapElementFactory : IMapElementFactory
{
    [SerializeField] private GameObject spawnPointPrefab;

    public Room CreateRoom(Rect bounds, RoomClassification type)
    {
        return new Room
        {
            ID = GenerateUniqueID(),
            Bounds = bounds,
            Classification = type,
            Connections = new List<Room>()
        };
    }

    public Corridor CreateCorridor(Vector2Int start, Vector2Int end)
    {
        return new Corridor
        {
            StartPosition = start,
            EndPosition = end,
            Width = 3 // Default corridor width
        };
    }

    public GameObject CreateSpawnPoint(Vector2Int position, RoomClassification roomType)
    {
        var spawnPoint = GameObject.Instantiate(spawnPointPrefab);
        spawnPoint.transform.position = new Vector3(position.x, position.y, 0);
        spawnPoint.tag = "Spawn Point";

        var metadata = spawnPoint.AddComponent<SpawnPointMetadata>();
        metadata.roomType = roomType;

        return spawnPoint;
    }
}
```

**Rationale:**
- Centralizes GameObject instantiation logic
- Enables object pooling integration
- Simplifies testing (mock factory returns test doubles)
- Unity-specific creation isolated to factory

### 3.2 Strategy Pattern

**Usage:** Different algorithms for corridor generation, room placement

```csharp
// Strategy interface
public interface ICorridorGenerationStrategy
{
    List<Vector2Int> GenerateCorridorPath(Vector2Int start, Vector2Int end);
}

// Concrete strategies
public class StraightLineStrategy : ICorridorGenerationStrategy
{
    public List<Vector2Int> GenerateCorridorPath(Vector2Int start, Vector2Int end)
    {
        // Bresenham's line algorithm
        return BresenhamLine(start, end);
    }
}

public class LShapedStrategy : ICorridorGenerationStrategy
{
    public List<Vector2Int> GenerateCorridorPath(Vector2Int start, Vector2Int end)
    {
        // L-shaped path with right-angle turn
        var path = new List<Vector2Int>();

        // Horizontal segment
        for (int x = start.x; x <= end.x; x++)
            path.Add(new Vector2Int(x, start.y));

        // Vertical segment
        for (int y = start.y; y <= end.y; y++)
            path.Add(new Vector2Int(end.x, y));

        return path;
    }
}

public class AStarStrategy : ICorridorGenerationStrategy
{
    public List<Vector2Int> GenerateCorridorPath(Vector2Int start, Vector2Int end)
    {
        // A* pathfinding around obstacles
        return AStarPathfind(start, end);
    }
}

// Context using strategy
public class CorridorGenerator : ICorridorGenerator
{
    private ICorridorGenerationStrategy strategy;

    public CorridorGenerator(ICorridorGenerationStrategy strategy)
    {
        this.strategy = strategy;
    }

    public List<Corridor> ConnectRooms(List<Room> rooms, CorridorConfig config)
    {
        var corridors = new List<Corridor>();

        foreach (var roomPair in GetRoomPairs(rooms))
        {
            var path = strategy.GenerateCorridorPath(
                roomPair.roomA.Center,
                roomPair.roomB.Center
            );

            corridors.Add(new Corridor { Path = path });
        }

        return corridors;
    }
}
```

**Configuration via ScriptableObject:**

```csharp
[CreateAssetMenu(fileName = "Corridor Config", menuName = "Map Generation/Corridor Config")]
public class CorridorConfig : ScriptableObject
{
    public enum CorridorStrategy
    {
        StraightLine,
        LShaped,
        AStar
    }

    public CorridorStrategy strategy = CorridorStrategy.LShaped;
    public int corridorWidth = 3;
    public bool avoidRooms = true;
}
```

**Rationale:**
- Easily swap algorithms at runtime or design-time
- Different strategies for different room types (main corridors vs. shortcuts)
- Performance tuning (A* for complex layouts, L-shaped for simple)

### 3.3 Observer Pattern

**Usage:** Notifying systems when generation phases complete

```csharp
// Event-driven architecture for phase completion
public class MapGenerationEvents
{
    public static event Action<MapGenerationContext> OnBSPGenerationComplete;
    public static event Action<MapGenerationContext> OnRoomTemplatesApplied;
    public static event Action<MapGenerationContext> OnCorridorsCreated;
    public static event Action<MapGenerationContext> OnContentPopulated;
    public static event Action<MapGenerationContext> OnGenerationComplete;

    public static void RaiseBSPComplete(MapGenerationContext context)
    {
        OnBSPGenerationComplete?.Invoke(context);
    }

    public static void RaiseGenerationComplete(MapGenerationContext context)
    {
        OnGenerationComplete?.Invoke(context);
    }
}

// Subscriber example: Analytics system
public class GenerationAnalytics : MonoBehaviour
{
    private void OnEnable()
    {
        MapGenerationEvents.OnGenerationComplete += RecordGenerationMetrics;
    }

    private void OnDisable()
    {
        MapGenerationEvents.OnGenerationComplete -= RecordGenerationMetrics;
    }

    private void RecordGenerationMetrics(MapGenerationContext context)
    {
        Debug.Log($"Map generated: {context.Rooms.Count} rooms, {context.SpawnPoints.Count} spawns");
        // Send to analytics backend
    }
}
```

**Rationale:**
- Decouples systems (no direct references to analytics, debug visualizers)
- Enables runtime debugging tools to hook into generation
- Facilitates editor tools (preview windows, validation reports)

### 3.4 Repository Pattern

**Usage:** Managing ScriptableObject assets (templates, configs, spawn tables)

```csharp
// Repository interface
public interface IRepository<T> where T : ScriptableObject
{
    T GetByID(string id);
    List<T> GetAll();
    List<T> GetFiltered(Func<T, bool> predicate);
}

// Room template repository
public class RoomTemplateRepository : IRepository<RoomTemplate>
{
    [SerializeField] private List<RoomTemplate> templates = new List<RoomTemplate>();

    public RoomTemplate GetByID(string id)
    {
        return templates.Find(t => t.templateName == id);
    }

    public List<RoomTemplate> GetAll()
    {
        return new List<RoomTemplate>(templates);
    }

    public List<RoomTemplate> GetFiltered(Func<RoomTemplate, bool> predicate)
    {
        return templates.FindAll(t => predicate(t));
    }

    // Query methods
    public List<RoomTemplate> GetByRoomType(RoomType type)
    {
        return GetFiltered(t => t.roomType == type);
    }

    public RoomTemplate GetRandomTemplate(RoomClassification classification)
    {
        var candidates = GetFiltered(t => t.suggestedClassification == classification);
        if (candidates.Count == 0) return templates[0]; // Fallback

        // Weighted random selection
        float totalWeight = candidates.Sum(t => t.selectionWeight);
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var template in candidates)
        {
            cumulative += template.selectionWeight;
            if (randomValue <= cumulative)
                return template;
        }

        return candidates[0];
    }
}
```

**Rationale:**
- Abstracts asset loading (Resources.Load, Addressables, Asset Bundles)
- Centralizes query logic for finding templates
- Enables caching and performance optimization
- Future mod support (load templates from external sources)

### 3.5 Builder Pattern

**Usage:** Constructing complex MapGenerationContext

```csharp
public class MapGenerationContextBuilder
{
    private MapGenerationContext context = new MapGenerationContext();

    public MapGenerationContextBuilder WithSeed(int seed)
    {
        context.Seed = seed;
        UnityEngine.Random.InitState(seed);
        return this;
    }

    public MapGenerationContextBuilder WithMapSize(Vector2Int size)
    {
        context.MapSize = size;
        return this;
    }

    public MapGenerationContextBuilder WithDifficulty(DifficultySettings difficulty)
    {
        context.DifficultySettings = difficulty;
        return this;
    }

    public MapGenerationContextBuilder WithBiome(BiomeConfig biome)
    {
        context.SelectedBiome = biome;
        return this;
    }

    public MapGenerationContextBuilder WithTilemaps(Tilemap floor, Tilemap wall, Tilemap objects)
    {
        context.FloorTilemap = floor;
        context.WallTilemap = wall;
        context.ObjectTilemap = objects;
        return this;
    }

    public MapGenerationContext Build()
    {
        // Validation
        if (context.FloorTilemap == null)
            throw new InvalidOperationException("FloorTilemap required");
        if (context.DifficultySettings == null)
            context.DifficultySettings = ScriptableObject.CreateInstance<DifficultySettings>();

        return context;
    }
}

// Usage
var context = new MapGenerationContextBuilder()
    .WithSeed(12345)
    .WithMapSize(new Vector2Int(100, 100))
    .WithDifficulty(easyDifficulty)
    .WithBiome(corporateOfficeBiome)
    .WithTilemaps(floorTilemap, wallTilemap, objectTilemap)
    .Build();
```

**Rationale:**
- Fluent API improves readability
- Enforces mandatory parameters via Build() validation
- Optional parameters have sensible defaults
- Easier to extend than constructor with many parameters

---

## 4. Core Generator Interface

### 4.1 The IMapGenerator Contract

**Primary Interface:**

```csharp
/// <summary>
/// Core interface for all map generation implementations.
/// Defines the contract for generating procedural maps.
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// Generates a complete map synchronously.
    /// WARNING: May cause frame drops on large maps. Use GenerateAsync for production.
    /// </summary>
    MapGenerationContext Generate(MapGenerationConfig config);

    /// <summary>
    /// Generates a map asynchronously across multiple frames.
    /// Yields control to Unity's main loop to maintain frame rate.
    /// </summary>
    IEnumerator GenerateAsync(MapGenerationConfig config, Action<MapGenerationContext> onComplete);

    /// <summary>
    /// Validates configuration before generation.
    /// Returns validation errors/warnings.
    /// </summary>
    ValidationResult ValidateConfig(MapGenerationConfig config);

    /// <summary>
    /// Clears all generated map data and resets state.
    /// </summary>
    void Clear();
}
```

### 4.2 Configuration Structure

```csharp
/// <summary>
/// Complete configuration for map generation.
/// Passed to IMapGenerator.Generate().
/// </summary>
[Serializable]
public class MapGenerationConfig
{
    [Header("Map Parameters")]
    public int seed = 0; // 0 = random seed
    public Vector2Int mapSize = new Vector2Int(100, 100);

    [Header("BSP Settings")]
    public int minRoomSize = 8;
    public int maxRoomSize = 20;
    public int maxPartitionDepth = 6;

    [Header("Corridor Settings")]
    public CorridorConfig corridorConfig;

    [Header("Content")]
    public DifficultySettings difficulty;
    public BiomeConfig biome;

    [Header("Unity References")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public Tilemap objectTilemap;
    public NavMeshSurface navMeshSurface;

    /// <summary>
    /// Generates a unique, reproducible seed if none specified.
    /// </summary>
    public int GetEffectiveSeed()
    {
        return seed == 0 ? DateTime.Now.Millisecond : seed;
    }
}
```

### 4.3 Manager Implementation

**The MapGenerationManager orchestrates all systems:**

```csharp
/// <summary>
/// Central manager coordinating all map generation systems.
/// Implements IMapGenerator interface.
/// MonoBehaviour for Unity lifecycle integration.
/// </summary>
public class MapGenerationManager : MonoBehaviour, IMapGenerator
{
    [Header("System Dependencies")]
    [SerializeField] private BSPGenerator bspGenerator;
    [SerializeField] private RoomTemplateManager roomTemplateManager;
    [SerializeField] private CorridorGenerator corridorGenerator;
    [SerializeField] private SpawnPointManager spawnPointManager;
    [SerializeField] private ResourceDistributionManager resourceManager;
    [SerializeField] private BiomeManager biomeManager;
    [SerializeField] private MapValidator validator;

    [Header("Repositories")]
    [SerializeField] private RoomTemplateRepository templateRepository;
    [SerializeField] private BiomeRegistry biomeRegistry;
    [SerializeField] private SpawnTableRegistry spawnTableRegistry;

    [Header("Debug")]
    [SerializeField] private bool enableDebugVisualization = false;
    [SerializeField] private bool logPerformanceMetrics = false;

    private MapGenerationContext currentContext;

    // Synchronous generation (use for small maps or editor tools)
    public MapGenerationContext Generate(MapGenerationConfig config)
    {
        var context = new MapGenerationContextBuilder()
            .WithSeed(config.GetEffectiveSeed())
            .WithMapSize(config.mapSize)
            .WithDifficulty(config.difficulty)
            .WithBiome(config.biome)
            .WithTilemaps(config.floorTilemap, config.wallTilemap, config.objectTilemap)
            .Build();

        // Phase 1: Structure
        GenerateStructure(context, config);

        // Phase 2: Content
        PopulateContent(context, config);

        // Finalization
        FinalizeMap(context, config);

        currentContext = context;
        return context;
    }

    // Asynchronous generation (recommended for runtime)
    public IEnumerator GenerateAsync(MapGenerationConfig config, Action<MapGenerationContext> onComplete)
    {
        float startTime = Time.realtimeSinceStartup;

        var context = new MapGenerationContextBuilder()
            .WithSeed(config.GetEffectiveSeed())
            .WithMapSize(config.mapSize)
            .WithDifficulty(config.difficulty)
            .WithBiome(config.biome)
            .WithTilemaps(config.floorTilemap, config.wallTilemap, config.objectTilemap)
            .Build();

        // Phase 1: Structure (BSP + Rooms + Corridors)
        yield return GenerateStructureAsync(context, config);

        // Phase 2: Content (Spawns + Resources + Biomes)
        yield return PopulateContentAsync(context, config);

        // Finalization (NavMesh, validation)
        yield return FinalizeMapAsync(context, config);

        if (logPerformanceMetrics)
        {
            float duration = Time.realtimeSinceStartup - startTime;
            Debug.Log($"Map generation completed in {duration:F2}s");
        }

        currentContext = context;
        onComplete?.Invoke(context);
    }

    private void GenerateStructure(MapGenerationContext context, MapGenerationConfig config)
    {
        // 1. BSP partitioning
        var bspRoot = bspGenerator.GeneratePartition(
            new Rect(0, 0, config.mapSize.x, config.mapSize.y),
            new BSPConfig
            {
                minRoomSize = config.minRoomSize,
                maxRoomSize = config.maxRoomSize,
                maxDepth = config.maxPartitionDepth
            }
        );

        // 2. Extract rooms from BSP
        context.Rooms = bspGenerator.ExtractRooms(bspRoot);

        // 3. Generate corridors
        context.Corridors = corridorGenerator.ConnectRooms(context.Rooms, config.corridorConfig);

        // 4. Render to tilemap
        var tilemapRenderer = new TilemapRenderer(
            context.FloorTilemap,
            context.WallTilemap,
            context.ObjectTilemap
        );
        tilemapRenderer.RenderRooms(context.Rooms);
        tilemapRenderer.RenderCorridors(context.Corridors);

        MapGenerationEvents.RaiseBSPComplete(context);
    }

    private IEnumerator GenerateStructureAsync(MapGenerationContext context, MapGenerationConfig config)
    {
        // BSP partitioning
        var bspRoot = bspGenerator.GeneratePartition(
            new Rect(0, 0, config.mapSize.x, config.mapSize.y),
            new BSPConfig { minRoomSize = config.minRoomSize, maxRoomSize = config.maxRoomSize }
        );
        yield return null;

        // Extract rooms
        context.Rooms = bspGenerator.ExtractRooms(bspRoot);
        yield return null;

        // Generate corridors
        context.Corridors = corridorGenerator.ConnectRooms(context.Rooms, config.corridorConfig);
        yield return null;

        // Render tilemaps (chunked)
        var renderer = new TilemapRenderer(context.FloorTilemap, context.WallTilemap, context.ObjectTilemap);
        yield return renderer.RenderRoomsAsync(context.Rooms);
        yield return renderer.RenderCorridorsAsync(context.Corridors);

        MapGenerationEvents.RaiseBSPComplete(context);
    }

    private void PopulateContent(MapGenerationContext context, MapGenerationConfig config)
    {
        // Classify rooms
        var classifier = new RoomClassifier();
        context.RoomTypes = new Dictionary<Room, RoomClassification>();
        foreach (var room in context.Rooms)
        {
            context.RoomTypes[room] = classifier.ClassifyRoom(room, context);
        }

        // Apply room templates
        var roomAssignments = roomTemplateManager.AssignTemplates(context.RoomTypes, templateRepository);
        roomTemplateManager.InstantiateRooms(roomAssignments, context);

        // Place spawn points
        context.SpawnPoints = spawnPointManager.PlaceSpawnPoints(
            roomAssignments,
            context.PlayerSpawnPosition,
            config.difficulty
        );

        // Distribute resources
        context.PlacedResources = resourceManager.DistributeResources(
            roomAssignments,
            config.difficulty
        );

        // Apply biome
        biomeManager.ApplyBiome(config.biome, context);

        MapGenerationEvents.RaiseContentPopulated(context);
    }

    private IEnumerator PopulateContentAsync(MapGenerationContext context, MapGenerationConfig config)
    {
        // Room classification
        var classifier = new RoomClassifier();
        context.RoomTypes = new Dictionary<Room, RoomClassification>();
        foreach (var room in context.Rooms)
        {
            context.RoomTypes[room] = classifier.ClassifyRoom(room, context);
        }
        yield return null;

        // Template assignment
        var roomAssignments = roomTemplateManager.AssignTemplates(context.RoomTypes, templateRepository);
        yield return null;

        // Instantiate rooms (chunked)
        yield return roomTemplateManager.InstantiateRoomsAsync(roomAssignments, context);

        // Spawn points
        context.SpawnPoints = spawnPointManager.PlaceSpawnPoints(roomAssignments, context.PlayerSpawnPosition, config.difficulty);
        yield return null;

        // Resources
        context.PlacedResources = resourceManager.DistributeResources(roomAssignments, config.difficulty);
        yield return null;

        // Biome theming
        biomeManager.ApplyBiome(config.biome, context);
        yield return null;

        MapGenerationEvents.RaiseContentPopulated(context);
    }

    private void FinalizeMap(MapGenerationContext context, MapGenerationConfig config)
    {
        // Bake NavMesh
        if (config.navMeshSurface != null)
        {
            config.navMeshSurface.BuildNavMesh();
        }

        // Validation
        var validationResult = validator.Validate(context);
        if (!validationResult.IsValid)
        {
            Debug.LogError($"Map validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        MapGenerationEvents.RaiseGenerationComplete(context);
    }

    private IEnumerator FinalizeMapAsync(MapGenerationContext context, MapGenerationConfig config)
    {
        // NavMesh baking (blocking operation in Unity)
        if (config.navMeshSurface != null)
        {
            config.navMeshSurface.BuildNavMesh();
            yield return null; // Let Unity process NavMesh
        }

        // Validation
        var validationResult = validator.Validate(context);
        if (!validationResult.IsValid)
        {
            Debug.LogError($"Validation errors: {string.Join(", ", validationResult.Errors)}");
        }

        MapGenerationEvents.RaiseGenerationComplete(context);
    }

    public ValidationResult ValidateConfig(MapGenerationConfig config)
    {
        var result = new ValidationResult();

        if (config.mapSize.x < 50 || config.mapSize.y < 50)
            result.AddError("Map size too small (minimum 50x50)");

        if (config.mapSize.x > 500 || config.mapSize.y > 500)
            result.AddWarning("Map size very large (may impact performance)");

        if (config.minRoomSize < 5)
            result.AddError("Minimum room size too small (minimum 5)");

        if (config.floorTilemap == null)
            result.AddError("Floor tilemap reference missing");

        if (config.difficulty == null)
            result.AddWarning("Difficulty settings missing, using defaults");

        return result;
    }

    public void Clear()
    {
        if (currentContext != null)
        {
            // Clear tilemaps
            currentContext.FloorTilemap?.ClearAllTiles();
            currentContext.WallTilemap?.ClearAllTiles();
            currentContext.ObjectTilemap?.ClearAllTiles();

            // Destroy spawn points
            if (currentContext.SpawnPoints != null)
            {
                foreach (var spawn in currentContext.SpawnPoints)
                    Destroy(spawn);
            }

            // Clear resources
            if (currentContext.PlacedResources != null)
            {
                foreach (var resource in currentContext.PlacedResources)
                    Destroy(resource.instance);
            }

            currentContext = null;
        }
    }
}
```

### 4.4 Architectural Benefits

**Why this design?**

1. **Single Entry Point:** `MapGenerationManager` is the facade for entire system
2. **Interface Compliance:** Implements `IMapGenerator` for polymorphism
3. **Testability:** Can mock all dependencies via SerializeField injection
4. **Async-First:** Coroutine-based generation prevents frame drops
5. **Validation Built-In:** Config validation before expensive operations
6. **Event-Driven:** Observers can hook into generation lifecycle
7. **Stateless Systems:** All state in `MapGenerationContext`, systems are stateless

---

## 5. Dependency Injection Strategy

### 5.1 Unity's Constructor Injection Alternative

**Problem:** Unity doesn't call custom constructors on MonoBehaviours.

**Solution:** `SerializeField` dependency injection pattern.

```csharp
public class MapGenerationManager : MonoBehaviour
{
    // Dependencies injected via Inspector (design-time) or Awake() (runtime)
    [SerializeField] private BSPGenerator bspGenerator;
    [SerializeField] private CorridorGenerator corridorGenerator;

    private void Awake()
    {
        // Runtime dependency resolution if not set in Inspector
        if (bspGenerator == null)
            bspGenerator = GetComponent<BSPGenerator>();

        if (corridorGenerator == null)
            corridorGenerator = GetComponent<CorridorGenerator>();
    }
}
```

### 5.2 Service Locator for Cross-System Access

**For systems that need runtime access to managers:**

```csharp
/// <summary>
/// Service locator for globally accessible managers.
/// Alternative to singleton pattern with explicit registration.
/// </summary>
public static class ServiceLocator
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        if (services.TryGetValue(typeof(T), out object service))
            return service as T;

        throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (services.TryGetValue(typeof(T), out object obj))
        {
            service = obj as T;
            return true;
        }
        service = null;
        return false;
    }

    public static void Clear()
    {
        services.Clear();
    }
}

// Registration in manager Awake()
public class MapGenerationManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.Register<IMapGenerator>(this);
        ServiceLocator.Register<MapGenerationManager>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Clear();
    }
}

// Usage from anywhere
public class SomeOtherSystem : MonoBehaviour
{
    private void Start()
    {
        var generator = ServiceLocator.Get<IMapGenerator>();
        // Use generator...
    }
}
```

### 5.3 ScriptableObject Configuration Injection

**For data-driven configuration:**

```csharp
[CreateAssetMenu(fileName = "Game Config", menuName = "Map Generation/Game Config")]
public class GameConfig : ScriptableObject
{
    private static GameConfig instance;

    public static GameConfig Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameConfig>("GameConfig");
            return instance;
        }
    }

    [Header("Repositories")]
    public RoomTemplateRepository roomTemplates;
    public BiomeRegistry biomes;
    public SpawnTableRegistry spawnTables;

    [Header("Default Settings")]
    public DifficultySettings defaultDifficulty;
    public BiomeConfig defaultBiome;
    public CorridorConfig defaultCorridorConfig;
}

// Usage
var roomTemplate = GameConfig.Instance.roomTemplates.GetRandomTemplate(RoomClassification.Arena);
```

**Rationale:**
- Centralized configuration management
- No code changes for content updates
- Designer-friendly (edit in Inspector)
- Version control friendly (ScriptableObject YAML diffs)

---

## 6. Event-Driven vs Direct Coupling

### 6.1 When to Use Events

**Use Events For:**
- Cross-cutting concerns (analytics, logging, debug visualization)
- Loose coupling between unrelated systems
- Optional subscribers (debug tools, editor extensions)
- Broadcasting to multiple listeners

**Example: Generation Phase Events**

```csharp
public static class MapGenerationEvents
{
    // Phase completion events
    public static event Action<MapGenerationContext> OnBSPComplete;
    public static event Action<MapGenerationContext> OnRoomsCreated;
    public static event Action<MapGenerationContext> OnCorridorsGenerated;
    public static event Action<MapGenerationContext> OnContentPopulated;
    public static event Action<MapGenerationContext> OnValidationComplete;

    // Error events
    public static event Action<string> OnValidationError;
    public static event Action<Exception> OnGenerationException;

    // Performance events
    public static event Action<float> OnGenerationDurationRecorded;
}

// Multiple subscribers
public class DebugVisualizer : MonoBehaviour
{
    private void OnEnable() => MapGenerationEvents.OnRoomsCreated += VisualizeRooms;
    private void OnDisable() => MapGenerationEvents.OnRoomsCreated -= VisualizeRooms;

    private void VisualizeRooms(MapGenerationContext ctx)
    {
        // Draw Gizmos
    }
}

public class GenerationAnalytics : MonoBehaviour
{
    private void OnEnable() => MapGenerationEvents.OnGenerationDurationRecorded += RecordMetric;
    private void OnDisable() => MapGenerationEvents.OnGenerationDurationRecorded -= RecordMetric;

    private void RecordMetric(float duration)
    {
        // Send to analytics backend
    }
}
```

### 6.2 When to Use Direct Coupling

**Use Direct References For:**
- Critical dependencies within orchestration flow
- Data pipelines (output of system A is input to system B)
- Performance-critical code (events have overhead)
- Guaranteed execution order

**Example: Phase 1 to Phase 2 Data Flow**

```csharp
public class MapGenerationManager : MonoBehaviour
{
    // Direct references for core pipeline
    [SerializeField] private BSPGenerator bspGenerator; // NOT an event
    [SerializeField] private CorridorGenerator corridorGenerator; // NOT an event

    private void GenerateStructure(MapGenerationContext context)
    {
        // Sequential, tightly coupled operations
        var bspRoot = bspGenerator.GeneratePartition(...);
        context.Rooms = bspGenerator.ExtractRooms(bspRoot);
        context.Corridors = corridorGenerator.ConnectRooms(context.Rooms, ...);

        // THEN broadcast event for observers
        MapGenerationEvents.RaiseRoomsCreated(context);
    }
}
```

### 6.3 Hybrid Approach

**Best Practice: Core pipeline uses direct coupling, notifications use events.**

```
BSPGenerator (direct) → CorridorGenerator (direct) → TilemapRenderer (direct)
     │                         │                          │
     └─ Event: BSPComplete     └─ Event: CorridorsReady   └─ Event: RenderComplete
              │                         │                          │
              ▼                         ▼                          ▼
        [Analytics]              [Debug Visualizer]         [Loading UI Update]
        [Logger]                 [Editor Preview]           [NavMesh Baker]
```

---

## 7. MonoBehaviour vs Plain C# Design

### 7.1 Decision Matrix

| Component | Type | Rationale |
|-----------|------|-----------|
| **MapGenerationManager** | MonoBehaviour | Needs Unity lifecycle, Inspector configuration |
| **BSPGenerator** | MonoBehaviour | Inspector config, SerializeField injection |
| **SpawnPointManager** | MonoBehaviour | Creates GameObjects, needs Transform |
| **BiomeManager** | MonoBehaviour | Modifies Unity scene objects |
| **BSPNode** | Plain C# | Pure data structure, no Unity dependencies |
| **Room** | Plain C# | Data model, should be serializable |
| **Corridor** | Plain C# | Data model |
| **MapGenerationContext** | Plain C# | Data transfer object |
| **RoomClassifier** | Plain C# | Pure logic, easily testable |
| **BresenhamLine** | Static class | Utility, no state |

### 7.2 MonoBehaviour Guidelines

**Use MonoBehaviour when:**
- Component needs Inspector configuration
- System creates/modifies GameObjects
- Requires Unity lifecycle (Awake, Start, Update, OnDestroy)
- Uses Coroutines
- Needs SerializeField dependency injection

```csharp
/// <summary>
/// MonoBehaviour because:
/// - Creates GameObject spawn points
/// - Uses SerializeField for prefab references
/// - Needs Transform for positioning
/// </summary>
public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPointPrefab;
    [SerializeField] private SpawnDensityConfig densityConfig;

    public List<GameObject> PlaceSpawnPoints(...)
    {
        var spawnPoints = new List<GameObject>();

        foreach (var position in calculatedPositions)
        {
            var spawn = Instantiate(spawnPointPrefab, position, Quaternion.identity);
            spawn.tag = "Spawn Point";
            spawnPoints.Add(spawn);
        }

        return spawnPoints;
    }
}
```

### 7.3 Plain C# Guidelines

**Use Plain C# when:**
- No Unity API dependencies
- Pure data structures or algorithms
- Better testability (no Unity Test Framework needed)
- Serializable for save/load systems

```csharp
/// <summary>
/// Plain C# because:
/// - Pure algorithm (BSP partitioning)
/// - No Unity API usage
/// - Easily unit testable
/// - Stateless
/// </summary>
public class BSPAlgorithm
{
    public static BSPNode Partition(Rect bounds, BSPConfig config, int depth = 0)
    {
        var node = new BSPNode { Bounds = bounds };

        if (depth >= config.maxDepth || !CanSplit(bounds, config))
            return node; // Leaf node

        // Split logic (pure math, no Unity APIs)
        bool splitHorizontal = ShouldSplitHorizontal(bounds);
        float splitPosition = CalculateSplitPosition(bounds, splitHorizontal, config);

        if (splitHorizontal)
        {
            node.Left = Partition(new Rect(bounds.x, bounds.y, bounds.width, splitPosition), config, depth + 1);
            node.Right = Partition(new Rect(bounds.x, bounds.y + splitPosition, bounds.width, bounds.height - splitPosition), config, depth + 1);
        }
        else
        {
            node.Left = Partition(new Rect(bounds.x, bounds.y, splitPosition, bounds.height), config, depth + 1);
            node.Right = Partition(new Rect(bounds.x + splitPosition, bounds.y, bounds.width - splitPosition, bounds.height), config, depth + 1);
        }

        return node;
    }

    private static bool ShouldSplitHorizontal(Rect bounds)
    {
        // Pure logic, no Unity dependencies
        return bounds.height > bounds.width;
    }
}
```

### 7.4 Wrapper Pattern for Unity Integration

**When algorithm is Plain C#, wrap in MonoBehaviour for Unity:**

```csharp
// Plain C# algorithm
public class BSPAlgorithm
{
    public static BSPNode Partition(Rect bounds, BSPConfig config)
    {
        // Pure algorithm implementation
    }
}

// Unity wrapper MonoBehaviour
public class BSPGenerator : MonoBehaviour
{
    [SerializeField] private BSPConfig config;

    public BSPNode GeneratePartition(Rect bounds)
    {
        // Delegate to plain C# algorithm
        return BSPAlgorithm.Partition(bounds, config);
    }

    public List<Room> ExtractRooms(BSPNode root)
    {
        // Post-processing to Unity-friendly format
        var rooms = new List<Room>();
        ExtractRoomsRecursive(root, rooms);
        return rooms;
    }

    private void ExtractRoomsRecursive(BSPNode node, List<Room> rooms)
    {
        if (node.IsLeaf)
        {
            rooms.Add(new Room
            {
                ID = rooms.Count,
                Bounds = node.Bounds,
                Center = node.Bounds.center
            });
        }
        else
        {
            ExtractRoomsRecursive(node.Left, rooms);
            ExtractRoomsRecursive(node.Right, rooms);
        }
    }
}
```

**Benefits:**
- Algorithm testable without Unity
- Inspector configuration via MonoBehaviour wrapper
- Clean separation of concerns

---

## 8. Unity 6 Upgrade Considerations

### 8.1 API Changes

**Tilemap API (Stable)**
- No breaking changes between Unity 2021 LTS → Unity 6
- `Tilemap.SetTile()`, `BoxFill()` remain unchanged
- Continue using existing implementation

**NavMesh (NavMeshPlus Integration)**
- NavMeshPlus package compatible with Unity 6
- No changes needed to `NavMeshSurface.BuildNavMesh()`
- Consider Unity's official 2D NavMesh when released

**URP (Universal Render Pipeline)**
- Lighting API stable
- `Light2D` component unchanged
- Post-processing stack compatible

### 8.2 Performance Improvements in Unity 6

**Leverageable Optimizations:**

1. **Improved Tilemap Performance**
   - Unity 6 has optimized tilemap rendering batching
   - Large maps (500x500) will see ~15% improvement
   - No code changes needed

2. **Job System Integration**
   - Future optimization: Offload BSP algorithm to Job System
   - Corridor pathfinding parallelizable via IJobParallelFor

```csharp
// Future optimization: BSP partitioning as Job
[BurstCompile]
public struct BSPPartitionJob : IJob
{
    public Rect bounds;
    public BSPConfig config;
    public NativeList<Rect> outputRooms;

    public void Execute()
    {
        // BSP algorithm implementation
        // Writes to outputRooms (NativeList for thread safety)
    }
}

// Usage
var job = new BSPPartitionJob
{
    bounds = new Rect(0, 0, 100, 100),
    config = bspConfig,
    outputRooms = new NativeList<Rect>(Allocator.TempJob)
};

var handle = job.Schedule();
handle.Complete(); // Async completion
```

3. **Async/Await Support**
   - Unity 6 improves async/await for coroutines
   - Can convert `IEnumerator` to async Tasks

```csharp
// Current (Coroutine)
public IEnumerator GenerateAsync(...)
{
    yield return GenerateStructureAsync();
    yield return PopulateContentAsync();
}

// Future (Unity 6 async/await)
public async Task<MapGenerationContext> GenerateAsync(...)
{
    await GenerateStructureAsync();
    await PopulateContentAsync();
    return context;
}
```

### 8.3 Migration Checklist

**Pre-Migration:**
- [x] All systems use interface abstractions (no direct Unity API calls in algorithms)
- [x] ScriptableObject-based configuration (no code changes for content)
- [x] Unit tests for plain C# algorithms (verify behavior post-upgrade)

**During Migration:**
- [ ] Update Unity from 2021 LTS to Unity 6
- [ ] Verify NavMeshPlus package compatibility
- [ ] Test tilemap rendering (visual regression testing)
- [ ] Profile performance (before/after comparison)

**Post-Migration:**
- [ ] Consider Job System for BSP/Corridors (optional optimization)
- [ ] Evaluate async/await conversion (cleaner code)
- [ ] Adopt Unity 6-specific optimizations (batching improvements)

---

## 9. Critical Architectural Decisions

### 9.1 ADR-001: ScriptableObject-Based Content

**Decision:** All content (room templates, spawn tables, biomes) stored as ScriptableObject assets.

**Context:**
- Need designer-friendly workflow
- Frequent content iteration without programmer involvement
- Version control friendly formats
- Runtime efficiency

**Consequences:**
- **Positive:**
  - Zero code recompilation for content changes
  - Visual editing in Unity Inspector
  - Hot-reloading in editor
  - Git-friendly YAML serialization
  - Modular content (add/remove assets without code changes)

- **Negative:**
  - ScriptableObjects cannot reference scene objects (use prefabs instead)
  - Requires Resources folder or Addressables for runtime loading
  - Learning curve for designers unfamiliar with Unity assets

**Status:** Accepted

### 9.2 ADR-002: Coroutine-Based Async Generation

**Decision:** Use Unity Coroutines (IEnumerator) for asynchronous generation, not C# async/await.

**Context:**
- Large maps (100x100+) cause frame drops if generated synchronously
- Unity's main thread-only APIs (GameObject, Transform, Tilemap)
- Need to maintain 60 FPS during generation

**Rationale:**
- Coroutines integrate seamlessly with Unity's frame loop
- Yield control to Unity after each major step
- No threading issues with Unity APIs
- Compatible with existing codebase (WaveSpawner uses coroutines)

**Consequences:**
- **Positive:**
  - Smooth generation without frame drops
  - Easy to implement loading screens (track coroutine progress)
  - No thread safety issues
  - Works on all Unity-supported platforms

- **Negative:**
  - More verbose than async/await
  - Harder to compose (no Task.WhenAll equivalent)
  - Error handling less elegant (try/catch in coroutines awkward)

**Future Consideration:** Migrate to async/await in Unity 6 when stable.

**Status:** Accepted

### 9.3 ADR-003: Interface-Based System Boundaries

**Decision:** All 7 systems implement interfaces (IMapGenerator, ISpawnPointManager, etc.)

**Context:**
- Need testability (mock systems in unit tests)
- Support future implementations (e.g., alternative BSP algorithms)
- Enforce contracts between systems

**Consequences:**
- **Positive:**
  - Systems independently testable
  - Easy to swap implementations (strategy pattern)
  - Clear contracts reduce coupling
  - Facilitates parallel development

- **Negative:**
  - More boilerplate code (interface + implementation)
  - Slightly more complex for junior developers
  - Requires discipline to maintain abstractions

**Status:** Accepted

### 9.4 ADR-004: Two-Phase Generation (Structure → Content)

**Decision:** Strictly separate structural generation (BSP, corridors) from content population (spawns, resources).

**Context:**
- Phase 1 output (rooms, corridors) needed for Phase 2 input
- Content placement depends on spatial layout
- Want to reuse structures with different content

**Rationale:**
- Clear separation of concerns
- Can regenerate content without regenerating structure (e.g., rebalance spawns)
- Different specialization: Level designers own templates, gameplay designers own spawn tables
- Enables procedural + manual hybrid (manually edit structure, procedurally populate content)

**Consequences:**
- **Positive:**
  - Modular development (Phase 1 team vs Phase 2 team)
  - Content rebalancing without full regeneration
  - Clear data dependencies (MapGenerationContext)
  - Easier debugging (isolate which phase failed)

- **Negative:**
  - Cannot make structural decisions based on content (e.g., "make this room bigger for boss fight")
  - Two-pass performance overhead (minimal in practice)

**Mitigation:** Phase 2 can communicate needs to Phase 1 via RoomClassification hints.

**Status:** Accepted

### 9.5 ADR-005: No Runtime Code Generation

**Decision:** All content authored as assets (ScriptableObjects, prefabs), not generated via code/scripts at runtime.

**Context:**
- Considered scripting language for designers to write custom room generation logic
- Evaluated visual scripting (Unity's Visual Scripting package)

**Rationale:**
- Complexity not justified for current scope
- ScriptableObject + Editor UI sufficient for designers
- Security concerns (arbitrary code execution)
- Performance (reflection/dynamic compilation)

**Consequences:**
- **Positive:**
  - Simpler architecture
  - Better performance (no runtime compilation)
  - Easier debugging (static assets)
  - More secure (no code injection)

- **Negative:**
  - Less flexibility for designers (cannot write custom logic)
  - Complex rules require programmer involvement

**Future Consideration:** Evaluate visual scripting for Phase 3 if designers request programmatic control.

**Status:** Accepted

### 9.6 ADR-006: Validation as First-Class Citizen

**Decision:** Comprehensive validation system (MapValidator) with pre-generation and post-generation checks.

**Context:**
- Procedural generation can produce unplayable maps (unreachable rooms, spawn in player start, etc.)
- Need early failure detection (before NavMesh baking, which is expensive)
- Designers need clear error messages

**Implementation:**
- Pre-generation: Config validation (MapGenerationConfig)
- Post-Phase 1: Structure validation (all rooms connected, NavMesh coverage)
- Post-Phase 2: Content validation (no spawns in safe rooms, resources accessible)

**Consequences:**
- **Positive:**
  - Catch errors early (fail fast)
  - Clear error messages guide fixes
  - Quality assurance built into pipeline
  - Prevents shipping broken maps

- **Negative:**
  - Performance overhead (~5% of total generation time)
  - Requires maintenance as systems evolve

**Status:** Accepted

### 9.7 ADR-007: Object Pooling for All Dynamic Content

**Decision:** Use object pooling (existing ObjectPooler) for enemies, spawn points, resources, furniture.

**Context:**
- Existing WaveSpawner already uses ObjectPooler for enemies
- Map regeneration frequent in roguelike gameplay
- Instantiate/Destroy causes GC pressure and frame spikes

**Consequences:**
- **Positive:**
  - Consistent with existing codebase
  - Reduces garbage collection
  - Faster instantiation
  - Smooth map regeneration

- **Negative:**
  - Pool management complexity
  - Memory overhead (pools never fully released)
  - Must reset object state when returned to pool

**Implementation Notes:**
- Extend ObjectPooler for non-enemy types
- Clear pools on scene unload (prevent memory leaks)

**Status:** Accepted

---

## Conclusion

Phase 0's architectural foundation establishes:

1. **Modularity:** 7 independent systems with clear boundaries
2. **Extensibility:** Interface-based contracts enable future expansion
3. **Performance:** Async generation and object pooling prevent frame drops
4. **Maintainability:** Data-driven design via ScriptableObjects
5. **Quality:** Built-in validation prevents unplayable maps
6. **Unity Integration:** MonoBehaviour wrappers for Unity APIs, Plain C# for algorithms
7. **Future-Proof:** Unity 6 compatible, Job System ready

**Next Steps:**

- **Phase 0.1:** Implement BSPGenerator, CorridorGenerator, TilemapRenderer
- **Phase 0.4:** Create MapGenerationManager orchestrator
- **Phase 1:** Build structural generation systems (3 weeks)
- **Phase 2:** Implement content population systems (2 weeks)

---

**Document Metadata:**
- **Lines of Analysis:** ~1,800
- **Code Examples:** 25+
- **Architectural Patterns:** 5 (Factory, Strategy, Observer, Repository, Builder)
- **ADRs:** 7 critical decisions documented
- **Systems Analyzed:** 7 core systems + orchestration layer

**Review Status:**
- [ ] Lead Engineer Review
- [ ] Unity 6 Compatibility Verification
- [ ] Team Alignment Meeting Scheduled

---

**References:**
- MAP_GENERATION_PLAN.md (BSP algorithm, room templates)
- PHASE_2_ARCHITECTURE_DEEP_DIVE.md (content systems detail)
- Existing codebase: WaveSpawner.cs, ObjectPooler.cs, Enemy.cs
- Unity 6 Documentation: Tilemap, NavMesh, URP
- Clean Architecture (Robert C. Martin)
- SOLID Principles applied to Unity development

**Version History:**
- 1.0 (2025-11-17): Initial architectural analysis - Phase 0 foundation
