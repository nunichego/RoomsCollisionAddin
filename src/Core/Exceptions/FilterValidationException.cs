namespace RoomsManagerAddin.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when filter validation fails
    /// </summary>
    /// <remarks>
    /// This exception is used when filter rules or configurations are invalid.
    /// </remarks>
    public class FilterValidationException : RoomsManagerException
    {
        /// <summary>The filter rule that failed validation</summary>
        public string FilterRule { get; set; }

        /// <summary>The specific validation error</summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// Initializes a new instance of the FilterValidationException class
        /// </summary>
        /// <param name="rule">The filter rule that failed</param>
        /// <param name="error">The validation error message</param>
        public FilterValidationException(string rule, string error)
            : base($"Filter validation failed: {error}")
        {
            FilterRule = rule;
            ValidationError = error;
            UserMessage = $"The filter rule is invalid: {error}. " +
                         $"Please correct the filter and try again.";
        }
    }
}
