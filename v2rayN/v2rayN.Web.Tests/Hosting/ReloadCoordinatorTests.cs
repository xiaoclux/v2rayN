namespace v2rayN.Web.Tests.Hosting;

public class ReloadCoordinatorTests
{
    private const int ConcurrentRequests = 10;
    private const int MaxExpectedRuns = 2;

    /// <summary>Replaces the real core restart with a counter that blocks until released.</summary>
    private sealed class CountingReloadCoordinator(EventHub hub) : ReloadCoordinator(hub, NullLogger<ReloadCoordinator>.Instance)
    {
        private int _runs;

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FirstRunStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Runs => Volatile.Read(ref _runs);

        protected override async Task ReloadCoreAsync()
        {
            Interlocked.Increment(ref _runs);
            FirstRunStarted.TrySetResult();
            await Release.Task;
        }
    }

    [Test]
    public async Task ReloadAsync_ShouldCoalesceOverlappingRequests()
    {
        using var hub = new EventHub(NullLogger<EventHub>.Instance);
        var coordinator = new CountingReloadCoordinator(hub);

        var first = coordinator.ReloadAsync();
        await coordinator.FirstRunStarted.Task;
        var overlapping = Enumerable.Range(0, ConcurrentRequests).Select(_ => coordinator.ReloadAsync()).ToArray();
        await Task.WhenAll(overlapping);
        coordinator.Release.SetResult();
        await first;

        await coordinator.Runs.Should().BeEqualTo(MaxExpectedRuns);
        await coordinator.State.Reloading.Should().BeFalse();
    }
}
