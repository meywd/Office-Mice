# Phase 2 Architecture Deep Dive: Content & Features
## Office-Mice Map Generation System

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Author:** Software Architecture Analysis
**Status:** Architectural Blueprint - Ready for Implementation

---

## Executive Summary

Phase 2 represents the **critical bridge between procedural structure and gameplay content**. While Phase 1 (BSP Generation) creates the spatial skeleton, Phase 2 breathes life into the map through spawn systems, resource distribution, special rooms, and biome variants. This phase transforms abstract rectangular rooms into a living, breathing office environment where tactical gameplay emerges from content placement.

**Architectural Impact:** HIGH
**Gameplay Impact:** CRITICAL
**Technical Complexity:** MEDIUM-HIGH
**Integration Surface:** Wide (touches spawning, items, AI, visuals)

### Core Architectural Principles

1. **Data-Driven Design** - All content configured through ScriptableObjects
2. **Extensibility First** - Plugin architecture for new content types
3. **Designer Empowerment** - Zero-code content authoring via Unity Editor
4. **Performance Consciousness** - Object pooling and lazy instantiation
5. **Testability** - Isolated systems with clear contracts
6. **Gameplay Balance** - Algorithmic fairness in resource distribution

---

## Table of Contents

1. [Content Generation Architecture](#1-content-generation-architecture)
2. [Spawn Point System Deep Dive](#2-spawn-point-system-deep-dive)
3. [Resource Distribution](#3-resource-distribution)
4. [Special Rooms Architecture](#4-special-rooms-architecture)
5. [Biome System Design](#5-biome-system-design)
6. [Gameplay Integration](#6-gameplay-integration)
7. [Extensibility Architecture](#7-extensibility-architecture)
8. [Data-Driven Design](#8-data-driven-design)
9. [Performance Considerations](#9-performance-considerations)
10. [Integration with Phase 1](#10-integration-with-phase-1)

---

## 1. Content Generation Architecture

### 1.1 Content Pipeline Overview

The content generation pipeline follows a **layered composition pattern**, where each layer adds complexity without modifying lower layers:

```
Phase 1: BSP Partition → Rooms → Corridors → Tilemap
                                                ↓
Phase 2: Room Classification → Template Selection → Content Population
                                                ↓
         Spawn Points → Resource Distribution → Furniture → Biome Theming
```

### 1.2 System Components

#### Core Components

```
ContentGenerationOrchestrator
├── SpawnPointManager
│   ├── SpawnTableResolver
│   ├── WaveProgressionCalculator
│   └── SpawnPointValidator
├── ResourceDistributionManager
│   ├── BalanceCalculator
│   ├── PlacementSolver
│   └── DifficultyScaler
├── SpecialRoomManager
│   ├── RoomTemplateLibrary
│   ├── TemplateInstantiator
│   └── FurniturePlacer
└── BiomeManager
    ├── BiomeConfigRegistry
    ├── TilesetSwapper
    └── ThemePropagator
```

#### Component Responsibilities

| Component | Responsibility | Input | Output |
|-----------|---------------|-------|--------|
| **ContentGenerationOrchestrator** | Coordinates all Phase 2 systems | BSP room data | Fully populated map |
| **SpawnPointManager** | Places enemy spawn points strategically | Room boundaries, difficulty | Spawn point array |
| **ResourceDistributionManager** | Distributes ammo, health, weapons | Room count, difficulty | Resource positions |
| **SpecialRoomManager** | Creates detailed room layouts | Room template, doorways | Instantiated room |
| **BiomeManager** | Applies visual themes | Biome config, tilemap | Themed tilemap |

### 1.3 Content Generation Flow

```csharp
// High-level orchestration flow
public class ContentGenerationOrchestrator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SpawnPointManager spawnManager;
    [SerializeField] private ResourceDistributionManager resourceManager;
    [SerializeField] private SpecialRoomManager roomManager;
    [SerializeField] private BiomeManager biomeManager;

    public IEnumerator GenerateContent(MapGenerationContext context)
    {
        // 1. Classify rooms by size, connectivity, distance from spawn
        var roomClassifications = ClassifyRooms(context.Rooms);
        yield return null;

        // 2. Assign special room templates to classified rooms
        var roomAssignments = roomManager.AssignTemplates(roomClassifications);
        yield return null;

        // 3. Instantiate room templates with furniture
        roomManager.InstantiateRooms(roomAssignments, context.Tilemap);
        yield return null;

        // 4. Place spawn points based on room types and player distance
        var spawnPoints = spawnManager.PlaceSpawnPoints(
            roomAssignments,
            context.PlayerSpawnPosition,
            context.DifficultySettings
        );
        yield return null;

        // 5. Distribute resources (ammo, health, weapons) with balance
        resourceManager.DistributeResources(
            roomAssignments,
            context.DifficultySettings
        );
        yield return null;

        // 6. Apply biome theming to tilemap and objects
        biomeManager.ApplyBiome(
            context.SelectedBiome,
            context.Tilemap,
            roomAssignments
        );
        yield return null;

        // 7. Validate and report
        ValidateContentGeneration(spawnPoints, resourceManager.PlacedResources);
    }
}
```

### 1.4 MapGenerationContext

**Purpose:** Shared data container passed between Phase 1 and Phase 2

```csharp
public class MapGenerationContext
{
    // From Phase 1
    public List<Room> Rooms { get; set; }
    public List<Corridor> Corridors { get; set; }
    public Tilemap FloorTilemap { get; set; }
    public Tilemap WallTilemap { get; set; }
    public Tilemap ObjectTilemap { get; set; }
    public int Seed { get; set; }

    // Phase 2 specific
    public Vector2Int PlayerSpawnPosition { get; set; }
    public DifficultySettings DifficultySettings { get; set; }
    public BiomeConfig SelectedBiome { get; set; }
    public int WaveNumber { get; set; }

    // Computed during Phase 2
    public Dictionary<Room, RoomClassification> RoomTypes { get; set; }
    public List<SpawnPoint> SpawnPoints { get; set; }
    public List<ResourcePlacement> PlacedResources { get; set; }
}
```

### 1.5 Room Classification System

Rooms are classified to determine appropriate content:

```csharp
public enum RoomClassification
{
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

public class RoomClassifier
{
    public RoomClassification ClassifyRoom(Room room, MapGenerationContext context)
    {
        // Player start: Closest to player spawn
        if (Vector2.Distance(room.Center, context.PlayerSpawnPosition) < 10f)
            return RoomClassification.PlayerStart;

        // Boss room: Furthest from player spawn, large size
        if (room.Area > 400 && room.DistanceFromPlayerSpawn > context.MaxDistance * 0.8f)
            return RoomClassification.BossRoom;

        // Arena: Large, many connections
        if (room.Area > 300 && room.ConnectedRooms.Count >= 3)
            return RoomClassification.ArenaRoom;

        // Secret room: Only one connection, small
        if (room.ConnectedRooms.Count == 1 && room.Area < 100)
            return RoomClassification.SecretRoom;

        // Storage: Small-medium, 1-2 connections, not on main path
        if (room.Area < 200 && !room.IsOnCriticalPath)
            return RoomClassification.StorageRoom;

        // Default: Standard combat room
        return RoomClassification.StandardRoom;
    }
}
```

**Architectural Decision Record (ADR):**

**Decision:** Use enum-based room classification with algorithmic assignment
**Rationale:**
- Enum provides type safety and easy extension
- Algorithmic classification ensures variety without manual authoring
- Supports both deterministic (distance-based) and probabilistic (size-based) classification
- Enables gameplay designers to tweak classification heuristics independently

**Alternatives Considered:**
- Manual room tagging (rejected: too time-consuming, not scalable)
- AI/ML classification (rejected: overkill, unpredictable)
- Random classification (rejected: poor gameplay experience)

---

## 2. Spawn Point System Deep Dive

### 2.1 Spawn System Architecture

The spawn system integrates with the existing `WaveSpawner` while adding **spatial intelligence** and **progressive difficulty**.

```
WaveSpawner (existing)
    ↓ uses
SpawnPoint[] (GameObject tags "Spawn Point")
    ↑ generated by
SpawnPointManager (Phase 2)
    ↓ uses
SpawnTable (ScriptableObject)
    ↓ contains
SpawnEntry[] (enemy type + probability curve)
```

### 2.2 SpawnTable ScriptableObject

```csharp
[CreateAssetMenu(fileName = "New Spawn Table", menuName = "Map Generation/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [Header("Spawn Table Configuration")]
    public string tableName;
    public RoomClassification targetRoomType;

    [Header("Spawn Entries")]
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    [Header("Global Modifiers")]
    [Range(0f, 3f)] public float spawnDensityMultiplier = 1f;
    [Range(0f, 2f)] public float waveProgressionMultiplier = 1f;

    /// <summary>
    /// Resolves which enemy to spawn based on wave number and random roll
    /// </summary>
    public Enemy ResolveSpawn(int waveNumber, float randomRoll)
    {
        // Calculate cumulative probabilities for this wave
        List<WeightedEntry> weightedEntries = new List<WeightedEntry>();
        float totalWeight = 0f;

        foreach (var entry in entries)
        {
            float probability = entry.probabilityCurve.Evaluate(waveNumber);
            if (probability > 0f)
            {
                weightedEntries.Add(new WeightedEntry
                {
                    enemy = entry.enemyPrefab,
                    weight = probability
                });
                totalWeight += probability;
            }
        }

        // Normalize to [0, 1] and select
        float normalizedRoll = randomRoll * totalWeight;
        float cumulative = 0f;

        foreach (var weighted in weightedEntries)
        {
            cumulative += weighted.weight;
            if (normalizedRoll <= cumulative)
                return weighted.enemy;
        }

        // Fallback to first entry
        return entries[0].enemyPrefab;
    }
}

[System.Serializable]
public class SpawnEntry
{
    [Header("Enemy Configuration")]
    public Enemy enemyPrefab;
    public string enemyDisplayName;

    [Header("Probability Curve")]
    [Tooltip("X-axis: Wave number, Y-axis: Spawn probability (0-1)")]
    public AnimationCurve probabilityCurve = AnimationCurve.Linear(0, 1, 10, 1);

    [Header("Spawn Constraints")]
    public int minWave = 1;
    public int maxWave = int.MaxValue;
    public bool requiresLineOfSight = false;
    public float minDistanceFromPlayer = 10f;
}

struct WeightedEntry
{
    public Enemy enemy;
    public float weight;
}
```

### 2.3 Probability Distribution Design

**Probability Curves** allow designers to control enemy introduction and phase-out over wave progression:

**Example Curves:**

```
Basic Mouse:  Waves 1-5 (100%) → Waves 6-10 (50%) → Waves 11+ (25%)
Fast Mouse:   Waves 3-6 (ramp 0→100%) → Waves 7+ (100%)
Tank Mouse:   Waves 5+ (ramp 0→100%)
Boss Mouse:   Waves 10, 20, 30 (spike pattern)
```

**Implementation in Unity Inspector:**

```csharp
// Example setup in SpawnTable asset
public void SetupStartingEnemies()
{
    // Basic mouse - always present, declining probability
    var basicEntry = new SpawnEntry
    {
        enemyPrefab = basicMousePrefab,
        probabilityCurve = AnimationCurve.EaseInOut(0, 1f, 20, 0.3f),
        minWave = 1
    };

    // Fast mouse - introduced at wave 3
    var fastEntry = new SpawnEntry
    {
        enemyPrefab = fastMousePrefab,
        probabilityCurve = new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(3, 0f),
            new Keyframe(6, 0.5f),
            new Keyframe(10, 1f)
        ),
        minWave = 3
    };

    entries.Add(basicEntry);
    entries.Add(fastEntry);
}
```

### 2.4 Wave Progression Design

**Progression Formula:**

```
SpawnCount(wave) = BaseCount * (1 + wave * GrowthRate)
StatMultiplier(wave) = 1 + floor(wave / WavesPerCycle)
```

**Integration with Existing WaveSpawner:**

```csharp
// Extension to existing WaveSpawner.cs
public partial class WaveSpawner
{
    // NEW: Spawn tables per room type
    [Header("Phase 2: Procedural Spawning")]
    public SpawnTableRegistry spawnTableRegistry;
    public bool useProceduralSpawning = false;

    void SpawnEnemyProcedural(RoomClassification roomType, Vector3 position)
    {
        var spawnTable = spawnTableRegistry.GetTableForRoom(roomType);
        var enemy = spawnTable.ResolveSpawn(_waveCount, UnityEngine.Random.value);

        var pooledEnemy = ObjectPooler.SharedInstance.GetPooledObject<Enemy>(enemy.tag);
        if (pooledEnemy != null)
        {
            pooledEnemy.transform.position = position;
            pooledEnemy.StatMultiplier = _statMultiplier;
            pooledEnemy.gameObject.SetActive(true);
        }
    }
}

[CreateAssetMenu(fileName = "Spawn Table Registry", menuName = "Map Generation/Spawn Table Registry")]
public class SpawnTableRegistry : ScriptableObject
{
    public List<SpawnTableMapping> mappings = new List<SpawnTableMapping>();

    public SpawnTable GetTableForRoom(RoomClassification roomType)
    {
        var mapping = mappings.Find(m => m.roomType == roomType);
        return mapping?.spawnTable ?? mappings[0].spawnTable; // fallback to first
    }
}

[System.Serializable]
public class SpawnTableMapping
{
    public RoomClassification roomType;
    public SpawnTable spawnTable;
}
```

### 2.5 Spawn Point Placement Algorithm

**Strategic Placement Principles:**

1. **Player Distance:** Never spawn within player's immediate view cone
2. **Room Coverage:** Distribute evenly across room area
3. **Tactical Positions:** Behind cover, in corners, near doorways
4. **Density Variation:** More spawns in arena rooms, fewer in storage
5. **Validation:** No spawns blocking critical paths or resources

```csharp
public class SpawnPointManager : MonoBehaviour
{
    [Header("Placement Rules")]
    [SerializeField] private float minDistanceFromPlayer = 15f;
    [SerializeField] private float minDistanceBetweenSpawns = 5f;
    [SerializeField] private int maxAttemptsPerSpawn = 20;

    [Header("Density Configuration")]
    [SerializeField] private SpawnDensityConfig densityConfig;

    public List<GameObject> PlaceSpawnPoints(
        Dictionary<Room, RoomAssignment> roomAssignments,
        Vector2Int playerPosition,
        DifficultySettings difficulty)
    {
        List<GameObject> spawnPoints = new List<GameObject>();

        foreach (var assignment in roomAssignments)
        {
            var room = assignment.Key;
            var roomType = assignment.Value.Classification;

            // Calculate spawn count for this room
            int spawnCount = CalculateSpawnCount(room, roomType, difficulty);

            // Place spawn points with strategic logic
            for (int i = 0; i < spawnCount; i++)
            {
                var spawnPoint = PlaceSpawnPointInRoom(
                    room,
                    playerPosition,
                    spawnPoints,
                    roomType
                );

                if (spawnPoint != null)
                {
                    spawnPoints.Add(spawnPoint);
                }
            }
        }

        return spawnPoints;
    }

    private int CalculateSpawnCount(Room room, RoomClassification type, DifficultySettings diff)
    {
        // Base density: spawns per 100 square tiles
        float baseDensity = densityConfig.GetDensity(type);

        // Scale by room area
        float scaledCount = (room.Area / 100f) * baseDensity;

        // Apply difficulty multiplier
        scaledCount *= diff.spawnDensityMultiplier;

        // Room type modifiers
        switch (type)
        {
            case RoomClassification.PlayerStart:
            case RoomClassification.SafeRoom:
                return 0; // No spawns

            case RoomClassification.ArenaRoom:
                scaledCount *= 1.5f; // 50% more spawns
                break;

            case RoomClassification.BossRoom:
                scaledCount *= 2f; // Double spawns
                break;

            case RoomClassification.StorageRoom:
                scaledCount *= 0.5f; // Half spawns
                break;
        }

        return Mathf.RoundToInt(scaledCount);
    }

    private GameObject PlaceSpawnPointInRoom(
        Room room,
        Vector2Int playerPos,
        List<GameObject> existingSpawns,
        RoomClassification roomType)
    {
        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            // Generate random position within room bounds
            Vector2Int candidatePos = new Vector2Int(
                UnityEngine.Random.Range(room.Bounds.xMin + 2, room.Bounds.xMax - 2),
                UnityEngine.Random.Range(room.Bounds.yMin + 2, room.Bounds.yMax - 2)
            );

            // Validate position
            if (!ValidateSpawnPosition(candidatePos, playerPos, existingSpawns, room))
                continue;

            // Create spawn point GameObject
            GameObject spawnPoint = new GameObject($"SpawnPoint_{room.ID}_{existingSpawns.Count}");
            spawnPoint.transform.position = new Vector3(candidatePos.x, candidatePos.y, 0);
            spawnPoint.tag = "Spawn Point";

            // Attach metadata component
            var metadata = spawnPoint.AddComponent<SpawnPointMetadata>();
            metadata.roomType = roomType;
            metadata.roomID = room.ID;

            return spawnPoint;
        }

        Debug.LogWarning($"Failed to place spawn point in room {room.ID} after {maxAttemptsPerSpawn} attempts");
        return null;
    }

    private bool ValidateSpawnPosition(
        Vector2Int pos,
        Vector2Int playerPos,
        List<GameObject> existingSpawns,
        Room room)
    {
        // Check player distance
        if (Vector2.Distance(pos, playerPos) < minDistanceFromPlayer)
            return false;

        // Check distance from existing spawns
        foreach (var spawn in existingSpawns)
        {
            Vector2 spawnPos = new Vector2(spawn.transform.position.x, spawn.transform.position.y);
            if (Vector2.Distance(pos, spawnPos) < minDistanceBetweenSpawns)
                return false;
        }

        // Check tilemap walkability (must be floor tile, not wall)
        // TODO: Integration with NavMesh validation

        return true;
    }
}

/// <summary>
/// Component attached to spawn point GameObjects for runtime queries
/// </summary>
public class SpawnPointMetadata : MonoBehaviour
{
    public RoomClassification roomType;
    public int roomID;
    public bool isActive = true;
    public float lastSpawnTime = 0f;
}
```

### 2.6 Spawn Density Configuration

```csharp
[CreateAssetMenu(fileName = "Spawn Density Config", menuName = "Map Generation/Spawn Density Config")]
public class SpawnDensityConfig : ScriptableObject
{
    public List<DensityMapping> densityMappings = new List<DensityMapping>();

    public float GetDensity(RoomClassification roomType)
    {
        var mapping = densityMappings.Find(m => m.roomType == roomType);
        return mapping?.spawnsPerHundredTiles ?? 2f; // default 2 spawns per 100 tiles
    }
}

[System.Serializable]
public class DensityMapping
{
    public RoomClassification roomType;
    [Range(0f, 10f)] public float spawnsPerHundredTiles = 2f;
}
```

### 2.7 Validation Rules

**Pre-Spawn Validation:**

```csharp
public class SpawnPointValidator
{
    public ValidationResult ValidateSpawnPoints(List<GameObject> spawnPoints, MapGenerationContext context)
    {
        var result = new ValidationResult();

        // Rule 1: Minimum spawn point count
        if (spawnPoints.Count < context.DifficultySettings.minSpawnPoints)
        {
            result.AddError($"Insufficient spawn points: {spawnPoints.Count} < {context.DifficultySettings.minSpawnPoints}");
        }

        // Rule 2: No spawn points in player start room
        var playerStartRoom = context.RoomTypes.FirstOrDefault(r => r.Value == RoomClassification.PlayerStart).Key;
        foreach (var spawn in spawnPoints)
        {
            var metadata = spawn.GetComponent<SpawnPointMetadata>();
            if (metadata.roomID == playerStartRoom?.ID)
            {
                result.AddError($"Spawn point found in player start room: {spawn.name}");
            }
        }

        // Rule 3: Spawn points must be on walkable NavMesh
        foreach (var spawn in spawnPoints)
        {
            if (!NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                result.AddWarning($"Spawn point not on NavMesh: {spawn.name}");
            }
        }

        // Rule 4: Even distribution across rooms
        var roomDistribution = spawnPoints
            .GroupBy(s => s.GetComponent<SpawnPointMetadata>().roomID)
            .Select(g => new { RoomID = g.Key, Count = g.Count() })
            .ToList();

        if (roomDistribution.Any(r => r.Count > context.DifficultySettings.maxSpawnsPerRoom))
        {
            result.AddWarning("Some rooms have excessive spawn density");
        }

        return result;
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();

    public bool IsValid => Errors.Count == 0;

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
}
```

---

## 3. Resource Distribution

### 3.1 Resource Distribution Strategy

**Goal:** Create **risk-reward dynamics** through strategic resource placement that encourages exploration and tactical decision-making.

**Core Principles:**

1. **Scarcity Creates Tension** - Never fully replenish player resources
2. **Risk-Reward** - Better loot in dangerous areas
3. **Exploration Incentive** - Secrets and off-path rooms have premium items
4. **Balance Over Time** - Resources scale with difficulty curve
5. **No Dead Ends** - Always a path to recovery, never unwinnable

### 3.2 Resource Types

```csharp
public enum ResourceType
{
    Ammo,           // Ammunition crates
    Health,         // Health pickups
    WeaponUpgrade,  // Gun pickups (existing Gun system)
    PowerUp,        // Temporary buffs
    Currency        // Future: score multipliers, shop currency
}

[CreateAssetMenu(fileName = "Resource Config", menuName = "Map Generation/Resource Config")]
public class ResourceConfig : ScriptableObject
{
    public ResourceType type;
    public GameObject prefab;
    public int valueAmount; // Ammo count, health points, etc.

    [Header("Placement Rules")]
    public float rarityWeight = 1f; // Higher = more common
    public RoomClassification[] allowedRoomTypes;
    public int minWaveToAppear = 1;
    public float minDistanceFromSpawns = 5f;

    [Header("Visual")]
    public Sprite icon;
    public Color glowColor;
}
```

### 3.3 Balancing Algorithms

**Resource Budget System:**

```csharp
public class ResourceDistributionManager : MonoBehaviour
{
    [Header("Resource Budgets")]
    [SerializeField] private ResourceBudgetConfig budgetConfig;

    [Header("Placement Strategy")]
    [SerializeField] private PlacementStrategyConfig placementStrategy;

    public List<ResourcePlacement> DistributeResources(
        Dictionary<Room, RoomAssignment> roomAssignments,
        DifficultySettings difficulty)
    {
        var placements = new List<ResourcePlacement>();

        // Calculate total resource budget based on map size and difficulty
        var budget = CalculateResourceBudget(roomAssignments.Count, difficulty);

        // Distribute ammo
        placements.AddRange(DistributeResourceType(
            ResourceType.Ammo,
            budget.ammoBudget,
            roomAssignments,
            difficulty
        ));

        // Distribute health
        placements.AddRange(DistributeResourceType(
            ResourceType.Health,
            budget.healthBudget,
            roomAssignments,
            difficulty
        ));

        // Distribute weapon upgrades
        placements.AddRange(DistributeWeaponUpgrades(
            budget.weaponBudget,
            roomAssignments,
            difficulty
        ));

        return placements;
    }

    private ResourceBudget CalculateResourceBudget(int roomCount, DifficultySettings difficulty)
    {
        return new ResourceBudget
        {
            // Base formula: resources scale with room count but sub-linearly (sqrt)
            ammoBudget = Mathf.RoundToInt(
                budgetConfig.baseAmmoPerRoom * Mathf.Sqrt(roomCount) * difficulty.resourceScarcityMultiplier
            ),
            healthBudget = Mathf.RoundToInt(
                budgetConfig.baseHealthPerRoom * Mathf.Sqrt(roomCount) * difficulty.resourceScarcityMultiplier
            ),
            weaponBudget = Mathf.Max(2, roomCount / 5) // 1 weapon per 5 rooms
        };
    }

    private List<ResourcePlacement> DistributeResourceType(
        ResourceType type,
        int budget,
        Dictionary<Room, RoomAssignment> roomAssignments,
        DifficultySettings difficulty)
    {
        var placements = new List<ResourcePlacement>();
        var resourceConfigs = budgetConfig.GetResourcesOfType(type);

        // Prioritize rooms for placement
        var prioritizedRooms = PrioritizeRoomsForResources(roomAssignments, type);

        int remainingBudget = budget;
        int roomIndex = 0;

        while (remainingBudget > 0 && roomIndex < prioritizedRooms.Count)
        {
            var room = prioritizedRooms[roomIndex];
            var resourceConfig = SelectResourceConfig(resourceConfigs, difficulty.currentWave);

            // Place resource in room
            var placement = PlaceResourceInRoom(room.Key, room.Value, resourceConfig);
            if (placement != null)
            {
                placements.Add(placement);
                remainingBudget -= resourceConfig.valueAmount;
            }

            roomIndex++;

            // Cycle back to start if we haven't exhausted budget
            if (roomIndex >= prioritizedRooms.Count && remainingBudget > 0)
                roomIndex = 0;
        }

        return placements;
    }

    private List<KeyValuePair<Room, RoomAssignment>> PrioritizeRoomsForResources(
        Dictionary<Room, RoomAssignment> roomAssignments,
        ResourceType type)
    {
        // Priority scoring for room selection
        return roomAssignments
            .Select(ra => new
            {
                Room = ra,
                Score = CalculateRoomResourceScore(ra.Key, ra.Value, type)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Room)
            .ToList();
    }

    private float CalculateRoomResourceScore(Room room, RoomAssignment assignment, ResourceType type)
    {
        float score = 0f;

        // Prefer off-path rooms (exploration reward)
        if (!room.IsOnCriticalPath)
            score += 10f;

        // Prefer secret rooms (high reward)
        if (assignment.Classification == RoomClassification.SecretRoom)
            score += 20f;

        // Prefer storage rooms for ammo/health
        if (type == ResourceType.Ammo || type == ResourceType.Health)
        {
            if (assignment.Classification == RoomClassification.StorageRoom)
                score += 15f;
        }

        // Distance from player start (farther = better loot)
        score += room.DistanceFromPlayerSpawn * 0.5f;

        // Room area (larger rooms easier to navigate, safer to place)
        score += Mathf.Sqrt(room.Area) * 0.1f;

        return score;
    }

    private ResourcePlacement PlaceResourceInRoom(
        Room room,
        RoomAssignment assignment,
        ResourceConfig config)
    {
        // Find valid placement position
        Vector2Int position = FindValidResourcePosition(room, config);
        if (position == Vector2Int.zero)
            return null;

        // Instantiate resource prefab
        var instance = Instantiate(
            config.prefab,
            new Vector3(position.x, position.y, 0),
            Quaternion.identity
        );

        return new ResourcePlacement
        {
            resourceConfig = config,
            position = position,
            roomID = room.ID,
            instance = instance
        };
    }

    private Vector2Int FindValidResourcePosition(Room room, ResourceConfig config)
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Random position within room
            Vector2Int candidate = new Vector2Int(
                UnityEngine.Random.Range(room.Bounds.xMin + 1, room.Bounds.xMax - 1),
                UnityEngine.Random.Range(room.Bounds.yMin + 1, room.Bounds.yMax - 1)
            );

            // Validate: not near spawn points
            var nearbySpawns = Physics2D.OverlapCircleAll(
                new Vector2(candidate.x, candidate.y),
                config.minDistanceFromSpawns
            );

            if (nearbySpawns.Any(c => c.CompareTag("Spawn Point")))
                continue;

            // Validate: on walkable floor
            // TODO: Tilemap walkability check

            return candidate;
        }

        Debug.LogWarning($"Failed to place resource in room {room.ID}");
        return Vector2Int.zero;
    }
}

public struct ResourceBudget
{
    public int ammoBudget;
    public int healthBudget;
    public int weaponBudget;
}

public class ResourcePlacement
{
    public ResourceConfig resourceConfig;
    public Vector2Int position;
    public int roomID;
    public GameObject instance;
}
```

### 3.4 Difficulty Scaling

**Resource availability decreases as waves progress:**

```csharp
[CreateAssetMenu(fileName = "Difficulty Settings", menuName = "Map Generation/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Spawn Configuration")]
    [Range(0.5f, 3f)] public float spawnDensityMultiplier = 1f;
    public int minSpawnPoints = 5;
    public int maxSpawnsPerRoom = 10;

    [Header("Resource Scarcity")]
    [Range(0.3f, 2f)] public float resourceScarcityMultiplier = 1f;
    public AnimationCurve scarcityCurveOverWaves;

    [Header("Wave Progression")]
    public int currentWave = 1;
    public float waveScalingFactor = 0.1f;

    public float GetCurrentScarcityMultiplier()
    {
        // Curve evaluation: X = wave number, Y = scarcity (lower = fewer resources)
        return scarcityCurveOverWaves.Evaluate(currentWave) * resourceScarcityMultiplier;
    }
}
```

**Example Curve:**
- Waves 1-3: 1.0x resources (normal)
- Waves 4-8: 0.8x resources (slight scarcity)
- Waves 9-15: 0.6x resources (moderate scarcity)
- Waves 16+: 0.5x resources (harsh survival)

### 3.5 Placement Constraints

**Constraint Solver Pattern:**

```csharp
public interface IPlacementConstraint
{
    bool IsSatisfied(Vector2Int position, Room room, MapGenerationContext context);
    string ConstraintName { get; }
}

public class MinDistanceFromSpawnsConstraint : IPlacementConstraint
{
    private float minDistance;

    public MinDistanceFromSpawnsConstraint(float distance)
    {
        minDistance = distance;
    }

    public bool IsSatisfied(Vector2Int position, Room room, MapGenerationContext context)
    {
        foreach (var spawn in context.SpawnPoints)
        {
            Vector2 spawnPos = new Vector2(spawn.transform.position.x, spawn.transform.position.y);
            if (Vector2.Distance(position, spawnPos) < minDistance)
                return false;
        }
        return true;
    }

    public string ConstraintName => $"MinDistanceFromSpawns({minDistance})";
}

public class WalkableFloorConstraint : IPlacementConstraint
{
    public bool IsSatisfied(Vector2Int position, Room room, MapGenerationContext context)
    {
        // Check if tilemap has floor tile at position
        var tile = context.FloorTilemap.GetTile(new Vector3Int(position.x, position.y, 0));
        return tile != null;
    }

    public string ConstraintName => "WalkableFloor";
}

public class NotBlockingPathConstraint : IPlacementConstraint
{
    public bool IsSatisfied(Vector2Int position, Room room, MapGenerationContext context)
    {
        // Check NavMesh after hypothetical placement
        // This is expensive, use sparingly
        return true; // Simplified
    }

    public string ConstraintName => "NotBlockingPath";
}

// Usage in PlacementSolver
public class PlacementSolver
{
    private List<IPlacementConstraint> constraints = new List<IPlacementConstraint>();

    public void AddConstraint(IPlacementConstraint constraint)
    {
        constraints.Add(constraint);
    }

    public bool IsValidPlacement(Vector2Int position, Room room, MapGenerationContext context)
    {
        foreach (var constraint in constraints)
        {
            if (!constraint.IsSatisfied(position, room, context))
            {
                Debug.Log($"Constraint violated: {constraint.ConstraintName}");
                return false;
            }
        }
        return true;
    }
}
```

### 3.6 Economy Design

**Resource Economy Loop:**

```
Player fights enemies → Consumes ammo/health
        ↓
Player explores rooms → Finds resources
        ↓
Player decides: Safe path (fewer resources) vs Risky path (more resources)
        ↓
Wave difficulty increases → Resources become scarcer
        ↓
Player must optimize resource usage → Tactical gameplay
```

**Balance Targets:**

| Wave Range | Ammo Availability | Health Availability | Weapon Upgrades |
|------------|------------------|---------------------|-----------------|
| 1-3 | Abundant (120%) | Plentiful (100%) | 2-3 common |
| 4-8 | Normal (100%) | Normal (90%) | 1-2 uncommon |
| 9-15 | Scarce (80%) | Scarce (70%) | 1 rare |
| 16+ | Very Scarce (60%) | Very Scarce (50%) | 0-1 legendary |

**Design Insight:**
The economy creates a **difficulty curve** where early waves teach mechanics with abundant resources, mid-game requires resource management, and late-game demands mastery.

---

## 4. Special Rooms Architecture

### 4.1 Room Template System

**Room templates** are the **bridge between procedural generation and hand-crafted design**. They provide:

1. **Art Direction** - Detailed tile layouts
2. **Gameplay Variety** - Different room functions (conference, cubicles, server room)
3. **Narrative Context** - Environmental storytelling through object placement
4. **Design Control** - Predictable spaces for specific encounters

### 4.2 RoomTemplate ScriptableObject (Enhanced)

```csharp
[CreateAssetMenu(fileName = "New Room Template", menuName = "Map Generation/Room Template")]
public class RoomTemplate : ScriptableObject
{
    [Header("Identification")]
    public string templateName;
    public RoomType roomType;

    [Header("Dimensions")]
    public Vector2Int minSize = new Vector2Int(10, 10);
    public Vector2Int maxSize = new Vector2Int(20, 20);
    [Tooltip("Actual template size, must be within min/max")]
    public Vector2Int templateSize = new Vector2Int(15, 15);

    [Header("Tile Data")]
    [Tooltip("Flattened 2D array: floor tiles for each cell")]
    public TileBase[] floorTiles;
    [Tooltip("Flattened 2D array: wall/obstacle tiles")]
    public TileBase[] wallTiles;
    [Tooltip("Flattened 2D array: decorative object tiles")]
    public TileBase[] objectTiles;

    [Header("Connection Points")]
    public List<DoorwayDefinition> doorways = new List<DoorwayDefinition>();

    [Header("Furniture Placement")]
    public List<FurnitureSpawnPoint> furniturePoints = new List<FurnitureSpawnPoint>();

    [Header("Metadata")]
    public RoomClassification suggestedClassification;
    [Range(0f, 1f)] public float selectionWeight = 1f;
    public bool allowRotation = false;
    public bool allowMirroring = false;

    [Header("Validation")]
    public bool validateNavMeshCoverage = true;
    [Range(0.5f, 1f)] public float minNavMeshCoveragePercent = 0.8f;

    /// <summary>
    /// Gets the tile at local position (x, y) from the flattened array
    /// </summary>
    public TileBase GetFloorTile(int x, int y)
    {
        int index = y * templateSize.x + x;
        if (index >= 0 && index < floorTiles.Length)
            return floorTiles[index];
        return null;
    }

    public TileBase GetWallTile(int x, int y)
    {
        int index = y * templateSize.x + x;
        if (index >= 0 && index < wallTiles.Length)
            return wallTiles[index];
        return null;
    }

    public TileBase GetObjectTile(int x, int y)
    {
        int index = y * templateSize.x + x;
        if (index >= 0 && index < objectTiles.Length)
            return objectTiles[index];
        return null;
    }

    /// <summary>
    /// Validates template data integrity
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        // Check array sizes
        int expectedSize = templateSize.x * templateSize.y;
        if (floorTiles.Length != expectedSize)
            result.AddError($"Floor tiles array size mismatch: expected {expectedSize}, got {floorTiles.Length}");
        if (wallTiles.Length != expectedSize)
            result.AddError($"Wall tiles array size mismatch: expected {expectedSize}, got {wallTiles.Length}");
        if (objectTiles.Length != expectedSize)
            result.AddError($"Object tiles array size mismatch: expected {expectedSize}, got {objectTiles.Length}");

        // Check doorways
        if (doorways.Count == 0)
            result.AddWarning("No doorways defined, room may be inaccessible");

        foreach (var doorway in doorways)
        {
            if (doorway.localPosition.x < 0 || doorway.localPosition.x >= templateSize.x ||
                doorway.localPosition.y < 0 || doorway.localPosition.y >= templateSize.y)
            {
                result.AddError($"Doorway {doorway.localPosition} outside template bounds");
            }
        }

        // Check furniture
        foreach (var furniture in furniturePoints)
        {
            if (furniture.localPosition.x < 0 || furniture.localPosition.x >= templateSize.x ||
                furniture.localPosition.y < 0 || furniture.localPosition.y >= templateSize.y)
            {
                result.AddWarning($"Furniture at {furniture.localPosition} outside template bounds");
            }
        }

        return result;
    }
}

public enum RoomType
{
    Generic,
    Cubicles,
    ConferenceRoom,
    BreakRoom,
    ServerRoom,
    ExecutiveOffice,
    OpenPlan,
    Reception,
    Storage,
    Restroom
}

[System.Serializable]
public class DoorwayDefinition
{
    [Tooltip("Local position within template (0,0) = bottom-left")]
    public Vector2Int localPosition;

    [Tooltip("Direction doorway faces")]
    public DoorwayDirection direction;

    [Tooltip("Width of doorway (usually 1-3 tiles)")]
    public int width = 1;

    [Tooltip("Priority for corridor connection (higher = preferred)")]
    [Range(0, 10)] public int connectionPriority = 5;
}

public enum DoorwayDirection
{
    North,
    South,
    East,
    West
}

[System.Serializable]
public class FurnitureSpawnPoint
{
    public Vector2Int localPosition;
    public GameObject prefab;
    public float rotationDegrees = 0f;
    public bool randomizeRotation = false;

    [Tooltip("Spawn probability (0-1). Allows random furniture variation.")]
    [Range(0f, 1f)] public float spawnProbability = 1f;
}
```

### 4.3 Template Instantiation

**TemplateInstantiator** handles the complex task of mapping template data to world space:

```csharp
public class TemplateInstantiator
{
    private Tilemap floorTilemap;
    private Tilemap wallTilemap;
    private Tilemap objectTilemap;

    public TemplateInstantiator(Tilemap floor, Tilemap wall, Tilemap objects)
    {
        floorTilemap = floor;
        wallTilemap = wall;
        objectTilemap = objects;
    }

    /// <summary>
    /// Instantiates a room template at the specified room location
    /// </summary>
    public RoomInstance InstantiateTemplate(
        RoomTemplate template,
        Room room,
        Vector2Int anchorPoint,
        TemplateTransform transform = null)
    {
        transform = transform ?? TemplateTransform.Identity;

        var instance = new RoomInstance
        {
            template = template,
            room = room,
            worldAnchor = anchorPoint,
            transform = transform,
            furnitureInstances = new List<GameObject>(),
            doorwayWorldPositions = new List<Vector2Int>()
        };

        // Paint tiles
        PaintTemplateTiles(template, anchorPoint, transform);

        // Instantiate furniture
        InstantiateFurniture(template, anchorPoint, transform, instance);

        // Map doorways to world positions
        MapDoorways(template, anchorPoint, transform, instance);

        return instance;
    }

    private void PaintTemplateTiles(
        RoomTemplate template,
        Vector2Int anchor,
        TemplateTransform transform)
    {
        for (int y = 0; y < template.templateSize.y; y++)
        {
            for (int x = 0; x < template.templateSize.x; x++)
            {
                // Apply transformations (rotation, mirroring)
                Vector2Int localPos = new Vector2Int(x, y);
                Vector2Int transformedLocal = transform.Apply(localPos, template.templateSize);
                Vector2Int worldPos = anchor + transformedLocal;

                // Paint tiles
                var floorTile = template.GetFloorTile(x, y);
                if (floorTile != null)
                    floorTilemap.SetTile(new Vector3Int(worldPos.x, worldPos.y, 0), floorTile);

                var wallTile = template.GetWallTile(x, y);
                if (wallTile != null)
                    wallTilemap.SetTile(new Vector3Int(worldPos.x, worldPos.y, 0), wallTile);

                var objectTile = template.GetObjectTile(x, y);
                if (objectTile != null)
                    objectTilemap.SetTile(new Vector3Int(worldPos.x, worldPos.y, 0), objectTile);
            }
        }
    }

    private void InstantiateFurniture(
        RoomTemplate template,
        Vector2Int anchor,
        TemplateTransform transform,
        RoomInstance instance)
    {
        foreach (var furniturePoint in template.furniturePoints)
        {
            // Probability check
            if (UnityEngine.Random.value > furniturePoint.spawnProbability)
                continue;

            // Transform local position to world position
            Vector2Int localPos = furniturePoint.localPosition;
            Vector2Int transformedLocal = transform.Apply(localPos, template.templateSize);
            Vector2Int worldPos = anchor + transformedLocal;

            // Calculate rotation
            float rotation = furniturePoint.rotationDegrees;
            if (furniturePoint.randomizeRotation)
                rotation = UnityEngine.Random.Range(0f, 360f);
            rotation += transform.rotationDegrees; // Apply template rotation

            // Instantiate
            var furniture = GameObject.Instantiate(
                furniturePoint.prefab,
                new Vector3(worldPos.x + 0.5f, worldPos.y + 0.5f, 0), // Center of tile
                Quaternion.Euler(0, 0, rotation)
            );

            instance.furnitureInstances.Add(furniture);
        }
    }

    private void MapDoorways(
        RoomTemplate template,
        Vector2Int anchor,
        TemplateTransform transform,
        RoomInstance instance)
    {
        foreach (var doorway in template.doorways)
        {
            Vector2Int transformedLocal = transform.Apply(doorway.localPosition, template.templateSize);
            Vector2Int worldPos = anchor + transformedLocal;
            instance.doorwayWorldPositions.Add(worldPos);
        }
    }
}

/// <summary>
/// Represents transformations applied to a template during instantiation
/// </summary>
public class TemplateTransform
{
    public int rotationDegrees = 0; // 0, 90, 180, 270
    public bool mirrorHorizontal = false;
    public bool mirrorVertical = false;

    public static TemplateTransform Identity => new TemplateTransform();

    /// <summary>
    /// Applies transformation to a local position
    /// </summary>
    public Vector2Int Apply(Vector2Int localPos, Vector2Int templateSize)
    {
        Vector2Int result = localPos;

        // Apply mirroring first
        if (mirrorHorizontal)
            result.x = templateSize.x - 1 - result.x;
        if (mirrorVertical)
            result.y = templateSize.y - 1 - result.y;

        // Apply rotation
        switch (rotationDegrees)
        {
            case 90:
                result = new Vector2Int(templateSize.y - 1 - result.y, result.x);
                break;
            case 180:
                result = new Vector2Int(templateSize.x - 1 - result.x, templateSize.y - 1 - result.y);
                break;
            case 270:
                result = new Vector2Int(result.y, templateSize.x - 1 - result.x);
                break;
        }

        return result;
    }
}

/// <summary>
/// Runtime data for an instantiated room template
/// </summary>
public class RoomInstance
{
    public RoomTemplate template;
    public Room room;
    public Vector2Int worldAnchor;
    public TemplateTransform transform;
    public List<GameObject> furnitureInstances;
    public List<Vector2Int> doorwayWorldPositions;
}
```

### 4.4 Connection Point Handling

**Aligning template doorways with BSP corridors:**

```csharp
public class DoorwayConnectionSolver
{
    /// <summary>
    /// Finds the best doorway pair between two room instances for corridor connection
    /// </summary>
    public (Vector2Int doorwayA, Vector2Int doorwayB) FindBestDoorwayPair(
        RoomInstance roomA,
        RoomInstance roomB)
    {
        float minDistance = float.MaxValue;
        Vector2Int bestDoorwayA = Vector2Int.zero;
        Vector2Int bestDoorwayB = Vector2Int.zero;

        foreach (var doorwayA in roomA.doorwayWorldPositions)
        {
            foreach (var doorwayB in roomB.doorwayWorldPositions)
            {
                float distance = Vector2Int.Distance(doorwayA, doorwayB);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestDoorwayA = doorwayA;
                    bestDoorwayB = doorwayB;
                }
            }
        }

        return (bestDoorwayA, bestDoorwayB);
    }

    /// <summary>
    /// Creates a corridor between two doorways, respecting their orientations
    /// </summary>
    public List<Vector2Int> CreateCorridor(
        Vector2Int doorwayA,
        DoorwayDirection directionA,
        Vector2Int doorwayB,
        DoorwayDirection directionB,
        int corridorWidth = 3)
    {
        // L-shaped corridor algorithm
        List<Vector2Int> corridorTiles = new List<Vector2Int>();

        // Extend from doorway A in its facing direction
        Vector2Int extensionA = GetDirectionVector(directionA) * 3;
        Vector2Int junctionA = doorwayA + extensionA;

        // Extend from doorway B in its facing direction
        Vector2Int extensionB = GetDirectionVector(directionB) * 3;
        Vector2Int junctionB = doorwayB + extensionB;

        // Create three segments: A→Junction, Junction→Junction, Junction→B
        corridorTiles.AddRange(CreateStraightCorridor(doorwayA, junctionA, corridorWidth));
        corridorTiles.AddRange(CreateStraightCorridor(junctionA, junctionB, corridorWidth));
        corridorTiles.AddRange(CreateStraightCorridor(junctionB, doorwayB, corridorWidth));

        return corridorTiles;
    }

    private List<Vector2Int> CreateStraightCorridor(Vector2Int start, Vector2Int end, int width)
    {
        var tiles = new List<Vector2Int>();

        // Bresenham's line algorithm for main path
        var mainPath = BresenhamLine(start, end);

        // Widen corridor by adding perpendicular tiles
        foreach (var tile in mainPath)
        {
            tiles.Add(tile);

            // Add width
            for (int w = 1; w < width; w++)
            {
                // Determine perpendicular direction
                Vector2Int perp = GetPerpendicularDirection(end - start);
                tiles.Add(tile + perp * w);
                tiles.Add(tile - perp * w);
            }
        }

        return tiles;
    }

    private Vector2Int GetDirectionVector(DoorwayDirection dir)
    {
        switch (dir)
        {
            case DoorwayDirection.North: return Vector2Int.up;
            case DoorwayDirection.South: return Vector2Int.down;
            case DoorwayDirection.East: return Vector2Int.right;
            case DoorwayDirection.West: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }

    private Vector2Int GetPerpendicularDirection(Vector2Int direction)
    {
        // Perpendicular to (x, y) is (-y, x) or (y, -x)
        return new Vector2Int(-direction.y, direction.x);
    }

    private List<Vector2Int> BresenhamLine(Vector2Int start, Vector2Int end)
    {
        var points = new List<Vector2Int>();

        int x = start.x, y = start.y;
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Vector2Int(x, y));

            if (x == end.x && y == end.y)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return points;
    }
}
```

### 4.5 Furniture Placement

**Furniture adds tactical depth:**

- **Cover Mechanics** - Desks provide line-of-sight blocking
- **Pathfinding Obstacles** - Force player tactical positioning
- **Visual Variety** - Different room types have distinct looks
- **Performance** - Object pooling for reusable furniture prefabs

```csharp
public class FurniturePlacer
{
    private Dictionary<GameObject, Queue<GameObject>> furniturePools = new Dictionary<GameObject, Queue<GameObject>>();

    /// <summary>
    /// Retrieves furniture from pool or instantiates new
    /// </summary>
    public GameObject GetFurniture(GameObject prefab)
    {
        if (!furniturePools.ContainsKey(prefab))
        {
            furniturePools[prefab] = new Queue<GameObject>();
        }

        if (furniturePools[prefab].Count > 0)
        {
            var pooled = furniturePools[prefab].Dequeue();
            pooled.SetActive(true);
            return pooled;
        }
        else
        {
            return GameObject.Instantiate(prefab);
        }
    }

    /// <summary>
    /// Returns furniture to pool for reuse
    /// </summary>
    public void ReturnFurniture(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        if (furniturePools.ContainsKey(prefab))
        {
            furniturePools[prefab].Enqueue(instance);
        }
    }

    /// <summary>
    /// Clears all furniture pools (e.g., on new map generation)
    /// </summary>
    public void ClearPools()
    {
        foreach (var pool in furniturePools.Values)
        {
            foreach (var obj in pool)
            {
                GameObject.Destroy(obj);
            }
            pool.Clear();
        }
        furniturePools.Clear();
    }
}
```

---

## 5. Biome System Design

### 5.1 Biome Configuration

**Biomes** provide **visual and thematic variety** without changing gameplay mechanics. They're purely cosmetic transformations.

```csharp
[CreateAssetMenu(fileName = "New Biome", menuName = "Map Generation/Biome Config")]
public class BiomeConfig : ScriptableObject
{
    [Header("Identification")]
    public string biomeName;
    public Sprite icon;

    [Header("Tilesets")]
    public TilesetPalette floorTileset;
    public TilesetPalette wallTileset;
    public TilesetPalette objectTileset;

    [Header("Lighting")]
    public Color ambientLightColor = Color.white;
    [Range(0f, 1f)] public float ambientLightIntensity = 1f;
    public Color fogColor = new Color(0, 0, 0, 0);

    [Header("Particle Effects")]
    public GameObject ambientParticlePrefab; // Dust, sparks, etc.
    [Range(0, 20)] public int particleEmittersCount = 5;

    [Header("Audio")]
    public AudioClip ambientSound;
    [Range(0f, 1f)] public float ambientVolume = 0.3f;

    [Header("Furniture Variants")]
    public List<FurniturePaletteSwap> furnitureSwaps = new List<FurniturePaletteSwap>();
}

[System.Serializable]
public class FurniturePaletteSwap
{
    public GameObject originalPrefab;
    public GameObject variantPrefab;
}
```

### 5.2 Tileset Management

**TilesetPalette** maps generic tile types to biome-specific tiles:

```csharp
[CreateAssetMenu(fileName = "New Tileset Palette", menuName = "Map Generation/Tileset Palette")]
public class TilesetPalette : ScriptableObject
{
    public List<TileMapping> mappings = new List<TileMapping>();

    /// <summary>
    /// Swaps a generic tile for a biome-specific variant
    /// </summary>
    public TileBase GetVariant(TileBase genericTile)
    {
        var mapping = mappings.Find(m => m.genericTile == genericTile);

        if (mapping != null && mapping.variants.Count > 0)
        {
            // Random selection from variants for visual variety
            int index = UnityEngine.Random.Range(0, mapping.variants.Count);
            return mapping.variants[index];
        }

        // Fallback to original
        return genericTile;
    }
}

[System.Serializable]
public class TileMapping
{
    [Tooltip("Generic tile type (e.g., 'FloorGeneric')")]
    public TileBase genericTile;

    [Tooltip("Biome-specific variants (randomly selected)")]
    public List<TileBase> variants = new List<TileBase>();
}
```

### 5.3 Theme Propagation

**BiomeManager** applies theming post-generation:

```csharp
public class BiomeManager : MonoBehaviour
{
    public void ApplyBiome(
        BiomeConfig biome,
        Tilemap floorTilemap,
        Tilemap wallTilemap,
        Tilemap objectTilemap,
        List<RoomInstance> roomInstances)
    {
        // Swap tiles
        SwapTileset(floorTilemap, biome.floorTileset);
        SwapTileset(wallTilemap, biome.wallTileset);
        SwapTileset(objectTilemap, biome.objectTileset);

        // Apply lighting
        ApplyLighting(biome);

        // Spawn ambient particles
        SpawnAmbientParticles(biome, floorTilemap);

        // Setup ambient audio
        SetupAmbientAudio(biome);

        // Swap furniture variants
        SwapFurnitureVariants(biome, roomInstances);
    }

    private void SwapTileset(Tilemap tilemap, TilesetPalette palette)
    {
        BoundsInt bounds = tilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase currentTile = tilemap.GetTile(pos);
            if (currentTile != null)
            {
                TileBase variantTile = palette.GetVariant(currentTile);
                tilemap.SetTile(pos, variantTile);
            }
        }
    }

    private void ApplyLighting(BiomeConfig biome)
    {
        // Adjust Unity's 2D Global Light
        var globalLight = GameObject.FindObjectOfType<UnityEngine.Rendering.Universal.Light2D>();
        if (globalLight != null)
        {
            globalLight.color = biome.ambientLightColor;
            globalLight.intensity = biome.ambientLightIntensity;
        }

        // Optionally: Apply fog/post-processing
        RenderSettings.fogColor = biome.fogColor;
    }

    private void SpawnAmbientParticles(BiomeConfig biome, Tilemap tilemap)
    {
        if (biome.ambientParticlePrefab == null)
            return;

        BoundsInt bounds = tilemap.cellBounds;

        for (int i = 0; i < biome.particleEmittersCount; i++)
        {
            // Random position within map bounds
            Vector3Int randomPos = new Vector3Int(
                UnityEngine.Random.Range(bounds.xMin, bounds.xMax),
                UnityEngine.Random.Range(bounds.yMin, bounds.yMax),
                0
            );

            if (tilemap.GetTile(randomPos) != null)
            {
                GameObject.Instantiate(
                    biome.ambientParticlePrefab,
                    tilemap.CellToWorld(randomPos),
                    Quaternion.identity
                );
            }
        }
    }

    private void SetupAmbientAudio(BiomeConfig biome)
    {
        if (biome.ambientSound == null)
            return;

        // Find or create ambient audio source
        var audioSource = GameObject.Find("AmbientAudio")?.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            var audioObj = new GameObject("AmbientAudio");
            audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        audioSource.clip = biome.ambientSound;
        audioSource.volume = biome.ambientVolume;
        audioSource.Play();
    }

    private void SwapFurnitureVariants(BiomeConfig biome, List<RoomInstance> roomInstances)
    {
        foreach (var room in roomInstances)
        {
            foreach (var furniture in room.furnitureInstances)
            {
                foreach (var swap in biome.furnitureSwaps)
                {
                    // Check if furniture prefab matches original
                    if (furniture.name.StartsWith(swap.originalPrefab.name))
                    {
                        // Swap to variant
                        Vector3 pos = furniture.transform.position;
                        Quaternion rot = furniture.transform.rotation;

                        GameObject.Destroy(furniture);
                        GameObject.Instantiate(swap.variantPrefab, pos, rot);
                        break;
                    }
                }
            }
        }
    }
}
```

### 5.4 Visual Variation Examples

**Biome Examples:**

| Biome | Floor Tiles | Wall Tiles | Lighting | Particles | Audio |
|-------|------------|------------|----------|-----------|-------|
| **Corporate Office** | Beige carpet | Gray walls | Bright fluorescent | None | Office ambiance |
| **Tech Startup** | Concrete | Exposed brick | Blue-tinted | None | Electronic hum |
| **Abandoned Office** | Dirty tiles | Cracked walls | Dim yellow | Dust motes | Creaking sounds |
| **Government Facility** | Linoleum | Painted drywall | Cold white | None | HVAC hum |
| **Cyberpunk Office** | Neon grid | Chrome panels | Purple/pink | Holographic glitches | Synthwave ambient |

---

## 6. Gameplay Integration

### 6.1 How Content Affects Gameplay

**Spawn Points:**
- **Density** determines combat intensity (sparse = tactical, dense = chaotic)
- **Positioning** creates ambush vs. open combat scenarios
- **Wave tables** control enemy variety and challenge escalation

**Resources:**
- **Scarcity** forces exploration vs. safe camping
- **Placement** rewards risk-taking in dangerous areas
- **Distribution** prevents resource starvation while maintaining tension

**Special Rooms:**
- **Boss rooms** provide climactic encounters
- **Storage rooms** offer safe resource gathering
- **Secret rooms** reward exploration
- **Arena rooms** enable high-octane combat

**Biomes:**
- **Variety** prevents visual monotony
- **Theming** supports narrative (corporate→startup→abandoned progression)
- **Cosmetic only** ensures balanced gameplay

### 6.2 Balance Considerations

**Design Pillars:**

1. **Player Empowerment Through Choices**
   - Safe path (fewer resources) vs. risky path (more resources)
   - Exploration (secrets) vs. efficiency (direct route)
   - Resource conservation vs. aggressive play

2. **Escalating Challenge**
   - Waves increase spawn count and enemy stats
   - Resources become scarcer over time
   - Weapon upgrades provide temporary power spikes

3. **Fair But Challenging**
   - Never impossible (always a recovery path)
   - Skill ceiling through resource management
   - Predictable patterns (spawn tables) with random variation

### 6.3 Difficulty Curves

**Wave Progression Model:**

```
Difficulty = BaseEnemyStrength * WaveMultiplier * SpawnDensity / ResourceAvailability
```

**Example Curve:**

| Wave | Enemy Strength | Spawn Density | Resources | Net Difficulty |
|------|---------------|---------------|-----------|----------------|
| 1 | 1.0x | 1.0x | 1.2x | Easy |
| 5 | 1.5x | 1.2x | 1.0x | Moderate |
| 10 | 2.0x | 1.5x | 0.8x | Hard |
| 20 | 3.0x | 2.0x | 0.5x | Very Hard |

### 6.4 Progression Systems

**Player Progression Loop:**

```
Start Wave → Fight Enemies → Collect Resources → Upgrade Weapons
     ↓
Explore Map → Find Secrets → Risk vs. Reward → Resource Management
     ↓
Complete Wave → Difficulty Increases → Repeat with Higher Stakes
```

**Mastery Incentives:**

- **Score System** - Rewards efficient play (low damage taken, high accuracy)
- **Time Bonuses** - Faster wave completion = better score
- **Exploration Bonuses** - Finding all secret rooms
- **Resource Efficiency** - Beating waves with minimal ammo usage

---

## 7. Extensibility Architecture

### 7.1 Adding New Spawn Types

**Plugin Pattern:**

```csharp
// 1. Create new Enemy prefab (already supported by existing Enemy.cs)
// 2. Create SpawnEntry in SpawnTable asset
// 3. Configure probability curve

// No code changes required!
```

**Example: Adding "Explosive Mouse"**

1. Create enemy prefab with Enemy component
2. Open SpawnTable asset in Inspector
3. Add new SpawnEntry:
   - `enemyPrefab`: ExplosiveMouse prefab
   - `probabilityCurve`: Keyframes (Wave 7: 0%, Wave 10: 50%, Wave 15+: 100%)
   - `minWave`: 7

**Result:** Explosive Mouse automatically spawns starting wave 7, increasing probability by wave 15.

### 7.2 Creating New Room Types

**Steps:**

1. **Create RoomTemplate asset**
   ```
   Right-click → Create → Map Generation → Room Template
   ```

2. **Configure tiles**
   - Set `templateSize` (e.g., 15x12)
   - Populate `floorTiles`, `wallTiles`, `objectTiles` arrays
   - Define `doorways` with positions and directions

3. **Add furniture points**
   - Assign prefabs
   - Set positions and rotations
   - Configure spawn probabilities

4. **Register in RoomTemplateLibrary**
   ```csharp
   [CreateAssetMenu(fileName = "Room Template Library", menuName = "Map Generation/Room Template Library")]
   public class RoomTemplateLibrary : ScriptableObject
   {
       public List<RoomTemplate> templates = new List<RoomTemplate>();

       public RoomTemplate GetRandomTemplate(RoomClassification roomType)
       {
           var candidates = templates.Where(t => t.suggestedClassification == roomType).ToList();
           if (candidates.Count == 0)
               return templates[0]; // Fallback

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

### 7.3 New Biome Support

**Steps:**

1. **Create BiomeConfig asset**
   ```
   Right-click → Create → Map Generation → Biome Config
   ```

2. **Create TilesetPalette assets** for floor, wall, object tiles
   - Map generic tiles to biome-specific variants

3. **Configure lighting, particles, audio**

4. **Register in BiomeRegistry**
   ```csharp
   [CreateAssetMenu(fileName = "Biome Registry", menuName = "Map Generation/Biome Registry")]
   public class BiomeRegistry : ScriptableObject
   {
       public List<BiomeConfig> biomes = new List<BiomeConfig>();

       public BiomeConfig GetBiomeForWave(int wave)
       {
           // Example: Different biome every 10 waves
           int biomeIndex = (wave / 10) % biomes.Count;
           return biomes[biomeIndex];
       }

       public BiomeConfig GetRandomBiome()
       {
           return biomes[UnityEngine.Random.Range(0, biomes.Count)];
       }
   }
   ```

### 7.4 Mod Support Considerations

**Architecture designed for future mod support:**

1. **ScriptableObject-based content** - Easy to serialize and share
2. **Asset bundle compatibility** - Templates can be packaged
3. **No hardcoded references** - All content loaded via registries
4. **Validation systems** - ModValidator can check template integrity

**Future Mod API:**

```csharp
public interface IModContent
{
    string ModID { get; }
    string ModVersion { get; }
    void Register(ModRegistry registry);
}

public class ModRegistry
{
    public void RegisterEnemy(Enemy enemyPrefab, SpawnTable spawnTable);
    public void RegisterRoomTemplate(RoomTemplate template);
    public void RegisterBiome(BiomeConfig biome);
}
```

---

## 8. Data-Driven Design

### 8.1 ScriptableObject Usage

**All Phase 2 content is ScriptableObject-based:**

| Content Type | ScriptableObject | Purpose |
|--------------|------------------|---------|
| Spawn Tables | `SpawnTable` | Enemy spawn configurations |
| Room Templates | `RoomTemplate` | Room layouts and furniture |
| Biomes | `BiomeConfig` | Visual themes |
| Resources | `ResourceConfig` | Ammo, health, weapons |
| Difficulty | `DifficultySettings` | Wave scaling parameters |

**Benefits:**

- **No code recompilation** - Content changes hot-reload in editor
- **Designer-friendly** - Visual editing in Unity Inspector
- **Version control friendly** - Assets stored as YAML
- **Runtime efficient** - Loaded once, referenced by ID
- **Modular** - Easy to add/remove content

### 8.2 Configuration Management

**Configuration Hierarchy:**

```
GameConfig (root)
├── DifficultySettings
├── BiomeRegistry
├── SpawnTableRegistry
├── RoomTemplateLibrary
└── ResourceBudgetConfig
```

**Centralized Config Access:**

```csharp
[CreateAssetMenu(fileName = "Game Config", menuName = "Map Generation/Game Config")]
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

    [Header("Registries")]
    public BiomeRegistry biomeRegistry;
    public SpawnTableRegistry spawnTableRegistry;
    public RoomTemplateLibrary roomTemplateLibrary;
    public ResourceBudgetConfig resourceBudgetConfig;

    [Header("Settings")]
    public DifficultySettings defaultDifficulty;
}

// Usage:
var biome = GameConfig.Instance.biomeRegistry.GetBiomeForWave(currentWave);
```

### 8.3 Designer-Friendly Interfaces

**Custom Property Drawers:**

```csharp
[CustomPropertyDrawer(typeof(SpawnEntry))]
public class SpawnEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, 18), property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Enemy prefab
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + 20, position.width, 18),
                property.FindPropertyRelative("enemyPrefab"),
                new GUIContent("Enemy Prefab")
            );

            // Probability curve with preview
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + 42, position.width, 60),
                property.FindPropertyRelative("probabilityCurve"),
                new GUIContent("Spawn Probability (X: Wave, Y: Chance)")
            );

            // Min/Max wave
            EditorGUI.PropertyField(
                new Rect(position.x, position.y + 104, position.width / 2 - 5, 18),
                property.FindPropertyRelative("minWave"),
                new GUIContent("Min Wave")
            );
            EditorGUI.PropertyField(
                new Rect(position.x + position.width / 2, position.y + 104, position.width / 2, 18),
                property.FindPropertyRelative("maxWave"),
                new GUIContent("Max Wave")
            );

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return property.isExpanded ? 126 : 18;
    }
}
```

**Custom Inspector for RoomTemplate:**

```csharp
[CustomEditor(typeof(RoomTemplate))]
public class RoomTemplateEditor : Editor
{
    private SerializedProperty templateSize;
    private SerializedProperty floorTiles;
    private SerializedProperty doorways;

    private void OnEnable()
    {
        templateSize = serializedObject.FindProperty("templateSize");
        floorTiles = serializedObject.FindProperty("floorTiles");
        doorways = serializedObject.FindProperty("doorways");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

        // Auto-resize arrays button
        if (GUILayout.Button("Resize Tile Arrays"))
        {
            var template = (RoomTemplate)target;
            int size = template.templateSize.x * template.templateSize.y;

            template.floorTiles = new TileBase[size];
            template.wallTiles = new TileBase[size];
            template.objectTiles = new TileBase[size];

            EditorUtility.SetDirty(template);
        }

        // Validate button
        if (GUILayout.Button("Validate Template"))
        {
            var template = (RoomTemplate)target;
            var result = template.Validate();

            if (result.IsValid)
            {
                EditorUtility.DisplayDialog("Validation Success", "Template is valid!", "OK");
            }
            else
            {
                string errors = string.Join("\n", result.Errors);
                string warnings = string.Join("\n", result.Warnings);
                EditorUtility.DisplayDialog("Validation Failed",
                    $"Errors:\n{errors}\n\nWarnings:\n{warnings}", "OK");
            }
        }

        // Visualize doorways in Scene view
        EditorGUILayout.HelpBox("Doorways visualized in Scene view with Gizmos", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    // Gizmo visualization
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmosForRoomTemplate(RoomTemplate template, GizmoType gizmoType)
    {
        if (template.doorways == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (var doorway in template.doorways)
        {
            Vector3 worldPos = new Vector3(doorway.localPosition.x, doorway.localPosition.y, 0);
            Gizmos.DrawWireSphere(worldPos, 0.5f);

            // Draw direction arrow
            Vector3 direction = GetDoorwayDirectionVector(doorway.direction);
            Gizmos.DrawLine(worldPos, worldPos + direction);
        }
    }

    static Vector3 GetDoorwayDirectionVector(DoorwayDirection dir)
    {
        switch (dir)
        {
            case DoorwayDirection.North: return Vector3.up;
            case DoorwayDirection.South: return Vector3.down;
            case DoorwayDirection.East: return Vector3.right;
            case DoorwayDirection.West: return Vector3.left;
            default: return Vector3.zero;
        }
    }
}
```

### 8.4 Runtime vs Edit-Time Data

**Separation of Concerns:**

| Data Type | Storage | Mutability | Usage |
|-----------|---------|------------|-------|
| **Template Definitions** | ScriptableObject assets | Immutable at runtime | Configuration |
| **Spawn Tables** | ScriptableObject assets | Immutable at runtime | Configuration |
| **Biome Configs** | ScriptableObject assets | Immutable at runtime | Configuration |
| **Room Instances** | Runtime classes | Mutable | Gameplay state |
| **Spawn Points** | GameObjects | Mutable | Gameplay state |
| **Resource Placements** | Runtime classes | Mutable | Gameplay state |

**Design Principle:** Configuration is immutable, state is mutable.

---

## 9. Performance Considerations

### 9.1 Object Pooling

**Already integrated with existing ObjectPooler:**

```csharp
// Existing pooling for enemies
public partial class WaveSpawner
{
    void SpawnEnemy(Wave wave)
    {
        var enemy = ObjectPooler.SharedInstance.GetPooledObject<Enemy>(wave.ObjectTag);
        // ...
    }
}

// NEW: Pooling for resources
public class ResourcePool
{
    private Dictionary<ResourceConfig, Queue<GameObject>> pools = new Dictionary<ResourceConfig, Queue<GameObject>>();

    public GameObject Get(ResourceConfig config)
    {
        if (!pools.ContainsKey(config))
            pools[config] = new Queue<GameObject>();

        if (pools[config].Count > 0)
        {
            var obj = pools[config].Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return GameObject.Instantiate(config.prefab);
    }

    public void Return(ResourceConfig config, GameObject obj)
    {
        obj.SetActive(false);
        pools[config].Enqueue(obj);
    }
}
```

### 9.2 Instantiation Strategies

**Lazy Instantiation:**

```csharp
// Don't instantiate all furniture upfront, only when room is near player
public class LazyRoomInstantiator
{
    private Dictionary<int, bool> instantiatedRooms = new Dictionary<int, bool>();
    private float instantiationRadius = 30f; // Tiles

    public void Update(Vector2 playerPosition, List<RoomInstance> allRooms)
    {
        foreach (var room in allRooms)
        {
            if (instantiatedRooms.ContainsKey(room.room.ID))
                continue; // Already instantiated

            float distance = Vector2.Distance(playerPosition, room.room.Center);
            if (distance < instantiationRadius)
            {
                InstantiateFurniture(room);
                instantiatedRooms[room.room.ID] = true;
            }
        }
    }

    private void InstantiateFurniture(RoomInstance room)
    {
        // Instantiate furniture for this room
        // (Moved from ContentGenerationOrchestrator)
    }
}
```

**Chunked Generation:**

```csharp
// Generate content in chunks to avoid frame spikes
public IEnumerator GenerateContentChunked(List<Room> rooms)
{
    const int roomsPerChunk = 5;

    for (int i = 0; i < rooms.Count; i += roomsPerChunk)
    {
        int end = Mathf.Min(i + roomsPerChunk, rooms.Count);

        for (int j = i; j < end; j++)
        {
            InstantiateRoom(rooms[j]);
        }

        yield return null; // Spread across multiple frames
    }
}
```

### 9.3 Memory Management

**Tilemap Memory:**

- **Use single tilemaps** - Don't create tilemap per room, use shared tilemaps
- **Bounded tile arrays** - Clear old tiles when regenerating map
- **Texture atlasing** - Combine biome tiles into atlases

```csharp
public void ClearMap()
{
    // Clear all tilemaps
    floorTilemap.ClearAllTiles();
    wallTilemap.ClearAllTiles();
    objectTilemap.ClearAllTiles();

    // Return all pooled objects
    ResourcePool.Instance.ReturnAll();
    FurniturePlacer.Instance.ClearPools();

    // Destroy spawn points
    foreach (var spawn in GameObject.FindGameObjectsWithTag("Spawn Point"))
        GameObject.Destroy(spawn);

    // Force garbage collection (use sparingly)
    System.GC.Collect();
}
```

### 9.4 Loading Times

**Target: <3 seconds for 100x100 map**

**Optimization Checklist:**

- [x] Use BoxFill for rectangular tile regions (not SetTile loops)
- [x] Batch furniture instantiation (InstantiatePrefabs array API)
- [x] Yield between major generation steps
- [x] Object pooling for reusable content
- [x] NavMesh baking in background thread (if possible)
- [x] Display loading bar for user feedback

**Loading Screen Integration:**

```csharp
public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;
    public Text statusText;

    public IEnumerator ShowGenerationProgress(ContentGenerationOrchestrator orchestrator)
    {
        gameObject.SetActive(true);

        yield return UpdateProgress(0.1f, "Classifying rooms...");
        yield return new WaitForSeconds(0.5f);

        yield return UpdateProgress(0.3f, "Assigning templates...");
        yield return new WaitForSeconds(0.5f);

        yield return UpdateProgress(0.5f, "Placing furniture...");
        yield return new WaitForSeconds(0.5f);

        yield return UpdateProgress(0.7f, "Distributing resources...");
        yield return new WaitForSeconds(0.5f);

        yield return UpdateProgress(0.9f, "Applying biome theme...");
        yield return new WaitForSeconds(0.5f);

        yield return UpdateProgress(1f, "Finalizing...");
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);
    }

    private IEnumerator UpdateProgress(float progress, string status)
    {
        progressBar.value = progress;
        statusText.text = status;
        yield return null;
    }
}
```

---

## 10. Integration with Phase 1

### 10.1 How Content Layers on Generation

**Phase 1 Output → Phase 2 Input:**

```csharp
// Phase 1: BSP Generation
public class BSPMapGenerator
{
    public MapGenerationContext Generate(int seed, Vector2Int mapSize)
    {
        // ... BSP algorithm ...

        return new MapGenerationContext
        {
            Rooms = generatedRooms,
            Corridors = generatedCorridors,
            FloorTilemap = floorTilemap,
            WallTilemap = wallTilemap,
            ObjectTilemap = objectTilemap,
            Seed = seed
        };
    }
}

// Phase 2: Content Population
public class ContentGenerationOrchestrator
{
    public IEnumerator PopulateContent(MapGenerationContext context)
    {
        // Receives completed structure from Phase 1
        // Adds spawn points, resources, special rooms, biomes
        // ...
    }
}
```

**Sequential Execution:**

```csharp
public class MapGenerationController : MonoBehaviour
{
    [SerializeField] private BSPMapGenerator bspGenerator;
    [SerializeField] private ContentGenerationOrchestrator contentOrchestrator;

    public IEnumerator GenerateFullMap(int seed)
    {
        // Phase 1: Structure
        var context = bspGenerator.Generate(seed, new Vector2Int(100, 100));
        yield return null;

        // Phase 2: Content
        yield return contentOrchestrator.PopulateContent(context);

        // Finalization
        FinalizeMap(context);
    }

    private void FinalizeMap(MapGenerationContext context)
    {
        // Bake NavMesh
        GetComponent<NavMeshSurface>().BuildNavMesh();

        // Notify game systems
        WaveSpawner.Instance.Initialize(context.SpawnPoints);
        CameraController.Instance.SetBounds(context.MapBounds);
    }
}
```

### 10.2 Data Dependencies

**Dependency Graph:**

```
BSP Rooms → Room Classification → Template Assignment
    ↓                                     ↓
Corridors → Doorway Mapping → Corridor-Doorway Connection
    ↓                                     ↓
Player Spawn → Spawn Point Placement ← Room Types
    ↓                                     ↓
Difficulty Settings → Resource Distribution ← Room Count
    ↓                                     ↓
Selected Biome → Biome Application ← Tilemap
```

**Critical Dependencies:**

| Phase 2 System | Depends On (Phase 1) | Why |
|---------------|----------------------|-----|
| Room Classification | Room boundaries, connectivity | Determines room type based on size, position |
| Spawn Points | Room boundaries, player spawn | Maintains distance from player |
| Resources | Room count, classifications | Calculates resource budget |
| Templates | Corridor doorways | Aligns template doors with corridors |
| Biomes | Completed tilemap | Swaps tiles in-place |

### 10.3 Validation Points

**Phase 1 Validation (before Phase 2):**

```csharp
public class Phase1Validator
{
    public ValidationResult ValidatePhase1Output(MapGenerationContext context)
    {
        var result = new ValidationResult();

        // 1. All rooms connected
        if (!AllRoomsConnected(context.Rooms))
            result.AddError("Not all rooms are connected");

        // 2. Player spawn is valid
        if (!IsPlayerSpawnValid(context.PlayerSpawnPosition, context.Rooms))
            result.AddError("Player spawn not in a valid room");

        // 3. Minimum room count
        if (context.Rooms.Count < 10)
            result.AddWarning($"Low room count: {context.Rooms.Count}");

        // 4. NavMesh coverage
        var navMeshCoverage = CalculateNavMeshCoverage(context.FloorTilemap);
        if (navMeshCoverage < 0.9f)
            result.AddWarning($"Low NavMesh coverage: {navMeshCoverage * 100}%");

        return result;
    }
}
```

**Phase 2 Validation (post-generation):**

```csharp
public class Phase2Validator
{
    public ValidationResult ValidatePhase2Output(MapGenerationContext context)
    {
        var result = new ValidationResult();

        // 1. Spawn points exist
        if (context.SpawnPoints.Count == 0)
            result.AddError("No spawn points generated");

        // 2. Resources placed
        if (context.PlacedResources.Count == 0)
            result.AddWarning("No resources placed");

        // 3. No spawn in player start room
        var playerStartRoom = context.RoomTypes.FirstOrDefault(r => r.Value == RoomClassification.PlayerStart).Key;
        foreach (var spawn in context.SpawnPoints)
        {
            if (spawn.GetComponent<SpawnPointMetadata>().roomID == playerStartRoom?.ID)
                result.AddError("Spawn point in player start room");
        }

        // 4. Resource accessibility
        foreach (var resource in context.PlacedResources)
        {
            if (!NavMesh.SamplePosition(new Vector3(resource.position.x, resource.position.y, 0), out NavMeshHit hit, 1f, NavMesh.AllAreas))
                result.AddWarning($"Resource at {resource.position} not on NavMesh");
        }

        return result;
    }
}
```

**Integration Validation:**

```csharp
public class IntegrationValidator
{
    public ValidationResult ValidateFullMap(MapGenerationContext context)
    {
        var result = new ValidationResult();

        // Cross-phase validation

        // 1. Corridors connect to template doorways
        foreach (var room in context.RoomTypes.Keys)
        {
            var instance = GetRoomInstance(room);
            if (instance != null && instance.doorwayWorldPositions.Count > 0)
            {
                bool hasCorridorConnection = context.Corridors.Any(c =>
                    instance.doorwayWorldPositions.Any(d =>
                        Vector2Int.Distance(c.StartPosition, d) < 2 ||
                        Vector2Int.Distance(c.EndPosition, d) < 2
                    )
                );

                if (!hasCorridorConnection)
                    result.AddWarning($"Room {room.ID} template doors not connected to corridors");
            }
        }

        // 2. Furniture doesn't block corridors
        // ... validation logic ...

        return result;
    }
}
```

---

## Conclusion

Phase 2 (Content & Features) is the **gameplay heart** of the procedural generation system. While Phase 1 creates the spatial structure, Phase 2 makes it playable, balanced, and engaging.

### Key Architectural Achievements

1. **Data-Driven Flexibility**
   - All content configured through ScriptableObjects
   - Zero code changes for new enemies, rooms, biomes
   - Designer empowerment through Unity Inspector

2. **Scalable Spawn System**
   - Probability curves enable complex enemy progression
   - Room-aware spawning creates tactical variety
   - Integrates seamlessly with existing WaveSpawner

3. **Balanced Resource Economy**
   - Algorithmic distribution prevents exploits
   - Difficulty scaling maintains challenge
   - Risk-reward exploration incentives

4. **Template-Based Room Design**
   - Hand-crafted quality with procedural variety
   - Modular furniture placement
   - Clean corridor integration via doorway system

5. **Visual Theming**
   - Biome system for cosmetic variety
   - No gameplay impact (fair balance)
   - Supports narrative progression

6. **Performance Optimized**
   - Object pooling for all instantiated content
   - Lazy loading strategies
   - Chunked generation for smooth frame rates

7. **Extensible Architecture**
   - Plugin system for mods
   - Clear interfaces for new content types
   - Validation frameworks prevent broken content

### Next Steps

**Phase 3: Finalization & Integration**
- NavMesh optimization
- Multiplayer considerations
- Save/load system for generated maps
- Analytics for balance tuning

**Phase 4: Advanced Features**
- Multi-floor dungeons with stairs
- Destructible environments
- Dynamic events (fire, flooding)
- Boss encounter scripting

---

**Document Status:** ✅ Complete
**Review Required:** Gameplay Designer, Lead Engineer
**Implementation Target:** Q1 2026
**Estimated Effort:** 10-12 days (Days 8-12 from MAP_GENERATION_PLAN.md)

---

**References:**
- MAP_GENERATION_PLAN.md (Phase 1 BSP architecture)
- Existing codebase: WaveSpawner.cs, Enemy.cs, ObjectPooler.cs
- Unity API: Tilemap, NavMeshPlus, ScriptableObject

**Version History:**
- 1.0 (2025-11-17): Initial comprehensive deep dive
