// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType EndOfConversation = new("endOfConversation");
    public bool IsEndOfConversation => EndOfConversation.Equals(Value);
}

public class EndOfConversationActivity() : Activity(ActivityType.EndOfConversation)
{
    /// <summary>
    /// The a code for endOfConversation activities that indicates why the conversation ended.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonPropertyOrder(31)]
    public EndOfConversationCode? Code { get; set; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(32)]
    public required string Text { get; set; }
}

[JsonConverter(typeof(CamelCaseEnumConverter<EndOfConversationCode>))]
public enum EndOfConversationCode
{
    Unknown,
    CompletedSuccessfully,
    UserCancelled,
    BotTimedOut,
    BotIssuedInvalidMessage,
    ChannelFailed
}