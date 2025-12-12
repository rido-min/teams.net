// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common;

/// <summary>
/// Specifies the string value for an enum member when serializing to/from JSON.
/// Used by StringEnumConverter to map enum members to specific string values.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class EnumStringValueAttribute : Attribute
{
    public string Value { get; }

    public EnumStringValueAttribute(string value)
    {
        Value = value;
    }
}
