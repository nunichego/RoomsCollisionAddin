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
using RoomsManagerAddin.UI;
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfLabel = System.Windows.Controls.Label;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfGroupBox = System.Windows.Controls.GroupBox;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfGridSplitter = System.Windows.Controls.GridSplitter;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using WpfCursor = System.Windows.Input.Cursor;
using WpfCursors = System.Windows.Input.Cursors;

namespace RoomsManagerAddin
{
    /// <summary>
    /// WPF window for RoomDataSync analysis interface
    /// </summary>
    public class RoomWallAnalysisWindow : Window
    {
        #region Fields
        private Document _document;
        private RoomWallAnalysisController _controller;
        private List<RoomItem> _roomItems;
        private List<WallItem> _wallItems;
        private ElementCollectorService _elementCollector;
        
        // Filtered data
        private List<RoomItem> _filteredRooms;
        private List<WallItem> _filteredWalls;
        
        // UI Controls
        private WpfGrid mainGrid;
        private WpfGridSplitter splitter;
        private WpfGrid leftPanel;
        private WpfGrid rightPanel;
        private WpfGrid bottomPanel;
        
        // Filter controls
        private FilterRulesPanel filterRulesPanel;
        
        // Wall controls
        private WpfGroupBox wallsGroupBox;
        private WpfComboBox wallLevelFilter;
        private WpfComboBox wallTypeFilter;
        private WpfLabel wallSummaryLabel;
        private WpfLabel wallsCountLabel;
        private WpfListBox wallsListBox;
        private WpfTextBlock wallDetailsTextBlock;
        
        // Action controls
        private WpfLabel statusLabel;
        private WpfButton runAnalysisButton;
        private WpfButton testFilterButton;
        private WpfButton cancelButton;
        #endregion

        #region Constructor
        public RoomWallAnalysisWindow(Document document)
        {
            _document = document;
            _elementCollector = new ElementCollectorService();
            _controller = new RoomWallAnalysisController(document);
            
            InitializeWindow();
            LoadData();
            SetupEventHandlers();
        }
        #endregion

        #region Window Initialization
        private void InitializeWindow()
        {
            // Window setup - Modern styling like SimpleWindow template
            Title = "RoomDataSync - Rooms-Walls Analysis";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;
            Background = new WpfSolidColorBrush(WpfColor.FromRgb(248, 249, 250)); // #F8F9FA
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 14;

            // Main grid
            mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Top panel with splitter
            var topPanel = new WpfGrid();
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Left panel (Filter Rules)
            leftPanel = new WpfGrid();
            CreateFilterPanel();
            
            // Splitter
            splitter = new WpfGridSplitter
            {
                Width = 8,
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(224, 224, 224)), // #E0E0E0
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Right panel (Walls)
            rightPanel = new WpfGrid();
            CreateWallsPanel();
            
            // Bottom panel
            bottomPanel = new WpfGrid();
            CreateBottomPanel();
            
            // Assemble layout
            topPanel.Children.Add(leftPanel);
            WpfGrid.SetColumn(leftPanel, 0);
            
            topPanel.Children.Add(splitter);
            WpfGrid.SetColumn(splitter, 1);
            
            topPanel.Children.Add(rightPanel);
            WpfGrid.SetColumn(rightPanel, 2);
            
            mainGrid.Children.Add(topPanel);
            WpfGrid.SetRow(topPanel, 0);
            
            mainGrid.Children.Add(bottomPanel);
            WpfGrid.SetRow(bottomPanel, 1);
            
            Content = mainGrid;
        }

        private void CreateFilterPanel()
        {
            // Create the Filter Rules Panel
            filterRulesPanel = new FilterRulesPanel(_controller);
            filterRulesPanel.FilterChanged += OnFilterChanged;
            
            leftPanel.Children.Add(filterRulesPanel);
        }

        private void OnFilterChanged(object sender, FilterChangedEventArgs e)
        {
            try
            {
                // Apply the advanced filter and update the display
                var filteredRooms = _controller.ApplyAdvancedFilter(e.FilterConfiguration);
                _filteredRooms = filteredRooms;
                
                // Update UI (we'll keep the right panel for now to show results)
                UpdateWallList(); // Keep walls as-is for now
                
                // Update status
                statusLabel.Content = $"Filter applied: {filteredRooms.Count} rooms match criteria";
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Filter error: {ex.Message}";
            }
        }

        private void CreateWallsPanel()
        {
            wallsGroupBox = new WpfGroupBox
            {
                Header = "Walls",
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(16),
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(224, 224, 224)), // #E0E0E0
                BorderThickness = new Thickness(1),
                Background = new WpfSolidColorBrush(WpfColors.White)
            };

            var wallsGrid = new WpfGrid();
            wallsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Filters
            wallsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Summary
            wallsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Count
            wallsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // List
            wallsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Details

            // Filters row
            var filtersPanel = new WpfStackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            
            filtersPanel.Children.Add(new WpfLabel { Content = "Level:", Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(51, 51, 51)) });
            wallLevelFilter = new WpfComboBox { Width = 150, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
            ApplyModernComboBoxStyle(wallLevelFilter);
            filtersPanel.Children.Add(wallLevelFilter);
            
            filtersPanel.Children.Add(new WpfLabel { Content = "Type:", Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(51, 51, 51)) });
            wallTypeFilter = new WpfComboBox { Width = 150, Margin = new Thickness(0, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
            ApplyModernComboBoxStyle(wallTypeFilter);
            filtersPanel.Children.Add(wallTypeFilter);

            // Summary and count labels
            wallSummaryLabel = new WpfLabel { Content = "Loading walls...", Margin = new Thickness(0, 0, 0, 8), FontSize = 12, Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(102, 102, 102)) };
            wallsCountLabel = new WpfLabel { Content = "Walls: 0", Margin = new Thickness(0, 0, 0, 8), FontSize = 14, Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(51, 51, 51)), FontWeight = FontWeights.SemiBold };

            // List box
            wallsListBox = new WpfListBox { Margin = new Thickness(0, 8, 0, 0), Background = new WpfSolidColorBrush(WpfColors.White), BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(204, 204, 204)), BorderThickness = new Thickness(1) };

            // Details
            wallDetailsTextBlock = new WpfTextBlock { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(102, 102, 102)) };

            // Add to grid
            wallsGrid.Children.Add(filtersPanel);
            WpfGrid.SetRow(filtersPanel, 0);

            wallsGrid.Children.Add(wallSummaryLabel);
            WpfGrid.SetRow(wallSummaryLabel, 1);

            wallsGrid.Children.Add(wallsCountLabel);
            WpfGrid.SetRow(wallsCountLabel, 2);

            wallsGrid.Children.Add(wallsListBox);
            WpfGrid.SetRow(wallsListBox, 3);

            wallsGrid.Children.Add(wallDetailsTextBlock);
            WpfGrid.SetRow(wallDetailsTextBlock, 4);

            wallsGroupBox.Content = wallsGrid;
            rightPanel.Children.Add(wallsGroupBox);
        }

        private void CreateBottomPanel()
        {
            bottomPanel.Background = new WpfSolidColorBrush(WpfColors.White);
            bottomPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Status label
            statusLabel = new WpfLabel
            {
                Content = "Ready",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 12,
                Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(102, 102, 102))
            };

            // Buttons panel
            var buttonsPanel = new WpfStackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            runAnalysisButton = new WpfButton
            {
                Content = "Run Analysis",
                Width = 120,
                Height = 36,
                Margin = new Thickness(0, 0, 8, 0),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            ApplyModernButtonStyle(runAnalysisButton);

            testFilterButton = new WpfButton
            {
                Content = "Test Filter",
                Width = 100,
                Height = 36,
                Margin = new Thickness(0, 0, 8, 0),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            ApplySecondaryButtonStyle(testFilterButton);

            cancelButton = new WpfButton
            {
                Content = "Cancel",
                Width = 80,
                Height = 36,
                Margin = new Thickness(0, 0, 0, 0),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            ApplySecondaryButtonStyle(cancelButton);

            buttonsPanel.Children.Add(runAnalysisButton);
            buttonsPanel.Children.Add(testFilterButton);
            buttonsPanel.Children.Add(cancelButton);

            bottomPanel.Children.Add(statusLabel);
            WpfGrid.SetColumn(statusLabel, 0);

            bottomPanel.Children.Add(buttonsPanel);
            WpfGrid.SetColumn(buttonsPanel, 1);
        }
        #endregion

        #region Data Loading
        private void LoadData()
        {
            try
            {
                statusLabel.Content = "Loading data...";
                
                // Load data through controller
                var data = _controller.LoadInitialData();
                _roomItems = data.Rooms;
                _wallItems = data.Walls;
                _filteredRooms = new List<RoomItem>(_roomItems);
                _filteredWalls = new List<WallItem>(_wallItems);
                
                // Populate UI
                PopulateFilterDropdowns();
                UpdateWallList();
                
                statusLabel.Content = "Ready";
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Error loading data: {ex.Message}";
                MessageBox.Show($"Error loading data: {ex.Message}", "RoomDataSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupEventHandlers()
        {
            // Wall filter change events
            wallLevelFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            wallTypeFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            
            // List selection events
            wallsListBox.SelectionChanged += (s, e) => UpdateWallDetails();
            
            // Button events
            runAnalysisButton.Click += RunAnalysisButton_Click;
            testFilterButton.Click += TestFilterButton_Click;
            cancelButton.Click += CancelButton_Click;
        }

        private void PopulateFilterDropdowns()
        {
            // Wall level filter
            var wallLevels = _wallItems.Select(w => w.LevelName).Distinct().OrderBy(l => l).ToList();
            wallLevels.Insert(0, "All Levels");
            wallLevelFilter.ItemsSource = wallLevels;
            wallLevelFilter.SelectedIndex = 0;
            
            // Wall type filter
            var wallTypes = _wallItems.Select(w => w.WallTypeName).Distinct().OrderBy(t => t).ToList();
            wallTypes.Insert(0, "All Types");
            wallTypeFilter.ItemsSource = wallTypes;
            wallTypeFilter.SelectedIndex = 0;
        }
        #endregion

        #region Filtering

        private void ApplyWallFilters()
        {
            try
            {
                var levelFilter = wallLevelFilter.SelectedItem as string;
                var typeFilter = wallTypeFilter.SelectedItem as string;
                
                _filteredWalls = _controller.ApplyWallFilters(_wallItems, levelFilter, typeFilter);
                UpdateWallList();
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Error applying wall filters: {ex.Message}";
            }
        }
        #endregion

        #region UI Updates
        private void UpdateWallList()
        {
            wallsListBox.ItemsSource = _filteredWalls.Select(w => $"{w.Name} ({w.LevelName})");
            wallSummaryLabel.Content = $"Showing {_filteredWalls.Count} of {_wallItems.Count} walls";
            wallsCountLabel.Content = $"Walls: {_filteredWalls.Count}";
        }

        private void UpdateWallDetails()
        {
            if (wallsListBox.SelectedIndex >= 0 && wallsListBox.SelectedIndex < _filteredWalls.Count)
            {
                var wall = _filteredWalls[wallsListBox.SelectedIndex];
                wallDetailsTextBlock.Text = $"Name: {wall.Name}\nLevel: {wall.LevelName}\nType: {wall.WallTypeName}\nLength: {wall.Length:F2} ft";
            }
            else
            {
                wallDetailsTextBlock.Text = "";
            }
        }
        #endregion

        #region Event Handlers
        private void RunAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                statusLabel.Content = "Running analysis...";
                runAnalysisButton.IsEnabled = false;
                
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
                
                statusLabel.Content = "Analysis complete";
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Error during analysis: {ex.Message}";
                MessageBox.Show($"Error during analysis: {ex.Message}", "RoomDataSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                runAnalysisButton.IsEnabled = true;
            }
        }

        private void TestFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                statusLabel.Content = "Testing filter system...";
                testFilterButton.IsEnabled = false;

                // Get available parameters
                var parameters = _controller.GetAvailableRoomParameters();
                var paramCount = parameters.Count;

                // Create and test a sample filter
                var sampleFilter = _controller.CreateSampleFilter();
                var matchingRooms = _controller.CountMatchingRooms(sampleFilter);
                var filterDescription = _controller.GetFilterDescription(sampleFilter);

                var message = $"Filter System Test Results:\n\n" +
                             $"Available Parameters: {paramCount}\n" +
                             $"Sample Filter: {filterDescription}\n" +
                             $"Matching Rooms: {matchingRooms}\n\n" +
                             $"Filter system is working correctly!";

                MessageBox.Show(message, "Filter System Test", MessageBoxButton.OK, MessageBoxImage.Information);
                statusLabel.Content = "Filter test completed";
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Filter test failed: {ex.Message}";
                MessageBox.Show($"Filter test error: {ex.Message}", "Filter Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                testFilterButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Modern Styling Methods
        private void ApplyModernButtonStyle(WpfButton button)
        {
            button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)); // #0078D4
            button.Foreground = new WpfSolidColorBrush(WpfColors.White);
            button.BorderThickness = new Thickness(0);
            button.Cursor = WpfCursors.Hand;
            
            // Add hover and press effects
            button.MouseEnter += (s, e) => button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(0, 90, 158)); // #005A9E
            button.MouseLeave += (s, e) => button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)); // #0078D4
        }

        private void ApplySecondaryButtonStyle(WpfButton button)
        {
            button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(227, 242, 253)); // #E3F2FD
            button.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(25, 118, 210)); // #1976D2
            button.BorderThickness = new Thickness(1);
            button.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(25, 118, 210)); // #1976D2
            button.Cursor = WpfCursors.Hand;
            
            // Add hover and press effects
            button.MouseEnter += (s, e) => 
            {
                button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(187, 222, 251)); // #BBDEFB
                button.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(21, 101, 192)); // #1565C0
            };
            button.MouseLeave += (s, e) => 
            {
                button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(227, 242, 253)); // #E3F2FD
                button.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(25, 118, 210)); // #1976D2
            };
        }

        private void ApplyModernTextBoxStyle(WpfTextBox textBox)
        {
            textBox.Background = new WpfSolidColorBrush(WpfColors.White);
            textBox.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(204, 204, 204)); // #CCCCCC
            textBox.BorderThickness = new Thickness(1);
            textBox.Padding = new Thickness(8, 6, 8, 6);
            
            // Add focus effect
            textBox.GotFocus += (s, e) => textBox.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)); // #0078D4
            textBox.LostFocus += (s, e) => textBox.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(204, 204, 204)); // #CCCCCC
        }

        private void ApplyModernComboBoxStyle(WpfComboBox comboBox)
        {
            comboBox.Background = new WpfSolidColorBrush(WpfColors.White);
            comboBox.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(204, 204, 204)); // #CCCCCC
            comboBox.BorderThickness = new Thickness(1);
            comboBox.Padding = new Thickness(8, 6, 8, 6);
        }
        #endregion
    }
}
