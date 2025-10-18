using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for Revit geometry operations
    /// </summary>
    /// <remarks>
    /// Provides centralized geometry extraction and spatial analysis functionality.
    /// </remarks>
    public interface IGeometryService
    {
        /// <summary>Extract solid geometry from an element</summary>
        /// <param name="element">The element to extract geometry from</param>
        /// <returns>The solid geometry, or null if not available</returns>
        Solid GetElementSolid(Element element);

        /// <summary>Get bounding box for an element</summary>
        /// <param name="element">The element</param>
        /// <returns>The bounding box</returns>
        BoundingBoxXYZ GetBoundingBox(Element element);

        /// <summary>Check if two solids intersect</summary>
        /// <param name="solid1">First solid</param>
        /// <param name="solid2">Second solid</param>
        /// <returns>True if solids intersect</returns>
        bool DoSolidsIntersect(Solid solid1, Solid solid2);
    }
}
