using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    public class RoomParameterDiscoveryService
    {
        private readonly Document _document;
        private List<ParameterInfo> _cachedParameters;
        private readonly LoggingService _loggingService;

        public RoomParameterDiscoveryService(Document document, LoggingService loggingService = null)
        {
            _document = document;
            _loggingService = loggingService ?? new LoggingService();
        }

        public List<ParameterInfo> GetRoomParameters(bool useCache = true)
        {
            if (useCache && _cachedParameters != null)
                return _cachedParameters;

            try
            {
                _loggingService.WriteToLog("Starting room parameter discovery...");
                
                var parameterMap = new Dictionary<string, ParameterInfo>();
                var rooms = GetSampleRooms();

                if (!rooms.Any())
                {
                    _loggingService.WriteToLog("No rooms found in document for parameter discovery");
                    return new List<ParameterInfo>();
                }

                _loggingService.WriteToLog($"Analyzing parameters from {rooms.Count} sample rooms");

                foreach (var room in rooms)
                {
                    DiscoverParametersFromRoom(room, parameterMap);
                }

                _cachedParameters = parameterMap.Values
                    .OrderBy(p => p.Name)
                    .ToList();

                _loggingService.WriteToLog($"Discovered {_cachedParameters.Count} unique room parameters");
                
                return _cachedParameters;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error during room parameter discovery: {ex.Message}");
                return new List<ParameterInfo>();
            }
        }

        private List<Room> GetSampleRooms()
        {
            try
            {
                // Get all rooms from document
                var allRooms = new FilteredElementCollector(_document)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>()
                    .Where(r => r != null)
                    .ToList();

                // For performance, sample up to 20 rooms (or all if less than 20)
                // This should capture most parameter variations without being too slow
                var sampleSize = Math.Min(20, allRooms.Count);
                
                if (allRooms.Count <= sampleSize)
                    return allRooms;

                // Take evenly distributed sample
                var step = allRooms.Count / sampleSize;
                var sampleRooms = new List<Room>();
                
                for (int i = 0; i < allRooms.Count; i += step)
                {
                    sampleRooms.Add(allRooms[i]);
                    if (sampleRooms.Count >= sampleSize)
                        break;
                }

                return sampleRooms;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error getting sample rooms: {ex.Message}");
                return new List<Room>();
            }
        }

        private void DiscoverParametersFromRoom(Room room, Dictionary<string, ParameterInfo> parameterMap)
        {
            try
            {
                // Use GetOrderedParameters to get visible parameters in UI order
                var parameters = room.GetOrderedParameters();

                foreach (var param in parameters)
                {
                    try
                    {
                        var paramName = param.Definition.Name;
                        
                        // Skip if we already have this parameter
                        if (parameterMap.ContainsKey(paramName))
                            continue;

                        var paramInfo = CreateParameterInfo(param);
                        if (paramInfo != null)
                        {
                            parameterMap[paramName] = paramInfo;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.WriteToLog($"Error processing parameter {param?.Definition?.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error discovering parameters from room {room.Id}: {ex.Message}");
            }
        }

        private ParameterInfo CreateParameterInfo(Parameter parameter)
        {
            try
            {
                var paramInfo = new ParameterInfo
                {
                    Name = parameter.Definition.Name,
                    DataType = ParameterInfo.GetParameterDataType(parameter),
                    StorageType = parameter.StorageType,
                    IsBuiltIn = parameter.IsShared == false && parameter.Definition is InternalDefinition,
                    IsShared = parameter.IsShared
                };

                // Try to get built-in parameter ID for built-in parameters
                if (paramInfo.IsBuiltIn && parameter.Definition is InternalDefinition internalDef)
                {
                    try
                    {
                        paramInfo.BuiltInParameterId = internalDef.BuiltInParameter != BuiltInParameter.INVALID 
                            ? internalDef.BuiltInParameter 
                            : (BuiltInParameter?)null;
                    }
                    catch
                    {
                        // Some parameters may not have valid built-in parameter IDs
                        paramInfo.BuiltInParameterId = (BuiltInParameter?)null;
                    }
                }

                // For ElementId parameters, we could potentially discover possible values
                // but this would require more complex logic and performance considerations
                if (paramInfo.DataType == ParameterDataType.ElementId)
                {
                    // TODO: Implement ElementId possible values discovery if needed
                    // This would involve scanning the document for elements of the appropriate type
                }

                return paramInfo;
            }
            catch (Exception ex)
            {
                _loggingService.WriteToLog($"Error creating parameter info for {parameter?.Definition?.Name}: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            _cachedParameters = null;
            _loggingService.WriteToLog("Room parameter cache cleared");
        }

        public ParameterInfo GetParameterByName(string parameterName)
        {
            var parameters = GetRoomParameters();
            return parameters.FirstOrDefault(p => 
                string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
        }

        public List<ParameterInfo> GetParametersByType(ParameterDataType dataType)
        {
            var parameters = GetRoomParameters();
            return parameters.Where(p => p.DataType == dataType).ToList();
        }

        public List<string> GetParameterNames()
        {
            var parameters = GetRoomParameters();
            return parameters.Select(p => p.Name).ToList();
        }

        public List<string> GetBuiltInParameterNames()
        {
            var parameters = GetRoomParameters();
            return parameters.Where(p => p.IsBuiltIn).Select(p => p.Name).ToList();
        }

        public List<string> GetSharedParameterNames()
        {
            var parameters = GetRoomParameters();
            return parameters.Where(p => p.IsShared).Select(p => p.Name).ToList();
        }
    }
}