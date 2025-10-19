using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RoomsManagerAddin.Domain.Models.Filtering;
using RoomsManagerAddin.Infrastructure.Progress;

namespace RoomsManagerAddin.Domain.Services.Processing
{
    /// <summary>
    /// Interface for executing parameter mapping operations between elements.
    /// </summary>
    public interface IParameterMappingExecutionService
    {
        /// <summary>
        /// Set the progress reporter for displaying progress bars
        /// </summary>
        void SetProgressReporter(ProgressReporter progressReporter);

        /// <summary>
        /// Execute room-to-element parameter mappings during analysis
        /// </summary>
        void ExecuteRoomToElementMappings(Room room, List<Element> relatedElements, List<ParameterMappingConfiguration> mappings);

        /// <summary>
        /// Execute room-to-element parameter mappings in batch with duplicate detection
        /// </summary>
        void ExecuteRoomToElementMappingsBatch(Dictionary<Room, List<Element>> roomElementRelationships, List<ParameterMappingConfiguration> mappings);

        /// <summary>
        /// Execute element-to-room parameter mappings in batch with duplicate detection
        /// </summary>
        void ExecuteElementToRoomMappings(Dictionary<ElementId, List<Room>> elementRoomRelationships, Dictionary<ElementId, Element> elementIdToElementMap, List<ParameterMappingConfiguration> mappings);

        /// <summary>
        /// Validate all mapping configurations before execution
        /// </summary>
        bool ValidateAllMappings(List<ParameterMappingConfiguration> mappings);
    }
}
