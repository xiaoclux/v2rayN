namespace v2rayN.Web.Endpoints;

/// <summary>
/// Request body for a test run. <see cref="Ids"/> selects profiles; for Mixedtest and
/// FastRealping the whole list of <see cref="SubId"/> is used instead, as in the desktop.
/// </summary>
public sealed record SpeedtestRequest(ESpeedActionType Action, List<string>? Ids, string? SubId);

/// <summary>Speed/latency test endpoints; results stream over SSE as <c>speedtest.result</c>.</summary>
public static class SpeedtestEndpoints
{
    public static RouteGroupBuilder MapSpeedtestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/speedtest", StartAsync);
        group.MapPost("/speedtest/stop", Stop);
        group.MapPost("/speedtest/current", TestCurrentAsync);
        return group;
    }

    private static async Task<IResult> StartAsync(SpeedtestRequest request, SpeedtestRunner runner)
    {
        if (!Enum.IsDefined(request.Action))
        {
            return ApiResults.Invalid("action", "Unknown action");
        }

        List<string> ids;
        var isWholeList = request.Action is ESpeedActionType.Mixedtest or ESpeedActionType.FastRealping;
        if (isWholeList)
        {
            if (await ApiResults.CheckSubIdAsync(request.SubId) is { } subError)
            {
                return subError;
            }
            var rows = await ProfileQueryService.ListAsync(request.SubId ?? string.Empty, string.Empty);
            ids = rows.Select(r => r.IndexId).ToList();
        }
        else
        {
            if (ApiResults.CheckIds(request.Ids) is { } idsError)
            {
                return idsError;
            }
            ids = request.Ids!;
        }

        var profiles = await AppManager.Instance.GetProfileItemsOrderedByIndexIds(ids);
        if (profiles.Count == 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ResUI.PleaseSelectServer);
        }
        runner.Start(request.Action, profiles);
        return Results.Accepted();
    }

    private static IResult Stop(SpeedtestRunner runner)
    {
        runner.Stop();
        return Results.NoContent();
    }

    private static async Task<IResult> TestCurrentAsync(ReloadCoordinator reload)
    {
        var item = await ConfigHandler.GetDefaultServer(AppManager.Instance.Config);
        if (item == null)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ResUI.CheckServerSettings);
        }
        _ = Task.Run(() => reload.TestAvailabilityAsync(item.IndexId));
        return Results.Accepted();
    }
}
