using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Core.Exceptions;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for updating element parameters
    /// </summary>
    public class ParameterUpdateService
    {
        public ParameterUpdateService()
        {
        }

        /// <summary>
        /// Update room Filter Tag parameter
        /// </summary>
        /// <param name="room">The room to update</param>
        /// <param name="filterTagValue">The value to set for the filter tag</param>
        /// <exception cref="ArgumentNullException">Thrown when room is null or filterTagValue is null/empty</exception>
        /// <exception cref="RevitApiException">Thrown when parameter update fails</exception>
        public void UpdateRoomFilterTag(Room room, string filterTagValue)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room), "Room cannot be null");
            if (string.IsNullOrEmpty(filterTagValue))
                throw new ArgumentNullException(nameof(filterTagValue), "Filter tag value cannot be null or empty");

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
                        return;
                    }
                }

                // Could not find suitable parameter - this is a warning, not an error
                throw new RevitApiException($"No suitable Filter Tag parameter found for room {room.Id}", null)
                {
                    UserMessage = $"Room {room.Number} does not have a compatible Filter Tag parameter. Please add a text parameter named 'Filter Tag' to the room."
                };
            }
            catch (RevitApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RevitApiException($"updating Filter Tag for room {room.Id}", ex);
            }
        }

        /// <summary>
        /// Update wall Filter Tag parameter
        /// </summary>
        /// <param name="wall">The wall to update</param>
        /// <param name="filterTagValue">The value to set for the filter tag</param>
        /// <exception cref="ArgumentNullException">Thrown when wall is null or filterTagValue is null/empty</exception>
        /// <exception cref="RevitApiException">Thrown when parameter update fails</exception>
        public void UpdateWallFilterTag(Wall wall, string filterTagValue)
        {
            if (wall == null)
                throw new ArgumentNullException(nameof(wall), "Wall cannot be null");
            if (string.IsNullOrEmpty(filterTagValue))
                throw new ArgumentNullException(nameof(filterTagValue), "Filter tag value cannot be null or empty");

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
                        return;
                    }
                }

                // Could not find suitable parameter - this is a warning, not an error
                throw new RevitApiException($"No suitable Filter Tag parameter found for wall {wall.Id}", null)
                {
                    UserMessage = $"Wall {wall.Id} does not have a compatible Filter Tag parameter. Please add a text parameter named 'Filter Tag' to the wall."
                };
            }
            catch (RevitApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RevitApiException($"updating Filter Tag for wall {wall.Id}", ex);
            }
        }
    }
}
