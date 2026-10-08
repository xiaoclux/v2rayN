namespace v2rayN.Web.Tests.Realtime;

public class EventHubTests
{
    private const int RingCapacity = 3;

    [Test]
    public async Task LogRingBuffer_ShouldKeepNewestLinesInOrder()
    {
        var ring = new LogRingBuffer(RingCapacity);
        foreach (var line in new[] { "a", "b", "c", "d", "e" })
        {
            ring.Add(line);
        }

        await ring.Snapshot().Should().BeEquivalentTo(["c", "d", "e"]);
    }

    [Test]
    public async Task Publish_ShouldDeliverToSubscribersWithIncreasingIds()
    {
        using var hub = new EventHub(NullLogger<EventHub>.Instance);
        var (id, reader) = hub.Subscribe();

        hub.Publish(EventTypes.Toast, new { message = "one" });
        hub.Publish(EventTypes.Toast, new { message = "two" });

        await reader.TryRead(out var first).Should().BeTrue();
        await reader.TryRead(out var second).Should().BeTrue();
        await first!.Data.Should().BeEqualTo("{\"message\":\"one\"}");
        await (second!.Id > first.Id).Should().BeTrue();

        hub.Unsubscribe(id);
        await hub.ClientCount.Should().BeEqualTo(0);
    }
}
