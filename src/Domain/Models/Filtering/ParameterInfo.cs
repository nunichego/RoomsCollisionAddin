using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Filtering
{
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
                    // Check if this is actually a Yes/No parameter
                    if (IsYesNoParameter(parameter))
                    {
                        return ParameterDataType.YesNo;
                    }
                    return ParameterDataType.Integer;
                case StorageType.Double:
                    return ParameterDataType.Double;
                case StorageType.ElementId:
                    return ParameterDataType.ElementId;
                default:
                    return ParameterDataType.Unknown;
            }
        }

        private static bool IsYesNoParameter(Parameter parameter)
        {
            try
            {
                // Method 1: Check parameter definition for Yes/No type
                if (parameter.Definition is InternalDefinition internalDef)
                {
                    // Common Yes/No built-in parameters
                    var yesNoParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "Room Bounding", "Structural", "Bearing", "Enabled in Project",
                        "Visible", "Shared", "Load Bearing", "Structural Usage",
                        "Visible in Plan", "Show in Schedule", "Assembly"
                    };

                    if (yesNoParameterNames.Contains(parameter.Definition.Name))
                    {
                        return true;
                    }
                }

                // Method 2: Try to determine from parameter value range
                // Yes/No parameters typically have values 0 or 1
                if (parameter.HasValue)
                {
                    int value = parameter.AsInteger();
                    // If it's 0 or 1, and the parameter name suggests boolean, treat as Yes/No
                    if ((value == 0 || value == 1) &&
                        (parameter.Definition.Name.Contains("Enable") ||
                         parameter.Definition.Name.Contains("Show") ||
                         parameter.Definition.Name.Contains("Visible") ||
                         parameter.Definition.Name.Contains("Bound")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
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
}
