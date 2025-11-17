using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using NUnit.Framework;
using OfficeMice.MapGeneration.AssetLoading;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Interfaces;
using Unity.PerformanceTesting;

namespace OfficeMice.MapGeneration.Performance.Tests
{
    /// <summary>
    /// Performance-specific tests for Asset Loading system.
    /// Tests against the established performance targets from PerformanceBenchmark.
    /// </summary>
    [TestFixture]
    public class AssetLoadingPerformanceTests
    {
        private TileAssetLoader _assetLoader;
        private TilesetConfiguration _performanceConfiguration;
        private List<TileBase> _performanceTiles;

        [SetUp]
        public void SetUp()
        {
            _assetLoader = new TileAssetLoader(
                tileSearchPaths: new[] { "Assets/Game/Layout/Palette_Assets" },
                maxCacheSize: 1000,
                enableMemoryMonitoring: true);
            
            _performanceTiles = new List<TileBase>();
            CreatePerformanceTestAssets();
            CreatePerformanceConfiguration();
        }

        [TearDown]
        public void TearDown()
        {
            _assetLoader?.ClearCache();
            _assetLoader = null;
            
            foreach (var tile in _performanceTiles)
            {
                if (tile != null)
                {
                    UnityEngine.Object.DestroyImmediate(tile);
                }
            }
            _performanceTiles.Clear();
        }

        #region Performance Target Tests

        [Test, Performance]
        [Category("Performance")]
        [Category("AssetLoading")]
        public void Performance_AssetLoading_Target100ms()
        {
            // Arrange
            var assetNames = GenerateAssetNames(691); // Target number of tiles
            
            // Act & Assert
            Measure.Method(() =>
            {
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(3)
            .MeasurementCount(20)
            .SampleGroup("AssetLoading_691Tiles")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(100); // 100ms target
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("AssetLoading")]
        public void Performance_MemoryUsage_Target20MB()
        {
            // Arrange
            var assetNames = GenerateAssetNames(691);
            var memoryBefore = GC.GetTotalMemory(true);
            
            // Act
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .SampleGroup("MemoryUsage_691Tiles")
            .Definitions()
            .Run();
            
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsedMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
            
            // Assert
            Assert.Less(memoryUsedMB, 20.0, 
                $"Memory usage {memoryUsedMB:F2}MB should be less than 20MB target");
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("AssetLoading")]
        public void Performance_GcPressure_Target20KB()
        {
            // Arrange
            var assetNames = GenerateAssetNames(100); // Smaller set for GC pressure test
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("GcPressure_AssetLoading")
            .Definitions()
            .Run()
            .AssertGcLessThan(20 * 1024); // 20KB target
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("AssetLoading")]
        public void Performance_CacheHitRate_Target95Percent()
        {
            // Arrange
            var assetNames = GenerateAssetNames(100);
            
            // Preload assets to cache
            foreach (var assetName in assetNames)
            {
                _assetLoader.LoadTile(assetName);
            }
            
            // Act & Assert
            Measure.Method(() =>
            {
                // All these should be cache hits
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("CacheHitRate_100Tiles")
            .Definitions()
            .Run();
            
            // Verify cache hit rate
            var stats = _assetLoader.GetCacheStats();
            Assert.GreaterOrEqual(stats.HitRate, 0.95f, 
                $"Cache hit rate {stats.HitRate:P2} should be at least 95%");
        }

        #endregion

        #region Stress Tests

        [Test, Performance]
        [Category("Performance")]
        [Category("Stress")]
        public void Stress_LargeTileSet_HandlesEfficiently()
        {
            // Arrange
            var largeAssetSet = GenerateAssetNames(1000); // Stress test with more tiles
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                foreach (var assetName in largeAssetSet)
                {
                    _assetLoader.LoadTile(assetName);
                }
            })
            .WarmupCount(2)
            .MeasurementCount(5)
            .SampleGroup("Stress_1000Tiles")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(150); // Allow more time for stress test
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Stress")]
        public void Stress_RepeatedAccess_MaintainsPerformance()
        {
            // Arrange
            var assetNames = GenerateAssetNames(200);
            
            // Preload assets
            foreach (var assetName in assetNames)
            {
                _assetLoader.LoadTile(assetName);
            }
            
            // Act & Assert - Test repeated access
            Measure.Method(() =>
            {
                for (int i = 0; i < 10; i++) // Multiple passes
                {
                    foreach (var assetName in assetNames)
                    {
                        _assetLoader.LoadTile(assetName);
                    }
                }
            })
            .WarmupCount(3)
            .MeasurementCount(20)
            .SampleGroup("RepeatedAccess_200Tiles")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(50); // Should be fast due to caching
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Stress")]
        public void Stress_MemoryPressure_HandlesCorrectly()
        {
            // Arrange
            var assetNames = GenerateAssetNames(500);
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                
                // Load assets in batches to test memory management
                for (int i = 0; i < assetNames.Count; i += 50)
                {
                    var batch = assetNames.Skip(i).Take(50);
                    foreach (var assetName in batch)
                    {
                        _assetLoader.LoadTile(assetName);
                    }
                    
                    // Force occasional GC to test memory pressure handling
                    if (i % 100 == 0)
                    {
                        GC.Collect();
                    }
                }
            })
            .WarmupCount(2)
            .MeasurementCount(5)
            .SampleGroup("MemoryPressure_500Tiles")
            .Definitions()
            .Run()
            .AssertGcLessThan(50 * 1024); // Allow more GC for stress test
        }

        #endregion

        #region Weighted Selection Performance Tests

        [Test, Performance]
        [Category("Performance")]
        [Category("WeightedSelection")]
        public void Performance_WeightedSelection_FastExecution()
        {
            // Arrange
            _assetLoader.SetTilesetConfiguration(_performanceConfiguration);
            var random = new System.Random(42);
            
            // Act & Assert
            Measure.Method(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _assetLoader.GetRandomTile(TileType.Floor, random);
                    _assetLoader.GetRandomTile(TileType.Wall, random);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("WeightedSelection_2000Calls")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(10); // Should be very fast
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("WeightedSelection")]
        public void Performance_WeightedSelectionDistribution_Correct()
        {
            // Arrange
            _assetLoader.SetTilesetConfiguration(_performanceConfiguration);
            var random = new System.Random(42);
            var results = new Dictionary<string, int>();
            
            // Act
            Measure.Method(() =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    var tile = _assetLoader.GetRandomTile(TileType.Floor, random);
                    if (tile != null)
                    {
                        results[tile.name] = results.GetValueOrDefault(tile.name, 0) + 1;
                    }
                }
            })
            .WarmupCount(1)
            .MeasurementCount(5)
            .SampleGroup("WeightedSelection_Distribution")
            .Definitions()
            .Run();
            
            // Assert distribution correctness
            var totalSelections = results.Values.Sum();
            var floorTiles = _performanceConfiguration.FloorTiles.Tiles;
            
            foreach (var tileEntry in floorTiles)
            {
                if (results.ContainsKey(tileEntry.Tile.name))
                {
                    var actualRatio = (double)results[tileEntry.Tile.name] / totalSelections;
                    var expectedRatio = tileEntry.Weight / floorTiles.Sum(t => t.Weight);
                    var tolerance = 0.05; // 5% tolerance for large sample
                    
                    Assert.AreEqual(expectedRatio, actualRatio, tolerance,
                        $"Tile {tileEntry.Tile.name} distribution should match weight within tolerance");
                }
            }
        }

        #endregion

        #region Categorization Performance Tests

        [Test, Performance]
        [Category("Performance")]
        [Category("Categorization")]
        public void Performance_TileCategorization_EfficientOrganization()
        {
            // Arrange
            var assetNames = GenerateAssetNames(300);
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
                
                // Test categorization performance
                _assetLoader.GetTilesByType(TileType.Floor);
                _assetLoader.GetTilesByType(TileType.Wall);
                _assetLoader.GetTilesByType(TileType.Decoration);
            })
            .WarmupCount(3)
            .MeasurementCount(20)
            .SampleGroup("TileCategorization_300Tiles")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(50);
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Categorization")]
        public void Performance_GetTilesByType_FastRetrieval()
        {
            // Arrange
            var assetNames = GenerateAssetNames(200);
            foreach (var assetName in assetNames)
            {
                _assetLoader.LoadTile(assetName);
            }
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.GetTilesByType(TileType.Floor);
                _assetLoader.GetTilesByType(TileType.Wall);
                _assetLoader.GetTilesByType(TileType.Ceiling);
                _assetLoader.GetTilesByType(TileType.Door);
                _assetLoader.GetTilesByType(TileType.Window);
            })
            .WarmupCount(10)
            .MeasurementCount(100)
            .SampleGroup("GetTilesByType_5Types")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(5); // Should be very fast
        }

        #endregion

        #region Preloading Performance Tests

        [Test, Performance]
        [Category("Performance")]
        [Category("Preloading")]
        public void Performance_PreloadAssets_ImprovesSubsequentAccess()
        {
            // Arrange
            var assetNames = GenerateAssetNames(100);
            
            // Measure time without preloading
            var timeWithoutPreload = Measure.Action(() =>
            {
                _assetLoader.ClearCache();
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            });
            
            // Measure time with preloading
            var timeWithPreload = Measure.Action(() =>
            {
                _assetLoader.ClearCache();
                _assetLoader.PreloadAssets(assetNames, typeof(TileBase));
                
                // Access preloaded assets
                foreach (var assetName in assetNames)
                {
                    _assetLoader.LoadTile(assetName);
                }
            });
            
            // Assert
            Assert.Less(timeWithPreload, timeWithoutPreload * 1.1,
                "Preloading should not significantly increase total time");
            
            // Verify cache hit rate after preloading
            var stats = _assetLoader.GetCacheStats();
            Assert.Greater(stats.HitRate, 0.95f,
                "Preloading should result in high cache hit rate");
        }

        [Test, Performance]
        [Category("Performance")]
        [Category("Preloading")]
        public void Performance_PreloadLargeSet_HandlesEfficiently()
        {
            // Arrange
            var largeAssetSet = GenerateAssetNames(500);
            
            // Act & Assert
            Measure.Method(() =>
            {
                _assetLoader.ClearCache();
                _assetLoader.PreloadAssets(largeAssetSet, typeof(TileBase));
            })
            .WarmupCount(2)
            .MeasurementCount(5)
            .SampleGroup("PreloadLargeSet_500Tiles")
            .Definitions()
            .Run()
            .AssertAverageTimeLessThan(200); // Allow reasonable time for large preload
        }

        #endregion

        #region Memory Monitoring Tests

        [Test]
        [Category("Performance")]
        [Category("Memory")]
        public void MemoryMonitoring_DetectsHighUsage()
        {
            // Arrange
            var assetNames = GenerateAssetNames(300);
            
            // Act
            foreach (var assetName in assetNames)
            {
                _assetLoader.LoadTile(assetName);
            }
            
            // Assert
            var stats = _assetLoader.GetCacheStats();
            Assert.Greater(stats.MemoryUsage, 0, "Memory usage should be tracked");
            Assert.Less(stats.MemoryUsage, 50 * 1024 * 1024, "Memory usage should be reasonable");
        }

        [Test]
        [Category("Performance")]
        [Category("Memory")]
        public void MemoryMonitoring_CacheLimit_Enforced()
        {
            // Arrange
            var smallCacheLoader = new TileAssetLoader(
                tileSearchPaths: new[] { "Assets" },
                maxCacheSize: 50, // Small cache limit
                enableMemoryMonitoring: true);
            
            var assetNames = GenerateAssetNames(100); // More assets than cache limit
            
            // Act
            foreach (var assetName in assetNames)
            {
                smallCacheLoader.LoadTile(assetName);
            }
            
            // Assert
            var stats = smallCacheLoader.GetCacheStats();
            Assert.LessOrEqual(stats.CachedAssets, 50, "Cache should not exceed limit");
            
            // Cleanup
            smallCacheLoader.ClearCache();
        }

        #endregion

        #region Helper Methods

        private List<string> GenerateAssetNames(int count)
        {
            var names = new List<string>();
            var types = new[] { "floor", "wall", "door", "window", "ceiling", "decor", "hazard" };
            var random = new System.Random(42);
            
            for (int i = 0; i < count; i++)
            {
                var type = types[random.Next(types.Length)];
                names.Add($"terrainTiles_retina_{type}_{i:D3}");
            }
            
            return names;
        }

        private void CreatePerformanceTestAssets()
        {
            // Create a variety of tiles for performance testing
            var types = new[] { "floor", "wall", "door", "window", "ceiling", "decor", "hazard" };
            
            foreach (var type in types)
            {
                for (int i = 0; i < 20; i++)
                {
                    var tile = ScriptableObject.CreateInstance<TileBase>();
                    tile.name = $"terrainTiles_retina_{type}_{i:D3}";
                    _performanceTiles.Add(tile);
                }
            }
        }

        private void CreatePerformanceConfiguration()
        {
            _performanceConfiguration = ScriptableObject.CreateInstance<TilesetConfiguration>();
            
            // Create floor tiles mapping with various weights
            var floorMapping = new TileMapping { UseRandomSelection = true };
            var floorTiles = _performanceTiles.Where(t => t.name.Contains("floor")).ToList();
            
            for (int i = 0; i < floorTiles.Count; i++)
            {
                floorMapping.Tiles.Add(new TileEntry
                {
                    Tile = floorTiles[i],
                    Weight = (i + 1) * 0.1f // Varying weights
                });
            }
            
            // Create wall tiles mapping
            var wallMapping = new TileMapping { UseRandomSelection = true };
            var wallTiles = _performanceTiles.Where(t => t.name.Contains("wall")).ToList();
            
            foreach (var wallTile in wallTiles)
            {
                wallMapping.Tiles.Add(new TileEntry
                {
                    Tile = wallTile,
                    Weight = 1.0f
                });
            }
            
            // Use reflection to set private fields
            var floorField = typeof(TilesetConfiguration).GetField("_floorTiles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wallField = typeof(TilesetConfiguration).GetField("_wallTiles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            floorField?.SetValue(_performanceConfiguration, floorMapping);
            wallField?.SetValue(_performanceConfiguration, wallMapping);
        }

        #endregion
    }
}