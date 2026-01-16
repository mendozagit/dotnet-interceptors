using DotnetInterceptors.Registration;

namespace DotnetInterceptors.DependencyInjection;

/// <summary>
/// Configuration options for the interception system.
/// </summary>
public sealed class InterceptorOptions
{
    /// <summary>
    /// Gets the list of registrars to execute during service registration.
    /// </summary>
    public List<IInterceptorRegistrar> Registrars { get; } = [];

    /// <summary>
    /// Gets the list of registration actions to execute during service registration.
    /// </summary>
    internal List<Action<IServiceRegistrationContext>> RegistrationActions { get; } = [];

    /// <summary>
    /// Adds a registrar for convention-based interception.
    /// </summary>
    /// <typeparam name="TRegistrar">The type of registrar to add.</typeparam>
    /// <returns>The current options instance for chaining.</returns>
    public InterceptorOptions AddRegistrar<TRegistrar>()
        where TRegistrar : IInterceptorRegistrar, new()
    {
        Registrars.Add(new TRegistrar());
        return this;
    }

    /// <summary>
    /// Adds a registrar instance for convention-based interception.
    /// </summary>
    /// <param name="registrar">The registrar instance to add.</param>
    /// <returns>The current options instance for chaining.</returns>
    public InterceptorOptions AddRegistrar(IInterceptorRegistrar registrar)
    {
        ArgumentNullException.ThrowIfNull(registrar);
        Registrars.Add(registrar);
        return this;
    }

    /// <summary>
    /// Adds a registration action to be invoked for each service registration.
    /// This enables static method-style registration (ABP pattern).
    /// </summary>
    /// <param name="action">The action to execute during registration.</param>
    /// <returns>The current options instance for chaining.</returns>
    /// <example>
    /// <code>
    /// options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
    /// </code>
    /// </example>
    public InterceptorOptions OnServiceRegistered(Action<IServiceRegistrationContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        RegistrationActions.Add(action);
        return this;
    }
}
