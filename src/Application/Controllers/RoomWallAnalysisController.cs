using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Core.Exceptions;
using RoomsManagerAddin.Domain.Models.Analysis;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Models.Shared;
using RoomsManagerAddin.Infrastructure.Logging;
using RoomsManagerAddin.Infrastructure.RevitApi;
using RoomsManagerAddin.Domain.Services.Analysis;
using RoomsManagerAddin.Domain.Services.Filtering;
using RoomsManagerAddin.Domain.Services.Processing;
using RoomsManagerAddin.Presentation.Windows;

namespace RoomsManagerAddin.Application.Controllers
{
    /// <summary>
    /// Orchestrates data loading, filtering, and analysis for the Rooms-Walls workflow.
    /// Uses dependency injection for all services.
    /// </summary>
    public class RoomWallAnalysisController
    {
        private readonly Document _document;
        private readonly IElementCollectorService _elementCollectorService;
        private readonly ICollisionAnalysisService _collisionAnalysisService;
        private readonly ILoggingService _loggingService;
        private readonly RoomFilterService _roomFilterService;

        /// <summary>
        /// Creates a new RoomWallAnalysisController with dependency injection.
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="elementCollectorService">Service for collecting elements</param>
        /// <param name="collisionAnalysisService">Service for collision analysis</param>
        /// <param name="loggingService">Service for logging</param>
        /// <param name="roomFilterService">Service for room filtering</param>
        public RoomWallAnalysisController(
            Document document,
            IElementCollectorService elementCollectorService,
            ICollisionAnalysisService collisionAnalysisService,
            ILoggingService loggingService,
            RoomFilterService roomFilterService)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _elementCollectorService = elementCollectorService ?? throw new ArgumentNullException(nameof(elementCollectorService));
            _collisionAnalysisService = collisionAnalysisService ?? throw new ArgumentNullException(nameof(collisionAnalysisService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _roomFilterService = roomFilterService ?? throw new ArgumentNullException(nameof(roomFilterService));
        }

        public InitialDataResult LoadInitialData()
        {
            var rooms = _elementCollectorService.GetRooms(_document);
            var walls = _elementCollectorService.GetWalls(_document);
            var floors = _elementCollectorService.GetFloors(_document);

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

            var floorItems = floors.Select(f => new FloorItem
            {
                Name = f.Name,
                LevelName = f.LevelId != ElementId.InvalidElementId
                    ? _document.GetElement(f.LevelId)?.Name ?? "Unknown"
                    : "Unknown",
                FloorTypeName = f.FloorType?.Name ?? "Unknown",
                Area = f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0,
                Id = f.Id
            }).ToList();

            return new InitialDataResult
            {
                Rooms = roomItems,
                Walls = wallItems,
                Floors = floorItems
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

        public List<FloorItem> ApplyFloorFilters(List<FloorItem> floors, string levelFilter, string typeFilter)
        {
            var result = floors.ToList();

            if (!string.IsNullOrEmpty(levelFilter) && levelFilter != "All Levels")
            {
                result = result.Where(f => f.LevelName == levelFilter).ToList();
            }

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All Types")
            {
                result = result.Where(f => f.FloorTypeName == typeFilter).ToList();
            }

            return result;
        }

        public List<RoomCollisionResult> RunAnalysis(List<RoomItem> roomItems, List<WallItem> wallItems, List<ParameterMappingConfiguration> parameterMappings, IntPtr? ownerWindowHandle = null)
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

            // Create and show modern progress window
            ModernProgressWindow progressWindow = null;
            List<RoomCollisionResult> results = null;
            Exception analysisException = null;

            try
            {
                progressWindow = new ModernProgressWindow();
                if (ownerWindowHandle.HasValue)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(progressWindow);
                    helper.Owner = ownerWindowHandle.Value;
                }
                progressWindow.Show();
                
                // Create modern progress reporter with type-safe design
                var progressReporter = new ProgressReporter(progressInfo =>
                {
                    _loggingService.WriteToLog($"{progressInfo.Title}: {progressInfo.Stage} - {progressInfo.Detail} ({progressInfo.OverallProgressPercentage:F0}%)");
                    progressWindow?.UpdateProgress(progressInfo);
                });

                // Run analysis with modern progress reporting (still on main thread for Revit API safety)
                results = _collisionAnalysisService.AnalyzeRoomCollisions(
                    _document,
                    rooms,
                    walls,
                    parameterMappings,
                    _loggingService.WriteToLog,
                    progressReporter
                );

                _loggingService.WriteToLog($"Analysis completed - {results.Count} results generated");
                return results;
            }
            catch (RoomsManagerException rme)
            {
                analysisException = rme;
                _loggingService.WriteToLog($"Analysis failed: {rme.Message}");
                _loggingService.WriteToLog($"Technical Details: {rme.TechnicalDetails}");
                if (rme.ShowToUser)
                {
                    TaskDialog.Show("Room-Wall Analysis Error", rme.UserMessage ?? rme.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                analysisException = ex;
                _loggingService.WriteToLog($"Unexpected error during analysis: {ex.Message}");
                _loggingService.WriteToLog($"Stack Trace: {ex.StackTrace}");
                TaskDialog.Show("Room-Wall Analysis Error",
                    $"An unexpected error occurred during the analysis.\n\n{ex.Message}\n\nPlease check the log file for details.");
                throw;
            }
            finally
            {
                // Allow progress window to close and then close it
                progressWindow?.AllowClose();
                progressWindow?.Close();
            }
        }

        public List<RoomCollisionResult> RunFloorAnalysis(List<RoomItem> roomItems, List<FloorItem> floorItems, List<ParameterMappingConfiguration> parameterMappings, IntPtr? ownerWindowHandle = null)
        {
            // Initialize debug logging with save dialog
            var logPath = _loggingService.InitializeDebugLogging(ownerWindowHandle);
            if (!string.IsNullOrEmpty(logPath))
            {
                _loggingService.WriteToLog($"Floor Analysis started - Log file: {logPath}");
                _loggingService.WriteToLog($"Analyzing {roomItems.Count} rooms and {floorItems.Count} floors");
            }

            // Convert back to Revit elements for analysis
            var rooms = roomItems.Select(ri => _document.GetElement(ri.Id) as Room).Where(r => r != null).ToList();
            var floors = floorItems.Select(fi => _document.GetElement(fi.Id) as Floor).Where(f => f != null).ToList();

            // DEBUG: Log floor conversion results
            _loggingService.WriteToLog($"CONTROLLER: Converting {floorItems.Count} FloorItems to Floor objects");
            _loggingService.WriteToLog($"CONTROLLER: Successfully converted {floors.Count} Floor objects");
            if (floors.Any())
            {
                var firstFewFloorIds = floors.Take(5).Select(f => f.Id.Value.ToString()).ToList();
                _loggingService.WriteToLog($"CONTROLLER: First 5 converted floor IDs: {string.Join(", ", firstFewFloorIds)}");
            }

            // Create and show modern progress window
            ModernProgressWindow progressWindow = null;
            List<RoomCollisionResult> results = null;
            Exception analysisException = null;

            try
            {
                progressWindow = new ModernProgressWindow();
                if (ownerWindowHandle.HasValue)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(progressWindow);
                    helper.Owner = ownerWindowHandle.Value;
                }
                progressWindow.Show();

                // Create modern progress reporter with type-safe design
                var progressReporter = new ProgressReporter(progressInfo =>
                {
                    _loggingService.WriteToLog($"{progressInfo.Title}: {progressInfo.Stage} - {progressInfo.Detail} ({progressInfo.OverallProgressPercentage:F0}%)");
                    progressWindow?.UpdateProgress(progressInfo);
                });

                // Run floor analysis with modern progress reporting
                results = _collisionAnalysisService.AnalyzeRoomFloorsCollisions(
                    _document,
                    rooms,
                    floors,
                    parameterMappings,
                    _loggingService.WriteToLog,
                    progressReporter
                );

                _loggingService.WriteToLog($"Floor Analysis completed - {results.Count} results generated");
                return results;
            }
            catch (RoomsManagerException rme)
            {
                analysisException = rme;
                _loggingService.WriteToLog($"Floor Analysis failed: {rme.Message}");
                _loggingService.WriteToLog($"Technical Details: {rme.TechnicalDetails}");
                if (rme.ShowToUser)
                {
                    TaskDialog.Show("Room-Floor Analysis Error", rme.UserMessage ?? rme.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                analysisException = ex;
                _loggingService.WriteToLog($"Unexpected error during floor analysis: {ex.Message}");
                _loggingService.WriteToLog($"Stack Trace: {ex.StackTrace}");
                TaskDialog.Show("Room-Floor Analysis Error",
                    $"An unexpected error occurred during the floor analysis.\n\n{ex.Message}\n\nPlease check the log file for details.");
                throw;
            }
            finally
            {
                // Allow progress window to close and then close it
                progressWindow?.AllowClose();
                progressWindow?.Close();
            }
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


