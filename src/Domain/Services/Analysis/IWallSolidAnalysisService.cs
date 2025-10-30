using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Domain.Models.Analysis;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Infrastructure.Progress;

namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Interface for analyzing room-wall collisions using solid intersection.
    /// This approach uses diagonal room solid expansion and 3D intersection for comprehensive wall detection.
    /// </summary>
    public interface IWallSolidAnalysisService
    {
        /// <summary>
        /// Analyzes room collisions with walls using solid-based intersection.
        /// Uses diagonally expanded room solids (±2cm along XY-plane) to detect wall intersections.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="walls">The list of walls to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <returns>A list of room collision results with wall intersection information.</returns>
        /// <remarks>
        /// Algorithm: Creates two diagonal offset room solids (±XY directions) and performs
        /// solid intersection with wall geometries. Includes bounding box and Z-axis pre-filtering for performance.
        /// This method is slower than Room Boundary API but detects all spatial intersections, not just boundaries.
        /// </remarks>
        List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Wall> walls,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);
    }
}
