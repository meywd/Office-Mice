using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Content;
using OfficeMice.MapGeneration.Configuration;

namespace OfficeMice.MapGeneration.Tests.PlayMode
{
    [TestFixture]
    public class SpawnPointIntegrationTests
    {
        private SpawnPointManager _spawnPointManager;
        private SpawnPointWaveSpawnerIntegration _waveSpawnerIntegration;
        private SpawnTableConfiguration _testSpawnTableConfig;
        private GameObject _testParent;

        [SetUp]
        public void SetUp()
        {
            _testParent = new GameObject("TestParent");
            _testSpawnTableConfig = CreateTestSpawnTableConfig();
            _spawnPointManager = new SpawnPointManager(_testSpawnTableConfig, 42);
            _waveSpawnerIntegration = new SpawnPointWaveSpawnerIntegration(_testSpawnTableConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _waveSpawnerIntegration?.DestroyAllSpawnPoints();
            
            if (_testParent != null)
            {
                Object.DestroyImmediate(_testParent);
            }
            
            _spawnPointManager = null;
            _waveSpawnerIntegration = null;
            _testSpawnTableConfig = null;
        }

        [UnityTest]
        public IEnumerator CreateSpawnPointGameObjects_WithValidData_CreatesGameObjects()
        {
            // Arrange
            var spawnPoints = CreateTestSpawnPoints();
            var tilemapOffset = Vector3.zero;

            // Act
            var createdObjects = _waveSpawnerIntegration.CreateSpawnPointGameObjects(spawnPoints, tilemapOffset);

            yield return null;

            // Assert
            Assert.IsNotNull(createdObjects, "Should return object list");
            Assert.AreEqual(spawnPoints.Count, createdObjects.Count, "Should create GameObject for each spawn point");

            // Verify GameObject properties
            for (int i = 0; i < createdObjects.Count; i++)
            {
                var obj = createdObjects[i];
                var spawnPoint = spawnPoints[i];

                Assert.IsNotNull(obj, $"GameObject {i} should not be null");
                Assert.AreEqual("Spawn Point", obj.tag, $"GameObject {i} should have 'Spawn Point' tag");
                
                var component = obj.GetComponent<GeneratedSpawnPointComponent>();
                Assert.IsNotNull(component, $"GameObject {i} should have GeneratedSpawnPointComponent");
                
                Assert.AreEqual(spawnPoint.RoomID, component.RoomID, 
                    $"Component {i} should have correct room ID");
                Assert.AreEqual(spawnPoint.Position, component.Position, 
                    $"Component {i} should have correct position");
                Assert.AreEqual(spawnPoint.EnemyType, component.EnemyType, 
                    $"Component {i} should have correct enemy type");
            }
        }

        [UnityTest]
        public IEnumerator CreateSingleSpawnPoint_WithValidData_CreatesCorrectGameObject()
        {
            // Arrange
            var spawnPoint = new SpawnPointData(1, new Vector2Int(5, 5), "Mouse", 1.0f);
            var tilemapOffset = new Vector3(10, 10, 0);

            // Act
            var spawnObject = _waveSpawnerIntegration.CreateSingleSpawnPoint(spawnPoint, tilemapOffset);

            yield return null;

            // Assert
            Assert.IsNotNull(spawnObject, "Should create spawn point GameObject");
            Assert.AreEqual("Spawn Point", spawnObject.tag, "Should have correct tag");
            
            var expectedPosition = tilemapOffset + new Vector3(5.5f, 5.5f, 0);
            Assert.AreEqual(expectedPosition, spawnObject.transform.position, "Should be at correct world position");
            
            var component = spawnObject.GetComponent<GeneratedSpawnPointComponent>();
            Assert.IsNotNull(component, "Should have GeneratedSpawnPointComponent");
            Assert.AreEqual(spawnPoint.RoomID, component.RoomID);
            Assert.AreEqual(spawnPoint.Position, component.Position);
            Assert.AreEqual(spawnPoint.EnemyType, component.EnemyType);
            Assert.AreEqual(spawnPoint.SpawnDelay, component.SpawnDelay);
        }

        [UnityTest]
        public IEnumerator DestroyAllSpawnPoints_AfterCreation_RemovesAllObjects()
        {
            // Arrange
            var spawnPoints = CreateTestSpawnPoints();
            var createdObjects = _waveSpawnerIntegration.CreateSpawnPointGameObjects(spawnPoints);
            yield return null;

            // Verify objects exist
            Assert.AreEqual(spawnPoints.Count, createdObjects.Count, "Should have created spawn point objects");

            // Act
            _waveSpawnerIntegration.DestroyAllSpawnPoints();
            yield return null;

            // Assert
            var remainingObjects = GameObject.FindGameObjectsWithTag("Spawn Point");
            Assert.AreEqual(0, remainingObjects.Length, "Should destroy all spawn point objects");
        }

        [UnityTest]
        public IEnumerator UpdateSpawnPoints_WithNewData_UpdatesCorrectly()
        {
            // Arrange
            var initialSpawnPoints = CreateTestSpawnPoints();
            _waveSpawnerIntegration.CreateSpawnPointGameObjects(initialSpawnPoints);
            yield return null;

            var initialObjects = GameObject.FindGameObjectsWithTag("Spawn Point");
            Assert.AreEqual(initialSpawnPoints.Count, initialObjects.Length, "Should have initial objects");

            // Act
            var updatedSpawnPoints = CreateUpdatedSpawnPoints();
            _waveSpawnerIntegration.UpdateSpawnPoints(updatedSpawnPoints);
            yield return null;

            // Assert
            var finalObjects = GameObject.FindGameObjectsWithTag("Spawn Point");
            Assert.AreEqual(updatedSpawnPoints.Count, finalObjects.Length, "Should have updated object count");

            // Verify updated data
            foreach (var spawnPoint in updatedSpawnPoints)
            {
                var foundObject = System.Array.Find(finalObjects, obj => 
                obj.GetComponent<GeneratedSpawnPointComponent>()?.Position.Equals(spawnPoint.Position) ?? false);
                
                Assert.IsNotNull(foundObject, $"Should find object for spawn point {spawnPoint.Position}");
                
                var component = foundObject.GetComponent<GeneratedSpawnPointComponent>();
                Assert.AreEqual(spawnPoint.EnemyType, component.EnemyType, 
                    $"Should have updated enemy type for {spawnPoint.Position}");
            }
        }

        [UnityTest]
        public IEnumerator ValidateWaveSpawnerIntegration_WithValidIntegration_ReturnsSuccess()
        {
            // Arrange
            var spawnPoints = CreateTestSpawnPoints();
            _waveSpawnerIntegration.CreateSpawnPointGameObjects(spawnPoints);
            yield return null;

            // Act
            var validationResult = _waveSpawnerIntegration.ValidateWaveSpawnerIntegration(spawnPoints);

            // Assert
            Assert.IsNotNull(validationResult, "Should return validation result");
            Assert.IsTrue(validationResult.IsValid, "Valid integration should pass validation");
        }

        [UnityTest]
        public IEnumerator ValidateWaveSpawnerIntegration_WithMissingObjects_ReturnsError()
        {
            // Arrange
            var spawnPoints = CreateTestSpawnPoints();
            // Don't create GameObjects - simulate missing objects

            // Act
            var validationResult = _waveSpawnerIntegration.ValidateWaveSpawnerIntegration(spawnPoints);

            // Assert
            Assert.IsNotNull(validationResult, "Should return validation result");
            Assert.IsFalse(validationResult.IsValid, "Missing objects should cause validation failure");
            Assert.IsTrue(validationResult.Errors.Count > 0, "Should have specific errors about missing objects");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnPointManager_WithRealFurniture_PlacesStrategically()
        {
            // Arrange
            var map = CreateTestMap();
            var furniture = CreateTestFurniture();

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);

            yield return null;

            // Assert
            Assert.IsNotNull(spawnPoints, "Should return spawn points");
            Assert.IsTrue(spawnPoints.Count > 0, "Should place spawn points");

            // Verify strategic positioning
            foreach (var room in map.Rooms)
            {
                var roomSpawnPoints = spawnPoints.Where(sp => sp.RoomID == room.RoomID).ToList();
                if (roomSpawnPoints.Count > 0)
                {
                    // Should have variety in positioning
                    var positions = roomSpawnPoints.Select(sp => sp.Position).ToList();
                    var hasCorner = positions.Any(pos => IsCornerPosition(pos, room));
                    var hasNearDoorway = positions.Any(pos => IsNearDoorway(pos, room));
                    
                    Assert.IsTrue(hasCorner || hasNearDoorway, 
                        $"Room {room.RoomID} should have strategic spawn point positioning");
                }
            }
        }

        [UnityTest]
        public IEnumerator SpawnPointGeneration_Performance_InPlayMode()
        {
            // Arrange
            var map = CreateLargeTestMap();
            var furniture = CreateTestFurnitureForLargeMap();
            var targetTimeMs = 150;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);
            stopwatch.Stop();

            yield return null;

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"PlayMode spawn point generation should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(spawnPoints.Count > 0, "Should place spawn points in large map");
        }

        [UnityTest]
        public IEnumerator WaveSpawnerIntegration_Performance_InPlayMode()
        {
            // Arrange
            var spawnPoints = CreateManyTestSpawnPoints(50);
            var targetTimeMs = 50;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var createdObjects = _waveSpawnerIntegration.CreateSpawnPointGameObjects(spawnPoints);
            stopwatch.Stop();

            yield return null;

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, targetTimeMs, 
                $"PlayMode WaveSpawner integration should complete within {targetTimeMs}ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.AreEqual(spawnPoints.Count, createdObjects.Count, "Should create all spawn point objects");
        }

        [UnityTest]
        public IEnumerator SpawnPointEvents_FireCorrectly_InPlayMode()
        {
            // Arrange
            var map = CreateTestMap();
            var furniture = CreateTestFurniture();
            var eventCount = 0;
            SpawnPointData lastSpawnPoint = null;

            _spawnPointManager.OnSpawnPointPlaced += (spawnPoint) =>
            {
                eventCount++;
                lastSpawnPoint = spawnPoint;
            };

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);

            yield return null;

            // Assert
            Assert.IsTrue(eventCount > 0, "Should fire spawn point placement events");
            Assert.IsNotNull(lastSpawnPoint, "Last event should have spawn point data");
            Assert.AreEqual(eventCount, spawnPoints.Count, "Event count should match placed spawn points");
        }

        [UnityTest]
        public IEnumerator MemoryUsage_StaysWithinLimits_InPlayMode()
        {
            // Arrange
            var map = CreateLargeTestMap();
            var furniture = CreateTestFurnitureForLargeMap();

            // Force garbage collection to get baseline
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var initialMemory = System.GC.GetTotalMemory(false);

            // Act
            var spawnPoints = _spawnPointManager.PlaceSpawnPoints(map, furniture);
            var createdObjects = _waveSpawnerIntegration.CreateSpawnPointGameObjects(spawnPoints);

            yield return null;

            var finalMemory = System.GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.Less(memoryIncrease, 10 * 1024 * 1024, // 10MB limit for PlayMode
                $"PlayMode spawn point system should use less than 10MB memory, used {memoryIncrease / (1024f * 1024f):F2}MB");
        }

        #region Helper Methods

        private List<SpawnPointData> CreateTestSpawnPoints()
        {
            return new List<SpawnPointData>
            {
                new SpawnPointData(1, new Vector2Int(5, 5), "Mouse", 0.5f),
                new SpawnPointData(1, new Vector2Int(10, 8), "Rat", 1.0f),
                new SpawnPointData(2, new Vector2Int(15, 12), "Bug", 0.8f),
                new SpawnPointData(2, new Vector2Int(20, 15), "Mouse", 1.2f)
            };
        }

        private List<SpawnPointData> CreateUpdatedSpawnPoints()
        {
            return new List<SpawnPointData>
            {
                new SpawnPointData(1, new Vector2Int(6, 6), "Rat", 0.7f), // Updated enemy type
                new SpawnPointData(1, new Vector2Int(11, 9), "Mouse", 1.1f), // Updated position
                new SpawnPointData(2, new Vector2Int(16, 13), "Bug", 0.9f),
                new SpawnPointData(3, new Vector2Int(25, 20), "Mouse", 0.6f) // New spawn point
            };
        }

        private List<SpawnPointData> CreateManyTestSpawnPoints(int count)
        {
            var spawnPoints = new List<SpawnPointData>();
            var enemyTypes = new[] { "Mouse", "Rat", "Bug" };
            var random = new System.Random(42);

            for (int i = 0; i < count; i++)
            {
                var roomID = (i % 5) + 1;
                var position = new Vector2Int(random.Next(5, 25), random.Next(5, 25));
                var enemyType = enemyTypes[random.Next(enemyTypes.Length)];
                var spawnDelay = random.Next(0, 3);

                spawnPoints.Add(new SpawnPointData(roomID, position, enemyType, spawnDelay));
            }

            return spawnPoints;
        }

        private MapData CreateTestMap()
        {
            var map = new MapData(12345, new Vector2Int(100, 100));

            var room1 = new RoomData(new RectInt(10, 10, 8, 6)) { RoomID = 1 };
            room1.SetClassification(RoomClassification.Office);
            room1.AddDoorway(new DoorwayPosition(new Vector2Int(14, 10), 1, DoorwayDirection.South));

            var room2 = new RoomData(new RectInt(30, 10, 12, 8)) { RoomID = 2 };
            room2.SetClassification(RoomClassification.Conference);
            room2.AddDoorway(new DoorwayPosition(new Vector2Int(36, 10), 2, DoorwayDirection.South));

            var room3 = new RoomData(new RectInt(10, 30, 6, 6)) { RoomID = 3 };
            room3.SetClassification(RoomClassification.BreakRoom);
            room3.AddDoorway(new DoorwayPosition(new Vector2Int(13, 30), 1, DoorwayDirection.North));

            map.AddRoom(room1);
            map.AddRoom(room2);
            map.AddRoom(room3);

            return map;
        }

        private MapData CreateLargeTestMap()
        {
            var map = new MapData(12345, new Vector2Int(200, 200));
            var roomId = 1;

            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var x = i * 15;
                    var y = j * 15;
                    var width = UnityEngine.Random.Range(6, 12);
                    var height = UnityEngine.Random.Range(6, 12);
                    
                    var room = new RoomData(new RectInt(x, y, width, height));
                    room.RoomID = roomId++;
                    
                    var classifications = new[] 
                    { 
                        RoomClassification.Office, 
                        RoomClassification.Conference, 
                        RoomClassification.BreakRoom,
                        RoomClassification.Storage,
                        RoomClassification.ServerRoom
                    };
                    room.SetClassification(classifications[UnityEngine.Random.Range(0, classifications.Length)]);
                    
                    map.AddRoom(room);
                }
            }

            return map;
        }

        private List<FurnitureData> CreateTestFurniture()
        {
            var furniture1 = new FurnitureData("desk1", "Desk", "path", 1, new Vector2Int(12, 12), new Vector2Int(2, 2));
            furniture1.SetCollisionProperties(true, true);

            var furniture2 = new FurnitureData("table1", "Table", "path", 2, new Vector2Int(35, 15), new Vector2Int(3, 2));
            furniture2.SetCollisionProperties(true, false);

            var furniture3 = new FurnitureData("shelf1", "Shelf", "path", 3, new Vector2Int(12, 32), new Vector2Int(1, 3));
            furniture3.SetCollisionProperties(true, true);

            return new List<FurnitureData> { furniture1, furniture2, furniture3 };
        }

        private List<FurnitureData> CreateTestFurnitureForLargeMap()
        {
            var furniture = new List<FurnitureData>();
            
            for (int i = 0; i < 60; i++)
            {
                var roomID = (i % 20) + 1;
                var x = (i % 10) * 2 + 10;
                var y = (i / 10) * 2 + 10;
                var width = UnityEngine.Random.Range(1, 3);
                var height = UnityEngine.Random.Range(1, 3);
                
                var furn = new FurnitureData($"furn_{i}", "Desk", "path", 
                    roomID, new Vector2Int(x, y), new Vector2Int(width, height));
                furn.SetBlockingProperties(true, UnityEngine.Random.value < 0.5f);
                
                furniture.Add(furn);
            }
            
            return furniture;
        }

        private bool IsCornerPosition(Vector2Int position, RoomData room)
        {
            var bounds = room.Bounds;
            var offset = 1;
            
            var corners = new[]
            {
                new Vector2Int(bounds.x + offset, bounds.y + offset),
                new Vector2Int(bounds.xMax - offset - 1, bounds.y + offset),
                new Vector2Int(bounds.x + offset, bounds.yMax - offset - 1),
                new Vector2Int(bounds.xMax - offset - 1, bounds.yMax - offset - 1)
            };
            
            return corners.Any(corner => corner == position);
        }

        private bool IsNearDoorway(Vector2Int position, RoomData room)
        {
            foreach (var doorway in room.Doorways)
            {
                if (Vector2Int.Distance(position, doorway.position) <= 4)
                    return true;
            }
            return false;
        }

        private SpawnTableConfiguration CreateTestSpawnTableConfig()
        {
            var config = ScriptableObject.CreateInstance<SpawnTableConfiguration>();
            return config;
        }

        #endregion
    }
}