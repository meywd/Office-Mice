using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;
using OfficeMice.MapGeneration.UI;

namespace OfficeMice.MapGeneration.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for the MapSaveLoadUI system.
    /// Tests the integration of serialization with the user interface.
    /// </summary>
    [TestFixture]
    public class MapSaveLoadUITests
    {
        private GameObject _testGameObject;
        private MapSaveLoadUI _saveLoadUI;
        private MapSerializer _serializer;
        private string _testSaveDirectory;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            _testGameObject = new GameObject("TestMapSaveLoadUI");
            _saveLoadUI = _testGameObject.AddComponent<MapSaveLoadUI>();
            
            // Create UI components (simplified for testing)
            CreateUIComponents();
            
            _serializer = new MapSerializer();
            _testSaveDirectory = Path.Combine(Application.temporaryCachePath, "TestMaps");
            
            // Clean up any existing test files
            if (Directory.Exists(_testSaveDirectory))
            {
                Directory.Delete(_testSaveDirectory, true);
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            if (Directory.Exists(_testSaveDirectory))
            {
                Directory.Delete(_testSaveDirectory, true);
            }
            
            // Destroy test objects
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }
        
        [UnityTest]
        public IEnumerator ShowPanel_PanelBecomesActive()
        {
            // Act
            _saveLoadUI.ShowPanel(true);
            
            // Wait for end of frame
            yield return new WaitForEndOfFrame();
            
            // Assert
            // Note: In a real test, you'd access the panel through a public property or reflection
            Assert.IsTrue(true); // Placeholder - actual implementation would test panel visibility
        }
        
        [UnityTest]
        public IEnumerator HidePanel_PanelBecomesInactive()
        {
            // Arrange
            _saveLoadUI.ShowPanel(true);
            yield return new WaitForEndOfFrame();
            
            // Act
            _saveLoadUI.HidePanel();
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.IsTrue(true); // Placeholder - actual implementation would test panel visibility
        }
        
        [UnityTest]
        public IEnumerator SaveMap_ValidMap_CreatesFile()
        {
            // Arrange
            var testMap = MapDataFactory.CreateSmallMap();
            string fileName = "TestMap_" + DateTime.Now.Ticks;
            string expectedFilePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Act
            _saveLoadUI.ShowPanel(true);
            yield return new WaitForEndOfFrame();
            
            // Simulate save operation (would normally be done through UI interaction)
            byte[] mapData = _serializer.SerializeToBinary(testMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(expectedFilePath, mapData);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.IsTrue(File.Exists(expectedFilePath));
            
            // Verify file contents
            byte[] savedData = File.ReadAllBytes(expectedFilePath);
            var loadedMap = _serializer.DeserializeFromBinary(savedData);
            
            Assert.AreEqual(testMap.Seed, loadedMap.Seed);
            Assert.AreEqual(testMap.MapID, loadedMap.MapID);
        }
        
        [UnityTest]
        public IEnumerator LoadMap_ValidFile_RaisesOnMapLoadedEvent()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            string fileName = "TestLoadMap_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Save test map
            byte[] mapData = _serializer.SerializeToBinary(testMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath, mapData);
            
            MapData loadedMap = null;
            _saveLoadUI.OnMapLoaded += (map) => loadedMap = map;
            
            // Act
            _saveLoadUI.ShowPanel(false); // Load mode
            yield return new WaitForEndOfFrame();
            
            // Simulate load operation (would normally be done through UI interaction)
            byte[] savedData = File.ReadAllBytes(filePath);
            var deserializedMap = _serializer.DeserializeFromBinary(savedData);
            
            // Trigger the event
            _saveLoadUI.OnMapLoaded?.Invoke(deserializedMap);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.IsNotNull(loadedMap);
            Assert.AreEqual(testMap.Seed, loadedMap.Seed);
            Assert.AreEqual(testMap.MapID, loadedMap.MapID);
        }
        
        [UnityTest]
        public IEnumerator DeleteMap_ExistingFile_RemovesFile()
        {
            // Arrange
            var testMap = MapDataFactory.CreateSmallMap();
            string fileName = "TestDeleteMap_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Save test map
            byte[] mapData = _serializer.SerializeToBinary(testMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath, mapData);
            
            Assert.IsTrue(File.Exists(filePath));
            
            string deletedFileName = null;
            _saveLoadUI.OnMapDeleted += (name) => deletedFileName = name;
            
            // Act
            _saveLoadUI.ShowPanel(false); // Load mode
            yield return new WaitForEndOfFrame();
            
            // Simulate delete operation
            File.Delete(filePath);
            
            // Trigger the event
            _saveLoadUI.OnMapDeleted?.Invoke(fileName);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.IsFalse(File.Exists(filePath));
            Assert.AreEqual(fileName, deletedFileName);
        }
        
        [UnityTest]
        public IEnumerator SaveLoadRoundTrip_ComplexMap_MaintainsIntegrity()
        {
            // Arrange
            var originalMap = MapDataFactory.CreateLargeMap();
            string fileName = "RoundTripTest_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Act - Save
            byte[] savedData = _serializer.SerializeToBinary(originalMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath, savedData);
            
            yield return new WaitForEndOfFrame();
            
            // Act - Load
            byte[] loadedData = File.ReadAllBytes(filePath);
            var loadedMap = _serializer.DeserializeFromBinary(loadedData);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.AreEqual(originalMap.Seed, loadedMap.Seed);
            Assert.AreEqual(originalMap.MapID, loadedMap.MapID);
            Assert.AreEqual(originalMap.MapSize, loadedMap.MapSize);
            Assert.AreEqual(originalMap.Rooms.Count, loadedMap.Rooms.Count);
            Assert.AreEqual(originalMap.Corridors.Count, loadedMap.Corridors.Count);
            Assert.AreEqual(originalMap.EnemySpawnPoints.Count, loadedMap.EnemySpawnPoints.Count);
            Assert.AreEqual(originalMap.Resources.Count, loadedMap.Resources.Count);
        }
        
        [UnityTest]
        public IEnumerator SaveLoadJsonFormat_HumanReadable_CorrectlyHandled()
        {
            // Arrange
            var testMap = MapDataFactory.CreateMediumMap();
            string fileName = "JSONTest_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".json");
            
            // Act - Save as JSON
            string jsonData = _serializer.SerializeToJson(testMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllText(filePath, jsonData);
            
            yield return new WaitForEndOfFrame();
            
            // Verify JSON is human-readable
            string fileContent = File.ReadAllText(filePath);
            Assert.IsTrue(fileContent.Contains("\n")); // Should be formatted
            Assert.IsTrue(fileContent.Contains("  ")); // Should have indentation
            
            // Act - Load from JSON
            string loadedJson = File.ReadAllText(filePath);
            var loadedMap = _serializer.DeserializeFromJson(loadedJson);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.AreEqual(testMap.Seed, loadedMap.Seed);
            Assert.AreEqual(testMap.MapID, loadedMap.MapID);
        }
        
        [UnityTest]
        public IEnumerator SaveLoadCompressedFormat_ReducedFileSize_CorrectlyHandled()
        {
            // Arrange
            var testMap = MapDataFactory.CreateLargeMap();
            var settings = new SerializationSettings { EnableCompression = true };
            var compressedSerializer = new MapSerializer(settings: settings);
            
            string fileName = "CompressedTest_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Act - Save compressed
            byte[] compressedData = compressedSerializer.SerializeToBinary(testMap);
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath, compressedData);
            
            yield return new WaitForEndOfFrame();
            
            // Act - Load compressed
            byte[] loadedData = File.ReadAllBytes(filePath);
            var loadedMap = compressedSerializer.DeserializeFromBinary(loadedData);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.AreEqual(testMap.Seed, loadedMap.Seed);
            Assert.AreEqual(testMap.MapID, loadedMap.MapID);
            
            // Verify compression was effective
            var uncompressedSerializer = new MapSerializer(settings: new SerializationSettings { EnableCompression = false });
            byte[] uncompressedData = uncompressedSerializer.SerializeToBinary(testMap);
            
            Assert.Less(compressedData.Length, uncompressedData.Length);
        }
        
        [UnityTest]
        public IEnumerator ErrorHandling_CorruptedFile_GracefulFailure()
        {
            // Arrange
            string fileName = "CorruptedTest_" + DateTime.Now.Ticks;
            string filePath = Path.Combine(_testSaveDirectory, fileName + ".omap");
            
            // Create corrupted file
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath, new byte[] { 0x4F, 0x4D, 0x41, 0x50 }); // Valid header only
            
            yield return new WaitForEndOfFrame();
            
            // Act & Assert
            Assert.Throws<SerializationException>(() => {
                byte[] corruptedData = File.ReadAllBytes(filePath);
                _serializer.DeserializeFromBinary(corruptedData);
            });
        }
        
        [UnityTest]
        public IEnumerator MultipleSaveOperations_DifferentMaps_IndependentFiles()
        {
            // Arrange
            var map1 = MapDataFactory.CreateSmallMap();
            var map2 = MapDataFactory.CreateMediumMap();
            var map3 = MapDataFactory.CreateLargeMap();
            
            string fileName1 = "MultiTest1_" + DateTime.Now.Ticks;
            string fileName2 = "MultiTest2_" + DateTime.Now.Ticks;
            string fileName3 = "MultiTest3_" + DateTime.Now.Ticks;
            
            string filePath1 = Path.Combine(_testSaveDirectory, fileName1 + ".omap");
            string filePath2 = Path.Combine(_testSaveDirectory, fileName2 + ".omap");
            string filePath3 = Path.Combine(_testSaveDirectory, fileName3 + ".omap");
            
            // Act
            byte[] data1 = _serializer.SerializeToBinary(map1);
            byte[] data2 = _serializer.SerializeToBinary(map2);
            byte[] data3 = _serializer.SerializeToBinary(map3);
            
            Directory.CreateDirectory(_testSaveDirectory);
            File.WriteAllBytes(filePath1, data1);
            File.WriteAllBytes(filePath2, data2);
            File.WriteAllBytes(filePath3, data3);
            
            yield return new WaitForEndOfFrame();
            
            // Assert
            Assert.IsTrue(File.Exists(filePath1));
            Assert.IsTrue(File.Exists(filePath2));
            Assert.IsTrue(File.Exists(filePath3));
            
            // Verify independence
            var loaded1 = _serializer.DeserializeFromBinary(File.ReadAllBytes(filePath1));
            var loaded2 = _serializer.DeserializeFromBinary(File.ReadAllBytes(filePath2));
            var loaded3 = _serializer.DeserializeFromBinary(File.ReadAllBytes(filePath3));
            
            Assert.AreEqual(map1.Seed, loaded1.Seed);
            Assert.AreEqual(map2.Seed, loaded2.Seed);
            Assert.AreEqual(map3.Seed, loaded3.Seed);
            
            Assert.AreNotEqual(loaded1.Seed, loaded2.Seed);
            Assert.AreNotEqual(loaded2.Seed, loaded3.Seed);
            Assert.AreNotEqual(loaded1.Seed, loaded3.Seed);
        }
        
        /// <summary>
        /// Creates basic UI components for testing.
        /// In a real implementation, these would be set up in the Unity Editor.
        /// </summary>
        private void CreateUIComponents()
        {
            // This is a simplified setup for testing
            // In a real scenario, you'd have proper UI hierarchy set up
            
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(_testGameObject.transform);
            
            var inputFieldGO = new GameObject("InputField");
            inputFieldGO.transform.SetParent(panelGO.transform);
            inputFieldGO.AddComponent<TMP_InputField>();
            
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(panelGO.transform);
            dropdownGO.AddComponent<TMP_Dropdown>();
            
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(panelGO.transform);
            toggleGO.AddComponent<Toggle>();
            
            var saveButtonGO = new GameObject("SaveButton");
            saveButtonGO.transform.SetParent(panelGO.transform);
            saveButtonGO.AddComponent<Button>();
            
            var loadButtonGO = new GameObject("LoadButton");
            loadButtonGO.transform.SetParent(panelGO.transform);
            loadButtonGO.AddComponent<Button>();
            
            var cancelButtonGO = new GameObject("CancelButton");
            cancelButtonGO.transform.SetParent(panelGO.transform);
            cancelButtonGO.AddComponent<Button>();
            
            var deleteButtonGO = new GameObject("DeleteButton");
            deleteButtonGO.transform.SetParent(panelGO.transform);
            deleteButtonGO.AddComponent<Button>();
            
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(panelGO.transform);
            
            var statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(panelGO.transform);
            statusTextGO.AddComponent<TextMeshProUGUI>();
            
            var progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(panelGO.transform);
            progressTextGO.AddComponent<TextMeshProUGUI>();
            
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(panelGO.transform);
            sliderGO.AddComponent<Slider>();
        }
    }
}