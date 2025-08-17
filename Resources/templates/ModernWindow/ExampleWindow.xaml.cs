using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AukettHeeseRevitAddin2024.Windows.Template
{
    /// <summary>
    /// Example Window - Demonstrates Modern Window Template Usage
    /// Shows how to create a custom window with specific functionality
    /// </summary>
    public partial class ExampleWindow : ModernWindowTemplate
    {
        #region Private Fields
        private ObservableCollection<ExampleItem> _allItems;
        private ObservableCollection<ExampleItem> _filteredItems;
        #endregion

        #region Constructor
        public ExampleWindow()
        {
            InitializeComponent();
            
            // Configure the window
            HeaderTitle = "🎯 Example Feature";
            HeaderSubtitle = "Demonstrates Modern Window Template usage with custom functionality";
            WindowTitle = "Example Window - Aukett + Heese";
            
            // Configure UI elements
            ShowAddButton = true;
            ShowSettingsButton = true;
            ShowHelpButton = true;
            ShowFilterButton = true;
            ShowSortButton = true;
            
            // Initialize collections
            _allItems = new ObservableCollection<ExampleItem>();
            _filteredItems = new ObservableCollection<ExampleItem>();
            
            // Set up event handlers
            ContentItemSelected += ExampleWindow_ContentItemSelected;
            SearchPerformed += ExampleWindow_SearchPerformed;
            FilterApplied += ExampleWindow_FilterApplied;
            SortApplied += ExampleWindow_SortApplied;
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Called when the window is loaded
        /// </summary>
        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();
            
            // Initialize the window
            StatusText = "Initializing...";
            LoadSampleData();
        }

        /// <summary>
        /// Loads the content for the window
        /// </summary>
        protected override void LoadContent()
        {
            IsLoading = true;
            StatusText = "Loading content...";
            
            try
            {
                // Simulate loading delay
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LoadSampleData();
                        IsLoading = false;
                        StatusText = $"Loaded {_allItems.Count} items";
                    });
                });
            }
            catch (Exception ex)
            {
                HandleError("Failed to load content", ex);
            }
        }

        /// <summary>
        /// Refreshes the content
        /// </summary>
        protected override void RefreshContent()
        {
            StatusText = "🔄 Refreshing...";
            LoadContent();
        }

        /// <summary>
        /// Called when settings are requested
        /// </summary>
        protected override void OnSettingsRequested()
        {
            MessageBox.Show("Settings functionality would be implemented here.", 
                           "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Called when filter is requested
        /// </summary>
        protected override void OnFilterRequested()
        {
            var filterDialog = new FilterDialog();
            if (filterDialog.ShowDialog() == true)
            {
                ApplyFilter(filterDialog.SelectedCategory);
            }
        }

        /// <summary>
        /// Called when sort is requested
        /// </summary>
        protected override void OnSortRequested()
        {
            var sortDialog = new SortDialog();
            if (sortDialog.ShowDialog() == true)
            {
                ApplySort(sortDialog.SortProperty, sortDialog.Ascending);
            }
        }

        /// <summary>
        /// Called when add is requested
        /// </summary>
        protected override void OnAddRequested()
        {
            var addDialog = new AddItemDialog();
            if (addDialog.ShowDialog() == true)
            {
                AddNewItem(addDialog.NewItem);
            }
        }

        /// <summary>
        /// Called when help is requested
        /// </summary>
        protected override void OnHelpRequested()
        {
            MessageBox.Show("This is an example window demonstrating the Modern Window Template.\n\n" +
                           "Features:\n" +
                           "• Search and filter functionality\n" +
                           "• Custom item templates\n" +
                           "• Event handling\n" +
                           "• Status management\n" +
                           "• Error handling", 
                           "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Called when a content item is selected
        /// </summary>
        protected override void OnContentItemSelected(object item, string action)
        {
            if (item is ExampleItem exampleItem)
            {
                switch (action)
                {
                    case "Open":
                        OpenItem(exampleItem);
                        break;
                    case "ViewDetails":
                        ShowItemDetails(exampleItem);
                        break;
                    case "Favorite":
                        ToggleFavorite(exampleItem);
                        break;
                }
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles content item selection events
        /// </summary>
        private void ExampleWindow_ContentItemSelected(object sender, ContentItemEventArgs e)
        {
            OnContentItemSelected(e.Item, e.Action);
        }

        /// <summary>
        /// Handles search performed events
        /// </summary>
        private void ExampleWindow_SearchPerformed(object sender, SearchEventArgs e)
        {
            ApplySearch(e.SearchText);
        }

        /// <summary>
        /// Handles filter applied events
        /// </summary>
        private void ExampleWindow_FilterApplied(object sender, FilterEventArgs e)
        {
            ApplyFilter(e.FilterValue?.ToString());
        }

        /// <summary>
        /// Handles sort applied events
        /// </summary>
        private void ExampleWindow_SortApplied(object sender, SortEventArgs e)
        {
            ApplySort(e.SortProperty, e.Ascending);
        }

        /// <summary>
        /// Handles view details button click
        /// </summary>
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is ExampleItem item)
            {
                ShowItemDetails(item);
            }
        }

        /// <summary>
        /// Handles open item button click
        /// </summary>
        private void OpenItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is ExampleItem item)
            {
                OpenItem(item);
            }
        }

        /// <summary>
        /// Handles favorite button click
        /// </summary>
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is ExampleItem item)
            {
                ToggleFavorite(item);
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Loads sample data for demonstration
        /// </summary>
        private void LoadSampleData()
        {
            _allItems.Clear();
            
            // Add sample items
            _allItems.Add(new ExampleItem
            {
                Id = 1,
                Title = "Sample Item 1",
                Description = "This is a sample item demonstrating the template functionality.",
                Category = "Category A",
                StatusText = "Available",
                StatusColor = Colors.Green,
                ImageSource = "/Resources/icons/placeholder-32_96dpi.png",
                IsFavorite = false
            });
            
            _allItems.Add(new ExampleItem
            {
                Id = 2,
                Title = "Sample Item 2",
                Description = "Another sample item with different properties.",
                Category = "Category B",
                StatusText = "In Progress",
                StatusColor = Colors.Orange,
                ImageSource = "/Resources/icons/placeholder-32_96dpi.png",
                IsFavorite = true
            });
            
            _allItems.Add(new ExampleItem
            {
                Id = 3,
                Title = "Sample Item 3",
                Description = "A third sample item for demonstration purposes.",
                Category = "Category A",
                StatusText = "Completed",
                StatusColor = Colors.Blue,
                ImageSource = "/Resources/icons/placeholder-32_96dpi.png",
                IsFavorite = false
            });
            
            // Set the items
            Items = _allItems;
            _filteredItems = new ObservableCollection<ExampleItem>(_allItems);
        }

        /// <summary>
        /// Applies search filter to items
        /// </summary>
        private void ApplySearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Items = _allItems;
            }
            else
            {
                var filtered = _allItems.Where(item =>
                    item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    item.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    item.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                Items = filtered;
            }
            
            StatusText = $"Found {Items.Count()} items";
        }

        /// <summary>
        /// Applies category filter to items
        /// </summary>
        private void ApplyFilter(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || category == "All")
            {
                Items = _allItems;
            }
            else
            {
                var filtered = _allItems.Where(item => item.Category == category).ToList();
                Items = filtered;
            }
            
            StatusText = $"Filtered: {category} ({Items.Count()} items)";
        }

        /// <summary>
        /// Applies sorting to items
        /// </summary>
        private void ApplySort(string property, bool ascending)
        {
            var sorted = property switch
            {
                "Title" => ascending ? _allItems.OrderBy(x => x.Title) : _allItems.OrderByDescending(x => x.Title),
                "Category" => ascending ? _allItems.OrderBy(x => x.Category) : _allItems.OrderByDescending(x => x.Category),
                "Status" => ascending ? _allItems.OrderBy(x => x.StatusText) : _allItems.OrderByDescending(x => x.StatusText),
                _ => _allItems.AsEnumerable()
            };
            
            Items = sorted.ToList();
            StatusText = $"Sorted by {property} ({ascending ? "ascending" : "descending"})";
        }

        /// <summary>
        /// Opens an item
        /// </summary>
        private void OpenItem(ExampleItem item)
        {
            MessageBox.Show($"Opening item: {item.Title}", "Open Item", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows item details
        /// </summary>
        private void ShowItemDetails(ExampleItem item)
        {
            var details = $"Title: {item.Title}\n" +
                         $"Description: {item.Description}\n" +
                         $"Category: {item.Category}\n" +
                         $"Status: {item.StatusText}\n" +
                         $"Favorite: {item.IsFavorite}";
            
            MessageBox.Show(details, "Item Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Toggles favorite status
        /// </summary>
        private void ToggleFavorite(ExampleItem item)
        {
            item.IsFavorite = !item.IsFavorite;
            StatusText = $"{(item.IsFavorite ? "Added to" : "Removed from")} favorites: {item.Title}";
        }

        /// <summary>
        /// Adds a new item
        /// </summary>
        private void AddNewItem(ExampleItem newItem)
        {
            newItem.Id = _allItems.Max(x => x.Id) + 1;
            _allItems.Add(newItem);
            Items = _allItems;
            StatusText = $"Added new item: {newItem.Title}";
        }
        #endregion
    }

    #region Example Item Model
    /// <summary>
    /// Example item model for demonstration
    /// </summary>
    public class ExampleItem : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private string _category;
        private string _statusText;
        private Color _statusColor;
        private string _imageSource;
        private bool _isFavorite;

        public int Id { get; set; }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

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

        public string ImageSource
        {
            get => _imageSource;
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    OnPropertyChanged(nameof(ImageSource));
                }
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

    #region Dialog Classes (Placeholder)
    /// <summary>
    /// Placeholder filter dialog
    /// </summary>
    public class FilterDialog
    {
        public string SelectedCategory { get; set; } = "All";
        public bool? ShowDialog() => true;
    }

    /// <summary>
    /// Placeholder sort dialog
    /// </summary>
    public class SortDialog
    {
        public string SortProperty { get; set; } = "Title";
        public bool Ascending { get; set; } = true;
        public bool? ShowDialog() => true;
    }

    /// <summary>
    /// Placeholder add item dialog
    /// </summary>
    public class AddItemDialog
    {
        public ExampleItem NewItem { get; set; } = new ExampleItem
        {
            Title = "New Item",
            Description = "New item description",
            Category = "Category A",
            StatusText = "New",
            StatusColor = Colors.Gray
        };
        public bool? ShowDialog() => true;
    }
    #endregion
}
