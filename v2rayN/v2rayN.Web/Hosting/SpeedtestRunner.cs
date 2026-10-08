// Wiring ported from upstream ServiceLib/ViewModels/ProfilesViewModel.cs ServerSpeedtest()/SetSpeedTestResult() @5ea8ae64.

namespace v2rayN.Web.Hosting;

/// <summary>
/// Owns the single <see cref="SpeedtestService"/> and forwards its per-profile results to the
/// browser. The service itself persists results via <see cref="ProfileExManager"/>.
/// </summary>
public sealed class SpeedtestRunner(EventHub hub)
{
    private readonly Lock _gate = new();
    private SpeedtestService? _service;

    /// <summary>Starts a test run over <paramref name="profiles"/> (in the given order) without waiting for it.</summary>
    public void Start(ESpeedActionType action, List<ProfileItem> profiles)
    {
        // FastRealping is Realping over the whole list; the caller already chose the list.
        var effective = action == ESpeedActionType.FastRealping ? ESpeedActionType.Realping : action;
        var task = GetService().RunLoop(effective, profiles);
        _ = task.ContinueWith(
            t => Logging.SaveLog(nameof(SpeedtestRunner), t.Exception!),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    /// <summary>Cancels all running tests.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _service?.ExitLoop();
        }
    }

    private SpeedtestService GetService()
    {
        lock (_gate)
        {
            return _service ??= new SpeedtestService(AppManager.Instance.Config, OnResultAsync);
        }
    }

    private Task OnResultAsync(SpeedTestResult result)
    {
        // An empty IndexId carries a summary message instead of a per-profile result.
        if (result.IndexId.IsNullOrEmpty())
        {
            NoticeManager.Instance.SendMessageEx(result.Delay);
            NoticeManager.Instance.Enqueue(result.Delay);
            return Task.CompletedTask;
        }
        hub.Publish(EventTypes.SpeedtestResult, new
        {
            indexId = result.IndexId,
            delay = result.Delay,
            speed = result.Speed,
            ipInfo = result.IpInfo,
        });
        return Task.CompletedTask;
    }
}
