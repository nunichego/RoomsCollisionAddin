using System;
using System.IO;
using System.Windows.Forms;

namespace RoomsManagerAddin.Infrastructure.Logging
{
    /// <summary>
    /// Service for handling debug logging to files
    /// </summary>
    /// <remarks>
    /// Provides file-based logging capabilities with timestamp support.
    /// Creates log files with SaveFileDialog or falls back to desktop/temp locations.
    /// Thread-safe for single-threaded Revit API usage.
    /// </remarks>
    public class LoggingService : ILoggingService
    {
        private string _debugLogPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class
        /// </summary>
        public LoggingService()
        {
        }

        /// <summary>
        /// Initialize debug logging to a text file with user-selected path
        /// </summary>
        /// <param name="ownerWindowHandle">Optional window handle for modal SaveFileDialog</param>
        /// <returns>The full path to the created log file, or null if initialization failed</returns>
        /// <remarks>
        /// <para>Shows a SaveFileDialog to let user choose log file location.</para>
        /// <para>Falls back to Desktop if dialog fails, then to Temp folder if Desktop fails.</para>
        /// <para>Creates log file with header containing timestamp and debugging information.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var loggingService = new LoggingService();
        /// var logPath = loggingService.InitializeDebugLogging(revitWindowHandle);
        /// if (logPath != null)
        /// {
        ///     Console.WriteLine($"Log file created at: {logPath}");
        /// }
        /// </code>
        /// </example>
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
        /// Write a message to the debug log file with timestamp
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        /// <remarks>
        /// Automatically prepends timestamp in HH:mm:ss.fff format.
        /// Silently ignores errors to prevent logging from breaking analysis.
        /// </remarks>
        /// <example>
        /// <code>
        /// loggingService.WriteToDebugLog("Analysis started");
        /// // Output: [14:23:45.123] Analysis started
        /// </code>
        /// </example>
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
        /// Get the current debug log file path
        /// </summary>
        /// <returns>The full path to the active log file, or null if logging not initialized</returns>
        public string GetDebugLogPath()
        {
            return _debugLogPath;
        }

        /// <summary>
        /// Write to log (alias for <see cref="WriteToDebugLog"/> for compatibility)
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(string message)
        {
            WriteToDebugLog(message);
        }

        /// <summary>
        /// Log an informational message with [INFO] prefix
        /// </summary>
        /// <param name="message">The informational message to log</param>
        /// <example>
        /// <code>
        /// loggingService.LogInfo("Processing started");
        /// // Output: [14:23:45.123] [INFO] Processing started
        /// </code>
        /// </example>
        public void LogInfo(string message)
        {
            WriteToDebugLog($"[INFO] {message}");
        }

        /// <summary>
        /// Log an error message with [ERROR] prefix
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <example>
        /// <code>
        /// loggingService.LogError("Failed to process room");
        /// // Output: [14:23:45.123] [ERROR] Failed to process room
        /// </code>
        /// </example>
        public void LogError(string message)
        {
            WriteToDebugLog($"[ERROR] {message}");
        }

        /// <summary>
        /// Log a warning message with [WARNING] prefix
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <example>
        /// <code>
        /// loggingService.LogWarning("Room has no area");
        /// // Output: [14:23:45.123] [WARNING] Room has no area
        /// </code>
        /// </example>
        public void LogWarning(string message)
        {
            WriteToDebugLog($"[WARNING] {message}");
        }
    }

    /// <summary>
    /// Wrapper class to use native window handles as WinForms dialog owners
    /// </summary>
    /// <remarks>
    /// Enables proper modal dialog behavior when showing WinForms dialogs from Revit.
    /// Prevents dialogs from appearing behind Revit window.
    /// </remarks>
    public class WindowWrapper : IWin32Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowWrapper"/> class
        /// </summary>
        /// <param name="handle">The native window handle (HWND) to wrap</param>
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Gets the native window handle
        /// </summary>
        public IntPtr Handle { get; private set; }
    }
}
