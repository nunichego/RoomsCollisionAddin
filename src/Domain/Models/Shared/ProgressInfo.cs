using System;
using System.ComponentModel;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// Type-safe progress information for analysis operations
    /// Replaces the error-prone 6-parameter progress callback system
    /// </summary>
    public class ProgressInfo : INotifyPropertyChanged
    {
        private string _title;
        private string _stage;
        private string _detail;
        private double _stepProgress;
        private double _overallProgress;
        private bool _isIndeterminate;

        /// <summary>
        /// Main title of the operation (e.g., "Room-Wall Analysis")
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        /// <summary>
        /// Current stage of operation (e.g., "Processing Rooms", "Updating Parameters")
        /// </summary>
        public string Stage
        {
            get => _stage;
            set
            {
                _stage = value;
                OnPropertyChanged(nameof(Stage));
            }
        }

        /// <summary>
        /// Detailed information about current step (e.g., "Room 001 (15/100)")
        /// </summary>
        public string Detail
        {
            get => _detail;
            set
            {
                _detail = value;
                OnPropertyChanged(nameof(Detail));
            }
        }

        /// <summary>
        /// Progress of current step (0.0 to 1.0)
        /// </summary>
        public double StepProgress
        {
            get => _stepProgress;
            set
            {
                _stepProgress = Math.Max(0.0, Math.Min(1.0, value)); // Clamp to 0-1
                OnPropertyChanged(nameof(StepProgress));
                OnPropertyChanged(nameof(StepProgressPercentage));
            }
        }

        /// <summary>
        /// Overall progress of entire operation (0.0 to 1.0)
        /// </summary>
        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                _overallProgress = Math.Max(0.0, Math.Min(1.0, value)); // Clamp to 0-1
                OnPropertyChanged(nameof(OverallProgress));
                OnPropertyChanged(nameof(OverallProgressPercentage));
            }
        }

        /// <summary>
        /// Whether progress is indeterminate (spinning instead of progress bar)
        /// </summary>
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                _isIndeterminate = value;
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        /// <summary>
        /// Step progress as percentage (0-100) for display
        /// </summary>
        public double StepProgressPercentage => _stepProgress * 100.0;

        /// <summary>
        /// Overall progress as percentage (0-100) for display
        /// </summary>
        public double OverallProgressPercentage => _overallProgress * 100.0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Create progress info with step-based calculation
        /// </summary>
        public static ProgressInfo FromStepCount(string title, string stage, string detail,
            int stepCurrent, int stepTotal, double overallProgress)
        {
            var stepProgress = stepTotal > 0 ? (double)stepCurrent / stepTotal : 0.0;

            return new ProgressInfo
            {
                Title = title ?? "Processing",
                Stage = stage ?? "Working",
                Detail = detail ?? $"{stepCurrent}/{stepTotal}",
                StepProgress = stepProgress,
                OverallProgress = overallProgress
            };
        }

        /// <summary>
        /// Create indeterminate progress info
        /// </summary>
        public static ProgressInfo CreateIndeterminate(string title, string stage, string detail = null)
        {
            return new ProgressInfo
            {
                Title = title ?? "Processing",
                Stage = stage ?? "Working",
                Detail = detail ?? "Please wait...",
                IsIndeterminate = true,
                StepProgress = 0.0,
                OverallProgress = 0.0
            };
        }
    }
}
