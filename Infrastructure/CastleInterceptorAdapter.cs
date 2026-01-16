using DotnetInterceptors.Internal;
using CastleIInvocation = Castle.DynamicProxy.IInvocation;
using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Infrastructure;

/// <summary>
/// Adapts DotnetInterceptors.IInterceptor to Castle.DynamicProxy.IAsyncInterceptor.
/// Properly handles async method interception using Castle.Core.AsyncInterceptor.
/// </summary>
internal sealed class CastleInterceptorAdapter(IInterceptor interceptor) : AsyncInterceptorBase
{
    private readonly IInterceptor _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));

    /// <summary>
    /// Intercepts synchronous methods (void or non-Task return types).
    /// </summary>
    protected override async Task InterceptAsync(
        CastleIInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<CastleIInvocation, IInvocationProceedInfo, Task> proceed)
    {
        var methodInvocation = new MethodInvocation(invocation, proceedInfo, proceed);
        await _interceptor.InterceptAsync(methodInvocation);
    }

    /// <summary>
    /// Intercepts async methods that return Task&lt;TResult&gt;.
    /// </summary>
    protected override async Task<TResult> InterceptAsync<TResult>(
        CastleIInvocation invocation,
        IInvocationProceedInfo proceedInfo,
        Func<CastleIInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    {
        var methodInvocation = new MethodInvocation<TResult>(invocation, proceedInfo, proceed);
        await _interceptor.InterceptAsync(methodInvocation);
        return (TResult)methodInvocation.ReturnValue!;
    }
}
