using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Generators;

namespace OfficeMice.MapGeneration.Visualization
{
    /// <summary>
    /// MonoBehaviour component for visualizing BSP generation in the Unity editor.
    /// Attach this component to a GameObject to debug BSP algorithm.
    /// </summary>
    public class BSPVisualizer : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private MapGenerationSettings _settings;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _autoGenerate = true;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool _showRooms = true;
        [SerializeField] private bool _showSplits = true;
        [SerializeField] private bool _showStatistics = true;
        
        [Header("Debug Controls")]
        [SerializeField] private bool _regenerateOnValidate = false;
        
        private BSPGenerator _generator;
        private BSPStatistics _lastStatistics;

        public MapGenerationSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public bool ShowRooms
        {
            get => _showRooms;
            set => _showRooms = value;
        }

        public bool ShowSplits
        {
            get => _showSplits;
            set => _showSplits = value;
        }

        public BSPStatistics LastStatistics => _lastStatistics;

        private void Awake()
        {
            _generator = new BSPGenerator();
            
            if (_autoGenerate && _settings != null)
            {
                GenerateBSP();
            }
        }

        private void OnValidate()
        {
            if (_regenerateOnValidate && _settings != null && Application.isPlaying)
            {
                GenerateBSP();
            }
        }

        /// <summary>
        /// Generates a new BSP tree with current settings.
        /// </summary>
        public void GenerateBSP()
        {
            if (_settings == null)
            {
                Debug.LogWarning("BSPVisualizer: MapGenerationSettings is null");
                return;
            }

            try
            {
                var rooms = _generator.GenerateRooms(_settings, _seed);
                _lastStatistics = _generator.GetStatistics();
                
                if (_showStatistics)
                {
                    Debug.Log($"BSP Generated: {_lastStatistics}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BSP Generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates BSP with specified seed.
        /// </summary>
        public void GenerateBSP(int seed)
        {
            _seed = seed;
            GenerateBSP();
        }

        /// <summary>
        /// Generates BSP with specified settings.
        /// </summary>
        public void GenerateBSP(MapGenerationSettings settings)
        {
            _settings = settings;
            GenerateBSP();
        }

        /// <summary>
        /// Generates BSP with specified settings and seed.
        /// </summary>
        public void GenerateBSP(MapGenerationSettings settings, int seed)
        {
            _settings = settings;
            _seed = seed;
            GenerateBSP();
        }

        private void OnDrawGizmos()
        {
            if (_generator != null)
            {
                _generator.DrawGizmos(_showRooms, _showSplits);
            }
        }

        /// <summary>
        /// Validates the current BSP structure.
        /// </summary>
        public void ValidateBSP()
        {
            if (_generator != null)
            {
                var result = _generator.ValidateBSPStructure();
                if (result.HasErrors)
                {
                    Debug.LogError($"BSP Validation Errors:\n{result.GetErrorSummary()}");
                }
                else if (result.HasWarnings)
                {
                    Debug.LogWarning($"BSP Validation Warnings:\n{result.GetWarningSummary()}");
                }
                else
                {
                    Debug.Log("BSP validation passed");
                }
            }
        }

        /// <summary>
        /// Gets the BSP root node for advanced debugging.
        /// </summary>
        public BSPNode GetRootNode()
        {
            return _generator?.GetRootNode();
        }

        /// <summary>
        /// Prints detailed tree structure to console.
        /// </summary>
        public void PrintTreeStructure()
        {
            var root = GetRootNode();
            if (root != null)
            {
                PrintNodeRecursive(root, 0);
            }
        }

        private void PrintNodeRecursive(BSPNode node, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}{node}");
            
            if (!node.IsLeaf)
            {
                PrintNodeRecursive(node.Left, depth + 1);
                PrintNodeRecursive(node.Right, depth + 1);
            }
        }
    }
}