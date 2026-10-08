using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

// Mirrors v2rayN.Desktop/Program.cs: load config and create tables before anything else,
// so the data directory (Utils.StartupPath) is settled before Data Protection uses it.
if (!AppManager.Instance.InitApp())
{
    Console.Error.WriteLine("v2rayN: failed to load configuration, see guiLogs for details.");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
Func<string, string?> getEnv = Environment.GetEnvironmentVariable;

builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = HostConsts.HostShutdownTimeout);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSingleton(CredentialVerifier.FromEnvironment(getEnv));
builder.Services.AddSingleton(HeadlessOptions.FromEnvironment(getEnv));
builder.Services.AddSingleton(new AllowedOrigins(getEnv));
builder.Services.AddSingleton<EventHub>();
builder.Services.AddSingleton<AppGate>();
builder.Services.AddSingleton<ReloadCoordinator>();
builder.Services.AddSingleton<SpeedtestRunner>();
builder.Services.AddSingleton<HeadlessWindowDialog>();
builder.Services.AddSingleton<HeadlessAppHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HeadlessAppHost>());

builder.Services.AddDataProtection()
    .SetApplicationName(AuthConsts.DataProtectionAppName)
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Utils.StartupPath(), AuthConsts.DataProtectionKeysDir)));

var cookieSecure = string.Equals(getEnv(EnvNames.WebCookieSecure), bool.TrueString, StringComparison.OrdinalIgnoreCase)
    ? CookieSecurePolicy.Always
    : CookieSecurePolicy.SameAsRequest;

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = AuthConsts.CookieName;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = cookieSecure;
        o.ExpireTimeSpan = AuthConsts.SessionSlidingExpiration;
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = AuthEndpoints.ValidatePrincipalAsync;
        // An API never redirects to a login page; the SPA handles 401/403 itself.
        o.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = AuthConsts.CsrfHeaderName;
    o.Cookie.Name = AuthConsts.AntiforgeryCookieName;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = cookieSecure;
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(AuthConsts.LoginRateLimitPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = AuthConsts.LoginPermitPerWindow,
            Window = AuthConsts.LoginWindow,
            QueueLimit = 0,
        }));
});

if (string.Equals(getEnv(EnvNames.WebTrustProxy), bool.TrueString, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        // Trust whichever proxy sits in front; only enable this behind a proxy you control.
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = InputLimits.MaxUploadBytes + MultipartOverheadBytes);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", (HeadlessAppHost host) => host.IsStarted ? Results.Ok(new { status = "ok" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

var api = app.MapGroup("/api").AddEndpointFilter<CsrfEndpointFilter>();
api.MapGroup("/auth").MapAuthEndpoints();

var secured = api.MapGroup(string.Empty).RequireAuthorization();
secured.MapStatusEndpoints();
secured.MapSseEndpoints();
secured.MapSubscriptionEndpoints();
secured.MapProfileEndpoints();
secured.MapSpeedtestEndpoints();

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
return 0;

/// <summary>Entry point marker for WebApplicationFactory in integration tests.</summary>
public partial class Program
{
    /// <summary>Headroom for multipart boundaries and form fields around an upload.</summary>
    private const long MultipartOverheadBytes = 64 * 1024;
}
