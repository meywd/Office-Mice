using UnityEngine;
using UnityEditor;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using System.Linq;

namespace OfficeMice.MapGeneration.Configuration.Editors
{
    [CustomEditor(typeof(SpawnTableConfiguration))]
    public class SpawnTableConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty _spawnTableIDProp;
        private SerializedProperty _tableNameProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _tableTypeProp;
        private SerializedProperty _spawnEntriesProp;
        private SerializedProperty _ensureMinimumSpawnsProp;
        private SerializedProperty _minimumSpawnsProp;
        private SerializedProperty _waveDefinitionsProp;
        private SerializedProperty _wavePatternProp;
        private SerializedProperty _maxConcurrentEnemiesProp;
        private SerializedProperty _spawnDelayBetweenWavesProp;
        private SerializedProperty _difficultyScalingProp;
        private SerializedProperty _scaleWithPlayerProgressProp;
        private SerializedProperty _difficultyMultiplierProp;
        private SerializedProperty _spawnRulesProp;
        private SerializedProperty _spawnConditionsProp;
        private SerializedProperty _respectExistingSpawnPointsProp;
        private SerializedProperty _useWaveSpawnerSystemProp;
        private SerializedProperty _spawnPointTagProp;
        private SerializedProperty _useObjectPoolingProp;
        private SerializedProperty _spawnRadiusProp;
        
        private bool _showValidation = false;
        private bool _showSpawnPreview = false;
        private ValidationResult _lastValidationResult;
        
        private void OnEnable()
        {
            _spawnTableIDProp = serializedObject.FindProperty("_spawnTableID");
            _tableNameProp = serializedObject.FindProperty("_tableName");
            _descriptionProp = serializedObject.FindProperty("_description");
            _tableTypeProp = serializedObject.FindProperty("_tableType");
            _spawnEntriesProp = serializedObject.FindProperty("_spawnEntries");
            _ensureMinimumSpawnsProp = serializedObject.FindProperty("_ensureMinimumSpawns");
            _minimumSpawnsProp = serializedObject.FindProperty("_minimumSpawns");
            _waveDefinitionsProp = serializedObject.FindProperty("_waveDefinitions");
            _wavePatternProp = serializedObject.FindProperty("_wavePattern");
            _maxConcurrentEnemiesProp = serializedObject.FindProperty("_maxConcurrentEnemies");
            _spawnDelayBetweenWavesProp = serializedObject.FindProperty("_spawnDelayBetweenWaves");
            _difficultyScalingProp = serializedObject.FindProperty("_difficultyScaling");
            _scaleWithPlayerProgressProp = serializedObject.FindProperty("_scaleWithPlayerProgress");
            _difficultyMultiplierProp = serializedObject.FindProperty("_difficultyMultiplier");
            _spawnRulesProp = serializedObject.FindProperty("_spawnRules");
            _spawnConditionsProp = serializedObject.FindProperty("_spawnConditions");
            _respectExistingSpawnPointsProp = serializedObject.FindProperty("_respectExistingSpawnPoints");
            _useWaveSpawnerSystemProp = serializedObject.FindProperty("_useWaveSpawnerSystem");
            _spawnPointTagProp = serializedObject.FindProperty("_spawnPointTag");
            _useObjectPoolingProp = serializedObject.FindProperty("_useObjectPooling");
            _spawnRadiusProp = serializedObject.FindProperty("_spawnRadius");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var spawnTable = (SpawnTableConfiguration)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw identity section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tableNameProp, new GUIContent("Table Name", "Human-readable name for this spawn table"));
            EditorGUILayout.PropertyField(_spawnTableIDProp, new GUIContent("Spawn Table ID", "Unique identifier (auto-generated from name)"));
            EditorGUILayout.PropertyField(_descriptionProp, new GUIContent("Description", "Detailed description of this spawn table"));
            EditorGUILayout.PropertyField(_tableTypeProp, new GUIContent("Table Type", "Type of spawn table for categorization"));
            
            // Draw spawn entries section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Entries", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_spawnEntriesProp, new GUIContent("Spawn Entries", "List of possible enemy spawns"), true);
            
            // Show total weight
            var totalWeight = CalculateTotalWeight();
            EditorGUILayout.LabelField($"Total Weight: {totalWeight:F2}", EditorStyles.miniLabel);
            if (totalWeight <= 0f)
            {
                EditorGUILayout.HelpBox("Total weight must be greater than 0 for random selection to work!", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(_ensureMinimumSpawnsProp, new GUIContent("Ensure Minimum Spawns", "Guarantee minimum number of spawns"));
            if (_ensureMinimumSpawnsProp.boolValue)
            {
                EditorGUILayout.PropertyField(_minimumSpawnsProp, new GUIContent("Minimum Spawns", "Minimum number of enemies to spawn"));
            }
            
            // Draw wave configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wave Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_wavePatternProp, new GUIContent("Wave Pattern", "How waves are selected"));
            EditorGUILayout.PropertyField(_waveDefinitionsProp, new GUIContent("Wave Definitions", "Structured wave definitions"), true);
            EditorGUILayout.PropertyField(_maxConcurrentEnemiesProp, new GUIContent("Max Concurrent Enemies", "Maximum enemies active at once"));
            EditorGUILayout.PropertyField(_spawnDelayBetweenWavesProp, new GUIContent("Spawn Delay Between Waves", "Delay between wave spawns"));
            
            // Draw difficulty scaling section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty Scaling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_difficultyScalingProp, new GUIContent("Difficulty Scaling", "Difficulty scaling configuration"));
            EditorGUILayout.PropertyField(_scaleWithPlayerProgressProp, new GUIContent("Scale With Player Progress", "Scale difficulty based on player progress"));
            EditorGUILayout.PropertyField(_difficultyMultiplierProp, new GUIContent("Difficulty Multiplier", "Global difficulty multiplier"));
            
            // Draw spawn rules section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_spawnRulesProp, new GUIContent("Spawn Rules", "Rules governing enemy spawning"));
            EditorGUILayout.PropertyField(_spawnConditionsProp, new GUIContent("Spawn Conditions", "Conditions that must be met for spawning"), true);
            EditorGUILayout.PropertyField(_respectExistingSpawnPointsProp, new GUIContent("Respect Existing Spawn Points", "Use existing spawn points when available"));
            
            // Draw integration settings section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Integration Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useWaveSpawnerSystemProp, new GUIContent("Use Wave Spawner System", "Integrate with existing WaveSpawner system"));
            if (_useWaveSpawnerSystemProp.boolValue)
            {
                EditorGUILayout.PropertyField(_spawnPointTagProp, new GUIContent("Spawn Point Tag", "Tag to identify spawn points"));
            }
            EditorGUILayout.PropertyField(_useObjectPoolingProp, new GUIContent("Use Object Pooling", "Use object pooling for spawned enemies"));
            EditorGUILayout.PropertyField(_spawnRadiusProp, new GUIContent("Spawn Radius", "Radius around spawn points for enemy placement"));
            
            // Draw validation section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Show Validation Results");
            if (EditorGUI.EndChangeCheck() || _showValidation)
            {
                _lastValidationResult = spawnTable.Validate();
            }
            
            if (_showValidation && _lastValidationResult != null)
            {
                DrawValidationResults(_lastValidationResult);
            }
            
            // Draw spawn preview section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Preview", EditorStyles.boldLabel);
            _showSpawnPreview = EditorGUILayout.Foldout(_showSpawnPreview, "Show Spawn Preview");
            
            if (_showSpawnPreview)
            {
                DrawSpawnPreview(spawnTable);
            }
            
            // Draw utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Configuration"))
            {
                _lastValidationResult = spawnTable.Validate();
                _showValidation = true;
                EditorUtility.DisplayDialog("Validation Complete", _lastValidationResult.GetSummary(), "OK");
            }
            
            if (GUILayout.Button("Test Random Spawn"))
            {
                var randomEntry = spawnTable.GetRandomSpawnEntry();
                if (randomEntry != null)
                {
                    EditorUtility.DisplayDialog("Random Spawn", $"Selected spawn: {randomEntry.EnemyType} (Weight: {randomEntry.Weight})", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No spawn entries available", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Balance Weights"))
            {
                BalanceSpawnWeights();
            }
            
            if (GUILayout.Button("Add Standard Entry"))
            {
                AddStandardSpawnEntry();
            }
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private float CalculateTotalWeight()
        {
            float totalWeight = 0f;
            for (int i = 0; i < _spawnEntriesProp.arraySize; i++)
            {
                var entryProp = _spawnEntriesProp.GetArrayElementAtIndex(i);
                var weightProp = entryProp.FindPropertyRelative("_weight");
                totalWeight += weightProp.floatValue;
            }
            return totalWeight;
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
        
        private void DrawSpawnPreview(SpawnTableConfiguration spawnTable)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField($"Table Type: {spawnTable.TableType}");
            EditorGUILayout.LabelField($"Spawn Entries: {spawnTable.SpawnEntries.Count}");
            EditorGUILayout.LabelField($"Total Weight: {CalculateTotalWeight():F2}");
            EditorGUILayout.LabelField($"Wave Definitions: {spawnTable.WaveDefinitions.Count}");
            EditorGUILayout.LabelField($"Wave Pattern: {spawnTable.WavePattern}");
            EditorGUILayout.LabelField($"Max Concurrent Enemies: {spawnTable.MaxConcurrentEnemies}");
            EditorGUILayout.LabelField($"Difficulty Multiplier: {spawnTable.DifficultyMultiplier:F2}");
            EditorGUILayout.LabelField($"Spawn Conditions: {spawnTable.SpawnConditions.Count}");
            EditorGUILayout.LabelField($"Use Wave Spawner: {(spawnTable.UseWaveSpawnerSystem ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Use Object Pooling: {(spawnTable.UseObjectPooling ? "Yes" : "No")}");
            
            // Show spawn entry probabilities
            if (spawnTable.SpawnEntries.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Spawn Probabilities:", EditorStyles.boldLabel);
                
                var totalWeight = CalculateTotalWeight();
                for (int i = 0; i < _spawnEntriesProp.arraySize; i++)
                {
                    var entryProp = _spawnEntriesProp.GetArrayElementAtIndex(i);
                    var typeProp = entryProp.FindPropertyRelative("_enemyType");
                    var weightProp = entryProp.FindPropertyRelative("_weight");
                    
                    var probability = totalWeight > 0 ? (weightProp.floatValue / totalWeight) * 100f : 0f;
                    EditorGUILayout.LabelField($"{typeProp.stringValue}: {probability:F1}% (Weight: {weightProp.floatValue})");
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void BalanceSpawnWeights()
        {
            if (_spawnEntriesProp.arraySize == 0)
                return;
            
            Undo.RecordObject(target, "Balance Spawn Weights");
            
            // Set all weights to 1.0f for equal probability
            for (int i = 0; i < _spawnEntriesProp.arraySize; i++)
            {
                var entryProp = _spawnEntriesProp.GetArrayElementAtIndex(i);
                var weightProp = entryProp.FindPropertyRelative("_weight");
                weightProp.floatValue = 1.0f;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        
        private void AddStandardSpawnEntry()
        {
            Undo.RecordObject(target, "Add Standard Spawn Entry");
            
            _spawnEntriesProp.arraySize++;
            var newEntryProp = _spawnEntriesProp.GetArrayElementAtIndex(_spawnEntriesProp.arraySize - 1);
            
            // Set default values
            newEntryProp.FindPropertyRelative("_enemyType").stringValue = "NewEnemy";
            newEntryProp.FindPropertyRelative("_weight").floatValue = 1.0f;
            newEntryProp.FindPropertyRelative("_minCount").intValue = 1;
            newEntryProp.FindPropertyRelative("_maxCount").intValue = 3;
            newEntryProp.FindPropertyRelative("_spawnDelay").floatValue = 0.5f;
            newEntryProp.FindPropertyRelative("_healthMultiplier").floatValue = 1.0f;
            newEntryProp.FindPropertyRelative("_damageMultiplier").floatValue = 1.0f;
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
    
    /// <summary>
    /// Custom property drawer for SpawnEntry.
    /// </summary>
    [CustomPropertyDrawer(typeof(SpawnEntry))]
    public class SpawnEntryPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var typeWidth = position.width * 0.3f;
            var weightWidth = position.width * 0.15f;
            var countWidth = position.width * 0.2f;
            var delayWidth = position.width * 0.15f;
            var buttonWidth = position.width * 0.2f;
            
            var typeRect = new Rect(position.x, position.y, typeWidth, position.height);
            var weightRect = new Rect(position.x + typeWidth, position.y, weightWidth, position.height);
            var countRect = new Rect(position.x + typeWidth + weightWidth, position.y, countWidth, position.height);
            var delayRect = new Rect(position.x + typeWidth + weightWidth + countWidth, position.y, delayWidth, position.height);
            var buttonRect = new Rect(position.x + typeWidth + weightWidth + countWidth + delayWidth, position.y, buttonWidth, position.height);
            
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("_enemyType"), GUIContent.none);
            EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("_weight"), GUIContent.none);
            
            // Show min-max count as a single field
            var minCountProp = property.FindPropertyRelative("_minCount");
            var maxCountProp = property.FindPropertyRelative("_maxCount");
            var countText = $"{minCountProp.intValue}-{maxCountProp.intValue}";
            EditorGUI.LabelField(countRect, countText, EditorStyles.miniLabel);
            
            EditorGUI.PropertyField(delayRect, property.FindPropertyRelative("_spawnDelay"), GUIContent.none);
            
            if (GUI.Button(buttonRect, "Validate"))
            {
                var enemyTypeProp = property.FindPropertyRelative("_enemyType");
                var weightProp = property.FindPropertyRelative("_weight");
                
                if (string.IsNullOrEmpty(enemyTypeProp.stringValue))
                {
                    EditorUtility.DisplayDialog("Validation Error", "Enemy type is required", "OK");
                }
                else if (weightProp.floatValue <= 0f)
                {
                    EditorUtility.DisplayDialog("Validation Error", "Weight must be greater than 0", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Success", "Spawn entry is valid", "OK");
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
}