using DotnetInterceptors.Infrastructure;
using DotnetInterceptors.Internal;
using DotnetInterceptors.Registration;
using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.DependencyInjection;

/// <summary>
/// Extension members for integrating DotnetInterceptors with Microsoft.Extensions.DependencyInjection.
/// Uses C# 14 extension block syntax to extend <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Lock _lock = new();
    private static InterceptorOptions? _options;
    private static bool _interceptorsConfigured;

    /// <summary>
    /// Extension block containing extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the interceptor infrastructure to the service collection.
        /// </summary>
        /// <param name="configure">Optional configuration action for <see cref="InterceptorOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddInterceptors(Action<InterceptorOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            lock (_lock)
            {
                if (_interceptorsConfigured)
                {
                    return services;
                }

                _options = new InterceptorOptions();
                configure?.Invoke(_options);

                // Register the proxy factory
                services.AddSingleton<ProxyFactory>();

                _interceptorsConfigured = true;
            }

            return services;
        }

        /// <summary>
        /// Registers an interceptor type to the DI container.
        /// </summary>
        /// <typeparam name="TInterceptor">The type of interceptor to register.</typeparam>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddInterceptor<TInterceptor>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterceptor : class, IInterceptor
        {
            ArgumentNullException.ThrowIfNull(services);
            services.Add(new ServiceDescriptor(typeof(TInterceptor), typeof(TInterceptor), lifetime));
            return services;
        }

        /// <summary>
        /// Registers an interceptor type with a factory to the DI container.
        /// </summary>
        /// <typeparam name="TInterceptor">The type of interceptor to register.</typeparam>
        /// <param name="factory">The factory function to create the interceptor.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddInterceptor<TInterceptor>(Func<IServiceProvider, TInterceptor> factory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterceptor : class, IInterceptor
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(factory);
            services.Add(new ServiceDescriptor(typeof(TInterceptor), factory, lifetime));
            return services;
        }

        /// <summary>
        /// Registers a service with interception enabled.
        /// </summary>
        /// <typeparam name="TService">The service type (typically an interface).</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="configureInterceptors">Action to configure which interceptors to apply.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddIntercepted<TService, TImplementation>(Action<IInterceptorList> configureInterceptors,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TService : class
            where TImplementation : class, TService
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureInterceptors);

            var interceptorList = new InterceptorList();
            configureInterceptors(interceptorList);

            services.Add(new ServiceDescriptor(
                typeof(TService),
                sp =>
                {
                    var factory = sp.GetRequiredService<ProxyFactory>();
                    var instance = ActivatorUtilities.CreateInstance<TImplementation>(sp);
                    return factory.CreateProxy(typeof(TService), instance, interceptorList);
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Builds a service provider with convention-based interception applied.
        /// Call this instead of BuildServiceProvider() to enable automatic interceptor registration.
        /// </summary>
        /// <returns>The configured service provider.</returns>
        public IServiceProvider BuildServiceProviderWithInterceptors()
        {
            ArgumentNullException.ThrowIfNull(services);

            ProcessServiceRegistrations(services);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Builds a service provider with convention-based interception applied.
        /// Call this instead of BuildServiceProvider() to enable automatic interceptor registration.
        /// </summary>
        /// <param name="options">Service provider options.</param>
        /// <returns>The configured service provider.</returns>
        public IServiceProvider BuildServiceProviderWithInterceptors(ServiceProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);

            ProcessServiceRegistrations(services);
            return services.BuildServiceProvider(options);
        }

        /// <summary>
        /// Processes all registered services and applies interceptors based on conventions.
        /// Call this before building the service provider if not using BuildServiceProviderWithInterceptors.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection ApplyInterceptorConventions()
        {
            ArgumentNullException.ThrowIfNull(services);
            ProcessServiceRegistrations(services);
            return services;
        }
    }

    private static void ProcessServiceRegistrations(IServiceCollection services)
    {
        if (_options is null)
        {
            return;
        }

        // Take a snapshot of descriptors to avoid modification during iteration
        var descriptors = services.ToList();

        foreach (var descriptor in descriptors)
        {
            // Skip if no implementation type (factories, instances)
            if (descriptor.ImplementationType is null)
            {
                continue;
            }

            // Skip ignored types
            if (ProxyIgnoreTypes.Contains(descriptor.ImplementationType))
            {
                continue;
            }

            // Skip types that can't be proxied
            if (!descriptor.ServiceType.IsInterface &&
                !ProxyHelper.CanBeProxied(descriptor.ImplementationType))
            {
                continue;
            }

            var context = new ServiceRegistrationContext(
                descriptor.ServiceType,
                descriptor.ImplementationType,
                descriptor.Lifetime);

            // Run all registrars
            foreach (var registrar in _options.Registrars)
            {
                registrar.RegisterIfNeeded(context);
            }

            // Run all registration actions
            foreach (var action in _options.RegistrationActions)
            {
                action(context);
            }

            // If interceptors were added, replace the service descriptor
            if (context.Interceptors.Count > 0)
            {
                ReplaceWithProxy(services, descriptor, (InterceptorList)context.Interceptors);
            }
        }
    }

    private static void ReplaceWithProxy(
        IServiceCollection services,
        ServiceDescriptor descriptor,
        InterceptorList interceptors)
    {
        services.Remove(descriptor);

        services.Add(new ServiceDescriptor(
            descriptor.ServiceType,
            sp =>
            {
                var factory = sp.GetRequiredService<ProxyFactory>();

                object instance;
                if (descriptor.ImplementationFactory is not null)
                {
                    instance = descriptor.ImplementationFactory(sp);
                }
                else if (descriptor.ImplementationInstance is not null)
                {
                    instance = descriptor.ImplementationInstance;
                }
                else
                {
                    instance = ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
                }

                return factory.CreateProxyWithFallback(
                    descriptor.ServiceType,
                    descriptor.ImplementationType!,
                    instance,
                    interceptors);
            },
            descriptor.Lifetime));
    }

    /// <summary>
    /// Resets the interceptor configuration. Primarily for testing purposes.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _options = null;
            _interceptorsConfigured = false;
        }
    }
}
