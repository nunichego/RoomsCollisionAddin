using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private List<RoomItem> _roomItems;
        private List<WallItem> _wallItems;
        private ElementCollectorService _elementCollector;
        private List<ParameterInfo> _availableParameters;
        private RoomFilterConfiguration _currentFilter;
        
        // Filtered data
        private List<RoomItem> _filteredRooms;
        private List<WallItem> _filteredWalls;
        #endregion

        #region Constructor
        public RoomWallAnalysisWindow(Document document)
        {
            _document = document;
            _elementCollector = new ElementCollectorService();
            _controller = new RoomWallAnalysisController(document);
            _availableParameters = _controller.GetAvailableRoomParameters();
            _currentFilter = _controller.CreateFilterConfiguration("Room Filter");

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
                
                // Load data through controller
                var data = _controller.LoadInitialData();
                _roomItems = data.Rooms;
                _wallItems = data.Walls;
                _filteredRooms = new List<RoomItem>(_roomItems);
                _filteredWalls = new List<WallItem>(_wallItems);
                
                // Populate UI
                PopulateFilterDropdowns();
                UpdateWallList();
                UpdateCounters();
                
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
            // Wall filters
            WallLevelFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            WallTypeFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            
            // Room filters removed - using advanced filters only
            MainOperatorCombo.SelectionChanged += (s, e) => OnMainOperatorChanged();
            
            // Filter buttons
            AddRuleButton.Click += (s, e) => AddNewRule();
            AddSetButton.Click += (s, e) => AddNewSet();
            ClearFiltersButton.Click += (s, e) => ClearAllFilters();
            FilterStatusAddRuleButton.Click += (s, e) => AddNewRule();
            
            // List selection events
            WallsListBox.SelectionChanged += (s, e) => UpdateWallDetails();

            // Analysis buttons
            RunAnalysisButton.Click += RunAnalysisButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        private void PopulateFilterDropdowns()
        {
            // Wall level filter
            var wallLevels = _wallItems.Select(w => w.LevelName).Distinct().OrderBy(l => l).ToList();
            wallLevels.Insert(0, "All Levels");
            WallLevelFilter.ItemsSource = wallLevels;
            WallLevelFilter.SelectedIndex = 0;
            
            // Wall type filter
            var wallTypes = _wallItems.Select(w => w.WallTypeName).Distinct().OrderBy(t => t).ToList();
            wallTypes.Insert(0, "All Types");
            WallTypeFilter.ItemsSource = wallTypes;
            WallTypeFilter.SelectedIndex = 0;
            
            // Quick filters removed
        }
        #endregion

        #region Filtering
        private void ApplyWallFilters()
        {
            try
            {
                var levelFilter = WallLevelFilter.SelectedItem as string;
                var typeFilter = WallTypeFilter.SelectedItem as string;
                
                _filteredWalls = _controller.ApplyWallFilters(_wallItems, levelFilter, typeFilter);
                UpdateWallList();
                UpdateCounters();
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error applying wall filters: {ex.Message}";
            }
        }
        #endregion

        #region UI Updates
        private void UpdateWallList()
        {
            WallsListBox.ItemsSource = _filteredWalls.Select(w => $"{w.Name} ({w.LevelName})");
            WallSummaryLabel.Content = $"Showing {_filteredWalls.Count} of {_wallItems.Count} walls";
        }

        private void UpdateCounters()
        {
            WallsCountLabel.Content = $"Walls: {_filteredWalls.Count}";
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

        private void UpdateWallDetails()
        {
            if (WallsListBox.SelectedIndex >= 0 && WallsListBox.SelectedIndex < _filteredWalls.Count)
            {
                var wall = _filteredWalls[WallsListBox.SelectedIndex];
                WallDetailsTextBlock.Text = $"Name: {wall.Name}\nLevel: {wall.LevelName}\nType: {wall.WallTypeName}\nLength: {wall.Length:F2} ft";
            }
            else
            {
                WallDetailsTextBlock.Text = "";
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
            
            // Empty state for set
            if (!filterSet.Items?.Any() == true)
            {
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
                setRulesContainer.Children.Add(emptyPanel);
            }
            
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

        #region Event Handlers
        private void RunAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusLabel.Content = "Running analysis...";
                RunAnalysisButton.IsEnabled = false;
                
                // Run analysis through controller
                var results = _controller.RunAnalysis(_filteredRooms, _filteredWalls);
                
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
    }
}
