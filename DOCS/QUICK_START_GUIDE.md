# Office-Mice Procedural Map Generation - Quick Start Guide

**Last Updated:** 2025-11-17
**Branch:** feature/map-generation
**Status:** ✅ Planning Complete, Ready for Implementation

---

## 📚 What We Have

### **Complete Documentation Suite** (11 documents, ~520,000 words)

1. **Planning & Architecture**
   - `MAP_GENERATION_PLAN.md` - Original BSP + Room Templates plan
   - `AI_AGENT_EXECUTION_PLAN.md` - 15-day implementation roadmap
   - `ARCHITECTURE_MASTER_INDEX.md` - Navigation guide to all docs
   - `ASSET_INTEGRATION_PLAN.md` - Maps generation to existing assets ⭐ NEW

2. **Phase 0: Foundation** (3 docs)
   - Data architecture, testing foundation, system design

3. **Phase 1: Core Generation** (3 docs)
   - BSP algorithms, A* pathfinding, layout optimization, serialization

4. **Phase 2: Content & Features** (1 doc)
   - Spawn systems, resources, special rooms, biomes

5. **Phase 3: Polish & Integration** (2 docs)
   - Performance optimization, editor tools, CI/CD

---

## 🎮 Your Existing Assets (Ready to Use)

### **691 Tiles**
- 160 terrain tiles (terrainTiles_retina palette)
- 524 standard tiles (tile palette)
- 7 additional decorative tiles

### **Furniture Prefabs**
- Table.prefab
- Desk (to be converted from Desk.png)
- Sofa (to be converted from sofa_green.png)
- 4 Chair variants

### **Pickup Prefabs**
- Health.prefab
- AmmoCrate.prefab
- Food items (cake, popcorn, cookies, chips, coldbrew)

### **Weapons**
- BasicGun.prefab
- HeavyGun.prefab
- MachineGun.prefab

### **Enemy System**
- Mouse.prefab (enemy AI)
- ObjectPooler.cs (for efficient spawning)
- WaveSpawner.cs (wave-based combat)

---

## 🎯 What Gets Generated

### **Map Structure**
```
Procedurally Generated Office Map
├── Rooms (BSP algorithm)
│   ├── Start Room (player spawn)
│   ├── Combat Rooms (10-15 rooms)
│   ├── Loot Rooms (2-3 rooms)
│   ├── Break Room (1 room)
│   └── Boss Room (1 room)
├── Corridors (A* pathfinding)
│   ├── Main corridors (5 tiles wide)
│   └── Secondary corridors (3 tiles wide)
├── Furniture (procedurally placed)
│   ├── Desks in cubicle areas
│   ├── Tables in meeting rooms
│   ├── Chairs throughout
│   └── Sofas in break rooms
├── Spawn Points (for WaveSpawner)
│   ├── Enemy spawns in combat rooms
│   └── Boss spawn in boss room
└── Pickups (resource distribution)
    ├── Health pickups (scarce)
    ├── Ammo crates (moderate)
    └── Food items (common in break rooms)
```

### **Key Features**
- ✅ 100% room connectivity (all rooms reachable)
- ✅ >95% NavMesh coverage (AI pathfinding)
- ✅ Deterministic (same seed = same map)
- ✅ Scalable (50-500 rooms)
- ✅ Fast generation (<3 seconds)
- ✅ Compatible with existing systems (zero code changes to WaveSpawner/ObjectPooler)

---

## 🚀 How to Start Implementation

### **Option 1: Follow the 15-Day Roadmap** (Recommended)

```bash
# Read the execution plan
cat DOCS/AI_AGENT_EXECUTION_PLAN.md

# Start with Phase 0 (Days 1-2)
# Read these in order:
1. DOCS/PHASE_0_PART1_DATA_ARCHITECTURE.md
2. DOCS/PHASE_0_PART2_TESTING_FOUNDATION.md
3. DOCS/PHASE_0_PART3_SYSTEM_DESIGN.md

# Then implement:
- Core data models (MapData, RoomData, CorridorData)
- Testing framework setup
- Interface definitions
```

### **Option 2: Jump to Asset Integration** (Quick Start)

```bash
# Read the integration guide
cat DOCS/ASSET_INTEGRATION_PLAN.md

# Follow Week 1 implementation:
1. Create ScriptableObject configurations
2. Build TileAssetLoader
3. Build PrefabManager
4. Test asset loading
```

### **Option 3: Use AI Agents** (Automated)

```bash
# Use the agent execution plan
# Each task has specific agent assignments and prompts
# Example:

# Phase 0, Task 0.1: Architecture Design
Agent: backend-architect
Prompt: [See AI_AGENT_EXECUTION_PLAN.md, Task 0.1]

# Phase 0, Task 0.2: Core Data Models
Agent: backend-architect → frontend-developer
Prompt: [See AI_AGENT_EXECUTION_PLAN.md, Task 0.2]
```

---

## 📋 Implementation Checklist

### **Phase 0: Foundation (Days 1-2)** ⬜
- [ ] Create Assets/Scripts/MapGeneration/ directory structure
- [ ] Implement MapData, RoomData, CorridorData classes
- [ ] Create RoomTemplate ScriptableObject
- [ ] Set up Unity Test Framework
- [ ] Define core interfaces (IMapGenerator, IRoomGenerator, etc.)

### **Phase 1: Core Generation (Days 3-7)** ⬜
- [ ] Implement BSP room generation algorithm
- [ ] Create room connection system (MST)
- [ ] Implement A* corridor pathfinding
- [ ] Add layout optimization (force-directed + grid-snapping)
- [ ] Build map serialization (JSON/Binary)
- [ ] Write integration tests

### **Phase 2: Content & Features (Days 8-12)** ⬜
- [ ] Create spawn point generation system
- [ ] Implement resource distribution algorithm
- [ ] Build room template system
- [ ] Add biome variations
- [ ] Create furniture placement logic
- [ ] Integrate with WaveSpawner.cs

### **Phase 3: Polish & Integration (Days 13-15)** ⬜
- [ ] Profile and optimize performance
- [ ] Build custom editor window
- [ ] Add Gizmo visualization
- [ ] Implement object pooling
- [ ] Write end-to-end tests
- [ ] Deploy to Cloudflare Workers

---

## 🎨 Asset Integration Quick Reference

### **Tile Usage by Room Type**

| Room Type | Floor Tiles | Wall Tiles | Decor Tiles |
|-----------|-------------|------------|-------------|
| Start Room | terrainTiles 0-20 | tile 100-120 | tile 300-320 |
| Combat Room | terrainTiles 21-40 | tile 121-140 | tile 321-340 |
| Loot Room | terrainTiles 41-60 | tile 141-160 | tile 341-360 |
| Break Room | terrainTiles 61-80 | tile 161-180 | tile 361-380 |
| Boss Room | terrainTiles 81-100 | tile 181-200 | tile 381-400 |
| Corridor | terrainTiles 101-120 | tile 201-220 | - |

### **Furniture Placement Rules**

| Furniture | Room Types | Density | Min Spacing |
|-----------|------------|---------|-------------|
| Desk | Combat, Start | 30% | 2 tiles |
| Table | Break, Loot | 20% | 3 tiles |
| Chair | All | 40% | 1 tile |
| Sofa | Break | 10% | 4 tiles |

### **Pickup Distribution**

| Pickup Type | Room Types | Probability | Max Per Room |
|-------------|------------|-------------|--------------|
| Health | Combat, Boss | 20% | 2 |
| AmmoCrate | Combat, Loot | 40% | 3 |
| Food Items | Break | 80% | 5 |
| Weapons | Loot | 30% | 1 |

---

## 🔧 Key Technical Details

### **File Structure**
```
Assets/
├── Scripts/
│   └── MapGeneration/
│       ├── Core/
│       │   ├── Models/           # Data classes
│       │   ├── Interfaces/       # IMapGenerator, etc.
│       │   └── Config/           # ScriptableObjects
│       ├── Generators/
│       │   ├── RoomGenerator.cs
│       │   ├── CorridorGenerator.cs
│       │   └── LayoutOptimizer.cs
│       ├── Systems/
│       │   ├── SpawnSystem.cs
│       │   ├── ResourceDistribution.cs
│       │   └── TileAssetLoader.cs
│       └── Editor/
│           └── MapGeneratorWindow.cs
├── Resources/
│   └── MapGeneration/
│       ├── RoomTemplates/
│       ├── BiomeConfigs/
│       └── SpawnTables/
└── Tests/
    ├── PlayMode/
    └── EditMode/
```

### **Performance Targets**

| Metric | Target | Notes |
|--------|--------|-------|
| Total Generation | <3s | 100-room map |
| BSP + Rooms | <500ms | Spatial partitioning |
| A* Corridors | <2s | All connections |
| Layout Optimize | <500ms | Force-directed |
| Gameplay FPS | 60 FPS | No frame drops |
| NavMesh Coverage | >95% | AI pathfinding |
| Memory Usage | <200MB | Runtime generation |
| GC Pressure | <500KB/frame | Object pooling |

### **Algorithmic Complexity**

| Algorithm | Time | Space | Scalability |
|-----------|------|-------|-------------|
| BSP Generation | O(n log n) | O(r + w×h) | ✅ 500 rooms |
| A* Pathfinding | O(len²) | O(w×h) | ✅ Long corridors |
| MST Connection | O(E log V) | O(V + E) | ✅ Many rooms |
| Layout Optimization | O(r²×i) | O(r) | ✅ Iterative |

---

## 🧪 Testing Strategy

### **Unit Tests** (70% of test suite)
```csharp
// Example test structure
[Test]
public void Test_BSPNode_Split_CreatesValidChildren()
{
    var node = new BSPNode(new Rect(0, 0, 100, 100));
    node.Split(minRoomSize: 10);

    Assert.IsNotNull(node.Left);
    Assert.IsNotNull(node.Right);
    Assert.Greater(node.Left.Rect.width, 10);
}
```

### **Integration Tests** (20% of test suite)
```csharp
[Test]
public void Test_RoomAndCorridorIntegration()
{
    var rooms = roomGenerator.GenerateRooms(settings, seed);
    var corridors = corridorGenerator.ConnectRooms(rooms, settings);

    Assert.IsTrue(AllRoomsReachable(rooms, corridors));
}
```

### **End-to-End Tests** (10% of test suite)
```csharp
[UnityTest]
public IEnumerator Test_FullGeneration_WithWaveSpawner()
{
    mapGenerator.GenerateMap();
    yield return new WaitForSeconds(1.0f);

    var waveSpawner = FindObjectOfType<WaveSpawner>();
    Assert.IsTrue(waveSpawner.CanSpawn());
}
```

---

## 📖 Learning Resources

### **For Junior Developers**
1. Start: `MAP_GENERATION_PLAN.md` (Executive Summary)
2. Then: `PHASE_0_PART1_DATA_ARCHITECTURE.md`
3. Practice: Implement MapData class with tests
4. Timeline: 2-3 weeks

### **For Mid-Level Developers**
1. Skim: All Phase 0 documents
2. Deep dive: `PHASE_1_PART1_GENERATION_ALGORITHMS.md`
3. Implement: BSP + A* with TDD
4. Timeline: 1-2 weeks

### **For Senior Developers**
1. Review: `ARCHITECTURE_MASTER_INDEX.md`
2. Validate: `PHASE_0_PART3_SYSTEM_DESIGN.md` (ADRs)
3. Implement: Full system with architectural oversight
4. Timeline: 1 week

### **For Architects**
1. Review: All architectural decision records (ADRs)
2. Validate: System boundaries and dependencies
3. Guide: Implementation with code reviews
4. Timeline: Ongoing

---

## ⚡ Quick Commands

### **Read Key Documents**
```bash
# Master index (start here)
cat DOCS/ARCHITECTURE_MASTER_INDEX.md

# Asset integration (for existing assets)
cat DOCS/ASSET_INTEGRATION_PLAN.md

# Execution plan (for AI agents)
cat DOCS/AI_AGENT_EXECUTION_PLAN.md

# Algorithm details
cat DOCS/PHASE_1_PART1_GENERATION_ALGORITHMS.md
```

### **Navigate Documentation**
```bash
# List all documentation
ls -lh DOCS/

# Search for specific topics
grep -r "BSP" DOCS/
grep -r "NavMesh" DOCS/
grep -r "ScriptableObject" DOCS/
```

### **Git Workflow**
```bash
# Check current branch
git branch

# View recent commits
git log --oneline -10

# See all documentation commits
git log --oneline --grep="documentation"

# View changes in a specific doc
git show HEAD:DOCS/ASSET_INTEGRATION_PLAN.md
```

---

## 🎯 Success Criteria

### **Functional Requirements** ✅
- [ ] Generate 100×100 tile maps in <3 seconds
- [ ] All rooms connected (100% connectivity)
- [ ] NavMesh coverage >95%
- [ ] Compatible with WaveSpawner.cs (no modifications)
- [ ] Same seed produces identical maps
- [ ] Supports 50-500 rooms

### **Quality Requirements** ✅
- [ ] 90%+ test coverage on core systems
- [ ] Zero flaky tests
- [ ] 60 FPS gameplay maintained
- [ ] <500KB GC pressure per frame
- [ ] No memory leaks after 100 generations
- [ ] Deterministic generation verified

### **Integration Requirements** ✅
- [ ] Uses existing tile palettes (691 tiles)
- [ ] Uses existing prefabs (Table, Desk, etc.)
- [ ] Works with ObjectPooler.cs
- [ ] Compatible with existing HUD/UI
- [ ] NavMeshSurface auto-bakes correctly
- [ ] Spawn points tagged correctly

---

## 🆘 Common Issues & Solutions

### **Issue: "I don't know where to start"**
**Solution:** Read `ARCHITECTURE_MASTER_INDEX.md` Section 3: Quick Navigation Guide

### **Issue: "Tests are failing"**
**Solution:** Read `PHASE_0_PART2_TESTING_FOUNDATION.md` Section 6: Test Isolation

### **Issue: "Generation is too slow"**
**Solution:** Read `PHASE_3_PART1_PERFORMANCE_OPTIMIZATION.md` Section 4: Object Pooling

### **Issue: "NavMesh has gaps"**
**Solution:** Read `ASSET_INTEGRATION_PLAN.md` Section 6: NavMesh Integration

### **Issue: "Maps aren't deterministic"**
**Solution:** Read `PHASE_1_PART1_GENERATION_ALGORITHMS.md` Section 4: Determinism

### **Issue: "Asset loading is complex"**
**Solution:** Read `ASSET_INTEGRATION_PLAN.md` Section 3.1: TileAssetLoader

---

## 📞 Next Steps

### **Immediate (Today)**
1. ✅ Review this Quick Start Guide
2. ⬜ Read `ARCHITECTURE_MASTER_INDEX.md` (10 minutes)
3. ⬜ Skim `ASSET_INTEGRATION_PLAN.md` (20 minutes)
4. ⬜ Decide: Manual implementation OR AI agent execution

### **This Week**
1. ⬜ Set up directory structure (`Assets/Scripts/MapGeneration/`)
2. ⬜ Create first ScriptableObject (RoomTemplate)
3. ⬜ Implement TileAssetLoader
4. ⬜ Write first unit test

### **Next Week**
1. ⬜ Implement BSP room generation
2. ⬜ Add Gizmo visualization for debugging
3. ⬜ Test with existing tilemap system
4. ⬜ Verify NavMesh baking works

### **Week 3**
1. ⬜ Implement A* corridor generation
2. ⬜ Add furniture placement
3. ⬜ Integrate spawn point system
4. ⬜ Test with WaveSpawner.cs

### **Week 4**
1. ⬜ Performance profiling and optimization
2. ⬜ Build custom editor window
3. ⬜ Write integration tests
4. ⬜ Create first procedural room template

---

## 🎓 Key Concepts to Understand

### **Binary Space Partitioning (BSP)**
- Recursively divides rectangular space into smaller rooms
- Guarantees no overlapping rooms
- O(n log n) time complexity
- Creates natural office-like layouts

### **A* Pathfinding**
- Finds shortest path between two points
- Uses Manhattan distance heuristic for 2D grids
- Avoids obstacles (rooms, existing corridors)
- Creates realistic corridor layouts

### **Minimum Spanning Tree (MST)**
- Connects all rooms with minimum corridor length
- Uses Prim's algorithm (O(E log V))
- Guarantees 100% connectivity
- Adds strategic loops for variety

### **ScriptableObjects**
- Unity data containers (no MonoBehaviour required)
- Persistent configuration (survives play mode)
- Designer-friendly (editable in Inspector)
- Memory efficient (shared instances)

### **Object Pooling**
- Reuses objects instead of Instantiate/Destroy
- Reduces garbage collection pressure
- Critical for 60 FPS gameplay
- Already implemented in ObjectPooler.cs

---

## 📝 Important Notes

### **Don't Modify These Files**
- ✋ WaveSpawner.cs (already perfect, zero changes needed)
- ✋ ObjectPooler.cs (reuse as-is)
- ✋ Game.cs (game state manager, keep intact)
- ✋ Player.cs (player controller, preserve)
- ✋ Mouse.cs (enemy AI, maintain)

### **You Will Create These**
- ✅ MapGenerator.cs
- ✅ RoomGenerator.cs
- ✅ CorridorGenerator.cs
- ✅ SpawnPointManager.cs
- ✅ TileAssetLoader.cs
- ✅ PrefabManager.cs
- ✅ MapGeneratorWindow.cs (editor)

### **You Will Extend These**
- 🔧 GameScene.unity (add MapGenerator GameObject)
- 🔧 NavMeshSurface (call BuildNavMesh() after generation)
- 🔧 Resources/ (add RoomTemplate assets)

---

## 🏆 Final Thoughts

You now have:
- ✅ **11 comprehensive documents** (~520,000 words)
- ✅ **Complete architectural blueprints** (all 4 phases)
- ✅ **Asset integration guide** (691 tiles, all prefabs)
- ✅ **15-day implementation roadmap** (with AI agent support)
- ✅ **Testing strategy** (90%+ coverage target)
- ✅ **Performance targets** (<3s generation, 60 FPS)

**Everything is ready.** The planning phase is complete.

Choose your path:
1. **Manual Implementation** - Follow the phase documents step-by-step
2. **AI Agent Execution** - Use the agent prompts in AI_AGENT_EXECUTION_PLAN.md
3. **Hybrid Approach** - AI agents for foundation, manual for polish

**Good luck! 🚀**

---

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Branch:** feature/map-generation
**Status:** ✅ Ready for Implementation
