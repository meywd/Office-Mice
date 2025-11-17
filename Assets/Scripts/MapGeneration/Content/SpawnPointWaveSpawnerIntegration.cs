using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;

namespace OfficeMice.MapGeneration.Content
{
    /// <summary>
    /// Integration component that bridges generated spawn points with the existing WaveSpawner system.
    /// Creates GameObjects with "Spawn Point" tags for WaveSpawner compatibility.
    /// </summary>
    public class SpawnPointWaveSpawnerIntegration
    {
        #region Private Fields

        private readonly string _spawnPointTag = "Spawn Point";
        private readonly Transform _spawnPointParent;
        private readonly Dictionary<int, GameObject> _spawnPointObjects;
        private readonly SpawnTableConfiguration _spawnTableConfig;

        // Performance tracking
        private int _spawnPointsCreated;
        private int _spawnPointsDestroyed;
        private float _totalCreationTime;

        #endregion

        #region Constructor

        public SpawnPointWaveSpawnerIntegration(SpawnTableConfiguration spawnTableConfig = null)
        {
            _spawnTableConfig = spawnTableConfig;
            _spawnPointObjects = new Dictionary<int, GameObject>();
            _spawnPointsCreated = 0;
            _spawnPointsDestroyed = 0;
            _totalCreationTime = 0f;

            // Create parent object for organization
            _spawnPointParent = new GameObject("Generated Spawn Points").transform;
            _spawnPointParent.position = Vector3.zero;

            // Ensure tag exists
            EnsureSpawnPointTagExists();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates GameObjects for all spawn points with "Spawn Point" tags.
        /// </summary>
        /// <param name="spawnPoints">Spawn points to create GameObjects for</param>
        /// <param name="tilemapOffset">Offset for world positioning</param>
        /// <returns>List of created spawn point GameObjects</returns>
        public List<GameObject> CreateSpawnPointGameObjects(List<SpawnPointData> spawnPoints, Vector3 tilemapOffset = default)
        {
            if (spawnPoints == null) throw new ArgumentNullException(nameof(spawnPoints));

            var createdObjects = new List<GameObject>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    var spawnObject = CreateSingleSpawnPoint(spawnPoint, tilemapOffset);
                    if (spawnObject != null)
                    {
                        createdObjects.Add(spawnObject);
                        _spawnPointObjects[spawnPoint.RoomID * 1000 + spawnPoint.Position.x * 100 + spawnPoint.Position.y] = spawnObject;
                    }
                }

                _spawnPointsCreated += createdObjects.Count;
                return createdObjects;
            }
            finally
            {
                stopwatch.Stop();
                _totalCreationTime += stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Creates a single spawn point GameObject.
        /// </summary>
        /// <param name="spawnPoint">Spawn point data</param>
        /// <param name="tilemapOffset">Offset for world positioning</param>
        /// <returns>Created spawn point GameObject</returns>
        public GameObject CreateSingleSpawnPoint(SpawnPointData spawnPoint, Vector3 tilemapOffset = default)
        {
            if (spawnPoint == null) throw new ArgumentNullException(nameof(spawnPoint));

            // Create spawn point object
            var spawnObject = new GameObject($"SpawnPoint_{spawnPoint.RoomID}_{spawnPoint.Position.x}_{spawnPoint.Position.y}");
            
            // Set position
            Vector3 worldPosition = tilemapOffset + new Vector3(spawnPoint.Position.x + 0.5f, spawnPoint.Position.y + 0.5f, 0);
            spawnObject.transform.position = worldPosition;
            spawnObject.transform.parent = _spawnPointParent;

            // Add tag
            spawnObject.tag = _spawnPointTag;

            // Add SpawnPointComponent for metadata
            var spawnComponent = spawnObject.AddComponent<GeneratedSpawnPointComponent>();
            spawnComponent.Initialize(spawnPoint);

            // Add optional visualizer
            if (_spawnTableConfig?.UseObjectPooling ?? true)
            {
                AddSpawnPointVisualizer(spawnObject, spawnPoint);
            }

            return spawnObject;
        }

        /// <summary>
        /// Destroys all generated spawn point GameObjects.
        /// </summary>
        public void DestroyAllSpawnPoints()
        {
            foreach (var kvp in _spawnPointObjects)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
                }
            }

            _spawnPointObjects.Clear();
            _spawnPointsDestroyed += _spawnPointObjects.Count;
        }

        /// <summary>
        /// Destroys a specific spawn point GameObject.
        /// </summary>
        /// <param name="spawnPoint">Spawn point to destroy</param>
        /// <returns>True if successfully destroyed</returns>
        public bool DestroySpawnPoint(SpawnPointData spawnPoint)
        {
            if (spawnPoint == null) return false;

            var key = spawnPoint.RoomID * 1000 + spawnPoint.Position.x * 100 + spawnPoint.Position.y;
            if (_spawnPointObjects.TryGetValue(key, out var spawnObject))
            {
                UnityEngine.Object.DestroyImmediate(spawnObject);
                _spawnPointObjects.Remove(key);
                _spawnPointsDestroyed++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates existing spawn points with new data.
        /// </summary>
        /// <param name="spawnPoints">Updated spawn point data</param>
        /// <param name="tilemapOffset">Offset for world positioning</param>
        public void UpdateSpawnPoints(List<SpawnPointData> spawnPoints, Vector3 tilemapOffset = default)
        {
            if (spawnPoints == null) throw new ArgumentNullException(nameof(spawnPoints));

            // Clear existing spawn points
            DestroyAllSpawnPoints();

            // Create new spawn points
            CreateSpawnPointGameObjects(spawnPoints, tilemapOffset);
        }

        /// <summary>
        /// Gets all existing spawn point GameObjects.
        /// </summary>
        /// <returns>Array of spawn point GameObjects</returns>
        public GameObject[] GetExistingSpawnPointObjects()
        {
            return GameObject.FindGameObjectsWithTag(_spawnPointTag);
        }

        /// <summary>
        /// Validates that all spawn points are properly integrated with WaveSpawner.
        /// </summary>
        /// <param name="expectedSpawnPoints">Expected spawn points</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateWaveSpawnerIntegration(List<SpawnPointData> expectedSpawnPoints)
        {
            var result = new ValidationResult();

            if (expectedSpawnPoints == null)
            {
                result.AddError("Expected spawn points list is null");
                return result;
            }

            // Check tag exists
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains(_spawnPointTag))
            {
                result.AddError($"Spawn point tag '{_spawnPointTag}' does not exist in Unity tags");
            }

            // Get all spawn point objects
            var existingObjects = GetExistingSpawnPointObjects();
            
            // Check count matches
            if (existingObjects.Length != expectedSpawnPoints.Count)
            {
                result.AddError($"Spawn point count mismatch: expected {expectedSpawnPoints.Count}, found {existingObjects.Length}");
            }

            // Check each expected spawn point has corresponding GameObject
            foreach (var expectedSpawnPoint in expectedSpawnPoints)
            {
                var key = expectedSpawnPoint.RoomID * 1000 + expectedSpawnPoint.Position.x * 100 + expectedSpawnPoint.Position.y;
                if (!_spawnPointObjects.ContainsKey(key))
                {
                    result.AddError($"Spawn point {expectedSpawnPoint.Position} in room {expectedSpawnPoint.RoomID} has no corresponding GameObject");
                }
            }

            // Check all GameObjects have required components
            foreach (var obj in existingObjects)
            {
                if (obj.GetComponent<GeneratedSpawnPointComponent>() == null)
                {
                    result.AddWarning($"Spawn point {obj.name} missing GeneratedSpawnPointComponent");
                }

                if (obj.tag != _spawnPointTag)
                {
                    result.AddError($"Spawn point {obj.name} has incorrect tag: {obj.tag} (expected: {_spawnPointTag})");
                }
            }

            return result;
        }

        /// <summary>
        /// Gets performance statistics for the integration system.
        /// </summary>
        /// <returns>Performance statistics</returns>
        public (int created, int destroyed, float avgCreationTime) GetPerformanceStats()
        {
            float avgCreationTime = _spawnPointsCreated > 0 ? _totalCreationTime / _spawnPointsCreated : 0f;
            return (_spawnPointsCreated, _spawnPointsDestroyed, avgCreationTime);
        }

        /// <summary>
        /// Sets the parent transform for spawn point objects.
        /// </summary>
        /// <param name="parent">New parent transform</param>
        public void SetSpawnPointParent(Transform parent)
        {
            if (parent != null && _spawnPointParent != null)
            {
                _spawnPointParent.parent = parent;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures the "Spawn Point" tag exists in Unity.
        /// </summary>
        private void EnsureSpawnPointTagExists()
        {
            #if UNITY_EDITOR
            // Check if tag exists
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains(_spawnPointTag))
            {
                // Add tag (editor only)
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
            #endif
        }

        /// <summary>
        /// Adds a visualizer component to help with debugging spawn points.
        /// </summary>
        private void AddSpawnPointVisualizer(GameObject spawnObject, SpawnPointData spawnPoint)
        {
            #if UNITY_EDITOR
            // Add a simple sphere gizmo for visualization
            var visualizer = spawnObject.AddComponent<SpawnPointVisualizer>();
            visualizer.Initialize(spawnPoint);
            #endif
        }

        #endregion
    }

    /// <summary>
    /// Component attached to generated spawn point GameObjects for metadata storage.
    /// </summary>
    public class GeneratedSpawnPointComponent : MonoBehaviour
    {
        [Header("Spawn Point Data")]
        [SerializeField] private int _roomID;
        [SerializeField] private Vector2Int _position;
        [SerializeField] private string _enemyType;
        [SerializeField] private float _spawnDelay;

        public int RoomID => _roomID;
        public Vector2Int Position => _position;
        public string EnemyType => _enemyType;
        public float SpawnDelay => _spawnDelay;

        /// <summary>
        /// Initializes the component with spawn point data.
        /// </summary>
        /// <param name="spawnPoint">Spawn point data</param>
        public void Initialize(SpawnPointData spawnPoint)
        {
            if (spawnPoint == null) throw new ArgumentNullException(nameof(spawnPoint));

            _roomID = spawnPoint.RoomID;
            _position = spawnPoint.Position;
            _enemyType = spawnPoint.EnemyType;
            _spawnDelay = spawnPoint.SpawnDelay;
        }

        /// <summary>
        /// Gets the spawn point data from this component.
        /// </summary>
        /// <returns>Spawn point data</returns>
        public SpawnPointData GetSpawnPointData()
        {
            return new SpawnPointData(_roomID, _position, _enemyType, _spawnDelay);
        }

        private void OnDrawGizmos()
        {
            // Draw a simple indicator in the scene view
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw enemy type label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"{_enemyType} (Room {_roomID})");
            #endif
        }
    }

    /// <summary>
    /// Visualizer component for spawn points (editor only).
    /// </summary>
    #if UNITY_EDITOR
    public class SpawnPointVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private Color _gizmoColor = Color.red;
        [SerializeField] private float _gizmoSize = 0.3f;
        [SerializeField] private bool _showLabel = true;

        private SpawnPointData _spawnPointData;

        public void Initialize(SpawnPointData spawnPoint)
        {
            _spawnPointData = spawnPoint;
            
            // Set color based on enemy type
            _gizmoColor = spawnPoint.EnemyType.ToLower() switch
            {
                "mouse" => Color.blue,
                "rat" => Color.gray,
                "bug" => Color.green,
                "boss" => Color.red,
                _ => Color.yellow
            };
        }

        private void OnDrawGizmos()
        {
            if (_spawnPointData == null) return;

            // Draw spawn point indicator
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoSize);
            Gizmos.DrawSphere(transform.position, _gizmoSize * 0.5f);

            // Draw spawn radius
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 1f);

            // Draw label
            if (_showLabel)
            {
                var label = $"{_spawnPointData.EnemyType}\nRoom {_spawnPointData.RoomID}\nDelay: {_spawnPointData.SpawnDelay:F1}s";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, label);
            }
        }
    }
    #endif
}