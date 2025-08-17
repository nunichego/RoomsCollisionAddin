using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AukettHeeseRevitAddin2024.Windows.Template
{
    /// <summary>
    /// Simple Window Template - Base class for all simple windows
    /// Provides comprehensive functionality for settings management, navigation, and configuration
    /// </summary>
    public partial class SimpleWindowTemplate : Window, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<NavigationEventArgs> NavigationChanged;
        public event EventHandler<SettingsSavedEventArgs> SettingsSaved;
        public event EventHandler<SettingsCancelledEventArgs> SettingsCancelled;
        #endregion

        #region Private Fields
        private string _currentTab = string.Empty;
        private bool _hasUnsavedChanges = false;
        private bool _isLoading = false;
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        private string _statusText = "Ready";
        private Color _statusColor = Colors.Green;
        private string _windowTitle = "Simple Window - Aukett + Heese";
        private List<AdminTabInfo> _tabs = new List<AdminTabInfo>();
        private Dictionary<string, UIElement> _tabPanels = new Dictionary<string, UIElement>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Current active tab
        /// </summary>
        public string CurrentTab
        {
            get => _currentTab;
            set
            {
                if (_currentTab != value)
                {
                    _currentTab = value;
                    OnPropertyChanged(nameof(CurrentTab));
                    UpdateNavigation();
                }
            }
        }

        /// <summary>
        /// Indicates if there are unsaved changes
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged(nameof(HasUnsavedChanges));
                    UpdateStatus();
                }
            }
        }

        /// <summary>
        /// Indicates if content is currently loading
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                    UpdateStatus();
                }
            }
        }

        /// <summary>
        /// Indicates if an error has occurred
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged(nameof(HasError));
                    UpdateStatus();
                }
            }
        }

        /// <summary>
        /// Error message when HasError is true
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        /// <summary>
        /// Status message in footer
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        /// <summary>
        /// Status indicator color
        /// </summary>
        public Color StatusColor
        {
            get => _statusColor;
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        /// <summary>
        /// Window title
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
                    Title = value;
                }
            }
        }

        /// <summary>
        /// List of available tabs
        /// </summary>
        public List<AdminTabInfo> Tabs
        {
            get => _tabs;
            set
            {
                if (_tabs != value)
                {
                    _tabs = value;
                    OnPropertyChanged(nameof(Tabs));
                    BuildNavigation();
                }
            }
        }
        #endregion

        #region Constructor
        public SimpleWindowTemplate()
        {
            InitializeComponent();
            Loaded += AdminPanelTemplate_Loaded;
            Closing += AdminPanelTemplate_Closing;
        }
        #endregion

        #region Event Handlers
        private void SimpleWindowTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OnWindowLoaded();
                LoadSettings();
                BuildNavigation();
                
                if (Tabs.Any())
                {
                    CurrentTab = Tabs.First().Name;
                }
            }
            catch (Exception ex)
            {
                HandleError("Failed to load simple window", ex);
            }
        }

        private void SimpleWindowTemplate_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (HasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes. Do you want to save them before closing?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveSettings();
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                OnWindowClosing(e);
            }
            catch (Exception ex)
            {
                HandleError("Error during window closing", ex);
            }
        }

        private void NavigationTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                CurrentTab = tabName;
                NavigationChanged?.Invoke(this, new NavigationEventArgs(tabName));
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            OnHelpRequested();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            SettingsCancelled?.Invoke(this, new SettingsCancelledEventArgs());
            OnSettingsCancelled();
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                SettingsSaved?.Invoke(this, new SettingsSavedEventArgs());
                OnSettingsSaved();
                Close();
            }
            catch (Exception ex)
            {
                HandleError("Failed to save settings", ex);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new tab to the simple window
        /// </summary>
        public void AddTab(string name, string displayName, UIElement content)
        {
            var tabInfo = new AdminTabInfo
            {
                Name = name,
                DisplayName = displayName
            };

            Tabs.Add(tabInfo);
            _tabPanels[name] = content;
        }

        /// <summary>
        /// Removes a tab from the simple window
        /// </summary>
        public void RemoveTab(string name)
        {
            var tab = Tabs.FirstOrDefault(t => t.Name == name);
            if (tab != null)
            {
                Tabs.Remove(tab);
                _tabPanels.Remove(name);
                
                if (CurrentTab == name && Tabs.Any())
                {
                    CurrentTab = Tabs.First().Name;
                }
            }
        }

        /// <summary>
        /// Shows an error message
        /// </summary>
        public void ShowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
            StatusText = "Error occurred";
            StatusColor = Colors.Red;
        }

        /// <summary>
        /// Shows a success message
        /// </summary>
        public void ShowSuccess(string message)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            StatusText = message;
            StatusColor = Colors.Green;
        }

        /// <summary>
        /// Shows a warning message
        /// </summary>
        public void ShowWarning(string message)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            StatusText = message;
            StatusColor = Colors.Orange;
        }

        /// <summary>
        /// Marks that there are unsaved changes
        /// </summary>
        public void MarkAsModified()
        {
            HasUnsavedChanges = true;
        }
        #endregion

        #region Protected Virtual Methods
        /// <summary>
        /// Called when the window is loaded
        /// </summary>
        protected virtual void OnWindowLoaded() { }

        /// <summary>
        /// Called when the window is closing
        /// </summary>
        protected virtual void OnWindowClosing(System.ComponentModel.CancelEventArgs e) { }

        /// <summary>
        /// Load settings from configuration
        /// </summary>
        protected virtual void LoadSettings() { }

        /// <summary>
        /// Save settings to configuration
        /// </summary>
        protected virtual void SaveSettings() { }

        /// <summary>
        /// Called when help is requested
        /// </summary>
        protected virtual void OnHelpRequested() { }

        /// <summary>
        /// Called when settings are saved
        /// </summary>
        protected virtual void OnSettingsSaved() { }

        /// <summary>
        /// Called when settings are cancelled
        /// </summary>
        protected virtual void OnSettingsCancelled() { }

        /// <summary>
        /// Called when navigation changes
        /// </summary>
        protected virtual void OnNavigationChanged(string tabName) { }
        #endregion

        #region Private Methods
        private void BuildNavigation()
        {
            var navigationPanel = FindName("NavigationPanel") as StackPanel;
            if (navigationPanel == null) return;

            navigationPanel.Children.Clear();

            foreach (var tab in Tabs)
            {
                var button = new Button
                {
                    Content = tab.DisplayName,
                    Tag = tab.Name,
                    Style = tab.Name == CurrentTab ? 
                        FindResource("ActiveNavigationTabStyle") as Style : 
                        FindResource("NavigationTabStyle") as Style
                };

                button.Click += NavigationTab_Click;
                navigationPanel.Children.Add(button);
            }
        }

        private void UpdateNavigation()
        {
            var navigationPanel = FindName("NavigationPanel") as StackPanel;
            if (navigationPanel == null) return;

            for (int i = 0; i < navigationPanel.Children.Count; i++)
            {
                if (navigationPanel.Children[i] is Button button)
                {
                    var tabName = button.Tag as string;
                    button.Style = tabName == CurrentTab ? 
                        FindResource("ActiveNavigationTabStyle") as Style : 
                        FindResource("NavigationTabStyle") as Style;
                }
            }

            UpdateContent();
            OnNavigationChanged(CurrentTab);
        }

        private void UpdateContent()
        {
            if (_tabPanels.ContainsKey(CurrentTab))
            {
                ContentGrid.Children.Clear();
                ContentGrid.Children.Add(_tabPanels[CurrentTab]);
            }
        }

        private void UpdateStatus()
        {
            if (HasError)
            {
                StatusText = $"Error: {ErrorMessage}";
                StatusColor = Colors.Red;
            }
            else if (IsLoading)
            {
                StatusText = "Loading...";
                StatusColor = Colors.Blue;
            }
            else if (HasUnsavedChanges)
            {
                StatusText = "You have unsaved changes";
                StatusColor = Colors.Orange;
            }
            else
            {
                StatusText = "Ready";
                StatusColor = Colors.Green;
            }
        }

        private void HandleError(string message, Exception ex)
        {
            ShowError($"{message}: {ex.Message}");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Information about an admin panel tab
    /// </summary>
    public class AdminTabInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for navigation changes
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string TabName { get; }

        public NavigationEventArgs(string tabName)
        {
            TabName = tabName;
        }
    }

    /// <summary>
    /// Event arguments for settings saved
    /// </summary>
    public class SettingsSavedEventArgs : EventArgs
    {
        public SettingsSavedEventArgs() { }
    }

    /// <summary>
    /// Event arguments for settings cancelled
    /// </summary>
    public class SettingsCancelledEventArgs : EventArgs
    {
        public SettingsCancelledEventArgs() { }
    }
    #endregion
}
