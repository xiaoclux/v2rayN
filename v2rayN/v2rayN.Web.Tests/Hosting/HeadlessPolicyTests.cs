namespace v2rayN.Web.Tests.Hosting;

public class HeadlessPolicyTests
{
    private const int DefaultPort = 10808;
    private const int PinnedPort = 20808;

    private static Config CreateConfig()
    {
        return new Config
        {
            TunModeItem = new TunModeItem { EnableTun = true },
            SystemProxyItem = new SystemProxyItem { SysProxyType = ESysProxyType.ForcedChange },
            GuiItem = new GUIItem { AutoRun = true },
            Inbound = [new InItem { LocalPort = DefaultPort, AllowLANConn = false }],
        };
    }

    [Test]
    public async Task Apply_ShouldDisableDesktopOnlyFeaturesAndOpenLan()
    {
        var config = CreateConfig();

        var changed = HeadlessPolicy.Apply(config, new HeadlessOptions(AllowLan: true, InboundPort: null));

        await changed.Should().BeTrue();
        await config.TunModeItem.EnableTun.Should().BeFalse();
        await config.SystemProxyItem.SysProxyType.Should().BeEqualTo(ESysProxyType.Unchanged);
        await config.GuiItem.AutoRun.Should().BeFalse();
        await config.Inbound[0].AllowLANConn.Should().BeTrue();
        await config.Inbound[0].LocalPort.Should().BeEqualTo(DefaultPort);
    }

    [Test]
    public async Task Apply_ShouldPinInboundPortAndBeIdempotent()
    {
        var config = CreateConfig();
        var options = new HeadlessOptions(AllowLan: true, InboundPort: PinnedPort);

        HeadlessPolicy.Apply(config, options);
        var changedAgain = HeadlessPolicy.Apply(config, options);

        await config.Inbound[0].LocalPort.Should().BeEqualTo(PinnedPort);
        await changedAgain.Should().BeFalse();
    }

    [Test]
    public async Task FromEnvironment_ShouldUseDefaultsWhenUnset()
    {
        var options = HeadlessOptions.FromEnvironment(_ => null);

        await options.AllowLan.Should().BeTrue();
        await options.InboundPort.Should().BeNull();
    }

    [Test]
    [Arguments("0")]
    [Arguments("65536")]
    [Arguments("abc")]
    public async Task FromEnvironment_ShouldRejectInvalidPort(string raw)
    {
        var act = () => HeadlessOptions.FromEnvironment(name => name == EnvNames.InboundPort ? raw : null);

        await act.Should().Throw<InvalidOperationException>();
    }
}
