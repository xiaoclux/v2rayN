namespace v2rayN.Web.Hosting;

/// <summary>
/// Safety net for <see cref="AppManager.WindowDialog"/>. The web host never opens dialogs
/// through view models, so any call here means an unported code path; it is logged and
/// treated as "cancelled".
/// </summary>
public sealed class HeadlessWindowDialog(ILogger<HeadlessWindowDialog> logger) : IWindowDialog
{
    public Task<bool> ShowDialogAsync<TViewModel>(TViewModel vm) where TViewModel : class
    {
        logger.LogWarning("Dialog requested in headless mode and ignored: {ViewModel}", typeof(TViewModel).Name);
        return Task.FromResult(false);
    }
}
