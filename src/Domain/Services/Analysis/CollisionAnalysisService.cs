using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Core.Exceptions;
using RoomsManagerAddin.Domain.Models.Analysis;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Infrastructure.Progress;

namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing collisions between rooms and other elements
    /// UPDATED: Supports Room Boundary API for walls, solid intersection for floors and ceilings
    /// </summary>
    public class CollisionAnalysisService : ICollisionAnalysisService
    {
        private readonly IWallBoundaryAnalysisService _wallBoundaryService;
        private readonly IFloorBoundaryAnalysisService _floorBoundaryService;
        private readonly ICeilingBoundaryAnalysisService _ceilingBoundaryService;

        public CollisionAnalysisService(
            IWallBoundaryAnalysisService wallBoundaryService,
            IFloorBoundaryAnalysisService floorBoundaryService,
            ICeilingBoundaryAnalysisService ceilingBoundaryService)
        {
            _wallBoundaryService = wallBoundaryService;
            _floorBoundaryService = floorBoundaryService;
            _ceilingBoundaryService = ceilingBoundaryService;
        }

        /// <summary>
        /// Analyze room collisions with walls using Room Boundary API
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Wall> walls,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter)
        {
            writeToLog("=== COLLISION ANALYSIS SERVICE ===");
            writeToLog("Delegating to Wall Boundary Analysis Service for optimized room-wall analysis");

            // Delegate to the wall boundary-based service
            return _wallBoundaryService.AnalyzeRoomCollisions(document, rooms, walls, parameterMappings, writeToLog, progressReporter);
        }

        /// <summary>
        /// Analyze room collisions with floors using solid intersection
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomFloorsCollisions(
            Document document,
            List<Room> rooms,
            List<Floor> floors,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter)
        {
            writeToLog("=== COLLISION ANALYSIS SERVICE ===");
            writeToLog("Delegating to Floor Boundary Analysis Service for room-floor solid intersection analysis");

            // Delegate to the floor solid-based service
            return _floorBoundaryService.AnalyzeRoomCollisions(document, rooms, floors, parameterMappings, writeToLog, progressReporter);
        }

        /// <summary>
        /// Analyze room collisions with ceilings using solid intersection
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCeilingsCollisions(
            Document document,
            List<Room> rooms,
            List<Ceiling> ceilings,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter)
        {
            writeToLog("=== COLLISION ANALYSIS SERVICE ===");
            writeToLog("Delegating to Ceiling Boundary Analysis Service for room-ceiling solid intersection analysis");

            // Delegate to the ceiling solid-based service
            return _ceilingBoundaryService.AnalyzeRoomCollisions(document, rooms, ceilings, parameterMappings, writeToLog, progressReporter);
        }
    }
}
