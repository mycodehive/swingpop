# M16 Authentication Manual Review

This review uses development-only credentials. Do not paste a token or key into Console, chat, screenshots, or a committed file.

1. Close running SwingPop player processes.
2. Open the project with Unity Hub using Unity 6000.5.7f1.
3. In Project, open `Assets > _Game > Scenes > Hole01_SkyIsland`.
4. Choose `SwingPop > Online > Build M16 Authentication Foundation`.
5. Choose `SwingPop > Online > Validate M16 Authentication` and confirm the Console contains `M16 AUTHENTICATION VALIDATION PASS`.
6. In Hierarchy, select `M12 Online Foundation`.
7. In Inspector, confirm one `Authentication Controller`, one `Reconnect Controller`, one `Match Session Controller`, and one `Dedicated Server Match Transport` exist.
8. Select `Assets > _Game > ScriptableObjects > Online > M12MultiplayerDevelopmentSettings`.
9. Confirm Development Authentication Enabled is checked, token lifetime is 900, session lifetime is 1800, and timeout is 8.
10. Confirm no signing-key or credential text field exists in that asset.
11. Choose `SwingPop > Online > M16 > Generate Development Credentials`.
12. In Console, note the temporary directory path; do not open or display credential contents during a recording.
13. Choose `SwingPop > Online > M16 > Build Dedicated Server`.
14. Choose `SwingPop > Online > M16 > Build Client`.
15. Confirm `Builds/M16Server/SwingPopServer.exe` and `Builds/M16Client/SwingPop.exe` exist.
16. Start the server from PowerShell with `-swingpopServer -batchmode -nographics -swingpopAuthKeyFile=<temp key path>` and an unused `-swingpopPort=<port>`.
17. Start Client A with `-swingpopClient`, the same port, and `-swingpopAuthCredentialFile=<client-a path>`.
18. Press F2 in Client A and confirm Auth State becomes Authenticated; verify no token text is shown.
19. Start Client B with the Client B credential and confirm it becomes Authenticated and the match starts.
20. Confirm A is `player-a`, B is `player-b`, and each can take its natural turn without identity/spoof rejection.
21. While A remains connected, start another client with Client A's credential and confirm `SessionConflict`/authentication failure.
22. While A/B fill the match, start Client C normally and confirm authentication succeeds but admission reports `MatchFull`.
23. Stop Client A, keep B open, and confirm B shows reconnect grace while the match is suspended.
24. Start Client C with A's reconnect-ticket file and confirm `AccountOwnershipMismatch`; then start a new Client A with the same ticket and confirm it restores `player-a` with a rotated generation.
25. Stop all player processes, return to Unity, and confirm there are no new Console errors, Missing Scripts, or missing Inspector references. Delete the temporary credential directory when it is no longer needed.

Do not interpret a successful development-provider review as production authentication approval.

