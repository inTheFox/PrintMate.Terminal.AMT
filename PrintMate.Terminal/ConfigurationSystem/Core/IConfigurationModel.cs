using System.Collections.Generic;

namespace PrintMate.Terminal.ConfigurationSystem.Core
{
    /// <summary>
    /// Marker interface for configuration models.
    /// All configuration classes should implement this interface.
    /// </summary>
    public interface IConfigurationModel
    {
        /// <summary>
        /// Validates the model properties.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        bool Validate(out List<string> errors);

        /// <summary>
        /// Called after the model is loaded from storage.
        /// Use this to initialize computed properties or fix legacy data.
        /// </summary>
        void OnLoaded();

        /// <summary>
        /// Called before the model is saved to storage.
        /// Use this to prepare data for serialization.
        /// </summary>
        void OnSaving();
    }
}
