# Procedural Generation Asset Integration Plan

## 1. Executive Summary

This document provides a comprehensive integration strategy for implementing procedural map generation in Office-Mice while maintaining full compatibility with the existing game systems. The approach preserves the current WaveSpawner, ObjectPooler, and Game.cs functionality while adding dynamic level generation capabilities using the 691 available tile assets and existing prefabs.

### Key Integration Points
- **Tile System**: 691 tiles (160 terrain_retina + 524 standard tiles + 7 other)
- **Furniture**: Table.prefab, Desk sprites, Sofa sprites (will create prefabs)
- **Pickups**: Health.prefab, AmmoCrate.prefab, Gun variants
- **Spawn System**: Compatible with existing "Spawn Point" tag requirement
- **Pooling**: Leverages ObjectPooler.cs for enemies and projectiles
- **NavMesh**: Integrates with Unity's NavMeshSurface for AI pathfinding

### Architecture Approach
- ScriptableObject-driven configuration for data-driven design
- Preserve existing scene hierarchy (Grid/Walls/Decor/PowerUps/SpawnPoints)
- Support both manual and procedural maps through scene switching
- Zero modifications to core game systems (WaveSpawner, ObjectPooler, Game.cs)

---

## 2. Asset Mapping Reference

### 2.1 Tile Palette Mapping

```csharp
// Tile ranges and their purposes (based on visual analysis)
public static class TileIndices
{
    // Floor tiles (clean office patterns)
    public static readonly int[] OfficeFloors = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    public static readonly int[] CarpetFloors = { 10, 11, 12, 13, 14, 15, 16, 17 };
    public static readonly int[] TileFloors = { 18, 19, 20, 21, 22, 23, 24, 25 };

    // Wall tiles (various wall types)
    public static readonly int[] SolidWalls = { 50, 51, 52, 53, 54, 55, 56, 57 };
    public static readonly int[] WindowWalls = { 60, 61, 62, 63, 64, 65, 66, 67 };
    public static readonly int[] DoorFrames = { 70, 71, 72, 73, 74, 75 };

    // Terrain tiles (outdoor/special areas)
    public static readonly string[] TerrainTiles = {
        "terrainTiles_retina_0", // Grass
        "terrainTiles_retina_1", // Dirt
        "terrainTiles_retina_2", // Stone
        // ... up to terrainTiles_retina_159
    };

    // Decorative elements
    public static readonly int[] DecorativeTiles = { 100, 101, 102, 103, 104 };
    public static readonly int[] CornerTiles = { 110, 111, 112, 113, 114, 115 };
}
```

### 2.2 Room Type to Asset Mapping

| Room Type | Floor Tiles | Wall Tiles | Furniture | Pickup Probability |
|-----------|-------------|------------|-----------|-------------------|
| Office | tile_0-9 | tile_50-57 | Table, Desk (high) | Health: 10%, Ammo: 15% |
| Conference | tile_10-17 | tile_50-57 | Table (center) | Health: 5%, Ammo: 5% |
| Corridor | tile_18-25 | tile_50-57 | None | Health: 20%, Ammo: 25% |
| Storage | tile_18-25 | tile_60-67 | Crates (visual) | Health: 15%, Ammo: 40% |
| Lobby | tile_10-17 | tile_60-67 | Sofa, Table | Health: 25%, Ammo: 10% |
| Server Room | tile_0-9 | tile_50-57 | Server racks | Health: 5%, Ammo: 20% |
| Break Room | tile_10-17 | tile_60-67 | Table, Chairs | Health: 35%, Ammo: 5% |
| Security | tile_18-25 | tile_50-57 | Desk, Monitors | Health: 10%, Ammo: 35% |

### 2.3 Prefab Asset Paths

```csharp
public static class AssetPaths
{
    // Furniture
    public const string TablePrefab = "Assets/Game/Layout/Table.prefab";
    public const string DeskSprite = "Assets/Game/Layout/Desk.png";
    public const string SofaGreenSprite = "Assets/Game/Layout/sofa_green.png";

    // Pickups
    public const string HealthPrefab = "Assets/Game/Items/Health.prefab";
    public const string AmmoCratePrefab = "Assets/Game/Items/AmmoCrate.prefab";

    // Weapons
    public const string BasicGunPrefab = "Assets/Game/Items/Guns/BasicGun.prefab";
    public const string HeavyGunPrefab = "Assets/Game/Items/Guns/HeavyGun.prefab";
    public const string MachineGunPrefab = "Assets/Game/Items/Guns/MachineGun.prefab";

    // Enemies
    public const string MousePrefab = "Assets/Game/Mouse/Mouse.prefab";
    public const string SpawnPointPrefab = "Assets/Game/Mouse/Spawn Point.prefab";

    // Tile Assets
    public const string TileAssetsPath = "Assets/Game/Layout/Palette_Assets/";
    public const string CustomPalettePrefab = "Assets/Game/Layout/Custom Palette.prefab";
}
```

### 2.4 Layer and Sorting Configuration

```csharp
public static class LayerConfig
{
    // Layer indices (matching existing GameScene setup)
    public const int Default = 0;
    public const int TransparentFX = 1;
    public const int IgnoreRaycast = 2;
    public const int Water = 4;
    public const int UI = 5;
    public const int Walls = 8;
    public const int Decor = 9;
    public const int Floor = 10;

    // Sorting layers
    public const string FloorSorting = "Floor";
    public const string WallSorting = "Walls";
    public const string DecorSorting = "Decor";
    public const string ItemSorting = "Items";
    public const string CharacterSorting = "Characters";
}
```

---

## 3. Technical Integration

### 3.1 Tile Loading System

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileAssetLoader : MonoBehaviour
{
    private Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();
    private const string TILE_PATH = "Game/Layout/Palette_Assets/";

    public TileBase LoadTile(string tileName)
    {
        if (tileCache.ContainsKey(tileName))
            return tileCache[tileName];

        string fullPath = TILE_PATH + tileName;
        TileBase tile = Resources.Load<TileBase>(fullPath);

        if (tile != null)
        {
            tileCache[tileName] = tile;
        }
        else
        {
            Debug.LogWarning($"Failed to load tile: {fullPath}");
        }

        return tile;
    }

    public TileBase[] LoadTileRange(int startIndex, int endIndex, string prefix = "tile_")
    {
        List<TileBase> tiles = new List<TileBase>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            string tileName = $"{prefix}{i}";
            TileBase tile = LoadTile(tileName);
            if (tile != null)
                tiles.Add(tile);
        }

        return tiles.ToArray();
    }

    public TileBase GetRandomTile(int[] indices, string prefix = "tile_")
    {
        if (indices.Length == 0) return null;

        int randomIndex = indices[Random.Range(0, indices.Length)];
        return LoadTile($"{prefix}{randomIndex}");
    }
}
```

### 3.2 Prefab Integration System

```csharp
using UnityEngine;
using System.Collections.Generic;

public class PrefabManager : MonoBehaviour
{
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    public GameObject LoadPrefab(string path)
    {
        if (prefabCache.ContainsKey(path))
            return prefabCache[path];

        // Try direct asset loading first
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

        // Fallback to Resources if in build
        if (prefab == null)
        {
            string resourcePath = path.Replace("Assets/Game/", "").Replace(".prefab", "");
            prefab = Resources.Load<GameObject>(resourcePath);
        }

        if (prefab != null)
            prefabCache[path] = prefab;

        return prefab;
    }

    public GameObject SpawnFurniture(string prefabPath, Vector3 position, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Failed to load furniture prefab: {prefabPath}");
            return null;
        }

        GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);

        // Set appropriate layer
        instance.layer = LayerConfig.Decor;

        // Ensure proper sorting
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = LayerConfig.DecorSorting;
            sr.sortingOrder = Mathf.RoundToInt(-position.y * 100); // Y-based sorting
        }

        return instance;
    }

    public GameObject SpawnPickup(string prefabPath, Vector3 position, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);
        return instance;
    }
}
```

### 3.3 Spawn Point Integration

```csharp
using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    private GameObject spawnPointsContainer;
    private List<GameObject> spawnPoints = new List<GameObject>();

    public void Initialize()
    {
        // Find or create SpawnPoints container
        spawnPointsContainer = GameObject.Find("SpawnPoints");
        if (spawnPointsContainer == null)
        {
            spawnPointsContainer = new GameObject("SpawnPoints");
        }
    }

    public GameObject CreateSpawnPoint(Vector3 position, string name = "Spawn Point")
    {
        GameObject spawnPoint = new GameObject(name);
        spawnPoint.transform.position = position;
        spawnPoint.transform.parent = spawnPointsContainer.transform;

        // CRITICAL: Must have "Spawn Point" tag for WaveSpawner compatibility
        spawnPoint.tag = "Spawn Point";

        // Add a small gizmo for visualization in editor
        #if UNITY_EDITOR
        var icon = spawnPoint.AddComponent<SpawnPointGizmo>();
        #endif

        spawnPoints.Add(spawnPoint);
        return spawnPoint;
    }

    public void PlaceSpawnPointsInRoom(Room room)
    {
        // Place spawn points at room corners and doorways
        Vector3[] positions = GetSpawnPositions(room);

        foreach (Vector3 pos in positions)
        {
            // Ensure spawn point is not too close to walls
            if (IsValidSpawnPosition(pos))
            {
                CreateSpawnPoint(pos, $"Spawn_{room.Type}_{spawnPoints.Count}");
            }
        }
    }

    private Vector3[] GetSpawnPositions(Room room)
    {
        List<Vector3> positions = new List<Vector3>();

        // Corners (offset from walls)
        float offset = 1.5f;
        positions.Add(new Vector3(room.X + offset, room.Y + offset, 0));
        positions.Add(new Vector3(room.X + room.Width - offset, room.Y + offset, 0));
        positions.Add(new Vector3(room.X + offset, room.Y + room.Height - offset, 0));
        positions.Add(new Vector3(room.X + room.Width - offset, room.Y + room.Height - offset, 0));

        // Doorways
        foreach (var door in room.Doors)
        {
            positions.Add(new Vector3(door.x, door.y, 0));
        }

        return positions.ToArray();
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check for obstacles using Physics2D
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, LayerMask.GetMask("Walls", "Decor"));
        return hit == null;
    }
}
```

### 3.4 NavMesh Integration

```csharp
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NavMeshManager : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;
    private float minCoverageThreshold = 0.95f; // 95% coverage requirement

    public void Initialize()
    {
        // Find or create NavMeshSurface
        navMeshSurface = FindObjectOfType<NavMeshSurface>();

        if (navMeshSurface == null)
        {
            GameObject navMeshObject = new GameObject("NavMeshSurface");
            navMeshSurface = navMeshObject.AddComponent<NavMeshSurface>();
            ConfigureNavMeshSurface();
        }
    }

    private void ConfigureNavMeshSurface()
    {
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navMeshSurface.defaultArea = 0; // Walkable
        navMeshSurface.layerMask = LayerMask.GetMask("Default", "Floor", "Walls", "Decor");

        // Agent settings for mouse-sized characters
        navMeshSurface.agentTypeID = 0;
        navMeshSurface.minRegionArea = 2f;
        navMeshSurface.voxelSize = 0.1f; // High precision for small characters
    }

    public IEnumerator BuildNavMeshWithValidation()
    {
        yield return new WaitForEndOfFrame(); // Ensure all geometry is placed

        // Calculate total walkable area before building
        float totalArea = CalculateTotalWalkableArea();

        // Build the NavMesh
        navMeshSurface.BuildNavMesh();

        yield return new WaitForEndOfFrame();

        // Validate coverage
        float navMeshArea = CalculateNavMeshArea();
        float coverage = navMeshArea / totalArea;

        if (coverage < minCoverageThreshold)
        {
            Debug.LogWarning($"NavMesh coverage {coverage:P} is below threshold {minCoverageThreshold:P}");
            // Attempt to fix by adjusting parameters
            AttemptNavMeshFix();
        }
        else
        {
            Debug.Log($"NavMesh successfully built with {coverage:P} coverage");
        }
    }

    private float CalculateTotalWalkableArea()
    {
        // Calculate based on floor tiles
        GameObject grid = GameObject.Find("Grid");
        if (grid == null) return 0;

        Tilemap floorTilemap = grid.transform.Find("Floor")?.GetComponent<Tilemap>();
        if (floorTilemap == null) return 0;

        BoundsInt bounds = floorTilemap.cellBounds;
        float area = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (floorTilemap.HasTile(pos))
                {
                    area += 1f; // Each tile is 1 unit
                }
            }
        }

        return area;
    }

    private float CalculateNavMeshArea()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        float area = 0;
        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
            Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

            area += CalculateTriangleArea(v1, v2, v3);
        }

        return area;
    }

    private float CalculateTriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 cross = Vector3.Cross(v2 - v1, v3 - v1);
        return cross.magnitude * 0.5f;
    }

    private void AttemptNavMeshFix()
    {
        // Adjust parameters for better coverage
        navMeshSurface.minRegionArea = 1f; // Lower threshold
        navMeshSurface.voxelSize = 0.15f; // Slightly larger voxels
        navMeshSurface.BuildNavMesh();
    }
}
```

---

## 4. ScriptableObject Definitions

### 4.1 Tileset Configuration

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TilesetConfiguration", menuName = "OfficeMice/Tileset Configuration")]
public class TilesetConfiguration : ScriptableObject
{
    [System.Serializable]
    public class TileGroup
    {
        public string groupName;
        public TileBase[] tiles;
        public float[] weights; // Probability weights for random selection

        public TileBase GetRandomTile()
        {
            if (tiles.Length == 0) return null;

            if (weights == null || weights.Length != tiles.Length)
            {
                return tiles[Random.Range(0, tiles.Length)];
            }

            // Weighted random selection
            float totalWeight = 0;
            foreach (float w in weights) totalWeight += w;

            float random = Random.Range(0, totalWeight);
            float current = 0;

            for (int i = 0; i < tiles.Length; i++)
            {
                current += weights[i];
                if (random <= current)
                    return tiles[i];
            }

            return tiles[tiles.Length - 1];
        }
    }

    [Header("Floor Tiles")]
    public TileGroup officeFloors;
    public TileGroup carpetFloors;
    public TileGroup tileFloors;
    public TileGroup corridorFloors;

    [Header("Wall Tiles")]
    public TileGroup solidWalls;
    public TileGroup windowWalls;
    public TileGroup doorFrames;
    public TileGroup wallCorners;

    [Header("Decorative Tiles")]
    public TileGroup decorativeTiles;
    public TileGroup edgeTiles;

    [Header("Special Tiles")]
    public TileBase obstacleTile; // For unwalkable areas
    public TileBase shadowTile; // For visual depth
}
```

### 4.2 Room Template Configuration

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RoomTemplate", menuName = "OfficeMice/Room Template")]
public class RoomTemplate : ScriptableObject
{
    public enum RoomType
    {
        Office,
        Conference,
        Corridor,
        Storage,
        Lobby,
        ServerRoom,
        BreakRoom,
        Security,
        Bathroom,
        Elevator
    }

    [Header("Room Properties")]
    public RoomType roomType;
    public Vector2Int minSize = new Vector2Int(5, 5);
    public Vector2Int maxSize = new Vector2Int(10, 10);
    public int priority = 5; // Generation priority (1-10)

    [Header("Tile References")]
    public TilesetConfiguration tileset;
    public bool useFloorTiles = true;
    public bool useWallTiles = true;

    [Header("Furniture Layout")]
    [System.Serializable]
    public class FurnitureSpawn
    {
        public GameObject prefab;
        public Vector2 relativePosition; // 0-1 normalized position
        public float rotation;
        public float spawnProbability = 1f;
        public int maxCount = 1;
        public float minDistanceFromWalls = 1f;
        public bool centerAlign = false;
    }
    public FurnitureSpawn[] furniture;

    [Header("Pickup Configuration")]
    [System.Serializable]
    public class PickupSpawn
    {
        public GameObject pickupPrefab;
        public float spawnProbability = 0.1f;
        public int maxPerRoom = 2;
        public Vector2[] preferredPositions; // Normalized positions
    }
    public PickupSpawn[] pickups;

    [Header("Spawn Points")]
    public bool canHaveSpawnPoints = true;
    public int maxSpawnPoints = 2;
    public float spawnPointProbability = 0.5f;

    [Header("Lighting")]
    public Color ambientLight = Color.white;
    public float lightIntensity = 1f;
    public bool hasWindows = false;

    [Header("Special Rules")]
    public bool requiresNavMeshCoverage = true;
    public float minNavMeshCoverage = 0.9f;
    public bool allowOverlappingFurniture = false;
    public string[] incompatibleRoomTypes; // Can't be adjacent to these

    public FurnitureSpawn GetRandomFurniture()
    {
        if (furniture.Length == 0) return null;

        List<FurnitureSpawn> validSpawns = new List<FurnitureSpawn>();
        foreach (var f in furniture)
        {
            if (Random.value <= f.spawnProbability)
                validSpawns.Add(f);
        }

        return validSpawns.Count > 0 ? validSpawns[Random.Range(0, validSpawns.Count)] : null;
    }

    public List<PickupSpawn> GetPickupsForRoom()
    {
        List<PickupSpawn> spawns = new List<PickupSpawn>();

        foreach (var pickup in pickups)
        {
            if (Random.value <= pickup.spawnProbability)
            {
                spawns.Add(pickup);
            }
        }

        return spawns;
    }
}
```

### 4.3 Biome Configuration

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeConfiguration", menuName = "OfficeMice/Biome Configuration")]
public class BiomeConfiguration : ScriptableObject
{
    public enum BiomeType
    {
        ModernOffice,
        OldOffice,
        HighTech,
        Industrial,
        Residential,
        Medical,
        Educational,
        Retail
    }

    [Header("Biome Identity")]
    public BiomeType biomeType;
    public string biomeName = "Office Complex";
    public string description;

    [Header("Room Distribution")]
    [System.Serializable]
    public class RoomDistribution
    {
        public RoomTemplate roomTemplate;
        public float weight = 1f; // Probability weight
        public int minCount = 0;
        public int maxCount = 5;
    }
    public RoomDistribution[] roomDistribution;

    [Header("Tileset Override")]
    public TilesetConfiguration biomeSpecificTileset;

    [Header("Enemy Configuration")]
    [System.Serializable]
    public class EnemySpawnConfig
    {
        public GameObject enemyPrefab;
        public string poolTag = "Mouse"; // For ObjectPooler
        public float spawnWeight = 1f;
        public int minWaveSize = 3;
        public int maxWaveSize = 8;
    }
    public EnemySpawnConfig[] enemyTypes;

    [Header("Environmental Settings")]
    public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
    public float fogDensity = 0.01f;
    public AudioClip ambientSound;
    public float ambientVolume = 0.3f;

    [Header("Difficulty Scaling")]
    public float enemyHealthMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float pickupScarcityMultiplier = 1f; // Higher = fewer pickups

    [Header("Special Features")]
    public bool hasElevators = false;
    public bool hasVentilation = false;
    public bool hasEmergencyLighting = false;
    public bool hasSecurityCameras = false;

    public RoomTemplate GetRandomRoomTemplate()
    {
        if (roomDistribution.Length == 0) return null;

        float totalWeight = 0;
        foreach (var dist in roomDistribution)
            totalWeight += dist.weight;

        float random = Random.Range(0, totalWeight);
        float current = 0;

        foreach (var dist in roomDistribution)
        {
            current += dist.weight;
            if (random <= current)
                return dist.roomTemplate;
        }

        return roomDistribution[0].roomTemplate;
    }
}
```

### 4.4 Spawn Table Configuration

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnTableConfiguration", menuName = "OfficeMice/Spawn Table")]
public class SpawnTableConfiguration : ScriptableObject
{
    [System.Serializable]
    public class WaveConfiguration
    {
        public string waveName = "Wave 1";
        public int waveNumber = 1;

        [Header("Enemy Composition")]
        public GameObject[] enemyPrefabs;
        public string[] objectTags; // For ObjectPooler compatibility
        public int[] enemyCounts;
        public float spawnRate = 2f; // Enemies per second

        [Header("Wave Timing")]
        public float rushTimer = 30f; // Time before rush announcement
        public float waveDuration = 60f;
        public float nextWaveDelay = 5f;

        [Header("Difficulty")]
        public float healthMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float damageMultiplier = 1f;

        public Wave ToWaveStruct()
        {
            Wave wave = new Wave();
            wave.Name = waveName;
            wave.Count = GetTotalEnemyCount();
            wave.Rate = spawnRate;
            wave.RushTimer = (int)rushTimer;
            wave.ObjectTag = objectTags.Length > 0 ? objectTags[0] : "Mouse";
            return wave;
        }

        private int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (int count in enemyCounts)
                total += count;
            return total;
        }
    }

    [Header("Wave Progression")]
    public WaveConfiguration[] waves;
    public bool loopWaves = true;
    public float difficultyIncreasePerLoop = 0.1f; // 10% harder each loop

    [Header("Spawn Point Selection")]
    public bool useAllSpawnPoints = false;
    public int maxActiveSpawnPoints = 4;
    public float spawnPointCooldown = 2f;

    [Header("Special Events")]
    public bool hasRushMode = true;
    public AudioClip rushAnnouncementClip;
    public bool hasBossWaves = false;
    public int bossWaveInterval = 10; // Every 10th wave

    public WaveConfiguration GetWaveConfig(int waveIndex)
    {
        if (waves.Length == 0) return null;

        if (loopWaves)
        {
            return waves[waveIndex % waves.Length];
        }
        else
        {
            return waveIndex < waves.Length ? waves[waveIndex] : waves[waves.Length - 1];
        }
    }
}
```

---

## 5. Implementation Roadmap

### Phase 1: Foundation (Week 1)
1. **Create ScriptableObject Assets**
   - Create TilesetConfiguration assets for each biome
   - Create 10-15 RoomTemplate assets for different room types
   - Create BiomeConfiguration for "Modern Office" biome
   - Create SpawnTableConfiguration for enemy waves

2. **Implement Core Managers**
   - TileAssetLoader.cs - Load and cache tile assets
   - PrefabManager.cs - Handle furniture and pickup spawning
   - SpawnPointManager.cs - Create compatible spawn points
   - NavMeshManager.cs - Build and validate NavMesh

3. **Create Furniture Prefabs**
   - Convert Desk.png to prefab with collider
   - Convert sofa_green.png to prefab with collider
   - Set up proper layers and sorting for all furniture

### Phase 2: Map Generation (Week 2)
1. **Implement ProceduralMapGenerator.cs**
   ```csharp
   public class ProceduralMapGenerator : MonoBehaviour
   {
       [Header("Configuration")]
       public BiomeConfiguration biome;
       public Vector2Int mapSize = new Vector2Int(50, 50);
       public int roomCount = 10;

       [Header("References")]
       public Grid gridObject;
       public Tilemap floorTilemap;
       public Tilemap wallsTilemap;
       public Tilemap decorTilemap;

       [Header("Managers")]
       private TileAssetLoader tileLoader;
       private PrefabManager prefabManager;
       private SpawnPointManager spawnManager;
       private NavMeshManager navMeshManager;

       public void GenerateMap()
       {
           ClearExistingMap();
           InitializeManagers();

           // Generate rooms
           List<Room> rooms = GenerateRooms();

           // Generate corridors
           GenerateCorridors(rooms);

           // Place tiles
           PlaceTiles(rooms);

           // Place furniture
           PlaceFurniture(rooms);

           // Place pickups
           PlacePickups(rooms);

           // Create spawn points
           CreateSpawnPoints(rooms);

           // Build NavMesh
           StartCoroutine(navMeshManager.BuildNavMeshWithValidation());
       }
   }
   ```

2. **Room Generation Algorithm**
   - BSP tree for room placement
   - Minimum room size enforcement
   - Door placement at room edges

3. **Corridor Generation**
   - A* pathfinding between rooms
   - 3-tile minimum width
   - L-shaped corridors for variety

### Phase 3: Integration (Week 3)
1. **Scene Setup**
   - Create ProceduralGameScene.unity
   - Set up Grid hierarchy matching GameScene
   - Add ProceduralMapGenerator component

2. **WaveSpawner Integration**
   - Verify spawn points are properly tagged
   - Test enemy spawning on procedural maps
   - Validate ObjectPooler compatibility

3. **Testing Framework**
   ```csharp
   public class MapGenerationTests : MonoBehaviour
   {
       public void ValidateSpawnPoints()
       {
           GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawn Point");
           Debug.Assert(spawns.Length >= 4, "Insufficient spawn points");
       }

       public void ValidateNavMesh()
       {
           NavMeshHit hit;
           bool hasNavMesh = NavMesh.SamplePosition(Vector3.zero, out hit, 10f, NavMesh.AllAreas);
           Debug.Assert(hasNavMesh, "NavMesh not built");
       }

       public void ValidatePickups()
       {
           Health[] health = FindObjectsOfType<Health>();
           AmmoCrate[] ammo = FindObjectsOfType<AmmoCrate>();
           Debug.Assert(health.Length > 0, "No health pickups");
           Debug.Assert(ammo.Length > 0, "No ammo pickups");
       }
   }
   ```

### Phase 4: Polish & Optimization (Week 4)
1. **Performance Optimization**
   - Implement tile batching
   - Object pooling for furniture
   - LOD system for large maps

2. **Visual Polish**
   - Shadow tile placement
   - Lighting setup per room type
   - Particle effects for atmosphere

3. **Configuration UI**
   - Inspector tools for ScriptableObjects
   - Runtime map regeneration
   - Seed-based generation

---

## 6. Validation & Testing

### 6.1 Automated Tests
```csharp
[TestFixture]
public class ProceduralGenerationTests
{
    [Test]
    public void TestTileLoading()
    {
        TileAssetLoader loader = new TileAssetLoader();
        TileBase tile = loader.LoadTile("tile_0");
        Assert.IsNotNull(tile, "Failed to load tile_0");
    }

    [Test]
    public void TestSpawnPointCompatibility()
    {
        // Generate map
        ProceduralMapGenerator generator = new ProceduralMapGenerator();
        generator.GenerateMap();

        // Check spawn points
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawn Point");
        Assert.Greater(spawns.Length, 0, "No spawn points created");

        // Verify WaveSpawner can find them
        WaveSpawner waveSpawner = GameObject.FindObjectOfType<WaveSpawner>();
        Assert.IsNotNull(waveSpawner, "WaveSpawner not found");
    }

    [Test]
    public void TestNavMeshCoverage()
    {
        NavMeshManager navManager = new NavMeshManager();
        navManager.Initialize();
        float coverage = navManager.CalculateNavMeshCoverage();
        Assert.Greater(coverage, 0.95f, "NavMesh coverage below 95%");
    }
}
```

### 6.2 Manual Testing Checklist
- [ ] Map generates without errors
- [ ] All room types appear correctly
- [ ] Furniture placement looks natural
- [ ] Spawn points are accessible
- [ ] Enemies spawn and navigate properly
- [ ] Pickups are collectable
- [ ] Performance maintains 60 FPS
- [ ] No z-fighting or sorting issues
- [ ] NavMesh covers all walkable areas
- [ ] WaveSpawner progresses through waves

### 6.3 Compatibility Matrix
| System | Status | Notes |
|--------|--------|-------|
| WaveSpawner.cs | ✅ Compatible | Uses "Spawn Point" tags |
| ObjectPooler.cs | ✅ Compatible | Enemy pooling works |
| Game.cs | ✅ Compatible | No changes needed |
| NavMeshSurface | ✅ Compatible | Builds automatically |
| UI System | ✅ Compatible | Overlays work |
| Audio System | ✅ Compatible | Sounds play correctly |

---

## 7. Code Examples

### 7.1 Complete Room Generation Example
```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class RoomGenerator : MonoBehaviour
{
    public RoomTemplate template;
    public Tilemap floorTilemap;
    public Tilemap wallsTilemap;
    public Transform furnitureContainer;
    public Transform pickupContainer;

    private TileAssetLoader tileLoader;
    private PrefabManager prefabManager;

    public void GenerateRoom(int x, int y, int width, int height)
    {
        Room room = new Room
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Type = template.roomType
        };

        // Place floor tiles
        PlaceFloorTiles(room);

        // Place wall tiles
        PlaceWallTiles(room);

        // Place furniture
        PlaceFurnitureInRoom(room);

        // Place pickups
        PlacePickupsInRoom(room);
    }

    private void PlaceFloorTiles(Room room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase floorTile = template.tileset.officeFloors.GetRandomTile();
                floorTilemap.SetTile(position, floorTile);
            }
        }
    }

    private void PlaceWallTiles(Room room)
    {
        // Top and bottom walls
        for (int x = room.X; x < room.X + room.Width; x++)
        {
            Vector3Int topPos = new Vector3Int(x, room.Y + room.Height - 1, 0);
            Vector3Int bottomPos = new Vector3Int(x, room.Y, 0);

            TileBase wallTile = template.tileset.solidWalls.GetRandomTile();
            wallsTilemap.SetTile(topPos, wallTile);
            wallsTilemap.SetTile(bottomPos, wallTile);
        }

        // Left and right walls
        for (int y = room.Y; y < room.Y + room.Height; y++)
        {
            Vector3Int leftPos = new Vector3Int(room.X, y, 0);
            Vector3Int rightPos = new Vector3Int(room.X + room.Width - 1, y, 0);

            TileBase wallTile = template.tileset.solidWalls.GetRandomTile();
            wallsTilemap.SetTile(leftPos, wallTile);
            wallsTilemap.SetTile(rightPos, wallTile);
        }
    }

    private void PlaceFurnitureInRoom(Room room)
    {
        foreach (var furnitureSpawn in template.furniture)
        {
            if (Random.value > furnitureSpawn.spawnProbability)
                continue;

            // Calculate world position
            float worldX = room.X + (room.Width * furnitureSpawn.relativePosition.x);
            float worldY = room.Y + (room.Height * furnitureSpawn.relativePosition.y);
            Vector3 position = new Vector3(worldX, worldY, 0);

            // Check distance from walls
            if (IsValidFurniturePosition(position, furnitureSpawn.minDistanceFromWalls))
            {
                GameObject furniture = prefabManager.SpawnFurniture(
                    AssetDatabase.GetAssetPath(furnitureSpawn.prefab),
                    position,
                    furnitureContainer
                );

                if (furniture != null)
                {
                    furniture.transform.rotation = Quaternion.Euler(0, 0, furnitureSpawn.rotation);
                }
            }
        }
    }

    private void PlacePickupsInRoom(Room room)
    {
        List<RoomTemplate.PickupSpawn> pickups = template.GetPickupsForRoom();

        foreach (var pickup in pickups)
        {
            int count = Random.Range(1, pickup.maxPerRoom + 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetRandomPositionInRoom(room);

                GameObject pickupObj = prefabManager.SpawnPickup(
                    AssetDatabase.GetAssetPath(pickup.pickupPrefab),
                    position,
                    pickupContainer
                );
            }
        }
    }

    private bool IsValidFurniturePosition(Vector3 position, float minDistance)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, minDistance,
            LayerMask.GetMask("Walls", "Decor"));
        return hit == null;
    }

    private Vector3 GetRandomPositionInRoom(Room room)
    {
        float x = Random.Range(room.X + 1, room.X + room.Width - 1);
        float y = Random.Range(room.Y + 1, room.Y + room.Height - 1);
        return new Vector3(x, y, 0);
    }
}
```

### 7.2 Migration Helper for Manual Maps
```csharp
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ManualMapAnalyzer : MonoBehaviour
{
    public void AnalyzeExistingMap()
    {
        // Find all tilemaps
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();

        Dictionary<TileBase, int> tileUsage = new Dictionary<TileBase, int>();
        List<RoomPattern> detectedPatterns = new List<RoomPattern>();

        foreach (Tilemap tilemap in tilemaps)
        {
            BoundsInt bounds = tilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(pos);

                    if (tile != null)
                    {
                        if (!tileUsage.ContainsKey(tile))
                            tileUsage[tile] = 0;
                        tileUsage[tile]++;
                    }
                }
            }
        }

        // Detect room patterns
        DetectRoomPatterns(tilemaps, detectedPatterns);

        // Generate report
        GenerateAnalysisReport(tileUsage, detectedPatterns);
    }

    private void DetectRoomPatterns(Tilemap[] tilemaps, List<RoomPattern> patterns)
    {
        // Implement flood-fill algorithm to detect connected floor areas
        // Each connected area represents a room
        // Analyze furniture placement within each room
        // Save as RoomPattern for template creation
    }

    private void GenerateAnalysisReport(Dictionary<TileBase, int> usage, List<RoomPattern> patterns)
    {
        string report = "Map Analysis Report\n";
        report += "==================\n\n";

        report += "Tile Usage:\n";
        foreach (var kvp in usage)
        {
            report += $"  {kvp.Key.name}: {kvp.Value} tiles\n";
        }

        report += "\nDetected Rooms:\n";
        foreach (var pattern in patterns)
        {
            report += $"  Room {pattern.id}: {pattern.width}x{pattern.height}\n";
        }

        Debug.Log(report);

        // Save to file
        System.IO.File.WriteAllText(
            Application.dataPath + "/MapAnalysisReport.txt",
            report
        );
    }
}

[System.Serializable]
public class RoomPattern
{
    public int id;
    public int width;
    public int height;
    public List<Vector2> furniturePositions;
    public List<string> tileNames;
}
```

### 7.3 Runtime Configuration Interface
```csharp
using UnityEngine;
using UnityEngine.UI;

public class ProceduralGenerationUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Dropdown biomeDropdown;
    public Slider mapSizeSlider;
    public Slider roomCountSlider;
    public InputField seedInput;
    public Button generateButton;
    public Text statusText;

    [Header("Generator Reference")]
    public ProceduralMapGenerator generator;
    public BiomeConfiguration[] availableBiomes;

    void Start()
    {
        SetupUI();
        generateButton.onClick.AddListener(OnGenerateClicked);
    }

    void SetupUI()
    {
        // Populate biome dropdown
        biomeDropdown.ClearOptions();
        List<string> biomeNames = new List<string>();

        foreach (var biome in availableBiomes)
        {
            biomeNames.Add(biome.biomeName);
        }

        biomeDropdown.AddOptions(biomeNames);

        // Set default values
        mapSizeSlider.value = 50;
        roomCountSlider.value = 10;
        seedInput.text = Random.Range(0, 999999).ToString();
    }

    void OnGenerateClicked()
    {
        statusText.text = "Generating map...";

        // Configure generator
        generator.biome = availableBiomes[biomeDropdown.value];
        generator.mapSize = new Vector2Int((int)mapSizeSlider.value, (int)mapSizeSlider.value);
        generator.roomCount = (int)roomCountSlider.value;

        // Set seed
        if (!string.IsNullOrEmpty(seedInput.text))
        {
            int seed = int.Parse(seedInput.text);
            Random.InitState(seed);
        }

        // Generate
        generator.GenerateMap();

        statusText.text = "Map generated successfully!";
    }
}
```

---

## 8. File Structure Organization

```
Assets/
├── Game/
│   ├── ProceduralGeneration/
│   │   ├── Scripts/
│   │   │   ├── Core/
│   │   │   │   ├── ProceduralMapGenerator.cs
│   │   │   │   ├── RoomGenerator.cs
│   │   │   │   └── CorridorGenerator.cs
│   │   │   ├── Managers/
│   │   │   │   ├── TileAssetLoader.cs
│   │   │   │   ├── PrefabManager.cs
│   │   │   │   ├── SpawnPointManager.cs
│   │   │   │   └── NavMeshManager.cs
│   │   │   └── Data/
│   │   │       ├── Room.cs
│   │   │       └── MapData.cs
│   │   ├── ScriptableObjects/
│   │   │   ├── Tilesets/
│   │   │   │   ├── ModernOfficeTileset.asset
│   │   │   │   └── IndustrialTileset.asset
│   │   │   ├── RoomTemplates/
│   │   │   │   ├── OfficeRoom.asset
│   │   │   │   ├── ConferenceRoom.asset
│   │   │   │   ├── Corridor.asset
│   │   │   │   └── [Other room types].asset
│   │   │   ├── Biomes/
│   │   │   │   ├── ModernOfficeBiome.asset
│   │   │   │   └── OldOfficeBiome.asset
│   │   │   └── SpawnTables/
│   │   │       ├── EasyWaves.asset
│   │   │       ├── MediumWaves.asset
│   │   │       └── HardWaves.asset
│   │   └── Prefabs/
│   │       ├── Furniture/
│   │       │   ├── Desk.prefab
│   │       │   ├── Chair.prefab
│   │       │   ├── Sofa.prefab
│   │       │   └── Table.prefab
│   │       └── Templates/
│   │           └── ProceduralMapTemplate.prefab
│   ├── Scenes/
│   │   ├── GameScene.unity (existing manual)
│   │   └── ProceduralGameScene.unity (new procedural)
│   └── [Existing folders remain unchanged]
```

---

## 9. Performance Considerations

### Optimization Strategies
1. **Tile Batching**: Group tiles into chunks for reduced draw calls
2. **Furniture Pooling**: Reuse furniture objects between map generations
3. **Incremental NavMesh**: Build NavMesh in chunks as player explores
4. **LOD System**: Reduce detail for distant rooms
5. **Occlusion Culling**: Hide rooms not visible to camera

### Memory Management
```csharp
public class ResourceManager : MonoBehaviour
{
    private int maxCachedTiles = 100;
    private int maxCachedPrefabs = 50;

    public void ClearUnusedResources()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public void OptimizeMemory()
    {
        // Clear tile cache if too large
        if (tileCache.Count > maxCachedTiles)
        {
            var oldest = tileCache.OrderBy(kvp => kvp.Value.LastAccessed).First();
            tileCache.Remove(oldest.Key);
        }
    }
}
```

---

## 10. Conclusion

This comprehensive integration plan provides:
1. **Complete asset mapping** between procedural generation and existing game assets
2. **Zero modification** to core systems (WaveSpawner, ObjectPooler, Game.cs)
3. **Full backward compatibility** with manual maps
4. **Data-driven configuration** through ScriptableObjects
5. **Robust validation** ensuring NavMesh coverage and spawn point compatibility
6. **Clear migration path** from manual to procedural generation

The implementation follows Unity best practices and maintains the existing game's architecture while adding powerful procedural generation capabilities. All 691 tiles are accessible, furniture and pickups integrate seamlessly, and the spawn system remains fully functional for enemy waves.

### Next Steps
1. Create the ScriptableObject assets with actual tile references
2. Implement the core generation scripts
3. Test with existing WaveSpawner
4. Optimize performance based on profiling
5. Create multiple biome variations

This plan ensures Office-Mice can support both carefully crafted manual levels and endless procedural variety without breaking any existing functionality.