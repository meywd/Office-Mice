using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration.Editors
{
    [CustomEditor(typeof(TilesetConfiguration))]
    public class TilesetConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty _tilesetIDProp;
        private SerializedProperty _tilesetNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _themeProp;
        private SerializedProperty _floorTilesProp;
        private SerializedProperty _wallTilesProp;
        private SerializedProperty _ceilingTilesProp;
        private SerializedProperty _doorTilesProp;
        private SerializedProperty _windowTilesProp;
        private SerializedProperty _decorativeTilesProp;
        private SerializedProperty _decorationDensityProp;
        private SerializedProperty _randomizeDecorationsProp;
        private SerializedProperty _hazardTilesProp;
        private SerializedProperty _interactiveTilesProp;
        private SerializedProperty _spawnTilesProp;
        private SerializedProperty _objectiveTilesProp;
        private SerializedProperty _tileVariationsProp;
        private SerializedProperty _variationChanceProp;
        private SerializedProperty _useVariationsForFloorsProp;
        private SerializedProperty _useVariationsForWallsProp;
        private SerializedProperty _tileRulesProp;
        private SerializedProperty _applyRulesAutomaticallyProp;
        private SerializedProperty _fallbackTileProp;
        private SerializedProperty _tilesetTextureProp;
        private SerializedProperty _tileSizeProp;
        private SerializedProperty _tilesPerRowProp;
        private SerializedProperty _enableTileCollidersProp;
        private SerializedProperty _tilePhysicsMaterialProp;
        private SerializedProperty _useTilemapCollider2DProp;
        
        private bool _showValidation = false;
        private bool _showTilePreview = false;
        private ValidationResult _lastValidationResult;
        
        private void OnEnable()
        {
            _tilesetIDProp = serializedObject.FindProperty("_tilesetID");
            _tilesetNameProp = serializedObject.FindProperty("_tilesetName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _themeProp = serializedObject.FindProperty("_theme");
            _floorTilesProp = serializedObject.FindProperty("_floorTiles");
            _wallTilesProp = serializedObject.FindProperty("_wallTiles");
            _ceilingTilesProp = serializedObject.FindProperty("_ceilingTiles");
            _doorTilesProp = serializedObject.FindProperty("_doorTiles");
            _windowTilesProp = serializedObject.FindProperty("_windowTiles");
            _decorativeTilesProp = serializedObject.FindProperty("_decorativeTiles");
            _decorationDensityProp = serializedObject.FindProperty("_decorationDensity");
            _randomizeDecorationsProp = serializedObject.FindProperty("_randomizeDecorations");
            _hazardTilesProp = serializedObject.FindProperty("_hazardTiles");
            _interactiveTilesProp = serializedObject.FindProperty("_interactiveTiles");
            _spawnTilesProp = serializedObject.FindProperty("_spawnTiles");
            _objectiveTilesProp = serializedObject.FindProperty("_objectiveTiles");
            _tileVariationsProp = serializedObject.FindProperty("_tileVariations");
            _variationChanceProp = serializedObject.FindProperty("_variationChance");
            _useVariationsForFloorsProp = serializedObject.FindProperty("_useVariationsForFloors");
            _useVariationsForWallsProp = serializedObject.FindProperty("_useVariationsForWalls");
            _tileRulesProp = serializedObject.FindProperty("_tileRules");
            _applyRulesAutomaticallyProp = serializedObject.FindProperty("_applyRulesAutomatically");
            _fallbackTileProp = serializedObject.FindProperty("_fallbackTile");
            _tilesetTextureProp = serializedObject.FindProperty("_tilesetTexture");
            _tileSizeProp = serializedObject.FindProperty("_tileSize");
            _tilesPerRowProp = serializedObject.FindProperty("_tilesPerRow");
            _enableTileCollidersProp = serializedObject.FindProperty("_enableTileColliders");
            _tilePhysicsMaterialProp = serializedObject.FindProperty("_tilePhysicsMaterial");
            _useTilemapCollider2DProp = serializedObject.FindProperty("_useTilemapCollider2D");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var tilesetConfig = (TilesetConfiguration)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw identity section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tilesetNameProp, new GUIContent("Tileset Name", "Human-readable name for this tileset"));
            EditorGUILayout.PropertyField(_tilesetIDProp, new GUIContent("Tileset ID", "Unique identifier (auto-generated from name)"));
            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("Description", "Detailed description of this tileset"));
            EditorGUILayout.PropertyField(_themeProp, new GUIContent("Theme", "Theme category for this tileset"));
            
            // Draw core tiles section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Core Tiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_floorTilesProp, new GUIContent("Floor Tiles", "Tiles for floor surfaces"));
            EditorGUILayout.PropertyField(_wallTilesProp, new GUIContent("Wall Tiles", "Tiles for wall surfaces"));
            EditorGUILayout.PropertyField(_ceilingTilesProp, new GUIContent("Ceiling Tiles", "Tiles for ceiling surfaces"));
            EditorGUILayout.PropertyField(_doorTilesProp, new GUIContent("Door Tiles", "Tiles for doorways"));
            EditorGUILayout.PropertyField(_windowTilesProp, new GUIContent("Window Tiles", "Tiles for windows"));
            
            // Draw decorative tiles section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Decorative Tiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_decorativeTilesProp, new GUIContent("Decorative Tiles", "Optional decorative tile mappings"), true);
            EditorGUILayout.PropertyField(_decorationDensityProp, new GUIContent("Decoration Density", "Density of decorative tiles (0-1)"));
            EditorGUILayout.PropertyField(_randomizeDecorationsProp, new GUIContent("Randomize Decorations", "Randomly place decorative tiles"));
            
            // Draw special tiles section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Special Tiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_hazardTilesProp, new GUIContent("Hazard Tiles", "Tiles that represent hazards"));
            EditorGUILayout.PropertyField(_interactiveTilesProp, new GUIContent("Interactive Tiles", "Tiles that can be interacted with"));
            EditorGUILayout.PropertyField(_spawnTilesProp, new GUIContent("Spawn Tiles", "Tiles that mark spawn points"));
            EditorGUILayout.PropertyField(_objectiveTilesProp, new GUIContent("Objective Tiles", "Tiles that mark objectives"));
            
            // Draw tile variations section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Variations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tileVariationsProp, new GUIContent("Tile Variations", "Variations that can be applied to tiles"), true);
            EditorGUILayout.PropertyField(_variationChanceProp, new GUIContent("Variation Chance", "Chance to apply variations (0-1)"));
            EditorGUILayout.PropertyField(_useVariationsForFloorsProp, new GUIContent("Use Variations for Floors", "Apply variations to floor tiles"));
            EditorGUILayout.PropertyField(_useVariationsForWallsProp, new GUIContent("Use Variations for Walls", "Apply variations to wall tiles"));
            
            // Draw tile rules section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tileRulesProp, new GUIContent("Tile Rules", "Rules for modifying tile placement"), true);
            EditorGUILayout.PropertyField(_applyRulesAutomaticallyProp, new GUIContent("Apply Rules Automatically", "Automatically apply tile rules during generation"));
            
            // Draw asset references section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Asset References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_fallbackTileProp, new GUIContent("Fallback Tile", "Tile to use when no specific tile is available"));
            EditorGUILayout.PropertyField(_tilesetTextureProp, new GUIContent("Tileset Texture", "Texture containing all tiles"));
            EditorGUILayout.PropertyField(_tileSizeProp, new GUIContent("Tile Size", "Size of individual tiles in pixels"));
            EditorGUILayout.PropertyField(_tilesPerRowProp, new GUIContent("Tiles Per Row", "Number of tiles in each row of the texture"));
            
            // Draw performance settings section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableTileCollidersProp, new GUIContent("Enable Tile Colliders", "Enable colliders for tiles"));
            if (_enableTileCollidersProp.boolValue)
            {
                EditorGUILayout.PropertyField(_tilePhysicsMaterialProp, new GUIContent("Tile Physics Material", "Physics material for tile colliders"));
                EditorGUILayout.PropertyField(_useTilemapCollider2DProp, new GUIContent("Use Tilemap Collider 2D", "Use TilemapCollider2D component"));
            }
            
            // Draw validation section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Show Validation Results");
            if (EditorGUI.EndChangeCheck() || _showValidation)
            {
                _lastValidationResult = tilesetConfig.Validate();
            }
            
            if (_showValidation && _lastValidationResult != null)
            {
                DrawValidationResults(_lastValidationResult);
            }
            
            // Draw tile preview section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Preview", EditorStyles.boldLabel);
            _showTilePreview = EditorGUILayout.Foldout(_showTilePreview, "Show Tile Preview");
            
            if (_showTilePreview)
            {
                DrawTilePreview(tilesetConfig);
            }
            
            // Draw utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Configuration"))
            {
                _lastValidationResult = tilesetConfig.Validate();
                _showValidation = true;
                EditorUtility.DisplayDialog("Validation Complete", _lastValidationResult.GetSummary(), "OK");
            }
            
            if (GUILayout.Button("Test Random Tile"))
            {
                var randomTile = tilesetConfig.GetTileForType(TileType.Floor);
                if (randomTile != null)
                {
                    EditorUtility.DisplayDialog("Random Tile", $"Selected tile: {randomTile.name}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No tiles available", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Configure from Texture"))
            {
                AutoConfigureFromTexture();
            }
            
            if (GUILayout.Button("Create Tile Mapping"))
            {
                CreateTileMapping();
            }
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
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
        
        private void DrawTilePreview(TilesetConfiguration tilesetConfig)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField($"Theme: {tilesetConfig.Theme}");
            EditorGUILayout.LabelField($"Floor Tiles: {tilesetConfig.FloorTiles?.Tiles?.Count ?? 0}");
            EditorGUILayout.LabelField($"Wall Tiles: {tilesetConfig.WallTiles?.Tiles?.Count ?? 0}");
            EditorGUILayout.LabelField($"Decorative Tiles: {tilesetConfig.DecorativeTiles.Count}");
            EditorGUILayout.LabelField($"Tile Variations: {tilesetConfig.TileVariations.Count}");
            EditorGUILayout.LabelField($"Tile Rules: {tilesetConfig.TileRules.Count}");
            EditorGUILayout.LabelField($"Tile Size: {tilesetConfig.TileSize}");
            EditorGUILayout.LabelField($"Tiles Per Row: {tilesetConfig.TilesPerRow}");
            EditorGUILayout.LabelField($"Decoration Density: {tilesetConfig.DecorationDensity:P0}");
            EditorGUILayout.LabelField($"Variation Chance: {tilesetConfig.VariationChance:P0}");
            
            // Show texture preview if available
            if (tilesetConfig.TilesetTexture != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Texture Preview:", EditorStyles.boldLabel);
                
                var textureRect = EditorGUILayout.GetControlRect(GUILayout.Height(100), GUILayout.Width(100));
                EditorGUI.DrawPreviewTexture(textureRect, tilesetConfig.TilesetTexture);
            }
            
            // Show fallback tile preview if available
            if (tilesetConfig.FallbackTile != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Fallback Tile:", EditorStyles.boldLabel);
                
                // Try to get a preview of the tile
                var tilePreview = AssetPreview.GetAssetPreview(tilesetConfig.FallbackTile);
                if (tilePreview != null)
                {
                    var previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(64), GUILayout.Width(64));
                    EditorGUI.DrawPreviewTexture(previewRect, tilePreview);
                }
                else
                {
                    EditorGUILayout.LabelField($"Fallback Tile: {tilesetConfig.FallbackTile.name}");
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void AutoConfigureFromTexture()
        {
            var tilesetConfig = (TilesetConfiguration)target;
            
            if (tilesetConfig.TilesetTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a tileset texture first", "OK");
                return;
            }
            
            Undo.RecordObject(tilesetConfig, "Auto-Configure from Texture");
            
            // This is a simplified auto-configuration
            // In a real implementation, you would analyze the texture and create appropriate tile mappings
            
            EditorUtility.DisplayDialog("Auto-Configure", "Auto-configuration is a simplified demo. In a full implementation, this would analyze the texture and create appropriate tile mappings.", "OK");
            
            EditorUtility.SetDirty(tilesetConfig);
        }
        
        private void CreateTileMapping()
        {
            var tilesetConfig = (TilesetConfiguration)target;
            
            Undo.RecordObject(tilesetConfig, "Create Tile Mapping");
            
            // Create a new tile mapping asset
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Tile Mapping",
                "NewTileMapping",
                "asset",
                "Choose where to save the tile mapping asset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                // This would create a new TileMapping asset
                // For now, just show a message
                EditorUtility.DisplayDialog("Create Tile Mapping", "Tile mapping creation would be implemented here", "OK");
            }
            
            EditorUtility.SetDirty(tilesetConfig);
        }
    }
    
    /// <summary>
    /// Custom property drawer for TileMapping.
    /// </summary>
    [CustomPropertyDrawer(typeof(TileMapping))]
    public class TileMappingPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var buttonWidth = 80f;
            var propertyWidth = position.width - buttonWidth - 5f;
            var propertyRect = new Rect(position.x, position.y, propertyWidth, position.height);
            var buttonRect = new Rect(position.x + propertyWidth + 5f, position.y, buttonWidth, position.height);
            
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
            
            if (GUI.Button(buttonRect, "Edit"))
            {
                // Open a detailed editor for the tile mapping
                var window = EditorWindow.GetWindow<TileMappingEditorWindow>();
                window.SetProperty(property);
                window.Show();
            }
            
            EditorGUI.EndProperty();
        }
    }
    
    /// <summary>
    /// Editor window for detailed tile mapping editing.
    /// </summary>
    public class TileMappingEditorWindow : EditorWindow
    {
        private SerializedProperty _property;
        private Vector2 _scrollPosition;
        
        public void SetProperty(SerializedProperty property)
        {
            _property = property;
        }
        
        private void OnGUI()
        {
            if (_property == null)
            {
                EditorGUILayout.LabelField("No tile mapping selected");
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField("Tile Mapping Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Draw all properties of the tile mapping
            _property.serializedObject.Update();
            
            var mappingNameProp = _property.FindPropertyRelative("_mappingName");
            var useRandomSelectionProp = _property.FindPropertyRelative("_useRandomSelection");
            var defaultIndexProp = _property.FindPropertyRelative("_defaultIndex");
            var tilesProp = _property.FindPropertyRelative("_tiles");
            
            EditorGUILayout.PropertyField(mappingNameProp);
            EditorGUILayout.PropertyField(useRandomSelectionProp);
            
            if (!useRandomSelectionProp.boolValue)
            {
                EditorGUILayout.PropertyField(defaultIndexProp);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tiles:", EditorStyles.boldLabel);
            
            // Draw tile entries with enhanced UI
            for (int i = 0; i < tilesProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var tileEntryProp = tilesProp.GetArrayElementAtIndex(i);
                var tileProp = tileEntryProp.FindPropertyRelative("_tile");
                var weightProp = tileEntryProp.FindPropertyRelative("_weight");
                var isWalkableProp = tileEntryProp.FindPropertyRelative("_isWalkable");
                var hasCollisionProp = tileEntryProp.FindPropertyRelative("_hasCollision");
                
                EditorGUILayout.PropertyField(tileProp, GUIContent.none, GUILayout.Width(150));
                EditorGUILayout.PropertyField(weightProp, GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.PropertyField(isWalkableProp, GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.PropertyField(hasCollisionProp, GUIContent.none, GUILayout.Width(60));
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    tilesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Tile"))
            {
                tilesProp.arraySize++;
                var newTileProp = tilesProp.GetArrayElementAtIndex(tilesProp.arraySize - 1);
                newTileProp.FindPropertyRelative("_weight").floatValue = 1.0f;
                newTileProp.FindPropertyRelative("_isWalkable").boolValue = true;
                newTileProp.FindPropertyRelative("_hasCollision").boolValue = false;
            }
            
            _property.serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.EndScrollView();
        }
    }
}