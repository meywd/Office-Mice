using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.PerformanceTesting;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Mocks;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Base;
using OfficeMice.MapGeneration.Factories;

namespace OfficeMice.MapGeneration.Performance
{
    /// <summary>
    /// Automated performance regression detection system.
    /// Continuously monitors performance metrics and detects regressions against established baselines.
    /// </summary>
    [TestFixture]
    public class PerformanceRegressionTests : BaseTestFixture
    {
        private struct RegressionThreshold
        {
            public double TimeRegressionTolerance; // Percentage
            public double MemoryRegressionTolerance; // Percentage
            public double GcPressureRegressionTolerance; // Percentage
            public double AbsoluteTimeThresholdMs; // Absolute ms increase
            public long AbsoluteMemoryThresholdMB; // Absolute MB increase
        }

        private static readonly Dictionary<string, RegressionThreshold> RegressionThresholds = new()
        {
            ["MapGeneration"] = new RegressionThreshold 
            { 
                TimeRegressionTolerance = 0.1, // 10%
                MemoryRegressionTolerance = 0.15, // 15%
                GcPressureRegressionTolerance = 0.2, // 20%
                AbsoluteTimeThresholdMs = 100, // 100ms
                AbsoluteMemoryThresholdMB = 10 * 1024 * 1024 // 10MB
            },
            ["RoomGeneration"] = new RegressionThreshold 
            { 
                TimeRegressionTolerance = 0.15,
                MemoryRegressionTolerance = 0.2,
                GcPressureRegressionTolerance = 0.25,
                AbsoluteTimeThresholdMs = 50,
                AbsoluteMemoryThresholdMB = 5 * 1024 * 1024
            },
            ["CorridorGeneration"] = new RegressionThreshold 
            { 
                TimeRegressionTolerance = 0.2,
                MemoryRegressionTolerance = 0.25,
                GcPressureRegressionTolerance = 0.3,
                AbsoluteTimeThresholdMs = 25,
                AbsoluteMemoryThresholdMB = 3 * 1024 * 1024
            },
            ["Pathfinding"] = new RegressionThreshold 
            { 
                TimeRegressionTolerance = 0.25,
                MemoryRegressionTolerance = 0.3,
                GcPressureRegressionTolerance = 0.35,
                AbsoluteTimeThresholdMs = 10,
                AbsoluteMemoryThresholdMB = 2 * 1024 * 1024
            }
        };

        private MemoryTracker _memoryTracker;
        private GcPressureMonitor _gcMonitor;

        [SetUp]
        public void RegressionSetUp()
        {
            _memoryTracker = new MemoryTracker();
            _gcMonitor = new GcPressureMonitor();
        }

        [TearDown]
        public void RegressionTearDown()
        {
            _memoryTracker?.Reset();
            _gcMonitor?.Reset();
        }

        #region Automated Regression Detection Tests

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Automated")]
        public void Regression_MapGeneration_AutomatedDetection()
        {
            // Arrange
            var testName = "MapGeneration";
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("standard");
            var threshold = RegressionThresholds[testName];

            // Act
            var baseline = LoadOrCreateBaseline(testName, () => generator.GenerateMap(settings));
            var current = MeasureCurrentPerformance(testName, () => generator.GenerateMap(settings));

            // Assert
            var regression = DetectRegression(baseline, current, threshold);
            LogRegressionResults(testName, baseline, current, regression);

            Assert.IsFalse(regression.HasRegression, 
                $"Performance regression detected in {testName}: {regression.RegressionDetails}");
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Automated")]
        public void Regression_RoomGeneration_AutomatedDetection()
        {
            // Arrange
            var testName = "RoomGeneration";
            var generator = new MockRoomGenerator();
            var settings = CreateTestSettings("standard");
            var threshold = RegressionThresholds[testName];

            // Act
            var baseline = LoadOrCreateBaseline(testName, () => generator.GenerateRooms(settings));
            var current = MeasureCurrentPerformance(testName, () => generator.GenerateRooms(settings));

            // Assert
            var regression = DetectRegression(baseline, current, threshold);
            LogRegressionResults(testName, baseline, current, regression);

            Assert.IsFalse(regression.HasRegression, 
                $"Performance regression detected in {testName}: {regression.RegressionDetails}");
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Automated")]
        public void Regression_CorridorGeneration_AutomatedDetection()
        {
            // Arrange
            var testName = "CorridorGeneration";
            var generator = new MockCorridorGenerator();
            var settings = CreateTestSettings("standard");
            var rooms = CreateTestMapData("multiple_rooms").Rooms;
            var threshold = RegressionThresholds[testName];

            // Act
            var baseline = LoadOrCreateBaseline(testName, () => generator.ConnectRooms(rooms, settings));
            var current = MeasureCurrentPerformance(testName, () => generator.ConnectRooms(rooms, settings));

            // Assert
            var regression = DetectRegression(baseline, current, threshold);
            LogRegressionResults(testName, baseline, current, regression);

            Assert.IsFalse(regression.HasRegression, 
                $"Performance regression detected in {testName}: {regression.RegressionDetails}");
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Automated")]
        public void Regression_Pathfinding_AutomatedDetection()
        {
            // Arrange
            var testName = "Pathfinding";
            var pathfinder = new MockPathfinder();
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(100, 100);
            var obstacles = new bool[200, 200];
            var threshold = RegressionThresholds[testName];

            // Act
            var baseline = LoadOrCreateBaseline(testName, () => pathfinder.FindPath(start, end, obstacles));
            var current = MeasureCurrentPerformance(testName, () => pathfinder.FindPath(start, end, obstacles));

            // Assert
            var regression = DetectRegression(baseline, current, threshold);
            LogRegressionResults(testName, baseline, current, regression);

            Assert.IsFalse(regression.HasRegression, 
                $"Performance regression detected in {testName}: {regression.RegressionDetails}");
        }

        #endregion

        #region Memory and GC Regression Tests

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Memory")]
        public void Regression_MemoryUsage_DetectsMemoryLeaks()
        {
            // Arrange
            var testName = "MemoryLeakDetection";
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            // Act - Monitor memory usage over multiple generations
            _memoryTracker.StartTracking(testName);
            
            var memorySnapshots = new List<long>();
            for (int i = 0; i < 50; i++)
            {
                var map = generator.GenerateMap(settings);
                _memoryTracker.TakeSnapshot($"generation_{i}");
                memorySnapshots.Add(GC.GetTotalMemory(false));
                
                // Force GC every 10 iterations
                if (i % 10 == 9)
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                }
            }

            _memoryTracker.StopTracking(testName);
            var memoryAnalysis = _memoryTracker.AnalyzeMemoryUsage();

            // Assert - Check for memory leaks
            var memoryGrowthRate = CalculateMemoryGrowthRate(memorySnapshots);
            var hasMemoryLeak = memoryGrowthRate > 0.1; // 10% growth rate threshold

            Debug.Log($"🔍 Memory Leak Analysis:");
            Debug.Log($"   Memory Growth Rate: {memoryGrowthRate:P2}");
            Debug.Log($"   Final Memory Delta: {memoryAnalysis.MemoryDelta / (1024 * 1024):F2}MB");
            Debug.Log($"   GC Collections: Gen0={memoryAnalysis.Gen0Collections}, Gen1={memoryAnalysis.Gen1Collections}, Gen2={memoryAnalysis.Gen2Collections}");

            Assert.IsFalse(hasMemoryLeak, 
                $"Memory leak detected: growth rate {memoryGrowthRate:P2} exceeds 10% threshold");
            Assert.Less(memoryAnalysis.Gen2Collections, 5, 
                $"Too many Gen2 collections: {memoryAnalysis.Gen2Collections}, indicates potential memory leaks");
        }

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("GC")]
        public void Regression_GcPressure_DetectsIncreasedPressure()
        {
            // Arrange
            var testName = "GcPressureRegression";
            var generator = new MockMapGenerator();
            var settings = CreateTestSettings("performance");

            // Act - Monitor GC pressure
            _gcMonitor.StartMonitoring(testName);
            
            for (int i = 0; i < 100; i++)
            {
                generator.GenerateMap(settings);
                _gcMonitor.RecordFrameGcPressure($"frame_{i}");
            }

            _gcMonitor.StopMonitoring(testName);
            var gcAnalysis = _gcMonitor.AnalyzeGcPressure();

            // Assert - Check GC pressure regression
            var isValid = _gcMonitor.ValidateGcPressure();

            Debug.Log($"🗑️ GC Pressure Regression Analysis:");
            Debug.Log($"   Average GC Pressure: {gcAnalysis.AverageGcPressure / 1024:F2}KB/frame");
            Debug.Log($"   Max GC Pressure: {gcAnalysis.MaxGcPressure / 1024:F2}KB/frame");
            Debug.Log($"   Threshold Violations: {gcAnalysis.ThresholdViolationCount}/{gcAnalysis.FrameCount}");
            Debug.Log($"   GC Efficiency: {gcAnalysis.GcEfficiency / 1024:F2}KB/collection");

            Assert.IsTrue(isValid, 
                $"GC pressure regression detected: average {gcAnalysis.AverageGcPressure / 1024:F2}KB/frame exceeds acceptable limits");
        }

        #endregion

        #region Scaling Regression Tests

        [Test]
        [Category("Performance")]
        [Category("Regression")]
        [Category("Scaling")]
        public void Regression_Scaling_DetectsNonLinearScaling()
        {
            // Arrange
            var testName = "ScalingRegression";
            var generator = new MockMapGenerator();
            var sizes = new[] { 25, 50, 100, 200 };
            var scalingMetrics = new List<(int size, double timeMs, double memoryMB)>();

            // Act - Measure performance at different scales
            foreach (var size in sizes)
            {
                var settings = CreateTestSettings("standard");
                settings.mapWidth = size;
                settings.mapHeight = size;
                
                var metrics = MeasurePerformanceWithGC(() => generator.GenerateMap(settings), 
                    $"{testName}_size_{size}", 20);
                
                scalingMetrics.Add((size, metrics.AverageTimeMs, metrics.MemoryUsedBytes / (1024.0 * 1024.0)));
            }

            // Assert - Check for non-linear scaling regression
            var scalingRegression = DetectScalingRegression(scalingMetrics);
            
            Debug.Log($"📈 Scaling Regression Analysis:");
            foreach (var (size, time, memory) in scalingMetrics)
            {
                Debug.Log($"   Size {size}x{size}: {time:F2}ms, {memory:F2}MB");
            }
            Debug.Log($"   Scaling Regression: {(scalingRegression.HasRegression ? "❌ Detected" : "✅ None")}");
            if (scalingRegression.HasRegression)
            {
                Debug.Log($"   Details: {scalingRegression.RegressionDetails}");
            }

            Assert.IsFalse(scalingRegression.HasRegression, 
                $"Scaling regression detected: {scalingRegression.RegressionDetails}");
        }

        #endregion

        #region Baseline Management

        private PerformanceMetrics LoadOrCreateBaseline(string testName, Action testAction)
        {
            var baseline = LoadBaselineMetrics(testName);
            if (baseline.HasValue)
            {
                Debug.Log($"📂 Loaded existing baseline for {testName}");
                return baseline.Value;
            }

            Debug.Log($"🆕 Creating new baseline for {testName}");
            var newBaseline = MeasurePerformanceWithGC(testAction, testName, 100);
            SaveBaselineMetrics(testName, newBaseline);
            return newBaseline;
        }

        private PerformanceMetrics MeasureCurrentPerformance(string testName, Action testAction)
        {
            return MeasurePerformanceWithGC(testAction, $"{testName}_current", 50);
        }

        private RegressionResult DetectRegression(PerformanceMetrics baseline, PerformanceMetrics current, RegressionThreshold threshold)
        {
            var regressions = new List<string>();

            // Time regression
            var timeRegression = (current.AverageTimeMs - baseline.AverageTimeMs) / baseline.AverageTimeMs;
            var absoluteTimeIncrease = current.AverageTimeMs - baseline.AverageTimeMs;
            
            if (timeRegression > threshold.TimeRegressionTolerance || absoluteTimeIncrease > threshold.AbsoluteTimeThresholdMs)
            {
                regressions.Add($"Time: +{timeRegression:P2} (+{absoluteTimeIncrease:F2}ms)");
            }

            // Memory regression
            var memoryRegression = (double)(current.MemoryUsedBytes - baseline.MemoryUsedBytes) / baseline.MemoryUsedBytes;
            var absoluteMemoryIncrease = current.MemoryUsedBytes - baseline.MemoryUsedBytes;
            
            if (memoryRegression > threshold.MemoryRegressionTolerance || absoluteMemoryIncrease > threshold.AbsoluteMemoryThresholdMB)
            {
                regressions.Add($"Memory: +{memoryRegression:P2} (+{absoluteMemoryIncrease / (1024 * 1024):F2}MB)");
            }

            // GC pressure regression
            var gcRegression = (current.GcPressureBytes - baseline.GcPressureBytes) / Math.Max(baseline.GcPressureBytes, 1);
            
            if (gcRegression > threshold.GcPressureRegressionTolerance)
            {
                regressions.Add($"GC Pressure: +{gcRegression:P2} (+{(current.GcPressureBytes - baseline.GcPressureBytes) / 1024:F2}KB)");
            }

            return new RegressionResult
            {
                HasRegression = regressions.Count > 0,
                RegressionDetails = string.Join(", ", regressions),
                TimeRegression = timeRegression,
                MemoryRegression = memoryRegression,
                GcPressureRegression = gcRegression
            };
        }

        private ScalingRegressionResult DetectScalingRegression(List<(int size, double timeMs, double memoryMB)> metrics)
        {
            if (metrics.Count < 2)
                return new ScalingRegressionResult { HasRegression = false };

            var regressions = new List<string>();

            // Check time scaling
            for (int i = 1; i < metrics.Count; i++)
            {
                var sizeRatio = (double)metrics[i].size / metrics[i - 1].size;
                var timeRatio = metrics[i].timeMs / metrics[i - 1].timeMs;
                var memoryRatio = metrics[i].memoryMB / metrics[i - 1].memoryMB;

                // Allow up to 2x the size ratio (some non-linearity is expected)
                if (timeRatio > sizeRatio * 2)
                {
                    regressions.Add($"Time scaling non-linear: size {sizeRatio:F2}x, time {timeRatio:F2}x");
                }

                if (memoryRatio > sizeRatio * 2)
                {
                    regressions.Add($"Memory scaling non-linear: size {sizeRatio:F2}x, memory {memoryRatio:F2}x");
                }
            }

            return new ScalingRegressionResult
            {
                HasRegression = regressions.Count > 0,
                RegressionDetails = string.Join(", ", regressions)
            };
        }

        private double CalculateMemoryGrowthRate(List<long> memorySnapshots)
        {
            if (memorySnapshots.Count < 2) return 0;

            var first = memorySnapshots[0];
            var last = memorySnapshots[memorySnapshots.Count - 1];
            
            return (double)(last - first) / first;
        }

        private void LogRegressionResults(string testName, PerformanceMetrics baseline, PerformanceMetrics current, RegressionResult regression)
        {
            Debug.Log($"🔍 Regression Analysis for {testName}:");
            Debug.Log($"   Baseline: {baseline.AverageTimeMs:F2}ms, {baseline.MemoryUsedBytes / (1024 * 1024):F2}MB, {baseline.GcPressureBytes / 1024:F2}KB");
            Debug.Log($"   Current: {current.AverageTimeMs:F2}ms, {current.MemoryUsedBytes / (1024 * 1024):F2}MB, {current.GcPressureBytes / 1024:F2}KB");
            Debug.Log($"   Regression: {(regression.HasRegression ? "❌ Detected" : "✅ None")}");
            if (regression.HasRegression)
            {
                Debug.Log($"   Details: {regression.RegressionDetails}");
            }
        }

        #endregion

        #region Helper Methods

        private PerformanceMetrics MeasurePerformanceWithGC(Action action, string testName, int iterations)
        {
            var times = new List<long>();
            var gcPressures = new List<long>();
            var memoryBefore = GC.GetTotalMemory(true);

            // Warmup
            for (int i = 0; i < 10; i++)
            {
                action();
            }

            // Force GC before measurement
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // Actual measurement
            for (int i = 0; i < iterations; i++)
            {
                var memoryBeforeIteration = GC.GetTotalMemory(false);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                
                var memoryAfterIteration = GC.GetTotalMemory(false);
                
                times.Add(stopwatch.ElapsedMilliseconds);
                gcPressures.Add(memoryAfterIteration - memoryBeforeIteration);
            }

            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;
            var totalGcPressure = gcPressures.Count > 0 ? gcPressures.Average() : 0;

            // Calculate statistics
            times.Sort();
            var averageTime = times.Count > 0 ? times.Average() : 0;
            
            var variance = times.Count > 0 ? times.Sum(t => Math.Pow(t - averageTime, 2)) / times.Count : 0;
            var standardDeviation = Math.Sqrt(variance);

            return new PerformanceMetrics
            {
                ExecutionTimeMs = times.Count > 0 ? times.Sum() : 0,
                MemoryUsedBytes = memoryUsed,
                TestIterations = iterations,
                AverageTimeMs = averageTime,
                MinTimeMs = times.Count > 0 ? times[0] : 0,
                MaxTimeMs = times.Count > 0 ? times[times.Count - 1] : 0,
                StandardDeviationMs = standardDeviation,
                GcPressureBytes = totalGcPressure,
                FrameCount = 1,
                FrameTimeMs = averageTime
            };
        }

        private PerformanceMetrics? LoadBaselineMetrics(string testName)
        {
            var baselinePath = Path.Combine(Application.persistentDataPath, "PerformanceBaselines");
            var filePath = Path.Combine(baselinePath, $"{testName}_baseline.json");

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                var baseline = JsonUtility.FromJson<BaselineData>(json);
                
                return new PerformanceMetrics
                {
                    AverageTimeMs = baseline.AverageTimeMs,
                    MemoryUsedBytes = baseline.MemoryUsedBytes,
                    GcPressureBytes = baseline.GcPressureBytes,
                    StandardDeviationMs = baseline.StandardDeviationMs,
                    TestIterations = baseline.TestIterations,
                    MinTimeMs = baseline.AverageTimeMs,
                    MaxTimeMs = baseline.AverageTimeMs,
                    ExecutionTimeMs = baseline.AverageTimeMs * baseline.TestIterations,
                    FrameCount = 1,
                    FrameTimeMs = baseline.AverageTimeMs
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load baseline for {testName}: {ex.Message}");
                return null;
            }
        }

        private void SaveBaselineMetrics(string testName, PerformanceMetrics metrics)
        {
            var baselinePath = Path.Combine(Application.persistentDataPath, "PerformanceBaselines");
            Directory.CreateDirectory(baselinePath);
            
            var filePath = Path.Combine(baselinePath, $"{testName}_baseline.json");
            var baseline = new BaselineData
            {
                TestName = testName,
                Timestamp = DateTime.UtcNow.ToString("O"),
                AverageTimeMs = metrics.AverageTimeMs,
                MemoryUsedBytes = metrics.MemoryUsedBytes,
                GcPressureBytes = metrics.GcPressureBytes,
                StandardDeviationMs = metrics.StandardDeviationMs,
                TestIterations = metrics.TestIterations
            };

            File.WriteAllText(filePath, JsonUtility.ToJson(baseline, true));
            Debug.Log($"💾 Baseline saved to: {filePath}");
        }

        [System.Serializable]
        private class BaselineData
        {
            public string TestName;
            public string Timestamp;
            public double AverageTimeMs;
            public long MemoryUsedBytes;
            public double GcPressureBytes;
            public double StandardDeviationMs;
            public int TestIterations;
        }

        private struct RegressionResult
        {
            public bool HasRegression;
            public string RegressionDetails;
            public double TimeRegression;
            public double MemoryRegression;
            public double GcPressureRegression;
        }

        private struct ScalingRegressionResult
        {
            public bool HasRegression;
            public string RegressionDetails;
        }

        #endregion
    }
}