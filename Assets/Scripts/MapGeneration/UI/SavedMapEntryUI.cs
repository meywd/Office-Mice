using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OfficeMice.MapGeneration.UI
{
    /// <summary>
    /// UI component for displaying a saved map entry in the save/load interface.
    /// </summary>
    public class SavedMapEntryUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _formatText;
        [SerializeField] private TextMeshProUGUI _sizeText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private Image _selectionIndicator;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _normalColor = Color.white;
        
        private SavedMapEntry _entry;
        private bool _isSelected;
        
        public event Action<SavedMapEntry> OnSelected;
        
        private void Awake()
        {
            _selectButton.onClick.AddListener(HandleSelection);
            _selectionIndicator.color = _normalColor;
        }
        
        /// <summary>
        /// Initializes the UI with saved map entry data.
        /// </summary>
        public void Initialize(SavedMapEntry entry)
        {
            _entry = entry;
            
            _nameText.text = entry.Name;
            _formatText.text = entry.Format;
            _sizeText.text = FormatFileSize(entry.FileSize);
            _dateText.text = entry.LastModified.ToString("yyyy-MM-dd HH:mm");
        }
        
        /// <summary>
        /// Sets the selection state of this entry.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            _selectionIndicator.color = _isSelected ? _selectedColor : _normalColor;
        }
        
        /// <summary>
        /// Handles the selection button click.
        /// </summary>
        private void HandleSelection()
        {
            OnSelected?.Invoke(_entry);
        }
        
        /// <summary>
        /// Formats file size for display.
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}