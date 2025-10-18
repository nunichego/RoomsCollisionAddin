using System;

namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when Revit API operations fail
    /// </summary>
    /// <remarks>
    /// This exception wraps Revit API errors and provides user-friendly messages.
    /// </remarks>
    public class RevitApiException : RoomsManagerException
    {
        /// <summary>The Revit operation that failed</summary>
        public string RevitOperation { get; set; }

        /// <summary>
        /// Initializes a new instance of the RevitApiException class
        /// </summary>
        /// <param name="operation">The Revit operation that failed (e.g., "collecting rooms")</param>
        /// <param name="innerException">The exception from the Revit API</param>
        public RevitApiException(string operation, Exception innerException)
            : base($"Revit API error during {operation}", innerException)
        {
            RevitOperation = operation;
            UserMessage = $"An error occurred while accessing Revit elements. " +
                         $"Please check that the document is valid and try again.";
        }
    }
}
