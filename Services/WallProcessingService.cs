using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for processing wall geometry and creating solids
    /// </summary>
    public class WallProcessingService
    {
        private readonly ILogger _logger;

        public WallProcessingService(ILogger logger)
        {
            _logger = logger;
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
                        writeToLog?.Invoke($"Processing curtain wall: {wall.Id} ({wall.WallType.Name})");
                        var curtainSolid = CreateCurtainWallSolid(wall, writeToLog);
                        if (curtainSolid != null)
                        {
                            result.CurtainWallSolids[wall] = curtainSolid;
                            writeToLog?.Invoke($"  ✓ Created curtain wall solid: Volume={curtainSolid.Volume:F2}");
                        }
                        else
                        {
                            writeToLog?.Invoke($"  ✗ Failed to create curtain wall solid");
                        }
                    }
                    else
                    {
                        result.RegularWalls.Add(wall);
                        writeToLog?.Invoke($"Regular wall: {wall.Id} ({wall.WallType.Name})");
                    }
                }
                catch (Exception ex)
                {
                    writeToLog?.Invoke($"  ✗ Error processing wall {wall.Id}: {ex.Message}");
                    _logger?.LogError(ex, $"Error processing wall: {wall.Id}");
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
                writeToLog?.Invoke($"  Getting solid for regular wall: {wall.Id}");
                
                // First, try to get the solid using standard geometry extraction
                var standardSolid = GetStandardWallSolid(wall, writeToLog);
                if (standardSolid != null)
                {
                    return standardSolid;
                }

                // If standard extraction fails, create solid from wall location line
                writeToLog?.Invoke($"    Standard geometry extraction failed, creating solid from location line...");
                return CreateRegularWallSolidFromLocation(wall, writeToLog);
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error getting solid for wall {wall.Id}: {ex.Message}");
                _logger?.LogError(ex, $"Error getting solid for wall: {wall.Id}");
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
                    writeToLog?.Invoke($"    ✗ No geometry found for wall {wall.Id}");
                    return null;
                }

                foreach (var geomObject in geometryElement)
                {
                    if (geomObject is Solid solid && solid.Volume > 0)
                    {
                        writeToLog?.Invoke($"    Standard solid: Volume={solid.Volume:F2}, Faces={solid.Faces.Size}");
                        return solid;
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        writeToLog?.Invoke($"    Found GeometryInstance, checking instance geometry...");
                        var instanceGeometry = geomInstance.GetInstanceGeometry();
                        foreach (var instanceGeom in instanceGeometry)
                        {
                            if (instanceGeom is Solid instanceSolid && instanceSolid.Volume > 0)
                            {
                                writeToLog?.Invoke($"    Instance solid: Volume={instanceSolid.Volume:F2}, Faces={instanceSolid.Faces.Size}");
                                return instanceSolid;
                            }
                        }
                    }
                }

                writeToLog?.Invoke($"    ✗ No valid standard solid found for wall {wall.Id}");
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
                writeToLog?.Invoke($"    Creating solid from location line for wall: {wall.Id}");
                
                // Get the wall's location line
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve == null || locationCurve.Curve == null)
                {
                    writeToLog?.Invoke($"      ✗ No location curve found for wall: {wall.Id}");
                    return null;
                }

                var curve = locationCurve.Curve;
                writeToLog?.Invoke($"      Curve type: {curve.GetType().Name}, Length: {curve.Length:F2}");

                // Get wall parameters
                var wallHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
                var wallWidth = wall.WallType.Width;
                var wallThickness = wallWidth; // Use wall type width as thickness

                writeToLog?.Invoke($"      Wall height: {wallHeight:F2}, Width: {wallWidth:F2}");

                // Create a rectangular profile from the wall location line
                var profile = CreateRectangularProfileFromCurve(curve, wallHeight, wallThickness, writeToLog);
                if (profile == null)
                {
                    writeToLog?.Invoke($"      ✗ Failed to create profile for wall: {wall.Id}");
                    return null;
                }

                // Create solid by extruding the profile
                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, wallHeight);
                
                writeToLog?.Invoke($"      Location-based solid: Volume={solid.Volume:F2}, Faces={solid.Faces.Size}");
                return solid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating location-based solid: {ex.Message}");
                _logger?.LogError(ex, $"Error creating location-based solid for wall: {wall.Id}");
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
                writeToLog?.Invoke($"        Creating rectangular profile from curve...");
                
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

                writeToLog?.Invoke($"        Profile points: P1({p1.X:F2},{p1.Y:F2},{p1.Z:F2}) -> P4({p4.X:F2},{p4.Y:F2},{p4.Z:F2})");

                // Create the rectangular loop
                var profile = new CurveLoop();
                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p4));
                profile.Append(Line.CreateBound(p4, p1));

                writeToLog?.Invoke($"        ✓ Profile created successfully");
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
                writeToLog?.Invoke($"    Creating curtain wall solid for: {wall.Id}");
                
                // Get the wall's location line
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve == null || locationCurve.Curve == null)
                {
                    writeToLog?.Invoke($"      ✗ No location curve found for curtain wall: {wall.Id}");
                    return null;
                }

                var curve = locationCurve.Curve;
                writeToLog?.Invoke($"      Curve type: {curve.GetType().Name}, Length: {curve.Length:F2}");

                // Get wall height and thickness
                var wallHeight = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 10.0;
                var wallThickness = 0.02; // 2cm thickness for curtain wall solid

                writeToLog?.Invoke($"      Wall height: {wallHeight:F2}, Thickness: {wallThickness:F2}");

                // Create a simple rectangular profile
                var profile = CreateRectangularProfile(curve, wallHeight, wallThickness, writeToLog);
                if (profile == null)
                {
                    writeToLog?.Invoke($"      ✗ Failed to create profile for curtain wall: {wall.Id}");
                    return null;
                }

                // Create solid by extruding the profile
                var solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, wallHeight);
                
                writeToLog?.Invoke($"      ✓ Created curtain wall solid: Volume={solid.Volume:F2}, Faces={solid.Faces.Size}");
                return solid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating curtain wall solid: {ex.Message}");
                _logger?.LogError(ex, $"Error creating curtain wall solid for wall: {wall.Id}");
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
                writeToLog?.Invoke($"        Creating rectangular profile...");
                
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

                writeToLog?.Invoke($"        Profile points: P1({p1.X:F2},{p1.Y:F2},{p1.Z:F2}) -> P4({p4.X:F2},{p4.Y:F2},{p4.Z:F2})");

                // Create the rectangular loop
                var profile = new CurveLoop();
                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p4));
                profile.Append(Line.CreateBound(p4, p1));

                writeToLog?.Invoke($"        ✓ Profile created successfully");
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
