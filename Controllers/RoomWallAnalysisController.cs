using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Services.Categories.Walls;

namespace RoomsManagerAddin.Controllers
{
    /// <summary>
    /// Orchestrates data loading, filtering, and analysis for the Rooms-Walls workflow.
    /// Keeps UI code in the form and computation/coordination here.
    /// </summary>
    public class RoomWallAnalysisController
    {
        private readonly Document _document;
        private readonly ElementCollectorService _elementCollectorService;

        // Services used by analysis
        private readonly ParameterUpdateService _parameterUpdateService;
        private readonly WallBoundaryAnalysisService _wallBoundaryAnalysisService;
        private readonly CollisionAnalysisService _collisionAnalysisService;
        private readonly LoggingService _loggingService;
        private readonly RoomFilterService _roomFilterService;

        public RoomWallAnalysisController(Document document)
        {
            _document = document;
            _elementCollectorService = new ElementCollectorService();

            _parameterUpdateService = new ParameterUpdateService();
            _loggingService = new LoggingService();
            _roomFilterService = new RoomFilterService(_document, _loggingService);

            // Initialize new wall boundary analysis service
            _wallBoundaryAnalysisService = new WallBoundaryAnalysisService(_parameterUpdateService);
            
            // Initialize collision analysis service with wall boundary service
            _collisionAnalysisService = new CollisionAnalysisService(_wallBoundaryAnalysisService);
        }

        public InitialDataResult LoadInitialData()
        {
            var rooms = _elementCollectorService.GetRooms(_document);
            var walls = _elementCollectorService.GetWalls(_document);

            var roomItems = rooms.Select(r => new RoomItem
            {
                Name = r.Name,
                Number = r.Number,
                LevelName = r.Level?.Name ?? "Unknown",
                Area = r.Area,
                Volume = r.Volume,
                Id = r.Id
            }).ToList();

            var wallItems = walls.Select(w => new WallItem
            {
                Name = w.Name,
                LevelName = w.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsElementId() is ElementId levelId && levelId != ElementId.InvalidElementId 
                    ? _document.GetElement(levelId)?.Name ?? "Unknown" 
                    : "Unknown",
                WallTypeName = w.WallType?.Name ?? "Unknown",
                Length = w.Location is LocationCurve curve ? curve.Curve.Length : 0,
                Height = w.WallType?.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0,
                Id = w.Id
            }).ToList();

            return new InitialDataResult
            {
                Rooms = roomItems,
                Walls = wallItems
            };
        }

        public List<RoomItem> ApplyRoomFilters(List<RoomItem> rooms, string levelFilter, string areaFilter)
        {
            var result = rooms.ToList();

            if (!string.IsNullOrEmpty(levelFilter) && levelFilter != "All Levels")
            {
                result = result.Where(r => r.LevelName == levelFilter).ToList();
            }

            if (!string.IsNullOrEmpty(areaFilter) && double.TryParse(areaFilter, out double minArea) && minArea > 0)
            {
                result = result.Where(r => r.Area >= minArea).ToList();
            }

            return result;
        }

        public List<WallItem> ApplyWallFilters(List<WallItem> walls, string levelFilter, string typeFilter)
        {
            var result = walls.ToList();

            if (!string.IsNullOrEmpty(levelFilter) && levelFilter != "All Levels")
            {
                result = result.Where(w => w.LevelName == levelFilter).ToList();
            }

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All Types")
            {
                result = result.Where(w => w.WallTypeName == typeFilter).ToList();
            }

            return result;
        }

        public List<RoomCollisionResult> RunAnalysis(List<RoomItem> roomItems, List<WallItem> wallItems, IntPtr? ownerWindowHandle = null)
        {
            // Initialize debug logging with save dialog
            var logPath = _loggingService.InitializeDebugLogging(ownerWindowHandle);
            if (!string.IsNullOrEmpty(logPath))
            {
                _loggingService.WriteToLog($"Analysis started - Log file: {logPath}");
                _loggingService.WriteToLog($"Analyzing {roomItems.Count} rooms and {wallItems.Count} walls");
            }

            // Convert back to Revit elements for analysis
            var rooms = roomItems.Select(ri => _document.GetElement(ri.Id) as Room).Where(r => r != null).ToList();
            var walls = wallItems.Select(wi => _document.GetElement(wi.Id) as Wall).Where(w => w != null).ToList();
            
            // DEBUG: Log wall conversion results
            _loggingService.WriteToLog($"CONTROLLER: Converting {wallItems.Count} WallItems to Wall objects");
            _loggingService.WriteToLog($"CONTROLLER: Successfully converted {walls.Count} Wall objects");
            if (walls.Any())
            {
                var firstFewWallIds = walls.Take(5).Select(w => w.Id.Value.ToString()).ToList();
                _loggingService.WriteToLog($"CONTROLLER: First 5 converted wall IDs: {string.Join(", ", firstFewWallIds)}");
            }

            // Simple progress callback (no UI progress window for now)
            Action<string, string, int, int, int, int> progressCallback = 
                (title, message, stepCurrent, stepTotal, overallCurrent, overallTotal) =>
                {
                    // TODO: Replace with actual progress window
                    System.Diagnostics.Debug.WriteLine($"{title}: {message} ({overallCurrent}/{overallTotal})");
                };

            var results = _collisionAnalysisService.AnalyzeRoomCollisions(
                _document,
                rooms,
                walls,
                _loggingService.WriteToLog,
                progressCallback
            );

            _loggingService.WriteToLog($"Analysis completed - {results.Count} results generated");
            return results;
        }

        // New filtering system methods
        public List<ParameterInfo> GetAvailableRoomParameters()
        {
            return _roomFilterService.GetAvailableParameters();
        }

        public List<FilterOperator> GetAvailableOperators(string parameterName)
        {
            return _roomFilterService.GetAvailableOperators(parameterName);
        }

        public ParameterDataType GetParameterDataType(string parameterName)
        {
            return _roomFilterService.GetParameterDataType(parameterName);
        }

        public RoomFilterConfiguration CreateFilterConfiguration(string name = "Custom Filter")
        {
            return _roomFilterService.CreateFilterConfiguration(name);
        }

        public List<RoomItem> ApplyAdvancedFilter(RoomFilterConfiguration filterConfig)
        {
            return _roomFilterService.ApplyFilter(filterConfig);
        }

        public bool ValidateFilterConfiguration(RoomFilterConfiguration filterConfig)
        {
            return _roomFilterService.ValidateFilterSet(filterConfig.RootFilterSet);
        }

        public int CountMatchingRooms(RoomFilterConfiguration filterConfig)
        {
            return _roomFilterService.CountMatchingRooms(filterConfig);
        }

        public string GetFilterDescription(RoomFilterConfiguration filterConfig)
        {
            return _roomFilterService.GetFilterDescription(filterConfig);
        }

        // Test method to create a sample filter configuration for demonstration
        public RoomFilterConfiguration CreateSampleFilter()
        {
            try
            {
                var filterConfig = CreateFilterConfiguration("Sample Filter");
                
                // Try to create a basic filter: Area > 100
                var parameters = GetAvailableRoomParameters();
                var areaParam = parameters.FirstOrDefault(p => p.Name.Equals("Area", StringComparison.OrdinalIgnoreCase));
                
                if (areaParam != null)
                {
                    var rule = _roomFilterService.CreateFilterRule(areaParam.Name, FilterOperator.GreaterThan, "100");
                    filterConfig.RootFilterSet.Items.Add(rule);
                }

                return filterConfig;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error creating sample filter: {ex.Message}");
                return CreateFilterConfiguration("Empty Filter");
            }
        }
    }
}


