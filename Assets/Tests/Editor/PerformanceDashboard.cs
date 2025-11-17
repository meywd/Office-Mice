using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace OfficeMice.MapGeneration.Performance
{
    /// <summary>
    /// Performance reporting dashboard for Unity Editor.
    /// Provides real-time performance monitoring, historical data analysis, and visualization.
    /// </summary>
    public class PerformanceDashboard : EditorWindow
    {
        private struct PerformanceReport
        {
            public string TestName;
            public DateTime Timestamp;
            public double AverageTimeMs;
            public long MemoryUsedBytes;
            public double GcPressureBytes;
            public double StandardDeviationMs;
            public string Status;
            public int TestIterations;
        }

        private struct DashboardMetrics
        {
            public int TotalTests;
            public int PassedTests;
            public int FailedTests;
            public double PassRate;
            public double AverageExecutionTime;
            public double TotalMemoryUsage;
            public double AverageGcPressure;
            public DateTime LastUpdated;
        }

        private Vector2 _scrollPosition;
        private Dictionary<string, List<PerformanceReport>> _performanceHistory = new();
        private DashboardMetrics _currentMetrics;
        private bool _autoRefresh = false;
        private double _refreshInterval = 5.0; // seconds
        private double _lastRefreshTime;
        private bool _showDetails = false;
        private string _selectedTest = "";

        [MenuItem("Tools/Performance Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<PerformanceDashboard>("Performance Dashboard");
        }

        private void OnEnable()
        {
            LoadPerformanceData();
            RefreshMetrics();
        }

        private void OnDisable()
        {
            _autoRefresh = false;
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                RefreshMetrics();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawMetrics();
            DrawControls();
            DrawTestResults();
            DrawDetails();

            if (_autoRefresh)
            {
                Repaint();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            
            GUILayout.Label("🚀 Performance Dashboard", headerStyle);
            GUILayout.Label($"Last Updated: {_currentMetrics.LastUpdated:yyyy-MM-dd HH:mm:ss}", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
        }

        private void DrawMetrics()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("📊 Current Metrics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Test Results
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Test Results", EditorStyles.boldLabel);
            GUILayout.Label($"Total: {_currentMetrics.TotalTests}", EditorStyles.miniLabel);
            GUILayout.Label($"Passed: {_currentMetrics.PassedTests}", EditorStyles.miniLabel);
            GUILayout.Label($"Failed: {_currentMetrics.FailedTests}", EditorStyles.miniLabel);
            GUILayout.Label($"Pass Rate: {_currentMetrics.PassRate:P1}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // Performance
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Performance", EditorStyles.boldLabel);
            GUILayout.Label($"Avg Time: {_currentMetrics.AverageExecutionTime:F2}ms", EditorStyles.miniLabel);
            GUILayout.Label($"Total Memory: {_currentMetrics.TotalMemoryUsage / (1024 * 1024):F1}MB", EditorStyles.miniLabel);
            GUILayout.Label($"Avg GC Pressure: {_currentMetrics.AverageGcPressure / 1024:F1}KB", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 Refresh"))
            {
                RefreshMetrics();
            }
            
            if (GUILayout.Button("📁 Load Data"))
            {
                LoadPerformanceData();
            }
            
            if (GUILayout.Button("💾 Save Report"))
            {
                SavePerformanceReport();
            }
            
            if (GUILayout.Button("🗑️ Clear Data"))
            {
                ClearPerformanceData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);
            if (_autoRefresh)
            {
                _refreshInterval = EditorGUILayout.Slider("Interval (s)", _refreshInterval, 1.0, 60.0);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawTestResults()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("📋 Test Results", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            foreach (var kvp in _performanceHistory.OrderByDescending(x => x.Value.LastOrDefault().Timestamp))
            {
                var testName = kvp.Key;
                var reports = kvp.Value;
                var latest = reports.LastOrDefault();
                
                if (latest.TestName == null) continue;
                
                EditorGUILayout.BeginHorizontal("box");
                
                // Status indicator
                var statusColor = latest.Status == "PASS" ? Color.green : Color.red;
                var originalColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(latest.Status == "PASS" ? "✅" : "❌", GUILayout.Width(20));
                GUI.color = originalColor;
                
                // Test name
                if (GUILayout.Button(testName, EditorStyles.linkLabel))
                {
                    _selectedTest = testName;
                    _showDetails = true;
                }
                
                // Metrics
                GUILayout.Label($"{latest.AverageTimeMs:F1}ms", GUILayout.Width(60));
                GUILayout.Label($"{latest.MemoryUsedBytes / (1024 * 1024):F1}MB", GUILayout.Width(60));
                GUILayout.Label($"{latest.GcPressureBytes / 1024:F1}KB", GUILayout.Width(60));
                GUILayout.Label(latest.Timestamp.ToString("HH:mm:ss"), EditorStyles.miniLabel, GUILayout.Width(60));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }

        private void DrawDetails()
        {
            if (!_showDetails || string.IsNullOrEmpty(_selectedTest) || !_performanceHistory.ContainsKey(_selectedTest))
                return;
            
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label($"📈 Details: {_selectedTest}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("✖️ Close", GUILayout.Width(60)))
            {
                _showDetails = false;
                _selectedTest = "";
                return;
            }
            
            var reports = _performanceHistory[_selectedTest];
            if (reports.Count == 0)
            {
                GUILayout.Label("No data available", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            
            // Statistics
            var times = reports.Select(r => r.AverageTimeMs).ToList();
            var memories = reports.Select(r => r.MemoryUsedBytes).ToList();
            var gcPressures = reports.Select(r => r.GcPressureBytes).ToList();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Time Statistics", EditorStyles.boldLabel);
            GUILayout.Label($"Average: {times.Average():F2}ms", EditorStyles.miniLabel);
            GUILayout.Label($"Min: {times.Min():F2}ms", EditorStyles.miniLabel);
            GUILayout.Label($"Max: {times.Max():F2}ms", EditorStyles.miniLabel);
            GUILayout.Label($"Std Dev: {CalculateStandardDeviation(times):F2}ms", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Memory Statistics", EditorStyles.boldLabel);
            GUILayout.Label($"Average: {memories.Average() / (1024 * 1024):F2}MB", EditorStyles.miniLabel);
            GUILayout.Label($"Min: {memories.Min() / (1024 * 1024):F2}MB", EditorStyles.miniLabel);
            GUILayout.Label($"Max: {memories.Max() / (1024 * 1024):F2}MB", EditorStyles.miniLabel);
            GUILayout.Label($"Std Dev: {CalculateStandardDeviation(memories) / (1024 * 1024):F2}MB", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label("GC Pressure Statistics", EditorStyles.boldLabel);
            GUILayout.Label($"Average: {gcPressures.Average() / 1024:F2}KB", EditorStyles.miniLabel);
            GUILayout.Label($"Min: {gcPressures.Min() / 1024:F2}KB", EditorStyles.miniLabel);
            GUILayout.Label($"Max: {gcPressures.Max() / 1024:F2}KB", EditorStyles.miniLabel);
            GUILayout.Label($"Std Dev: {CalculateStandardDeviation(gcPressures) / 1024:F2}KB", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            // Recent history
            GUILayout.Label("Recent History", EditorStyles.boldLabel);
            var recentReports = reports.TakeLast(10).Reverse().ToList();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Timestamp", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            GUILayout.Label("Time (ms)", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            GUILayout.Label("Memory (MB)", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            GUILayout.Label("GC (KB)", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            GUILayout.Label("Status", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            foreach (var report in recentReports)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(report.Timestamp.ToString("HH:mm:ss"), EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.Label($"{report.AverageTimeMs:F1}", EditorStyles.miniLabel, GUILayout.Width(60));
                GUILayout.Label($"{report.MemoryUsedBytes / (1024 * 1024):F1}", EditorStyles.miniLabel, GUILayout.Width(70));
                GUILayout.Label($"{report.GcPressureBytes / 1024:F1}", EditorStyles.miniLabel, GUILayout.Width(60));
                
                var statusColor = report.Status == "PASS" ? Color.green : Color.red;
                GUI.color = statusColor;
                GUILayout.Label(report.Status, EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void RefreshMetrics()
        {
            LoadPerformanceData();
            
            var allReports = _performanceHistory.SelectMany(kvp => kvp.Value).ToList();
            
            _currentMetrics = new DashboardMetrics
            {
                TotalTests = allReports.Count,
                PassedTests = allReports.Count(r => r.Status == "PASS"),
                FailedTests = allReports.Count(r => r.Status == "FAIL"),
                PassRate = allReports.Count > 0 ? (double)allReports.Count(r => r.Status == "PASS") / allReports.Count : 0,
                AverageExecutionTime = allReports.Count > 0 ? allReports.Average(r => r.AverageTimeMs) : 0,
                TotalMemoryUsage = allReports.Count > 0 ? allReports.Sum(r => r.MemoryUsedBytes) : 0,
                AverageGcPressure = allReports.Count > 0 ? allReports.Average(r => r.GcPressureBytes) : 0,
                LastUpdated = DateTime.Now
            };
        }

        private void LoadPerformanceData()
        {
            _performanceHistory.Clear();
            
            // Load from various sources
            LoadFromPerformanceBaselines();
            LoadFromMemoryReports();
            LoadFromGcPressureReports();
            LoadFromTestResults();
        }

        private void LoadFromPerformanceBaselines()
        {
            var basePath = Path.Combine(Application.persistentDataPath, "PerformanceBaselines");
            if (!Directory.Exists(basePath)) return;
            
            foreach (var file in Directory.GetFiles(basePath, "*_baseline.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonUtility.FromJson<BaselineData>(json);
                    
                    var report = new PerformanceReport
                    {
                        TestName = data.TestName,
                        Timestamp = DateTime.Parse(data.Timestamp),
                        AverageTimeMs = data.AverageTimeMs,
                        MemoryUsedBytes = data.MemoryUsedBytes,
                        GcPressureBytes = data.GcPressureBytes,
                        StandardDeviationMs = data.StandardDeviationMs,
                        Status = "PASS", // Baselines are considered passing
                        TestIterations = data.TestIterations
                    };
                    
                    AddReport(report);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load baseline from {file}: {ex.Message}");
                }
            }
        }

        private void LoadFromMemoryReports()
        {
            var basePath = Path.Combine(Application.persistentDataPath, "MemoryReports");
            if (!Directory.Exists(basePath)) return;
            
            foreach (var file in Directory.GetFiles(basePath, "*_memory_*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    // Parse memory report and convert to PerformanceReport
                    // This is a simplified version - in practice, you'd parse the full report structure
                    var report = new PerformanceReport
                    {
                        TestName = Path.GetFileNameWithoutExtension(file).Split('_')[0],
                        Timestamp = File.GetCreationTime(file),
                        Status = "PASS"
                    };
                    
                    AddReport(report);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load memory report from {file}: {ex.Message}");
                }
            }
        }

        private void LoadFromGcPressureReports()
        {
            var basePath = Path.Combine(Application.persistentDataPath, "GcPressureReports");
            if (!Directory.Exists(basePath)) return;
            
            foreach (var file in Directory.GetFiles(basePath, "*_gc_pressure_*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    // Parse GC pressure report and convert to PerformanceReport
                    var report = new PerformanceReport
                    {
                        TestName = Path.GetFileNameWithoutExtension(file).Split('_')[0],
                        Timestamp = File.GetCreationTime(file),
                        Status = "PASS"
                    };
                    
                    AddReport(report);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load GC pressure report from {file}: {ex.Message}");
                }
            }
        }

        private void LoadFromTestResults()
        {
            // Load from Unity test results if available
            // This would parse XML test results and extract performance data
        }

        private void AddReport(PerformanceReport report)
        {
            if (!_performanceHistory.ContainsKey(report.TestName))
                _performanceHistory[report.TestName] = new List<PerformanceReport>();
            
            _performanceHistory[report.TestName].Add(report);
            
            // Keep only last 50 reports per test to avoid memory issues
            if (_performanceHistory[report.TestName].Count > 50)
                _performanceHistory[report.TestName].RemoveAt(0);
        }

        private void SavePerformanceReport()
        {
            var reportPath = Path.Combine(Application.persistentDataPath, "PerformanceReports");
            Directory.CreateDirectory(reportPath);
            
            var fileName = $"dashboard_report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportPath, fileName);
            
            var report = new
            {
                Timestamp = DateTime.Now.ToString("O"),
                Metrics = _currentMetrics,
                PerformanceHistory = _performanceHistory.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.TakeLast(20).ToList() // Last 20 reports per test
                )
            };
            
            File.WriteAllText(filePath, JsonUtility.ToJson(report, true));
            Debug.Log($"💾 Performance dashboard report saved to: {filePath}");
            
            EditorUtility.RevealInFinder(filePath);
        }

        private void ClearPerformanceData()
        {
            if (EditorUtility.DisplayDialog("Clear Data", "Are you sure you want to clear all performance data?", "Yes", "No"))
            {
                _performanceHistory.Clear();
                _currentMetrics = new DashboardMetrics();
                _selectedTest = "";
                _showDetails = false;
                Debug.Log("🗑️ Performance data cleared");
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0) return 0;
            
            var mean = valuesList.Average();
            var sumOfSquares = valuesList.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumOfSquares / valuesList.Count);
        }

        private double CalculateStandardDeviation(IEnumerable<long> values)
        {
            return CalculateStandardDeviation(values.Select(v => (double)v));
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
    }
}