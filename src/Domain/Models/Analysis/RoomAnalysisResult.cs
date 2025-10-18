using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Analysis
{
    /// <summary>
    /// Result of room volume analysis
    /// </summary>
    public class RoomAnalysisResult
    {
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public double Area { get; set; }
        public double Volume { get; set; }
        public string Level { get; set; }
        public Solid RoomSolid { get; set; }
        public List<Element> CollidingElements { get; set; } = new List<Element>();
        public int CollisionCount { get; set; }
        public string ErrorMessage { get; set; }
    }
}
