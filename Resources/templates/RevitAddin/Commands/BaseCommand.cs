using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.Logging;
using RevitAddinTemplate;

namespace RevitAddinTemplate.Commands
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
        protected ILogger Logger => App.Logger;

        /// <summary>
        /// Service provider for dependency injection
        /// </summary>
        protected IServiceProvider ServiceProvider => App.ServiceProvider;
        #endregion

        #region IExternalCommand Implementation
        /// <summary>
        /// Main execution method for the command
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Log command execution
                Logger?.LogInformation($"Executing command: {GetType().Name}");

                // Execute the command logic
                var result = ExecuteCommand(commandData, ref message, elements);

                // Log successful execution
                if (result == Result.Succeeded)
                {
                    Logger?.LogInformation($"Command {GetType().Name} executed successfully");
                }
                else
                {
                    Logger?.LogWarning($"Command {GetType().Name} completed with result: {result}");
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log error
                Logger?.LogError(ex, $"Error executing command {GetType().Name}");

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
        /// Get the current document
        /// </summary>
        protected Document GetDocument(ExternalCommandData commandData)
        {
            return commandData.Application.ActiveUIDocument?.Document;
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

        /// <summary>
        /// Validate that a document is open
        /// </summary>
        protected bool ValidateDocument(Document doc, ref string message)
        {
            if (doc == null)
            {
                message = "No active document found. Please open a Revit document.";
                return false;
            }

            if (doc.IsReadOnly)
            {
                message = "The document is read-only. Please save the document first.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start a transaction with proper error handling
        /// </summary>
        protected Result ExecuteInTransaction(Document doc, string transactionName, Action<Transaction> action)
        {
            using (var transaction = new Transaction(doc, transactionName))
            {
                try
                {
                    transaction.Start();
                    action(transaction);
                    transaction.Commit();
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    if (transaction.HasStarted())
                    {
                        transaction.RollBack();
                    }
                    throw;
                }
            }
        }
        #endregion
    }
}
