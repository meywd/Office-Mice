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
            settings.mapWidth = 20;
            settings.mapHeight = 20;
            settings.minRoomSize = 3;
            settings.maxRoomSize = 5;
            settings.maxRoomCount = 3;
            settings.corridorWidth = 1;
            settings.seed = seed;
            settings.enableDebugLogging = false;
            return settings;
        }

        private static MapGenerationSettings CreateStandardSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapWidth = 50;
            settings.mapHeight = 50;
            settings.minRoomSize = 4;
            settings.maxRoomSize = 10;
            settings.maxRoomCount = 10;
            settings.corridorWidth = 2;
            settings.seed = seed;
            settings.enableDebugLogging = true;
            return settings;
        }

        private static MapGenerationSettings CreateComplexSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapWidth = 100;
            settings.mapHeight = 100;
            settings.minRoomSize = 5;
            settings.maxRoomSize = 15;
            settings.maxRoomCount = 25;
            settings.corridorWidth = 3;
            settings.seed = seed;
            settings.enableDebugLogging = true;
            return settings;
        }

        private static MapGenerationSettings CreatePerformanceTestSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapWidth = 200;
            settings.mapHeight = 200;
            settings.minRoomSize = 3;
            settings.maxRoomSize = 20;
            settings.maxRoomCount = 50;
            settings.corridorWidth = 2;
            settings.seed = seed;
            settings.enableDebugLogging = false;
            return settings;
        }

        private static MapGenerationSettings CreateEdgeCaseSettings(int seed)
        {
            var settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            settings.mapWidth = 1;
            settings.mapHeight = 1;
            settings.minRoomSize = 1;
            settings.maxRoomSize = 1;
            settings.maxRoomCount = 1;
            settings.corridorWidth = 1;
            settings.seed = seed;
            settings.enableDebugLogging = true;
            return settings;
        }

        #endregion

        #region MapData Creators

        private static MapData CreateEmptyMapData(int seed)
        {
            var mapData = new MapData();
            mapData.SetDimensions(10, 10);
            mapData.SetSeed(seed);
            return mapData;
        }

        private static MapData CreateSingleRoomMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData();
            mapData.SetDimensions(20, 20);
            mapData.SetSeed(seed);

            var room = new RoomData();
            var x = random.Next(2, 10);
            var y = random.Next(2, 10);
            var width = random.Next(3, 8);
            var height = random.Next(3, 8);
            room.SetBounds(new Rect(x, y, width, height));
            room.Classification = RoomClassification.Office;

            mapData.AddRoom(room);
            return mapData;
        }

        private static MapData CreateMultipleRoomsMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData();
            mapData.SetDimensions(50, 50);
            mapData.SetSeed(seed);

            var roomCount = random.Next(3, 8);
            for (int i = 0; i < roomCount; i++)
            {
                var room = new RoomData();
                var x = random.Next(2, 40);
                var y = random.Next(2, 40);
                var width = random.Next(4, 12);
                var height = random.Next(4, 12);
                room.SetBounds(new Rect(x, y, width, height));
                
                // Assign random classification
                var classifications = new[] { RoomClassification.Office, RoomClassification.BreakRoom, RoomClassification.MeetingRoom };
                room.Classification = classifications[random.Next(classifications.Length)];

                mapData.AddRoom(room);
            }

            return mapData;
        }

        private static MapData CreateComplexLayoutMapData(int seed)
        {
            var random = new System.Random(seed);
            var mapData = new MapData();
            mapData.SetDimensions(100, 100);
            mapData.SetSeed(seed);

            // Create a more complex layout with specific patterns
            var roomCount = random.Next(15, 25);
            for (int i = 0; i < roomCount; i++)
            {
                var room = new RoomData();
                var x = random.Next(5, 80);
                var y = random.Next(5, 80);
                var width = random.Next(6, 20);
                var height = random.Next(6, 20);
                room.SetBounds(new Rect(x, y, width, height));
                
                // Vary room classifications
                var classifications = Enum.GetValues(typeof(RoomClassification)) as RoomClassification[];
                room.Classification = classifications[random.Next(classifications.Length)];

                mapData.AddRoom(room);
            }

            // Add some corridors
            var corridorCount = random.Next(5, 10);
            for (int i = 0; i < corridorCount; i++)
            {
                var corridor = new CorridorData();
                corridor.start = new Vector2Int(random.Next(0, 100), random.Next(0, 100));
                corridor.end = new Vector2Int(random.Next(0, 100), random.Next(0, 100));
                corridor.width = random.Next(1, 4);
                corridor.shape = (CorridorShape)random.Next(Enum.GetValues(typeof(CorridorShape)).Length);
                
                mapData.AddCorridor(corridor);
            }

            return mapData;
        }

        private static MapData CreateCorruptedMapData(int seed)
        {
            // Creates intentionally problematic map data for error testing
            var mapData = new MapData();
            mapData.SetDimensions(-1, -1); // Invalid dimensions
            mapData.SetSeed(seed);

            // Add overlapping rooms
            var room1 = new RoomData();
            room1.SetBounds(new Rect(5, 5, 10, 10));
            room1.Classification = RoomClassification.Office;

            var room2 = new RoomData();
            room2.SetBounds(new Rect(8, 8, 10, 10)); // Overlaps with room1
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
    }
}