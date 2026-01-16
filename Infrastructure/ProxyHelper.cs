namespace DotnetInterceptors.Infrastructure;

/// <summary>
/// Helper utilities for working with Castle DynamicProxy proxies.
/// </summary>
public static class ProxyHelper
{
    private const string ProxyNamespace = "Castle.Proxies";

    /// <summary>
    /// Determines if an object is a Castle DynamicProxy instance.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object is a dynamic proxy; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
    public static bool IsProxy(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.GetType().Namespace == ProxyNamespace;
    }

    /// <summary>
    /// Unwraps a proxy to get the underlying target object.
    /// If the object is not a proxy, returns the original object.
    /// </summary>
    /// <param name="obj">The object to unwrap.</param>
    /// <returns>The underlying target object, or the original object if not a proxy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
    public static object UnProxy(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj.GetType().Namespace != ProxyNamespace)
        {
            return obj;
        }

        var targetField = obj.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name == "__target");

        return targetField?.GetValue(obj) ?? obj;
    }

    /// <summary>
    /// Gets the actual type of an object, unwrapping any proxy layer.
    /// </summary>
    /// <param name="obj">The object to get the type of.</param>
    /// <returns>The actual underlying type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
    public static Type GetUnProxiedType(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj.GetType().Namespace == ProxyNamespace)
        {
            var target = UnProxy(obj);
            if (target == obj)
            {
                // If we couldn't unwrap, the base type is the original type
                return obj.GetType().BaseType ?? obj.GetType();
            }
            return target.GetType();
        }

        return obj.GetType();
    }

    /// <summary>
    /// Checks if a type can be proxied by Castle DynamicProxy.
    /// A type can be proxied if it's an interface, or if it has virtual methods.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be proxied; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public static bool CanBeProxied(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Interfaces can always be proxied
        if (type.IsInterface)
        {
            return true;
        }

        // Sealed classes cannot be proxied via inheritance
        if (type.IsSealed)
        {
            return false;
        }

        // Check if the class has any virtual methods that can be intercepted
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.IsVirtual && !m.IsFinal);
    }

    /// <summary>
    /// Gets the proxy target interface for a given type.
    /// If the type is an interface, returns it directly.
    /// Otherwise, returns the first implemented interface or null.
    /// </summary>
    /// <param name="type">The type to get the proxy interface for.</param>
    /// <returns>The interface to use for proxying, or null if none suitable.</returns>
    public static Type? GetProxyInterface(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsInterface)
        {
            return type;
        }

        // Return the first public interface that's not a system interface
        return type.GetInterfaces()
            .FirstOrDefault(i => !i.Namespace?.StartsWith("System", StringComparison.Ordinal) == true);
    }
}
