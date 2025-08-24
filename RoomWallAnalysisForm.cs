using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Controllers;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Main interface form for RoomDataSync analysis
    /// </summary>
    public class RoomWallAnalysisForm : System.Windows.Forms.Form
    {
        #region Fields
        private Document _document;
        private RoomWallAnalysisController _controller;
        private List<RoomItem> _roomItems;
        private List<WallItem> _wallItems;
        private ElementCollectorService _elementCollector;
        
        // UI Controls
        private SplitContainer mainSplitContainer;
        private GroupBox roomsGroupBox;
        private GroupBox wallsGroupBox;
        private Label roomsCountLabel;
        private Label wallsCountLabel;
        private Button runAnalysisButton;
        private Button cancelButton;
        private Label statusLabel;
        
        // Filter controls
        private ComboBox _roomLevelFilter;
        private TextBox _roomAreaFilter;
        private Label _roomSummaryLabel;
        private ComboBox _wallLevelFilter;
        private ComboBox _wallTypeFilter;
        private Label _wallSummaryLabel;
        
        // Filtered data
        private List<RoomItem> _filteredRooms;
        private List<WallItem> _filteredWalls;
        #endregion

        #region Constructor
        public RoomWallAnalysisForm(Document document)
        {
            _document = document;
            _elementCollector = new ElementCollectorService();
            _controller = new RoomWallAnalysisController(document);
            
            InitializeComponent();
        }
        #endregion

        #region Component Initialization
        private void InitializeComponent()
        {
            // Form setup - Authentic Revit native style
            this.Text = "RoomDataSync - Rooms-Walls Analysis";
            this.Size = new System.Drawing.Size(1200, 800); // Reasonable default size
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.SystemColors.Control; // Windows standard background
            this.FormBorderStyle = FormBorderStyle.Sizable; // Make resizable
            this.MaximizeBox = true; // Allow maximize
            this.MinimizeBox = true; // Allow minimize
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular); // Windows standard font
            this.AutoScaleMode = AutoScaleMode.Dpi; // Scale UI for 4K/HiDPI

            // Create main split container (left/right)
            mainSplitContainer = new SplitContainer();
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.SplitterDistance = 600; // Will be set to center after form loads
            mainSplitContainer.SplitterWidth = 8; // Make splitter wider and more visible
            mainSplitContainer.BackColor = System.Drawing.SystemColors.Control;

            // Create rooms section
            CreateRoomsSection();
            
            // Create walls section
            CreateWallsSection();

            // Assemble the layout
            mainSplitContainer.Panel1.Controls.Add(roomsGroupBox);
            mainSplitContainer.Panel2.Controls.Add(wallsGroupBox);
            this.Controls.Add(mainSplitContainer);

            // Create action buttons (add after main layout so it stays on top)
            CreateActionButtons();

            // Load data after form is created
            this.Load += (s, e) => {
                // Center the splitter
                CenterSplitter();
                
                LoadData();
                UpdateCounters();
            };

            // Keep splitter centered when window is resized
            this.Resize += (s, e) => {
                if (mainSplitContainer != null && mainSplitContainer.Width > 0)
                {
                    // Maintain proportional centering
                    var currentRatio = (double)mainSplitContainer.SplitterDistance / mainSplitContainer.Width;
                    if (Math.Abs(currentRatio - 0.5) > 0.1) // If significantly off-center, recenter
                    {
                        CenterSplitter();
                    }
                }
            };
        }

        private void CreateRoomsSection()
        {
            roomsGroupBox = new GroupBox();
            roomsGroupBox.Text = "Rooms";
            roomsGroupBox.Dock = DockStyle.Fill;
            roomsGroupBox.Padding = new Padding(12, 8, 12, 12); // 7 DLU equivalent margins
            roomsGroupBox.BackColor = System.Drawing.SystemColors.Control;
            roomsGroupBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);

            // Rooms counter
            roomsCountLabel = new Label();
            roomsCountLabel.Text = "0 of 0 rooms selected";
            roomsCountLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            roomsCountLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            roomsCountLabel.Location = new System.Drawing.Point(20, 40);
            roomsCountLabel.Size = new System.Drawing.Size(400, 30);
            roomsCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Filter by Level
            var levelFilterLabel = new Label();
            levelFilterLabel.Text = "Filter by Level:";
            levelFilterLabel.Location = new System.Drawing.Point(20, 90);
            levelFilterLabel.Size = new System.Drawing.Size(120, 25);
            levelFilterLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            levelFilterLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            levelFilterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var levelFilterCombo = new ComboBox();
            levelFilterCombo.Location = new System.Drawing.Point(150, 88);
            levelFilterCombo.Size = new System.Drawing.Size(200, 28);
            levelFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            levelFilterCombo.Font = new System.Drawing.Font("Segoe UI", 9F);
            levelFilterCombo.Items.Add("All Levels");
            levelFilterCombo.SelectedIndex = 0;
            levelFilterCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Filter by Area
            var areaFilterLabel = new Label();
            areaFilterLabel.Text = "Min Area (SF):";
            areaFilterLabel.Location = new System.Drawing.Point(20, 130);
            areaFilterLabel.Size = new System.Drawing.Size(120, 25);
            areaFilterLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            areaFilterLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            areaFilterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var areaFilterTextBox = new TextBox();
            areaFilterTextBox.Location = new System.Drawing.Point(150, 128);
            areaFilterTextBox.Size = new System.Drawing.Size(100, 28);
            areaFilterTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            areaFilterTextBox.Text = "0";
            areaFilterTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Apply Filter button
            var applyFilterButton = new Button();
            applyFilterButton.Text = "Apply Filter";
            applyFilterButton.Location = new System.Drawing.Point(270, 127);
            applyFilterButton.Size = new System.Drawing.Size(100, 28);
            applyFilterButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            applyFilterButton.UseVisualStyleBackColor = true; // Windows theme button
            applyFilterButton.Click += (s, e) => ApplyRoomFilters(levelFilterCombo.Text, areaFilterTextBox.Text, "");
            applyFilterButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Summary display (filtered results)
            var summaryLabel = new Label();
            summaryLabel.Text = "Filtered Results: All rooms will be included in analysis";
            summaryLabel.Location = new System.Drawing.Point(20, 170);
            summaryLabel.Size = new System.Drawing.Size(700, 35);
            summaryLabel.Font = new System.Drawing.Font("Segoe UI", 9F); // Larger font for better readability
            summaryLabel.ForeColor = System.Drawing.SystemColors.HotTrack; // Windows blue color
            summaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            roomsGroupBox.Controls.Add(roomsCountLabel);
            roomsGroupBox.Controls.Add(levelFilterLabel);
            roomsGroupBox.Controls.Add(levelFilterCombo);
            roomsGroupBox.Controls.Add(areaFilterLabel);
            roomsGroupBox.Controls.Add(areaFilterTextBox);
            roomsGroupBox.Controls.Add(applyFilterButton);
            roomsGroupBox.Controls.Add(summaryLabel);

            // Store references for later use
            _roomLevelFilter = levelFilterCombo;
            _roomAreaFilter = areaFilterTextBox;
            _roomSummaryLabel = summaryLabel;
        }

        private void CreateWallsSection()
        {
            wallsGroupBox = new GroupBox();
            wallsGroupBox.Text = "Walls";
            wallsGroupBox.Dock = DockStyle.Fill;
            wallsGroupBox.Padding = new Padding(12, 8, 12, 12); // 7 DLU equivalent margins
            wallsGroupBox.BackColor = System.Drawing.SystemColors.Control;
            wallsGroupBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);

            // Walls counter
            wallsCountLabel = new Label();
            wallsCountLabel.Text = "0 of 0 walls selected";
            wallsCountLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            wallsCountLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            wallsCountLabel.Location = new System.Drawing.Point(20, 40);
            wallsCountLabel.Size = new System.Drawing.Size(400, 30);
            wallsCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Filter by Level
            var levelFilterLabel = new Label();
            levelFilterLabel.Text = "Filter by Level:";
            levelFilterLabel.Location = new System.Drawing.Point(20, 90);
            levelFilterLabel.Size = new System.Drawing.Size(120, 25);
            levelFilterLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            levelFilterLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            levelFilterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var levelFilterCombo = new ComboBox();
            levelFilterCombo.Location = new System.Drawing.Point(150, 88);
            levelFilterCombo.Size = new System.Drawing.Size(200, 28);
            levelFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            levelFilterCombo.Font = new System.Drawing.Font("Segoe UI", 9F);
            levelFilterCombo.Items.Add("All Levels");
            levelFilterCombo.SelectedIndex = 0;
            levelFilterCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Filter by Wall Type
            var typeFilterLabel = new Label();
            typeFilterLabel.Text = "Wall Type:";
            typeFilterLabel.Location = new System.Drawing.Point(20, 130);
            typeFilterLabel.Size = new System.Drawing.Size(100, 25);
            typeFilterLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            typeFilterLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            typeFilterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var typeFilterCombo = new ComboBox();
            typeFilterCombo.Location = new System.Drawing.Point(150, 128);
            typeFilterCombo.Size = new System.Drawing.Size(200, 28);
            typeFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            typeFilterCombo.Font = new System.Drawing.Font("Segoe UI", 9F);
            typeFilterCombo.Items.Add("All Types");
            typeFilterCombo.SelectedIndex = 0;
            typeFilterCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Apply Filter button
            var applyFilterButton = new Button();
            applyFilterButton.Text = "Apply Filter";
            applyFilterButton.Location = new System.Drawing.Point(270, 127);
            applyFilterButton.Size = new System.Drawing.Size(100, 28);
            applyFilterButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            applyFilterButton.UseVisualStyleBackColor = true; // Windows theme button
            applyFilterButton.Click += (s, e) => ApplyWallFilters(levelFilterCombo.Text, typeFilterCombo.Text, "");
            applyFilterButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Summary display (filtered results)
            var summaryLabel = new Label();
            summaryLabel.Text = "Filtered Results: All walls will be included in analysis";
            summaryLabel.Location = new System.Drawing.Point(20, 170);
            summaryLabel.Size = new System.Drawing.Size(700, 35);
            summaryLabel.Font = new System.Drawing.Font("Segoe UI", 9F); // Larger font for better readability
            summaryLabel.ForeColor = System.Drawing.SystemColors.HotTrack; // Windows blue color
            summaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            wallsGroupBox.Controls.Add(wallsCountLabel);
            wallsGroupBox.Controls.Add(levelFilterLabel);
            wallsGroupBox.Controls.Add(levelFilterCombo);
            wallsGroupBox.Controls.Add(typeFilterLabel);
            wallsGroupBox.Controls.Add(typeFilterCombo);
            wallsGroupBox.Controls.Add(applyFilterButton);
            wallsGroupBox.Controls.Add(summaryLabel);

            // Store references for later use
            _wallLevelFilter = levelFilterCombo;
            _wallTypeFilter = typeFilterCombo;
            _wallSummaryLabel = summaryLabel;
        }

        private void CreateActionButtons()
        {
            // Create a panel for the bottom section
            var bottomPanel = new System.Windows.Forms.Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 56;
            bottomPanel.BackColor = System.Drawing.SystemColors.Control;

            // Use a table layout to keep status on the left and buttons on the right
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.Padding = new Padding(12, 12, 12, 12);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Status label
            statusLabel = new Label();
            statusLabel.Text = "Ready to analyze. Apply filters, then click 'Run'.";
            statusLabel.AutoSize = true;
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusLabel.ForeColor = System.Drawing.SystemColors.ControlText;

            // Right-aligned button flow
            var buttonFlow = new FlowLayoutPanel();
            buttonFlow.FlowDirection = FlowDirection.LeftToRight;
            buttonFlow.WrapContents = false;
            buttonFlow.AutoSize = true;
            buttonFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonFlow.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            // Run button
            runAnalysisButton = new Button();
            runAnalysisButton.Text = "Run";
            runAnalysisButton.AutoSize = true;
            runAnalysisButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            runAnalysisButton.Margin = new Padding(0, 0, 8, 0);
            runAnalysisButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            runAnalysisButton.UseVisualStyleBackColor = true;
            runAnalysisButton.Click += RunAnalysisButton_Click;

            // Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.AutoSize = true;
            cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cancelButton.Margin = new Padding(0, 0, 0, 0);
            cancelButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += (s, e) => this.Close();

            buttonFlow.Controls.Add(runAnalysisButton);
            buttonFlow.Controls.Add(cancelButton);

            layout.Controls.Add(statusLabel, 0, 0);
            layout.Controls.Add(buttonFlow, 1, 0);

            bottomPanel.Controls.Add(layout);
            bottomPanel.BringToFront();
            this.Controls.Add(bottomPanel);
        }

        private void CenterSplitter()
        {
            // Center the splitter after form is fully loaded
            if (mainSplitContainer != null && mainSplitContainer.Width > 0)
            {
                mainSplitContainer.SplitterDistance = mainSplitContainer.Width / 2;
            }
        }
        #endregion

        #region Data Loading
        private void LoadData()
        {
            try
            {
                // Load via controller
                _controller.LoadElements(out _roomItems, out _wallItems);

                // Populate filter dropdowns
                PopulateFilterDropdowns();

                // Apply default filters (no filtering initially)
                ApplyRoomFilters("All Levels", "0", "");
                ApplyWallFilters("All Levels", "All Types", "");

                // Debug info
                System.Diagnostics.Debug.WriteLine($"Loaded {_roomItems.Count} rooms and {_wallItems.Count} walls");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading elements: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateFilterDropdowns()
        {
            // Get unique levels for rooms
            var roomLevels = _roomItems.Where(r => r.Room.Level != null)
                                     .Select(r => r.Room.Level.Name)
                                     .Distinct()
                                     .OrderBy(name => name)
                                     .ToList();
            
            _roomLevelFilter.Items.Clear();
            _roomLevelFilter.Items.Add("All Levels");
            foreach (var level in roomLevels)
            {
                _roomLevelFilter.Items.Add(level);
            }
            _roomLevelFilter.SelectedIndex = 0;

            // Get unique levels for walls
            var wallLevels = _wallItems.Where(w => w.GetLevel() != null)
                                     .Select(w => w.GetLevel())
                                     .Distinct()
                                     .OrderBy(name => name)
                                     .ToList();
            
            _wallLevelFilter.Items.Clear();
            _wallLevelFilter.Items.Add("All Levels");
            foreach (var level in wallLevels)
            {
                _wallLevelFilter.Items.Add(level);
            }
            _wallLevelFilter.SelectedIndex = 0;

            // Get unique wall types
            var wallTypes = _wallItems.Select(w => w.Wall.WallType?.Name ?? "Unknown")
                                     .Distinct()
                                     .OrderBy(name => name)
                                     .ToList();
            
            _wallTypeFilter.Items.Clear();
            _wallTypeFilter.Items.Add("All Types");
            foreach (var type in wallTypes)
            {
                _wallTypeFilter.Items.Add(type);
            }
            _wallTypeFilter.SelectedIndex = 0;
        }

        private void ApplyRoomFilters(string levelFilter, string minAreaText, string nameFilter)
        {
            try
            {
                _filteredRooms = _controller.FilterRooms(_roomItems, levelFilter, minAreaText);

                // Name filtering removed for cleaner interface

                // Update UI
                roomsCountLabel.Text = $"{_filteredRooms.Count} of {_roomItems.Count} rooms selected";
                _roomSummaryLabel.Text = $"Filtered Results: {_filteredRooms.Count} rooms will be included in analysis";
                
                if (_filteredRooms.Count != _roomItems.Count)
                {
                    _roomSummaryLabel.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    _roomSummaryLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying room filters: {ex.Message}", "Filter Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyWallFilters(string levelFilter, string typeFilter, string nameFilter)
        {
            try
            {
                _filteredWalls = _controller.FilterWalls(_wallItems, levelFilter, typeFilter);

                // Name filtering removed for cleaner interface

                // Update UI
                wallsCountLabel.Text = $"{_filteredWalls.Count} of {_wallItems.Count} walls selected";
                _wallSummaryLabel.Text = $"Filtered Results: {_filteredWalls.Count} walls will be included in analysis";
                
                if (_filteredWalls.Count != _wallItems.Count)
                {
                    _wallSummaryLabel.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    _wallSummaryLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying wall filters: {ex.Message}", "Filter Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateCounters()
        {
            if (_filteredRooms != null && _filteredWalls != null)
            {
                roomsCountLabel.Text = $"{_filteredRooms.Count} of {_roomItems?.Count ?? 0} rooms selected";
                wallsCountLabel.Text = $"{_filteredWalls.Count} of {_wallItems?.Count ?? 0} walls selected";
            }
        }
        #endregion

        #region Event Handlers
        private void RunAnalysisButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_filteredRooms == null || !_filteredRooms.Any())
                {
                    MessageBox.Show("Please apply room filters to select rooms for analysis.", "No Rooms Selected", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_filteredWalls == null || !_filteredWalls.Any())
                {
                    MessageBox.Show("Please apply wall filters to select walls for analysis.", "No Walls Selected", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                statusLabel.Text = "Starting analysis...";
                this.Enabled = false;

                // Show progress and run analysis
                RunCollisionAnalysis(_filteredRooms, _filteredWalls);
            }
            catch (Exception ex)
            {
                this.Enabled = true;
                statusLabel.Text = "Analysis failed.";
                MessageBox.Show($"Error running analysis: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Analysis Methods
        private void RunCollisionAnalysis(List<RoomItem> selectedRooms, List<WallItem> selectedWalls)
        {
            try
            {
                // Convert back to Revit elements
                var rooms = selectedRooms.Select(r => r.Room).ToList();
                var walls = selectedWalls.Select(w => w.Wall).ToList();

                // Create and show progress form
                var progressForm = new ProgressForm();
                progressForm.Show();

                // Initialize services
                // Create progress callback
                Action<string, string, int, int, int, int> progressCallback = 
                    (title, message, stepCurrent, stepTotal, overallCurrent, overallTotal) =>
                    {
                        progressForm.UpdateProgress(title, message, stepCurrent, stepTotal, overallCurrent, overallTotal);
                    };

                // Run analysis via controller
                var results = _controller.Analyze(rooms, walls, progressCallback);

                // Close progress form
                progressForm.Close();
                this.Enabled = true;

                // Show results
                ShowAnalysisResults(results);
            }
            catch (Exception)
            {
                this.Enabled = true;
                statusLabel.Text = "Analysis failed.";
                throw;
            }
        }

        // Controller encapsulates services; local DI initializer is no longer needed

        private void ShowAnalysisResults(List<RoomCollisionResult> results)
        {
            var totalRooms = results.Count;
            var roomsWithCollisions = results.Count(r => r.WallsColliding > 0);
            var totalCollisions = results.Sum(r => r.WallsColliding);

            // Update status label with shorter, clearer message
            statusLabel.Text = $"Analysis complete: {roomsWithCollisions}/{totalRooms} rooms have collisions ({totalCollisions} total)";

            var message = $"Analysis Complete!\n\n" +
                         $"Total Rooms Analyzed: {totalRooms}\n" +
                         $"Rooms with Collisions: {roomsWithCollisions}\n" +
                         $"Total Collisions Found: {totalCollisions}\n\n" +
                         $"Parameter exchange completed successfully.\n" +
                         $"Check the log file for detailed results.";

            MessageBox.Show(message, "Analysis Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }

    #region Data Classes
    public class RoomItem
    {
        public Room Room { get; private set; }

        public RoomItem(Room room)
        {
            Room = room;
        }

        public override string ToString()
        {
            var area = Room.Area > 0 ? $" ({Room.Area:F1} SF)" : "";
            return $"{Room.Number} - {Room.Name}{area} [{Room.Level?.Name}]";
        }
    }

    public class WallItem
    {
        public Wall Wall { get; private set; }

        public WallItem(Wall wall)
        {
            Wall = wall;
        }

        public string GetLevel()
        {
            var levelParam = Wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
            if (levelParam != null)
            {
                var levelId = levelParam.AsElementId();
                var level = Wall.Document.GetElement(levelId) as Level;
                return level?.Name ?? "Unknown";
            }
            return "Unknown";
        }

        public override string ToString()
        {
            var wallType = Wall.WallType?.Name ?? "Unknown";
            var length = "";
            if (Wall.Location is LocationCurve locationCurve)
            {
                length = $" ({locationCurve.Curve.Length:F1}')";
            }
            
            var levelName = GetLevel();
            return $"{wallType}{length} [{levelName}]";
        }
    }
    #endregion
}
