using System;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Core.Exceptions;
using RoomsManagerAddin.Infrastructure.Logging;

namespace RoomsManagerAddin.Core.ErrorHandling
{
    /// <summary>
    /// Centralized error handling for the RoomsManagerAddin
    /// </summary>
    /// <remarks>
    /// Provides consistent error handling, logging, and user-friendly error messages
    /// </remarks>
    public static class GlobalErrorHandler
    {
        private static ILoggingService _loggingService;

        /// <summary>
        /// Initialize the global error handler with a logging service
        /// </summary>
        /// <param name="loggingService">The logging service to use for error logging</param>
        public static void Initialize(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Handle an exception by logging it and optionally showing a user message
        /// </summary>
        /// <param name="ex">The exception to handle</param>
        /// <param name="context">Context information (e.g., "Room Analysis", "Filter Validation")</param>
        /// <param name="showToUser">Whether to display an error dialog to the user</param>
        public static void HandleException(Exception ex, string context, bool showToUser = true)
        {
            // Extract user message and technical details
            string userMessage;
            string technicalDetails;

            if (ex is RoomsManagerException rme)
            {
                userMessage = rme.UserMessage ?? ex.Message;
                technicalDetails = rme.TechnicalDetails ?? ex.ToString();
                showToUser = showToUser && rme.ShowToUser;
            }
            else
            {
                userMessage = GetUserFriendlyMessage(ex, context);
                technicalDetails = ex.ToString();
            }

            // Log the error
            _loggingService?.WriteToLog($"ERROR in {context}: {ex.Message}");
            _loggingService?.WriteToLog($"Technical Details: {technicalDetails}");

            // Show to user if requested
            if (showToUser)
            {
                TaskDialog.Show($"Error - {context}", userMessage);
            }
        }

        /// <summary>
        /// Get a user-friendly error message from a generic exception
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="context">Context information</param>
        /// <returns>User-friendly error message</returns>
        public static string GetUserFriendlyMessage(Exception ex, string context)
        {
            if (ex == null) return "An unknown error occurred.";

            // Check for common exception types
            if (ex is ArgumentNullException argEx)
            {
                return $"A required value was missing: {argEx.ParamName}. Please try again.";
            }

            if (ex is InvalidOperationException)
            {
                return $"The operation could not be completed. {ex.Message}";
            }

            if (ex is UnauthorizedAccessException)
            {
                return "Access denied. Please check file permissions and try again.";
            }

            // Default message
            return $"An error occurred during {context}. Please check the log file for details.";
        }

        /// <summary>
        /// Execute an action with centralized error handling
        /// </summary>
        /// <param name="context">Context information (e.g., "Room Analysis")</param>
        /// <param name="action">The action to execute</param>
        /// <param name="errorMessage">Output parameter for error message (for Revit commands)</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool ExecuteWithErrorHandling(string context, Action action, ref string errorMessage)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (RoomsManagerException rme)
            {
                errorMessage = rme.UserMessage ?? rme.Message;
                HandleException(rme, context, rme.ShowToUser);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = GetUserFriendlyMessage(ex, context);
                HandleException(ex, context, true);
                return false;
            }
        }

        /// <summary>
        /// Execute a function with centralized error handling
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="context">Context information</param>
        /// <param name="function">The function to execute</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="errorMessage">Output parameter for error message</param>
        /// <returns>The function result, or defaultValue if an error occurred</returns>
        public static T ExecuteWithErrorHandling<T>(string context, Func<T> function, T defaultValue, ref string errorMessage)
        {
            try
            {
                return function != null ? function() : defaultValue;
            }
            catch (RoomsManagerException rme)
            {
                errorMessage = rme.UserMessage ?? rme.Message;
                HandleException(rme, context, rme.ShowToUser);
                return defaultValue;
            }
            catch (Exception ex)
            {
                errorMessage = GetUserFriendlyMessage(ex, context);
                HandleException(ex, context, true);
                return defaultValue;
            }
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogInfo(string message)
        {
            _loggingService?.WriteToLog($"INFO: {message}");
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        public static void LogWarning(string message)
        {
            _loggingService?.WriteToLog($"WARNING: {message}");
        }

        /// <summary>
        /// Log an error message without throwing
        /// </summary>
        /// <param name="message">The error message</param>
        public static void LogError(string message)
        {
            _loggingService?.WriteToLog($"ERROR: {message}");
        }
    }
}
