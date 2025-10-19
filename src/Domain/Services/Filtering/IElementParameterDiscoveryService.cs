using System.Collections.Generic;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Domain.Models.Filtering;

namespace RoomsManagerAddin.Domain.Services.Filtering
{
    /// <summary>
    /// Interface for discovering and retrieving element parameter information for any Revit category.
    /// </summary>
    public interface IElementParameterDiscoveryService
    {
        /// <summary>
        /// Gets all available element categories in the document.
        /// </summary>
        /// <returns>A list of category information objects.</returns>
        List<CategoryInfo> GetAvailableCategories();

        /// <summary>
        /// Gets parameters available for elements in a specific category.
        /// </summary>
        /// <param name="categoryId">The Revit category ID.</param>
        /// <returns>A list of parameter information objects for the category.</returns>
        List<ParameterInfo> GetParametersForCategory(ElementId categoryId);
    }
}
