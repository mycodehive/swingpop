# M18 Relay / NAT Traversal Prototype Architecture

## Decision

M18 adds a replaceable connectivity layer and a real standalone local TCP relay-proxy. It does not claim production NAT traversal.

- Unity: 6000.5.7f1
- Unity Transport: 6.5.0, `WebSocketNetworkInterface`
- Default development mode: `Direct`
- Prototype Relay provider: `local-relay-proxy`
- Production Relay: not configured
- Cross-NAT/WAN: not verified

Unity's current guidance for Unity 6 is the unified Multiplayer Services package. The repository has neither that package nor a linked UGS project/service credentials, so M18 does not add an untestable cloud dependency. `IMatchConnectivityProvider` is the replacement seam for a future Unity Relay or other provider.

## Boundaries

```text
Authenticated client
  -> Lobby create/join/ready/start
  -> IMatchConnectivityProvider allocation
       Direct: client endpoint == dedicated bind endpoint
       Relay:  client endpoint == relay proxy endpoint
               dedicated bind endpoint remains server-side
  -> Relay credential validation
  -> Authentication
  -> MatchJoinTicket or ReconnectTicket admission
  -> existing dedicated authoritative Hole01 simulation
```

Relay is transport infrastructure only. It does not authenticate an account, own a Lobby, allocate `player-a`/`player-b`, validate a shot, simulate the ball, calculate score, or publish snapshots.

## Connectivity DTOs

`MatchConnectivityDescriptor` is a serializable network DTO containing protocol version, Direct/Relay mode, provider identifier, client-visible endpoint, allocation identifier, scoped credential, and expiry. `ServerConnectivityDescriptor` contains only the dedicated bind endpoint.

In Relay mode the Lobby grant is created from the client descriptor, so the private server endpoint is not serialized to the client. The server-only reservation file contains the bind endpoint.

## Credential separation

1. Relay credential: permission to use the connectivity allocation; reusable until expiry so reconnect is possible.
2. `MatchJoinTicket`: one-time, authenticated-account and GameMatch-bound initial player-slot admission.
3. `ReconnectTicket`: rotating, account-bound recovery of the existing MatchPlayerId.

The server accepts them in that order. A relay failure occurs before authentication/admission and cannot consume a `MatchJoinTicket`.

## Local relay proxy

`LocalRelayProxyRuntime` runs in a separate headless process. It accepts TCP clients and transparently forwards the UTP WebSocket byte stream to the dedicated server bind endpoint. It writes safe telemetry containing only state, connection count, byte count, and time. It never logs the relay credential or join/reconnect ticket.

This proves actual proxy traffic and adapter integration on one machine. It does not emulate public Internet routing, CGNAT, symmetric NAT, region routing, DDoS protection, provider encryption, or production allocation APIs.

## Allocation lifecycle

```text
Allocating -> RelayReady -> ServerReady -> InUse -> Released
                         \-> Failed
```

- Allocation count and port range are bounded by `ConnectivityDevelopmentSettings`.
- Relay and dedicated server must each create a ready marker.
- Relay is released if dedicated startup fails.
- Relay and dedicated child processes monitor the parent Lobby allocator PID.
- Relay also has a bounded credential/lifetime expiry.
- Immediate per-match completion callbacks are not implemented; local resources remain until parent exit or relay lifetime expiry.

## Failure and retry

- Allocation and server-ready waits have bounded data-backed timeouts.
- UTP connection attempts and M15 reconnect remain bounded.
- `ConnectivityRetryPolicy` clamps attempts and delay for a future provider implementation.
- Failed allocation returns the existing Lobby `AllocationFailed` path; no partial grant is issued.
- No cloud region failover exists in M18.

## Security boundary

- Reservation files store SHA-256 hashes of join tickets and relay credentials, not plaintext secrets.
- Logs, captures, telemetry, and documentation use fingerprints only.
- Dedicated admission still validates authentication, account ownership, and the correct ticket.
- The local relay is transparent localhost TCP. It does not add TLS or end-to-end encryption.
- Development Lobby/auth credentials and connectivity descriptors are not a production security design.

## Gameplay isolation

No Relay dependency was added to Ball, ShotFlow, HoleFlow, Camera, Character, HUD core, VFX, or Audio. `MatchSessionController` only passes an abstract descriptor to the transport adapter before connection. Existing authoritative gameplay and snapshot/hash behavior remain unchanged.

