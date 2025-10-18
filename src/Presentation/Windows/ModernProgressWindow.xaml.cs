using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Presentation.Windows
{
    /// <summary>
    /// Modern XAML-based progress window with clean design and data binding
    /// </summary>
    public partial class ModernProgressWindow : Window
    {
        private ProgressInfo _progressInfo;
        private bool _canClose = false;

        public ModernProgressWindow()
        {
            InitializeComponent();
            
            // Initialize with default progress info
            _progressInfo = new ProgressInfo
            {
                Title = "RoomDataSync Analysis",
                Stage = "Initializing...",
                Detail = "Preparing analysis...",
                StepProgress = 0.0,
                OverallProgress = 0.0
            };

            DataContext = _progressInfo;

            // Make window draggable
            MouseLeftButtonDown += (sender, e) => DragMove();
        }

        /// <summary>
        /// Update progress information
        /// </summary>
        public void UpdateProgress(ProgressInfo progressInfo)
        {
            if (progressInfo == null) return;

            // Update all properties on the UI thread
            Dispatcher.Invoke(() =>
            {
                _progressInfo.Title = progressInfo.Title;
                _progressInfo.Stage = progressInfo.Stage;
                _progressInfo.Detail = progressInfo.Detail;
                _progressInfo.StepProgress = progressInfo.StepProgress;
                _progressInfo.OverallProgress = progressInfo.OverallProgress;
                _progressInfo.IsIndeterminate = progressInfo.IsIndeterminate;
            });
        }

        /// <summary>
        /// Allow the window to be closed (called when operation completes)
        /// </summary>
        public void AllowClose()
        {
            _canClose = true;
        }

        /// <summary>
        /// Close the progress window
        /// </summary>
        public new void Close()
        {
            _canClose = true;
            base.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_canClose)
            {
                Close();
            }
            else
            {
                // Optional: Show a message that the operation is still running
                MessageBox.Show("Analysis is still in progress. Please wait for it to complete.", 
                              "Operation Running", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                MessageBox.Show("Analysis is still in progress. Please wait for it to complete.", 
                              "Operation Running", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Converter for progress bar width calculation (if needed for custom progress bar)
    /// </summary>
    public class ProgressToWidthConverter : IValueConverter
    {
        public static readonly ProgressToWidthConverter Instance = new ProgressToWidthConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                return new GridLength(progress, GridUnitType.Star);
            }
            return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}