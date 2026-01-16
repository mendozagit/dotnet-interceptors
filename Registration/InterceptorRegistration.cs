using DotnetInterceptors.Attributes;
using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Registration;

/// <summary>
/// Represents a registered interceptor with its configuration.
/// </summary>
/// <param name="InterceptorType">The type of the interceptor.</param>
/// <param name="Order">The execution order. Lower values execute first.</param>
public sealed record InterceptorRegistration(Type InterceptorType, int Order = 0)
{
    /// <summary>
    /// Creates a registration from an interceptor type, reading order from <see cref="InterceptorOrderAttribute"/> if present.
    /// </summary>
    /// <typeparam name="TInterceptor">The type of interceptor.</typeparam>
    /// <returns>A new <see cref="InterceptorRegistration"/> instance.</returns>
    public static InterceptorRegistration Create<TInterceptor>()
        where TInterceptor : class, IInterceptor
    {
        return Create(typeof(TInterceptor));
    }

    /// <summary>
    /// Creates a registration from an interceptor type, reading order from <see cref="InterceptorOrderAttribute"/> if present.
    /// </summary>
    /// <param name="interceptorType">The type of interceptor.</param>
    /// <returns>A new <see cref="InterceptorRegistration"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when interceptorType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when interceptorType does not implement <see cref="IInterceptor"/>.</exception>
    public static InterceptorRegistration Create(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        if (!typeof(IInterceptor).IsAssignableFrom(interceptorType))
        {
            throw new ArgumentException(
                $"Type {interceptorType.Name} must implement {nameof(IInterceptor)}",
                nameof(interceptorType));
        }

        var orderAttr = interceptorType.GetCustomAttribute<InterceptorOrderAttribute>();
        return new InterceptorRegistration(interceptorType, orderAttr?.Order ?? 0);
    }
}
