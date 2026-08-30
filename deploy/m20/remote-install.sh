#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run as root: sudo bash remote-install.sh" >&2
  exit 1
fi

: "${SWINGPOP_LOBBY_DOMAIN:?Set SWINGPOP_LOBBY_DOMAIN to the DNS name pointing at this VM}"
: "${SWINGPOP_AUTH_KEY_FILE:?Set SWINGPOP_AUTH_KEY_FILE to a local base64 development key file}"

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ARTIFACT_DIR="${SCRIPT_DIR}/artifacts"
[[ -f "${ARTIFACT_DIR}/lobby/SwingPopLobby.x86_64" ]] || { echo "Lobby artifact missing" >&2; exit 1; }
[[ -f "${ARTIFACT_DIR}/server/SwingPopServer.x86_64" ]] || { echo "Server artifact missing" >&2; exit 1; }
[[ -f "${SWINGPOP_AUTH_KEY_FILE}" ]] || { echo "Auth key file missing" >&2; exit 1; }
command -v caddy >/dev/null 2>&1 || { echo "Install Caddy from its official repository first." >&2; exit 1; }

id swingpop >/dev/null 2>&1 || useradd --system --home /var/lib/swingpop --shell /usr/sbin/nologin swingpop
install -d -o swingpop -g swingpop /opt/swingpop/lobby /opt/swingpop/server \
  /var/lib/swingpop/allocations /var/log/swingpop
cp -a "${ARTIFACT_DIR}/lobby/." /opt/swingpop/lobby/
cp -a "${ARTIFACT_DIR}/server/." /opt/swingpop/server/
chown -R swingpop:swingpop /opt/swingpop /var/lib/swingpop /var/log/swingpop
chmod 0755 /opt/swingpop/lobby/SwingPopLobby.x86_64 /opt/swingpop/server/SwingPopServer.x86_64

install -d -m 0750 -o swingpop -g swingpop /etc/swingpop
install -m 0600 -o swingpop -g swingpop "${SWINGPOP_AUTH_KEY_FILE}" /etc/swingpop/auth-key.txt
sed "s/lobby.example.com/${SWINGPOP_LOBBY_DOMAIN}/g" "${SCRIPT_DIR}/staging.env.example" \
  > /etc/swingpop/staging.env
chown swingpop:swingpop /etc/swingpop/staging.env
chmod 0640 /etc/swingpop/staging.env

install -m 0644 "${SCRIPT_DIR}/swingpop-control-plane.service" /etc/systemd/system/swingpop-control-plane.service
install -d -m 0755 /etc/caddy
sed "s/lobby.example.com/${SWINGPOP_LOBBY_DOMAIN}/g" "${SCRIPT_DIR}/Caddyfile" > /etc/caddy/Caddyfile
systemctl daemon-reload
systemctl enable --now caddy swingpop-control-plane
systemctl restart caddy swingpop-control-plane

echo "Installed. Verify with: curl --fail --show-error https://${SWINGPOP_LOBBY_DOMAIN}/healthz"
