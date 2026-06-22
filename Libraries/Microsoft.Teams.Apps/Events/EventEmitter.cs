// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

internal class EventEmitter
{
    protected Dictionary<string, Topic> Topics { get; set; } = [];

    // Helper method to convert EventType enum to string using JSON serialization
    private static string GetEventName(EventType eventType)
    {
        return JsonSerializer.Serialize(eventType).Trim('"');
    }

    public EventEmitter On(EventType eventType, Action<IPlugin, Event> handler)
    {
        return On(GetEventName(eventType), handler);
    }

    public EventEmitter On<TResult>(EventType eventType, Func<IPlugin, Event, TResult> handler)
    {
        return On(GetEventName(eventType), handler);
    }

    public EventEmitter On(EventType eventType, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        return On(GetEventName(eventType), handler);
    }

    public EventEmitter On<TResult>(EventType eventType, Func<IPlugin, Event, CancellationToken, Task<TResult>> handler)
    {
        return On(GetEventName(eventType), handler);
    }

    public Task<object?> Emit(IPlugin plugin, EventType eventType, Event? @event = null, CancellationToken cancellationToken = default)
    {
        return Emit(plugin, GetEventName(eventType), @event, cancellationToken);
    }

    public EventEmitter On(string name, Action<IPlugin, Event> handler)
    {
        if (!Topics.ContainsKey(name))
        {
            Topics[name] = [];
        }

        Topics[name].Add((plugin, @event, cancellationToken) =>
        {
            handler(plugin, @event);
            return Task.FromResult<object?>(null);
        });

        return this;
    }

    public EventEmitter On<TResult>(string name, Func<IPlugin, Event, TResult> handler)
    {
        if (!Topics.ContainsKey(name))
        {
            Topics[name] = [];
        }

        Topics[name].Add((plugin, @event, cancellationToken) =>
        {
            var res = handler(plugin, @event);
            return Task.FromResult<object?>(res);
        });

        return this;
    }

    public EventEmitter On(string name, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        if (!Topics.ContainsKey(name))
        {
            Topics[name] = [];
        }

        Topics[name].Add(async (plugin, @event, cancellationToken) =>
        {
            await handler(plugin, @event, cancellationToken);
            return null;
        });

        return this;
    }

    public EventEmitter On<TResult>(string name, Func<IPlugin, Event, CancellationToken, Task<TResult>> handler)
    {
        if (!Topics.ContainsKey(name))
        {
            Topics[name] = [];
        }

        Topics[name].Add(async (plugin, @event, cancellationToken) =>
        {
            var res = await handler(plugin, @event, cancellationToken);
            return res;
        });

        return this;
    }

    public async Task<object?> Emit(IPlugin plugin, string name, Event? @event = null, CancellationToken cancellationToken = default)
    {
        if (!Topics.ContainsKey(name))
        {
            Topics[name] = [];
        }

        return await Topics[name].Emit(plugin, @event, cancellationToken);
    }
}