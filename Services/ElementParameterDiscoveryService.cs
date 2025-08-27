using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Generic service for discovering parameters of any Revit element category
    /// </summary>
    public class ElementParameterDiscoveryService
    {
        private readonly Document _document;
        private readonly LoggingService _loggingService;

        public ElementParameterDiscoveryService(Document document, LoggingService loggingService = null)
        {
            _document = document;
            _loggingService = loggingService ?? new LoggingService();
        }

        /// <summary>
        /// Get all user-relevant categories from the document (3D visible categories)
        /// </summary>
        public List<CategoryInfo> GetAvailableCategories()
        {
            var categories = new List<CategoryInfo>();
            
            try
            {
                var categorySet = _document.Settings.Categories;
                
                foreach (Category category in categorySet)
                {
                    // Filter for user-relevant 3D categories
                    if (IsUserRelevantCategory(category))
                    {
                        categories.Add(new CategoryInfo
                        {
                            Id = category.Id,
                            Name = category.Name,
                            CategoryType = category.CategoryType
                        });
                    }
                }

                // Sort alphabetically
                categories = categories.OrderBy(c => c.Name).ToList();
                
                _loggingService?.LogInfo($"Discovered {categories.Count} user-relevant categories");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error discovering categories: {ex.Message}");
            }

            return categories;
        }

        /// <summary>
        /// Get parameters for elements of a specific category
        /// </summary>
        public List<ParameterInfo> GetParametersForCategory(ElementId categoryId)
        {
            var parameters = new List<ParameterInfo>();
            
            try
            {
                // Get a sample element from this category to analyze its parameters
                var sampleElement = GetSampleElementFromCategory(categoryId);
                if (sampleElement == null)
                {
                    _loggingService?.LogWarning($"No elements found for category {categoryId}");
                    return parameters;
                }

                var processedParameters = new HashSet<string>();

                // Process element parameters
                foreach (Parameter param in sampleElement.Parameters)
                {
                    if (IsValidParameter(param) && !processedParameters.Contains(param.Definition.Name))
                    {
                        var paramInfo = CreateParameterInfo(param);
                        if (paramInfo != null)
                        {
                            parameters.Add(paramInfo);
                            processedParameters.Add(param.Definition.Name);
                        }
                    }
                }

                // Sort parameters alphabetically
                parameters = parameters.OrderBy(p => p.Name).ToList();
                
                _loggingService?.LogInfo($"Discovered {parameters.Count} parameters for category {categoryId}");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error discovering parameters for category {categoryId}: {ex.Message}");
            }

            return parameters;
        }

        private bool IsUserRelevantCategory(Category category)
        {
            try
            {
                // Skip hidden or system categories
                if (!category.CanAddSubcategory || category.CategoryType != CategoryType.Model)
                {
                    return false;
                }

                // Skip categories that typically don't have instances or are not useful for filtering
                var excludeCategories = new HashSet<string>
                {
                    "Materials", "Fill Patterns", "Line Patterns", "Line Weights", 
                    "Line Styles", "Object Styles", "Analytical Surfaces", "Analytical Nodes"
                };

                if (excludeCategories.Contains(category.Name))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private Element GetSampleElementFromCategory(ElementId categoryId)
        {
            try
            {
                var collector = new FilteredElementCollector(_document)
                    .OfCategoryId(categoryId)
                    .WhereElementIsNotElementType()
                    .FirstOrDefault();

                return collector;
            }
            catch
            {
                return null;
            }
        }

        private bool IsValidParameter(Parameter param)
        {
            try
            {
                // Skip parameters without proper definition
                if (param?.Definition == null || string.IsNullOrEmpty(param.Definition.Name))
                    return false;

                // Skip read-only system parameters that aren't useful for filtering
                var excludeParameterNames = new HashSet<string>
                {
                    "Element ID", "Unique ID", "Type ID", "Family and Type",
                    "Constraints", "Phasing Created", "Phasing Demolished"
                };

                return !excludeParameterNames.Contains(param.Definition.Name);
            }
            catch
            {
                return false;
            }
        }

        private ParameterInfo CreateParameterInfo(Parameter param)
        {
            try
            {
                var parameterInfo = new ParameterInfo
                {
                    Name = param.Definition.Name,
                    StorageType = param.StorageType,
                    DataType = ParameterInfo.GetParameterDataType(param),
                    IsReadOnly = param.IsReadOnly,
                    IsBuiltIn = param.IsShared == false && param.Definition is InternalDefinition,
                    IsShared = param.IsShared
                };

                return parameterInfo;
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"Error creating parameter info for {param?.Definition?.Name}: {ex.Message}");
                return null;
            }
        }
    }
}