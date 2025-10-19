using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Application.Controllers;
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfLabel = System.Windows.Controls.Label;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfBorder = System.Windows.Controls.Border;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColor = System.Windows.Media.Color;

namespace RoomsManagerAddin.Presentation.Controls
{
    public class FilterRulesPanel : UserControl
    {
        private readonly RoomWallAnalysisController _controller;
        private readonly bool _showOuterChrome;
        private RoomFilterConfiguration _currentFilter;
        private WpfStackPanel _rulesContainer;
        private WpfBorder _sectionContainerRef;
        private WpfBorder _mainBorderRef;
        private List<ParameterInfo> _availableParameters;

        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public FilterRulesPanel(RoomWallAnalysisController controller)
            : this(controller, true) { }

        public FilterRulesPanel(RoomWallAnalysisController controller, bool showOuterChrome)
        {
            _controller = controller;
            _showOuterChrome = showOuterChrome;
            _currentFilter = controller.CreateFilterConfiguration("Room Filter");
            _availableParameters = controller.GetAvailableRoomParameters();
            
            InitializePanel();
            BuildInitialUI();
        }

        private void InitializePanel()
        {
            Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)); // Pure white background
            Padding = new Thickness(20);
        }

        private void BuildInitialUI()
        {
            var mainStackPanel = new WpfStackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0)
            };

            var sectionContainer = new WpfBorder
            {
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(229, 229, 229)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(252, 252, 252)),
                Margin = new Thickness(0)
            };
            _sectionContainerRef = sectionContainer;

            var sectionPanel = new WpfStackPanel { Orientation = Orientation.Vertical };

            if (_showOuterChrome)
            {
                // Section header - Windows 11 style
                var sectionHeader = new WpfBorder
                {
                    Background = new WpfSolidColorBrush(WpfColor.FromRgb(248, 248, 248)),
                    BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(240, 240, 240)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(16, 12, 16, 12)
                };

                var titleLabel = new WpfLabel
                {
                    Content = "Filter Rules",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0),
                    Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(32, 32, 32)),
                    Padding = new Thickness(0)
                };

                sectionHeader.Child = titleLabel;
                sectionPanel.Children.Add(sectionHeader);
            }

            // Create the main Filter Rules container 
            var filterRulesContainer = CreateMainFilterRulesContainer();
            sectionPanel.Children.Add(filterRulesContainer);
            
            sectionContainer.Child = sectionPanel;
            if (_showOuterChrome)
            {
                // Container group for filter rules (matches draft style "Rooms | Filter Elements")
                var groupContainer = new GroupBox
                {
                    Header = "Rooms | Filter Elements",
                    Margin = new Thickness(0, 0, 0, 16),
                    Padding = new Thickness(0),
                    BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255))
                };
                groupContainer.Content = sectionContainer;
                mainStackPanel.Children.Add(groupContainer);
            }
            else
            {
                mainStackPanel.Children.Add(sectionContainer);
            }

            // Remove dev/test button in final UI

            Content = mainStackPanel;
        }

        private FrameworkElement CreateMainFilterRulesContainer()
        {
            // This is the main rule container - Windows 11 style
            var mainBorder = new WpfBorder
            {
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(200, 200, 200)), // Clean neutral border
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4), // Minimal rounded corners
                Padding = new Thickness(0),
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)), // Pure white background
                Margin = new Thickness(20, 12, 12, 12) // Left margin spacing
            };
            _mainBorderRef = mainBorder;

            var mainPanel = new WpfStackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Header section only when rendering internal chrome
            if (_showOuterChrome)
            {
                var headerContainer = new WpfBorder
                {
                    Padding = new Thickness(8, 8, 10, 8),
                    Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255))
                };

                var mainHeader = new WpfStackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0)
                };

                // Main logical operator - styled like HTML .rulemode
                var mainOperatorCombo = CreateStyledComboBox(200, true);
                mainOperatorCombo.Margin = new Thickness(0, 0, 8, 0);

                mainOperatorCombo.Items.Add("AND (All rules must be true)");
                mainOperatorCombo.Items.Add("OR (Any rule may be true)");
                mainOperatorCombo.SelectedIndex = _currentFilter.RootFilterSet.Operator == LogicalOperator.And ? 0 : 1;
                mainOperatorCombo.SelectionChanged += (s, e) =>
                {
                    _currentFilter.RootFilterSet.Operator = mainOperatorCombo.SelectedIndex == 0 ? LogicalOperator.And : LogicalOperator.Or;
                    UpdateBorderColor(mainBorder, _currentFilter.RootFilterSet.Operator);
                    OnFilterChanged();
                };

                mainHeader.Children.Add(mainOperatorCombo);

                // Toolbar with buttons - matches HTML .toolbar
                var toolbarPanel = new WpfStackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                // Add Rule button
                var addRuleButton = CreateStyledButton("Add Rule");
                _rulesContainer = new WpfStackPanel { Orientation = Orientation.Vertical };
                addRuleButton.Click += (s, e) => AddNewRule(_currentFilter.RootFilterSet, _rulesContainer);
                toolbarPanel.Children.Add(addRuleButton);

                // Add Set button
                var addSetButton = CreateStyledButton("Add Set");
                addSetButton.Margin = new Thickness(8, 0, 0, 0);
                addSetButton.Click += (s, e) => AddNewSet(_currentFilter.RootFilterSet, _rulesContainer);
                toolbarPanel.Children.Add(addSetButton);

                // Delete root Set button (allowed for root as per requirements)
                var deleteRootButton = CreateStyledButton("Delete Set");
                deleteRootButton.Margin = new Thickness(8, 0, 0, 0);
                deleteRootButton.Click += (s, e) => 
                {
                    bool wasDeleted = DeleteRootSet();
                    if (wasDeleted)
                    {
                        System.Diagnostics.Debug.WriteLine("Root filter set cleared successfully");
                    }
                };
                toolbarPanel.Children.Add(deleteRootButton);

                mainHeader.Children.Add(toolbarPanel);
                headerContainer.Child = mainHeader;
                mainPanel.Children.Add(headerContainer);
            }

            // Rules content area - Windows 11 style spacing
            var rulesContent = new WpfBorder
            {
                Padding = new Thickness(20, 16, 20, 16), // Consistent padding all around
                Margin = new Thickness(16, 12, 16, 0) // Balanced margins
            };
            if (_rulesContainer == null)
            {
                _rulesContainer = new WpfStackPanel { Orientation = Orientation.Vertical };
            }
            rulesContent.Child = _rulesContainer;

            mainPanel.Children.Add(rulesContent);

            mainBorder.Child = mainPanel;

            // No default rule; show placeholder if empty (root visible chrome)
            EnsurePlaceholderIfEmpty();

            return mainBorder;
        }

        private void EnsurePlaceholderIfEmpty()
        {
            try
            {
                if (_currentFilter.RootFilterSet == null)
                {
                    _currentFilter.RootFilterSet = new FilterSet { Operator = LogicalOperator.And };
                }
                if (_currentFilter.RootFilterSet.Items.Any())
                    return;
                _rulesContainer.Children.Clear();
                var placeholder = new WpfStackPanel { Orientation = Orientation.Horizontal };
                var addRuleBtn = CreateStyledButton("Add Rule");
                addRuleBtn.Click += (s, e) => AddNewRule(_currentFilter.RootFilterSet, _rulesContainer);
                placeholder.Children.Add(addRuleBtn);
                _rulesContainer.Children.Add(placeholder);
            }
            catch (NullReferenceException ex)
            {
                System.Diagnostics.Debug.WriteLine($"NullReference in EnsurePlaceholderIfEmpty: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"InvalidOperation in EnsurePlaceholderIfEmpty: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void RemovePlaceholder()
        {
            try
            {
                if (_rulesContainer.Children.Count == 1 && _rulesContainer.Children[0] is WpfStackPanel sp && sp.Children.OfType<WpfButton>().Any())
                {
                    _rulesContainer.Children.Clear();
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"InvalidOperation in RemovePlaceholder: {ex.Message}\n{ex.StackTrace}");
                // Safe to suppress this specific exception as it's a UI state issue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in RemovePlaceholder: {ex.Message}\n{ex.StackTrace}");
                // Log but allow to continue as this is not critical for functionality
            }
        }

        public bool DeleteRootSet()
        {
            if (_currentFilter?.RootFilterSet != null && _currentFilter.RootFilterSet.Items.Count > 0)
            {
                _currentFilter.RootFilterSet.Items.Clear();
                EnsurePlaceholderIfEmpty();
                OnFilterChanged();
                return true;
            }
            return false;
        }

        // Public API for external header controls
        public void SetMainOperator(LogicalOperator op)
        {
            _currentFilter.RootFilterSet.Operator = op;
            if (_mainBorderRef != null)
            {
                UpdateBorderColor(_mainBorderRef, op);
            }
            OnFilterChanged();
        }

        public LogicalOperator GetMainOperator()
        {
            return _currentFilter.RootFilterSet.Operator;
        }

        public void AddRule()
        {
            if (_rulesContainer == null)
            {
                _rulesContainer = new WpfStackPanel { Orientation = Orientation.Vertical };
            }
            AddNewRule(_currentFilter.RootFilterSet, _rulesContainer);
        }

        public void AddSet()
        {
            if (_rulesContainer == null)
            {
                _rulesContainer = new WpfStackPanel { Orientation = Orientation.Vertical };
            }
            AddNewSet(_currentFilter.RootFilterSet, _rulesContainer);
        }

        private void UpdateBorderColor(WpfBorder border, LogicalOperator logicalOperator)
        {
            if (logicalOperator == LogicalOperator.And)
            {
                // Green border only for AND - no background
                border.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(16, 124, 16)); // Modern green
                border.Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)); // White background
            }
            else
            {
                // Blue border only for OR - no background
                border.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)); // Windows 11 blue
                border.Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)); // White background
            }
        }

        private void AddNewRule(FilterSet parentSet, WpfStackPanel container)
        {
            RemovePlaceholder();
            var rule = new RoomFilterRule
            {
                Parameter = _availableParameters.FirstOrDefault(),
                Operator = FilterOperator.Equals,
                Value = ""
            };

            if (rule.Parameter != null)
            {
                parentSet.Items.Add(rule);
                var ruleUI = CreateRuleUI(rule, parentSet, container);
                container.Children.Add(ruleUI);
                OnFilterChanged();
            }
        }

        private void AddNewSet(FilterSet parentSet, WpfStackPanel container)
        {
            RemovePlaceholder();
            var newSet = new FilterSet
            {
                Operator = LogicalOperator.And,
                Items = new List<IFilterItem>()
            };

            parentSet.Items.Add(newSet);
            var setUI = CreateSetUI(newSet, parentSet, container);
            container.Children.Add(setUI);
            OnFilterChanged();
        }

        private FrameworkElement CreateRuleUI(RoomFilterRule rule, FilterSet parentSet, WpfStackPanel parentContainer)
        {
            var ruleContainer = new WpfStackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Top row: Category and Parameter (matches HTML .row)
            var topRowGrid = new WpfGrid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            topRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Category (always "Rooms" - fixed)
            var categoryCombo = CreateStyledComboBox(0);
            categoryCombo.Items.Add("Rooms");
            categoryCombo.SelectedIndex = 0;
            categoryCombo.IsEnabled = false; // Read-only since we're only doing rooms
            categoryCombo.Margin = new Thickness(0, 0, 10, 0);
            topRowGrid.Children.Add(categoryCombo);
            WpfGrid.SetColumn(categoryCombo, 0);

            // Parameter dropdown
            var parameterCombo = CreateStyledComboBox(0);
            foreach (var param in _availableParameters)
            {
                parameterCombo.Items.Add(param.Name);
            }
            parameterCombo.SelectedItem = rule.Parameter?.Name;
            topRowGrid.Children.Add(parameterCombo);
            WpfGrid.SetColumn(parameterCombo, 1);

            ruleContainer.Children.Add(topRowGrid);

            // Bottom row: Operator, Value, and Delete button (matches HTML .row-bottom)
            var bottomRowGrid = new WpfGrid();
            bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left part: Operator and Value in a sub-grid
            var operatorValueGrid = new WpfGrid();
            operatorValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            operatorValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Operator dropdown
            var operatorCombo = CreateStyledComboBox(0);
            operatorCombo.Margin = new Thickness(0, 0, 10, 0);
            UpdateOperatorCombo(rule, operatorCombo);
            operatorValueGrid.Children.Add(operatorCombo);
            WpfGrid.SetColumn(operatorCombo, 0);

            // Value input
            var valueInput = CreateStyledTextBox(0);
            valueInput.Text = rule.Value ?? "";
            operatorValueGrid.Children.Add(valueInput);
            WpfGrid.SetColumn(valueInput, 1);

            bottomRowGrid.Children.Add(operatorValueGrid);
            WpfGrid.SetColumn(operatorValueGrid, 0);

            // Delete button
            var deleteButton = CreateDeleteButton();
            deleteButton.Margin = new Thickness(10, 0, 0, 0);
            bottomRowGrid.Children.Add(deleteButton);
            WpfGrid.SetColumn(deleteButton, 1);

            ruleContainer.Children.Add(bottomRowGrid);

            // Event handlers
            parameterCombo.SelectionChanged += (s, e) =>
            {
                if (parameterCombo.SelectedItem != null)
                {
                    rule.Parameter = _availableParameters.FirstOrDefault(p => p.Name == parameterCombo.SelectedItem.ToString());
                    UpdateOperatorCombo(rule, operatorCombo);
                    OnFilterChanged();
                }
            };

            operatorCombo.SelectionChanged += (s, e) =>
            {
                if (operatorCombo.SelectedItem != null)
                {
                    var displayText = operatorCombo.SelectedItem.ToString();
                    var op = GetOperatorFromDisplayText(displayText);
                    rule.Operator = op;
                    OnFilterChanged();
                }
            };

            valueInput.TextChanged += (s, e) =>
            {
                rule.Value = valueInput.Text;
                OnFilterChanged();
            };

            deleteButton.Click += (s, e) =>
            {
                parentSet.Items.Remove(rule);
                parentContainer.Children.Remove(ruleContainer);
                OnFilterChanged();
            };

            return ruleContainer;
        }

        private FrameworkElement CreateSetUI(FilterSet filterSet, FilterSet parentSet, WpfStackPanel parentContainer)
        {
            var setBorder = new WpfBorder
            {
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(8)
            };
            
            // Set initial border color based on operator
            UpdateBorderColor(setBorder, filterSet.Operator);

            var setPanel = new WpfStackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Set header with AND/OR and buttons
            var setHeader = new WpfStackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Set logical operator
            var setOperatorCombo = CreateStyledComboBox(180, true);
            setOperatorCombo.Items.Add("OR (Any rule may be true)");
            setOperatorCombo.Items.Add("AND (All rules must be true)");
            setOperatorCombo.SelectedIndex = filterSet.Operator == LogicalOperator.Or ? 0 : 1;
            setOperatorCombo.SelectionChanged += (s, e) =>
            {
                filterSet.Operator = setOperatorCombo.SelectedIndex == 0 ? LogicalOperator.Or : LogicalOperator.And;
                UpdateBorderColor(setBorder, filterSet.Operator);
                OnFilterChanged();
            };
            setHeader.Children.Add(setOperatorCombo);

            // Add Rule to Set button
            var addRuleToSetButton = CreateStyledButton("Add Rule");
            var setRulesContainer = new WpfStackPanel { Orientation = Orientation.Vertical };
            addRuleToSetButton.Click += (s, e) => AddNewRule(filterSet, setRulesContainer);
            addRuleToSetButton.Margin = new Thickness(8, 0, 0, 0);
            setHeader.Children.Add(addRuleToSetButton);

            // Add Set to Set button (not implemented - only 1 level deep as requested)
            var addSetToSetButton = CreateStyledButton("Add Set");
            addSetToSetButton.IsEnabled = false; // Disabled for 1 level depth limit
            addSetToSetButton.ToolTip = "Only one level of nesting allowed";
            addSetToSetButton.Margin = new Thickness(8, 0, 0, 0);
            setHeader.Children.Add(addSetToSetButton);

            // Delete Set button
            var deleteSetButton = CreateDeleteButton();
            deleteSetButton.Click += (s, e) =>
            {
                parentSet.Items.Remove(filterSet);
                parentContainer.Children.Remove(setBorder);
                OnFilterChanged();
            };
            setHeader.Children.Add(deleteSetButton);

            setPanel.Children.Add(setHeader);
            setPanel.Children.Add(setRulesContainer);

            setBorder.Child = setPanel;

            // Add initial rule to set if empty
            if (!filterSet.Items.Any())
            {
                AddNewRule(filterSet, setRulesContainer);
            }

            return setBorder;
        }

        private WpfComboBox CreateStyledComboBox(int width, bool isRuleMode = false)
        {
            var combo = new WpfComboBox
            {
                Height = 32, // Windows 11 standard height
                FontSize = 13, // Better readability
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)),
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(200, 200, 200)), // Neutral border
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 28, 6), // More padding for modern feel
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            // Only set width if specified (greater than 0)
            if (width > 0)
            {
                combo.Width = width;
                combo.HorizontalAlignment = HorizontalAlignment.Left;
            }

            if (isRuleMode)
            {
                // Special styling for rule mode combo (AND/OR) - Windows 11 style
                combo.Background = new WpfSolidColorBrush(WpfColor.FromRgb(245, 245, 245)); // Light neutral background
                combo.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(32, 32, 32)); // Dark text
                combo.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(180, 180, 180)); // Neutral border
                combo.FontWeight = FontWeights.SemiBold;
                combo.Padding = new Thickness(8, 4, 14, 4); // Better vertical centering
            }

            return combo;
        }

        private WpfTextBox CreateStyledTextBox(int width)
        {
            var textBox = new WpfTextBox
            {
                Height = 32, // Windows 11 standard height
                FontSize = 13, // Better readability
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)),
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(200, 200, 200)), // Neutral border
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 6, 12, 6), // More padding for modern feel
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            // Only set width if specified (greater than 0)
            if (width > 0)
            {
                textBox.Width = width;
                textBox.HorizontalAlignment = HorizontalAlignment.Left;
            }

            return textBox;
        }

        private WpfButton CreateStyledButton(string text)
        {
            var button = new WpfButton
            {
                Content = text,
                Height = 32, // Windows 11 standard height
                MinWidth = 80, // More generous minimum width
                FontSize = 13, // Better readability
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(251, 251, 251)), // Very light background
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(200, 200, 200)), // Neutral border
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 6, 16, 6), // More padding for modern feel
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // Windows 11 hover effect
            button.MouseEnter += (s, e) => button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(240, 240, 240));
            button.MouseLeave += (s, e) => button.Background = new WpfSolidColorBrush(WpfColor.FromRgb(251, 251, 251));

            return button;
        }

        private WpfButton CreateDeleteButton()
        {
            var deleteButton = new WpfButton
            {
                Content = "×", // Modern × character
                Width = 32, // Standard Windows 11 size
                Height = 32,
                FontSize = 14,
                FontWeight = FontWeights.Normal, // Less bold for modern look
                Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255)),
                Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(162, 162, 162)), // Subtle gray
                BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(200, 200, 200)), // Neutral border
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // Windows 11 delete button hover effect
            deleteButton.MouseEnter += (s, e) => 
            {
                deleteButton.Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 242, 242));
                deleteButton.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(196, 43, 28)); // Red on hover
            };
            deleteButton.MouseLeave += (s, e) => 
            {
                deleteButton.Background = new WpfSolidColorBrush(WpfColor.FromRgb(255, 255, 255));
                deleteButton.Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(162, 162, 162));
            };

            return deleteButton;
        }


        private void UpdateOperatorCombo(RoomFilterRule rule, WpfComboBox operatorCombo)
        {
            operatorCombo.Items.Clear();
            
            if (rule.Parameter != null)
            {
                var operators = rule.Parameter.GetAvailableOperators();
                foreach (var op in operators)
                {
                    var displayText = GetOperatorDisplayText(op);
                    operatorCombo.Items.Add(displayText);
                }
                
                var currentDisplay = GetOperatorDisplayText(rule.Operator);
                operatorCombo.SelectedItem = currentDisplay;
            }
        }

        private string GetOperatorDisplayText(FilterOperator op)
        {
            switch (op)
            {
                case FilterOperator.Equals:
                    return "equals";
                case FilterOperator.NotEquals:
                    return "does not equal";
                case FilterOperator.Contains:
                    return "contains";
                case FilterOperator.NotContains:
                    return "does not contain";
                case FilterOperator.BeginsWith:
                    return "begins with";
                case FilterOperator.EndsWith:
                    return "ends with";
                case FilterOperator.GreaterThan:
                    return "is greater than";
                case FilterOperator.LessThan:
                    return "is less than";
                case FilterOperator.GreaterThanOrEqual:
                    return "is greater than or equal to";
                case FilterOperator.LessThanOrEqual:
                    return "is less than or equal to";
                case FilterOperator.HasValue:
                    return "has a value";
                case FilterOperator.HasNoValue:
                    return "has no value";
                default:
                    return op.ToString();
            }
        }

        private FilterOperator GetOperatorFromDisplayText(string displayText)
        {
            switch (displayText)
            {
                case "equals":
                    return FilterOperator.Equals;
                case "does not equal":
                    return FilterOperator.NotEquals;
                case "contains":
                    return FilterOperator.Contains;
                case "does not contain":
                    return FilterOperator.NotContains;
                case "begins with":
                    return FilterOperator.BeginsWith;
                case "ends with":
                    return FilterOperator.EndsWith;
                case "is greater than":
                    return FilterOperator.GreaterThan;
                case "is less than":
                    return FilterOperator.LessThan;
                case "is greater than or equal to":
                    return FilterOperator.GreaterThanOrEqual;
                case "is less than or equal to":
                    return FilterOperator.LessThanOrEqual;
                case "has a value":
                    return FilterOperator.HasValue;
                case "has no value":
                    return FilterOperator.HasNoValue;
                default:
                    return FilterOperator.Equals;
            }
        }

        public RoomFilterConfiguration GetCurrentFilter()
        {
            return _currentFilter;
        }

        private void OnFilterChanged()
        {
            FilterChanged?.Invoke(this, new FilterChangedEventArgs(_currentFilter));
        }
    }

    public class FilterChangedEventArgs : EventArgs
    {
        public RoomFilterConfiguration FilterConfiguration { get; }

        public FilterChangedEventArgs(RoomFilterConfiguration filterConfiguration)
        {
            FilterConfiguration = filterConfiguration;
        }
    }
}