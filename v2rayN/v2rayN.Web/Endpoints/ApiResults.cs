namespace v2rayN.Web.Endpoints;

/// <summary>Shared result helpers so every endpoint reports errors the same way.</summary>
public static class ApiResults
{
    /// <summary>400 with a single field error.</summary>
    public static IResult Invalid(string field, string message)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>(capacity: 1) { [field] = [message] });
    }

    /// <summary>Maps a <see cref="SaveResult"/> to 200 (with the saved id) or 400.</summary>
    public static IResult FromSave(SaveResult result)
    {
        return result.Ok
            ? Results.Ok(new { id = result.Id, message = result.Message })
            : Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: result.Message);
    }

    /// <summary>
    /// Returns an error result when <paramref name="subId"/> is set but does not exist. Upstream
    /// <c>AppManager.ProfileModels</c> splices the id into SQL, so only known ids may reach it.
    /// </summary>
    public static async Task<IResult?> CheckSubIdAsync(string? subId)
    {
        if (subId.IsNullOrEmpty())
        {
            return null;
        }
        return await AppManager.Instance.GetSubItem(subId) == null ? Invalid("subId", "Unknown subscription") : null;
    }

    /// <summary>Returns an error result when the id list is empty or too large.</summary>
    public static IResult? CheckIds(IReadOnlyCollection<string>? ids)
    {
        if (ids is not { Count: > 0 })
        {
            return Invalid("ids", "Required");
        }
        return ids.Count > InputLimits.MaxBatchIds ? Invalid("ids", $"At most {InputLimits.MaxBatchIds} items") : null;
    }
}
