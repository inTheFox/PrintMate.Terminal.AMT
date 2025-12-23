using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PrintMate.Terminal.ConfigurationSystem.Core
{
    /// <summary>
    /// Base class for all configuration models.
    /// Provides validation support and lifecycle hooks.
    /// </summary>
    public abstract class ConfigurationModelBase : IConfigurationModel
    {
        /// <summary>
        /// Validates all properties decorated with validation attributes.
        /// </summary>
        public virtual bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(this, context, results, validateAllProperties: true);

            if (!isValid)
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            }

            return isValid;
        }

        /// <summary>
        /// Override this to perform custom logic after loading from storage.
        /// </summary>
        public virtual void OnLoaded()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Override this to perform custom logic before saving to storage.
        /// </summary>
        public virtual void OnSaving()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Creates a deep clone of this configuration model.
        /// </summary>
        public T Clone<T>() where T : ConfigurationModelBase
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
