# DotnetInterceptors

A modern, lightweight AOP (Aspect-Oriented Programming) library for .NET with async-first interceptors built on Castle.DynamicProxy.

## Table of Contents

- [Overview](#overview)
- [What is Dynamic Proxying](#what-is-dynamic-proxying)
- [Use Cases](#use-cases)
- [Comparison with MVC Filters](#comparison-with-mvc-filters)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Creating Interceptors](#creating-interceptors)
- [Registration Patterns](#registration-patterns)
- [Excluding Types and Methods](#excluding-types-and-methods)
- [Advanced Usage](#advanced-usage)
- [Restrictions and Important Notes](#restrictions-and-important-notes)
- [Performance Considerations](#performance-considerations)
- [Testing Interceptors](#testing-interceptors)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)
- [API Reference](#api-reference)
- [Architecture](#architecture)

## Overview

DotnetInterceptors provides a clean abstraction over Castle.DynamicProxy for implementing cross-cutting concerns thru interceptor such as:

- Logging
- Caching
- Validation
- Transaction management
- Performance monitoring
- Authorization

### Key Features

- **Async-first design**: Native support for `async/await` patterns
- **Convention-based registration**: Flexible registrar pattern for automatic interceptor application
- **Attribute-based configuration**: `[Intercept<T>]` and `[DisableInterception]`
- **Microsoft.Extensions.DependencyInjection integration**: Seamless DI support
- **Type and method exclusion**: Fine-grained control over what gets intercepted
- **Interceptor ordering**: Control execution order with `[InterceptorOrder]`

## What is Dynamic Proxying

Interception is a technique that allows executing additional logic before or after a method call without directly modifying the method's code. This is achieved through dynamic proxying, where the runtime generates proxy classes that wrap the original class.

When a method is called on a proxied object:

1. The call is intercepted by the proxy
2. Custom behaviors (like logging, validation, or authorization) are executed
3. The original method is called
4. Additional logic can be executed after the method completes
5. The result is returned to the caller

This enables cross-cutting concerns (logic that applies across many parts of an application) to be handled in a clean, reusable way without code duplication.

## Use Cases

Interceptors are ideal for implementing cross-cutting concerns that apply across many parts of an application. Here are the most common use cases:

### Unit of Work (Transaction Management)

Automatically begins and commits/rolls back a database transaction when entering or exiting a service method. This ensures data consistency without manual transaction management in every method.

### Input Validation

Input DTOs and method parameters are automatically validated against data annotation attributes and custom validation rules before executing the service logic, providing consistent validation behavior across all services.

### Authorization

Checks user permissions before allowing the execution of service methods, ensuring security policies are enforced consistently without scattering authorization code throughout the application.

### Feature Checking

Checks if a feature is enabled before executing the service logic, allowing you to conditionally enable or restrict functionality for tenants or specific users without modifying business logic.

### Auditing

Automatically logs who performed an action, when it happened, what parameters were used, and what data was involved, providing comprehensive audit trails for compliance and debugging.

### Logging and Performance Monitoring

Logs method entry, exit, execution time, and exceptions without adding logging code to every method. Useful for debugging, performance analysis, and operational monitoring.

### Caching

Intercepts method calls to check if the result is already cached, returning the cached value if available, or caching the result after execution. Eliminates repetitive caching logic.

### Exception Handling

Catches and handles exceptions in a centralized manner, allowing for consistent error logging, transformation, or recovery strategies across all services.

### Retry Logic

Automatically retries failed operations (like network calls) with configurable retry policies, without cluttering business logic with retry loops.

## Comparison with MVC Filters

If you are familiar with ASP.NET Core MVC, you have likely used action filters or page filters. Interceptors are conceptually similar but have key differences:

### Similarities

| Aspect | MVC Filters | Interceptors |
|--------|-------------|--------------|
| Execute code before/after | Yes | Yes |
| Cross-cutting concerns | Yes | Yes |
| Async support | Yes | Yes |

### Differences

| Aspect | MVC Filters | Interceptors |
|--------|-------------|--------------|
| Scope | MVC request pipeline only | Any class or service in the application |
| Target | Controllers and Razor Pages | Application services, domain services, repositories, any DI-resolved service |
| Configuration | Attributes or middleware | Dependency injection and dynamic proxies |
| Runtime | Only during HTTP requests | Any code path that uses DI |

**When to use each:**

- **MVC Filters**: For concerns specific to HTTP requests (authentication, response caching, CORS)
- **Interceptors**: For concerns that apply to business logic regardless of the entry point (validation, transactions, auditing)

## Installation

```bash
dotnet add package DotnetInterceptors
```


## Quick Start

### 1. Create an Interceptor

```csharp
using DotnetInterceptors;

public class LoggingInterceptor : InterceptorBase
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        var methodName = $"{invocation.TargetObject.GetType().Name}.{invocation.Method.Name}";

        _logger.LogInformation("Entering {Method}", methodName);

        try
        {
            await invocation.ProceedAsync();
            _logger.LogInformation("Exited {Method} successfully", methodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in {Method}", methodName);
            throw;
        }
    }
}
```

### 2. Create a Marker Interface

```csharp
public interface ILoggingEnabled { }
```

### 3. Create a Registrar

```csharp
public static class LoggingInterceptorRegistrar
{
    public static void RegisterIfNeeded(IServiceRegistrationContext context)
    {
        if (ProxyIgnoreTypes.Contains(context.ImplementationType))
            return;

        if (typeof(ILoggingEnabled).IsAssignableFrom(context.ImplementationType))
        {
            context.Interceptors.TryAdd<LoggingInterceptor>();
        }
    }
}
```

### 4. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure interceptors
builder.Services.AddInterceptors(options =>
{
    options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
});

// Register interceptor type
builder.Services.AddInterceptor<LoggingInterceptor>();

// Register your services
builder.Services.AddScoped<IOrderService, OrderService>();

// Apply interceptor conventions (must be called after all registrations)
builder.Services.ApplyInterceptorConventions();

var app = builder.Build();
```

### 5. Implement Your Service

```csharp
public interface IOrderService
{
    Task<Order> CreateOrderAsync(string customerName, decimal total);
}

public class OrderService : IOrderService, ILoggingEnabled
{
    public virtual async Task<Order> CreateOrderAsync(string customerName, decimal total)
    {
        await Task.Delay(100);
        return new Order(Guid.NewGuid(), customerName, total, DateTime.UtcNow);
    }
}
```

When `CreateOrderAsync` is called, the output will be:

```
Entering OrderService.CreateOrderAsync
Exited OrderService.CreateOrderAsync successfully
```

## Core Concepts

### IInterceptor

The core interface for all interceptors:

```csharp
public interface IInterceptor
{
    Task InterceptAsync(IMethodInvocation invocation);
}
```

### IMethodInvocation

Provides context about the method being intercepted:

```csharp
public interface IMethodInvocation
{
    object?[] Arguments { get; }
    IReadOnlyDictionary<string, object?> ArgumentsDictionary { get; }
    Type[]? GenericArguments { get; }
    object TargetObject { get; }
    MethodInfo Method { get; }
    object? ReturnValue { get; set; }

    Task ProceedAsync();
    T? GetArgument<T>(int index);
    T? GetArgument<T>(string name);
    void SetArgument(int index, object? value);
}
```

### InterceptorBase

Abstract base class providing common functionality:

```csharp
public abstract class InterceptorBase : IInterceptor
{
    public abstract Task InterceptAsync(IMethodInvocation invocation);

    protected virtual bool ShouldIntercept(IMethodInvocation invocation);
    protected bool IsDisabledForMethod(IMethodInvocation invocation);
    protected virtual Task BeforeInvocationAsync(IMethodInvocation invocation);
    protected virtual Task AfterInvocationAsync(IMethodInvocation invocation);
    protected virtual Task OnExceptionAsync(IMethodInvocation invocation, Exception exception);
    protected async Task StandardInterceptAsync(IMethodInvocation invocation);
}
```

### Registrar

A **Registrar** is a class or method that evaluates whether a service should be intercepted and decides which interceptors to apply based on conventions. Each interceptor can have its own registrar with unique rules.

**Responsibilities:**

| Component | Responsibility |
|-----------|----------------|
| Interceptor | Defines WHAT to do (logging, caching, validation) |
| Registrar | Defines WHEN to apply the interceptor (conventions) |
| Marker Interface | Signals WHICH services want interception |

**Why Registrars instead of automatic scanning:**

- Each interceptor can have different detection rules (interfaces, attributes, namespaces)
- Explicit control over what gets intercepted
- No hidden conventions or magic
- Better performance (lazy evaluation vs assembly scanning)
- Easy to test and debug

```csharp
public static class LoggingInterceptorRegistrar
{
    public static void RegisterIfNeeded(IServiceRegistrationContext context)
    {
        // Rule: Apply LoggingInterceptor to services implementing ILoggingEnabled
        if (typeof(ILoggingEnabled).IsAssignableFrom(context.ImplementationType))
        {
            context.Interceptors.TryAdd<LoggingInterceptor>();
        }
    }
}
```

## Creating Interceptors

### Basic Interceptor

```csharp
public class TimingInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await invocation.ProceedAsync();
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"{invocation.Method.Name} took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
```

### Using StandardInterceptAsync

For common before/after patterns, override the hook methods:

```csharp
public class AuditInterceptor : InterceptorBase
{
    private readonly IAuditService _auditService;

    public AuditInterceptor(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public override Task InterceptAsync(IMethodInvocation invocation)
    {
        return StandardInterceptAsync(invocation);
    }

    protected override async Task BeforeInvocationAsync(IMethodInvocation invocation)
    {
        await _auditService.LogEntryAsync(invocation.Method.Name, invocation.Arguments);
    }

    protected override async Task AfterInvocationAsync(IMethodInvocation invocation)
    {
        await _auditService.LogExitAsync(invocation.Method.Name, invocation.ReturnValue);
    }

    protected override async Task OnExceptionAsync(IMethodInvocation invocation, Exception ex)
    {
        await _auditService.LogErrorAsync(invocation.Method.Name, ex);
    }
}
```

### Interceptor with Dependencies

Interceptors support constructor injection:

```csharp
public class CachingInterceptor : InterceptorBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingInterceptor> _logger;

    public CachingInterceptor(IMemoryCache cache, ILogger<CachingInterceptor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        var cacheKey = GenerateCacheKey(invocation);

        if (_cache.TryGetValue(cacheKey, out var cachedValue))
        {
            _logger.LogDebug("Cache hit for {Key}", cacheKey);
            invocation.ReturnValue = cachedValue;
            return;
        }

        await invocation.ProceedAsync();

        _cache.Set(cacheKey, invocation.ReturnValue, TimeSpan.FromMinutes(5));
    }

    private string GenerateCacheKey(IMethodInvocation invocation)
    {
        var args = string.Join("_", invocation.Arguments.Select(a => a?.ToString() ?? "null"));
        return $"{invocation.Method.DeclaringType?.Name}_{invocation.Method.Name}_{args}";
    }
}
```

### Controlling Interceptor Order

Use `[InterceptorOrder]` to control execution order (lower values execute first):

```csharp
[InterceptorOrder(-100)]  // Executes first (outermost)
public class LoggingInterceptor : InterceptorBase { }

[InterceptorOrder(-50)]   // Executes second
public class TimingInterceptor : InterceptorBase { }

[InterceptorOrder(0)]     // Executes third (default)
public class ValidationInterceptor : InterceptorBase { }

[InterceptorOrder(100)]   // Executes last (innermost)
public class CachingInterceptor : InterceptorBase { }
```

Execution flow:

```
Request
   |
   v
LoggingInterceptor (enter)
   |
   v
TimingInterceptor (enter)
   |
   v
ValidationInterceptor (enter)
   |
   v
CachingInterceptor (enter)
   |
   v
[Target Method]
   |
   v
CachingInterceptor (exit)
   |
   v
ValidationInterceptor (exit)
   |
   v
TimingInterceptor (exit)
   |
   v
LoggingInterceptor (exit)
   |
   v
Response
```

## Registration Patterns

### Pattern 1: Marker Interface (Recommended)

```csharp
// Define marker interface
public interface ILoggingEnabled { }

// Implement in service
public class OrderService : IOrderService, ILoggingEnabled { }

// Create registrar
public static class LoggingInterceptorRegistrar
{
    public static void RegisterIfNeeded(IServiceRegistrationContext context)
    {
        if (typeof(ILoggingEnabled).IsAssignableFrom(context.ImplementationType))
        {
            context.Interceptors.TryAdd<LoggingInterceptor>();
        }
    }
}

// Configure
services.AddInterceptors(options =>
{
    options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
});
```

### Pattern 2: Attribute-Based

```csharp
// Apply attribute to class
[Intercept<LoggingInterceptor>]
[Intercept<ValidationInterceptor>]
public class OrderService : IOrderService { }

// Create registrar that checks for attribute
public static class AttributeInterceptorRegistrar
{
    public static void RegisterIfNeeded(IServiceRegistrationContext context)
    {
        var attributes = context.ImplementationType
            .GetCustomAttributes(typeof(InterceptAttribute<>), true);

        foreach (var attr in attributes)
        {
            var interceptorType = attr.GetType().GetGenericArguments()[0];
            context.Interceptors.TryAdd(interceptorType);
        }
    }
}
```

### Pattern 3: Direct Registration

For explicit control without conventions:

```csharp
services.AddIntercepted<IOrderService, OrderService>(interceptors =>
{
    interceptors.TryAdd<LoggingInterceptor>();
    interceptors.TryAdd<ValidationInterceptor>();
});
```

### Pattern 4: Multiple Registrars

Combine multiple conventions:

```csharp
services.AddInterceptors(options =>
{
    options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
    options.OnServiceRegistered(ValidationInterceptorRegistrar.RegisterIfNeeded);
    options.OnServiceRegistered(CachingInterceptorRegistrar.RegisterIfNeeded);
    options.OnServiceRegistered(AuditInterceptorRegistrar.RegisterIfNeeded);
});
```

## Excluding Types and Methods

### Exclude Entire Types

Use `ProxyIgnoreTypes` to exclude types from proxying:

```csharp
// In Program.cs or startup
ProxyIgnoreTypes.Add<MyInternalService>();
ProxyIgnoreTypes.Add(typeof(BackgroundService));
ProxyIgnoreTypes.Add(typeof(ControllerBase));

// Check in registrar
public static void RegisterIfNeeded(IServiceRegistrationContext context)
{
    if (ProxyIgnoreTypes.Contains(context.ImplementationType))
        return;

    // ... registration logic
}
```

### Exclude Specific Methods

Use `[DisableInterception]` on methods:

```csharp
public class OrderService : IOrderService, ILoggingEnabled
{
    // This method WILL be intercepted
    public virtual async Task<Order> CreateOrderAsync(string name, decimal total)
    {
        return new Order(name, total);
    }

    // This method will NOT be intercepted
    [DisableInterception]
    public virtual async Task<IReadOnlyList<Order>> GetAllOrdersAsync()
    {
        return _orders.AsReadOnly();
    }
}
```

### Exclude Specific Interceptors

Disable only certain interceptors:

```csharp
public class OrderService : IOrderService, ILoggingEnabled, IAuditEnabled
{
    // Disable only LoggingInterceptor, AuditInterceptor still runs
    [DisableInterception(typeof(LoggingInterceptor))]
    public virtual async Task<Order> GetOrderAsync(Guid id)
    {
        return await _repository.GetAsync(id);
    }
}
```

### Exclude Entire Class

Apply to class to disable all interception:

```csharp
[DisableInterception]
public class InternalService : IInternalService, ILoggingEnabled
{
    // No methods will be intercepted despite ILoggingEnabled
    public virtual Task DoWorkAsync() { }
}
```

### Respecting Exclusions in Interceptors

Call `ShouldIntercept` in your interceptor:

```csharp
public class LoggingInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        if (!ShouldIntercept(invocation))
        {
            await invocation.ProceedAsync();
            return;
        }

        // Interception logic here
        Console.WriteLine($"Entering {invocation.Method.Name}");
        await invocation.ProceedAsync();
        Console.WriteLine($"Exited {invocation.Method.Name}");
    }
}
```

## Advanced Usage

### Modifying Arguments

```csharp
public class ArgumentSanitizerInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        // Sanitize string arguments
        for (int i = 0; i < invocation.Arguments.Length; i++)
        {
            if (invocation.Arguments[i] is string str)
            {
                invocation.SetArgument(i, str.Trim());
            }
        }

        await invocation.ProceedAsync();
    }
}
```

### Modifying Return Values

```csharp
public class ResponseWrapperInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        await invocation.ProceedAsync();

        // Wrap the return value
        if (invocation.ReturnValue is not null)
        {
            invocation.ReturnValue = new ApiResponse
            {
                Data = invocation.ReturnValue,
                Timestamp = DateTime.UtcNow,
                Success = true
            };
        }
    }
}
```

### Short-Circuiting Execution

```csharp
public class AuthorizationInterceptor : InterceptorBase
{
    private readonly IAuthorizationService _authService;

    public AuthorizationInterceptor(IAuthorizationService authService)
    {
        _authService = authService;
    }

    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        var requiredPermission = invocation.Method
            .GetCustomAttribute<RequirePermissionAttribute>()?.Permission;

        if (requiredPermission is not null)
        {
            var isAuthorized = await _authService.HasPermissionAsync(requiredPermission);

            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException(
                    $"Permission '{requiredPermission}' is required");
            }
        }

        await invocation.ProceedAsync();
    }
}
```

### Retry Pattern

```csharp
public class RetryInterceptor : InterceptorBase
{
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                attempts++;
                await invocation.ProceedAsync();
                return;
            }
            catch (Exception ex) when (attempts < _maxRetries && IsTransient(ex))
            {
                await Task.Delay(_delay * attempts);
            }
        }
    }

    private bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException or TimeoutException;
    }
}
```

### Working with Generic Methods

```csharp
public class GenericMethodInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        if (invocation.GenericArguments is { Length: > 0 })
        {
            var genericTypes = string.Join(", ",
                invocation.GenericArguments.Select(t => t.Name));
            Console.WriteLine($"Generic method with types: {genericTypes}");
        }

        await invocation.ProceedAsync();
    }
}
```

### Accessing Arguments by Name

```csharp
public class ValidationInterceptor : InterceptorBase
{
    public override async Task InterceptAsync(IMethodInvocation invocation)
    {
        // Access by index
        var firstArg = invocation.GetArgument<string>(0);

        // Access by name
        var customerId = invocation.GetArgument<Guid>("customerId");

        // Access via dictionary
        if (invocation.ArgumentsDictionary.TryGetValue("email", out var email))
        {
            ValidateEmail((string?)email);
        }

        await invocation.ProceedAsync();
    }
}
```

## API Reference

### ServiceCollectionExtensions

| Method | Description |
|--------|-------------|
| `AddInterceptors(Action<InterceptorOptions>?)` | Configures the interceptor infrastructure |
| `AddInterceptor<T>(ServiceLifetime)` | Registers an interceptor type |
| `AddIntercepted<TService, TImpl>(Action<IInterceptorList>)` | Registers a service with explicit interceptors |
| `ApplyInterceptorConventions()` | Processes all registrations and applies interceptors |
| `BuildServiceProviderWithInterceptors()` | Builds provider with interception (alternative to ApplyInterceptorConventions) |

### InterceptorOptions

| Method | Description |
|--------|-------------|
| `OnServiceRegistered(Action<IServiceRegistrationContext>)` | Adds a registration callback |
| `AddRegistrar<T>()` | Adds an `IInterceptorRegistrar` instance |

### IServiceRegistrationContext

| Property | Description |
|----------|-------------|
| `ServiceType` | The service type being registered |
| `ImplementationType` | The implementation type |
| `Lifetime` | The service lifetime |
| `Interceptors` | The interceptor list to modify |

### IInterceptorList

| Method | Description |
|--------|-------------|
| `TryAdd<T>()` | Adds an interceptor if not present |
| `TryAdd<T>(int order)` | Adds with explicit order |
| `Remove<T>()` | Removes an interceptor |
| `Contains<T>()` | Checks if interceptor is registered |

### ProxyIgnoreTypes

| Method | Description |
|--------|-------------|
| `Add<T>()` | Adds a type to ignore |
| `Add(params Type[])` | Adds multiple types |
| `Contains(Type, bool)` | Checks if type is ignored |
| `Remove(Type)` | Removes a type |
| `Clear()` | Clears all ignored types |

### ProxyHelper

| Method | Description |
|--------|-------------|
| `IsProxy(object)` | Checks if object is a proxy |
| `UnProxy(object)` | Gets the underlying target |
| `GetUnProxiedType(object)` | Gets the actual type |
| `CanBeProxied(Type)` | Checks if type can be proxied |

## Architecture

```
                                    DotnetInterceptors
    +------------------------------------------------------------------------+
    |                                                                        |
    |   +------------------------+     +---------------------------+         |
    |   |    IInterceptor        |     |   IMethodInvocation       |         |
    |   |  InterceptAsync()      |     |   Arguments, Method,      |         |
    |   +------------------------+     |   ProceedAsync()          |         |
    |              ^                   +---------------------------+         |
    |              |                              ^                          |
    |   +------------------------+               |                          |
    |   |   InterceptorBase      |    +---------------------------+         |
    |   |   ShouldIntercept()    |    |   MethodInvocation        |         |
    |   |   StandardIntercept()  |    |   (wraps Castle)          |         |
    |   +------------------------+    +---------------------------+         |
    |              ^                              ^                          |
    |              |                              |                          |
    |   +------------------------+    +---------------------------+         |
    |   |  Your Interceptors     |    | CastleInterceptorAdapter  |         |
    |   |  (LoggingInterceptor)  |    | (AsyncInterceptorBase)    |         |
    |   +------------------------+    +---------------------------+         |
    |                                             |                          |
    +---------------------------------------------|---------------------------+
                                                  |
                                                  v
    +------------------------------------------------------------------------+
    |                        Castle.DynamicProxy                             |
    |                                                                        |
    |   ProxyGenerator  -->  IInvocation  -->  Target Method                 |
    +------------------------------------------------------------------------+
```

### Registration Flow

```
1. AddInterceptors()
   - Creates InterceptorOptions
   - Registers ProxyFactory
   - Stores registrar callbacks

2. AddInterceptor<T>()
   - Registers interceptor in DI

3. AddScoped<IService, Impl>()
   - Normal service registration

4. ApplyInterceptorConventions()
   - Iterates all ServiceDescriptors
   - Runs registrars for each
   - Replaces descriptors with proxy factories
```

### Runtime Flow

```
1. Service resolved from DI
   - ProxyFactory creates Castle proxy
   - Interceptors resolved from DI
   - CastleInterceptorAdapter wraps each

2. Method called on proxy
   - Castle creates IInvocation
   - Passes to CastleInterceptorAdapter

3. Adapter bridges to our IInterceptor
   - Creates MethodInvocation wrapper
   - Calls InterceptAsync()

4. Interceptor executes
   - Runs before logic
   - Calls ProceedAsync()
   - Runs after logic

5. ProceedAsync() flows to target
   - Or next interceptor in chain
```

## Restrictions and Important Notes

### Always Use Asynchronous Methods

For best performance and reliability, implement your service methods as asynchronous. Async-over-sync patterns can cause thread pool starvation and deadlocks. DotnetInterceptors is designed async-first, so synchronous methods work but async is recommended.

### Virtual Methods Requirement

For class-based proxies, methods must be marked as `virtual` to be intercepted. Non-virtual methods cannot be overridden by the proxy and will not trigger interception.

```csharp
public class MyService : ILoggingEnabled
{
    // This method CANNOT be intercepted (not virtual)
    public void CannotBeIntercepted() { }

    // This method CAN be intercepted (virtual)
    public virtual void CanBeIntercepted() { }
}
```

This restriction does not apply to interface-based proxies. If your service implements an interface and is injected via that interface, all methods can be intercepted regardless of the virtual keyword.

### Dependency Injection Scope

Interceptors only work when services are resolved from the dependency injection container. Direct instantiation with `new` bypasses interception entirely.

```csharp
// This will NOT be intercepted
var service = new MyService();
service.DoWork();

// This WILL be intercepted (resolved from DI)
var service = serviceProvider.GetRequiredService<IMyService>();
service.DoWork();
```

### Self-Calls Are Not Intercepted

When a method calls another method on the same instance, the internal call is not intercepted because it bypasses the proxy.

```csharp
public class OrderService : IOrderService, ILoggingEnabled
{
    public virtual async Task ProcessOrderAsync(Order order)
    {
        // This call to ValidateOrder is NOT intercepted
        // because it's a direct call within the same instance
        ValidateOrder(order);

        await SaveOrderAsync(order);
    }

    public virtual void ValidateOrder(Order order) { }
}
```

## Performance Considerations

### Interceptor Overhead

Each interceptor adds method-call overhead. While generally efficient, consider the following:

- Keep the number of interceptors minimal on hot paths (frequently called methods)
- Avoid expensive operations in interceptors that run on every call
- Use `ShouldIntercept` to skip interception when not needed

### Types to Exclude from Proxying

Castle DynamicProxy can negatively impact performance for certain components, notably ASP.NET Core MVC controllers and Razor components.

Use `ProxyIgnoreTypes` to exclude types that should not be proxied:

```csharp
// Exclude base types - all derived types are also excluded
ProxyIgnoreTypes.Add(typeof(ControllerBase));    // MVC Controllers
ProxyIgnoreTypes.Add(typeof(PageModel));         // Razor Pages
ProxyIgnoreTypes.Add(typeof(ViewComponent));     // View Components
ProxyIgnoreTypes.Add(typeof(BackgroundService)); // Background services
```

### Prefer Interface-Based Proxies

Interface-based proxies perform better than class-based proxies:

- **Interface proxy**: Service registered as `IService` -> `Service`
- **Class proxy**: Service registered as `Service` -> `Service`

When possible, always inject services via their interfaces rather than concrete types.

### Caching Considerations

The `ProxyGenerator` is reused as a singleton to leverage Castle's internal type cache. Creating multiple `ProxyGenerator` instances defeats this caching and causes high CPU usage and memory pressure.

DotnetInterceptors handles this automatically through `ProxyFactory`.

## Testing Interceptors

### Unit Testing Interceptors

Test your interceptors in isolation by mocking `IMethodInvocation`:

```csharp
[Fact]
public async Task LoggingInterceptor_ShouldLogMethodEntry()
{
    // Arrange
    var loggerMock = new Mock<ILogger<LoggingInterceptor>>();
    var interceptor = new LoggingInterceptor(loggerMock.Object);

    var invocationMock = new Mock<IMethodInvocation>();
    invocationMock.Setup(i => i.Method).Returns(typeof(IOrderService).GetMethod("CreateOrderAsync")!);
    invocationMock.Setup(i => i.TargetObject).Returns(new OrderService());
    invocationMock.Setup(i => i.ProceedAsync()).Returns(Task.CompletedTask);

    // Act
    await interceptor.InterceptAsync(invocationMock.Object);

    // Assert
    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Entering")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Integration Testing with Real Proxies

Test the full interception pipeline:

```csharp
[Fact]
public async Task OrderService_ShouldBeIntercepted()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddInterceptors(options =>
    {
        options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
    });

    services.AddInterceptor<LoggingInterceptor>();
    services.AddScoped<IOrderService, OrderService>();
    services.ApplyInterceptorConventions();

    var provider = services.BuildServiceProvider();

    // Act
    var orderService = provider.GetRequiredService<IOrderService>();
    var result = await orderService.CreateOrderAsync("Test", 100);

    // Assert
    Assert.NotNull(result);
    Assert.True(ProxyHelper.IsProxy(orderService));
}
```

### Testing That Exclusions Work

```csharp
[Fact]
public async Task DisabledMethod_ShouldNotBeIntercepted()
{
    // Arrange
    var callCount = 0;
    var interceptor = new TestInterceptor(() => callCount++);

    var invocationMock = new Mock<IMethodInvocation>();
    invocationMock.Setup(i => i.Method)
        .Returns(typeof(OrderService).GetMethod("GetAllOrdersAsync")!);
    invocationMock.Setup(i => i.TargetObject).Returns(new OrderService());
    invocationMock.Setup(i => i.ProceedAsync()).Returns(Task.CompletedTask);

    // Assume GetAllOrdersAsync has [DisableInterception]
    // Act
    await interceptor.InterceptAsync(invocationMock.Object);

    // Assert - interceptor logic should be skipped
    Assert.Equal(0, callCount);
}
```

## Troubleshooting

### Common Issues

#### "Method is not being intercepted"

**Possible causes:**

1. **Method is not virtual** (for class-based proxies)
   ```csharp
   // Wrong - won't be intercepted
   public void MyMethod() { }

   // Correct
   public virtual void MyMethod() { }
   ```

2. **Service not resolved from DI**
   ```csharp
   // Wrong - direct instantiation bypasses proxy
   var service = new MyService();

   // Correct - resolve from DI
   var service = provider.GetRequiredService<IMyService>();
   ```

3. **ApplyInterceptorConventions not called**
   ```csharp
   services.AddInterceptors(...);
   services.AddScoped<IService, Service>();
   services.ApplyInterceptorConventions(); // Don't forget this!
   ```

4. **Registrar not matching the type**
   - Check that your marker interface is implemented
   - Check that the registrar logic matches your service

#### "Castle.DynamicProxy exception on startup"

**Possible causes:**

1. **Sealed class** - Cannot proxy sealed classes
2. **Internal class without InternalsVisibleTo** - Add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]`
3. **Type already has a proxy** - Don't register the same type twice with different configurations

#### "Interceptor dependencies not resolved"

Ensure interceptors are registered in DI:

```csharp
// Register the interceptor type
services.AddInterceptor<LoggingInterceptor>();

// Or manually with specific lifetime
services.AddScoped<LoggingInterceptor>();
```

#### "Interception works in some methods but not others"

Check for `[DisableInterception]` attribute on the method or class, or verify that `ShouldIntercept` returns true for those methods.

#### "Self-calls are not intercepted"

This is by design. Internal method calls bypass the proxy:

```csharp
public class MyService : IMyService
{
    public virtual void MethodA()
    {
        MethodB(); // This call is NOT intercepted
    }

    public virtual void MethodB() { }
}
```

**Workaround**: Inject the service into itself (lazy) or split into separate services.

### Debugging Tips

1. **Check if object is a proxy**
   ```csharp
   var isProxy = ProxyHelper.IsProxy(myService);
   Console.WriteLine($"Is proxy: {isProxy}");
   ```

2. **Get the underlying type**
   ```csharp
   var actualType = ProxyHelper.GetUnProxiedType(myService);
   Console.WriteLine($"Actual type: {actualType.Name}");
   ```

3. **Log in registrars**
   ```csharp
   public static void RegisterIfNeeded(IServiceRegistrationContext context)
   {
       Console.WriteLine($"Evaluating: {context.ImplementationType.Name}");
       // ...
   }
   ```

## Best Practices

### Do

- **Keep interceptors focused** - Each interceptor should handle one concern (Single Responsibility Principle)
- **Use marker interfaces** - They make intent explicit and are easily searchable
- **Check ShouldIntercept** - Respect exclusion attributes in your interceptors
- **Use async throughout** - Keep the entire call chain asynchronous
- **Register interceptors with appropriate lifetime** - Match the lifetime of dependencies they use
- **Test interceptors in isolation** - Mock `IMethodInvocation` for unit tests

### Don't

- **Don't use interceptors for simple method calls** - Overhead may not be justified
- **Don't modify state unexpectedly** - Avoid side effects that surprise callers
- **Don't swallow exceptions silently** - Log and rethrow, or handle explicitly
- **Don't create heavy objects in interceptors** - Use DI to inject expensive dependencies
- **Don't intercept everything** - Use ProxyIgnoreTypes for controllers, view components, etc.
- **Don't forget virtual on class methods** - Interface-based proxies don't need it, but class proxies do

### Interceptor Ordering Guidelines

| Order Range | Purpose | Examples |
|-------------|---------|----------|
| -100 to -50 | Outer concerns (logging, metrics) | LoggingInterceptor, MetricsInterceptor |
| -50 to 0 | Security and validation | AuthorizationInterceptor, ValidationInterceptor |
| 0 to 50 | Business concerns | AuditInterceptor, FeatureCheckInterceptor |
| 50 to 100 | Data concerns | CachingInterceptor, TransactionInterceptor |

### Recommended Project Structure

```
YourProject/
├── Interceptors/
│   ├── Logging/
│   │   ├── ILoggingEnabled.cs
│   │   ├── LoggingInterceptor.cs
│   │   └── LoggingInterceptorRegistrar.cs
│   ├── Validation/
│   │   ├── IValidationEnabled.cs
│   │   ├── ValidationInterceptor.cs
│   │   └── ValidationInterceptorRegistrar.cs
│   └── InterceptorConfiguration.cs
├── Services/
│   └── ...
└── Program.cs
```

### Extension Method for Clean Configuration

```csharp
public static class InterceptorConfiguration
{
    public static IServiceCollection AddApplicationInterceptors(
        this IServiceCollection services)
    {
        ProxyIgnoreTypes.Add(typeof(ControllerBase));
        ProxyIgnoreTypes.Add(typeof(BackgroundService));

        services.AddInterceptors(options =>
        {
            options.OnServiceRegistered(LoggingInterceptorRegistrar.RegisterIfNeeded);
            options.OnServiceRegistered(ValidationInterceptorRegistrar.RegisterIfNeeded);
            options.OnServiceRegistered(AuditInterceptorRegistrar.RegisterIfNeeded);
        });

        services.AddInterceptor<LoggingInterceptor>();
        services.AddInterceptor<ValidationInterceptor>();
        services.AddInterceptor<AuditInterceptor>();

        return services;
    }
}

// Usage in Program.cs
builder.Services.AddApplicationInterceptors();
// ... register services ...
builder.Services.ApplyInterceptorConventions();
```

## API Reference

### ServiceCollectionExtensions

| Method | Description |
|--------|-------------|
| `AddInterceptors(Action<InterceptorOptions>?)` | Configures the interceptor infrastructure |
| `AddInterceptor<T>(ServiceLifetime)` | Registers an interceptor type |
| `AddIntercepted<TService, TImpl>(Action<IInterceptorList>)` | Registers a service with explicit interceptors |
| `ApplyInterceptorConventions()` | Processes all registrations and applies interceptors |
| `BuildServiceProviderWithInterceptors()` | Builds provider with interception (alternative to ApplyInterceptorConventions) |

### InterceptorOptions

| Method | Description |
|--------|-------------|
| `OnServiceRegistered(Action<IServiceRegistrationContext>)` | Adds a registration callback |
| `AddRegistrar<T>()` | Adds an `IInterceptorRegistrar` instance |

### IServiceRegistrationContext

| Property | Description |
|----------|-------------|
| `ServiceType` | The service type being registered |
| `ImplementationType` | The implementation type |
| `Lifetime` | The service lifetime |
| `Interceptors` | The interceptor list to modify |

### IInterceptorList

| Method | Description |
|--------|-------------|
| `TryAdd<T>()` | Adds an interceptor if not present |
| `TryAdd<T>(int order)` | Adds with explicit order |
| `Remove<T>()` | Removes an interceptor |
| `Contains<T>()` | Checks if interceptor is registered |

### ProxyIgnoreTypes

| Method | Description |
|--------|-------------|
| `Add<T>()` | Adds a type to ignore |
| `Add(params Type[])` | Adds multiple types |
| `Contains(Type, bool)` | Checks if type is ignored |
| `Remove(Type)` | Removes a type |
| `Clear()` | Clears all ignored types |

### ProxyHelper

| Method | Description |
|--------|-------------|
| `IsProxy(object)` | Checks if object is a proxy |
| `UnProxy(object)` | Gets the underlying target |
| `GetUnProxiedType(object)` | Gets the actual type |
| `CanBeProxied(Type)` | Checks if type can be proxied |

## Architecture

```
                                    DotnetInterceptors
    +------------------------------------------------------------------------+
    |                                                                        |
    |   +------------------------+     +---------------------------+         |
    |   |    IInterceptor        |     |   IMethodInvocation       |         |
    |   |  InterceptAsync()      |     |   Arguments, Method,      |         |
    |   +------------------------+     |   ProceedAsync()          |         |
    |              ^                   +---------------------------+         |
    |              |                              ^                          |
    |   +------------------------+               |                          |
    |   |   InterceptorBase      |    +---------------------------+         |
    |   |   ShouldIntercept()    |    |   MethodInvocation        |         |
    |   |   StandardIntercept()  |    |   (wraps Castle)          |         |
    |   +------------------------+    +---------------------------+         |
    |              ^                              ^                          |
    |              |                              |                          |
    |   +------------------------+    +---------------------------+         |
    |   |  Your Interceptors     |    | CastleInterceptorAdapter  |         |
    |   |  (LoggingInterceptor)  |    | (AsyncInterceptorBase)    |         |
    |   +------------------------+    +---------------------------+         |
    |                                             |                          |
    +---------------------------------------------|---------------------------+
                                                  |
                                                  v
    +------------------------------------------------------------------------+
    |                        Castle.DynamicProxy                             |
    |                                                                        |
    |   ProxyGenerator  -->  IInvocation  -->  Target Method                 |
    +------------------------------------------------------------------------+
```

### Registration Flow

```
1. AddInterceptors()
   - Creates InterceptorOptions
   - Registers ProxyFactory
   - Stores registrar callbacks

2. AddInterceptor<T>()
   - Registers interceptor in DI

3. AddScoped<IService, Impl>()
   - Normal service registration

4. ApplyInterceptorConventions()
   - Iterates all ServiceDescriptors
   - Runs registrars for each
   - Replaces descriptors with proxy factories
```

### Runtime Flow

```
1. Service resolved from DI
   - ProxyFactory creates Castle proxy
   - Interceptors resolved from DI
   - CastleInterceptorAdapter wraps each

2. Method called on proxy
   - Castle creates IInvocation
   - Passes to CastleInterceptorAdapter

3. Adapter bridges to our IInterceptor
   - Creates MethodInvocation wrapper
   - Calls InterceptAsync()

4. Interceptor executes
   - Runs before logic
   - Calls ProceedAsync()
   - Runs after logic

5. ProceedAsync() flows to target
   - Or next interceptor in chain
```

## License

MIT License
