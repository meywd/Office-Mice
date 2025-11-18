using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of IRoomGenerator for testing purposes.
    /// Provides configurable room generation behavior for unit testing.
    /// </summary>
    public class MockRoomGenerator : IRoomGenerator
    {
        private List<RoomData> _mockRooms;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;
        private float _mockTotalArea;

        public event Action<RoomData> OnRoomGenerated;
        public event Action<RoomData, Exception> OnRoomGenerationFailed;

        public MockRoomGenerator()
        {
            _mockRooms = CreateDefaultMockRooms();
            _shouldThrowException = false;
            _mockValidationResult = ValidationResult.Success();
            _mockTotalArea = 324f; // Default area for mock rooms
        }

        /// <summary>
        /// Sets the mock rooms to return from generation methods.
        /// </summary>
        public void SetMockRooms(List<RoomData> rooms)
        {
            _mockRooms = rooms ?? new List<RoomData>();
        }

        /// <summary>
        /// Sets the mock total area for the rooms.
        /// </summary>
        public void SetMockTotalArea(float area)
        {
            _mockTotalArea = area;
        }

        /// <summary>
        /// Configures the mock to throw an exception during room generation.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock room generation failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        public List<RoomData> GenerateRooms(MapGenerationSettings settings)
        {
            return GenerateRooms(settings, 0);
        }

        public List<RoomData> GenerateRooms(MapGenerationSettings settings, int seed)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_shouldThrowException)
            {
                OnRoomGenerationFailed?.Invoke(null, _exceptionToThrow);
                throw _exceptionToThrow;
            }

            foreach (var room in _mockRooms)
            {
                OnRoomGenerated?.Invoke(room);
            }

            return new List<RoomData>(_mockRooms);
        }

        public ValidationResult ValidateRoomPlacement(List<RoomData> rooms, MapGenerationSettings settings)
        {
            if (rooms == null)
                return ValidationResult.Failure("Rooms list cannot be null");
            if (settings == null)
                return ValidationResult.Failure("Settings cannot be null");

            return _mockValidationResult;
        }

        public List<RoomData> OptimizeRoomLayout(List<RoomData> rooms, MapGenerationSettings settings)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Return a copy of the rooms to simulate optimization
            return new List<RoomData>(rooms);
        }

        public List<RoomData> ClassifyRooms(List<RoomData> rooms, MapGenerationSettings settings)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Return a copy with classifications applied
            var classifiedRooms = new List<RoomData>();
            foreach (var room in rooms)
            {
                var roomCopy = room; // In real implementation, this would be a deep copy
                if (roomCopy.Classification == RoomClassification.Undefined)
                {
                    roomCopy.Classification = RoomClassification.Office; // Default classification
                }
                classifiedRooms.Add(roomCopy);
            }

            return classifiedRooms;
        }

        public float CalculateTotalRoomArea(List<RoomData> rooms)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));

            return _mockTotalArea;
        }

        public Vector2Int? FindOptimalRoomPosition(List<RoomData> existingRooms, Vector2Int newRoomSize, MapGenerationSettings settings)
        {
            if (existingRooms == null)
                throw new ArgumentNullException(nameof(existingRooms));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Return a mock optimal position
            return new Vector2Int(30, 30);
        }

        private List<RoomData> CreateDefaultMockRooms()
        {
            var rooms = new List<RoomData>();

            var room1 = new RoomData();
            room1.SetBounds(new Rect(5, 5, 12, 12));
            room1.Classification = RoomClassification.Office;
            rooms.Add(room1);

            var room2 = new RoomData();
            room2.SetBounds(new Rect(20, 8, 8, 10));
            room2.Classification = RoomClassification.BreakRoom;
            rooms.Add(room2);

            var room3 = new RoomData();
            room3.SetBounds(new Rect(35, 15, 10, 8));
            room3.Classification = RoomClassification.MeetingRoom;
            rooms.Add(room3);

            return rooms;
        }
    }
}