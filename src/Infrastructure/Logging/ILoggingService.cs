using System;

namespace RoomsManagerAddin.Infrastructure.Logging
{
    /// <summary>
    /// Service for application logging
    /// </summary>
    /// <remarks>
    /// Provides centralized logging functionality for the add-in.
    /// Supports file-based logging with configurable paths.
    /// </remarks>
    public interface ILoggingService
    {
        /// <summary>Initialize logging with optional file path</summary>
        /// <param name="ownerWindowHandle">Optional window handle for UI parent</param>
        /// <returns>Path to the log file</returns>
        string InitializeDebugLogging(IntPtr? ownerWindowHandle = null);

        /// <summary>Write message to log</summary>
        /// <param name="message">Message to write</param>
        void WriteToLog(string message);

        /// <summary>Log informational message</summary>
        /// <param name="message">Information message</param>
        void LogInfo(string message);

        /// <summary>Log warning message</summary>
        /// <param name="message">Warning message</param>
        void LogWarning(string message);

        /// <summary>Log error message</summary>
        /// <param name="message">Error message</param>
        void LogError(string message);

        /// <summary>Get current log file path</summary>
        /// <returns>Full path to the log file</returns>
        string GetDebugLogPath();
    }
}
