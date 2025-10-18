using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Filtering
{
    public class FilterSet : IFilterItem
    {
        public LogicalOperator Operator { get; set; } = LogicalOperator.And;
        public List<IFilterItem> Items { get; set; } = new List<IFilterItem>();

        public bool Evaluate(Element element)
        {
            if (!Items.Any())
                return true;

            switch (Operator)
            {
                case LogicalOperator.And:
                    return Items.All(item => item.Evaluate(element));

                case LogicalOperator.Or:
                    return Items.Any(item => item.Evaluate(element));

                default:
                    return true;
            }
        }

        public string GetDescription()
        {
            var operatorText = Operator == LogicalOperator.And ? "AND" : "OR";
            var itemDescriptions = Items.Select(item => item.GetDescription());
            return $"{operatorText} ({string.Join($" {operatorText} ", itemDescriptions)})";
        }
    }
}
