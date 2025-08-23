using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for collecting elements from Revit document
    /// </summary>
    public class ElementCollectorService
    {
        private readonly ILogger _logger;

        public ElementCollectorService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all rooms from the document
        /// </summary>
        public List<Room> GetRooms(Document document)
        {
            try
            {
                var collector = new FilteredElementCollector(document);
                var rooms = collector.OfClass(typeof(SpatialElement))
                                   .Cast<SpatialElement>()
                                   .Where(se => se is Room)
                                   .Cast<Room>()
                                   .Where(r => r.Area > 0)
                                   .ToList();

                // Keep summary logging elsewhere; suppress here
                return rooms;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error collecting rooms");
                return new List<Room>();
            }
        }

        /// <summary>
        /// Get all walls from the document
        /// </summary>
        public List<Wall> GetWalls(Document document)
        {
            try
            {
                var collector = new FilteredElementCollector(document);
                var walls = collector.OfClass(typeof(Wall))
                                   .Cast<Wall>()
                                   .Where(w => w.WallType != null)
                                   .ToList();

                // Keep summary logging elsewhere; suppress here
                return walls;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error collecting walls");
                return new List<Wall>();
            }
        }

        /// <summary>
        /// Get all floors from the document
        /// </summary>
        public List<Floor> GetFloors(Document document)
        {
            try
            {
                var collector = new FilteredElementCollector(document);
                var floors = collector.OfClass(typeof(Floor))
                                    .Cast<Floor>()
                                    .Where(f => f.FloorType != null)
                                    .ToList();

                // Keep summary logging elsewhere; suppress here
                return floors;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error collecting floors");
                return new List<Floor>();
            }
        }
    }
}
