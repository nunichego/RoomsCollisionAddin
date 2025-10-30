using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Core.Exceptions;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for collecting elements from Revit document
    /// </summary>
    public class ElementCollectorService : IElementCollectorService
    {
        public ElementCollectorService()
        {
        }

        /// <summary>
        /// Get all rooms from the document
        /// </summary>
        /// <param name="document">The Revit document to collect rooms from</param>
        /// <returns>List of rooms with area greater than 0</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null</exception>
        /// <exception cref="RevitApiException">Thrown when Revit API operations fail</exception>
        public List<Room> GetRooms(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document), "Document cannot be null");

            try
            {
                var collector = new FilteredElementCollector(document);
                var rooms = collector.OfClass(typeof(SpatialElement))
                                   .Cast<SpatialElement>()
                                   .Where(se => se is Room)
                                   .Cast<Room>()
                                   .Where(r => r.Area > 0)
                                   .ToList();

                return rooms;
            }
            catch (Exception ex)
            {
                throw new RevitApiException("collecting rooms", ex);
            }
        }

        /// <summary>
        /// Get all walls from the document
        /// </summary>
        /// <param name="document">The Revit document to collect walls from</param>
        /// <returns>List of walls with valid wall types</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null</exception>
        /// <exception cref="RevitApiException">Thrown when Revit API operations fail</exception>
        public List<Wall> GetWalls(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document), "Document cannot be null");

            try
            {
                var collector = new FilteredElementCollector(document);
                var walls = collector.OfClass(typeof(Wall))
                                   .Cast<Wall>()
                                   .Where(w => w.WallType != null)
                                   .ToList();

                return walls;
            }
            catch (Exception ex)
            {
                throw new RevitApiException("collecting walls", ex);
            }
        }

        /// <summary>
        /// Get all floors from the document
        /// </summary>
        /// <param name="document">The Revit document to collect floors from</param>
        /// <returns>List of floors with valid floor types</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null</exception>
        /// <exception cref="RevitApiException">Thrown when Revit API operations fail</exception>
        public List<Floor> GetFloors(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document), "Document cannot be null");

            try
            {
                var collector = new FilteredElementCollector(document);
                var floors = collector.OfClass(typeof(Floor))
                                    .Cast<Floor>()
                                    .Where(f => f.FloorType != null)
                                    .ToList();

                return floors;
            }
            catch (Exception ex)
            {
                throw new RevitApiException("collecting floors", ex);
            }
        }

        /// <summary>
        /// Get all ceilings from the document
        /// </summary>
        /// <param name="document">The Revit document to collect ceilings from</param>
        /// <returns>List of ceilings with valid ceiling types</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null</exception>
        /// <exception cref="RevitApiException">Thrown when Revit API operations fail</exception>
        public List<Ceiling> GetCeilings(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document), "Document cannot be null");

            try
            {
                var collector = new FilteredElementCollector(document);
                var ceilings = collector.OfClass(typeof(Ceiling))
                                      .Cast<Ceiling>()
                                      .Where(c => c.GetTypeId() != ElementId.InvalidElementId)
                                      .ToList();

                return ceilings;
            }
            catch (Exception ex)
            {
                throw new RevitApiException("collecting ceilings", ex);
            }
        }
    }
}
