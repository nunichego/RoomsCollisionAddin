using System.Windows;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
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
                TitleTextBlock.Text = title;
            
            if (!string.IsNullOrEmpty(stage))
                StageTextBlock.Text = stage;
            
            if (!string.IsNullOrEmpty(detail))
                DetailTextBlock.Text = detail;
            
            StepProgressTextBlock.Text = $"Step: {stepCurrent}/{stepTotal}";
            StepProgressBar.Maximum = stepTotal;
            StepProgressBar.Value = stepCurrent;
            
            OverallProgressTextBlock.Text = $"Overall: {overallCurrent}/{overallTotal}";
            OverallProgressBar.Maximum = overallTotal;
            OverallProgressBar.Value = overallCurrent;
        }
    }
}
