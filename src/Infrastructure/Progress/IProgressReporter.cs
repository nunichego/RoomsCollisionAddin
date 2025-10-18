namespace RoomsManagerAddin.Infrastructure.Progress
{
    /// <summary>
    /// Interface for reporting progress during long-running operations
    /// </summary>
    /// <remarks>
    /// Provides callback-based progress reporting for UI updates.
    /// </remarks>
    public interface IProgressReporter
    {
        /// <summary>Report progress percentage</summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="currentStep">Description of current step</param>
        void ReportProgress(int percentage, string currentStep);

        /// <summary>Check if operation was cancelled</summary>
        /// <returns>True if operation should be cancelled</returns>
        bool IsCancelled();
    }
}
