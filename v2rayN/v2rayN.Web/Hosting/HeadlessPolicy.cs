namespace v2rayN.Web.Hosting;

/// <summary>
/// Settings the headless host enforces on top of the user's config.
/// </summary>
/// <param name="AllowLan">Listen on 0.0.0.0 so Docker port publishing works.</param>
/// <param name="InboundPort">Pinned mixed inbound port, or null to keep the configured one.</param>
public sealed record HeadlessOptions(bool AllowLan, int? InboundPort)
{
    /// <summary>
    /// Reads <see cref="EnvNames.AllowLan"/> (default true) and <see cref="EnvNames.InboundPort"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An env value is present but invalid.</exception>
    public static HeadlessOptions FromEnvironment(Func<string, string?> getEnv)
    {
        var allowLan = true;
        var allowLanRaw = getEnv(EnvNames.AllowLan);
        if (!string.IsNullOrWhiteSpace(allowLanRaw) && !bool.TryParse(allowLanRaw, out allowLan))
        {
            throw new InvalidOperationException($"{EnvNames.AllowLan} must be true or false.");
        }

        int? inboundPort = null;
        var portRaw = getEnv(EnvNames.InboundPort);
        if (!string.IsNullOrWhiteSpace(portRaw))
        {
            if (!int.TryParse(portRaw, out var port) || port < HostConsts.MinPort || port > HostConsts.MaxPort)
            {
                throw new InvalidOperationException($"{EnvNames.InboundPort} must be an integer between {HostConsts.MinPort} and {HostConsts.MaxPort}.");
            }
            inboundPort = port;
        }

        return new HeadlessOptions(allowLan, inboundPort);
    }
}

/// <summary>
/// Forces config values that make no sense without a desktop: no TUN, no system proxy,
/// no OS autostart. The user sets http_proxy themselves.
/// </summary>
public static class HeadlessPolicy
{
    /// <summary>Applies the policy in place.</summary>
    /// <returns>True when any value changed and the config should be saved.</returns>
    public static bool Apply(Config config, HeadlessOptions options)
    {
        var changed = false;

        if (config.TunModeItem.EnableTun)
        {
            config.TunModeItem.EnableTun = false;
            changed = true;
        }
        if (config.SystemProxyItem.SysProxyType != ESysProxyType.Unchanged)
        {
            config.SystemProxyItem.SysProxyType = ESysProxyType.Unchanged;
            changed = true;
        }
        if (config.GuiItem.AutoRun)
        {
            config.GuiItem.AutoRun = false;
            changed = true;
        }

        var inbound = config.Inbound.FirstOrDefault();
        if (inbound == null)
        {
            return changed;
        }
        if (inbound.AllowLANConn != options.AllowLan)
        {
            inbound.AllowLANConn = options.AllowLan;
            changed = true;
        }
        if (options.InboundPort is { } port && inbound.LocalPort != port)
        {
            inbound.LocalPort = port;
            changed = true;
        }
        return changed;
    }
}
