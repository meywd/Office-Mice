using NUnit.Framework;
using UnityEngine;
using OfficeMice.MapGeneration.Validation;
using System.Collections.Generic;

namespace OfficeMice.MapGeneration.Tests
{
    [TestFixture]
    public class ValidationResultTests
    {
        private ValidationResult _result;

        [SetUp]
        public void SetUp()
        {
            _result = new ValidationResult();
        }

        [Test]
        public void ValidationResult_Constructor_InitializesCorrectly()
        {
            // Assert
            Assert.IsTrue(_result.IsValid);
            Assert.IsFalse(_result.HasWarnings);
            Assert.AreEqual(0, _result.Errors.Count);
            Assert.AreEqual(0, _result.Warnings.Count);
        }

        [Test]
        public void ValidationResult_AddError_WorksCorrectly()
        {
            // Act
            _result.AddError("Test error message");

            // Assert
            Assert.IsFalse(_result.IsValid);
            Assert.AreEqual(1, _result.Errors.Count);
            Assert.IsTrue(_result.Errors[0].Contains("[ERROR]"));
            Assert.IsTrue(_result.Errors[0].Contains("Test error message"));
        }

        [Test]
        public void ValidationResult_AddWarning_WorksCorrectly()
        {
            // Act
            _result.AddWarning("Test warning message");

            // Assert
            Assert.IsTrue(_result.IsValid); // Warnings don't make it invalid
            Assert.IsTrue(_result.HasWarnings);
            Assert.AreEqual(1, _result.Warnings.Count);
            Assert.IsTrue(_result.Warnings[0].Contains("[WARNING]"));
            Assert.IsTrue(_result.Warnings[0].Contains("Test warning message"));
        }

        [Test]
        public void ValidationResult_Merge_WorksCorrectly()
        {
            // Arrange
            var otherResult = new ValidationResult();
            otherResult.AddError("Other error");
            otherResult.AddWarning("Other warning");

            _result.AddError("Original error");
            _result.AddWarning("Original warning");

            // Act
            _result.Merge(otherResult);

            // Assert
            Assert.IsFalse(_result.IsValid);
            Assert.IsTrue(_result.HasWarnings);
            Assert.AreEqual(2, _result.Errors.Count);
            Assert.AreEqual(2, _result.Warnings.Count);
            Assert.IsTrue(_result.Errors.Any(e => e.Contains("Original error")));
            Assert.IsTrue(_result.Errors.Any(e => e.Contains("Other error")));
            Assert.IsTrue(_result.Warnings.Any(w => w.Contains("Original warning")));
            Assert.IsTrue(_result.Warnings.Any(w => w.Contains("Other warning")));
        }

        [Test]
        public void ValidationResult_Merge_WithNull_WorksCorrectly()
        {
            // Arrange
            _result.AddError("Original error");

            // Act
            _result.Merge(null);

            // Assert
            Assert.IsFalse(_result.IsValid);
            Assert.AreEqual(1, _result.Errors.Count);
            Assert.AreEqual("Original error", _result.Errors[0].Replace("[ERROR] ", ""));
        }

        [Test]
        public void ValidationResult_GetSummary_WorksCorrectly()
        {
            // Arrange
            _result.AddError("Error 1");
            _result.AddError("Error 2");
            _result.AddWarning("Warning 1");
            _result.AddWarning("Warning 2");
            _result.AddWarning("Warning 3");

            // Act
            var summary = _result.GetSummary();

            // Assert
            Assert.AreEqual("Validation: 2 errors, 3 warnings", summary);
        }

        [Test]
        public void ValidationResult_GetSummary_EmptyResult_WorksCorrectly()
        {
            // Act
            var summary = _result.GetSummary();

            // Assert
            Assert.AreEqual("Validation: 0 errors, 0 warnings", summary);
        }

        [Test]
        public void ValidationResult_Clear_WorksCorrectly()
        {
            // Arrange
            _result.AddError("Error");
            _result.AddWarning("Warning");

            // Act
            _result.Clear();

            // Assert
            Assert.IsTrue(_result.IsValid);
            Assert.IsFalse(_result.HasWarnings);
            Assert.AreEqual(0, _result.Errors.Count);
            Assert.AreEqual(0, _result.Warnings.Count);
        }

        [Test]
        public void ValidationResult_LogAll_WorksCorrectly()
        {
            // Arrange
            _result.AddError("Test error");
            _result.AddWarning("Test warning");

            // Act & Assert - This test mainly ensures no exceptions are thrown
            // In a real test environment, you'd capture the log output
            Assert.DoesNotThrow(() => _result.LogAll());
        }

        [Test]
        public void ValidationResult_MultipleErrorsAndWarnings_HandlesCorrectly()
        {
            // Arrange
            var errors = new[] { "Error 1", "Error 2", "Error 3" };
            var warnings = new[] { "Warning 1", "Warning 2" };

            // Act
            foreach (var error in errors)
                _result.AddError(error);
            
            foreach (var warning in warnings)
                _result.AddWarning(warning);

            // Assert
            Assert.IsFalse(_result.IsValid);
            Assert.IsTrue(_result.HasWarnings);
            Assert.AreEqual(errors.Length, _result.Errors.Count);
            Assert.AreEqual(warnings.Length, _result.Warnings.Count);

            for (int i = 0; i < errors.Length; i++)
            {
                Assert.IsTrue(_result.Errors[i].Contains(errors[i]));
            }

            for (int i = 0; i < warnings.Length; i++)
            {
                Assert.IsTrue(_result.Warnings[i].Contains(warnings[i]));
            }
        }

        [Test]
        public void ValidationResult_ReadOnlyCollections_WorkCorrectly()
        {
            // Arrange
            _result.AddError("Test error");
            _result.AddWarning("Test warning");

            // Act
            var readOnlyErrors = _result.Errors;
            var readOnlyWarnings = _result.Warnings;

            // Assert
            Assert.IsTrue(readOnlyErrors is IReadOnlyList<string>);
            Assert.IsTrue(readOnlyWarnings is IReadOnlyList<string>);
            Assert.AreEqual(1, readOnlyErrors.Count);
            Assert.AreEqual(1, readOnlyWarnings.Count);
        }
    }
}