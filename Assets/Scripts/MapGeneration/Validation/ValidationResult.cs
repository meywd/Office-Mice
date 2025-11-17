using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeMice.MapGeneration.Validation
{
    /// <summary>
    /// Standardized validation result across all data types.
    /// Collects errors (must fix) and warnings (should review).
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        [SerializeField] private List<string> _errors = new List<string>();
        [SerializeField] private List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;
        public bool HasWarnings => _warnings.Count > 0;

        public void AddError(string message)
        {
            string error = $"[ERROR] {message}";
            _errors.Add(error);
            Debug.LogError(error);
        }

        public void AddWarning(string message)
        {
            string warning = $"[WARNING] {message}";
            _warnings.Add(warning);
            Debug.LogWarning(warning);
        }

        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                _errors.AddRange(other._errors);
                _warnings.AddRange(other._warnings);
            }
        }

        public string GetSummary()
        {
            return $"Validation: {_errors.Count} errors, {_warnings.Count} warnings";
        }

        public void LogAll()
        {
            foreach (var error in _errors)
                Debug.LogError(error);
            foreach (var warning in _warnings)
                Debug.LogWarning(warning);
        }

        public void Clear()
        {
            _errors.Clear();
            _warnings.Clear();
        }
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>ValidationResult with no errors</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult();
        }
        
        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        /// <param name="errorMessage">Error message to include</param>
        /// <returns>ValidationResult with the specified error</returns>
        public static ValidationResult Failure(string errorMessage)
        {
            var result = new ValidationResult();
            result.AddError(errorMessage);
            return result;
        }
    }
}