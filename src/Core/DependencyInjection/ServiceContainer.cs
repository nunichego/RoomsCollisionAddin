using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Simple dependency injection container implementation
    /// </summary>
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services;
        private readonly Dictionary<Type, object> _singletonInstances;

        public ServiceContainer()
        {
            _services = new Dictionary<Type, ServiceDescriptor>();
            _singletonInstances = new Dictionary<Type, object>();
        }

        public void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }

        public void AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
        }

        public void AddSingleton<TService>(TService instance) where TService : class
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationInstance = instance,
                Lifetime = ServiceLifetime.Singleton
            };
            _singletonInstances[typeof(TService)] = instance;
        }

        public void AddTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class
        {
            _services[typeof(TService)] = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationFactory = container => factory(container),
                Lifetime = ServiceLifetime.Transient
            };
        }

        public TService Resolve<TService>() where TService : class
        {
            return (TService)Resolve(typeof(TService));
        }

        public object Resolve(Type serviceType)
        {
            if (!_services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException(
                    $"Service of type {serviceType.Name} is not registered");
            }

            var descriptor = _services[serviceType];

            // Return singleton instance if already created
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (_singletonInstances.ContainsKey(serviceType))
                {
                    return _singletonInstances[serviceType];
                }
            }

            // Create instance
            object instance;

            if (descriptor.ImplementationInstance != null)
            {
                instance = descriptor.ImplementationInstance;
            }
            else if (descriptor.ImplementationFactory != null)
            {
                instance = descriptor.ImplementationFactory(this);
            }
            else
            {
                instance = CreateInstance(descriptor.ImplementationType);
            }

            // Cache singleton
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[serviceType] = instance;
            }

            return instance;
        }

        private object CreateInstance(Type type)
        {
            // Get constructor with most parameters (assumes DI constructor)
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No public constructors found for {type.Name}");
            }

            var constructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterInstances[i] = Resolve(parameters[i].ParameterType);
            }

            return constructor.Invoke(parameterInstances);
        }

        public bool IsRegistered<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }
    }
}
