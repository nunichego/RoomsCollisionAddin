using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing collisions between rooms and other elements
    /// UPDATED: Supports Room Boundary API for walls, solid intersection for floors
    /// </summary>
    public class CollisionAnalysisService
    {
        private readonly WallBoundaryAnalysisService _wallBoundaryService;
        private readonly FloorBoundaryAnalysisService _floorBoundaryService;

        public CollisionAnalysisService(
            WallBoundaryAnalysisService wallBoundaryService,
            FloorBoundaryAnalysisService floorBoundaryService)
        {
            _wallBoundaryService = wallBoundaryService;
            _floorBoundaryService = floorBoundaryService;
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
    }
}
