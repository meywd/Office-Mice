# Office-Mice Unity 6 Upgrade Guide

**Current Version:** Unity 2018.4.9f1
**Target Version:** Unity 6.0 LTS (Released October 2024)
**Upgrade Date:** November 16, 2025
**Project:** Office-Mice 2D Wave-Based Shooter

---

## Executive Summary

This guide documents the upgrade path for Office-Mice from Unity 2018.4.9f1 to Unity 6.0 LTS, spanning approximately 6 years of Unity development. This is a **major upgrade** with significant API changes, deprecated features, and new systems.

### Recommended Approach

**Option 1: Direct Upgrade (Riskier)**
- Backup project completely
- Open in Unity 6 directly
- Fix all compilation errors
- Test thoroughly

**Option 2: Sequential Upgrade (Safer)**
- Upgrade: 2018 → 2019 LTS → 2020 LTS → 2021 LTS → 2022 LTS → Unity 6
- Test after each upgrade
- More time-consuming but fewer issues

**Recommended:** Sequential upgrade for production projects, direct upgrade for experimental/learning purposes.

---

## Pre-Upgrade Checklist

### 1. Backup Everything
- [x] Git branch created: `claude/how-are-you-games-01CXHpPvSHrydh2hesXujgey`
- [ ] External backup of entire project folder
- [ ] Backup Unity project settings
- [ ] Document current Unity version: 2018.4.9f1

### 2. Current Project Analysis

**Technologies Used:**
- ✅ MonoBehaviour-based architecture
- ✅ NavMeshAgent (2D pathfinding)
- ✅ SpriteRenderer (2D graphics)
- ✅ Unity UI (uGUI)
- ✅ SceneManagement
- ✅ AudioSource/AudioClip
- ✅ Rigidbody2D
- ✅ Collider2D
- ✅ Coroutines
- ✅ Object pooling

**Key Files:**
- Player.cs (200 lines)
- Mouse.cs (204 lines) - Uses NavMeshAgent
- WaveSpawner.cs (153 lines)
- Game.cs (67 lines)
- Items system (multiple files)

---

## Major Breaking Changes (2018 → Unity 6)

### 1. NavMesh for 2D Games

**Current State (2018.4):**
- Using 3D NavMeshAgent on 2D game
- Works but is hacky

**Unity 6 Changes:**
- NavMesh still works but now requires "AI Navigation" package
- **Recommended:** Migrate to NavMeshPlus for proper 2D support
- Alternative: New AI Navigation package (Unity 2022+)

**Action Required:**
```
1. Install "AI Navigation" package from Unity Registry
2. Verify NavMeshAgent still works with 2D setup
3. Consider migrating to NavMeshPlus for Unity 6
```

### 2. UI System

**Current State:**
- Using Unity UI (uGUI) - UnityEngine.UI

**Unity 6:**
- uGUI still fully supported (no migration required)
- UI Toolkit available as modern alternative
- Canvas, Text, Button all work as-is

**Action Required:**
- ✅ No immediate action required
- Optional: Consider UI Toolkit for future features

### 3. Input System

**Current State:**
- Likely using Input.GetKey, Input.GetAxis (old Input Manager)

**Unity 6:**
- Old Input Manager still works
- New Input System recommended for new features

**Action Required:**
- ✅ No immediate action required
- Optional: Migrate to new Input System for rebinding/gamepad support

### 4. Scene Management

**Current State:**
- Using UnityEngine.SceneManagement

**Unity 6:**
- ✅ No changes - fully compatible

### 5. Audio System

**Current State:**
- AudioSource, AudioClip

**Unity 6:**
- ✅ No changes - fully compatible

### 6. Physics 2D

**Current State:**
- Rigidbody2D, Collider2D

**Unity 6:**
- ✅ No changes - fully compatible
- Enhanced performance in newer versions

### 7. Sprites and 2D Graphics

**Current State:**
- SpriteRenderer, Sprite

**Unity 6:**
- ✅ No changes - fully compatible
- New 2D features available (lights, shadows)

---

## Deprecated APIs to Watch

### Known Deprecations (Check after upgrade)

1. **ExecuteDefaultAction** (UI Toolkit) - Deprecated in Unity 6
   - **Impact:** None (you're using uGUI)

2. **CustomEditorForRenderPipelineAttribute** - Deprecated
   - **Impact:** None (no custom editors detected)

3. **SetupRenderPasses** (URP) - Deprecated in Unity 6.2
   - **Impact:** None (using Built-in Pipeline)

### Potential Issues

1. **NavMeshAgent on 2D:**
   - May require AI Navigation package install
   - Mouse.cs line 38-41 uses NavMeshAgent

2. **Coroutines:**
   - Should work but verify yield statements

3. **FindObjectOfType/FindObjectsOfType:**
   - Still works but slower
   - Consider caching references

---

## Unity 6 Installation

### Download Unity 6

1. **Via Unity Hub (Recommended):**
   - Open Unity Hub
   - Go to "Installs"
   - Click "Install Editor"
   - Select "Unity 6.0.x LTS"
   - Include modules: Linux Build Support, Documentation

2. **Direct Download:**
   - https://unity.com/releases/unity-6
   - Unity 6.0 LTS released October 18, 2024
   - Support until October 2026

### Required Packages for Office-Mice

```
1. AI Navigation (for NavMesh)
2. 2D Sprite (should be included)
3. 2D Physics (should be included)
4. Universal RP (optional - for better performance)
```

---

## Step-by-Step Upgrade Process

### Phase 1: Preparation

1. ✅ Read this document completely
2. ✅ Ensure git branch is clean
3. [ ] Create additional backup
4. [ ] Install Unity 6.0 LTS via Unity Hub
5. [ ] Do NOT open project yet

### Phase 2: Open in Unity 6

1. [ ] In Unity Hub, add existing project
2. [ ] Select Unity 6.0 LTS as editor version
3. [ ] Unity will prompt to upgrade - ACCEPT
4. [ ] Unity will run API Updater automatically
5. [ ] Wait for initial compilation

**Expected warnings/errors:**
- Shader compilation warnings (normal)
- Possible NavMesh warnings
- API deprecation warnings (non-breaking)

### Phase 3: Fix Compilation Errors

1. [ ] Open Console (Ctrl+Shift+C)
2. [ ] Address errors in order:
   - Red errors (breaking)
   - Yellow warnings (check relevance)
   - Blue messages (informational)

### Phase 4: Install Required Packages

1. [ ] Open Package Manager (Window → Package Manager)
2. [ ] Install "AI Navigation" package
3. [ ] Verify all packages are compatible

### Phase 5: Test Core Systems

1. [ ] Test Player movement
2. [ ] Test Mouse AI (NavMesh)
3. [ ] Test Wave spawning
4. [ ] Test Shooting/weapons
5. [ ] Test UI (menus, HUD)
6. [ ] Test Scene transitions
7. [ ] Test Audio
8. [ ] Test Game Over flow

### Phase 6: Performance Check

1. [ ] Check frame rate in Game view
2. [ ] Profile with Unity Profiler
3. [ ] Compare with 2018 version (if possible)

### Phase 7: Commit Changes

1. [ ] Review all changes
2. [ ] Test build (if possible)
3. [ ] Commit to git
4. [ ] Push to remote

---

## Known Issues & Solutions

### Issue 1: NavMesh Not Visible

**Symptom:** NavMesh doesn't show in Scene view

**Solution:**
```
1. Install "AI Navigation" package
2. Window → AI → Navigation
3. Rebuild NavMesh
```

### Issue 2: Compilation Errors

**Symptom:** Errors about missing types/methods

**Solution:**
```
1. Let Unity's API Updater run
2. Check Unity 6 upgrade guide
3. Manually update deprecated APIs
```

### Issue 3: Scene Loading Fails

**Symptom:** SceneManager.LoadScene errors

**Solution:**
```
1. Verify scene is in Build Settings
2. Check scene names match exactly
3. Verify SceneManagement namespace
```

### Issue 4: NavMeshAgent Errors

**Symptom:** NavMeshAgent component missing/broken

**Solution:**
```
1. Install AI Navigation package
2. Re-bake NavMesh
3. Verify NavMeshAgent settings
```

---

## Optional Enhancements for Unity 6

### 1. Universal Render Pipeline (URP)

**Benefits:**
- Better performance
- Modern rendering features
- 2D Lights support

**Migration Effort:** Medium (requires shader updates)

### 2. New Input System

**Benefits:**
- Gamepad support
- Rebinding
- Multi-device

**Migration Effort:** Medium (requires input refactor)

### 3. 2D Lights

**Benefits:**
- Dynamic lighting
- Normal maps
- Shadows

**Migration Effort:** Low (add-on feature)

### 4. DOTS/ECS (Advanced)

**Benefits:**
- Massive performance gains
- Handle 10,000+ enemies

**Migration Effort:** Very High (complete rewrite)

### 5. Addressables

**Benefits:**
- Better asset management
- Dynamic loading
- Memory optimization

**Migration Effort:** Medium

---

## Code Migration Guide

### NavMeshAgent (Mouse.cs)

**Current code (2018.4):**
```csharp
NavMeshAgent agent;
agent = GetComponent<NavMeshAgent>();
agent.Warp(transform.position);
agent.updateRotation = false;
agent.updateUpAxis = false;
agent.SetDestination(player.transform.position);
```

**Unity 6:**
- ✅ Should work as-is
- Ensure AI Navigation package installed
- May need to re-bake NavMesh

### Coroutines

**Current code:**
```csharp
IEnumerator EndHit()
{
    yield return new WaitForSeconds(0.3f);
    animator.SetInteger("state", (int)Animations.Moving);
    _stunned = false;
    yield break;
}
```

**Unity 6:**
- ✅ No changes required

### Scene Loading

**Current code:**
```csharp
using UnityEngine.SceneManagement;
SceneManager.LoadScene("GameOverScene", LoadSceneMode.Single);
```

**Unity 6:**
- ✅ No changes required

---

## Testing Checklist

### Functional Testing

- [ ] Player spawns correctly
- [ ] Player movement works
- [ ] Player shooting works
- [ ] Player takes damage (red flash)
- [ ] Player dies and respawns
- [ ] Mice spawn in waves
- [ ] Mice detect and chase player
- [ ] Mice pathfinding works (NavMesh)
- [ ] Mice take damage and die
- [ ] Wave system progresses
- [ ] Rush mechanic triggers
- [ ] Power-ups spawn
- [ ] Power-ups can be collected
- [ ] Ammo system works
- [ ] Health system works
- [ ] Lives system works
- [ ] Score increases
- [ ] Main menu loads
- [ ] Game scene loads
- [ ] Game over scene loads
- [ ] High scores work
- [ ] Audio plays correctly
- [ ] All UI elements visible
- [ ] No visual glitches

### Performance Testing

- [ ] Frame rate stable (60+ FPS)
- [ ] No memory leaks
- [ ] Wave spawning doesn't lag
- [ ] Many enemies don't cause slowdown
- [ ] Scene transitions smooth

### Build Testing

- [ ] Project builds without errors
- [ ] Built game runs correctly
- [ ] All scenes included in build
- [ ] All assets load correctly

---

## Rollback Plan

If upgrade fails:

1. **Git Rollback:**
```bash
git checkout master  # or previous branch
git branch -D claude/how-are-you-games-01CXHpPvSHrydh2hesXujgey
```

2. **File System Backup:**
- Restore from external backup
- Reopen in Unity 2018.4.9f1

3. **Document Issues:**
- Note what broke
- Research solutions
- Try again with fixes

---

## Post-Upgrade Recommendations

### Immediate (Must Do)

1. ✅ Verify all functionality works
2. ✅ Test on target platform
3. ✅ Update README with new Unity version
4. ✅ Commit all changes

### Short-term (Should Do)

1. Explore Unity 6 features
2. Consider URP migration
3. Profile performance gains
4. Update documentation

### Long-term (Nice to Have)

1. Migrate to new Input System
2. Add 2D lighting
3. Implement Addressables
4. Consider DOTS for performance

---

## Resources

### Official Documentation

- Unity 6 Release Notes: https://unity.com/releases/unity-6
- Unity 6 Upgrade Guide: https://docs.unity3d.com/6000.1/Documentation/Manual/UpgradeGuideUnity6.html
- AI Navigation Package: Unity Package Manager
- Unity 6 Support: https://unity.com/releases/unity-6/support

### Community Resources

- NavMeshPlus (2D NavMesh): https://github.com/h8man/NavMeshPlus
- NavMeshPlus Unity 6: https://github.com/sevensiete/NavMeshPlus-UNITY-6
- Unity Forums: https://discussions.unity.com/
- Unity Discord: https://discord.gg/unity

### Version Information

- **Unity 6.0 LTS:** Released October 18, 2024
- **Support:** Until October 2026 (2 years)
- **Extended Support:** +1 year for Enterprise/Industry

---

## Changelog

### 2025-11-16
- Created upgrade guide
- Analyzed current codebase
- Researched Unity 6 compatibility
- Documented breaking changes
- Created testing checklist

---

## Notes

- This is a **major upgrade** (6+ years)
- Expect 1-3 days of work for thorough testing
- Most code should work as-is
- NavMesh requires package install
- No game-breaking changes expected
- Performance should improve overall

---

## Approval

Before starting upgrade:

- [ ] Project lead approval
- [ ] Timeline approved (1-3 days)
- [ ] Backup verified
- [ ] Unity 6 installed
- [ ] Team notified

---

**Ready to upgrade? Follow the Step-by-Step Upgrade Process above.**

Good luck! 🚀
