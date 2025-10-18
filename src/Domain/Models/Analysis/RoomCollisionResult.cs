using System.Collections.Generic;

namespace RoomsManagerAddin.Domain.Models.Analysis
{
    /// <summary>
    /// Result of room collision analysis
    /// </summary>
    public class RoomCollisionResult
    {
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public string Level { get; set; }
        public double RoomSolidVolume { get; set; }
        public int RoomSolidFaces { get; set; }
        public int WallsColliding { get; set; }
        public List<string> WallTypes { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }
}
