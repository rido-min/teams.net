// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common;

/// <summary>
/// JSON converter for enums that:
/// 1. Uses EnumStringValueAttribute if present on enum members
/// 2. Falls back to camelCase naming for members without the attribute
/// This is the replacement for StringEnum pattern.
/// Usage: [JsonConverter(typeof(StringEnumConverter<YourEnum>))]
/// </summary>
/// <typeparam name="T">The enum type to convert</typeparam>
public class StringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly Dictionary<T, string> _enumToString = new();
    private readonly Dictionary<string, T> _stringToEnum = new(StringComparer.OrdinalIgnoreCase);

    public StringEnumConverter()
    {
        var enumType = typeof(T);
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            var enumValue = (T)field.GetValue(null)!;
            string stringValue;

            // Check for EnumStringValueAttribute first
            var attr = field.GetCustomAttribute<EnumStringValueAttribute>();
            if (attr != null)
            {
                stringValue = attr.Value;
            }
            else
            {
                // Use camelCase naming policy
                stringValue = JsonNamingPolicy.CamelCase.ConvertName(field.Name);
            }

            _enumToString[enumValue] = stringValue;
            _stringToEnum[stringValue] = enumValue;
        }
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (value is null)
        {
            throw new JsonException($"Cannot convert null to {typeof(T)}");
        }

        if (_stringToEnum.TryGetValue(value, out var enumValue))
        {
            return enumValue;
        }

        throw new JsonException($"Unable to convert \"{value}\" to enum {typeof(T)}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (_enumToString.TryGetValue(value, out var stringValue))
        {
            writer.WriteStringValue(stringValue);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
