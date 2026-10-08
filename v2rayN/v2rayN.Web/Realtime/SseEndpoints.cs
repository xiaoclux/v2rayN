namespace v2rayN.Web.Realtime;

/// <summary>
/// Server-Sent Events endpoint streaming <see cref="EventHub"/> events to the browser.
/// </summary>
public static class SseEndpoints
{
    private const string ContentType = "text/event-stream";
    private const string HeartbeatFrame = ": ping\n\n";

    public static RouteGroupBuilder MapSseEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/events", StreamAsync);
        return group;
    }

    private static async Task StreamAsync(HttpContext ctx, EventHub hub)
    {
        var ct = ctx.RequestAborted;
        ctx.Response.ContentType = ContentType;
        ctx.Response.Headers.CacheControl = "no-cache";
        // Stops nginx-style reverse proxies from buffering the stream.
        ctx.Response.Headers["X-Accel-Buffering"] = "no";

        var (id, reader) = hub.Subscribe();
        try
        {
            await ctx.Response.WriteAsync(HeartbeatFrame, ct);
            await ctx.Response.Body.FlushAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(ct);
                heartbeat.CancelAfter(RealtimeConsts.HeartbeatInterval);

                var frame = await ReadNextFrameAsync(reader, heartbeat.Token, ct);
                if (frame == null)
                {
                    return;
                }
                await ctx.Response.WriteAsync(frame, ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; nothing to clean up beyond the finally block.
        }
        finally
        {
            hub.Unsubscribe(id);
        }
    }

    /// <returns>The next SSE frame, a heartbeat frame on idle timeout, or null when the stream ended.</returns>
    private static async Task<string?> ReadNextFrameAsync(ChannelReader<ServerEvent> reader, CancellationToken waitToken, CancellationToken requestToken)
    {
        try
        {
            if (!await reader.WaitToReadAsync(waitToken))
            {
                return null;
            }
        }
        catch (OperationCanceledException) when (!requestToken.IsCancellationRequested)
        {
            return HeartbeatFrame;
        }

        var sb = new StringBuilder();
        while (reader.TryRead(out var evt))
        {
            sb.Append("id: ").Append(evt.Id).Append('\n')
              .Append("event: ").Append(evt.Type).Append('\n')
              .Append("data: ").Append(evt.Data).Append("\n\n");
        }
        return sb.ToString();
    }
}
