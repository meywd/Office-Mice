using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OfficeMice.MapGeneration.Performance
{
    /// <summary>
    /// Garbage Collection pressure monitoring system for tracking allocation patterns and GC performance.
    /// Ensures GC pressure stays within acceptable thresholds and provides detailed analysis.
    /// </summary>
    public class GcPressureMonitor
    {
        private struct GcSnapshot
        {
            public int Gen0Collections;
            public int Gen1Collections;
            public int Gen2Collections;
            public long TotalMemory;
            public long GcMemory;
            public DateTime Timestamp;
            public string Tag;
            public double FrameTimeMs;
        }

        private readonly List<GcSnapshot> _snapshots = new();
        private readonly Dictionary<string, List<double>> _frameGcPressure = new();
        private GcSnapshot _baseline;
        private bool _isMonitoring;
        private double _maxAcceptableGcPressureKb = 500.0; // 500KB per frame
        private DateTime _lastFrameTime;

        public double MaxAcceptableGcPressureKb
        {
            get => _maxAcceptableGcPressureKb;
            set => _maxAcceptableGcPressureKb = Math.Max(0, value);
        }

        public void StartMonitoring(string tag = "default")
        {
            if (_isMonitoring)
            {
                Debug.LogWarning("GC pressure monitoring is already active");
                return;
            }

            _isMonitoring = true;
            _baseline = TakeSnapshot($"start_{tag}");
            _lastFrameTime = DateTime.UtcNow;
            
            Debug.Log($"🗑️ GC pressure monitoring started with tag '{tag}', max acceptable: {_maxAcceptableGcPressureKb:F2}KB/frame");
        }

        public void StopMonitoring(string tag = "default")
        {
            if (!_isMonitoring)
            {
                Debug.LogWarning("GC pressure monitoring is not active");
                return;
            }

            TakeSnapshot($"end_{tag}");
            _isMonitoring = false;
            
            Debug.Log($"🗑️ GC pressure monitoring stopped for tag '{tag}'");
        }

        public void RecordFrameGcPressure(string tag = "frame")
        {
            if (!_isMonitoring)
            {
                Debug.LogWarning("Cannot record frame GC pressure: monitoring is not active");
                return;
            }

            var now = DateTime.UtcNow;
            var frameTime = (now - _lastFrameTime).TotalMilliseconds;
            _lastFrameTime = now;

            var snapshot = TakeSnapshot(tag);
            snapshot.FrameTimeMs = frameTime;
            
            // Calculate GC pressure for this frame
            var gcPressure = CalculateFrameGcPressure(snapshot);
            
            if (!_frameGcPressure.ContainsKey(tag))
                _frameGcPressure[tag] = new List<double>();
            
            _frameGcPressure[tag].Add(gcPressure);

            // Check if GC pressure exceeds threshold
            if (gcPressure > _maxAcceptableGcPressureKb * 1024)
            {
                Debug.LogWarning($"⚠️ High GC pressure detected: {gcPressure / 1024:F2}KB (threshold: {_maxAcceptableGcPressureKb:F2}KB)");
            }
        }

        private GcSnapshot TakeSnapshot(string tag)
        {
            var snapshot = new GcSnapshot
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemory = GC.GetTotalMemory(false),
                GcMemory = GC.GetTotalMemory(false),
                Timestamp = DateTime.UtcNow,
                Tag = tag,
                FrameTimeMs = 0
            };

            _snapshots.Add(snapshot);
            return snapshot;
        }

        private double CalculateFrameGcPressure(GcSnapshot current)
        {
            if (_snapshots.Count < 2) return 0;

            var previous = _snapshots[_snapshots.Count - 2];
            var memoryDelta = Math.Abs(current.GcMemory - previous.GcMemory);
            var collectionDelta = (current.Gen0Collections - previous.Gen0Collections) +
                                (current.Gen1Collections - previous.Gen1Collections) +
                                (current.Gen2Collections - previous.Gen2Collections);

            // GC pressure is combination of memory allocation and collection frequency
            return memoryDelta + (collectionDelta * 1024); // Weight collections more heavily
        }

        public GcPressureAnalysis AnalyzeGcPressure()
        {
            if (_snapshots.Count < 2)
                return new GcPressureAnalysis { IsValid = false, ErrorMessage = "Insufficient snapshots for analysis" };

            var first = _baseline;
            var last = _snapshots[_snapshots.Count - 1];
            
            var totalGcPressure = 0.0;
            var maxGcPressure = 0.0;
            var minGcPressure = double.MaxValue;
            var frameCount = 0;

            // Analyze frame-by-frame GC pressure
            foreach (var kvp in _frameGcPressure)
            {
                foreach (var pressure in kvp.Value)
                {
                    totalGcPressure += pressure;
                    maxGcPressure = Math.Max(maxGcPressure, pressure);
                    minGcPressure = Math.Min(minGcPressure, pressure);
                    frameCount++;
                }
            }

            var averageGcPressure = frameCount > 0 ? totalGcPressure / frameCount : 0;
            var variance = 0.0;

            if (frameCount > 0)
            {
                foreach (var kvp in _frameGcPressure)
                {
                    foreach (var pressure in kvp.Value)
                    {
                        var diff = pressure - averageGcPressure;
                        variance += diff * diff;
                    }
                }
                variance /= frameCount;
            }

            // Calculate GC efficiency
            var totalCollections = (last.Gen0Collections - first.Gen0Collections) +
                                 (last.Gen1Collections - first.Gen1Collections) +
                                 (last.Gen2Collections - first.Gen2Collections);
            
            var memoryReclaimed = first.GcMemory - last.GcMemory + totalGcPressure;
            var gcEfficiency = totalCollections > 0 ? memoryReclaimed / (double)totalCollections : 0;

            return new GcPressureAnalysis
            {
                IsValid = true,
                AverageGcPressure = averageGcPressure,
                MaxGcPressure = maxGcPressure,
                MinGcPressure = minGcPressure == double.MaxValue ? 0 : minGcPressure,
                GcPressureStandardDeviation = Math.Sqrt(variance),
                TotalGcPressure = totalGcPressure,
                FrameCount = frameCount,
                Gen0Collections = last.Gen0Collections - first.Gen0Collections,
                Gen1Collections = last.Gen1Collections - first.Gen1Collections,
                Gen2Collections = last.Gen2Collections - first.Gen2Collections,
                TotalCollections = totalCollections,
                GcEfficiency = gcEfficiency,
                MaxAcceptableGcPressure = _maxAcceptableGcPressureKb * 1024,
                ExceedsThreshold = maxGcPressure > _maxAcceptableGcPressureKb * 1024,
                ThresholdViolationCount = CountThresholdViolations(),
                Duration = last.Timestamp - first.Timestamp,
                FrameGcPressureData = new Dictionary<string, List<double>>(_frameGcPressure)
            };
        }

        private int CountThresholdViolations()
        {
            var threshold = _maxAcceptableGcPressureKb * 1024;
            var violations = 0;

            foreach (var kvp in _frameGcPressure)
            {
                foreach (var pressure in kvp.Value)
                {
                    if (pressure > threshold)
                        violations++;
                }
            }

            return violations;
        }

        public void LogGcPressureAnalysis()
        {
            var analysis = AnalyzeGcPressure();
            
            if (!analysis.IsValid)
            {
                Debug.LogError($"❌ GC pressure analysis failed: {analysis.ErrorMessage}");
                return;
            }

            Debug.Log($"🗑️ GC Pressure Analysis:");
            Debug.Log($"   Average: {analysis.AverageGcPressure / 1024:F2}KB/frame");
            Debug.Log($"   Max: {analysis.MaxGcPressure / 1024:F2}KB/frame");
            Debug.Log($"   Min: {analysis.MinGcPressure / 1024:F2}KB/frame");
            Debug.Log($"   Std Dev: {analysis.GcPressureStandardDeviation / 1024:F2}KB/frame");
            Debug.Log($"   Total: {analysis.TotalGcPressure / (1024 * 1024):F2}MB");
            Debug.Log($"   Frame Count: {analysis.FrameCount}");
            Debug.Log($"   Threshold: {_maxAcceptableGcPressureKb:F2}KB/frame");
            Debug.Log($"   Violations: {analysis.ThresholdViolationCount} ({(double)analysis.ThresholdViolationCount / analysis.FrameCount:P2})");
            Debug.Log($"   GC Collections: Gen0={analysis.Gen0Collections}, Gen1={analysis.Gen1Collections}, Gen2={analysis.Gen2Collections}");
            Debug.Log($"   GC Efficiency: {analysis.GcEfficiency / 1024:F2}KB/collection");
            Debug.Log($"   Duration: {analysis.Duration.TotalSeconds:F2}s");
            Debug.Log($"   Status: {(analysis.ExceedsThreshold ? "⚠️ Exceeds threshold" : "✅ Within threshold")}");

            // Frame-by-frame analysis
            Debug.Log($"📊 Frame GC Pressure Breakdown:");
            foreach (var kvp in analysis.FrameGcPressureData)
            {
                var frameData = kvp.Value;
                if (frameData.Count == 0) continue;

                var avg = frameData.Count > 0 ? frameData.ToArray().Sum() / frameData.Count : 0;
                var max = frameData.Count > 0 ? frameData.Max() : 0;
                var violations = frameData.Count(p => p > _maxAcceptableGcPressureKb * 1024);

                Debug.Log($"   {kvp.Key}: {avg / 1024:F2}KB avg, {max / 1024:F2}KB max, {violations} violations ({frameData.Count} frames)");
            }
        }

        public void SaveGcPressureReport(string testName)
        {
            var analysis = AnalyzeGcPressure();
            if (!analysis.IsValid) return;

            var reportPath = Path.Combine(Application.persistentDataPath, "GcPressureReports");
            Directory.CreateDirectory(reportPath);

            var fileName = $"{testName}_gc_pressure_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportPath, fileName);

            var report = new
            {
                TestName = testName,
                Timestamp = DateTime.UtcNow.ToString("O"),
                MaxAcceptableGcPressureKb = _maxAcceptableGcPressureKb,
                Analysis = analysis
            };

            File.WriteAllText(filePath, JsonUtility.ToJson(report, true));
            Debug.Log($"💾 GC pressure report saved to: {filePath}");
        }

        public bool ValidateGcPressure()
        {
            var analysis = AnalyzeGcPressure();
            if (!analysis.IsValid) return false;

            // GC pressure is valid if:
            // 1. Average GC pressure is within threshold
            // 2. Less than 10% of frames exceed threshold
            // 3. No Gen2 collections (indicates memory leaks)
            // 4. GC efficiency is reasonable

            var avgWithinThreshold = analysis.AverageGcPressure <= _maxAcceptableGcPressureKb * 1024;
            var violationRate = (double)analysis.ThresholdViolationCount / analysis.FrameCount;
            var acceptableViolationRate = violationRate < 0.1; // Less than 10% violations
            var noGen2Collections = analysis.Gen2Collections == 0;
            var reasonableEfficiency = analysis.GcEfficiency > 1024; // At least 1KB per collection

            Debug.Log($"✅ GC Pressure Validation:");
            Debug.Log($"   Average within threshold: {(avgWithinThreshold ? "✅" : "❌")}");
            Debug.Log($"   Acceptable violation rate: {(acceptableViolationRate ? "✅" : "❌")} ({violationRate:P2})");
            Debug.Log($"   No Gen2 collections: {(noGen2Collections ? "✅" : "❌")}");
            Debug.Log($"   Reasonable efficiency: {(reasonableEfficiency ? "✅" : "❌")} ({analysis.GcEfficiency / 1024:F2}KB/collection)");

            return avgWithinThreshold && acceptableViolationRate && noGen2Collections && reasonableEfficiency;
        }

        public void Reset()
        {
            _snapshots.Clear();
            _frameGcPressure.Clear();
            _baseline = default;
            _isMonitoring = false;
            _lastFrameTime = DateTime.UtcNow;
        }
    }

    public struct GcPressureAnalysis
    {
        public bool IsValid;
        public string ErrorMessage;
        public double AverageGcPressure;
        public double MaxGcPressure;
        public double MinGcPressure;
        public double GcPressureStandardDeviation;
        public double TotalGcPressure;
        public int FrameCount;
        public int Gen0Collections;
        public int Gen1Collections;
        public int Gen2Collections;
        public int TotalCollections;
        public double GcEfficiency;
        public double MaxAcceptableGcPressure;
        public bool ExceedsThreshold;
        public int ThresholdViolationCount;
        public TimeSpan Duration;
        public Dictionary<string, List<double>> FrameGcPressureData;
    }
}