using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for processing room geometry and creating solids
    /// </summary>
    public class RoomProcessingService
    {
        private readonly ILogger _logger;

        public RoomProcessingService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process all rooms and create their solids
        /// </summary>
        public RoomProcessingResult ProcessRooms(List<Room> rooms, Action<string> writeToLog)
        {
            var result = new RoomProcessingResult();
            
            writeToLog?.Invoke("=== ROOM PROCESSING STARTED ===");
            
            foreach (var room in rooms)
            {
                try
                {
                    var roomSolid = GetRoomSolid(room, writeToLog);
                    if (roomSolid != null)
                    {
                        result.SuccessfulRooms[room] = roomSolid;
                    }
                    else
                    {
                        result.FailedRooms.Add(room);
                    }
                }
                catch (Exception ex)
                {
                    writeToLog?.Invoke($"  ✗ Error processing room {room.Id}: {ex.Message}");
                    _logger?.LogError(ex, $"Error processing room: {room.Id}");
                    result.FailedRooms.Add(room);
                }
            }
            
            writeToLog?.Invoke($"=== ROOM PROCESSING COMPLETED ===");
            writeToLog?.Invoke($"Summary: {result.SuccessfulRooms.Count} rooms processed successfully, {result.FailedRooms.Count} failed");
            writeToLog?.Invoke("");
            
            return result;
        }

        /// <summary>
        /// Get solid geometry from a room. If a SpatialElementGeometryCalculator is provided,
        /// reuse it for performance; otherwise falls back to legacy display geometry.
        /// </summary>
        public Solid GetRoomSolid(Room room, SpatialElementGeometryCalculator calculator, Action<string> writeToLog = null)
        {
            try
            {
                // Minimal per-room logging

                // Preferred: accurate SEGC solid
                try
                {
                    if (calculator == null)
                    {
                        writeToLog?.Invoke($"    WARNING: Creating new SEGC for room {room.Id} - shared calculator is null!");
                        var localCalculator = new SpatialElementGeometryCalculator(room.Document);
                        var results = localCalculator.CalculateSpatialElementGeometry(room);
                        var segcSolid = results?.GetGeometry();
                        if (segcSolid != null && segcSolid.Volume > 0)
                        {
                            return segcSolid;
                        }
                    }
                    else
                    {
                        // Use shared SEGC (optimal)
                        var results = calculator.CalculateSpatialElementGeometry(room);
                        var segcSolid = results?.GetGeometry();
                        if (segcSolid != null && segcSolid.Volume > 0)
                        {
                            return segcSolid;
                        }
                    }
                }
                catch
                {
                    // Fall back to legacy geometry below
                }

                // Fallback: legacy display geometry (fast, less robust)
                var options = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Medium };
                var geometryElement = room.get_Geometry(options);
                if (geometryElement == null)
                {
                    return null;
                }

                foreach (var geomObject in geometryElement)
                {
                    if (geomObject is Solid solid && solid.Volume > 0)
                    {
                        return solid;
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        var instanceGeometry = geomInstance.GetInstanceGeometry();
                        foreach (var instanceGeom in instanceGeometry)
                        {
                            if (instanceGeom is Solid instanceSolid && instanceSolid.Volume > 0)
                            {
                                return instanceSolid;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error getting solid for room {room.Id}: {ex.Message}");
                _logger?.LogError(ex, $"Error getting solid for room: {room.Id}");
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
                _logger?.LogError(ex, "Error creating expanded room solids");
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

        /// <summary>
        /// Test room solid creation performance
        /// </summary>
        public RoomPerformanceResult TestRoomSolidCreation(List<Room> rooms, Action<string> writeToLog)
        {
            var result = new RoomPerformanceResult();
            
            writeToLog?.Invoke("=== ROOM SOLID CREATION PERFORMANCE TEST ===");
            
            var totalStopwatch = Stopwatch.StartNew();
            
            foreach (var room in rooms)
            {
                var roomStopwatch = Stopwatch.StartNew();
                
                // Test basic solid creation
                var solid = GetRoomSolid(room, writeToLog);
                roomStopwatch.Stop();
                
                if (solid != null)
                {
                    result.SuccessfulRooms++;
                    
                    // Test expanded solids creation
                    var expandedStopwatch = Stopwatch.StartNew();
                    var expandedSolids = CreateExpandedRoomSolids(solid, writeToLog);
                    expandedStopwatch.Stop();
                    
                    result.TotalExpandedSolids += expandedSolids.Count;
                }
                else
                {
                    result.FailedRooms++;
                }
                
                result.TotalProcessingTime += roomStopwatch.ElapsedMilliseconds;
            }
            
            totalStopwatch.Stop();
            result.TotalTime = totalStopwatch.ElapsedMilliseconds;
            
            writeToLog?.Invoke($"=== ROOM PERFORMANCE SUMMARY ===");
            writeToLog?.Invoke($"Total rooms: {rooms.Count}");
            writeToLog?.Invoke($"Successful: {result.SuccessfulRooms}");
            writeToLog?.Invoke($"Failed: {result.FailedRooms}");
            writeToLog?.Invoke($"Success rate: {(double)result.SuccessfulRooms / rooms.Count * 100:F1}%");
            writeToLog?.Invoke($"Total processing time: {result.TotalTime}ms");
            writeToLog?.Invoke($"Average time per room: {result.TotalTime / rooms.Count:F1}ms");
            writeToLog?.Invoke($"Total expanded solids created: {result.TotalExpandedSolids}");
            writeToLog?.Invoke($"Average expanded solids per room: {(double)result.TotalExpandedSolids / result.SuccessfulRooms:F1}");
            writeToLog?.Invoke("");
            
            return result;
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

    /// <summary>
    /// Result of room performance testing
    /// </summary>
    public class RoomPerformanceResult
    {
        public int SuccessfulRooms { get; set; }
        public int FailedRooms { get; set; }
        public long TotalProcessingTime { get; set; }
        public long TotalTime { get; set; }
        public int TotalExpandedSolids { get; set; }
    }
}
