using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RoomsManagerAddin.Application.Commands
{
    /// <summary>
    /// Base class for all Revit commands
    /// Provides common functionality, error handling, and logging
    /// </summary>
    public abstract class BaseCommand : IExternalCommand
    {
        #region Protected Properties
        /// <summary>
        /// Logger instance for the command
        /// </summary>
        protected object Logger => null;

        /// <summary>
        /// Service provider for dependency injection
        /// </summary>
        protected object ServiceProvider => null;
        #endregion

        #region IExternalCommand Implementation
        /// <summary>
        /// Main execution method for the command
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Execute the command logic
                var result = ExecuteCommand(commandData, ref message, elements);

                return result;
            }
            catch (Exception ex)
            {
                // Set error message
                message = $"Error executing {GetType().Name}: {ex.Message}";

                // Show error dialog
                ShowError($"Command Error", ex.Message, ex);

                return Result.Failed;
            }
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Execute the specific command logic
        /// Override this method in derived classes
        /// </summary>
        protected abstract Result ExecuteCommand(ExternalCommandData commandData, ref string message, ElementSet elements);
        #endregion

        #region Protected Helper Methods
        /// <summary>
        /// Show an error dialog
        /// </summary>
        protected void ShowError(string title, string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}\n\nDetails: {ex}" : message;
            
            TaskDialog.Show(title, fullMessage);
        }

        /// <summary>
        /// Show an information dialog
        /// </summary>
        protected void ShowInfo(string title, string message)
        {
            TaskDialog.Show(title, message);
        }

        /// <summary>
        /// Show a confirmation dialog
        /// </summary>
        protected bool ShowConfirmation(string title, string message)
        {
            var result = TaskDialog.Show(title, message, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            return result == TaskDialogResult.Yes;
        }

        /// <summary>
        /// Get the current Revit document
        /// </summary>
        protected Document GetDocument(ExternalCommandData commandData)
        {
            return commandData.Application.ActiveUIDocument.Document;
        }

        /// <summary>
        /// Get the current UI document
        /// </summary>
        protected UIDocument GetUIDocument(ExternalCommandData commandData)
        {
            return commandData.Application.ActiveUIDocument;
        }

        /// <summary>
        /// Get the current application
        /// </summary>
        protected UIApplication GetApplication(ExternalCommandData commandData)
        {
            return commandData.Application;
        }
        #endregion
    }
}


