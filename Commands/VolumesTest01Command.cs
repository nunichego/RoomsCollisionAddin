using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;
using RoomsManagerAddin;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Commands
{
    /// <summary>
    /// Command to preview room geometry in 3D view using DirectShape
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class VolumesTest01Command : BaseCommand
    {
        private string debugLogPath;

        #region IExternalCommand Implementation
        /// <summary>
        /// Execute the VolumesTest01 command
        /// </summary>
        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var document = GetDocument(commandData);
                var uiDocument = GetUIDocument(commandData);

                // Check if we're in a 3D view
                if (!Is3DView(uiDocument.ActiveView))
                {
                    ShowInfo("3D View Required", "Please switch to a 3D view to preview room geometry.");
                    return Result.Succeeded;
                }

                // Initialize debug logging
                InitializeDebugLogging(document);

                // Get all rooms in the document
                var rooms = GetRooms(document);
                
                if (!rooms.Any())
                {
                    ShowInfo("No Rooms Found", "No rooms were found in the current document. Please create rooms first.");
                    return Result.Succeeded;
                }

                // Ask user if they want to clear existing previews
                bool clearExisting = ShowConfirmation("Clear Existing Previews", 
                    "Do you want to clear any existing room geometry previews before creating new ones?");

                if (clearExisting)
                {
                    ClearExistingPreviews(document);
                }

                // Create 3D preview of room geometry
                var previewResults = CreateRoomGeometryPreviews(document, rooms);

                // Show results
                ShowPreviewResults(previewResults);

                Logger?.LogInformation($"Room geometry preview created for {rooms.Count} rooms.");
                
                // If we have a debug log, show its location
                if (!string.IsNullOrEmpty(debugLogPath))
                {
                    WriteToDebugLog($"Analysis completed. Check debug log at: {debugLogPath}");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in VolumesTest01Command");
                message = $"Error creating room geometry preview: {ex.Message}";
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize debug logging to a text file
        /// </summary>
        private void InitializeDebugLogging(Document document)
        {
            try
            {
                // Ask user where to save the debug log
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Room Geometry Debug Log",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"RoomGeometryDebug_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    DefaultExt = ".txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    debugLogPath = saveDialog.FileName;
                    
                    WriteToDebugLog($"=== ROOM GEOMETRY DEBUG LOG ===");
                    WriteToDebugLog($"Document: {document.Title}");
                    WriteToDebugLog($"Timestamp: {DateTime.Now}");
                    WriteToDebugLog($"Revit Version: {document.Application.VersionName}");
                    WriteToDebugLog($"Units: {document.DisplayUnitSystem}");
                    WriteToDebugLog($"");
                }
                else
                {
                    // User cancelled, create a default path
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    debugLogPath = Path.Combine(desktopPath, $"RoomGeometryDebug_{timestamp}.txt");
                    
                    WriteToDebugLog($"=== ROOM GEOMETRY DEBUG LOG ===");
                    WriteToDebugLog($"Document: {document.Title}");
                    WriteToDebugLog($"Timestamp: {DateTime.Now}");
                    WriteToDebugLog($"Revit Version: {document.Application.VersionName}");
                    WriteToDebugLog($"Units: {document.DisplayUnitSystem}");
                    WriteToDebugLog($"");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing debug logging");
                // Fallback to desktop
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                debugLogPath = Path.Combine(desktopPath, $"RoomGeometryDebug_{timestamp}.txt");
            }
        }

        /// <summary>
        /// Write message to debug log file
        /// </summary>
        private void WriteToDebugLog(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(debugLogPath))
                {
                    File.AppendAllText(debugLogPath, message + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error writing to debug log");
            }
        }

        /// <summary>
        /// Check if the current view is a 3D view
        /// </summary>
        private bool Is3DView(Autodesk.Revit.DB.View view)
        {
            return view is View3D;
        }

        /// <summary>
        /// Get all rooms from the document
        /// </summary>
        private List<Room> GetRooms(Document document)
        {
            var rooms = new List<Room>();
            
            try
            {
                // Get all room elements
                var roomCollector = new FilteredElementCollector(document)
                    .OfClass(typeof(SpatialElement))
                    .Cast<SpatialElement>()
                    .Where(se => se is Room)
                    .Cast<Room>()
                    .Where(r => r.Area > 0) // Only rooms with area
                    .ToList();

                rooms.AddRange(roomCollector);
                
                Logger?.LogInformation($"Found {rooms.Count} rooms in the document");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting rooms from document");
                throw;
            }

            return rooms;
        }

        /// <summary>
        /// Clear existing room geometry previews
        /// </summary>
        private void ClearExistingPreviews(Document document)
        {
            try
            {
                // Find and delete existing DirectShape elements with our custom name
                var existingPreviews = new FilteredElementCollector(document)
                    .OfClass(typeof(DirectShape))
                    .Cast<DirectShape>()
                    .Where(ds => ds.Name.StartsWith("RoomPreview_"))
                    .ToList();

                if (existingPreviews.Any())
                {
                    using (var transaction = new Transaction(document, "Clear Room Previews"))
                    {
                        transaction.Start();
                        
                        foreach (var preview in existingPreviews)
                        {
                            document.Delete(preview.Id);
                        }
                        
                        transaction.Commit();
                    }

                    Logger?.LogInformation($"Cleared {existingPreviews.Count} existing room previews");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error clearing existing previews");
            }
        }

        /// <summary>
        /// Create 3D preview of room geometry
        /// </summary>
        private List<RoomPreviewResult> CreateRoomGeometryPreviews(Document document, List<Room> rooms)
        {
            var results = new List<RoomPreviewResult>();

            WriteToDebugLog("");
            WriteToDebugLog("=== CREATING ROOM GEOMETRY PREVIEWS ===");

            using (var transaction = new Transaction(document, "Create Room Geometry Previews"))
            {
                transaction.Start();

                foreach (var room in rooms)
                {
                    WriteToDebugLog("");
                    WriteToDebugLog($"--- PROCESSING ROOM: {room.Number} - {room.Name} ---");
                    
                    try
                    {
                        var result = new RoomPreviewResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            Level = room.Level?.Name ?? "Unknown"
                        };

                        WriteToDebugLog($"  Room properties:");
                        WriteToDebugLog($"    Area: {room.Area} sq ft");
                        WriteToDebugLog($"    Volume: {room.Volume} cu ft");
                        WriteToDebugLog($"    Level: {room.Level?.Name ?? "Unknown"}");
                        WriteToDebugLog($"    Level Elevation: {room.Level?.Elevation ?? 0}");

                        // Get room geometry
                        var roomGeometry = GetRoomGeometry(room);
                        if (roomGeometry != null)
                        {
                            WriteToDebugLog($"  ✓ Successfully got room geometry");
                            WriteToDebugLog($"    Solid Volume: {roomGeometry.Volume} cu ft");
                            WriteToDebugLog($"    Solid Faces: {roomGeometry.Faces.Size}");
                            WriteToDebugLog($"    Solid Edges: {roomGeometry.Edges.Size}");
                            
                            // Create DirectShape from the geometry
                            var directShape = CreateDirectShapeFromSolid(document, room, roomGeometry);
                            
                            if (directShape != null)
                            {
                                result.PreviewCreated = true;
                                result.DirectShapeId = directShape.Id;
                                WriteToDebugLog($"  ✓ Successfully created DirectShape (ID: {directShape.Id})");
                                Logger?.LogInformation($"Created preview for room: {room.Name}");
                            }
                            else
                            {
                                result.ErrorMessage = "Failed to create DirectShape from geometry";
                                WriteToDebugLog($"  ✗ FAILED to create DirectShape from geometry");
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "Failed to get room geometry";
                            WriteToDebugLog($"  ✗ FAILED to get room geometry - this room needs investigation!");
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        WriteToDebugLog($"  ✗ ERROR processing room {room.Name}: {ex.Message}");
                        WriteToDebugLog($"    Stack trace: {ex.StackTrace}");
                        
                        Logger?.LogError(ex, $"Error creating preview for room: {room.Name}");
                        results.Add(new RoomPreviewResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            PreviewCreated = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                transaction.Commit();
            }

            return results;
        }

        /// <summary>
        /// Get room geometry for preview with detailed debugging
        /// </summary>
        private Solid GetRoomGeometry(Room room)
        {
            WriteToDebugLog($"  Getting geometry for room: {room.Name} (ID: {room.Id})");
            
            try
            {
                // Get room geometry using the standard Revit API approach
                var options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                WriteToDebugLog($"  Geometry options: ComputeReferences={options.ComputeReferences}, DetailLevel={options.DetailLevel}");

                var geometryElement = room.get_Geometry(options);
                if (geometryElement == null)
                {
                    WriteToDebugLog($"  ✗ GeometryElement is null for room: {room.Name}");
                    return null;
                }

                WriteToDebugLog($"  ✓ Got GeometryElement with {geometryElement.Count()} objects");

                // Find the first solid in the geometry
                int objectIndex = 0;
                foreach (var geomObject in geometryElement)
                {
                    objectIndex++;
                    WriteToDebugLog($"  Object {objectIndex}: Type = {geomObject.GetType().Name}");
                    
                    if (geomObject is Solid solid)
                    {
                        WriteToDebugLog($"    ✓ Found Solid: Volume={solid.Volume}, Faces={solid.Faces.Size}, Edges={solid.Edges.Size}");
                        if (solid.Volume > 0)
                        {
                            WriteToDebugLog($"    ✓ Using this solid (volume > 0)");
                            return solid;
                        }
                        else
                        {
                            WriteToDebugLog($"    ✗ Solid has no volume (volume = {solid.Volume})");
                        }
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        WriteToDebugLog($"    Found GeometryInstance, getting instance geometry...");
                        var instanceGeometry = geomInstance.GetInstanceGeometry();
                        WriteToDebugLog($"    Instance geometry has {instanceGeometry.Count()} objects");
                        
                        int instanceIndex = 0;
                        foreach (var instanceGeom in instanceGeometry)
                        {
                            instanceIndex++;
                            WriteToDebugLog($"      Instance Object {instanceIndex}: Type = {instanceGeom.GetType().Name}");
                            
                            if (instanceGeom is Solid instanceSolid)
                            {
                                WriteToDebugLog($"        ✓ Found Solid in instance: Volume={instanceSolid.Volume}, Faces={instanceSolid.Faces.Size}, Edges={instanceSolid.Edges.Size}");
                                if (instanceSolid.Volume > 0)
                                {
                                    WriteToDebugLog($"        ✓ Using this instance solid (volume > 0)");
                                    return instanceSolid;
                                }
                                else
                                {
                                    WriteToDebugLog($"        ✗ Instance solid has no volume (volume = {instanceSolid.Volume})");
                                }
                            }
                        }
                    }
                    else
                    {
                        WriteToDebugLog($"    Other geometry type: {geomObject.GetType().Name}");
                    }
                }

                WriteToDebugLog($"  ✗ No valid solid found in geometry for room: {room.Name}");
                return null;
            }
            catch (Exception ex)
            {
                WriteToDebugLog($"  ✗ ERROR getting geometry: {ex.Message}");
                WriteToDebugLog($"    Stack trace: {ex.StackTrace}");
                Logger?.LogError(ex, $"Error getting geometry for room: {room.Name}");
                return null;
            }
        }

        /// <summary>
        /// Create DirectShape from solid
        /// </summary>
        private DirectShape CreateDirectShapeFromSolid(Document document, Room room, Solid solid)
        {
            try
            {
                WriteToDebugLog($"  Creating DirectShape for room: {room.Name}");
                WriteToDebugLog($"    Solid properties: Volume={solid.Volume}, Faces={solid.Faces.Size}, Edges={solid.Edges.Size}");
                
                // Check if solid is valid (basic checks)
                if (solid == null || solid.Volume <= 0)
                {
                    WriteToDebugLog($"    ✗ Solid is null or has no volume");
                    return null;
                }
                
                // Check solid bounds
                var boundingBox = solid.GetBoundingBox();
                if (boundingBox != null)
                {
                    WriteToDebugLog($"    Solid bounds: Min=({boundingBox.Min.X:F2}, {boundingBox.Min.Y:F2}, {boundingBox.Min.Z:F2}), Max=({boundingBox.Max.X:F2}, {boundingBox.Max.Y:F2}, {boundingBox.Max.Z:F2})");
                }
                
                // Create DirectShape
                WriteToDebugLog($"    Creating DirectShape element...");
                var directShape = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
                
                // Set name for identification
                WriteToDebugLog($"    Setting DirectShape name...");
                directShape.SetName($"RoomPreview_{room.Id}");

                // Set the solid geometry
                WriteToDebugLog($"    Setting solid geometry to DirectShape...");
                directShape.SetShape(new List<GeometryObject> { solid });
                
                WriteToDebugLog($"    ✓ DirectShape created successfully (ID: {directShape.Id})");
                return directShape;
            }
            catch (Exception ex)
            {
                WriteToDebugLog($"    ✗ ERROR creating DirectShape: {ex.Message}");
                WriteToDebugLog($"      Exception type: {ex.GetType().Name}");
                WriteToDebugLog($"      Stack trace: {ex.StackTrace}");
                Logger?.LogError(ex, $"Error creating DirectShape for room: {room.Name}");
                return null;
            }
        }

        /// <summary>
        /// Show preview results to the user
        /// </summary>
        private void ShowPreviewResults(List<RoomPreviewResult> results)
        {
            var totalRooms = results.Count;
            var successfulPreviews = results.Count(r => r.PreviewCreated);
            var failedPreviews = results.Count(r => !r.PreviewCreated);

            var message = $"Room Geometry Preview Complete\n\n" +
                         $"Total Rooms Processed: {totalRooms}\n" +
                         $"Successful Previews: {successfulPreviews}\n" +
                         $"Failed Previews: {failedPreviews}\n\n" +
                         $"The room geometry is now visible as 3D shapes in the 3D view.\n" +
                         $"Use the 'Clear Room Previews' option to remove them.";

            if (failedPreviews > 0)
            {
                message += "\n\nFailed Previews:";
                foreach (var result in results.Where(r => !r.PreviewCreated))
                {
                    message += $"\n• {result.RoomNumber} - {result.RoomName}: {result.ErrorMessage}";
                }
                
                message += $"\n\nDetailed debug information has been saved to: {debugLogPath}";
            }

            ShowInfo("Room Geometry Preview", message);
        }
        #endregion
    }
}
