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
    /// Interface for analyzing room-floor collisions using solid intersection.
    /// This approach uses vertical room solid expansion and 3D intersection for accurate floor detection.
    /// </summary>
    public interface IFloorBoundaryAnalysisService
    {
        /// <summary>
        /// Analyzes room collisions with floors using solid-based intersection.
        /// Uses vertically expanded room solids (±1cm along Z-axis) to detect floor intersections.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="floors">The list of floors to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <returns>A list of room collision results with floor intersection information.</returns>
        /// <remarks>
        /// Algorithm: Creates two offset room solids (+Z and -Z directions) and performs
        /// solid intersection with floor geometries. Includes bounding box pre-filtering for performance.
        /// </remarks>
        List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Floor> floors,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);
    }
}
