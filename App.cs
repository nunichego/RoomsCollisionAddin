using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RoomsManagerAddin.Core.ErrorHandling;
using RoomsManagerAddin.Infrastructure.Logging;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Core.DependencyInjection;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Main Revit Add-in Application Class
    /// Creates ribbon panel under Add-ins tab and initializes dependency injection
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        #region Constants
        private const string TAB_NAME = "Aukett + Heese";
        private const string PANEL_NAME = "AH RoomsDataSync (Demo)";
        private const string ASSEMBLY_PATH = "RoomsManagerAddin.dll";
        #endregion

        #region Private Fields
        private static string _assemblyPath;
        private static IServiceContainer _serviceContainer;
        #endregion

        #region Properties
        /// <summary>
        /// Assembly path for loading resources
        /// </summary>
        public static string AssemblyPath => _assemblyPath;

        /// <summary>
        /// Global service container for dependency injection
        /// </summary>
        /// <remarks>
        /// Accessible to commands for resolving services
        /// </remarks>
        public static IServiceContainer ServiceContainer => _serviceContainer;
        #endregion

        #region IExternalApplication Implementation
        /// <summary>
        /// Called when Revit starts up
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Initialize dependency injection container
                _serviceContainer = new ServiceContainer();
                ConfigureServices(_serviceContainer);

                // Initialize GlobalErrorHandler with logging service
                var loggingService = _serviceContainer.Resolve<Infrastructure.Logging.ILoggingService>();
                GlobalErrorHandler.Initialize(loggingService);

                // Get assembly path
                _assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create custom ribbon tab and panel
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

        #region Dependency Injection Configuration
        /// <summary>
        /// Configure dependency injection services
        /// </summary>
        /// <remarks>
        /// Registers all services with appropriate lifetimes:
        /// - Singleton: Infrastructure services (Logging, Configuration)
        /// - Transient: Document-dependent services (all others)
        /// </remarks>
        /// <param name="services">The service container to configure</param>
        private void ConfigureServices(IServiceContainer services)
        {
            // ============================================
            // Infrastructure Services (Singleton)
            // ============================================
            services.AddSingleton<RoomsManagerAddin.Infrastructure.Logging.ILoggingService,
                                  RoomsManagerAddin.Infrastructure.Logging.LoggingService>();

            services.AddSingleton<RoomsManagerAddin.Infrastructure.Configuration.IConfigurationService,
                                  RoomsManagerAddin.Infrastructure.Configuration.ConfigurationService>();

            // ============================================
            // Revit API Services (Transient - Document-dependent)
            // ============================================
            services.AddTransient<RoomsManagerAddin.Infrastructure.RevitApi.IElementCollectorService,
                                  RoomsManagerAddin.Infrastructure.RevitApi.ElementCollectorService>();

            // Register infrastructure services (GeometryService also implements IGeometryService via duck typing)
            services.AddTransient<RoomsManagerAddin.Infrastructure.RevitApi.IGeometryService>(
                c => new RoomsManagerAddin.Infrastructure.RevitApi.GeometryService());

            // Note: ParameterUpdateService, ProgressService, ProgressReporter don't fully implement interfaces yet
            // TODO: Complete interface implementations in future phase

            // ============================================
            // Domain Services - Analysis (Transient)
            // ============================================
            services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.ICollisionAnalysisService,
                                  RoomsManagerAddin.Domain.Services.Analysis.CollisionAnalysisService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.IWallBoundaryAnalysisService,
                                  RoomsManagerAddin.Domain.Services.Analysis.WallBoundaryAnalysisService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.IWallSolidAnalysisService,
                                  RoomsManagerAddin.Domain.Services.Analysis.WallSolidAnalysisService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.IFloorBoundaryAnalysisService,
                                  RoomsManagerAddin.Domain.Services.Analysis.FloorBoundaryAnalysisService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Analysis.ICeilingBoundaryAnalysisService,
                                  RoomsManagerAddin.Domain.Services.Analysis.CeilingBoundaryAnalysisService>();

            // ============================================
            // Domain Services - Filtering (Transient)
            // ============================================
            services.AddTransient<RoomsManagerAddin.Domain.Services.Filtering.IRoomFilterService,
                                  RoomsManagerAddin.Domain.Services.Filtering.RoomFilterService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Filtering.IGenericElementFilterService,
                                  RoomsManagerAddin.Domain.Services.Filtering.GenericElementFilterService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Filtering.IRoomParameterDiscoveryService,
                                  RoomsManagerAddin.Domain.Services.Filtering.RoomParameterDiscoveryService>();

            services.AddTransient<RoomsManagerAddin.Domain.Services.Filtering.IElementParameterDiscoveryService,
                                  RoomsManagerAddin.Domain.Services.Filtering.ElementParameterDiscoveryService>();

            // ============================================
            // Domain Services - Processing (Transient)
            // ============================================
            services.AddTransient<RoomsManagerAddin.Domain.Services.Processing.IRoomProcessingService,
                                  RoomsManagerAddin.Domain.Services.Processing.RoomProcessingService>();

            // ParameterMappingExecutionService requires Action<string> for logging
            // Register with factory that gets ILoggingService and creates Action<string>
            services.AddTransient<RoomsManagerAddin.Domain.Services.Processing.IParameterMappingExecutionService>(
                container =>
                {
                    var loggingService = container.Resolve<ILoggingService>();
                    return new RoomsManagerAddin.Domain.Services.Processing.ParameterMappingExecutionService(
                        loggingService.WriteToLog);
                });

            // ============================================
            // Domain Services - Mapping (Transient)
            // ============================================
            services.AddTransient<RoomsManagerAddin.Domain.Services.Mapping.IParameterMappingService,
                                  RoomsManagerAddin.Domain.Services.Mapping.ParameterMappingService>();

            // Note: WallProcessingService doesn't have interface yet - to be added if needed
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create the custom ribbon tab and panel
        /// </summary>
        private void CreateRibbonPanel(UIControlledApplication application)
        {
            try
            {
                // Create custom ribbon tab (only if it doesn't exist)
                try
                {
                    application.CreateRibbonTab(TAB_NAME);
                }
                catch (Exception ex) when (ex.Message.Contains("tab with the input name exists already") || ex.Message.Contains("already exists"))
                {
                    // Tab already exists - this is fine, we can still add our panel to it
                    System.Diagnostics.Debug.WriteLine($"Ribbon tab '{TAB_NAME}' already exists - adding panel to existing tab");
                }
                
                // Create ribbon panel under the custom tab
                var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_NAME);

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
                    "RoomsMapping",
                    "RoomsMapping",
                    _assemblyPath,
                    "RoomsManagerAddin.Application.Commands.RoomDataSyncCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Analyze room collisions and synchronize parameters with surrounding elements";
                buttonData.LongDescription = "Performs comprehensive collision analysis between rooms and walls, updating room parameters with collision information.";

                // Load icon
                var icon = LoadIcon("room-32.png");
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
                    "RoomsManagerAddin.Application.Commands.SettingsCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Configure collision analysis settings and tolerance values";
                buttonData.LongDescription = "Open settings dialog to configure collision detection tolerance, volume thresholds, and other analysis parameters.";

                // Load icon
                var icon = LoadIcon("setting-32.png");
                if (icon != null)
                {
                    buttonData.LargeImage = icon;
                }

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
                    "RoomsManagerAddin.Application.Commands.HelpCommand"
                );

                // Set button properties
                buttonData.ToolTip = "Get help and documentation for RoomDataSync";
                buttonData.LongDescription = "View help documentation, tutorials, and troubleshooting information for the RoomDataSync add-in.";

                // Load icon
                var icon = LoadIcon("help-32.png");
                if (icon != null)
                {
                    buttonData.LargeImage = icon;
                }

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
