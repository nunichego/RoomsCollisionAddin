using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Models.Shared;
using RoomsManagerAddin.Infrastructure.Logging;
using RoomsManagerAddin.Infrastructure.RevitApi;

namespace RoomsManagerAddin.Domain.Services.Filtering
{
    /// <summary>
    /// Generic filtering service that can work with any Revit element category
    /// </summary>
    public class GenericElementFilterService : IGenericElementFilterService
    {
        private readonly Document _document;
        private readonly ElementParameterDiscoveryService _parameterDiscoveryService;
        private readonly LoggingService _loggingService;
        private readonly ElementCollectorService _elementCollectorService;

        public GenericElementFilterService(Document document, LoggingService loggingService = null)
        {
            _document = document;
            _loggingService = loggingService ?? new LoggingService();
            _parameterDiscoveryService = new ElementParameterDiscoveryService(document, _loggingService);
            _elementCollectorService = new ElementCollectorService();
        }

        /// <summary>
        /// Get all available categories for selection
        /// </summary>
        public List<CategoryInfo> GetAvailableCategories()
        {
            return _parameterDiscoveryService.GetAvailableCategories();
        }

        /// <summary>
        /// Get parameters for a specific category
        /// </summary>
        public List<ParameterInfo> GetParametersForCategory(ElementId categoryId)
        {
            return _parameterDiscoveryService.GetParametersForCategory(categoryId);
        }

        /// <summary>
        /// Get elements of a specific category from the document
        /// </summary>
        public List<Element> GetElementsByCategory(ElementId categoryId)
        {
            try
            {
                var collector = new FilteredElementCollector(_document)
                    .OfCategoryId(categoryId)
                    .WhereElementIsNotElementType();

                var elements = collector.ToList();
                
                _loggingService?.LogInfo($"Found {elements.Count} elements for category {categoryId}");
                return elements;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error collecting elements for category {categoryId}: {ex.Message}");
                return new List<Element>();
            }
        }

        /// <summary>
        /// Create a filter rule for elements
        /// </summary>
        public ElementFilterRule CreateFilterRule(string parameterName, FilterOperator filterOperator, string value)
        {
            try
            {
                var availableParams = _parameterDiscoveryService.GetParametersForCategory(ElementId.InvalidElementId);
                var targetParam = availableParams.FirstOrDefault(p => p.Name == parameterName);

                if (targetParam == null)
                {
                    throw new ArgumentException($"Parameter '{parameterName}' not found in available parameters");
                }

                return new ElementFilterRule
                {
                    Parameter = targetParam,
                    Operator = filterOperator,
                    Value = value
                };
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error creating filter rule: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create a filter configuration for elements
        /// </summary>
        public ElementFilterConfiguration CreateFilterConfiguration(string name, ElementId categoryId)
        {
            return new ElementFilterConfiguration
            {
                Name = name,
                CategoryId = categoryId,
                RootFilterSet = new FilterSet
                {
                    Operator = LogicalOperator.And,
                    Items = new List<IFilterItem>()
                }
            };
        }

        /// <summary>
        /// Apply advanced filter to elements
        /// </summary>
        public List<Element> ApplyAdvancedFilter(ElementFilterConfiguration filterConfig, List<Element> elements)
        {
            try
            {
                if (filterConfig?.RootFilterSet?.Items?.Any() != true)
                {
                    return elements;
                }

                var filteredElements = elements.Where(element => 
                    filterConfig.RootFilterSet.Evaluate(element)
                ).ToList();

                _loggingService?.LogInfo($"Applied filter '{filterConfig.Name}': {filteredElements.Count} of {elements.Count} elements match");
                return filteredElements;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error applying filter: {ex.Message}");
                return elements;
            }
        }

        /// <summary>
        /// Get category name by ID
        /// </summary>
        public string GetCategoryName(ElementId categoryId)
        {
            try
            {
                var category = Category.GetCategory(_document, categoryId);
                return category?.Name ?? "Unknown Category";
            }
            catch
            {
                return "Unknown Category";
            }
        }
    }

    /// <summary>
    /// Filter rule for generic elements (parallel to RoomFilterRule)
    /// </summary>
    public class ElementFilterRule : IFilterItem
    {
        public ParameterInfo Parameter { get; set; }
        public FilterOperator Operator { get; set; }
        public string Value { get; set; }

        public bool Evaluate(Element element)
        {
            if (Parameter == null || element == null)
                return false;

            var param = element.LookupParameter(Parameter.Name);
            if (param == null)
                return false;

            switch (Operator)
            {
                case FilterOperator.HasValue:
                    return param.HasValue;

                case FilterOperator.HasNoValue:
                    return !param.HasValue;

                case FilterOperator.Equals:
                    return CompareValues(param, Value, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase));

                case FilterOperator.NotEquals:
                    return CompareValues(param, Value, (a, b) => !string.Equals(a, b, StringComparison.OrdinalIgnoreCase));

                case FilterOperator.Contains:
                    return CompareValues(param, Value, (a, b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);

                case FilterOperator.NotContains:
                    return CompareValues(param, Value, (a, b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) < 0);

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

            string valueText = Operator == FilterOperator.HasValue || Operator == FilterOperator.HasNoValue
                ? ""
                : $" '{Value}'";

            return $"{Parameter?.Name ?? "Unknown"} {operatorText}{valueText}";
        }
    }

    /// <summary>
    /// Filter configuration for generic elements (parallel to RoomFilterConfiguration)
    /// </summary>
    public class ElementFilterConfiguration
    {
        public string Name { get; set; }
        public ElementId CategoryId { get; set; }
        public FilterSet RootFilterSet { get; set; }

        public List<Element> ApplyFilter(IEnumerable<Element> elements)
        {
            return elements.Where(element => RootFilterSet.Evaluate(element)).ToList();
        }
    }
}