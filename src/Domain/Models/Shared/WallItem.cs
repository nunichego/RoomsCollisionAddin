using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// UI model for wall items
    /// </summary>
    public class WallItem
    {
        public string Name { get; set; }
        public string LevelName { get; set; }
        public string WallTypeName { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }
        public ElementId Id { get; set; }
    }
}
