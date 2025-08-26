using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    public class RoomFilterService
    {
        private readonly Document _document;
        private readonly RoomParameterDiscoveryService _parameterDiscoveryService;
        private readonly LoggingService _loggingService;
        private readonly ElementCollectorService _elementCollectorService;

        public RoomFilterService(Document document, LoggingService loggingService = null)
        {
            _document = document;
            _loggingService = loggingService ?? new LoggingService();
            _parameterDiscoveryService = new RoomParameterDiscoveryService(document, _loggingService);
            _elementCollectorService = new ElementCollectorService();
        }

        public List<ParameterInfo> GetAvailableParameters()
        {
            return _parameterDiscoveryService.GetRoomParameters();
        }

        public RoomFilterRule CreateFilterRule(string parameterName, FilterOperator filterOperator, string value)
        {
            var parameterInfo = _parameterDiscoveryService.GetParameterByName(parameterName);
            if (parameterInfo == null)
            {
                throw new ArgumentException($"Parameter '{parameterName}' not found");
            }

            var availableOperators = parameterInfo.GetAvailableOperators();
            if (!availableOperators.Contains(filterOperator))
            {
                throw new ArgumentException($"Operator '{filterOperator}' is not valid for parameter type '{parameterInfo.DataType}'");
            }

            return new RoomFilterRule
            {
                Parameter = parameterInfo,
                Operator = filterOperator,
                Value = value ?? ""
            };
        }

        public FilterSet CreateFilterSet(LogicalOperator logicalOperator)
        {
            return new FilterSet
            {
                Operator = logicalOperator,
                Items = new List<IFilterItem>()
            };
        }

        public RoomFilterConfiguration CreateFilterConfiguration(string name)
        {
            return new RoomFilterConfiguration
            {
                Name = name,
                RootFilterSet = CreateFilterSet(LogicalOperator.And),
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
        }

        public List<RoomItem> ApplyFilter(RoomFilterConfiguration filterConfig)
        {
            try
            {
                _loggingService.WriteToLog($"Applying filter configuration: {filterConfig.Name}");

                // Get all rooms from document
                var rooms = _elementCollectorService.GetRooms(_document);
                
                if (!rooms.Any())
                {
                    _loggingService.WriteToLog("No rooms found in document");
                    return new List<RoomItem>();
                }

                _loggingService.WriteToLog($"Filtering {rooms.Count} rooms");

                // Apply the filter
                var filteredRooms = filterConfig.ApplyFilter(rooms.Cast<Element>());

                // Convert to RoomItem objects
                var roomItems = filteredRooms.Cast<Room>().Select(r => new RoomItem
                {
                    Name = r.Name,
                    Number = r.Number,
                    LevelName = r.Level?.Name ?? "Unknown",
                    Area = r.Area,
                    Volume = r.Volume,
                    Id = r.Id
                }).ToList();

                _loggingService.WriteToLog($"Filter result: {roomItems.Count} rooms match criteria");

                return roomItems;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error applying filter: {ex.Message}");
                throw;
            }
        }

        public bool ValidateFilterRule(RoomFilterRule rule)
        {
            try
            {
                if (rule == null || rule.Parameter == null)
                    return false;

                var availableOperators = rule.Parameter.GetAvailableOperators();
                if (!availableOperators.Contains(rule.Operator))
                    return false;

                // Validate value based on parameter type and operator
                if (rule.Operator != FilterOperator.HasValue && rule.Operator != FilterOperator.HasNoValue)
                {
                    if (string.IsNullOrEmpty(rule.Value))
                        return false;

                    // Additional validation based on parameter type
                    switch (rule.Parameter.DataType)
                    {
                        case ParameterDataType.Integer:
                            return int.TryParse(rule.Value, out _);
                        
                        case ParameterDataType.Double:
                            return double.TryParse(rule.Value, out _);
                        
                        case ParameterDataType.YesNo:
                            return rule.Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) || 
                                   rule.Value.Equals("No", StringComparison.OrdinalIgnoreCase) ||
                                   rule.Value.Equals("1") || rule.Value.Equals("0");
                        
                        case ParameterDataType.Text:
                        case ParameterDataType.ElementId:
                        default:
                            return true; // Text values are generally valid
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error validating filter rule: {ex.Message}");
                return false;
            }
        }

        public bool ValidateFilterSet(FilterSet filterSet)
        {
            try
            {
                if (filterSet == null || !filterSet.Items.Any())
                    return true; // Empty filter set is valid (matches all)

                foreach (var item in filterSet.Items)
                {
                    if (item is RoomFilterRule rule)
                    {
                        if (!ValidateFilterRule(rule))
                            return false;
                    }
                    else if (item is FilterSet subSet)
                    {
                        if (!ValidateFilterSet(subSet))
                            return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error validating filter set: {ex.Message}");
                return false;
            }
        }

        public int CountMatchingRooms(RoomFilterConfiguration filterConfig)
        {
            try
            {
                var rooms = _elementCollectorService.GetRooms(_document);
                var filteredRooms = filterConfig.ApplyFilter(rooms.Cast<Element>());
                return filteredRooms.Count;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error counting matching rooms: {ex.Message}");
                return 0;
            }
        }

        public string GetFilterDescription(RoomFilterConfiguration filterConfig)
        {
            try
            {
                if (filterConfig?.RootFilterSet == null || !filterConfig.RootFilterSet.Items.Any())
                    return "No filter criteria specified";

                return filterConfig.RootFilterSet.GetDescription();
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error getting filter description: {ex.Message}");
                return "Error generating description";
            }
        }

        public List<FilterOperator> GetAvailableOperators(string parameterName)
        {
            var parameterInfo = _parameterDiscoveryService.GetParameterByName(parameterName);
            return parameterInfo?.GetAvailableOperators() ?? new List<FilterOperator>();
        }

        public ParameterDataType GetParameterDataType(string parameterName)
        {
            var parameterInfo = _parameterDiscoveryService.GetParameterByName(parameterName);
            return parameterInfo?.DataType ?? ParameterDataType.Unknown;
        }

        public void RefreshParameterCache()
        {
            _parameterDiscoveryService.ClearCache();
            _loggingService.WriteToLog("Parameter cache refreshed");
        }
    }
}