using Microsoft.AspNetCore.Antiforgery;

namespace v2rayN.Web.Auth;

/// <summary>
/// Rejects unsafe requests (POST/PUT/PATCH/DELETE) that lack a valid antiforgery token in
/// the <see cref="AuthConsts.CsrfHeaderName"/> header, or that come from a foreign origin.
/// Layered on top of the SameSite=Strict session cookie.
/// </summary>
public sealed class CsrfEndpointFilter(IAntiforgery antiforgery, AllowedOrigins allowedOrigins) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (IsSafeMethod(http.Request.Method))
        {
            return await next(context);
        }

        if (!allowedOrigins.IsAllowed(http.Request))
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Cross-origin request rejected");
        }

        try
        {
            await antiforgery.ValidateRequestAsync(http);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing or invalid CSRF token");
        }
        return await next(context);
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
    }
}

/// <summary>
/// Origin check: an unsafe request carrying an <c>Origin</c> header must match the request
/// host or one of <see cref="EnvNames.WebAllowedOrigins"/> (comma separated).
/// </summary>
public sealed class AllowedOrigins
{
    private readonly HashSet<string> _extra;

    public AllowedOrigins(Func<string, string?> getEnv)
    {
        var raw = getEnv(EnvNames.WebAllowedOrigins) ?? string.Empty;
        _extra = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAllowed(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }
        if (_extra.Contains(origin))
        {
            return true;
        }
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }
        return string.Equals(originUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
