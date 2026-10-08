// Ported from upstream ServiceLib/ViewModels/MainWindowViewModel.cs Reload() (669-757)
// and ServiceLib/ViewModels/StatusBarViewModel.cs TestServerAvailability() (317-343) @5ea8ae64.
// Re-check those methods when rebasing on upstream.

namespace v2rayN.Web.Hosting;

/// <summary>Snapshot of the last core (re)start, served by <c>/api/status</c>.</summary>
public sealed record RunningState(
    bool Reloading,
    string? ProfileIndexId,
    string? ProfileRemarks,
    ECoreType? CoreType,
    int? DelayMs,
    string? OutboundIp,
    DateTimeOffset? LastReloadAt);

/// <summary>
/// Regenerates the core config for the default profile and restarts the core. Overlapping
/// requests are coalesced: while one reload runs, further requests collapse into a single
/// follow-up run, exactly like the desktop app.
/// </summary>
public class ReloadCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly EventHub _hub;
    private readonly ILogger<ReloadCoordinator> _logger;
    private int _hasNextJob;
    private RunningState _state = new(false, null, null, null, null, null, null);

    public ReloadCoordinator(EventHub hub, ILogger<ReloadCoordinator> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>Current running state.</summary>
    public RunningState State => Volatile.Read(ref _state);

    /// <summary>Requests a reload; returns immediately if one is already running (a follow-up is queued).</summary>
    public async Task ReloadAsync()
    {
        if (!await _semaphore.WaitAsync(0))
        {
            Interlocked.Exchange(ref _hasNextJob, 1);
            return;
        }

        try
        {
            SetState(State with { Reloading = true });
            await ReloadCoreAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reload failed");
            NoticeManager.Instance.SendMessageEx(ex.Message);
        }
        finally
        {
            SetState(State with { Reloading = false, LastReloadAt = DateTimeOffset.UtcNow });
            _semaphore.Release();
        }

        if (Interlocked.Exchange(ref _hasNextJob, 0) == 1)
        {
            await ReloadAsync();
        }
    }

    /// <summary>The actual reload work; virtual so tests can replace it.</summary>
    protected virtual async Task ReloadCoreAsync()
    {
        var config = AppManager.Instance.Config;
        var profileItem = await ConfigHandler.GetDefaultServer(config);
        if (profileItem == null)
        {
            NoticeManager.Instance.Enqueue(ResUI.CheckServerSettings);
            return;
        }

        var allResult = await CoreConfigContextBuilder.BuildAll(config, profileItem);
        if (NoticeManager.Instance.NotifyValidatorResult(allResult.CombinedValidatorResult) && !allResult.Success)
        {
            return;
        }

        await CoreManager.Instance.LoadCore(allResult.MainResult.Context, allResult.PreSocksResult?.Context);
        // No-op under the headless policy (SysProxyType.Unchanged); kept for parity with the desktop flow.
        await SysProxyHandler.UpdateSysProxy(config, false);

        SetState(State with
        {
            ProfileIndexId = profileItem.IndexId,
            ProfileRemarks = profileItem.Remarks,
            CoreType = AppManager.Instance.RunningCoreType,
            DelayMs = null,
            OutboundIp = null,
        });

        if (AppManager.Instance.IsRunningCore(ECoreType.sing_box))
        {
            _hub.Publish(EventTypes.ClashReload, new { });
        }

        await Task.Delay(HostConsts.ReloadSettleDelay);
        _ = Task.Run(() => TestAvailabilityAsync(profileItem.IndexId));
    }

    /// <summary>Measures delay and outbound IP of the running profile and records them.</summary>
    public async Task<AvailabilityCheckResult?> TestAvailabilityAsync(string indexId)
    {
        try
        {
            NoticeManager.Instance.SendMessageEx(ResUI.Speedtesting);
            var result = await ConnectionHandler.RunAvailabilityCheck();
            var ip = result.GetValidIp();
            if (ip.IsNotEmpty())
            {
                ProfileExManager.Instance.SetTestIpInfo(indexId, ip);
            }
            if (result.Time > 0)
            {
                ProfileExManager.Instance.SetTestDelay(indexId, result.Time);
            }
            NoticeManager.Instance.SendMessageEx(string.Format(ResUI.TestMeOutput, result.Time, result.Ip));

            if (State.ProfileIndexId == indexId)
            {
                SetState(State with { DelayMs = result.Time, OutboundIp = ip });
            }
            _hub.Publish(EventTypes.SpeedtestResult, new
            {
                indexId,
                delay = result.Time > 0 ? result.Time.ToString() : null,
                ipInfo = ip,
            });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Availability check failed for {IndexId}", indexId);
            return null;
        }
    }

    private void SetState(RunningState state)
    {
        Volatile.Write(ref _state, state);
        _hub.Publish(EventTypes.StatusRunning, state);
    }
}
