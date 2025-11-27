using System.Collections.Concurrent;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

/// <summary>
/// Provides utilities for configuring JWT token validation.
/// </summary>
public static class TokenValidator
{
    // Static cache for OpenID Connect configuration managers.
    // Each ConfigurationManager holds a reference to an HttpClient for metadata retrieval.
    // These are intentionally long-lived and shared across the application lifetime
    // to avoid repeated configuration lookups and benefit from automatic refresh.
    private static readonly ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache = new();

    /// <summary>
    /// Configures JWT bearer token validation options.
    /// </summary>
    /// <param name="options">The JWT bearer options to configure.</param>
    /// <param name="validIssuers">The valid token issuers.</param>
    /// <param name="validAudiences">The valid token audiences.</param>
    /// <param name="openIdMetadataUrl">Optional OpenID Connect metadata URL for automatic key retrieval.</param>
    /// <remarks>
    /// The OpenID Connect configuration manager and its underlying HttpClient are cached statically
    /// per metadata URL for the lifetime of the application. This is intentional as the configuration
    /// manager handles automatic refresh of signing keys.
    /// </remarks>
    public static void ConfigureValidation(JwtBearerOptions options, IEnumerable<string> validIssuers, IEnumerable<string> validAudiences,
        string? openIdMetadataUrl = null)
    {
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = validIssuers.Any(),
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidIssuers = validIssuers,
            ValidAudiences = validAudiences,
        };

        // stricter validation: ensures the key’s issuer matches the token issuer
        options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

        // use cached OpenID Connect metadata
        if (openIdMetadataUrl != null)
        {
            options.ConfigurationManager = _openIdMetadataCache.GetOrAdd(
                openIdMetadataUrl,
                key => new ConfigurationManager<OpenIdConnectConfiguration>(
                openIdMetadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpClient())
                {
                    AutomaticRefreshInterval = BaseConfigurationManager.DefaultAutomaticRefreshInterval
                });
        }
    }
}