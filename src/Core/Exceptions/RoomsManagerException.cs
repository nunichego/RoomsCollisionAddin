using System;

namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Base exception for all RoomsManager errors
    /// </summary>
    /// <remarks>
    /// This exception provides user-friendly error messages and technical details for logging.
    /// </remarks>
    public class RoomsManagerException : Exception
    {
        /// <summary>User-friendly error message for display to end users</summary>
        public string UserMessage { get; set; }

        /// <summary>Technical details for logging and debugging</summary>
        public string TechnicalDetails { get; set; }

        /// <summary>Indicates if error should be shown to user</summary>
        public bool ShowToUser { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the RoomsManagerException class
        /// </summary>
        /// <param name="message">The error message</param>
        public RoomsManagerException(string message) : base(message)
        {
            UserMessage = message;
        }

        /// <summary>
        /// Initializes a new instance of the RoomsManagerException class with an inner exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public RoomsManagerException(string message, Exception innerException)
            : base(message, innerException)
        {
            UserMessage = message;
            TechnicalDetails = innerException?.ToString();
        }

        /// <summary>
        /// Initializes a new instance with separate user and technical messages
        /// </summary>
        /// <param name="userMessage">User-friendly message for display</param>
        /// <param name="technicalDetails">Technical details for logging</param>
        public RoomsManagerException(string userMessage, string technicalDetails)
            : base(userMessage)
        {
            UserMessage = userMessage;
            TechnicalDetails = technicalDetails;
        }
    }
}
