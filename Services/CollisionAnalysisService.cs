using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Microsoft.Extensions.Logging;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for analyzing collisions between rooms and other elements
    /// </summary>
    public class CollisionAnalysisService
    {
        private readonly ILogger _logger;
        private readonly GeometryService _geometryService;
        private readonly ParameterUpdateService _parameterService;
        private readonly WallProcessingService _wallProcessingService;
        private readonly RoomProcessingService _roomProcessingService;

        public CollisionAnalysisService(ILogger logger, GeometryService geometryService, ParameterUpdateService parameterService, WallProcessingService wallProcessingService, RoomProcessingService roomProcessingService)
        {
            _logger = logger;
            _geometryService = geometryService;
            _parameterService = parameterService;
            _wallProcessingService = wallProcessingService;
            _roomProcessingService = roomProcessingService;
        }

        /// <summary>
        /// Analyze room collisions with walls and floors
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document, 
            List<Room> rooms, 
            List<Wall> walls, 
            Action<string> writeToLog,
            Action<string, string, int, int, int, int> showProgress)
        {
            var results = new List<RoomCollisionResult>();
            var wallCollisionData = new Dictionary<Wall, List<string>>(); // Track which rooms each wall collides with

            // PHASE 1 OPTIMIZATION: Pre-process all wall solids once
            writeToLog("=== PHASE 1: PRE-PROCESSING ALL WALL SOLIDS ===");
            showProgress("Pre-processing Walls", "Creating wall solids...", 0, walls.Count, 5, 100);
            
            var allWallSolids = new Dictionary<Wall, Solid>();
            var wallProcessingResult = _wallProcessingService.ProcessWalls(walls, writeToLog);
            
            // Add curtain wall solids (already created)
            foreach (var curtainWallData in wallProcessingResult.CurtainWallSolids)
            {
                allWallSolids[curtainWallData.Key] = curtainWallData.Value;
            }
            
            // Pre-process regular wall solids
            var regularWalls = wallProcessingResult.RegularWalls;
            int wallIndex = 0;
            foreach (var wall in regularWalls)
            {
                wallIndex++;
                if (wallIndex % 50 == 0) // Update progress every 50 walls
                {
                    showProgress("Pre-processing Walls", $"Creating wall solids ({wallIndex}/{regularWalls.Count})...", 
                                wallIndex, regularWalls.Count, 5, 100);
                }
                
                var wallSolid = _wallProcessingService.GetRegularWallSolid(wall, writeToLog);
                if (wallSolid != null)
                {
                    allWallSolids[wall] = wallSolid;
                }
            }
            
            writeToLog($"✓ Pre-processed {allWallSolids.Count} wall solids ({wallProcessingResult.CurtainWallSolids.Count} curtain walls, {allWallSolids.Count - wallProcessingResult.CurtainWallSolids.Count} regular walls)");
            
            // PHASE 2 OPTIMIZATION: Pre-calculate wall bounding boxes
            writeToLog("=== PHASE 2: PRE-CALCULATING WALL BOUNDING BOXES ===");
            showProgress("Pre-calculating Bounding Boxes", "Calculating wall bounding boxes...", 0, allWallSolids.Count, 10, 100);
            
            var wallBoundingBoxes = new Dictionary<Wall, BoundingBoxXYZ>();
            int bboxIndex = 0;
            foreach (var wallSolidData in allWallSolids)
            {
                bboxIndex++;
                if (bboxIndex % 100 == 0) // Update progress every 100 walls
                {
                    showProgress("Pre-calculating Bounding Boxes", $"Calculating bounding boxes ({bboxIndex}/{allWallSolids.Count})...", 
                                bboxIndex, allWallSolids.Count, 10, 100);
                }
                
                var wall = wallSolidData.Key;
                var wallSolid = wallSolidData.Value;
                wallBoundingBoxes[wall] = wallSolid.GetBoundingBox();
            }
            
            writeToLog($"✓ Pre-calculated {wallBoundingBoxes.Count} wall bounding boxes");
            
            // Log some bounding box statistics for debugging
            var wallBoundingBoxSizes = wallBoundingBoxes.Values.Select(bbox => 
                Math.Sqrt(Math.Pow(bbox.Max.X - bbox.Min.X, 2) + 
                         Math.Pow(bbox.Max.Y - bbox.Min.Y, 2) + 
                         Math.Pow(bbox.Max.Z - bbox.Min.Z, 2))).ToList();
            
            writeToLog($"Wall bounding box statistics: Min size={wallBoundingBoxSizes.Min():F2}, Max size={wallBoundingBoxSizes.Max():F2}, Avg size={wallBoundingBoxSizes.Average():F2}");
            writeToLog("");
            
            // PHASE 3 OPTIMIZATION: Pre-process all room solids
            writeToLog("=== PHASE 3: PRE-PROCESSING ALL ROOM SOLIDS ===");
            showProgress("Pre-processing Rooms", "Creating room solids...", 0, rooms.Count, 15, 100);
            
            var allRoomSolids = new Dictionary<Room, (Solid Original, List<Solid> Expanded, BoundingBoxXYZ BoundingBox)>();
            int roomPreprocessIndex = 0;
            foreach (var room in rooms)
            {
                roomPreprocessIndex++;
                if (roomPreprocessIndex % 10 == 0) // Update progress every 10 rooms
                {
                    showProgress("Pre-processing Rooms", $"Creating room solids ({roomPreprocessIndex}/{rooms.Count})...", 
                                roomPreprocessIndex, rooms.Count, 15, 100);
                }
                
                try
                {
                    var roomSolid = _roomProcessingService.GetRoomSolid(room, writeToLog);
                    if (roomSolid != null)
                    {
                        // DEBUG: Log the original room solid bounding box
                        var originalBoundingBox = roomSolid.GetBoundingBox();
                        writeToLog($"  *** DEBUG: Room {room.Number} original bounding box: Min({originalBoundingBox.Min.X:F3},{originalBoundingBox.Min.Y:F3},{originalBoundingBox.Min.Z:F3}) Max({originalBoundingBox.Max.X:F3},{originalBoundingBox.Max.Y:F3},{originalBoundingBox.Max.Z:F3})");
                        writeToLog($"  *** DEBUG: Room {room.Number} original size: {Math.Sqrt(Math.Pow(originalBoundingBox.Max.X - originalBoundingBox.Min.X, 2) + Math.Pow(originalBoundingBox.Max.Y - originalBoundingBox.Min.Y, 2) + Math.Pow(originalBoundingBox.Max.Z - originalBoundingBox.Min.Z, 2)):F3} units");
                        
                        var expandedSolids = _roomProcessingService.CreateExpandedRoomSolids(roomSolid, writeToLog);
                        var roomBoundingBox = CalculateRoomBoundingBox(roomSolid, document, writeToLog);
                        allRoomSolids[room] = (roomSolid, expandedSolids, roomBoundingBox);
                    }
                }
                catch (Exception ex)
                {
                    writeToLog($"  ✗ Failed to pre-process room {room.Number}: {ex.Message}");
                }
            }
            
            writeToLog($"✓ Pre-processed {allRoomSolids.Count} room solids");
            writeToLog("");
            
            using (var transaction = new Transaction(document, "Update Room and Wall Filter Tags"))
            {
                transaction.Start();

                int roomIndex = 0;
                foreach (var room in rooms)
                {
                    roomIndex++;
                    showProgress("Analyzing Rooms", $"Processing room {roomIndex}/{rooms.Count}: {room.Number} - {room.Name}", 
                                1, 1, 20 + (int)((double)roomIndex / rooms.Count * 60), 100);
                    
                    writeToLog($"--- ANALYZING ROOM: {room.Number} - {room.Name} ---");
                    
                    try
                    {
                        var result = new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            Level = room.Level?.Name ?? "Unknown"
                        };

                        // Get pre-processed room data
                        if (!allRoomSolids.ContainsKey(room))
                        {
                            writeToLog($"  ✗ Room not found in pre-processed data");
                            result.ErrorMessage = "Room not found in pre-processed data";
                            results.Add(result);
                            continue;
                        }

                        var (roomSolid, expandedRoomSolids, roomBoundingBox) = allRoomSolids[room];
                        
                        result.RoomSolidVolume = roomSolid.Volume;
                        result.RoomSolidFaces = roomSolid.Faces.Size;
                                               writeToLog($"  Room solid: Volume={roomSolid.Volume:F1}, Faces={roomSolid.Faces.Size}, Expanded solids={expandedRoomSolids.Count}");

                        // Check wall collisions using bounding box pre-filtering
                        var collidingWalls = new List<Wall>();
                        var wallTypes = new HashSet<string>();

                                                 // Check all walls using optimized filtering (Z-axis -> BoundingBox -> Solid)
                         writeToLog($"  Checking {allWallSolids.Count} walls...");
                         int wallCheckIndex = 0;
                         int zAxisHits = 0;
                         int boundingBoxHits = 0;
                         int solidIntersectionTests = 0;
                        
                                               // DEBUG: Examine wall solids and their bounding boxes
                       var firstFewWalls = allWallSolids.Take(5).ToList();
                       writeToLog($"  *** DEBUG: Examining first 5 wall solids:");
                       foreach (var wallData in firstFewWalls)
                       {
                           var wall = wallData.Key;
                           var wallSolid = wallData.Value;
                           var wallBoundingBox = wallBoundingBoxes[wall];
                           
                           writeToLog($"    *** Wall {wall.Id}:");
                           writeToLog($"      Solid Volume: {wallSolid.Volume:F2}, Faces: {wallSolid.Faces.Size}");
                           writeToLog($"      Solid BoundingBox: Min({wallSolid.GetBoundingBox().Min.X:F3},{wallSolid.GetBoundingBox().Min.Y:F3},{wallSolid.GetBoundingBox().Min.Z:F3}) Max({wallSolid.GetBoundingBox().Max.X:F3},{wallSolid.GetBoundingBox().Max.Y:F3},{wallSolid.GetBoundingBox().Max.Z:F3})");
                           writeToLog($"      Stored BoundingBox: Min({wallBoundingBox.Min.X:F3},{wallBoundingBox.Min.Y:F3},{wallBoundingBox.Min.Z:F3}) Max({wallBoundingBox.Max.X:F3},{wallBoundingBox.Max.Y:F3},{wallBoundingBox.Max.Z:F3})");
                           
                           // Calculate dimensions
                           var solidBox = wallSolid.GetBoundingBox();
                           var solidWidth = solidBox.Max.X - solidBox.Min.X;
                           var solidHeight = solidBox.Max.Y - solidBox.Min.Y;
                           var solidDepth = solidBox.Max.Z - solidBox.Min.Z;
                           writeToLog($"      Solid Dimensions: Width={solidWidth:F3}, Height={solidHeight:F3}, Depth={solidDepth:F3}");
                       }
                        
                                                 foreach (var wallSolidData in allWallSolids)
                         {
                             var wall = wallSolidData.Key;
                             var wallSolid = wallSolidData.Value;
                             var wallBoundingBox = wallBoundingBoxes[wall];
                             
                             wallCheckIndex++;
                             if (wallCheckIndex % 100 == 0) // Update progress every 100 walls
                             {
                                 showProgress("Analyzing Rooms", $"Checking walls for room {room.Number} ({wallCheckIndex}/{allWallSolids.Count})", 
                                             wallCheckIndex, allWallSolids.Count, 15 + (int)((double)roomIndex / rooms.Count * 60), 100);
                             }
                             
                             try
                             {
                                 // 1. FASTEST: Z-axis check first (walls on different levels)
                                 bool zAxisIntersects = (roomBoundingBox.Min.Z <= wallBoundingBox.Max.Z && 
                                                        roomBoundingBox.Max.Z >= wallBoundingBox.Min.Z);
                                 
                                 if (!zAxisIntersects)
                                 {
                                     // Wall is completely above or below room - skip immediately
                                     continue;
                                 }
                                 
                                 zAxisHits++;
                                 
                                 // 2. SECOND FASTEST: X,Y bounding box check
                                 bool boundingBoxIntersects = BoundingBoxesIntersect(roomBoundingBox, wallBoundingBox);
                                 
                                 // DEBUG: Log first few intersection checks
                                 if (wallCheckIndex <= 5)
                                 {
                                     writeToLog($"    *** DEBUG: Wall {wall.Id} - Z-axis: {zAxisIntersects}, BoundingBox: {boundingBoxIntersects}");
                                 }
                                 
                                 if (boundingBoxIntersects)
                                 {
                                     boundingBoxHits++;
                                     
                                     // 3. MOST EXPENSIVE: Solid-solid intersection (only if both fast checks pass)
                                     bool hasCollision = false;
                                     foreach (var expandedSolid in expandedRoomSolids)
                                     {
                                         solidIntersectionTests++;
                                         if (_geometryService.SolidsIntersect(expandedSolid, wallSolid))
                                         {
                                             hasCollision = true;
                                             break;
                                         }
                                     }
                                     
                                     if (hasCollision)
                                     {
                                         collidingWalls.Add(wall);
                                         var wallType = wall.WallType?.Name ?? "Unknown";
                                         wallTypes.Add(wallType);

                                         // Track this collision for wall parameter update
                                         if (!wallCollisionData.ContainsKey(wall))
                                         {
                                             wallCollisionData[wall] = new List<string>();
                                         }
                                         wallCollisionData[wall].Add($"{room.Number} - {room.Name}");
                                     }
                                 }
                             }
                             catch (Exception ex)
                             {
                                 writeToLog($"    Error checking wall collision: {ex.Message}");
                             }
                         }
                        

                                                 writeToLog($"    Z-axis hits: {zAxisHits}/{allWallSolids.Count} ({(double)zAxisHits / allWallSolids.Count * 100:F1}%)");
                         writeToLog($"    Bounding box hits: {boundingBoxHits}/{zAxisHits} ({(zAxisHits > 0 ? (double)boundingBoxHits / zAxisHits * 100 : 0):F1}%)");
                         writeToLog($"    Solid intersection tests: {solidIntersectionTests}");
                         writeToLog($"    Actual collisions: {collidingWalls.Count} ({(boundingBoxHits > 0 ? (double)collidingWalls.Count / boundingBoxHits * 100 : 0):F1}% precision)");

                        result.WallsColliding = collidingWalls.Count;
                        result.WallTypes = wallTypes.ToList();

                        // Update room Filter Tag parameter
                        var filterTagValue = string.Join(", ", wallTypes);
                        _parameterService.UpdateRoomFilterTag(room, filterTagValue);
                        writeToLog($"    Updated Room Filter Tag: {filterTagValue}");

                        writeToLog($"  Summary: {collidingWalls.Count} walls colliding");
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        writeToLog($"  ✗ ERROR analyzing room {room.Name}: {ex.Message}");
                        _logger?.LogError(ex, $"Error analyzing room: {room.Name}");
                        results.Add(new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Update wall Filter Tag parameters with room collision information
                showProgress("Updating Parameters", "Updating wall Filter Tag parameters...", 0, wallCollisionData.Count, 85, 100);
                
                int wallUpdateIndex = 0;
                foreach (var wallData in wallCollisionData)
                {
                    wallUpdateIndex++;
                    showProgress("Updating Parameters", $"Updating wall {wallUpdateIndex}/{wallCollisionData.Count}...", 
                                wallUpdateIndex, wallCollisionData.Count, 85, 100);
                    
                    var wall = wallData.Key;
                    var collidingRooms = wallData.Value;
                    var wallFilterTagValue = string.Join("; ", collidingRooms);
                    _parameterService.UpdateWallFilterTag(wall, wallFilterTagValue);
                    writeToLog($"  Wall {wall.Id}: Updated Filter Tag with {collidingRooms.Count} rooms");
                }

                transaction.Commit();
            }
            
            return results;
        }

        /// <summary>
        /// Calculate bounding box from original room solid with proper unit conversion
        /// </summary>
        private BoundingBoxXYZ CalculateRoomBoundingBox(Solid roomSolid, Document document, Action<string> writeToLog = null)
        {
            if (roomSolid == null)
                return null;

            var originalBoundingBox = roomSolid.GetBoundingBox();
            
            // DEBUG: Log the original bounding box details
            writeToLog?.Invoke($"    *** DEBUG: Original room solid bounding box: Min({originalBoundingBox.Min.X:F3},{originalBoundingBox.Min.Y:F3},{originalBoundingBox.Min.Z:F3}) Max({originalBoundingBox.Max.X:F3},{originalBoundingBox.Max.Y:F3},{originalBoundingBox.Max.Z:F3})");
            writeToLog?.Invoke($"    *** DEBUG: Original room solid size: {Math.Sqrt(Math.Pow(originalBoundingBox.Max.X - originalBoundingBox.Min.X, 2) + Math.Pow(originalBoundingBox.Max.Y - originalBoundingBox.Min.Y, 2) + Math.Pow(originalBoundingBox.Max.Z - originalBoundingBox.Min.Z, 2)):F3} units");
            
            // Convert 0.5cm to project units (Revit internal units are feet)
            // 0.5cm = 0.5 / 30.48 feet ≈ 0.0164 feet
            var offsetInFeet = 0.5 / 30.48; // Convert cm to feet
            var offsetInProjectUnits = UnitUtils.Convert(offsetInFeet, UnitTypeId.Feet, document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());
            
            // Calculate center point of the bounding box
            var centerX = (originalBoundingBox.Min.X + originalBoundingBox.Max.X) / 2.0;
            var centerY = (originalBoundingBox.Min.Y + originalBoundingBox.Max.Y) / 2.0;
            var centerZ = (originalBoundingBox.Min.Z + originalBoundingBox.Max.Z) / 2.0;
            
            // Calculate half dimensions
            var halfWidth = (originalBoundingBox.Max.X - originalBoundingBox.Min.X) / 2.0;
            var halfHeight = (originalBoundingBox.Max.Y - originalBoundingBox.Min.Y) / 2.0;
            var halfDepth = (originalBoundingBox.Max.Z - originalBoundingBox.Min.Z) / 2.0;
            
            // Add a small buffer (1cm in project units) to account for the expanded solids
            var bufferInFeet = 1.0 / 30.48; // 1cm in feet
            var bufferInProjectUnits = UnitUtils.Convert(bufferInFeet, UnitTypeId.Feet, document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());
            
            writeToLog?.Invoke($"    *** DEBUG: Buffer in project units: {bufferInProjectUnits:F6}");
            
            // Create bounding box with small buffer
            var boundedBox = new BoundingBoxXYZ
            {
                Min = new XYZ(
                    originalBoundingBox.Min.X - bufferInProjectUnits,
                    originalBoundingBox.Min.Y - bufferInProjectUnits,
                    originalBoundingBox.Min.Z - bufferInProjectUnits
                ),
                Max = new XYZ(
                    originalBoundingBox.Max.X + bufferInProjectUnits,
                    originalBoundingBox.Max.Y + bufferInProjectUnits,
                    originalBoundingBox.Max.Z + bufferInProjectUnits
                )
            };
            
            writeToLog?.Invoke($"    *** DEBUG: Final bounding box: Min({boundedBox.Min.X:F3},{boundedBox.Min.Y:F3},{boundedBox.Min.Z:F3}) Max({boundedBox.Max.X:F3},{boundedBox.Max.Y:F3},{boundedBox.Max.Z:F3})");
            writeToLog?.Invoke($"    *** DEBUG: Final bounding box size: {Math.Sqrt(Math.Pow(boundedBox.Max.X - boundedBox.Min.X, 2) + Math.Pow(boundedBox.Max.Y - boundedBox.Min.Y, 2) + Math.Pow(boundedBox.Max.Z - boundedBox.Min.Z, 2)):F3} units");
            
            return boundedBox;
        }

        /// <summary>
        /// Check if two bounding boxes intersect
        /// </summary>
        private bool BoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            return (box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X) &&
                   (box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y) &&
                   (box1.Min.Z <= box2.Max.Z && box1.Max.Z >= box2.Min.Z);
        }
    }
}
