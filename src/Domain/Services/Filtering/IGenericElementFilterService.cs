using System.Collections.Generic;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Domain.Models.Shared;

namespace RoomsManagerAddin.Domain.Services.Filtering
{
    /// <summary>
    /// Interface for generic filtering service that can work with any Revit element category.
    /// </summary>
    public interface IGenericElementFilterService
    {
        /// <summary>
        /// Gets all available element categories that can be filtered.
        /// </summary>
        /// <returns>A list of category information objects.</returns>
        List<CategoryInfo> GetAvailableCategories();

        /// <summary>
        /// Gets the parameters available for a specific element category.
        /// </summary>
        /// <param name="categoryId">The Revit category ID.</param>
        /// <returns>A list of parameter information objects for the category.</returns>
        List<ParameterInfo> GetParametersForCategory(ElementId categoryId);

        /// <summary>
        /// Gets all elements of a specific category from the document.
        /// </summary>
        /// <param name="categoryId">The Revit category ID.</param>
        /// <returns>A list of elements belonging to the category.</returns>
        List<Element> GetElementsByCategory(ElementId categoryId);
    }
}
