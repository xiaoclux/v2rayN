namespace v2rayN.Web.Auth;

/// <summary>Adds CSP and related hardening headers to every response.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        return next(context);
    }
}
