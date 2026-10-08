namespace v2rayN.Web.Endpoints;

/// <summary>Request body for updating subscriptions.</summary>
public sealed record UpdateSubsRequest(string? SubId, bool ViaProxy);

/// <summary>Request body for choosing the current subscription tab (also the import target).</summary>
public sealed record SetCurrentSubRequest(string? SubId);

/// <summary>Subscription list, edit and update endpoints.</summary>
public static class SubscriptionEndpoints
{
    public static RouteGroupBuilder MapSubscriptionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/subs", ListAsync);
        group.MapGet("/subs/{id}", GetAsync);
        group.MapPost("/subs", CreateAsync);
        group.MapPut("/subs/current", SetCurrentAsync);
        group.MapPut("/subs/{id}", UpdateAsync);
        group.MapDelete("/subs/{id}", DeleteAsync);
        group.MapPost("/subs/update", StartUpdateAsync);
        return group;
    }

    private static async Task<object> ListAsync()
    {
        var subs = await AppManager.Instance.SubItems() ?? [];
        return new { currentSubId = AppManager.Instance.Config.SubIndexId, items = subs };
    }

    private static async Task<IResult> GetAsync(string id)
    {
        var item = await AppManager.Instance.GetSubItem(id);
        return item == null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(SubItem body, AppGate gate, EventHub hub)
    {
        body.Id = string.Empty;
        return await SaveAsync(body, gate, hub);
    }

    private static async Task<IResult> UpdateAsync(string id, SubItem body, AppGate gate, EventHub hub)
    {
        if (await AppManager.Instance.GetSubItem(id) == null)
        {
            return Results.NotFound();
        }
        body.Id = id;
        return await SaveAsync(body, gate, hub);
    }

    private static async Task<IResult> SaveAsync(SubItem body, AppGate gate, EventHub hub)
    {
        var result = await gate.RunAsync(() => SubscriptionService.SaveAsync(body));
        if (result.Ok)
        {
            hub.Publish(EventTypes.SubsChanged, new { });
        }
        return ApiResults.FromSave(result);
    }

    private static async Task<IResult> DeleteAsync(string id, AppGate gate, EventHub hub)
    {
        if (await AppManager.Instance.GetSubItem(id) == null)
        {
            return Results.NotFound();
        }
        await gate.RunAsync(() => ConfigHandler.DeleteSubItem(AppManager.Instance.Config, id));
        hub.Publish(EventTypes.SubsChanged, new { });
        hub.Publish(EventTypes.ProfilesChanged, new { });
        return Results.NoContent();
    }

    private static async Task<IResult> SetCurrentAsync(SetCurrentSubRequest request, AppGate gate, EventHub hub)
    {
        if (await ApiResults.CheckSubIdAsync(request.SubId) is { } error)
        {
            return error;
        }
        await gate.RunAsync(async () =>
        {
            var config = AppManager.Instance.Config;
            config.SubIndexId = request.SubId ?? string.Empty;
            await ConfigHandler.SaveConfig(config);
        });
        return Results.NoContent();
    }

    private static async Task<IResult> StartUpdateAsync(UpdateSubsRequest request, HeadlessAppHost host)
    {
        if (await ApiResults.CheckSubIdAsync(request.SubId) is { } error)
        {
            return error;
        }
        var config = AppManager.Instance.Config;
        _ = Task.Run(() => SubscriptionHandler.UpdateProcess(config, request.SubId ?? string.Empty, request.ViaProxy, host.HandleUpdateTaskAsync));
        return Results.Accepted();
    }
}
