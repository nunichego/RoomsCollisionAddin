using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Infrastructure.RevitApi
{
    /// <summary>
    /// Service for updating Revit element parameters
    /// </summary>
    /// <remarks>
    /// Provides centralized parameter update functionality with transaction management.
    /// </remarks>
    public interface IParameterUpdateService
    {
        /// <summary>Update a parameter value on an element</summary>
        /// <param name="document">The Revit document</param>
        /// <param name="element">The element to update</param>
        /// <param name="parameterName">Name of the parameter</param>
        /// <param name="value">New value to set</param>
        /// <returns>True if update succeeded</returns>
        bool UpdateParameter(Document document, Element element, string parameterName, string value);

        /// <summary>Update multiple parameters in a single transaction</summary>
        /// <param name="document">The Revit document</param>
        /// <param name="element">The element to update</param>
        /// <param name="parameters">Dictionary of parameter names and values</param>
        /// <returns>True if all updates succeeded</returns>
        bool UpdateParameters(Document document, Element element, System.Collections.Generic.Dictionary<string, string> parameters);
    }
}
