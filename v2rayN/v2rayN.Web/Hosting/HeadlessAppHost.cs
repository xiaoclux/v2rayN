// Startup sequence ported from upstream v2rayN.Desktop/App.axaml.cs, StatusBarViewModel.Init()
// and ServiceLib/ViewModels/MainWindowViewModel.cs Init() (324-347), UpdateHandler and
// UpdateTaskHandler (364-392) @5ea8ae64. Re-check those methods when rebasing on upstream.

namespace v2rayN.Web.Hosting;

/// <summary>
/// Runs the v2rayN app lifecycle without a UI: initializes managers, starts the core for the
/// default profile, wires scheduled tasks and bridges app events to the browser. On host
/// shutdown (SIGTERM) it saves state and stops the core.
/// </summary>
/// <remarks>Requires <see cref="AppManager.InitApp"/> to have run before the host is built.</remarks>
public sealed class HeadlessAppHost(
    EventHub hub,
    ReloadCoordinator reload,
    HeadlessOptions options,
    HeadlessWindowDialog windowDialog,
    IHostApplicationLifetime lifetime,
    ILogger<HeadlessAppHost> logger) : IHostedService
{
    private readonly List<IDisposable> _subscriptions = new(capacity: 4);
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once initialization finished; used by the health check.</summary>
    public bool IsStarted => _started.Task.IsCompletedSuccessfully;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Logging.SaveLog("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => Logging.SaveLog("TaskScheduler_UnobservedTaskException", e.Exception);

        var config = AppManager.Instance.Config;
        AppManager.Instance.WindowDialog = windowDialog;
        if (HeadlessPolicy.Apply(config, options))
        {
            await ConfigHandler.SaveConfig(config);
        }

        BridgeAppEvents();

        AppManager.Instance.InitComponents();
        // Statistics publishing, log flushing and Clash polling are gated on this flag.
        AppManager.Instance.ShowInTaskbar = true;

        await ConfigHandler.InitBuiltinRouting(config);
        await ConfigHandler.InitBuiltinDNS(config);
        await ConfigHandler.InitBuiltinFullConfigTemplate(config);
        await ProfileExManager.Instance.Init();
        EnsureBundledBinDirectory();
        // Copies the image's bundled bin/ into the data volume without overwriting cores the user updated.
        await CoreManager.Instance.Init(config, UpdateHandler);
        await CertPemManager.Instance.Init(config);
        TaskManager.Instance.RegUpdateTask(config, HandleUpdateTaskAsync);

        if (config.GuiItem.EnableStatistics || config.GuiItem.DisplayRealTimeSpeed)
        {
            await StatisticsManager.Instance.Init(config, UpdateStatisticsHandler);
        }

        _started.TrySetResult();
        logger.LogInformation("v2rayN headless host started: {Runtime}", Utils.GetRuntimeInfo());

        _ = Task.Run(reload.ReloadAsync, CancellationToken.None);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        hub.Publish(EventTypes.AppStopping, new { });
        var exit = AppManager.Instance.AppExitAsync(false);
        var finished = await Task.WhenAny(exit, Task.Delay(HostConsts.ExitTimeout, cancellationToken));
        if (finished != exit)
        {
            logger.LogWarning("AppExitAsync did not finish within {Timeout}", HostConsts.ExitTimeout);
        }

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }

    /// <summary>
    /// <see cref="CoreManager.Init"/> throws when the bundled bin/ next to the executable is
    /// missing (e.g. a dev build without cores); create it empty so startup proceeds.
    /// </summary>
    private void EnsureBundledBinDirectory()
    {
        var bundledBin = Utils.GetBaseDirectory("bin");
        if (Directory.Exists(bundledBin))
        {
            return;
        }
        logger.LogWarning("No bundled cores found at {Path}; download cores from the update page", bundledBin);
        try
        {
            Directory.CreateDirectory(bundledBin);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Cannot create {bundledBin}; ship cores in the image or make the directory writable.", ex);
        }
    }

    private void BridgeAppEvents()
    {
        _subscriptions.Add(AppEvents.SendMsgViewRequested.AsObservable().Subscribe(new ActionObserver<string>(hub.AppendLog)));
        _subscriptions.Add(AppEvents.SendSnackMsgRequested.AsObservable().Subscribe(new ActionObserver<string>(msg => hub.Publish(EventTypes.Toast, new { message = msg }))));
        _subscriptions.Add(AppEvents.HasUpdateNotified.AsObservable().Subscribe(new ActionObserver<bool>(has => hub.Publish(EventTypes.UpdateAvailable, new { available = has }))));
        // Restore and "restart" publish this; the container restart policy relaunches us.
        _subscriptions.Add(AppEvents.ShutdownRequested.AsObservable().Subscribe(new ActionObserver<bool>(_ => lifetime.StopApplication())));
    }

    private Task UpdateHandler(bool notify, string msg)
    {
        NoticeManager.Instance.SendMessage(msg);
        if (notify)
        {
            NoticeManager.Instance.Enqueue(msg);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Progress callback for subscription/geo updates: logs the message and, after a successful
    /// update, restarts the core when the active profile may have changed.
    /// </summary>
    public async Task HandleUpdateTaskAsync(bool success, string msg)
    {
        NoticeManager.Instance.SendMessageEx(msg);
        if (!success)
        {
            return;
        }

        var config = AppManager.Instance.Config;
        hub.Publish(EventTypes.ProfilesChanged, new { });
        hub.Publish(EventTypes.SubsChanged, new { });

        // Same reload conditions as the desktop: the active profile may have changed.
        var activeChanged = reload.State.ProfileIndexId != config.IndexId || config.SubIndexId.IsNullOrEmpty();
        if (activeChanged)
        {
            await reload.ReloadAsync();
            return;
        }
        var profile = await AppManager.Instance.GetProfileItem(config.IndexId);
        if (profile != null && profile.Subid == config.SubIndexId)
        {
            await reload.ReloadAsync();
        }
    }

    private Task UpdateStatisticsHandler(ServerSpeedItem update)
    {
        hub.Publish(EventTypes.Speed, update);
        return Task.CompletedTask;
    }

    /// <summary>Adapts a callback to <see cref="IObserver{T}"/> without pulling in an Rx dependency.</summary>
    private sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnNext(T value)
        {
            onNext(value);
        }

        public void OnError(Exception error)
        {
            Logging.SaveLog(nameof(HeadlessAppHost), error);
        }

        public void OnCompleted()
        {
        }
    }
}
