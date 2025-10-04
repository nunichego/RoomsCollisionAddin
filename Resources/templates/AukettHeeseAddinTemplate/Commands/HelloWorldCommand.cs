using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace YourAddinName.Commands
{
    /// <summary>
    /// Simple Hello World command to test the add-in
    /// Replace this with your actual functionality
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class HelloWorldCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData, 
            ref string message, 
            ElementSet elements)
        {
            try
            {
                // Get the active document
                var doc = commandData.Application.ActiveUIDocument?.Document;
                
                if (doc == null)
                {
                    TaskDialog.Show("Error", "No active Revit document found.");
                    return Result.Failed;
                }

                // Show Hello World message with document info
                var projectName = doc.Title ?? "Untitled";
                var messageText = $"Hello World from your Aukett + Heese add-in!\n\n" +
                                 $"Current project: {projectName}\n" +
                                 $"Add-in is working correctly!";

                TaskDialog.Show("Hello World", messageText);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}