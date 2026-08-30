# M17 Lobby / Match Creation Foundation Architecture

## Goals

- 인증된 두 사용자가 별도 Lobby control plane에서 방을 생성·조회·참가하고 Ready를 동기화한다.
- Owner의 Start 한 번으로 로컬 Dedicated Match Server 한 개를 할당한다.
- 서버가 발급한 account-bound `MatchJoinTicket`으로만 예약된 `player-a`/`player-b` slot에 최초 입장한다.
- 입장 뒤에는 M12~M16 Dedicated Authority, Snapshot, Shot approval, reconnect 구조를 그대로 사용한다.

## Non-Goals

Automatic/skill matchmaking, invite/friends/chat, production auth, public cloud Lobby, Relay/NAT, TLS, DB persistence, region routing, production allocator, spectator, ranking, economy, multi-hole은 M17 범위가 아니다.

## Lobby vs Gameplay Authority

Lobby는 방 metadata, membership, readiness, capacity, start coordination과 admission 발급만 소유하는 control plane이다. Shot, Ball physics, turn resolve, score, penalty, cup, gameplay snapshot은 알지 못한다.

Dedicated Match Server는 player slot, turn, shot approval, Rigidbody simulation, score, snapshot과 M15/M16 reconnect를 소유하는 gameplay/data plane이다. Owner는 방 시작 권한만 가지며 gameplay host가 아니다.

## Identity

```text
PlayerAccountId
  -> AuthSessionId
  -> LobbyPlayerSession
  -> LobbyMatchId membership
  -> MatchReservation / MatchJoinTicket
  -> authenticated match ConnectionId
  -> server-assigned MatchPlayerId
```

`LobbyMatchId`와 gameplay `MatchId`는 별도 ID이며 reservation이 mapping한다. Client payload가 owner/account/player slot을 결정하지 않는다.

## Lobby Session

`LobbyPlayerSession`은 검증된 `PlayerAccountId`, `AuthSessionId`, expiry, connected/revoked 상태만 가진다. Gameplay `MatchSessionController`와 별도이며 Lobby 요청마다 인증 유효성을 다시 확인한다.

## Lobby Match

`LobbyMatchSnapshot`은 `LobbyMatchId`, display name, 2인 capacity, Lobby state, `hole-01`, 생성 시각, visibility, joinable, revision, 선택적 GameMatchId, member snapshot으로 구성된다. 상태는 `Created`, `WaitingForPlayers`, `Full`, `Starting`, `InGame`, `Completed`, `Closed`로 gameplay `MatchPhase`와 분리된다.

방 이름은 1~32자, control character 금지다. M17은 public/joinable 목록만 UI에 노출하며 계정 ID와 credential은 목록 DTO에 넣지 않는다.

## Match Registry

`ILobbyService`가 backend 교체 seam이다. 현재 `InMemoryLobbyService`는 단일 process, bounded registry이며 전체 mutation을 하나의 lock 안에서 수행한다. Account 하나당 active Lobby membership 하나만 허용하고, final slot join validation과 insertion도 동일 critical section에서 처리한다.

## Create

인증된 요청만 처리한다. Server는 protocol, request ID, room name, 정확히 2명, 지원 hole, room limit, 기존 membership을 검증한 뒤 creator를 slot 0/owner로 추가한다. Client-supplied OwnerAccountId는 없다.

## List

인증된 client에게 public이며 joinable인 snapshot만 반환한다. M17은 bounded simple list이며 pagination/filter 확장 seam은 production 과제로 남긴다. 매 frame serialize하지 않고 사용자 요청 및 변경 event에만 전송한다.

## Join

존재, state, capacity, duplicate membership, 다른 active room 여부를 server가 확인한다. 성공 시 server가 다음 slot을 배정한다. 두 요청이 마지막 한 자리를 동시에 요청해도 lock으로 정확히 한 요청만 성공한다.

## Leave

Start 전 일반 member가 나가면 남은 member의 Ready를 `NotReady`로 reset한다. Owner가 나가면 room을 `Closed`로 만들고 다른 member에게 update한다. Owner migration은 없다. `Starting` 이후 Lobby Leave는 거부하며 실제 gameplay disconnect/reconnect는 M15/M16 경로가 처리한다.

## Ready

각 authenticated member는 자신의 Ready만 변경할 수 있다. Membership 변경 시 remaining member의 Ready도 reset하여 오래된 all-ready 상태가 start 조건을 통과하지 않게 한다.

## Start

정확히 2명, 모두 Ready, Owner 요청, `Full` state일 때만 Start한다. Request ID와 기존 reservation을 보관하여 중복 Start가 여러 GameMatch/process를 만들지 않는다. Start lock 이후 신규 Join/Leave는 거부한다.

## Game Server Allocation

`IGameServerAllocator`가 allocation seam이다. M17의 `DevelopmentGameServerAllocator`는 localhost port range와 active allocation 수를 제한하고 Windows development match server process를 숨김/headless 모드로 실행한다. Reservation file 생성 후 ready marker를 timeout 안에 확인해야 allocation이 성공한다. 이는 cloud orchestration이 아니다.

## Match Reservation

`MatchReservation`은 LobbyMatchId, 새 GameMatchId, localhost endpoint, expiry와 두 account별 admission grant를 가진다. Dedicated process로 넘기는 JSON에는 plaintext ticket 대신 SHA-256 hash, account, reserved MatchPlayerId, expiry만 저장한다.

## MatchJoinTicket

`MatchJoinTicket`은 최초 입장 전용이며 GameMatchId에 bound되고, 특정 authenticated account만 사용할 수 있고, 짧은 TTL과 one-time consume를 가진 256-bit random secret이다. Lobby debug/log와 reservation file에는 원문을 기록하지 않는다. M15 `ReconnectTicket`과 재사용하거나 혼동하지 않는다.

## Admission

Dedicated Server에서 순서는 Auth handshake -> authenticated account lookup -> MatchAdmissionRequest -> GameMatch/account/ticket hash/expiry/replay 검증 -> reserved MatchPlayerId assignment다. Admission 성공 전 Shot/Result/Reconnect payload는 허용되지 않는다. Reservation이 없는 M16 직접 개발 모드는 기존 connection-order admission을 유지한다.

## Reconnect Separation

최초 접속은 `AuthSession + MatchJoinTicket`, 접속 복구는 `AuthSession + ReconnectTicket`이다. Lobby credential은 gameplay reconnect 권한이 아니며, M15/M16의 same-account slot ownership, grace, ticket rotation을 그대로 사용한다.

## Scene Transition

Client는 개인 `LobbyAdmissionGrantedMessage`를 받으면 `MatchAdmissionHandoff`에 자신의 grant만 한 번 보관하고 `Hole01_SkyIsland`를 load한다. `MatchSessionController`가 handoff를 consume해 endpoint와 ticket을 transport에 설정한다. Dedicated process는 reservation file의 GameMatchId와 admission registry로 시작한다.

## Flow Diagram

```text
Client A             Lobby Service             Client B
   | Create                 |                       |
   |----------------------->|                       |
   |<------ room update ----|<--------- List ------|
   |                        |<--------- Join ------|
   |<------ 2/2 update -----|------ 2/2 update --->|
   | Ready ---------------->|<-------- Ready -------|
   | Start ---------------->|                       |
   |                        | allocate exactly one  |
   |                        v                       |
   |                 Dedicated Server              |
   |                        |                       |
   |<-- endpoint/ticket ----|---- endpoint/ticket ->|
   |                        |                       |
   +------ authenticated connect + ticket ---------+
                            |
                    player-a / player-b
```

## Security Boundary

M17이 제공하는 것은 auth-only Lobby access, server-owned membership/owner checks, 2인 capacity와 atomic join, account/match-bound expiring one-time join ticket, authenticated dedicated admission이다. M16 development HMAC provider, localhost WebSocket transport, in-memory registry와 local process allocator이므로 TLS, WAN threat model, secure secret storage, distributed revocation/persistence, DDoS 방어는 제공하지 않는다.

## Authorization Matrix

| Operation | Authenticated browser | Room member | Owner | Dedicated server |
|---|---:|---:|---:|---:|
| Create/List | Yes | Yes | Yes | No |
| Join public room | Yes | No active room | No active room | No |
| Leave/Set own Ready | No | Yes | Yes | No |
| Start/Close room | No | No | Yes | No |
| Allocate reservation/tickets | No | No | Trigger only | Via Lobby allocator |
| Validate ticket/assign MatchPlayerId | No | No | No | Yes |
| Shot/physics/score/snapshot | No | No | No | Yes |

## Message Direction Matrix

| Direction | Allowed messages |
|---|---|
| Client -> Lobby | AuthRequest, Create/List/Join/Leave/SetReady/Start/Get/Close, Ping/Pong, Disconnect |
| Lobby -> Client | AuthAccepted/Rejected, MatchList, MatchUpdated, AdmissionGranted, OperationRejected, Ping/Pong, Disconnect |
| Client -> Dedicated | AuthRequest, MatchAdmissionRequest, ReconnectRequest, ShotSubmission, PredictedResult, Ping/Pong, Disconnect |
| Dedicated -> Client | Auth/Admission/Reconnect responses, MatchStarted, ShotApproved/Rejected, Snapshot, Lifecycle, Ping/Pong, Disconnect |

`AdmissionGranted`는 Client가 Lobby로 보낼 수 없고, `StartMatch`는 Lobby가 Client 명령인 것처럼 위조해 보낼 수 없다. Dedicated 쪽 기존 message direction guard도 유지한다.

## Production Gaps

- Public cloud/distributed Lobby, persistence, owner migration, pagination, invite/private password
- Production identity provider, TLS, backend-signed admission, secure client credential storage
- Relay/NAT, region routing, production allocator, crash recovery, server reclamation
- Rate limit storage/metrics, abuse/DDoS, load/soak/chaos/WAN tests, observability
- Match complete 후 automatic Lobby return 및 production cleanup lifecycle

