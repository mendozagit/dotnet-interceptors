namespace DotnetInterceptors.Infrastructure;

/// <summary>
/// Static registry of types that should be excluded from dynamic proxying.
/// Use this to optimize performance for types where proxying causes overhead,
/// such as ASP.NET Core controllers or other framework types.
/// </summary>
/// <remarks>
/// This is thread-safe for concurrent access.
/// </remarks>
public static class ProxyIgnoreTypes
{
    private static readonly HashSet<Type> _ignoredTypes = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Adds a type to the ignore list.
    /// </summary>
    /// <typeparam name="T">The type to ignore.</typeparam>
    public static void Add<T>() => Add(typeof(T));

    /// <summary>
    /// Adds a type to the ignore list.
    /// </summary>
    /// <param name="type">The type to ignore.</param>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static void Add(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_lock)
        {
            _ignoredTypes.Add(type);
        }
    }

    /// <summary>
    /// Adds multiple types to the ignore list.
    /// </summary>
    /// <param name="types">The types to ignore.</param>
    /// <exception cref="ArgumentNullException">Thrown when types is null.</exception>
    public static void Add(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        lock (_lock)
        {
            foreach (var type in types)
            {
                if (type is not null)
                {
                    _ignoredTypes.Add(type);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a type should be ignored from proxying.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="includeDerivedTypes">
    /// If true, returns true if any ignored type is assignable from the given type.
    /// This means derived classes of ignored types are also ignored.
    /// </param>
    /// <returns>True if the type should be ignored; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static bool Contains(Type type, bool includeDerivedTypes = true)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_lock)
        {
            if (includeDerivedTypes)
            {
                return _ignoredTypes.Any(t => t.IsAssignableFrom(type));
            }
            return _ignoredTypes.Contains(type);
        }
    }

    /// <summary>
    /// Removes a type from the ignore list.
    /// </summary>
    /// <param name="type">The type to remove.</param>
    /// <returns>True if the type was removed; false if it was not in the list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static bool Remove(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_lock)
        {
            return _ignoredTypes.Remove(type);
        }
    }

    /// <summary>
    /// Removes a type from the ignore list.
    /// </summary>
    /// <typeparam name="T">The type to remove.</typeparam>
    /// <returns>True if the type was removed; false if it was not in the list.</returns>
    public static bool Remove<T>() => Remove(typeof(T));

    /// <summary>
    /// Clears all ignored types from the list.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _ignoredTypes.Clear();
        }
    }

    /// <summary>
    /// Gets a snapshot of all currently ignored types.
    /// </summary>
    /// <returns>An array of ignored types.</returns>
    public static Type[] GetAll()
    {
        lock (_lock)
        {
            return [.. _ignoredTypes];
        }
    }
}
