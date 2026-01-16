using DotnetInterceptors.Registration;
using IInterceptor = DotnetInterceptors.Abstractions.IInterceptor;

namespace DotnetInterceptors.Internal;

/// <summary>
/// Implementation of IInterceptorList for managing interceptor registrations.
/// </summary>
internal sealed class InterceptorList : IInterceptorList
{
    private readonly List<InterceptorRegistration> _registrations = [];

    public InterceptorRegistration this[int index] => _registrations[index];

    public int Count => _registrations.Count;

    public bool TryAdd<TInterceptor>() where TInterceptor : class, IInterceptor
    {
        return TryAdd(typeof(TInterceptor));
    }

    public bool TryAdd(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        if (!typeof(IInterceptor).IsAssignableFrom(interceptorType))
        {
            throw new ArgumentException(
                $"Type {interceptorType.Name} must implement {nameof(IInterceptor)}",
                nameof(interceptorType));
        }

        if (Contains(interceptorType))
        {
            return false;
        }

        _registrations.Add(InterceptorRegistration.Create(interceptorType));
        return true;
    }

    public bool TryAdd<TInterceptor>(int order) where TInterceptor : class, IInterceptor
    {
        if (Contains<TInterceptor>())
        {
            return false;
        }

        _registrations.Add(new InterceptorRegistration(typeof(TInterceptor), order));
        return true;
    }

    public bool Remove<TInterceptor>() where TInterceptor : class, IInterceptor
    {
        return Remove(typeof(TInterceptor));
    }

    public bool Remove(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        var index = _registrations.FindIndex(r => r.InterceptorType == interceptorType);
        if (index >= 0)
        {
            _registrations.RemoveAt(index);
            return true;
        }
        return false;
    }

    public bool Contains<TInterceptor>() where TInterceptor : class, IInterceptor
    {
        return Contains(typeof(TInterceptor));
    }

    public bool Contains(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);
        return _registrations.Any(r => r.InterceptorType == interceptorType);
    }

    public IEnumerator<InterceptorRegistration> GetEnumerator()
    {
        return _registrations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
