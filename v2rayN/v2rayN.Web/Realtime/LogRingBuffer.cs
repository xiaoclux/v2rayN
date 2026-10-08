namespace v2rayN.Web.Realtime;

/// <summary>
/// Fixed-size, thread-safe buffer of the most recent log lines, used to give a newly
/// connected browser the log history it missed.
/// </summary>
public sealed class LogRingBuffer
{
    private readonly string[] _items;
    private readonly Lock _gate = new();
    private int _next;
    private int _count;

    /// <param name="capacity">Maximum number of lines kept; must be positive.</param>
    public LogRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new string[capacity];
    }

    /// <summary>Appends a line, overwriting the oldest one when full.</summary>
    public void Add(string line)
    {
        lock (_gate)
        {
            _items[_next] = line;
            _next = (_next + 1) % _items.Length;
            _count = Math.Min(_count + 1, _items.Length);
        }
    }

    /// <summary>Returns the buffered lines, oldest first.</summary>
    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            var result = new List<string>(_count);
            var start = (_next - _count + _items.Length) % _items.Length;
            for (var i = 0; i < _count; i++)
            {
                result.Add(_items[(start + i) % _items.Length]);
            }
            return result;
        }
    }
}
