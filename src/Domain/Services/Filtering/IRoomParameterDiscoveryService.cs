using System.Collections.Generic;
using RoomsManagerAddin.Domain.Models.Filtering;

namespace RoomsManagerAddin.Domain.Services.Filtering
{
    /// <summary>
    /// Interface for discovering and retrieving room parameter information.
    /// </summary>
    public interface IRoomParameterDiscoveryService
    {
        /// <summary>
        /// Gets all available parameters for rooms in the document.
        /// </summary>
        /// <param name="useCache">Whether to use cached parameters if available.</param>
        /// <returns>A list of parameter information objects.</returns>
        List<ParameterInfo> GetRoomParameters(bool useCache = true);

        /// <summary>
        /// Gets a specific parameter by name.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to find.</param>
        /// <returns>The parameter information object, or null if not found.</returns>
        ParameterInfo GetParameterByName(string parameterName);
    }
}
