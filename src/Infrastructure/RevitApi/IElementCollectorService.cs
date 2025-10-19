using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for collecting Revit elements from documents
    /// </summary>
    /// <remarks>
    /// Provides centralized element collection with proper filtering and error handling.
    /// </remarks>
    public interface IElementCollectorService
    {
        /// <summary>Get all rooms in document</summary>
        /// <param name="document">The Revit document</param>
        /// <returns>List of all placed rooms with area &gt; 0</returns>
        List<Room> GetRooms(Document document);

        /// <summary>Get all walls in document</summary>
        /// <param name="document">The Revit document</param>
        /// <returns>List of all walls</returns>
        List<Wall> GetWalls(Document document);

        /// <summary>Get all floors in document</summary>
        /// <param name="document">The Revit document</param>
        /// <returns>List of all floors</returns>
        List<Floor> GetFloors(Document document);
    }
}
