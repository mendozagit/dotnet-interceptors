namespace DotnetInterceptors.Abstractions;

/// <summary>
/// Represents the context of a method invocation being intercepted.
/// Provides access to method metadata, arguments, and the ability to proceed to the next interceptor or target.
/// </summary>
public interface IMethodInvocation
{
    /// <summary>
    /// Gets the arguments passed to the method as an array.
    /// </summary>
    object?[] Arguments { get; }

    /// <summary>
    /// Gets the arguments as a read-only dictionary keyed by parameter name.
    /// </summary>
    IReadOnlyDictionary<string, object?> ArgumentsDictionary { get; }

    /// <summary>
    /// Gets the generic type arguments if the method is generic; otherwise, null or empty.
    /// </summary>
    Type[]? GenericArguments { get; }

    /// <summary>
    /// Gets the target object on which the method is being invoked.
    /// </summary>
    object TargetObject { get; }

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> of the method being invoked.
    /// </summary>
    MethodInfo Method { get; }

    /// <summary>
    /// Gets or sets the return value of the method invocation.
    /// Can be modified by interceptors before or after proceeding.
    /// </summary>
    object? ReturnValue { get; set; }

    /// <summary>
    /// Proceeds to the next interceptor in the pipeline or invokes the target method.
    /// This method properly awaits async method completion.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProceedAsync();

    /// <summary>
    /// Gets an argument value by its index with type casting.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument.</typeparam>
    /// <param name="index">The zero-based index of the argument.</param>
    /// <returns>The argument value cast to type T, or default if null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    T? GetArgument<T>(int index);

    /// <summary>
    /// Gets an argument value by its parameter name with type casting.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument.</typeparam>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The argument value cast to type T, or default if null.</returns>
    /// <exception cref="ArgumentException">Thrown when no parameter with the given name exists.</exception>
    T? GetArgument<T>(string name);

    /// <summary>
    /// Sets an argument value by its index. Useful for modifying ref/out parameters.
    /// </summary>
    /// <param name="index">The zero-based index of the argument.</param>
    /// <param name="value">The new value for the argument.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    void SetArgument(int index, object? value);
}
