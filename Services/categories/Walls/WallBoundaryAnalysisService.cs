using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services.Categories.Walls
{
    /// <summary>
    /// Service for analyzing room-wall relationships using Revit's Room Boundary API
    /// OPTIMIZED: Uses native room boundary detection instead of solid intersection
    /// </summary>
    public class WallBoundaryAnalysisService
    {
        private readonly ParameterUpdateService _parameterService;
        
        public WallBoundaryAnalysisService(ParameterUpdateService parameterService)
        {
            _parameterService = parameterService;
        }

        /// <summary>
        /// Analyze room-wall relationships using Room Boundary API
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document, 
            List<Room> rooms, 
            List<Wall> walls, 
            Action<string> writeToLog,
            Action<string, string, int, int, int, int> showProgress)
        {
            var results = new List<RoomCollisionResult>();
            var wallRoomRelationships = new Dictionary<ElementId, List<string>>(); // Track which rooms each wall bounds
            var wallIdToWallMap = new Dictionary<ElementId, Wall>(); // Map wall ID to wall object

            writeToLog("=== WALL BOUNDARY ANALYSIS (Room Boundary API) ===");
            writeToLog("Using Revit's native room boundary detection for maximum performance and accuracy");
            
            // Configure boundary options - using Finish for most accurate room space analysis
            var boundaryOptions = new SpatialElementBoundaryOptions
            {
                StoreFreeBoundaryFaces = true,
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };
            writeToLog("✓ Configured boundary options: SpatialElementBoundaryLocation.Finish");

            using (var transaction = new Transaction(document, "Update Room and Wall Filter Tags - Boundary Analysis"))
            {
                transaction.Start();

                int roomIndex = 0;
                foreach (var room in rooms)
                {
                    roomIndex++;
                    showProgress("Analyzing Room Boundaries", 
                                $"Processing room {roomIndex}/{rooms.Count}: {room.Number} - {room.Name}", 
                                roomIndex, rooms.Count, 
                                10 + (int)((double)roomIndex / rooms.Count * 70), 100);
                    
                    writeToLog($"--- ANALYZING ROOM BOUNDARIES: {room.Number} - {room.Name} ---");
                    
                    try
                    {
                        var result = new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            Level = room.Level?.Name ?? "Unknown"
                        };

                        // Get room boundary segments using Room Boundary API
                        var roomWalls = GetRoomBoundaryWalls(room, boundaryOptions, writeToLog);
                        
                        if (roomWalls != null && roomWalls.Any())
                        {
                            // Collect wall types for this room
                            var wallTypes = new HashSet<string>();
                            var boundaryWallsCount = 0;

                            writeToLog($"    → Analyzing {roomWalls.Count} boundary walls for room {room.Number}");
                            writeToLog($"    → Total walls in analysis set: {walls.Count}");
                            
                            // DEBUG: Show first few wall IDs from analysis set for comparison
                            var firstFewWallIds = walls.Take(5).Select(w => w.Id.Value.ToString()).ToList();
                            writeToLog($"    → First 5 wall IDs in analysis set: {string.Join(", ", firstFewWallIds)}");
                            
                            foreach (var wall in roomWalls)
                            {
                                var wallType = wall.WallType?.Name ?? "Unknown";
                                // Fix: Use ID comparison instead of object reference comparison
                                var inAnalysisSet = walls.Any(w => w.Id.Equals(wall.Id));
                                writeToLog($"      • Boundary Wall {wall.Id}: {wallType} - In analysis set: {inAnalysisSet}");
                                
                                // Only count walls that are in our analysis set
                                if (inAnalysisSet)
                                {
                                    boundaryWallsCount++;
                                    wallTypes.Add(wallType);

                                    // Track this relationship for wall parameter update
                                    if (!wallRoomRelationships.ContainsKey(wall.Id))
                                    {
                                        wallRoomRelationships[wall.Id] = new List<string>();
                                    }
                                    wallRoomRelationships[wall.Id].Add($"{room.Number} - {room.Name}");
                                    
                                    // Keep reference to wall object
                                    wallIdToWallMap[wall.Id] = wall;
                                }
                            }

                            result.WallsColliding = boundaryWallsCount;
                            result.WallTypes = wallTypes.ToList();

                            // Update room Filter Tag parameter
                            var filterTagValue = string.Join(", ", wallTypes);
                            _parameterService.UpdateRoomFilterTag(room, filterTagValue);
                            
                            writeToLog($"    ✓ Found {roomWalls.Count} boundary walls ({boundaryWallsCount} in analysis set)");
                            writeToLog($"    ✓ Wall types: {filterTagValue}");
                            writeToLog($"    ✓ Updated Room Filter Tag: {filterTagValue}");
                        }
                        else
                        {
                            writeToLog($"    ⚠ No boundary walls found for room {room.Number} - {room.Name}");
                            writeToLog($"      → This could indicate:");
                            writeToLog($"        • Room is unbounded or invalid");
                            writeToLog($"        • Room boundaries are non-wall elements (room separation lines, etc.)"); 
                            writeToLog($"        • Room spans multiple levels");
                            writeToLog($"        • Room calculation issues");
                            
                            result.WallsColliding = 0;
                            result.WallTypes = new List<string>();
                        }

                        // Set room geometry info (optional - for compatibility with existing UI)
                        try
                        {
                            var roomGeometry = room.get_Geometry(new Options());
                            if (roomGeometry != null)
                            {
                                foreach (var geomObject in roomGeometry)
                                {
                                    if (geomObject is Solid solid && solid.Volume > 0)
                                    {
                                        result.RoomSolidVolume = solid.Volume;
                                        result.RoomSolidFaces = solid.Faces.Size;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            writeToLog($"    ⚠ Could not get room geometry info: {ex.Message}");
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        writeToLog($"  ✗ ERROR analyzing room {room.Name}: {ex.Message}");
                        results.Add(new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Update wall Filter Tag parameters with room boundary information
                showProgress("Updating Wall Parameters", "Updating wall Filter Tag parameters...", 
                            0, wallRoomRelationships.Count, 85, 100);
                
                writeToLog($"=== UPDATING WALL PARAMETERS ===");
                writeToLog($"Updating Filter Tag for {wallRoomRelationships.Count} walls with boundary room information");
                
                // DEBUG: Analyze wall-room relationships for inconsistencies
                foreach (var wallData in wallRoomRelationships)
                {
                    var wallId = wallData.Key;
                    var boundaryRooms = wallData.Value;
                    var wall = wallIdToWallMap[wallId];
                    
                    if (boundaryRooms.Count == 1)
                    {
                        writeToLog($"    ⚠ SINGLE BOUNDARY: Wall {wallId} ({wall.WallType?.Name}) bounds only 1 room: {boundaryRooms[0]}");
                        
                        // Check if wall has "Room Bounding" enabled
                        var roomBoundingParam = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                        var isRoomBounding = roomBoundingParam?.AsInteger() == 1;
                        writeToLog($"      → Room Bounding enabled: {isRoomBounding}");
                        
                        // Check wall function
                        var wallFunction = wall.WallType?.Function;
                        writeToLog($"      → Wall Function: {wallFunction}");
                    }
                    else if (boundaryRooms.Count > 2)
                    {
                        writeToLog($"    ℹ MULTI BOUNDARY: Wall {wallId} bounds {boundaryRooms.Count} rooms: {string.Join(", ", boundaryRooms)}");
                    }
                }
                
                int wallUpdateIndex = 0;
                foreach (var wallData in wallRoomRelationships)
                {
                    wallUpdateIndex++;
                    if (wallUpdateIndex % 10 == 0 || wallUpdateIndex == wallRoomRelationships.Count)
                    {
                        showProgress("Updating Wall Parameters", 
                                    $"Updating wall {wallUpdateIndex}/{wallRoomRelationships.Count}...", 
                                    wallUpdateIndex, wallRoomRelationships.Count, 85, 100);
                    }
                    
                    var wallId = wallData.Key;
                    var boundaryRooms = wallData.Value;
                    var wall = wallIdToWallMap[wallId];
                    var wallFilterTagValue = string.Join("; ", boundaryRooms);
                    
                    try
                    {
                        _parameterService.UpdateWallFilterTag(wall, wallFilterTagValue);
                        writeToLog($"    ✓ Wall {wallId}: {wallFilterTagValue}");
                    }
                    catch (Exception ex)
                    {
                        writeToLog($"    ✗ Failed to update wall {wallId}: {ex.Message}");
                    }
                }

                transaction.Commit();
                writeToLog("✓ Transaction committed successfully");
            }
            
            writeToLog("");
            writeToLog($"=== BOUNDARY ANALYSIS SUMMARY ===");
            writeToLog($"Processed {rooms.Count} rooms using Room Boundary API");
            writeToLog($"Updated parameters for {wallRoomRelationships.Count} walls");
            
            // ENHANCED DEBUG: Comprehensive analysis summary
            var roomsWithBoundaries = results.Count(r => r.WallsColliding > 0);
            var roomsWithoutBoundaries = results.Count(r => r.WallsColliding == 0);
            var totalWallBoundaries = results.Sum(r => r.WallsColliding);
            
            writeToLog($"");
            writeToLog($"=== DETAILED ANALYSIS RESULTS ===");
            writeToLog($"✓ Rooms with wall boundaries: {roomsWithBoundaries}/{rooms.Count}");
            writeToLog($"⚠ Rooms without wall boundaries: {roomsWithoutBoundaries}/{rooms.Count}");
            writeToLog($"📊 Total wall-room boundary relationships: {totalWallBoundaries}");
            writeToLog($"🧱 Unique walls with room boundaries: {wallRoomRelationships.Count}");
            
            // DEBUG: Analyze distribution of wall-room relationships
            var wallsWithSingleRoom = wallRoomRelationships.Count(kvp => kvp.Value.Count == 1);
            var wallsWithMultipleRooms = wallRoomRelationships.Count(kvp => kvp.Value.Count > 1);
            var maxRoomsPerWall = wallRoomRelationships.Any() ? wallRoomRelationships.Max(kvp => kvp.Value.Count) : 0;
            
            writeToLog($"");
            writeToLog($"=== WALL-ROOM RELATIONSHIP ANALYSIS ===");
            writeToLog($"🔸 Walls bounding only 1 room: {wallsWithSingleRoom} ({(double)wallsWithSingleRoom / wallRoomRelationships.Count * 100:F1}%)");
            writeToLog($"🔹 Walls bounding multiple rooms: {wallsWithMultipleRooms} ({(double)wallsWithMultipleRooms / wallRoomRelationships.Count * 100:F1}%)");
            writeToLog($"📈 Maximum rooms per wall: {maxRoomsPerWall}");
            
            // DEBUG: List rooms that had no wall boundaries
            if (roomsWithoutBoundaries > 0)
            {
                writeToLog($"");
                writeToLog($"=== ROOMS WITHOUT WALL BOUNDARIES ===");
                foreach (var result in results.Where(r => r.WallsColliding == 0))
                {
                    writeToLog($"❌ {result.RoomNumber} - {result.RoomName} (Level: {result.Level})");
                }
            }
            
            writeToLog("");
            writeToLog("Performance: Native boundary detection - no solid intersection required");
            
            return results;
        }

        /// <summary>
        /// Get walls that form the boundary of a room using Room Boundary API
        /// </summary>
        private List<Wall> GetRoomBoundaryWalls(Room room, SpatialElementBoundaryOptions options, Action<string> writeToLog)
        {
            var boundaryWalls = new List<Wall>();
            
            try
            {
                writeToLog($"    → Getting boundary segments for room {room.Number} - {room.Name}");
                var boundarySegments = room.GetBoundarySegments(options);
                
                if (boundarySegments == null)
                {
                    writeToLog($"    ⚠ GetBoundarySegments returned null for room {room.Number}");
                    return boundaryWalls;
                }
                
                if (boundarySegments.Count == 0)
                {
                    writeToLog($"    ⚠ GetBoundarySegments returned empty list for room {room.Number}");
                    return boundaryWalls;
                }

                int segmentListCount = 0;
                int totalSegments = 0;
                int wallSegments = 0;
                int nonWallSegments = 0;
                var elementTypeCounts = new Dictionary<string, int>();
                
                foreach (var segmentList in boundarySegments)
                {
                    segmentListCount++;
                    if (segmentList == null) continue;
                    
                    foreach (var segment in segmentList)
                    {
                        totalSegments++;
                        if (segment == null) continue;
                        
                        try
                        {
                            var element = room.Document.GetElement(segment.ElementId);
                            if (element != null)
                            {
                                var elementType = element.GetType().Name;
                                
                                // Track element type counts
                                if (!elementTypeCounts.ContainsKey(elementType))
                                {
                                    elementTypeCounts[elementType] = 0;
                                }
                                elementTypeCounts[elementType]++;
                                
                                writeToLog($"      → Segment {totalSegments}: Element {element.Id} ({elementType})");
                                
                                if (element is Wall wall)
                                {
                                    wallSegments++;
                                    // Avoid duplicates - a wall might appear in multiple segments
                                    if (!boundaryWalls.Any(w => w.Id.Equals(wall.Id)))
                                    {
                                        boundaryWalls.Add(wall);
                                        writeToLog($"        ✓ Added Wall {wall.Id}: {wall.WallType?.Name ?? "Unknown"}");
                                        
                                        // Check wall properties for debugging
                                        var roomBounding = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING)?.AsInteger() == 1;
                                        var wallFunction = wall.WallType?.Function.ToString() ?? "Unknown";
                                        writeToLog($"        → Room Bounding: {roomBounding}, Function: {wallFunction}");
                                    }
                                    else
                                    {
                                        writeToLog($"        ⟳ Wall {wall.Id} already in list (duplicate segment)");
                                    }
                                }
                                else
                                {
                                    nonWallSegments++;
                                    writeToLog($"        ⚠ Non-wall boundary element: {elementType}");
                                }
                            }
                            else
                            {
                                writeToLog($"      ⚠ Could not get element for segment {totalSegments}");
                            }
                        }
                        catch (Exception ex)
                        {
                            writeToLog($"      ⚠ Could not process boundary segment {totalSegments}: {ex.Message}");
                        }
                    }
                }
                
                writeToLog($"    → Processed {segmentListCount} boundary loops, {totalSegments} total segments");
                writeToLog($"    → Wall segments: {wallSegments}, Non-wall segments: {nonWallSegments}");
                writeToLog($"    → Found {boundaryWalls.Count} unique boundary walls");
                
                // DEBUG: Show boundary element type distribution
                if (elementTypeCounts.Any())
                {
                    writeToLog($"    → Boundary element types:");
                    foreach (var typeCount in elementTypeCounts.OrderByDescending(kvp => kvp.Value))
                    {
                        writeToLog($"        • {typeCount.Key}: {typeCount.Value} segments");
                    }
                }
                
                // Log wall details for debugging
                foreach (var wall in boundaryWalls)
                {
                    var wallType = wall.WallType?.Name ?? "Unknown";
                    writeToLog($"      • Wall {wall.Id}: {wallType}");
                }
            }
            catch (Exception ex)
            {
                writeToLog($"    ✗ Error getting boundary segments for room {room.Number}: {ex.Message}");
            }
            
            return boundaryWalls;
        }
    }
}