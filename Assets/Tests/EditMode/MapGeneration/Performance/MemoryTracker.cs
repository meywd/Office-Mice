using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace OfficeMice.MapGeneration.Performance
{
    /// <summary>
    /// Memory usage tracking and analysis system for performance monitoring.
    /// Provides detailed memory allocation tracking, pattern analysis, and leak detection.
    /// </summary>
    public class MemoryTracker
    {
        private struct MemorySnapshot
        {
            public long TotalMemory;
            public long UsedMemory;
            public long GcMemory;
            public int Gen0Collections;
            public int Gen1Collections;
            public int Gen2Collections;
            public DateTime Timestamp;
            public string Tag;
        }

        private readonly List<MemorySnapshot> _snapshots = new();
        private readonly Dictionary<string, List<long>> _allocationPatterns = new();
        private long _baselineMemory;
        private bool _isTracking;

        public void StartTracking(string tag = "default")
        {
            if (_isTracking)
            {
                Debug.LogWarning("Memory tracking is already active");
                return;
            }

            _isTracking = true;
            _baselineMemory = GC.GetTotalMemory(true);
            TakeSnapshot($"start_{tag}");
            
            Debug.Log($"🧠 Memory tracking started with tag '{tag}', baseline: {_baselineMemory / (1024 * 1024):F2}MB");
        }

        public void StopTracking(string tag = "default")
        {
            if (!_isTracking)
            {
                Debug.LogWarning("Memory tracking is not active");
                return;
            }

            TakeSnapshot($"end_{tag}");
            _isTracking = false;
            
            Debug.Log($"🧠 Memory tracking stopped for tag '{tag}'");
        }

        public void TakeSnapshot(string tag)
        {
            if (!_isTracking && !tag.StartsWith("start_") && !tag.StartsWith("end_"))
            {
                Debug.LogWarning("Cannot take snapshot: tracking is not active");
                return;
            }

            var snapshot = new MemorySnapshot
            {
                TotalMemory = GC.GetTotalMemory(false),
                UsedMemory = Process.GetCurrentProcess().WorkingSet64,
                GcMemory = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                Timestamp = DateTime.UtcNow,
                Tag = tag
            };

            _snapshots.Add(snapshot);

            if (!_allocationPatterns.ContainsKey(tag))
                _allocationPatterns[tag] = new List<long>();
            
            _allocationPatterns[tag].Add(snapshot.GcMemory);
        }

        public MemoryAnalysisResult AnalyzeMemoryUsage()
        {
            if (_snapshots.Count < 2)
                return new MemoryAnalysisResult { IsValid = false, ErrorMessage = "Insufficient snapshots for analysis" };

            var first = _snapshots[0];
            var last = _snapshots[_snapshots.Count - 1];
            var memoryDelta = last.GcMemory - first.GcMemory;
            var totalAllocations = 0L;
            var maxMemory = 0L;
            var minMemory = long.MaxValue;

            foreach (var snapshot in _snapshots)
            {
                totalAllocations += snapshot.GcMemory;
                maxMemory = Math.Max(maxMemory, snapshot.GcMemory);
                minMemory = Math.Min(minMemory, snapshot.GcMemory);
            }

            var averageMemory = totalAllocations / (double)_snapshots.Count;
            var memoryVariance = 0.0;
            var gcPressure = CalculateGcPressure();

            foreach (var snapshot in _snapshots)
            {
                var diff = snapshot.GcMemory - averageMemory;
                memoryVariance += diff * diff;
            }
            memoryVariance /= _snapshots.Count;

            return new MemoryAnalysisResult
            {
                IsValid = true,
                BaselineMemory = _baselineMemory,
                FinalMemory = last.GcMemory,
                MemoryDelta = memoryDelta,
                AverageMemory = averageMemory,
                MaxMemory = maxMemory,
                MinMemory = minMemory,
                MemoryVariance = memoryVariance,
                MemoryStandardDeviation = Math.Sqrt(memoryVariance),
                TotalGcCollections = last.Gen0Collections + last.Gen1Collections + last.Gen2Collections,
                Gen0Collections = last.Gen0Collections - first.Gen0Collections,
                Gen1Collections = last.Gen1Collections - first.Gen1Collections,
                Gen2Collections = last.Gen2Collections - first.Gen2Collections,
                GcPressure = gcPressure,
                SnapshotCount = _snapshots.Count,
                Duration = last.Timestamp - first.Timestamp,
                AllocationPatterns = AnalyzeAllocationPatterns()
            };
        }

        private double CalculateGcPressure()
        {
            if (_snapshots.Count < 2) return 0;

            var totalPressure = 0.0;
            for (int i = 1; i < _snapshots.Count; i++)
            {
                var pressure = Math.Abs(_snapshots[i].GcMemory - _snapshots[i - 1].GcMemory);
                totalPressure += pressure;
            }

            return totalPressure / (_snapshots.Count - 1);
        }

        private Dictionary<string, AllocationPattern> AnalyzeAllocationPatterns()
        {
            var patterns = new Dictionary<string, AllocationPattern>();

            foreach (var kvp in _allocationPatterns)
            {
                var tag = kvp.Key;
                var allocations = kvp.Value;

                if (allocations.Count < 2) continue;

                var total = 0L;
                var max = long.MinValue;
                var min = long.MaxValue;

                foreach (var allocation in allocations)
                {
                    total += allocation;
                    max = Math.Max(max, allocation);
                    min = Math.Min(min, allocation);
                }

                var average = total / (double)allocations.Count;
                var variance = 0.0;

                foreach (var allocation in allocations)
                {
                    var diff = allocation - average;
                    variance += diff * diff;
                }
                variance /= allocations.Count;

                patterns[tag] = new AllocationPattern
                {
                    Tag = tag,
                    AverageAllocation = average,
                    MaxAllocation = max,
                    MinAllocation = min,
                    StandardDeviation = Math.Sqrt(variance),
                    SampleCount = allocations.Count,
                    IsStable = Math.Sqrt(variance) < average * 0.1 // 10% variance threshold
                };
            }

            return patterns;
        }

        public void LogMemoryAnalysis()
        {
            var analysis = AnalyzeMemoryUsage();
            
            if (!analysis.IsValid)
            {
                Debug.LogError($"❌ Memory analysis failed: {analysis.ErrorMessage}");
                return;
            }

            Debug.Log($"📊 Memory Usage Analysis:");
            Debug.Log($"   Baseline: {analysis.BaselineMemory / (1024 * 1024):F2}MB");
            Debug.Log($"   Final: {analysis.FinalMemory / (1024 * 1024):F2}MB");
            Debug.Log($"   Delta: {analysis.MemoryDelta / (1024 * 1024):F2}MB");
            Debug.Log($"   Average: {analysis.AverageMemory / (1024 * 1024):F2}MB");
            Debug.Log($"   Max: {analysis.MaxMemory / (1024 * 1024):F2}MB");
            Debug.Log($"   Min: {analysis.MinMemory / (1024 * 1024):F2}MB");
            Debug.Log($"   Std Dev: {analysis.MemoryStandardDeviation / (1024 * 1024):F2}MB");
            Debug.Log($"   GC Pressure: {analysis.GcPressure / 1024:F2}KB");
            Debug.Log($"   GC Collections: Gen0={analysis.Gen0Collections}, Gen1={analysis.Gen1Collections}, Gen2={analysis.Gen2Collections}");
            Debug.Log($"   Duration: {analysis.Duration.TotalSeconds:F2}s");
            Debug.Log($"   Snapshots: {analysis.SnapshotCount}");

            Debug.Log($"📈 Allocation Patterns:");
            foreach (var pattern in analysis.AllocationPatterns.Values)
            {
                Debug.Log($"   {pattern.Tag}: {pattern.AverageAllocation / (1024 * 1024):F2}MB avg, " +
                         $"{(pattern.IsStable ? "✅ Stable" : "⚠️ Unstable")} " +
                         $"(σ={pattern.StandardDeviation / (1024 * 1024):F2}MB)");
            }
        }

        public void SaveMemoryReport(string testName)
        {
            var analysis = AnalyzeMemoryUsage();
            if (!analysis.IsValid) return;

            var reportPath = Path.Combine(Application.persistentDataPath, "MemoryReports");
            Directory.CreateDirectory(reportPath);

            var fileName = $"{testName}_memory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportPath, fileName);

            var report = new
            {
                TestName = testName,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Analysis = analysis
            };

            File.WriteAllText(filePath, JsonUtility.ToJson(report, true));
            Debug.Log($"💾 Memory report saved to: {filePath}");
        }

        public void Reset()
        {
            _snapshots.Clear();
            _allocationPatterns.Clear();
            _baselineMemory = 0;
            _isTracking = false;
        }
    }

    public struct MemoryAnalysisResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public long BaselineMemory;
        public long FinalMemory;
        public long MemoryDelta;
        public double AverageMemory;
        public long MaxMemory;
        public long MinMemory;
        public double MemoryVariance;
        public double MemoryStandardDeviation;
        public int TotalGcCollections;
        public int Gen0Collections;
        public int Gen1Collections;
        public int Gen2Collections;
        public double GcPressure;
        public int SnapshotCount;
        public TimeSpan Duration;
        public Dictionary<string, AllocationPattern> AllocationPatterns;
    }

    public struct AllocationPattern
    {
        public string Tag;
        public double AverageAllocation;
        public long MaxAllocation;
        public long MinAllocation;
        public double StandardDeviation;
        public int SampleCount;
        public bool IsStable;
    }
}