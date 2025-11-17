# Performance Targets and Thresholds

This document defines the performance targets, thresholds, and monitoring strategies for the Office Mice map generation system.

## Overview

The performance benchmarking infrastructure ensures that the map generation system meets strict performance requirements while maintaining code quality and preventing regressions.

## Performance Targets

### Primary Performance Requirements

| Metric | Target | Acceptance Criteria | Measurement Method |
|--------|--------|-------------------|-------------------|
| **Map Generation Time** | < 3 seconds | 100-room maps must generate in under 3 seconds | Unity Performance Testing API |
| **Gameplay Performance** | 60 FPS | Maintain 60 FPS during map generation | Frame time budgeting |
| **Memory Usage** | < 200MB | Runtime memory usage must stay under 200MB | Memory tracking system |
| **GC Pressure** | < 500KB/frame | Garbage collection pressure per frame | GC pressure monitoring |
| **Loading Time** | < 5 seconds | Complete map loading with progress bar | Async generation timing |

### Component-Level Performance Targets

| Component | Max Time | Max Memory | Max GC Pressure | Notes |
|-----------|----------|------------|------------------|-------|
| **Map Generation** | 3000ms | 200MB | 500KB | Complete map pipeline |
| **Room Generation** | 500ms | 50MB | 100KB | BSP algorithm |
| **Corridor Generation** | 200ms | 30MB | 50KB | Pathfinding-based |
| **Pathfinding** | 50ms | 10MB | 10KB | A* algorithm |
| **Content Population** | 300ms | 40MB | 80KB | Asset placement |
| **Tile Rendering** | 400ms | 60MB | 100KB | Visual output |
| **Asset Loading** | 100ms | 20MB | 20KB | Resource management |

## Performance Thresholds

### Regression Detection Thresholds

| Test Category | Time Tolerance | Memory Tolerance | GC Pressure Tolerance | Absolute Thresholds |
|---------------|----------------|------------------|----------------------|-------------------|
| **Map Generation** | ±10% | ±15% | ±20% | ±100ms, ±10MB |
| **Room Generation** | ±15% | ±20% | ±25% | ±50ms, ±5MB |
| **Corridor Generation** | ±20% | ±25% | ±30% | ±25ms, ±3MB |
| **Pathfinding** | ±25% | ±30% | ±35% | ±10ms, ±2MB |

### Scaling Requirements

| Map Size | Expected Time | Expected Memory | Scaling Factor |
|----------|---------------|----------------|----------------|
| **25x25** | ~250ms | ~25MB | Baseline |
| **50x50** | ~500ms | ~50MB | ~2x |
| **100x100** | ~1000ms | ~100MB | ~4x |
| **200x200** | ~2000ms | ~200MB | ~8x |

*Scaling should be roughly linear with some tolerance for non-linear factors.*

## Monitoring and Validation

### Automated Performance Tests

#### Test Categories

1. **Baseline Tests**: Establish performance baselines for reference
2. **Regression Tests**: Detect performance regressions against baselines
3. **Scaling Tests**: Validate linear scaling across map sizes
4. **Memory Tests**: Monitor memory usage and detect leaks
5. **GC Pressure Tests**: Ensure garbage collection stays within limits
6. **Frame Time Tests**: Validate frame time budgeting

#### Test Execution

```bash
# Run all performance tests
Unity -runTests -testCategory "Performance"

# Run specific test categories
Unity -runTests -testCategory "Performance,Baseline"
Unity -runTests -testCategory "Performance,Regression"
Unity -runTests -testCategory "Performance,Memory"
```

### Continuous Integration

#### CI/CD Pipeline Integration

The performance testing is integrated into the GitHub Actions workflow:

1. **Performance Tests Job**: Runs all performance tests
2. **Performance Validation Job**: Validates results against thresholds
3. **Performance Analysis Job**: Generates trend reports
4. **Artifact Collection**: Stores performance reports and trends

#### Failure Conditions

The CI/CD pipeline fails if:

- Performance test failure rate exceeds 5%
- Any critical performance test fails
- Performance regression exceeds tolerance thresholds
- Memory leaks are detected
- GC pressure consistently exceeds limits

### Performance Dashboard

#### Real-time Monitoring

The Unity Editor Performance Dashboard provides:

- **Current Metrics**: Live performance statistics
- **Test Results**: Pass/fail status for all tests
- **Historical Data**: Performance trends over time
- **Detailed Analysis**: Per-test statistics and breakdowns

#### Dashboard Features

- Auto-refresh capability
- Interactive test result exploration
- Statistical analysis (mean, std dev, min/max)
- Performance trend visualization
- Export functionality for reports

## Performance Optimization Strategies

### Memory Management

1. **Object Pooling**: Reuse objects to reduce allocations
2. **Lazy Loading**: Load resources only when needed
3. **Memory Profiling**: Regular profiling to identify leaks
4. **GC Optimization**: Minimize allocations in hot paths

### Algorithm Optimization

1. **Spatial Partitioning**: Use efficient data structures
2. **Caching**: Cache expensive computations
3. **Async Processing**: Use coroutines for long operations
4. **Batch Processing**: Group similar operations

### Frame Time Budgeting

1. **Coroutine-based Generation**: Spread work across frames
2. **Progressive Loading**: Load incrementally
3. **Priority Scheduling**: Prioritize critical operations
4. **Adaptive Quality**: Adjust quality based on performance

## Performance Testing Best Practices

### Test Design

1. **Deterministic Tests**: Use fixed seeds for reproducibility
2. **Isolated Testing**: Test components independently
3. **Realistic Scenarios**: Use typical map configurations
4. **Multiple Iterations**: Run tests multiple times for accuracy

### Measurement Techniques

1. **Warmup Iterations**: Allow JIT compilation and caching
2. **Statistical Analysis**: Use mean, median, and standard deviation
3. **Baseline Comparison**: Compare against established baselines
4. **Trend Analysis**: Monitor performance over time

### Data Collection

1. **Comprehensive Metrics**: Collect time, memory, and GC data
2. **Context Information**: Record test parameters and environment
3. **Historical Data**: Maintain performance history
4. **Automated Reporting**: Generate reports automatically

## Troubleshooting Performance Issues

### Common Performance Problems

1. **Memory Leaks**: Objects not properly disposed
2. **Excessive Allocations**: Too many temporary objects
3. **Inefficient Algorithms**: Poor algorithmic complexity
4. **Blocking Operations**: Synchronous operations on main thread

### Diagnostic Tools

1. **Unity Profiler**: Built-in profiling tools
2. **Memory Tracker**: Custom memory monitoring
3. **GC Pressure Monitor**: Garbage collection analysis
4. **Performance Dashboard**: Real-time monitoring

### Resolution Strategies

1. **Profile First**: Always profile before optimizing
2. **Target Hotspots**: Focus on critical paths
3. **Measure Impact**: Validate optimization effectiveness
4. **Iterative Approach**: Optimize incrementally

## Performance Reporting

### Report Types

1. **Daily Reports**: Automated daily performance summaries
2. **Regression Reports**: Detailed regression analysis
3. **Trend Reports**: Long-term performance trends
4. **Benchmark Reports**: Comprehensive benchmark results

### Report Contents

1. **Executive Summary**: High-level performance overview
2. **Detailed Metrics**: Comprehensive performance data
3. **Trend Analysis**: Performance changes over time
4. **Recommendations**: Optimization suggestions

### Distribution

1. **CI/CD Integration**: Automatic report generation
2. **Dashboard Access**: Real-time performance monitoring
3. **Email Notifications**: Critical performance alerts
4. **Documentation**: Historical performance records

## Performance Governance

### Performance Standards

1. **Code Reviews**: Performance impact assessment
2. **Acceptance Criteria**: Performance requirements in stories
3. **Definition of Done**: Performance validation required
4. **Quality Gates**: Performance thresholds must be met

### Performance Culture

1. **Performance Awareness**: Team education on performance
2. **Shared Responsibility**: Everyone owns performance
3. **Continuous Improvement**: Ongoing optimization efforts
4. **Knowledge Sharing**: Performance best practices

## Future Enhancements

### Planned Improvements

1. **Advanced Analytics**: Machine learning for performance prediction
2. **Automated Optimization**: AI-driven performance tuning
3. **Real-time Monitoring**: Production performance monitoring
4. **Cross-platform Optimization**: Platform-specific optimizations

### Research Areas

1. **New Algorithms**: Investigate more efficient algorithms
2. **Hardware Acceleration**: GPU-based map generation
3. **Distributed Computing**: Parallel map generation
4. **Cloud-based Processing**: Server-side generation options

---

## Appendix

### A. Performance Test Commands

```bash
# Run all performance tests
Unity -batchmode -runTests -testCategory "Performance" -logFile -

# Run specific performance test
Unity -batchmode -runTests -testMethod "Performance_MapGeneration_CompletesWithinThreshold" -logFile -

# Generate performance report
Unity -batchmode -executeMethod "PerformanceDashboard.GenerateReport" -logFile -
```

### B. Configuration Files

Performance settings are configured in:

- `Assets/Scripts/MapGeneration/Configuration/MapGenerationSettings.cs`
- `Assets/Tests/EditMode/MapGeneration/Performance/PerformanceBenchmark.cs`
- `.github/workflows/test.yml` (CI/CD configuration)

### C. Performance Data Locations

- Baselines: `Application.persistentDataPath/PerformanceBaselines/`
- Memory Reports: `Application.persistentDataPath/MemoryReports/`
- GC Reports: `Application.persistentDataPath/GcPressureReports/`
- Dashboard Reports: `Application.persistentDataPath/PerformanceReports/`

---

*This document is maintained by the development team and updated as performance requirements evolve.*