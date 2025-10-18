using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// UI model for generic elements (parallel to RoomItem and WallItem)
    /// </summary>
    public class ElementItem
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string LevelName { get; set; }
        public string TypeName { get; set; }
    }
}
