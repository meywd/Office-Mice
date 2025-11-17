using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration.Editors
{
    [CustomEditor(typeof(BiomeConfiguration))]
    public class BiomeConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty _biomeIDProp;
        private SerializedProperty _biomeNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _biomeTypeProp;
        private SerializedProperty _primaryTilesetProp;
        private SerializedProperty _secondaryTilesetProp;
        private SerializedProperty _decorativeTilesetsProp;
        private SerializedProperty _secondaryTilesetChanceProp;
        private SerializedProperty _colorPaletteProp;
        private SerializedProperty _applyColorTintingProp;
        private SerializedProperty _colorVariationProp;
        private SerializedProperty _environmentalEffectsProp;
        private SerializedProperty _ambientLightIntensityProp;
        private SerializedProperty _ambientLightColorProp;
        private SerializedProperty _generationRulesProp;
        private SerializedProperty _biomeModifiersProp;
        private SerializedProperty _ambientMusicProp;
        private SerializedProperty _ambientSoundsProp;
        private SerializedProperty _musicVolumeProp;
        private SerializedProperty _soundVolumeProp;
        private SerializedProperty _commonResourcesProp;
        private SerializedProperty _rareResourcesProp;
        private SerializedProperty _rareResourceChanceProp;
        
        private bool _showValidation = false;
        private bool _showPreview = false;
        private ValidationResult _lastValidationResult;
        
        private void OnEnable()
        {
            _biomeIDProp = serializedObject.FindProperty("_biomeID");
            _biomeNameProp = serializedObject.FindProperty("_biomeName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _biomeTypeProp = serializedObject.FindProperty("_biomeType");
            _primaryTilesetProp = serializedObject.FindProperty("_primaryTileset");
            _secondaryTilesetProp = serializedObject.FindProperty("_secondaryTileset");
            _decorativeTilesetsProp = serializedObject.FindProperty("_decorativeTilesets");
            _secondaryTilesetChanceProp = serializedObject.FindProperty("_secondaryTilesetChance");
            _colorPaletteProp = serializedObject.FindProperty("_colorPalette");
            _applyColorTintingProp = serializedObject.FindProperty("_applyColorTinting");
            _colorVariationProp = serializedObject.FindProperty("_colorVariation");
            _environmentalEffectsProp = serializedObject.FindProperty("_environmentalEffects");
            _ambientLightIntensityProp = serializedObject.FindProperty("_ambientLightIntensity");
            _ambientLightColorProp = serializedObject.FindProperty("_ambientLightColor");
            _generationRulesProp = serializedObject.FindProperty("_generationRules");
            _biomeModifiersProp = serializedObject.FindProperty("_biomeModifiers");
            _ambientMusicProp = serializedObject.FindProperty("_ambientMusic");
            _ambientSoundsProp = serializedObject.FindProperty("_ambientSounds");
            _musicVolumeProp = serializedObject.FindProperty("_musicVolume");
            _soundVolumeProp = serializedObject.FindProperty("_soundVolume");
            _commonResourcesProp = serializedObject.FindProperty("_commonResources");
            _rareResourcesProp = serializedObject.FindProperty("_rareResources");
            _rareResourceChanceProp = serializedObject.FindProperty("_rareResourceChance");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var biomeConfig = (BiomeConfiguration)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw identity section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_biomeNameProp, new GUIContent("Biome Name", "Human-readable name for this biome"));
            EditorGUILayout.PropertyField(_biomeIDProp, new GUIContent("Biome ID", "Unique identifier (auto-generated from name)"));
            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("Description", "Detailed description of this biome"));
            EditorGUILayout.PropertyField(_biomeTypeProp, new GUIContent("Biome Type", "Type of biome for categorization"));
            
            // Draw tileset configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tileset Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_primaryTilesetProp, new GUIContent("Primary Tileset", "Main tileset for this biome"));
            EditorGUILayout.PropertyField(_secondaryTilesetProp, new GUIContent("Secondary Tileset", "Optional secondary tileset for variation"));
            EditorGUILayout.PropertyField(_secondaryTilesetChanceProp, new GUIContent("Secondary Chance", "Chance to use secondary tileset (0-1)"));
            EditorGUILayout.PropertyField(_decorativeTilesetsProp, new GUIContent("Decorative Tilesets", "Additional decorative tilesets"), true);
            
            // Draw color palette section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Palette", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_colorPaletteProp, new GUIContent("Color Palette", "Color palette for this biome"));
            EditorGUILayout.PropertyField(_applyColorTintingProp, new GUIContent("Apply Color Tinting", "Apply color variations to tiles"));
            EditorGUILayout.PropertyField(_colorVariationProp, new GUIContent("Color Variation", "Amount of color variation (0-1)"));
            
            // Draw environmental effects section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Environmental Effects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_ambientLightIntensityProp, new GUIContent("Ambient Light Intensity", "Intensity of ambient lighting"));
            EditorGUILayout.PropertyField(_ambientLightColorProp, new GUIContent("Ambient Light Color", "Color of ambient lighting"));
            EditorGUILayout.PropertyField(_environmentalEffectsProp, new GUIContent("Environmental Effects", "Particle effects and other environmental elements"), true);
            
            // Draw generation rules section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_generationRulesProp, new GUIContent("Generation Rules", "Biome-specific generation rules"));
            EditorGUILayout.PropertyField(_biomeModifiersProp, new GUIContent("Biome Modifiers", "Modifiers that affect generation"), true);
            
            // Draw audio configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_ambientMusicProp, new GUIContent("Ambient Music", "Background music for this biome"));
            EditorGUILayout.PropertyField(_ambientSoundsProp, new GUIContent("Ambient Sounds", "Ambient sound effects"), true);
            EditorGUILayout.PropertyField(_musicVolumeProp, new GUIContent("Music Volume", "Volume for ambient music (0-1)"));
            EditorGUILayout.PropertyField(_soundVolumeProp, new GUIContent("Sound Volume", "Volume for ambient sounds (0-1)"));
            
            // Draw resource configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resource Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_commonResourcesProp, new GUIContent("Common Resources", "Resources commonly found in this biome"), true);
            EditorGUILayout.PropertyField(_rareResourcesProp, new GUIContent("Rare Resources", "Rare resources for this biome"), true);
            EditorGUILayout.PropertyField(_rareResourceChanceProp, new GUIContent("Rare Resource Chance", "Chance to include rare resources (0-1)"));
            
            // Draw validation section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Show Validation Results");
            if (EditorGUI.EndChangeCheck() || _showValidation)
            {
                _lastValidationResult = biomeConfig.Validate();
            }
            
            if (_showValidation && _lastValidationResult != null)
            {
                DrawValidationResults(_lastValidationResult);
            }
            
            // Draw preview section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _showPreview = EditorGUILayout.Foldout(_showPreview, "Show Biome Preview");
            
            if (_showPreview)
            {
                DrawBiomePreview(biomeConfig);
            }
            
            // Draw utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Configuration"))
            {
                _lastValidationResult = biomeConfig.Validate();
                _showValidation = true;
                EditorUtility.DisplayDialog("Validation Complete", _lastValidationResult.GetSummary(), "OK");
            }
            
            if (GUILayout.Button("Test Random Tileset"))
            {
                var randomTileset = biomeConfig.GetRandomTileset();
                if (randomTileset != null)
                {
                    EditorUtility.DisplayDialog("Random Tileset", $"Selected tileset: {randomTileset.TilesetName}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No tilesets available", "OK");
                }
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
        
        private void DrawBiomePreview(BiomeConfiguration biomeConfig)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField($"Biome Type: {biomeConfig.BiomeType}");
            EditorGUILayout.LabelField($"Primary Tileset: {biomeConfig.PrimaryTileset?.TilesetName ?? "None"}");
            EditorGUILayout.LabelField($"Secondary Tileset: {biomeConfig.SecondaryTileset?.TilesetName ?? "None"}");
            EditorGUILayout.LabelField($"Secondary Chance: {biomeConfig.SecondaryTilesetChance:P0}");
            EditorGUILayout.LabelField($"Color Tinting: {(biomeConfig.ApplyColorTinting ? "Enabled" : "Disabled")}");
            EditorGUILayout.LabelField($"Color Variation: {biomeConfig.ColorVariation:P0}");
            EditorGUILayout.LabelField($"Ambient Light: {biomeConfig.AmbientLightIntensity:F2} intensity, {biomeConfig.AmbientLightColor}");
            EditorGUILayout.LabelField($"Environmental Effects: {biomeConfig.EnvironmentalEffects.Count}");
            EditorGUILayout.LabelField($"Biome Modifiers: {biomeConfig.BiomeModifiers.Count}");
            EditorGUILayout.LabelField($"Common Resources: {biomeConfig.CommonResources.Count}");
            EditorGUILayout.LabelField($"Rare Resources: {biomeConfig.RareResources.Count}");
            EditorGUILayout.LabelField($"Rare Resource Chance: {biomeConfig.RareResourceChance:P0}");
            
            // Show color palette preview if available
            if (biomeConfig.ColorPalette != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Color Palette Preview:", EditorStyles.boldLabel);
                
                var colorRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                var colors = new Color[] { 
                    biomeConfig.ColorPalette.PrimaryColor,
                    biomeConfig.ColorPalette.SecondaryColor,
                    biomeConfig.ColorPalette.AccentColor,
                    biomeConfig.ColorPalette.ShadowColor,
                    biomeConfig.ColorPalette.HighlightColor
                };
                
                var width = colorRect.width / colors.Length;
                for (int i = 0; i < colors.Length; i++)
                {
                    var rect = new Rect(colorRect.x + i * width, colorRect.y, width, colorRect.height);
                    EditorGUI.DrawRect(rect, colors[i]);
                }
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    /// <summary>
    /// Custom property drawer for BiomeColorPalette.
    /// </summary>
    [CustomPropertyDrawer(typeof(BiomeColorPalette))]
    public class BiomeColorPalettePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var labelWidth = 80f;
            var colorWidth = (position.width - labelWidth) / 5;
            
            // Draw color fields
            var colors = new[] { "Primary", "Secondary", "Accent", "Shadow", "Highlight" };
            var colorProps = new[] { "_primaryColor", "_secondaryColor", "_accentColor", "_shadowColor", "_highlightColor" };
            
            for (int i = 0; i < colors.Length; i++)
            {
                var labelRect = new Rect(position.x + i * colorWidth, position.y, labelWidth, position.height);
                var colorRect = new Rect(position.x + i * colorWidth + labelWidth, position.y, colorWidth - labelWidth, position.height);
                
                GUI.Label(labelRect, colors[i]);
                EditorGUI.PropertyField(colorRect, property.FindPropertyRelative(colorProps[i]), GUIContent.none);
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }
    }
}