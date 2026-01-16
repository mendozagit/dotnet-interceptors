using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Attributes;

/// <summary>
/// Marks a class, interface, or method for interception by a specific interceptor type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptAttribute : Attribute
{
    /// <summary>
    /// Gets the type of interceptor to apply.
    /// </summary>
    public Type InterceptorType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptAttribute"/> class.
    /// </summary>
    /// <param name="interceptorType">The type of interceptor to apply. Must implement <see cref="IInterceptor"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when interceptorType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when interceptorType does not implement <see cref="IInterceptor"/>.</exception>
    public InterceptAttribute(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        if (!typeof(IInterceptor).IsAssignableFrom(interceptorType))
        {
            throw new ArgumentException(
                $"Type {interceptorType.Name} must implement {nameof(IInterceptor)}",
                nameof(interceptorType));
        }

        InterceptorType = interceptorType;
    }
}

/// <summary>
/// Marks a class, interface, or method for interception by a specific interceptor type.
/// Generic version for compile-time type safety.
/// </summary>
/// <typeparam name="TInterceptor">The type of interceptor to apply.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptAttribute<TInterceptor> : Attribute
    where TInterceptor : class, IInterceptor
{
    /// <summary>
    /// Gets the type of interceptor to apply.
    /// </summary>
    public Type InterceptorType => typeof(TInterceptor);
}
