using System;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for handling progress tracking and UI updates
    /// </summary>
    public class ProgressService
    {
        private readonly ILogger _logger;
        private ProgressWindow _progressWindow;
        private DateTime _analysisStartTime;
        private DateTime _stageStartTime;

        public ProgressService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Show progress to the user with visible progress window
        /// </summary>
        public void ShowProgress(string title, string message, int stepCurrent, int stepTotal, int overallCurrent, int overallTotal)
        {
            try
            {
                // Show progress window
                if (_progressWindow == null)
                {
                    _progressWindow = new ProgressWindow();
                    _progressWindow.Show();
                }
                
                // Update progress window
                _progressWindow.UpdateProgress(title, message, stepCurrent, stepTotal, overallCurrent, overallTotal);
                
                // Process UI events to keep the window responsive
                System.Windows.Forms.Application.DoEvents();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Error showing progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Start timing a stage
        /// </summary>
        public void StartStage(string stageName, Action<string> writeToLog)
        {
            _stageStartTime = DateTime.Now;
            writeToLog?.Invoke("");
            writeToLog?.Invoke($"=== STARTING STAGE: {stageName} ===");
            writeToLog?.Invoke($"Start time: {_stageStartTime:HH:mm:ss}");
        }

        /// <summary>
        /// End timing a stage and log the duration
        /// </summary>
        public void EndStage(string stageName, Action<string> writeToLog)
        {
            var stageEndTime = DateTime.Now;
            var stageDuration = stageEndTime - _stageStartTime;
            writeToLog?.Invoke($"=== COMPLETED STAGE: {stageName} ===");
            writeToLog?.Invoke($"End time: {stageEndTime:HH:mm:ss}");
            writeToLog?.Invoke($"Duration: {stageDuration:mm\\:ss}");
            writeToLog?.Invoke("");
        }

        /// <summary>
        /// Start the overall analysis timing
        /// </summary>
        public void StartAnalysis(Action<string> writeToLog)
        {
            _analysisStartTime = DateTime.Now;
            writeToLog?.Invoke($"=== ROOM COLLISION ANALYSIS STARTED ===");
            writeToLog?.Invoke($"Analysis start time: {_analysisStartTime:HH:mm:ss}");
            writeToLog?.Invoke("");
        }

        /// <summary>
        /// End the overall analysis and log total time
        /// </summary>
        public void EndAnalysis(Action<string> writeToLog)
        {
            var totalAnalysisTime = DateTime.Now - _analysisStartTime;
            writeToLog?.Invoke($"=== ANALYSIS COMPLETED ===");
            writeToLog?.Invoke($"Total analysis time: {totalAnalysisTime:mm\\:ss}");
            writeToLog?.Invoke("");
        }

        /// <summary>
        /// Close the progress window
        /// </summary>
        public void CloseProgressWindow()
        {
            if (_progressWindow != null)
            {
                _progressWindow.Close();
                _progressWindow = null;
            }
        }
    }
}
