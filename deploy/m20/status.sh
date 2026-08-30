#!/usr/bin/env bash
set -euo pipefail
source /etc/swingpop/staging.env
systemctl --no-pager --full status swingpop-control-plane caddy
curl --fail --show-error --silent "https://${SWINGPOP_LOBBY_DOMAIN}/healthz"
echo
pgrep -a -f 'SwingPop(Server|Lobby)\.x86_64' || true
