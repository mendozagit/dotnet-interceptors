using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Registration;

/// <summary>
/// Interface for registrars that determine when to apply interceptors based on conventions.
/// Implement this interface for convention-based interceptor registration.
/// </summary>
/// <remarks>
/// Registrars are invoked for each service registration and can inspect the service
/// and implementation types to decide whether to add interceptors.
/// </remarks>
/// <example>
/// <code>
/// public class LoggingInterceptorRegistrar : IInterceptorRegistrar
/// {
///     public void RegisterIfNeeded(IServiceRegistrationContext context)
///     {
///         if (typeof(ILoggingEnabled).IsAssignableFrom(context.ImplementationType))
///         {
///             context.Interceptors.TryAdd&lt;LoggingInterceptor&gt;();
///         }
///     }
/// }
/// </code>
/// </example>
public interface IInterceptorRegistrar
{
    /// <summary>
    /// Determines if interception should be applied and registers interceptors.
    /// </summary>
    /// <param name="context">The service registration context containing type information and the interceptor list.</param>
    void RegisterIfNeeded(IServiceRegistrationContext context);
}

/// <summary>
/// Strongly-typed registrar interface for a specific interceptor type.
/// </summary>
/// <typeparam name="TInterceptor">The type of interceptor this registrar manages.</typeparam>
public interface IInterceptorRegistrar<TInterceptor> : IInterceptorRegistrar
    where TInterceptor : class, IInterceptor
{
}
