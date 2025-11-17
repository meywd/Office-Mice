using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Configuration.Editors
{
    [CustomEditor(typeof(RoomTemplate))]
    public class RoomTemplateEditor : UnityEditor.Editor
    {
        private SerializedProperty _templateIDProp;
        private SerializedProperty _templateNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _minWidthProp;
        private SerializedProperty _minHeightProp;
        private SerializedProperty _maxWidthProp;
        private SerializedProperty _maxHeightProp;
        private SerializedProperty _requiredClassificationProp;
        private SerializedProperty _floorPatternProp;
        private SerializedProperty _wallPatternProp;
        private SerializedProperty _decorativePatternsProp;
        private SerializedProperty _furniturePlacementsProp;
        private SerializedProperty _furnitureDensityProp;
        private SerializedProperty _ensurePathingProp;
        private SerializedProperty _enemySpawnPointsProp;
        private SerializedProperty _resourceSpawnPointsProp;
        private SerializedProperty _autoPlacePlayerSpawnProp;
        private SerializedProperty _preferredDoorwaysProp;
        private SerializedProperty _minDoorwaysProp;
        private SerializedProperty _maxDoorwaysProp;
        
        private bool _showValidation = false;
        private ValidationResult _lastValidationResult;
        
        private void OnEnable()
        {
            _templateIDProp = serializedObject.FindProperty("_templateID");
            _templateNameProp = serializedObject.FindProperty("_templateName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _minWidthProp = serializedObject.FindProperty("_minWidth");
            _minHeightProp = serializedObject.FindProperty("_minHeight");
            _maxWidthProp = serializedObject.FindProperty("_maxWidth");
            _maxHeightProp = serializedObject.FindProperty("_maxHeight");
            _requiredClassificationProp = serializedObject.FindProperty("_requiredClassification");
            _floorPatternProp = serializedObject.FindProperty("_floorPattern");
            _wallPatternProp = serializedObject.FindProperty("_wallPattern");
            _decorativePatternsProp = serializedObject.FindProperty("_decorativePatterns");
            _furniturePlacementsProp = serializedObject.FindProperty("_furniturePlacements");
            _furnitureDensityProp = serializedObject.FindProperty("_furnitureDensity");
            _ensurePathingProp = serializedObject.FindProperty("_ensurePathing");
            _enemySpawnPointsProp = serializedObject.FindProperty("_enemySpawnPoints");
            _resourceSpawnPointsProp = serializedObject.FindProperty("_resourceSpawnPoints");
            _autoPlacePlayerSpawnProp = serializedObject.FindProperty("_autoPlacePlayerSpawn");
            _preferredDoorwaysProp = serializedObject.FindProperty("_preferredDoorways");
            _minDoorwaysProp = serializedObject.FindProperty("_minDoorways");
            _maxDoorwaysProp = serializedObject.FindProperty("_maxDoorways");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var roomTemplate = (RoomTemplate)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw identity section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_templateNameProp, new GUIContent("Template Name", "Human-readable name for this room template"));
            EditorGUILayout.PropertyField(_templateIDProp, new GUIContent("Template ID", "Unique identifier (auto-generated from name)"));
            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("Description", "Detailed description of this room template"));
            
            // Draw room requirements section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Requirements", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_minWidthProp, new GUIContent("Min Width", "Minimum room width in tiles"));
            EditorGUILayout.PropertyField(_maxWidthProp, new GUIContent("Max Width", "Maximum room width in tiles"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_minHeightProp, new GUIContent("Min Height", "Minimum room height in tiles"));
            EditorGUILayout.PropertyField(_maxHeightProp, new GUIContent("Max Height", "Maximum room height in tiles"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(_requiredClassificationProp, new GUIContent("Required Classification", "Room classification this template can be used for"));
            
            // Draw tile configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_floorPatternProp, new GUIContent("Floor Pattern", "Tile pattern for room floors"));
            EditorGUILayout.PropertyField(_wallPatternProp, new GUIContent("Wall Pattern", "Tile pattern for room walls"));
            EditorGUILayout.PropertyField(_decorativePatternsProp, new GUIContent("Decorative Patterns", "Optional decorative tile patterns"), true);
            
            // Draw furniture configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Furniture Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_furnitureDensityProp, new GUIContent("Furniture Density", "How much of the room should be filled with furniture (0-1)"));
            EditorGUILayout.PropertyField(_ensurePathingProp, new GUIContent("Ensure Pathing", "Guarantee that furniture placement doesn't block paths"));
            EditorGUILayout.PropertyField(_furniturePlacementsProp, new GUIContent("Furniture Placements", "Predefined furniture positions"), true);
            
            // Draw spawn configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoPlacePlayerSpawnProp, new GUIContent("Auto Place Player Spawn", "Automatically place player spawn point"));
            EditorGUILayout.PropertyField(_enemySpawnPointsProp, new GUIContent("Enemy Spawn Points", "Predefined enemy spawn positions"), true);
            EditorGUILayout.PropertyField(_resourceSpawnPointsProp, new GUIContent("Resource Spawn Points", "Predefined resource spawn positions"), true);
            
            // Draw doorway configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Doorway Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_minDoorwaysProp, new GUIContent("Min Doorways", "Minimum number of doorways"));
            EditorGUILayout.PropertyField(_maxDoorwaysProp, new GUIContent("Max Doorways", "Maximum number of doorways"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(_preferredDoorwaysProp, new GUIContent("Preferred Doorways", "Preferred doorway positions and orientations"), true);
            
            // Draw validation section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Show Validation Results");
            if (EditorGUI.EndChangeCheck() || _showValidation)
            {
                _lastValidationResult = roomTemplate.Validate();
            }
            
            if (_showValidation && _lastValidationResult != null)
            {
                EditorGUI.indentLevel++;
                
                // Show validation status
                var statusColor = _lastValidationResult.IsValid ? Color.green : Color.red;
                var statusText = _lastValidationResult.IsValid ? "VALID" : "INVALID";
                var originalColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField($"Status: {statusText}", EditorStyles.boldLabel);
                GUI.color = originalColor;
                
                EditorGUILayout.LabelField($"Summary: {_lastValidationResult.GetSummary()}");
                
                // Show errors
                if (_lastValidationResult.Errors.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var error in _lastValidationResult.Errors)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                    EditorGUI.indentLevel--;
                }
                
                // Show warnings
                if (_lastValidationResult.Warnings.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var warning in _lastValidationResult.Warnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Draw utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Configuration"))
            {
                _lastValidationResult = roomTemplate.Validate();
                _showValidation = true;
                EditorUtility.DisplayDialog("Validation Complete", _lastValidationResult.GetSummary(), "OK");
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset to Defaults", "Are you sure you want to reset all values to defaults?", "Yes", "No"))
                {
                    ResetToDefaults(roomTemplate);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Show room compatibility info
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Compatibility", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Can fit rooms from {roomTemplate.MinWidth}x{roomTemplate.MinHeight} to {roomTemplate.MaxWidth}x{roomTemplate.MaxHeight}");
            EditorGUILayout.LabelField($"Compatible with classification: {roomTemplate.RequiredClassification}");
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void ResetToDefaults(RoomTemplate roomTemplate)
        {
            Undo.RecordObject(roomTemplate, "Reset Room Template to Defaults");
            
            // Reset to default values
            _minWidthProp.intValue = 3;
            _minHeightProp.intValue = 3;
            _maxWidthProp.intValue = 20;
            _maxHeightProp.intValue = 20;
            _requiredClassificationProp.enumValueIndex = (int)RoomClassification.Unassigned;
            _furnitureDensityProp.floatValue = 0.3f;
            _ensurePathingProp.boolValue = true;
            _autoPlacePlayerSpawnProp.boolValue = true;
            _minDoorwaysProp.intValue = 1;
            _maxDoorwaysProp.intValue = 4;
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(roomTemplate);
        }
    }
    
    /// <summary>
    /// Custom property drawer for TilePattern to provide better editor experience.
    /// </summary>
    [CustomPropertyDrawer(typeof(TilePattern))]
    public class TilePatternPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            // Reduce width to make room for validation button
            var buttonWidth = 60f;
            var propertyWidth = position.width - buttonWidth - 5f;
            var propertyRect = new Rect(position.x, position.y, propertyWidth, position.height);
            var buttonRect = new Rect(position.x + propertyWidth + 5f, position.y, buttonWidth, position.height);
            
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
            
            if (GUI.Button(buttonRect, "Validate"))
            {
                var tilePattern = new TilePattern();
                // Extract values from serialized property
                var patternNameProp = property.FindPropertyRelative("_patternName");
                var tileProp = property.FindPropertyRelative("_tile");
                var probabilityProp = property.FindPropertyRelative("_probability");
                
                // Create temporary instance for validation
                // Note: This is a simplified validation - in practice you'd want to deserialize the full object
                if (tileProp.objectReferenceValue == null)
                {
                    EditorUtility.DisplayDialog("Validation Error", "Tile pattern has no tile assigned", "OK");
                }
                else if (probabilityProp.floatValue < 0f || probabilityProp.floatValue > 1f)
                {
                    EditorUtility.DisplayDialog("Validation Error", "Tile pattern has invalid probability", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Success", "Tile pattern is valid", "OK");
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
    
    /// <summary>
    /// Custom property drawer for Vector2IntRange attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector2IntRangeAttribute))]
    public class Vector2IntRangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rangeAttribute = (Vector2IntRangeAttribute)attribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var labelRect = new Rect(position.x, position.y, 30, position.height);
            var minRect = new Rect(position.x + 35, position.y, (position.width - 70) / 2, position.height);
            var maxRect = new Rect(position.x + 35 + (position.width - 70) / 2 + 5, position.y, (position.width - 70) / 2, position.height);
            
            GUI.Label(labelRect, "Min:");
            var minValue = EditorGUI.IntField(minRect, property.vector2IntValue.x);
            GUI.Label(new Rect(minRect.x + minRect.width - 25, position.y, 20, position.height), "to");
            var maxValue = EditorGUI.IntField(maxRect, property.vector2IntValue.y);
            
            // Clamp values to attribute range
            minValue = Mathf.Max(rangeAttribute.min, minValue);
            maxValue = Mathf.Min(rangeAttribute.max, Mathf.Max(minValue, maxValue));
            
            property.vector2IntValue = new Vector2Int(minValue, maxValue);
            
            EditorGUI.EndProperty();
        }
    }
}