using System;
using System.Collections.Generic;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Mocks
{
    /// <summary>
    /// Mock implementation of ICorridorGenerator for testing purposes.
    /// Provides configurable corridor generation behavior for unit testing.
    /// </summary>
    public class MockCorridorGenerator : ICorridorGenerator
    {
        private List<CorridorData> _mockCorridors;
        private bool _shouldThrowException;
        private Exception _exceptionToThrow;
        private ValidationResult _mockValidationResult;
        private float _mockTotalLength;

        public event Action<CorridorData> OnCorridorGenerated;
        public event Action<RoomData, RoomData, Exception> OnCorridorGenerationFailed;

        public MockCorridorGenerator()
        {
            _mockCorridors = new List<CorridorData>();
            _shouldThrowException = false;
            _mockValidationResult = ValidationResult.Success();
            _mockTotalLength = 50f; // Default total length
        }

        /// <summary>
        /// Sets the mock corridors to return from generation methods.
        /// </summary>
        public void SetMockCorridors(List<CorridorData> corridors)
        {
            _mockCorridors = corridors ?? new List<CorridorData>();
        }

        /// <summary>
        /// Sets the mock total corridor length.
        /// </summary>
        public void SetMockTotalLength(float length)
        {
            _mockTotalLength = length;
        }

        /// <summary>
        /// Configures the mock to throw an exception during corridor generation.
        /// </summary>
        public void SetThrowException(bool shouldThrow, Exception exception = null)
        {
            _shouldThrowException = shouldThrow;
            _exceptionToThrow = exception ?? new InvalidOperationException("Mock corridor generation failed");
        }

        /// <summary>
        /// Sets the mock validation result.
        /// </summary>
        public void SetMockValidationResult(ValidationResult result)
        {
            _mockValidationResult = result;
        }

        public List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings)
        {
            return ConnectRooms(rooms, settings, 0);
        }

        public List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings, int seed)
        {
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_shouldThrowException)
            {
                OnCorridorGenerationFailed?.Invoke(null, null, _exceptionToThrow);
                throw _exceptionToThrow;
            }

            // Generate mock corridors connecting adjacent rooms
            var corridors = new List<CorridorData>();
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                var corridor = CreateMockCorridor(rooms[i], rooms[i + 1]);
                corridors.Add(corridor);
                OnCorridorGenerated?.Invoke(corridor);
            }

            return corridors;
        }

        public CorridorData? ConnectRooms(RoomData room1, RoomData room2, MapGenerationSettings settings)
        {
            if (room1 == null)
                throw new ArgumentNullException(nameof(room1));
            if (room2 == null)
                throw new ArgumentNullException(nameof(room2));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_shouldThrowException)
            {
                OnCorridorGenerationFailed?.Invoke(room1, room2, _exceptionToThrow);
                return null;
            }

            var corridor = CreateMockCorridor(room1, room2);
            OnCorridorGenerated?.Invoke(corridor);
            return corridor;
        }

        public ValidationResult ValidateConnectivity(List<RoomData> rooms, List<CorridorData> corridors)
        {
            if (rooms == null)
                return ValidationResult.Failure("Rooms list cannot be null");
            if (corridors == null)
                return ValidationResult.Failure("Corridors list cannot be null");

            return _mockValidationResult;
        }

        public List<CorridorData> OptimizeCorridors(List<CorridorData> corridors, List<RoomData> rooms, MapGenerationSettings settings)
        {
            if (corridors == null)
                throw new ArgumentNullException(nameof(corridors));
            if (rooms == null)
                throw new ArgumentNullException(nameof(rooms));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Return a copy to simulate optimization
            return new List<CorridorData>(corridors);
        }

        public List<CorridorData> ResolveIntersections(List<CorridorData> corridors)
        {
            if (corridors == null)
                throw new ArgumentNullException(nameof(corridors));

            // Return a copy to simulate intersection resolution
            return new List<CorridorData>(corridors);
        }

        public float CalculateTotalCorridorLength(List<CorridorData> corridors)
        {
            if (corridors == null)
                throw new ArgumentNullException(nameof(corridors));

            return _mockTotalLength;
        }

        public List<CorridorData> FindShortestPath(RoomData startRoom, RoomData endRoom, List<CorridorData> corridors)
        {
            if (startRoom == null)
                throw new ArgumentNullException(nameof(startRoom));
            if (endRoom == null)
                throw new ArgumentNullException(nameof(endRoom));
            if (corridors == null)
                throw new ArgumentNullException(nameof(corridors));

            // Return mock shortest path
            return corridors.Count > 0 ? new List<CorridorData> { corridors[0] } : new List<CorridorData>();
        }

        private CorridorData CreateMockCorridor(RoomData room1, RoomData room2)
        {
            var corridor = new CorridorData();
            corridor.SetRooms(room1, room2);
            
            // Create a simple L-shaped path
            var path = new List<Vector2Int>();
            var start = room1.Bounds.center;
            var end = room2.Bounds.center;
            
            path.Add(Vector2Int.RoundToInt(start));
            path.Add(new Vector2Int(Vector2Int.RoundToInt(start).x, Vector2Int.RoundToInt(end).y));
            path.Add(Vector2Int.RoundToInt(end));
            
            corridor.SetPath(path);
            return corridor;
        }
    }
}