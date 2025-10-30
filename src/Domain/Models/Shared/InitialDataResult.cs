using System.Collections.Generic;

namespace RoomsManagerAddin.Domain.Models.Shared
{
    /// <summary>
    /// Data container for initial data loading
    /// </summary>
    public class InitialDataResult
    {
        public List<RoomItem> Rooms { get; set; } = new List<RoomItem>();
        public List<WallItem> Walls { get; set; } = new List<WallItem>();
        public List<FloorItem> Floors { get; set; } = new List<FloorItem>();
        public List<CeilingItem> Ceilings { get; set; } = new List<CeilingItem>();
    }
}
