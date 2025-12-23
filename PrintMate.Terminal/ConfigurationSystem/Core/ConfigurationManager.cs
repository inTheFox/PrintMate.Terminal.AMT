using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using PrintMate.Terminal.ConfigurationSystem.Attributes;
using PrintMate.Terminal.ConfigurationSystem.Encryption;

namespace PrintMate.Terminal.ConfigurationSystem.Core
{
    /// <summary>
    /// Thread-safe configuration manager with atomic file operations, encryption, and debounced saves.
    /// Manages all application configuration models in a single JSON file.
    /// </summary>
    public sealed class ConfigurationManager : IDisposable
    {
        private readonly string _configFilePath;
        private readonly string _encryptionPassphrase;
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly ConcurrentDictionary<Type, object> _models = new();
        private readonly Timer _saveTimer;
        private readonly TimeSpan _saveDebounceInterval;
        private bool _isDirty = false;
        private bool _disposed = false;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = true, // Required for serializing public fields (e.g., PlcSettings.Address)
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Creates a new configuration manager.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration JSON file</param>
        /// <param name="encryptionPassphrase">Passphrase for encrypting sensitive properties (leave null to disable encryption)</param>
        /// <param name="autoSaveDebounceMs">Milliseconds to wait before auto-saving after changes (default: 2000ms)</param>
        public ConfigurationManager(string configFilePath, string? encryptionPassphrase = null, int autoSaveDebounceMs = 2000)
        {
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            _encryptionPassphrase = encryptionPassphrase ?? string.Empty;
            _saveDebounceInterval = TimeSpan.FromMilliseconds(autoSaveDebounceMs);
            _saveTimer = new Timer(SaveTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            EnsureDirectoryExists();
            LoadFromFile();
        }

        #region Public API

        /// <summary>
        /// Gets a configuration model. Creates with default values if not exists.
        /// Thread-safe for concurrent reads.
        /// </summary>
        public T Get<T>() where T : class, IConfigurationModel, new()
        {
            _lock.EnterReadLock();
            try
            {
                if (_models.TryGetValue(typeof(T), out var model))
                {
                    return (T)model;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // Model doesn't exist - create with defaults
            _lock.EnterWriteLock();
            try
            {
                // Double-check after acquiring write lock
                if (_models.TryGetValue(typeof(T), out var model))
                {
                    return (T)model;
                }

                var newModel = new T();
                newModel.OnLoaded();
                _models[typeof(T)] = newModel;
                MarkDirty(); // Save new model to file
                return newModel;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Updates a configuration model and triggers auto-save.
        /// Thread-safe for concurrent writes.
        /// </summary>
        /// <param name="updateAction">Action to modify the model</param>
        public void Update<T>(Action<T> updateAction) where T : class, IConfigurationModel, new()
        {
            if (updateAction == null)
                throw new ArgumentNullException(nameof(updateAction));

            _lock.EnterWriteLock();
            try
            {
                var model = Get<T>();
                updateAction(model);

                // Validate after update
                if (!model.Validate(out var errors))
                {
                    throw new InvalidOperationException(
                        $"Configuration validation failed for {typeof(T).Name}:\n{string.Join("\n", errors)}");
                }

                MarkDirty();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Resets a model to default values.
        /// </summary>
        public void Reset<T>() where T : class, IConfigurationModel, new()
        {
            _lock.EnterWriteLock();
            try
            {
                var defaultModel = new T();
                defaultModel.OnLoaded();
                _models[typeof(T)] = defaultModel;
                MarkDirty();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Forces immediate save to file (bypassing debounce).
        /// </summary>
        public void SaveNow()
        {
            _lock.EnterReadLock();
            try
            {
                SaveToFileInternal();
                _isDirty = false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Reloads configuration from file, discarding in-memory changes.
        /// </summary>
        public void Reload()
        {
            _lock.EnterWriteLock();
            try
            {
                _models.Clear();
                LoadFromFile();
                _isDirty = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Validates all loaded models.
        /// </summary>
        public bool ValidateAll(out Dictionary<Type, List<string>> errors)
        {
            errors = new Dictionary<Type, List<string>>();
            bool allValid = true;

            _lock.EnterReadLock();
            try
            {
                foreach (var kvp in _models)
                {
                    if (kvp.Value is IConfigurationModel model)
                    {
                        if (!model.Validate(out var modelErrors))
                        {
                            errors[kvp.Key] = modelErrors;
                            allValid = false;
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return allValid;
        }

        #endregion

        #region File Operations

        private void LoadFromFile()
        {
            if (!File.Exists(_configFilePath))
            {
                // No config file - will use default values
                return;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                var document = JsonDocument.Parse(json);

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var typeName = property.Name;
                    var modelType = FindModelType(typeName);

                    if (modelType == null)
                        continue; // Skip unknown types

                    var modelJson = property.Value.GetRawText();
                    var model = JsonSerializer.Deserialize(modelJson, modelType, _jsonOptions) as IConfigurationModel;

                    if (model != null)
                    {
                        // Decrypt encrypted properties
                        DecryptModel(model);
                        model.OnLoaded();
                        _models[modelType] = model;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {_configFilePath}", ex);
            }
        }

        private void SaveToFileInternal()
        {
            try
            {
                // Prepare models for saving
                var modelsToSave = new Dictionary<string, object>();

                foreach (var kvp in _models)
                {
                    if (kvp.Value is IConfigurationModel model)
                    {
                        model.OnSaving();

                        // Clone and encrypt sensitive properties
                        var clonedModel = CloneModel(model);
                        EncryptModel(clonedModel);

                        modelsToSave[kvp.Key.Name] = clonedModel;
                    }
                }

                var json = JsonSerializer.Serialize(modelsToSave, _jsonOptions);

                // Atomic write: write to temp file, then rename
                var tempFile = _configFilePath + ".tmp";
                var backupFile = _configFilePath + ".bak";

                // Write to temp file
                File.WriteAllText(tempFile, json);

                // Create backup of existing file
                if (File.Exists(_configFilePath))
                {
                    File.Copy(_configFilePath, backupFile, overwrite: true);
                }

                // Atomic rename (replaces existing file)
                File.Move(tempFile, _configFilePath, overwrite: true);

                // Success - can delete temp if it still exists
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration to {_configFilePath}", ex);
            }
        }

        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        #endregion

        #region Encryption

        private void EncryptModel(IConfigurationModel model)
        {
            if (string.IsNullOrEmpty(_encryptionPassphrase))
                return;

            var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string))
                    continue;

                var encryptedAttr = prop.GetCustomAttribute<EncryptedAttribute>();
                if (encryptedAttr == null)
                    continue;

                var value = prop.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var encrypted = AesEncryption.Encrypt(value, _encryptionPassphrase);
                    prop.SetValue(model, encrypted);
                }
            }
        }

        private void DecryptModel(IConfigurationModel model)
        {
            if (string.IsNullOrEmpty(_encryptionPassphrase))
                return;

            var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string))
                    continue;

                var encryptedAttr = prop.GetCustomAttribute<EncryptedAttribute>();
                if (encryptedAttr == null)
                    continue;

                var value = prop.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        var decrypted = AesEncryption.Decrypt(value, _encryptionPassphrase);
                        prop.SetValue(model, decrypted);
                    }
                    catch (CryptographicException)
                    {
                        // Decryption failed - possibly wrong passphrase or corrupted data
                        // Leave the encrypted value as-is or throw based on requirements
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private void MarkDirty()
        {
            _isDirty = true;
            // Reset debounce timer
            _saveTimer.Change(_saveDebounceInterval, Timeout.InfiniteTimeSpan);
        }

        private void SaveTimerCallback(object? state)
        {
            if (_isDirty)
            {
                try
                {
                    SaveNow();
                }
                catch (Exception ex)
                {
                    // Log error but don't throw (we're in a timer callback)
                    Console.WriteLine($"Auto-save failed: {ex.Message}");
                }
            }
        }

        private Type? FindModelType(string typeName)
        {
            // Search in current assembly for matching type
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                                    typeof(IConfigurationModel).IsAssignableFrom(t));
        }

        private IConfigurationModel CloneModel(IConfigurationModel model)
        {
            var json = JsonSerializer.Serialize(model, model.GetType(), _jsonOptions);
            return (IConfigurationModel)JsonSerializer.Deserialize(json, model.GetType(), _jsonOptions)!;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            // Save any pending changes
            if (_isDirty)
            {
                try
                {
                    SaveNow();
                }
                catch
                {
                    // Ignore errors during dispose
                }
            }

            _saveTimer?.Dispose();
            _lock?.Dispose();
            _disposed = true;
        }

        #endregion
    }
}
