using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Services;
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
            services[typeof(GeometryService)] = new GeometryService();
            services[typeof(ElementCollectorService)] = new ElementCollectorService();
            services[typeof(ParameterUpdateService)] = new ParameterUpdateService();
            services[typeof(ProgressService)] = new ProgressService();
            services[typeof(LoggingService)] = new LoggingService();
            services[typeof(WallProcessingService)] = new WallProcessingService();
            services[typeof(RoomProcessingService)] = new RoomProcessingService();
            
            // Add CollisionAnalysisService last since it depends on others
            services[typeof(CollisionAnalysisService)] = new CollisionAnalysisService(
                null, // logger
                services[typeof(GeometryService)] as GeometryService,
                services[typeof(ParameterUpdateService)] as ParameterUpdateService,
                services[typeof(WallProcessingService)] as WallProcessingService,
                services[typeof(RoomProcessingService)] as RoomProcessingService
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

                // Simple progress callback (will be replaced with proper progress window)
                Action<string, string, int, int, int, int> progressCallback = 
                    (title, message, stepCurrent, stepTotal, overallCurrent, overallTotal) =>
                    {
                        // TODO: Replace with actual progress window
                        System.Diagnostics.Debug.WriteLine($"{title}: {message} ({overallCurrent}/{overallTotal})");
                    };

                // Run analysis
                var results = collisionService.AnalyzeRoomCollisions(
                    document, 
                    rooms, 
                    walls,
                    loggingService.WriteToLog,
                    progressCallback
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
