using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Configuration service implementation
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly string _configFilePath;

        public ConfigurationService()
        {
            _configuration = new Dictionary<string, object>();
            _configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RoomsManagerAddin",
                "config.json"
            );

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (_configuration.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set configuration value
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            _configuration[key] = value;
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                var json = "{}"; // Simplified for now
                File.WriteAllText(_configFilePath, json);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Load configuration
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var loadedConfig = new Dictionary<string, object>(); // Simplified for now
                    
                    if (loadedConfig != null)
                    {
                        _configuration.Clear();
                        foreach (var kvp in loadedConfig)
                        {
                            _configuration[kvp.Key] = kvp.Value;
                        }
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }
    }
}
