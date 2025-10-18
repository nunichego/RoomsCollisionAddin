using System;

namespace RoomsManagerAddin.Core.DependencyInjection
{
    /// <summary>
    /// Describes a service registration
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public object ImplementationInstance { get; set; }
        public Func<IServiceContainer, object> ImplementationFactory { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }
}
