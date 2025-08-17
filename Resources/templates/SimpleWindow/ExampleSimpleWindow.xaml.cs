using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AukettHeeseRevitAddin2024.Windows.Template
{
    /// <summary>
    /// Example Simple Window - Demonstrates Simple Window Template Usage
    /// Shows how to create a custom simple window with specific functionality
    /// </summary>
    public partial class ExampleSimpleWindow : SimpleWindowTemplate
    {
        #region Private Fields
        private Dictionary<string, object> _settings = new Dictionary<string, object>();
        #endregion

        #region Constructor
        public ExampleSimpleWindow()
        {
            InitializeComponent();
            
            // Configure the window
            WindowTitle = "Example Simple Window - Aukett + Heese";
            
            // Set up event handlers
            SettingsSaved += ExampleSimpleWindow_SettingsSaved;
            SettingsCancelled += ExampleSimpleWindow_SettingsCancelled;
            NavigationChanged += ExampleSimpleWindow_NavigationChanged;
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Called when the window is loaded
        /// </summary>
        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();
            
            // Initialize the simple window
            InitializeTabs();
            LoadDefaultSettings();
        }

        /// <summary>
        /// Load settings from configuration
        /// </summary>
        protected override void LoadSettings()
        {
            try
            {
                // Load settings from your configuration service
                // This is where you would load actual settings
                LoadDefaultSettings();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save settings to configuration
        /// </summary>
        protected override void SaveSettings()
        {
            try
            {
                // Save settings to your configuration service
                SaveSettingsFromUI();
                ShowSuccess("Settings saved successfully");
                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Called when help is requested
        /// </summary>
        protected override void OnHelpRequested()
        {
            MessageBox.Show(
                "This is the help information for the Example Admin Panel.\n\n" +
                "Use the navigation tabs on the left to switch between different settings categories.\n" +
                "Make changes to the settings and click OK to save them.\n" +
                "Click Cancel to discard any unsaved changes.",
                "Help - Example Admin Panel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Called when settings are saved
        /// </summary>
        protected override void OnSettingsSaved()
        {
            // Additional logic after settings are saved
            Console.WriteLine("Settings saved successfully");
        }

        /// <summary>
        /// Called when settings are cancelled
        /// </summary>
        protected override void OnSettingsCancelled()
        {
            // Additional logic after settings are cancelled
            Console.WriteLine("Settings changes were cancelled");
        }

        /// <summary>
        /// Called when navigation changes
        /// </summary>
        protected override void OnNavigationChanged(string tabName)
        {
            // Additional logic when navigation changes
            Console.WriteLine($"Switched to tab: {tabName}");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the tabs for the simple window
        /// </summary>
        private void InitializeTabs()
        {
            // Add tabs to the simple window
            AddTab("General", "General Settings", GeneralSettingsPanel);
            AddTab("Content", "Content Management", ContentManagementPanel);
            AddTab("Security", "Security", SecurityPanel);
            AddTab("Advanced", "Advanced", AdvancedPanel);
            AddTab("About", "About", AboutPanel);
        }

        /// <summary>
        /// Load default settings into the UI
        /// </summary>
        private void LoadDefaultSettings()
        {
            // General Settings
            ShowWelcomeMessageCheckBox.IsChecked = true;
            AutoLoadContentCheckBox.IsChecked = true;
            EnableNotificationsCheckBox.IsChecked = false;
            WindowSizeComboBox.SelectedIndex = 1; // Medium
            ThemeComboBox.SelectedIndex = 0; // Light
            LanguageComboBox.SelectedIndex = 0; // English

            // Content Management Settings
            ContentMetadataLocationTextBox.Text = @"C:\AukettHeese\Content";
            MaxItemsComboBox.SelectedIndex = 1; // 25
            ShowPreviewImagesCheckBox.IsChecked = true;
            EnableSearchCheckBox.IsChecked = true;

            // Security Settings
            RequirePasswordCheckBox.IsChecked = true;
            LogAdminActionsCheckBox.IsChecked = true;
            EnableAuditTrailCheckBox.IsChecked = false;
            PasswordExpirationTextBox.Text = "90";
            RequireComplexPasswordCheckBox.IsChecked = true;
            EnablePasswordHistoryCheckBox.IsChecked = true;

            // Advanced Settings
            EnableCachingCheckBox.IsChecked = true;
            CacheSizeTextBox.Text = "100";
            EnableBackgroundUpdatesCheckBox.IsChecked = false;
            AutoBackupCheckBox.IsChecked = true;
            BackupFrequencyComboBox.SelectedIndex = 1; // Weekly
            BackupLocationTextBox.Text = @"C:\AukettHeese\Backups";

            // Set up change tracking
            SetupChangeTracking();
        }

        /// <summary>
        /// Save settings from the UI
        /// </summary>
        private void SaveSettingsFromUI()
        {
            // Save General Settings
            _settings["ShowWelcomeMessage"] = ShowWelcomeMessageCheckBox.IsChecked ?? false;
            _settings["AutoLoadContent"] = AutoLoadContentCheckBox.IsChecked ?? false;
            _settings["EnableNotifications"] = EnableNotificationsCheckBox.IsChecked ?? false;
            _settings["WindowSize"] = WindowSizeComboBox.SelectedItem?.ToString() ?? "Medium";
            _settings["Theme"] = ThemeComboBox.SelectedItem?.ToString() ?? "Light";
            _settings["Language"] = LanguageComboBox.SelectedItem?.ToString() ?? "English";

            // Save Content Management Settings
            _settings["ContentMetadataLocation"] = ContentMetadataLocationTextBox.Text;
            _settings["MaxItems"] = MaxItemsComboBox.SelectedItem?.ToString() ?? "25";
            _settings["ShowPreviewImages"] = ShowPreviewImagesCheckBox.IsChecked ?? false;
            _settings["EnableSearch"] = EnableSearchCheckBox.IsChecked ?? false;

            // Save Security Settings
            _settings["RequirePassword"] = RequirePasswordCheckBox.IsChecked ?? false;
            _settings["LogAdminActions"] = LogAdminActionsCheckBox.IsChecked ?? false;
            _settings["EnableAuditTrail"] = EnableAuditTrailCheckBox.IsChecked ?? false;
            _settings["PasswordExpiration"] = PasswordExpirationTextBox.Text;
            _settings["RequireComplexPassword"] = RequireComplexPasswordCheckBox.IsChecked ?? false;
            _settings["EnablePasswordHistory"] = EnablePasswordHistoryCheckBox.IsChecked ?? false;

            // Save Advanced Settings
            _settings["EnableCaching"] = EnableCachingCheckBox.IsChecked ?? false;
            _settings["CacheSize"] = CacheSizeTextBox.Text;
            _settings["EnableBackgroundUpdates"] = EnableBackgroundUpdatesCheckBox.IsChecked ?? false;
            _settings["AutoBackup"] = AutoBackupCheckBox.IsChecked ?? false;
            _settings["BackupFrequency"] = BackupFrequencyComboBox.SelectedItem?.ToString() ?? "Weekly";
            _settings["BackupLocation"] = BackupLocationTextBox.Text;

            // Here you would save to your configuration service
            Console.WriteLine("Settings saved to configuration");
        }

        /// <summary>
        /// Set up change tracking for all controls
        /// </summary>
        private void SetupChangeTracking()
        {
            // General Settings
            ShowWelcomeMessageCheckBox.Checked += OnSettingChanged;
            ShowWelcomeMessageCheckBox.Unchecked += OnSettingChanged;
            AutoLoadContentCheckBox.Checked += OnSettingChanged;
            AutoLoadContentCheckBox.Unchecked += OnSettingChanged;
            EnableNotificationsCheckBox.Checked += OnSettingChanged;
            EnableNotificationsCheckBox.Unchecked += OnSettingChanged;
            WindowSizeComboBox.SelectionChanged += OnSettingChanged;
            ThemeComboBox.SelectionChanged += OnSettingChanged;
            LanguageComboBox.SelectionChanged += OnSettingChanged;

            // Content Management Settings
            ContentMetadataLocationTextBox.TextChanged += OnSettingChanged;
            MaxItemsComboBox.SelectionChanged += OnSettingChanged;
            ShowPreviewImagesCheckBox.Checked += OnSettingChanged;
            ShowPreviewImagesCheckBox.Unchecked += OnSettingChanged;
            EnableSearchCheckBox.Checked += OnSettingChanged;
            EnableSearchCheckBox.Unchecked += OnSettingChanged;

            // Security Settings
            RequirePasswordCheckBox.Checked += OnSettingChanged;
            RequirePasswordCheckBox.Unchecked += OnSettingChanged;
            LogAdminActionsCheckBox.Checked += OnSettingChanged;
            LogAdminActionsCheckBox.Unchecked += OnSettingChanged;
            EnableAuditTrailCheckBox.Checked += OnSettingChanged;
            EnableAuditTrailCheckBox.Unchecked += OnSettingChanged;
            PasswordExpirationTextBox.TextChanged += OnSettingChanged;
            RequireComplexPasswordCheckBox.Checked += OnSettingChanged;
            RequireComplexPasswordCheckBox.Unchecked += OnSettingChanged;
            EnablePasswordHistoryCheckBox.Checked += OnSettingChanged;
            EnablePasswordHistoryCheckBox.Unchecked += OnSettingChanged;

            // Advanced Settings
            EnableCachingCheckBox.Checked += OnSettingChanged;
            EnableCachingCheckBox.Unchecked += OnSettingChanged;
            CacheSizeTextBox.TextChanged += OnSettingChanged;
            EnableBackgroundUpdatesCheckBox.Checked += OnSettingChanged;
            EnableBackgroundUpdatesCheckBox.Unchecked += OnSettingChanged;
            AutoBackupCheckBox.Checked += OnSettingChanged;
            AutoBackupCheckBox.Unchecked += OnSettingChanged;
            BackupFrequencyComboBox.SelectionChanged += OnSettingChanged;
            BackupLocationTextBox.TextChanged += OnSettingChanged;
        }

        /// <summary>
        /// Handle setting changes
        /// </summary>
        private void OnSettingChanged(object sender, EventArgs e)
        {
            MarkAsModified();
        }
        #endregion

        #region Event Handlers
        private void ExampleSimpleWindow_SettingsSaved(object sender, SettingsSavedEventArgs e)
        {
            Console.WriteLine("Settings saved event fired");
        }

        private void ExampleSimpleWindow_SettingsCancelled(object sender, SettingsCancelledEventArgs e)
        {
            Console.WriteLine("Settings cancelled event fired");
        }

        private void ExampleSimpleWindow_NavigationChanged(object sender, NavigationEventArgs e)
        {
            Console.WriteLine($"Navigation changed to: {e.TabName}");
        }

        private void BrowseContentLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Content Metadata Location",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ContentMetadataLocationTextBox.Text = dialog.SelectedPath;
                MarkAsModified();
            }
        }

        private void BrowseBackupLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Backup Location",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupLocationTextBox.Text = dialog.SelectedPath;
                MarkAsModified();
            }
        }

        private void ExportConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "admin-config-export.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Here you would export the configuration
                    System.IO.File.WriteAllText(dialog.FileName, "Configuration export placeholder");
                    ShowSuccess("Configuration exported successfully");
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to export configuration: {ex.Message}");
                }
            }
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Here you would check for updates
                ShowSuccess("No updates available");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to check for updates: {ex.Message}");
            }
        }
        #endregion
    }
}
