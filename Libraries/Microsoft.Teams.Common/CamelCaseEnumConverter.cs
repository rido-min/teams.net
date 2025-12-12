// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common;

/// <summary>
/// JSON converter for enums that serializes/deserializes using camelCase naming.
/// This is used as a replacement for the StringEnum pattern.
/// Usage: [JsonConverter(typeof(CamelCaseEnumConverter<YourEnum>))]
/// </summary>
/// <typeparam name="T">The enum type to convert</typeparam>
public class CamelCaseEnumConverter<T> : JsonStringEnumConverter where T : struct, Enum
{
    public CamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase)
    {
    }
}
