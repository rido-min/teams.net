// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// HTTP message handler that automatically acquires and attaches authentication tokens
/// for Bot Framework API calls. Supports both app-only and agentic (user-delegated) token acquisition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BotAuthenticationHandler"/> class.
/// </remarks>
/// <param name="authorizationHeaderProvider">The authorization header provider for acquiring tokens. Can be null when authentication is not configured.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="scope">The scope for the token request.</param>
/// <param name="isAuthenticationConfigured">Indicates whether authentication is configured. When false, the handler operates in anonymous mode.</param>
/// <param name="managedIdentityOptions">Optional managed identity options for user-assigned managed identity authentication.</param>
internal sealed class BotAuthenticationHandler(
    IAuthorizationHeaderProvider? authorizationHeaderProvider,
    ILogger<BotAuthenticationHandler> logger,
    string scope,
    bool isAuthenticationConfigured = true,
    IOptions<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
{
    private readonly IAuthorizationHeaderProvider? _authorizationHeaderProvider = authorizationHeaderProvider;
    private readonly ILogger<BotAuthenticationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    private readonly bool _isAuthenticationConfigured = isAuthenticationConfigured;
    private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;
    private static readonly Action<ILogger, string, Exception?> _logAgenticToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(2), "Acquiring agentic token for app {AgenticAppId}");
    private static readonly Action<ILogger, string, Exception?> _logAppOnlyToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Acquiring app-only token for scope: {Scope}");
    private static readonly Action<ILogger, Exception?> _logAnonymousMode =
        LoggerMessage.Define(LogLevel.Debug, new(1), "Authentication not configured, proceeding without authorization header (anonymous mode)");

    /// <summary>
    /// Key used to store the agentic identity in HttpRequestMessage options.
    /// </summary>
    public static readonly HttpRequestOptionsKey<AgenticIdentity?> AgenticIdentityKey = new("AgenticIdentity");

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_isAuthenticationConfigured)
        {
            _logAnonymousMode(_logger, null);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        request.Options.TryGetValue(AgenticIdentityKey, out AgenticIdentity? agenticIdentity);

        string token = await GetAuthorizationHeaderAsync(agenticIdentity, cancellationToken).ConfigureAwait(false);

        string tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..]
            : token;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an authorization header for Bot Framework API calls.
    /// Supports both app-only and agentic (user-delegated) token acquisition.
    /// </summary>
    /// <param name="agenticIdentity">Optional agentic identity for user-delegated token acquisition. If not provided, acquires an app-only token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization header value.</returns>
    private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken cancellationToken)
    {
        if (_authorizationHeaderProvider is null)
        {
            throw new InvalidOperationException("Authorization header provider is not available. This method should not be called when authentication is not configured.");
        }

        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = AddBotApplicationExtensions.MsalConfigKey,
            }
        };

        // Conditionally apply ManagedIdentity configuration if registered
        if (_managedIdentityOptions is not null)
        {
            ManagedIdentityOptions miOptions = _managedIdentityOptions.Value;

            if (!string.IsNullOrEmpty(miOptions.UserAssignedClientId))
            {
                options.AcquireTokenOptions.ManagedIdentity = miOptions;
            }
        }

        if (agenticIdentity is not null &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
        {
            _logAgenticToken(_logger, agenticIdentity.AgenticAppId, null);

            options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
            string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([_scope], options, null, cancellationToken).ConfigureAwait(false);
            return token;
        }

        _logAppOnlyToken(_logger, _scope, null);
        string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken).ConfigureAwait(false);
        return appToken;
    }
}
