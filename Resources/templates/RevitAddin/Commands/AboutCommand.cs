using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAddinTemplate.Commands
{
    /// <summary>
    /// About Command - Shows information about the add-in
    /// Demonstrates a simple command implementation
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class AboutCommand : BaseCommand
    {
        protected override Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Show about information
                ShowAboutDialog();
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in AboutCommand: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Show the about dialog
        /// </summary>
        private void ShowAboutDialog()
        {
            var aboutText = 
                "RevitAddinTemplate\n\n" +
                "Version: 1.0.0\n" +
                "Target: Revit 2024\n" +
                "Framework: .NET Framework 4.8\n\n" +
                "A comprehensive template for Revit add-in development\n" +
                "with modular architecture and modern UI patterns.\n\n" +
                "Features:\n" +
                "• Modular command structure\n" +
                "• Dependency injection\n" +
                "• Comprehensive logging\n" +
                "• Modern WPF templates\n" +
                "• Content management system\n" +
                "• Admin panel functionality\n\n" +
                "© 2024 Your Company Name";

            ShowInfo("About RevitAddinTemplate", aboutText);
        }
    }
}
