using DotnetInterceptors.Attributes;

namespace DotnetInterceptors.Abstractions;

/// <summary>
/// Abstract base class for interceptors providing common functionality and helper methods.
/// Extend this class to create custom interceptors with before/after hooks.
/// </summary>
public abstract class InterceptorBase : IInterceptor
{
    /// <inheritdoc />
    public abstract Task InterceptAsync(IMethodInvocation invocation);

    /// <summary>
    /// Determines whether the current method invocation should be intercepted.
    /// Override this method to implement custom filtering logic.
    /// By default, checks for [DisableInterception] attribute.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <returns>True if the invocation should be intercepted; otherwise, false.</returns>
    protected virtual bool ShouldIntercept(IMethodInvocation invocation)
    {
        return !IsDisabledForMethod(invocation);
    }

    /// <summary>
    /// Checks if interception is disabled for the current method via [DisableInterception] attribute.
    /// Checks both the method and the declaring class.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <returns>True if interception is disabled; otherwise, false.</returns>
    protected bool IsDisabledForMethod(IMethodInvocation invocation)
    {
        var interceptorType = GetType();

        // Check method-level attribute first
        var methodAttr = invocation.Method
            .GetCustomAttribute<DisableInterceptionAttribute>();
        if (methodAttr is not null && methodAttr.IsDisabled(interceptorType))
        {
            return true;
        }

        // Check class-level attribute
        var classAttr = invocation.TargetObject.GetType()
            .GetCustomAttribute<DisableInterceptionAttribute>();
        if (classAttr is not null && classAttr.IsDisabled(interceptorType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes before the target method is invoked.
    /// Override this method to implement pre-processing logic.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task BeforeInvocationAsync(IMethodInvocation invocation)
        => Task.CompletedTask;

    /// <summary>
    /// Executes after the target method completes successfully.
    /// Override this method to implement post-processing logic.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task AfterInvocationAsync(IMethodInvocation invocation)
        => Task.CompletedTask;

    /// <summary>
    /// Executes when an exception is thrown by the target method or a preceding interceptor.
    /// Override this method to implement exception handling or logging.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnExceptionAsync(IMethodInvocation invocation, Exception exception)
        => Task.CompletedTask;

    /// <summary>
    /// Standard interception pattern that calls BeforeInvocationAsync, proceeds, and calls AfterInvocationAsync.
    /// If ShouldIntercept returns false, proceeds directly without before/after hooks.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task StandardInterceptAsync(IMethodInvocation invocation)
    {
        if (!ShouldIntercept(invocation))
        {
            await invocation.ProceedAsync();
            return;
        }

        await BeforeInvocationAsync(invocation);
        try
        {
            await invocation.ProceedAsync();
            await AfterInvocationAsync(invocation);
        }
        catch (Exception ex)
        {
            await OnExceptionAsync(invocation, ex);
            throw;
        }
    }
}
