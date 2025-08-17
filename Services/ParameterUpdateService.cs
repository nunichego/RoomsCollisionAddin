using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Microsoft.Extensions.Logging;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for updating element parameters
    /// </summary>
    public class ParameterUpdateService
    {
        private readonly ILogger _logger;

        public ParameterUpdateService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Update room Filter Tag parameter
        /// </summary>
        public void UpdateRoomFilterTag(Room room, string filterTagValue)
        {
            try
            {
                // Try common parameter names for Filter Tag
                var parameterNames = new[] { "Filter Tag", "Filter", "Tag", "Comments", "Mark" };
                
                foreach (var paramName in parameterNames)
                {
                    var parameter = room.LookupParameter(paramName);
                    if (parameter != null && parameter.StorageType == StorageType.String)
                    {
                        parameter.Set(filterTagValue);
                        _logger?.LogDebug($"Updated room {room.Number} parameter '{paramName}' with: {filterTagValue}");
                        return;
                    }
                }

                _logger?.LogWarning($"Could not find suitable parameter for room {room.Number}");
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, $"Error updating room {room.Number} Filter Tag");
            }
        }

        /// <summary>
        /// Update wall Filter Tag parameter
        /// </summary>
        public void UpdateWallFilterTag(Wall wall, string filterTagValue)
        {
            try
            {
                // Try common parameter names for Filter Tag
                var parameterNames = new[] { "Filter Tag", "Filter", "Tag", "Comments", "Mark" };
                
                foreach (var paramName in parameterNames)
                {
                    var parameter = wall.LookupParameter(paramName);
                    if (parameter != null && parameter.StorageType == StorageType.String)
                    {
                        parameter.Set(filterTagValue);
                        _logger?.LogDebug($"Updated wall {wall.Id} parameter '{paramName}' with: {filterTagValue}");
                        return;
                    }
                }

                _logger?.LogWarning($"Could not find suitable parameter for wall {wall.Id}");
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, $"Error updating wall {wall.Id} Filter Tag");
            }
        }
    }
}
