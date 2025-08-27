using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Models;
using RoomsManagerAddin.Services;

namespace RoomsManagerAddin.Controllers
{
    /// <summary>
    /// Controller for managing generic element selection, filtering, and data preparation
    /// Handles the right panel functionality for dynamic category selection and filtering
    /// </summary>
    public class GenericElementController
    {
        private readonly Document _document;
        private readonly GenericElementFilterService _filterService;
        private readonly LoggingService _loggingService;

        // Current state
        private CategoryInfo _selectedCategory;
        private List<Element> _allElements;
        private List<Element> _filteredElements;
        private List<ParameterInfo> _availableParameters;
        private ElementFilterConfiguration _currentFilter;

        public GenericElementController(Document document)
        {
            _document = document;
            _loggingService = new LoggingService();
            _filterService = new GenericElementFilterService(_document, _loggingService);
            
            _allElements = new List<Element>();
            _filteredElements = new List<Element>();
            _availableParameters = new List<ParameterInfo>();
        }

        #region Public Properties

        /// <summary>
        /// Currently selected category
        /// </summary>
        public CategoryInfo SelectedCategory => _selectedCategory;

        /// <summary>
        /// All elements of the selected category
        /// </summary>
        public List<Element> AllElements => _allElements?.ToList() ?? new List<Element>();

        /// <summary>
        /// Filtered elements based on current filter configuration
        /// </summary>
        public List<Element> FilteredElements => _filteredElements?.ToList() ?? new List<Element>();

        /// <summary>
        /// Available parameters for the selected category
        /// </summary>
        public List<ParameterInfo> AvailableParameters => _availableParameters?.ToList() ?? new List<ParameterInfo>();

        /// <summary>
        /// Current filter configuration
        /// </summary>
        public ElementFilterConfiguration CurrentFilter => _currentFilter;

        #endregion

        #region Category Management

        /// <summary>
        /// Get all available categories for selection
        /// </summary>
        public List<CategoryInfo> GetAvailableCategories()
        {
            return _filterService.GetAvailableCategories();
        }

        /// <summary>
        /// Select a category and load its elements and parameters
        /// </summary>
        public void SelectCategory(CategoryInfo category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            try
            {
                _selectedCategory = category;
                
                // Load elements for this category
                _allElements = _filterService.GetElementsByCategory(category.Id);
                _filteredElements = new List<Element>(_allElements);

                // Discover parameters for this category
                _availableParameters = _filterService.GetParametersForCategory(category.Id);

                // Create new filter configuration
                _currentFilter = _filterService.CreateFilterConfiguration(
                    $"{category.Name} Filter", 
                    category.Id
                );

                _loggingService.LogInfo($"Selected category '{category.Name}': {_allElements.Count} elements, {_availableParameters.Count} parameters");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error selecting category '{category?.Name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear current category selection
        /// </summary>
        public void ClearCategory()
        {
            _selectedCategory = null;
            _allElements.Clear();
            _filteredElements.Clear();
            _availableParameters.Clear();
            _currentFilter = null;
        }

        #endregion

        #region Filtering

        /// <summary>
        /// Create a filter configuration for the current category
        /// </summary>
        public ElementFilterConfiguration CreateFilterConfiguration(string name)
        {
            if (_selectedCategory == null)
            {
                throw new InvalidOperationException("No category selected");
            }

            return _filterService.CreateFilterConfiguration(name, _selectedCategory.Id);
        }

        /// <summary>
        /// Apply the current filter to elements
        /// </summary>
        public List<Element> ApplyAdvancedFilter(ElementFilterConfiguration filterConfig)
        {
            try
            {
                if (_selectedCategory == null || _allElements == null)
                {
                    return new List<Element>();
                }

                _filteredElements = _filterService.ApplyAdvancedFilter(filterConfig, _allElements);
                return _filteredElements;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error applying filter: {ex.Message}");
                return _allElements;
            }
        }

        #endregion

        #region Data Conversion

        /// <summary>
        /// Convert elements to display items (similar to WallItem, RoomItem)
        /// </summary>
        public List<ElementItem> GetElementItems(List<Element> elements = null)
        {
            var elementsToConvert = elements ?? _filteredElements;
            var items = new List<ElementItem>();

            try
            {
                foreach (var element in elementsToConvert)
                {
                    var item = new ElementItem
                    {
                        Id = element.Id,
                        Name = GetElementDisplayName(element),
                        CategoryName = _selectedCategory?.Name ?? "Unknown",
                        LevelName = GetElementLevel(element),
                        TypeName = GetElementTypeName(element)
                    };

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error converting elements to items: {ex.Message}");
            }

            return items;
        }

        private string GetElementDisplayName(Element element)
        {
            try
            {
                // Try to get a meaningful name for the element
                var name = element.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = element.Category?.Name ?? "Unnamed Element";
                }
                return name;
            }
            catch
            {
                return "Unnamed Element";
            }
        }

        private string GetElementLevel(Element element)
        {
            try
            {
                var levelParam = element.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                if (levelParam != null && levelParam.HasValue)
                {
                    var levelId = levelParam.AsElementId();
                    var level = _document.GetElement(levelId) as Level;
                    return level?.Name ?? "Unknown Level";
                }
                return "No Level";
            }
            catch
            {
                return "Unknown Level";
            }
        }

        private string GetElementTypeName(Element element)
        {
            try
            {
                var elementType = _document.GetElement(element.GetTypeId());
                return elementType?.Name ?? "Unknown Type";
            }
            catch
            {
                return "Unknown Type";
            }
        }

        #endregion

        #region Status and Information

        /// <summary>
        /// Get status information for display
        /// </summary>
        public string GetFilterStatusText()
        {
            if (_selectedCategory == null)
            {
                return "No category selected";
            }

            var totalCount = _allElements?.Count ?? 0;
            var filteredCount = _filteredElements?.Count ?? 0;

            if (_currentFilter?.RootFilterSet?.Items?.Any() == true)
            {
                return $"Filters applied - {filteredCount} of {totalCount} {_selectedCategory.Name.ToLower()} selected";
            }
            else
            {
                return $"No filters applied - showing all {totalCount} {_selectedCategory.Name.ToLower()}";
            }
        }

        /// <summary>
        /// Get count information for display
        /// </summary>
        public string GetCountText()
        {
            if (_selectedCategory == null)
            {
                return "No category: 0 of 0";
            }

            var totalCount = _allElements?.Count ?? 0;
            var filteredCount = _filteredElements?.Count ?? 0;
            
            return $"{_selectedCategory.Name}: {filteredCount} of {totalCount}";
        }

        #endregion
    }
}