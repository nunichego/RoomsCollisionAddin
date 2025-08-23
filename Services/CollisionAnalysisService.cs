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
                var elementBox = wall.get_BoundingBox(null);
                if (elementBox != null)
                {
                    wallBoundingBoxes[wall] = elementBox;
                }
            }
            
            writeToLog($"✓ Pre-calculated {wallBoundingBoxes.Count} wall bounding boxes");
            
            writeToLog("");
            
            // PHASE 3 OPTIMIZATION: Pre-process all room solids
            writeToLog("=== PHASE 3: PRE-PROCESSING ALL ROOM SOLIDS ===");
            showProgress("Pre-processing Rooms", "Creating room solids...", 0, rooms.Count, 15, 100);
            var sharedSegc = new SpatialElementGeometryCalculator(document);
            
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
                    var roomSolid = _roomProcessingService.GetRoomSolid(room, sharedSegc, writeToLog);
                    if (roomSolid != null)
                    {
                        var expandedSolids = _roomProcessingService.CreateExpandedRoomSolids(roomSolid, writeToLog);
                        var roomBoundingBox = CalculateRoomBoundingBox(room, document, writeToLog);
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


                        // Check wall collisions using bounding box pre-filtering
                        var collidingWalls = new List<Wall>();
                        var wallTypes = new HashSet<string>();

                                                 // Check all walls using optimized filtering (Z-axis -> BoundingBox -> Solid)
                        // Minimal logging for wall checks
                         int wallCheckIndex = 0;
                         int zAxisHits = 0;
                         int boundingBoxHits = 0;
                         int solidIntersectionTests = 0;
                        

                        
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
                                 
                                // Skip per-wall debug details
                                 
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
                         writeToLog($"    Actual collisions: {collidingWalls.Count}");

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
        /// Calculate room bounding box from the Room element with a small buffer
        /// </summary>
        private BoundingBoxXYZ CalculateRoomBoundingBox(Room room, Document document, Action<string> writeToLog = null)
        {
            if (room == null)
                return null;

            var originalBoundingBox = room.get_BoundingBox(null);
            if (originalBoundingBox == null)
                return null;

            // Add a small buffer (1cm) in project units
            var bufferInFeet = 1.0 / 30.48; // 1 cm in feet
            var bufferInProjectUnits = UnitUtils.Convert(bufferInFeet, UnitTypeId.Feet, document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());

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
