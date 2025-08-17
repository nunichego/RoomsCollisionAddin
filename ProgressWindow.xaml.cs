using System;
using System.Windows;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private DateTime startTime;
        private System.Windows.Threading.DispatcherTimer timer;

        public ProgressWindow()
        {
            InitializeComponent();
            startTime = DateTime.Now;
            
            // Start timer to update elapsed time
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - startTime;
            TimeTextBlock.Text = $"Elapsed: {elapsed:mm\\:ss}";
        }

        public void UpdateProgress(string stage, string detail, int stepCurrent, int stepTotal, int overallCurrent, int overallTotal)
        {
            var stepPercentage = (int)((double)stepCurrent / stepTotal * 100);
            var overallPercentage = (int)((double)overallCurrent / overallTotal * 100);
            
            StageTextBlock.Text = stage;
            DetailTextBlock.Text = detail;
            StepProgressTextBlock.Text = $"Step Progress: {stepPercentage}%";
            StepProgressBar.Value = stepPercentage;
            OverallProgressTextBlock.Text = $"Overall Progress: {overallPercentage}%";
            OverallProgressBar.Value = overallPercentage;
        }

        public void UpdateTime(string timeInfo)
        {
            TimeTextBlock.Text = timeInfo;
        }

        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            base.OnClosed(e);
        }
    }
}
