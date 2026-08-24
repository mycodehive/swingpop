# M12 Online Multiplayer Foundation Review

## Decision

- `M12 ONLINE FOUNDATION: GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

M12의 local authority/loopback 범위는 자동 검증을 통과했다. 실제 transport, 인증, reconnect, server-side physics/result verification이 없으므로 production online 판정은 NO-GO다.

## Automated Evidence

- EditMode: 158 passed, 0 failed
- PlayMode: 15 passed, 0 failed
- M12 structure: PASS, Missing Scripts 0, DTO Unity Object references 0
- Final graphics regression validators: 7/7 PASS
- Local 2P capture: 6/6, 1920x1080, `MatchPhase=HoleComplete`
- Capture runtime: GameObjects 575, Cameras 1, Canvases 1
- Capture transport: messages 21, total serialized bytes 14,264, maximum single payload 954 bytes
- Static sample payload: ShotSubmission 560 bytes, MatchSnapshot 830 bytes
- Profiler CPU/GPU/GC capture: not run; manual verification remains.

## Review Captures

- `docs/review-captures/m12-online-foundation/A-Player-A-Turn.png`
- `docs/review-captures/m12-online-foundation/B-Player-A-Shot.png`
- `docs/review-captures/m12-online-foundation/C-Player-B-Turn.png`
- `docs/review-captures/m12-online-foundation/D-Player-B-Shot.png`
- `docs/review-captures/m12-online-foundation/E-Player-A-Restored-Position.png`
- `docs/review-captures/m12-online-foundation/F-Multiplayer-Hole-Result.png`

## Beginner Manual Validation

1. Unity Hub에서 SwingPop 프로젝트를 Unity `6000.5.7f1`로 연다.
2. Unity 하단 `Project` 창에서 `Assets > _Game > Scenes > Hole01_SkyIsland`를 더블클릭한다.
3. 상단 메뉴 `Window > General > Console`을 눌러 Console 창을 연다.
4. Console 왼쪽 위 `Clear`를 누른다.
5. 하단 `Project` 창에서 `Assets > _Game > ScriptableObjects > Online > M12MultiplayerDevelopmentSettings`를 클릭한다.
6. 오른쪽 `Inspector`에서 `Mode = Offline Single`, `Simulated Latency Ms = 0`, `Verbose Logging = 꺼짐`인지 확인한다.
7. Unity 상단 중앙의 삼각형 `Play` 버튼을 누른다.
8. 기존 Hole01이 시작되고 왼쪽 위 M12 턴 패널이 보이지 않는지 확인한다. 이것이 기존 single-player 기본 모드다.
9. Space 또는 오른쪽 아래 `START SHOT`을 사용해 기존 Aim/Power/Impact/launch가 정상 동작하는지 확인한다.
10. Unity 상단 중앙 `Play`를 다시 눌러 Play Mode를 종료한다.
11. 상단 메뉴 `SwingPop > Online > Validate M12 Foundation`을 클릭한다.
12. Console에서 `M12 ONLINE FOUNDATION VALIDATION PASS`를 확인한다.
13. 상단 메뉴 `SwingPop > Online > Run Local 2P Simulation`을 클릭한다. 이 메뉴는 Hole01을 열고 Play Mode를 시작하며 one-way 200 ms latency를 runtime에 적용한다.
14. 왼쪽 위 새 패널에 `YOUR TURN`, `PLAYER A 0`, `PLAYER B 0`이 보이는지 확인한다.
15. Space/버튼으로 Aim -> Power -> Impact를 확정한다.
16. 마지막 확정 직후 공이 즉시 발사되지 않고 approval을 기다리는지 확인한다. 200 ms 왕복 두 구간 때문에 약 400 ms 뒤 승인된다.
17. 승인 후 기존 캐릭터 swing/impact를 거쳐 공이 정확히 한 번 발사되는지 확인한다.
18. 공이 bounce/roll/stop할 때 기존 카메라, VFX, 오디오가 정상 반응하는지 확인한다.
19. A 결과가 처리된 뒤 패널이 `PLAYER B TURN`으로 바뀌고 A stroke가 유지되는지 확인한다.
20. B turn에서 오른쪽 아래 action button이 비활성화되는지 확인한다.
21. B turn에서 Space를 눌러도 Player A용 shot state가 진행되지 않는지 확인한다.
22. 약 1.2초 뒤 simulated B가 command를 제출하고 approval 후 기존 캐릭터/공 경로로 한 번 발사되는지 확인한다.
23. B 결과 처리 후 `YOUR TURN`으로 돌아오고 A의 이전 ball position/lie/stroke가 복원되는지 확인한다.
24. Play Mode 중 `F2`를 눌러 Match ID, protocol, snapshot version, current player, turn, sequence, player별 lie/stroke/penalty/holed와 transport telemetry가 보이는지 확인한다.
25. `F2`를 다시 눌러 overlay가 꺼지는지 확인한다.
26. 여러 턴을 진행해 한 player가 Green이면 그 player 차례에만 Putter가 선택되고 다른 player의 Driver/lie가 섞이지 않는지 확인한다.
27. Water/OOB 결과가 난 player에게만 penalty와 last valid recovery가 적용되고 다른 player의 state가 바뀌지 않는지 확인한다.
28. 한 player가 hole-in한 뒤 그 player turn이 건너뛰어지고 다른 player가 계속 플레이하는지 확인한다.
29. 양쪽 모두 hole-in하면 패널이 `MATCH COMPLETE`가 되고 기존 result presentation이 나타나는지 확인한다.
30. Play Mode를 종료하고 Console의 빨간 Error가 0인지 확인한다.
31. 캡처를 다시 만들려면 Play Mode가 꺼진 상태에서 scene을 저장한 뒤 `SwingPop > Online > Capture M12 Review Set`을 클릭한다.
32. 자동 실행이 끝나면 `docs/review-captures/m12-online-foundation`의 PNG 6개를 확인한다.
33. 테스트는 `Window > General > Test Runner`를 열고 `EditMode` 탭에서 전체 실행한 뒤 `PlayMode` 탭에서도 전체 실행한다.
34. `Window > Analysis > Profiler`에서 CPU Usage와 Memory를 Record한 상태로 A/B 3턴 이상 진행하고 frame spike와 shot당 지속 allocation이 없는지 수동 확인한다.

## Settings

- Development mode/latency/verbose/remote delay/power: `Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset`
- Protocol/range budgets: `Assets/_Game/Scripts/Online/OnlineProtocol.cs`
- Scene wiring: `Assets/_Game/Scenes/Hole01_SkyIsland.unity > M12 Online Foundation`
- Turn panel: `Gameplay HUD > Safe Area > M12 Turn Panel`
- F2 telemetry: `MultiplayerDebugOverlay`

## Known Limitations

- 실제 두 기기, WAN, packet loss/reorder/disconnect/reconnect는 검증하지 않았다.
- Rigidbody lockstep determinism을 제공하지 않는다.
- Local authority가 기존 gameplay 결과를 수락하며 production anti-cheat 수준의 결과 재검증은 없다.
- Profiler CPU/GPU/GC 수치는 자동 수집하지 않았다.
- B는 실제 두 번째 입력 장치가 아니라 simulated remote player다.
