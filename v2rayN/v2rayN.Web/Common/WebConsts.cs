namespace v2rayN.Web.Common;

/// <summary>
/// Environment variable names read by the headless web host.
/// </summary>
public static class EnvNames
{
    public const string WebCredential = "V2RAYN_WEB_PASSWORD";
    public const string WebCredentialFile = "V2RAYN_WEB_PASSWORD_FILE";
    public const string WebCookieSecure = "V2RAYN_WEB_COOKIE_SECURE";
    public const string WebTrustProxy = "V2RAYN_WEB_TRUST_PROXY";
    public const string WebAllowedOrigins = "V2RAYN_WEB_ALLOWED_ORIGINS";
    public const string AllowLan = "V2RAYN_ALLOW_LAN";
    public const string InboundPort = "V2RAYN_INBOUND_PORT";
}

/// <summary>
/// Constants for authentication, sessions and CSRF protection.
/// </summary>
public static class AuthConsts
{
    public const string CookieName = "v2rayn.sid";
    public const string AntiforgeryCookieName = "v2rayn.xsrf";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";
    public const string StampClaim = "v2rayn:stamp";
    public const string IssuedAtClaim = "v2rayn:iat";
    public const string AdminName = "admin";
    public const string LoginRateLimitPolicy = "login";
    public const string DataProtectionAppName = "v2rayN.Web";
    public const string DataProtectionKeysDir = "webkeys";
    public const string StampSalt = "v2rayN.Web.AuthStamp";

    public const int Pbkdf2Iterations = 210_000;
    public const int SaltSizeBytes = 16;
    public const int HashSizeBytes = 32;
    public const int StampSizeBytes = 16;
    public const int MaxCredentialLength = 256;

    public const int LoginPermitPerWindow = 5;
    public static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan LoginFailureDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan SessionSlidingExpiration = TimeSpan.FromHours(12);
    public static readonly TimeSpan SessionAbsoluteExpiration = TimeSpan.FromDays(7);
}

/// <summary>
/// Constants for the realtime event stream.
/// </summary>
public static class RealtimeConsts
{
    public const int ClientChannelCapacity = 1000;
    public const int LogRingCapacity = 2000;
    public const int MaxLogLineLength = 4096;
    public static readonly TimeSpan LogFlushInterval = TimeSpan.FromMilliseconds(200);
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Event type names pushed over the SSE stream.
/// </summary>
public static class EventTypes
{
    public const string Log = "log";
    public const string Toast = "toast";
    public const string Speed = "speed";
    public const string StatusRunning = "status.running";
    public const string ReloadState = "reload.state";
    public const string ProfilesChanged = "profiles.changed";
    public const string SubsChanged = "subs.changed";
    public const string RoutingChanged = "routing.changed";
    public const string ConfigChanged = "config.changed";
    public const string SpeedtestResult = "speedtest.result";
    public const string JobProgress = "job.progress";
    public const string UpdateAvailable = "update.available";
    public const string ClashReload = "clash.reload";
    public const string AppStopping = "app.stopping";
}

/// <summary>
/// Timing and range constants for the headless app lifecycle.
/// </summary>
public static class HostConsts
{
    public const int MinPort = 1;
    public const int MaxPort = 65535;
    public static readonly TimeSpan ReloadSettleDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan HostShutdownTimeout = TimeSpan.FromSeconds(25);
}

/// <summary>
/// Input limits for API requests.
/// </summary>
public static class InputLimits
{
    public const int MaxFilterLength = 128;
    public const int MaxRemarksLength = 256;
    public const int MaxUrlLength = 8192;
    public const int MaxTextFieldLength = 8192;
    public const int MaxImportTextLength = 2 * 1024 * 1024;
    public const long MaxUploadBytes = 10L * 1024 * 1024;
    public const int MaxBatchIds = 10_000;
    public const int MaxAutoUpdateIntervalMinutes = 60 * 24 * 365;
}
