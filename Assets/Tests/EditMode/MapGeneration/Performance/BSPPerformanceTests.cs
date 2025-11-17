using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OfficeMice.MapGeneration.Generators;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using Unity.PerformanceTesting;

namespace OfficeMice.MapGeneration.Tests.Performance
{
    [TestFixture]
    public class BSPPerformanceTests
    {
        private BSPGenerator _generator;
        private MapGenerationSettings _defaultSettings;

        [SetUp]
        public void SetUp()
        {
            _generator = new BSPGenerator();
            _defaultSettings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 100, 100),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 10,
                    MaxDepth = 5,
                    SplitPreference = SplitPreference.Alternate,
                    SplitPositionVariation = 0.3f,
                    StopSplittingChance = 0.1f,
                    RoomSizeRatio = 0.8f,
                    RoomPositionVariation = 0.1f
                }
            };
        }

        [Test, Performance]
        public void GenerateRooms_SmallMap_PerformsWithinTarget()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 50, 50),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 8,
                    MaxDepth = 4,
                    RoomSizeRatio = 0.8f
                }
            };

            // Act & Assert
            Measure.Method(() => {
                var rooms = _generator.GenerateRooms(settings, 12345);
                Assert.IsTrue(rooms.Count > 0);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .SampleGroup("SmallMapGeneration")
            .MaxMilliseconds(50) // Target: 50ms for small maps
            .Run();
        }

        [Test, Performance]
        public void GenerateRooms_MediumMap_PerformsWithinTarget()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 200, 200),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 12,
                    MaxDepth = 6,
                    RoomSizeRatio = 0.8f
                }
            };

            // Act & Assert
            Measure.Method(() => {
                var rooms = _generator.GenerateRooms(settings, 12345);
                Assert.IsTrue(rooms.Count > 0);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .SampleGroup("MediumMapGeneration")
            .MaxMilliseconds(200) // Target: 200ms for medium maps
            .Run();
        }

        [Test, Performance]
        public void GenerateRooms_LargeMap_PerformsWithinTarget()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 500, 500),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 15,
                    MaxDepth = 8,
                    RoomSizeRatio = 0.8f
                }
            };

            // Act & Assert
            Measure.Method(() => {
                var rooms = _generator.GenerateRooms(settings, 12345);
                Assert.IsTrue(rooms.Count > 0);
            })
            .WarmupCount(2)
            .MeasurementCount(5)
            .SampleGroup("LargeMapGeneration")
            .MaxMilliseconds(1000) // Target: 1 second for large maps
            .Run();
        }

        [Test]
        public void GenerateRooms_ComplexityTest_ScalesLogarithmically()
        {
            // Arrange
            var mapSizes = new[] { 50, 100, 200, 400 };
            var generationTimes = new List<double>();

            // Act
            foreach (var size in mapSizes)
            {
                var settings = new MapGenerationSettings
                {
                    mapBounds = new RectInt(0, 0, size, size),
                    bsp = new BSPConfiguration
                    {
                        MinPartitionSize = 10,
                        MaxDepth = 7,
                        RoomSizeRatio = 0.8f
                    }
                };

                var stopwatch = Stopwatch.StartNew();
                var rooms = _generator.GenerateRooms(settings, 12345);
                stopwatch.Stop();

                generationTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                Debug.Log($"Map size {size}x{size}: {rooms.Count} rooms in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            }

            // Assert - Check that growth is roughly logarithmic
            // For O(n log n), doubling input should roughly double the time plus some logarithmic factor
            for (int i = 1; i < generationTimes.Count; i++)
            {
                double previousTime = generationTimes[i - 1];
                double currentTime = generationTimes[i];
                double ratio = currentTime / previousTime;
                
                // Allow some tolerance for the logarithmic factor
                Assert.IsTrue(ratio < 3.0, 
                    $"Performance scaling appears worse than O(n log n). Ratio: {ratio:F2}");
            }
        }

        [Test]
        public void GenerateRooms_MemoryUsage_StaysWithinBounds()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 300, 300),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 12,
                    MaxDepth = 7,
                    RoomSizeRatio = 0.8f
                }
            };

            // Act
            var initialMemory = System.GC.GetTotalMemory(true);
            var rooms = _generator.GenerateRooms(settings, 12345);
            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(rooms.Count > 0);
            Assert.IsTrue(memoryUsed < 50 * 1024 * 1024, // 50MB limit
                $"Memory usage too high: {memoryUsed / (1024 * 1024):F2}MB for {rooms.Count} rooms");
            
            Debug.Log($"Generated {rooms.Count} rooms using {memoryUsed / 1024:F2}KB memory");
        }

        [Test]
        public void GenerateRooms_MultipleGenerations_PerformsConsistently()
        {
            // Arrange
            var settings = _defaultSettings;
            var generationTimes = new List<double>();
            int iterations = 50;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var rooms = _generator.GenerateRooms(settings, i); // Different seed each time
                stopwatch.Stop();
                
                generationTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                Assert.IsTrue(rooms.Count > 0);
            }

            // Assert
            var averageTime = generationTimes.Average();
            var maxTime = generationTimes.Max();
            var minTime = generationTimes.Min();

            Debug.Log($"BSP Generation over {iterations} iterations:");
            Debug.Log($"  Average: {averageTime:F2}ms");
            Debug.Log($"  Min: {minTime:F2}ms");
            Debug.Log($"  Max: {maxTime:F2}ms");

            // Performance should be consistent (within reasonable variance)
            Assert.IsTrue(maxTime / averageTime < 3.0, 
                "Performance variance too high - potential memory leak or inefficient caching");
        }

        [Test]
        public void ValidateRoomPlacement_PerformsEfficiently()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 400, 400),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 10,
                    MaxDepth = 7,
                    RoomSizeRatio = 0.8f
                }
            };
            var rooms = _generator.GenerateRooms(settings, 12345);

            // Act & Assert
            Measure.Method(() => {
                var result = _generator.ValidateRoomPlacement(rooms, settings);
                Assert.IsFalse(result.HasErrors);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .SampleGroup("RoomValidation")
            .MaxMilliseconds(10) // Validation should be very fast
            .Run();
        }

        [Test]
        public void GetStatistics_PerformsEfficiently()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 300, 300),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 10,
                    MaxDepth = 7,
                    RoomSizeRatio = 0.8f
                }
            };
            _generator.GenerateRooms(settings, 12345);

            // Act & Assert
            Measure.Method(() => {
                var stats = _generator.GetStatistics();
                Assert.IsTrue(stats.TotalNodes > 0);
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .SampleGroup("StatisticsCalculation")
            .MaxMilliseconds(1) // Statistics should be extremely fast
            .Run();
        }

        [Test]
        public void ValidateBSPStructure_PerformsEfficiently()
        {
            // Arrange
            var settings = new MapGenerationSettings
            {
                mapBounds = new RectInt(0, 0, 300, 300),
                bsp = new BSPConfiguration
                {
                    MinPartitionSize = 10,
                    MaxDepth = 7,
                    RoomSizeRatio = 0.8f
                }
            };
            _generator.GenerateRooms(settings, 12345);

            // Act & Assert
            Measure.Method(() => {
                var result = _generator.ValidateBSPStructure();
                Assert.IsFalse(result.HasErrors);
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .SampleGroup("BSPValidation")
            .MaxMilliseconds(5) // Validation should be fast
            .Run();
        }
    }
}