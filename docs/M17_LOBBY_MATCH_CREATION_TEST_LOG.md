# M17 Lobby / Match Creation Test Log

## Environment

| Item | Result |
|---|---|
| Date | 2026-08-30 (Asia/Seoul) |
| Unity | 6000.5.7f1 |
| Unity Transport | 6.5.0 |
| Lobby protocol | 1 |
| Gameplay protocol | 3 (M16 preserved) |
| Lobby endpoint | `127.0.0.1:18817` |
| Match endpoint | `127.0.0.1:19817` |
| Client A account fingerprint | `AB4D39D6` -> `player-a` |
| Client B account fingerprint | `E2B282C4` -> `player-b` |

## Automated Validation

| Validation | Result |
|---|---|
| `SwingPop > Online > Validate M17 Lobby` | PASS |
| M17 EditMode | 24/24 PASS |
| Full EditMode regression | 237/237 PASS |
| M17 PlayMode | 7/7 PASS |
| Full PlayMode regression | 58/58 PASS |
| Lobby Service Windows Development build | PASS, 175,155,209 bytes |
| Match Server Windows Development build | PASS, 176,797,785 bytes |
| Client Windows Development build | PASS, 176,800,837 bytes |

Validator result:

```text
PASS | lobbyProtocol=1 gameplayProtocol=3 scene=PASS registry=PASS auth=PASS capacity=2 atomic=LOCK admission=PASS ticketPlaintext=NO m16=PASS payloadBytes=create:124,list1:387,join:87,ready:101,start:88,grant:376,reservation:947
```

## Real Multi-Process Lobby Flow

실제 별도 process로 Lobby Service, auto-launched Dedicated Match Server, Client A, Client B를 실행했다. Direct in-process Lobby 호출을 real-flow 증거로 사용하지 않았다.

| Step | Evidence / Result |
|---|---|
| Authentication | A/B accepted with separate development credentials |
| Create | `lobby-1a051945770-1`, revision 1 |
| List / Join | B list에서 room 확인 후 join; A/B 모두 2/2 확인 |
| Ready | A/B Ready; Start 직전 revision 4 |
| Start | Owner A 요청; exactly one GameMatch allocation |
| GameMatch | `game-1a051945e79-1` |
| Server ready | `127.0.0.1:19817` ready marker received |
| Reservation secrecy | plaintext ticket 없음; SHA-256 hashes만 저장 |
| Admission | A -> `player-a`, B -> `player-b`; both Hole01 load |
| Gameplay | A natural shot accepted at turn 0; B natural shot accepted at turn 1 |
| Final convergence | A/B turn 2, snapshot version 7, hash `AB92FB997B4A0CA2` |
| Cleanup | A/B와 Lobby process 정상 종료; 검증 후 남은 local match helper process 명시적으로 종료 |
| Error scan | NullReference/Argument/InvalidOperation/IndexOutOfRange/Unhandled patterns 0 |

실제 실행 증거 원본은 `Library/M17/real/20260830-163104/`에 있고, 채택한 A~H 화면은 `docs/review-captures/m17-lobby/`에 저장했다.

## Admission Security Tests

- EditMode: account mismatch, wrong match, expiry, consumed ticket replay를 각각 reject했다.
- PlayMode UTP: valid A/B ticket으로 reserved player assignment와 match start를 확인했다.
- PlayMode UTP: Account C가 A ticket을 사용하면 player assignment 전에 reject했다.
- PlayMode UTP: 이미 consume된 ticket을 재사용하면 reject했다.
- Stolen/replay의 별도 OS-process 재현은 수행하지 않았다. 따라서 해당 두 공격의 증거 수준은 real UTP PlayMode이며 production security test가 아니다.

## Reconnect Regression

M17 initial admission과 M15 reconnect credential은 코드와 테스트에서 분리했다. 전체 237 EditMode/58 PlayMode regression 및 M17 validator의 M12~M16 validator chain은 PASS다. 이번 M17 실제 4-process run에서는 disconnect/replacement-client reconnect 시나리오를 다시 실행하지 않았으므로 실제 M17 reconnect는 수동 확인 대상으로 남는다.

## Payload / Bandwidth

UTF-8 JSON payload sample: Create 124 B, one-room list 387 B, Join 87 B, Ready 101 B, Start 88 B, individual admission grant 376 B, two-player reservation 947 B. Envelope/UTP framing은 제외한 application payload 크기다. Lobby는 request/change 기반 저빈도 control traffic이며 per-frame serialization/traffic은 없다.

## Performance

Registry와 active allocation은 Inspector data로 제한된다. List는 요청 시에만 만들고 gameplay Update/physics를 Lobby service가 실행하지 않는다. Unity Profiler CPU/GC 측정, load/soak 및 WAN bandwidth 측정은 수행하지 않았으므로 **NOT VERIFIED**다.

## Captures

- `A-Lobby-Empty.png`
- `B-Room-Created.png`
- `C-Room-List.png`
- `D-Player-B-Joined.png`
- `E-Both-Ready.png`
- `F-Match-Starting.png`
- `G-Connected-to-Hole01.png`
- `H-Match-Gameplay.png`

각 파일은 서로 다른 SHA-256 hash를 가지며 육안 검수했다.

