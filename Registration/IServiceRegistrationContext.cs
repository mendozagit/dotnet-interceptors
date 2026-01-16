namespace DotnetInterceptors.Registration;

/// <summary>
/// Context provided during service registration for interceptor configuration.
/// Used by <see cref="IInterceptorRegistrar"/> to determine which interceptors to apply.
/// </summary>
public interface IServiceRegistrationContext
{
    /// <summary>
    /// Gets the service type being registered (typically an interface).
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// Gets the implementation type being registered.
    /// </summary>
    Type ImplementationType { get; }

    /// <summary>
    /// Gets the service lifetime (Singleton, Scoped, or Transient).
    /// </summary>
    ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets the collection of interceptors to apply to this service.
    /// Use this to add or remove interceptors based on conventions.
    /// </summary>
    IInterceptorList Interceptors { get; }
}
