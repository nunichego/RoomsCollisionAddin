using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Services;

namespace RoomsManagerAddin.Commands
{
    /// <summary>
    /// Command to test wall geometry processing and creation
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class WallsTestingCommand : BaseCommand
    {
        private LoggingService _loggingService;
        private ProgressService _progressService;
        private ElementCollectorService _elementCollectorService;
        private WallProcessingService _wallProcessingService;

        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var document = commandData.Application.ActiveUIDocument.Document;
                Logger?.LogInformation("Starting Walls Testing Command");

                InitializeServices();
                var debugLogPath = _loggingService.InitializeDebugLogging();
                
                if (string.IsNullOrEmpty(debugLogPath))
                {
                    TaskDialog.Show("Error", "Failed to initialize debug logging. Operation cancelled.");
                    return Result.Failed;
                }

                _loggingService.WriteToDebugLog("=== WALLS TESTING COMMAND STARTED ===");
                _loggingService.WriteToDebugLog($"Document: {document.Title}");
                _loggingService.WriteToDebugLog($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _loggingService.WriteToDebugLog("");

                // Collect all walls
                _loggingService.WriteToDebugLog("Collecting walls...");
                var walls = _elementCollectorService.GetWalls(document);
                _loggingService.WriteToDebugLog($"Found {walls.Count} walls in the document");
                _loggingService.WriteToDebugLog("");

                if (walls.Count == 0)
                {
                    _loggingService.WriteToDebugLog("No walls found in the document.");
                    TaskDialog.Show("Walls Testing", "No walls found in the document.");
                    return Result.Succeeded;
                }

                // Test wall processing with progress tracking
                _progressService.StartAnalysis(_loggingService.WriteToDebugLog);
                
                var stopwatch = Stopwatch.StartNew();
                var wallProcessingResult = TestWallProcessing(walls);
                stopwatch.Stop();

                _progressService.EndAnalysis(_loggingService.WriteToDebugLog);
                _progressService.CloseProgressWindow();

                // Generate summary report
                GenerateSummaryReport(wallProcessingResult, stopwatch.Elapsed, debugLogPath);

                TaskDialog.Show("Walls Testing Complete", 
                    $"Wall processing completed!\n\n" +
                    $"Total walls: {walls.Count}\n" +
                    $"Regular walls: {wallProcessingResult.RegularWalls.Count}\n" +
                    $"Curtain walls: {wallProcessingResult.CurtainWallSolids.Count}\n" +
                    $"Total time: {stopwatch.Elapsed:mm\\:ss}\n\n" +
                    $"Debug log saved to:\n{debugLogPath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in WallsTestingCommand");
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private WallProcessingResult TestWallProcessing(List<Wall> walls)
        {
            _progressService.StartStage("Processing Walls", _loggingService.WriteToDebugLog);
            
            var wallProcessingResult = _wallProcessingService.ProcessWalls(walls, _loggingService.WriteToDebugLog);
            
            _progressService.EndStage("Processing Walls", _loggingService.WriteToDebugLog);

            // Test individual wall solid creation
            _progressService.StartStage("Testing Individual Wall Solids", _loggingService.WriteToDebugLog);
            
            TestRegularWallSolids(wallProcessingResult.RegularWalls);
            TestCurtainWallSolids(wallProcessingResult.CurtainWallSolids);
            
            _progressService.EndStage("Testing Individual Wall Solids", _loggingService.WriteToDebugLog);

            return wallProcessingResult;
        }

        private void TestRegularWallSolids(List<Wall> regularWalls)
        {
            _loggingService.WriteToDebugLog("=== TESTING REGULAR WALL SOLIDS ===");
            
            int successCount = 0;
            int failureCount = 0;

            for (int i = 0; i < regularWalls.Count; i++)
            {
                var wall = regularWalls[i];
                var progressPercentage = (int)((double)i / regularWalls.Count * 100);
                
                _progressService.ShowProgress("Testing Regular Walls", 
                    $"Testing wall {i + 1}/{regularWalls.Count}: {wall.Id}", 
                    i + 1, regularWalls.Count, progressPercentage, 100);

                _loggingService.WriteToDebugLog($"--- Testing Regular Wall {i + 1}/{regularWalls.Count}: {wall.Id} ---");
                _loggingService.WriteToDebugLog($"Wall Type: {wall.WallType?.Name ?? "Unknown"}");
                _loggingService.WriteToDebugLog($"Wall Kind: {wall.WallType?.Kind ?? WallKind.Basic}");

                var solid = _wallProcessingService.GetRegularWallSolid(wall, _loggingService.WriteToDebugLog);
                
                if (solid != null)
                {
                    successCount++;
                    _loggingService.WriteToDebugLog($"✓ SUCCESS: Wall {wall.Id} - Volume: {solid.Volume:F2}, Faces: {solid.Faces.Size}");
                }
                else
                {
                    failureCount++;
                    _loggingService.WriteToDebugLog($"✗ FAILED: Wall {wall.Id} - No solid created");
                }
                
                _loggingService.WriteToDebugLog("");
            }

            _loggingService.WriteToDebugLog($"=== REGULAR WALLS SUMMARY ===");
            _loggingService.WriteToDebugLog($"Total regular walls: {regularWalls.Count}");
            _loggingService.WriteToDebugLog($"Successful solid creation: {successCount}");
            _loggingService.WriteToDebugLog($"Failed solid creation: {failureCount}");
            _loggingService.WriteToDebugLog($"Success rate: {(double)successCount / regularWalls.Count * 100:F1}%");
            _loggingService.WriteToDebugLog("");
        }

        private void TestCurtainWallSolids(Dictionary<Wall, Solid> curtainWallSolids)
        {
            _loggingService.WriteToDebugLog("=== TESTING CURTAIN WALL SOLIDS ===");
            
            int successCount = 0;
            int failureCount = 0;

            var curtainWalls = curtainWallSolids.Keys.ToList();
            for (int i = 0; i < curtainWalls.Count; i++)
            {
                var wall = curtainWalls[i];
                var solid = curtainWallSolids[wall];
                
                _progressService.ShowProgress("Testing Curtain Walls", 
                    $"Testing curtain wall {i + 1}/{curtainWalls.Count}: {wall.Id}", 
                    i + 1, curtainWalls.Count, 0, 0);

                _loggingService.WriteToDebugLog($"--- Testing Curtain Wall {i + 1}/{curtainWalls.Count}: {wall.Id} ---");
                _loggingService.WriteToDebugLog($"Wall Type: {wall.WallType?.Name ?? "Unknown"}");

                if (solid != null)
                {
                    successCount++;
                    _loggingService.WriteToDebugLog($"✓ SUCCESS: Curtain Wall {wall.Id} - Volume: {solid.Volume:F2}, Faces: {solid.Faces.Size}");
                }
                else
                {
                    failureCount++;
                    _loggingService.WriteToDebugLog($"✗ FAILED: Curtain Wall {wall.Id} - No solid created");
                }
                
                _loggingService.WriteToDebugLog("");
            }

            _loggingService.WriteToDebugLog($"=== CURTAIN WALLS SUMMARY ===");
            _loggingService.WriteToDebugLog($"Total curtain walls: {curtainWalls.Count}");
            _loggingService.WriteToDebugLog($"Successful solid creation: {successCount}");
            _loggingService.WriteToDebugLog($"Failed solid creation: {failureCount}");
            _loggingService.WriteToDebugLog($"Success rate: {(double)successCount / curtainWalls.Count * 100:F1}%");
            _loggingService.WriteToDebugLog("");
        }

        private void GenerateSummaryReport(WallProcessingResult result, TimeSpan totalTime, string debugLogPath)
        {
            _loggingService.WriteToDebugLog("=== FINAL SUMMARY REPORT ===");
            _loggingService.WriteToDebugLog($"Total processing time: {totalTime:mm\\:ss}");
            _loggingService.WriteToDebugLog($"Total walls processed: {result.RegularWalls.Count + result.CurtainWallSolids.Count}");
            _loggingService.WriteToDebugLog($"Regular walls: {result.RegularWalls.Count}");
            _loggingService.WriteToDebugLog($"Curtain walls: {result.CurtainWallSolids.Count}");
            _loggingService.WriteToDebugLog($"Average time per wall: {totalTime.TotalMilliseconds / (result.RegularWalls.Count + result.CurtainWallSolids.Count):F1} ms");
            _loggingService.WriteToDebugLog("=== END OF REPORT ===");
        }

        private void InitializeServices()
        {
            var geometryService = new GeometryService(Logger);
            var parameterService = new ParameterUpdateService(Logger);
            var wallProcessingService = new WallProcessingService(Logger);
            
            _loggingService = new LoggingService(Logger);
            _progressService = new ProgressService(Logger);
            _elementCollectorService = new ElementCollectorService(Logger);
            _wallProcessingService = wallProcessingService;
        }
    }
}
