using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// UI model for floor items
    /// </summary>
    public class FloorItem
    {
        public string Name { get; set; }
        public string LevelName { get; set; }
        public string FloorTypeName { get; set; }
        public double Area { get; set; }
        public ElementId Id { get; set; }
    }
}
