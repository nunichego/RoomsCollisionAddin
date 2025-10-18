using System;

namespace RoomsManagerAddin.Domain.Models.Configuration
{
    /// <summary>
    /// Application settings model
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Show collision warnings
        /// </summary>
        public bool ShowCollisionWarnings { get; set; } = true;

        /// <summary>
        /// Minimum collision threshold
        /// </summary>
        public double CollisionThreshold { get; set; } = 0.1;

        /// <summary>
        /// Last analysis date
        /// </summary>
        public DateTime? LastAnalysisDate { get; set; }

        /// <summary>
        /// Analysis timeout in seconds
        /// </summary>
        public int AnalysisTimeoutSeconds { get; set; } = 30;
    }
}
