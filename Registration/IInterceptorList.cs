using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Registration;

/// <summary>
/// A collection of interceptors to be applied to a service.
/// Provides methods to add, remove, and query interceptor registrations.
/// </summary>
public interface IInterceptorList : IReadOnlyList<InterceptorRegistration>
{
    /// <summary>
    /// Adds an interceptor type if not already present.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of interceptor to add.</typeparam>
    /// <returns>True if the interceptor was added; false if already present.</returns>
    bool TryAdd<TInterceptor>() where TInterceptor : class, IInterceptor;

    /// <summary>
    /// Adds an interceptor type if not already present.
    /// </summary>
    /// <param name="interceptorType">The type of interceptor to add.</param>
    /// <returns>True if the interceptor was added; false if already present.</returns>
    bool TryAdd(Type interceptorType);

    /// <summary>
    /// Adds an interceptor type with an explicit order if not already present.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of interceptor to add.</typeparam>
    /// <param name="order">The execution order. Lower values execute first.</param>
    /// <returns>True if the interceptor was added; false if already present.</returns>
    bool TryAdd<TInterceptor>(int order) where TInterceptor : class, IInterceptor;

    /// <summary>
    /// Removes an interceptor type from the list.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of interceptor to remove.</typeparam>
    /// <returns>True if the interceptor was removed; false if not found.</returns>
    bool Remove<TInterceptor>() where TInterceptor : class, IInterceptor;

    /// <summary>
    /// Removes an interceptor type from the list.
    /// </summary>
    /// <param name="interceptorType">The type of interceptor to remove.</param>
    /// <returns>True if the interceptor was removed; false if not found.</returns>
    bool Remove(Type interceptorType);

    /// <summary>
    /// Checks if an interceptor type is registered.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of interceptor to check.</typeparam>
    /// <returns>True if the interceptor is registered; otherwise, false.</returns>
    bool Contains<TInterceptor>() where TInterceptor : class, IInterceptor;

    /// <summary>
    /// Checks if an interceptor type is registered.
    /// </summary>
    /// <param name="interceptorType">The type of interceptor to check.</param>
    /// <returns>True if the interceptor is registered; otherwise, false.</returns>
    bool Contains(Type interceptorType);
}
