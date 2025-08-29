using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Models;

namespace RoomsManagerAddin.Services
{
    /// <summary>
    /// Service for executing parameter mappings between rooms and other elements
    /// Replaces the old Filter Tag update system with user-configured parameter mappings
    /// </summary>
    public class ParameterMappingExecutionService
    {
        #region Fields
        private readonly Action<string> _writeToLog;
        private ProgressReporter _progressReporter;
        #endregion

        #region Constructor
        public ParameterMappingExecutionService(Action<string> writeToLog = null)
        {
            _writeToLog = writeToLog ?? (msg => System.Diagnostics.Debug.WriteLine(msg));
        }
        #endregion

        #region Public Methods - Progress Setup
        /// <summary>
        /// Set the progress reporter for displaying progress bars
        /// </summary>
        public void SetProgressReporter(ProgressReporter progressReporter)
        {
            _progressReporter = progressReporter;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Execute room-to-element parameter mappings during analysis
        /// Called for each room after its relationships are analyzed
        /// </summary>
        public void ExecuteRoomToElementMappings(
            Room room, 
            List<Element> relatedElements, 
            List<ParameterMappingConfiguration> mappings)
        {
            if (room == null || mappings == null || !mappings.Any(m => m.IsEnabled && m.Direction == MappingDirection.RoomsToCategory))
                return;

            var enabledMappings = mappings
                .Where(m => m.IsEnabled && m.Direction == MappingDirection.RoomsToCategory)
                .ToList();

            if (!enabledMappings.Any())
                return;

            foreach (var mapping in enabledMappings)
            {
                ExecuteRoomToElementMapping(room, relatedElements, mapping);
            }
        }

        /// <summary>
        /// Execute room-to-element parameter mappings in batch with duplicate detection
        /// Called once with all room-element relationships to avoid duplication
        /// </summary>
        public void ExecuteRoomToElementMappingsBatch(
            Dictionary<Room, List<Element>> roomElementRelationships,
            List<ParameterMappingConfiguration> mappings)
        {
            if (roomElementRelationships == null || !roomElementRelationships.Any() || mappings == null)
                return;

            var enabledMappings = mappings
                .Where(m => m.IsEnabled && m.Direction == MappingDirection.RoomsToCategory)
                .ToList();

            if (!enabledMappings.Any())
            {
                _writeToLog("No enabled room-to-element mappings found");
                return;
            }

            _writeToLog($"=== EXECUTING ROOM-TO-ELEMENT PARAMETER MAPPINGS (BATCH) ===");
            _writeToLog($"Processing {enabledMappings.Count} mapping(s) for {roomElementRelationships.Count} rooms");

            // Phase 1: Collect all values (with progress)
            _progressReporter?.ReportProgress("Parameter Mapping", "Collecting Relationships", "Collecting room-to-element relationships...", 0.0, 0.8);
            
            var elementValueMappings = new Dictionary<ElementId, Dictionary<ParameterInfo, HashSet<string>>>();
            int roomIndex = 0;
            int totalRooms = roomElementRelationships.Count;

            foreach (var roomData in roomElementRelationships)
            {
                roomIndex++;
                var room = roomData.Key;
                var elements = roomData.Value;

                // Update progress for collection phase
                if (roomIndex % Math.Max(1, totalRooms / 20) == 0 || roomIndex == totalRooms)
                {
                    var progressPercent = (int)((double)roomIndex / totalRooms * 50); // Collection is 50% of this phase
                    // TODO: Update progress reporting
                    // _showProgress?.Invoke("Parameter Mapping", 
                    //     $"Collecting relationships: Room {roomIndex}/{totalRooms} ({room.Number})", 
                    //     roomIndex, totalRooms, 80 + progressPercent / 10, 90);
                }

                foreach (var mapping in enabledMappings)
                {
                    var sourceValue = GetParameterValue(room, mapping.FromParameter);
                    if (string.IsNullOrEmpty(sourceValue))
                        continue;

                    foreach (var element in elements)
                    {
                        if (!elementValueMappings.ContainsKey(element.Id))
                        {
                            elementValueMappings[element.Id] = new Dictionary<ParameterInfo, HashSet<string>>();
                        }

                        if (!elementValueMappings[element.Id].ContainsKey(mapping.ToParameter))
                        {
                            elementValueMappings[element.Id][mapping.ToParameter] = new HashSet<string>();
                        }

                        elementValueMappings[element.Id][mapping.ToParameter].Add(sourceValue);
                    }
                }
            }

            // Phase 2: Apply collected values to elements (with progress)
            _progressReporter?.ReportProgress("Parameter Mapping", "Updating Parameters", "Updating element parameters...", 0.0, 0.85);
            
            int elementIndex = 0;
            int totalElements = elementValueMappings.Count;
            
            foreach (var elementMapping in elementValueMappings)
            {
                elementIndex++;
                var elementId = elementMapping.Key;
                var parameterValues = elementMapping.Value;

                // Update progress for application phase
                if (elementIndex % Math.Max(1, totalElements / 10) == 0 || elementIndex == totalElements)
                {
                    var progressPercent = (int)((double)elementIndex / totalElements * 50); // Application is 50% of this phase
                    // TODO: Update progress reporting
                    // _showProgress?.Invoke("Parameter Mapping", 
                    //     $"Updating elements: {elementIndex}/{totalElements} (Element {elementId})", 
                    //     elementIndex, totalElements, 85 + progressPercent / 10, 90);
                }

                // Find the element
                var element = roomElementRelationships.Values
                    .SelectMany(elements => elements)
                    .FirstOrDefault(e => e.Id == elementId);

                if (element == null) continue;

                foreach (var paramMapping in parameterValues)
                {
                    var toParameter = paramMapping.Key;
                    var uniqueValues = paramMapping.Value.ToList(); // HashSet automatically deduplicates

                    // Find the corresponding mapping configuration for separator and override settings
                    var mappingConfig = enabledMappings.FirstOrDefault(m => m.ToParameter == toParameter);
                    if (mappingConfig == null) continue;

                    var combinedValue = string.Join(mappingConfig.ValueSeparator ?? ", ", uniqueValues.OrderBy(v => v));

                    var success = SetParameterValueWithAccumulation(element, toParameter, combinedValue, mappingConfig.ValueSeparator, mappingConfig.OverrideExistingValues);
                }
            }
            
            _writeToLog($"✓ Completed room-to-element mappings: {totalElements} elements updated");
        }

        /// <summary>
        /// Execute element-to-room parameter mappings in batch with duplicate detection
        /// Called once with all element-room relationships to avoid duplication
        /// </summary>
        public void ExecuteElementToRoomMappings(
            Dictionary<ElementId, List<Room>> elementRoomRelationships,
            Dictionary<ElementId, Element> elementIdToElementMap,
            List<ParameterMappingConfiguration> mappings)
        {
            if (elementRoomRelationships == null || !elementRoomRelationships.Any() || mappings == null)
                return;

            var enabledMappings = mappings
                .Where(m => m.IsEnabled && m.Direction == MappingDirection.CategoryToRooms)
                .ToList();

            if (!enabledMappings.Any())
            {
                _writeToLog("No enabled element-to-room mappings found");
                return;
            }

            _writeToLog($"=== EXECUTING ELEMENT-TO-ROOM PARAMETER MAPPINGS (BATCH) ===");
            _writeToLog($"Processing {enabledMappings.Count} mapping(s) for {elementRoomRelationships.Count} elements");

            // Phase 1: Collect all values (with progress)
            _progressReporter?.ReportProgress("Parameter Mapping", "Element-to-Room Mapping", "Collecting element-to-room relationships...", 0.0, 0.9);
            
            var roomValueMappings = new Dictionary<Room, Dictionary<ParameterInfo, HashSet<string>>>();
            int elementIndex = 0;
            int totalElements = elementRoomRelationships.Count;

            foreach (var elementData in elementRoomRelationships)
            {
                elementIndex++;
                var elementId = elementData.Key;
                var relatedRooms = elementData.Value;
                var element = elementIdToElementMap[elementId];

                // Update progress for collection phase
                if (elementIndex % Math.Max(1, totalElements / 20) == 0 || elementIndex == totalElements)
                {
                    var progressPercent = (int)((double)elementIndex / totalElements * 50); // Collection is 50% of this phase
                    // TODO: Update progress reporting
                    // _showProgress?.Invoke("Parameter Mapping", 
                    //     $"Collecting relationships: Element {elementIndex}/{totalElements} ({elementId})", 
                    //     elementIndex, totalElements, 90 + progressPercent / 10, 100);
                }

                foreach (var mapping in enabledMappings)
                {
                    var sourceValue = GetParameterValue(element, mapping.FromParameter);
                    if (string.IsNullOrEmpty(sourceValue))
                        continue;

                    foreach (var room in relatedRooms)
                    {
                        if (!roomValueMappings.ContainsKey(room))
                        {
                            roomValueMappings[room] = new Dictionary<ParameterInfo, HashSet<string>>();
                        }

                        if (!roomValueMappings[room].ContainsKey(mapping.ToParameter))
                        {
                            roomValueMappings[room][mapping.ToParameter] = new HashSet<string>();
                        }

                        roomValueMappings[room][mapping.ToParameter].Add(sourceValue);
                    }
                }
            }

            // Phase 2: Apply collected values to rooms (with progress)
            _progressReporter?.ReportProgress("Parameter Mapping", "Updating Room Parameters", "Updating room parameters...", 0.0, 0.95);
            
            int roomIndex = 0;
            int totalRooms = roomValueMappings.Count;

            foreach (var roomMapping in roomValueMappings)
            {
                roomIndex++;
                var room = roomMapping.Key;
                var parameterValues = roomMapping.Value;

                // Update progress for application phase
                if (roomIndex % Math.Max(1, totalRooms / 10) == 0 || roomIndex == totalRooms)
                {
                    var progressPercent = (int)((double)roomIndex / totalRooms * 50); // Application is 50% of this phase
                    // TODO: Update progress reporting
                    // _showProgress?.Invoke("Parameter Mapping", 
                    //     $"Updating rooms: {roomIndex}/{totalRooms} (Room {room.Number})", 
                    //     roomIndex, totalRooms, 95 + progressPercent / 10, 100);
                }

                foreach (var paramMapping in parameterValues)
                {
                    var toParameter = paramMapping.Key;
                    var uniqueValues = paramMapping.Value.ToList(); // HashSet automatically deduplicates

                    // Find the corresponding mapping configuration for separator and override settings
                    var mappingConfig = enabledMappings.FirstOrDefault(m => m.ToParameter == toParameter);
                    if (mappingConfig == null) continue;

                    var combinedValue = string.Join(mappingConfig.ValueSeparator ?? ", ", uniqueValues.OrderBy(v => v));

                    var success = SetParameterValueWithAccumulation(room, toParameter, combinedValue, mappingConfig.ValueSeparator, mappingConfig.OverrideExistingValues);
                }
            }
            
            _writeToLog($"✓ Completed element-to-room mappings: {totalRooms} rooms updated");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Execute a single room-to-element parameter mapping
        /// </summary>
        private void ExecuteRoomToElementMapping(
            Room room, 
            List<Element> relatedElements, 
            ParameterMappingConfiguration mapping)
        {
            try
            {
                // Get source value from room
                var sourceValue = GetParameterValue(room, mapping.FromParameter);
                if (string.IsNullOrEmpty(sourceValue))
                    return;

                // Handle multiple related elements with separator
                var targetElements = relatedElements ?? new List<Element>();
                if (!targetElements.Any())
                    return;

                // Update each related element
                foreach (var element in targetElements)
                {
                    SetParameterValueWithAccumulation(element, mapping.ToParameter, sourceValue, mapping.ValueSeparator, mapping.OverrideExistingValues);
                }
            }
            catch (Exception ex)
            {
                _writeToLog($"Error executing room-to-element mapping: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a single element-to-room parameter mapping
        /// </summary>
        private void ExecuteElementToRoomMapping(
            Element element, 
            List<Room> relatedRooms, 
            ParameterMappingConfiguration mapping)
        {
            try
            {
                // Get source value from element
                var sourceValue = GetParameterValue(element, mapping.FromParameter);
                if (string.IsNullOrEmpty(sourceValue))
                    return;

                // Handle multiple related rooms with separator
                if (!relatedRooms.Any())
                    return;

                // Update each related room
                foreach (var room in relatedRooms)
                {
                    SetParameterValueWithAccumulation(room, mapping.ToParameter, sourceValue, mapping.ValueSeparator, mapping.OverrideExistingValues);
                }
            }
            catch (Exception ex)
            {
                _writeToLog($"Error executing element-to-room mapping: {ex.Message}");
            }
        }

        /// <summary>
        /// Get parameter value from an element as string
        /// </summary>
        private string GetParameterValue(Element element, ParameterInfo parameterInfo)
        {
            try
            {
                Parameter parameter = null;
                
                // Get parameter by method depending on type
                if (parameterInfo.IsBuiltIn && parameterInfo.BuiltInParameterId.HasValue)
                {
                    parameter = element.get_Parameter(parameterInfo.BuiltInParameterId.Value);
                }
                else
                {
                    parameter = element.LookupParameter(parameterInfo.Name);
                }

                if (parameter == null || !parameter.HasValue)
                    return string.Empty;

                // Convert to string based on storage type
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        return parameter.AsString() ?? string.Empty;
                    case StorageType.Integer:
                        return parameter.AsInteger().ToString();
                    case StorageType.Double:
                        return parameter.AsDouble().ToString("F2");
                    case StorageType.ElementId:
                        var elementId = parameter.AsElementId();
                        if (elementId == null || elementId == ElementId.InvalidElementId)
                            return string.Empty;
                        
                        // Try to get element name if possible
                        var referencedElement = element.Document.GetElement(elementId);
                        return referencedElement?.Name ?? elementId.Value.ToString();
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _writeToLog($"Error getting parameter '{parameterInfo.Name}': {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Set parameter value on an element with accumulation support
        /// </summary>
        private bool SetParameterValueWithAccumulation(Element element, ParameterInfo parameterInfo, string newValue, string separator, bool overrideExistingValues)
        {
            try
            {
                Parameter parameter = null;
                
                // Get parameter by method depending on type
                if (parameterInfo.IsBuiltIn && parameterInfo.BuiltInParameterId.HasValue)
                {
                    parameter = element.get_Parameter(parameterInfo.BuiltInParameterId.Value);
                }
                else
                {
                    parameter = element.LookupParameter(parameterInfo.Name);
                }

                if (parameter == null || parameter.IsReadOnly)
                    return false;

                // Handle accumulation logic based on storage type
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        return SetStringParameterWithAccumulation(parameter, newValue, separator, overrideExistingValues);
                    case StorageType.Integer:
                        // For non-string types, just set the value directly (no accumulation makes sense)
                        if (int.TryParse(newValue, out int intValue))
                        {
                            parameter.Set(intValue);
                            return true;
                        }
                        return false;
                    case StorageType.Double:
                        if (double.TryParse(newValue, out double doubleValue))
                        {
                            parameter.Set(doubleValue);
                            return true;
                        }
                        return false;
                    case StorageType.ElementId:
                        if (long.TryParse(newValue, out long elementIdValue))
                        {
                            parameter.Set(new ElementId(elementIdValue));
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _writeToLog($"Error setting parameter '{parameterInfo.Name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set string parameter value with accumulation logic and duplicate detection
        /// </summary>
        private bool SetStringParameterWithAccumulation(Parameter parameter, string newValue, string separator, bool overrideExistingValues)
        {
            try
            {
                var existingValue = parameter.AsString() ?? "";
                
                // Check override behavior
                if (!overrideExistingValues && !string.IsNullOrEmpty(existingValue))
                {
                    return true; // Consider this success since we're honoring the override setting
                }
                
                string finalValue;
                
                if (overrideExistingValues || string.IsNullOrEmpty(existingValue))
                {
                    // First value or override mode - just set the new value
                    finalValue = newValue;
                }
                else
                {
                    // Accumulate values with separator and deduplicate
                    var separatorToUse = string.IsNullOrEmpty(separator) ? ", " : separator;
                    
                    // Split existing values, add new value, deduplicate, and rejoin
                    var existingValues = existingValue.Split(new[] { separatorToUse }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrEmpty(v))
                        .ToHashSet();
                    
                    // Add new values (handle case where newValue might also be a separated list)
                    var newValues = newValue.Split(new[] { separatorToUse }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrEmpty(v));
                    
                    foreach (var value in newValues)
                    {
                        existingValues.Add(value);
                    }
                    
                    finalValue = string.Join(separatorToUse, existingValues.OrderBy(v => v));
                }
                
                parameter.Set(finalValue);
                return true;
            }
            catch (Exception ex)
            {
                _writeToLog($"Error in parameter accumulation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set parameter value on an element (legacy method, kept for compatibility)
        /// </summary>
        private bool SetParameterValue(Element element, ParameterInfo parameterInfo, string value)
        {
            try
            {
                Parameter parameter = null;
                
                // Get parameter by method depending on type
                if (parameterInfo.IsBuiltIn && parameterInfo.BuiltInParameterId.HasValue)
                {
                    parameter = element.get_Parameter(parameterInfo.BuiltInParameterId.Value);
                }
                else
                {
                    parameter = element.LookupParameter(parameterInfo.Name);
                }

                if (parameter == null || parameter.IsReadOnly)
                    return false;

                // Set value based on storage type
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        parameter.Set(value);
                        return true;
                    case StorageType.Integer:
                        if (int.TryParse(value, out int intValue))
                        {
                            parameter.Set(intValue);
                            return true;
                        }
                        return false;
                    case StorageType.Double:
                        if (double.TryParse(value, out double doubleValue))
                        {
                            parameter.Set(doubleValue);
                            return true;
                        }
                        return false;
                    case StorageType.ElementId:
                        if (long.TryParse(value, out long elementIdValue))
                        {
                            parameter.Set(new ElementId(elementIdValue));
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _writeToLog($"Error setting parameter '{parameterInfo.Name}': {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Collect parameter mapping configurations from UI
        /// Helper method to gather configurations from the window
        /// </summary>
        public static List<ParameterMappingConfiguration> CollectMappingConfigurations(
            List<ParameterMappingConfiguration> roomsToCategoryMappings,
            List<ParameterMappingConfiguration> categoryToRoomsMappings)
        {
            var allMappings = new List<ParameterMappingConfiguration>();
            
            if (roomsToCategoryMappings != null)
                allMappings.AddRange(roomsToCategoryMappings.Where(m => m.IsEnabled));
                
            if (categoryToRoomsMappings != null)
                allMappings.AddRange(categoryToRoomsMappings.Where(m => m.IsEnabled));
            
            return allMappings;
        }

        /// <summary>
        /// Validate all mapping configurations before execution
        /// </summary>
        public bool ValidateAllMappings(List<ParameterMappingConfiguration> mappings)
        {
            _writeToLog("=== PARAMETER MAPPING VALIDATION ===");
            
            if (mappings == null || !mappings.Any())
            {
                _writeToLog("⚠ No parameter mappings configured");
                return false;
            }

            var enabledMappings = mappings.Where(m => m.IsEnabled).ToList();
            if (!enabledMappings.Any())
            {
                _writeToLog("⚠ No enabled parameter mappings found");
                return false;
            }

            _writeToLog($"✓ Found {enabledMappings.Count} enabled parameter mapping(s)");
            
            foreach (var mapping in enabledMappings)
            {
                if (mapping.FromParameter == null || mapping.ToParameter == null)
                {
                    _writeToLog($"✗ Invalid mapping: missing From or To parameter");
                    return false;
                }
                
                _writeToLog($"  • {mapping.Direction}: {mapping.FromParameter.Name} → {mapping.ToParameter.Name}");
            }

            return true;
        }
        #endregion
    }
}