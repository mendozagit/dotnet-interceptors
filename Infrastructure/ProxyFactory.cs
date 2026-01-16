using DotnetInterceptors.Registration;
using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Infrastructure;

/// <summary>
/// Factory for creating proxied instances with interceptors.
/// Wraps Castle DynamicProxy's ProxyGenerator with a singleton pattern for optimal performance.
/// </summary>
public sealed class ProxyFactory
{
    // ProxyGenerator should be reused as per Castle documentation to leverage type caching
    private static readonly Lazy<ProxyGenerator> _generator =
        new(() => new ProxyGenerator(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving interceptor instances.</param>
    public ProxyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the shared ProxyGenerator instance.
    /// </summary>
    public static ProxyGenerator Generator => _generator.Value;

    /// <summary>
    /// Creates a proxy for an interface-based service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="target">The target instance to proxy.</param>
    /// <param name="interceptorRegistrations">The interceptors to apply.</param>
    /// <returns>A proxied instance of the service.</returns>
    public TService CreateInterfaceProxy<TService>(
        TService target,
        IEnumerable<InterceptorRegistration> interceptorRegistrations)
        where TService : class
    {
        var interceptors = ResolveInterceptors(interceptorRegistrations);
        if (interceptors.Length == 0)
        {
            return target;
        }

        return Generator.CreateInterfaceProxyWithTarget(target, interceptors);
    }

    /// <summary>
    /// Creates a proxy for a class-based service.
    /// </summary>
    /// <typeparam name="TService">The service class type.</typeparam>
    /// <param name="target">The target instance to proxy.</param>
    /// <param name="interceptorRegistrations">The interceptors to apply.</param>
    /// <returns>A proxied instance of the service.</returns>
    public TService CreateClassProxy<TService>(
        TService target,
        IEnumerable<InterceptorRegistration> interceptorRegistrations)
        where TService : class
    {
        var interceptors = ResolveInterceptors(interceptorRegistrations);
        if (interceptors.Length == 0)
        {
            return target;
        }

        return Generator.CreateClassProxyWithTarget(target, interceptors);
    }

    /// <summary>
    /// Creates a proxy for an object based on its service type.
    /// Automatically selects interface or class proxying based on the service type.
    /// </summary>
    /// <param name="serviceType">The service type (interface or class).</param>
    /// <param name="target">The target instance to proxy.</param>
    /// <param name="interceptorRegistrations">The interceptors to apply.</param>
    /// <returns>A proxied instance of the service.</returns>
    public object CreateProxy(
        Type serviceType,
        object target,
        IEnumerable<InterceptorRegistration> interceptorRegistrations)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(target);

        var interceptors = ResolveInterceptors(interceptorRegistrations);
        if (interceptors.Length == 0)
        {
            return target;
        }

        if (serviceType.IsInterface)
        {
            return Generator.CreateInterfaceProxyWithTarget(serviceType, target, interceptors);
        }

        return Generator.CreateClassProxyWithTarget(serviceType, target, interceptors);
    }

    /// <summary>
    /// Creates a proxy for a service, attempting interface proxying first, then class proxying.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="target">The target instance to proxy.</param>
    /// <param name="interceptorRegistrations">The interceptors to apply.</param>
    /// <returns>A proxied instance of the service.</returns>
    public object CreateProxyWithFallback(
        Type serviceType,
        Type implementationType,
        object target,
        IEnumerable<InterceptorRegistration> interceptorRegistrations)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(target);

        var interceptors = ResolveInterceptors(interceptorRegistrations);
        if (interceptors.Length == 0)
        {
            return target;
        }

        // Prefer interface proxying
        if (serviceType.IsInterface)
        {
            return Generator.CreateInterfaceProxyWithTarget(serviceType, target, interceptors);
        }

        // Fall back to class proxying if the class has virtual methods
        if (ProxyHelper.CanBeProxied(implementationType))
        {
            return Generator.CreateClassProxyWithTarget(implementationType, target, interceptors);
        }

        // If we can't proxy, return the original target
        return target;
    }

    private Castle.DynamicProxy.IInterceptor[] ResolveInterceptors(
        IEnumerable<InterceptorRegistration> registrations)
    {
        return registrations
            .OrderBy(r => r.Order)
            .Select(r => CreateCastleInterceptor(r.InterceptorType))
            .ToArray();
    }

    private Castle.DynamicProxy.IInterceptor CreateCastleInterceptor(Type interceptorType)
    {
        var interceptor = (IInterceptor)_serviceProvider.GetRequiredService(interceptorType);
        var asyncInterceptor = new CastleInterceptorAdapter(interceptor);
        return asyncInterceptor.ToInterceptor();
    }
}
