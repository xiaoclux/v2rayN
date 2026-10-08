namespace v2rayN.Web.Endpoints;

/// <summary>Request body for switching the active routing set.</summary>
public sealed record SetRoutingRequest(string? RoutingId);

/// <summary>Request body for switching the active profile.</summary>
public sealed record SetServerRequest(string? IndexId);

/// <summary>Status bar data: running core, inbound ports and routing choice.</summary>
public sealed record StatusResponse(
    RunningState Running,
    int InboundPort,
    bool AllowLan,
    string? CurrentRoutingId,
    IReadOnlyList<RoutingSummary> Routings);

/// <summary>Routing set as shown in the status bar picker.</summary>
public sealed record RoutingSummary(string Id, string Remarks, bool IsActive);

/// <summary>Meta information for the SPA: version, runtime and which features are available.</summary>
public sealed record MetaResponse(string Version, string RuntimeIdentifier, string Language, IReadOnlyDictionary<string, bool> Features);

/// <summary>Status, core control and meta endpoints.</summary>
public static class StatusEndpoints
{
    public static RouteGroupBuilder MapStatusEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/meta", GetMeta);
        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/core/reload", ReloadAsync);
        group.MapPut("/status/routing", SetRoutingAsync);
        group.MapPut("/status/server", SetServerAsync);
        group.MapGet("/logs/recent", (EventHub hub) => hub.RecentLogs);
        return group;
    }

    private static MetaResponse GetMeta()
    {
        var config = AppManager.Instance.Config;
        var features = new Dictionary<string, bool>(capacity: 3)
        {
            ["tun"] = false,
            ["sysproxy"] = false,
            ["selfUpdate"] = false,
        };
        return new MetaResponse(Utils.GetVersionInfo(), RuntimeInformation.RuntimeIdentifier, config.UiItem.CurrentLanguage, features);
    }

    private static async Task<StatusResponse> GetStatusAsync(ReloadCoordinator reload)
    {
        var config = AppManager.Instance.Config;
        var routings = await AppManager.Instance.RoutingItems() ?? [];
        var current = routings.FirstOrDefault(r => r.IsActive);
        var inbound = config.Inbound.FirstOrDefault();
        return new StatusResponse(
            reload.State,
            AppManager.Instance.GetLocalPort(EInboundProtocol.socks),
            inbound?.AllowLANConn ?? false,
            current?.Id,
            routings.Select(r => new RoutingSummary(r.Id, r.Remarks, r.IsActive)).ToList());
    }

    private static IResult ReloadAsync(ReloadCoordinator reload)
    {
        _ = Task.Run(reload.ReloadAsync);
        return Results.Accepted();
    }

    private static async Task<IResult> SetRoutingAsync(SetRoutingRequest request, AppGate gate, ReloadCoordinator reload, EventHub hub)
    {
        if (string.IsNullOrWhiteSpace(request.RoutingId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["routingId"] = ["Required"] });
        }
        var item = await AppManager.Instance.GetRoutingItem(request.RoutingId);
        if (item == null)
        {
            return Results.NotFound();
        }

        var ret = await gate.RunAsync(() => ConfigHandler.SetDefaultRouting(AppManager.Instance.Config, item));
        if (ret != 0)
        {
            return Results.Problem(title: ResUI.OperationFailed);
        }
        NoticeManager.Instance.SendMessageEx(ResUI.TipChangeRouting);
        hub.Publish(EventTypes.RoutingChanged, new { });
        _ = Task.Run(reload.ReloadAsync);
        return Results.NoContent();
    }

    private static async Task<IResult> SetServerAsync(SetServerRequest request, AppGate gate, ReloadCoordinator reload, EventHub hub)
    {
        if (string.IsNullOrWhiteSpace(request.IndexId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["indexId"] = ["Required"] });
        }
        var item = await AppManager.Instance.GetProfileItem(request.IndexId);
        if (item == null)
        {
            return Results.NotFound();
        }

        var ret = await gate.RunAsync(() => ConfigHandler.SetDefaultServerIndex(AppManager.Instance.Config, request.IndexId));
        if (ret != 0)
        {
            return Results.Problem(title: ResUI.OperationFailed);
        }
        hub.Publish(EventTypes.ProfilesChanged, new { });
        _ = Task.Run(reload.ReloadAsync);
        return Results.NoContent();
    }
}
