#!/usr/bin/env bash
# Downloads proxy cores and geo/rule files for one architecture into <out>/bin.
# Usage: fetch-cores.sh <amd64|arm64> <bundle|upstream> <out-dir>
#   bundle   - 2dust/v2rayN-core-bin archive (same source as package-debian.sh)
#   upstream - Xray + sing-box official releases (XRAY_VERSION / SINGBOX_VERSION, default latest) + geo files
# URLs and layout follow package-debian.sh (xray_url_for_rid, populate_assets_zip_mode, download_geo_assets).
set -euo pipefail

ARCH="${1:?arch required}"
SOURCE="${2:?source required}"
OUT="${3:?out dir required}"
BIN="$OUT/bin"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

case "$ARCH" in
  amd64) BUNDLE_SUFFIX=64;    XRAY_SUFFIX=64;        SINGBOX_SUFFIX=amd64 ;;
  arm64) BUNDLE_SUFFIX=arm64; XRAY_SUFFIX=arm64-v8a; SINGBOX_SUFFIX=arm64 ;;
  *) echo "[!] unsupported arch: $ARCH" >&2; exit 1 ;;
esac

fetch() { curl -fsSL --retry 3 -o "$2" "$1"; }

latest_tag() {
  curl -fsSL "https://api.github.com/repos/$1/releases/latest" | jq -r '.tag_name' | sed 's/^v//'
}

unify_geo_layout() {
  local n
  for n in geosite.dat geoip.dat geoip-only-cn-private.dat Country.mmdb geoip.metadb; do
    if [[ -f "$BIN/xray/$n" ]]; then
      mv -f "$BIN/xray/$n" "$BIN/$n"
    fi
  done
}

fetch_bundle() {
  local url="https://raw.githubusercontent.com/2dust/v2rayN-core-bin/refs/heads/master/v2rayN-linux-${BUNDLE_SUFFIX}.zip"
  echo "[+] bundle: $url"
  fetch "$url" "$TMP/bundle.zip"
  unzip -q "$TMP/bundle.zip" -d "$TMP/bundle"
  local src
  src="$(find "$TMP/bundle" -maxdepth 3 -type d -name bin | head -n1)"
  [[ -n "$src" ]] || { echo "[!] bundle has no bin/ directory" >&2; exit 1; }
  cp -a "$src/." "$BIN/"
}

fetch_geo() {
  local srss="$BIN/srss" f
  mkdir -p "$srss"
  fetch "https://github.com/Loyalsoldier/V2ray-rules-dat/releases/latest/download/geosite.dat" "$BIN/geosite.dat"
  fetch "https://github.com/Loyalsoldier/V2ray-rules-dat/releases/latest/download/geoip.dat" "$BIN/geoip.dat"
  fetch "https://raw.githubusercontent.com/Loyalsoldier/geoip/release/geoip-only-cn-private.dat" "$BIN/geoip-only-cn-private.dat"
  fetch "https://raw.githubusercontent.com/Loyalsoldier/geoip/release/Country.mmdb" "$BIN/Country.mmdb"
  fetch "https://github.com/MetaCubeX/meta-rules-dat/releases/latest/download/geoip.metadb" "$BIN/geoip.metadb"
  for f in geoip-private.srs geoip-cn.srs geoip-facebook.srs geoip-fastly.srs geoip-google.srs geoip-netflix.srs geoip-telegram.srs geoip-twitter.srs; do
    fetch "https://raw.githubusercontent.com/2dust/sing-box-rules/refs/heads/rule-set-geoip/$f" "$srss/$f"
  done
  for f in geosite-cn.srs geosite-gfw.srs geosite-google.srs geosite-greatfire.srs geosite-geolocation-cn.srs geosite-category-ads-all.srs geosite-private.srs; do
    fetch "https://raw.githubusercontent.com/2dust/sing-box-rules/refs/heads/rule-set-geosite/$f" "$srss/$f"
  done
}

fetch_upstream() {
  local xray_ver="${XRAY_VERSION:-}" singbox_ver="${SINGBOX_VERSION:-}"
  [[ -n "$xray_ver" ]] || xray_ver="$(latest_tag XTLS/Xray-core)"
  [[ -n "$singbox_ver" ]] || singbox_ver="$(latest_tag SagerNet/sing-box)"
  echo "[+] upstream: Xray $xray_ver, sing-box $singbox_ver"

  mkdir -p "$BIN/xray" "$BIN/sing_box"
  fetch "https://github.com/XTLS/Xray-core/releases/download/v${xray_ver}/Xray-linux-${XRAY_SUFFIX}.zip" "$TMP/xray.zip"
  unzip -q "$TMP/xray.zip" -d "$TMP/xray"
  install -m 755 "$TMP/xray/xray" "$BIN/xray/xray"

  fetch "https://github.com/SagerNet/sing-box/releases/download/v${singbox_ver}/sing-box-${singbox_ver}-linux-${SINGBOX_SUFFIX}.tar.gz" "$TMP/singbox.tar.gz"
  tar -C "$TMP" -xzf "$TMP/singbox.tar.gz"
  install -m 755 "$(find "$TMP" -type f -name sing-box | head -n1)" "$BIN/sing_box/sing-box"

  fetch_geo
}

mkdir -p "$BIN"
case "$SOURCE" in
  bundle)   fetch_bundle ;;
  upstream) fetch_upstream ;;
  *) echo "[!] unsupported source: $SOURCE" >&2; exit 1 ;;
esac
unify_geo_layout
# Mihomo is not shipped by package-debian.sh either; users can add it from the update page.
rm -rf "$BIN/mihomo"
find "$BIN" -type f \( -name xray -o -name sing-box -o -name 'v2ray*' -o -name 'hysteria*' -o -name 'naive' -o -name 'brook' -o -name 'juicity*' \) -exec chmod 755 {} +
echo "[+] cores ready in $BIN"; find "$BIN" -maxdepth 2 -type f -perm -u+x | sort
