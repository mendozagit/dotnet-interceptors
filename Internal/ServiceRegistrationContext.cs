using DotnetInterceptors.Registration;

namespace DotnetInterceptors.Internal;

/// <summary>
/// Implementation of IServiceRegistrationContext.
/// </summary>
internal sealed class ServiceRegistrationContext(
    Type serviceType,
    Type implementationType,
    ServiceLifetime lifetime)
    : IServiceRegistrationContext
{
    public Type ServiceType { get; } = serviceType ?? throw new ArgumentNullException(nameof(serviceType));

    public Type ImplementationType { get; } = implementationType ?? throw new ArgumentNullException(nameof(implementationType));

    public ServiceLifetime Lifetime { get; } = lifetime;

    public IInterceptorList Interceptors { get; } = new InterceptorList();
}
