# M15 Match Lifecycle / Reconnect Manual Review

자동 테스트와 실제 교체 프로세스 검증은 완료됐습니다. 아래는 Unity 초보자가 화면 품질, 특수 lie 복원, 거부 경로까지 직접 확인하는 33단계 절차입니다. 테스트 중 생성되는 ticket 파일은 OS 임시 폴더에만 두고 공유하거나 커밋하지 마세요.

1. Unity Hub의 `Projects`에서 SwingPop을 열고, 상단 `File > Open Scene`에서 `Assets/_Game/Scenes/Hole01_SkyIsland.unity`를 엽니다.
2. Project 창에서 `Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset`을 선택합니다.
3. Inspector에서 `Mode = Offline Single`, `Reconnect Grace Seconds = 30`, `Reconnect Attempt Limit = 3`인지 확인합니다. 만료 테스트만 3초 override를 사용합니다.
4. 상단 `SwingPop > Online > Validate M15 Match Lifecycle`을 누르고 `Window > General > Console`에서 `M15 MATCH LIFECYCLE VALIDATION PASS`를 확인합니다.
5. `SwingPop > Online > M15 > Build Dedicated Server`를 눌러 `Builds/M15Server/SwingPopServer.exe`를 만듭니다.
6. `SwingPop > Online > M15 > Build Client`를 눌러 `Builds/M15Client/SwingPop.exe`를 만듭니다.
7. Windows 시작 버튼을 우클릭해 `터미널`을 열고 `cd C:\Users\Dodari\Documents\GitHub\swingpop`을 입력합니다.
8. 서버 터미널에서 `./Builds/M15Server/SwingPopServer.exe -swingpopServer -batchmode -nographics -swingpopAddress=127.0.0.1 -swingpopPort=7777 -logFile ./Library/M15-manual-server.log`를 실행합니다.
9. 두 번째 터미널에서 `$ticket="$env:TEMP\swingpop-m15-a.json"`을 입력한 뒤 `./Builds/M15Client/SwingPop.exe -swingpopClient -swingpopAddress=127.0.0.1 -swingpopPort=7777 -swingpopReconnectOutput=$ticket -logFile ./Library/M15-manual-a.log`를 실행합니다.
10. 세 번째 터미널에서 `./Builds/M15Client/SwingPop.exe -swingpopClient -swingpopAddress=127.0.0.1 -swingpopPort=7777 -logFile ./Library/M15-manual-b.log`를 실행합니다.
11. A와 B에서 `F2`를 누릅니다. A가 `player-a`, B가 `player-b`, 양쪽 MatchId/current player/snapshot version이 같은지 확인합니다.
12. A의 `YOUR TURN`에서 기존 Power/Impact 입력으로 자연 Rigidbody 샷을 실행합니다.
13. 공이 멈추고 B가 `YOUR TURN`이 되는지 확인합니다. 이때 서버 로그에 stable snapshot과 B turn이 남습니다.
14. 작업 표시줄을 우클릭해 `작업 관리자`를 열고 A에 해당하는 `SwingPop` 프로세스만 선택한 뒤 `작업 끝내기`를 누릅니다. 서버와 B는 종료하지 않습니다.
15. 서버 로그에서 `[M15][Reconnect] player-a disconnected`와 grace 시작을 확인하고, B 화면에서 `WAITING FOR PLAYER`, F2에서 A `ReconnectGrace`를 확인합니다. B의 샷 버튼과 입력이 작동하지 않아야 합니다.
16. 새 터미널에서 `cd C:\Users\Dodari\Documents\GitHub\swingpop`과 `$ticket="$env:TEMP\swingpop-m15-a.json"`을 입력합니다.
17. 같은 터미널에서 `./Builds/M15Client/SwingPop.exe -swingpopClient -swingpopAddress=127.0.0.1 -swingpopPort=7777 -swingpopReconnectFile=$ticket -swingpopReconnectOutput=$ticket -logFile ./Library/M15-manual-a-reconnected.log`를 실행합니다. 이것은 기존 객체 재사용이 아닌 새 프로세스입니다.
18. 새 A의 F2에서 `Local: player-a`, `Reconnect: Reconnected`, `Generation: 2`를 확인합니다.
19. A와 B에서 대기 문구가 사라지고 같은 snapshot version/hash로 복원되는지 확인합니다. A의 ball position, lie, stroke, penalty, holed와 현재 B turn이 종료 전 authoritative state와 같아야 합니다.
20. B에서 자연 샷을 실행해 양쪽이 동일한 다음 snapshot과 A turn으로 진행하는지 확인합니다.
21. 별도 매치를 시작해 A의 공을 Green까지 진행시키고 Putter가 선택된 상태에서 A를 종료합니다. 새 A로 재접속한 뒤 `Lie = Green`, Putter, 공 위치, Cup framing이 유지되는지 확인합니다.
22. 다시 별도 매치에서 A가 Water penalty를 받은 stable turn 경계에서 종료합니다. 재접속 후 penalty count, last-valid ball position, lie와 stroke가 서버 snapshot대로 복원되는지 확인합니다.
23. 서버와 B는 유지한 채 ticket 임시 파일의 `secret` 한 글자를 복사본에서 바꾸고 그 복사본으로 새 클라이언트를 실행합니다. `ReconnectRejected: InvalidTicket`이어야 하며 원본은 보존하세요.
24. 정상 재접속으로 generation 2 ticket을 받은 뒤, 재접속 전에 따로 복사해 둔 generation 1 ticket으로 또 접속합니다. 이전 ticket은 replay로 거부되어야 합니다.
25. 정상 A가 이미 연결된 상태에서 현재 ticket 복사본으로 추가 클라이언트를 실행합니다. `PlayerAlreadyConnected`로 거부되고 기존 A 연결은 유지되어야 합니다.
26. 서버를 `-swingpopReconnectGrace=3` 옵션으로 새로 실행하고 A/B를 연결한 뒤 A를 종료하되 재접속하지 않습니다. 약 3초 후 B에서 `RECONNECT FAILED` 또는 종료 상태를 확인합니다.
27. B의 F2에서 match `Aborted / TurnComplete`, A `Expired`를 확인합니다. 서버는 final snapshot을 보낸 뒤 `Ended`가 되어 turn deadlock이 없어야 합니다.
28. 만료된 ticket으로 다시 접속해 `ExpiredTicket`, `SlotExpired`, 또는 `MatchEnded` 중 현재 lifecycle에 맞는 typed reject가 오는지 확인합니다.
29. 모든 프로세스를 종료하고 같은 포트로 서버와 A/B를 다시 실행합니다. 새 MatchId가 생기고 이전 ticket/state가 재사용되지 않아야 합니다.
30. 새 매치가 진행 중일 때 서버 프로세스를 종료합니다. A/B가 `ConnectionLost`/`Disconnected`로 전환되는지 확인합니다. 서버 crash persistence는 M15 범위가 아니므로 매치가 살아남는다고 기대하지 않습니다.
31. 새 서버와 A/B를 다시 실행한 뒤 ticket 없는 세 번째 클라이언트를 실행합니다. 기존 `MatchFull` 정책이 유지되어야 합니다.
32. Unity에서 `Window > General > Test Runner`를 열고 EditMode와 PlayMode 탭에서 각각 `Run All`을 누릅니다. Console 및 `Library/M15-manual-*.log`에 Error, Exception, NullReference, plaintext secret이 없는지 확인합니다.
33. `Window > Analysis > Profiler`를 열고 CPU Usage와 Memory 모듈을 켭니다. A 종료→grace→새 A 재접속을 반복해 중복 `ReconnectController`/transport, 계속 증가하는 coroutine·GC allocation·session history가 없는지 확인합니다.

## Decision

- M15 MATCH LIFECYCLE FOUNDATION: GO
- REAL CLIENT PROCESS RECONNECT: VERIFIED
- STATE RESTORE AFTER RECONNECT: VERIFIED
- GRACE EXPIRY / MATCH ABORT: VERIFIED
- PRODUCTION ONLINE MULTIPLAYER: NO-GO

프로덕션 판정은 인증, 안전한 저장, Relay/NAT, 서버 상태 지속성, WAN/soak/chaos 검증이 없으므로 NO-GO입니다.
