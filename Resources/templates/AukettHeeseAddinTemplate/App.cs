using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace YourAddinName
{
    /// <summary>
    /// Main Revit Add-in Application Class
    /// Creates ribbon panel under shared "Aukett + Heese" tab
    /// Handles the case where the tab already exists from other add-ins
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        #region Constants
        private const string TAB_NAME = "Aukett + Heese";
        private const string PANEL_NAME = "Your Panel Name";  // TODO: Change this to your panel name
        private const string ASSEMBLY_PATH = "YourAddinName.dll";  // TODO: Change this to your DLL name
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

                // Create custom ribbon tab and panel
                CreateRibbonPanel(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to start {PANEL_NAME} Add-in: {ex.Message}");
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
                TaskDialog.Show("Error", $"Error during {PANEL_NAME} Add-in shutdown: {ex.Message}");
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create the custom ribbon tab and panel
        /// Handles the case where "Aukett + Heese" tab already exists
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

                // Add Hello World button
                AddHelloWorldButton(panel);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create ribbon panel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Add the Hello World button to the ribbon panel
        /// </summary>
        private void AddHelloWorldButton(RibbonPanel panel)
        {
            try
            {
                // Create button data
                var buttonData = new PushButtonData(
                    "HelloWorld",
                    "Hello World",
                    _assemblyPath,
                    "YourAddinName.Commands.HelloWorldCommand"  // TODO: Update namespace if changed
                );

                // Set button properties
                buttonData.ToolTip = "Click to show Hello World message";
                buttonData.LongDescription = "A simple Hello World command to test the add-in is working correctly.";

                // Load icon (using help icon as example)
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
                throw new Exception($"Failed to add Hello World button: {ex.Message}", ex);
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
                var resourceName = $"YourAddinName.Resources.icons.{iconName}";  // TODO: Update namespace if changed

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