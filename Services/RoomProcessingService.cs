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
                    writeToLog?.Invoke($"Processing room: {room.Id} ({room.Number} - {room.Name})");
                    
                    var roomSolid = GetRoomSolid(room, writeToLog);
                    if (roomSolid != null)
                    {
                        result.SuccessfulRooms[room] = roomSolid;
                        writeToLog?.Invoke($"  ✓ Created room solid: Volume={roomSolid.Volume:F2}, Faces={roomSolid.Faces.Size}");
                    }
                    else
                    {
                        result.FailedRooms.Add(room);
                        writeToLog?.Invoke($"  ✗ Failed to create room solid");
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
        /// Get solid geometry from a room
        /// </summary>
        public Solid GetRoomSolid(Room room, Action<string> writeToLog = null)
        {
            try
            {
                writeToLog?.Invoke($"  Getting solid for room: {room.Id}");
                
                var options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                var geometryElement = room.get_Geometry(options);
                if (geometryElement == null)
                {
                    writeToLog?.Invoke($"    ✗ No geometry found for room {room.Id}");
                    return null;
                }

                foreach (var geomObject in geometryElement)
                {
                    if (geomObject is Solid solid && solid.Volume > 0)
                    {
                        writeToLog?.Invoke($"    ✓ Found solid: Volume={solid.Volume:F2}, Faces={solid.Faces.Size}");
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
                                writeToLog?.Invoke($"    ✓ Found instance solid: Volume={instanceSolid.Volume:F2}, Faces={instanceSolid.Faces.Size}");
                                return instanceSolid;
                            }
                        }
                    }
                }

                writeToLog?.Invoke($"    ✗ No valid solid found for room {room.Id}");
                return null;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error getting solid for room {room.Id}: {ex.Message}");
                _logger?.LogError(ex, $"Error getting solid for room: {room.Id}");
                return null;
            }
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
               
               writeToLog?.Invoke($"    Created {expandedSolids.Count} expanded solids");
                
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
                
                writeToLog?.Invoke($"      Created offset solid in direction {direction}: Volume={offsetSolid.Volume:F2}");
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
                
                writeToLog?.Invoke($"--- Testing Room: {room.Id} ({room.Number} - {room.Name}) ---");
                
                // Test basic solid creation
                var solid = GetRoomSolid(room, writeToLog);
                roomStopwatch.Stop();
                
                if (solid != null)
                {
                    result.SuccessfulRooms++;
                    writeToLog?.Invoke($"✓ SUCCESS: Room {room.Id} - Volume: {solid.Volume:F2}, Faces: {solid.Faces.Size}, Time: {roomStopwatch.ElapsedMilliseconds}ms");
                    
                    // Test expanded solids creation
                    var expandedStopwatch = Stopwatch.StartNew();
                    var expandedSolids = CreateExpandedRoomSolids(solid, writeToLog);
                    expandedStopwatch.Stop();
                    
                    writeToLog?.Invoke($"  Expanded solids: {expandedSolids.Count} solids created in {expandedStopwatch.ElapsedMilliseconds}ms");
                    result.TotalExpandedSolids += expandedSolids.Count;
                }
                else
                {
                    result.FailedRooms++;
                    writeToLog?.Invoke($"✗ FAILED: Room {room.Id} - No solid created, Time: {roomStopwatch.ElapsedMilliseconds}ms");
                }
                
                result.TotalProcessingTime += roomStopwatch.ElapsedMilliseconds;
                writeToLog?.Invoke("");
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
