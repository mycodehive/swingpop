# M14 Dedicated Authority Manual Review

이 절차는 Unity와 Windows Terminal 사용이 익숙하지 않은 사용자를 기준으로 작성했다. 자동화 실행 인자인 `-swingpopAutomatedDedicatedTest`는 수동 플레이 때 사용하지 않는다.

## 사전 준비

1. Unity Hub를 열고 `Projects`에서 SwingPop 프로젝트를 클릭한다.
2. Unity 상단 메뉴에서 `File > Open Scene`을 클릭하고 `Assets/_Game/Scenes/Hole01_SkyIsland.unity`를 연다.
3. 상단 메뉴에서 `Window > General > Console`을 클릭하고 Console 왼쪽 위 `Clear`를 누른다.
4. Project 창에서 `Assets > _Game > ScriptableObjects > Online > M12MultiplayerDevelopmentSettings`를 클릭한다.
5. Inspector의 기본 `Mode`가 `Offline Single`인지 확인한다. 기본값을 Dedicated Server로 저장하지 않는다.
6. 상단 메뉴 `SwingPop > Online > Validate M14 Dedicated Authority`를 클릭한다.
7. Console에 `M14 DEDICATED AUTHORITY VALIDATION PASS`가 보이는지 확인한다.

## 빌드

8. 상단 메뉴 `SwingPop > Online > M14 > Build Dedicated Server`를 클릭하고 빌드가 끝날 때까지 기다린다.
9. Project 폴더의 `Builds/M14Server/SwingPopServer.exe`가 생겼는지 Windows 파일 탐색기로 확인한다.
10. Unity로 돌아와 `SwingPop > Online > M14 > Build Client`를 클릭한다.
11. `Builds/M14Client/SwingPop.exe`가 생겼는지 확인한다.

## 서버와 두 클라이언트 실행

12. Windows 시작 버튼을 우클릭하고 `터미널` 또는 `Windows PowerShell`을 클릭한다.
13. 아래 명령으로 프로젝트 폴더로 이동한다: `cd C:\Users\Dodari\Documents\GitHub\swingpop`.
14. 서버 명령을 실행한다: `.\Builds\M14Server\SwingPopServer.exe -swingpopServer -batchmode -nographics -swingpopAddress=127.0.0.1 -swingpopPort=7777 -logFile .\Library\M14-manual-server.log`.
15. 새 터미널 탭을 열고 Client A를 실행한다: `.\Builds\M14Client\SwingPop.exe -swingpopClient -swingpopAddress=127.0.0.1 -swingpopPort=7777 -logFile .\Library\M14-manual-client-a.log`.
16. Client A 창에서 `F2`를 누르고 `Local: player-a`와 `Connected`가 보이는지 확인한다.
17. 또 새 터미널 탭을 열고 같은 클라이언트 명령을 실행해 Client B를 연다. 로그 파일 이름만 `M14-manual-client-b.log`로 바꾼다.
18. Client B의 F2 overlay에서 `Local: player-b`가 보이는지 확인한다.
19. 두 창의 `Match` ID, Protocol `v2`, Snapshot version/hash가 같은지 확인한다.

## 샷·턴·결과

20. Client A에 `YOUR TURN`, Client B에 `OPPONENT TURN`이 보이고 B의 Shot 입력이 비활성인지 확인한다.
21. Client A에서 Space를 세 번 사용해 Power/Impact/Shot을 완료한다. 승인 전에는 공이 먼저 발사되지 않아야 한다.
22. 두 창 모두 같은 승인 샷을 재생하고, 서버 로그에 A 승인/결과/snapshot이 기록되는지 확인한다.
23. 공이 멈춘 뒤 Client B가 `YOUR TURN`으로 바뀌고 A 입력이 비활성인지 확인한다.
24. Client B에서도 한 샷을 실행하고 양쪽에서 B의 승인 재생과 다음 snapshot을 확인한다.
25. 샷을 반복하면서 Water에 들어갔을 때 서버 snapshot의 penalty/stroke/recovery 위치가 두 클라이언트에서 같아지는지 확인한다.
26. Green에 도달하면 Putter가 선택되고 공과 Cup이 함께 보이는지 양쪽 창에서 확인한다.
27. 각 플레이어를 Hole-In시키고, 한 플레이어만 holed인 동안 그 플레이어가 다음 턴에서 제외되는지 확인한다.
28. 두 플레이어가 모두 holed이면 두 창 모두 `MATCH COMPLETE`이며 F2의 version/hash가 같은지 확인한다.

## 연결 해제·재시작·오류

29. Client B 창을 닫고 서버 로그에 player-b disconnect가 기록되며 Client A가 `Aborted` snapshot을 받는지 확인한다.
30. 서버 프로세스를 종료하고 남은 Client A에 연결 해제 표시가 나타나는지 확인한다.
31. 서버와 두 클라이언트를 모두 종료한 뒤 14~18단계를 다시 수행해 같은 포트로 재시작 가능한지 확인한다.
32. 세 번째 Client를 하나 더 실행해 `MatchFull`로 거절되고 Player A/B 매치에는 들어오지 않는지 확인한다. 각 로그에서 `Exception`, `NullReference`, `Resetting event queue`가 없는지도 확인한다.
33. Unity에서 `Window > General > Test Runner`를 열어 EditMode와 PlayMode를 각각 `Run All`하고, `Window > Analysis > Profiler`에서 서버 실행 중 CPU/Physics/Memory를 기록한다. 마지막으로 모든 Console/로그의 예기치 않은 Error가 0인지 확인한다.

수동 전체 홀·Water·Putter·Hole-In·Profiler 검증은 자동 테스트를 대체하지 않으며, 자동 테스트 또한 화면 품질/조작감 검증을 대체하지 않는다.

