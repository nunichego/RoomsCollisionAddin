using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Domain.Models.Analysis;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Services.Processing;
using RoomsManagerAddin.Infrastructure.Progress;

namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing room-wall relationships using Revit's Room Boundary API
    /// OPTIMIZED: Uses native room boundary detection instead of solid intersection
    /// </summary>
    public class WallBoundaryAnalysisService : IWallBoundaryAnalysisService
    {
        private readonly ParameterMappingExecutionService _parameterMappingExecutionService;
        
        public WallBoundaryAnalysisService(ParameterMappingExecutionService parameterMappingExecutionService)
        {
            _parameterMappingExecutionService = parameterMappingExecutionService;
        }

        /// <summary>
        /// Analyze room-wall relationships using Room Boundary API with parameter mappings
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document, 
            List<Room> rooms, 
            List<Wall> walls, 
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter)
        {
            var results = new List<RoomCollisionResult>();
            var wallRoomRelationships = new Dictionary<ElementId, List<Room>>(); // Track which rooms each wall bounds
            var wallIdToWallMap = new Dictionary<ElementId, Wall>(); // Map wall ID to wall object
            var roomElementRelationships = new Dictionary<Room, List<Element>>(); // Track which elements each room bounds (for batch processing)

            writeToLog("=== WALL BOUNDARY ANALYSIS (Room Boundary API) ===");
            writeToLog("Using Revit's native room boundary detection for maximum performance and accuracy");
            
            // Validate parameter mappings before starting analysis
            if (!_parameterMappingExecutionService.ValidateAllMappings(parameterMappings))
            {
                writeToLog("⚠ No valid parameter mappings configured - analysis will complete but no parameters will be updated");
            }
            
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
                    var overallProgress = 0.1 + ((double)roomIndex / rooms.Count * 0.7); // 10% to 80%
                    progressReporter.ReportProgress(
                        "Room-Wall Analysis",
                        "Analyzing Room Boundaries", 
                        $"Processing room {roomIndex}/{rooms.Count}: {room.Number} - {room.Name}",
                        (double)roomIndex / rooms.Count,
                        overallProgress);
                    
                    // Process room boundaries (detailed logging removed for performance)
                    
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

                            // Process boundary walls for this room (no per-wall logging)
                            foreach (var wall in roomWalls)
                            {
                                var wallType = wall.WallType?.Name ?? "Unknown";
                                // Fix: Use ID comparison instead of object reference comparison
                                var inAnalysisSet = walls.Any(w => w.Id.Equals(wall.Id));
                                
                                // Only count walls that are in our analysis set
                                if (inAnalysisSet)
                                {
                                    boundaryWallsCount++;
                                    wallTypes.Add(wallType);

                                    // Track this relationship for parameter mapping
                                    if (!wallRoomRelationships.ContainsKey(wall.Id))
                                    {
                                        wallRoomRelationships[wall.Id] = new List<Room>();
                                    }
                                    wallRoomRelationships[wall.Id].Add(room);
                                    
                                    // Keep reference to wall object
                                    wallIdToWallMap[wall.Id] = wall;
                                }
                            }

                            result.WallsColliding = boundaryWallsCount;
                            result.WallTypes = wallTypes.ToList();

                            // Collect room-to-element relationships for batch processing later
                            var relatedWalls = roomWalls.Where(w => walls.Any(analysisWall => analysisWall.Id.Equals(w.Id))).Cast<Element>().ToList();
                            roomElementRelationships[room] = relatedWalls;
                            
                            // Summary logging only
                            // Room processed successfully (detailed logging removed)
                        }
                        else
                        {
                            // Room has no boundary walls (detailed logging removed)
                            
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
                            // Could not get room geometry (detailed logging removed)
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        // Error analyzing room (detailed logging removed)
                        results.Add(new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Set up progress callback for parameter mapping service
                _parameterMappingExecutionService.SetProgressReporter(progressReporter.CreateSubReporter(0.8, 1.0));

                // Execute room-to-element parameter mappings in batch (first)
                _parameterMappingExecutionService.ExecuteRoomToElementMappingsBatch(roomElementRelationships, parameterMappings);

                // Execute element-to-room parameter mappings in batch (second)
                _parameterMappingExecutionService.ExecuteElementToRoomMappings(
                    wallRoomRelationships, 
                    wallIdToWallMap.ToDictionary(kvp => kvp.Key, kvp => (Element)kvp.Value), 
                    parameterMappings);

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
                // Getting boundary segments (detailed logging removed)
                var boundarySegments = room.GetBoundarySegments(options);
                
                if (boundarySegments == null)
                {
                    // GetBoundarySegments returned null (detailed logging removed)
                    return boundaryWalls;
                }
                
                if (boundarySegments.Count == 0)
                {
                    // GetBoundarySegments returned empty list (detailed logging removed)
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
                                
                                // Segment processed (detailed logging removed)
                                
                                if (element is Wall wall)
                                {
                                    wallSegments++;
                                    // Avoid duplicates - a wall might appear in multiple segments
                                    if (!boundaryWalls.Any(w => w.Id.Equals(wall.Id)))
                                    {
                                        boundaryWalls.Add(wall);
                                        // Wall added to boundary list (detailed logging removed)
                                    }
                                    else
                                    {
                                        // Wall already in list (duplicate segment, detailed logging removed)
                                    }
                                }
                                else
                                {
                                    nonWallSegments++;
                                    // Non-wall boundary element (detailed logging removed)
                                }
                            }
                            else
                            {
                                // Could not get element for segment (detailed logging removed)
                            }
                        }
                        catch (Exception ex)
                        {
                            // Could not process boundary segment (detailed logging removed)
                        }
                    }
                }
                
                // Summary logging removed for performance
            }
            catch (Exception ex)
            {
                // Error getting boundary segments (detailed logging removed)
            }
            
            return boundaryWalls;
        }
    }
}