using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AukettHeeseRevitAddin2024.Windows.Template
{
    /// <summary>
    /// Modern Window Template - Base class for all application windows
    /// Provides comprehensive functionality for content browsing, searching, and management
    /// </summary>
    public partial class ModernWindowTemplate : Window, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ContentItemEventArgs> ContentItemSelected;
        public event EventHandler<SearchEventArgs> SearchPerformed;
        public event EventHandler<FilterEventArgs> FilterApplied;
        public event EventHandler<SortEventArgs> SortApplied;
        #endregion

        #region Private Fields
        private string _searchText = string.Empty;
        private bool _isLoading = false;
        private bool _hasError = false;
        private bool _isEmpty = false;
        private bool _hasContent = true;
        private string _errorMessage = string.Empty;
        private string _emptyMessage = "No content available.";
        private string _statusText = "Ready";
        private Color _statusColor = Colors.Green;
        private int _gridColumns = 3;
        private bool _showRefreshButton = true;
        private bool _showSettingsButton = false;
        private bool _showToolbar = true;
        private bool _showFilterButton = true;
        private bool _showSortButton = true;
        private bool _showAddButton = false;
        private bool _showHelpButton = false;
        private string _headerTitle = "🎨 Aukett + Heese Window";
        private string _headerSubtitle = "Modern window template with scalable design";
        private string _windowTitle = "Modern Window - Aukett + Heese";
        private IEnumerable<object> _items = new List<object>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Search text for filtering content
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    PerformSearch();
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
                    UpdateContentVisibility();
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
                    UpdateContentVisibility();
                }
            }
        }

        /// <summary>
        /// Indicates if no content is available
        /// </summary>
        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (_isEmpty != value)
                {
                    _isEmpty = value;
                    OnPropertyChanged(nameof(IsEmpty));
                    UpdateContentVisibility();
                }
            }
        }

        /// <summary>
        /// Indicates if content is available to display
        /// </summary>
        public bool HasContent
        {
            get => _hasContent;
            set
            {
                if (_hasContent != value)
                {
                    _hasContent = value;
                    OnPropertyChanged(nameof(HasContent));
                }
            }
        }

        /// <summary>
        /// Error message to display when HasError is true
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
        /// Message to display when no content is available
        /// </summary>
        public string EmptyMessage
        {
            get => _emptyMessage;
            set
            {
                if (_emptyMessage != value)
                {
                    _emptyMessage = value;
                    OnPropertyChanged(nameof(EmptyMessage));
                }
            }
        }

        /// <summary>
        /// Status text displayed in the footer
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
        /// Status color for the footer indicator
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
        /// Number of columns in the grid layout
        /// </summary>
        public int GridColumns
        {
            get => _gridColumns;
            set
            {
                if (_gridColumns != value)
                {
                    _gridColumns = value;
                    OnPropertyChanged(nameof(GridColumns));
                }
            }
        }

        /// <summary>
        /// Collection of items to display
        /// </summary>
        public IEnumerable<object> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                    UpdateContentState();
                }
            }
        }

        #region UI Visibility Properties
        public bool ShowRefreshButton
        {
            get => _showRefreshButton;
            set
            {
                if (_showRefreshButton != value)
                {
                    _showRefreshButton = value;
                    OnPropertyChanged(nameof(ShowRefreshButton));
                }
            }
        }

        public bool ShowSettingsButton
        {
            get => _showSettingsButton;
            set
            {
                if (_showSettingsButton != value)
                {
                    _showSettingsButton = value;
                    OnPropertyChanged(nameof(ShowSettingsButton));
                }
            }
        }

        public bool ShowToolbar
        {
            get => _showToolbar;
            set
            {
                if (_showToolbar != value)
                {
                    _showToolbar = value;
                    OnPropertyChanged(nameof(ShowToolbar));
                }
            }
        }

        public bool ShowFilterButton
        {
            get => _showFilterButton;
            set
            {
                if (_showFilterButton != value)
                {
                    _showFilterButton = value;
                    OnPropertyChanged(nameof(ShowFilterButton));
                }
            }
        }

        public bool ShowSortButton
        {
            get => _showSortButton;
            set
            {
                if (_showSortButton != value)
                {
                    _showSortButton = value;
                    OnPropertyChanged(nameof(ShowSortButton));
                }
            }
        }

        public bool ShowAddButton
        {
            get => _showAddButton;
            set
            {
                if (_showAddButton != value)
                {
                    _showAddButton = value;
                    OnPropertyChanged(nameof(ShowAddButton));
                }
            }
        }

        public bool ShowHelpButton
        {
            get => _showHelpButton;
            set
            {
                if (_showHelpButton != value)
                {
                    _showHelpButton = value;
                    OnPropertyChanged(nameof(ShowHelpButton));
                }
            }
        }

        public string HeaderTitle
        {
            get => _headerTitle;
            set
            {
                if (_headerTitle != value)
                {
                    _headerTitle = value;
                    OnPropertyChanged(nameof(HeaderTitle));
                }
            }
        }

        public string HeaderSubtitle
        {
            get => _headerSubtitle;
            set
            {
                if (_headerSubtitle != value)
                {
                    _headerSubtitle = value;
                    OnPropertyChanged(nameof(HeaderSubtitle));
                }
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }
        #endregion
        #endregion

        #region Constructor
        public ModernWindowTemplate()
        {
            InitializeComponent();
            DataContext = this;
            SetupEventHandlers();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Sets up event handlers for the window
        /// </summary>
        private void SetupEventHandlers()
        {
            // Window events
            Loaded += ModernWindowTemplate_Loaded;
            Closing += ModernWindowTemplate_Closing;
        }

        /// <summary>
        /// Handles window loaded event
        /// </summary>
        private void ModernWindowTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OnWindowLoaded();
                LoadContent();
            }
            catch (Exception ex)
            {
                HandleError("Failed to load window content", ex);
            }
        }

        /// <summary>
        /// Handles window closing event
        /// </summary>
        private void ModernWindowTemplate_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                OnWindowClosing(e);
            }
            catch (Exception ex)
            {
                // Log error but don't prevent closing
                System.Diagnostics.Debug.WriteLine($"Error during window closing: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles refresh button click
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshContent();
            }
            catch (Exception ex)
            {
                HandleError("Failed to refresh content", ex);
            }
        }

        /// <summary>
        /// Handles settings button click
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnSettingsRequested();
            }
            catch (Exception ex)
            {
                HandleError("Failed to open settings", ex);
            }
        }

        /// <summary>
        /// Handles search box text changed
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Search is handled by the SearchText property setter
            }
            catch (Exception ex)
            {
                HandleError("Failed to process search", ex);
            }
        }

        /// <summary>
        /// Handles filter button click
        /// </summary>
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnFilterRequested();
            }
            catch (Exception ex)
            {
                HandleError("Failed to apply filter", ex);
            }
        }

        /// <summary>
        /// Handles sort button click
        /// </summary>
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnSortRequested();
            }
            catch (Exception ex)
            {
                HandleError("Failed to apply sorting", ex);
            }
        }

        /// <summary>
        /// Handles add button click
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnAddRequested();
            }
            catch (Exception ex)
            {
                HandleError("Failed to add new item", ex);
            }
        }

        /// <summary>
        /// Handles retry button click
        /// </summary>
        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadContent();
            }
            catch (Exception ex)
            {
                HandleError("Failed to retry loading", ex);
            }
        }

        /// <summary>
        /// Handles help button click
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnHelpRequested();
            }
            catch (Exception ex)
            {
                HandleError("Failed to show help", ex);
            }
        }

        /// <summary>
        /// Handles close button click
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                HandleError("Failed to close window", ex);
            }
        }

        /// <summary>
        /// Handles view details button click
        /// </summary>
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is object item)
                {
                    OnContentItemSelected(item, "ViewDetails");
                }
            }
            catch (Exception ex)
            {
                HandleError("Failed to view details", ex);
            }
        }

        /// <summary>
        /// Handles open button click
        /// </summary>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is object item)
                {
                    OnContentItemSelected(item, "Open");
                }
            }
            catch (Exception ex)
            {
                HandleError("Failed to open item", ex);
            }
        }
        #endregion

        #region Virtual Methods - Override in derived classes
        /// <summary>
        /// Called when the window is loaded
        /// </summary>
        protected virtual void OnWindowLoaded()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when the window is closing
        /// </summary>
        protected virtual void OnWindowClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Loads the content for the window
        /// </summary>
        protected virtual void LoadContent()
        {
            // Override in derived classes
            IsLoading = true;
            
            // Simulate loading
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                    UpdateContentState();
                });
            });
        }

        /// <summary>
        /// Refreshes the content
        /// </summary>
        protected virtual void RefreshContent()
        {
            LoadContent();
        }

        /// <summary>
        /// Called when settings are requested
        /// </summary>
        protected virtual void OnSettingsRequested()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when filter is requested
        /// </summary>
        protected virtual void OnFilterRequested()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when sort is requested
        /// </summary>
        protected virtual void OnSortRequested()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when add is requested
        /// </summary>
        protected virtual void OnAddRequested()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when help is requested
        /// </summary>
        protected virtual void OnHelpRequested()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when a content item is selected
        /// </summary>
        protected virtual void OnContentItemSelected(object item, string action)
        {
            ContentItemSelected?.Invoke(this, new ContentItemEventArgs(item, action));
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Performs search on the content
        /// </summary>
        private void PerformSearch()
        {
            try
            {
                SearchPerformed?.Invoke(this, new SearchEventArgs(SearchText));
            }
            catch (Exception ex)
            {
                HandleError("Failed to perform search", ex);
            }
        }

        /// <summary>
        /// Updates the content visibility based on current state
        /// </summary>
        private void UpdateContentVisibility()
        {
            if (IsLoading)
            {
                HasContent = false;
                HasError = false;
                IsEmpty = false;
            }
            else if (HasError)
            {
                HasContent = false;
                IsEmpty = false;
            }
            else if (IsEmpty)
            {
                HasContent = false;
                HasError = false;
            }
            else
            {
                HasContent = true;
                HasError = false;
                IsEmpty = false;
            }
        }

        /// <summary>
        /// Updates the content state based on available items
        /// </summary>
        private void UpdateContentState()
        {
            if (Items == null || !Items.Any())
            {
                IsEmpty = true;
                StatusText = "No content available";
                StatusColor = Colors.Orange;
            }
            else
            {
                IsEmpty = false;
                StatusText = $"Loaded {Items.Count()} items";
                StatusColor = Colors.Green;
            }
        }

        /// <summary>
        /// Handles errors in the window
        /// </summary>
        private void HandleError(string message, Exception ex)
        {
            HasError = true;
            ErrorMessage = $"{message}: {ex.Message}";
            StatusText = "Error occurred";
            StatusColor = Colors.Red;
            
            System.Diagnostics.Debug.WriteLine($"Window Error: {message} - {ex}");
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region Event Args Classes
    /// <summary>
    /// Event arguments for content item selection
    /// </summary>
    public class ContentItemEventArgs : EventArgs
    {
        public object Item { get; }
        public string Action { get; }

        public ContentItemEventArgs(object item, string action)
        {
            Item = item;
            Action = action;
        }
    }

    /// <summary>
    /// Event arguments for search operations
    /// </summary>
    public class SearchEventArgs : EventArgs
    {
        public string SearchText { get; }

        public SearchEventArgs(string searchText)
        {
            SearchText = searchText;
        }
    }

    /// <summary>
    /// Event arguments for filter operations
    /// </summary>
    public class FilterEventArgs : EventArgs
    {
        public string FilterType { get; }
        public object FilterValue { get; }

        public FilterEventArgs(string filterType, object filterValue)
        {
            FilterType = filterType;
            FilterValue = filterValue;
        }
    }

    /// <summary>
    /// Event arguments for sort operations
    /// </summary>
    public class SortEventArgs : EventArgs
    {
        public string SortProperty { get; }
        public bool Ascending { get; }

        public SortEventArgs(string sortProperty, bool ascending)
        {
            SortProperty = sortProperty;
            Ascending = ascending;
        }
    }
    #endregion
}
