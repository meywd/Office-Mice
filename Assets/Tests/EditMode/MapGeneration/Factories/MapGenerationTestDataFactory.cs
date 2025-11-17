using System;
using System.Collections.Generic;
using UnityEngine;
using OfficeMice.MapGeneration.Configuration;
using OfficeMice.MapGeneration.Data;

namespace OfficeMice.MapGeneration.Factories
{
    /// <summary>
    /// Factory class for creating test data with reproducible, deterministic scenarios.
    /// Uses seed-based generation for consistent test results across runs.
    /// </summary>
    public static class MapGenerationTestDataFactory
    {
        private static readonly Dictionary<string, Func<int, MapGenerationSettings>> _settingsCreators;
        private static readonly Dictionary<string, Func<int, MapData>> _mapDataCreators;

        static MapGenerationTestDataFactory()
        {
            _settingsCreators = new Dictionary<string, Func<int, MapGenerationSettings>>
            {
                ["minimal"] = CreateMinimalSettings,
                ["standard"] = CreateStandardSettings,
                ["complex"] = CreateComplexSettings,
                ["performance"] = CreatePerformanceTestSettings,
                ["edge_case"] = CreateEdgeCaseSettings
            };

            _mapDataCreators = new Dictionary<string, Func<int, MapData>>
            {
                ["empty"] = CreateEmptyMapData,
                ["single_room"] = CreateSingleRoomMapData,
                ["multiple_rooms"] = CreateMultipleRoomsMapData,
                ["complex_layout"] = CreateComplexLayoutMapData,
                ["corrupted"] = CreateCorruptedMapData
            };
        }

        /// <summary>
        /// Creates MapGenerationSettings based on the specified scenario type and seed.
        /// </summary>
        /// <param name="scenarioType">Type of settings scenario (minimal, standard, complex, performance, edge_case)</param>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>Configured MapGenerationSettings</returns>
        public static MapGenerationSettings CreateSettings(string scenarioType, int seed = 0)
        {
            if (!_settingsCreators.TryGetValue(scenarioType, out var creator))
            {
                throw new ArgumentException($"Unknown scenario type: {scenarioType}. Available types: {string.Join(", ", _settingsCreators.Keys)}");
            }

            return creator(seed);
        }

        /// <summary>
        /// Creates MapData based on the specified scenario type and seed.
        /// </summary>
        /// <param name="scenarioType">Type of map scenario (empty, single_room, multiple_rooms, complex_layout, corrupted)</param>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>Configured MapData</returns>
        public static MapData CreateMapData(string scenarioType, int seed = 0)
        {
            if (!_mapDataCreators.TryGetValue(scenarioType, out var creator))
            {
                throw new ArgumentException($"Unknown scenario type: {scenarioType}. Available types: {string.Join(", ", _mapDataCreators.Keys)}");
            }

            return creator(seed);
        }

        /// <summary>
        /// Creates a collection of test settings for comprehensive testing.
        /// </summary>
        /// <param name="baseSeed">Base seed for all generated settings</param>
        /// <returns>Dictionary of scenario types to MapGenerationSettings</returns>
        public static Dictionary<string, MapGenerationSettings> CreateAllTestSettings(int baseSeed = 12345)
        {
            var settings = new Dictionary<string, MapGenerationSettings>();
            var random = new System.Random(baseSeed);

            foreach (var scenarioType in _settingsCreators.Keys)
            {
                var seed = random.Next();
                settings[scenarioType] = CreateSettings(scenarioType, seed);
            }

            return settings;
        }

        /// <summary>
        /// Creates a collection of test map data for comprehensive testing.
        /// </summary>
        /// <param name="baseSeed">Base seed for all generated map data</param>
        /// <returns>Dictionary of scenario types to MapData</returns>
        public static Dictionary<string, MapData> CreateAllTestMapData(int baseSeed = 54321)
        {
            var mapData = new Dictionary<string, MapData>();
            var random = new System.Random(baseSeed);

            foreach (var scenarioType in _mapDataCreators.Keys)
            {
                var seed = random.Next();
                mapData[scenarioType] = CreateMapData(scenarioType, seed);
            }

            return mapData;
        }

        #region Settings Creators

        private static MapGenerationSettings CreateMinimalSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 20, 20);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 6,
                MaxDepth = 3,
                SplitPreference = SplitPreference.Alternate,
                SplitPositionVariation = 0.3f,
                StopSplittingChance = 0.1f,
                RoomSizeRatio = 0.8f,
                RoomPositionVariation = 0.1f
            };
            
            // Add classification settings
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            settings.ClassificationSettings = classificationSettings;
            
            return settings;
        }

        private static MapGenerationSettings CreateStandardSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 50, 50);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 10,
                MaxDepth = 5,
                SplitPreference = SplitPreference.Alternate,
                SplitPositionVariation = 0.3f,
                StopSplittingChance = 0.1f,
                RoomSizeRatio = 0.8f,
                RoomPositionVariation = 0.1f
            };
            
            // Add classification settings
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            settings.ClassificationSettings = classificationSettings;
            
            return settings;
        }

        private static MapGenerationSettings CreateComplexSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 100, 100);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 15,
                MaxDepth = 7,
                SplitPreference = SplitPreference.Balanced,
                SplitPositionVariation = 0.4f,
                StopSplittingChance = 0.15f,
                RoomSizeRatio = 0.75f,
                RoomPositionVariation = 0.2f
            };
            
            // Add classification settings
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            classificationSettings.RandomnessFactor = 0.4f;
            settings.ClassificationSettings = classificationSettings;
            
            return settings;
        }

        private static MapGenerationSettings CreatePerformanceTestSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 200, 200);
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 20,
                MaxDepth = 8,
                SplitPreference = SplitPreference.Random,
                SplitPositionVariation = 0.5f,
                StopSplittingChance = 0.2f,
                RoomSizeRatio = 0.7f,
                RoomPositionVariation = 0.3f
            };
            
            // Add classification settings optimized for performance
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            classificationSettings.RandomnessFactor = 0.1f; // Less randomness for consistent performance
            classificationSettings.EnableCaching = true;
            settings.ClassificationSettings = classificationSettings;
            
            return settings;
        }

        private static MapGenerationSettings CreateEdgeCaseSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapBounds = new RectInt(0, 0, 10, 10); // Very small map
            settings.bsp = new BSPConfiguration
            {
                MinPartitionSize = 5,
                MaxDepth = 2,
                SplitPreference = SplitPreference.Alternate,
                SplitPositionVariation = 0.1f,
                StopSplittingChance = 0.0f,
                RoomSizeRatio = 0.9f,
                RoomPositionVariation = 0.0f
            };
            
            // Add classification settings
            var classificationSettings = ScriptableObject.CreateInstance<RoomClassificationSettings>();
            classificationSettings.CreateDefaultConfiguration();
            settings.ClassificationSettings = classificationSettings;
            
            return settings;
        }

        #endregion

        #region MapData Creators

        private static MapData CreateEmptyMapData(int seed)
        {
            var mapData = new MapData(seed, new Vector2Int(10, 10));
            return mapData;
        }

        private static MapData CreateSingleRoomMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData(seed, new Vector2Int(20, 20));

            var x = random.Next(2, 10);
            var y = random.Next(2, 10);
            var width = random.Next(3, 8);
            var height = random.Next(3, 8);
            var room = new RoomData(new RectInt(x, y, width, height));
            room.Classification = RoomClassification.Office;

            mapData.AddRoom(room);
            return mapData;
        }

        private static MapData CreateMultipleRoomsMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData(seed, new Vector2Int(50, 50));

            var roomCount = random.Next(3, 8);
            for (int i = 0; i < roomCount; i++)
            {
                var x = random.Next(2, 40);
                var y = random.Next(2, 40);
                var width = random.Next(4, 12);
                var height = random.Next(4, 12);
                var room = new RoomData(new RectInt(x, y, width, height));
                
                // Assign random classification
                var classifications = new[] { RoomClassification.Office, RoomClassification.BreakRoom, RoomClassification.Conference };
                room.Classification = classifications[random.Next(classifications.Length)];

                mapData.AddRoom(room);
            }

            return mapData;
        }

        private static MapData CreateComplexLayoutMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData(seed, new Vector2Int(100, 100));

            // Create a more complex layout with specific patterns
            var roomCount = random.Next(15, 25);
            for (int i = 0; i < roomCount; i++)
            {
                var x = random.Next(5, 80);
                var y = random.Next(5, 80);
                var width = random.Next(6, 20);
                var height = random.Next(6, 20);
                var room = new RoomData(new RectInt(x, y, width, height));
                
                // Vary room classifications
                var classifications = Enum.GetValues(typeof(RoomClassification)) as RoomClassification[];
                room.Classification = classifications[random.Next(classifications.Length)];

                mapData.AddRoom(room);
            }

            // Add some corridors
            var corridorCount = random.Next(5, 10);
            for (int i = 0; i < corridorCount; i++)
            {
                var start = new Vector2Int(random.Next(0, 100), random.Next(0, 100));
                var end = new Vector2Int(random.Next(0, 100), random.Next(0, 100));
                var width = random.Next(1, 4);
                var corridor = new CorridorData(0, 1, start, end, width);
                
                mapData.AddCorridor(corridor);
            }

            return mapData;
        }

        private static MapData CreateCorruptedMapData(int seed)
        {
            // Creates intentionally problematic map data for error testing
            var mapData = new MapData(seed, new Vector2Int(-1, -1)); // Invalid dimensions

            // Add overlapping rooms
            var room1 = new RoomData(new RectInt(5, 5, 10, 10));
            room1.Classification = RoomClassification.Office;

            var room2 = new RoomData(new RectInt(8, 8, 10, 10)); // Overlaps with room1
            room2.Classification = RoomClassification.BreakRoom;

            mapData.AddRoom(room1);
            mapData.AddRoom(room2);

            return mapData;
        }

        #endregion

        /// <summary>
        /// Creates a deterministic random instance for testing.
        /// </summary>
        /// <param name="seed">Seed for the random instance</param>
        /// <returns>Deterministic System.Random instance</returns>
        public static System.Random CreateDeterministicRandom(int seed)
        {
            return new System.Random(seed);
        }

        /// <summary>
        /// Creates test biome configuration.
        /// </summary>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>BiomeConfiguration for testing</returns>
        public static BiomeConfiguration CreateTestBiomeConfiguration(int seed = 0)
        {
            var biome = ScriptableObject.CreateInstance<BiomeConfiguration>();
            biome.biomeName = "Test Biome";
            biome.description = "Biome created for testing purposes";
            biome.seed = seed;
            return biome;
        }

        /// <summary>
        /// Creates test room template.
        /// </summary>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>RoomTemplate for testing</returns>
        public static RoomTemplate CreateTestRoomTemplate(int seed = 0)
        {
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.templateName = "Test Room Template";
            template.description = "Template created for testing purposes";
            template.minWidth = 3;
            template.maxWidth = 10;
            template.minHeight = 3;
            template.maxHeight = 10;
            template.seed = seed;
            return template;
        }
        
        /// <summary>
        /// Creates test tileset configuration for rendering tests.
        /// </summary>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>TilesetConfiguration for testing</returns>
        public static TilesetConfiguration CreateTestTilesetConfiguration(int seed = 0)
        {
            var tileset = ScriptableObject.CreateInstance<TilesetConfiguration>();
            tileset.name = "TestTileset";
            tileset.TilesetID = "test_tileset";
            tileset.TilesetName = "Test Tileset";
            tileset.Description = "Tileset created for testing purposes";
            tileset.Theme = TilesetTheme.Office;
            
            // Create mock tiles for testing
            var floorTile = ScriptableObject.CreateInstance<TileBase>();
            floorTile.name = "TestFloorTile";
            
            var wallTile = ScriptableObject.CreateInstance<TileBase>();
            wallTile.name = "TestWallTile";
            
            var ceilingTile = ScriptableObject.CreateInstance<TileBase>();
            ceilingTile.name = "TestCeilingTile";
            
            var decorativeTile = ScriptableObject.CreateInstance<TileBase>();
            decorativeTile.name = "TestDecorativeTile";
            
            // Set up floor tiles
            tileset.FloorTiles = new TileMapping
            {
                MappingName = "Floor Tiles",
                UseRandomSelection = true,
                DefaultIndex = 0
            };
            tileset.FloorTiles.Tiles.Add(new TileEntry
            {
                Tile = floorTile,
                Weight = 1.0f,
                IsWalkable = true,
                HasCollision = false
            });
            
            // Set up wall tiles
            tileset.WallTiles = new TileMapping
            {
                MappingName = "Wall Tiles",
                UseRandomSelection = true,
                DefaultIndex = 0
            };
            tileset.WallTiles.Tiles.Add(new TileEntry
            {
                Tile = wallTile,
                Weight = 1.0f,
                IsWalkable = false,
                HasCollision = true
            });
            
            // Set up ceiling tiles
            tileset.CeilingTiles = new TileMapping
            {
                MappingName = "Ceiling Tiles",
                UseRandomSelection = true,
                DefaultIndex = 0
            };
            tileset.CeilingTiles.Tiles.Add(new TileEntry
            {
                Tile = ceilingTile,
                Weight = 1.0f,
                IsWalkable = false,
                HasCollision = false
            });
            
            // Set up decorative tiles
            tileset.DecorativeTiles.Add(new TileMapping
            {
                MappingName = "Decorative Tiles",
                UseRandomSelection = true,
                DefaultIndex = 0
            });
            tileset.DecorativeTiles[0].Tiles.Add(new TileEntry
            {
                Tile = decorativeTile,
                Weight = 0.5f,
                IsWalkable = true,
                HasCollision = false
            });
            
            // Set fallback tile
            tileset.FallbackTile = floorTile;
            
            return tileset;
        }
        
        /// <summary>
        /// Creates test map data with specified number of rooms.
        /// </summary>
        /// <param name="roomCount">Number of rooms to create</param>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>MapData with specified room count</returns>
        public static MapData CreateTestMapData(int roomCount = 10, int seed = 42)
        {
            var random = new System.Random(seed);
            var mapData = new MapData(seed, new Vector2Int(100, 100));
            
            for (int i = 0; i < roomCount; i++)
            {
                var x = random.Next(5, 80);
                var y = random.Next(5, 80);
                var width = random.Next(6, 15);
                var height = random.Next(6, 15);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.Classification = (RoomClassification)random.Next(Enum.GetValues(typeof(RoomClassification)).Length);
                
                mapData.AddRoom(room);
            }
            
            // Add corridors between rooms
            for (int i = 0; i < roomCount - 1; i++)
            {
                var roomA = mapData.Rooms[i];
                var roomB = mapData.Rooms[i + 1];
                
                var corridor = CreateTestCorridor(roomA, roomB, random);
                mapData.AddCorridor(corridor);
            }
            
            return mapData;
        }
        
        /// <summary>
        /// Creates test map data for large scale testing.
        /// </summary>
        /// <param name="roomCount">Number of rooms (typically 100+)</param>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>Large MapData for performance testing</returns>
        public static MapData CreateLargeTestMapData(int roomCount = 100, int seed = 42)
        {
            var random = new System.Random(seed);
            var mapData = new MapData(seed, new Vector2Int(200, 200));
            
            for (int i = 0; i < roomCount; i++)
            {
                var x = random.Next(10, 180);
                var y = random.Next(10, 180);
                var width = random.Next(8, 20);
                var height = random.Next(8, 20);
                
                var room = new RoomData(new RectInt(x, y, width, height));
                room.Classification = (RoomClassification)random.Next(Enum.GetValues(typeof(RoomClassification)).Length);
                
                mapData.AddRoom(room);
            }
            
            // Add corridors to connect rooms in a network
            for (int i = 0; i < roomCount - 1; i++)
            {
                var roomA = mapData.Rooms[i];
                var roomB = mapData.Rooms[(i + 1) % roomCount];
                
                var corridor = CreateTestCorridor(roomA, roomB, random);
                mapData.AddCorridor(corridor);
            }
            
            return mapData;
        }
        
        /// <summary>
        /// Creates a test room with specified parameters.
        /// </summary>
        /// <param name="classification">Room classification</param>
        /// <param name="width">Room width</param>
        /// <param name="height">Room height</param>
        /// <param name="seed">Seed for deterministic generation</param>
        /// <returns>RoomData for testing</returns>
        public static RoomData CreateTestRoom(RoomClassification classification = RoomClassification.Office, int width = 10, int height = 10, int seed = 42)
        {
            var random = new System.Random(seed);
            var x = random.Next(0, 50);
            var y = random.Next(0, 50);
            
            var room = new RoomData(new RectInt(x, y, width, height));
            room.Classification = classification;
            
            return room;
        }
        
        /// <summary>
        /// Creates a test corridor between two rooms.
        /// </summary>
        /// <param name="roomA">First room</param>
        /// <param name="roomB">Second room</param>
        /// <param name="random">Random instance for generation</param>
        /// <returns>CorridorData for testing</returns>
        private static CorridorData CreateTestCorridor(RoomData roomA, RoomData roomB, System.Random random)
        {
            // Simple L-shaped corridor
            var start = roomA.Bounds.center;
            var end = roomB.Bounds.center;
            
            var pathTiles = new List<Vector2Int>();
            var current = start;
            
            // Move horizontally first, then vertically
            while (current.x != end.x)
            {
                pathTiles.Add(current);
                current.x += Math.Sign(end.x - current.x);
            }
            
            while (current.y != end.y)
            {
                pathTiles.Add(current);
                current.y += Math.Sign(end.y - current.y);
            }
            
            pathTiles.Add(end);
            
            var corridor = new CorridorData(roomA.RoomID, roomB.RoomID, start, end, random.Next(1, 3));
            corridor.SetPath(pathTiles);
            
            return corridor;
        }
    }
}