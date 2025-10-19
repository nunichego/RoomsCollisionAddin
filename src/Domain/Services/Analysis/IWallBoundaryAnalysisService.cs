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
    /// Interface for analyzing room-wall collisions using the Room Boundary API.
    /// This approach is optimized for walls and uses Revit's native boundary detection.
    /// </summary>
    public interface IWallBoundaryAnalysisService
    {
        /// <summary>
        /// Analyzes room collisions with walls using the Room Boundary API.
        /// This method uses Revit's GetBoundarySegments to efficiently detect which walls bound each room.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="walls">The list of walls to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <returns>A list of room collision results with wall boundary information.</returns>
        /// <remarks>
        /// Performance: This method is significantly faster than solid-based collision detection for walls
        /// as it leverages Revit's built-in room boundary calculation.
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
