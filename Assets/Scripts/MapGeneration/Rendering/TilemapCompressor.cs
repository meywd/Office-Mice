using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OfficeMice.MapGeneration.Rendering
{
    /// <summary>
    /// Component that optimizes tilemap rendering by compressing adjacent tiles of the same type.
    /// Reduces draw calls and improves rendering performance.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class TilemapCompressor : MonoBehaviour
    {
        [Header("Compression Settings")]
        [SerializeField] private bool _enableCompression = true;
        [SerializeField] private bool _compressOnStart = false;
        [SerializeField] private bool _compressOnEnable = false;
        [SerializeField] private float _compressionThreshold = 0.8f;
        
        [Header("Debug")]
        [SerializeField] private bool _showCompressionStats = false;
        [SerializeField, TextArea(3, 5)] private string _compressionStats;
        
        private Tilemap _tilemap;
        private Dictionary<TileBase, List<Vector3Int>> _tileGroups;
        
        // Performance tracking
        private int _originalTileCount;
        private int _compressedTileCount;
        private float _compressionRatio;
        
        public bool EnableCompression
        {
            get => _enableCompression;
            set => _enableCompression = value;
        }
        
        public float CompressionRatio => _compressionRatio;
        public int OriginalTileCount => _originalTileCount;
        public int CompressedTileCount => _compressedTileCount;
        
        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            _tileGroups = new Dictionary<TileBase, List<Vector3Int>>();
        }
        
        private void Start()
        {
            if (_compressOnStart)
            {
                Compress();
            }
        }
        
        private void OnEnable()
        {
            if (_compressOnEnable)
            {
                Compress();
            }
        }
        
        /// <summary>
        /// Compresses the tilemap by grouping adjacent tiles of the same type.
        /// </summary>
        public void Compress()
        {
            if (!_enableCompression || _tilemap == null)
                return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Get all tiles in the tilemap
            var bounds = _tilemap.cellBounds;
            var allTiles = _tilemap.GetTilesBlock(bounds);
            
            _originalTileCount = 0;
            _compressedTileCount = 0;
            _tileGroups.Clear();
            
            // Group tiles by type
            for (int x = bounds.x; x < bounds.xMax; x++)
            {
                for (int y = bounds.y; y < bounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var tile = _tilemap.GetTile(pos);
                    
                    if (tile != null)
                    {
                        _originalTileCount++;
                        
                        if (!_tileGroups.ContainsKey(tile))
                        {
                            _tileGroups[tile] = new List<Vector3Int>();
                        }
                        
                        _tileGroups[tile].Add(pos);
                    }
                }
            }
            
            // Optimize each tile group
            foreach (var kvp in _tileGroups)
            {
                var tile = kvp.Key;
                var positions = kvp.Value;
                
                if (positions.Count > 1)
                {
                    OptimizeTileGroup(tile, positions);
                }
            }
            
            stopwatch.Stop();
            _compressionRatio = _originalTileCount > 0 ? (float)_compressedTileCount / _originalTileCount : 1f;
            
            // Update stats
            if (_showCompressionStats)
            {
                _compressionStats = $"Original: {_originalTileCount} tiles\n" +
                                  $"Compressed: {_compressedTileCount} tiles\n" +
                                  $"Ratio: {_compressionRatio:P1}\n" +
                                  $"Time: {stopwatch.ElapsedMilliseconds}ms";
            }
            
            Debug.Log($"Tilemap compression completed in {stopwatch.ElapsedMilliseconds}ms. " +
                     $"Reduced from {_originalTileCount} to {_compressedTileCount} tiles ({_compressionRatio:P1})");
        }
        
        /// <summary>
        /// Optimizes a group of tiles of the same type by finding rectangular regions.
        /// </summary>
        private void OptimizeTileGroup(TileBase tile, List<Vector3Int> positions)
        {
            if (positions.Count < 2)
                return;
            
            var unprocessedPositions = new HashSet<Vector3Int>(positions);
            var optimizedRegions = new List<BoundsInt>();
            
            while (unprocessedPositions.Count > 0)
            {
                // Find the largest rectangular region from remaining positions
                var region = FindLargestRectangle(unprocessedPositions);
                if (region.size.x <= 0 || region.size.y <= 0)
                    break;
                
                optimizedRegions.Add(region);
                
                // Remove positions in this region from unprocessed set
                for (int x = region.x; x < region.xMax; x++)
                {
                    for (int y = region.y; y < region.yMax; y++)
                    {
                        unprocessedPositions.Remove(new Vector3Int(x, y, 0));
                    }
                }
            }
            
            // Apply optimization if it's beneficial
            if (ShouldApplyOptimization(positions.Count, optimizedRegions))
            {
                // Clear original tiles
                foreach (var pos in positions)
                {
                    _tilemap.SetTile(pos, null);
                }
                
                // Place optimized tiles
                foreach (var region in optimizedRegions)
                {
                    _tilemap.BoxFill(region.min, tile, region.min.x, region.min.y, region.max.x - 1, region.max.y - 1);
                    _compressedTileCount += region.size.x * region.size.y;
                }
            }
            else
            {
                _compressedTileCount += positions.Count;
            }
        }
        
        /// <summary>
        /// Finds the largest rectangular region from a set of positions.
        /// </summary>
        private BoundsInt FindLargestRectangle(HashSet<Vector3Int> positions)
        {
            if (positions.Count == 0)
                return new BoundsInt();
            
            var positionList = new List<Vector3Int>(positions);
            var bestRegion = new BoundsInt();
            var maxArea = 0;
            
            // Try each position as a potential corner
            foreach (var startPos in positionList)
            {
                // Expand rectangle to find maximum size
                var maxX = startPos.x;
                var maxY = startPos.y;
                
                // Find maximum width
                while (positions.Contains(new Vector3Int(maxX + 1, startPos.y, 0)))
                {
                    maxX++;
                }
                
                // Find maximum height for each column
                var currentWidth = maxX - startPos.x + 1;
                for (var width = currentWidth; width > 0; width--)
                {
                    var height = 0;
                    while (IsRectangleValid(startPos, width, height + 1, positions))
                    {
                        height++;
                    }
                    
                    var area = width * height;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        bestRegion = new BoundsInt(startPos.x, startPos.y, 0, width, height, 1);
                    }
                }
            }
            
            return bestRegion;
        }
        
        /// <summary>
        /// Checks if a rectangle is valid (all positions exist in the set).
        /// </summary>
        private bool IsRectangleValid(Vector3Int start, int width, int height, HashSet<Vector3Int> positions)
        {
            for (int x = start.x; x < start.x + width; x++)
            {
                for (int y = start.y; y < start.y + height; y++)
                {
                    if (!positions.Contains(new Vector3Int(x, y, 0)))
                        return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Determines if optimization should be applied based on compression benefits.
        /// </summary>
        private bool ShouldApplyOptimization(int originalCount, List<BoundsInt> optimizedRegions)
        {
            if (optimizedRegions.Count == 0)
                return false;
            
            var optimizedCount = optimizedRegions.Sum(r => r.size.x * r.size.y);
            var compressionRatio = (float)optimizedCount / originalCount;
            
            // Apply optimization if it reduces the number of draw calls significantly
            return compressionRatio >= _compressionThreshold;
        }
        
        /// <summary>
        /// Gets compression statistics for debugging.
        /// </summary>
        public string GetCompressionStats()
        {
            return $"Tilemap Compression Stats:\n" +
                   $"Original Tiles: {_originalTileCount}\n" +
                   $"Compressed Tiles: {_compressedTileCount}\n" +
                   $"Compression Ratio: {_compressionRatio:P1}\n" +
                   $"Tile Groups: {_tileGroups.Count}";
        }
        
        /// <summary>
        /// Resets compression statistics.
        /// </summary>
        public void ResetStats()
        {
            _originalTileCount = 0;
            _compressedTileCount = 0;
            _compressionRatio = 1f;
            _compressionStats = string.Empty;
        }
        
        private void OnValidate()
        {
            _compressionThreshold = Mathf.Clamp01(_compressionThreshold);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_showCompressionStats || _tileGroups == null)
                return;
            
            // Draw debug information for tile groups
            Gizmos.color = Color.yellow;
            
            foreach (var kvp in _tileGroups)
            {
                var positions = kvp.Value;
                if (positions.Count > 10) // Only show large groups
                {
                    var center = Vector3.zero;
                    foreach (var pos in positions)
                    {
                        center += _tilemap.GetCellCenterWorld(pos);
                    }
                    center /= positions.Count;
                    
                    Gizmos.DrawWireSphere(center, 0.5f);
                }
            }
        }
    }
}