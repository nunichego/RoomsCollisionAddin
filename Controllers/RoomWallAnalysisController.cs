using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Controllers
{
    /// <summary>
    /// Controller that separates UI logic from business logic for room-wall analysis
    /// </summary>
    public class RoomWallAnalysisController
    {
        private readonly Document _document;
        private readonly ElementCollectorService _elementCollector;
        private readonly CollisionAnalysisService _collisionAnalysisService;

        public RoomWallAnalysisController(Document document)
        {
            _document = document;
            _elementCollector = new ElementCollectorService();
            
            // Initialize services
            var geometryService = new GeometryService();
            var parameterService = new ParameterUpdateService();
            var wallProcessingService = new WallProcessingService();
            var roomProcessingService = new RoomProcessingService();
            
            _collisionAnalysisService = new CollisionAnalysisService(
                null, geometryService, parameterService, wallProcessingService, roomProcessingService);
        }

        /// <summary>
        /// Load all rooms and walls from the document
        /// </summary>
        public void LoadElements(out List<RoomItem> roomItems, out List<WallItem> wallItems)
        {
            var rooms = _elementCollector.GetRooms(_document);
            var walls = _elementCollector.GetWalls(_document);

            roomItems = rooms.Select(r => new RoomItem(r)).ToList();
            wallItems = walls.Select(w => new WallItem(w)).ToList();
        }

        /// <summary>
        /// Filter rooms based on level and minimum area
        /// </summary>
        public List<RoomItem> FilterRooms(List<RoomItem> roomItems, string levelFilter, string minAreaText)
        {
            var filteredRooms = roomItems.AsEnumerable();

            // Filter by level
            if (levelFilter != "All Levels")
            {
                filteredRooms = filteredRooms.Where(r => r.Room.Level?.Name == levelFilter);
            }

            // Filter by minimum area
            if (double.TryParse(minAreaText, out double minArea) && minArea > 0)
            {
                filteredRooms = filteredRooms.Where(r => r.Room.Area >= minArea);
            }

            return filteredRooms.ToList();
        }

        /// <summary>
        /// Filter walls based on level and wall type
        /// </summary>
        public List<WallItem> FilterWalls(List<WallItem> wallItems, string levelFilter, string typeFilter)
        {
            var filteredWalls = wallItems.AsEnumerable();

            // Filter by level
            if (levelFilter != "All Levels")
            {
                filteredWalls = filteredWalls.Where(w => w.GetLevel() == levelFilter);
            }

            // Filter by wall type
            if (typeFilter != "All Types")
            {
                filteredWalls = filteredWalls.Where(w => w.Wall.WallType?.Name == typeFilter);
            }

            return filteredWalls.ToList();
        }

        /// <summary>
        /// Run collision analysis on selected rooms and walls
        /// </summary>
        public List<RoomCollisionResult> Analyze(List<Room> rooms, List<Wall> walls, 
            Action<string, string, int, int, int, int> progressCallback)
        {
            // Create a simple logging callback
            Action<string> writeToLog = (message) => 
            {
                System.Diagnostics.Debug.WriteLine(message);
            };

            return _collisionAnalysisService.AnalyzeRoomCollisions(_document, rooms, walls, writeToLog, progressCallback);
        }
    }
}