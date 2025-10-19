using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Core.Exceptions;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for handling geometry operations
    /// </summary>
    public class GeometryService
    {
        public GeometryService()
        {
        }

        /// <summary>
        /// Get solid geometry from any element
        /// </summary>
        /// <param name="element">The element to extract solid geometry from</param>
        /// <returns>Solid geometry or null if no valid solid found</returns>
        /// <exception cref="ArgumentNullException">Thrown when element is null</exception>
        /// <exception cref="RevitApiException">Thrown when geometry extraction fails</exception>
        public Solid GetElementSolid(Element element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element), "Element cannot be null");

            try
            {
                if (element is Wall wall)
                {
                    return GetWallSolid(wall);
                }

                return ExtractSolidFromGeometry(element.get_Geometry(new Options()));
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RevitApiException($"extracting solid geometry from element {element.Id}", ex);
            }
        }

        /// <summary>
        /// Get solid geometry from a wall, handling both regular and curtain walls
        /// </summary>
        /// <param name="wall">The wall to extract solid geometry from</param>
        /// <returns>Solid geometry or null if no valid solid found</returns>
        /// <exception cref="ArgumentNullException">Thrown when wall is null</exception>
        /// <exception cref="RevitApiException">Thrown when geometry extraction fails</exception>
        public Solid GetWallSolid(Wall wall)
        {
            if (wall == null)
                throw new ArgumentNullException(nameof(wall), "Wall cannot be null");

            try
            {
                if (wall.WallType.Kind == WallKind.Curtain)
                {
                    return CreateCurtainWallSolid(wall);
                }

                // For regular walls, use standard geometry extraction
                return ExtractSolidFromGeometry(wall.get_Geometry(new Options()));
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RevitApiException($"extracting solid geometry from wall {wall.Id}", ex);
            }
        }

        /// <summary>
        /// Extract solid geometry from GeometryElement (common logic)
        /// </summary>
        private Solid ExtractSolidFromGeometry(GeometryElement geometryElement)
        {
            if (geometryElement == null)
            {
                return null;
            }

            foreach (var geometryObject in geometryElement)
            {
                if (geometryObject is Solid solid && solid.Volume > 0)
                {
                    return solid;
                }
                else if (geometryObject is GeometryInstance geometryInstance)
                {
                    var instanceGeometry = geometryInstance.GetInstanceGeometry();
                    foreach (var instanceObject in instanceGeometry)
                    {
                        if (instanceObject is Solid instanceSolid && instanceSolid.Volume > 0)
                        {
                            return instanceSolid;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Create a simple solid for curtain walls using their location line
        /// </summary>
        /// <param name="wall">The curtain wall to create solid for</param>
        /// <returns>Solid geometry or null if creation failed</returns>
        /// <exception cref="ArgumentNullException">Thrown when wall is null</exception>
        /// <exception cref="RevitApiException">Thrown when solid creation fails</exception>
        public Solid CreateCurtainWallSolid(Wall wall)
        {
            if (wall == null)
                throw new ArgumentNullException(nameof(wall), "Wall cannot be null");

            try
            {
                if (wall.Location is LocationCurve locationCurve)
                {
                    var curve = locationCurve.Curve;
                    var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
                    var thickness = 0.02; // 2cm thickness

                    var profile = CreateRectangularProfile(curve, height, thickness);
                    if (profile != null)
                    {
                        var solid = GeometryCreationUtilities.CreateExtrusionGeometry(
                            new List<CurveLoop> { profile }, XYZ.BasisZ, height);
                        return solid;
                    }
                }

                // Could not create solid for curtain wall
                return null;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RevitApiException($"creating solid for curtain wall {wall.Id}", ex);
            }
        }

        /// <summary>
        /// Create a rectangular profile from a curve
        /// </summary>
        private CurveLoop CreateRectangularProfile(Curve curve, double height, double thickness)
        {
            try
            {
                var startPoint = curve.GetEndPoint(0);
                var endPoint = curve.GetEndPoint(1);
                var direction = (endPoint - startPoint).Normalize();
                var up = XYZ.BasisZ;
                var right = direction.CrossProduct(up).Normalize();

                var points = new List<XYZ>
                {
                    startPoint + right * (thickness / 2),
                    startPoint - right * (thickness / 2),
                    endPoint - right * (thickness / 2),
                    endPoint + right * (thickness / 2)
                };

                var curveLoop = new CurveLoop();
                for (int i = 0; i < points.Count; i++)
                {
                    var current = points[i];
                    var next = points[(i + 1) % points.Count];
                    var line = Line.CreateBound(current, next);
                    curveLoop.Append(line);
                }

                return curveLoop;
            }
            catch (Exception)
            {
                // Error creating rectangular profile
                return null;
            }
        }



        /// <summary>
        /// Check if two solids intersect
        /// </summary>
        /// <param name="solid1">First solid</param>
        /// <param name="solid2">Second solid</param>
        /// <returns>True if solids intersect, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when either solid is null</exception>
        /// <exception cref="RevitApiException">Thrown when intersection check fails</exception>
        public bool SolidsIntersect(Solid solid1, Solid solid2)
        {
            if (solid1 == null)
                throw new ArgumentNullException(nameof(solid1), "First solid cannot be null");
            if (solid2 == null)
                throw new ArgumentNullException(nameof(solid2), "Second solid cannot be null");

            try
            {
                var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    solid1, solid2, BooleanOperationsType.Intersect);
                return intersection != null && intersection.Volume > 0;
            }
            catch (Exception ex)
            {
                throw new RevitApiException("checking solid intersection", ex);
            }
        }
    }
}
