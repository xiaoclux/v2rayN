// Validation ported from upstream ServiceLib/ViewModels/SubEditViewModel.cs SaveSubAsync() @5ea8ae64.

namespace v2rayN.Web.Hosting;

/// <summary>Outcome of a save: success, or a user-facing reason.</summary>
public sealed record SaveResult(bool Ok, string? Message, string? Id = null)
{
    public static SaveResult Fail(string message) => new(false, message);
}

/// <summary>Validates and saves subscriptions, mirroring the desktop edit dialog.</summary>
public static class SubscriptionService
{
    /// <summary>Saves <paramref name="input"/>; a null/empty Id creates a new subscription.</summary>
    public static async Task<SaveResult> SaveAsync(SubItem input)
    {
        if (input.Remarks.IsNullOrEmpty())
        {
            return SaveResult.Fail(ResUI.PleaseFillRemarks);
        }
        if (input.Remarks.Length > InputLimits.MaxRemarksLength
            || (input.Url?.Length ?? 0) > InputLimits.MaxUrlLength
            || (input.MoreUrl?.Length ?? 0) > InputLimits.MaxUrlLength
            || (input.RequestHeaders?.Length ?? 0) > InputLimits.MaxTextFieldLength
            || (input.Filter?.Length ?? 0) > InputLimits.MaxTextFieldLength
            || (input.Memo?.Length ?? 0) > InputLimits.MaxTextFieldLength)
        {
            return SaveResult.Fail(ResUI.OperationFailed);
        }
        if (input.AutoUpdateInterval < 0 || input.AutoUpdateInterval > InputLimits.MaxAutoUpdateIntervalMinutes)
        {
            return SaveResult.Fail(ResUI.OperationFailed);
        }
        if (input.PreSocksPort is { } port && (port < HostConsts.MinPort || port > HostConsts.MaxPort))
        {
            return SaveResult.Fail(ResUI.OperationFailed);
        }

        var url = input.Url;
        if (url.IsNotEmpty())
        {
            var uri = Utils.TryUri(url);
            if (uri == null)
            {
                return SaveResult.Fail(ResUI.InvalidUrlTip);
            }
            // The desktop only warns about plain http subscriptions; keep that behaviour.
            if (url.StartsWith(Global.HttpProtocol) && !Utils.IsPrivateNetwork(uri.IdnHost))
            {
                NoticeManager.Instance.Enqueue(ResUI.InsecureUrlProtocol);
            }
        }
        if (!HttpRequestHeadersHelper.TryParse(input.RequestHeaders, out _))
        {
            return SaveResult.Fail(ResUI.SubRequestHeadersInvalid);
        }

        if (await ConfigHandler.AddSubItem(AppManager.Instance.Config, input) != 0)
        {
            return SaveResult.Fail(ResUI.OperationFailed);
        }
        return new SaveResult(true, ResUI.OperationSuccess, input.Id);
    }
}
