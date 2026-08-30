# M19 Production Relay / WAN Quality Architecture

## Goals

M19 replaces the M18 localhost-only Relay implementation with an opt-in, production-capable Unity Relay adapter while preserving every authority and admission boundary from M12 through M18. It does not claim that public matchmaking, production Lobby, Cross-NAT, adverse-network quality, or operations are complete.

The default remains `Direct`. The three explicit routes are:

- `Direct`: the M17 dedicated endpoint is client-visible.
- `LocalRelay`: the M18 standalone localhost proxy is used for deterministic development regression.
- `ProductionRelay`: Unity Relay allocation/join and encrypted DTLS transport are used. There is no Direct fallback.

## Provider Decision

Selected provider: Unity Relay through the unified Multiplayer Services SDK.

- Unity Editor: 6000.5.7f1
- `com.unity.services.multiplayer`: 2.3.1, exact direct dependency
- `com.unity.services.authentication`: 3.7.3, resolved transitive dependency
- `com.unity.transport`: 6.5.0, existing project dependency
- Transport endpoint: DTLS by default; WSS is supported by configuration
- Verified Relay region: `asia-northeast1`

Unity's current Unity 6 documentation recommends the unified Multiplayer Services package and marks the standalone Relay package as deprecated. The provider SDK is isolated in `SwingPop.Online.UnityRelayProvider`; the gameplay runtime assembly does not reference `Unity.Services.*`.

Official references:

- <https://docs.unity.com/en-us/relay/relay-unity-sdk-landing>
- <https://docs.unity.com/en-us/mps-sdk/install-and-upgrade>
- <https://docs.unity.com/en-us/relay/relay-and-ngo>
- <https://docs.unity.com/en-us/relay/enable-dtls-encryption>

## Package / Version Boundary

`Packages/manifest.json` pins Multiplayer Services 2.3.1 and `packages-lock.json` records the resolved graph. The generic connectivity DTO/provider contracts live in `SwingPop.Runtime`; only the adapter assembly references Core, Authentication, Environments, and Multiplayer Services.

This keeps Ball, Shot, Terrain, Hole, Character, Camera, HUD, VFX, and Audio independent from the service SDK.

## Credentials / Configuration

No service secret, password, private key, Relay join code, or allocation key is serialized into a Unity asset or committed document.

- Unity Project linking and Relay enablement are project/dashboard configuration.
- UGS client identity uses `SignInAnonymouslyAsync`; this is provider authentication, not SwingPop production account identity.
- `ConnectivityDevelopmentSettings` stores only non-secret environment/region/connection-type tuning.
- Real cloud tests require `-swingpopEnableRealRelayTests` or the explicit Inspector opt-in. The default is disabled.
- The verified project environment is `production`. An initial `development` attempt failed safely because that environment was not configured.

M16 development authentication remains a separate runtime-generated credential boundary. It is not replaced by UGS anonymous authentication.

## Connectivity Flow

```text
Lobby owner Start
  -> UnityRelayConnectivityProvider.PrepareAsync
       -> UnityServices initialize
       -> anonymous provider sign-in
       -> CreateAllocation + GetJoinCode
  -> server-only opaque Relay payload in temporary reservation
  -> dedicated process consumes and deletes reservation
  -> dedicated UTP driver Bind/Listen through Relay
  -> Relay Established
  -> server ready marker
  -> each client receives join code through its MatchAdmissionGrant
  -> JoinAllocation
  -> client UTP driver Connect through Relay
  -> connectivity credential handshake
  -> M16 Authentication
  -> MatchJoinTicket or ReconnectTicket
  -> existing dedicated authoritative gameplay
```

The provider payload contains allocation/key material required by Unity Transport. It is server-only, written to a temporary reservation just before process launch, consumed once, and deleted immediately after load. A client receives the provider join code but never the host payload.

## Dedicated Server Integration

`DedicatedServerMatchTransport` creates a Relay-configured UTP driver, binds `AnyIpv4`, calls `Listen`, and waits for `RelayConnectionStatus.Established`. The Lobby allocator does not publish grants until the dedicated process writes its ready marker after this state.

The dedicated process remains the only match authority. Relay does not assign `player-a`/`player-b`, validate shots, simulate Rigidbody physics, resolve terrain/score, or publish authoritative snapshots.

## Client Integration

`MatchSessionController` joins the provider allocation before starting the UTP client. The resulting opaque payload configures the existing `UnityTransportMatchTransport`. A failed provider initialize, allocation, join, timeout, or endpoint mapping aborts the flow and does not attempt Direct.

## Lobby Integration

M17 Lobby still owns room state and initial reservation ordering. `IMatchConnectivityProvider` supplies the route-specific descriptors only. `LobbyMatchId`, gameplay `MatchId`, room revision, Ready, capacity, and Owner Start behavior are unchanged.

The current Lobby control plane is an in-memory local service bound to the configured development address. It is not publicly hosted, durable, encrypted, or suitable for remote Cross-NAT players. This is the primary full-flow WAN blocker after Relay integration.

## Authentication / Admission

The admission sequence remains:

1. Relay allocation permission / connectivity credential
2. M16 authenticated account/session
3. one-time account-bound `MatchJoinTicket` for initial entry, or rotating account-bound `ReconnectTicket`
4. server-owned `MatchPlayerId`

An allocation or Relay handshake cannot grant a player slot. A provider failure before admission does not consume the one-time match ticket.

## Reconnect

Reconnect rejoins the same Relay allocation, repeats the connectivity and authentication gates, and then submits the current M15 `ReconnectTicket`. On success the server rotates the reconnect ticket generation and restores the latest authoritative snapshot. Relay join data and reconnect secrets remain distinct.

## Failure Handling

- Provider calls have bounded 5-60 second timeouts.
- Retry attempts are clamped to 1-5 with exponential delay capped at 10 seconds.
- Errors cross the SDK boundary as generic configuration/authentication/allocation/connection/timeout categories.
- Safe diagnostics redact token, key, secret, credential, join-code, and authorization values.
- No Direct fallback occurs in ProductionRelay mode.
- Stale descriptor versions, expired credentials, wrong allocations, and malformed payloads are rejected.

## Resource Lifecycle

The local state machine remains `Allocating -> RelayReady -> ServerReady -> InUse -> Released/Failed`. Lobby/server/client processes clear in-memory payloads on shutdown. The temporary host reservation is deleted after the dedicated process consumes it.

Unity Relay allocation lifetime is tied to the Relay host connection. The verified run explicitly stopped the dedicated host and Lobby service after both clients completed, closing the allocation. The current service prepares one cloud allocation per Lobby-service process; automatic replenishment for several simultaneous/serial production matches is not implemented.

## NAT Assumptions

Unity Relay removes the gameplay data-plane requirement for inbound client/server port forwarding because all participants make outbound provider connections. The M19 verification used three local player processes plus a local dedicated process connecting to a real cloud Relay region. It did not use two routers, CGNAT, symmetric NAT, mobile tethering, or a remote public Lobby endpoint.

## WAN Quality Profiles

The quality gate defines four profiles, but M19 only executed the unshaped real-provider baseline:

| Profile | Target | M19 execution |
|---|---|---|
| A Clean | normal Internet, no artificial impairment | Partial: one-machine cloud Relay path |
| B Typical WAN | moderate RTT/jitter/loss | Not run |
| C Bad WAN | high RTT/jitter/loss | Not run |
| D Disconnect/Reconnect | forced disconnect and restore | Provider reconnect verified on one machine; WAN topology not verified |

## Security Boundary

DTLS protects the Unity Transport hop to Relay. It is not application-level end-to-end encryption and does not make the local Lobby/auth system production-ready. Allocation keys and join codes are bearer material and must never be logged or committed. Production identity, secure platform credential storage, backend token issuance/revocation, audit, abuse prevention, DDoS policy, and a public TLS Lobby remain future work.

## Production Gaps

- public production Lobby/control plane and orchestration
- Cross-NAT/CGNAT/symmetric-NAT tests on physically separate networks
- controlled latency, jitter, loss, bandwidth, and disconnect profiles
- 30-minute and repeated allocation leak/soak runs
- Profiler/60 FPS and GC allocation captures during network stress
- concurrent/serial allocation replenishment and explicit service-side match completion release
- production identity, secret storage, monitoring, alerting, cost quotas, and incident handling

