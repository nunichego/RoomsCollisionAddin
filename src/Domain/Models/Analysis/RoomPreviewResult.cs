using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Analysis
{
    /// <summary>
    /// Result of room geometry preview creation
    /// </summary>
    public class RoomPreviewResult
    {
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public string Level { get; set; }
        public bool PreviewCreated { get; set; }
        public ElementId DirectShapeId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
