using System;
using System.IO;


namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for handling debug logging to files
    /// </summary>
    public class LoggingService
    {
        private string _debugLogPath;

        public LoggingService()
        {
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
                                 catch (Exception)
                 {
                     // Could not show save dialog, using default path
                 }

                // Create the log file with header
                var header = $"=== ROOM COLLISION ANALYSIS LOG ===\n";
                header += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                header += $"Log File: {_debugLogPath}\n";
                header += $"=====================================\n\n";

                File.WriteAllText(_debugLogPath, header);

                return _debugLogPath;
            }
                         catch (Exception)
             {
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
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}\n";
                    File.AppendAllText(_debugLogPath, logMessage);
                }
            }
                         catch (Exception)
             {
                 // Error writing to debug log
             }
        }

        /// <summary>
        /// Get the current debug log path
        /// </summary>
        public string GetDebugLogPath()
        {
            return _debugLogPath;
        }

        /// <summary>
        /// Write to log (alias for WriteToDebugLog for compatibility)
        /// </summary>
        public void WriteToLog(string message)
        {
            WriteToDebugLog(message);
        }
    }
}
