using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OfficeMice.MapGeneration.Data;
using OfficeMice.MapGeneration.Serialization;

namespace OfficeMice.MapGeneration.UI
{
    /// <summary>
    /// UI controller for map save/load functionality.
    /// Integrates with the serialization system to provide user-friendly save/load interface.
    /// </summary>
    public class MapSaveLoadUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _saveLoadPanel;
        [SerializeField] private TMP_InputField _mapNameInput;
        [SerializeField] private TMP_Dropdown _formatDropdown;
        [SerializeField] private Toggle _compressionToggle;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Transform _savedMapsContainer;
        [SerializeField] private GameObject _savedMapEntryPrefab;
        
        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Slider _progressSlider;
        
        [Header("File Settings")]
        [SerializeField] private string _saveDirectory = "SavedMaps";
        [SerializeField] private string _fileExtension = ".omap";
        
        private MapSerializer _serializer;
        private List<SavedMapEntry> _savedMaps;
        private SavedMapEntry _selectedMap;
        private bool _isLoading;
        
        // Events
        public event Action<MapData> OnMapLoaded;
        public event Action<string> OnMapSaved;
        public event Action<string> OnMapDeleted;
        
        private void Awake()
        {
            _serializer = new MapSerializer();
            _savedMaps = new List<SavedMapEntry>();
            
            InitializeUI();
            LoadSavedMapsList();
        }
        
        private void Start()
        {
            HidePanel();
        }
        
        /// <summary>
        /// Initializes UI components and event listeners.
        /// </summary>
        private void InitializeUI()
        {
            // Format dropdown options
            _formatDropdown.ClearOptions();
            _formatDropdown.AddOptions(new List<string> { "JSON", "Binary" });
            _formatDropdown.value = 1; // Default to Binary for production
            
            // Compression toggle
            _compressionToggle.isOn = true;
            
            // Button listeners
            _saveButton.onClick.AddListener(HandleSaveButton);
            _loadButton.onClick.AddListener(HandleLoadButton);
            _cancelButton.onClick.AddListener(HidePanel);
            _deleteButton.onClick.AddListener(HandleDeleteButton);
            
            // Input field validation
            _mapNameInput.onValueChanged.AddListener(ValidateSaveButton);
            
            // Initial state
            ValidateSaveButton(_mapNameInput.text);
            UpdateSelectionUI();
        }
        
        /// <summary>
        /// Shows the save/load panel.
        /// </summary>
        public void ShowPanel(bool showSaveMode = false)
        {
            _saveLoadPanel.SetActive(true);
            _isLoading = !showSaveMode;
            
            if (showSaveMode)
            {
                _mapNameInput.text = $"Map_{DateTime.Now:yyyyMMdd_HHmmss}";
                _mapNameInput.Select();
            }
            
            UpdateUIForMode();
            LoadSavedMapsList();
        }
        
        /// <summary>
        /// Hides the save/load panel.
        /// </summary>
        public void HidePanel()
        {
            _saveLoadPanel.SetActive(false);
            _selectedMap = null;
            UpdateSelectionUI();
        }
        
        /// <summary>
        /// Handles the save button click.
        /// </summary>
        private async void HandleSaveButton()
        {
            if (string.IsNullOrEmpty(_mapNameInput.text))
                return;
                
            var currentMap = GetCurrentMap();
            if (currentMap == null)
            {
                ShowStatus("No map available to save", false);
                return;
            }
            
            try
            {
                SetLoadingState(true, "Saving map...");
                
                string fileName = SanitizeFileName(_mapNameInput.text);
                string filePath = Path.Combine(GetSaveDirectory(), fileName + _fileExtension);
                
                // Check if file already exists
                if (File.Exists(filePath))
                {
                    // Show confirmation dialog (simplified for this implementation)
                    if (!await ShowConfirmationDialog($"Map '{fileName}' already exists. Overwrite?"))
                        return;
                }
                
                // Serialize based on selected format
                bool useJson = _formatDropdown.value == 0;
                bool useCompression = _compressionToggle.isOn;
                
                var settings = new SerializationSettings
                {
                    EnableCompression = useCompression,
                    CompressJson = useCompression && useJson,
                    PrettyPrintJson = useJson
                };
                
                var serializer = new MapSerializer(settings: settings);
                
                byte[] data;
                if (useJson)
                {
                    string json = serializer.SerializeToJson(currentMap);
                    data = System.Text.Encoding.UTF8.GetBytes(json);
                }
                else
                {
                    data = serializer.SerializeToBinary(currentMap);
                }
                
                // Write to file
                await File.WriteAllBytesAsync(filePath, data);
                
                ShowStatus($"Map '{fileName}' saved successfully!", true);
                OnMapSaved?.Invoke(fileName);
                
                // Refresh the saved maps list
                LoadSavedMapsList();
                
                // Switch to load mode
                _isLoading = true;
                UpdateUIForMode();
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to save map: {ex.Message}", false);
                Debug.LogError($"Map save error: {ex}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }
        
        /// <summary>
        /// Handles the load button click.
        /// </summary>
        private async void HandleLoadButton()
        {
            if (_selectedMap == null)
                return;
                
            try
            {
                SetLoadingState(true, "Loading map...");
                
                byte[] data = await File.ReadAllBytesAsync(_selectedMap.FilePath);
                
                MapData loadedMap;
                if (_selectedMap.Format == "JSON")
                {
                    string json = System.Text.Encoding.UTF8.GetString(data);
                    loadedMap = _serializer.DeserializeFromJson(json);
                }
                else
                {
                    loadedMap = _serializer.DeserializeFromBinary(data);
                }
                
                ShowStatus($"Map '{_selectedMap.Name}' loaded successfully!", true);
                OnMapLoaded?.Invoke(loadedMap);
                
                HidePanel();
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to load map: {ex.Message}", false);
                Debug.LogError($"Map load error: {ex}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }
        
        /// <summary>
        /// Handles the delete button click.
        /// </summary>
        private async void HandleDeleteButton()
        {
            if (_selectedMap == null)
                return;
                
            try
            {
                if (!await ShowConfirmationDialog($"Delete map '{_selectedMap.Name}'?"))
                    return;
                    
                SetLoadingState(true, "Deleting map...");
                
                File.Delete(_selectedMap.FilePath);
                
                ShowStatus($"Map '{_selectedMap.Name}' deleted successfully!", true);
                OnMapDeleted?.Invoke(_selectedMap.Name);
                
                // Refresh the list
                LoadSavedMapsList();
                _selectedMap = null;
                UpdateSelectionUI();
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to delete map: {ex.Message}", false);
                Debug.LogError($"Map delete error: {ex}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }
        
        /// <summary>
        /// Loads the list of saved maps from the save directory.
        /// </summary>
        private void LoadSavedMapsList()
        {
            // Clear existing entries
            foreach (Transform child in _savedMapsContainer)
            {
                Destroy(child.gameObject);
            }
            
            _savedMaps.Clear();
            
            try
            {
                string saveDir = GetSaveDirectory();
                if (!Directory.Exists(saveDir))
                    return;
                    
                var files = Directory.GetFiles(saveDir, "*" + _fileExtension);
                
                foreach (string filePath in files)
                {
                    var entry = CreateSavedMapEntry(filePath);
                    if (entry != null)
                    {
                        _savedMaps.Add(entry);
                        CreateUIEntry(entry);
                    }
                }
                
                // Sort by date modified (newest first)
                _savedMaps.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load saved maps list: {ex}");
                ShowStatus("Failed to load saved maps", false);
            }
        }
        
        /// <summary>
        /// Creates a saved map entry from file info.
        /// </summary>
        private SavedMapEntry CreateSavedMapEntry(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Try to read the file header to determine format
                string format = "Binary"; // Default
                if (fileInfo.Length > 4)
                {
                    byte[] header = new byte[4];
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        fileStream.Read(header, 0, 4);
                    }
                    
                    string headerStr = System.Text.Encoding.ASCII.GetString(header);
                    if (headerStr.StartsWith("{") || headerStr.StartsWith("["))
                    {
                        format = "JSON";
                    }
                }
                
                return new SavedMapEntry
                {
                    Name = fileName,
                    FilePath = filePath,
                    Format = format,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create saved map entry for {filePath}: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a UI entry for a saved map.
        /// </summary>
        private void CreateUIEntry(SavedMapEntry entry)
        {
            if (_savedMapEntryPrefab == null)
                return;
                
            var entryGO = Instantiate(_savedMapEntryPrefab, _savedMapsContainer);
            var entryUI = entryGO.GetComponent<SavedMapEntryUI>();
            
            if (entryUI != null)
            {
                entryUI.Initialize(entry);
                entryUI.OnSelected += (selectedEntry) => {
                    _selectedMap = selectedEntry;
                    UpdateSelectionUI();
                };
            }
        }
        
        /// <summary>
        /// Updates the UI based on the current mode (save/load).
        /// </summary>
        private void UpdateUIForMode()
        {
            _mapNameInput.gameObject.SetActive(!_isLoading);
            _formatDropdown.gameObject.SetActive(!_isLoading);
            _compressionToggle.gameObject.SetActive(!_isLoading);
            _saveButton.gameObject.SetActive(!_isLoading);
            _loadButton.gameObject.SetActive(_isLoading);
            _deleteButton.gameObject.SetActive(_isLoading);
            
            _statusText.text = _isLoading ? "Select a map to load" : "Enter map name and save";
        }
        
        /// <summary>
        /// Updates the selection UI state.
        /// </summary>
        private void UpdateSelectionUI()
        {
            bool hasSelection = _selectedMap != null;
            _loadButton.interactable = hasSelection && _isLoading;
            _deleteButton.interactable = hasSelection && _isLoading;
        }
        
        /// <summary>
        /// Validates the save button state based on input.
        /// </summary>
        private void ValidateSaveButton(string input)
        {
            bool isValid = !string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input);
            _saveButton.interactable = isValid && !_isLoading;
        }
        
        /// <summary>
        /// Sets the loading state of the UI.
        /// </summary>
        private void SetLoadingState(bool isLoading, string message = "")
        {
            _progressSlider.gameObject.SetActive(isLoading);
            _progressText.gameObject.SetActive(isLoading);
            
            if (isLoading)
            {
                _progressText.text = message;
                _progressSlider.value = 0f;
                
                // Simulate progress (in a real implementation, this would be driven by actual progress)
                StartCoroutine(SimulateProgress());
            }
        }
        
        /// <summary>
        /// Simulates progress for loading operations.
        /// </summary>
        private IEnumerator SimulateProgress()
        {
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _progressSlider.value = elapsed / duration;
                yield return null;
            }
            
            _progressSlider.value = 1f;
        }
        
        /// <summary>
        /// Shows a status message to the user.
        /// </summary>
        private void ShowStatus(string message, bool isSuccess)
        {
            _statusText.text = message;
            _statusText.color = isSuccess ? Color.green : Color.red;
            
            // Clear the message after a few seconds
            StartCoroutine(ClearStatusAfterDelay(3f));
        }
        
        /// <summary>
        /// Clears the status message after a delay.
        /// </summary>
        private IEnumerator ClearStatusAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _statusText.text = "";
        }
        
        /// <summary>
        /// Shows a confirmation dialog (simplified implementation).
        /// </summary>
        private System.Threading.Tasks.Task<bool> ShowConfirmationDialog(string message)
        {
            // In a real implementation, this would show a proper dialog UI
            // For now, we'll just log it and return true
            Debug.Log($"Confirmation: {message}");
            return System.Threading.Tasks.Task.FromResult(true);
        }
        
        /// <summary>
        /// Gets the current map that should be saved.
        /// This should be implemented based on your game's map management system.
        /// </summary>
        private MapData GetCurrentMap()
        {
            // This is a placeholder - implement based on your game's architecture
            // You might get this from a MapManager, GameManager, or similar
            return null;
        }
        
        /// <summary>
        /// Gets the save directory path.
        /// </summary>
        private string GetSaveDirectory()
        {
            string saveDir = Path.Combine(Application.persistentDataPath, _saveDirectory);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            return saveDir;
        }
        
        /// <summary>
        /// Sanitizes a file name to remove invalid characters.
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
    
    /// <summary>
    /// Represents a saved map entry.
    /// </summary>
    [Serializable]
    public class SavedMapEntry
    {
        public string Name;
        public string FilePath;
        public string Format;
        public long FileSize;
        public DateTime LastModified;
    }
}