// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps;

public partial class App
{
    protected IList<IPlugin> Plugins { get; set; }

    // Helper method to parse event names back to EventType enum
    private static bool TryParseEventType(string name, out EventType eventType)
    {
        try
        {
            // Try to deserialize the string as an EventType using JSON
            var json = System.Text.Json.JsonSerializer.Serialize(name);
            eventType = System.Text.Json.JsonSerializer.Deserialize<EventType>($"\"{name}\"");
            return true;
        }
        catch
        {
            eventType = default;
            return false;
        }
    }

    public IPlugin? GetPlugin(string name)
    {
        return Plugins.SingleOrDefault(p => PluginService.GetAttribute(p).Name == name);
    }

    public IPlugin? GetPlugin(Type type)
    {
        return Plugins.SingleOrDefault(p => p.GetType() == type);
    }

    public TPlugin? GetPlugin<TPlugin>() where TPlugin : IPlugin
    {
        return (TPlugin?)Plugins.SingleOrDefault(p => p.GetType() == typeof(TPlugin));
    }

    public App AddPlugin(IPlugin plugin)
    {
        var attr = PluginService.GetAttribute(plugin);

        // broadcast plugin events
        plugin.Events += async (plugin, name, @event, token) =>
        {
            await Events.Emit(plugin, $"{attr.Name}.{name}", @event, token);

            // Try to parse the event name as a built-in EventType
            if (TryParseEventType(name, out var eventType) && eventType.IsBuiltIn() && eventType != EventType.Start)
            {
                return await Events.Emit(plugin, name, @event, token);
            }

            return null;
        };

        Plugins.Add(plugin);
        Container.Register(attr.Name, new ValueProvider(plugin));
        Container.Register(plugin.GetType().Name, new ValueProvider(plugin));
        Logger.Debug($"plugin {attr.Name} registered");
        return this;
    }

    protected void Inject(IPlugin plugin)
    {
        var assembly = Assembly.GetAssembly(plugin.GetType());
        var metadata = PluginService.GetAttribute(plugin);
        var properties = plugin
            .GetType()
            .GetProperties()
            .Where(property => property.IsDefined(typeof(DependencyAttribute), true));

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<DependencyAttribute>();

            if (attribute is null) continue;

            var dependency = Container.Resolve<object>(attribute.Name ?? property.PropertyType.Name);

            if (dependency is null)
            {
                dependency = Container.Resolve<object>(property.Name);
            }

            if (dependency is null)
            {
                if (attribute.Optional) continue;
                throw new InvalidOperationException($"dependency '{property.PropertyType.Name}' of property '{property.Name}' not found, but plugin '{metadata.Name}' depends on it");
            }

            if (dependency is ILogger logger)
            {
                dependency = logger.Child(metadata.Name);
            }

            property.SetValue(plugin, dependency);
        }
    }
}