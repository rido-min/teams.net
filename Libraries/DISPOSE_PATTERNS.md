# Dispose Pattern Analysis and Guidelines

This document summarizes the Dispose pattern review conducted across the Teams.NET libraries and provides guidelines for proper resource management.

## Summary of Findings

### Classes Implementing IDisposable

| Class | Location | Status | Description |
|-------|----------|--------|-------------|
| `HttpClient` | Microsoft.Teams.Common | ✅ Fixed | Wraps `System.Net.Http.HttpClient` |
| `TeamsLogger` | Microsoft.Teams.Extensions.Logging | ✅ Fixed | Wraps `Common.Logging.ILogger` |
| `TeamsLoggerProvider` | Microsoft.Teams.Extensions.Logging | ✅ Fixed | Provides `ILoggerProvider` implementation |
| `Stream` | Microsoft.Teams.Plugins.AspNetCore | ✅ Fixed | Added IDisposable for Timer/SemaphoreSlim |

### Resource Leak Issues Fixed

| Issue | Location | Fix Applied |
|-------|----------|-------------|
| `CancellationTokenSource` leak | ActionExtensions.cs | Properly dispose previous source before creating new |
| `CancellationTokenSource` leak | TeamsPluginService.cs | Use `using` statement |
| `CancellationTokenSource` leak | TeamsService.cs | Use `using` statement |
| `Timer` not disposed | AspNetCorePlugin.Stream.cs | Added IDisposable implementation |
| `SemaphoreSlim` not disposed | AspNetCorePlugin.Stream.cs | Added IDisposable implementation |

### Intentional Long-Lived Resources

| Resource | Location | Justification |
|----------|----------|---------------|
| `HttpClient` in `ConfigurationManager` | TokenValidator.cs | Statically cached per OpenID metadata URL; intended for application lifetime |

## Dispose Pattern Guidelines

### Standard Dispose Pattern

All classes that hold disposable resources should implement the standard dispose pattern:

```csharp
public class MyDisposableClass : IDisposable
{
    private bool _disposed;
    private readonly SomeDisposableResource _resource;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Dispose managed resources
            _resource?.Dispose();
        }

        _disposed = true;
    }
}
```

### Key Principles

1. **Always call `GC.SuppressFinalize(this)`** in the public `Dispose()` method to prevent unnecessary finalization.

2. **Use the `_disposed` flag** to prevent double disposal and ensure idempotent dispose calls.

3. **Implement `protected virtual void Dispose(bool disposing)`** to allow derived classes to extend disposal logic.

4. **Use `using` statements** for short-lived disposable objects:
   ```csharp
   using var cts = new CancellationTokenSource();
   ```

5. **Dispose replaced resources** when replacing disposable fields:
   ```csharp
   var previous = _field;
   previous?.Dispose();
   _field = new Resource();
   ```

### Common Disposable Types to Watch For

- `HttpClient` / `HttpMessageHandler`
- `CancellationTokenSource`
- `Timer`
- `SemaphoreSlim`
- `Stream` (all types)
- `IDisposable` interface implementations

### Testing Dispose

When testing dispose patterns:

```csharp
[Fact]
public void ShouldDisposeWithoutException()
{
    var obj = new MyDisposableClass();
    obj.Dispose();
    // No exception should be thrown
}

[Fact]
public void ShouldHandleDoubleDispose()
{
    var obj = new MyDisposableClass();
    obj.Dispose();
    obj.Dispose(); // Should not throw
}
```

## Changes Made

### 1. HttpClient (Microsoft.Teams.Common)

- Added standard dispose pattern with `GC.SuppressFinalize`
- Added `protected virtual void Dispose(bool disposing)` for inheritance support

### 2. TeamsLogger (Microsoft.Teams.Extensions.Logging)

- Replaced empty `Dispose()` with proper implementation
- Added disposed check to prevent double disposal
- Disposes underlying logger if it implements `IDisposable`

### 3. TeamsLoggerProvider (Microsoft.Teams.Extensions.Logging)

- Added standard dispose pattern with `GC.SuppressFinalize`
- Added `_disposed` flag

### 4. Stream (Microsoft.Teams.Plugins.AspNetCore)

- **NEW**: Added `IDisposable` implementation
- Properly disposes `Timer` and `SemaphoreSlim` resources
- Documented the disposal behavior

### 5. ActionExtensions (Microsoft.Teams.Common)

- Fixed `CancellationTokenSource` leak in `Debounce` methods
- Previous source is now disposed before creating a new one

### 6. TeamsPluginService & TeamsService

- Fixed `CancellationTokenSource` leak using `using` statement

## Future Considerations

1. **Async Dispose**: Consider implementing `IAsyncDisposable` for classes with async cleanup needs.

2. **Dependency Injection**: When classes are managed by DI containers, ensure proper lifetime configuration.

3. **HttpClient Factory**: For `HttpClient` usage, prefer `IHttpClientFactory` pattern for better connection management.
