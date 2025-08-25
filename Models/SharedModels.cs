using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Models
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

    /// <summary>
    /// Groups of curves separated by size
    /// </summary>
    public class CurveGroups
    {
        public CurveLoop MainPerimeter { get; set; }
        public List<CurveLoop> Cutouts { get; set; } = new List<CurveLoop>();
    }

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

    /// <summary>
    /// Data container for initial data loading
    /// </summary>
    public class InitialDataResult
    {
        public List<RoomItem> Rooms { get; set; } = new List<RoomItem>();
        public List<WallItem> Walls { get; set; } = new List<WallItem>();
    }
}


