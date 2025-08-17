using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RevitAddinTemplate.Models;

namespace RevitAddinTemplate.Services
{
    /// <summary>
    /// Configuration service implementation
    /// Provides JSON-based configuration management with validation and backup capabilities
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        #region Private Fields
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configDirectory;
        private readonly JsonSerializerSettings _jsonSettings;
        #endregion

        #region Constants
        private const string CONFIG_DIR = "Config";
        private const string APP_SETTINGS_FILE = "app-settings.json";
        private const string ADMIN_CONFIG_FILE = "admin-config.json";
        private const string MASTER_CONFIG_FILE = "master-config.json";
        private const string BACKUP_DIR = "Backups";
        #endregion

        #region Constructor
        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            
            // Set up configuration directory
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _configDirectory = Path.Combine(appDataPath, "RevitAddinTemplate", CONFIG_DIR);
            
            // Ensure directory exists
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            // Configure JSON serialization
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include
            };
        }
        #endregion

        #region IConfigurationService Implementation
        public void Initialize()
        {
            try
            {
                _logger?.LogInformation("Initializing ConfigurationService");

                // Create backup directory
                var backupDir = Path.Combine(_configDirectory, BACKUP_DIR);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Initialize default configurations if they don't exist
                InitializeDefaultConfigurations();

                _logger?.LogInformation("ConfigurationService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize ConfigurationService");
                throw;
            }
        }

        public T LoadConfiguration<T>(string configPath) where T : class, new()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    _logger?.LogWarning($"Configuration file not found: {configPath}");
                    return CreateDefaultConfiguration<T>();
                }

                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<T>(json, _jsonSettings);

                if (config == null)
                {
                    _logger?.LogWarning($"Failed to deserialize configuration from: {configPath}");
                    return CreateDefaultConfiguration<T>();
                }

                _logger?.LogInformation($"Configuration loaded successfully from: {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to load configuration from: {configPath}");
                return CreateDefaultConfiguration<T>();
            }
        }

        public async Task<T> LoadConfigurationAsync<T>(string configPath) where T : class, new()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    _logger?.LogWarning($"Configuration file not found: {configPath}");
                    return CreateDefaultConfiguration<T>();
                }

                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<T>(json, _jsonSettings);

                if (config == null)
                {
                    _logger?.LogWarning($"Failed to deserialize configuration from: {configPath}");
                    return CreateDefaultConfiguration<T>();
                }

                _logger?.LogInformation($"Configuration loaded successfully from: {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to load configuration from: {configPath}");
                return CreateDefaultConfiguration<T>();
            }
        }

        public void SaveConfiguration<T>(T config, string configPath) where T : class
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                // Validate configuration
                if (!ValidateConfiguration(config))
                {
                    throw new InvalidOperationException("Configuration validation failed");
                }

                // Create backup before saving
                if (File.Exists(configPath))
                {
                    BackupConfiguration(configPath, GetBackupPath(configPath));
                }

                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var json = JsonConvert.SerializeObject(config, _jsonSettings);
                File.WriteAllText(configPath, json);

                _logger?.LogInformation($"Configuration saved successfully to: {configPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to save configuration to: {configPath}");
                throw;
            }
        }

        public async Task SaveConfigurationAsync<T>(T config, string configPath) where T : class
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                // Validate configuration
                if (!ValidateConfiguration(config))
                {
                    throw new InvalidOperationException("Configuration validation failed");
                }

                // Create backup before saving
                if (File.Exists(configPath))
                {
                    BackupConfiguration(configPath, GetBackupPath(configPath));
                }

                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var json = JsonConvert.SerializeObject(config, _jsonSettings);
                await File.WriteAllTextAsync(configPath, json);

                _logger?.LogInformation($"Configuration saved successfully to: {configPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to save configuration to: {configPath}");
                throw;
            }
        }

        public AppSettings GetAppSettings()
        {
            var configPath = Path.Combine(_configDirectory, APP_SETTINGS_FILE);
            return LoadConfiguration<AppSettings>(configPath);
        }

        public void SaveAppSettings(AppSettings settings)
        {
            var configPath = Path.Combine(_configDirectory, APP_SETTINGS_FILE);
            SaveConfiguration(settings, configPath);
        }

        public AdminConfig GetAdminConfig()
        {
            var configPath = Path.Combine(_configDirectory, ADMIN_CONFIG_FILE);
            return LoadConfiguration<AdminConfig>(configPath);
        }

        public void SaveAdminConfig(AdminConfig config)
        {
            var configPath = Path.Combine(_configDirectory, ADMIN_CONFIG_FILE);
            SaveConfiguration(config, configPath);
        }

        public MasterConfig GetMasterConfig()
        {
            var configPath = Path.Combine(_configDirectory, MASTER_CONFIG_FILE);
            return LoadConfiguration<MasterConfig>(configPath);
        }

        public void SaveMasterConfig(MasterConfig config)
        {
            var configPath = Path.Combine(_configDirectory, MASTER_CONFIG_FILE);
            SaveConfiguration(config, configPath);
        }

        public bool ValidateConfiguration<T>(T config) where T : class
        {
            try
            {
                if (config == null)
                {
                    return false;
                }

                // Basic validation - ensure object can be serialized
                var json = JsonConvert.SerializeObject(config, _jsonSettings);
                var deserialized = JsonConvert.DeserializeObject<T>(json, _jsonSettings);

                return deserialized != null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Configuration validation failed");
                return false;
            }
        }

        public T CreateDefaultConfiguration<T>() where T : class, new()
        {
            try
            {
                var config = new T();

                // Set default values based on type
                if (config is AppSettings appSettings)
                {
                    appSettings.Version = "1.0.0";
                    appSettings.LastUpdated = DateTime.Now;
                    appSettings.IsFirstRun = true;
                }
                else if (config is AdminConfig adminConfig)
                {
                    adminConfig.AdminPassword = "MisterBim";
                    adminConfig.RequirePassword = true;
                    adminConfig.LogAdminActions = true;
                }
                else if (config is MasterConfig masterConfig)
                {
                    masterConfig.Version = "1.0.0";
                    masterConfig.CreatedDate = DateTime.Now;
                    masterConfig.LastModified = DateTime.Now;
                }

                _logger?.LogInformation($"Default configuration created for type: {typeof(T).Name}");
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to create default configuration for type: {typeof(T).Name}");
                return new T();
            }
        }

        public void BackupConfiguration(string configPath, string backupPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    _logger?.LogWarning($"Cannot backup non-existent file: {configPath}");
                    return;
                }

                // Ensure backup directory exists
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                File.Copy(configPath, backupPath, true);
                _logger?.LogInformation($"Configuration backed up to: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to backup configuration from {configPath} to {backupPath}");
            }
        }

        public void RestoreConfiguration(string backupPath, string configPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger?.LogWarning($"Backup file not found: {backupPath}");
                    return;
                }

                // Ensure target directory exists
                var targetDir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(backupPath, configPath, true);
                _logger?.LogInformation($"Configuration restored from: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to restore configuration from {backupPath} to {configPath}");
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize default configurations
        /// </summary>
        private void InitializeDefaultConfigurations()
        {
            // Initialize app settings
            var appSettingsPath = Path.Combine(_configDirectory, APP_SETTINGS_FILE);
            if (!File.Exists(appSettingsPath))
            {
                var appSettings = CreateDefaultConfiguration<AppSettings>();
                SaveConfiguration(appSettings, appSettingsPath);
            }

            // Initialize admin config
            var adminConfigPath = Path.Combine(_configDirectory, ADMIN_CONFIG_FILE);
            if (!File.Exists(adminConfigPath))
            {
                var adminConfig = CreateDefaultConfiguration<AdminConfig>();
                SaveConfiguration(adminConfig, adminConfigPath);
            }

            // Initialize master config
            var masterConfigPath = Path.Combine(_configDirectory, MASTER_CONFIG_FILE);
            if (!File.Exists(masterConfigPath))
            {
                var masterConfig = CreateDefaultConfiguration<MasterConfig>();
                SaveConfiguration(masterConfig, masterConfigPath);
            }
        }

        /// <summary>
        /// Get backup path for a configuration file
        /// </summary>
        private string GetBackupPath(string configPath)
        {
            var fileName = Path.GetFileName(configPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
            return Path.Combine(_configDirectory, BACKUP_DIR, backupFileName);
        }
        #endregion
    }
}
