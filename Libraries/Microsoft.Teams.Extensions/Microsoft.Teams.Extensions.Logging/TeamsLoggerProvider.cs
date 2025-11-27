// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Extensions.Logging;

[ProviderAlias("Microsoft.Teams")]
public class TeamsLoggerProvider : ILoggerProvider, IDisposable
{
    protected TeamsLogger _logger;

    public TeamsLoggerProvider(Common.Logging.ILogger logger)
    {
        _logger = new TeamsLogger(logger);
    }

    public TeamsLoggerProvider(TeamsLogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public ILogger CreateLogger<T>()
    {
        var name = typeof(T).Name;
        return _logger.Create(name);
    }

    public ILogger CreateLogger(string name)
    {
        return _logger.Create(name);
    }

    private bool _disposed;

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
            _logger.Dispose();
        }

        _disposed = true;
    }
}