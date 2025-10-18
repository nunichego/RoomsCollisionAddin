namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service in the container
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>New instance created each time</summary>
        Transient,

        /// <summary>Single instance per scope (not implemented for simplicity)</summary>
        Scoped,

        /// <summary>Single instance for application lifetime</summary>
        Singleton
    }
}
