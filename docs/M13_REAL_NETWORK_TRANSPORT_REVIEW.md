# M13 Real Network Transport Review

## Decision

- `M13 REAL NETWORK PROTOTYPE: GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

The prototype quality gate passed on localhost. Production remains NO-GO because relay/NAT, authentication, reconnect, dedicated authority, hardened security, service lifecycle, and WAN/LAN testing are absent.

## Automated Evidence

- EditMode: 170 passed, 0 failed (M12 baseline 158 + M13 12)
- M13 socket PlayMode integration: 10 passed, 0 failed
- Full PlayMode target: 25 tests (M12 baseline 15 + M13 10)
- M13 validator: PASS
- Development build: `Builds/M13/SwingPopM13.exe`
- Independent localhost process test: PASS, both exit code 0
- Capture set: `docs/review-captures/m13-network-prototype/`
- Unity Profiler: NOT VERIFIED

## Beginner Manual Two-Process Test

1. Open Unity Hub.
2. Click **Projects** on the left.
3. Open the SwingPop project at `C:\Users\Dodari\Documents\GitHub\swingpop` with Unity **6000.5.7f1**.
4. In the Project window, double-click `Assets > _Game > Scenes > Hole01_SkyIsland`.
5. Open **Window > General > Console**.
6. Click **Clear** in the Console toolbar.
7. In the Project window, select `Assets > _Game > ScriptableObjects > Online > M12MultiplayerDevelopmentSettings`.
8. In the Inspector, confirm **Mode = Offline Single**, **Host Address = 127.0.0.1**, **Port = 7777**, and **Connection Timeout Seconds = 8**.
9. Choose **SwingPop > Online > Build M13 Network Prototype**.
10. Choose **SwingPop > Online > Validate M13 Network Prototype** and confirm the Console contains `M13 NETWORK PROTOTYPE VALIDATION PASS`.
11. Open **File > Build Profiles** (called **Build Settings** on some layouts).
12. Confirm `Hole01_SkyIsland` is scene 0 and `Foundation` is scene 1; do not reorder them.
13. Close Build Profiles without changing scenes.
14. Choose **SwingPop > Online > M13 > Build Development Prototype**.
15. Wait for `M13 DEVELOPMENT BUILD PASS`; the executable is `Builds\M13\SwingPopM13.exe`.
16. Start the host in the Editor with **SwingPop > Online > M13 > Start Host**.
17. Wait until the turn panel says **HOST WAITING**.
18. Open Windows Terminal or PowerShell from the repository root.
19. Run `Builds\M13\SwingPopM13.exe -swingpopClient -swingpopAddress=127.0.0.1 -swingpopPort=7777`.
20. On both screens press **F2** and confirm Host/Client roles and `InMatch` connection state.
21. Confirm the Editor host is local `player-a`, the build client is local `player-b`, and both show the same Match ID/version/turn.
22. Confirm the host says **YOUR TURN** and the client says **OPPONENT TURN**.
23. Try Space/click on the client; Player B must not start Power/Impact while A owns the turn.
24. On the host, complete Aim/Power/Impact with Space or the shot button.
25. Confirm the host ball does not launch before approval, then both screens play the approved A shot once.
26. Wait for bounce/roll/stop and confirm both screens advance to Player B with matching F2 snapshot version/hash.
27. Confirm host input is disabled and the client now says **YOUR TURN**.
28. On the client, complete Aim/Power/Impact and confirm it waits for host approval.
29. Confirm both screens play the approved B shot once and return to Player A after settling.
30. Compare Player A/B ball position, lie, strokes, penalties, holed flags, current turn, sequence, and snapshot version in F2.
31. During a longer manual round, send one player into Water; confirm only that player receives the stroke/penalty change after host correction.
32. Reach Green and confirm the owning player's club restores as Putter, with ball and Cup visible on that player's turn.
33. Hole Player A and confirm Player B continues if not yet holed.
34. Hole Player B and confirm both screens show **MATCH COMPLETE** with matching results.
35. Press **R** in a network match and confirm the local debug reset does not change authoritative state.
36. Close the client window and confirm the host F2 state becomes **Disconnected** without stale shot callbacks.
37. Stop Editor Play Mode using the top-center Play button.
38. Start Host again, relaunch the client command, and confirm a clean new Match ID/session.
39. Stop both processes, then check the Console: gameplay/network Error count should be 0. Do not count the documented Unity shutdown Curl warning as a verified gameplay success; investigate if it repeats interactively.
40. Open **Window > General > Test Runner**, run **EditMode** and **PlayMode**, and confirm all tests pass.
41. Optional: open **Window > Analysis > Profiler**, attach to one Development Player, record a complete A/B turn, and inspect CPU, GC Alloc, and frame time. This profiler check was not completed by Codex.

## Manual Scope Notes

The automated two-process acceptance used a development-only command-line probe and host gameplay force-hole hook to reach HoleComplete quickly. Steps 31–34 remain important manual natural-gameplay checks. Do not present localhost results as LAN or WAN verification.
