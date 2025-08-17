using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAddinTemplate.Windows.DynamoScripts;

namespace RevitAddinTemplate.Commands
{
    /// <summary>
    /// Dynamo Scripts Command - Opens the Dynamo scripts management window
    /// Demonstrates how to open Dynamo scripts management windows
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class DynamoScriptsCommand : BaseCommand
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

                // Open the Dynamo scripts window
                OpenDynamoScriptsManager(doc);
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in DynamoScriptsCommand: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Open the Dynamo scripts management window
        /// </summary>
        private void OpenDynamoScriptsManager(Document doc)
        {
            try
            {
                // Create and show the Dynamo scripts window
                var dynamoWindow = new DynamoScriptsWindow();
                
                // Set the owner to the main Revit window
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    dynamoWindow.Owner = mainWindow;
                }

                // Show the window as dialog
                dynamoWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Dynamo Scripts Error", $"Failed to open Dynamo scripts manager: {ex.Message}", ex);
            }
        }
    }
}
