using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoomsManagerAddin.Services;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Main Revit Add-in Application Class
    /// Implements IExternalApplication to create ribbon interface and initialize services
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        #region Constants
        private const string TAB_NAME = "Rooms Manager";
        private const string PANEL_NAME = "Rooms Manager";
        private const string ASSEMBLY_PATH = "RoomsManagerAddin.dll";
        #endregion

        #region Private Fields
        private static IServiceProvider _serviceProvider;
        private static ILogger<App> _logger;
        private static string _assemblyPath;
        #endregion

        #region Properties
        /// <summary>
        /// Service provider for dependency injection
        /// </summary>
        public static IServiceProvider ServiceProvider => _serviceProvider;

        /// <summary>
        /// Logger instance for the application
        /// </summary>
        public static ILogger<App> Logger => _logger;

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
                // Initialize application
                InitializeApplication(application);
                
                // Create ribbon interface
                CreateRibbonInterface(application);
                
                // Log successful startup
                _logger?.LogInformation("Rooms Manager Add-in started successfully");
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start Rooms Manager Add-in");
                TaskDialog.Show("Error", $"Failed to start Rooms Manager Add-in: {ex.Message}");
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
                // Dispose services
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _logger?.LogInformation("Rooms Manager Add-in shut down successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during Rooms Manager Add-in shutdown");
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the application and services
        /// </summary>
        private void InitializeApplication(UIControlledApplication application)
        {
            // Get assembly path
            _assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Configure services
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                // Simple logging configuration for .NET Framework
            });

            // Add services
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetService<ILogger<App>>();
        }

        /// <summary>
        /// Create the ribbon interface
        /// </summary>
        private void CreateRibbonInterface(UIControlledApplication application)
        {
            try
            {
                // Create ribbon tab
                application.CreateRibbonTab(TAB_NAME);

                // Create ribbon panel
                var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_NAME);

                // Add Room Volumes button
                AddRoomVolumesButton(panel);

                // Add VolumesTest01 button
                AddVolumesTest01Button(panel);

                _logger?.LogInformation("Ribbon interface created successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create ribbon interface");
                throw;
            }
        }

        /// <summary>
        /// Add the Room Volumes button to the ribbon panel
        /// </summary>
        private void AddRoomVolumesButton(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "RoomVolumes",
                    "Room Volumes",
                    _assemblyPath,
                    "RoomsManagerAddin.Commands.RoomVolumesCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Analyze room volumes and detect element collisions";
                buttonData.LongDescription = "Opens the Room Volumes analysis tool to examine room geometry and detect element collisions within room boundaries.";

                // Load icon
                var icon = LoadIcon("plans_32_96dpi.png");
                if (icon != null)
                {
                    buttonData.LargeImage = icon;
                }

                // Create button
                var button = panel.AddItem(buttonData) as PushButton;

                _logger?.LogInformation("Room Volumes button added successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add Room Volumes button");
                throw;
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
                var resourcePath = $"RoomsManagerAddin.Resources.icons.{iconName}";

                using (var stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }

                _logger?.LogWarning($"Icon not found: {iconName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to load icon: {iconName}");
                return null;
            }
        }

        /// <summary>
        /// Add the VolumesTest01 button to the ribbon panel
        /// </summary>
        private void AddVolumesTest01Button(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "VolumesTest01",
                    "VolumesTest01",
                    _assemblyPath,
                    "RoomsManagerAddin.Commands.VolumesTest01Command"
                );

                // Set button properties
                buttonData.ToolTip = "Preview room geometry in 3D view using temporary DirectShape elements";
                buttonData.LongDescription = "Creates semi-transparent 3D previews of room geometry in the current 3D view. Requires a 3D view to be active.";

                // Load icon (same as Room Volumes)
                var icon = LoadIcon("plans_32_96dpi.png");
                if (icon != null)
                {
                    buttonData.LargeImage = icon;
                }

                // Create button
                var button = panel.AddItem(buttonData) as PushButton;

                _logger?.LogInformation("VolumesTest01 button added successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add VolumesTest01 button");
                throw;
            }
        }
        #endregion
    }
}
