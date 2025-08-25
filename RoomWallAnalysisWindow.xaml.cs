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
                UpdateRoomList();
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
            // Filter change events
            RoomLevelFilter.SelectionChanged += (s, e) => ApplyRoomFilters();
            RoomAreaFilter.TextChanged += (s, e) => ApplyRoomFilters();
            WallLevelFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            WallTypeFilter.SelectionChanged += (s, e) => ApplyWallFilters();
            
            // List selection events
            RoomsListBox.SelectionChanged += (s, e) => UpdateRoomDetails();
            WallsListBox.SelectionChanged += (s, e) => UpdateWallDetails();
        }

        private void PopulateFilterDropdowns()
        {
            // Room level filter
            var roomLevels = _roomItems.Select(r => r.LevelName).Distinct().OrderBy(l => l).ToList();
            roomLevels.Insert(0, "All Levels");
            RoomLevelFilter.ItemsSource = roomLevels;
            RoomLevelFilter.SelectedIndex = 0;
            
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
        private void ApplyRoomFilters()
        {
            try
            {
                var levelFilter = RoomLevelFilter.SelectedItem as string;
                var areaFilter = RoomAreaFilter.Text;
                
                _filteredRooms = _controller.ApplyRoomFilters(_roomItems, levelFilter, areaFilter);
                UpdateRoomList();
                UpdateCounters();
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error applying room filters: {ex.Message}";
            }
        }

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
        private void UpdateRoomList()
        {
            RoomsListBox.ItemsSource = _filteredRooms.Select(r => $"{r.Name} ({r.LevelName})");
            RoomSummaryLabel.Content = $"Showing {_filteredRooms.Count} of {_roomItems.Count} rooms";
        }

        private void UpdateWallList()
        {
            WallsListBox.ItemsSource = _filteredWalls.Select(w => $"{w.Name} ({w.LevelName})");
            WallSummaryLabel.Content = $"Showing {_filteredWalls.Count} of {_wallItems.Count} walls";
        }

        private void UpdateCounters()
        {
            RoomsCountLabel.Content = $"Rooms: {_filteredRooms.Count}";
            WallsCountLabel.Content = $"Walls: {_filteredWalls.Count}";
        }

        private void UpdateRoomDetails()
        {
            if (RoomsListBox.SelectedIndex >= 0 && RoomsListBox.SelectedIndex < _filteredRooms.Count)
            {
                var room = _filteredRooms[RoomsListBox.SelectedIndex];
                RoomDetailsTextBlock.Text = $"Name: {room.Name}\nLevel: {room.LevelName}\nArea: {room.Area:F2} sq ft\nVolume: {room.Volume:F2} cu ft";
            }
            else
            {
                RoomDetailsTextBlock.Text = "";
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
