# syntax=docker/dockerfile:1.7
# Headless v2rayN with web UI.
#   Native arch:  docker build -t v2rayn-web .
#   Multi-arch:   docker buildx build --platform linux/amd64,linux/arm64 -t <registry>/v2rayn-web --push .
# Build args: CORE_SOURCE=bundle|upstream, XRAY_VERSION, SINGBOX_VERSION (upstream only, default latest).
ARG DOTNET_VERSION=10.0
ARG NODE_VERSION=22

# ---- Frontend: architecture independent, always built on the build host ----
FROM --platform=$BUILDPLATFORM node:${NODE_VERSION}-bookworm-slim AS web
WORKDIR /src/v2rayN
COPY v2rayN/ServiceLib/Resx/ ServiceLib/Resx/
COPY v2rayN/v2rayN.Web.Client/package.json v2rayN/v2rayN.Web.Client/package-lock.json v2rayN.Web.Client/
RUN cd v2rayN.Web.Client && npm ci --no-audit --no-fund
COPY v2rayN/v2rayN.Web.Client/ v2rayN.Web.Client/
RUN cd v2rayN.Web.Client && npm run build -- --outDir /out/wwwroot

# ---- Backend: cross-publish for the target RID from the build host (no emulation needed) ----
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG TARGETARCH
WORKDIR /src
COPY global.json ./
COPY v2rayN/Directory.Build.props v2rayN/Directory.Packages.props v2rayN/
COPY v2rayN/ServiceLib/ServiceLib.csproj v2rayN/ServiceLib/
COPY v2rayN/ServiceLib.UdpTest/ServiceLib.UdpTest.csproj v2rayN/ServiceLib.UdpTest/
COPY v2rayN/v2rayN.Web/v2rayN.Web.csproj v2rayN/v2rayN.Web/
RUN case "$TARGETARCH" in \
      amd64) echo linux-x64 ;; \
      arm64) echo linux-arm64 ;; \
      *) echo "unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
    esac > /rid \
 && dotnet restore v2rayN/v2rayN.Web/v2rayN.Web.csproj -r "$(cat /rid)"
COPY v2rayN/ServiceLib/ v2rayN/ServiceLib/
COPY v2rayN/ServiceLib.UdpTest/ v2rayN/ServiceLib.UdpTest/
COPY v2rayN/v2rayN.Web/ v2rayN/v2rayN.Web/
RUN dotnet publish v2rayN/v2rayN.Web/v2rayN.Web.csproj -c Release -r "$(cat /rid)" \
      --self-contained false --no-restore -o /out/app

# ---- Cores and geo files for the target architecture ----
FROM --platform=$BUILDPLATFORM debian:bookworm-slim AS cores
ARG TARGETARCH
ARG CORE_SOURCE=bundle
ARG XRAY_VERSION=
ARG SINGBOX_VERSION=
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates unzip jq \
 && rm -rf /var/lib/apt/lists/*
COPY docker/fetch-cores.sh /usr/local/bin/fetch-cores.sh
RUN XRAY_VERSION="$XRAY_VERSION" SINGBOX_VERSION="$SINGBOX_VERSION" \
    fetch-cores.sh "$TARGETARCH" "$CORE_SOURCE" /out

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
RUN apt-get update \
 && apt-get install -y --no-install-recommends tini libfontconfig1 ca-certificates curl tzdata \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /data && chown app:app /data
COPY --from=build /out/app /opt/v2rayn/
COPY --from=web /out/wwwroot /opt/v2rayn/wwwroot/
# Bundled cores; CoreManager.Init copies them into /data/v2rayN/bin without overwriting cores updated from the UI.
COPY --from=cores /out/bin /opt/v2rayn/bin/

# Data lives in $XDG_DATA_HOME/v2rayN = /data/v2rayN (see ServiceLib Utils.StartupPath).
ENV V2RAYN_LOCAL_APPLICATION_DATA_V2=1 \
    XDG_DATA_HOME=/data \
    HOME=/data \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_CONTENTROOT=/opt/v2rayn \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=UTC

WORKDIR /opt/v2rayn
VOLUME ["/data"]
# 8080 web UI; 10808 mixed socks/http inbound; 10810 optional LAN inbound with auth.
EXPOSE 8080 10808 10810
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/healthz || exit 1
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "/opt/v2rayn/v2rayN.Web.dll"]
