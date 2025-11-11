// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Apps.Events;

[JsonConverter(typeof(StringEnumConverter<EventType>))]
public enum EventType
{
    Start,
    Error,
    SignIn,
    Activity,
    [EnumStringValue("activity.sent")]
    ActivitySent,
    [EnumStringValue("activity.response")]
    ActivityResponse
}

public static class EventTypeExtensions
{
    public static bool IsBuiltIn(this EventType eventType)
    {
        return eventType == EventType.Start 
            || eventType == EventType.Error 
            || eventType == EventType.SignIn 
            || eventType == EventType.Activity 
            || eventType == EventType.ActivitySent 
            || eventType == EventType.ActivityResponse;
    }
}