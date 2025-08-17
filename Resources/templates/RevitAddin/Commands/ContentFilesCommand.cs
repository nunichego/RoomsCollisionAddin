using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAddinTemplate.Windows.ContentBrowser;

namespace RevitAddinTemplate.Commands
{
    /// <summary>
    /// Content Files Command - Opens the content browser window
    /// Demonstrates how to open content management windows
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class ContentFilesCommand : BaseCommand
    {
        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current document for context
                var doc = GetDocument(commandData);
                if (!ValidateDocument(doc, ref message))
                {
                    return Result.Failed;
                }

                // Open the content browser window
                OpenContentBrowser(doc);
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in ContentFilesCommand: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Open the content browser window
        /// </summary>
        private void OpenContentBrowser(Document doc)
        {
            try
            {
                // Create and show the content browser window
                var contentWindow = new ContentBrowserWindow();
                
                // Set the owner to the main Revit window
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    contentWindow.Owner = mainWindow;
                }

                // Show the window as dialog
                contentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Content Browser Error", $"Failed to open content browser: {ex.Message}", ex);
            }
        }
    }
}
