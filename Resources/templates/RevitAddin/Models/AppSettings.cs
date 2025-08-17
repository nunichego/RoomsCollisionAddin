using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace RevitAddinTemplate.Models
{
    /// <summary>
    /// Application settings model
    /// Defines the configuration structure for the add-in
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        #region Private Fields
        private string _version = "1.0.0";
        private DateTime _lastUpdated = DateTime.Now;
        private bool _isFirstRun = true;
        private string _contentLibraryPath = string.Empty;
        private string _dynamoScriptsPath = string.Empty;
        private bool _enableLogging = true;
        private string _logLevel = "Information";
        private bool _showWelcomeMessage = true;
        private bool _autoLoadContent = false;
        private bool _enableNotifications = true;
        private string _theme = "Light";
        private string _language = "English";
        private int _maxItemsPerPage = 25;
        private bool _showPreviewImages = true;
        private bool _enableSearch = true;
        private bool _enableCaching = true;
        private int _cacheSizeMB = 100;
        private bool _enableBackgroundUpdates = false;
        private bool _autoBackup = true;
        private string _backupFrequency = "Weekly";
        private string _backupLocation = string.Empty;
        #endregion

        #region Properties
        /// <summary>
        /// Application version
        /// </summary>
        [JsonProperty("version")]
        public string Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged(nameof(Version));
                }
            }
        }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    OnPropertyChanged(nameof(LastUpdated));
                }
            }
        }

        /// <summary>
        /// Indicates if this is the first run
        /// </summary>
        [JsonProperty("isFirstRun")]
        public bool IsFirstRun
        {
            get => _isFirstRun;
            set
            {
                if (_isFirstRun != value)
                {
                    _isFirstRun = value;
                    OnPropertyChanged(nameof(IsFirstRun));
                }
            }
        }

        /// <summary>
        /// Content library path
        /// </summary>
        [JsonProperty("contentLibraryPath")]
        public string ContentLibraryPath
        {
            get => _contentLibraryPath;
            set
            {
                if (_contentLibraryPath != value)
                {
                    _contentLibraryPath = value;
                    OnPropertyChanged(nameof(ContentLibraryPath));
                }
            }
        }

        /// <summary>
        /// Dynamo scripts path
        /// </summary>
        [JsonProperty("dynamoScriptsPath")]
        public string DynamoScriptsPath
        {
            get => _dynamoScriptsPath;
            set
            {
                if (_dynamoScriptsPath != value)
                {
                    _dynamoScriptsPath = value;
                    OnPropertyChanged(nameof(DynamoScriptsPath));
                }
            }
        }

        /// <summary>
        /// Enable logging
        /// </summary>
        [JsonProperty("enableLogging")]
        public bool EnableLogging
        {
            get => _enableLogging;
            set
            {
                if (_enableLogging != value)
                {
                    _enableLogging = value;
                    OnPropertyChanged(nameof(EnableLogging));
                }
            }
        }

        /// <summary>
        /// Log level
        /// </summary>
        [JsonProperty("logLevel")]
        public string LogLevel
        {
            get => _logLevel;
            set
            {
                if (_logLevel != value)
                {
                    _logLevel = value;
                    OnPropertyChanged(nameof(LogLevel));
                }
            }
        }

        /// <summary>
        /// Show welcome message
        /// </summary>
        [JsonProperty("showWelcomeMessage")]
        public bool ShowWelcomeMessage
        {
            get => _showWelcomeMessage;
            set
            {
                if (_showWelcomeMessage != value)
                {
                    _showWelcomeMessage = value;
                    OnPropertyChanged(nameof(ShowWelcomeMessage));
                }
            }
        }

        /// <summary>
        /// Auto load content
        /// </summary>
        [JsonProperty("autoLoadContent")]
        public bool AutoLoadContent
        {
            get => _autoLoadContent;
            set
            {
                if (_autoLoadContent != value)
                {
                    _autoLoadContent = value;
                    OnPropertyChanged(nameof(AutoLoadContent));
                }
            }
        }

        /// <summary>
        /// Enable notifications
        /// </summary>
        [JsonProperty("enableNotifications")]
        public bool EnableNotifications
        {
            get => _enableNotifications;
            set
            {
                if (_enableNotifications != value)
                {
                    _enableNotifications = value;
                    OnPropertyChanged(nameof(EnableNotifications));
                }
            }
        }

        /// <summary>
        /// Application theme
        /// </summary>
        [JsonProperty("theme")]
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged(nameof(Theme));
                }
            }
        }

        /// <summary>
        /// Application language
        /// </summary>
        [JsonProperty("language")]
        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged(nameof(Language));
                }
            }
        }

        /// <summary>
        /// Maximum items per page
        /// </summary>
        [JsonProperty("maxItemsPerPage")]
        public int MaxItemsPerPage
        {
            get => _maxItemsPerPage;
            set
            {
                if (_maxItemsPerPage != value)
                {
                    _maxItemsPerPage = value;
                    OnPropertyChanged(nameof(MaxItemsPerPage));
                }
            }
        }

        /// <summary>
        /// Show preview images
        /// </summary>
        [JsonProperty("showPreviewImages")]
        public bool ShowPreviewImages
        {
            get => _showPreviewImages;
            set
            {
                if (_showPreviewImages != value)
                {
                    _showPreviewImages = value;
                    OnPropertyChanged(nameof(ShowPreviewImages));
                }
            }
        }

        /// <summary>
        /// Enable search
        /// </summary>
        [JsonProperty("enableSearch")]
        public bool EnableSearch
        {
            get => _enableSearch;
            set
            {
                if (_enableSearch != value)
                {
                    _enableSearch = value;
                    OnPropertyChanged(nameof(EnableSearch));
                }
            }
        }

        /// <summary>
        /// Enable caching
        /// </summary>
        [JsonProperty("enableCaching")]
        public bool EnableCaching
        {
            get => _enableCaching;
            set
            {
                if (_enableCaching != value)
                {
                    _enableCaching = value;
                    OnPropertyChanged(nameof(EnableCaching));
                }
            }
        }

        /// <summary>
        /// Cache size in MB
        /// </summary>
        [JsonProperty("cacheSizeMB")]
        public int CacheSizeMB
        {
            get => _cacheSizeMB;
            set
            {
                if (_cacheSizeMB != value)
                {
                    _cacheSizeMB = value;
                    OnPropertyChanged(nameof(CacheSizeMB));
                }
            }
        }

        /// <summary>
        /// Enable background updates
        /// </summary>
        [JsonProperty("enableBackgroundUpdates")]
        public bool EnableBackgroundUpdates
        {
            get => _enableBackgroundUpdates;
            set
            {
                if (_enableBackgroundUpdates != value)
                {
                    _enableBackgroundUpdates = value;
                    OnPropertyChanged(nameof(EnableBackgroundUpdates));
                }
            }
        }

        /// <summary>
        /// Auto backup
        /// </summary>
        [JsonProperty("autoBackup")]
        public bool AutoBackup
        {
            get => _autoBackup;
            set
            {
                if (_autoBackup != value)
                {
                    _autoBackup = value;
                    OnPropertyChanged(nameof(AutoBackup));
                }
            }
        }

        /// <summary>
        /// Backup frequency
        /// </summary>
        [JsonProperty("backupFrequency")]
        public string BackupFrequency
        {
            get => _backupFrequency;
            set
            {
                if (_backupFrequency != value)
                {
                    _backupFrequency = value;
                    OnPropertyChanged(nameof(BackupFrequency));
                }
            }
        }

        /// <summary>
        /// Backup location
        /// </summary>
        [JsonProperty("backupLocation")]
        public string BackupLocation
        {
            get => _backupLocation;
            set
            {
                if (_backupLocation != value)
                {
                    _backupLocation = value;
                    OnPropertyChanged(nameof(BackupLocation));
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create a copy of the settings
        /// </summary>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                Version = this.Version,
                LastUpdated = this.LastUpdated,
                IsFirstRun = this.IsFirstRun,
                ContentLibraryPath = this.ContentLibraryPath,
                DynamoScriptsPath = this.DynamoScriptsPath,
                EnableLogging = this.EnableLogging,
                LogLevel = this.LogLevel,
                ShowWelcomeMessage = this.ShowWelcomeMessage,
                AutoLoadContent = this.AutoLoadContent,
                EnableNotifications = this.EnableNotifications,
                Theme = this.Theme,
                Language = this.Language,
                MaxItemsPerPage = this.MaxItemsPerPage,
                ShowPreviewImages = this.ShowPreviewImages,
                EnableSearch = this.EnableSearch,
                EnableCaching = this.EnableCaching,
                CacheSizeMB = this.CacheSizeMB,
                EnableBackgroundUpdates = this.EnableBackgroundUpdates,
                AutoBackup = this.AutoBackup,
                BackupFrequency = this.BackupFrequency,
                BackupLocation = this.BackupLocation
            };
        }

        /// <summary>
        /// Update settings from another instance
        /// </summary>
        public void UpdateFrom(AppSettings other)
        {
            if (other == null) return;

            Version = other.Version;
            LastUpdated = other.LastUpdated;
            IsFirstRun = other.IsFirstRun;
            ContentLibraryPath = other.ContentLibraryPath;
            DynamoScriptsPath = other.DynamoScriptsPath;
            EnableLogging = other.EnableLogging;
            LogLevel = other.LogLevel;
            ShowWelcomeMessage = other.ShowWelcomeMessage;
            AutoLoadContent = other.AutoLoadContent;
            EnableNotifications = other.EnableNotifications;
            Theme = other.Theme;
            Language = other.Language;
            MaxItemsPerPage = other.MaxItemsPerPage;
            ShowPreviewImages = other.ShowPreviewImages;
            EnableSearch = other.EnableSearch;
            EnableCaching = other.EnableCaching;
            CacheSizeMB = other.CacheSizeMB;
            EnableBackgroundUpdates = other.EnableBackgroundUpdates;
            AutoBackup = other.AutoBackup;
            BackupFrequency = other.BackupFrequency;
            BackupLocation = other.BackupLocation;
        }
        #endregion
    }
}
