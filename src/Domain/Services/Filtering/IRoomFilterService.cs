using System.Collections.Generic;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Models.Shared;

namespace RoomsManagerAddin.Domain.Services.Filtering
{
    /// <summary>
    /// Interface for filtering rooms based on parameter values and logical conditions.
    /// </summary>
    public interface IRoomFilterService
    {
        /// <summary>
        /// Gets the list of available parameters for filtering rooms.
        /// </summary>
        /// <returns>A list of parameter information objects.</returns>
        List<ParameterInfo> GetAvailableParameters();

        /// <summary>
        /// Creates a filter rule for a specific parameter, operator, and value.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to filter by.</param>
        /// <param name="filterOperator">The comparison operator to use.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>A configured filter rule.</returns>
        /// <exception cref="System.ArgumentException">Thrown when parameter not found or operator is invalid for the parameter type.</exception>
        RoomFilterRule CreateFilterRule(string parameterName, FilterOperator filterOperator, string value);

        /// <summary>
        /// Creates a filter set with a specified logical operator (AND/OR).
        /// </summary>
        /// <param name="logicalOperator">The logical operator for combining filter items.</param>
        /// <returns>An empty filter set.</returns>
        FilterSet CreateFilterSet(LogicalOperator logicalOperator);

        /// <summary>
        /// Creates a new filter configuration with default settings.
        /// </summary>
        /// <param name="name">The name of the filter configuration.</param>
        /// <returns>A new filter configuration.</returns>
        RoomFilterConfiguration CreateFilterConfiguration(string name);

        /// <summary>
        /// Applies the filter configuration to rooms and returns matching rooms.
        /// </summary>
        /// <param name="filterConfig">The filter configuration to apply.</param>
        /// <returns>A list of room items that match the filter criteria.</returns>
        List<RoomItem> ApplyFilter(RoomFilterConfiguration filterConfig);
    }
}
