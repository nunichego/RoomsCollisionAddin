using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RoomsManagerAddin.Commands
{
    /// <summary>
    /// Help command for RoomDataSync add-in
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class HelpCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TaskDialog.Show("RoomDataSync Help", 
                    "RoomDataSync - Room Collision Analysis Tool\n\n" +
                    "What it does:\n" +
                    "• Analyzes collisions between rooms and walls\n" +
                    "• Updates room parameters with collision information\n" +
                    "• Generates detailed analysis logs\n" +
                    "• Provides performance-optimized analysis\n\n" +
                    "How to use:\n" +
                    "1. Open a Revit project with rooms\n" +
                    "2. Click 'Room Collision Analysis' button\n" +
                    "3. Wait for analysis to complete\n" +
                    "4. Check results in the summary dialog\n" +
                    "5. Review detailed logs on your desktop\n\n" +
                    "For more information, contact your BIM administrator.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error showing help: {ex.Message}";
                TaskDialog.Show("RoomDataSync Error", message);
                return Result.Failed;
            }
        }
    }
}
