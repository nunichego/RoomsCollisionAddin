using System;

namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown during collision analysis operations
    /// </summary>
    /// <remarks>
    /// This exception is used when collision analysis encounters errors during processing.
    /// </remarks>
    public class CollisionAnalysisException : RoomsManagerException
    {
        /// <summary>The phase of analysis that failed</summary>
        public string AnalysisPhase { get; set; }

        /// <summary>
        /// Initializes a new instance of the CollisionAnalysisException class
        /// </summary>
        /// <param name="phase">The analysis phase that failed (e.g., "geometry extraction")</param>
        /// <param name="innerException">The exception that caused the failure</param>
        public CollisionAnalysisException(string phase, Exception innerException)
            : base($"Collision analysis failed during {phase}", innerException)
        {
            AnalysisPhase = phase;
            UserMessage = $"The collision analysis encountered an error. " +
                         $"Some results may be incomplete. Please check the log for details.";
        }
    }
}
