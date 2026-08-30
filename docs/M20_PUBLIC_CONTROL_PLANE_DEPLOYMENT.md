# M20 Public Control Plane Deployment

## Architecture

```text
Client A/B
  |  wss://<staging-domain>/lobby (system trust, no bypass)
  v
Caddy :443 / automatic public certificate
  |-- /lobby   -> 127.0.0.1:18817 (Unity UTP WebSocket)
  `-- /healthz -> 127.0.0.1:18818 (safe counters only)
                         |
                  In-memory ILobbyService
                         |
                bounded process allocator
                         |
              remote Unity dedicated server
                         |
              outbound Unity Relay DTLS/WSS
```

The Lobby is a control plane for authentication gating, rooms, Ready/Start, reservations, allocation coordination, and admission grants. It does not calculate shots, turns, physics, scoring, or gameplay snapshots. The dedicated server remains gameplay authority.

## Target Environment

- One small Ubuntu LTS x86-64 VM is the M20 staging target.
- The VM must have a public DNS name, outbound internet access, and enough CPU/RAM for one Lobby plus the configured maximum of four lightweight development match processes.
- This is a bounded single-VM staging foundation, not production scaling, high availability, or a service SLA.
- The current workstation lacks Unity Linux Build Support, a public VM, DNS credentials, and SSH credentials; public deployment is therefore `NOT VERIFIED` in this change.

## DNS

1. Purchase or use an existing domain.
2. In the DNS provider, create an `A` record such as `lobby-staging.example.com` pointing to the VM public IPv4 address.
3. Wait until `nslookup lobby-staging.example.com` returns that VM.
4. Do not put a public IP, account identity, or secret in committed test logs.

## TLS

Caddy obtains and renews a publicly trusted certificate when the configured domain resolves to the VM and ports 80/443 are reachable. Unity Transport 6.5.0 configures `WithSecureClientParameters(hostname)`, so the platform trust store and hostname validation are required. There is no certificate-validation bypass or self-signed “verified” path.

Official references: [Caddy automatic HTTPS](https://caddyserver.com/docs/automatic-https), [Caddy HTTPS quick start](https://caddyserver.com/docs/quick-starts/https).

## Reverse Proxy

[`deploy/m20/Caddyfile`](../deploy/m20/Caddyfile) exposes only `/lobby` and `/healthz`, proxies WebSocket upgrade traffic to the loopback UTP listener, returns 404 elsewhere, and rotates JSON access logs. It does not configure `trusted_proxies`, and SwingPop never uses `X-Forwarded-For` as identity or authorization evidence.

Caddy WebSocket behavior is documented by [Caddy reverse_proxy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy).

## Lobby Service

The staging service must launch with:

- `-swingpopLobbyService`
- `-swingpopControlPlaneEnvironment=Staging`
- `-swingpopLobbyEndpoint=wss://<domain>/lobby`
- loopback bind address/port
- an external auth key file
- explicit `ProductionRelay` opt-in

Staging rejects plaintext `ws://`, a non-loopback internal bind, and Direct/LocalRelay gameplay routing. Development retains localhost `ws://` and Direct/LocalRelay modes.

## Match Allocator

- Connection, room, active match, payload, handshake, and operation-rate limits are bounded.
- Server-ready means the process is alive, its reservation is loaded, gameplay authority is ready, and the ProductionRelay bind has completed before the ready marker is written.
- Staging match processes are not parent-bound to the Lobby. A Lobby crash therefore does not immediately terminate an active match.
- Every match process receives a bounded maximum lifetime; the allocator also reaps expired or exited processes and releases the exact connectivity allocation and port.
- This process model is not autoscaling or durable orchestration.

## Unity Relay

`ProductionRelay` remains behind `IMatchConnectivityProvider`. The dedicated server hosts the allocation and keeps all gameplay authority. Provider permission, M16 Authentication, M17 one-use `MatchJoinTicket`, and M15 rotating `ReconnectTicket` remain separate ordered checks. Failed Relay setup does not fall back to Direct.

Unity documents Relay support for UDP, DTLS, and WSS in [Relay networking](https://docs.unity.com/relay/networking).

## Configuration

- Repository template: `deploy/m20/staging.env.example`
- Host configuration: `/etc/swingpop/staging.env`
- Current linked UGS environment value: `production` (the M19-verified UGS name; this does not make M20 production-ready)
- Public client argument: `-swingpopLobbyEndpoint=wss://<domain>/lobby`
- Client staging argument: `-swingpopControlPlaneEnvironment=Staging`
- Control-plane internal WS: `127.0.0.1:18817/lobby`
- Health: `127.0.0.1:18818/healthz`
- Direct stays the serialized development default; public staging must explicitly select ProductionRelay.

## Secrets

- Never commit the M16 HMAC key, client credential, Relay join code, reservation file, match ticket, reconnect ticket, SSH key, or provider credential.
- Store the staging HMAC key at `/etc/swingpop/auth-key.txt`, owner `swingpop`, mode `0600`.
- Credentials generated by M16/M17 are development-only and short-lived.
- M20 development HMAC authentication is not a production identity provider, refresh flow, distributed revocation system, or secure platform credential store.

## Firewall

Allow inbound TCP 22 (restricted administrator sources), 80, and 443. Do not expose 18817, 18818, or the match-server port range. ProductionRelay requires outbound provider connectivity and no client port forwarding or public gameplay inbound rule.

## Start/Stop

```bash
sudo systemctl start swingpop-control-plane caddy
sudo systemctl restart swingpop-control-plane caddy
sudo systemctl stop swingpop-control-plane
sudo systemctl status swingpop-control-plane caddy
```

For deployment from Windows PowerShell, after building Linux artifacts:

```powershell
.\deploy\m20\deploy.ps1 -Server <vm-host> -Domain <dns-name> -AuthKeyFile <key-file>
```

## Logs

- Control plane: `/var/log/swingpop/control-plane.log`
- Caddy access: `/var/log/caddy/swingpop-access.log` with rotation
- Match allocation logs: `/var/lib/swingpop/allocations/*.server.log`
- Expected prefixes: `[M20][ControlPlane]`, `[M20][TLS]`, `[M20][Allocation]`, `[M20][WAN]`, `[M20][Soak]`
- Logs must contain fingerprints/counters only, never credentials or tickets.

## Health

`GET https://<domain>/healthz` returns only readiness and bounded counts for authenticated connections, rooms, servers, and allocations. It exposes no endpoint, account, session, IP, ticket, credential, or process command line.

## Cleanup

After every lifecycle, confirm room closure, process exit, Relay allocation release, port release, and removal of temporary reservation/ready files. Run `deploy/m20/status.sh` and inspect the provider dashboard only when access exists. A dashboard not inspected must be recorded as `NOT VERIFIED`.

## Rollback

1. Stop the M20 control-plane service.
2. Restore the previous `/opt/swingpop/lobby` and `/opt/swingpop/server` directories.
3. Restore the previous Caddyfile and systemd unit.
4. Run `systemctl daemon-reload` and restart Caddy/control plane.
5. Keep active match processes bounded by their maximum lifetime; terminate only exact verified SwingPop PIDs if emergency cleanup is required.

## Known Limitations

- Public TLS, Cross-NAT, WAN profiles, 30-minute soak, five real allocation cycles, provider dashboard cleanup, Profiler, and bandwidth are not yet verified.
- Lobby state is in-memory and is lost on restart.
- Development HMAC credentials are staging-only.
- There is no persistent DB, distributed allocator, WAF/DDoS service, production secret manager, autoscaling, monitoring/alerting stack, backup, or multi-region failover.
- `KillMode=process` plus server maximum lifetime is a bounded single-VM staging compromise, not a production supervisor design.
