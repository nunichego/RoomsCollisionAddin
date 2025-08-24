using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;


namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for processing room geometry and creating solids
    /// </summary>
    public class RoomProcessingService
    {
        public RoomProcessingService()
        {
        }



        /// <summary>
        /// Get solid geometry from a room. If a SpatialElementGeometryCalculator is provided,
        /// reuse it for performance; otherwise falls back to legacy display geometry.
        /// </summary>
        public Solid GetRoomSolid(Room room, SpatialElementGeometryCalculator calculator, Action<string> writeToLog = null)
        {
            try
            {
                // Try fast geometry extraction first (like old code)
                try
                {
                    var options = new Options
                    {
                        ComputeReferences = true,
                        DetailLevel = ViewDetailLevel.Fine
                    };

                    var geometryElement = room.get_Geometry(options);
                    if (geometryElement != null)
                    {
                        foreach (var geomObject in geometryElement)
                        {
                            if (geomObject is Solid solid && solid.Volume > 0)
                            {
                                writeToLog?.Invoke($"    ✓ Fast geometry success for room {room.Id}");
                                return solid; // Fast success!
                            }
                            else if (geomObject is GeometryInstance geomInstance)
                            {
                                var instanceGeometry = geomInstance.GetInstanceGeometry();
                                foreach (var instanceGeom in instanceGeometry)
                                {
                                    if (instanceGeom is Solid instanceSolid && instanceSolid.Volume > 0)
                                    {
                                        writeToLog?.Invoke($"    ✓ Fast geometry success (instance) for room {room.Id}");
                                        return instanceSolid; // Fast success!
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    writeToLog?.Invoke($"    ⚠ Fast geometry failed for room {room.Id}: {ex.Message}");
                }

                // Fallback: accurate but slow SEGC (only if fast method fails)
                try
                {
                    if (calculator != null)
                    {
                        writeToLog?.Invoke($"    → Falling back to SEGC for room {room.Id}");
                        var results = calculator.CalculateSpatialElementGeometry(room);
                        var segcSolid = results?.GetGeometry();
                        if (segcSolid != null && segcSolid.Volume > 0)
                        {
                            writeToLog?.Invoke($"    ✓ SEGC fallback success for room {room.Id}");
                            return segcSolid;
                        }
                    }
                }
                catch (Exception ex)
                {
                    writeToLog?.Invoke($"    ✗ SEGC fallback failed for room {room.Id}: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error getting solid for room {room.Id}: {ex.Message}");
                // Error getting solid for room
                return null;
            }
        }

        // Back-compat overload
        public Solid GetRoomSolid(Room room, Action<string> writeToLog = null)
        {
            return GetRoomSolid(room, null, writeToLog);
        }

        /// <summary>
        /// Create expanded room solids for better collision detection
        /// Optimized for walls: only 2 diagonal directions, no original solid
        /// </summary>
        public List<Solid> CreateExpandedRoomSolids(Solid roomSolid, Action<string> writeToLog = null)
        {
            try
            {
                               var expandedSolids = new List<Solid>();
               
               // Create only 2 diagonal offset copies (0.5cm in diagonal directions)
               var offsetDistance = 0.5 / 30.48; // 0.5cm in feet
               
               // Create diagonal directions: +X+Y and -X-Y
               var diagonal1 = (XYZ.BasisX + XYZ.BasisY).Normalize();
               var diagonal2 = (-XYZ.BasisX - XYZ.BasisY).Normalize();
               
               var directions = new[] { diagonal1, diagonal2 };
               
               foreach (var direction in directions)
               {
                   var offsetSolid = CreateOffsetSolid(roomSolid, offsetDistance, direction, writeToLog);
                   if (offsetSolid != null)
                   {
                       expandedSolids.Add(offsetSolid);
                   }
               }
               
               // Minimal logging
                
                return expandedSolids;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error creating expanded room solids: {ex.Message}");
                // Error creating expanded room solids
                return new List<Solid> { roomSolid }; // Return original solid as fallback
            }
        }

        /// <summary>
        /// Create an offset solid by moving the original solid
        /// </summary>
        private Solid CreateOffsetSolid(Solid originalSolid, double offsetDistance, XYZ direction, Action<string> writeToLog = null)
        {
            try
            {
                var translation = Transform.CreateTranslation(direction * offsetDistance);
                var offsetSolid = SolidUtils.CreateTransformed(originalSolid, translation);
                
                // Minimal logging
                return offsetSolid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating offset solid: {ex.Message}");
                return null;
            }
        }


    }

    /// <summary>
    /// Result of room processing
    /// </summary>
    public class RoomProcessingResult
    {
        public Dictionary<Room, Solid> SuccessfulRooms { get; set; } = new Dictionary<Room, Solid>();
        public List<Room> FailedRooms { get; set; } = new List<Room>();
    }


}
