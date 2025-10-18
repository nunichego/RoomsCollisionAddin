using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RoomsManagerAddin.Application.Commands
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
                TaskDialog.Show("RoomDataSync Help", "Hi zusammen ;)");

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
