using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for handling debug logging to files
    /// </summary>
    public class LoggingService
    {
        private readonly ILogger _logger;
        private string _debugLogPath;
        private LogVerbosity _verbosity = LogVerbosity.Normal;

        public LoggingService(ILogger logger)
        {
            _logger = logger;
        }

        public enum LogVerbosity
        {
            Minimal,
            Normal,
            Verbose
        }

        public void SetVerbosity(LogVerbosity verbosity)
        {
            _verbosity = verbosity;
        }

        /// <summary>
        /// Initialize debug logging to a text file
        /// </summary>
        public string InitializeDebugLogging()
        {
            try
            {
                // Create default path first
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _debugLogPath = Path.Combine(desktopPath, $"RoomCollisionAnalysis_{timestamp}.txt");
                
                // Try to show save dialog
                try
                {
                    var saveDialog = new System.Windows.Forms.SaveFileDialog
                    {
                        Title = "Save Room Collision Analysis Log",
                        Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                        FileName = $"RoomCollisionAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                        DefaultExt = ".txt",
                        InitialDirectory = desktopPath
                    };

                    if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _debugLogPath = saveDialog.FileName;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Could not show save dialog: {ex.Message}. Using default path.");
                }

                // Create the log file with header
                var header = $"=== ROOM COLLISION ANALYSIS LOG ===\n";
                header += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                header += $"Log File: {_debugLogPath}\n";
                header += $"=====================================\n\n";

                File.WriteAllText(_debugLogPath, header);
                _logger?.LogInformation($"Debug logging initialized: {_debugLogPath}");

                return _debugLogPath;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing debug logging");
                return null;
            }
        }

        /// <summary>
        /// Write a message to the debug log file
        /// </summary>
        public void WriteToDebugLog(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(_debugLogPath))
                {
                    // Filter noisy lines unless Verbose
                    if (_verbosity != LogVerbosity.Verbose)
                    {
                        if (IsNoisyMessage(message))
                        {
                            return;
                        }
                    }

                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}\n";
                    File.AppendAllText(_debugLogPath, logMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error writing to debug log");
            }
        }

        private bool IsNoisyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            // Skip overly verbose per-element debug lines in Normal/Minimal modes
            if (message.StartsWith("Regular wall:") ||
                message.Contains("Getting solid for regular wall") ||
                message.Contains("Standard solid:") ||
                message.Contains("Instance solid:") ||
                message.Contains("Location-based solid") ||
                message.Contains("Processing curtain wall:") ||
                message.Contains("Creating curtain wall solid") ||
                message.Contains("Curve type:") ||
                message.Contains("Wall height:") ||
                message.Contains("Creating rectangular profile") ||
                message.Contains("Profile points:") ||
                message.Contains("Profile created successfully") ||
                message.Contains("Created curtain wall solid") ||
                message.Contains("Updated Room Filter Tag") ||
                (message.Contains("Wall ") && message.Contains(": Updated Filter Tag")) ||
                message.Contains("*** DEBUG"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the current debug log path
        /// </summary>
        public string GetDebugLogPath()
        {
            return _debugLogPath;
        }
    }
}
