// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(CamelCaseEnumConverter<DeliveryMode>))]
public enum DeliveryMode
{
    Normal,
    Notification,
    ExpectReplies,
    Ephemeral
}