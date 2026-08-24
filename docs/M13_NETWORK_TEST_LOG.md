# M13 Network Test Log

## Date

2026-08-25 KST (wire logs recorded in UTC on 2026-08-24)

## Host Environment

- Windows 11 x64
- SwingPop Development Player built by Unity 6000.5.7f1
- Process mode: `-swingpopHost`

## Client Environment

- Windows 11 x64, same machine, independent process
- Same Development Player artifact
- Process mode: `-swingpopClient`

## Address

`127.0.0.1` (localhost only; LAN and WAN were not tested)

## Port

- Final automated verification run: `18781`
- Review capture run: `18780`
- Product default: `7777`

## Protocol

- SwingPop envelope version: 2
- Unity Transport: 6.5.0
- Pipeline: Fragmentation → Reliable Sequenced
- Maximum envelope: 65,536 bytes

## Connection Success

PASS. Two independent `SwingPopM13.exe` processes connected over the real UTP socket. Host entered Listening/Handshaking/InMatch. Client received host-assigned `player-b`. Both exited with code 0.

## A Shot

PASS. Player A submitted sequence 1. Host approved it once. Both processes logged snapshot version 3 / `ShotPlaying` with hash `E069E934135DFDED` in the final verification run.

## B Shot

PASS. After host result advanced turn to B, Player B submitted sequence 2. Host approved it once. Both processes logged snapshot version 6 / `ShotPlaying` with hash `68CF0820AC52394E`.

## Snapshot

PASS. Final authoritative snapshot was version 7, `HoleComplete / TurnComplete`, with identical host/client hash `1199ADBE346937A9`. Matching-version hash telemetry reported zero desyncs.

## Disconnect

PASS. Client shut down cleanly; host logged `DISCONNECTED reason=Local shutdown`, cleared connection state, and exited normally. PlayMode restart validation also passed.

## Errors

- Gameplay/network exceptions: none in the evidence run.
- An earlier host player log contained Unity runtime `Curl error 23` during shutdown/log output. It did not recur in the final verification run and did not affect UTP messages, snapshots, exit code, or the client. It remains recorded as a non-gameplay runtime warning to watch.
- Unity Profiler: NOT VERIFIED.
- LAN/WAN: NOT VERIFIED.

## Result

`REAL TWO-PROCESS TEST: VERIFIED (LOCALHOST)`

`M13 REAL NETWORK PROTOTYPE: GO`

The automated test intentionally used the existing host gameplay `TryCompleteHole` debug route after each approved playback so the two-player HoleComplete state could be reached quickly. This is not evidence of manual full-course play or natural cup capture.

Transport totals from the evidence run:

- Host: TX 9,542 bytes, RX 2,620 bytes, RTT 1 ms
- Client: TX 2,501 bytes, RX 9,542 bytes, RTT 1 ms
- These totals include handshake, snapshots, ping/pong, test flow, and reliable envelope overhead.
