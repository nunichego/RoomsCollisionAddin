using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// UI model for ceiling items
    /// </summary>
    public class CeilingItem
    {
        public string Name { get; set; }
        public string LevelName { get; set; }
        public string CeilingTypeName { get; set; }
        public double Area { get; set; }
        public ElementId Id { get; set; }
    }
}
