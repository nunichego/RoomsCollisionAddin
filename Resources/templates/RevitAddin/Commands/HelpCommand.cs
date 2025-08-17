using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAddinTemplate.Commands
{
    /// <summary>
    /// Help Command - Shows help information about the add-in
    /// Demonstrates how to provide help functionality
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class HelpCommand : BaseCommand
    {
        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Show help information
                ShowHelpDialog();
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in HelpCommand: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Show the help dialog
        /// </summary>
        private void ShowHelpDialog()
        {
            var helpText = 
                "RevitAddinTemplate Help\n\n" +
                "Available Commands:\n" +
                "• About - Shows information about this add-in\n" +
                "• Admin Panel - Opens the admin panel for configuration\n" +
                "• Content Files - Manages content files and libraries\n" +
                "• Dynamo Scripts - Manages Dynamo scripts\n" +
                "• Help - Shows this help information\n" +
                "• Settings - Opens the settings panel\n\n" +
                "Features:\n" +
                "• Modular command architecture\n" +
                "• Dependency injection\n" +
                "• Comprehensive logging\n" +
                "• Modern WPF templates\n" +
                "• JSON-based configuration\n" +
                "• Content management system\n" +
                "• Admin panel functionality\n\n" +
                "For more information, visit the project documentation.\n\n" +
                "© 2024 Your Company Name";

            ShowInfo("Help - RevitAddinTemplate", helpText);
        }
    }
}
