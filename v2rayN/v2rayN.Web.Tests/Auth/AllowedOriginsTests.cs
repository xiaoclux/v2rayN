namespace v2rayN.Web.Tests.Auth;

public class AllowedOriginsTests
{
    private const string Host = "nas.local:8080";

    private static HttpRequest CreateRequest(string? origin)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString(Host);
        if (origin != null)
        {
            ctx.Request.Headers.Origin = origin;
        }
        return ctx.Request;
    }

    [Test]
    public async Task IsAllowed_ShouldAcceptMissingOrSameOrigin()
    {
        var origins = new AllowedOrigins(_ => null);

        await origins.IsAllowed(CreateRequest(null)).Should().BeTrue();
        await origins.IsAllowed(CreateRequest($"http://{Host}")).Should().BeTrue();
    }

    [Test]
    public async Task IsAllowed_ShouldRejectForeignOriginUnlessConfigured()
    {
        const string foreign = "https://proxy.example.com";
        var strict = new AllowedOrigins(_ => null);
        var configured = new AllowedOrigins(name => name == EnvNames.WebAllowedOrigins ? $" {foreign} ,other" : null);

        await strict.IsAllowed(CreateRequest(foreign)).Should().BeFalse();
        await strict.IsAllowed(CreateRequest("null")).Should().BeFalse();
        await configured.IsAllowed(CreateRequest(foreign)).Should().BeTrue();
    }
}
