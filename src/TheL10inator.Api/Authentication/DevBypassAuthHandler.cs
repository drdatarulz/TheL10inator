using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheL10inator.Api.Authentication;

/// <summary>
/// Authentication handler that reads the caller's email from the <c>X-Dev-User-Email</c>
/// header and emits a <see cref="ClaimsPrincipal"/> carrying the standard Entra-style
/// claims (<c>oid</c>, <c>preferred_username</c>, <c>name</c>). Registered only when
/// <c>Authentication:UseDevBypass</c> is <c>true</c>; the production JWT pipeline silently
/// ignores the header otherwise.
/// </summary>
public sealed class DevBypassAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevBypass";
    public const string EmailHeaderName = "X-Dev-User-Email";

    public DevBypassAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(EmailHeaderName, out var emailValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = emailValues.ToString();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Synthetic, deterministic "oid" so UserResolutionMiddleware can bridge the invited
        // Users row on first request the same way it would for a real Entra token.
        var oid = $"dev-bypass:{email.ToLowerInvariant()}";

        var claims = new[]
        {
            new Claim("oid", oid),
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Email, email),
            new Claim("name", email),
            new Claim(ClaimTypes.Name, email),
        };

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
