using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Filtering
{
    public class RoomFilterConfiguration
    {
        public string Name { get; set; }
        public FilterSet RootFilterSet { get; set; } = new FilterSet();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public List<Element> ApplyFilter(IEnumerable<Element> rooms)
        {
            return rooms.Where(room => RootFilterSet.Evaluate(room)).ToList();
        }
    }
}
