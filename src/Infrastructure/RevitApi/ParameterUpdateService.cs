using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

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
                        // Updated room parameter
                        return;
                    }
                }

                // Could not find suitable parameter for room
            }
                         catch (System.Exception)
             {
                 // Error updating room Filter Tag
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
                        // Updated wall parameter
                        return;
                    }
                }

                // Could not find suitable parameter for wall
            }
                         catch (System.Exception)
             {
                 // Error updating wall Filter Tag
             }
        }
    }
}
