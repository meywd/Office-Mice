using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Factories;

namespace OfficeMice.MapGeneration.Base
{
    /// <summary>
    /// Base test fixture providing common setup, teardown, and utility methods for MapGeneration tests.
    /// Includes deterministic test data creation, validation helpers, and performance measurement utilities.
    /// </summary>
    public abstract class BaseTestFixture
    {
        protected const int DEFAULT_TEST_SEED = 12345;
        protected const float DEFAULT_TIMEOUT_SECONDS = 30f;
        protected const float PERFORMANCE_TOLERANCE_PERCENT = 0.1f; // 10% tolerance

        protected MapGenerationSettings _testSettings;
        protected MapData _testMapData;
        protected System.Random _testRandom;

        [SetUp]
        public virtual void SetUp()
        {
            _testRandom = MapGenerationTestDataFactory.CreateDeterministicRandom(DEFAULT_TEST_SEED);
            _testSettings = MapGenerationTestDataFactory.CreateSettings("standard", DEFAULT_TEST_SEED);
            _testMapData = MapGenerationTestDataFactory.CreateMapData("multiple_rooms", DEFAULT_TEST_SEED);
        }

        [TearDown]
        public virtual void TearDown()
        {
            _testSettings = null;
            _testMapData = null;
            _testRandom = null;
        }

        #region Assertion Helpers

        /// <summary>
        /// Asserts that a ValidationResult is valid and contains no errors.
        /// </summary>
        /// <param name="result">ValidationResult to check</param>
        /// <param name="message">Optional custom message</param>
        protected static void AssertValidationSuccess(ValidationResult result, string message = null)
        {
            Assert.IsTrue(result.IsValid, message ?? $"Expected validation to succeed, but failed with errors: {string.Join(", ", result.Errors)}");
            Assert.IsEmpty(result.Errors, message ?? "Expected no validation errors");
        }

        /// <summary>
        /// Asserts that a ValidationResult is invalid and contains errors.
        /// </summary>
        /// <param name="result">ValidationResult to check</param>
        /// <param name="expectedErrorCount">Expected number of errors (optional)</param>
        /// <param name="message">Optional custom message</param>
        protected static void AssertValidationFailure(ValidationResult result, int expectedErrorCount = -1, string message = null)
        {
            Assert.IsFalse(result.IsValid, message ?? "Expected validation to fail, but it succeeded");
            Assert.IsNotEmpty(result.Errors, message ?? "Expected validation to contain errors");
            
            if (expectedErrorCount >= 0)
            {
                Assert.AreEqual(expectedErrorCount, result.Errors.Count, message ?? $"Expected {expectedErrorCount} validation errors");
            }
        }

        /// <summary>
        /// Asserts that MapData has valid dimensions and structure.
        /// </summary>
        /// <param name="mapData">MapData to validate</param>
        /// <param name="expectedWidth">Expected width (optional)</param>
        /// <param name="expectedHeight">Expected height (optional)</param>
        protected static void AssertValidMapData(MapData mapData, int? expectedWidth = null, int? expectedHeight = null)
        {
            Assert.IsNotNull(mapData, "MapData should not be null");
            Assert.IsTrue(mapData.Width > 0, "MapData width should be positive");
            Assert.IsTrue(mapData.Height > 0, "MapData height should be positive");
            
            if (expectedWidth.HasValue)
                Assert.AreEqual(expectedWidth.Value, mapData.Width, $"Expected map width {expectedWidth.Value}");
            
            if (expectedHeight.HasValue)
                Assert.AreEqual(expectedHeight.Value, mapData.Height, $"Expected map height {expectedHeight.Value}");
        }

        /// <summary>
        /// Asserts that MapGenerationSettings has valid configuration.
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        protected static void AssertValidSettings(MapGenerationSettings settings)
        {
            Assert.IsNotNull(settings, "MapGenerationSettings should not be null");
            Assert.IsTrue(settings.mapWidth > 0, "Map width should be positive");
            Assert.IsTrue(settings.mapHeight > 0, "Map height should be positive");
            Assert.IsTrue(settings.minRoomSize > 0, "Min room size should be positive");
            Assert.IsTrue(settings.maxRoomSize >= settings.minRoomSize, "Max room size should be >= min room size");
            Assert.IsTrue(settings.maxRoomCount > 0, "Max room count should be positive");
            Assert.IsTrue(settings.corridorWidth > 0, "Corridor width should be positive");
        }

        #endregion

        #region Performance Testing Helpers

        /// <summary>
        /// Measures execution time of an action and asserts it's within expected bounds.
        /// </summary>
        /// <param name="action">Action to measure</param>
        /// <param name="expectedMaxTimeMs">Maximum expected time in milliseconds</param>
        /// <param name="message">Optional custom message</param>
        protected void AssertPerformance(Action action, float expectedMaxTimeMs, string message = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            var actualTimeMs = stopwatch.ElapsedMilliseconds;
            Assert.LessOrEqual(actualTimeMs, expectedMaxTimeMs, 
                message ?? $"Action took {actualTimeMs}ms, expected <= {expectedMaxTimeMs}ms");
        }

        /// <summary>
        /// Measures execution time of a coroutine and asserts it's within expected bounds.
        /// </summary>
        /// <param name="coroutine">Coroutine to measure</param>
        /// <param name="expectedMaxTimeMs">Maximum expected time in milliseconds</param>
        /// <param name="message">Optional custom message</param>
        protected IEnumerator AssertPerformance(IEnumerator coroutine, float expectedMaxTimeMs, string message = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            while (coroutine.MoveNext())
            {
                yield return coroutine.Current;
            }
            
            stopwatch.Stop();
            var actualTimeMs = stopwatch.ElapsedMilliseconds;
            Assert.LessOrEqual(actualTimeMs, expectedMaxTimeMs, 
                message ?? $"Coroutine took {actualTimeMs}ms, expected <= {expectedMaxTimeMs}ms");
        }

        /// <summary>
        /// Asserts that memory usage is within acceptable bounds.
        /// </summary>
        /// <param name="action">Action to measure memory usage for</param>
        /// <param name="expectedMaxMemoryMB">Maximum expected memory usage in MB</param>
        /// <param name="message">Optional custom message</param>
        protected void AssertMemoryUsage(Action action, float expectedMaxMemoryMB, string message = null)
        {
            // Force garbage collection before measurement
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            var beforeMemory = System.GC.GetTotalMemory(false);
            action();
            
            var afterMemory = System.GC.GetTotalMemory(false);
            var memoryUsedMB = (afterMemory - beforeMemory) / (1024f * 1024f);

            Assert.LessOrEqual(memoryUsedMB, expectedMaxMemoryMB, 
                message ?? $"Action used {memoryUsedMB:F2}MB memory, expected <= {expectedMaxMemoryMB}MB");
        }

        #endregion

        #region Test Data Helpers

        /// <summary>
        /// Creates test settings with the specified scenario type.
        /// </summary>
        /// <param name="scenarioType">Type of settings scenario</param>
        /// <param name="seed">Optional seed override</param>
        /// <returns>Configured MapGenerationSettings</returns>
        protected MapGenerationSettings CreateTestSettings(string scenarioType, int? seed = null)
        {
            return MapGenerationTestDataFactory.CreateSettings(scenarioType, seed ?? DEFAULT_TEST_SEED);
        }

        /// <summary>
        /// Creates test map data with the specified scenario type.
        /// </summary>
        /// <param name="scenarioType">Type of map scenario</param>
        /// <param name="seed">Optional seed override</param>
        /// <returns>Configured MapData</returns>
        protected MapData CreateTestMapData(string scenarioType, int? seed = null)
        {
            return MapGenerationTestDataFactory.CreateMapData(scenarioType, seed ?? DEFAULT_TEST_SEED);
        }

        /// <summary>
        /// Creates a deterministic random instance for testing.
        /// </summary>
        /// <param name="seed">Seed for the random instance</param>
        /// <returns>Deterministic System.Random instance</returns>
        protected System.Random CreateTestRandom(int seed)
        {
            return MapGenerationTestDataFactory.CreateDeterministicRandom(seed);
        }

        /// <summary>
        /// Creates a collection of all test settings scenarios.
        /// </summary>
        /// <param name="baseSeed">Base seed for all settings</param>
        /// <returns>Dictionary of scenario types to settings</returns>
        protected Dictionary<string, MapGenerationSettings> CreateAllTestSettings(int baseSeed = DEFAULT_TEST_SEED)
        {
            return MapGenerationTestDataFactory.CreateAllTestSettings(baseSeed);
        }

        /// <summary>
        /// Creates a collection of all test map data scenarios.
        /// </summary>
        /// <param name="baseSeed">Base seed for all map data</param>
        /// <returns>Dictionary of scenario types to map data</returns>
        protected Dictionary<string, MapData> CreateAllTestMapData(int baseSeed = DEFAULT_TEST_SEED)
        {
            return MapGenerationTestDataFactory.CreateAllTestMapData(baseSeed);
        }

        #endregion

        #region Exception Testing Helpers

        /// <summary>
        /// Asserts that an action throws the expected exception type.
        /// </summary>
        /// <typeparam name="TException">Expected exception type</typeparam>
        /// <param name="action">Action that should throw</param>
        /// <param name="message">Optional custom message</param>
        protected static void AssertThrows<TException>(Action action, string message = null) where TException : Exception
        {
            var exception = Assert.Throws<TException>(action, message ?? $"Expected action to throw {typeof(TException).Name}");
            Assert.IsNotNull(exception, "Exception should not be null");
        }

        /// <summary>
        /// Asserts that an action throws the expected exception type with specific message.
        /// </summary>
        /// <typeparam name="TException">Expected exception type</typeparam>
        /// <param name="action">Action that should throw</param>
        /// <param name="expectedMessage">Expected exception message</param>
        /// <param name="message">Optional custom message</param>
        protected static void AssertThrows<TException>(Action action, string expectedMessage, string message = null) where TException : Exception
        {
            var exception = Assert.Throws<TException>(action, message ?? $"Expected action to throw {typeof(TException).Name}");
            Assert.IsNotNull(exception, "Exception should not be null");
            Assert.AreEqual(expectedMessage, exception.Message, "Exception message should match expected");
        }

        #endregion

        #region Async Testing Helpers

        /// <summary>
        /// Runs a coroutine test with timeout and proper cleanup.
        /// </summary>
        /// <param name="coroutine">Coroutine to test</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        /// <returns>IEnumerator for UnityTest</returns>
        protected IEnumerator RunCoroutineWithTimeout(IEnumerator coroutine, float timeoutSeconds = DEFAULT_TIMEOUT_SECONDS)
        {
            var startTime = Time.time;
            
            while (coroutine.MoveNext())
            {
                if (Time.time - startTime > timeoutSeconds)
                {
                    Assert.Fail($"Coroutine test timed out after {timeoutSeconds} seconds");
                    yield break;
                }
                yield return coroutine.Current;
            }
        }

        /// <summary>
        /// Asserts that a coroutine completes without throwing exceptions.
        /// </summary>
        /// <param name="coroutine">Coroutine to test</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        /// <returns>IEnumerator for UnityTest</returns>
        protected IEnumerator AssertCoroutineCompletes(IEnumerator coroutine, float timeoutSeconds = DEFAULT_TIMEOUT_SECONDS)
        {
            var exceptionThrown = false;
            Exception caughtException = null;

            try
            {
                yield return RunCoroutineWithTimeout(coroutine, timeoutSeconds);
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                caughtException = ex;
            }

            Assert.IsFalse(exceptionThrown, $"Coroutine threw exception: {caughtException?.Message}");
        }

        #endregion
    }
}