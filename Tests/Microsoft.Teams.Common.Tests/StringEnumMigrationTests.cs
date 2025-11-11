// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Common.Tests;

/// <summary>
/// Tests demonstrating the migration from StringEnum to standard C# enums with CamelCaseEnumConverter.
/// This pattern works across all target frameworks (netstandard2.0/2.1, net8.0, net9.0).
/// </summary>
public class StringEnumMigrationTests
{
    // Example of the new pattern: standard enum with CamelCaseEnumConverter
    [JsonConverter(typeof(CamelCaseEnumConverter<DeliveryModeEnum>))]
    public enum DeliveryModeEnum
    {
        Normal,
        Notification,
        ExpectReplies,
        Ephemeral
    }

    // Example of an enum that uses default naming (PascalCase to camelCase)
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SimpleEventTypeEnum
    {
        Start,
        Error,
        SignIn,
        Activity
    }

    [Fact]
    public void JsonSerialize_WithCamelCaseNaming()
    {
        var value = DeliveryModeEnum.Normal;
        var json = JsonSerializer.Serialize(value);
        
        // With the CamelCaseEnumConverter, enums are serialized to camelCase automatically
        Assert.Equal("\"normal\"", json);
    }

    [Fact]
    public void JsonDeserialize_WithCamelCaseNaming()
    {
        var json = "\"notification\"";
        var value = JsonSerializer.Deserialize<DeliveryModeEnum>(json);
        
        Assert.Equal(DeliveryModeEnum.Notification, value);
    }

    [Fact]
    public void JsonSerialize_InObject()
    {
        var obj = new Dictionary<string, object>()
        {
            { "mode", DeliveryModeEnum.ExpectReplies }
        };

        var json = JsonSerializer.Serialize(obj);
        Assert.Equal("{\"mode\":\"expectReplies\"}", json);
    }

    [Fact]
    public void JsonSerialize_WithDefaultNaming()
    {
        // When using default JsonStringEnumConverter naming policy
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        
        var value = SimpleEventTypeEnum.SignIn;
        var json = JsonSerializer.Serialize(value, options);
        
        // With camelCase naming policy, "SignIn" becomes "signIn"
        Assert.Equal("\"signIn\"", json);
    }

    [Fact]
    public void EnumComparison_Works()
    {
        var mode1 = DeliveryModeEnum.Normal;
        var mode2 = DeliveryModeEnum.Normal;
        var mode3 = DeliveryModeEnum.Notification;
        
        Assert.Equal(mode1, mode2);
        Assert.NotEqual(mode1, mode3);
        Assert.True(mode1 == mode2);
        Assert.True(mode1 != mode3);
    }

    [Fact]
    public void EnumToString_ReturnsEnumName()
    {
        var mode = DeliveryModeEnum.Normal;
        
        // ToString() returns the enum member name, not the JSON value
        Assert.Equal("Normal", mode.ToString());
    }

    [Fact]
    public void EnumParsing_FromString()
    {
        var mode = Enum.Parse<DeliveryModeEnum>("Normal");
        Assert.Equal(DeliveryModeEnum.Normal, mode);
        
        var success = Enum.TryParse<DeliveryModeEnum>("Notification", out var result);
        Assert.True(success);
        Assert.Equal(DeliveryModeEnum.Notification, result);
    }

    [Fact]
    public void EnumValues_CanBeEnumerated()
    {
        var values = Enum.GetValues<DeliveryModeEnum>();
        
        Assert.Equal(4, values.Length);
        Assert.Contains(DeliveryModeEnum.Normal, values);
        Assert.Contains(DeliveryModeEnum.Notification, values);
        Assert.Contains(DeliveryModeEnum.ExpectReplies, values);
        Assert.Contains(DeliveryModeEnum.Ephemeral, values);
    }

    // Tests for StringEnumConverter with EnumStringValueAttribute
    [JsonConverter(typeof(StringEnumConverter<EventTypeWithSpecialValues>))]
    public enum EventTypeWithSpecialValues
    {
        Start,
        Error,
        [EnumStringValue("activity.sent")]
        ActivitySent,
        [EnumStringValue("activity.response")]
        ActivityResponse,
        [EnumStringValue("application/vnd.microsoft.test")]
        SpecialMimeType
    }

    [Fact]
    public void StringEnumConverter_WithDottedValues_Serializes()
    {
        var value = EventTypeWithSpecialValues.ActivitySent;
        var json = JsonSerializer.Serialize(value);
        
        Assert.Equal("\"activity.sent\"", json);
    }

    [Fact]
    public void StringEnumConverter_WithDottedValues_Deserializes()
    {
        var json = "\"activity.response\"";
        var value = JsonSerializer.Deserialize<EventTypeWithSpecialValues>(json);
        
        Assert.Equal(EventTypeWithSpecialValues.ActivityResponse, value);
    }

    [Fact]
    public void StringEnumConverter_WithoutAttribute_UsesCamelCase()
    {
        var value = EventTypeWithSpecialValues.Start;
        var json = JsonSerializer.Serialize(value);
        
        // Start doesn't have EnumStringValue, so it uses camelCase
        Assert.Equal("\"start\"", json);
    }

    [Fact]
    public void StringEnumConverter_WithSlashes_Serializes()
    {
        var value = EventTypeWithSpecialValues.SpecialMimeType;
        var json = JsonSerializer.Serialize(value);
        
        Assert.Equal("\"application/vnd.microsoft.test\"", json);
    }

    [Fact]
    public void StringEnumConverter_InObject_Works()
    {
        var obj = new Dictionary<string, object>()
        {
            { "eventType", EventTypeWithSpecialValues.ActivitySent }
        };

        var json = JsonSerializer.Serialize(obj);
        Assert.Equal("{\"eventType\":\"activity.sent\"}", json);
    }
}
