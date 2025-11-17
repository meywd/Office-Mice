using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Validation;

namespace OfficeMice.MapGeneration.Data
{
    /// <summary>
    /// Represents a single room in the generated map.
    /// Rooms are rectangular regions created by BSP partitioning.
    /// </summary>
    [Serializable]
    public class RoomData
    {
        [Header("Identity")]
        [SerializeField] private int _roomID;

        [Header("Spatial Properties")]
        [SerializeField] private RectInt _bounds; // Actual room area (smaller than BSP partition)
        [SerializeField] private Vector2Int _center;
        [SerializeField] private int _area; // Cached: bounds.width * bounds.height

        [Header("Connectivity")]
        [SerializeField] private List<int> _connectedRoomIDs; // IDs of rooms connected via corridors
        [SerializeField] private List<DoorwayPosition> _doorways;

        [Header("Classification (Phase 2)")]
        [SerializeField] private RoomClassification _classification;
        [SerializeField] private bool _isOnCriticalPath; // Main path from spawn to boss

        [Header("Template Assignment (Phase 2)")]
        [SerializeField] private string _assignedTemplateID; // RoomTemplate asset GUID

        [Header("Computed Properties")]
        [SerializeField] private float _distanceFromPlayerSpawn; // Set during classification

        // Public Properties
        public int RoomID
        {
            get => _roomID;
            set => _roomID = value;
        }

        public RectInt Bounds => _bounds;
        public Vector2Int Center => _center;
        public int Area => _area;
        public IReadOnlyList<int> ConnectedRoomIDs => _connectedRoomIDs.AsReadOnly();
        public IReadOnlyList<DoorwayPosition> Doorways => _doorways.AsReadOnly();
        public RoomClassification Classification => _classification;
        public bool IsOnCriticalPath => _isOnCriticalPath;
        public float DistanceFromPlayerSpawn => _distanceFromPlayerSpawn;
        public string AssignedTemplateID => _assignedTemplateID;

        // Constructor
        public RoomData(RectInt bounds)
        {
            _bounds = bounds;
            _center = new Vector2Int(
                bounds.x + bounds.width / 2,
                bounds.y + bounds.height / 2
            );
            _area = bounds.width * bounds.height;

            _connectedRoomIDs = new List<int>();
            _doorways = new List<DoorwayPosition>();
            _classification = RoomClassification.Unassigned;
            _isOnCriticalPath = false;
            _distanceFromPlayerSpawn = -1f;
            _assignedTemplateID = string.Empty;
        }

        // Mutators
        public void ConnectToRoom(int roomID)
        {
            if (!_connectedRoomIDs.Contains(roomID))
                _connectedRoomIDs.Add(roomID);
        }

        public void DisconnectFromRoom(int roomID)
        {
            _connectedRoomIDs.Remove(roomID);
        }

        public void AddDoorway(DoorwayPosition doorway)
        {
            if (!_doorways.Contains(doorway))
                _doorways.Add(doorway);
        }

        public void RemoveDoorway(DoorwayPosition doorway)
        {
            _doorways.Remove(doorway);
        }

        public void SetClassification(RoomClassification classification)
        {
            _classification = classification;
        }

        public void SetOnCriticalPath(bool isOnPath)
        {
            _isOnCriticalPath = isOnPath;
        }

        public void SetDistanceFromPlayerSpawn(float distance)
        {
            _distanceFromPlayerSpawn = distance;
        }

        public void AssignTemplate(string templateID)
        {
            _assignedTemplateID = templateID ?? string.Empty;
        }

        // Query Methods
        public bool IsConnectedTo(int roomID)
        {
            return _connectedRoomIDs.Contains(roomID);
        }

        public bool HasDoorwayAt(Vector2Int position)
        {
            return _doorways.Any(d => d.position == position);
        }

        public DoorwayPosition? GetDoorwayAt(Vector2Int position)
        {
            return _doorways.FirstOrDefault(d => d.position == position);
        }

        public bool ContainsPoint(Vector2Int point)
        {
            return _bounds.Contains(point);
        }

        public bool OverlapsWith(RectInt otherBounds)
        {
            return _bounds.Overlaps(otherBounds);
        }

        public Vector2Int GetRandomPoint(System.Random random = null)
        {
            random = random ?? new System.Random();
            return new Vector2Int(
                _bounds.x + random.Next(_bounds.width),
                _bounds.y + random.Next(_bounds.height)
            );
        }

        public Vector2Int GetRandomEdgePoint(System.Random random = null)
        {
            random = random ?? new System.Random();
            int edge = random.Next(4); // 0=top, 1=right, 2=bottom, 3=left
            
            switch (edge)
            {
                case 0: // Top edge
                    return new Vector2Int(_bounds.x + random.Next(_bounds.width), _bounds.yMax - 1);
                case 1: // Right edge
                    return new Vector2Int(_bounds.xMax - 1, _bounds.y + random.Next(_bounds.height));
                case 2: // Bottom edge
                    return new Vector2Int(_bounds.x + random.Next(_bounds.width), _bounds.y);
                case 3: // Left edge
                    return new Vector2Int(_bounds.x, _bounds.y + random.Next(_bounds.height));
                default:
                    return _center;
            }
        }

        // Validation
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate basic properties
            if (_area <= 0)
                result.AddError($"Room {_roomID} has zero or negative area");

            if (_bounds.width < 3 || _bounds.height < 3)
                result.AddError($"Room {_roomID} is too small (min 3x3), current: {_bounds.width}x{_bounds.height}");

            // Validate center calculation
            Vector2Int expectedCenter = new Vector2Int(
                _bounds.x + _bounds.width / 2,
                _bounds.y + _bounds.height / 2
            );
            if (_center != expectedCenter)
                result.AddError($"Room {_roomID} center mismatch: expected {expectedCenter}, got {_center}");

            // Validate area calculation
            int expectedArea = _bounds.width * _bounds.height;
            if (_area != expectedArea)
                result.AddError($"Room {_roomID} area mismatch: expected {expectedArea}, got {_area}");

            // Validate connectivity
            if (_doorways.Count == 0)
                result.AddWarning($"Room {_roomID} has no doorways (may be inaccessible)");

            // Validate doorway positions
            foreach (var doorway in _doorways)
            {
                if (!_bounds.Contains(doorway.position))
                    result.AddError($"Room {_roomID} has doorway at {doorway.position} outside room bounds {_bounds}");

                if (doorway.width < 1 || doorway.width > 3)
                    result.AddError($"Room {_roomID} has invalid doorway width: {doorway.width}");
            }

            // Validate connected room IDs
            var uniqueConnections = new HashSet<int>(_connectedRoomIDs);
            if (uniqueConnections.Count != _connectedRoomIDs.Count)
                result.AddWarning($"Room {_roomID} has duplicate room connections");

            if (_connectedRoomIDs.Contains(_roomID))
                result.AddError($"Room {_roomID} is connected to itself");

            // Validate classification
            if (_classification == RoomClassification.Unassigned)
                result.AddWarning($"Room {_roomID} has not been classified");

            // Validate template assignment
            if (!string.IsNullOrEmpty(_assignedTemplateID) && _classification == RoomClassification.Unassigned)
                result.AddWarning($"Room {_roomID} has template assigned but no classification");

            return result;
        }

        // Utility Methods
        public void ClearConnections()
        {
            _connectedRoomIDs.Clear();
        }

        public void ClearDoorways()
        {
            _doorways.Clear();
        }

        public RoomData Clone()
        {
            var clone = new RoomData(_bounds);
            clone._roomID = _roomID;
            clone._classification = _classification;
            clone._isOnCriticalPath = _isOnCriticalPath;
            clone._distanceFromPlayerSpawn = _distanceFromPlayerSpawn;
            clone._assignedTemplateID = _assignedTemplateID;
            
            // Clone collections
            clone._connectedRoomIDs = new List<int>(_connectedRoomIDs);
            clone._doorways = new List<DoorwayPosition>(_doorways);
            
            return clone;
        }

        public override string ToString()
        {
            return $"Room[{_roomID}] {_bounds} ({_classification}) - {_connectedRoomIDs.Count} connections, {_doorways.Count} doorways";
        }

        public override bool Equals(object obj)
        {
            if (obj is RoomData other)
            {
                return _roomID == other._roomID && _bounds == other._bounds;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_roomID, _bounds);
        }
    }
}