namespace v2rayN.Web.Hosting;

/// <summary>
/// App-wide lock serializing every mutation of the shared <see cref="Config"/> and database,
/// replacing the serialization the desktop app gets from its single UI thread.
/// </summary>
public sealed class AppGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Runs <paramref name="action"/> while holding the gate.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>Runs <paramref name="action"/> while holding the gate.</summary>
    public async Task RunAsync(Func<Task> action, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
