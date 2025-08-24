using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using RoomsManagerAddin;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Commands
{
    /// <summary>
    /// Command to analyze room volumes and detect collisions with walls and floors
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class RoomVolumesCommand : BaseCommand
    {
        private LoggingService _loggingService;
        private ProgressService _progressService;
        private ElementCollectorService _elementCollectorService;
        private CollisionAnalysisService _collisionAnalysisService;

        #region IExternalCommand Implementation
        /// <summary>
        /// Execute the Room Volumes command
        /// </summary>
        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var document = GetDocument(commandData);
                var uiDocument = GetUIDocument(commandData);

                // Initialize services
                InitializeServices();

                // Initialize debug logging
                var debugLogPath = _loggingService.InitializeDebugLogging();
                if (string.IsNullOrEmpty(debugLogPath))
                {
                    message = "Failed to initialize debug logging";
                    return Result.Failed;
                }

                // Get all rooms and walls
                var rooms = _elementCollectorService.GetRooms(document);
                var walls = _elementCollectorService.GetWalls(document);

                if (!rooms.Any())
                {
                    ShowInfo("No Rooms Found", "No rooms were found in the current document. Please create rooms first.");
                    return Result.Succeeded;
                }

                _loggingService.WriteToDebugLog($"Found {rooms.Count} rooms, {walls.Count} walls");

                // Start analysis timing
                _progressService.StartAnalysis(_loggingService.WriteToDebugLog);

                // Stage 1: Pre-process curtain walls
                _progressService.StartStage("Pre-processing Curtain Walls", _loggingService.WriteToDebugLog);
                _progressService.EndStage("Pre-processing Curtain Walls", _loggingService.WriteToDebugLog);

                // Stage 2: Analyze room collisions
                _progressService.StartStage("Analyzing Room Collisions", _loggingService.WriteToDebugLog);
                var analysisResults = _collisionAnalysisService.AnalyzeRoomCollisions(
                    document, rooms, walls,
                    _loggingService.WriteToDebugLog,
                    _progressService.ShowProgress);
                _progressService.EndStage("Analyzing Room Collisions", _loggingService.WriteToDebugLog);

                // End analysis timing
                _progressService.EndAnalysis(_loggingService.WriteToDebugLog);

                // Show results
                ShowAnalysisResults(analysisResults, debugLogPath);

                // Close progress window
                _progressService.CloseProgressWindow();

                // Room collision analysis completed
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Error in RoomVolumesCommand
                message = $"Error analyzing room volumes: {ex.Message}";
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize all services
        /// </summary>
        private void InitializeServices()
        {
            var geometryService = new GeometryService();
            var parameterService = new ParameterUpdateService();
            var wallProcessingService = new WallProcessingService();
            var roomProcessingService = new RoomProcessingService();
            
            _loggingService = new LoggingService();
            _progressService = new ProgressService();
            _elementCollectorService = new ElementCollectorService();
            _collisionAnalysisService = new CollisionAnalysisService(null, geometryService, parameterService, wallProcessingService, roomProcessingService);
        }

        /// <summary>
        /// Show analysis results to the user
        /// </summary>
        private void ShowAnalysisResults(List<RoomCollisionResult> results, string debugLogPath)
        {
            var totalRooms = results.Count;
            var successfulAnalysis = results.Count(r => string.IsNullOrEmpty(r.ErrorMessage));
            var failedAnalysis = results.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));

            var message = $"Room Collision Analysis Complete\n\n" +
                         $"Total Rooms Processed: {totalRooms}\n" +
                         $"Successful Analysis: {successfulAnalysis}\n" +
                         $"Failed Analysis: {failedAnalysis}\n\n" +
                         $"Room Filter Tag parameters have been updated with wall type information.";

            if (failedAnalysis > 0)
            {
                message += "\n\nFailed Analysis:";
                foreach (var result in results.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)))
                {
                    message += $"\n• {result.RoomNumber} - {result.RoomName}: {result.ErrorMessage}";
                }
            }
            
            message += $"\n\nDetailed analysis has been saved to: {debugLogPath}";

            ShowInfo("Room Collision Analysis", message);
        }
        #endregion
    }
}
