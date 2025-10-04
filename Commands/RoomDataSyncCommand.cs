using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Services.Categories.Walls;
using RoomsManagerAddin.Services.Categories.Floors;
using RoomsManagerAddin.Models;
using System.Windows.Interop;

namespace RoomsManagerAddin.Commands
{
    /// <summary>
    /// Main command for RoomDataSync add-in
    /// Analyzes room collisions and synchronizes parameters with surrounding elements
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class RoomDataSyncCommand : IExternalCommand
    {
        #region IExternalCommand Implementation
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get Revit application and document
                var uiApp = commandData.Application;
                var document = uiApp.ActiveUIDocument.Document;

                // Initialize services
                var services = InitializeServices();

                // Show main dialog
                var result = ShowMainDialog(document, services, uiApp);

                return result;
            }
            catch (Exception ex)
            {
                message = $"Error executing RoomDataSync: {ex.Message}";
                TaskDialog.Show("RoomDataSync Error", message);
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize services
        /// </summary>
        private Dictionary<Type, object> InitializeServices()
        {
            var services = new Dictionary<Type, object>();

            // Add services in dependency order
            services[typeof(IConfigurationService)] = new ConfigurationService();
            var geometryService = new GeometryService();
            services[typeof(GeometryService)] = geometryService;
            services[typeof(ElementCollectorService)] = new ElementCollectorService();
            services[typeof(ProgressService)] = new ProgressService();
            var loggingService = new LoggingService();
            services[typeof(LoggingService)] = loggingService;
            services[typeof(WallProcessingService)] = new WallProcessingService();
            var roomProcessingService = new RoomProcessingService();
            services[typeof(RoomProcessingService)] = roomProcessingService;

            // Add ParameterMappingExecutionService, WallBoundaryAnalysisService, and FloorBoundaryAnalysisService
            var parameterMappingExecutionService = new ParameterMappingExecutionService(loggingService.WriteToLog);
            services[typeof(ParameterMappingExecutionService)] = parameterMappingExecutionService;

            var wallBoundaryAnalysisService = new WallBoundaryAnalysisService(parameterMappingExecutionService);
            services[typeof(WallBoundaryAnalysisService)] = wallBoundaryAnalysisService;

            var floorBoundaryAnalysisService = new FloorBoundaryAnalysisService(
                parameterMappingExecutionService,
                geometryService,
                roomProcessingService);
            services[typeof(FloorBoundaryAnalysisService)] = floorBoundaryAnalysisService;

            // Add CollisionAnalysisService last since it depends on both wall and floor services
            services[typeof(CollisionAnalysisService)] = new CollisionAnalysisService(
                wallBoundaryAnalysisService,
                floorBoundaryAnalysisService
            );

            return services;
        }

        /// <summary>
        /// Show the main RoomDataSync interface window
        /// </summary>
        private Result ShowMainDialog(Document document, Dictionary<Type, object> services, UIApplication uiApp)
        {
            try
            {
                // Check if document has rooms
                var elementCollector = services[typeof(ElementCollectorService)] as ElementCollectorService;
                var rooms = elementCollector.GetRooms(document);

                if (!rooms.Any())
                {
                    TaskDialog.Show("RoomDataSync", "No rooms found in the current document.\n\nPlease create rooms before running the analysis.");
                    return Result.Succeeded;
                }

                // Show the main interface window (WPF-based with native Revit styling)
                var analysisWindow = new RoomWallAnalysisWindow(document);
                // Set Revit as the owner window to keep dialog on top and modal
                new WindowInteropHelper(analysisWindow) { Owner = uiApp.MainWindowHandle };
                analysisWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RoomDataSync Error", $"Error opening interface: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Run full collision analysis (temporary implementation)
        /// </summary>
        private Result RunFullAnalysis(Document document, Dictionary<Type, object> services)
        {
            try
            {
                var collisionService = services[typeof(CollisionAnalysisService)] as CollisionAnalysisService;
                var elementCollector = services[typeof(ElementCollectorService)] as ElementCollectorService;
                var loggingService = services[typeof(LoggingService)] as LoggingService;

                // Collect all elements
                var rooms = elementCollector.GetRooms(document);
                var walls = elementCollector.GetWalls(document);

                // Simple progress reporter (will be replaced with proper progress window)
                var progressReporter = new ProgressReporter(progressInfo =>
                {
                    // TODO: Replace with actual progress window
                    System.Diagnostics.Debug.WriteLine($"{progressInfo.Title}: {progressInfo.Stage} - {progressInfo.Detail} ({progressInfo.OverallProgressPercentage:F0}%)");
                });

                // Run analysis with empty parameter mappings (this command doesn't use the UI)
                var emptyParameterMappings = new List<ParameterMappingConfiguration>();
                var results = collisionService.AnalyzeRoomCollisions(
                    document, 
                    rooms, 
                    walls,
                    emptyParameterMappings,
                    loggingService.WriteToLog,
                    progressReporter
                );

                // Show results
                ShowResultsDialog(results);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RoomDataSync Error", $"Error during analysis: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Show results dialog
        /// </summary>
        private void ShowResultsDialog(List<RoomCollisionResult> results)
        {
            var totalRooms = results.Count;
            var roomsWithCollisions = results.Count(r => r.WallsColliding > 0);
            var totalCollisions = results.Sum(r => r.WallsColliding);

            var message = $"Analysis Complete!\n\n" +
                         $"Total Rooms: {totalRooms}\n" +
                         $"Rooms with Collisions: {roomsWithCollisions}\n" +
                         $"Total Collisions: {totalCollisions}\n\n" +
                         $"Check the log file for detailed results.";

            TaskDialog.Show("RoomDataSync Results", message);
        }
        #endregion
    }
}
