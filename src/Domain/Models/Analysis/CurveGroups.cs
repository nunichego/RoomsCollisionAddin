using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Analysis
{
    /// <summary>
    /// Groups of curves separated by size
    /// </summary>
    public class CurveGroups
    {
        public CurveLoop MainPerimeter { get; set; }
        public List<CurveLoop> Cutouts { get; set; } = new List<CurveLoop>();
    }
}
