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
    /// Service for analyzing room-ceiling relationships using solid intersection
    /// OPTIMIZED: Uses vertical (Z-axis) solid expansion for ceiling collision detection
    /// </summary>
    public class CeilingBoundaryAnalysisService : ICeilingBoundaryAnalysisService
    {
        private readonly IParameterMappingExecutionService _parameterMappingExecutionService;
        private readonly IGeometryService _geometryService;
        private readonly IRoomProcessingService _roomProcessingService;

        public CeilingBoundaryAnalysisService(
            IParameterMappingExecutionService parameterMappingExecutionService,
            IGeometryService geometryService,
            IRoomProcessingService roomProcessingService)
        {
            _parameterMappingExecutionService = parameterMappingExecutionService;
            _geometryService = geometryService;
            _roomProcessingService = roomProcessingService;
        }

        /// <summary>
        /// Analyze room-ceiling relationships using solid intersection with Z-axis expansion
        /// </summary>
        public List<RoomCollisionResult> AnalyzeRoomCollisions(
            Document document,
            List<Room> rooms,
            List<Ceiling> ceilings,
            List<ParameterMappingConfiguration> parameterMappings,
            Action<string> writeToLog,
            ProgressReporter progressReporter)
        {
            var results = new List<RoomCollisionResult>();
            var ceilingRoomRelationships = new Dictionary<ElementId, List<Room>>(); // Track which rooms each ceiling intersects
            var ceilingIdToCeilingMap = new Dictionary<ElementId, Ceiling>(); // Map ceiling ID to ceiling object
            var roomElementRelationships = new Dictionary<Room, List<Element>>(); // Track which elements each room intersects

            writeToLog("=== CEILING BOUNDARY ANALYSIS (Solid Intersection) ===");
            writeToLog("Using solid-based collision detection with vertical room expansion for ceilings");

            // Validate parameter mappings before starting analysis
            if (!_parameterMappingExecutionService.ValidateAllMappings(parameterMappings))
            {
                writeToLog("⚠ No valid parameter mappings configured - analysis will complete but no parameters will be updated");
            }

            // PHASE 1: Pre-process all ceiling solids
            writeToLog("=== PHASE 1: PRE-PROCESSING CEILING SOLIDS ===");
            progressReporter.ReportProgress(
                "Ceiling Analysis",
                "Pre-processing Ceilings",
                $"Creating ceiling solids...",
                0.0,
                0.05);

            var ceilingSolids = new Dictionary<Ceiling, Solid>();
            var ceilingBoundingBoxes = new Dictionary<Ceiling, BoundingBoxXYZ>();

            int ceilingIndex = 0;
            foreach (var ceiling in ceilings)
            {
                ceilingIndex++;
                try
                {
                    var ceilingSolid = GetCeilingSolid(ceiling, writeToLog);
                    if (ceilingSolid != null && ceilingSolid.Volume > 0)
                    {
                        ceilingSolids[ceiling] = ceilingSolid;

                        var ceilingBBox = ceiling.get_BoundingBox(null);
                        if (ceilingBBox != null)
                        {
                            ceilingBoundingBoxes[ceiling] = ceilingBBox;
                        }
                    }
                }
                catch (Exception ex)
                {
                    writeToLog($"  ✗ Failed to process ceiling {ceiling.Id}: {ex.Message}");
                }
            }

            writeToLog($"✓ Pre-processed {ceilingSolids.Count} ceiling solids");

            // PHASE 2: Pre-process all room solids with vertical expansion
            writeToLog("=== PHASE 2: PRE-PROCESSING ROOM SOLIDS ===");
            progressReporter.ReportProgress(
                "Ceiling Analysis",
                "Pre-processing Rooms",
                $"Creating room solids with vertical expansion...",
                0.0,
                0.1);

            var sharedSegc = new SpatialElementGeometryCalculator(document);
            var roomSolids = new Dictionary<Room, (Solid Original, List<Solid> ExpandedVertical, BoundingBoxXYZ BoundingBox)>();

            int roomPreprocessIndex = 0;
            foreach (var room in rooms)
            {
                roomPreprocessIndex++;
                try
                {
                    var roomSolid = _roomProcessingService.GetRoomSolid(room, sharedSegc, writeToLog);
                    if (roomSolid != null && roomSolid.Volume > 0)
                    {
                        // Create vertically expanded solids (up and down for ceiling detection)
                        var expandedSolids = CreateVerticallyExpandedRoomSolids(roomSolid, writeToLog);
                        var roomBBox = CalculateRoomBoundingBox(room, document, writeToLog);

                        roomSolids[room] = (roomSolid, expandedSolids, roomBBox);
                    }
                }
                catch (Exception ex)
                {
                    writeToLog($"  ✗ Failed to pre-process room {room.Number}: {ex.Message}");
                }
            }

            writeToLog($"✓ Pre-processed {roomSolids.Count} room solids with vertical expansion");

            using (var transaction = new Transaction(document, "Update Room and Ceiling Parameters - Ceiling Analysis"))
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
                        "Ceiling Analysis",
                        "Analyzing Room-Ceiling Collisions",
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

                        // Check ceiling collisions using bounding box pre-filtering
                        var collidingCeilings = new List<Ceiling>();
                        var ceilingTypes = new HashSet<string>();

                        int ceilingCheckIndex = 0;
                        int boundingBoxHits = 0;
                        int solidIntersectionTests = 0;

                        foreach (var ceilingSolidData in ceilingSolids)
                        {
                            var ceiling = ceilingSolidData.Key;
                            var ceilingSolid = ceilingSolidData.Value;

                            ceilingCheckIndex++;

                            try
                            {
                                // 1. FASTEST: Bounding box check
                                if (ceilingBoundingBoxes.ContainsKey(ceiling))
                                {
                                    var ceilingBBox = ceilingBoundingBoxes[ceiling];
                                    bool boundingBoxIntersects = BoundingBoxesIntersect(roomBoundingBox, ceilingBBox);

                                    if (!boundingBoxIntersects)
                                    {
                                        continue;
                                    }

                                    boundingBoxHits++;
                                }

                                // 2. EXPENSIVE: Solid-solid intersection
                                bool hasCollision = false;
                                foreach (var expandedSolid in expandedRoomSolids)
                                {
                                    solidIntersectionTests++;
                                    if (_geometryService.SolidsIntersect(expandedSolid, ceilingSolid))
                                    {
                                        hasCollision = true;
                                        break;
                                    }
                                }

                                if (hasCollision)
                                {
                                    collidingCeilings.Add(ceiling);
                                    var ceilingType = ceiling.GetTypeId() != ElementId.InvalidElementId
                                        ? document.GetElement(ceiling.GetTypeId())?.Name ?? "Unknown"
                                        : "Unknown";
                                    ceilingTypes.Add(ceilingType);

                                    // Track this collision for ceiling parameter update
                                    if (!ceilingRoomRelationships.ContainsKey(ceiling.Id))
                                    {
                                        ceilingRoomRelationships[ceiling.Id] = new List<Room>();
                                    }
                                    ceilingRoomRelationships[ceiling.Id].Add(room);

                                    // Keep reference to ceiling object
                                    ceilingIdToCeilingMap[ceiling.Id] = ceiling;
                                }
                            }
                            catch (Exception ex)
                            {
                                writeToLog($"    Error checking ceiling collision: {ex.Message}");
                            }
                        }

                        writeToLog($"  Room {room.Number}: {collidingCeilings.Count} ceilings detected (BBox hits: {boundingBoxHits}, Solid tests: {solidIntersectionTests})");

                        result.WallsColliding = collidingCeilings.Count; // Reusing same field for count
                        result.WallTypes = ceilingTypes.ToList(); // Reusing same field for types

                        // Collect room-to-element relationships for batch processing later
                        var relatedCeilings = collidingCeilings.Cast<Element>().ToList();
                        roomElementRelationships[room] = relatedCeilings;

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
                    ceilingRoomRelationships,
                    ceilingIdToCeilingMap.ToDictionary(kvp => kvp.Key, kvp => (Element)kvp.Value),
                    parameterMappings);

                transaction.Commit();
                writeToLog("✓ Transaction committed successfully");
            }

            writeToLog("");
            writeToLog($"=== CEILING ANALYSIS SUMMARY ===");
            writeToLog($"Processed {rooms.Count} rooms using solid intersection");
            writeToLog($"Updated parameters for {ceilingRoomRelationships.Count} ceilings");

            var roomsWithCeilings = results.Count(r => r.WallsColliding > 0);
            var roomsWithoutCeilings = results.Count(r => r.WallsColliding == 0);
            var totalCeilingCollisions = results.Sum(r => r.WallsColliding);

            writeToLog($"");
            writeToLog($"=== DETAILED ANALYSIS RESULTS ===");
            writeToLog($"✓ Rooms with ceiling intersections: {roomsWithCeilings}/{rooms.Count}");
            writeToLog($"⚠ Rooms without ceiling intersections: {roomsWithoutCeilings}/{rooms.Count}");
            writeToLog($"📊 Total room-ceiling relationships: {totalCeilingCollisions}");
            writeToLog($"🔲 Unique ceilings with room intersections: {ceilingRoomRelationships.Count}");

            return results;
        }

        /// <summary>
        /// Get solid geometry from a ceiling
        /// </summary>
        private Solid GetCeilingSolid(Ceiling ceiling, Action<string> writeToLog)
        {
            try
            {
                var options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                var geometryElement = ceiling.get_Geometry(options);
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
                writeToLog?.Invoke($"  ✗ Error getting ceiling solid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create vertically expanded room solids for ceiling collision detection
        /// Expands room solid up and down along Z-axis
        /// </summary>
        private List<Solid> CreateVerticallyExpandedRoomSolids(Solid roomSolid, Action<string> writeToLog)
        {
            try
            {
                var expandedSolids = new List<Solid>();

                // Create vertical offset copies (1cm up and down along Z-axis)
                var offsetDistance = 1.0 / 30.48; // 1cm in feet

                // Create vertical directions: +Z (up) and -Z (down)
                var directions = new[] { XYZ.BasisZ, -XYZ.BasisZ };

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
                writeToLog?.Invoke($"    ✗ Error creating vertically expanded room solids: {ex.Message}");
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

            // Add a small buffer (1cm) in project units
            var bufferInFeet = 1.0 / 30.48; // 1cm in feet
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
