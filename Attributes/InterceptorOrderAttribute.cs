namespace DotnetInterceptors.Attributes;

/// <summary>
/// Specifies the execution order of an interceptor in the pipeline.
/// Interceptors with lower order values execute first (outer interceptors).
/// </summary>
/// <remarks>
/// Default order is 0. Use negative values for interceptors that should run first
/// (e.g., logging, timing) and positive values for interceptors that should run later
/// (e.g., caching, transaction management).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InterceptorOrderAttribute : Attribute
{
    /// <summary>
    /// Gets the order value. Lower values execute first.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">The order value. Lower values execute first.</param>
    public InterceptorOrderAttribute(int order)
    {
        Order = order;
    }
}
