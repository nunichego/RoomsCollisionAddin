using System.Threading.Tasks;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Interface for configuration service
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Get configuration value
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default(T));

        /// <summary>
        /// Set configuration value
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Save configuration
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Load configuration
        /// </summary>
        Task LoadAsync();
    }
}


