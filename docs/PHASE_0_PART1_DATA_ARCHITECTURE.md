# Phase 0 Part 1: Data Architecture Deep Dive
## Office-Mice Procedural Map Generation - Core Data Models & Interfaces

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Author:** Software Architecture Analysis
**Status:** Architectural Specification - Foundation Layer

---

## Executive Summary

Phase 0 represents the **foundational data layer** upon which all procedural generation builds. This document analyzes the core data models (Task 0.2) and subsystem interfaces (Task 0.5) that form the architectural bedrock of the map generation system.

**Architectural Impact:** CRITICAL
**Technical Complexity:** MEDIUM
**Downstream Dependencies:** ALL subsequent phases depend on this foundation

### Core Architectural Decisions

1. **Struct-based primitives for performance** - Value types for hot-path data
2. **Class-based complex models for flexibility** - Reference types for hierarchical data
3. **Unity serialization with custom validation** - Native serialization + architectural constraints
4. **ScriptableObject configuration separation** - Immutable templates vs mutable runtime state
5. **Interface-driven subsystem contracts** - Loose coupling between generation stages
6. **Memory-conscious layout** - Cache-friendly data structures for large maps
7. **Validation-first design** - Fail fast with detailed diagnostics

---

## Table of Contents

1. [Core Data Model Architecture](#1-core-data-model-architecture)
2. [MapData Structure Design](#2-mapdata-structure-design)
3. [RoomData & CorridorData](#3-roomdata--corridordata)
4. [Serialization Strategy](#4-serialization-strategy)
5. [Interface Design Philosophy](#5-interface-design-philosophy)
6. [Memory Layout & Performance](#6-memory-layout--performance)
7. [ScriptableObject Usage](#7-scriptableobject-usage)
8. [Unity Serialization Constraints](#8-unity-serialization-constraints)
9. [Data Validation Architecture](#9-data-validation-architecture)

---

## 1. Core Data Model Architecture

### 1.1 Design Philosophy

The data architecture follows **separation of concerns** between:
- **Structure** (what data exists)
- **Behavior** (what operations are performed)
- **Configuration** (design-time parameters)
- **State** (runtime mutations)

**Guiding Principles:**

```
Immutable Configuration → Mutable Runtime State → Validated Output
     ↓                           ↓                        ↓
ScriptableObjects          C# Classes/Structs      Serialized Data
```

### 1.2 Data Type Hierarchy

```
Foundation Layer (Phase 0)
├── Primitives (structs)
│   ├── Vector2Int (Unity built-in)
│   ├── RectInt (Unity built-in)
│   ├── TilePosition
│   └── DoorwayPosition
│
├── Core Models (classes)
│   ├── MapData
│   ├── RoomData
│   ├── CorridorData
│   └── BSPNode
│
├── Configuration (ScriptableObjects)
│   ├── MapGenerationSettings
│   ├── RoomTemplate
│   └── BiomeConfig
│
└── Interfaces (contracts)
    ├── IMapGenerator
    ├── IRoomClassifier
    └── IContentPopulator
```

### 1.3 Value Types vs Reference Types

**Decision Matrix:**

| Data Type | Type | Rationale |
|-----------|------|-----------|
| **TilePosition** | `struct` | Hot-path iteration, millions of instances |
| **DoorwayPosition** | `struct` | Passed by value in pathfinding |
| **RoomData** | `class` | Contains references, hierarchical |
| **CorridorData** | `class` | Contains tile lists, mutable |
| **MapData** | `class` | Root aggregate, contains collections |
| **BSPNode** | `class` | Tree structure requires references |

**Performance Implication:** Structs avoid heap allocation for frequently created data, critical for 100x100+ tile maps.

---

## 2. MapData Structure Design

### 2.1 Core MapData Class

```csharp
/// <summary>
/// Root data structure representing a complete procedurally generated map.
/// This is the canonical output of Phase 1 (BSP Generation) and input to Phase 2 (Content Population).
/// </summary>
[System.Serializable]
public class MapData
{
    // === IDENTITY ===
    [SerializeField] private int _seed;
    [SerializeField] private string _mapID;
    [SerializeField] private System.DateTime _generatedTimestamp;

    // === SPATIAL PROPERTIES ===
    [SerializeField] private Vector2Int _mapSize;
    [SerializeField] private RectInt _mapBounds;

    // === STRUCTURAL DATA ===
    [SerializeField] private List<RoomData> _rooms;
    [SerializeField] private List<CorridorData> _corridors;
    [SerializeField] private BSPNode _rootNode; // Phase 1 BSP tree

    // === TILEMAP REFERENCES ===
    // Note: Tilemaps are NOT serialized, only referenced at runtime
    private Tilemap _floorTilemap;
    private Tilemap _wallTilemap;
    private Tilemap _objectTilemap;

    // === GAMEPLAY DATA (populated in Phase 2) ===
    [SerializeField] private Vector2Int _playerSpawnPosition;
    [SerializeField] private List<SpawnPointData> _enemySpawnPoints;
    [SerializeField] private List<ResourcePlacementData> _resources;

    // === METADATA ===
    [SerializeField] private MapMetadata _metadata;

    // === PUBLIC PROPERTIES (Read-Only) ===
    public int Seed => _seed;
    public Vector2Int MapSize => _mapSize;
    public RectInt MapBounds => _mapBounds;
    public IReadOnlyList<RoomData> Rooms => _rooms.AsReadOnly();
    public IReadOnlyList<CorridorData> Corridors => _corridors.AsReadOnly();
    public Vector2Int PlayerSpawnPosition => _playerSpawnPosition;

    // === CONSTRUCTOR ===
    public MapData(int seed, Vector2Int mapSize)
    {
        _seed = seed;
        _mapSize = mapSize;
        _mapBounds = new RectInt(0, 0, mapSize.x, mapSize.y);
        _mapID = System.Guid.NewGuid().ToString();
        _generatedTimestamp = System.DateTime.UtcNow;

        _rooms = new List<RoomData>();
        _corridors = new List<CorridorData>();
        _enemySpawnPoints = new List<SpawnPointData>();
        _resources = new List<ResourcePlacementData>();

        _metadata = new MapMetadata();
    }

    // === MUTATORS (Phase 1: Structure) ===
    public void AddRoom(RoomData room)
    {
        room.RoomID = _rooms.Count;
        _rooms.Add(room);
    }

    public void AddCorridor(CorridorData corridor)
    {
        corridor.CorridorID = _corridors.Count;
        _corridors.Add(corridor);
    }

    public void SetBSPRoot(BSPNode root)
    {
        _rootNode = root;
    }

    // === MUTATORS (Phase 2: Content) ===
    public void SetPlayerSpawn(Vector2Int position)
    {
        _playerSpawnPosition = position;
    }

    public void AddEnemySpawnPoint(SpawnPointData spawnPoint)
    {
        _enemySpawnPoints.Add(spawnPoint);
    }

    public void AddResource(ResourcePlacementData resource)
    {
        _resources.Add(resource);
    }

    // === TILEMAP BINDING (Runtime Only) ===
    public void BindTilemaps(Tilemap floor, Tilemap wall, Tilemap objects)
    {
        _floorTilemap = floor;
        _wallTilemap = wall;
        _objectTilemap = objects;
    }

    public Tilemap FloorTilemap => _floorTilemap;
    public Tilemap WallTilemap => _wallTilemap;
    public Tilemap ObjectTilemap => _objectTilemap;

    // === VALIDATION ===
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        // Rule 1: Map must have rooms
        if (_rooms.Count == 0)
            result.AddError("MapData contains no rooms");

        // Rule 2: All rooms must be within map bounds
        foreach (var room in _rooms)
        {
            if (!_mapBounds.Contains(room.Bounds.min) || !_mapBounds.Contains(room.Bounds.max))
                result.AddError($"Room {room.RoomID} exceeds map bounds");
        }

        // Rule 3: Player spawn must be set
        if (_playerSpawnPosition == Vector2Int.zero)
            result.AddWarning("Player spawn not set");

        // Rule 4: Rooms must have unique IDs
        var roomIDs = new HashSet<int>();
        foreach (var room in _rooms)
        {
            if (!roomIDs.Add(room.RoomID))
                result.AddError($"Duplicate room ID: {room.RoomID}");
        }

        return result;
    }

    // === SERIALIZATION SUPPORT ===
    public MapDataSnapshot CreateSnapshot()
    {
        return new MapDataSnapshot(this);
    }
}
```

### 2.2 Why This Structure?

**Architectural Rationale:**

1. **Separation of Concerns**
   - Structure data (`_rooms`, `_corridors`) separate from content data (`_enemySpawnPoints`, `_resources`)
   - Allows Phase 1 to complete without Phase 2 dependency
   - Clean handoff point between generation stages

2. **Encapsulation**
   - Private fields with public read-only properties
   - Mutators clearly indicate which phase modifies what
   - Prevents accidental state corruption

3. **Tilemap Reference Pattern**
   - Tilemaps are **not serialized** (Unity limitation + performance)
   - Bound at runtime via `BindTilemaps()`
   - Avoids circular serialization dependencies

4. **Metadata Embedding**
   - `_seed` enables reproducible generation
   - `_generatedTimestamp` for debugging/analytics
   - `_mapID` for save/load systems

**Alternative Considered: Flat Structure**
```csharp
// REJECTED: Monolithic structure
public class MapData
{
    public Vector2Int[] allTiles;
    public int[] roomIDs;
    public bool[] isWalkable;
    // ... 10+ more arrays
}
```
**Rejection Reason:** No semantic meaning, error-prone, hard to validate, difficult to extend.

---

## 3. RoomData & CorridorData

### 3.1 RoomData Class

```csharp
/// <summary>
/// Represents a single room in the generated map.
/// Rooms are rectangular regions created by BSP partitioning.
/// </summary>
[System.Serializable]
public class RoomData
{
    // === IDENTITY ===
    [SerializeField] private int _roomID;

    // === SPATIAL PROPERTIES ===
    [SerializeField] private RectInt _bounds; // Actual room area (smaller than BSP partition)
    [SerializeField] private Vector2Int _center;
    [SerializeField] private int _area; // Cached: bounds.width * bounds.height

    // === CONNECTIVITY ===
    [SerializeField] private List<int> _connectedRoomIDs; // IDs of rooms connected via corridors
    [SerializeField] private List<DoorwayPosition> _doorways;

    // === CLASSIFICATION (Phase 2) ===
    [SerializeField] private RoomClassification _classification;
    [SerializeField] private bool _isOnCriticalPath; // Main path from spawn to boss

    // === TEMPLATE ASSIGNMENT (Phase 2) ===
    [SerializeField] private string _assignedTemplateID; // RoomTemplate asset GUID

    // === COMPUTED PROPERTIES ===
    [SerializeField] private float _distanceFromPlayerSpawn; // Set during classification

    // === PUBLIC PROPERTIES ===
    public int RoomID
    {
        get => _roomID;
        set => _roomID = value;
    }

    public RectInt Bounds => _bounds;
    public Vector2Int Center => _center;
    public int Area => _area;
    public IReadOnlyList<int> ConnectedRoomIDs => _connectedRoomIDs.AsReadOnly();
    public IReadOnlyList<DoorwayPosition> Doorways => _doorways.AsReadOnly();
    public RoomClassification Classification => _classification;
    public bool IsOnCriticalPath => _isOnCriticalPath;
    public float DistanceFromPlayerSpawn => _distanceFromPlayerSpawn;

    // === CONSTRUCTOR ===
    public RoomData(RectInt bounds)
    {
        _bounds = bounds;
        _center = new Vector2Int(
            bounds.x + bounds.width / 2,
            bounds.y + bounds.height / 2
        );
        _area = bounds.width * bounds.height;

        _connectedRoomIDs = new List<int>();
        _doorways = new List<DoorwayPosition>();
        _classification = RoomClassification.Unassigned;
    }

    // === MUTATORS ===
    public void ConnectToRoom(int roomID)
    {
        if (!_connectedRoomIDs.Contains(roomID))
            _connectedRoomIDs.Add(roomID);
    }

    public void AddDoorway(DoorwayPosition doorway)
    {
        _doorways.Add(doorway);
    }

    public void SetClassification(RoomClassification classification)
    {
        _classification = classification;
    }

    public void SetOnCriticalPath(bool isOnPath)
    {
        _isOnCriticalPath = isOnPath;
    }

    public void SetDistanceFromPlayerSpawn(float distance)
    {
        _distanceFromPlayerSpawn = distance;
    }

    public void AssignTemplate(string templateID)
    {
        _assignedTemplateID = templateID;
    }

    // === VALIDATION ===
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (_area <= 0)
            result.AddError($"Room {_roomID} has zero or negative area");

        if (_bounds.width < 3 || _bounds.height < 3)
            result.AddError($"Room {_roomID} is too small (min 3x3)");

        if (_doorways.Count == 0)
            result.AddWarning($"Room {_roomID} has no doorways (may be inaccessible)");

        return result;
    }
}
```

### 3.2 CorridorData Class

```csharp
/// <summary>
/// Represents a corridor connecting two rooms.
/// Corridors are generated by pathfinding between room doorways.
/// </summary>
[System.Serializable]
public class CorridorData
{
    // === IDENTITY ===
    [SerializeField] private int _corridorID;

    // === CONNECTIVITY ===
    [SerializeField] private int _roomA_ID;
    [SerializeField] private int _roomB_ID;
    [SerializeField] private Vector2Int _startPosition; // Doorway position in Room A
    [SerializeField] private Vector2Int _endPosition;   // Doorway position in Room B

    // === PATH DATA ===
    [SerializeField] private List<Vector2Int> _pathTiles; // All tiles in corridor
    [SerializeField] private int _width; // Corridor width in tiles (typically 3)
    [SerializeField] private int _length; // Cached: _pathTiles.Count

    // === PROPERTIES ===
    [SerializeField] private CorridorShape _shape; // Straight, L-shaped, Z-shaped

    // === PUBLIC PROPERTIES ===
    public int CorridorID
    {
        get => _corridorID;
        set => _corridorID = value;
    }

    public int RoomA_ID => _roomA_ID;
    public int RoomB_ID => _roomB_ID;
    public Vector2Int StartPosition => _startPosition;
    public Vector2Int EndPosition => _endPosition;
    public IReadOnlyList<Vector2Int> PathTiles => _pathTiles.AsReadOnly();
    public int Width => _width;
    public int Length => _length;
    public CorridorShape Shape => _shape;

    // === CONSTRUCTOR ===
    public CorridorData(int roomA, int roomB, Vector2Int start, Vector2Int end, int width = 3)
    {
        _roomA_ID = roomA;
        _roomB_ID = roomB;
        _startPosition = start;
        _endPosition = end;
        _width = width;
        _pathTiles = new List<Vector2Int>();
    }

    // === MUTATORS ===
    public void SetPath(List<Vector2Int> path)
    {
        _pathTiles = new List<Vector2Int>(path);
        _length = _pathTiles.Count;
        _shape = DetermineShape(path);
    }

    // === SHAPE DETECTION ===
    private CorridorShape DetermineShape(List<Vector2Int> path)
    {
        if (path.Count < 2)
            return CorridorShape.Point;

        // Check if all tiles are collinear
        bool allHorizontal = true;
        bool allVertical = true;

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i].y != path[0].y)
                allHorizontal = false;
            if (path[i].x != path[0].x)
                allVertical = false;
        }

        if (allHorizontal || allVertical)
            return CorridorShape.Straight;

        // Count direction changes
        int directionChanges = 0;
        for (int i = 2; i < path.Count; i++)
        {
            Vector2Int dir1 = path[i - 1] - path[i - 2];
            Vector2Int dir2 = path[i] - path[i - 1];
            if (dir1 != dir2)
                directionChanges++;
        }

        if (directionChanges == 1)
            return CorridorShape.L_Shaped;
        else if (directionChanges == 2)
            return CorridorShape.Z_Shaped;
        else
            return CorridorShape.Complex;
    }

    // === VALIDATION ===
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (_pathTiles.Count == 0)
            result.AddError($"Corridor {_corridorID} has no path tiles");

        if (_width < 1)
            result.AddError($"Corridor {_corridorID} has invalid width: {_width}");

        if (_width < 3)
            result.AddWarning($"Corridor {_corridorID} is narrow (width: {_width}), may cause NavMesh gaps");

        return result;
    }
}

public enum CorridorShape
{
    Point,      // Single tile (degenerate case)
    Straight,   // Horizontal or vertical line
    L_Shaped,   // One 90° turn
    Z_Shaped,   // Two 90° turns
    Complex     // Three or more turns
}
```

### 3.3 Supporting Structs

```csharp
/// <summary>
/// Lightweight struct representing a doorway position and orientation.
/// Used extensively in pathfinding - value type for performance.
/// </summary>
[System.Serializable]
public struct DoorwayPosition
{
    public Vector2Int position;
    public DoorwayDirection direction;
    public int width; // Number of tiles wide (1-3)

    public DoorwayPosition(Vector2Int pos, DoorwayDirection dir, int w = 1)
    {
        position = pos;
        direction = dir;
        width = w;
    }

    public Vector2Int GetDirectionVector()
    {
        switch (direction)
        {
            case DoorwayDirection.North: return Vector2Int.up;
            case DoorwayDirection.South: return Vector2Int.down;
            case DoorwayDirection.East: return Vector2Int.right;
            case DoorwayDirection.West: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }
}

public enum DoorwayDirection
{
    North,
    South,
    East,
    West
}

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
```

### 3.4 Why Classes Over Structs for RoomData/CorridorData?

**Decision Rationale:**

1. **Contains Collections**
   - `List<int> _connectedRoomIDs`
   - `List<DoorwayPosition> _doorways`
   - `List<Vector2Int> _pathTiles`
   - Collections are reference types; struct would just hold references anyway

2. **Hierarchical Relationships**
   - Rooms reference corridors, corridors reference rooms
   - Class reference semantics natural for graph structures

3. **Mutable State**
   - Classification changes during Phase 2
   - Template assignment happens post-construction
   - Structs require copying entire value on mutation

4. **Unity Serialization**
   - Unity serializes classes to scene/prefab files
   - `[SerializeField]` works seamlessly with classes
   - Struct serialization has edge cases with nested collections

**Performance Consideration:** For 100 rooms × 150 corridors, heap allocation is ~50KB. Negligible compared to tilemap memory (100×100 tiles × 4 bytes = 40KB).

---

## 4. Serialization Strategy

### 4.1 Unity Built-In Serialization

**What Unity Serializes:**

```csharp
[System.Serializable]
public class MapData
{
    // ✅ SERIALIZED by Unity
    [SerializeField] private int _seed;
    [SerializeField] private Vector2Int _mapSize;
    [SerializeField] private List<RoomData> _rooms;

    // ❌ NOT SERIALIZED (no [SerializeField])
    private Tilemap _floorTilemap;
    private Dictionary<int, RoomData> _roomLookup; // Dictionaries not serializable
    private BSPNode _rootNode; // Cyclic references problematic
}
```

**Unity Serialization Rules:**

| Type | Serializable? | Notes |
|------|---------------|-------|
| Primitives (`int`, `float`, `bool`) | ✅ Yes | Native support |
| Unity types (`Vector2Int`, `Color`) | ✅ Yes | Built-in |
| `List<T>` | ✅ Yes | T must be serializable |
| `Dictionary<K,V>` | ❌ No | Workaround: parallel lists |
| `class` with `[Serializable]` | ✅ Yes | Must be marked |
| `struct` with `[Serializable]` | ✅ Yes | Value type |
| Circular references | ⚠️ Partial | Unity handles some cases |
| Polymorphism | ❌ No | Must use `[SerializeReference]` |

### 4.2 Custom Serialization for Dictionaries

**Problem:** Need fast room lookup by ID, but Dictionary not serializable.

**Solution:** Dual representation pattern.

```csharp
public class MapData
{
    // Serialized: List for Unity serialization
    [SerializeField] private List<RoomData> _rooms;

    // Runtime: Dictionary for O(1) lookup
    private Dictionary<int, RoomData> _roomLookup;

    // Build lookup on load
    void OnAfterDeserialize()
    {
        _roomLookup = new Dictionary<int, RoomData>();
        foreach (var room in _rooms)
        {
            _roomLookup[room.RoomID] = room;
        }
    }

    // Query API uses dictionary
    public RoomData GetRoomByID(int id)
    {
        return _roomLookup.TryGetValue(id, out var room) ? room : null;
    }
}
```

**Why Not Just Use List?**

- List lookup: O(n) - Linear search through all rooms
- Dictionary lookup: O(1) - Constant time
- For 100 rooms queried 1000 times: List = 100,000 ops, Dictionary = 1,000 ops

### 4.3 Snapshot Pattern for Save/Load

**Problem:** `MapData` contains non-serializable references (Tilemaps, BSP nodes).

**Solution:** Snapshot pattern - separate serialization DTO.

```csharp
/// <summary>
/// Serialization snapshot of MapData.
/// Contains only serializable data, no Unity object references.
/// </summary>
[System.Serializable]
public class MapDataSnapshot
{
    public int seed;
    public Vector2Int mapSize;
    public RoomDataSnapshot[] rooms;
    public CorridorDataSnapshot[] corridors;
    public Vector2Int playerSpawnPosition;

    public MapDataSnapshot(MapData source)
    {
        seed = source.Seed;
        mapSize = source.MapSize;
        playerSpawnPosition = source.PlayerSpawnPosition;

        // Convert rooms to snapshots
        rooms = new RoomDataSnapshot[source.Rooms.Count];
        for (int i = 0; i < source.Rooms.Count; i++)
        {
            rooms[i] = new RoomDataSnapshot(source.Rooms[i]);
        }

        // Convert corridors
        corridors = new CorridorDataSnapshot[source.Corridors.Count];
        for (int i = 0; i < source.Corridors.Count; i++)
        {
            corridors[i] = new CorridorDataSnapshot(source.Corridors[i]);
        }
    }

    // Reconstruction
    public MapData ToMapData()
    {
        var mapData = new MapData(seed, mapSize);
        mapData.SetPlayerSpawn(playerSpawnPosition);

        // Rebuild rooms
        foreach (var roomSnapshot in rooms)
        {
            mapData.AddRoom(roomSnapshot.ToRoomData());
        }

        // Rebuild corridors
        foreach (var corridorSnapshot in corridors)
        {
            mapData.AddCorridor(corridorSnapshot.ToCorridorData());
        }

        return mapData;
    }
}

[System.Serializable]
public class RoomDataSnapshot
{
    public int roomID;
    public RectInt bounds;
    public int[] connectedRoomIDs;
    public RoomClassification classification;

    public RoomDataSnapshot(RoomData source)
    {
        roomID = source.RoomID;
        bounds = source.Bounds;
        connectedRoomIDs = source.ConnectedRoomIDs.ToArray();
        classification = source.Classification;
    }

    public RoomData ToRoomData()
    {
        var room = new RoomData(bounds);
        room.RoomID = roomID;
        room.SetClassification(classification);
        foreach (var id in connectedRoomIDs)
        {
            room.ConnectToRoom(id);
        }
        return room;
    }
}
```

**Save/Load Usage:**

```csharp
public class MapSaveSystem
{
    public void SaveMap(MapData mapData, string filePath)
    {
        var snapshot = mapData.CreateSnapshot();
        string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
        System.IO.File.WriteAllText(filePath, json);
    }

    public MapData LoadMap(string filePath)
    {
        string json = System.IO.File.ReadAllText(filePath);
        var snapshot = JsonUtility.FromJson<MapDataSnapshot>(json);
        return snapshot.ToMapData();
    }
}
```

### 4.4 Why Not Custom Binary Serialization?

**Alternative Considered:** BinaryFormatter, protobuf, MessagePack

**Rejected Because:**
1. **Unity Inspector Compatibility** - JSON/Unity serialization shows in Inspector
2. **Debugging** - Text format is human-readable
3. **Version Migration** - Easy to add fields without breaking old saves
4. **Platform Compatibility** - JSON works on WebGL, mobile, console
5. **Simplicity** - No external dependencies

**When to Use Binary:** If save files exceed 10MB or load time > 3 seconds.

---

## 5. Interface Design Philosophy

### 5.1 Subsystem Contracts

**Principle:** Each generation phase exposes a contract, not an implementation.

```csharp
/// <summary>
/// Contract for map structure generation (Phase 1: BSP).
/// Implementors provide different algorithms (BSP, WFC, Graph-based).
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// Generates map structure (rooms + corridors).
    /// </summary>
    /// <param name="settings">Configuration parameters</param>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <returns>Populated MapData with rooms and corridors</returns>
    MapData GenerateMap(MapGenerationSettings settings, int seed);

    /// <summary>
    /// Validates that generator can produce maps with given settings.
    /// </summary>
    ValidationResult ValidateSettings(MapGenerationSettings settings);
}

/// <summary>
/// Contract for classifying rooms based on spatial properties.
/// Used in Phase 2 to determine content placement strategy.
/// </summary>
public interface IRoomClassifier
{
    /// <summary>
    /// Classifies all rooms in map based on size, connectivity, distance.
    /// </summary>
    void ClassifyRooms(MapData mapData, Vector2Int playerSpawn);

    /// <summary>
    /// Classifies a single room.
    /// </summary>
    RoomClassification ClassifyRoom(RoomData room, MapData context);
}

/// <summary>
/// Contract for populating map with gameplay content.
/// </summary>
public interface IContentPopulator
{
    /// <summary>
    /// Populates map with spawn points, resources, templates.
    /// </summary>
    void PopulateContent(MapData mapData, ContentSettings settings);
}

/// <summary>
/// Contract for applying visual themes.
/// </summary>
public interface IBiomeApplicator
{
    /// <summary>
    /// Applies biome tiles and effects to map.
    /// </summary>
    void ApplyBiome(MapData mapData, BiomeConfig biome);
}
```

### 5.2 Why Interfaces Over Abstract Classes?

**Decision Matrix:**

| Factor | Interface | Abstract Class | Decision |
|--------|-----------|----------------|----------|
| Multiple implementations | ✅ Yes | ✅ Yes | Tie |
| Multiple inheritance | ✅ Yes | ❌ No | **Interface wins** |
| Default implementations | ❌ No (C# <8.0) | ✅ Yes | Abstract wins |
| Testability | ✅ Easy mocking | ⚠️ Harder | **Interface wins** |
| Unity serialization | ✅ Works | ✅ Works | Tie |
| Design intent | ✅ "Can do" | ⚠️ "Is a" | **Interface wins** for contracts |

**Example: Multiple Interface Implementation**

```csharp
public class HybridGenerator : MonoBehaviour, IMapGenerator, IContentPopulator
{
    // Implements both contracts
    public MapData GenerateMap(MapGenerationSettings settings, int seed) { ... }
    public void PopulateContent(MapData mapData, ContentSettings settings) { ... }
}
```

This is impossible with abstract classes due to single inheritance.

### 5.3 Dependency Injection Pattern

**Problem:** Hard dependencies make testing difficult.

**Solution:** Constructor injection with interfaces.

```csharp
public class MapGenerationController : MonoBehaviour
{
    // Injected dependencies
    private IMapGenerator _mapGenerator;
    private IRoomClassifier _roomClassifier;
    private IContentPopulator _contentPopulator;
    private IBiomeApplicator _biomeApplicator;

    // Unity Inspector configuration (fallback)
    [SerializeField] private GameObject mapGeneratorPrefab;
    [SerializeField] private GameObject contentPopulatorPrefab;

    void Awake()
    {
        // Manual dependency injection (Unity doesn't have DI container)
        _mapGenerator = mapGeneratorPrefab.GetComponent<IMapGenerator>();
        _contentPopulator = contentPopulatorPrefab.GetComponent<IContentPopulator>();

        // Or: Use Zenject/VContainer if available
    }

    public IEnumerator GenerateFullMap(int seed)
    {
        // 1. Generate structure
        var mapData = _mapGenerator.GenerateMap(settings, seed);
        yield return null;

        // 2. Classify rooms
        _roomClassifier.ClassifyRooms(mapData, playerSpawn);
        yield return null;

        // 3. Populate content
        _contentPopulator.PopulateContent(mapData, contentSettings);
        yield return null;

        // 4. Apply biome
        _biomeApplicator.ApplyBiome(mapData, selectedBiome);
        yield return null;
    }
}
```

**Testing Benefits:**

```csharp
[Test]
public void TestMapGeneration()
{
    // Mock dependencies
    var mockGenerator = new MockMapGenerator();
    var mockClassifier = new MockRoomClassifier();

    var controller = new MapGenerationController();
    controller.InjectDependencies(mockGenerator, mockClassifier, ...);

    // Test without Unity dependencies
    controller.GenerateFullMap(42);

    Assert.IsTrue(mockGenerator.WasCalled);
}
```

---

## 6. Memory Layout & Performance

### 6.1 Struct Memory Layout

**TilePosition Struct:**

```csharp
public struct TilePosition
{
    public int x;  // 4 bytes
    public int y;  // 4 bytes
    // Total: 8 bytes
}
```

**Memory for 100×100 map:** 100 × 100 × 8 bytes = 80 KB

**DoorwayPosition Struct:**

```csharp
public struct DoorwayPosition
{
    public Vector2Int position; // 8 bytes (2 × int)
    public DoorwayDirection direction; // 4 bytes (enum)
    public int width; // 4 bytes
    // Total: 16 bytes
}
```

**Typical room:** 4 doorways × 16 bytes = 64 bytes per room

### 6.2 Class Memory Overhead

**RoomData Class:**

```csharp
public class RoomData
{
    // Object header: 16 bytes (64-bit runtime)
    private int _roomID;  // 4 bytes
    private RectInt _bounds;  // 16 bytes (4 × int)
    private Vector2Int _center;  // 8 bytes
    private int _area;  // 4 bytes
    private List<int> _connectedRoomIDs;  // 8 bytes (reference) + list overhead (~32 bytes)
    private List<DoorwayPosition> _doorways;  // 8 bytes + overhead
    // ... more fields
    // Estimated total per room: ~150-200 bytes
}
```

**100 rooms:** 100 × 200 bytes = 20 KB

**Total map memory estimate:**
- Tile data: 80 KB
- Room objects: 20 KB
- Corridor objects: ~10 KB (fewer than rooms)
- BSP tree: ~5 KB (binary tree overhead)
- **Total: ~115 KB**

**Comparison:** Unity Tilemap for same map uses ~500 KB (TileBase references + chunk overhead).

### 6.3 Cache-Friendly Design

**Problem:** List iteration with random access is cache-unfriendly.

**Solution:** Sequential access patterns.

```csharp
// ❌ BAD: Random access via dictionary
foreach (var corridor in mapData.Corridors)
{
    var roomA = mapData.GetRoomByID(corridor.RoomA_ID); // Dictionary lookup
    var roomB = mapData.GetRoomByID(corridor.RoomB_ID); // Another lookup
    ProcessCorridor(roomA, roomB);
}

// ✅ GOOD: Sequential iteration with pre-computed connections
foreach (var room in mapData.Rooms)
{
    foreach (var connectedRoomID in room.ConnectedRoomIDs)
    {
        // Process connection
    }
}
```

**Performance Impact:** Sequential access is 3-5× faster due to CPU cache prefetching.

### 6.4 Object Pooling for Runtime Data

**Problem:** Creating/destroying spawn points and resources causes GC pressure.

**Solution:** Pool pattern (already exists in `ObjectPooler`).

```csharp
public class SpawnPointData
{
    // Mark as poolable
    private static Queue<SpawnPointData> _pool = new Queue<SpawnPointData>();

    public static SpawnPointData Acquire()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();
        return new SpawnPointData();
    }

    public void Release()
    {
        // Clear state
        roomID = -1;
        position = Vector2Int.zero;
        _pool.Enqueue(this);
    }

    // Data fields
    public int roomID;
    public Vector2Int position;
    public RoomClassification roomType;
}
```

**GC Impact:** Without pooling, 1000 spawn point allocations = 200 KB garbage. With pooling: 0 garbage after first wave.

---

## 7. ScriptableObject Usage

### 7.1 Configuration ScriptableObjects

**Principle:** Separate data (ScriptableObject) from behavior (MonoBehaviour).

```csharp
/// <summary>
/// Immutable configuration for map generation.
/// Created as asset in Editor, referenced at runtime.
/// </summary>
[CreateAssetMenu(fileName = "MapSettings", menuName = "Map Generation/Map Settings")]
public class MapGenerationSettings : ScriptableObject
{
    [Header("Map Size")]
    [Range(50, 500)] public int mapWidth = 100;
    [Range(50, 500)] public int mapHeight = 100;

    [Header("BSP Parameters")]
    [Range(5, 30)] public int minRoomSize = 8;
    [Range(10, 50)] public int maxRoomSize = 20;
    [Range(1, 10)] public int maxBSPDepth = 5;

    [Header("Corridor Configuration")]
    [Range(1, 5)] public int corridorWidth = 3;
    public bool useTwoPassCorridors = true;

    [Header("Validation")]
    public bool enforceConnectivity = true;
    public bool enforceNavMeshCoverage = true;
    [Range(0.5f, 1f)] public float minNavMeshCoveragePercent = 0.9f;

    // Validation
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (mapWidth * mapHeight > 250000) // 500×500
            result.AddWarning("Large map size may impact performance");

        if (minRoomSize > maxRoomSize)
            result.AddError("minRoomSize cannot exceed maxRoomSize");

        if (corridorWidth < 3)
            result.AddWarning("Narrow corridors may cause NavMesh gaps");

        return result;
    }
}
```

### 7.2 Why ScriptableObjects Over MonoBehaviour Configuration?

**Comparison:**

| Factor | ScriptableObject | MonoBehaviour |
|--------|------------------|---------------|
| **Shareable** | ✅ Single asset, many scenes | ❌ Per-scene instance |
| **Version Control** | ✅ Separate asset file | ⚠️ Embedded in scene |
| **Runtime Overhead** | ✅ Minimal (no Update) | ❌ GameObject overhead |
| **Hot-Reload** | ✅ Changes apply immediately | ⚠️ Requires scene reload |
| **Testable** | ✅ Easy to create in tests | ❌ Requires Unity runtime |

**Example: Multiple Difficulty Presets**

```
Assets/Settings/
├── MapSettings_Easy.asset      (small maps, many resources)
├── MapSettings_Normal.asset    (balanced)
└── MapSettings_Hard.asset      (large maps, scarce resources)
```

**Runtime Selection:**

```csharp
public class DifficultyManager : MonoBehaviour
{
    [SerializeField] private MapGenerationSettings easySettings;
    [SerializeField] private MapGenerationSettings normalSettings;
    [SerializeField] private MapGenerationSettings hardSettings;

    public MapGenerationSettings GetSettings(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return easySettings;
            case Difficulty.Normal: return normalSettings;
            case Difficulty.Hard: return hardSettings;
            default: return normalSettings;
        }
    }
}
```

### 7.3 ScriptableObject Singleton Pattern

**Problem:** Global access to configuration needed across systems.

**Solution:** ScriptableObject singleton.

```csharp
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<GameConfig>("GameConfig");
            return _instance;
        }
    }

    public MapGenerationSettings mapSettings;
    public ContentSettings contentSettings;
    public BiomeRegistry biomeRegistry;
}

// Usage anywhere:
var mapSize = GameConfig.Instance.mapSettings.mapWidth;
```

**Caveat:** Singletons create hidden dependencies. Use sparingly for truly global configuration.

---

## 8. Unity Serialization Constraints

### 8.1 What Unity Can't Serialize

**Dictionary:**

```csharp
// ❌ NOT SERIALIZED
[SerializeField] private Dictionary<int, RoomData> _roomLookup;
```

**Workaround: Parallel Lists**

```csharp
// ✅ SERIALIZED
[SerializeField] private List<int> _roomIDs;
[SerializeField] private List<RoomData> _rooms;

private Dictionary<int, RoomData> _roomLookup; // Runtime only

void OnAfterDeserialize()
{
    _roomLookup = new Dictionary<int, RoomData>();
    for (int i = 0; i < _roomIDs.Count; i++)
    {
        _roomLookup[_roomIDs[i]] = _rooms[i];
    }
}

void OnBeforeSerialize()
{
    _roomIDs.Clear();
    _rooms.Clear();
    foreach (var kvp in _roomLookup)
    {
        _roomIDs.Add(kvp.Key);
        _rooms.Add(kvp.Value);
    }
}
```

### 8.2 Polymorphic Serialization

**Problem:** Unity doesn't serialize polymorphic fields by default.

```csharp
public interface ISpawnable { }
public class Enemy : MonoBehaviour, ISpawnable { }
public class Item : MonoBehaviour, ISpawnable { }

// ❌ FAILS: Unity sees only interface reference
[SerializeField] private ISpawnable spawnable;
```

**Solution: `[SerializeReference]` (Unity 2019.3+)**

```csharp
[SerializeReference] private ISpawnable spawnable; // ✅ Works
```

**Alternative: Concrete Type Registry**

```csharp
public enum SpawnableType { Enemy, Item }

[System.Serializable]
public class SpawnableData
{
    public SpawnableType type;
    public GameObject prefab;
}

// No polymorphism needed
[SerializeField] private List<SpawnableData> spawnables;
```

### 8.3 Circular References

**Problem:** Rooms reference corridors, corridors reference rooms.

```csharp
public class RoomData
{
    public List<CorridorData> connectedCorridors; // ❌ Circular!
}

public class CorridorData
{
    public RoomData roomA; // ❌ Points back to RoomData
}
```

**Unity's Behavior:** Will serialize, but can cause issues with deep copies and prefab overrides.

**Better Design: ID References**

```csharp
public class RoomData
{
    public List<int> connectedCorridorIDs; // ✅ ID instead of reference
}

public class CorridorData
{
    public int roomA_ID; // ✅ ID instead of reference
}
```

**Resolution at Runtime:**

```csharp
public CorridorData GetCorridor(int corridorID)
{
    return _corridors[corridorID]; // O(1) array access
}
```

---

## 9. Data Validation Architecture

### 9.1 Validation Result Pattern

```csharp
/// <summary>
/// Standardized validation result across all data types.
/// Collects errors (must fix) and warnings (should review).
/// </summary>
public class ValidationResult
{
    private List<string> _errors = new List<string>();
    private List<string> _warnings = new List<string>();

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Warnings => _warnings;
    public bool IsValid => _errors.Count == 0;
    public bool HasWarnings => _warnings.Count > 0;

    public void AddError(string message)
    {
        _errors.Add($"[ERROR] {message}");
        Debug.LogError(message);
    }

    public void AddWarning(string message)
    {
        _warnings.Add($"[WARNING] {message}");
        Debug.LogWarning(message);
    }

    public void Merge(ValidationResult other)
    {
        _errors.AddRange(other._errors);
        _warnings.AddRange(other._warnings);
    }

    public string GetSummary()
    {
        return $"Validation: {_errors.Count} errors, {_warnings.Count} warnings";
    }

    public void LogAll()
    {
        foreach (var error in _errors)
            Debug.LogError(error);
        foreach (var warning in _warnings)
            Debug.LogWarning(warning);
    }
}
```

### 9.2 Validation Pipeline

**Multi-Stage Validation:**

```csharp
public class MapValidator
{
    public ValidationResult ValidateCompleteMap(MapData mapData)
    {
        var result = new ValidationResult();

        // Stage 1: Structural integrity
        result.Merge(ValidateStructure(mapData));

        // Stage 2: Connectivity
        if (result.IsValid)
            result.Merge(ValidateConnectivity(mapData));

        // Stage 3: Gameplay requirements
        if (result.IsValid)
            result.Merge(ValidateGameplay(mapData));

        // Stage 4: Performance constraints
        result.Merge(ValidatePerformance(mapData));

        return result;
    }

    private ValidationResult ValidateStructure(MapData mapData)
    {
        var result = new ValidationResult();

        // Validate map itself
        result.Merge(mapData.Validate());

        // Validate each room
        foreach (var room in mapData.Rooms)
        {
            result.Merge(room.Validate());
        }

        // Validate each corridor
        foreach (var corridor in mapData.Corridors)
        {
            result.Merge(corridor.Validate());
        }

        return result;
    }

    private ValidationResult ValidateConnectivity(MapData mapData)
    {
        var result = new ValidationResult();

        // All rooms must be reachable from player spawn
        var reachableRooms = FloodFillRooms(mapData, mapData.PlayerSpawnPosition);
        if (reachableRooms.Count != mapData.Rooms.Count)
        {
            result.AddError($"Only {reachableRooms.Count}/{mapData.Rooms.Count} rooms are reachable");
        }

        return result;
    }

    private ValidationResult ValidateGameplay(MapData mapData)
    {
        var result = new ValidationResult();

        // Must have player spawn
        if (mapData.PlayerSpawnPosition == Vector2Int.zero)
            result.AddError("Player spawn not set");

        // Must have at least one safe room
        if (!mapData.Rooms.Any(r => r.Classification == RoomClassification.SafeRoom))
            result.AddWarning("No safe rooms for player respite");

        return result;
    }

    private ValidationResult ValidatePerformance(MapData mapData)
    {
        var result = new ValidationResult();

        int totalTiles = mapData.MapSize.x * mapData.MapSize.y;
        if (totalTiles > 250000) // 500×500
            result.AddWarning($"Large map ({totalTiles} tiles) may impact performance");

        return result;
    }

    private HashSet<RoomData> FloodFillRooms(MapData mapData, Vector2Int startPos)
    {
        var visited = new HashSet<RoomData>();
        var queue = new Queue<RoomData>();

        // Find starting room
        var startRoom = mapData.Rooms.FirstOrDefault(r => r.Bounds.Contains(startPos));
        if (startRoom == null)
            return visited;

        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var connectedID in current.ConnectedRoomIDs)
            {
                var connected = mapData.Rooms.FirstOrDefault(r => r.RoomID == connectedID);
                if (connected != null && !visited.Contains(connected))
                {
                    visited.Add(connected);
                    queue.Enqueue(connected);
                }
            }
        }

        return visited;
    }
}
```

### 9.3 Validation in Unity Editor

**Custom Inspector Validation:**

```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MapGenerationSettings))]
public class MapGenerationSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        var settings = (MapGenerationSettings)target;
        var result = settings.Validate();

        if (result.IsValid)
        {
            EditorGUILayout.HelpBox("✓ Settings are valid", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"⚠ {result.Errors.Count} errors, {result.Warnings.Count} warnings", MessageType.Error);
            foreach (var error in result.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        if (result.HasWarnings)
        {
            foreach (var warning in result.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }
    }
}
#endif
```

**Result in Inspector:**

```
MapGenerationSettings
├── Map Width: 100
├── Map Height: 100
├── Min Room Size: 8
├── Max Room Size: 20
└── Validation
    ✓ Settings are valid
    ⚠ Warning: Narrow corridors may cause NavMesh gaps
```

---

## Conclusion: Phase 0 Architectural Foundation

### Key Achievements

1. **Clean Data Separation**
   - Structure (MapData, RoomData) independent of behavior
   - Configuration (ScriptableObjects) separate from runtime state
   - Serialization (snapshots) decoupled from live data

2. **Performance-Conscious Design**
   - Struct-based primitives for hot-path data
   - Cache-friendly sequential access patterns
   - ~115 KB memory footprint for 100-room map

3. **Unity Integration**
   - Native serialization with workarounds for limitations
   - ScriptableObject configuration pattern
   - Inspector-friendly validation

4. **Interface-Driven Extensibility**
   - Generator, classifier, populator contracts
   - Enables multiple algorithm implementations
   - Testable without Unity dependencies

5. **Robust Validation**
   - Multi-stage validation pipeline
   - Clear error/warning separation
   - Editor integration for immediate feedback

### Architectural Trade-Offs

| Decision | Benefit | Cost |
|----------|---------|------|
| **Class-based rooms** | Flexible, hierarchical | Heap allocation |
| **Struct-based positions** | Cache-friendly, fast | No identity, copying |
| **Interface contracts** | Testable, extensible | Indirection overhead |
| **Snapshot serialization** | Clean separation | Conversion overhead |
| **ID-based references** | Avoids circular refs | Requires lookup table |

### Design Patterns Applied

1. **Data Transfer Object (DTO)** - Snapshot pattern
2. **Repository** - MapData as aggregate root
3. **Strategy** - IMapGenerator interface
4. **Singleton** - GameConfig ScriptableObject
5. **Object Pool** - SpawnPointData pooling
6. **Validation** - ValidationResult accumulator

### Next Steps

**Phase 1 Dependencies:**
- BSP algorithm needs `MapData.AddRoom()` and `MapData.AddCorridor()`
- Corridor generator needs `DoorwayPosition` structs
- Tilemap renderer needs `RoomData.Bounds` and `CorridorData.PathTiles`

**Phase 2 Dependencies:**
- Room classifier needs `RoomData.SetClassification()`
- Spawn manager needs `RoomData.Classification` query
- Content populator needs `MapData.AddEnemySpawnPoint()` and `AddResource()`

**Testing Requirements:**
- Unit tests for all validation logic
- Integration tests for serialization round-trips
- Performance tests for large map data structures

---

**Document Status:** ✅ Complete
**Review Required:** Lead Engineer, Unity Architect
**Implementation Target:** Phase 0 Foundation
**Estimated Effort:** 2-3 days

**Token Count:** ~18,500 tokens (under 20,000 limit)

---

**References:**
- MAP_GENERATION_PLAN.md (BSP algorithm context)
- PHASE_2_ARCHITECTURE_DEEP_DIVE.md (content system context)
- Unity Serialization Documentation
- C# Struct vs Class Performance Benchmarks
