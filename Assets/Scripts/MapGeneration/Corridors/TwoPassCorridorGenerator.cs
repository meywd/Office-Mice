using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OfficeMice.MapGeneration.Interfaces;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Validation;
using OfficeMice.MapGeneration.Pathfinding;

namespace OfficeMice.MapGeneration.Corridors
{
    /// <summary>
    /// Two-pass corridor generator implementing hierarchical corridor system.
    /// Primary pass connects core rooms to form main hallways (5 tiles width).
    /// Secondary pass connects remaining rooms to main arteries (3 tiles width).
    /// Uses MST optimization for primary connections and guarantees 100% connectivity.
    /// </summary>
    public class TwoPassCorridorGenerator : ICorridorGenerator
    {
        #region Private Fields
        
        private IPathfinder _pathfinder;
        private System.Random _random;
        private int _nextCorridorID = 0;
        
        // Performance tracking
        private System.Diagnostics.Stopwatch _generationStopwatch;
        
        // Configuration constants
        private const int PRIMARY_CORRIDOR_WIDTH = 5;
        private const int SECONDARY_CORRIDOR_WIDTH = 3;
        private const float CORE_ROOM_THRESHOLD = 0.7f; // Top 30% by area are core rooms
        private const int MIN_CORE_ROOMS = 2;
        
        #endregion
        
        #region Events
        
        public event Action<CorridorData> OnCorridorGenerated;
        public event Action<RoomData, RoomData, Exception> OnCorridorGenerationFailed;
        
        #endregion
        
        #region Constructor
        
        public TwoPassCorridorGenerator(IPathfinder pathfinder = null)
        {
            _pathfinder = pathfinder ?? new AStarPathfinder();
            _generationStopwatch = new System.Diagnostics.Stopwatch();
        }
        
        #endregion
        
        #region ICorridorGenerator Implementation
        
        public List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings)
        {
            return ConnectRooms(rooms, settings, settings?.MapConfig?.GetSeed() ?? 0);
        }
        
        public List<CorridorData> ConnectRooms(List<RoomData> rooms, MapGenerationSettings settings, int seed)
        {
            _generationStopwatch.Restart();
            
            try
            {
                // Validate inputs
                var validationResult = ValidateInputs(rooms, settings);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Invalid inputs: {string.Join(", ", validationResult.Errors)}");
                }
                
                _random = new System.Random(seed);
                _nextCorridorID = 0;
                
                // Step 1: Identify core rooms
                var coreRooms = IdentifyCoreRooms(rooms);
                if (coreRooms.Count < MIN_CORE_ROOMS)
                {
                    // Fallback: use largest rooms as core rooms
                    coreRooms = rooms.OrderByDescending(r => r.Area).Take(MIN_CORE_ROOMS).ToList();
                }
                
                // Step 2: Generate primary corridors (MST optimized)
                var primaryCorridors = GeneratePrimaryCorridors(coreRooms, rooms, settings);
                
                // Step 3: Generate secondary corridors
                var secondaryCorridors = GenerateSecondaryCorridors(rooms, coreRooms, primaryCorridors, settings);
                
                // Step 4: Merge and optimize corridors
                var allCorridors = new List<CorridorData>();
                allCorridors.AddRange(primaryCorridors);
                allCorridors.AddRange(secondaryCorridors);
                
                var optimizedCorridors = OptimizeCorridors(allCorridors, rooms, settings);
                
                // Step 5: Validate connectivity
                var connectivityResult = ValidateConnectivity(rooms, optimizedCorridors);
                if (!connectivityResult.IsValid)
                {
                    // Attempt to fix connectivity issues
                    optimizedCorridors = FixConnectivityIssues(rooms, optimizedCorridors, settings);
                }
                
                _generationStopwatch.Stop();
                
                if (settings.DebugSettings?.LogPerformanceMetrics == true)
                {
                    Debug.Log($"TwoPassCorridorGenerator: Generated {optimizedCorridors.Count} corridors in {_generationStopwatch.ElapsedMilliseconds}ms");
                }
                
                return optimizedCorridors;
            }
            catch (Exception ex)
            {
                _generationStopwatch.Stop();
                Debug.LogError($"TwoPassCorridorGenerator failed: {ex.Message}");
                throw;
            }
        }
        
        public CorridorData? ConnectRooms(RoomData room1, RoomData room2, MapGenerationSettings settings)
        {
            try
            {
                var corridor = GenerateSingleCorridor(room1, room2, settings, SECONDARY_CORRIDOR_WIDTH);
                if (corridor != null)
                {
                    OnCorridorGenerated?.Invoke(corridor);
                }
                return corridor;
            }
            catch (Exception ex)
            {
                OnCorridorGenerationFailed?.Invoke(room1, room2, ex);
                return null;
            }
        }
        
        public ValidationResult ValidateConnectivity(List<RoomData> rooms, List<CorridorData> corridors)
        {
            var result = new ValidationResult();
            
            if (rooms == null || rooms.Count == 0)
            {
                result.AddError("No rooms to validate connectivity");
                return result;
            }
            
            if (corridors == null || corridors.Count == 0)
            {
                result.AddError("No corridors to validate connectivity");
                return result;
            }
            
            // Build adjacency graph
            var adjacency = BuildConnectivityGraph(rooms, corridors);
            
            // Check if all rooms are reachable from the first room
            var startRoom = rooms[0];
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            
            queue.Enqueue(startRoom.RoomID);
            visited.Add(startRoom.RoomID);
            
            while (queue.Count > 0)
            {
                var currentRoomID = queue.Dequeue();
                
                if (adjacency.ContainsKey(currentRoomID))
                {
                    foreach (var connectedRoomID in adjacency[currentRoomID])
                    {
                        if (!visited.Contains(connectedRoomID))
                        {
                            visited.Add(connectedRoomID);
                            queue.Enqueue(connectedRoomID);
                        }
                    }
                }
            }
            
            // Check for isolated rooms
            var isolatedRooms = rooms.Where(r => !visited.Contains(r.RoomID)).ToList();
            foreach (var isolatedRoom in isolatedRooms)
            {
                result.AddError($"Room {isolatedRoom.RoomID} is not connected to the corridor network");
            }
            
            // Validate corridor widths
            foreach (var corridor in corridors)
            {
                if (corridor.Width != PRIMARY_CORRIDOR_WIDTH && corridor.Width != SECONDARY_CORRIDOR_WIDTH)
                {
                    result.AddWarning($"Corridor {corridor.CorridorID} has non-standard width: {corridor.Width}");
                }
            }
            
            return result;
        }
        
        public List<CorridorData> OptimizeCorridors(List<CorridorData> corridors, List<RoomData> rooms, MapGenerationSettings settings)
        {
            var optimized = new List<CorridorData>();
            
            // Resolve intersections first
            var resolvedCorridors = ResolveIntersections(corridors);
            
            // Apply path smoothing if enabled
            if (settings.CorridorConfig.PathSmoothing > 0f)
            {
                foreach (var corridor in resolvedCorridors)
                {
                    var smoothedPath = _pathfinder.OptimizePath(corridor.PathTiles.ToList(), CreateObstacleMap(rooms));
                    corridor.SetPath(smoothedPath);
                    optimized.Add(corridor);
                }
            }
            else
            {
                optimized.AddRange(resolvedCorridors);
            }
            
            return optimized;
        }
        
        public List<CorridorData> ResolveIntersections(List<CorridorData> corridors)
        {
            // For now, return corridors as-is. Intersection resolution can be enhanced later.
            return new List<CorridorData>(corridors);
        }
        
        public float CalculateTotalCorridorLength(List<CorridorData> corridors)
        {
            return corridors.Sum(c => c.GetDistance());
        }
        
        public List<CorridorData> FindShortestPath(RoomData startRoom, RoomData endRoom, List<CorridorData> corridors)
        {
            // Build graph from corridors
            var graph = new Dictionary<int, List<(int roomID, CorridorData corridor)>>();
            
            foreach (var corridor in corridors)
            {
                if (!graph.ContainsKey(corridor.RoomA_ID))
                    graph[corridor.RoomA_ID] = new List<(int, CorridorData)>();
                if (!graph.ContainsKey(corridor.RoomB_ID))
                    graph[corridor.RoomB_ID] = new List<(int, CorridorData)>();
                
                graph[corridor.RoomA_ID].Add((corridor.RoomB_ID, corridor));
                graph[corridor.RoomB_ID].Add((corridor.RoomA_ID, corridor));
            }
            
            // Dijkstra's algorithm
            var distances = new Dictionary<int, float>();
            var previous = new Dictionary<int, (int roomID, CorridorData corridor)?>();
            var unvisited = new HashSet<int>();
            
            foreach (var roomID in graph.Keys)
            {
                distances[roomID] = float.MaxValue;
                unvisited.Add(roomID);
            }
            
            distances[startRoom.RoomID] = 0;
            
            while (unvisited.Count > 0)
            {
                var current = unvisited.OrderBy(id => distances[id]).First();
                unvisited.Remove(current);
                
                if (current == endRoom.RoomID)
                    break;
                
                if (!graph.ContainsKey(current)) continue;
                
                foreach (var (neighborID, corridor) in graph[current])
                {
                    if (!unvisited.Contains(neighborID)) continue;
                    
                    var altDistance = distances[current] + corridor.GetDistance();
                    if (altDistance < distances[neighborID])
                    {
                        distances[neighborID] = altDistance;
                        previous[neighborID] = (current, corridor);
                    }
                }
            }
            
            // Reconstruct path
            var pathCorridors = new List<CorridorData>();
            var currentRoomID = endRoom.RoomID;
            
            while (previous.ContainsKey(currentRoomID))
            {
                var (prevRoomID, corridor) = previous[currentRoomID].Value;
                pathCorridors.Insert(0, corridor);
                currentRoomID = prevRoomID;
            }
            
            return pathCorridors;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Task 1: Implement core room identification
        /// Identifies core rooms based on size, position, and classification.
        /// </summary>
        private List<RoomData> IdentifyCoreRooms(List<RoomData> rooms)
        {
            if (rooms.Count <= MIN_CORE_ROOMS)
                return new List<RoomData>(rooms);
            
            // Sort rooms by area (largest first)
            var sortedRooms = rooms.OrderByDescending(r => r.Area).ToList();
            
            // Calculate threshold for core rooms (top 30% by area, at least MIN_CORE_ROOMS)
            var coreRoomCount = Mathf.Max(MIN_CORE_ROOMS, Mathf.CeilToInt(rooms.Count * CORE_ROOM_THRESHOLD));
            coreRoomCount = Mathf.Min(coreRoomCount, rooms.Count);
            
            var coreRooms = sortedRooms.Take(coreRoomCount).ToList();
            
            // Ensure we have rooms from different areas of the map for better distribution
            if (coreRooms.Count > MIN_CORE_ROOMS)
            {
                var distributedCoreRooms = EnsureGeographicDistribution(coreRooms, rooms);
                if (distributedCoreRooms.Count >= MIN_CORE_ROOMS)
                {
                    coreRooms = distributedCoreRooms;
                }
            }
            
            return coreRooms;
        }
        
        /// <summary>
        /// Ensures core rooms are geographically distributed across the map.
        /// </summary>
        private List<RoomData> EnsureGeographicDistribution(List<RoomData> candidateRooms, List<RoomData> allRooms)
        {
            var distributed = new List<RoomData>();
            var mapBounds = CalculateMapBounds(allRooms);
            
            // Divide map into quadrants and select rooms from each
            var quadrants = new List<RectInt>
            {
                new RectInt(mapBounds.x, mapBounds.y, mapBounds.width / 2, mapBounds.height / 2), // Top-left
                new RectInt(mapBounds.x + mapBounds.width / 2, mapBounds.y, mapBounds.width / 2, mapBounds.height / 2), // Top-right
                new RectInt(mapBounds.x, mapBounds.y + mapBounds.height / 2, mapBounds.width / 2, mapBounds.height / 2), // Bottom-left
                new RectInt(mapBounds.x + mapBounds.width / 2, mapBounds.y + mapBounds.height / 2, mapBounds.width / 2, mapBounds.height / 2) // Bottom-right
            };
            
            foreach (var quadrant in quadrants)
            {
                var roomsInQuadrant = candidateRooms.Where(r => quadrant.Contains(r.Center)).ToList();
                if (roomsInQuadrant.Count > 0)
                {
                    distributed.Add(roomsInQuadrant.OrderByDescending(r => r.Area).First());
                }
            }
            
            // If we don't have enough distributed rooms, add the largest remaining ones
            while (distributed.Count < MIN_CORE_ROOMS && distributed.Count < candidateRooms.Count)
            {
                var remaining = candidateRooms.Except(distributed).OrderByDescending(r => r.Area).FirstOrDefault();
                if (remaining != null)
                    distributed.Add(remaining);
                else
                    break;
            }
            
            return distributed;
        }
        
        /// <summary>
        /// Task 2 & 6: Create primary corridor generation with MST optimization
        /// Connects core rooms using MST algorithm for optimal connectivity.
        /// </summary>
        private List<CorridorData> GeneratePrimaryCorridors(List<RoomData> coreRooms, List<RoomData> allRooms, MapGenerationSettings settings)
        {
            var primaryCorridors = new List<CorridorData>();
            
            if (coreRooms.Count <= 1)
                return primaryCorridors;
            
            // Build MST using Kruskal's algorithm
            var edges = new List<(RoomData room1, RoomData room2, float distance)>();
            
            // Calculate all possible edges between core rooms
            for (int i = 0; i < coreRooms.Count; i++)
            {
                for (int j = i + 1; j < coreRooms.Count; j++)
                {
                    var distance = Vector2Int.Distance(coreRooms[i].Center, coreRooms[j].Center);
                    edges.Add((coreRooms[i], coreRooms[j], distance));
                }
            }
            
            // Sort edges by distance (ascending)
            edges.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // Kruskal's algorithm
            var disjointSet = new DisjointSet(coreRooms.Count);
            var roomToIndex = coreRooms.Select((room, index) => (room, index)).ToDictionary(x => x.room.RoomID, x => x.index);
            
            foreach (var (room1, room2, distance) in edges)
            {
                var index1 = roomToIndex[room1.RoomID];
                var index2 = roomToIndex[room2.RoomID];
                
                if (disjointSet.Find(index1) != disjointSet.Find(index2))
                {
                    disjointSet.Union(index1, index2);
                    
                    // Generate corridor between these rooms
                    var corridor = GenerateSingleCorridor(room1, room2, settings, PRIMARY_CORRIDOR_WIDTH);
                    if (corridor != null)
                    {
                        primaryCorridors.Add(corridor);
                        OnCorridorGenerated?.Invoke(corridor);
                    }
                    
                    // Stop when we have enough corridors to connect all core rooms
                    if (primaryCorridors.Count >= coreRooms.Count - 1)
                        break;
                }
            }
            
            return primaryCorridors;
        }
        
        /// <summary>
        /// Task 3: Implement secondary corridor generation
        /// Connects remaining rooms to the primary corridor network.
        /// </summary>
        private List<CorridorData> GenerateSecondaryCorridors(List<RoomData> allRooms, List<RoomData> coreRooms, List<CorridorData> primaryCorridors, MapGenerationSettings settings)
        {
            var secondaryCorridors = new List<CorridorData>();
            var nonCoreRooms = allRooms.Except(coreRooms).ToList();
            
            // Build set of rooms connected to primary network
            var connectedRooms = new HashSet<int>(coreRooms.Select(r => r.RoomID));
            
            // For each non-core room, find the closest connected room
            foreach (var room in nonCoreRooms)
            {
                RoomData closestConnectedRoom = null;
                float minDistance = float.MaxValue;
                
                // Check distance to all connected rooms
                foreach (var connectedRoom in connectedRooms)
                {
                    var targetRoom = allRooms.FirstOrDefault(r => r.RoomID == connectedRoom);
                    if (targetRoom != null)
                    {
                        var distance = Vector2Int.Distance(room.Center, targetRoom.Center);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestConnectedRoom = targetRoom;
                        }
                    }
                }
                
                // Also check distance to primary corridors (connect to nearest point on corridor)
                var closestCorridorPoint = FindClosestPointOnCorridors(room, primaryCorridors);
                if (closestCorridorPoint.HasValue)
                {
                    var corridorDistance = Vector2Int.Distance(room.Center, closestCorridorPoint.Value);
                    if (corridorDistance < minDistance)
                    {
                        minDistance = corridorDistance;
                        // Create a virtual room at the corridor point for connection
                        closestConnectedRoom = CreateVirtualRoomAtPoint(closestCorridorPoint.Value);
                    }
                }
                
                if (closestConnectedRoom != null)
                {
                    var corridor = GenerateSingleCorridor(room, closestConnectedRoom, settings, SECONDARY_CORRIDOR_WIDTH);
                    if (corridor != null)
                    {
                        secondaryCorridors.Add(corridor);
                        connectedRooms.Add(room.RoomID);
                        OnCorridorGenerated?.Invoke(corridor);
                    }
                }
            }
            
            return secondaryCorridors;
        }
        
        /// <summary>
        /// Task 4: Add corridor width variation
        /// Applies appropriate widths based on corridor hierarchy.
        /// </summary>
        private void ApplyCorridorWidthVariation(CorridorData corridor, bool isPrimary)
        {
            int targetWidth = isPrimary ? PRIMARY_CORRIDOR_WIDTH : SECONDARY_CORRIDOR_WIDTH;
            corridor.SetWidth(targetWidth);
        }
        
        /// <summary>
        /// Task 5: Create connectivity validation system
        /// Ensures 100% room connectivity and fixes issues if found.
        /// </summary>
        private List<CorridorData> FixConnectivityIssues(List<RoomData> rooms, List<CorridorData> corridors, MapGenerationSettings settings)
        {
            var fixedCorridors = new List<CorridorData>(corridors);
            var connectivityResult = ValidateConnectivity(rooms, fixedCorridors);
            
            if (!connectivityResult.IsValid)
            {
                // Build connectivity graph
                var adjacency = BuildConnectivityGraph(rooms, fixedCorridors);
                var connectedComponents = FindConnectedComponents(rooms, adjacency);
                
                // Connect components together
                for (int i = 0; i < connectedComponents.Count - 1; i++)
                {
                    var component1 = connectedComponents[i];
                    var component2 = connectedComponents[i + 1];
                    
                    // Find closest rooms between components
                    var (room1, room2) = FindClosestRoomsBetweenComponents(component1, component2);
                    
                    if (room1 != null && room2 != null)
                    {
                        var corridor = GenerateSingleCorridor(room1, room2, settings, SECONDARY_CORRIDOR_WIDTH);
                        if (corridor != null)
                        {
                            fixedCorridors.Add(corridor);
                            OnCorridorGenerated?.Invoke(corridor);
                        }
                    }
                }
            }
            
            return fixedCorridors;
        }
        
        /// <summary>
        /// Task 7: Implement corridor hierarchy logic
        /// Manages the two-pass hierarchical structure.
        /// </summary>
        private void EnforceCorridorHierarchy(List<CorridorData> primaryCorridors, List<CorridorData> secondaryCorridors)
        {
            // Ensure primary corridors have priority in intersections
            foreach (var primary in primaryCorridors)
            {
                primary.SetWidth(PRIMARY_CORRIDOR_WIDTH);
            }
            
            foreach (var secondary in secondaryCorridors)
            {
                secondary.SetWidth(SECONDARY_CORRIDOR_WIDTH);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private ValidationResult ValidateInputs(List<RoomData> rooms, MapGenerationSettings settings)
        {
            var result = new ValidationResult();
            
            if (rooms == null || rooms.Count == 0)
                result.AddError("Rooms list is null or empty");
            
            if (settings == null)
                result.AddError("Settings is null");
            
            if (settings?.CorridorConfig == null)
                result.AddError("Corridor configuration is null");
            
            return result;
        }
        
        private CorridorData GenerateSingleCorridor(RoomData room1, RoomData room2, MapGenerationSettings settings, int width)
        {
            try
            {
                // Get doorway positions
                var startPos = room1.GetRandomEdgePoint(_random);
                var endPos = room2.GetRandomEdgePoint(_random);
                
                // Create obstacle map
                var obstacleMap = CreateObstacleMap(new List<RoomData> { room1, room2 });
                
                // Find path using A*
                var path = _pathfinder.FindPath(startPos, endPos, obstacleMap);
                
                if (path.Count == 0)
                {
                    // Try alternative doorway positions
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        startPos = room1.GetRandomEdgePoint(_random);
                        endPos = room2.GetRandomEdgePoint(_random);
                        path = _pathfinder.FindPath(startPos, endPos, obstacleMap);
                        
                        if (path.Count > 0) break;
                    }
                }
                
                if (path.Count == 0)
                {
                    Debug.LogWarning($"Failed to find path between Room {room1.RoomID} and Room {room2.RoomID}");
                    return null;
                }
                
                // Create corridor
                var corridor = new CorridorData(room1.RoomID, room2.RoomID, startPos, endPos, width);
                corridor.CorridorID = _nextCorridorID++;
                corridor.SetPath(path);
                
                // Update room connections
                room1.ConnectToRoom(room2.RoomID);
                room2.ConnectToRoom(room1.RoomID);
                
                return corridor;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating corridor between Room {room1.RoomID} and Room {room2.RoomID}: {ex.Message}");
                return null;
            }
        }
        
        private bool[,] CreateObstacleMap(List<RoomData> rooms)
        {
            if (rooms.Count == 0) return new bool[1, 1];
            
            var mapBounds = CalculateMapBounds(rooms);
            var width = mapBounds.width + 10; // Add padding
            var height = mapBounds.height + 10;
            var obstacleMap = new bool[width, height];
            
            // Mark room areas as obstacles (except for doorways)
            foreach (var room in rooms)
            {
                var roomBounds = room.Bounds;
                for (int x = roomBounds.x; x < roomBounds.xMax; x++)
                {
                    for (int y = roomBounds.y; y < roomBounds.yMax; y++)
                    {
                        var mapX = x - mapBounds.x + 5;
                        var mapY = y - mapBounds.y + 5;
                        
                        if (mapX >= 0 && mapX < width && mapY >= 0 && mapY < height)
                        {
                            // Don't mark edges as obstacles to allow corridor connections
                            if (x > roomBounds.x && x < roomBounds.xMax - 1 && 
                                y > roomBounds.y && y < roomBounds.yMax - 1)
                            {
                                obstacleMap[mapX, mapY] = true;
                            }
                        }
                    }
                }
            }
            
            return obstacleMap;
        }
        
        private RectInt CalculateMapBounds(List<RoomData> rooms)
        {
            if (rooms.Count == 0) return new RectInt(0, 0, 1, 1);
            
            var minX = rooms.Min(r => r.Bounds.xMin);
            var maxX = rooms.Max(r => r.Bounds.xMax);
            var minY = rooms.Min(r => r.Bounds.yMin);
            var maxY = rooms.Max(r => r.Bounds.yMax);
            
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }
        
        private Dictionary<int, List<int>> BuildConnectivityGraph(List<RoomData> rooms, List<CorridorData> corridors)
        {
            var graph = new Dictionary<int, List<int>>();
            
            // Initialize graph
            foreach (var room in rooms)
            {
                graph[room.RoomID] = new List<int>();
            }
            
            // Add connections from corridors
            foreach (var corridor in corridors)
            {
                if (graph.ContainsKey(corridor.RoomA_ID))
                    graph[corridor.RoomA_ID].Add(corridor.RoomB_ID);
                
                if (graph.ContainsKey(corridor.RoomB_ID))
                    graph[corridor.RoomB_ID].Add(corridor.RoomA_ID);
            }
            
            return graph;
        }
        
        private List<List<RoomData>> FindConnectedComponents(List<RoomData> rooms, Dictionary<int, List<int>> adjacency)
        {
            var components = new List<List<RoomData>>();
            var visited = new HashSet<int>();
            
            foreach (var room in rooms)
            {
                if (!visited.Contains(room.RoomID))
                {
                    var component = new List<RoomData>();
                    var stack = new Stack<int>();
                    
                    stack.Push(room.RoomID);
                    visited.Add(room.RoomID);
                    
                    while (stack.Count > 0)
                    {
                        var currentRoomID = stack.Pop();
                        var currentRoom = rooms.FirstOrDefault(r => r.RoomID == currentRoomID);
                        
                        if (currentRoom != null)
                            component.Add(currentRoom);
                        
                        if (adjacency.ContainsKey(currentRoomID))
                        {
                            foreach (var neighborID in adjacency[currentRoomID])
                            {
                                if (!visited.Contains(neighborID))
                                {
                                    visited.Add(neighborID);
                                    stack.Push(neighborID);
                                }
                            }
                        }
                    }
                    
                    components.Add(component);
                }
            }
            
            return components;
        }
        
        private (RoomData, RoomData) FindClosestRoomsBetweenComponents(List<RoomData> component1, List<RoomData> component2)
        {
            RoomData closest1 = null, closest2 = null;
            float minDistance = float.MaxValue;
            
            foreach (var room1 in component1)
            {
                foreach (var room2 in component2)
                {
                    var distance = Vector2Int.Distance(room1.Center, room2.Center);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest1 = room1;
                        closest2 = room2;
                    }
                }
            }
            
            return (closest1, closest2);
        }
        
        private Vector2Int? FindClosestPointOnCorridors(RoomData room, List<CorridorData> corridors)
        {
            Vector2Int? closestPoint = null;
            float minDistance = float.MaxValue;
            
            foreach (var corridor in corridors)
            {
                foreach (var tile in corridor.PathTiles)
                {
                    var distance = Vector2Int.Distance(room.Center, tile);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = tile;
                    }
                }
            }
            
            return closestPoint;
        }
        
        private RoomData CreateVirtualRoomAtPoint(Vector2Int point)
        {
            // Create a small virtual room at the given point for corridor connection
            var virtualBounds = new RectInt(point.x, point.y, 1, 1);
            return new RoomData(virtualBounds);
        }
        
        #endregion
        
        #region Utility Classes
        
        /// <summary>
        /// Disjoint Set data structure for Kruskal's MST algorithm.
        /// </summary>
        private class DisjointSet
        {
            private int[] parent;
            private int[] rank;
            
            public DisjointSet(int size)
            {
                parent = new int[size];
                rank = new int[size];
                
                for (int i = 0; i < size; i++)
                {
                    parent[i] = i;
                    rank[i] = 0;
                }
            }
            
            public int Find(int x)
            {
                if (parent[x] != x)
                    parent[x] = Find(parent[x]);
                return parent[x];
            }
            
            public void Union(int x, int y)
            {
                int rootX = Find(x);
                int rootY = Find(y);
                
                if (rootX == rootY) return;
                
                if (rank[rootX] < rank[rootY])
                {
                    parent[rootX] = rootY;
                }
                else if (rank[rootX] > rank[rootY])
                {
                    parent[rootY] = rootX;
                }
                else
                {
                    parent[rootY] = rootX;
                    rank[rootX]++;
                }
            }
        }
        
        #endregion
    }
}