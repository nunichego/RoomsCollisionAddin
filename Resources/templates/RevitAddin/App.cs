using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RevitAddinTemplate.Services;
using RevitAddinTemplate.Models;

namespace RevitAddinTemplate
{
    /// <summary>
    /// Main Revit Add-in Application Class
    /// Implements IExternalApplication to create ribbon interface and initialize services
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        #region Constants
        private const string TAB_NAME = "RevitAddinTemplate";
        private const string PANEL_NAME = "Tools";
        private const string ASSEMBLY_PATH = "RevitAddinTemplate.dll";
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
                _logger?.LogInformation("RevitAddinTemplate started successfully");
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start RevitAddinTemplate");
                TaskDialog.Show("Error", $"Failed to start RevitAddinTemplate: {ex.Message}");
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
                
                _logger?.LogInformation("RevitAddinTemplate shut down successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during RevitAddinTemplate shutdown");
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
            ConfigureServices();
            
            // Get logger
            _logger = _serviceProvider.GetService<ILogger<App>>();
            
            // Initialize configuration service
            var configService = _serviceProvider.GetService<IConfigurationService>();
            configService?.Initialize();
        }

        /// <summary>
        /// Configure dependency injection services
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add configuration
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();

            // Add business services
            services.AddSingleton<IContentManager, ContentManager>();
            services.AddSingleton<IDynamoScriptsManager, DynamoScriptsManager>();
            services.AddSingleton<IAdminConfigurationService, AdminConfigurationService>();

            // Add models
            services.AddSingleton<AppSettings>();
            services.AddSingleton<AdminConfig>();
            services.AddSingleton<MasterConfig>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Create the ribbon interface with tabs and panels
        /// </summary>
        private void CreateRibbonInterface(UIControlledApplication application)
        {
            // Create main tab
            application.CreateRibbonTab(TAB_NAME);

            // Create main panel
            var mainPanel = application.CreateRibbonPanel(TAB_NAME, PANEL_NAME);

            // Add commands to main panel
            AddCommandToPanel(mainPanel, "AboutCommand", "About", "Shows information about this add-in", "icon_info_32_96dpi.png");
            AddCommandToPanel(mainPanel, "AdminPanelCommand", "Admin Panel", "Opens the admin panel for configuration", "administrator-32_96dpi.png");
            AddCommandToPanel(mainPanel, "ContentFilesCommand", "Content Files", "Manages content files and libraries", "content-library-32_96dpi.png");
            AddCommandToPanel(mainPanel, "DynamoScriptsCommand", "Dynamo Scripts", "Manages Dynamo scripts", "dynamo-scripts-32_96dpi.png");
            AddCommandToPanel(mainPanel, "HelpCommand", "Help", "Shows help information", "help-32_96dpi.png");
            AddCommandToPanel(mainPanel, "SettingsCommand", "Settings", "Opens the settings panel", "settings-32_96dpi.png");

            // Create additional panels for different categories
            CreateContentPanel(application);
            CreateAdminPanel(application);
            CreateToolsPanel(application);
        }

        /// <summary>
        /// Create content management panel
        /// </summary>
        private void CreateContentPanel(UIControlledApplication application)
        {
            var contentPanel = application.CreateRibbonPanel(TAB_NAME, "Content Management");
            
            // Add content-related commands
            AddCommandToPanel(contentPanel, "ContentFilesCommand", "Content Files", "Manages content files and libraries", "content-library-32_96dpi.png");
            AddCommandToPanel(contentPanel, "DynamoScriptsCommand", "Dynamo Scripts", "Manages Dynamo scripts", "dynamo-scripts-32_96dpi.png");
        }

        /// <summary>
        /// Create admin panel
        /// </summary>
        private void CreateAdminPanel(UIControlledApplication application)
        {
            var adminPanel = application.CreateRibbonPanel(TAB_NAME, "Administration");
            
            // Add admin-related commands
            AddCommandToPanel(adminPanel, "AdminPanelCommand", "Admin Panel", "Opens the admin panel for configuration", "administrator-32_96dpi.png");
            AddCommandToPanel(adminPanel, "SettingsCommand", "Settings", "Opens the settings panel", "settings-32_96dpi.png");
        }

        /// <summary>
        /// Create tools panel
        /// </summary>
        private void CreateToolsPanel(UIControlledApplication application)
        {
            var toolsPanel = application.CreateRibbonPanel(TAB_NAME, "Tools");
            
            // Add utility commands
            AddCommandToPanel(toolsPanel, "AboutCommand", "About", "Shows information about this add-in", "icon_info_32_96dpi.png");
            AddCommandToPanel(toolsPanel, "HelpCommand", "Help", "Shows help information", "help-32_96dpi.png");
        }

        /// <summary>
        /// Add a command to a ribbon panel
        /// </summary>
        private void AddCommandToPanel(RibbonPanel panel, string commandName, string buttonText, string tooltip, string iconName)
        {
            try
            {
                // Create push button data
                var buttonData = new PushButtonData(
                    commandName,
                    buttonText,
                    ASSEMBLY_PATH,
                    $"RevitAddinTemplate.Commands.{commandName}"
                );

                // Set tooltip
                buttonData.ToolTip = tooltip;

                // Add button to panel
                var button = panel.AddItem(buttonData) as PushButton;

                // Set icon if available
                if (button != null && !string.IsNullOrEmpty(iconName))
                {
                    var iconPath = Path.Combine(Path.GetDirectoryName(_assemblyPath), "Resources", "icons", iconName);
                    if (File.Exists(iconPath))
                    {
                        button.LargeImage = new BitmapImage(new Uri(iconPath));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to add command {commandName} to ribbon panel");
            }
        }
        #endregion
    }
}
