using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Controllers;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Interaction logic for RoomWallAnalysisWindow.xaml
    /// </summary>
    public partial class RoomWallAnalysisWindow : Window
    {
        #region Fields
        private Document _document;
        private RoomWallAnalysisController _controller;
        private GenericElementController _elementController;
        private ParameterMappingService _parameterMappingService;
        private List<RoomItem> _roomItems;
        private ElementCollectorService _elementCollector;
        private List<ParameterInfo> _availableParameters;
        private RoomFilterConfiguration _currentFilter;
        
        // Filtered data
        private List<RoomItem> _filteredRooms;
        private List<WallItem> _wallItems; // Original wall data for analysis
        
        // Element panel data
        private List<CategoryInfo> _availableCategories;
        private List<ParameterInfo> _availableElementParameters;
        private ElementFilterConfiguration _currentElementFilter;
        private List<Element> _filteredElements;
        
        // Parameter mapping configurations
        private ParameterMappingConfiguration _roomsToCategoryMapping;
        private ParameterMappingConfiguration _categoryToRoomsMapping;
        #endregion

        #region Constructor
        public RoomWallAnalysisWindow(Document document)
        {
            _document = document;
            _elementCollector = new ElementCollectorService();
            _controller = new RoomWallAnalysisController(document);
            _elementController = new GenericElementController(document);
            _parameterMappingService = new ParameterMappingService(document);
            _availableParameters = _controller.GetAvailableRoomParameters();
            _currentFilter = _controller.CreateFilterConfiguration("Room Filter");

            // Initialize mapping configurations
            _roomsToCategoryMapping = new ParameterMappingConfiguration 
            { 
                Direction = MappingDirection.RoomsToCategory, 
                IsEnabled = false 
            };
            _categoryToRoomsMapping = new ParameterMappingConfiguration 
            { 
                Direction = MappingDirection.CategoryToRooms, 
                IsEnabled = false 
            };

            InitializeComponent();
            
            LoadData();
            SetupEventHandlers();
            UpdateFilterUI();
        }
        #endregion

        #region Initialization
        private void LoadData()
        {
            try
            {
                StatusLabel.Content = "Loading data...";
                
                // Load room and wall data through controller
                var data = _controller.LoadInitialData();
                _roomItems = data.Rooms;
                _filteredRooms = new List<RoomItem>(_roomItems);
                _wallItems = data.Walls; // Store wall data for analysis
                
                // Load element categories
                _availableCategories = _elementController.GetAvailableCategories();
                _availableElementParameters = new List<ParameterInfo>();
                _filteredElements = new List<Element>();
                
                // Populate UI
                PopulateFilterDropdowns();
                PopulateElementCategoryDropdown();
                UpdateElementCounters();
                UpdateParameterMappingLabels(); // Set initial parameter mapping labels
                
                StatusLabel.Content = "Ready";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error loading data: {ex.Message}";
                MessageBox.Show($"Error loading data: {ex.Message}", "RoomDataSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupEventHandlers()
        {
            // Element category selection
            ElementCategoryCombo.SelectionChanged += OnElementCategoryChanged;
            
            // Room filter events  
            MainOperatorCombo.SelectionChanged += OnMainOperatorChanged;
            
            // Room filter buttons
            AddRuleButton.Click += (s, e) => AddNewRule();
            AddSetButton.Click += (s, e) => AddNewSet();
            ClearFiltersButton.Click += (s, e) => ClearAllFilters();
            FilterStatusAddRuleButton.Click += (s, e) => AddNewRule();
            
            // Element filter events
            ElementMainOperatorCombo.SelectionChanged += OnElementMainOperatorChanged;
            
            // Element filter buttons
            ElementAddRuleButton.Click += (s, e) => AddNewElementRule();
            ElementAddSetButton.Click += (s, e) => AddNewElementSet();
            ElementClearFiltersButton.Click += (s, e) => ClearAllElementFilters();
            ElementFilterStatusAddRuleButton.Click += (s, e) => AddNewElementRule();

            // Parameter mapping buttons
            RoomsMappingEnableButton.Click += (s, e) => EnableRoomsParameterMapping();
            CategoryMappingEnableButton.Click += (s, e) => EnableCategoryParameterMapping();
            RoomsMappingAddButton.Click += (s, e) => AddRoomsParameterMapping();
            CategoryMappingAddButton.Click += (s, e) => AddCategoryParameterMapping();
            RoomsMappingRemoveButton.Click += (s, e) => DisableRoomsParameterMapping();
            CategoryMappingRemoveButton.Click += (s, e) => DisableCategoryParameterMapping();

            // Parameter mapping ComboBoxes
            RoomsFromParameterCombo.SelectionChanged += (s, e) => OnRoomsMappingParameterChanged();
            RoomsToParameterCombo.SelectionChanged += (s, e) => OnRoomsMappingParameterChanged();
            CategoryFromParameterCombo.SelectionChanged += (s, e) => OnCategoryMappingParameterChanged();
            CategoryToParameterCombo.SelectionChanged += (s, e) => OnCategoryMappingParameterChanged();

            // Separator TextBoxes
            RoomsSeparatorTextBox.TextChanged += (s, e) => OnRoomsSeparatorChanged();
            CategorySeparatorTextBox.TextChanged += (s, e) => OnCategorySeparatorChanged();

            // Analysis buttons
            RunAnalysisButton.Click += RunAnalysisButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        // Convert lambda expressions to proper event handler methods
        private void OnElementCategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            OnElementCategoryChanged();
        }

        private void OnMainOperatorChanged(object sender, SelectionChangedEventArgs e)
        {
            OnMainOperatorChanged();
        }

        private void OnElementMainOperatorChanged(object sender, SelectionChangedEventArgs e)
        {
            OnElementMainOperatorChanged();
        }

        private void PopulateFilterDropdowns()
        {
            // Room filtering dropdowns would be here if needed
            // Currently using advanced filtering only
        }

        private void PopulateElementCategoryDropdown()
        {
            try
            {
                // Add "Select Category" as the first item
                var categoryItems = new List<string> { "Select Category" };
                categoryItems.AddRange(_availableCategories.Select(c => c.Name));
                
                ElementCategoryCombo.ItemsSource = categoryItems;
                ElementCategoryCombo.SelectedIndex = 0;
                
                StatusLabel.Content = $"Loaded {_availableCategories.Count} element categories";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error loading categories: {ex.Message}";
            }
        }
        #endregion

        #region Element Category Selection
        private void OnElementCategoryChanged()
        {
            try
            {
                var selectedIndex = ElementCategoryCombo.SelectedIndex;
                if (selectedIndex <= 0) // "Select Category" selected
                {
                    ClearElementSelection();
                    return; // Early return prevents further processing
                }

                var selectedCategoryName = ElementCategoryCombo.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedCategoryName) || _availableCategories == null)
                {
                    ClearElementSelection();
                    return;
                }

                var selectedCategory = _availableCategories.FirstOrDefault(c => c.Name == selectedCategoryName);
                
                if (selectedCategory != null && _elementController != null)
                {
                    StatusLabel.Content = $"Loading {selectedCategory.Name} elements...";
                    
                    // Select category in controller
                    _elementController.SelectCategory(selectedCategory);
                    
                    // Update parameter mapping service with selected category
                    _parameterMappingService.SetSelectedCategory(selectedCategory);
                    
                    // Update available parameters for this category with null checks
                    _availableElementParameters = _elementController.AvailableParameters ?? new List<ParameterInfo>();
                    
                    // Create new filter configuration with null checks
                    _currentElementFilter = _elementController.CreateFilterConfiguration($"{selectedCategory.Name} Filter");
                    if (_currentElementFilter?.RootFilterSet == null)
                    {
                        _currentElementFilter = new ElementFilterConfiguration
                        {
                            Name = $"{selectedCategory.Name} Filter",
                            CategoryId = selectedCategory.Id,
                            RootFilterSet = new FilterSet
                            {
                                Operator = LogicalOperator.And,
                                Items = new List<IFilterItem>()
                            }
                        };
                    }
                    
                    // Update filtered elements with null checks
                    _filteredElements = _elementController.FilteredElements ?? new List<Element>();
                    
                    // Update UI
                    UpdateElementFilterUI();
                    UpdateElementCounters();
                    UpdateParameterMappingLabels(); // Update the parameter mapping labels
                    PopulateParameterMappingComboBoxes(); // Populate the mapping ComboBoxes
                    
                    // Update GroupBox headers with selected category name
                    RightPanelGroupBox.Header = $"{selectedCategory.Name} | Filter Elements";
                    RoomsMappingGroupBox.Header = $"Parameter Mapping: Rooms → {selectedCategory.Name}";
                    CategoryMappingGroupBox.Header = $"Parameter Mapping: {selectedCategory.Name} → Rooms";
                    
                    StatusLabel.Content = $"Selected {selectedCategory.Name}: {_filteredElements.Count} elements";
                }
                else
                {
                    ClearElementSelection();
                }
            }
            catch (Exception ex)
            {
                ClearElementSelection(); // Ensure clean state on error
                StatusLabel.Content = $"Error selecting category: {ex.Message}";
                MessageBox.Show($"Error selecting category: {ex.Message}", "RoomDataSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearElementSelection()
        {
            try
            {
                _elementController?.ClearCategory();
                _parameterMappingService?.ClearSelectedCategory();
                _availableElementParameters?.Clear();
                _filteredElements?.Clear();
                _currentElementFilter = null;
                
                UpdateElementFilterUI();
                UpdateElementCounters();
                UpdateParameterMappingLabels(); // Update the parameter mapping labels
                ClearParameterMappingComboBoxes(); // Clear the mapping ComboBoxes
                
                // Reset GroupBox headers to default
                RightPanelGroupBox.Header = "Other Elements";
                RoomsMappingGroupBox.Header = "Parameter Mapping: Rooms → Category";
                CategoryMappingGroupBox.Header = "Parameter Mapping: Category → Rooms";
                
                StatusLabel.Content = "No element category selected";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error clearing selection: {ex.Message}";
            }
        }
        #endregion

        #region UI Updates        
        private void UpdateElementCounters()
        {
            try
            {
                if (_elementController.SelectedCategory == null)
                {
                    ElementCountText.Text = "Elements: 0 of 0";
                    return;
                }

                var totalCount = _elementController.AllElements.Count;
                var filteredCount = _filteredElements?.Count ?? 0;
                
                ElementCountText.Text = $"Elements: {filteredCount} of {totalCount}";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating element counters: {ex.Message}";
            }
        }

        private void UpdateParameterMappingLabels()
        {
            try
            {
                var categoryName = _parameterMappingService?.GetCategoryDisplayName() ?? "Category";
                
                // Update the mapping summary labels with dynamic category name
                if (_roomsToCategoryMapping?.IsEnabled == true)
                {
                    RoomsMappingSummaryText.Text = $"Rooms → {categoryName} Parameter Mapping Enabled";
                }
                else
                {
                    RoomsMappingSummaryText.Text = $"No Rooms → {categoryName} Parameter Mapping";
                }
                
                if (_categoryToRoomsMapping?.IsEnabled == true)
                {
                    CategoryMappingSummaryText.Text = $"{categoryName} → Rooms Parameter Mapping Enabled";
                }
                else
                {
                    CategoryMappingSummaryText.Text = $"No {categoryName} → Rooms Parameter Mapping";
                }
                
                // Update the enable button state and text based on category selection
                var hasCategorySelected = _parameterMappingService?.HasCategorySelected ?? false;
                
                RoomsMappingEnableButton.IsEnabled = hasCategorySelected;
                RoomsMappingEnableButton.Content = hasCategorySelected ? "Enable Parameter Mapping" : "Select Category";
                
                CategoryMappingEnableButton.IsEnabled = hasCategorySelected;
                CategoryMappingEnableButton.Content = hasCategorySelected ? "Enable Parameter Mapping" : "Select Category";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating parameter mapping labels: {ex.Message}";
            }
        }

        private void PopulateParameterMappingComboBoxes()
        {
            try
            {
                // Clear existing items
                RoomsFromParameterCombo.Items.Clear();
                RoomsToParameterCombo.Items.Clear();
                CategoryFromParameterCombo.Items.Clear();
                CategoryToParameterCombo.Items.Clear();

                if (_parameterMappingService?.HasCategorySelected != true) return;

                // For Rooms → Category mapping
                // From: Room parameters, To: Category parameters
                RoomsFromParameterCombo.Items.Add("Select Parameter");
                foreach (var param in _parameterMappingService.RoomParameters)
                {
                    RoomsFromParameterCombo.Items.Add(param.Name);
                }
                RoomsFromParameterCombo.SelectedIndex = 0;

                RoomsToParameterCombo.Items.Add("Select Parameter");
                foreach (var param in _parameterMappingService.ElementParameters)
                {
                    RoomsToParameterCombo.Items.Add(param.Name);
                }
                RoomsToParameterCombo.SelectedIndex = 0;

                // For Category → Rooms mapping
                // From: Category parameters, To: Room parameters
                CategoryFromParameterCombo.Items.Add("Select Parameter");
                foreach (var param in _parameterMappingService.ElementParameters)
                {
                    CategoryFromParameterCombo.Items.Add(param.Name);
                }
                CategoryFromParameterCombo.SelectedIndex = 0;

                CategoryToParameterCombo.Items.Add("Select Parameter");
                foreach (var param in _parameterMappingService.RoomParameters)
                {
                    CategoryToParameterCombo.Items.Add(param.Name);
                }
                CategoryToParameterCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error populating parameter ComboBoxes: {ex.Message}";
            }
        }

        private void ClearParameterMappingComboBoxes()
        {
            try
            {
                RoomsFromParameterCombo.Items.Clear();
                RoomsToParameterCombo.Items.Clear();
                CategoryFromParameterCombo.Items.Clear();
                CategoryToParameterCombo.Items.Clear();

                RoomsFromParameterCombo.Items.Add("No Category Selected");
                RoomsToParameterCombo.Items.Add("No Category Selected");
                CategoryFromParameterCombo.Items.Add("No Category Selected");
                CategoryToParameterCombo.Items.Add("No Category Selected");

                RoomsFromParameterCombo.SelectedIndex = 0;
                RoomsToParameterCombo.SelectedIndex = 0;
                CategoryFromParameterCombo.SelectedIndex = 0;
                CategoryToParameterCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error clearing parameter ComboBoxes: {ex.Message}";
            }
        }

        private void ApplyRoomFilters()
        {
            try
            {
                // Apply advanced filter if rules exist
                if (_currentFilter?.RootFilterSet?.Items?.Any() == true)
                {
                    _filteredRooms = _controller.ApplyAdvancedFilter(_currentFilter);
                }
                else
                {
                    // No filters - show all rooms
                    _filteredRooms = new List<RoomItem>(_roomItems);
                }
                
                UpdateFilterSummary();
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Filter error: {ex.Message}";
            }
        }

        private void ApplyElementFilters()
        {
            try
            {
                // Apply advanced filter if rules exist
                if (_currentElementFilter?.RootFilterSet?.Items?.Any() == true && _elementController != null)
                {
                    _filteredElements = _elementController.ApplyAdvancedFilter(_currentElementFilter) ?? new List<Element>();
                }
                else
                {
                    // No filters - show all elements with null checks
                    if (_elementController != null)
                    {
                        _filteredElements = _elementController.AllElements ?? new List<Element>();
                    }
                    else
                    {
                        _filteredElements = new List<Element>(); // Safe fallback
                    }
                }
                
                UpdateElementCounters();
            }
            catch (Exception ex)
            {
                _filteredElements = new List<Element>(); // Ensure never null
                StatusLabel.Content = $"Element filter error: {ex.Message}";
            }
        }
        
        private void UpdateFilterSummary()
        {
            var totalRooms = _roomItems?.Count ?? 0;
            var filteredCount = _filteredRooms?.Count ?? 0;
            
            if (filteredCount == totalRooms)
            {
                FilterSummaryText.Text = "No filters applied - showing all rooms";
            }
            else
            {
                FilterSummaryText.Text = $"Filters applied - {filteredCount} of {totalRooms} rooms selected";
            }
            
            FilterCountText.Text = $"Rooms: {filteredCount} of {totalRooms}";
            
            StatusLabel.Content = $"Filter applied: {filteredCount} rooms match criteria";
        }
        
        private void UpdateFilterUI()
        {
            var hasRules = _currentFilter?.RootFilterSet?.Items?.Any() == true;
            
            // Show/hide the main filter container
            MainFilterContainer.Visibility = hasRules ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            
            // Show/hide the Filter Status Add Rule button (opposite of main container)
            FilterStatusAddRuleButton.Visibility = hasRules ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            
            if (hasRules)
            {
                // Update main operator combo value
                MainOperatorCombo.SelectedIndex = _currentFilter?.RootFilterSet?.Operator == LogicalOperator.And ? 0 : 1;
                
                // Update main container border color based on operator
                MainFilterContainer.BorderBrush = _currentFilter.RootFilterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :  // Green for AND
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));   // Blue for OR
            }
            
            // Rebuild rules UI
            RebuildRulesUI();
            
            // Update filter summary
            UpdateFilterSummary();
        }
        
        private void RebuildRulesUI()
        {
            RulesContainer.Children.Clear();
            
            if (_currentFilter?.RootFilterSet?.Items != null)
            {
                foreach (var item in _currentFilter.RootFilterSet.Items)
                {
                    if (item is RoomFilterRule rule)
                    {
                        var ruleUI = CreateRuleUI(rule);
                        RulesContainer.Children.Add(ruleUI);
                    }
                    else if (item is FilterSet filterSet)
                    {
                        var setUI = CreateFilterSetUI(filterSet);
                        RulesContainer.Children.Add(setUI);
                    }
                }
            }
        }
        
        private void UpdateElementFilterUI()
        {
            var hasRules = _currentElementFilter?.RootFilterSet?.Items?.Any() == true;
            
            // Show/hide the element filter container
            ElementMainFilterContainer.Visibility = hasRules ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            
            // Show/hide the Element Filter Status Add Rule button (opposite of main container)
            ElementFilterStatusAddRuleButton.Visibility = hasRules ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            
            if (hasRules)
            {
                // Update element operator combo value
                ElementMainOperatorCombo.SelectedIndex = _currentElementFilter?.RootFilterSet?.Operator == LogicalOperator.And ? 0 : 1;
                
                // Update element container border color based on operator
                ElementMainFilterContainer.BorderBrush = _currentElementFilter.RootFilterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :  // Green for AND
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));   // Blue for OR
            }
            
            // Rebuild element rules UI
            RebuildElementRulesUI();
        }
        
        private void RebuildElementRulesUI()
        {
            ElementRulesContainer.Children.Clear();
            
            if (_currentElementFilter?.RootFilterSet?.Items != null)
            {
                foreach (var item in _currentElementFilter.RootFilterSet.Items)
                {
                    if (item is ElementFilterRule rule)
                    {
                        var ruleUI = CreateElementRuleUI(rule);
                        ElementRulesContainer.Children.Add(ruleUI);
                    }
                    else if (item is FilterSet filterSet)
                    {
                        var setUI = CreateElementFilterSetUI(filterSet);
                        ElementRulesContainer.Children.Add(setUI);
                    }
                }
            }
        }
        
        #endregion
        
        #region Filter UI Creation Methods
        
        private FrameworkElement CreateRuleUI(RoomFilterRule rule)
        {
            var rulePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
            
            // Top row: Category and Parameter
            var topGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Category (always "Rooms")
            var categoryCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
            categoryCombo.Items.Add("Rooms");
            categoryCombo.SelectedIndex = 0;
            System.Windows.Controls.Grid.SetColumn(categoryCombo, 0);
            topGrid.Children.Add(categoryCombo);
            
            // Parameter dropdown
            var parameterCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox") };
            foreach (var param in _availableParameters)
            {
                parameterCombo.Items.Add(param.Name);
            }
            parameterCombo.SelectedItem = rule.Parameter?.Name;
            parameterCombo.SelectionChanged += (s, e) =>
            {
                rule.Parameter = _availableParameters.FirstOrDefault(p => p.Name == parameterCombo.SelectedItem?.ToString());
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(parameterCombo, 1);
            topGrid.Children.Add(parameterCombo);
            
            rulePanel.Children.Add(topGrid);
            
            // Bottom row: Operator, Value, Delete
            var bottomGrid = new System.Windows.Controls.Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Operator
            var operatorCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0) };
            UpdateOperatorCombo(rule, operatorCombo);
            operatorCombo.SelectionChanged += (s, e) =>
            {
                rule.Operator = GetOperatorFromDisplayText(operatorCombo.SelectedItem?.ToString());
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(operatorCombo, 0);
            bottomGrid.Children.Add(operatorCombo);
            
            // Value
            var valueTextBox = new TextBox { Style = (Style)FindResource("ModernTextBox"), Text = rule.Value ?? "", Margin = new Thickness(0, 0, 8, 0) };
            valueTextBox.TextChanged += (s, e) =>
            {
                rule.Value = valueTextBox.Text;
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(valueTextBox, 1);
            bottomGrid.Children.Add(valueTextBox);
            
            // Delete button
            var deleteButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteButton.Click += (s, e) =>
            {
                _currentFilter.RootFilterSet.Items.Remove(rule);
                UpdateFilterUI();
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(deleteButton, 2);
            bottomGrid.Children.Add(deleteButton);
            
            rulePanel.Children.Add(bottomGrid);
            return rulePanel;
        }
        
        private FrameworkElement CreateFilterSetUI(FilterSet filterSet)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8),
                Background = Brushes.White
            };
            
            // Set border color based on operator
            border.BorderBrush = filterSet.Operator == LogicalOperator.And ? 
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) : 
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
            
            var setPanel = new StackPanel { Orientation = Orientation.Vertical };
            
            // Set header with full functionality - using Grid for proper alignment
            var headerGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Set operator combo
            var setOperatorCombo = new ComboBox { Width = 180, Style = (Style)FindResource("RuleModeComboBox") };
            setOperatorCombo.Items.Add("AND (All must be true)");
            setOperatorCombo.Items.Add("OR (Any may be true)");
            setOperatorCombo.SelectedIndex = filterSet.Operator == LogicalOperator.And ? 0 : 1;
            setOperatorCombo.SelectionChanged += (s, e) =>
            {
                filterSet.Operator = setOperatorCombo.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                border.BorderBrush = filterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(setOperatorCombo, 0);
            headerGrid.Children.Add(setOperatorCombo);
            
            // Right-aligned buttons panel
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Create container for set rules
            var setRulesContainer = new StackPanel { Orientation = Orientation.Vertical };
            
            // Add Rule to Set button
            var addRuleToSetButton = new Button { Content = "Add Rule", Style = (Style)FindResource("ModernButton"), Margin = new Thickness(0, 0, 4, 0) };
            addRuleToSetButton.Click += (s, e) => AddRuleToSet(filterSet, setRulesContainer);
            buttonsPanel.Children.Add(addRuleToSetButton);
            
            // Add Set to Set button (disabled for 1 level depth limit)
            var addSetToSetButton = new Button { Content = "Add Set", Style = (Style)FindResource("ModernButton"), Margin = new Thickness(0, 0, 4, 0) };
            addSetToSetButton.IsEnabled = false; // Only 1 level of nesting
            addSetToSetButton.ToolTip = "Only one level of nesting allowed";
            buttonsPanel.Children.Add(addSetToSetButton);
            
            // Delete Set button
            var deleteSetButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteSetButton.Click += (s, e) =>
            {
                _currentFilter.RootFilterSet.Items.Remove(filterSet);
                UpdateFilterUI();
                ApplyRoomFilters();
            };
            buttonsPanel.Children.Add(deleteSetButton);
            
            System.Windows.Controls.Grid.SetColumn(buttonsPanel, 2);
            headerGrid.Children.Add(buttonsPanel);
            
            setPanel.Children.Add(headerGrid);
            
            // Set rules container
            setPanel.Children.Add(setRulesContainer);
            
            // Build rules UI for this set
            RebuildSetRulesUI(filterSet, setRulesContainer);
            
            border.Child = setPanel;
            return border;
        }
        
        private void AddRuleToSet(FilterSet filterSet, StackPanel container)
        {
            if (_availableParameters?.Any() == true)
            {
                var rule = new RoomFilterRule
                {
                    Parameter = _availableParameters.First(),
                    Operator = FilterOperator.Equals,
                    Value = ""
                };
                
                filterSet.Items.Add(rule);
                RebuildSetRulesUI(filterSet, container);
                ApplyRoomFilters();
            }
        }
        
        private void RebuildSetRulesUI(FilterSet filterSet, StackPanel container)
        {
            container.Children.Clear();
            
            if (filterSet?.Items != null && filterSet.Items.Any())
            {
                foreach (var item in filterSet.Items)
                {
                    if (item is RoomFilterRule rule)
                    {
                        var ruleUI = CreateSetRuleUI(rule, filterSet, container);
                        container.Children.Add(ruleUI);
                    }
                    // Note: Nested sets not supported at this level (1 level limit)
                }
            }
            else
            {
                // Empty state
                var emptyPanel = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                
                var emptyText = new TextBlock 
                { 
                    Text = "No rules in this set - click 'Add Rule' to add conditions",
                    FontStyle = FontStyles.Italic,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                emptyPanel.Child = emptyText;
                container.Children.Add(emptyPanel);
            }
        }
        
        private FrameworkElement CreateSetRuleUI(RoomFilterRule rule, FilterSet parentSet, StackPanel parentContainer)
        {
            var rulePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 6) };
            
            // Top row: Category and Parameter
            var topGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 6) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Category (always "Rooms")
            var categoryCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
            categoryCombo.Items.Add("Rooms");
            categoryCombo.SelectedIndex = 0;
            System.Windows.Controls.Grid.SetColumn(categoryCombo, 0);
            topGrid.Children.Add(categoryCombo);
            
            // Parameter dropdown
            var parameterCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox") };
            foreach (var param in _availableParameters)
            {
                parameterCombo.Items.Add(param.Name);
            }
            parameterCombo.SelectedItem = rule.Parameter?.Name;
            parameterCombo.SelectionChanged += (s, e) =>
            {
                rule.Parameter = _availableParameters.FirstOrDefault(p => p.Name == parameterCombo.SelectedItem?.ToString());
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(parameterCombo, 1);
            topGrid.Children.Add(parameterCombo);
            
            rulePanel.Children.Add(topGrid);
            
            // Bottom row: Operator, Value, Delete
            var bottomGrid = new System.Windows.Controls.Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Operator
            var operatorCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0) };
            UpdateOperatorCombo(rule, operatorCombo);
            operatorCombo.SelectionChanged += (s, e) =>
            {
                rule.Operator = GetOperatorFromDisplayText(operatorCombo.SelectedItem?.ToString());
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(operatorCombo, 0);
            bottomGrid.Children.Add(operatorCombo);
            
            // Value
            var valueTextBox = new TextBox { Style = (Style)FindResource("ModernTextBox"), Text = rule.Value ?? "", Margin = new Thickness(0, 0, 8, 0) };
            valueTextBox.TextChanged += (s, e) =>
            {
                rule.Value = valueTextBox.Text;
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(valueTextBox, 1);
            bottomGrid.Children.Add(valueTextBox);
            
            // Delete button
            var deleteButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteButton.Click += (s, e) =>
            {
                parentSet.Items.Remove(rule);
                RebuildSetRulesUI(parentSet, parentContainer);
                ApplyRoomFilters();
            };
            System.Windows.Controls.Grid.SetColumn(deleteButton, 2);
            bottomGrid.Children.Add(deleteButton);
            
            rulePanel.Children.Add(bottomGrid);
            return rulePanel;
        }
        
        #endregion
        
        #region Filter Event Handlers
        
        private void OnMainOperatorChanged()
        {
            if (_currentFilter?.RootFilterSet != null)
            {
                _currentFilter.RootFilterSet.Operator = MainOperatorCombo.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                
                // Update main container border color based on operator
                MainFilterContainer.BorderBrush = _currentFilter.RootFilterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :  // Green for AND
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));   // Blue for OR
                
                ApplyRoomFilters();
            }
        }
        
        private void AddNewRule()
        {
            if (_availableParameters?.Any() == true)
            {
                var rule = new RoomFilterRule
                {
                    Parameter = _availableParameters.First(),
                    Operator = FilterOperator.Equals,
                    Value = ""
                };
                
                _currentFilter.RootFilterSet.Items.Add(rule);
                UpdateFilterUI(); // This will show the other controls now that we have rules
                ApplyRoomFilters();
            }
        }
        
        private void AddNewSet()
        {
            var newSet = new FilterSet
            {
                Operator = LogicalOperator.And,
                Items = new List<IFilterItem>()
            };
            
            // Add initial rule to new set
            if (_availableParameters?.Any() == true)
            {
                var initialRule = new RoomFilterRule
                {
                    Parameter = _availableParameters.First(),
                    Operator = FilterOperator.Equals,
                    Value = ""
                };
                newSet.Items.Add(initialRule);
            }
            
            _currentFilter.RootFilterSet.Items.Add(newSet);
            UpdateFilterUI();
            ApplyRoomFilters();
        }
        
        private void ClearAllFilters()
        {
            _currentFilter.RootFilterSet.Items.Clear();
            UpdateFilterUI();
            ApplyRoomFilters();
        }
        
        private void UpdateOperatorCombo(RoomFilterRule rule, ComboBox operatorCombo)
        {
            operatorCombo.Items.Clear();
            if (rule.Parameter != null)
            {
                var operators = rule.Parameter.GetAvailableOperators();
                foreach (var op in operators)
                {
                    operatorCombo.Items.Add(GetOperatorDisplayText(op));
                }
                operatorCombo.SelectedItem = GetOperatorDisplayText(rule.Operator);
            }
        }
        
        private string GetOperatorDisplayText(FilterOperator op)
        {
            switch (op)
            {
                case FilterOperator.Equals: return "equals";
                case FilterOperator.NotEquals: return "does not equal";
                case FilterOperator.Contains: return "contains";
                case FilterOperator.NotContains: return "does not contain";
                case FilterOperator.BeginsWith: return "begins with";
                case FilterOperator.EndsWith: return "ends with";
                case FilterOperator.GreaterThan: return "is greater than";
                case FilterOperator.LessThan: return "is less than";
                case FilterOperator.GreaterThanOrEqual: return "is greater than or equal to";
                case FilterOperator.LessThanOrEqual: return "is less than or equal to";
                case FilterOperator.HasValue: return "has a value";
                case FilterOperator.HasNoValue: return "has no value";
                default: return op.ToString();
            }
        }
        
        private FilterOperator GetOperatorFromDisplayText(string displayText)
        {
            switch (displayText)
            {
                case "equals": return FilterOperator.Equals;
                case "does not equal": return FilterOperator.NotEquals;
                case "contains": return FilterOperator.Contains;
                case "does not contain": return FilterOperator.NotContains;
                case "begins with": return FilterOperator.BeginsWith;
                case "ends with": return FilterOperator.EndsWith;
                case "is greater than": return FilterOperator.GreaterThan;
                case "is less than": return FilterOperator.LessThan;
                case "is greater than or equal to": return FilterOperator.GreaterThanOrEqual;
                case "is less than or equal to": return FilterOperator.LessThanOrEqual;
                case "has a value": return FilterOperator.HasValue;
                case "has no value": return FilterOperator.HasNoValue;
                default: return FilterOperator.Equals;
            }
        }
        
        #endregion

        #region Element Filter Event Handlers
        
        private void OnElementMainOperatorChanged()
        {
            if (_currentElementFilter?.RootFilterSet != null)
            {
                _currentElementFilter.RootFilterSet.Operator = ElementMainOperatorCombo.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                
                // Update element container border color based on operator
                ElementMainFilterContainer.BorderBrush = _currentElementFilter.RootFilterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :  // Green for AND
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));   // Blue for OR
                
                ApplyElementFilters();
            }
        }
        
        private void AddNewElementRule()
        {
            try
            {
                // Ensure we have a valid filter configuration
                if (_currentElementFilter?.RootFilterSet == null)
                {
                    if (_elementController?.SelectedCategory != null)
                    {
                        // Initialize filter if missing
                        _currentElementFilter = new ElementFilterConfiguration
                        {
                            Name = $"{_elementController.SelectedCategory.Name} Filter",
                            CategoryId = _elementController.SelectedCategory.Id,
                            RootFilterSet = new FilterSet
                            {
                                Operator = LogicalOperator.And,
                                Items = new List<IFilterItem>()
                            }
                        };
                    }
                    else
                    {
                        StatusLabel.Content = "No category selected - cannot add rule";
                        return;
                    }
                }

                if (_availableElementParameters?.Any() == true)
                {
                    var rule = new ElementFilterRule
                    {
                        Parameter = _availableElementParameters.First(),
                        Operator = FilterOperator.Equals,
                        Value = ""
                    };
                    
                    _currentElementFilter.RootFilterSet.Items.Add(rule);
                    UpdateElementFilterUI(); // This will show the other controls now that we have rules
                    ApplyElementFilters();
                }
                else
                {
                    StatusLabel.Content = "No parameters available for this category";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error adding element rule: {ex.Message}";
            }
        }
        
        private void AddNewElementSet()
        {
            if (_currentElementFilter != null)
            {
                var newSet = new FilterSet
                {
                    Operator = LogicalOperator.And,
                    Items = new List<IFilterItem>()
                };
                
                // Add initial rule to new set
                if (_availableElementParameters?.Any() == true)
                {
                    var initialRule = new ElementFilterRule
                    {
                        Parameter = _availableElementParameters.First(),
                        Operator = FilterOperator.Equals,
                        Value = ""
                    };
                    newSet.Items.Add(initialRule);
                }
                
                _currentElementFilter.RootFilterSet.Items.Add(newSet);
                UpdateElementFilterUI();
                ApplyElementFilters();
            }
        }
        
        private void ClearAllElementFilters()
        {
            if (_currentElementFilter != null)
            {
                _currentElementFilter.RootFilterSet.Items.Clear();
                UpdateElementFilterUI();
                ApplyElementFilters();
            }
        }

        // Full element rule UI creation - mirrors the room rule creation exactly
        private FrameworkElement CreateElementRuleUI(ElementFilterRule rule)
        {
            var rulePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
            
            // Top row: Category and Parameter
            var topGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Category (shows selected category name)
            var categoryCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
            categoryCombo.Items.Add(_elementController.SelectedCategory?.Name ?? "Elements");
            categoryCombo.SelectedIndex = 0;
            System.Windows.Controls.Grid.SetColumn(categoryCombo, 0);
            topGrid.Children.Add(categoryCombo);
            
            // Parameter dropdown
            var parameterCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox") };
            foreach (var param in _availableElementParameters)
            {
                parameterCombo.Items.Add(param.Name);
            }
            parameterCombo.SelectedItem = rule.Parameter?.Name;
            
            System.Windows.Controls.Grid.SetColumn(parameterCombo, 1);
            topGrid.Children.Add(parameterCombo);
            
            rulePanel.Children.Add(topGrid);
            
            // Bottom row: Operator, Value, Delete
            var bottomGrid = new System.Windows.Controls.Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Operator
            var operatorCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0) };
            UpdateElementOperatorCombo(rule, operatorCombo);
            
            // Set up parameter change handler with access to operatorCombo
            parameterCombo.SelectionChanged += (s, e) =>
            {
                rule.Parameter = _availableElementParameters.FirstOrDefault(p => p.Name == parameterCombo.SelectedItem?.ToString());
                // Update operator combo when parameter changes to reflect new data type
                UpdateElementOperatorCombo(rule, operatorCombo);
                ApplyElementFilters();
            };
            
            operatorCombo.SelectionChanged += (s, e) =>
            {
                rule.Operator = GetOperatorFromDisplayText(operatorCombo.SelectedItem?.ToString());
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(operatorCombo, 0);
            bottomGrid.Children.Add(operatorCombo);
            
            // Value
            var valueTextBox = new TextBox { Style = (Style)FindResource("ModernTextBox"), Text = rule.Value ?? "", Margin = new Thickness(0, 0, 8, 0) };
            valueTextBox.TextChanged += (s, e) =>
            {
                rule.Value = valueTextBox.Text;
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(valueTextBox, 1);
            bottomGrid.Children.Add(valueTextBox);
            
            // Delete button
            var deleteButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteButton.Click += (s, e) =>
            {
                _currentElementFilter.RootFilterSet.Items.Remove(rule);
                UpdateElementFilterUI();
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(deleteButton, 2);
            bottomGrid.Children.Add(deleteButton);
            
            rulePanel.Children.Add(bottomGrid);
            return rulePanel;
        }

        private FrameworkElement CreateElementFilterSetUI(FilterSet filterSet)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8),
                Background = Brushes.White
            };
            
            // Set border color based on operator
            border.BorderBrush = filterSet.Operator == LogicalOperator.And ? 
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) : 
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
            
            var setPanel = new StackPanel { Orientation = Orientation.Vertical };
            
            // Set header with full functionality - using Grid for proper alignment
            var headerGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Set operator combo
            var setOperatorCombo = new ComboBox { Width = 180, Style = (Style)FindResource("RuleModeComboBox") };
            setOperatorCombo.Items.Add("AND (All must be true)");
            setOperatorCombo.Items.Add("OR (Any may be true)");
            setOperatorCombo.SelectedIndex = filterSet.Operator == LogicalOperator.And ? 0 : 1;
            setOperatorCombo.SelectionChanged += (s, e) =>
            {
                filterSet.Operator = setOperatorCombo.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                border.BorderBrush = filterSet.Operator == LogicalOperator.And ?
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)) :
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(setOperatorCombo, 0);
            headerGrid.Children.Add(setOperatorCombo);
            
            // Right-aligned buttons panel
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Create container for set rules
            var setRulesContainer = new StackPanel { Orientation = Orientation.Vertical };
            
            // Add Rule to Set button
            var addRuleToSetButton = new Button { Content = "Add Rule", Style = (Style)FindResource("ModernButton"), Margin = new Thickness(0, 0, 4, 0) };
            addRuleToSetButton.Click += (s, e) => AddElementRuleToSet(filterSet, setRulesContainer);
            buttonsPanel.Children.Add(addRuleToSetButton);
            
            // Add Set to Set button (disabled for 1 level depth limit)
            var addSetToSetButton = new Button { Content = "Add Set", Style = (Style)FindResource("ModernButton"), Margin = new Thickness(0, 0, 4, 0) };
            addSetToSetButton.IsEnabled = false; // Only 1 level of nesting
            addSetToSetButton.ToolTip = "Only one level of nesting allowed";
            buttonsPanel.Children.Add(addSetToSetButton);
            
            // Delete Set button
            var deleteSetButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteSetButton.Click += (s, e) =>
            {
                _currentElementFilter.RootFilterSet.Items.Remove(filterSet);
                UpdateElementFilterUI();
                ApplyElementFilters();
            };
            buttonsPanel.Children.Add(deleteSetButton);
            
            System.Windows.Controls.Grid.SetColumn(buttonsPanel, 2);
            headerGrid.Children.Add(buttonsPanel);
            
            setPanel.Children.Add(headerGrid);
            
            // Set rules container
            setPanel.Children.Add(setRulesContainer);
            
            // Build rules UI for this set
            RebuildElementSetRulesUI(filterSet, setRulesContainer);
            
            border.Child = setPanel;
            return border;
        }
        
        private void AddElementRuleToSet(FilterSet filterSet, StackPanel container)
        {
            if (_availableElementParameters?.Any() == true)
            {
                var rule = new ElementFilterRule
                {
                    Parameter = _availableElementParameters.First(),
                    Operator = FilterOperator.Equals,
                    Value = ""
                };
                
                filterSet.Items.Add(rule);
                RebuildElementSetRulesUI(filterSet, container);
                ApplyElementFilters();
            }
        }
        
        private void RebuildElementSetRulesUI(FilterSet filterSet, StackPanel container)
        {
            container.Children.Clear();
            
            if (filterSet?.Items != null && filterSet.Items.Any())
            {
                foreach (var item in filterSet.Items)
                {
                    if (item is ElementFilterRule rule)
                    {
                        var ruleUI = CreateElementSetRuleUI(rule, filterSet, container);
                        container.Children.Add(ruleUI);
                    }
                    // Note: Nested sets not supported at this level (1 level limit)
                }
            }
            else
            {
                // Empty state
                var emptyPanel = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                
                var emptyText = new TextBlock 
                { 
                    Text = "No rules in this set - click 'Add Rule' to add conditions",
                    FontStyle = FontStyles.Italic,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                emptyPanel.Child = emptyText;
                container.Children.Add(emptyPanel);
            }
        }
        
        private FrameworkElement CreateElementSetRuleUI(ElementFilterRule rule, FilterSet parentSet, StackPanel parentContainer)
        {
            var rulePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 6) };
            
            // Top row: Category and Parameter
            var topGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 6) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Category (shows selected category name)
            var categoryCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
            categoryCombo.Items.Add(_elementController.SelectedCategory?.Name ?? "Elements");
            categoryCombo.SelectedIndex = 0;
            System.Windows.Controls.Grid.SetColumn(categoryCombo, 0);
            topGrid.Children.Add(categoryCombo);
            
            // Parameter dropdown
            var parameterCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox") };
            foreach (var param in _availableElementParameters)
            {
                parameterCombo.Items.Add(param.Name);
            }
            parameterCombo.SelectedItem = rule.Parameter?.Name;
            
            System.Windows.Controls.Grid.SetColumn(parameterCombo, 1);
            topGrid.Children.Add(parameterCombo);
            
            rulePanel.Children.Add(topGrid);
            
            // Bottom row: Operator, Value, Delete
            var bottomGrid = new System.Windows.Controls.Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Operator
            var operatorCombo = new ComboBox { Style = (Style)FindResource("ModernComboBox"), Margin = new Thickness(0, 0, 8, 0) };
            UpdateElementOperatorCombo(rule, operatorCombo);
            
            // Set up parameter change handler with access to operatorCombo
            parameterCombo.SelectionChanged += (s, e) =>
            {
                rule.Parameter = _availableElementParameters.FirstOrDefault(p => p.Name == parameterCombo.SelectedItem?.ToString());
                // Update operator combo when parameter changes to reflect new data type
                UpdateElementOperatorCombo(rule, operatorCombo);
                ApplyElementFilters();
            };
            
            operatorCombo.SelectionChanged += (s, e) =>
            {
                rule.Operator = GetOperatorFromDisplayText(operatorCombo.SelectedItem?.ToString());
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(operatorCombo, 0);
            bottomGrid.Children.Add(operatorCombo);
            
            // Value
            var valueTextBox = new TextBox { Style = (Style)FindResource("ModernTextBox"), Text = rule.Value ?? "", Margin = new Thickness(0, 0, 8, 0) };
            valueTextBox.TextChanged += (s, e) =>
            {
                rule.Value = valueTextBox.Text;
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(valueTextBox, 1);
            bottomGrid.Children.Add(valueTextBox);
            
            // Delete button
            var deleteButton = new Button { Style = (Style)FindResource("DeleteButton") };
            deleteButton.Click += (s, e) =>
            {
                parentSet.Items.Remove(rule);
                RebuildElementSetRulesUI(parentSet, parentContainer);
                ApplyElementFilters();
            };
            System.Windows.Controls.Grid.SetColumn(deleteButton, 2);
            bottomGrid.Children.Add(deleteButton);
            
            rulePanel.Children.Add(bottomGrid);
            return rulePanel;
        }
        
        private void UpdateElementOperatorCombo(ElementFilterRule rule, ComboBox operatorCombo)
        {
            operatorCombo.Items.Clear();
            if (rule.Parameter != null)
            {
                var operators = rule.Parameter.GetAvailableOperators();
                foreach (var op in operators)
                {
                    operatorCombo.Items.Add(GetOperatorDisplayText(op));
                }
                operatorCombo.SelectedItem = GetOperatorDisplayText(rule.Operator);
            }
        }
        
        // Helper method to convert Element objects back to WallItem objects
        private List<WallItem> ConvertElementsToWallItems(List<Element> elements)
        {
            var wallItems = new List<WallItem>();
            
            foreach (var element in elements)
            {
                if (element is Wall wall)
                {
                    try
                    {
                        var wallItem = new WallItem
                        {
                            Name = wall.Name,
                            LevelName = GetSafeLevelName(wall),
                            WallTypeName = GetSafeWallTypeName(wall),
                            Length = wall.Location is LocationCurve curve ? curve.Curve.Length : 0,
                            Height = GetSafeParameterValue(wall, BuiltInParameter.WALL_USER_HEIGHT_PARAM),
                            Id = wall.Id
                        };
                        
                        wallItems.Add(wallItem);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error converting wall {wall.Id} to WallItem: {ex.Message}");
                    }
                }
            }
            
            return wallItems;
        }

        // Helper methods for safe wall parameter access
        private string GetSafeLevelName(Wall wall)
        {
            try
            {
                if (wall?.Document == null) return "Unknown";
                var level = wall.Document.GetElement(wall.LevelId) as Level;
                return level?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetSafeWallTypeName(Wall wall)
        {
            try
            {
                return wall?.WallType?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private double GetSafeParameterValue(Wall wall, BuiltInParameter parameterType)
        {
            try
            {
                var param = wall?.get_Parameter(parameterType);
                return param?.AsDouble() ?? 0;
            }
            catch
            {
                return 0;
            }
        }
        
        #endregion

        #region Parameter Mapping Methods
        
        private void EnableRoomsParameterMapping()
        {
            try
            {
                // Update mapping configuration
                _roomsToCategoryMapping.IsEnabled = true;
                
                // Show the mapping controls
                RoomsMappingContainer.Visibility = System.Windows.Visibility.Visible;
                
                // Hide the enable button
                RoomsMappingEnableButton.Visibility = System.Windows.Visibility.Collapsed;
                
                // Update summary text
                UpdateParameterMappingLabels();
                
                StatusLabel.Content = "Rooms parameter mapping enabled";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error enabling rooms mapping: {ex.Message}";
            }
        }
        
        private void EnableCategoryParameterMapping()
        {
            try
            {
                // Update mapping configuration
                _categoryToRoomsMapping.IsEnabled = true;
                
                // Show the mapping controls
                CategoryMappingContainer.Visibility = System.Windows.Visibility.Visible;
                
                // Hide the enable button
                CategoryMappingEnableButton.Visibility = System.Windows.Visibility.Collapsed;
                
                // Update summary text
                UpdateParameterMappingLabels();
                
                StatusLabel.Content = "Category parameter mapping enabled";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error enabling category mapping: {ex.Message}";
            }
        }
        
        private void DisableRoomsParameterMapping()
        {
            try
            {
                // Update mapping configuration
                _roomsToCategoryMapping.IsEnabled = false;
                _roomsToCategoryMapping.FromParameter = null;
                _roomsToCategoryMapping.ToParameter = null;
                _roomsToCategoryMapping.ValueSeparator = "";
                
                // Hide the mapping controls
                RoomsMappingContainer.Visibility = System.Windows.Visibility.Collapsed;
                
                // Show the enable button
                RoomsMappingEnableButton.Visibility = System.Windows.Visibility.Visible;
                
                // Clear separator text and ComboBoxes
                RoomsSeparatorTextBox.Text = "";
                if (RoomsFromParameterCombo.Items.Count > 0) RoomsFromParameterCombo.SelectedIndex = 0;
                if (RoomsToParameterCombo.Items.Count > 0) RoomsToParameterCombo.SelectedIndex = 0;
                UpdateRoomsSeparatorPreview();
                
                // Update summary text
                UpdateParameterMappingLabels();
                
                StatusLabel.Content = "Rooms parameter mapping disabled";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error disabling rooms mapping: {ex.Message}";
            }
        }
        
        private void DisableCategoryParameterMapping()
        {
            try
            {
                // Update mapping configuration
                _categoryToRoomsMapping.IsEnabled = false;
                _categoryToRoomsMapping.FromParameter = null;
                _categoryToRoomsMapping.ToParameter = null;
                _categoryToRoomsMapping.ValueSeparator = "";
                
                // Hide the mapping controls
                CategoryMappingContainer.Visibility = System.Windows.Visibility.Collapsed;
                
                // Show the enable button
                CategoryMappingEnableButton.Visibility = System.Windows.Visibility.Visible;
                
                // Clear separator text and ComboBoxes
                CategorySeparatorTextBox.Text = "";
                if (CategoryFromParameterCombo.Items.Count > 0) CategoryFromParameterCombo.SelectedIndex = 0;
                if (CategoryToParameterCombo.Items.Count > 0) CategoryToParameterCombo.SelectedIndex = 0;
                UpdateCategorySeparatorPreview();
                
                // Update summary text
                UpdateParameterMappingLabels();
                
                StatusLabel.Content = "Category parameter mapping disabled";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error disabling category mapping: {ex.Message}";
            }
        }

        // Event handlers for parameter mapping
        private void OnRoomsMappingParameterChanged()
        {
            try
            {
                UpdateRoomsMappingConfiguration();
                StatusLabel.Content = "Rooms mapping parameters updated";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating rooms mapping: {ex.Message}";
            }
        }

        private void OnCategoryMappingParameterChanged()
        {
            try
            {
                UpdateCategoryMappingConfiguration();
                StatusLabel.Content = "Category mapping parameters updated";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating category mapping: {ex.Message}";
            }
        }

        private void OnRoomsSeparatorChanged()
        {
            try
            {
                _roomsToCategoryMapping.ValueSeparator = RoomsSeparatorTextBox.Text;
                UpdateRoomsSeparatorPreview();
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating rooms separator: {ex.Message}";
            }
        }

        private void OnCategorySeparatorChanged()
        {
            try
            {
                _categoryToRoomsMapping.ValueSeparator = CategorySeparatorTextBox.Text;
                UpdateCategorySeparatorPreview();
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error updating category separator: {ex.Message}";
            }
        }

        private void UpdateRoomsMappingConfiguration()
        {
            if (_parameterMappingService == null) return;

            var fromParamName = RoomsFromParameterCombo.SelectedItem?.ToString();
            var toParamName = RoomsToParameterCombo.SelectedItem?.ToString();

            _roomsToCategoryMapping.FromParameter = _parameterMappingService.RoomParameters
                .FirstOrDefault(p => p.Name == fromParamName);
            _roomsToCategoryMapping.ToParameter = _parameterMappingService.ElementParameters
                .FirstOrDefault(p => p.Name == toParamName);
        }

        private void UpdateCategoryMappingConfiguration()
        {
            if (_parameterMappingService == null) return;

            var fromParamName = CategoryFromParameterCombo.SelectedItem?.ToString();
            var toParamName = CategoryToParameterCombo.SelectedItem?.ToString();

            _categoryToRoomsMapping.FromParameter = _parameterMappingService.ElementParameters
                .FirstOrDefault(p => p.Name == fromParamName);
            _categoryToRoomsMapping.ToParameter = _parameterMappingService.RoomParameters
                .FirstOrDefault(p => p.Name == toParamName);
        }

        private void UpdateRoomsSeparatorPreview()
        {
            RoomsSeparatorPreview.Text = _parameterMappingService?.GenerateSeparatorPreview(RoomsSeparatorTextBox.Text) ?? "*value* *value*";
        }

        private void UpdateCategorySeparatorPreview()
        {
            CategorySeparatorPreview.Text = _parameterMappingService?.GenerateSeparatorPreview(CategorySeparatorTextBox.Text) ?? "*value* *value*";
        }

        private void AddRoomsParameterMapping()
        {
            try
            {
                // Find the main content within RoomsMappingContainer
                var border = RoomsMappingContainer.Child as Border;
                if (border?.Child is StackPanel mainPanel)
                {
                    // Create a new mapping row
                    var newMappingGrid = CreateParameterMappingRow("Rooms", true);
                    
                    // Add the new row to the main panel (before any existing additional rows)
                    mainPanel.Children.Insert(mainPanel.Children.Count, newMappingGrid);
                    
                    StatusLabel.Content = "Added new rooms parameter mapping row";
                }
                else
                {
                    StatusLabel.Content = "Could not find mapping container to add new row";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error adding rooms mapping: {ex.Message}";
            }
        }

        private void AddCategoryParameterMapping()
        {
            try
            {
                // Find the main content within CategoryMappingContainer
                var border = CategoryMappingContainer.Child as Border;
                if (border?.Child is StackPanel mainPanel)
                {
                    // Create a new mapping row
                    var newMappingGrid = CreateParameterMappingRow("Category", false);
                    
                    // Add the new row to the main panel (before any existing additional rows)
                    mainPanel.Children.Insert(mainPanel.Children.Count, newMappingGrid);
                    
                    StatusLabel.Content = "Added new category parameter mapping row";
                }
                else
                {
                    StatusLabel.Content = "Could not find mapping container to add new row";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error adding category mapping: {ex.Message}";
            }
        }

        private System.Windows.Controls.Grid CreateParameterMappingRow(string mappingType, bool isRoomsToCategory)
        {
            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
            
            // Define grid columns to match the existing layout
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // From Parameter
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // To Parameter
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Spacer
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Separator
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Add button (hidden)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Remove button
            
            // From Parameter
            var fromPanel = new StackPanel { Margin = new Thickness(0, 0, 12, 8) };
            var fromLabel = new TextBlock 
            { 
                Text = "From Parameter", 
                FontSize = 11, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)), 
                Margin = new Thickness(0, 0, 0, 2) 
            };
            var fromCombo = new ComboBox 
            { 
                Style = (Style)FindResource("ModernComboBox"), 
                MinWidth = 130, 
                HorizontalAlignment = HorizontalAlignment.Left 
            };
            
            fromPanel.Children.Add(fromLabel);
            fromPanel.Children.Add(fromCombo);
            System.Windows.Controls.Grid.SetColumn(fromPanel, 0);
            grid.Children.Add(fromPanel);
            
            // To Parameter
            var toPanel = new StackPanel { Margin = new Thickness(0, 0, 12, 8) };
            var toLabel = new TextBlock 
            { 
                Text = "To Parameter", 
                FontSize = 11, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)), 
                Margin = new Thickness(0, 0, 0, 2) 
            };
            var toCombo = new ComboBox 
            { 
                Style = (Style)FindResource("ModernComboBox"), 
                MinWidth = 130, 
                HorizontalAlignment = HorizontalAlignment.Left 
            };
            
            toPanel.Children.Add(toLabel);
            toPanel.Children.Add(toCombo);
            System.Windows.Controls.Grid.SetColumn(toPanel, 1);
            grid.Children.Add(toPanel);
            
            // Values Separator
            var separatorPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 8, 8) };
            var separatorLabel = new TextBlock 
            { 
                Text = "Values Separator", 
                FontSize = 11, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)), 
                Margin = new Thickness(0, 0, 0, 2) 
            };
            var separatorTextBox = new TextBox 
            { 
                Style = (Style)FindResource("ModernTextBox"), 
                Width = 80, 
                HorizontalAlignment = HorizontalAlignment.Left 
            };
            var separatorPreview = new TextBlock 
            { 
                Text = "value01 value02", 
                FontStyle = FontStyles.Italic, 
                FontSize = 9, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136)), 
                Margin = new Thickness(0, 2, 0, 0), 
                HorizontalAlignment = HorizontalAlignment.Left 
            };
            
            separatorPanel.Children.Add(separatorLabel);
            separatorPanel.Children.Add(separatorTextBox);
            separatorPanel.Children.Add(separatorPreview);
            System.Windows.Controls.Grid.SetColumn(separatorPanel, 3);
            grid.Children.Add(separatorPanel);
            
            // Remove button
            var removeButton = new Button 
            { 
                Content = "🗑️", 
                Style = (Style)FindResource("DeleteButton"), 
                VerticalAlignment = VerticalAlignment.Bottom,
                Width = 24, 
                Height = 24, 
                FontSize = 12, 
                Margin = new Thickness(0, 0, 0, 2) 
            };
            
            // Wire up remove button to remove this grid from its parent
            removeButton.Click += (s, e) =>
            {
                if (grid.Parent is StackPanel parentPanel)
                {
                    parentPanel.Children.Remove(grid);
                    StatusLabel.Content = $"Removed {mappingType.ToLower()} parameter mapping row";
                }
            };
            
            System.Windows.Controls.Grid.SetColumn(removeButton, 5);
            grid.Children.Add(removeButton);
            
            // Populate ComboBoxes based on mapping direction
            if (isRoomsToCategory)
            {
                // Rooms → Category mapping: From = Room parameters, To = Category parameters
                PopulateComboBox(fromCombo, _parameterMappingService?.RoomParameters);
                PopulateComboBox(toCombo, _parameterMappingService?.ElementParameters);
            }
            else
            {
                // Category → Rooms mapping: From = Category parameters, To = Room parameters
                PopulateComboBox(fromCombo, _parameterMappingService?.ElementParameters);
                PopulateComboBox(toCombo, _parameterMappingService?.RoomParameters);
            }
            
            // Wire up separator preview updates
            separatorTextBox.TextChanged += (s, e) =>
            {
                separatorPreview.Text = _parameterMappingService?.GenerateSeparatorPreview(separatorTextBox.Text) ?? "value01 value02";
            };
            
            return grid;
        }
        
        private void PopulateComboBox(ComboBox combo, List<ParameterInfo> parameters)
        {
            combo.Items.Add("Select Parameter");
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    combo.Items.Add(param.Name);
                }
            }
            combo.SelectedIndex = 0;
        }
        
        #endregion

        #region Event Handlers
        private void RunAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusLabel.Content = "Running analysis...";
                RunAnalysisButton.IsEnabled = false;
                
                // Run analysis through controller - use filtered elements from "Other Elements" panel
                // Convert filtered Element objects back to WallItem objects for analysis
                var wallItems = ConvertElementsToWallItems(_filteredElements ?? new List<Element>());

                // Get window handle for save dialog ownership
                var windowHelper = new WindowInteropHelper(this);
                var results = _controller.RunAnalysis(_filteredRooms, wallItems, windowHelper.Handle);
                
                // Show results
                var totalRooms = results.Count;
                var roomsWithCollisions = results.Count(r => r.WallsColliding > 0);
                var totalCollisions = results.Sum(r => r.WallsColliding);
                
                var message = $"Analysis Complete!\n\n" +
                             $"Total Rooms: {totalRooms}\n" +
                             $"Rooms with Collisions: {roomsWithCollisions}\n" +
                             $"Total Collisions: {totalCollisions}\n\n" +
                             $"Check the log file for detailed results.";
                
                MessageBox.Show(message, "RoomDataSync Results", MessageBoxButton.OK, MessageBoxImage.Information);
                
                StatusLabel.Content = "Analysis complete";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error during analysis: {ex.Message}";
                MessageBox.Show($"Error during analysis: {ex.Message}", "RoomDataSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RunAnalysisButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Cleanup and Disposal
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from UI event handlers to prevent memory leaks
                if (ElementCategoryCombo != null)
                    ElementCategoryCombo.SelectionChanged -= OnElementCategoryChanged;
                    
                if (MainOperatorCombo != null)
                    MainOperatorCombo.SelectionChanged -= OnMainOperatorChanged;
                    
                if (ElementMainOperatorCombo != null)
                    ElementMainOperatorCombo.SelectionChanged -= OnElementMainOperatorChanged;
                    
                if (RunAnalysisButton != null)
                    RunAnalysisButton.Click -= RunAnalysisButton_Click;
                    
                if (CancelButton != null)
                    CancelButton.Click -= CancelButton_Click;

                // Note: Lambda expressions for parameter mapping buttons are automatically cleaned up with the window

                // Clean up collections
                _availableElementParameters?.Clear();
                _availableElementParameters = null;
                
                _filteredElements?.Clear();
                _filteredElements = null;
                
                _availableCategories?.Clear();
                _availableCategories = null;
                
                _filteredRooms?.Clear();
                _filteredRooms = null;
                
                _roomItems?.Clear();
                _roomItems = null;

                // Clear filter configurations
                _currentElementFilter = null;
                _currentFilter = null;

                // Dispose services and controllers
                _parameterMappingService?.Dispose();
                _parameterMappingService = null;
                
                if (_elementController is IDisposable disposableElementController)
                    disposableElementController.Dispose();
                    
                if (_controller is IDisposable disposableController)
                    disposableController.Dispose();
                    
                _elementController = null;
                _controller = null;
                _elementCollector = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }
        #endregion
    }
}
