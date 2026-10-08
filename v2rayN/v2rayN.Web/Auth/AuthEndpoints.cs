using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace v2rayN.Web.Auth;

/// <summary>Login request body.</summary>
public sealed record LoginRequest(string? Password);

/// <summary>Session info returned to the SPA, including the CSRF token it must echo.</summary>
public sealed record SessionResponse(bool Authenticated, string? CsrfToken);

/// <summary>Login, logout and session endpoints, plus cookie validation helpers.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/session", GetSession).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting(AuthConsts.LoginRateLimitPolicy);
        group.MapPost("/logout", (Delegate)LogoutAsync);
        return group;
    }

    /// <summary>
    /// Rejects cookies issued for a different credential or older than the absolute session cap.
    /// Wired into <see cref="CookieAuthenticationEvents.OnValidatePrincipal"/>.
    /// </summary>
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var verifier = context.HttpContext.RequestServices.GetRequiredService<CredentialVerifier>();
        var principal = context.Principal;
        var stamp = principal?.FindFirstValue(AuthConsts.StampClaim);
        var issuedAtRaw = principal?.FindFirstValue(AuthConsts.IssuedAtClaim);
        var stampValid = stamp != null && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(stamp), Encoding.ASCII.GetBytes(verifier.Stamp));
        var notExpired = long.TryParse(issuedAtRaw, out var issuedAt)
            && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAt) < AuthConsts.SessionAbsoluteExpiration;

        if (stampValid && notExpired)
        {
            return;
        }
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static SessionResponse GetSession(HttpContext ctx, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(ctx);
        return new SessionResponse(ctx.User.Identity?.IsAuthenticated == true, tokens.RequestToken);
    }

    private static async Task<IResult> LoginAsync(HttpContext ctx, LoginRequest request, CredentialVerifier verifier, ILoggerFactory loggerFactory)
    {
        if (!verifier.Verify(request.Password))
        {
            loggerFactory.CreateLogger(nameof(AuthEndpoints))
                .LogWarning("Failed web UI login from {RemoteIp}", ctx.Connection.RemoteIpAddress);
            await Task.Delay(AuthConsts.LoginFailureDelay, ctx.RequestAborted);
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credential");
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, AuthConsts.AdminName),
            new Claim(AuthConsts.StampClaim, verifier.Stamp),
            new Claim(AuthConsts.IssuedAtClaim, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(HttpContext ctx)
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }
}
