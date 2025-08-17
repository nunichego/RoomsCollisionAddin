using System;
using System.Threading.Tasks;
using RevitAddinTemplate.Models;

namespace RevitAddinTemplate.Services
{
    /// <summary>
    /// Configuration service interface
    /// Defines the contract for managing application configuration
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Initialize the configuration service
        /// </summary>
        void Initialize();

        /// <summary>
        /// Load configuration from file
        /// </summary>
        /// <typeparam name="T">Type of configuration to load</typeparam>
        /// <param name="configPath">Path to configuration file</param>
        /// <returns>Configuration object</returns>
        T LoadConfiguration<T>(string configPath) where T : class, new();

        /// <summary>
        /// Load configuration from file asynchronously
        /// </summary>
        /// <typeparam name="T">Type of configuration to load</typeparam>
        /// <param name="configPath">Path to configuration file</param>
        /// <returns>Configuration object</returns>
        Task<T> LoadConfigurationAsync<T>(string configPath) where T : class, new();

        /// <summary>
        /// Save configuration to file
        /// </summary>
        /// <typeparam name="T">Type of configuration to save</typeparam>
        /// <param name="config">Configuration object</param>
        /// <param name="configPath">Path to configuration file</param>
        void SaveConfiguration<T>(T config, string configPath) where T : class;

        /// <summary>
        /// Save configuration to file asynchronously
        /// </summary>
        /// <typeparam name="T">Type of configuration to save</typeparam>
        /// <param name="config">Configuration object</param>
        /// <param name="configPath">Path to configuration file</param>
        Task SaveConfigurationAsync<T>(T config, string configPath) where T : class;

        /// <summary>
        /// Get application settings
        /// </summary>
        /// <returns>Application settings</returns>
        AppSettings GetAppSettings();

        /// <summary>
        /// Save application settings
        /// </summary>
        /// <param name="settings">Application settings</param>
        void SaveAppSettings(AppSettings settings);

        /// <summary>
        /// Get admin configuration
        /// </summary>
        /// <returns>Admin configuration</returns>
        AdminConfig GetAdminConfig();

        /// <summary>
        /// Save admin configuration
        /// </summary>
        /// <param name="config">Admin configuration</param>
        void SaveAdminConfig(AdminConfig config);

        /// <summary>
        /// Get master configuration
        /// </summary>
        /// <returns>Master configuration</returns>
        MasterConfig GetMasterConfig();

        /// <summary>
        /// Save master configuration
        /// </summary>
        /// <param name="config">Master configuration</param>
        void SaveMasterConfig(MasterConfig config);

        /// <summary>
        /// Validate configuration
        /// </summary>
        /// <typeparam name="T">Type of configuration to validate</typeparam>
        /// <param name="config">Configuration object</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateConfiguration<T>(T config) where T : class;

        /// <summary>
        /// Create default configuration
        /// </summary>
        /// <typeparam name="T">Type of configuration to create</typeparam>
        /// <returns>Default configuration object</returns>
        T CreateDefaultConfiguration<T>() where T : class, new();

        /// <summary>
        /// Backup configuration
        /// </summary>
        /// <param name="configPath">Path to configuration file</param>
        /// <param name="backupPath">Path to backup file</param>
        void BackupConfiguration(string configPath, string backupPath);

        /// <summary>
        /// Restore configuration from backup
        /// </summary>
        /// <param name="backupPath">Path to backup file</param>
        /// <param name="configPath">Path to configuration file</param>
        void RestoreConfiguration(string backupPath, string configPath);
    }
}
