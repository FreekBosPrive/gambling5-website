using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Gambling5.Api.Services;

public sealed class AzureAdTokenValidator
{
    private const string BearerPrefix = "Bearer ";

    private readonly ILogger<AzureAdTokenValidator> _logger;
    private readonly IConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;
    private readonly string[] _audiences;
    private readonly string? _authority;

    public AzureAdTokenValidator(IConfiguration configuration, ILogger<AzureAdTokenValidator> logger)
    {
        _logger = logger;
        _authority = configuration["AzureAd:Authority"]?.TrimEnd('/');
        var configuredAudience = configuration["AzureAd:ApiAudience"];
        var clientId = configuration["AzureAd:ClientId"];

        _audiences = [.. new[]
        {
            configuredAudience,
            clientId,
            string.IsNullOrWhiteSpace(clientId) ? null : $"api://{clientId}"
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)!];

        if (!string.IsNullOrWhiteSpace(_authority))
        {
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{_authority}/v2.0/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true });
        }
    }

    public async Task<TokenValidationOutcome> ValidateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_authority) || _audiences.Length == 0 || _configurationManager is null)
        {
            return TokenValidationOutcome.Misconfigured();
        }

        if (!request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return TokenValidationOutcome.Missing();
        }

        var headerValue = authorizationHeader.ToString();

        if (!headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return TokenValidationOutcome.Missing();
        }

        var token = headerValue[BearerPrefix.Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenValidationOutcome.Missing();
        }

        try
        {
            var openIdConfiguration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = openIdConfiguration.Issuer,
                ValidateAudience = true,
                ValidAudiences = _audiences,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfiguration.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return TokenValidationOutcome.Success(principal);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Admin token validation failed");
            return TokenValidationOutcome.Invalid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating admin token");
            return TokenValidationOutcome.Invalid();
        }
    }
}

public sealed record TokenValidationOutcome(bool IsAuthenticated, bool IsMisconfigured, ClaimsPrincipal? Principal)
{
    public static TokenValidationOutcome Success(ClaimsPrincipal principal) => new(true, false, principal);

    public static TokenValidationOutcome Missing() => new(false, false, null);

    public static TokenValidationOutcome Invalid() => new(false, false, null);

    public static TokenValidationOutcome Misconfigured() => new(false, true, null);
}