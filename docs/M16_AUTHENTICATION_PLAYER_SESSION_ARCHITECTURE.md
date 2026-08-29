# M16 Authentication / Player Session Architecture

## Scope and Decision

M16 adds a development authentication and player-session boundary above the M15 reconnect credential. It does not add a production identity provider, login UI, password store, lobby, matchmaking, relay, profile, economy, or any M17 feature.

The implementation keeps M12-M15 authority, transport, snapshot, shot, and reconnect contracts. Dedicated match admission now requires a verified authentication session before a slot can be assigned.

## Identity Layers

```text
PlayerAccountId        stable development account identity
        |
AuthSessionId          one authenticated login/session
        |
ConnectionId           one UTP connection owned by the server
        |
MatchPlayerId          player-a or player-b inside one match
        |
ReconnectTicket        rotating bearer credential for that reserved slot
```

These IDs are distinct value types or transport concepts. `MatchSnapshot` continues to contain `MatchPlayerId`, not account identity or credentials. The server owns every binding.

| Layer | Authority | Lifetime | Stored where |
|---|---|---|---|
| `PlayerAccountId` | Authentication provider | Longer than a match | Development token claim and server session metadata |
| `AuthSessionId` | Authentication provider / server registry | Configured session lifetime | Server memory; short fingerprint on F2 |
| Connection ID | Unity Transport | One network connection | Server transport memory |
| `MatchPlayerId` | Dedicated match server | One match slot | Authority/snapshot and ownership registry |
| `ReconnectTicket` | Dedicated match server | Match/grace generation | Client development handoff file; hash only on server |

## Authentication Components

- `IAuthenticationService` validates a credential without knowing gameplay.
- `DevelopmentAuthenticationProvider` issues and validates development-only HMAC-SHA256 credentials using a runtime 256-bit key.
- `AuthenticatedConnectionRegistry` creates bounded server-side auth sessions, binds connection/account/session, enforces one active connection per account, releases a disconnected binding, and exposes revocation.
- `MatchPlayerOwnershipRegistry` binds one verified account to one `MatchPlayerId`.
- `AuthenticationController` is a thin client adapter that loads an explicit credential file and exposes safe status to presentation.
- `DedicatedServerMatchTransport` orders authentication, admission, ownership, shot authorization, and reconnect authorization.

The runtime server key is passed with `-swingpopAuthKeyFile=<path>`. It is not serialized in a Scene, ScriptableObject, source constant, capture, or repository file. The M16 credential generator writes the key and credentials under the OS temporary directory and logs only paths/fingerprints.

## Development Token

The development credential is:

```text
base64url(Json claims) + "." + base64url(HMAC-SHA256(payload))
```

Claims are `TokenVersion`, issuer, `PlayerAccountId`, `AuthSessionId`, issued-at, expiry, and nonce. Validation checks format, size, MAC using fixed-time comparison, version, issuer, ID validity, nonce, future-issued sanity, and expiry. This format is a local development mechanism, not a production access-token recommendation.

Defaults in `M12MultiplayerDevelopmentSettings.asset`:

- Token lifetime: 900 seconds
- Server auth-session cap: 1,800 seconds
- Authentication handshake timeout: 8 seconds
- Duplicate policy: one active match connection per account

## Connection and Match Admission

```text
UTP connect
  -> Unauthenticated
  -> AuthRequest(credential, client nonce)
  -> server MAC/claims/session validation
  -> AuthAccepted(account, auth session, expiry)
  -> ClientHello or ReconnectRequest
  -> match admission / ownership validation
  -> PlayerAssigned or ReconnectAccepted
```

A client-provided plain account ID is never accepted as identity. The server obtains account/session values only from validated claims. Authentication success and match admission success are separate: a third unique account can receive `AuthAccepted` and then `MatchFull`.

Before authentication, the dedicated server accepts only `AuthRequest`, optional `Ping`, and `DisconnectNotice`. `ClientHello`, `ReconnectRequest`, `ShotSubmission`, results, and snapshots are rejected on that connection. Authentication requests are bounded at three per connection and twelve globally per one-second window.

## Match Ownership and Shot Security

```text
live ConnectionId
  -> AuthenticatedConnectionRegistry account/session
  -> peer authenticated account
  -> MatchPlayerOwnershipRegistry owner of payload MatchPlayerId
  -> ConnectionPlayerRegistry binding
  -> existing current-turn / sequence / authority checks
```

Failure in the identity or ownership portion is rejected before gameplay authority is invoked. Account metadata is not inserted into gameplay DTOs.

## Reconnect Ownership

M16 reconnect requires two independent proofs:

1. A newly established, currently valid authentication credential.
2. The M15 rotating reconnect ticket for the reserved match slot.

`ReconnectSessionRegistry` stores the original `OwnerAccountId` beside the server-side ticket hash. A valid ticket presented by another authenticated account is rejected with `AccountOwnershipMismatch`. A valid same-account reconnect restores the same `MatchPlayerId`, rotates only the reconnect ticket generation, and preserves the `AuthSessionId` unless the provider issued another session.

## Expiry and Revocation Policy

- New admission and reconnect always require a credential that is valid at authentication time.
- Development tokens are deliberately longer than a normal acceptance test.
- A token that expires after an already-bound connection entered a match does not interrupt an in-flight shot and is not refreshed in M16.
- Disconnect/reconnect after token expiry fails authentication.
- Revoking an `AuthSessionId` removes its live server registry binding. Subsequent shot authorization and reauthentication fail; production-grade push logout/kick UX is future work.
- Mid-match refresh, refresh tokens, persistent revocation storage, and server-crash restoration are explicitly deferred.

## Message Direction Matrix

| Message | Client -> Server | Server -> Client | Allowed before auth |
|---|---:|---:|---:|
| `AuthRequest` | Yes | No | Yes |
| `AuthAccepted` | No | Yes | N/A |
| `AuthRejected` | No | Yes | N/A |
| `ClientHello` | Yes | No | No on M16 dedicated server |
| `ReconnectRequest` | Yes | No | No |
| `ShotSubmission` | Yes | No | No |
| `PlayerAssigned` / `ReconnectAccepted` | No | Yes | N/A |

M16 increments `OnlineProtocol.CurrentVersion` to 3 because the dedicated connection handshake order changed. Historical M12-M15 validators accept the current version while retaining their own structural gates.

## Debug and Privacy Boundary

F2 may show authentication state, a development account label/short fingerprint, auth-session fingerprint, expiry, match player, and reconnect generation. It must not show the credential, HMAC key, reconnect secret, email, or personal information. Test accounts are generated names such as `dev-player-a`; real personal data is prohibited.

## Replacement Seam

A production provider can replace `IAuthenticationService` and the development credential-loading adapter. Gameplay authority, match snapshots, `MatchPlayerId`, ball physics, camera, HUD, VFX, and scoring do not depend on token format. Production identity, secure OS storage, TLS, backend session persistence, refresh, auditing, and abuse protection remain NO-GO.

