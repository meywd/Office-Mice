# Phase 3 Part 2: Editor Tools & Production Deployment
## Office-Mice Map Generation System

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Author:** Software Architecture Analysis
**Status:** Architectural Blueprint - Production Ready

---

## Executive Summary

Phase 3 Part 2 represents the **operational excellence layer** of the Office-Mice procedural generation system. While Phases 1-2 focus on generation algorithms and content, Phase 3 bridges development workflow and production deployment. This phase delivers **custom Unity Editor tools** for designer productivity and **CI/CD infrastructure** for automated builds, testing, and cloud deployment.

**Architectural Impact:** MEDIUM-HIGH
**Developer Experience Impact:** CRITICAL
**Production Readiness Impact:** CRITICAL
**Technical Complexity:** MEDIUM

### Core Focus Areas

1. **Custom Editor Windows** - Visual tools for map generation and debugging
2. **Gizmo Visualization System** - Runtime debugging and spatial analysis
3. **Inspector Customization** - Designer-friendly ScriptableObject editing
4. **Scene View Tools** - Interactive map editing and validation
5. **Asset Creation Workflows** - Automated template generation
6. **Build Pipeline Integration** - Unity Cloud Build and GitHub Actions
7. **CI/CD for Unity WebGL** - Cloudflare Workers deployment
8. **Production Monitoring** - Analytics and error tracking

---

## Table of Contents

1. [Custom Editor Window Architecture](#1-custom-editor-window-architecture)
2. [Gizmo Visualization System](#2-gizmo-visualization-system)
3. [Inspector Customization Patterns](#3-inspector-customization-patterns)
4. [Scene View Tools](#4-scene-view-tools)
5. [Asset Creation Workflows](#5-asset-creation-workflows)
6. [Build Pipeline Integration](#6-build-pipeline-integration)
7. [CI/CD for Unity](#7-cicd-for-unity)
8. [Production Monitoring and Analytics](#8-production-monitoring-and-analytics)

---

## 1. Custom Editor Window Architecture

### 1.1 Map Generator Editor Window

**Purpose:** Central control panel for procedural map generation, replacing Inspector-based workflow.

**File Structure:**
```
Assets/
└── Scripts/
    └── MapGeneration/
        └── Editor/
            ├── MapGeneratorEditorWindow.cs      // Main window
            ├── GenerationPreviewRenderer.cs     // Preview system
            ├── ValidationPanel.cs               // Validation UI
            └── DebugVisualizationPanel.cs       // Debug tools
```

**Architecture:**

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Primary editor window for procedural map generation
/// Provides visual controls, previews, and debugging tools
/// </summary>
public class MapGeneratorEditorWindow : EditorWindow
{
    // State Management
    private MapGenerator targetGenerator;
    private MapGenerationContext lastGenerationContext;
    private Vector2 scrollPosition;
    private int selectedTabIndex = 0;

    // Generation Parameters
    private int seed = 0;
    private Vector2Int mapSize = new Vector2Int(100, 100);
    private int minRoomSize = 8;
    private int maxRoomSize = 20;
    private bool useRandomSeed = true;

    // Visualization State
    private bool showRoomBounds = true;
    private bool showCorridors = true;
    private bool showSpawnPoints = true;
    private bool showResources = true;
    private bool showDoorways = true;

    // Preview System
    private Texture2D previewTexture;
    private GenerationPreviewRenderer previewRenderer;

    // Validation
    private ValidationResult lastValidationResult;

    [MenuItem("Tools/Map Generator", priority = 100)]
    static void ShowWindow()
    {
        var window = GetWindow<MapGeneratorEditorWindow>("Map Generator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    void OnEnable()
    {
        // Initialize preview renderer
        previewRenderer = new GenerationPreviewRenderer();

        // Auto-find generator in scene
        FindGeneratorInScene();

        // Load saved preferences
        LoadEditorPreferences();
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        DrawHeader();

        // Tab Bar
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, new string[]
        {
            "Generate",
            "Validate",
            "Visualize",
            "Settings"
        });

        EditorGUILayout.Space(10);

        // Tab Content
        switch (selectedTabIndex)
        {
            case 0: DrawGenerationTab(); break;
            case 1: DrawValidationTab(); break;
            case 2: DrawVisualizationTab(); break;
            case 3: DrawSettingsTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region Generation Tab

    private void DrawGenerationTab()
    {
        EditorGUILayout.LabelField("Map Generation", EditorStyles.boldLabel);

        // Generator Reference
        targetGenerator = (MapGenerator)EditorGUILayout.ObjectField(
            "Map Generator",
            targetGenerator,
            typeof(MapGenerator),
            true
        );

        if (targetGenerator == null)
        {
            EditorGUILayout.HelpBox(
                "No MapGenerator found. Please add MapGenerator component to a GameObject in the scene.",
                MessageType.Warning
            );

            if (GUILayout.Button("Create New Generator"))
            {
                CreateNewMapGenerator();
            }
            return;
        }

        EditorGUILayout.Space(10);

        // Seed Configuration
        EditorGUILayout.LabelField("Seed Configuration", EditorStyles.boldLabel);
        useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);

        EditorGUI.BeginDisabledGroup(useRandomSeed);
        seed = EditorGUILayout.IntField("Seed", seed);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Generate Random Seed"))
        {
            seed = UnityEngine.Random.Range(0, 999999);
            useRandomSeed = false;
        }

        EditorGUILayout.Space(10);

        // Map Dimensions
        EditorGUILayout.LabelField("Map Dimensions", EditorStyles.boldLabel);
        mapSize = EditorGUILayout.Vector2IntField("Map Size", mapSize);

        // Quick size presets
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Small (50x50)")) mapSize = new Vector2Int(50, 50);
        if (GUILayout.Button("Medium (100x100)")) mapSize = new Vector2Int(100, 100);
        if (GUILayout.Button("Large (200x200)")) mapSize = new Vector2Int(200, 200);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Room Parameters
        EditorGUILayout.LabelField("Room Parameters", EditorStyles.boldLabel);
        minRoomSize = EditorGUILayout.IntSlider("Min Room Size", minRoomSize, 5, 30);
        maxRoomSize = EditorGUILayout.IntSlider("Max Room Size", maxRoomSize, minRoomSize, 50);

        EditorGUILayout.Space(10);

        // Preview
        if (previewTexture != null)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            Rect previewRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true));
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.Space(10);

        // Generation Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate Map", GUILayout.Height(40)))
        {
            GenerateMap();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Preview Only", GUILayout.Height(40)))
        {
            GeneratePreview();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Regenerate (Same Seed)", GUILayout.Height(30)))
        {
            RegenerateWithSameSeed();
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
        {
            ClearMap();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // Generation Stats
        if (lastGenerationContext != null)
        {
            EditorGUILayout.Space(10);
            DrawGenerationStats();
        }
    }

    private void DrawGenerationStats()
    {
        EditorGUILayout.LabelField("Last Generation Statistics", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Rooms Generated: {lastGenerationContext.Rooms?.Count ?? 0}");
        EditorGUILayout.LabelField($"Corridors: {lastGenerationContext.Corridors?.Count ?? 0}");
        EditorGUILayout.LabelField($"Spawn Points: {lastGenerationContext.SpawnPoints?.Count ?? 0}");
        EditorGUILayout.LabelField($"Resources Placed: {lastGenerationContext.PlacedResources?.Count ?? 0}");
        EditorGUILayout.LabelField($"Seed Used: {lastGenerationContext.Seed}");
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Validation Tab

    private void DrawValidationTab()
    {
        EditorGUILayout.LabelField("Map Validation", EditorStyles.boldLabel);

        if (lastGenerationContext == null)
        {
            EditorGUILayout.HelpBox("Generate a map first to run validation.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Run Full Validation", GUILayout.Height(40)))
        {
            RunValidation();
        }

        EditorGUILayout.Space(10);

        // Display validation results
        if (lastValidationResult != null)
        {
            // Summary
            if (lastValidationResult.IsValid)
            {
                EditorGUILayout.HelpBox("✓ Map validation passed!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ Validation failed with {lastValidationResult.Errors.Count} errors", MessageType.Error);
            }

            // Errors
            if (lastValidationResult.Errors.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                foreach (var error in lastValidationResult.Errors)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                    EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Warnings
            if (lastValidationResult.Warnings.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                foreach (var warning in lastValidationResult.Warnings)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
                    EditorGUILayout.LabelField(warning, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        EditorGUILayout.Space(10);

        // Validation Categories
        EditorGUILayout.LabelField("Validation Categories", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Connectivity"))
        {
            ValidateConnectivity();
        }

        if (GUILayout.Button("Validate NavMesh Coverage"))
        {
            ValidateNavMesh();
        }

        if (GUILayout.Button("Validate Spawn Points"))
        {
            ValidateSpawnPoints();
        }

        if (GUILayout.Button("Validate Resource Distribution"))
        {
            ValidateResources();
        }
    }

    #endregion

    #region Visualization Tab

    private void DrawVisualizationTab()
    {
        EditorGUILayout.LabelField("Visualization Controls", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Toggle visualization layers in Scene view. Use Gizmos for runtime debugging.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Visualization Toggles
        showRoomBounds = EditorGUILayout.Toggle("Show Room Bounds", showRoomBounds);
        showCorridors = EditorGUILayout.Toggle("Show Corridors", showCorridors);
        showSpawnPoints = EditorGUILayout.Toggle("Show Spawn Points", showSpawnPoints);
        showResources = EditorGUILayout.Toggle("Show Resources", showResources);
        showDoorways = EditorGUILayout.Toggle("Show Doorways", showDoorways);

        EditorGUILayout.Space(10);

        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Show All"))
        {
            showRoomBounds = showCorridors = showSpawnPoints = showResources = showDoorways = true;
        }
        if (GUILayout.Button("Hide All"))
        {
            showRoomBounds = showCorridors = showSpawnPoints = showResources = showDoorways = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Color Legend
        EditorGUILayout.LabelField("Color Legend", EditorStyles.boldLabel);
        DrawColorLegend();

        // Apply to scene
        SceneView.RepaintAll();
    }

    private void DrawColorLegend()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawColorItem(Color.green, "Room Bounds");
        DrawColorItem(Color.yellow, "Corridors");
        DrawColorItem(Color.red, "Spawn Points");
        DrawColorItem(Color.blue, "Resources");
        DrawColorItem(Color.cyan, "Doorways");
        DrawColorItem(Color.magenta, "Boss Room");
        DrawColorItem(new Color(1f, 0.5f, 0f), "Secret Room");

        EditorGUILayout.EndVertical();
    }

    private void DrawColorItem(Color color, string label)
    {
        EditorGUILayout.BeginHorizontal();

        Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
        EditorGUI.DrawRect(colorRect, color);

        EditorGUILayout.LabelField(label);

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Settings Tab

    private void DrawSettingsTab()
    {
        EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);

        // Auto-save preferences
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preferences", EditorStyles.boldLabel);

        bool autoSavePrefs = EditorPrefs.GetBool("MapGen_AutoSavePrefs", true);
        autoSavePrefs = EditorGUILayout.Toggle("Auto-Save Preferences", autoSavePrefs);
        EditorPrefs.SetBool("MapGen_AutoSavePrefs", autoSavePrefs);

        bool autoValidateOnGenerate = EditorPrefs.GetBool("MapGen_AutoValidate", true);
        autoValidateOnGenerate = EditorGUILayout.Toggle("Auto-Validate After Generation", autoValidateOnGenerate);
        EditorPrefs.SetBool("MapGen_AutoValidate", autoValidateOnGenerate);

        EditorGUILayout.Space(10);

        // Performance Settings
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);

        int maxPreviewSize = EditorPrefs.GetInt("MapGen_MaxPreviewSize", 512);
        maxPreviewSize = EditorGUILayout.IntSlider("Max Preview Texture Size", maxPreviewSize, 256, 2048);
        EditorPrefs.SetInt("MapGen_MaxPreviewSize", maxPreviewSize);

        EditorGUILayout.Space(10);

        // Debug Settings
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        bool verboseLogging = EditorPrefs.GetBool("MapGen_VerboseLogging", false);
        verboseLogging = EditorGUILayout.Toggle("Verbose Logging", verboseLogging);
        EditorPrefs.SetBool("MapGen_VerboseLogging", verboseLogging);

        EditorGUILayout.Space(10);

        // Reset Button
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Reset All Settings to Default"))
        {
            if (EditorUtility.DisplayDialog(
                "Reset Settings",
                "Are you sure you want to reset all Map Generator settings to default?",
                "Yes", "Cancel"))
            {
                ResetEditorPreferences();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    #endregion

    #region Generation Methods

    private void GenerateMap()
    {
        if (targetGenerator == null)
        {
            EditorUtility.DisplayDialog("Error", "No MapGenerator assigned.", "OK");
            return;
        }

        // Record for undo
        Undo.RecordObject(targetGenerator, "Generate Map");

        // Set parameters
        int finalSeed = useRandomSeed ? UnityEngine.Random.Range(0, 999999) : seed;
        seed = finalSeed; // Store for regeneration

        // Generate
        EditorUtility.DisplayProgressBar("Map Generation", "Generating map...", 0.5f);

        try
        {
            lastGenerationContext = targetGenerator.GenerateMap(finalSeed, mapSize, minRoomSize, maxRoomSize);

            // Auto-validate if enabled
            if (EditorPrefs.GetBool("MapGen_AutoValidate", true))
            {
                RunValidation();
            }

            // Generate preview
            GeneratePreview();

            EditorUtility.DisplayDialog("Success", $"Map generated successfully!\nSeed: {finalSeed}", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Map generation failed:\n{e.Message}", "OK");
            Debug.LogError($"Map generation error: {e}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void GeneratePreview()
    {
        if (lastGenerationContext == null) return;

        int maxSize = EditorPrefs.GetInt("MapGen_MaxPreviewSize", 512);
        previewTexture = previewRenderer.RenderPreview(lastGenerationContext, maxSize);
    }

    private void RegenerateWithSameSeed()
    {
        useRandomSeed = false;
        GenerateMap();
    }

    private void ClearMap()
    {
        if (targetGenerator == null) return;

        if (EditorUtility.DisplayDialog(
            "Clear Map",
            "Are you sure you want to clear the current map? This cannot be undone.",
            "Yes", "Cancel"))
        {
            Undo.RecordObject(targetGenerator, "Clear Map");
            targetGenerator.ClearMap();

            lastGenerationContext = null;
            previewTexture = null;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

    #endregion

    #region Validation Methods

    private void RunValidation()
    {
        if (lastGenerationContext == null) return;

        var validator = new MapValidator();
        lastValidationResult = validator.ValidateFullMap(lastGenerationContext);

        // Log to console
        if (lastValidationResult.IsValid)
        {
            Debug.Log("Map validation passed!");
        }
        else
        {
            Debug.LogWarning($"Map validation found {lastValidationResult.Errors.Count} errors and {lastValidationResult.Warnings.Count} warnings");
        }
    }

    private void ValidateConnectivity()
    {
        // Specific connectivity validation
        Debug.Log("Validating room connectivity...");
    }

    private void ValidateNavMesh()
    {
        // NavMesh validation
        Debug.Log("Validating NavMesh coverage...");
    }

    private void ValidateSpawnPoints()
    {
        // Spawn point validation
        Debug.Log("Validating spawn points...");
    }

    private void ValidateResources()
    {
        // Resource distribution validation
        Debug.Log("Validating resource distribution...");
    }

    #endregion

    #region Utility Methods

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("Procedural Map Generator", titleStyle);
        EditorGUILayout.LabelField("Office-Mice Level Editor", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
    }

    private void FindGeneratorInScene()
    {
        if (targetGenerator == null)
        {
            targetGenerator = FindObjectOfType<MapGenerator>();
        }
    }

    private void CreateNewMapGenerator()
    {
        GameObject generatorObj = new GameObject("MapGenerator");
        targetGenerator = generatorObj.AddComponent<MapGenerator>();

        Selection.activeGameObject = generatorObj;

        EditorUtility.DisplayDialog(
            "Generator Created",
            "New MapGenerator created and selected in hierarchy.",
            "OK"
        );
    }

    private void LoadEditorPreferences()
    {
        // Load saved preferences
        mapSize = new Vector2Int(
            EditorPrefs.GetInt("MapGen_MapWidth", 100),
            EditorPrefs.GetInt("MapGen_MapHeight", 100)
        );
        minRoomSize = EditorPrefs.GetInt("MapGen_MinRoomSize", 8);
        maxRoomSize = EditorPrefs.GetInt("MapGen_MaxRoomSize", 20);
    }

    private void SaveEditorPreferences()
    {
        if (!EditorPrefs.GetBool("MapGen_AutoSavePrefs", true)) return;

        EditorPrefs.SetInt("MapGen_MapWidth", mapSize.x);
        EditorPrefs.SetInt("MapGen_MapHeight", mapSize.y);
        EditorPrefs.SetInt("MapGen_MinRoomSize", minRoomSize);
        EditorPrefs.SetInt("MapGen_MaxRoomSize", maxRoomSize);
    }

    private void ResetEditorPreferences()
    {
        EditorPrefs.DeleteKey("MapGen_MapWidth");
        EditorPrefs.DeleteKey("MapGen_MapHeight");
        EditorPrefs.DeleteKey("MapGen_MinRoomSize");
        EditorPrefs.DeleteKey("MapGen_MaxRoomSize");
        EditorPrefs.DeleteKey("MapGen_AutoSavePrefs");
        EditorPrefs.DeleteKey("MapGen_AutoValidate");
        EditorPrefs.DeleteKey("MapGen_MaxPreviewSize");
        EditorPrefs.DeleteKey("MapGen_VerboseLogging");

        LoadEditorPreferences(); // Reload defaults
    }

    void OnDestroy()
    {
        SaveEditorPreferences();

        // Cleanup
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
    }

    #endregion
}
```

### 1.2 Preview Rendering System

**Purpose:** Generate visual previews of map layouts without full instantiation.

```csharp
using UnityEngine;

/// <summary>
/// Renders map generation context to a texture for preview
/// </summary>
public class GenerationPreviewRenderer
{
    public Texture2D RenderPreview(MapGenerationContext context, int maxSize)
    {
        if (context?.Rooms == null || context.Rooms.Count == 0)
            return null;

        // Calculate preview dimensions
        Vector2Int bounds = CalculateBounds(context);
        float scale = CalculateScale(bounds, maxSize);

        int width = Mathf.RoundToInt(bounds.x * scale);
        int height = Mathf.RoundToInt(bounds.y * scale);

        // Create texture
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;

        // Fill background
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0.15f, 0.15f, 0.15f); // Dark gray

        // Draw rooms
        foreach (var room in context.Rooms)
        {
            DrawRoom(pixels, width, height, room, scale, GetRoomColor(room, context));
        }

        // Draw corridors
        if (context.Corridors != null)
        {
            foreach (var corridor in context.Corridors)
            {
                DrawCorridor(pixels, width, height, corridor, scale, Color.yellow);
            }
        }

        // Draw spawn points
        if (context.SpawnPoints != null)
        {
            foreach (var spawn in context.SpawnPoints)
            {
                DrawPoint(pixels, width, height, spawn.transform.position, scale, Color.red, 2);
            }
        }

        // Draw resources
        if (context.PlacedResources != null)
        {
            foreach (var resource in context.PlacedResources)
            {
                DrawPoint(pixels, width, height, new Vector3(resource.position.x, resource.position.y, 0), scale, Color.cyan, 1);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    private Vector2Int CalculateBounds(MapGenerationContext context)
    {
        int maxX = context.Rooms.Max(r => r.Bounds.xMax);
        int maxY = context.Rooms.Max(r => r.Bounds.yMax);
        return new Vector2Int(maxX, maxY);
    }

    private float CalculateScale(Vector2Int bounds, int maxSize)
    {
        float scaleX = (float)maxSize / bounds.x;
        float scaleY = (float)maxSize / bounds.y;
        return Mathf.Min(scaleX, scaleY);
    }

    private Color GetRoomColor(Room room, MapGenerationContext context)
    {
        if (context.RoomTypes == null || !context.RoomTypes.ContainsKey(room))
            return Color.green;

        switch (context.RoomTypes[room])
        {
            case RoomClassification.PlayerStart: return new Color(0.5f, 1f, 0.5f);
            case RoomClassification.BossRoom: return Color.magenta;
            case RoomClassification.SecretRoom: return new Color(1f, 0.5f, 0f);
            case RoomClassification.ArenaRoom: return new Color(1f, 0.8f, 0.2f);
            case RoomClassification.SafeRoom: return new Color(0.5f, 0.5f, 1f);
            default: return Color.green;
        }
    }

    private void DrawRoom(Color[] pixels, int width, int height, Room room, float scale, Color color)
    {
        int x1 = Mathf.RoundToInt(room.Bounds.xMin * scale);
        int y1 = Mathf.RoundToInt(room.Bounds.yMin * scale);
        int x2 = Mathf.RoundToInt(room.Bounds.xMax * scale);
        int y2 = Mathf.RoundToInt(room.Bounds.yMax * scale);

        // Fill room
        for (int y = y1; y < y2; y++)
        {
            for (int x = x1; x < x2; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    pixels[y * width + x] = color;
                }
            }
        }

        // Draw border
        DrawRect(pixels, width, height, x1, y1, x2, y2, Color.white);
    }

    private void DrawCorridor(Color[] pixels, int width, int height, Corridor corridor, float scale, Color color)
    {
        int x1 = Mathf.RoundToInt(corridor.StartPosition.x * scale);
        int y1 = Mathf.RoundToInt(corridor.StartPosition.y * scale);
        int x2 = Mathf.RoundToInt(corridor.EndPosition.x * scale);
        int y2 = Mathf.RoundToInt(corridor.EndPosition.y * scale);

        DrawLine(pixels, width, height, x1, y1, x2, y2, color);
    }

    private void DrawPoint(Color[] pixels, int width, int height, Vector3 position, float scale, Color color, int radius)
    {
        int x = Mathf.RoundToInt(position.x * scale);
        int y = Mathf.RoundToInt(position.y * scale);

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    int px = x + dx;
                    int py = y + dy;

                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        pixels[py * width + px] = color;
                    }
                }
            }
        }
    }

    private void DrawLine(Color[] pixels, int width, int height, int x1, int y1, int x2, int y2, Color color)
    {
        // Bresenham's line algorithm
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
            {
                pixels[y1 * width + x1] = color;
            }

            if (x1 == x2 && y1 == y2) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    private void DrawRect(Color[] pixels, int width, int height, int x1, int y1, int x2, int y2, Color color)
    {
        DrawLine(pixels, width, height, x1, y1, x2, y1, color);
        DrawLine(pixels, width, height, x2, y1, x2, y2, color);
        DrawLine(pixels, width, height, x2, y2, x1, y2, color);
        DrawLine(pixels, width, height, x1, y2, x1, y1, color);
    }
}
```

---

## 2. Gizmo Visualization System

### 2.1 Runtime Debugging with Gizmos

**Purpose:** Visual debugging of map generation in Scene view without cluttering Hierarchy.

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Provides Gizmo visualization for map generation debugging
/// Attach to MapGenerator GameObject
/// </summary>
public class MapGenerationGizmos : MonoBehaviour
{
    [Header("Visualization Toggles")]
    public bool showRoomBounds = true;
    public bool showCorridors = true;
    public bool showSpawnPoints = true;
    public bool showResources = true;
    public bool showDoorways = true;
    public bool showNavMesh = false;

    [Header("Colors")]
    public Color roomBoundsColor = Color.green;
    public Color corridorColor = Color.yellow;
    public Color spawnPointColor = Color.red;
    public Color resourceColor = Color.cyan;
    public Color doorwayColor = new Color(0, 1, 1, 0.5f);

    [Header("References")]
    public MapGenerator mapGenerator;

    private MapGenerationContext context;

    void OnValidate()
    {
        if (mapGenerator == null)
            mapGenerator = GetComponent<MapGenerator>();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (mapGenerator == null) return;

        context = mapGenerator.LastGenerationContext;
        if (context == null) return;

        // Draw rooms
        if (showRoomBounds && context.Rooms != null)
        {
            foreach (var room in context.Rooms)
            {
                DrawRoom(room);
            }
        }

        // Draw corridors
        if (showCorridors && context.Corridors != null)
        {
            foreach (var corridor in context.Corridors)
            {
                DrawCorridor(corridor);
            }
        }

        // Draw spawn points
        if (showSpawnPoints && context.SpawnPoints != null)
        {
            foreach (var spawn in context.SpawnPoints)
            {
                DrawSpawnPoint(spawn.transform.position);
            }
        }

        // Draw resources
        if (showResources && context.PlacedResources != null)
        {
            foreach (var resource in context.PlacedResources)
            {
                DrawResource(new Vector3(resource.position.x, resource.position.y, 0));
            }
        }

        // Draw doorways
        if (showDoorways && context.RoomTypes != null)
        {
            // Implementation depends on room instance data
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw additional details when generator is selected
        if (mapGenerator == null || context == null) return;

        // Draw room labels
        if (showRoomBounds && context.Rooms != null)
        {
            foreach (var room in context.Rooms)
            {
                DrawRoomLabel(room);
            }
        }
    }

    private void DrawRoom(Room room)
    {
        // Determine color based on room type
        Color color = roomBoundsColor;

        if (context.RoomTypes != null && context.RoomTypes.ContainsKey(room))
        {
            color = GetRoomTypeColor(context.RoomTypes[room]);
        }

        Gizmos.color = color;

        // Draw room bounds
        Vector3 center = new Vector3(
            room.Bounds.center.x,
            room.Bounds.center.y,
            0
        );
        Vector3 size = new Vector3(
            room.Bounds.size.x,
            room.Bounds.size.y,
            0.1f
        );

        Gizmos.DrawWireCube(center, size);

        // Draw filled semi-transparent
        Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
        Gizmos.DrawCube(center, size);
    }

    private void DrawRoomLabel(Room room)
    {
        Vector3 center = new Vector3(
            room.Bounds.center.x,
            room.Bounds.center.y,
            0
        );

        string label = $"Room {room.ID}";
        if (context.RoomTypes != null && context.RoomTypes.ContainsKey(room))
        {
            label += $"\n{context.RoomTypes[room]}";
        }
        label += $"\nArea: {room.Area}";

        Handles.Label(center, label, EditorStyles.whiteLabel);
    }

    private void DrawCorridor(Corridor corridor)
    {
        Gizmos.color = corridorColor;

        Vector3 start = new Vector3(corridor.StartPosition.x, corridor.StartPosition.y, 0);
        Vector3 end = new Vector3(corridor.EndPosition.x, corridor.EndPosition.y, 0);

        Gizmos.DrawLine(start, end);

        // Draw corridor width
        float width = corridor.Width;
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * width * 0.5f;

        Gizmos.color = new Color(corridorColor.r, corridorColor.g, corridorColor.b, 0.3f);
        Gizmos.DrawLine(start + perpendicular, end + perpendicular);
        Gizmos.DrawLine(start - perpendicular, end - perpendicular);
    }

    private void DrawSpawnPoint(Vector3 position)
    {
        Gizmos.color = spawnPointColor;
        Gizmos.DrawWireSphere(position, 0.5f);

        // Draw direction indicator
        Gizmos.DrawLine(position, position + Vector3.up * 0.8f);
    }

    private void DrawResource(Vector3 position)
    {
        Gizmos.color = resourceColor;
        Gizmos.DrawWireCube(position, Vector3.one * 0.4f);
    }

    private Color GetRoomTypeColor(RoomClassification type)
    {
        switch (type)
        {
            case RoomClassification.PlayerStart: return new Color(0.5f, 1f, 0.5f);
            case RoomClassification.BossRoom: return Color.magenta;
            case RoomClassification.SecretRoom: return new Color(1f, 0.5f, 0f);
            case RoomClassification.ArenaRoom: return new Color(1f, 0.8f, 0.2f);
            case RoomClassification.SafeRoom: return new Color(0.5f, 0.5f, 1f);
            case RoomClassification.StorageRoom: return new Color(0.8f, 0.8f, 0.5f);
            default: return roomBoundsColor;
        }
    }
#endif
}
```

### 2.2 Custom Gizmo Icons

**Purpose:** Visual markers for special objects in Scene view.

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Custom gizmo drawer for spawn points and resources
/// </summary>
public class SpawnPointGizmo : MonoBehaviour
{
    public SpawnPointMetadata metadata;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw icon based on room type
        if (metadata != null)
        {
            Color color = GetColorForRoomType(metadata.roomType);
            Gizmos.color = color;

            Gizmos.DrawIcon(transform.position, "SpawnPoint_Icon.png", true);
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (metadata == null) return;

        // Draw detailed info when selected
        Handles.Label(
            transform.position + Vector3.up * 1.5f,
            $"Spawn Point\nRoom: {metadata.roomID}\nType: {metadata.roomType}",
            EditorStyles.whiteBoldLabel
        );

        // Draw activation radius
        Handles.color = new Color(1, 0, 0, 0.1f);
        Handles.DrawSolidDisc(transform.position, Vector3.forward, 2f);
    }

    private Color GetColorForRoomType(RoomClassification type)
    {
        // Color coding for different room types
        switch (type)
        {
            case RoomClassification.ArenaRoom: return Color.red;
            case RoomClassification.BossRoom: return Color.magenta;
            case RoomClassification.StandardRoom: return new Color(1f, 0.5f, 0f);
            default: return Color.yellow;
        }
    }
#endif
}
```

---

## 3. Inspector Customization Patterns

### 3.1 Custom Property Drawers

**Purpose:** Enhanced Inspector UI for complex data types.

```csharp
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for SpawnEntry in SpawnTable
/// Provides compact, readable layout with curve preview
/// </summary>
[CustomPropertyDrawer(typeof(SpawnEntry))]
public class SpawnEntryDrawer : PropertyDrawer
{
    private const float LINE_HEIGHT = 18f;
    private const float SPACING = 2f;
    private const float CURVE_HEIGHT = 60f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Calculate rects
        Rect foldoutRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);

        // Foldout
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            float yPos = position.y + LINE_HEIGHT + SPACING;

            // Enemy Prefab
            Rect enemyRect = new Rect(position.x, yPos, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(
                enemyRect,
                property.FindPropertyRelative("enemyPrefab"),
                new GUIContent("Enemy Prefab")
            );
            yPos += LINE_HEIGHT + SPACING;

            // Display Name
            Rect nameRect = new Rect(position.x, yPos, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(
                nameRect,
                property.FindPropertyRelative("enemyDisplayName"),
                new GUIContent("Display Name")
            );
            yPos += LINE_HEIGHT + SPACING;

            // Probability Curve with preview
            Rect curveRect = new Rect(position.x, yPos, position.width, CURVE_HEIGHT);
            EditorGUI.PropertyField(
                curveRect,
                property.FindPropertyRelative("probabilityCurve"),
                new GUIContent("Spawn Probability", "X-axis: Wave number, Y-axis: Spawn probability (0-1)")
            );
            yPos += CURVE_HEIGHT + SPACING;

            // Min/Max Wave (side-by-side)
            float halfWidth = position.width / 2f - 2f;
            Rect minWaveRect = new Rect(position.x, yPos, halfWidth, LINE_HEIGHT);
            Rect maxWaveRect = new Rect(position.x + halfWidth + 4f, yPos, halfWidth, LINE_HEIGHT);

            EditorGUI.PropertyField(
                minWaveRect,
                property.FindPropertyRelative("minWave"),
                new GUIContent("Min Wave")
            );
            EditorGUI.PropertyField(
                maxWaveRect,
                property.FindPropertyRelative("maxWave"),
                new GUIContent("Max Wave")
            );
            yPos += LINE_HEIGHT + SPACING;

            // Constraints
            Rect losRect = new Rect(position.x, yPos, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(
                losRect,
                property.FindPropertyRelative("requiresLineOfSight"),
                new GUIContent("Requires Line of Sight")
            );
            yPos += LINE_HEIGHT + SPACING;

            Rect distRect = new Rect(position.x, yPos, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(
                distRect,
                property.FindPropertyRelative("minDistanceFromPlayer"),
                new GUIContent("Min Distance from Player")
            );

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return LINE_HEIGHT;

        // Calculate total height when expanded
        return LINE_HEIGHT + SPACING +          // Foldout
               (LINE_HEIGHT + SPACING) * 5 +   // 5 regular fields
               CURVE_HEIGHT + SPACING;          // Curve field
    }
}
```

### 3.2 Custom Inspector for ScriptableObjects

**Purpose:** User-friendly editing for RoomTemplate assets.

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom Inspector for RoomTemplate ScriptableObject
/// Provides utilities and visual aids for template creation
/// </summary>
[CustomEditor(typeof(RoomTemplate))]
public class RoomTemplateInspector : Editor
{
    private SerializedProperty templateName;
    private SerializedProperty roomType;
    private SerializedProperty templateSize;
    private SerializedProperty floorTiles;
    private SerializedProperty wallTiles;
    private SerializedProperty objectTiles;
    private SerializedProperty doorways;
    private SerializedProperty furniturePoints;

    private bool showUtilities = true;
    private bool showTileArrays = false;

    void OnEnable()
    {
        // Find properties
        templateName = serializedObject.FindProperty("templateName");
        roomType = serializedObject.FindProperty("roomType");
        templateSize = serializedObject.FindProperty("templateSize");
        floorTiles = serializedObject.FindProperty("floorTiles");
        wallTiles = serializedObject.FindProperty("wallTiles");
        objectTiles = serializedObject.FindProperty("objectTiles");
        doorways = serializedObject.FindProperty("doorways");
        furniturePoints = serializedObject.FindProperty("furniturePoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        RoomTemplate template = (RoomTemplate)target;

        // Header
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Room Template Configuration", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Basic Info
        EditorGUILayout.PropertyField(templateName);
        EditorGUILayout.PropertyField(roomType);

        EditorGUILayout.Space(5);

        // Dimensions
        EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(templateSize);

        Vector2Int size = templateSize.vector2IntValue;
        EditorGUILayout.LabelField($"Total Tiles: {size.x * size.y}", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // Tile Arrays (collapsible)
        showTileArrays = EditorGUILayout.Foldout(showTileArrays, "Tile Arrays", true);
        if (showTileArrays)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(floorTiles, true);
            EditorGUILayout.PropertyField(wallTiles, true);
            EditorGUILayout.PropertyField(objectTiles, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Doorways
        EditorGUILayout.PropertyField(doorways, new GUIContent("Doorways"), true);

        EditorGUILayout.Space(5);

        // Furniture
        EditorGUILayout.PropertyField(furniturePoints, new GUIContent("Furniture Points"), true);

        EditorGUILayout.Space(10);

        // Utilities Section
        showUtilities = EditorGUILayout.Foldout(showUtilities, "Utilities", true, EditorStyles.foldoutHeader);
        if (showUtilities)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Resize Arrays Button
            if (GUILayout.Button("Resize Tile Arrays to Match Dimensions"))
            {
                ResizeTileArrays(template);
            }

            // Validate Button
            if (GUILayout.Button("Validate Template"))
            {
                ValidateTemplate(template);
            }

            // Create from Tilemap Button
            if (GUILayout.Button("Import from Active Tilemap"))
            {
                ImportFromTilemap(template);
            }

            // Auto-Generate Doorways
            if (GUILayout.Button("Auto-Detect Doorway Positions"))
            {
                AutoDetectDoorways(template);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);

        // Statistics
        DrawStatistics(template);

        serializedObject.ApplyModifiedProperties();
    }

    private void ResizeTileArrays(RoomTemplate template)
    {
        Undo.RecordObject(template, "Resize Tile Arrays");

        int size = template.templateSize.x * template.templateSize.y;

        template.floorTiles = new UnityEngine.Tilemaps.TileBase[size];
        template.wallTiles = new UnityEngine.Tilemaps.TileBase[size];
        template.objectTiles = new UnityEngine.Tilemaps.TileBase[size];

        EditorUtility.SetDirty(template);
        EditorUtility.DisplayDialog("Success", $"Tile arrays resized to {size} elements", "OK");
    }

    private void ValidateTemplate(RoomTemplate template)
    {
        var result = template.Validate();

        string message;
        MessageType messageType;

        if (result.IsValid)
        {
            message = "✓ Template validation passed!";
            messageType = MessageType.Info;
        }
        else
        {
            message = $"Validation found {result.Errors.Count} errors:\n\n";
            message += string.Join("\n", result.Errors);

            if (result.Warnings.Count > 0)
            {
                message += $"\n\nWarnings ({result.Warnings.Count}):\n";
                message += string.Join("\n", result.Warnings);
            }

            messageType = MessageType.Error;
        }

        EditorUtility.DisplayDialog("Template Validation", message, "OK");
    }

    private void ImportFromTilemap(RoomTemplate template)
    {
        // Find active tilemap in scene
        var tilemap = FindObjectOfType<UnityEngine.Tilemaps.Tilemap>();
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "No Tilemap found in scene", "OK");
            return;
        }

        // Import logic would go here
        EditorUtility.DisplayDialog("Import", "Tilemap import functionality coming soon!", "OK");
    }

    private void AutoDetectDoorways(RoomTemplate template)
    {
        // Auto-detect doorway logic
        EditorUtility.DisplayDialog("Auto-Detect", "Auto-detect doorways functionality coming soon!", "OK");
    }

    private void DrawStatistics(RoomTemplate template)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        int tileCount = template.templateSize.x * template.templateSize.y;
        int doorwayCount = template.doorways?.Count ?? 0;
        int furnitureCount = template.furniturePoints?.Count ?? 0;

        EditorGUILayout.LabelField($"Total Tile Slots: {tileCount}");
        EditorGUILayout.LabelField($"Doorways: {doorwayCount}");
        EditorGUILayout.LabelField($"Furniture Points: {furnitureCount}");

        // Calculate filled tile percentage
        int filledFloor = 0;
        int filledWall = 0;
        int filledObject = 0;

        if (template.floorTiles != null)
            filledFloor = System.Array.FindAll(template.floorTiles, t => t != null).Length;
        if (template.wallTiles != null)
            filledWall = System.Array.FindAll(template.wallTiles, t => t != null).Length;
        if (template.objectTiles != null)
            filledObject = System.Array.FindAll(template.objectTiles, t => t != null).Length;

        float floorPercent = tileCount > 0 ? (float)filledFloor / tileCount * 100f : 0f;
        float wallPercent = tileCount > 0 ? (float)filledWall / tileCount * 100f : 0f;
        float objectPercent = tileCount > 0 ? (float)filledObject / tileCount * 100f : 0f;

        EditorGUILayout.LabelField($"Floor Tiles: {filledFloor}/{tileCount} ({floorPercent:F1}%)");
        EditorGUILayout.LabelField($"Wall Tiles: {filledWall}/{tileCount} ({wallPercent:F1}%)");
        EditorGUILayout.LabelField($"Object Tiles: {filledObject}/{tileCount} ({objectPercent:F1}%)");

        EditorGUILayout.EndVertical();
    }
}
```

---

## 4. Scene View Tools

### 4.1 Scene View Overlay

**Purpose:** In-scene UI overlay for quick actions and status display.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

#if UNITY_2021_2_OR_NEWER

[Overlay(typeof(SceneView), "Map Generation Overlay")]
public class MapGenerationOverlay : Overlay
{
    private MapGenerator mapGenerator;

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();
        root.style.paddingBottom = 10;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Title
        var title = new Label("Map Generator");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14;
        title.style.color = Color.white;
        root.Add(title);

        // Status
        var status = new Label("Status: Idle");
        status.name = "status-label";
        status.style.color = Color.gray;
        root.Add(status);

        // Quick Generate Button
        var generateBtn = new Button(() => QuickGenerate());
        generateBtn.text = "Quick Generate";
        root.Add(generateBtn);

        // Clear Button
        var clearBtn = new Button(() => QuickClear());
        clearBtn.text = "Clear Map";
        root.Add(clearBtn);

        return root;
    }

    private void QuickGenerate()
    {
        mapGenerator = Object.FindObjectOfType<MapGenerator>();
        if (mapGenerator != null)
        {
            // Quick generation with default parameters
            int seed = UnityEngine.Random.Range(0, 999999);
            mapGenerator.GenerateMap(seed, new Vector2Int(100, 100), 8, 20);
        }
    }

    private void QuickClear()
    {
        mapGenerator = Object.FindObjectOfType<MapGenerator>();
        if (mapGenerator != null)
        {
            mapGenerator.ClearMap();
        }
    }
}

#endif
```

### 4.2 Handle-Based Editing

**Purpose:** Interactive manipulation of room boundaries and doorways in Scene view.

```csharp
using UnityEngine;
using UnityEditor;

/// <summary>
/// Provides handle-based editing for room templates in Scene view
/// </summary>
[CustomEditor(typeof(RoomTemplatePreview))]
public class RoomTemplatePreviewEditor : Editor
{
    private void OnSceneGUI()
    {
        RoomTemplatePreview preview = (RoomTemplatePreview)target;
        if (preview.template == null) return;

        // Draw handles for doorways
        foreach (var doorway in preview.template.doorways)
        {
            DrawDoorwayHandle(preview, doorway);
        }

        // Draw handles for furniture
        foreach (var furniture in preview.template.furniturePoints)
        {
            DrawFurnitureHandle(preview, furniture);
        }
    }

    private void DrawDoorwayHandle(RoomTemplatePreview preview, DoorwayDefinition doorway)
    {
        Vector3 worldPos = preview.transform.position + new Vector3(
            doorway.localPosition.x,
            doorway.localPosition.y,
            0
        );

        // Position handle
        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(preview.template, "Move Doorway");

            Vector3 localPos = newPos - preview.transform.position;
            doorway.localPosition = new Vector2Int(
                Mathf.RoundToInt(localPos.x),
                Mathf.RoundToInt(localPos.y)
            );

            EditorUtility.SetDirty(preview.template);
        }

        // Direction handle
        Vector3 direction = GetDoorwayDirection(doorway.direction);
        Handles.color = Color.cyan;
        Handles.DrawLine(worldPos, worldPos + direction * 2f);
        Handles.ArrowHandleCap(0, worldPos + direction * 2f, Quaternion.LookRotation(direction), 0.5f, EventType.Repaint);

        // Label
        Handles.Label(worldPos + Vector3.up, $"Doorway\n{doorway.direction}");
    }

    private void DrawFurnitureHandle(RoomTemplatePreview preview, FurnitureSpawnPoint furniture)
    {
        Vector3 worldPos = preview.transform.position + new Vector3(
            furniture.localPosition.x,
            furniture.localPosition.y,
            0
        );

        // Position handle
        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.Euler(0, 0, furniture.rotationDegrees));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(preview.template, "Move Furniture");

            Vector3 localPos = newPos - preview.transform.position;
            furniture.localPosition = new Vector2Int(
                Mathf.RoundToInt(localPos.x),
                Mathf.RoundToInt(localPos.y)
            );

            EditorUtility.SetDirty(preview.template);
        }

        // Preview furniture icon
        Handles.color = Color.yellow;
        Handles.DrawWireCube(worldPos, Vector3.one * 0.5f);

        if (furniture.prefab != null)
        {
            Handles.Label(worldPos + Vector3.up, furniture.prefab.name);
        }
    }

    private Vector3 GetDoorwayDirection(DoorwayDirection direction)
    {
        switch (direction)
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

---

## 5. Asset Creation Workflows

### 5.1 Room Template Wizard

**Purpose:** Step-by-step wizard for creating room templates from scratch.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

/// <summary>
/// Wizard for creating new room templates
/// Guides user through template creation process
/// </summary>
public class RoomTemplateWizard : ScriptableWizard
{
    [Header("Template Info")]
    public string templateName = "NewRoomTemplate";
    public RoomType roomType = RoomType.Generic;

    [Header("Dimensions")]
    public Vector2Int templateSize = new Vector2Int(15, 15);

    [Header("Source Tilemap (Optional)")]
    public Tilemap sourceTilemap;
    public bool importFromTilemap = false;

    [Header("Output Location")]
    public string savePath = "Assets/Resources/RoomTemplates/";

    [MenuItem("Tools/Room Template Wizard")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<RoomTemplateWizard>("Create Room Template", "Create");
    }

    void OnWizardCreate()
    {
        // Create new RoomTemplate asset
        RoomTemplate template = ScriptableObject.CreateInstance<RoomTemplate>();

        // Set properties
        template.templateName = templateName;
        template.roomType = roomType;
        template.templateSize = templateSize;

        // Initialize tile arrays
        int tileCount = templateSize.x * templateSize.y;
        template.floorTiles = new TileBase[tileCount];
        template.wallTiles = new TileBase[tileCount];
        template.objectTiles = new TileBase[tileCount];

        // Import from tilemap if specified
        if (importFromTilemap && sourceTilemap != null)
        {
            ImportTilemapData(template, sourceTilemap);
        }

        // Create save directory if needed
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Save asset
        string assetPath = Path.Combine(savePath, $"{templateName}.asset");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(template, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select new asset
        Selection.activeObject = template;
        EditorGUIUtility.PingObject(template);

        EditorUtility.DisplayDialog(
            "Template Created",
            $"Room template '{templateName}' created at:\n{assetPath}",
            "OK"
        );
    }

    void OnWizardUpdate()
    {
        helpString = "Create a new room template asset.\n\n";

        if (importFromTilemap && sourceTilemap == null)
        {
            helpString += "⚠ Please assign a source Tilemap to import from.";
            isValid = false;
        }
        else if (string.IsNullOrEmpty(templateName))
        {
            helpString += "⚠ Please enter a template name.";
            isValid = false;
        }
        else if (templateSize.x < 5 || templateSize.y < 5)
        {
            helpString += "⚠ Template size must be at least 5x5.";
            isValid = false;
        }
        else
        {
            helpString += "✓ Ready to create template.";
            isValid = true;
        }
    }

    private void ImportTilemapData(RoomTemplate template, Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;

        // Adjust template size to match tilemap
        template.templateSize = new Vector2Int(bounds.size.x, bounds.size.y);

        int tileCount = bounds.size.x * bounds.size.y;
        template.floorTiles = new TileBase[tileCount];
        template.wallTiles = new TileBase[tileCount];
        template.objectTiles = new TileBase[tileCount];

        // Import tiles
        int index = 0;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);

                if (tile != null)
                {
                    // Simple heuristic: classify based on tile name
                    string tileName = tile.name.ToLower();

                    if (tileName.Contains("floor"))
                        template.floorTiles[index] = tile;
                    else if (tileName.Contains("wall"))
                        template.wallTiles[index] = tile;
                    else
                        template.objectTiles[index] = tile;
                }

                index++;
            }
        }

        Debug.Log($"Imported {index} tiles from Tilemap '{tilemap.name}'");
    }
}
```

### 5.2 Batch Asset Operations

**Purpose:** Bulk operations on multiple room templates.

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Batch operations for room templates
/// </summary>
public class RoomTemplateBatchOperations : EditorWindow
{
    private List<RoomTemplate> selectedTemplates = new List<RoomTemplate>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Room Template Batch Operations")]
    static void ShowWindow()
    {
        var window = GetWindow<RoomTemplateBatchOperations>("Batch Operations");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Room Template Batch Operations", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Template Selection
        EditorGUILayout.LabelField("Selected Templates", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        if (selectedTemplates.Count == 0)
        {
            EditorGUILayout.HelpBox("Drag and drop RoomTemplate assets here", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < selectedTemplates.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                selectedTemplates[i] = (RoomTemplate)EditorGUILayout.ObjectField(
                    selectedTemplates[i],
                    typeof(RoomTemplate),
                    false
                );

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    selectedTemplates.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        // Drag and drop area
        HandleDragAndDrop();

        EditorGUILayout.Space(10);

        // Operations
        EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(selectedTemplates.Count == 0);

        if (GUILayout.Button("Validate All Templates"))
        {
            ValidateAllTemplates();
        }

        if (GUILayout.Button("Resize All Tile Arrays"))
        {
            ResizeAllTileArrays();
        }

        if (GUILayout.Button("Auto-Detect All Doorways"))
        {
            AutoDetectAllDoorways();
        }

        if (GUILayout.Button("Export Report"))
        {
            ExportReport();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Quick Actions
        if (GUILayout.Button("Load All Templates from Resources"))
        {
            LoadAllTemplatesFromResources();
        }

        if (GUILayout.Button("Clear Selection"))
        {
            selectedTemplates.Clear();
        }
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));

        GUI.Box(dropArea, "Drag RoomTemplate assets here");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is RoomTemplate template)
                        {
                            if (!selectedTemplates.Contains(template))
                            {
                                selectedTemplates.Add(template);
                            }
                        }
                    }
                }
                break;
        }
    }

    private void ValidateAllTemplates()
    {
        int passedCount = 0;
        int failedCount = 0;
        List<string> errors = new List<string>();

        foreach (var template in selectedTemplates)
        {
            var result = template.Validate();

            if (result.IsValid)
            {
                passedCount++;
            }
            else
            {
                failedCount++;
                errors.Add($"{template.name}: {string.Join(", ", result.Errors)}");
            }
        }

        string message = $"Validation Complete:\n\nPassed: {passedCount}\nFailed: {failedCount}";

        if (errors.Count > 0)
        {
            message += "\n\nErrors:\n" + string.Join("\n", errors);
        }

        EditorUtility.DisplayDialog("Batch Validation", message, "OK");
    }

    private void ResizeAllTileArrays()
    {
        foreach (var template in selectedTemplates)
        {
            Undo.RecordObject(template, "Resize Tile Arrays");

            int size = template.templateSize.x * template.templateSize.y;
            template.floorTiles = new TileBase[size];
            template.wallTiles = new TileBase[size];
            template.objectTiles = new TileBase[size];

            EditorUtility.SetDirty(template);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Success", $"Resized tile arrays for {selectedTemplates.Count} templates", "OK");
    }

    private void AutoDetectAllDoorways()
    {
        // Implementation for auto-detecting doorways
        EditorUtility.DisplayDialog("Info", "Auto-detect doorways functionality coming soon!", "OK");
    }

    private void ExportReport()
    {
        string report = "Room Template Report\n";
        report += "===================\n\n";

        foreach (var template in selectedTemplates)
        {
            report += $"Template: {template.name}\n";
            report += $"  Type: {template.roomType}\n";
            report += $"  Size: {template.templateSize.x}x{template.templateSize.y}\n";
            report += $"  Doorways: {template.doorways?.Count ?? 0}\n";
            report += $"  Furniture: {template.furniturePoints?.Count ?? 0}\n";

            var validation = template.Validate();
            report += $"  Valid: {(validation.IsValid ? "Yes" : "No")}\n";

            if (!validation.IsValid)
            {
                report += $"  Errors: {string.Join(", ", validation.Errors)}\n";
            }

            report += "\n";
        }

        string path = EditorUtility.SaveFilePanel("Export Report", "", "RoomTemplateReport", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, report);
            EditorUtility.DisplayDialog("Success", $"Report exported to:\n{path}", "OK");
        }
    }

    private void LoadAllTemplatesFromResources()
    {
        var templates = Resources.LoadAll<RoomTemplate>("RoomTemplates");
        selectedTemplates = templates.ToList();

        EditorUtility.DisplayDialog("Success", $"Loaded {templates.Length} templates from Resources", "OK");
    }
}
```

---

## 6. Build Pipeline Integration

### 6.1 Pre-Build Validation

**Purpose:** Automatic validation before building to prevent broken builds.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Linq;

/// <summary>
/// Pre-build validation for map generation assets
/// Ensures all ScriptableObjects are valid before building
/// </summary>
public class MapGenerationBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[Build] Running Map Generation validation...");

        bool isValid = true;

        // Validate all RoomTemplates
        isValid &= ValidateRoomTemplates();

        // Validate SpawnTables
        isValid &= ValidateSpawnTables();

        // Validate BiomeConfigs
        isValid &= ValidateBiomeConfigs();

        // Validate GameConfig
        isValid &= ValidateGameConfig();

        if (!isValid)
        {
            throw new BuildFailedException("Map Generation validation failed. Fix errors before building.");
        }

        Debug.Log("[Build] Map Generation validation passed!");
    }

    private bool ValidateRoomTemplates()
    {
        var templates = Resources.LoadAll<RoomTemplate>("");

        Debug.Log($"[Build] Validating {templates.Length} RoomTemplates...");

        bool allValid = true;

        foreach (var template in templates)
        {
            var result = template.Validate();

            if (!result.IsValid)
            {
                Debug.LogError($"[Build] RoomTemplate '{template.name}' validation failed:\n" +
                              string.Join("\n", result.Errors));
                allValid = false;
            }

            if (result.Warnings.Count > 0)
            {
                Debug.LogWarning($"[Build] RoomTemplate '{template.name}' warnings:\n" +
                                string.Join("\n", result.Warnings));
            }
        }

        return allValid;
    }

    private bool ValidateSpawnTables()
    {
        var spawnTables = Resources.LoadAll<SpawnTable>("");

        Debug.Log($"[Build] Validating {spawnTables.Length} SpawnTables...");

        bool allValid = true;

        foreach (var table in spawnTables)
        {
            if (table.entries == null || table.entries.Count == 0)
            {
                Debug.LogError($"[Build] SpawnTable '{table.name}' has no entries");
                allValid = false;
                continue;
            }

            foreach (var entry in table.entries)
            {
                if (entry.enemyPrefab == null)
                {
                    Debug.LogError($"[Build] SpawnTable '{table.name}' has null enemy prefab");
                    allValid = false;
                }

                if (entry.probabilityCurve == null || entry.probabilityCurve.keys.Length == 0)
                {
                    Debug.LogError($"[Build] SpawnTable '{table.name}' entry has invalid probability curve");
                    allValid = false;
                }
            }
        }

        return allValid;
    }

    private bool ValidateBiomeConfigs()
    {
        var biomes = Resources.LoadAll<BiomeConfig>("");

        Debug.Log($"[Build] Validating {biomes.Length} BiomeConfigs...");

        bool allValid = true;

        foreach (var biome in biomes)
        {
            if (biome.floorTileset == null || biome.wallTileset == null)
            {
                Debug.LogError($"[Build] BiomeConfig '{biome.name}' missing tileset references");
                allValid = false;
            }
        }

        return allValid;
    }

    private bool ValidateGameConfig()
    {
        var gameConfig = Resources.Load<GameConfig>("GameConfig");

        if (gameConfig == null)
        {
            Debug.LogError("[Build] GameConfig not found in Resources folder");
            return false;
        }

        bool isValid = true;

        if (gameConfig.biomeRegistry == null)
        {
            Debug.LogError("[Build] GameConfig missing BiomeRegistry");
            isValid = false;
        }

        if (gameConfig.spawnTableRegistry == null)
        {
            Debug.LogError("[Build] GameConfig missing SpawnTableRegistry");
            isValid = false;
        }

        if (gameConfig.roomTemplateLibrary == null)
        {
            Debug.LogError("[Build] GameConfig missing RoomTemplateLibrary");
            isValid = false;
        }

        return isValid;
    }
}
```

### 6.2 Post-Build Report

**Purpose:** Generate build report with map generation statistics.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

/// <summary>
/// Generates post-build report for map generation assets
/// </summary>
public class MapGenerationBuildReporter : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        Debug.Log("[Build] Generating Map Generation build report...");

        string reportPath = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "MapGenerationReport.txt");

        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("Map Generation Build Report");
            writer.WriteLine("===========================");
            writer.WriteLine($"Build Time: {System.DateTime.Now}");
            writer.WriteLine($"Build Target: {report.summary.platform}");
            writer.WriteLine($"Build Result: {report.summary.result}");
            writer.WriteLine();

            // Content Statistics
            writer.WriteLine("Content Statistics:");
            writer.WriteLine("-------------------");

            var roomTemplates = Resources.LoadAll<RoomTemplate>("");
            writer.WriteLine($"Room Templates: {roomTemplates.Length}");

            var spawnTables = Resources.LoadAll<SpawnTable>("");
            writer.WriteLine($"Spawn Tables: {spawnTables.Length}");

            var biomes = Resources.LoadAll<BiomeConfig>("");
            writer.WriteLine($"Biome Configs: {biomes.Length}");

            writer.WriteLine();

            // Room Template Details
            writer.WriteLine("Room Template Breakdown:");
            writer.WriteLine("------------------------");

            foreach (var template in roomTemplates)
            {
                writer.WriteLine($"  {template.name}:");
                writer.WriteLine($"    Type: {template.roomType}");
                writer.WriteLine($"    Size: {template.templateSize.x}x{template.templateSize.y}");
                writer.WriteLine($"    Doorways: {template.doorways?.Count ?? 0}");
                writer.WriteLine($"    Furniture: {template.furniturePoints?.Count ?? 0}");
            }

            writer.WriteLine();
            writer.WriteLine("Report generated successfully!");
        }

        Debug.Log($"[Build] Map Generation report saved to: {reportPath}");
    }
}
```

---

## 7. CI/CD for Unity

### 7.1 GitHub Actions Workflow (Enhanced)

**Purpose:** Automated Unity WebGL builds with caching and deployment.

**File:** `/home/meywd/Office-Mice/.github/workflows/deploy-cloudflare.yml` (already exists, enhanced version below)

```yaml
name: Deploy Office-Mice to Cloudflare Workers

on:
  push:
    branches:
      - master
      - main
  pull_request:
    branches:
      - master
      - main
  workflow_dispatch:

env:
  UNITY_VERSION: 6000.0.12f1
  PROJECT_PATH: .

jobs:
  # Job 1: Validate Assets
  validate-assets:
    name: Validate Map Generation Assets
    runs-on: ubuntu-latest

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0

      - name: Cache Git LFS
        uses: actions/cache@v4
        with:
          path: .git/lfs
          key: GitLFS-${{ hashFiles('.lfsconfig') }}
          restore-keys: GitLFS-

      - name: Cache Unity Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-Validation-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-Validation-
            Library-

      # Run asset validation in editor mode
      - name: Validate Map Generation Assets
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          unityVersion: ${{ env.UNITY_VERSION }}
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          customParameters: -executeMethod MapGenerationValidator.ValidateFromCommandLine -quit -batchmode

      - name: Upload Validation Report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: validation-report
          path: validation-report.txt

  # Job 2: Build Unity WebGL
  build-webgl:
    name: Build Unity WebGL
    runs-on: ubuntu-latest
    needs: validate-assets

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0

      - name: Cache Git LFS
        uses: actions/cache@v4
        with:
          path: .git/lfs
          key: GitLFS-${{ hashFiles('.lfsconfig') }}
          restore-keys: GitLFS-

      - name: Cache Unity Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-WebGL-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-WebGL-
            Library-

      - name: Free Disk Space
        run: |
          sudo rm -rf /usr/share/dotnet
          sudo rm -rf /opt/ghc
          sudo rm -rf "/usr/local/share/boost"
          sudo rm -rf "$AGENT_TOOLSDIRECTORY"
          df -h

      - name: Build Unity WebGL
        uses: game-ci/unity-builder@v4
        env:
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          targetPlatform: WebGL
          unityVersion: ${{ env.UNITY_VERSION }}
          buildName: office-mice
          buildsPath: build
          customParameters: -compressedCompressionFormat Brotli -executeMethod BuildPostProcessor.OnPostBuild

      - name: List Build Contents
        run: |
          echo "Build directory contents:"
          ls -lah build/WebGL/office-mice/
          echo ""
          echo "Build size:"
          du -sh build/WebGL/office-mice/

      - name: Upload Build Artifacts
        uses: actions/upload-artifact@v4
        with:
          name: webgl-build
          path: build/WebGL/office-mice/
          retention-days: 7

      - name: Upload Build Report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: build-report
          path: |
            MapGenerationReport.txt
            build/WebGL/office-mice/Build/report.json

  # Job 3: Deploy to Cloudflare
  deploy-cloudflare:
    name: Deploy to Cloudflare Workers
    runs-on: ubuntu-latest
    needs: build-webgl
    if: github.event_name == 'push' && (github.ref == 'refs/heads/master' || github.ref == 'refs/heads/main')

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4

      - name: Download Build Artifacts
        uses: actions/download-artifact@v4
        with:
          name: webgl-build
          path: build/WebGL/office-mice/

      - name: Deploy to Cloudflare Workers
        uses: cloudflare/wrangler-action@v3
        with:
          apiToken: ${{ secrets.CLOUDFLARE_API_TOKEN }}
          accountId: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
          command: deploy
          wranglerVersion: "4.47.0"

      - name: Output Deployment URL
        run: |
          echo "🚀 Deployment successful!"
          echo "Your game is live at: https://office-mice.pages.dev"
          echo "Build SHA: ${{ github.sha }}"
          echo "Deployed at: $(date -u +'%Y-%m-%d %H:%M:%S UTC')"

      - name: Comment on PR (if PR)
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '🎮 **Preview Deployment Ready**\n\nYour Office-Mice build is live at: https://office-mice-pr-${{ github.event.number }}.pages.dev'
            })

  # Job 4: Performance Benchmarks
  performance-test:
    name: Performance Benchmarks
    runs-on: ubuntu-latest
    needs: build-webgl

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4

      - name: Download Build Artifacts
        uses: actions/download-artifact@v4
        with:
          name: webgl-build
          path: build/WebGL/office-mice/

      - name: Measure Build Size
        run: |
          echo "## Build Size Metrics" >> performance-report.md
          echo "" >> performance-report.md
          echo "| File | Size |" >> performance-report.md
          echo "|------|------|" >> performance-report.md

          cd build/WebGL/office-mice/Build/

          for file in *.{data,wasm,js,gz,br}; do
            if [ -f "$file" ]; then
              size=$(du -h "$file" | cut -f1)
              echo "| $file | $size |" >> ../../../../performance-report.md
            fi
          done

          cd ../../../../

          total_size=$(du -sh build/WebGL/office-mice/ | cut -f1)
          echo "" >> performance-report.md
          echo "**Total Build Size:** $total_size" >> performance-report.md

      - name: Upload Performance Report
        uses: actions/upload-artifact@v4
        with:
          name: performance-report
          path: performance-report.md

      - name: Comment Performance Metrics on PR
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            const report = fs.readFileSync('performance-report.md', 'utf8');

            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '📊 **Performance Metrics**\n\n' + report
            })
```

### 7.2 Build Automation Scripts

**Purpose:** Command-line validation and build scripts for CI/CD.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Command-line validation for CI/CD pipelines
/// </summary>
public static class MapGenerationValidator
{
    [MenuItem("Tools/Validate Map Generation (Command Line)")]
    public static void ValidateFromCommandLine()
    {
        Debug.Log("[CI] Starting map generation validation...");

        bool isValid = true;
        StreamWriter report = new StreamWriter("validation-report.txt");

        try
        {
            report.WriteLine("Map Generation Validation Report");
            report.WriteLine("=================================");
            report.WriteLine($"Timestamp: {System.DateTime.Now}");
            report.WriteLine();

            // Validate Room Templates
            isValid &= ValidateRoomTemplatesCI(report);

            // Validate Spawn Tables
            isValid &= ValidateSpawnTablesCI(report);

            // Validate Biomes
            isValid &= ValidateBiomeConfigsCI(report);

            // Validate Game Config
            isValid &= ValidateGameConfigCI(report);

            if (isValid)
            {
                report.WriteLine();
                report.WriteLine("✓ VALIDATION PASSED");
                Debug.Log("[CI] Validation passed!");
            }
            else
            {
                report.WriteLine();
                report.WriteLine("✗ VALIDATION FAILED");
                Debug.LogError("[CI] Validation failed! Check validation-report.txt for details.");
                EditorApplication.Exit(1); // Exit with error code
            }
        }
        finally
        {
            report.Close();
        }
    }

    private static bool ValidateRoomTemplatesCI(StreamWriter report)
    {
        var templates = Resources.LoadAll<RoomTemplate>("");

        report.WriteLine($"Room Templates: {templates.Length} found");
        report.WriteLine("-------------------------------------------");

        bool allValid = true;

        foreach (var template in templates)
        {
            var result = template.Validate();

            if (!result.IsValid)
            {
                report.WriteLine($"✗ {template.name}: FAILED");
                foreach (var error in result.Errors)
                {
                    report.WriteLine($"    - {error}");
                }
                allValid = false;
            }
            else
            {
                report.WriteLine($"✓ {template.name}: OK");
            }
        }

        report.WriteLine();
        return allValid;
    }

    private static bool ValidateSpawnTablesCI(StreamWriter report)
    {
        var spawnTables = Resources.LoadAll<SpawnTable>("");

        report.WriteLine($"Spawn Tables: {spawnTables.Length} found");
        report.WriteLine("-------------------------------------------");

        bool allValid = true;

        foreach (var table in spawnTables)
        {
            if (table.entries == null || table.entries.Count == 0)
            {
                report.WriteLine($"✗ {table.name}: No entries");
                allValid = false;
            }
            else
            {
                report.WriteLine($"✓ {table.name}: {table.entries.Count} entries");
            }
        }

        report.WriteLine();
        return allValid;
    }

    private static bool ValidateBiomeConfigsCI(StreamWriter report)
    {
        var biomes = Resources.LoadAll<BiomeConfig>("");

        report.WriteLine($"Biome Configs: {biomes.Length} found");
        report.WriteLine("-------------------------------------------");

        bool allValid = true;

        foreach (var biome in biomes)
        {
            bool valid = true;

            if (biome.floorTileset == null)
            {
                report.WriteLine($"✗ {biome.name}: Missing floor tileset");
                valid = false;
            }

            if (biome.wallTileset == null)
            {
                report.WriteLine($"✗ {biome.name}: Missing wall tileset");
                valid = false;
            }

            if (valid)
            {
                report.WriteLine($"✓ {biome.name}: OK");
            }

            allValid &= valid;
        }

        report.WriteLine();
        return allValid;
    }

    private static bool ValidateGameConfigCI(StreamWriter report)
    {
        var gameConfig = Resources.Load<GameConfig>("GameConfig");

        report.WriteLine("Game Config");
        report.WriteLine("-------------------------------------------");

        if (gameConfig == null)
        {
            report.WriteLine("✗ GameConfig not found in Resources");
            report.WriteLine();
            return false;
        }

        bool isValid = true;

        if (gameConfig.biomeRegistry == null)
        {
            report.WriteLine("✗ Missing BiomeRegistry");
            isValid = false;
        }

        if (gameConfig.spawnTableRegistry == null)
        {
            report.WriteLine("✗ Missing SpawnTableRegistry");
            isValid = false;
        }

        if (gameConfig.roomTemplateLibrary == null)
        {
            report.WriteLine("✗ Missing RoomTemplateLibrary");
            isValid = false;
        }

        if (isValid)
        {
            report.WriteLine("✓ GameConfig: OK");
        }

        report.WriteLine();
        return isValid;
    }
}

/// <summary>
/// Post-build processor for generating reports
/// </summary>
public static class BuildPostProcessor
{
    [MenuItem("Tools/Post-Build Processing")]
    public static void OnPostBuild()
    {
        Debug.Log("[Build] Running post-build processing...");

        // Generate build report
        using (StreamWriter writer = new StreamWriter("MapGenerationReport.txt"))
        {
            writer.WriteLine("Map Generation Build Report");
            writer.WriteLine("============================");
            writer.WriteLine($"Build Time: {System.DateTime.Now}");
            writer.WriteLine();

            // Content statistics
            writer.WriteLine("Content Statistics:");

            var roomTemplates = Resources.LoadAll<RoomTemplate>("");
            writer.WriteLine($"  Room Templates: {roomTemplates.Length}");

            var spawnTables = Resources.LoadAll<SpawnTable>("");
            writer.WriteLine($"  Spawn Tables: {spawnTables.Length}");

            var biomes = Resources.LoadAll<BiomeConfig>("");
            writer.WriteLine($"  Biome Configs: {biomes.Length}");
        }

        Debug.Log("[Build] Post-build processing complete!");
    }
}
```

---

## 8. Production Monitoring and Analytics

### 8.1 Unity Analytics Integration

**Purpose:** Track map generation metrics in production.

```csharp
using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;

/// <summary>
/// Analytics tracking for map generation
/// </summary>
public class MapGenerationAnalytics : MonoBehaviour
{
    private static MapGenerationAnalytics _instance;
    public static MapGenerationAnalytics Instance => _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Tracks map generation event
    /// </summary>
    public void TrackMapGeneration(MapGenerationContext context)
    {
        var parameters = new Dictionary<string, object>
        {
            { "seed", context.Seed },
            { "room_count", context.Rooms?.Count ?? 0 },
            { "corridor_count", context.Corridors?.Count ?? 0 },
            { "spawn_point_count", context.SpawnPoints?.Count ?? 0 },
            { "resource_count", context.PlacedResources?.Count ?? 0 },
            { "biome", context.SelectedBiome?.biomeName ?? "unknown" },
            { "map_size_x", context.Rooms?.Max(r => r.Bounds.xMax) ?? 0 },
            { "map_size_y", context.Rooms?.Max(r => r.Bounds.yMax) ?? 0 }
        };

        AnalyticsService.Instance.CustomData("map_generated", parameters);
        AnalyticsService.Instance.Flush();
    }

    /// <summary>
    /// Tracks generation failure
    /// </summary>
    public void TrackGenerationFailure(string errorMessage)
    {
        var parameters = new Dictionary<string, object>
        {
            { "error_message", errorMessage },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };

        AnalyticsService.Instance.CustomData("map_generation_failed", parameters);
        AnalyticsService.Instance.Flush();
    }

    /// <summary>
    /// Tracks generation performance
    /// </summary>
    public void TrackGenerationPerformance(float generationTime, int roomCount)
    {
        var parameters = new Dictionary<string, object>
        {
            { "generation_time_ms", generationTime * 1000f },
            { "room_count", roomCount },
            { "avg_time_per_room", (generationTime / roomCount) * 1000f }
        };

        AnalyticsService.Instance.CustomData("map_generation_performance", parameters);
        AnalyticsService.Instance.Flush();
    }
}
```

### 8.2 Error Tracking

**Purpose:** Production error logging and crash reporting.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Error tracking for map generation system
/// Integrates with external error tracking services
/// </summary>
public class MapGenerationErrorTracker : MonoBehaviour
{
    private static MapGenerationErrorTracker _instance;
    public static MapGenerationErrorTracker Instance => _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Application.logMessageReceived += HandleLog;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Only track errors from map generation
        if (!logString.Contains("[MapGen]") && !logString.Contains("MapGenerator"))
            return;

        if (type == LogType.Error || type == LogType.Exception)
        {
            TrackError(logString, stackTrace, type);
        }
    }

    public void TrackError(string message, string stackTrace, LogType type)
    {
        var errorData = new Dictionary<string, object>
        {
            { "message", message },
            { "stack_trace", stackTrace },
            { "log_type", type.ToString() },
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "unity_version", Application.unityVersion },
            { "platform", Application.platform.ToString() }
        };

        // Log to console
        Debug.LogError($"[MapGen Error Tracker] {message}");

        // Send to external service (e.g., Sentry, Bugsnag)
        // Implementation depends on chosen service
    }

    public void TrackWarning(string message, Dictionary<string, object> context = null)
    {
        Debug.LogWarning($"[MapGen Warning] {message}");

        // Track to analytics
        var warningData = new Dictionary<string, object>
        {
            { "message", message },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        if (context != null)
        {
            foreach (var kvp in context)
            {
                warningData[kvp.Key] = kvp.Value;
            }
        }
    }
}
```

### 8.3 Performance Monitoring

**Purpose:** Track generation performance metrics in production.

```csharp
using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

/// <summary>
/// Performance monitoring for map generation
/// </summary>
public class MapGenerationPerformanceMonitor : MonoBehaviour
{
    private static MapGenerationPerformanceMonitor _instance;
    public static MapGenerationPerformanceMonitor Instance => _instance;

    private Dictionary<string, Stopwatch> activeTimers = new Dictionary<string, Stopwatch>();
    private Dictionary<string, List<float>> performanceMetrics = new Dictionary<string, List<float>>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Starts a performance timer
    /// </summary>
    public void StartTimer(string timerName)
    {
        if (!activeTimers.ContainsKey(timerName))
        {
            activeTimers[timerName] = new Stopwatch();
        }

        activeTimers[timerName].Restart();
    }

    /// <summary>
    /// Stops a performance timer and records the metric
    /// </summary>
    public float StopTimer(string timerName)
    {
        if (!activeTimers.ContainsKey(timerName))
        {
            UnityEngine.Debug.LogWarning($"[PerfMonitor] Timer '{timerName}' not found");
            return 0f;
        }

        activeTimers[timerName].Stop();
        float elapsed = (float)activeTimers[timerName].Elapsed.TotalSeconds;

        // Record metric
        if (!performanceMetrics.ContainsKey(timerName))
        {
            performanceMetrics[timerName] = new List<float>();
        }
        performanceMetrics[timerName].Add(elapsed);

        return elapsed;
    }

    /// <summary>
    /// Gets performance statistics for a metric
    /// </summary>
    public PerformanceStats GetStats(string timerName)
    {
        if (!performanceMetrics.ContainsKey(timerName) || performanceMetrics[timerName].Count == 0)
        {
            return new PerformanceStats();
        }

        var metrics = performanceMetrics[timerName];

        float min = float.MaxValue;
        float max = float.MinValue;
        float sum = 0f;

        foreach (var value in metrics)
        {
            if (value < min) min = value;
            if (value > max) max = value;
            sum += value;
        }

        return new PerformanceStats
        {
            Count = metrics.Count,
            Min = min,
            Max = max,
            Average = sum / metrics.Count,
            Total = sum
        };
    }

    /// <summary>
    /// Prints performance report to console
    /// </summary>
    public void PrintReport()
    {
        UnityEngine.Debug.Log("=== Map Generation Performance Report ===");

        foreach (var kvp in performanceMetrics)
        {
            var stats = GetStats(kvp.Key);
            UnityEngine.Debug.Log($"{kvp.Key}:");
            UnityEngine.Debug.Log($"  Count: {stats.Count}");
            UnityEngine.Debug.Log($"  Min: {stats.Min:F3}s");
            UnityEngine.Debug.Log($"  Max: {stats.Max:F3}s");
            UnityEngine.Debug.Log($"  Avg: {stats.Average:F3}s");
            UnityEngine.Debug.Log($"  Total: {stats.Total:F3}s");
        }

        UnityEngine.Debug.Log("==========================================");
    }

    /// <summary>
    /// Clears all performance metrics
    /// </summary>
    public void ClearMetrics()
    {
        performanceMetrics.Clear();
        activeTimers.Clear();
    }
}

public struct PerformanceStats
{
    public int Count;
    public float Min;
    public float Max;
    public float Average;
    public float Total;
}

/// <summary>
/// Example usage in MapGenerator
/// </summary>
public partial class MapGenerator
{
    public MapGenerationContext GenerateMap(int seed, Vector2Int mapSize, int minRoomSize, int maxRoomSize)
    {
        // Start overall timer
        MapGenerationPerformanceMonitor.Instance.StartTimer("TotalGeneration");

        try
        {
            // BSP Generation
            MapGenerationPerformanceMonitor.Instance.StartTimer("BSPGeneration");
            var rooms = GenerateBSP(mapSize, minRoomSize, maxRoomSize);
            float bspTime = MapGenerationPerformanceMonitor.Instance.StopTimer("BSPGeneration");

            // Corridor Generation
            MapGenerationPerformanceMonitor.Instance.StartTimer("CorridorGeneration");
            var corridors = GenerateCorridors(rooms);
            float corridorTime = MapGenerationPerformanceMonitor.Instance.StopTimer("CorridorGeneration");

            // Content Population
            MapGenerationPerformanceMonitor.Instance.StartTimer("ContentPopulation");
            PopulateContent(rooms, corridors);
            float contentTime = MapGenerationPerformanceMonitor.Instance.StopTimer("ContentPopulation");

            // Stop overall timer
            float totalTime = MapGenerationPerformanceMonitor.Instance.StopTimer("TotalGeneration");

            // Track analytics
            MapGenerationAnalytics.Instance.TrackGenerationPerformance(totalTime, rooms.Count);

            Debug.Log($"[MapGen] Generation completed in {totalTime:F3}s " +
                     $"(BSP: {bspTime:F3}s, Corridors: {corridorTime:F3}s, Content: {contentTime:F3}s)");

            return context;
        }
        catch (System.Exception e)
        {
            MapGenerationPerformanceMonitor.Instance.StopTimer("TotalGeneration");
            MapGenerationErrorTracker.Instance.TrackError(e.Message, e.StackTrace, LogType.Exception);
            throw;
        }
    }
}
```

---

## Conclusion

Phase 3 Part 2 (Editor Tools & Production) completes the **developer experience and operational excellence** layer of the Office-Mice map generation system. This phase delivers:

### Key Achievements

1. **Custom Editor Windows**
   - Intuitive visual interface for map generation
   - Real-time preview and validation
   - Designer-friendly workflow

2. **Gizmo Visualization**
   - Runtime debugging in Scene view
   - Color-coded room classification
   - Interactive spatial analysis

3. **Inspector Customization**
   - Custom property drawers for complex types
   - Enhanced ScriptableObject editing
   - Automated utilities and validation

4. **Asset Creation Workflows**
   - Step-by-step template wizard
   - Batch operations for efficiency
   - Tilemap import functionality

5. **Build Pipeline Integration**
   - Pre-build validation prevents broken builds
   - Post-build reporting
   - GitHub Actions CI/CD automation

6. **Production Infrastructure**
   - Automated Unity WebGL builds
   - Cloudflare Workers deployment
   - Performance benchmarking

7. **Monitoring & Analytics**
   - Unity Analytics integration
   - Error tracking and logging
   - Performance monitoring

### Production Readiness

**Current CI/CD Pipeline:**
- ✓ Automated validation of map generation assets
- ✓ Unity WebGL builds with caching (50%+ speedup)
- ✓ Cloudflare Workers deployment
- ✓ Build size optimization (Brotli compression)
- ✓ Performance reporting
- ✓ PR preview deployments

**Deployment Target:** https://office-mice.pages.dev

**Build Performance:**
- Unity Library caching reduces build time by 50%+
- Git LFS caching for faster asset checkout
- Disk space optimization for GitHub Actions runners

### Next Steps

**Recommended Enhancements:**

1. **Editor Tools:**
   - Tilemap painting tools for room template creation
   - Visual room template editor with drag-and-drop
   - In-editor map testing and iteration

2. **Testing:**
   - Automated Unity Test Runner integration
   - PlayMode tests for generation validation
   - Load testing for large maps

3. **Analytics:**
   - Integration with Google Analytics for web builds
   - Heatmaps for player navigation patterns
   - A/B testing for map generation parameters

4. **Monitoring:**
   - Integration with Sentry or Bugsnag for error tracking
   - Real-time performance dashboards
   - Alert system for critical failures

---

**Document Status:** ✅ Complete
**Review Required:** Lead Engineer, DevOps Team
**Implementation Target:** Q1 2026
**Estimated Effort:** 5-7 days (Days 13-15 from original plan + enhancements)

---

**References:**
- MAP_GENERATION_PLAN.md (Phases 1-2 foundation)
- PHASE_2_ARCHITECTURE_DEEP_DIVE.md (Content systems)
- GitHub Actions: game-ci/unity-builder documentation
- Cloudflare Workers: Deployment best practices

**Version History:**
- 1.0 (2025-11-17): Initial comprehensive analysis
