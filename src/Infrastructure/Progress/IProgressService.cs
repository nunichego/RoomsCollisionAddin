using System;

namespace RoomsManagerAddin.Infrastructure.Progress
{
    /// <summary>
    /// Service for managing progress reporting
    /// </summary>
    /// <remarks>
    /// Coordinates progress reporting between background operations and UI.
    /// </remarks>
    public interface IProgressService
    {
        /// <summary>Execute an action with progress reporting</summary>
        /// <param name="action">The action to execute</param>
        /// <param name="progressReporter">Progress reporter callback</param>
        void ExecuteWithProgress(Action<IProgressReporter> action, IProgressReporter progressReporter);
    }
}
