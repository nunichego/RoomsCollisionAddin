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
    /// Interface for analyzing collisions between rooms and other building elements.
    /// Supports multiple analysis strategies (Room Boundary API for walls, solid intersection for floors).
    /// </summary>
    public interface ICollisionAnalysisService
    {
        /// <summary>
        /// Analyzes room collisions with walls using the specified algorithm.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="walls">The list of walls to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <param name="algorithm">The wall analysis algorithm to use (default: BoundaryApi).</param>
        /// <returns>A list of room collision results.</returns>
        List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Wall> walls,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter,
            WallAnalysisAlgorithm algorithm = WallAnalysisAlgorithm.BoundaryApi);

        /// <summary>
        /// Analyzes room collisions with floors using solid intersection.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="floors">The list of floors to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <returns>A list of room collision results.</returns>
        List<RoomCollisionResult> AnalyzeRoomFloorsCollisions(
            Document document,
            List<Room> rooms,
            List<Floor> floors,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);

        /// <summary>
        /// Analyzes room collisions with ceilings using solid intersection.
        /// </summary>
        /// <param name="document">The Revit document containing the elements.</param>
        /// <param name="rooms">The list of rooms to analyze.</param>
        /// <param name="ceilings">The list of ceilings to check for collisions.</param>
        /// <param name="parameterMappings">Parameter mapping configurations for data synchronization.</param>
        /// <param name="writeToLog">Action to write log messages.</param>
        /// <param name="progressReporter">Progress reporter for tracking analysis progress.</param>
        /// <returns>A list of room collision results.</returns>
        List<RoomCollisionResult> AnalyzeRoomCeilingsCollisions(
            Document document,
            List<Room> rooms,
            List<Ceiling> ceilings,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter);
    }
}
