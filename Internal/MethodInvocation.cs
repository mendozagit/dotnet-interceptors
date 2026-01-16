using DotnetInterceptors.Abstractions;
using CastleIInvocation = Castle.DynamicProxy.IInvocation;

namespace DotnetInterceptors.Internal;

/// <summary>
/// Implementation of IMethodInvocation wrapping Castle's IInvocation.
/// Used for methods returning void or Task (non-generic).
/// </summary>
internal sealed class MethodInvocation(
    CastleIInvocation invocation,
    IInvocationProceedInfo proceedInfo,
    Func<CastleIInvocation, IInvocationProceedInfo, Task> proceed)
    : IMethodInvocation
{
    private IReadOnlyDictionary<string, object?>? _argumentsDictionary;

    public object?[] Arguments => invocation.Arguments;

    public IReadOnlyDictionary<string, object?> ArgumentsDictionary
    {
        get
        {
            if (_argumentsDictionary is null)
            {
                var parameters = Method.GetParameters();
                var dict = new Dictionary<string, object?>(parameters.Length);
                for (var i = 0; i < parameters.Length; i++)
                {
                    dict[parameters[i].Name!] = Arguments[i];
                }
                _argumentsDictionary = dict;
            }
            return _argumentsDictionary;
        }
    }

    public Type[]? GenericArguments => invocation.GenericArguments;

    public object TargetObject => invocation.InvocationTarget ?? invocation.Proxy;

    public MethodInfo Method => invocation.Method;

    public object? ReturnValue
    {
        get => invocation.ReturnValue;
        set => invocation.ReturnValue = value;
    }

    public async Task ProceedAsync()
    {
        await proceed(invocation, proceedInfo);
    }

    public T? GetArgument<T>(int index)
    {
        if (index < 0 || index >= Arguments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Argument index {index} is out of range. Valid range: 0-{Arguments.Length - 1}");
        }
        return (T?)Arguments[index];
    }

    public T? GetArgument<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ArgumentsDictionary.TryGetValue(name, out var value))
        {
            return (T?)value;
        }
        throw new ArgumentException($"No argument named '{name}' exists.", nameof(name));
    }

    public void SetArgument(int index, object? value)
    {
        if (index < 0 || index >= Arguments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Argument index {index} is out of range. Valid range: 0-{Arguments.Length - 1}");
        }
        invocation.SetArgumentValue(index, value);
        _argumentsDictionary = null; // Invalidate cache
    }
}

/// <summary>
/// Implementation of IMethodInvocation for methods returning Task&lt;TResult&gt;.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
internal sealed class MethodInvocation<TResult>(
    CastleIInvocation invocation,
    IInvocationProceedInfo proceedInfo,
    Func<CastleIInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
    : IMethodInvocation
{
    private IReadOnlyDictionary<string, object?>? _argumentsDictionary;
    private TResult? _result;
    private bool _hasResult;

    public object?[] Arguments => invocation.Arguments;

    public IReadOnlyDictionary<string, object?> ArgumentsDictionary
    {
        get
        {
            if (_argumentsDictionary is null)
            {
                var parameters = Method.GetParameters();
                var dict = new Dictionary<string, object?>(parameters.Length);
                for (var i = 0; i < parameters.Length; i++)
                {
                    dict[parameters[i].Name!] = Arguments[i];
                }
                _argumentsDictionary = dict;
            }
            return _argumentsDictionary;
        }
    }

    public Type[]? GenericArguments => invocation.GenericArguments;

    public object TargetObject => invocation.InvocationTarget ?? invocation.Proxy;

    public MethodInfo Method => invocation.Method;

    public object? ReturnValue
    {
        get => _hasResult ? _result : invocation.ReturnValue;
        set
        {
            _result = (TResult?)value;
            _hasResult = true;
            invocation.ReturnValue = value;
        }
    }

    public async Task ProceedAsync()
    {
        _result = await proceed(invocation, proceedInfo);
        _hasResult = true;
        invocation.ReturnValue = _result;
    }

    public T? GetArgument<T>(int index)
    {
        if (index < 0 || index >= Arguments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Argument index {index} is out of range. Valid range: 0-{Arguments.Length - 1}");
        }
        return (T?)Arguments[index];
    }

    public T? GetArgument<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ArgumentsDictionary.TryGetValue(name, out var value))
        {
            return (T?)value;
        }
        throw new ArgumentException($"No argument named '{name}' exists.", nameof(name));
    }

    public void SetArgument(int index, object? value)
    {
        if (index < 0 || index >= Arguments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Argument index {index} is out of range. Valid range: 0-{Arguments.Length - 1}");
        }
        invocation.SetArgumentValue(index, value);
        _argumentsDictionary = null; // Invalidate cache
    }
}
