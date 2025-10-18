using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;


namespace RoomsManagerAddin.Domain.Services.Processing
{
    /// <summary>
    /// Service for processing wall geometry and creating solids
    /// </summary>
    public class WallProcessingService
    {
        public WallProcessingService()
        {
        }

        /// <summary>
        /// Process all walls and separate them into regular and curtain walls
        /// </summary>
        public WallProcessingResult ProcessWalls(List<Wall> walls, Action<string> writeToLog)
        {
            var result = new WallProcessingResult();
            
            writeToLog?.Invoke("=== WALL PROCESSING STARTED ===");
            
            foreach (var wall in walls)
            {
                try
                {
                    if (wall.WallType.Kind == WallKind.Curtain)
                    {
                        var curtainSolid = CreateCurtainWallSolid(wall, writeToLog);
                        if (curtainSolid != null)
                        {
                            result.CurtainWallSolids[wall] = curtainSolid;
                        }
                    }
                    else
                    {
                        result.RegularWalls.Add(wall);
                    }
                }
                catch (Exception ex)
                {
                    writeToLog?.Invoke($"  ✗ Error processing wall {wall.Id}: {ex.Message}");
                    // Error processing wall
                }
            }
            
            writeToLog?.Invoke($"=== WALL PROCESSING COMPLETED ===");
            writeToLog?.Invoke($"Summary: {result.CurtainWallSolids.Count} curtain walls processed, {result.RegularWalls.Count} regular walls");
            writeToLog?.Invoke("");
            
            return result;
        }

        /// <summary>
        /// Get solid geometry from a regular wall
        /// </summary>
        public Solid GetRegularWallSolid(Wall wall, Action<string> writeToLog = null)
        {
            try
            {
                // Minimal per-wall logging
                
                // First, try to get the solid using standard geometry extraction
                var standardSolid = GetStandardWallSolid(wall, writeToLog);
                if (standardSolid != null)
                {
                    return standardSolid;
                }

                // If standard extraction fails, create solid from wall location line
                // Fallback to location-based solid
                return CreateRegularWallSolidFromLocation(wall, writeToLog);
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error getting solid for wall {wall.Id}: {ex.Message}");
                // Error getting solid for wall
                return null;
            }
        }

        /// <summary>
        /// Try to get solid using standard geometry extraction
        /// </summary>
        private Solid GetStandardWallSolid(Wall wall, Action<string> writeToLog = null)
        {
            try
            {
                var options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                var geometryElement = wall.get_Geometry(options);
                if (geometryElement == null)
                {
                    // No geometry found
                    return null;
                }

                foreach (var geomObject in geometryElement)
                {
                    if (geomObject is Solid solid && solid.Volume > 0)
                    {
                        // Found standard solid
                        return solid;
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        // Found GeometryInstance
                        var instanceGeometry = geomInstance.GetInstanceGeometry();
                        foreach (var instanceGeom in instanceGeometry)
                        {
                            if (instanceGeom is Solid instanceSolid && instanceSolid.Volume > 0)
                            {
                                // Found instance solid
                                return instanceSolid;
                            }
                        }
                    }
                }

                // No valid standard solid
                return null;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error in standard geometry extraction: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create solid from wall location line and parameters (for walls with openings)
        /// </summary>
        private Solid CreateRegularWallSolidFromLocation(Wall wall, Action<string> writeToLog = null)
        {
            try
            {
                // Creating solid from location line
                
                // Get the wall's location line
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve == null || locationCurve.Curve == null)
                {
                    // No location curve
                    return null;
                }

                var curve = locationCurve.Curve;
                // Curve info

                // Get wall parameters
                var wallHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
                var wallWidth = wall.WallType.Width;
                var wallThickness = wallWidth; // wall thickness

                // Wall dims

                // Create a rectangular profile from the wall location line
                var profile = CreateRectangularProfileFromCurve(curve, wallHeight, wallThickness, writeToLog);
                if (profile == null)
                {
                    return null;
                }

                // Create solid by extruding the profile
                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, wallHeight);
                
                // Created location-based solid
                return solid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating location-based solid: {ex.Message}");
                // Error creating location-based solid for wall
                return null;
            }
        }

        /// <summary>
        /// Create a rectangular profile from a curve for regular walls
        /// </summary>
        private CurveLoop CreateRectangularProfileFromCurve(Curve curve, double height, double thickness, Action<string> writeToLog = null)
        {
            try
            {
                // Get curve direction and perpendicular direction
                var curveDirection = curve.GetEndPoint(1) - curve.GetEndPoint(0);
                curveDirection = curveDirection.Normalize();
                
                var perpendicular = XYZ.BasisZ.CrossProduct(curveDirection).Normalize();
                var halfThickness = thickness / 2.0;

                // Create four corners of the rectangle
                var startPoint = curve.GetEndPoint(0);
                var endPoint = curve.GetEndPoint(1);

                var p1 = startPoint + perpendicular * halfThickness;
                var p2 = startPoint - perpendicular * halfThickness;
                var p3 = endPoint - perpendicular * halfThickness;
                var p4 = endPoint + perpendicular * halfThickness;



                // Create the rectangular loop
                var profile = new CurveLoop();
                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p4));
                profile.Append(Line.CreateBound(p4, p1));


                return profile;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"        ✗ Error creating rectangular profile from curve: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a simple solid for curtain walls using their location line
        /// </summary>
        private Solid CreateCurtainWallSolid(Wall wall, Action<string> writeToLog = null)
        {
            try
            {

                
                // Get the wall's location line
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve == null || locationCurve.Curve == null)
                {
                    writeToLog?.Invoke($"      ✗ No location curve found for curtain wall: {wall.Id}");
                    return null;
                }

                var curve = locationCurve.Curve;


                // Get wall height and thickness
                var wallHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
                var wallThickness = 0.02; // 2cm thickness for curtain wall solid



                // Create a simple rectangular profile
                var profile = CreateRectangularProfile(curve, wallHeight, wallThickness, writeToLog);
                if (profile == null)
                {
                    writeToLog?.Invoke($"      ✗ Failed to create profile for curtain wall: {wall.Id}");
                    return null;
                }

                // Create solid by extruding the profile
                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, wallHeight);
                

                return solid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating curtain wall solid: {ex.Message}");
                // Error creating curtain wall solid for wall
                return null;
            }
        }

        /// <summary>
        /// Create a rectangular profile for curtain wall solid
        /// </summary>
        private CurveLoop CreateRectangularProfile(Curve curve, double height, double thickness, Action<string> writeToLog = null)
        {
            try
            {

                
                // Get curve direction and perpendicular direction
                var curveDirection = curve.GetEndPoint(1) - curve.GetEndPoint(0);
                curveDirection = curveDirection.Normalize();
                
                var perpendicular = XYZ.BasisZ.CrossProduct(curveDirection).Normalize();
                var halfThickness = thickness / 2.0;

                // Create four corners of the rectangle
                var startPoint = curve.GetEndPoint(0);
                var endPoint = curve.GetEndPoint(1);

                var p1 = startPoint + perpendicular * halfThickness;
                var p2 = startPoint - perpendicular * halfThickness;
                var p3 = endPoint - perpendicular * halfThickness;
                var p4 = endPoint + perpendicular * halfThickness;



                // Create the rectangular loop
                var profile = new CurveLoop();
                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p4));
                profile.Append(Line.CreateBound(p4, p1));


                return profile;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"        ✗ Error creating rectangular profile: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Result of wall processing
    /// </summary>
    public class WallProcessingResult
    {
        public Dictionary<Wall, Solid> CurtainWallSolids { get; set; } = new Dictionary<Wall, Solid>();
        public List<Wall> RegularWalls { get; set; } = new List<Wall>();
    }
}
