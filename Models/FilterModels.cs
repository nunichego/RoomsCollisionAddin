using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Models
{
    public enum ParameterDataType
    {
        Text,
        Integer, 
        Double,
        YesNo,
        ElementId,
        Unknown
    }

    public enum LogicalOperator
    {
        And,
        Or
    }

    public enum FilterOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        BeginsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        HasValue,
        HasNoValue
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public ParameterDataType DataType { get; set; }
        public StorageType StorageType { get; set; }
        public BuiltInParameter? BuiltInParameterId { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool IsShared { get; set; }
        public bool IsReadOnly { get; set; }
        public List<string> PossibleValues { get; set; } = new List<string>();

        public static ParameterDataType GetParameterDataType(Parameter parameter)
        {
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return ParameterDataType.Text;
                case StorageType.Integer:
                    // For Revit 2024, we need to check differently for YesNo parameters
                    // Most integer parameters in rooms are actually numeric values
                    return ParameterDataType.Integer;
                case StorageType.Double:
                    return ParameterDataType.Double;
                case StorageType.ElementId:
                    return ParameterDataType.ElementId;
                default:
                    return ParameterDataType.Unknown;
            }
        }

        public List<FilterOperator> GetAvailableOperators()
        {
            switch (DataType)
            {
                case ParameterDataType.Text:
                    return new List<FilterOperator>
                    {
                        FilterOperator.Equals,
                        FilterOperator.NotEquals,
                        FilterOperator.Contains,
                        FilterOperator.NotContains,
                        FilterOperator.BeginsWith,
                        FilterOperator.EndsWith,
                        FilterOperator.HasValue,
                        FilterOperator.HasNoValue
                    };

                case ParameterDataType.Integer:
                case ParameterDataType.Double:
                    return new List<FilterOperator>
                    {
                        FilterOperator.Equals,
                        FilterOperator.NotEquals,
                        FilterOperator.GreaterThan,
                        FilterOperator.LessThan,
                        FilterOperator.GreaterThanOrEqual,
                        FilterOperator.LessThanOrEqual,
                        FilterOperator.HasValue,
                        FilterOperator.HasNoValue
                    };

                case ParameterDataType.YesNo:
                case ParameterDataType.ElementId:
                    return new List<FilterOperator>
                    {
                        FilterOperator.Equals,
                        FilterOperator.NotEquals,
                        FilterOperator.HasValue,
                        FilterOperator.HasNoValue
                    };

                default:
                    return new List<FilterOperator>
                    {
                        FilterOperator.Equals,
                        FilterOperator.NotEquals,
                        FilterOperator.HasValue,
                        FilterOperator.HasNoValue
                    };
            }
        }
    }

    public interface IFilterItem
    {
        bool Evaluate(Element element);
        string GetDescription();
    }

    public class RoomFilterRule : IFilterItem
    {
        public ParameterInfo Parameter { get; set; }
        public FilterOperator Operator { get; set; }
        public string Value { get; set; }

        public bool Evaluate(Element element)
        {
            try
            {
                Parameter param = null;
                
                if (Parameter.IsBuiltIn && Parameter.BuiltInParameterId.HasValue)
                {
                    param = element.get_Parameter(Parameter.BuiltInParameterId.Value);
                }
                else
                {
                    param = element.LookupParameter(Parameter.Name);
                }

                if (param == null)
                {
                    // Debug: Parameter not found
                    System.Diagnostics.Debug.WriteLine($"Parameter '{Parameter.Name}' not found on element {element.Id}");
                    return Operator == FilterOperator.HasNoValue;
                }

                var result = EvaluateParameter(param);
                
                // Debug: Log evaluation
                var paramValue = param.HasValue ? (param.AsValueString() ?? param.AsString() ?? param.AsDouble().ToString()) : "NULL";
                System.Diagnostics.Debug.WriteLine($"Parameter '{Parameter.Name}' = '{paramValue}' {Operator} '{Value}' = {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating parameter '{Parameter.Name}': {ex.Message}");
                return false;
            }
        }

        private bool EvaluateParameter(Parameter param)
        {
            switch (Operator)
            {
                case FilterOperator.HasValue:
                    return param.HasValue;
                
                case FilterOperator.HasNoValue:
                    return !param.HasValue;
                
                case FilterOperator.Equals:
                    return CompareValues(param, Value, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase));
                
                case FilterOperator.NotEquals:
                    return !CompareValues(param, Value, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase));
                
                case FilterOperator.Contains:
                    return CompareValues(param, Value, (a, b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);
                
                case FilterOperator.NotContains:
                    return !CompareValues(param, Value, (a, b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);
                
                case FilterOperator.BeginsWith:
                    return CompareValues(param, Value, (a, b) => a.StartsWith(b, StringComparison.OrdinalIgnoreCase));
                
                case FilterOperator.EndsWith:
                    return CompareValues(param, Value, (a, b) => a.EndsWith(b, StringComparison.OrdinalIgnoreCase));
                
                case FilterOperator.GreaterThan:
                    return CompareNumeric(param, Value, (a, b) => a > b);
                
                case FilterOperator.LessThan:
                    return CompareNumeric(param, Value, (a, b) => a < b);
                
                case FilterOperator.GreaterThanOrEqual:
                    return CompareNumeric(param, Value, (a, b) => a >= b);
                
                case FilterOperator.LessThanOrEqual:
                    return CompareNumeric(param, Value, (a, b) => a <= b);
                
                default:
                    return false;
            }
        }

        private bool CompareValues(Parameter param, string value, Func<string, string, bool> comparer)
        {
            if (!param.HasValue || string.IsNullOrEmpty(value))
                return false;

            string paramValue = param.AsValueString() ?? param.AsString() ?? "";
            return comparer(paramValue, value);
        }

        private bool CompareNumeric(Parameter param, string value, Func<double, double, bool> comparer)
        {
            if (!param.HasValue || !double.TryParse(value, out double compareValue))
                return false;

            double paramValue = 0;
            switch (param.StorageType)
            {
                case StorageType.Double:
                    paramValue = param.AsDouble();
                    break;
                case StorageType.Integer:
                    paramValue = param.AsInteger();
                    break;
                default:
                    return false;
            }

            return comparer(paramValue, compareValue);
        }

        public string GetDescription()
        {
            string operatorText;
            switch (Operator)
            {
                case FilterOperator.Equals:
                    operatorText = "equals";
                    break;
                case FilterOperator.NotEquals:
                    operatorText = "does not equal";
                    break;
                case FilterOperator.Contains:
                    operatorText = "contains";
                    break;
                case FilterOperator.NotContains:
                    operatorText = "does not contain";
                    break;
                case FilterOperator.BeginsWith:
                    operatorText = "begins with";
                    break;
                case FilterOperator.EndsWith:
                    operatorText = "ends with";
                    break;
                case FilterOperator.GreaterThan:
                    operatorText = "is greater than";
                    break;
                case FilterOperator.LessThan:
                    operatorText = "is less than";
                    break;
                case FilterOperator.GreaterThanOrEqual:
                    operatorText = "is greater than or equal to";
                    break;
                case FilterOperator.LessThanOrEqual:
                    operatorText = "is less than or equal to";
                    break;
                case FilterOperator.HasValue:
                    operatorText = "has a value";
                    break;
                case FilterOperator.HasNoValue:
                    operatorText = "has no value";
                    break;
                default:
                    operatorText = Operator.ToString();
                    break;
            }

            if (Operator == FilterOperator.HasValue || Operator == FilterOperator.HasNoValue)
                return $"Rooms {Parameter.Name} {operatorText}";
            
            return $"Rooms {Parameter.Name} {operatorText} {Value}";
        }
    }

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

    /// <summary>
    /// Information about a Revit category for selection and filtering
    /// </summary>
    public class CategoryInfo
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public CategoryType CategoryType { get; set; }
        
        public override string ToString()
        {
            return Name;
        }
    }
}