namespace DotnetInterceptors.Abstractions;

/// <summary>
/// Core interceptor interface for async-first method interception.
/// Implement this interface to create custom interceptors.
/// </summary>
public interface IInterceptor
{
    /// <summary>
    /// Intercepts a method invocation asynchronously.
    /// </summary>
    /// <param name="invocation">The method invocation context containing arguments, method info, and proceed delegate.</param>
    /// <returns>A task representing the asynchronous interception operation.</returns>
    Task InterceptAsync(IMethodInvocation invocation);
}
