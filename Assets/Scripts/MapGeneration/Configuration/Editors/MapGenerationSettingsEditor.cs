using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Linq;

namespace OfficeMice.MapGeneration.Configuration.Editors
{
    [CustomEditor(typeof(MapGenerationSettings))]
    public class MapGenerationSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingsIDProp;
        private SerializedProperty _settingsNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _profileProp;
        private SerializedProperty _mapConfigProp;
        private SerializedProperty _bspConfigProp;
        private SerializedProperty _corridorConfigProp;
        private SerializedProperty _roomTemplatesProp;
        private SerializedProperty _biomeConfigurationsProp;
        private SerializedProperty _spawnTablesProp;
        private SerializedProperty _tilesetsProp;
        private SerializedProperty _generationRulesProp;
        private SerializedProperty _validationRulesProp;
        private SerializedProperty _performanceSettingsProp;
        private SerializedProperty _runtimeConfigProp;
        private SerializedProperty _debugSettingsProp;
        private SerializedProperty _qualitySettingsProp;
        private SerializedProperty _allowRuntimeModificationProp;
        
        private bool _showValidation = false;
        private bool _showConfigurationSummary = false;
        private bool _showQuickSetup = false;
        private ValidationResult _lastValidationResult;
        
        private void OnEnable()
        {
            _settingsIDProp = serializedObject.FindProperty("_settingsID");
            _settingsNameProp = serializedObject.FindProperty("_settingsName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _profileProp = serializedObject.FindProperty("_profile");
            _mapConfigProp = serializedObject.FindProperty("_mapConfig");
            _bspConfigProp = serializedObject.FindProperty("_bspConfig");
            _corridorConfigProp = serializedObject.FindProperty("_corridorConfig");
            _roomTemplatesProp = serializedObject.FindProperty("_roomTemplates");
            _biomeConfigurationsProp = serializedObject.FindProperty("_biomeConfigurations");
            _spawnTablesProp = serializedObject.FindProperty("_spawnTables");
            _tilesetsProp = serializedObject.FindProperty("_tilesets");
            _generationRulesProp = serializedObject.FindProperty("_generationRules");
            _validationRulesProp = serializedObject.FindProperty("_validationRules");
            _performanceSettingsProp = serializedObject.FindProperty("_performanceSettings");
            _runtimeConfigProp = serializedObject.FindProperty("_runtimeConfig");
            _debugSettingsProp = serializedObject.FindProperty("_debugSettings");
            _qualitySettingsProp = serializedObject.FindProperty("_qualitySettings");
            _allowRuntimeModificationProp = serializedObject.FindProperty("_allowRuntimeModification");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var settings = (MapGenerationSettings)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw identity section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_settingsNameProp, new GUIContent("Settings Name", "Human-readable name for these settings"));
            EditorGUILayout.PropertyField(_settingsIDProp, new GUIContent("Settings ID", "Unique identifier (auto-generated from name)"));
            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("Description", "Detailed description of these settings"));
            EditorGUILayout.PropertyField(_profileProp, new GUIContent("Profile", "Target profile for these settings"));
            
            // Draw quick setup section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            _showQuickSetup = EditorGUILayout.Foldout(_showQuickSetup, "Quick Setup Options");
            
            if (_showQuickSetup)
            {
                DrawQuickSetup(settings);
            }
            
            // Draw configuration sections
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Core Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mapConfigProp, new GUIContent("Map Configuration", "Basic map generation parameters"));
            EditorGUILayout.PropertyField(_bspConfigProp, new GUIContent("BSP Configuration", "Binary space partitioning settings"));
            EditorGUILayout.PropertyField(_corridorConfigProp, new GUIContent("Corridor Configuration", "Corridor generation settings"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Content Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_roomTemplatesProp, new GUIContent("Room Templates", "Available room templates"), true);
            EditorGUILayout.PropertyField(_biomeConfigurationsProp, new GUIContent("Biome Configurations", "Available biome configurations"), true);
            EditorGUILayout.PropertyField(_spawnTablesProp, new GUIContent("Spawn Tables", "Enemy spawn configurations"), true);
            EditorGUILayout.PropertyField(_tilesetsProp, new GUIContent("Tilesets", "Available tileset configurations"), true);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rules and Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_generationRulesProp, new GUIContent("Generation Rules", "High-level generation rules"));
            EditorGUILayout.PropertyField(_validationRulesProp, new GUIContent("Validation Rules", "Map validation settings"));
            EditorGUILayout.PropertyField(_performanceSettingsProp, new GUIContent("Performance Settings", "Performance optimization settings"));
            EditorGUILayout.PropertyField(_runtimeConfigProp, new GUIContent("Runtime Configuration", "Runtime behavior settings"));
            EditorGUILayout.PropertyField(_debugSettingsProp, new GUIContent("Debug Settings", "Debug and development settings"));
            EditorGUILayout.PropertyField(_qualitySettingsProp, new GUIContent("Quality Settings", "Quality and optimization settings"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_allowRuntimeModificationProp, new GUIContent("Allow Runtime Modification", "Allow settings to be modified at runtime"));
            
            // Draw configuration summary section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
            _showConfigurationSummary = EditorGUILayout.Foldout(_showConfigurationSummary, "Show Configuration Summary");
            
            if (_showConfigurationSummary)
            {
                DrawConfigurationSummary(settings);
            }
            
            // Draw validation section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Show Validation Results");
            if (EditorGUI.EndChangeCheck() || _showValidation)
            {
                _lastValidationResult = settings.Validate();
            }
            
            if (_showValidation && _lastValidationResult != null)
            {
                DrawValidationResults(_lastValidationResult);
            }
            
            // Draw utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Configuration"))
            {
                _lastValidationResult = settings.Validate();
                _showValidation = true;
                EditorUtility.DisplayDialog("Validation Complete", _lastValidationResult.GetSummary(), "OK");
            }
            
            if (GUILayout.Button("Test Configuration"))
            {
                TestConfiguration(settings);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Default Assets"))
            {
                CreateDefaultAssets();
            }
            
            if (GUILayout.Button("Export Configuration"))
            {
                ExportConfiguration(settings);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset to Defaults", "Are you sure you want to reset all settings to defaults?", "Yes", "No"))
                {
                    ResetToDefaults(settings);
                }
            }
            
            if (GUILayout.Button("Generate Test Map"))
            {
                GenerateTestMap(settings);
            }
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void DrawQuickSetup(MapGenerationSettings settings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("Quick setup options for common scenarios:", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Small Office"))
            {
                SetupSmallOffice(settings);
            }
            
            if (GUILayout.Button("Large Complex"))
            {
                SetupLargeComplex(settings);
            }
            
            if (GUILayout.Button("Mobile Optimized"))
            {
                SetupMobileOptimized(settings);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug Mode"))
            {
                SetupDebugMode(settings);
            }
            
            if (GUILayout.Button("Production Ready"))
            {
                SetupProductionReady(settings);
            }
            
            if (GUILayout.Button("High Quality"))
            {
                SetupHighQuality(settings);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawConfigurationSummary(MapGenerationSettings settings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField($"Profile: {settings.Profile}");
            EditorGUILayout.LabelField($"Room Templates: {settings.RoomTemplates.Count}");
            EditorGUILayout.LabelField($"Biome Configurations: {settings.BiomeConfigurations.Count}");
            EditorGUILayout.LabelField($"Spawn Tables: {settings.SpawnTables.Count}");
            EditorGUILayout.LabelField($"Tilesets: {settings.Tilesets.Count}");
            
            if (settings.MapConfig != null)
            {
                EditorGUILayout.LabelField($"Map Size Range: {settings.MapConfig.MapSizeRange}");
                EditorGUILayout.LabelField($"Room Count Range: {settings.MapConfig.MinRooms}-{settings.MapConfig.MaxRooms}");
            }
            
            if (settings.BSPConfig != null)
            {
                EditorGUILayout.LabelField($"BSP Max Depth: {settings.BSPConfig.MaxDepth}");
                EditorGUILayout.LabelField($"Min Partition Size: {settings.BSPConfig.MinPartitionSize}");
            }
            
            if (settings.CorridorConfig != null)
            {
                EditorGUILayout.LabelField($"Corridor Type: {settings.CorridorConfig.CorridorType}");
                EditorGUILayout.LabelField($"Corridor Width: {settings.CorridorConfig.MinWidth}-{settings.CorridorConfig.MaxWidth}");
            }
            
            EditorGUILayout.LabelField($"Runtime Modification: {(settings.AllowRuntimeModification ? "Allowed" : "Disabled")}");
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawValidationResults(ValidationResult result)
        {
            EditorGUI.indentLevel++;
            
            // Show validation status
            var statusColor = result.IsValid ? Color.green : Color.red;
            var statusText = result.IsValid ? "VALID" : "INVALID";
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", EditorStyles.boldLabel);
            GUI.color = originalColor;
            
            EditorGUILayout.LabelField($"Summary: {result.GetSummary()}");
            
            // Show errors
            if (result.Errors.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var error in result.Errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
                EditorGUI.indentLevel--;
            }
            
            // Show warnings
            if (result.Warnings.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var warning in result.Warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void TestConfiguration(MapGenerationSettings settings)
        {
            var result = settings.Validate();
            
            if (result.IsValid)
            {
                var message = $"Configuration is valid!\n\n" +
                             $"Room Templates: {settings.RoomTemplates.Count}\n" +
                             $"Biome Configurations: {settings.BiomeConfigurations.Count}\n" +
                             $"Spawn Tables: {settings.SpawnTables.Count}\n" +
                             $"Tilesets: {settings.Tilesets.Count}";
                
                EditorUtility.DisplayDialog("Configuration Test", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Configuration Test Failed", 
                    $"Configuration has {result.Errors.Count} errors and {result.Warnings.Count} warnings.\n\n" +
                    "Please check the validation results for details.", "OK");
            }
        }
        
        private void CreateDefaultAssets()
        {
            EditorUtility.DisplayDialog("Create Default Assets", 
                "This would create default room templates, biome configurations, spawn tables, and tilesets.\n\n" +
                "This is a placeholder for the full implementation.", "OK");
        }
        
        private void ExportConfiguration(MapGenerationSettings settings)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Export Configuration",
                $"{settings.SettingsName}_Config",
                "json",
                "Choose where to save the configuration"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Export Configuration", 
                    "Configuration export would be implemented here.\n\n" +
                    "This would serialize all settings to JSON format.", "OK");
            }
        }
        
        private void ResetToDefaults(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Reset Map Generation Settings to Defaults");
            
            // Reset to default values
            _profileProp.enumValueIndex = (int)GenerationProfile.Any;
            _allowRuntimeModificationProp.boolValue = false;
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
        
        private void GenerateTestMap(MapGenerationSettings settings)
        {
            EditorUtility.DisplayDialog("Generate Test Map", 
                "Test map generation would be implemented here.\n\n" +
                "This would use the current settings to generate a test map and show the results.", "OK");
        }
        
        // Quick setup methods
        private void SetupSmallOffice(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup Small Office");
            
            // Configure for small office maps
            if (settings.MapConfig != null)
            {
                var mapConfig = settings.MapConfig;
                mapConfig.MapSizeRange = new Vector2Int(30, 50);
                mapConfig.MinRooms = 3;
                mapConfig.MaxRooms = 8;
                mapConfig.RoomSizeRange = new Vector2Int(4, 8);
            }
            
            EditorUtility.DisplayDialog("Small Office Setup", "Configuration updated for small office maps", "OK");
        }
        
        private void SetupLargeComplex(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup Large Complex");
            
            // Configure for large complex maps
            if (settings.MapConfig != null)
            {
                var mapConfig = settings.MapConfig;
                mapConfig.MapSizeRange = new Vector2Int(100, 200);
                mapConfig.MinRooms = 15;
                mapConfig.MaxRooms = 40;
                mapConfig.RoomSizeRange = new Vector2Int(6, 20);
            }
            
            EditorUtility.DisplayDialog("Large Complex Setup", "Configuration updated for large complex maps", "OK");
        }
        
        private void SetupMobileOptimized(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup Mobile Optimized");
            
            // Configure for mobile optimization
            if (settings.PerformanceSettings != null)
            {
                var perfSettings = settings.PerformanceSettings;
                perfSettings.EnableMultithreading = false;
                perfSettings.EnableLOD = true;
                perfSettings.PoolObjects = true;
            }
            
            if (settings.QualitySettings != null)
            {
                var qualitySettings = settings.QualitySettings;
                qualitySettings.Quality = GenerationQuality.Low;
                qualitySettings.AdaptiveQuality = true;
            }
            
            EditorUtility.DisplayDialog("Mobile Optimized Setup", "Configuration updated for mobile optimization", "OK");
        }
        
        private void SetupDebugMode(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup Debug Mode");
            
            // Configure for debug mode
            if (settings.DebugSettings != null)
            {
                var debugSettings = settings.DebugSettings;
                debugSettings.ShowGizmos = true;
                debugSettings.ShowRoomLabels = true;
                debugSettings.ShowConnectivity = true;
                debugSettings.ColorizeRooms = true;
                debugSettings.EnableLogging = true;
                debugSettings.LogGenerationSteps = true;
                debugSettings.EnableTestMode = true;
            }
            
            EditorUtility.DisplayDialog("Debug Mode Setup", "Configuration updated for debug mode", "OK");
        }
        
        private void SetupProductionReady(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup Production Ready");
            
            // Configure for production
            if (settings.DebugSettings != null)
            {
                var debugSettings = settings.DebugSettings;
                debugSettings.EnableLogging = false;
                debugSettings.LogGenerationSteps = false;
                debugSettings.EnableTestMode = false;
            }
            
            if (settings.ValidationRules != null)
            {
                var validationRules = settings.ValidationRules;
                validationRules.RejectInvalidMaps = true;
                validationRules.MaxRetryAttempts = 3;
            }
            
            EditorUtility.DisplayDialog("Production Ready Setup", "Configuration updated for production", "OK");
        }
        
        private void SetupHighQuality(MapGenerationSettings settings)
        {
            Undo.RecordObject(settings, "Setup High Quality");
            
            // Configure for high quality
            if (settings.QualitySettings != null)
            {
                var qualitySettings = settings.QualitySettings;
                qualitySettings.Quality = GenerationQuality.Ultra;
                qualitySettings.DecorationQuality = 1.0f;
                qualitySettings.LightingQuality = 1.0f;
                qualitySettings.EffectsQuality = 1.0f;
            }
            
            EditorUtility.DisplayDialog("High Quality Setup", "Configuration updated for high quality", "OK");
        }
    }
}