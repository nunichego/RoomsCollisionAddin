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
    /// UPDATED: Supports dual algorithms for walls (Room Boundary API + Solid), solid intersection for floors and ceilings
    /// </summary>
    public class CollisionAnalysisService : ICollisionAnalysisService
    {
        private readonly IWallBoundaryAnalysisService _wallBoundaryService;
        private readonly IWallSolidAnalysisService _wallSolidService;
        private readonly IFloorBoundaryAnalysisService _floorBoundaryService;
        private readonly ICeilingBoundaryAnalysisService _ceilingBoundaryService;

        public CollisionAnalysisService(
            IWallBoundaryAnalysisService wallBoundaryService,
            IWallSolidAnalysisService wallSolidService,
            IFloorBoundaryAnalysisService floorBoundaryService,
            ICeilingBoundaryAnalysisService ceilingBoundaryService)
        {
            _wallBoundaryService = wallBoundaryService;
            _wallSolidService = wallSolidService;
            _floorBoundaryService = floorBoundaryService;
            _ceilingBoundaryService = ceilingBoundaryService;
        }

        /// <summary>
        /// Analyze room collisions with walls using the specified algorithm
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Wall> walls,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter,
            WallAnalysisAlgorithm algorithm = WallAnalysisAlgorithm.BoundaryApi)
        {
            writeToLog("=== COLLISION ANALYSIS SERVICE ===");

            if (algorithm == WallAnalysisAlgorithm.SolidBased)
            {
                writeToLog("Using SOLID INTERSECTION algorithm with diagonal room expansion (2cm)");
                writeToLog("This method is slower but detects all spatial intersections");
                return _wallSolidService.AnalyzeRoomCollisions(document, rooms, walls, parameterMappings, writeToLog, progressReporter);
            }
            else
            {
                writeToLog("Using ROOM BOUNDARY API algorithm (optimized for walls)");
                writeToLog("This method is fast but only detects proper room boundaries");
                return _wallBoundaryService.AnalyzeRoomCollisions(document, rooms, walls, parameterMappings, writeToLog, progressReporter);
            }
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
