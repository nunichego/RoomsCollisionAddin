using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAddinTemplate.Windows.AdminPanel;

namespace RevitAddinTemplate.Commands
{
    /// <summary>
    /// Admin Panel Command - Opens the admin panel window
    /// Demonstrates how to open WPF windows from commands
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class AdminPanelCommand : BaseCommand
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

                // Open the admin panel window
                OpenAdminPanel(doc);
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in AdminPanelCommand: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Open the admin panel window
        /// </summary>
        private void OpenAdminPanel(Document doc)
        {
            try
            {
                // Create and show the admin panel window
                var adminWindow = new AdminPanelWindow();
                
                // Set the owner to the main Revit window
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    adminWindow.Owner = mainWindow;
                }

                // Show the window as dialog
                adminWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Admin Panel Error", $"Failed to open admin panel: {ex.Message}", ex);
            }
        }
    }
}
