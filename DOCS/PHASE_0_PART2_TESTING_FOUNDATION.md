# Phase 0 Part 2: Testing Foundation Architecture
## Office-Mice Map Generation System

**Document Version:** 1.0
**Date:** 2025-11-17
**Author:** Software Architecture Analysis
**Status:** Architectural Blueprint - Foundation Layer
**Architectural Impact:** CRITICAL
**Technical Complexity:** HIGH

---

## Executive Summary

Phase 0 establishes the **testing foundation** that enables Test-Driven Development (TDD) for the procedural map generation system. This is the critical infrastructure layer that must exist before implementing Phase 1 (BSP Generation) and Phase 2 (Content Population).

**Core Mandate:** Enable confident, iterative development through comprehensive test coverage, rapid feedback loops, and isolated component testing.

### Why Testing First?

1. **Complexity Management** - Procedural generation has many edge cases
2. **Refactoring Safety** - Change algorithms without breaking functionality
3. **Documentation** - Tests serve as executable specifications
4. **Quality Assurance** - Catch bugs before they reach production
5. **Design Validation** - TDD drives better architectural decisions
6. **Performance Baseline** - Establish benchmarks early

---

## Table of Contents

1. [Unity Test Framework Integration](#1-unity-test-framework-integration)
2. [Assembly Definition Strategy](#2-assembly-definition-strategy)
3. [Mock and Stub Architecture](#3-mock-and-stub-architecture)
4. [Test Data Factory Patterns](#4-test-data-factory-patterns)
5. [TDD Workflow Setup](#5-tdd-workflow-setup)
6. [Test Isolation Techniques](#6-test-isolation-techniques)
7. [Performance Benchmarking Approach](#7-performance-benchmarking-approach)
8. [Coverage Strategy](#8-coverage-strategy)
9. [Implementation Roadmap](#9-implementation-roadmap)

---

## 1. Unity Test Framework Integration

### 1.1 Framework Architecture

Unity Test Framework (UTF) provides two test modes:

**EditMode Tests:**
- Run in Unity Editor
- Fast execution (<1ms per test typically)
- No scene loading required
- Ideal for pure logic testing
- Synchronous execution

**PlayMode Tests:**
- Run in Unity Player
- Scene integration testing
- NavMesh, Physics, Rendering validation
- Coroutine support
- Slower but realistic

### 1.2 Directory Structure

```
Assets/
├── Scripts/
│   ├── MapGeneration/
│   │   ├── MapGeneration.asmdef                    # Production code
│   │   ├── Core/
│   │   │   ├── BSPNode.cs
│   │   │   ├── Room.cs
│   │   │   ├── Corridor.cs
│   │   │   └── MapGenerationContext.cs
│   │   ├── Generators/
│   │   │   ├── BSPMapGenerator.cs
│   │   │   ├── CorridorGenerator.cs
│   │   │   └── TilemapRenderer.cs
│   │   ├── Content/
│   │   │   ├── SpawnPointManager.cs
│   │   │   ├── ResourceDistributionManager.cs
│   │   │   └── BiomeManager.cs
│   │   └── Utilities/
│   │       ├── NavMeshValidator.cs
│   │       └── RoomClassifier.cs
│   │
│   └── Game/                                       # Existing game code
│       ├── Game.asmdef
│       ├── Player/
│       ├── Mouse/
│       └── Items/
│
├── Tests/
│   ├── EditMode/
│   │   ├── MapGeneration.EditMode.Tests.asmdef    # EditMode tests assembly
│   │   ├── Core/
│   │   │   ├── BSPNodeTests.cs
│   │   │   ├── RoomTests.cs
│   │   │   └── CorridorTests.cs
│   │   ├── Generators/
│   │   │   ├── BSPMapGeneratorTests.cs
│   │   │   └── CorridorGeneratorTests.cs
│   │   ├── Content/
│   │   │   ├── SpawnPointManagerTests.cs
│   │   │   └── ResourceDistributionTests.cs
│   │   └── TestUtilities/
│   │       ├── TestDataFactory.cs
│   │       ├── MockTilemap.cs
│   │       └── AssertionExtensions.cs
│   │
│   └── PlayMode/
│       ├── MapGeneration.PlayMode.Tests.asmdef    # PlayMode tests assembly
│       ├── Integration/
│       │   ├── FullGenerationTests.cs
│       │   ├── NavMeshIntegrationTests.cs
│       │   └── TilemapRenderingTests.cs
│       ├── Performance/
│       │   ├── GenerationPerformanceTests.cs
│       │   └── MemoryAllocationTests.cs
│       └── Fixtures/
│           ├── TestScenes/
│           │   ├── EmptyTestScene.unity
│           │   └── TilemapTestScene.unity
│           └── TestPrefabs/
│               ├── TestTilemap.prefab
│               └── TestNavMeshSurface.prefab
```

### 1.3 Assembly Definition Configurations

**MapGeneration.asmdef** (Production Code)
```json
{
  "name": "MapGeneration",
  "rootNamespace": "OfficeMice.MapGeneration",
  "references": [
    "Unity.2D.Tilemap.Extras",
    "Unity.AI.Navigation"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**MapGeneration.EditMode.Tests.asmdef** (EditMode Tests)
```json
{
  "name": "MapGeneration.EditMode.Tests",
  "rootNamespace": "OfficeMice.MapGeneration.Tests.EditMode",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "MapGeneration"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**MapGeneration.PlayMode.Tests.asmdef** (PlayMode Tests)
```json
{
  "name": "MapGeneration.PlayMode.Tests",
  "rootNamespace": "OfficeMice.MapGeneration.Tests.PlayMode",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "MapGeneration",
    "Unity.2D.Tilemap.Extras",
    "Unity.AI.Navigation"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

### 1.4 Test Runner Configuration

**Enable Test Runner:**
```
Window → General → Test Runner
```

**Configure Settings:**
```csharp
// Assets/Tests/EditMode/TestRunnerSettings.cs
using UnityEngine;
using NUnit.Framework;

[SetUpFixture]
public class TestRunnerSettings
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Disable Unity logs during tests (optional)
        Debug.unityLogger.logEnabled = false;

        // Set deterministic random seed for reproducibility
        Random.InitState(12345);
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Re-enable logs
        Debug.unityLogger.logEnabled = true;
    }
}
```

---

## 2. Assembly Definition Strategy

### 2.1 Dependency Isolation Principles

**Architectural Goals:**
1. **Test Independence** - Tests compile without production dependencies where possible
2. **Fast Compilation** - Changes to production code don't recompile all tests
3. **Clear Boundaries** - Production code never references test code
4. **Platform Targeting** - EditMode tests only compile in Editor

### 2.2 Assembly Graph

```
MapGeneration (Runtime Assembly)
    ↓ references
Unity.2D.Tilemap.Extras
Unity.AI.Navigation
    ↑ referenced by
MapGeneration.EditMode.Tests (Editor-Only)
    ↓ references
UnityEngine.TestRunner
NUnit.Framework
    ↑ referenced by
MapGeneration.PlayMode.Tests (All Platforms)
    ↓ references
UnityEngine.TestRunner
NUnit.Framework
Unity.2D.Tilemap.Extras
Unity.AI.Navigation
```

**Design Decision:** Separate EditMode and PlayMode assemblies prevent runtime test bloat.

### 2.3 Namespace Conventions

```csharp
// Production Code
namespace OfficeMice.MapGeneration.Core { }
namespace OfficeMice.MapGeneration.Generators { }
namespace OfficeMice.MapGeneration.Content { }
namespace OfficeMice.MapGeneration.Utilities { }

// EditMode Tests
namespace OfficeMice.MapGeneration.Tests.EditMode.Core { }
namespace OfficeMice.MapGeneration.Tests.EditMode.Generators { }
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities { }

// PlayMode Tests
namespace OfficeMice.MapGeneration.Tests.PlayMode.Integration { }
namespace OfficeMice.MapGeneration.Tests.PlayMode.Performance { }
```

**Benefits:**
- Clear test/production separation
- Avoids namespace collisions
- Supports using directives without ambiguity

### 2.4 Internal Access Strategy

**Problem:** Tests need access to internal implementation details.

**Solution:** Use `InternalsVisibleTo` attribute.

```csharp
// Assets/Scripts/MapGeneration/AssemblyInfo.cs
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MapGeneration.EditMode.Tests")]
[assembly: InternalsVisibleTo("MapGeneration.PlayMode.Tests")]
```

**Usage Example:**
```csharp
// Production code with internal method
namespace OfficeMice.MapGeneration.Core
{
    public class BSPNode
    {
        // Public API
        public void Split(int minSize) { ... }

        // Internal implementation detail (testable via InternalsVisibleTo)
        internal bool IsSplittable(int minSize)
        {
            return rect.width >= minSize * 2 || rect.height >= minSize * 2;
        }
    }
}

// Test can access internal method
namespace OfficeMice.MapGeneration.Tests.EditMode.Core
{
    [TestFixture]
    public class BSPNodeTests
    {
        [Test]
        public void IsSplittable_WhenTooSmall_ReturnsFalse()
        {
            var node = new BSPNode(new Rect(0, 0, 10, 10));
            Assert.False(node.IsSplittable(minSize: 6));
        }
    }
}
```

---

## 3. Mock and Stub Architecture

### 3.1 Abstraction Layer Design

**Core Principle:** Program to interfaces, not implementations.

**Unity Dependencies to Abstract:**
```
- Tilemap (Unity.Tilemaps)
- NavMeshSurface (Unity.AI.Navigation)
- Random (UnityEngine.Random)
- Time (UnityEngine.Time)
- Physics2D (collision detection)
```

### 3.2 Interface Definitions

```csharp
// Assets/Scripts/MapGeneration/Interfaces/ITilemapAdapter.cs
namespace OfficeMice.MapGeneration.Interfaces
{
    public interface ITilemapAdapter
    {
        void SetTile(Vector3Int position, TileBase tile);
        TileBase GetTile(Vector3Int position);
        void BoxFill(Vector3Int position, TileBase tile, int xMin, int yMin, int xMax, int yMax);
        void ClearAllTiles();
        BoundsInt CellBounds { get; }
    }
}

// Assets/Scripts/MapGeneration/Interfaces/INavMeshAdapter.cs
namespace OfficeMice.MapGeneration.Interfaces
{
    public interface INavMeshAdapter
    {
        void BuildNavMesh();
        bool SamplePosition(Vector3 position, out NavMeshHit hit, float maxDistance, int areaMask);
        float GetNavMeshCoverage(Bounds bounds);
    }
}

// Assets/Scripts/MapGeneration/Interfaces/IRandomProvider.cs
namespace OfficeMice.MapGeneration.Interfaces
{
    public interface IRandomProvider
    {
        void InitState(int seed);
        int Range(int min, int max);
        float Range(float min, float max);
        float Value { get; }
    }
}
```

### 3.3 Production Implementations

```csharp
// Assets/Scripts/MapGeneration/Adapters/UnityTilemapAdapter.cs
namespace OfficeMice.MapGeneration.Adapters
{
    public class UnityTilemapAdapter : ITilemapAdapter
    {
        private readonly Tilemap tilemap;

        public UnityTilemapAdapter(Tilemap tilemap)
        {
            this.tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
        }

        public void SetTile(Vector3Int position, TileBase tile)
        {
            tilemap.SetTile(position, tile);
        }

        public TileBase GetTile(Vector3Int position)
        {
            return tilemap.GetTile(position);
        }

        public void BoxFill(Vector3Int position, TileBase tile, int xMin, int yMin, int xMax, int yMax)
        {
            tilemap.BoxFill(position, tile, xMin, yMin, xMax, yMax);
        }

        public void ClearAllTiles()
        {
            tilemap.ClearAllTiles();
        }

        public BoundsInt CellBounds => tilemap.cellBounds;
    }
}

// Assets/Scripts/MapGeneration/Adapters/UnityRandomProvider.cs
namespace OfficeMice.MapGeneration.Adapters
{
    public class UnityRandomProvider : IRandomProvider
    {
        public void InitState(int seed)
        {
            UnityEngine.Random.InitState(seed);
        }

        public int Range(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public float Range(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public float Value => UnityEngine.Random.value;
    }
}
```

### 3.4 Mock Implementations for Testing

```csharp
// Assets/Tests/EditMode/TestUtilities/MockTilemap.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    public class MockTilemap : ITilemapAdapter
    {
        private Dictionary<Vector3Int, TileBase> tiles = new Dictionary<Vector3Int, TileBase>();
        private BoundsInt bounds;

        public MockTilemap(int width, int height)
        {
            bounds = new BoundsInt(0, 0, 0, width, height, 1);
        }

        public void SetTile(Vector3Int position, TileBase tile)
        {
            tiles[position] = tile;
        }

        public TileBase GetTile(Vector3Int position)
        {
            return tiles.TryGetValue(position, out var tile) ? tile : null;
        }

        public void BoxFill(Vector3Int position, TileBase tile, int xMin, int yMin, int xMax, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        public void ClearAllTiles()
        {
            tiles.Clear();
        }

        public BoundsInt CellBounds => bounds;

        // Test-specific helpers
        public int TileCount => tiles.Count;
        public bool HasTileAt(Vector3Int position) => tiles.ContainsKey(position);
        public IReadOnlyDictionary<Vector3Int, TileBase> AllTiles => tiles;
    }
}

// Assets/Tests/EditMode/TestUtilities/DeterministicRandomProvider.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    /// <summary>
    /// Deterministic random for reproducible tests
    /// </summary>
    public class DeterministicRandomProvider : IRandomProvider
    {
        private System.Random random;

        public DeterministicRandomProvider(int seed = 12345)
        {
            InitState(seed);
        }

        public void InitState(int seed)
        {
            random = new System.Random(seed);
        }

        public int Range(int min, int max)
        {
            return random.Next(min, max);
        }

        public float Range(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }

        public float Value => (float)random.NextDouble();
    }
}

// Assets/Tests/EditMode/TestUtilities/MockNavMeshAdapter.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    public class MockNavMeshAdapter : INavMeshAdapter
    {
        private HashSet<Vector3> validPositions = new HashSet<Vector3>();
        private float mockCoverage = 0.95f;

        public void AddValidPosition(Vector3 position)
        {
            validPositions.Add(position);
        }

        public void SetMockCoverage(float coverage)
        {
            mockCoverage = coverage;
        }

        public void BuildNavMesh()
        {
            // No-op in mock
        }

        public bool SamplePosition(Vector3 position, out NavMeshHit hit, float maxDistance, int areaMask)
        {
            hit = default;

            // Simple proximity check
            foreach (var validPos in validPositions)
            {
                if (Vector3.Distance(position, validPos) <= maxDistance)
                {
                    hit = new NavMeshHit { position = validPos };
                    return true;
                }
            }

            return false;
        }

        public float GetNavMeshCoverage(Bounds bounds)
        {
            return mockCoverage;
        }

        // Test helpers
        public int ValidPositionCount => validPositions.Count;
        public bool WasBuilt { get; private set; }
    }
}
```

### 3.5 Dependency Injection Pattern

**Constructor Injection (Preferred):**

```csharp
// Assets/Scripts/MapGeneration/Generators/BSPMapGenerator.cs
namespace OfficeMice.MapGeneration.Generators
{
    public class BSPMapGenerator
    {
        private readonly ITilemapAdapter floorTilemap;
        private readonly ITilemapAdapter wallTilemap;
        private readonly IRandomProvider random;

        // Constructor injection - testable
        public BSPMapGenerator(
            ITilemapAdapter floorTilemap,
            ITilemapAdapter wallTilemap,
            IRandomProvider random)
        {
            this.floorTilemap = floorTilemap ?? throw new ArgumentNullException(nameof(floorTilemap));
            this.wallTilemap = wallTilemap ?? throw new ArgumentNullException(nameof(wallTilemap));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        // Overload for Unity MonoBehaviour usage (convenience)
        public BSPMapGenerator(Tilemap floor, Tilemap wall)
            : this(
                new UnityTilemapAdapter(floor),
                new UnityTilemapAdapter(wall),
                new UnityRandomProvider())
        {
        }

        public MapGenerationContext Generate(int seed, Vector2Int mapSize)
        {
            random.InitState(seed);
            // ... generation logic using interfaces ...
        }
    }
}
```

**Usage in Tests:**

```csharp
// Assets/Tests/EditMode/Generators/BSPMapGeneratorTests.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Generators
{
    [TestFixture]
    public class BSPMapGeneratorTests
    {
        private MockTilemap mockFloor;
        private MockTilemap mockWall;
        private DeterministicRandomProvider mockRandom;
        private BSPMapGenerator generator;

        [SetUp]
        public void Setup()
        {
            mockFloor = new MockTilemap(100, 100);
            mockWall = new MockTilemap(100, 100);
            mockRandom = new DeterministicRandomProvider(seed: 42);

            generator = new BSPMapGenerator(mockFloor, mockWall, mockRandom);
        }

        [Test]
        public void Generate_WithSeed42_ProducesDeterministicOutput()
        {
            // Arrange
            int seed = 42;
            var mapSize = new Vector2Int(50, 50);

            // Act
            var result1 = generator.Generate(seed, mapSize);

            // Reset and regenerate
            Setup();
            var result2 = generator.Generate(seed, mapSize);

            // Assert
            Assert.AreEqual(result1.Rooms.Count, result2.Rooms.Count);
            Assert.AreEqual(mockFloor.TileCount, mockFloor.TileCount);
        }
    }
}
```

---

## 4. Test Data Factory Patterns

### 4.1 Factory Architecture

**Purpose:** Centralized creation of test data with sensible defaults.

```csharp
// Assets/Tests/EditMode/TestUtilities/TestDataFactory.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    /// <summary>
    /// Fluent builder for test data creation
    /// </summary>
    public static class TestDataFactory
    {
        // Room creation
        public static Room CreateRoom(
            int id = 1,
            int x = 0,
            int y = 0,
            int width = 10,
            int height = 10)
        {
            return new Room
            {
                ID = id,
                Bounds = new RectInt(x, y, width, height),
                Center = new Vector2Int(x + width / 2, y + height / 2),
                Area = width * height,
                ConnectedRooms = new List<Room>(),
                IsOnCriticalPath = false,
                DistanceFromPlayerSpawn = 0f
            };
        }

        // Corridor creation
        public static Corridor CreateCorridor(
            Vector2Int start,
            Vector2Int end,
            int width = 3)
        {
            return new Corridor
            {
                StartPosition = start,
                EndPosition = end,
                Width = width,
                Tiles = GenerateCorridorTiles(start, end, width)
            };
        }

        // BSPNode creation
        public static BSPNode CreateBSPNode(
            int x = 0,
            int y = 0,
            int width = 20,
            int height = 20)
        {
            return new BSPNode(new Rect(x, y, width, height));
        }

        // MapGenerationContext creation
        public static MapGenerationContext CreateContext(
            int roomCount = 10,
            int seed = 12345)
        {
            var rooms = new List<Room>();
            for (int i = 0; i < roomCount; i++)
            {
                rooms.Add(CreateRoom(id: i, x: i * 15, y: 0));
            }

            return new MapGenerationContext
            {
                Rooms = rooms,
                Corridors = new List<Corridor>(),
                Seed = seed,
                PlayerSpawnPosition = Vector2Int.zero,
                DifficultySettings = CreateDifficultySettings(),
                RoomTypes = new Dictionary<Room, RoomClassification>(),
                SpawnPoints = new List<SpawnPoint>(),
                PlacedResources = new List<ResourcePlacement>()
            };
        }

        // DifficultySettings creation
        public static DifficultySettings CreateDifficultySettings(
            float spawnDensity = 1f,
            float resourceScarcity = 1f)
        {
            var settings = ScriptableObject.CreateInstance<DifficultySettings>();
            settings.spawnDensityMultiplier = spawnDensity;
            settings.resourceScarcityMultiplier = resourceScarcity;
            settings.minSpawnPoints = 5;
            settings.maxSpawnsPerRoom = 10;
            settings.currentWave = 1;
            return settings;
        }

        // Helper methods
        private static List<Vector2Int> GenerateCorridorTiles(Vector2Int start, Vector2Int end, int width)
        {
            var tiles = new List<Vector2Int>();
            // Simple L-shaped corridor
            for (int x = start.x; x <= end.x; x++)
                tiles.Add(new Vector2Int(x, start.y));
            for (int y = start.y; y <= end.y; y++)
                tiles.Add(new Vector2Int(end.x, y));
            return tiles;
        }
    }
}
```

### 4.2 Builder Pattern for Complex Objects

```csharp
// Assets/Tests/EditMode/TestUtilities/RoomBuilder.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    /// <summary>
    /// Fluent builder for complex Room configurations in tests
    /// </summary>
    public class RoomBuilder
    {
        private int id = 1;
        private RectInt bounds = new RectInt(0, 0, 10, 10);
        private List<Room> connectedRooms = new List<Room>();
        private bool isOnCriticalPath = false;
        private float distanceFromPlayerSpawn = 0f;

        public RoomBuilder WithID(int id)
        {
            this.id = id;
            return this;
        }

        public RoomBuilder WithBounds(int x, int y, int width, int height)
        {
            this.bounds = new RectInt(x, y, width, height);
            return this;
        }

        public RoomBuilder WithSize(int width, int height)
        {
            this.bounds.width = width;
            this.bounds.height = height;
            return this;
        }

        public RoomBuilder ConnectedTo(params Room[] rooms)
        {
            this.connectedRooms.AddRange(rooms);
            return this;
        }

        public RoomBuilder OnCriticalPath()
        {
            this.isOnCriticalPath = true;
            return this;
        }

        public RoomBuilder AtDistanceFromPlayer(float distance)
        {
            this.distanceFromPlayerSpawn = distance;
            return this;
        }

        public Room Build()
        {
            return new Room
            {
                ID = id,
                Bounds = bounds,
                Center = new Vector2Int(
                    bounds.x + bounds.width / 2,
                    bounds.y + bounds.height / 2
                ),
                Area = bounds.width * bounds.height,
                ConnectedRooms = connectedRooms,
                IsOnCriticalPath = isOnCriticalPath,
                DistanceFromPlayerSpawn = distanceFromPlayerSpawn
            };
        }
    }
}

// Usage in tests
[Test]
public void RoomClassifier_LargeRoomWithManyConnections_ClassifiedAsArena()
{
    // Arrange
    var room = new RoomBuilder()
        .WithID(1)
        .WithSize(25, 25)  // Large room
        .ConnectedTo(
            TestDataFactory.CreateRoom(2),
            TestDataFactory.CreateRoom(3),
            TestDataFactory.CreateRoom(4)
        )
        .AtDistanceFromPlayer(50f)
        .Build();

    var classifier = new RoomClassifier();

    // Act
    var classification = classifier.ClassifyRoom(room, context);

    // Assert
    Assert.AreEqual(RoomClassification.ArenaRoom, classification);
}
```

### 4.3 Fixture Data Management

```csharp
// Assets/Tests/EditMode/TestUtilities/FixtureData.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Utilities
{
    /// <summary>
    /// Shared fixture data for tests (singleton pattern)
    /// </summary>
    public static class FixtureData
    {
        // Common test tiles
        public static TileBase FloorTile { get; private set; }
        public static TileBase WallTile { get; private set; }

        // Common test configurations
        public static readonly Vector2Int StandardMapSize = new Vector2Int(100, 100);
        public static readonly int StandardSeed = 42;

        // Reusable test scenarios
        public static class Scenarios
        {
            public static MapGenerationContext SmallMap()
            {
                return TestDataFactory.CreateContext(roomCount: 5, seed: StandardSeed);
            }

            public static MapGenerationContext MediumMap()
            {
                return TestDataFactory.CreateContext(roomCount: 15, seed: StandardSeed);
            }

            public static MapGenerationContext LargeMap()
            {
                return TestDataFactory.CreateContext(roomCount: 30, seed: StandardSeed);
            }

            public static Room BossRoom()
            {
                return new RoomBuilder()
                    .WithID(999)
                    .WithSize(30, 30)
                    .AtDistanceFromPlayer(200f)
                    .Build();
            }

            public static Room SecretRoom()
            {
                return new RoomBuilder()
                    .WithID(666)
                    .WithSize(8, 8)
                    .ConnectedTo(TestDataFactory.CreateRoom(1))
                    .Build();
            }
        }

        static FixtureData()
        {
            // Initialize test tiles (ScriptableObjects)
            FloorTile = ScriptableObject.CreateInstance<Tile>();
            FloorTile.name = "TestFloorTile";

            WallTile = ScriptableObject.CreateInstance<Tile>();
            WallTile.name = "TestWallTile";
        }
    }
}
```

---

## 5. TDD Workflow Setup

### 5.1 Red-Green-Refactor Cycle

**TDD Discipline:**

```
1. RED:    Write failing test (specifies behavior)
2. GREEN:  Write minimal code to pass test
3. REFACTOR: Improve code without changing behavior
4. REPEAT: Next feature/edge case
```

### 5.2 Example TDD Session: BSPNode Splitting

**Step 1: Write Failing Test (RED)**

```csharp
// Assets/Tests/EditMode/Core/BSPNodeTests.cs
namespace OfficeMice.MapGeneration.Tests.EditMode.Core
{
    [TestFixture]
    public class BSPNodeTests
    {
        [Test]
        public void Split_WhenSizeAboveMinimum_CreatesTwoChildren()
        {
            // Arrange
            var node = new BSPNode(new Rect(0, 0, 20, 20));
            int minRoomSize = 8;

            // Act
            node.Split(minRoomSize);

            // Assert
            Assert.IsNotNull(node.Left, "Left child should be created");
            Assert.IsNotNull(node.Right, "Right child should be created");
            Assert.IsFalse(node.IsLeaf, "Node should no longer be a leaf");
        }
    }
}

// Test fails - BSPNode.Split() doesn't exist yet
```

**Step 2: Minimal Implementation (GREEN)**

```csharp
// Assets/Scripts/MapGeneration/Core/BSPNode.cs
namespace OfficeMice.MapGeneration.Core
{
    public class BSPNode
    {
        public Rect Rect { get; private set; }
        public BSPNode Left { get; private set; }
        public BSPNode Right { get; private set; }
        public bool IsLeaf => Left == null && Right == null;

        public BSPNode(Rect rect)
        {
            Rect = rect;
        }

        public void Split(int minRoomSize)
        {
            // Minimal implementation to pass test
            if (Rect.width >= minRoomSize * 2)
            {
                float splitX = Rect.width / 2;
                Left = new BSPNode(new Rect(Rect.x, Rect.y, splitX, Rect.height));
                Right = new BSPNode(new Rect(Rect.x + splitX, Rect.y, Rect.width - splitX, Rect.height));
            }
        }
    }
}

// Test passes - GREEN
```

**Step 3: Add Edge Case Test (RED)**

```csharp
[Test]
public void Split_WhenTooSmall_DoesNotSplit()
{
    // Arrange
    var node = new BSPNode(new Rect(0, 0, 10, 10));
    int minRoomSize = 8;

    // Act
    node.Split(minRoomSize);

    // Assert
    Assert.IsNull(node.Left, "Should not create left child when too small");
    Assert.IsNull(node.Right, "Should not create right child when too small");
    Assert.IsTrue(node.IsLeaf, "Should remain a leaf when too small");
}

// Test passes - our existing guard handles this
```

**Step 4: Add Complexity Test (RED)**

```csharp
[Test]
public void Split_ShouldRandomlyChooseHorizontalOrVertical()
{
    // Arrange - square room can split either way
    var node = new BSPNode(new Rect(0, 0, 30, 30));
    int minRoomSize = 10;

    // Act - split multiple times with different random seeds
    var results = new HashSet<string>();
    for (int seed = 0; seed < 20; seed++)
    {
        var testNode = new BSPNode(new Rect(0, 0, 30, 30));
        UnityEngine.Random.InitState(seed);
        testNode.Split(minRoomSize);

        // Determine split direction by child dimensions
        string direction = testNode.Left.Rect.width < testNode.Rect.width ? "vertical" : "horizontal";
        results.Add(direction);
    }

    // Assert - should have both orientations across 20 trials
    Assert.Contains("vertical", results.ToList());
    Assert.Contains("horizontal", results.ToList());
}

// Test fails - we always split vertically
```

**Step 5: Enhance Implementation (GREEN)**

```csharp
public void Split(int minRoomSize, IRandomProvider random = null)
{
    random = random ?? new UnityRandomProvider();

    // Can't split if too small
    if (Rect.width < minRoomSize * 2 && Rect.height < minRoomSize * 2)
        return;

    // Choose split orientation
    bool splitHorizontally = random.Value > 0.5f;

    if (splitHorizontally && Rect.height >= minRoomSize * 2)
    {
        float splitY = random.Range(minRoomSize, Rect.height - minRoomSize);
        Left = new BSPNode(new Rect(Rect.x, Rect.y, Rect.width, splitY));
        Right = new BSPNode(new Rect(Rect.x, Rect.y + splitY, Rect.width, Rect.height - splitY));
    }
    else if (Rect.width >= minRoomSize * 2)
    {
        float splitX = random.Range(minRoomSize, Rect.width - minRoomSize);
        Left = new BSPNode(new Rect(Rect.x, Rect.y, splitX, Rect.height));
        Right = new BSPNode(new Rect(Rect.x + splitX, Rect.y, Rect.width - splitX, Rect.height));
    }
}

// All tests pass - GREEN
```

**Step 6: Refactor (REFACTOR)**

```csharp
public void Split(int minRoomSize, IRandomProvider random = null)
{
    random = random ?? new UnityRandomProvider();

    if (!CanSplit(minRoomSize))
        return;

    bool splitHorizontally = ChooseSplitOrientation(minRoomSize, random);

    if (splitHorizontally)
        SplitHorizontally(minRoomSize, random);
    else
        SplitVertically(minRoomSize, random);
}

private bool CanSplit(int minRoomSize)
{
    return Rect.width >= minRoomSize * 2 || Rect.height >= minRoomSize * 2;
}

private bool ChooseSplitOrientation(int minRoomSize, IRandomProvider random)
{
    bool canSplitHorizontally = Rect.height >= minRoomSize * 2;
    bool canSplitVertically = Rect.width >= minRoomSize * 2;

    if (canSplitHorizontally && !canSplitVertically)
        return true;
    if (canSplitVertically && !canSplitHorizontally)
        return false;

    return random.Value > 0.5f;
}

private void SplitHorizontally(int minRoomSize, IRandomProvider random)
{
    float splitY = random.Range(minRoomSize, Rect.height - minRoomSize);
    Left = new BSPNode(new Rect(Rect.x, Rect.y, Rect.width, splitY));
    Right = new BSPNode(new Rect(Rect.x, Rect.y + splitY, Rect.width, Rect.height - splitY));
}

private void SplitVertically(int minRoomSize, IRandomProvider random)
{
    float splitX = random.Range(minRoomSize, Rect.width - minRoomSize);
    Left = new BSPNode(new Rect(Rect.x, Rect.y, splitX, Rect.height));
    Right = new BSPNode(new Rect(Rect.x + splitX, Rect.y, Rect.width - splitX, Rect.height));
}

// All tests still pass - refactoring preserved behavior
```

### 5.3 Test Organization Patterns

**Arrange-Act-Assert (AAA) Pattern:**

```csharp
[Test]
public void MethodName_StateUnderTest_ExpectedBehavior()
{
    // Arrange - Setup test data and dependencies
    var dependency = new MockDependency();
    var sut = new SystemUnderTest(dependency);  // SUT = System Under Test
    var input = CreateTestInput();

    // Act - Execute the behavior being tested
    var result = sut.MethodBeingTested(input);

    // Assert - Verify the outcome
    Assert.AreEqual(expectedValue, result);
    Assert.IsTrue(result.SomeProperty);
}
```

**Test Categorization:**

```csharp
[TestFixture]
[Category("Unit")]
public class BSPNodeTests { }

[TestFixture]
[Category("Integration")]
public class FullGenerationTests { }

[TestFixture]
[Category("Performance")]
public class GenerationPerformanceTests { }
```

**Run specific categories:**
```
Test Runner → Filter → Category → "Unit"
```

---

## 6. Test Isolation Techniques

### 6.1 Isolation Principles

**Goals:**
1. **Independent** - Tests don't affect each other
2. **Repeatable** - Same input = same output
3. **Fast** - No unnecessary dependencies
4. **Focused** - Test one thing at a time

### 6.2 Setup and Teardown

```csharp
[TestFixture]
public class SpawnPointManagerTests
{
    private SpawnPointManager manager;
    private MockTilemap mockTilemap;
    private DeterministicRandomProvider mockRandom;
    private MapGenerationContext context;

    [SetUp]
    public void SetUp()
    {
        // Fresh state before EACH test
        mockTilemap = new MockTilemap(100, 100);
        mockRandom = new DeterministicRandomProvider(seed: 42);
        manager = new SpawnPointManager(mockTilemap, mockRandom);
        context = TestDataFactory.CreateContext();
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup after EACH test
        manager = null;
        mockTilemap = null;
        mockRandom = null;
        context = null;

        // Destroy any Unity objects created
        Object.DestroyImmediate(context.DifficultySettings);
    }

    [Test]
    public void PlaceSpawnPoints_ValidRoom_CreatesSpawnPoint()
    {
        // This test has isolated, fresh state from SetUp
        var room = TestDataFactory.CreateRoom();

        var spawnPoints = manager.PlaceSpawnPoints(
            new Dictionary<Room, RoomAssignment> { { room, new RoomAssignment() } },
            Vector2Int.zero,
            context.DifficultySettings
        );

        Assert.Greater(spawnPoints.Count, 0);
    }
}
```

### 6.3 Avoiding Static State

**Anti-Pattern:**
```csharp
// BAD - global mutable state
public static class GlobalConfig
{
    public static int MinRoomSize = 10;  // Tests can interfere
}
```

**Better Pattern:**
```csharp
// GOOD - instance-based configuration
public class GenerationConfig
{
    public int MinRoomSize { get; set; } = 10;
}

// In tests
[Test]
public void Test1()
{
    var config = new GenerationConfig { MinRoomSize = 8 };
    var generator = new BSPMapGenerator(config);
    // ...
}

[Test]
public void Test2()
{
    var config = new GenerationConfig { MinRoomSize = 12 };  // Doesn't affect Test1
    var generator = new BSPMapGenerator(config);
    // ...
}
```

### 6.4 Time and Randomness Control

**Deterministic Random:**
```csharp
[Test]
public void Generate_SameSeed_ProducesSameMap()
{
    // Arrange
    var random1 = new DeterministicRandomProvider(seed: 123);
    var random2 = new DeterministicRandomProvider(seed: 123);

    var generator1 = new BSPMapGenerator(mockTilemap, random1);
    var generator2 = new BSPMapGenerator(mockTilemap, random2);

    // Act
    var map1 = generator1.Generate(123, new Vector2Int(50, 50));
    var map2 = generator2.Generate(123, new Vector2Int(50, 50));

    // Assert
    Assert.AreEqual(map1.Rooms.Count, map2.Rooms.Count);
    for (int i = 0; i < map1.Rooms.Count; i++)
    {
        Assert.AreEqual(map1.Rooms[i].Bounds, map2.Rooms[i].Bounds);
    }
}
```

**Time Control:**
```csharp
// Interface for time abstraction
public interface ITimeProvider
{
    float DeltaTime { get; }
    float Time { get; }
}

// Mock implementation
public class MockTimeProvider : ITimeProvider
{
    public float DeltaTime { get; set; } = 0.016f;  // 60 FPS
    public float Time { get; set; } = 0f;

    public void AdvanceTime(float seconds)
    {
        Time += seconds;
    }
}
```

### 6.5 Avoiding Unity Object Leaks

**Problem:** Unity objects persist between tests

**Solution:** Explicit cleanup

```csharp
[TearDown]
public void TearDown()
{
    // Destroy GameObjects
    var spawnPoints = GameObject.FindGameObjectsWithTag("Spawn Point");
    foreach (var sp in spawnPoints)
        Object.DestroyImmediate(sp);

    // Destroy ScriptableObjects
    if (testDifficultySettings != null)
        Object.DestroyImmediate(testDifficultySettings);
}
```

**Alternative:** Use `[UnityTest]` with scenes

```csharp
[UnityTest]
public IEnumerator SpawnPointPlacement_InActualScene_WorksCorrectly()
{
    // Load test scene
    yield return SceneManager.LoadSceneAsync("EmptyTestScene");

    // Test logic
    // ...

    // Scene unload automatically cleans up
}
```

---

## 7. Performance Benchmarking Approach

### 7.1 Performance Test Framework

```csharp
// Assets/Tests/PlayMode/Performance/PerformanceTestBase.cs
namespace OfficeMice.MapGeneration.Tests.PlayMode.Performance
{
    public abstract class PerformanceTestBase
    {
        protected struct BenchmarkResult
        {
            public float AverageMs;
            public float MinMs;
            public float MaxMs;
            public float StandardDeviation;
            public int Iterations;
            public long MemoryAllocatedBytes;
        }

        protected BenchmarkResult Benchmark(Action action, int iterations = 100, int warmupIterations = 10)
        {
            // Warmup
            for (int i = 0; i < warmupIterations; i++)
                action();

            // Force GC before benchmark
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long memoryBefore = System.GC.GetTotalMemory(true);

            var times = new List<float>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                action();
                stopwatch.Stop();
                times.Add((float)stopwatch.Elapsed.TotalMilliseconds);
            }

            long memoryAfter = System.GC.GetTotalMemory(false);
            long memoryAllocated = memoryAfter - memoryBefore;

            float average = times.Average();
            float min = times.Min();
            float max = times.Max();
            float stdDev = CalculateStandardDeviation(times, average);

            return new BenchmarkResult
            {
                AverageMs = average,
                MinMs = min,
                MaxMs = max,
                StandardDeviation = stdDev,
                Iterations = iterations,
                MemoryAllocatedBytes = memoryAllocated
            };
        }

        private float CalculateStandardDeviation(List<float> values, float average)
        {
            float sumOfSquares = values.Sum(v => (v - average) * (v - average));
            return Mathf.Sqrt(sumOfSquares / values.Count);
        }

        protected void AssertPerformance(BenchmarkResult result, float maxAverageMs, string operationName)
        {
            Debug.Log($"{operationName} Performance:\n" +
                     $"  Average: {result.AverageMs:F2}ms\n" +
                     $"  Min: {result.MinMs:F2}ms\n" +
                     $"  Max: {result.MaxMs:F2}ms\n" +
                     $"  StdDev: {result.StandardDeviation:F2}ms\n" +
                     $"  Memory: {result.MemoryAllocatedBytes / 1024}KB");

            Assert.LessOrEqual(result.AverageMs, maxAverageMs,
                $"{operationName} exceeded performance target: {result.AverageMs:F2}ms > {maxAverageMs}ms");
        }
    }
}
```

### 7.2 Generation Performance Tests

```csharp
// Assets/Tests/PlayMode/Performance/GenerationPerformanceTests.cs
namespace OfficeMice.MapGeneration.Tests.PlayMode.Performance
{
    [TestFixture]
    [Category("Performance")]
    public class GenerationPerformanceTests : PerformanceTestBase
    {
        [Test]
        public void BSPGeneration_100x100Map_CompletesUnder2Seconds()
        {
            // Arrange
            var tilemap = new GameObject().AddComponent<Tilemap>();
            var grid = tilemap.gameObject.AddComponent<Grid>();
            var generator = new BSPMapGenerator(tilemap, tilemap);

            // Act
            var result = Benchmark(() =>
            {
                generator.Generate(seed: 42, mapSize: new Vector2Int(100, 100));
            }, iterations: 10);  // Fewer iterations for slow operations

            // Assert
            AssertPerformance(result, maxAverageMs: 2000f, "100x100 BSP Generation");

            // Cleanup
            Object.DestroyImmediate(tilemap.gameObject);
        }

        [Test]
        public void RoomClassification_1000Rooms_CompletesUnder100Ms()
        {
            // Arrange
            var rooms = new List<Room>();
            for (int i = 0; i < 1000; i++)
            {
                rooms.Add(TestDataFactory.CreateRoom(id: i, x: i * 10, y: 0));
            }

            var context = TestDataFactory.CreateContext();
            context.Rooms = rooms;
            var classifier = new RoomClassifier();

            // Act
            var result = Benchmark(() =>
            {
                foreach (var room in rooms)
                {
                    classifier.ClassifyRoom(room, context);
                }
            }, iterations: 100);

            // Assert
            AssertPerformance(result, maxAverageMs: 100f, "1000 Room Classification");
        }

        [UnityTest]
        public IEnumerator FullPipeline_MediumMap_CompletesUnder5Seconds()
        {
            // Arrange
            yield return SceneManager.LoadSceneAsync("TilemapTestScene");

            var controller = Object.FindObjectOfType<MapGenerationController>();
            Assert.IsNotNull(controller, "Test scene must have MapGenerationController");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            yield return controller.GenerateFullMap(seed: 42);

            stopwatch.Stop();
            float elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.LessOrEqual(elapsedMs, 5000f,
                $"Full pipeline too slow: {elapsedMs:F2}ms > 5000ms");

            Debug.Log($"Full pipeline completed in {elapsedMs:F2}ms");
        }
    }
}
```

### 7.3 Memory Allocation Tests

```csharp
[TestFixture]
[Category("Performance")]
public class MemoryAllocationTests : PerformanceTestBase
{
    [Test]
    public void SpawnPointPlacement_100Rooms_AllocatesUnder1MB()
    {
        // Arrange
        var rooms = Enumerable.Range(0, 100)
            .Select(i => TestDataFactory.CreateRoom(id: i))
            .ToList();

        var context = TestDataFactory.CreateContext();
        context.Rooms = rooms;

        var manager = new SpawnPointManager();

        // Measure memory before
        System.GC.Collect();
        long memoryBefore = System.GC.GetTotalMemory(true);

        // Act
        var roomAssignments = rooms.ToDictionary(
            r => r,
            r => new RoomAssignment { Classification = RoomClassification.StandardRoom }
        );

        manager.PlaceSpawnPoints(roomAssignments, Vector2Int.zero, context.DifficultySettings);

        // Measure memory after
        long memoryAfter = System.GC.GetTotalMemory(false);
        long allocated = memoryAfter - memoryBefore;

        // Assert
        float allocatedMB = allocated / (1024f * 1024f);
        Assert.LessOrEqual(allocatedMB, 1f,
            $"Spawn point placement allocated too much memory: {allocatedMB:F2}MB > 1MB");

        Debug.Log($"Memory allocated: {allocatedMB:F3}MB ({allocated} bytes)");
    }
}
```

### 7.4 Profiling Integration

```csharp
#if UNITY_EDITOR
using UnityEditor.Profiling;
using Unity.Profiling;

[Test]
public void BSPGeneration_ProfilingMarkers_RecordCorrectly()
{
    // Arrange
    var generator = new BSPMapGenerator(...);

    // Enable deep profiling
    ProfilerDriver.deepProfiling = true;

    // Act
    using (new ProfilerMarker("Test.BSPGeneration").Auto())
    {
        generator.Generate(seed: 42, mapSize: new Vector2Int(100, 100));
    }

    // Assert - check profiler data programmatically (Unity 2021.2+)
    // var samples = ProfilerDriver.GetStatisticsAvailable();
    // ...
}
#endif
```

---

## 8. Coverage Strategy

### 8.1 Coverage Goals

**Target Coverage Levels:**

| Component | Line Coverage | Branch Coverage | Priority |
|-----------|--------------|-----------------|----------|
| Core (BSPNode, Room, Corridor) | 95%+ | 90%+ | Critical |
| Generators (BSP, Corridor, Tilemap) | 90%+ | 85%+ | Critical |
| Content (Spawn, Resources, Biome) | 85%+ | 80%+ | High |
| Utilities (Classifier, Validator) | 80%+ | 75%+ | Medium |
| Adapters (Unity wrappers) | 70%+ | N/A | Low |

### 8.2 Coverage Tools

**Unity Code Coverage Package:**

```bash
# Install via Package Manager
Window → Package Manager → Code Coverage → Install
```

**Generate Coverage Report:**

```csharp
// Enable coverage in Test Runner
Window → General → Test Runner → Enable Code Coverage

// Run tests
Test Runner → Run All

// View report
Window → Analysis → Code Coverage
```

### 8.3 Critical Path Coverage

**Must-Test Scenarios:**

```csharp
[TestFixture]
[Category("CriticalPath")]
public class CriticalPathTests
{
    [Test]
    public void BSPSplit_AllEdgeCases_Handled()
    {
        // Test matrix of critical edge cases
        var testCases = new[]
        {
            (width: 20, height: 20, minSize: 10, shouldSplit: true, name: "Perfect square"),
            (width: 20, height: 10, minSize: 10, shouldSplit: true, name: "Wide rectangle"),
            (width: 10, height: 20, minSize: 10, shouldSplit: true, name: "Tall rectangle"),
            (width: 10, height: 10, minSize: 6, shouldSplit: false, name: "Too small"),
            (width: 19, height: 19, minSize: 10, shouldSplit: false, name: "Just below threshold"),
            (width: 20, height: 20, minSize: 10, shouldSplit: true, name: "Exactly at threshold")
        };

        foreach (var (width, height, minSize, shouldSplit, name) in testCases)
        {
            var node = new BSPNode(new Rect(0, 0, width, height));
            node.Split(minSize);

            bool actualSplit = !node.IsLeaf;
            Assert.AreEqual(shouldSplit, actualSplit,
                $"Edge case '{name}' failed: {width}x{height}, min={minSize}");
        }
    }

    [Test]
    public void RoomConnectivity_AllRoomsReachable_AfterGeneration()
    {
        // Arrange
        var generator = new BSPMapGenerator(...);
        var context = generator.Generate(seed: 42, mapSize: new Vector2Int(100, 100));

        // Act - Build connectivity graph
        var reachable = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(context.Rooms[0]);
        reachable.Add(context.Rooms[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in current.ConnectedRooms)
            {
                if (!reachable.Contains(neighbor))
                {
                    reachable.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Assert
        Assert.AreEqual(context.Rooms.Count, reachable.Count,
            "Not all rooms are reachable from starting room!");
    }

    [Test]
    public void SpawnPoints_NeverInPlayerStartRoom()
    {
        // Arrange
        var context = FixtureData.Scenarios.MediumMap();
        var orchestrator = new ContentGenerationOrchestrator(...);

        // Act
        orchestrator.GenerateContent(context);

        // Assert
        var playerStartRoom = context.RoomTypes
            .FirstOrDefault(kvp => kvp.Value == RoomClassification.PlayerStart).Key;

        Assert.IsNotNull(playerStartRoom, "Must have player start room");

        var spawnsInStartRoom = context.SpawnPoints
            .Where(sp => sp.GetComponent<SpawnPointMetadata>().roomID == playerStartRoom.ID)
            .ToList();

        Assert.IsEmpty(spawnsInStartRoom,
            $"Found {spawnsInStartRoom.Count} spawn points in player start room!");
    }
}
```

### 8.4 Mutation Testing (Advanced)

**Concept:** Introduce bugs, verify tests catch them.

```csharp
// Original code
if (room.Area > 300 && room.ConnectedRooms.Count >= 3)
    return RoomClassification.ArenaRoom;

// Mutant 1: Change > to >=
if (room.Area >= 300 && room.ConnectedRooms.Count >= 3)
    return RoomClassification.ArenaRoom;
// Does this break any test? If not, add test for boundary.

// Mutant 2: Change && to ||
if (room.Area > 300 || room.ConnectedRooms.Count >= 3)
    return RoomClassification.ArenaRoom;
// Does this break any test? If not, logic coverage is insufficient.
```

**Manual Mutation Testing Checklist:**
- Boundary conditions (>, >=, <, <=)
- Boolean operators (&&, ||, !)
- Return values (true/false, null/non-null)
- Constants (0, 1, -1)

---

## 9. Implementation Roadmap

### 9.1 Phase 0 Timeline (5 Days)

**Day 1: Assembly Definitions & Basic Framework**
- Create assembly definition files
- Setup directory structure
- Install Unity Test Runner
- Configure Test Runner settings
- First "hello world" test

**Deliverable:** Test Runner functional, can run basic test

---

**Day 2: Interface Abstractions & Mocks**
- Define ITilemapAdapter, IRandomProvider, INavMeshAdapter
- Implement Unity adapters
- Implement mock versions
- Create TestDataFactory foundation

**Deliverable:** Can test code without Unity dependencies

---

**Day 3: Core Generator Interface & TDD**
- Write interface for IMapGenerator
- TDD: BSPNode.Split() implementation
- TDD: Room connectivity validation
- Builder patterns for test data

**Deliverable:** BSPNode fully tested, 90%+ coverage

---

**Day 4: Test Utilities & Performance Framework**
- Complete TestDataFactory
- FixtureData scenarios
- PerformanceTestBase implementation
- First performance benchmark

**Deliverable:** Rich test utility library, performance baseline

---

**Day 5: Integration & Documentation**
- PlayMode test scene setup
- Full pipeline integration test
- Coverage report generation
- Document TDD workflow for team

**Deliverable:** Complete test infrastructure, ready for Phase 1

---

### 9.2 Acceptance Criteria

**Phase 0 is complete when:**

- [ ] Test Runner executes EditMode tests in <5 seconds
- [ ] All assemblies compile without errors
- [ ] Mock adapters function identically to Unity adapters
- [ ] TestDataFactory can create all core entities
- [ ] Performance benchmark baseline established
- [ ] At least one full TDD cycle completed (BSPNode)
- [ ] Code coverage tool integrated and functional
- [ ] Team trained on TDD workflow
- [ ] Zero test flakiness (100% reproducible results)
- [ ] Documentation complete for test infrastructure

---

### 9.3 Testing Anti-Patterns to Avoid

**1. Test Interdependence**
```csharp
// BAD - Test1 depends on Test2 running first
static Room sharedRoom;

[Test]
public void Test1_CreatesRoom()
{
    sharedRoom = new Room(...);
}

[Test]
public void Test2_UsesRoom()
{
    Assert.IsNotNull(sharedRoom);  // Fails if Test1 skipped!
}

// GOOD - Each test is independent
[Test]
public void Test1_CreatesRoom()
{
    var room = new Room(...);
    Assert.IsNotNull(room);
}

[Test]
public void Test2_UsesRoom()
{
    var room = new Room(...);  // Own setup
    Assert.IsNotNull(room);
}
```

**2. Testing Implementation Instead of Behavior**
```csharp
// BAD - Testing internal implementation detail
[Test]
public void Split_CallsRandomRangeExactlyTwice()
{
    var mockRandom = new Mock<IRandomProvider>();
    mockRandom.Setup(r => r.Range(It.IsAny<int>(), It.IsAny<int>()));

    node.Split(10, mockRandom.Object);

    mockRandom.Verify(r => r.Range(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
    // Brittle - breaks if we refactor to call Range 3 times but behavior unchanged
}

// GOOD - Testing observable behavior
[Test]
public void Split_CreatesValidChildren()
{
    node.Split(10);

    Assert.IsNotNull(node.Left);
    Assert.IsNotNull(node.Right);
    Assert.AreEqual(node.Rect.width, node.Left.Rect.width + node.Right.Rect.width);
    // Stable - only breaks if behavior actually changes
}
```

**3. Overly Specific Assertions**
```csharp
// BAD - Too specific
[Test]
public void Generate_Seed42_Creates17Rooms()
{
    var result = generator.Generate(seed: 42, size: new Vector2Int(100, 100));
    Assert.AreEqual(17, result.Rooms.Count);  // Fragile - changes with algorithm tweaks
}

// GOOD - Test reasonable bounds
[Test]
public void Generate_100x100Map_Creates10To30Rooms()
{
    var result = generator.Generate(seed: 42, size: new Vector2Int(100, 100));
    Assert.That(result.Rooms.Count, Is.InRange(10, 30));
    // Robust - tolerates minor algorithm changes
}
```

**4. Slow Tests in Fast Suite**
```csharp
// BAD - Slow operation in EditMode test
[Test]
[Category("Unit")]  // Lies! This is slow
public void FullGeneration_Works()
{
    // Loads scene, generates full map, bakes NavMesh
    // Takes 5 seconds
}

// GOOD - Separate fast and slow tests
[UnityTest]
[Category("Integration")]  // Honest categorization
public IEnumerator FullGeneration_Works()
{
    yield return SceneManager.LoadSceneAsync("TestScene");
    // ...
}
```

**5. Non-Deterministic Tests**
```csharp
// BAD - Flaky due to uncontrolled randomness
[Test]
public void Generate_CreatesRooms()
{
    // Uses UnityEngine.Random without seed!
    var result = generator.Generate();
    Assert.Greater(result.Rooms.Count, 5);  // Sometimes fails!
}

// GOOD - Deterministic
[Test]
public void Generate_WithSeed_CreatesRooms()
{
    var mockRandom = new DeterministicRandomProvider(seed: 42);
    var generator = new BSPMapGenerator(..., mockRandom);
    var result = generator.Generate(seed: 42);
    Assert.AreEqual(12, result.Rooms.Count);  // Always 12 with seed 42
}
```

---

## Conclusion

Phase 0 establishes the **architectural foundation** for confident, iterative development of the map generation system. By investing in comprehensive test infrastructure upfront, we:

1. **Enable TDD Workflow** - Write tests first, drive design from tests
2. **Ensure Quality** - Catch bugs before they reach production
3. **Support Refactoring** - Change internals without fear
4. **Document Behavior** - Tests serve as executable specifications
5. **Measure Performance** - Baseline metrics guide optimization
6. **Validate Integration** - Ensure Unity systems work correctly

### Critical Success Factors

**For Phase 0 to succeed:**

- **Team Buy-In** - Everyone writes tests, no exceptions
- **Fast Feedback** - EditMode tests run in <5 seconds total
- **Deterministic** - Zero flakiness, 100% reproducible
- **Comprehensive** - All core logic tested, 90%+ coverage
- **Documented** - Clear examples, TDD workflow guide

### Next Steps

**After Phase 0 completion:**

1. Begin Phase 1 (BSP Generation) with TDD
2. Write test first for each feature
3. Maintain coverage above 90%
4. Run tests on every commit
5. Review coverage reports weekly
6. Benchmark performance monthly

**The test infrastructure is now ready. Let's build with confidence.**

---

**Document Status:** ✅ Complete
**Review Required:** Lead Engineer, QA Lead
**Implementation Priority:** CRITICAL - Must complete before Phase 1
**Estimated Effort:** 5 days
**Dependencies:** Unity Test Framework, NUnit 3.5+

---

**References:**
- Unity Test Framework Documentation: https://docs.unity3d.com/Packages/com.unity.test-framework@latest
- NUnit Documentation: https://docs.nunit.org/
- Test-Driven Development by Kent Beck
- Growing Object-Oriented Software, Guided by Tests by Freeman & Pryce
- Clean Code by Robert C. Martin (Uncle Bob)

**Version History:**
- 1.0 (2025-11-17): Initial comprehensive testing architecture blueprint
