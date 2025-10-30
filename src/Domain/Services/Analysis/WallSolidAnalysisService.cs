using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Core.Exceptions;
using RoomsManagerAddin.Domain.Models.Analysis;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Services.Processing;
using RoomsManagerAddin.Infrastructure.Progress;
using RoomsManagerAddin.Infrastructure.RevitApi;

namespace RoomsManagerAddin.Domain.Services.Analysis
{
    /// <summary>
    /// Service for analyzing room-wall relationships using solid intersection
    /// OPTIMIZED: Uses diagonal (XY-plane) solid expansion for wall collision detection
    /// </summary>
    public class WallSolidAnalysisService : IWallSolidAnalysisService
    {
        private readonly IParameterMappingExecutionService _parameterMappingExecutionService;
        private readonly IGeometryService _geometryService;
        private readonly IRoomProcessingService _roomProcessingService;

        public WallSolidAnalysisService(
            IParameterMappingExecutionService parameterMappingExecutionService,
            IGeometryService geometryService,
            IRoomProcessingService roomProcessingService)
        {
            _parameterMappingExecutionService = parameterMappingExecutionService;
            _geometryService = geometryService;
            _roomProcessingService = roomProcessingService;
        }

        /// <summary>
        /// Analyze room-wall relationships using solid intersection with diagonal expansion
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
            var wallRoomRelationships = new Dictionary<ElementId, List<Room>>(); // Track which rooms each wall intersects
            var wallIdToWallMap = new Dictionary<ElementId, Wall>(); // Map wall ID to wall object
            var roomElementRelationships = new Dictionary<Room, List<Element>>(); // Track which elements each room intersects

            writeToLog("=== WALL SOLID-BASED ANALYSIS ===");
            writeToLog("Using solid-based collision detection with diagonal room expansion (2cm) for walls");

            // Validate parameter mappings before starting analysis
            if (!_parameterMappingExecutionService.ValidateAllMappings(parameterMappings))
            {
                writeToLog("⚠ No valid parameter mappings configured - analysis will complete but no parameters will be updated");
            }

            // PHASE 1: Pre-process all wall solids
            writeToLog("=== PHASE 1: PRE-PROCESSING WALL SOLIDS ===");
            progressReporter.ReportProgress(
                "Wall Analysis (Solid)",
                "Pre-processing Walls",
                $"Creating wall solids...",
                0.0,
                0.05);

            var wallSolids = new Dictionary<Wall, Solid>();
            var wallBoundingBoxes = new Dictionary<Wall, BoundingBoxXYZ>();

            int wallIndex = 0;
            foreach (var wall in walls)
            {
                wallIndex++;
                try
                {
                    var wallSolid = GetWallSolid(wall, writeToLog);
                    if (wallSolid != null && wallSolid.Volume > 0)
                    {
                        wallSolids[wall] = wallSolid;

                        var wallBBox = wall.get_BoundingBox(null);
                        if (wallBBox != null)
                        {
                            wallBoundingBoxes[wall] = wallBBox;
                        }
                    }
                }
                catch (Exception ex)
                {
                    writeToLog($"  ✗ Failed to process wall {wall.Id}: {ex.Message}");
                }
            }

            writeToLog($"✓ Pre-processed {wallSolids.Count} wall solids");

            // PHASE 2: Pre-process all room solids with diagonal expansion
            writeToLog("=== PHASE 2: PRE-PROCESSING ROOM SOLIDS ===");
            progressReporter.ReportProgress(
                "Wall Analysis (Solid)",
                "Pre-processing Rooms",
                $"Creating room solids with diagonal expansion (2cm)...",
                0.0,
                0.1);

            var sharedSegc = new SpatialElementGeometryCalculator(document);
            var roomSolids = new Dictionary<Room, (Solid Original, List<Solid> ExpandedDiagonal, BoundingBoxXYZ BoundingBox)>();

            int roomPreprocessIndex = 0;
            foreach (var room in rooms)
            {
                roomPreprocessIndex++;
                try
                {
                    var roomSolid = _roomProcessingService.GetRoomSolid(room, sharedSegc, writeToLog);
                    if (roomSolid != null && roomSolid.Volume > 0)
                    {
                        // Create diagonally expanded solids (XY-plane for wall detection)
                        var expandedSolids = CreateDiagonallyExpandedRoomSolids(roomSolid, writeToLog);
                        var roomBBox = CalculateRoomBoundingBox(room, document, writeToLog);

                        roomSolids[room] = (roomSolid, expandedSolids, roomBBox);
                    }
                }
                catch (Exception ex)
                {
                    writeToLog($"  ✗ Failed to pre-process room {room.Number}: {ex.Message}");
                }
            }

            writeToLog($"✓ Pre-processed {roomSolids.Count} room solids with diagonal expansion");

            using (var transaction = new Transaction(document, "Update Room and Wall Parameters - Wall Solid Analysis"))
            {
                transaction.Start();

                // PHASE 3: Collision detection
                writeToLog("=== PHASE 3: COLLISION DETECTION ===");

                int roomIndex = 0;
                foreach (var room in rooms)
                {
                    roomIndex++;
                    var overallProgress = 0.1 + ((double)roomIndex / rooms.Count * 0.7); // 10% to 80%
                    progressReporter.ReportProgress(
                        "Wall Analysis (Solid)",
                        "Analyzing Room-Wall Collisions",
                        $"Processing room {roomIndex}/{rooms.Count}: {room.Number} - {room.Name}",
                        (double)roomIndex / rooms.Count,
                        overallProgress);

                    try
                    {
                        var result = new RoomCollisionResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            Level = room.Level?.Name ?? "Unknown"
                        };

                        // Get pre-processed room data
                        if (!roomSolids.ContainsKey(room))
                        {
                            writeToLog($"  ✗ Room {room.Number} not found in pre-processed data");
                            result.ErrorMessage = "Room not found in pre-processed data";
                            results.Add(result);
                            continue;
                        }

                        var (roomSolid, expandedRoomSolids, roomBoundingBox) = roomSolids[room];
                        result.RoomSolidVolume = roomSolid.Volume;
                        result.RoomSolidFaces = roomSolid.Faces.Size;

                        // Check wall collisions using bounding box pre-filtering
                        var collidingWalls = new List<Wall>();
                        var wallTypes = new HashSet<string>();

                        int wallCheckIndex = 0;
                        int zAxisFiltered = 0;
                        int boundingBoxHits = 0;
                        int solidIntersectionTests = 0;

                        foreach (var wallSolidData in wallSolids)
                        {
                            var wall = wallSolidData.Key;
                            var wallSolid = wallSolidData.Value;

                            wallCheckIndex++;

                            try
                            {
                                // 1. FASTEST: Z-axis check (level filtering)
                                if (wallBoundingBoxes.ContainsKey(wall) && roomBoundingBox != null)
                                {
                                    var wallBBox = wallBoundingBoxes[wall];

                                    // Check if they're on completely different levels
                                    if (wallBBox.Max.Z < roomBoundingBox.Min.Z || wallBBox.Min.Z > roomBoundingBox.Max.Z)
                                    {
                                        zAxisFiltered++;
                                        continue;
                                    }
                                }

                                // 2. FAST: Bounding box check
                                if (wallBoundingBoxes.ContainsKey(wall) && roomBoundingBox != null)
                                {
                                    var wallBBox = wallBoundingBoxes[wall];
                                    bool boundingBoxIntersects = BoundingBoxesIntersect(roomBoundingBox, wallBBox);

                                    if (!boundingBoxIntersects)
                                    {
                                        continue;
                                    }

                                    boundingBoxHits++;
                                }

                                // 3. EXPENSIVE: Solid-solid intersection
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
                                    if (!wallRoomRelationships.ContainsKey(wall.Id))
                                    {
                                        wallRoomRelationships[wall.Id] = new List<Room>();
                                    }
                                    wallRoomRelationships[wall.Id].Add(room);

                                    // Keep reference to wall object
                                    wallIdToWallMap[wall.Id] = wall;
                                }
                            }
                            catch (Exception ex)
                            {
                                writeToLog($"    Error checking wall collision: {ex.Message}");
                            }
                        }

                        writeToLog($"  Room {room.Number}: {collidingWalls.Count} walls detected (Z-filtered: {zAxisFiltered}, BBox hits: {boundingBoxHits}, Solid tests: {solidIntersectionTests})");

                        result.WallsColliding = collidingWalls.Count;
                        result.WallTypes = wallTypes.ToList();

                        // Collect room-to-element relationships for batch processing later
                        var relatedWalls = collidingWalls.Cast<Element>().ToList();
                        roomElementRelationships[room] = relatedWalls;

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
            writeToLog($"=== WALL SOLID ANALYSIS SUMMARY ===");
            writeToLog($"Processed {rooms.Count} rooms using solid intersection with diagonal expansion");
            writeToLog($"Updated parameters for {wallRoomRelationships.Count} walls");

            var roomsWithWalls = results.Count(r => r.WallsColliding > 0);
            var roomsWithoutWalls = results.Count(r => r.WallsColliding == 0);
            var totalWallCollisions = results.Sum(r => r.WallsColliding);

            writeToLog($"");
            writeToLog($"=== DETAILED ANALYSIS RESULTS ===");
            writeToLog($"✓ Rooms with wall intersections: {roomsWithWalls}/{rooms.Count}");
            writeToLog($"⚠ Rooms without wall intersections: {roomsWithoutWalls}/{rooms.Count}");
            writeToLog($"📊 Total room-wall relationships: {totalWallCollisions}");
            writeToLog($"🔲 Unique walls with room intersections: {wallRoomRelationships.Count}");

            return results;
        }

        /// <summary>
        /// Get solid geometry from a wall
        /// </summary>
        private Solid GetWallSolid(Wall wall, Action<string> writeToLog)
        {
            try
            {
                var options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                var geometryElement = wall.get_Geometry(options);
                if (geometryElement != null)
                {
                    foreach (var geomObject in geometryElement)
                    {
                        if (geomObject is Solid solid && solid.Volume > 0)
                        {
                            return solid;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"  ✗ Error getting wall solid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create diagonally expanded room solids for wall collision detection
        /// Expands room solid along XY-plane diagonals (2cm offset)
        /// </summary>
        private List<Solid> CreateDiagonallyExpandedRoomSolids(Solid roomSolid, Action<string> writeToLog)
        {
            try
            {
                var expandedSolids = new List<Solid>();

                // Create diagonal offset copies (2cm in diagonal directions)
                var offsetDistance = 2.0 / 30.48; // 2cm in feet (USER REQUESTED 2cm instead of default 0.5cm)

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

                return expandedSolids;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"    ✗ Error creating diagonally expanded room solids: {ex.Message}");
                return new List<Solid> { roomSolid }; // Return original solid as fallback
            }
        }

        /// <summary>
        /// Create an offset solid by moving the original solid
        /// </summary>
        private Solid CreateOffsetSolid(Solid originalSolid, double offsetDistance, XYZ direction, Action<string> writeToLog)
        {
            try
            {
                var translation = Transform.CreateTranslation(direction * offsetDistance);
                var offsetSolid = SolidUtils.CreateTransformed(originalSolid, translation);
                return offsetSolid;
            }
            catch (Exception ex)
            {
                writeToLog?.Invoke($"      ✗ Error creating offset solid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate room bounding box with a small buffer
        /// </summary>
        private BoundingBoxXYZ CalculateRoomBoundingBox(Room room, Document document, Action<string> writeToLog)
        {
            if (room == null)
                return null;

            var originalBoundingBox = room.get_BoundingBox(null);
            if (originalBoundingBox == null)
                return null;

            // Add a small buffer (2cm) in project units
            var bufferInFeet = 2.0 / 30.48; // 2cm in feet
            var bufferInProjectUnits = UnitUtils.Convert(bufferInFeet, UnitTypeId.Feet,
                document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());

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
