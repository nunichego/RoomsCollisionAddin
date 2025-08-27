using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Controllers;
using RoomsManagerAddin.UI;

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

            InitializeComponent();

            // Host the existing FilterRulesPanel (programmatic control) into the XAML placeholder
            var filterPanel = new FilterRulesPanel(_controller);
            filterPanel.FilterChanged += OnFilterChanged;
            var leftHost = this.FindName("LeftHost") as System.Windows.Controls.Grid;
            if (leftHost != null)
            {
                leftHost.Children.Add(filterPanel);
            }

            LoadData();
            SetupEventHandlers();
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
            // We keep only walls filters in this window; room filtering handled by FilterRulesPanel
            WallLevelFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            WallTypeFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            
            // List selection events
            WallsListBox.SelectionChanged += (s, e) => UpdateWallDetails();

            // Buttons
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

        private void OnFilterChanged(object sender, FilterChangedEventArgs e)
        {
            try
            {
                // Apply advanced filter to rooms and update status
                var filteredRooms = _controller.ApplyAdvancedFilter(e.FilterConfiguration);
                _filteredRooms = filteredRooms;
                StatusLabel.Content = $"Filter applied: {filteredRooms.Count} rooms match criteria";
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
