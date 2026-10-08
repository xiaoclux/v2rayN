# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

v2rayN: a cross-platform (Windows / Linux / macOS) desktop GUI client that drives proxy cores (Xray, sing-box, mihomo/clash and others). The app itself does no proxying — it manages profiles/subscriptions, generates core config JSON, and launches/stops core processes. C# on .NET 10 (`global.json` pins Microsoft.Testing.Platform as the test runner).

The solution lives under `v2rayN/` (`v2rayN.slnx` / `v2rayN.sln`). `v2rayN/GlobalHotKeys` is a git submodule — clone with `--recursive` or run `git submodule update --init` before building the Desktop project.

## Commands

Run from the repo root:

```bash
# Build (Avalonia desktop app, works on macOS/Linux)
dotnet build ./v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj

# Run the desktop app
dotnet run --project ./v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj

# Tests (same as CI, .github/workflows/test.yml)
dotnet test --project ./v2rayN/ServiceLib.Tests -c Release

# Single test class / test (TUnit tree-node filter: /assembly/namespace/class/test)
dotnet test --project ./v2rayN/ServiceLib.Tests -- --treenode-filter "/*/*/WireguardFmtTests/*"
dotnet test --project ./v2rayN/ServiceLib.Tests -- --treenode-filter "/*/*/WireguardFmtTests/<TestName>"

# Release publish (mirrors .github/workflows/build.yml)
dotnet publish ./v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj -c Release -r osx-arm64 -p:SelfContained=true -o <out>
```

The WPF project `v2rayN/v2rayN` targets `net10.0-windows` and only builds on Windows. Packaging scripts (`package-osx.sh`, `package-debian*.sh`, `package-rhel*.sh`) are invoked by CI with a release tag.

There is no separate linter; style is enforced via `.editorconfig` (4-space indent, `System` usings first, no `this.` qualification). `.editorconfig` says CRLF, but files are actually stored and checked out as LF (`.gitattributes` `text=auto`) — keep new files LF. Common props (version, `net10.0`, `Nullable=annotations`, `CheckForOverflowUnderflow`) are in `v2rayN/Directory.Build.props`; package versions are centrally managed in `v2rayN/Directory.Packages.props` (add `<PackageReference>` without a version in csproj, put the version there).

## Architecture

### Projects

- **ServiceLib** — all business logic and all ViewModels; UI-framework agnostic. Nearly every change lands here.
- **v2rayN.Desktop** — Avalonia UI (Semi.Avalonia theme) for Linux/macOS/Windows.
- **v2rayN** — WPF UI (MaterialDesign) for Windows only.
- **ServiceLib.UdpTest** — SOCKS5 UDP test channel, referenced by ServiceLib.
- **ServiceLib.Tests** — TUnit tests for ServiceLib.
- **AmazTool** — standalone self-updater executable (replaces app files after an upgrade download).

Both UI projects bind to the **same** ViewModels in `ServiceLib/ViewModels`. A UI-visible feature normally requires: ViewModel change in ServiceLib + view changes in **both** `v2rayN.Desktop/Views/*.axaml` and `v2rayN/Views/*.xaml`.

### MVVM / UI communication

- ViewModels derive from `MyReactiveObject` (ReactiveUI), use `[Reactive] public partial` properties (ReactiveUI.SourceGenerators) and `ReactiveCommand.CreateFromTask`.
- VM → View signals use `EventChannel<T>` (`ServiceLib/Events`): per-VM instances (e.g. `ProfilesViewModel.ReloadRequested`) or app-wide static channels in `AppEvents` (snack messages, exit, sys-proxy change). Views subscribe via `.AsObservable()`. Dialogs close via `ICloseable.RequestClose`.
- UI strings are in `ServiceLib/Resx/ResUI.resx` (+ translations; `ResUI.Designer.cs` is generated). Add new keys to `ResUI.resx` and regenerate/update the designer file.

### Core flow (profile → running core)

1. **Storage**: profiles, subscriptions, routing, DNS, templates are SQLite tables (`sqlite-net`, `SQLiteHelper`, tables created in `AppManager.InitApp`; entities in `Models/Entities`). App settings are a single `Config` object (`Models/Configs`) persisted as `guiConfigs/guiNConfig.json`.
2. **Import**: share links / subscription content are parsed by `Handler/Fmt/*Fmt.cs` (one per protocol, dispatched by `FmtHandler`); subscriptions are fetched by `SubscriptionHandler`; `ConfigHandler` is the large static facade for adding/editing/sorting profiles and settings.
3. **Context build**: `Handler/Builder/CoreConfigContextBuilder` resolves the selected node (including group/chained nodes and routing-referenced outbounds) into an immutable `CoreConfigContext`, with `NodeValidator` producing errors/warnings. A separate "pre-socks" context is built when TUN protection or chaining needs a second core.
4. **Config generation**: `Handler/CoreConfigHandler` dispatches to `Services/CoreConfig/V2ray/*` (Xray JSON), `Services/CoreConfig/Singbox/*` (sing-box JSON) or `CoreConfigClashService`. Each core service is split by concern (Inbound/Outbound/Routing/Dns/Log/Statistic/Balancer/Template). Base JSON templates are embedded resources in `ServiceLib/Sample/` (read via `EmbedUtils.GetEmbedText`); new sample files must be added as `<EmbeddedResource>` in `ServiceLib.csproj`.
5. **Process**: `Manager/CoreManager` writes `binConfigs/config.json` (and `configPre.json`), launches the core via `ProcessService`, handles sudo for TUN on Linux/macOS, and stops cores. `CoreInfoManager` knows each core's executable names and download URLs.

### Conventions

- Managers in `ServiceLib/Manager` are singletons: `private static readonly Lazy<X> _instance = new(() => new()); public static X Instance => _instance.Value;`. Handlers are mostly static classes.
- Platform-specific code branches on `Utils.IsWindows()/IsLinux()/IsMacOS()`; system-proxy implementations are in `Handler/SysProxy/ProxySetting{Windows,Linux,OSX}.cs` (Linux/macOS use embedded shell scripts from `Sample/`).
- Tests that need app config call `CoreConfigTestFactory.CreateConfig()` + `BindAppManagerConfig()` (reflection-sets `AppManager._config`) rather than initialising the full app.
