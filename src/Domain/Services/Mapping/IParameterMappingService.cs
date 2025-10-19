using System.Collections.Generic;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Domain.Models.Filtering;

namespace RoomsManagerAddin.Domain.Services.Mapping
{
    /// <summary>
    /// Interface for managing parameter mapping configurations between Rooms and other element categories.
    /// </summary>
    public interface IParameterMappingService
    {
        /// <summary>
        /// Gets the list of room parameters available for mapping.
        /// </summary>
        List<ParameterInfo> RoomParameters { get; }

        /// <summary>
        /// Gets the list of element parameters available for mapping (for currently selected category).
        /// </summary>
        List<ParameterInfo> ElementParameters { get; }

        /// <summary>
        /// Gets the currently selected element category.
        /// </summary>
        CategoryInfo SelectedCategory { get; }

        /// <summary>
        /// Indicates whether a category has been selected.
        /// </summary>
        bool HasCategorySelected { get; }

        /// <summary>
        /// Sets the selected category and loads its available parameters.
        /// </summary>
        /// <param name="category">The category to select, or null to clear selection.</param>
        void SetSelectedCategory(CategoryInfo category);
    }
}
