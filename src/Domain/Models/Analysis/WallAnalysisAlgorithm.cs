namespace RoomsManagerAddin.Domain.Models.Analysis
{
    /// <summary>
    /// Specifies the algorithm to use for wall-room collision analysis
    /// </summary>
    public enum WallAnalysisAlgorithm
    {
        /// <summary>
        /// Uses Revit's Room Boundary API (room.GetBoundarySegments()).
        /// Fast and efficient, but only detects walls that are proper room boundaries.
        /// </summary>
        BoundaryApi = 0,

        /// <summary>
        /// Uses solid-solid intersection with diagonal room expansion.
        /// Slower but more comprehensive - detects all walls with spatial intersections.
        /// </summary>
        SolidBased = 1
    }
}
