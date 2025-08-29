using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for managing parameter mapping between Rooms and other element categories
    /// </summary>
    public class ParameterMappingService
    {
        #region Fields
        private Document _document;
        private List<ParameterInfo> _roomParameters;
        private List<ParameterInfo> _elementParameters;
        private CategoryInfo _selectedCategory;
        #endregion

        #region Properties
        public List<ParameterInfo> RoomParameters => _roomParameters ?? new List<ParameterInfo>();
        public List<ParameterInfo> ElementParameters => _elementParameters ?? new List<ParameterInfo>();
        public CategoryInfo SelectedCategory => _selectedCategory;
        public bool HasCategorySelected => _selectedCategory != null;
        #endregion

        #region Constructor
        public ParameterMappingService(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            LoadRoomParameters();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Updates the selected category and loads its parameters
        /// </summary>
        public void SetSelectedCategory(CategoryInfo category)
        {
            _selectedCategory = category;
            if (category != null)
            {
                LoadElementParameters(category);
            }
            else
            {
                _elementParameters?.Clear();
            }
        }

        /// <summary>
        /// Clears the selected category and element parameters
        /// </summary>
        public void ClearSelectedCategory()
        {
            _selectedCategory = null;
            _elementParameters?.Clear();
        }

        /// <summary>
        /// Gets the dynamic category name for display
        /// </summary>
        public string GetCategoryDisplayName()
        {
            return _selectedCategory?.Name ?? "Category";
        }

        /// <summary>
        /// Validates if a mapping configuration is valid
        /// </summary>
        public bool ValidateMapping(ParameterInfo fromParameter, ParameterInfo toParameter, string separator)
        {
            if (fromParameter == null || toParameter == null)
                return false;

            // Additional validation logic can be added here
            // e.g., parameter type compatibility checks
            
            return true;
        }

        /// <summary>
        /// Generates separator preview text
        /// </summary>
        public string GenerateSeparatorPreview(string separator)
        {
            if (string.IsNullOrEmpty(separator))
                return "value01 value02";
            
            return $"value01{separator}value02";
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Loads available room parameters
        /// </summary>
        private void LoadRoomParameters()
        {
            try
            {
                _roomParameters = new List<ParameterInfo>();
                
                // Get a sample room to discover parameters
                var roomElements = new FilteredElementCollector(_document)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Where(e => e != null)
                    .Take(1);

                var sampleRoom = roomElements.FirstOrDefault();
                if (sampleRoom == null) return;

                // Get built-in parameters
                foreach (BuiltInParameter builtInParam in Enum.GetValues(typeof(BuiltInParameter)))
                {
                    try
                    {
                        var parameter = sampleRoom.get_Parameter(builtInParam);
                        if (parameter != null && !parameter.IsReadOnly)
                        {
                            var paramInfo = new ParameterInfo
                            {
                                Name = parameter.Definition.Name,
                                StorageType = parameter.StorageType,
                                BuiltInParameterId = builtInParam,
                                IsBuiltIn = true,
                                DataType = ParameterInfo.GetParameterDataType(parameter)
                            };

                            if (!_roomParameters.Any(p => p.Name == paramInfo.Name))
                            {
                                _roomParameters.Add(paramInfo);
                            }
                        }
                    }
                    catch
                    {
                        // Skip parameters that can't be accessed
                    }
                }

                // Get shared parameters
                foreach (Parameter param in sampleRoom.Parameters)
                {
                    if (param.IsShared && !param.IsReadOnly)
                    {
                        var paramInfo = new ParameterInfo
                        {
                            Name = param.Definition.Name,
                            StorageType = param.StorageType,
                            IsBuiltIn = false,
                            IsShared = true,
                            DataType = ParameterInfo.GetParameterDataType(param)
                        };

                        if (!_roomParameters.Any(p => p.Name == paramInfo.Name))
                        {
                            _roomParameters.Add(paramInfo);
                        }
                    }
                }

                // Sort by name for better UX
                _roomParameters = _roomParameters.OrderBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading room parameters: {ex.Message}");
                _roomParameters = new List<ParameterInfo>();
            }
        }

        /// <summary>
        /// Loads parameters for the selected element category
        /// </summary>
        private void LoadElementParameters(CategoryInfo category)
        {
            try
            {
                _elementParameters = new List<ParameterInfo>();

                // Get sample elements from the category
                var elements = new FilteredElementCollector(_document)
                    .OfCategoryId(category.Id)
                    .WhereElementIsNotElementType()
                    .Take(1);

                var sampleElement = elements.FirstOrDefault();
                if (sampleElement == null) return;

                // Get built-in parameters
                foreach (BuiltInParameter builtInParam in Enum.GetValues(typeof(BuiltInParameter)))
                {
                    try
                    {
                        var parameter = sampleElement.get_Parameter(builtInParam);
                        if (parameter != null && !parameter.IsReadOnly)
                        {
                            var paramInfo = new ParameterInfo
                            {
                                Name = parameter.Definition.Name,
                                StorageType = parameter.StorageType,
                                BuiltInParameterId = builtInParam,
                                IsBuiltIn = true,
                                DataType = ParameterInfo.GetParameterDataType(parameter)
                            };

                            if (!_elementParameters.Any(p => p.Name == paramInfo.Name))
                            {
                                _elementParameters.Add(paramInfo);
                            }
                        }
                    }
                    catch
                    {
                        // Skip parameters that can't be accessed
                    }
                }

                // Get shared parameters
                foreach (Parameter param in sampleElement.Parameters)
                {
                    if (param.IsShared && !param.IsReadOnly)
                    {
                        var paramInfo = new ParameterInfo
                        {
                            Name = param.Definition.Name,
                            StorageType = param.StorageType,
                            IsBuiltIn = false,
                            IsShared = true,
                            DataType = ParameterInfo.GetParameterDataType(param)
                        };

                        if (!_elementParameters.Any(p => p.Name == paramInfo.Name))
                        {
                            _elementParameters.Add(paramInfo);
                        }
                    }
                }

                // Sort by name for better UX
                _elementParameters = _elementParameters.OrderBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading element parameters: {ex.Message}");
                _elementParameters = new List<ParameterInfo>();
            }
        }
        #endregion

        #region Cleanup
        public void Dispose()
        {
            _roomParameters?.Clear();
            _elementParameters?.Clear();
            _roomParameters = null;
            _elementParameters = null;
            _selectedCategory = null;
            _document = null;
        }
        #endregion
    }

    /// <summary>
    /// Configuration for parameter mapping
    /// </summary>
    public class ParameterMappingConfiguration
    {
        public ParameterInfo FromParameter { get; set; }
        public ParameterInfo ToParameter { get; set; }
        public string ValueSeparator { get; set; }
        public bool IsEnabled { get; set; }
        public MappingDirection Direction { get; set; }
    }

    /// <summary>
    /// Direction of parameter mapping
    /// </summary>
    public enum MappingDirection
    {
        RoomsToCategory,
        CategoryToRooms
    }
}