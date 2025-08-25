using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Simple WPF progress window for analysis operations
    /// </summary>
    public class ProgressWindow : Window
    {
        private TextBlock titleTextBlock;
        private TextBlock stageTextBlock;
        private TextBlock detailTextBlock;
        private TextBlock stepProgressTextBlock;
        private ProgressBar stepProgressBar;
        private TextBlock overallProgressTextBlock;
        private ProgressBar overallProgressBar;

        public ProgressWindow()
        {
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Title = "RoomDataSync - Analysis Progress";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = SystemColors.ControlBrush;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 9;

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(10);
            
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Stage
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Detail
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Step progress text
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Step progress bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Overall progress text
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Overall progress bar

            // Title
            titleTextBlock = new TextBlock
            {
                Text = "Analysis Progress",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Stage
            stageTextBlock = new TextBlock
            {
                Text = "Initializing...",
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Detail
            detailTextBlock = new TextBlock
            {
                Text = "",
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Step progress
            stepProgressTextBlock = new TextBlock
            {
                Text = "Step: 0/0",
                Margin = new Thickness(0, 0, 0, 2)
            };

            stepProgressBar = new ProgressBar
            {
                Height = 20,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Overall progress
            overallProgressTextBlock = new TextBlock
            {
                Text = "Overall: 0/0",
                Margin = new Thickness(0, 0, 0, 2)
            };

            overallProgressBar = new ProgressBar
            {
                Height = 20
            };

            // Add to grid
            mainGrid.Children.Add(titleTextBlock);
            Grid.SetRow(titleTextBlock, 0);

            mainGrid.Children.Add(stageTextBlock);
            Grid.SetRow(stageTextBlock, 1);

            mainGrid.Children.Add(detailTextBlock);
            Grid.SetRow(detailTextBlock, 2);

            mainGrid.Children.Add(stepProgressTextBlock);
            Grid.SetRow(stepProgressTextBlock, 3);

            mainGrid.Children.Add(stepProgressBar);
            Grid.SetRow(stepProgressBar, 4);

            mainGrid.Children.Add(overallProgressTextBlock);
            Grid.SetRow(overallProgressTextBlock, 5);

            mainGrid.Children.Add(overallProgressBar);
            Grid.SetRow(overallProgressBar, 6);

            Content = mainGrid;
        }

        public void UpdateProgress(string title, string stage, string detail, int stepCurrent, int stepTotal, int overallCurrent, int overallTotal)
        {
            if (Dispatcher.CheckAccess())
            {
                UpdateProgressInternal(title, stage, detail, stepCurrent, stepTotal, overallCurrent, overallTotal);
            }
            else
            {
                Dispatcher.Invoke(() => UpdateProgressInternal(title, stage, detail, stepCurrent, stepTotal, overallCurrent, overallTotal));
            }
        }

        private void UpdateProgressInternal(string title, string stage, string detail, int stepCurrent, int stepTotal, int overallCurrent, int overallTotal)
        {
            if (!string.IsNullOrEmpty(title))
                titleTextBlock.Text = title;
            
            if (!string.IsNullOrEmpty(stage))
                stageTextBlock.Text = stage;
            
            if (!string.IsNullOrEmpty(detail))
                detailTextBlock.Text = detail;
            
            stepProgressTextBlock.Text = $"Step: {stepCurrent}/{stepTotal}";
            stepProgressBar.Maximum = stepTotal;
            stepProgressBar.Value = stepCurrent;
            
            overallProgressTextBlock.Text = $"Overall: {overallCurrent}/{overallTotal}";
            overallProgressBar.Maximum = overallTotal;
            overallProgressBar.Value = overallCurrent;
        }
    }
}
