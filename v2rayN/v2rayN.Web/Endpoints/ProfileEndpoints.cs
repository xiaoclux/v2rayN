namespace v2rayN.Web.Endpoints;

/// <summary>Request body carrying a list of profile ids.</summary>
public sealed record ProfileIdsRequest(List<string>? Ids);

/// <summary>Request body for importing share links or subscription content.</summary>
public sealed record ImportRequest(string? Text, string? SubId);

/// <summary>Number of imported profiles.</summary>
public sealed record ImportResponse(int Count);

/// <summary>Profile list, import, share and remove endpoints.</summary>
public static class ProfileEndpoints
{
    private const string PngContentType = "image/png";

    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/profiles", ListAsync);
        group.MapPost("/profiles/remove", RemoveAsync);
        group.MapPost("/profiles/import", ImportAsync);
        group.MapPost("/profiles/import-qr", ImportQrAsync);
        group.MapGet("/profiles/{id}/share", ShareAsync);
        group.MapGet("/profiles/{id}/qr.png", QrAsync);
        return group;
    }

    private static async Task<IResult> ListAsync(string? subId, string? filter)
    {
        if (await ApiResults.CheckSubIdAsync(subId) is { } error)
        {
            return error;
        }
        if ((filter?.Length ?? 0) > InputLimits.MaxFilterLength)
        {
            return ApiResults.Invalid("filter", $"At most {InputLimits.MaxFilterLength} characters");
        }
        return Results.Ok(await ProfileQueryService.ListAsync(subId ?? string.Empty, filter ?? string.Empty));
    }

    private static async Task<IResult> RemoveAsync(ProfileIdsRequest request, AppGate gate, EventHub hub, ReloadCoordinator reload)
    {
        if (ApiResults.CheckIds(request.Ids) is { } error)
        {
            return error;
        }
        var config = AppManager.Instance.Config;
        var removedActive = await gate.RunAsync(async () =>
        {
            var items = await AppManager.Instance.GetProfileItemsByIndexIds(request.Ids!);
            var containsActive = items.Exists(t => t.IndexId == config.IndexId);
            await ConfigHandler.RemoveServers(config, items);
            return containsActive;
        });
        NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        hub.Publish(EventTypes.ProfilesChanged, new { });
        if (removedActive)
        {
            _ = Task.Run(reload.ReloadAsync);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> ImportAsync(ImportRequest request, AppGate gate, EventHub hub)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return ApiResults.Invalid("text", "Required");
        }
        if (request.Text.Length > InputLimits.MaxImportTextLength)
        {
            return ApiResults.Invalid("text", "Too large");
        }
        return await ImportTextAsync(request.Text, request.SubId, gate, hub);
    }

    private static async Task<IResult> ImportQrAsync(HttpRequest http, AppGate gate, EventHub hub)
    {
        if (!http.HasFormContentType)
        {
            return ApiResults.Invalid("file", "Multipart form required");
        }
        var form = await http.ReadFormAsync(http.HttpContext.RequestAborted);
        var file = form.Files.GetFile("file");
        if (file == null || file.Length == 0)
        {
            return ApiResults.Invalid("file", "Required");
        }
        if (file.Length > InputLimits.MaxUploadBytes)
        {
            return ApiResults.Invalid("file", "Too large");
        }

        using var buffer = new MemoryStream(capacity: (int)file.Length);
        await file.CopyToAsync(buffer, http.HttpContext.RequestAborted);
        var text = QRCodeUtils.ParseBarcode(buffer.ToArray());
        if (text.IsNullOrEmpty())
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ResUI.NoValidQRcodeFound);
        }
        return await ImportTextAsync(text, form["subId"].ToString(), gate, hub);
    }

    private static async Task<IResult> ImportTextAsync(string text, string? subId, AppGate gate, EventHub hub)
    {
        if (await ApiResults.CheckSubIdAsync(subId) is { } error)
        {
            return error;
        }
        var config = AppManager.Instance.Config;
        var target = subId.IsNullOrEmpty() ? config.SubIndexId : subId;
        var count = await gate.RunAsync(() => ConfigHandler.AddBatchServers(config, text, target ?? string.Empty, false));
        if (count <= 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ResUI.OperationFailed);
        }
        hub.Publish(EventTypes.ProfilesChanged, new { });
        hub.Publish(EventTypes.SubsChanged, new { });
        return Results.Ok(new ImportResponse(count));
    }

    private static async Task<IResult> ShareAsync(string id)
    {
        var item = await AppManager.Instance.GetProfileItem(id);
        if (item == null)
        {
            return Results.NotFound();
        }
        var uri = FmtHandler.GetShareUri(item);
        return uri.IsNullOrEmpty() ? Results.Problem(title: ResUI.OperationFailed) : Results.Ok(new { uri });
    }

    private static async Task<IResult> QrAsync(string id)
    {
        var item = await AppManager.Instance.GetProfileItem(id);
        if (item == null)
        {
            return Results.NotFound();
        }
        var png = QRCodeUtils.GenQRCode(FmtHandler.GetShareUri(item));
        return png == null ? Results.Problem(title: ResUI.OperationFailed) : Results.File(png, PngContentType);
    }
}
