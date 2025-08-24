using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Main Revit Add-in Application Class
    /// Creates ribbon panel under Add-ins tab
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        #region Constants
        private const string PANEL_NAME = "RoomDataSync";
        private const string ASSEMBLY_PATH = "RoomsManagerAddin.dll";
        #endregion

        #region Private Fields
        private static string _assemblyPath;
        #endregion

        #region Properties
        /// <summary>
        /// Assembly path for loading resources
        /// </summary>
        public static string AssemblyPath => _assemblyPath;
        #endregion

        #region IExternalApplication Implementation
        /// <summary>
        /// Called when Revit starts up
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Get assembly path
                _assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create ribbon panel under Add-ins tab
                CreateRibbonPanel(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to start RoomDataSync Add-in: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Called when Revit shuts down
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error during RoomDataSync Add-in shutdown: {ex.Message}");
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create the ribbon panel under Add-ins tab
        /// </summary>
        private void CreateRibbonPanel(UIControlledApplication application)
        {
            try
            {
                // Create ribbon panel under Add-ins tab
                var panel = application.CreateRibbonPanel(PANEL_NAME);

                // Add Room Collision Analysis button
                AddRoomCollisionButton(panel);

                // Add Settings button (for future tolerance settings)
                AddSettingsButton(panel);

                // Add Help button
                AddHelpButton(panel);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create ribbon panel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Add the Room Collision Analysis button to the ribbon panel
        /// </summary>
        private void AddRoomCollisionButton(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "RoomsWalls",
                    "Rooms-Walls",
                    _assemblyPath,
                    "RoomsManagerAddin.Commands.RoomDataSyncCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Analyze room collisions and synchronize parameters with surrounding elements";
                buttonData.LongDescription = "Performs comprehensive collision analysis between rooms and walls, updating room parameters with collision information.";

                // Load icon
                var icon = LoadIcon("plans_32_96dpi.png");
                if (icon != null)
                {
                    buttonData.LargeImage = icon;
                }

                // Create button
                var button = panel.AddItem(buttonData) as PushButton;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add Room Collision Analysis button: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Add the Settings button to the ribbon panel
        /// </summary>
        private void AddSettingsButton(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "RoomDataSyncSettings",
                    "Settings",
                    _assemblyPath,
                    "RoomsManagerAddin.Commands.SettingsCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Configure collision analysis settings and tolerance values";
                buttonData.LongDescription = "Open settings dialog to configure collision detection tolerance, volume thresholds, and other analysis parameters.";

                // Create button
                var button = panel.AddItem(buttonData) as PushButton;
            }
            catch (Exception ex)
            {
                // Settings button is optional, don't fail if it can't be created
                System.Diagnostics.Debug.WriteLine($"Could not create Settings button: {ex.Message}");
            }
        }

        /// <summary>
        /// Add the Help button to the ribbon panel
        /// </summary>
        private void AddHelpButton(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "RoomDataSyncHelp",
                    "Help",
                    _assemblyPath,
                    "RoomsManagerAddin.Commands.HelpCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Get help and documentation for RoomDataSync";
                buttonData.LongDescription = "View help documentation, tutorials, and troubleshooting information for the RoomDataSync add-in.";

                // Create button
                var button = panel.AddItem(buttonData) as PushButton;
            }
            catch (Exception ex)
            {
                // Help button is optional, don't fail if it can't be created
                System.Diagnostics.Debug.WriteLine($"Could not create Help button: {ex.Message}");
            }
        }

        /// <summary>
        /// Load an icon from embedded resources
        /// </summary>
        private BitmapSource LoadIcon(string iconName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"RoomsManagerAddin.Resources.icons.{iconName}";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var decoder = new PngBitmapDecoder(
                            stream,
                            BitmapCreateOptions.PreservePixelFormat,
                            BitmapCacheOption.Default);

                        return decoder.Frames[0];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load icon {iconName}: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
