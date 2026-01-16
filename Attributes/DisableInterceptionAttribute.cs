using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Attributes;

/// <summary>
/// Disables interception for a class or specific method.
/// When applied to a class, all methods are excluded from interception.
/// When applied to a method, only that method is excluded.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableInterceptionAttribute : Attribute
{
    /// <summary>
    /// Gets the specific interceptor types to disable.
    /// If empty, all interceptors are disabled.
    /// </summary>
    public Type[] InterceptorTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisableInterceptionAttribute"/> class
    /// that disables all interceptors.
    /// </summary>
    public DisableInterceptionAttribute()
    {
        InterceptorTypes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisableInterceptionAttribute"/> class
    /// that disables specific interceptor types.
    /// </summary>
    /// <param name="interceptorTypes">The interceptor types to disable.</param>
    public DisableInterceptionAttribute(params Type[] interceptorTypes)
    {
        foreach (var type in interceptorTypes)
        {
            if (!typeof(IInterceptor).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    $"Type {type.Name} must implement {nameof(IInterceptor)}",
                    nameof(interceptorTypes));
            }
        }

        InterceptorTypes = interceptorTypes;
    }

    /// <summary>
    /// Determines whether a specific interceptor type is disabled by this attribute.
    /// </summary>
    /// <param name="interceptorType">The interceptor type to check.</param>
    /// <returns>True if the interceptor is disabled; otherwise, false.</returns>
    public bool IsDisabled(Type interceptorType)
    {
        if (InterceptorTypes.Length == 0)
        {
            return true; // All interceptors disabled
        }

        return InterceptorTypes.Contains(interceptorType);
    }
}
