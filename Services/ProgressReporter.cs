using System;
using System.Windows;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Modern progress reporting service with type-safe design
    /// Replaces the old Action&lt;string, string, int, int, int, int&gt; callback system
    /// </summary>
    public class ProgressReporter
    {
        private readonly Action<ProgressInfo> _progressCallback;

        /// <summary>
        /// Create progress reporter with callback
        /// </summary>
        public ProgressReporter(Action<ProgressInfo> progressCallback)
        {
            _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
        }

        /// <summary>
        /// Report progress using ProgressInfo object
        /// </summary>
        public void ReportProgress(ProgressInfo progressInfo)
        {
            if (progressInfo == null) return;

            try
            {
                // Ensure UI updates happen on the UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        // Already on UI thread
                        _progressCallback(progressInfo);
                    }
                    else
                    {
                        // Marshal to UI thread synchronously
                        Application.Current.Dispatcher.Invoke(() => _progressCallback(progressInfo));
                    }
                }
                else
                {
                    // Fallback - call directly
                    _progressCallback(progressInfo);
                }

                // Allow UI to process messages
                System.Windows.Forms.Application.DoEvents();
            }
            catch (Exception ex)
            {
                // Don't let progress reporting crash the analysis
                System.Diagnostics.Debug.WriteLine($"Progress reporting error: {ex.Message}");
            }
        }

        /// <summary>
        /// Report progress using step-based calculation
        /// </summary>
        public void ReportProgress(string title, string stage, string detail, 
            int stepCurrent, int stepTotal, double overallProgress)
        {
            var progressInfo = ProgressInfo.FromStepCount(title, stage, detail, stepCurrent, stepTotal, overallProgress);
            ReportProgress(progressInfo);
        }

        /// <summary>
        /// Report progress using percentage values
        /// </summary>
        public void ReportProgress(string title, string stage, string detail, 
            double stepProgress, double overallProgress)
        {
            var progressInfo = new ProgressInfo
            {
                Title = title,
                Stage = stage,
                Detail = detail,
                StepProgress = stepProgress,
                OverallProgress = overallProgress
            };
            ReportProgress(progressInfo);
        }

        /// <summary>
        /// Report indeterminate progress
        /// </summary>
        public void ReportIndeterminate(string title, string stage, string detail = null)
        {
            var progressInfo = ProgressInfo.CreateIndeterminate(title, stage, detail);
            ReportProgress(progressInfo);
        }

        /// <summary>
        /// Create a child reporter for sub-operations within a specific overall progress range
        /// </summary>
        public ProgressReporter CreateSubReporter(double overallStart, double overallEnd)
        {
            var range = overallEnd - overallStart;
            
            return new ProgressReporter(subProgress =>
            {
                // Map sub-progress to parent overall progress range
                var mappedOverallProgress = overallStart + (subProgress.OverallProgress * range);
                
                var parentProgress = new ProgressInfo
                {
                    Title = subProgress.Title,
                    Stage = subProgress.Stage,
                    Detail = subProgress.Detail,
                    StepProgress = subProgress.StepProgress,
                    OverallProgress = mappedOverallProgress,
                    IsIndeterminate = subProgress.IsIndeterminate
                };
                
                ReportProgress(parentProgress);
            });
        }
    }

    /// <summary>
    /// No-operation progress reporter for when progress reporting is disabled
    /// </summary>
    public class NullProgressReporter : ProgressReporter
    {
        public NullProgressReporter() : base(_ => { }) { }

        public static readonly NullProgressReporter Instance = new NullProgressReporter();
    }
}