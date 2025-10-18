using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// UI model for room items
    /// </summary>
    public class RoomItem
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string LevelName { get; set; }
        public double Area { get; set; }
        public double Volume { get; set; }
        public ElementId Id { get; set; }
    }
}
