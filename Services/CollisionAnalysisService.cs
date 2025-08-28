using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Services.Categories.Walls;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for analyzing collisions between rooms and other elements
    /// UPDATED: Now uses Room Boundary API for walls, solid intersection for other elements
    /// </summary>
    public class CollisionAnalysisService
    {
        private readonly WallBoundaryAnalysisService _wallBoundaryService;
        
        public CollisionAnalysisService(WallBoundaryAnalysisService wallBoundaryService)
        {
            _wallBoundaryService = wallBoundaryService;
        }

        /// <summary>
        /// Analyze room collisions with walls using Room Boundary API
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document, 
            List<Room> rooms, 
            List<Wall> walls, 
            Action<string> writeToLog,
            Action<string, string, int, int, int, int> showProgress)
        {
            writeToLog("=== COLLISION ANALYSIS SERVICE ===");
            writeToLog("Delegating to Wall Boundary Analysis Service for optimized room-wall analysis");
            
            // Delegate to the new boundary-based service
            return _wallBoundaryService.AnalyzeRoomCollisions(document, rooms, walls, writeToLog, showProgress);
        }

    }
}
