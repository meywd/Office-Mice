using System;
using System.Collections;
using UnityEngine;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of IMapGenerator for testing purposes.
    /// Provides deterministic behavior and configurable responses for unit testing.
    /// </summary>
    public class MockMapGenerator : IMapGenerator
    {
        private MapData _mockMapData;
        private float _mockGenerationTime;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;

        public event Action<float, string> OnProgressUpdated;
        public event Action<MapData> OnGenerationCompleted;
        public event Action<Exception> OnGenerationFailed;

        public MockMapGenerator()
        {
            _mockMapData = CreateDefaultMockMapData();
            _mockGenerationTime = 100f; // 100ms default
            _shouldThrowException = false;
            _mockValidationResult = ValidationResult.Success();
        }

        /// <summary>
        /// Sets the mock map data to return from generation methods.
        /// </summary>
        public void SetMockMapData(MapData mapData)
        {
            _mockMapData = mapData;
        }

        /// <summary>
        /// Sets the mock generation time in milliseconds.
        /// </summary>
        public void SetMockGenerationTime(float timeMs)
        {
            _mockGenerationTime = timeMs;
        }

        /// <summary>
        /// Configures the mock to throw an exception during generation.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock generation failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        public IEnumerator<MapData> GenerateMapAsync(MapGenerationSettings settings, int seed = 0)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_shouldThrowException)
            {
                OnGenerationFailed?.Invoke(_exceptionToThrow);
                throw _exceptionToThrow;
            }

            // Simulate async generation with progress updates
            float progress = 0f;
            while (progress < 1f)
            {
                progress += 0.1f;
                OnProgressUpdated?.Invoke(progress, $"Generating map... {Mathf.RoundToInt(progress * 100)}%");
                yield return null;
            }

            OnGenerationCompleted?.Invoke(_mockMapData);
            yield return _mockMapData;
        }

        public MapData GenerateMap(MapGenerationSettings settings, int seed = 0)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_shouldThrowException)
            {
                OnGenerationFailed?.Invoke(_exceptionToThrow);
                throw _exceptionToThrow;
            }

            OnGenerationCompleted?.Invoke(_mockMapData);
            return _mockMapData;
        }

        public ValidationResult ValidateSettings(MapGenerationSettings settings)
        {
            if (settings == null)
                return ValidationResult.Failure("Settings cannot be null");

            return _mockValidationResult;
        }

        public float EstimateGenerationTime(MapGenerationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return _mockGenerationTime;
        }

        private MapData CreateDefaultMockMapData()
        {
            var mapData = new MapData();
            mapData.SetDimensions(50, 50);
            mapData.SetSeed(12345);
            
            // Add some mock rooms
            var room1 = new RoomData();
            room1.SetBounds(new Rect(5, 5, 10, 10));
            room1.Classification = RoomClassification.Office;
            
            var room2 = new RoomData();
            room2.SetBounds(new Rect(20, 20, 8, 8));
            room2.Classification = RoomClassification.BreakRoom;
            
            mapData.AddRoom(room1);
            mapData.AddRoom(room2);
            
            return mapData;
        }
    }
}