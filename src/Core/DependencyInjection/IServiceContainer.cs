using System;

namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Service container for dependency injection
    /// </summary>
    public interface IServiceContainer
    {
        /// <summary>Register a service with transient lifetime</summary>
        void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>Register a service with singleton lifetime</summary>
        void AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>Register a singleton instance</summary>
        void AddSingleton<TService>(TService instance) where TService : class;

        /// <summary>Register a service with a factory</summary>
        void AddTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class;

        /// <summary>Resolve a service</summary>
        TService Resolve<TService>() where TService : class;

        /// <summary>Resolve a service by type</summary>
        object Resolve(Type serviceType);

        /// <summary>Check if service is registered</summary>
        bool IsRegistered<TService>() where TService : class;
    }
}
