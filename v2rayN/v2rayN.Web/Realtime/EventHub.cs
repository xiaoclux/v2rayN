namespace v2rayN.Web.Realtime;

/// <summary>
/// One event pushed to browsers over SSE. <see cref="Data"/> is pre-serialized JSON.
/// </summary>
public sealed record ServerEvent(long Id, string Type, string Data);

/// <summary>
/// Fans out server events to every connected SSE client. Each client gets its own bounded
/// channel that drops the oldest events when the client is too slow, so a stalled browser
/// can never block the app. Log lines are batched to keep the event rate low.
/// </summary>
public sealed class EventHub : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<Guid, Channel<ServerEvent>> _clients = new();
    private readonly LogRingBuffer _logRing = new(RealtimeConsts.LogRingCapacity);
    private readonly ConcurrentQueue<string> _pendingLogs = new();
    private readonly ILogger<EventHub> _logger;
    private readonly Timer _logFlushTimer;
    private long _lastId;

    public EventHub(ILogger<EventHub> logger)
    {
        _logger = logger;
        _logFlushTimer = new Timer(_ => FlushLogs(), null, RealtimeConsts.LogFlushInterval, RealtimeConsts.LogFlushInterval);
    }

    /// <summary>Recent log lines, oldest first.</summary>
    public IReadOnlyList<string> RecentLogs => _logRing.Snapshot();

    /// <summary>Number of connected SSE clients.</summary>
    public int ClientCount => _clients.Count;

    /// <summary>Registers a new client and returns its id and reader.</summary>
    public (Guid Id, ChannelReader<ServerEvent> Reader) Subscribe()
    {
        var channel = Channel.CreateBounded<ServerEvent>(new BoundedChannelOptions(RealtimeConsts.ClientChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        var id = Guid.NewGuid();
        _clients[id] = channel;
        return (id, channel.Reader);
    }

    /// <summary>Removes a client registered by <see cref="Subscribe"/>.</summary>
    public void Unsubscribe(Guid id)
    {
        if (_clients.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    /// <summary>Serializes <paramref name="payload"/> as JSON and sends it to all clients.</summary>
    public void Publish<T>(string type, T payload)
    {
        var data = JsonSerializer.Serialize(payload, _jsonOptions);
        var evt = new ServerEvent(Interlocked.Increment(ref _lastId), type, data);
        foreach (var channel in _clients.Values)
        {
            channel.Writer.TryWrite(evt);
        }
    }

    /// <summary>
    /// Queues a log line: stored in the ring buffer, written to the host logger
    /// (so it shows in <c>docker logs</c>) and pushed to clients in the next batch.
    /// </summary>
    public void AppendLog(string line)
    {
        if (line.Length > RealtimeConsts.MaxLogLineLength)
        {
            line = line[..RealtimeConsts.MaxLogLineLength];
        }
        _logRing.Add(line);
        _pendingLogs.Enqueue(line);
        _logger.LogInformation("{Line}", line);
    }

    private void FlushLogs()
    {
        if (_pendingLogs.IsEmpty)
        {
            return;
        }
        var batch = new List<string>(_pendingLogs.Count);
        while (_pendingLogs.TryDequeue(out var line))
        {
            batch.Add(line);
        }
        Publish(EventTypes.Log, batch);
    }

    public void Dispose()
    {
        _logFlushTimer.Dispose();
        foreach (var id in _clients.Keys)
        {
            Unsubscribe(id);
        }
    }
}
