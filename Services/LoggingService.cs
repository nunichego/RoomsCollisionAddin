using System;
using System.IO;
using System.Windows.Forms;


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
        public string InitializeDebugLogging(IntPtr? ownerWindowHandle = null)
        {
            try
            {
                // Create default path
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var defaultFileName = $"RoomCollisionAnalysis_{timestamp}.txt";
                _debugLogPath = Path.Combine(desktopPath, defaultFileName);
                
                // Try to show save dialog with proper owner
                try
                {
                    var saveDialog = new System.Windows.Forms.SaveFileDialog
                    {
                        Title = "Save Room Collision Analysis Log",
                        Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                        FileName = defaultFileName,
                        DefaultExt = ".txt",
                        InitialDirectory = desktopPath,
                        AddExtension = true
                    };

                    System.Windows.Forms.DialogResult result;
                    
                    if (ownerWindowHandle.HasValue && ownerWindowHandle.Value != IntPtr.Zero)
                    {
                        // Use Revit window as owner for proper modal behavior
                        var owner = new WindowWrapper(ownerWindowHandle.Value);
                        result = saveDialog.ShowDialog(owner);
                    }
                    else
                    {
                        // No owner - try regular dialog
                        result = saveDialog.ShowDialog();
                    }

                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(saveDialog.FileName))
                    {
                        _debugLogPath = saveDialog.FileName;
                    }
                }
                catch (Exception ex)
                {
                    // Could not show save dialog, using default path
                    WriteToDebugLog($"Save dialog failed: {ex.Message}, using default path");
                }

                // Create the log file with header
                var header = $"=== ROOM COLLISION ANALYSIS LOG ===\n";
                header += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                header += $"Log File: {_debugLogPath}\n";
                header += $"DEBUGGING: Room Boundary API Analysis\n";
                header += $"=====================================\n\n";

                File.WriteAllText(_debugLogPath, header);
                
                // Test write to ensure file is working
                WriteToDebugLog("Log initialized successfully");

                return _debugLogPath;
            }
            catch (Exception ex)
            {
                // Try backup location if desktop fails
                try
                {
                    var tempPath = Path.GetTempPath();
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _debugLogPath = Path.Combine(tempPath, $"RoomCollisionAnalysis_{timestamp}.txt");
                    
                    var header = $"=== ROOM COLLISION ANALYSIS LOG (TEMP LOCATION) ===\n";
                    header += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                    header += $"Log File: {_debugLogPath}\n";
                    header += $"Note: Using temp location due to desktop write error: {ex.Message}\n";
                    header += $"=====================================\n\n";

                    File.WriteAllText(_debugLogPath, header);
                    WriteToDebugLog("Log initialized in temp location");
                    return _debugLogPath;
                }
                catch
                {
                    return null;
                }
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

        /// <summary>
        /// Log an info message
        /// </summary>
        public void LogInfo(string message)
        {
            WriteToDebugLog($"[INFO] {message}");
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void LogError(string message)
        {
            WriteToDebugLog($"[ERROR] {message}");
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void LogWarning(string message)
        {
            WriteToDebugLog($"[WARNING] {message}");
        }
    }

    /// <summary>
    /// Wrapper class to use native window handles as dialog owners
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; private set; }
    }
}
