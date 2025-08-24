using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;


namespace RoomsManagerAddin.Services
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
        public Solid GetElementSolid(Element element)
        {
            try
            {
                if (element is Wall wall)
                {
                    return GetWallSolid(wall);
                }

                var geometryElement = element.get_Geometry(new Options());
                if (geometryElement == null)
                {
                    // No geometry found for element
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

                // No valid solid found for element
                return null;
            }
            catch (Exception)
            {
                // Error getting solid for element
                return null;
            }
        }

        /// <summary>
        /// Get solid geometry from a wall, handling both regular and curtain walls
        /// </summary>
        public Solid GetWallSolid(Wall wall)
        {
            try
            {
                if (wall.WallType.Kind == WallKind.Curtain)
                {
                    return CreateCurtainWallSolid(wall);
                }

                // For regular walls, use standard geometry
                var geometryElement = wall.get_Geometry(new Options());
                if (geometryElement == null)
                {
                    // No geometry found for wall
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

                // No valid solid found for wall
                return null;
            }
            catch (Exception)
            {
                // Error getting solid for wall
                return null;
            }
        }

        /// <summary>
        /// Create a simple solid for curtain walls using their location line
        /// </summary>
        public Solid CreateCurtainWallSolid(Wall wall)
        {
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
            catch (Exception)
            {
                // Error creating curtain wall solid
                return null;
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
        public bool SolidsIntersect(Solid solid1, Solid solid2)
        {
            try
            {
                var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    solid1, solid2, BooleanOperationsType.Intersect);
                return intersection != null && intersection.Volume > 0;
            }
            catch (Exception)
            {
                // Error checking solid intersection
                return false;
            }
        }
    }
}
