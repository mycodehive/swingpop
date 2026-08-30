using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace SwingPop.Online
{
    public interface ILocalMatchServerLauncher
    {
        bool TryLaunch(string executablePath, string address, ushort port, string authenticationKeyPath,
            string reservationPath, string readyPath, float timeoutSeconds,
            out int processId, out string failure);
    }

    public sealed class LocalMatchServerProcessLauncher : ILocalMatchServerLauncher
    {
        public bool TryLaunch(string executablePath, string address, ushort port, string authenticationKeyPath,
            string reservationPath, string readyPath, float timeoutSeconds,
            out int processId, out string failure)
        {
            processId = 0;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                failure = "Match server executable was not found.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(authenticationKeyPath) || !File.Exists(authenticationKeyPath))
            {
                failure = "Development authentication key file was not found.";
                return false;
            }
            try
            {
                if (File.Exists(readyPath)) File.Delete(readyPath);
                string arguments = string.Join(" ", new[]
                {
                    "-swingpopServer", "-batchmode", "-nographics",
                    "-swingpopAddress=" + Quote(address),
                    "-swingpopPort=" + port.ToString(CultureInfo.InvariantCulture),
                    "-swingpopAuthKeyFile=" + Quote(authenticationKeyPath),
                    "-swingpopMatchReservationFile=" + Quote(reservationPath),
                    "-swingpopServerReadyFile=" + Quote(readyPath),
                    "-logFile", Quote(Path.ChangeExtension(readyPath, ".server.log"))
                });
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Directory.GetCurrentDirectory()
                });
                if (process == null)
                {
                    failure = "Process.Start returned null.";
                    return false;
                }
                processId = process.Id;
                DateTime deadline = DateTime.UtcNow.AddSeconds(Math.Max(1f, timeoutSeconds));
                while (DateTime.UtcNow < deadline)
                {
                    if (File.Exists(readyPath)) return true;
                    if (process.HasExited)
                    {
                        failure = "Match server exited before the ready handshake.";
                        return false;
                    }
                    Thread.Sleep(50);
                }
                failure = "Match server ready handshake timed out.";
                return false;
            }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "") + "\"";
    }

    /// <summary>Local-development allocator. It is bounded and is not cloud orchestration.</summary>
    public sealed class DevelopmentGameServerAllocator : IGameServerAllocator
    {
        private readonly string executablePath;
        private readonly string address;
        private readonly ushort firstPort;
        private readonly int maximumActiveMatches;
        private readonly long ticketLifetimeMilliseconds;
        private readonly float readyTimeoutSeconds;
        private readonly string authenticationKeyPath;
        private readonly string evidenceDirectory;
        private readonly ILocalMatchServerLauncher launcher;
        private readonly HashSet<ushort> allocatedPorts = new();
        private long sequence;

        public DevelopmentGameServerAllocator(string executablePath, string address, ushort firstPort,
            int maximumActiveMatches, long ticketLifetimeMilliseconds, float readyTimeoutSeconds,
            string authenticationKeyPath, string evidenceDirectory, ILocalMatchServerLauncher launcher = null)
        {
            this.executablePath = executablePath ?? string.Empty;
            this.address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            this.firstPort = firstPort;
            this.maximumActiveMatches = Math.Max(1, maximumActiveMatches);
            this.ticketLifetimeMilliseconds = Math.Max(10_000L, ticketLifetimeMilliseconds);
            this.readyTimeoutSeconds = Math.Max(1f, readyTimeoutSeconds);
            this.authenticationKeyPath = authenticationKeyPath ?? string.Empty;
            this.evidenceDirectory = evidenceDirectory ?? Path.Combine(Path.GetTempPath(), "SwingPop", "M17");
            this.launcher = launcher ?? new LocalMatchServerProcessLauncher();
        }

        public int ActiveAllocationCount => allocatedPorts.Count;
        public DevelopmentMatchAdmissionRegistry LastAdmissionRegistry { get; private set; }
        public string LastReservationPath { get; private set; } = string.Empty;
        public int LastProcessId { get; private set; }

        public bool TryAllocate(LobbyMatchSnapshot match, long nowMilliseconds,
            out MatchReservation reservation, out string failure)
        {
            reservation = null;
            failure = string.Empty;
            if (match == null || match.Members.Length != LobbyProtocol.MatchPlayerCapacity)
            {
                failure = "A full two-player Lobby match is required.";
                return false;
            }
            if (allocatedPorts.Count >= maximumActiveMatches || !TryReservePort(out ushort port))
            {
                failure = "The bounded local match allocation limit was reached.";
                return false;
            }

            MatchId gameMatchId = new($"game-{nowMilliseconds:x}-{++sequence:x}");
            DevelopmentMatchAdmissionRegistry registry = new(gameMatchId);
            long expiry = nowMilliseconds + ticketLifetimeMilliseconds;
            MatchAdmissionGrant[] grants = new MatchAdmissionGrant[match.Members.Length];
            for (int index = 0; index < match.Members.Length; index++)
            {
                LobbyMatchMember member = match.Members[index];
                MatchPlayerId playerId = new(index == 0 ? "player-a" : "player-b");
                MatchJoinTicket ticket = registry.Register(member.AccountId, playerId, expiry);
                grants[index] = new MatchAdmissionGrant(match.LobbyMatchId, gameMatchId,
                    member.AccountId, playerId, address, port, ticket);
            }
            reservation = new MatchReservation(match.LobbyMatchId, gameMatchId, address, port, expiry, grants);
            Directory.CreateDirectory(evidenceDirectory);
            string safeId = gameMatchId.Value.Replace(':', '-').Replace('/', '-');
            string reservationPath = Path.Combine(evidenceDirectory, safeId + ".reservation.json");
            string readyPath = Path.Combine(evidenceDirectory, safeId + ".ready");
            MatchReservationFile.Write(reservationPath, MatchReservationFile.Create(reservation, registry));

            if (!launcher.TryLaunch(executablePath, address, port, authenticationKeyPath,
                    reservationPath, readyPath, readyTimeoutSeconds, out int processId, out failure))
            {
                allocatedPorts.Remove(port);
                reservation = null;
                return false;
            }
            LastAdmissionRegistry = registry;
            LastReservationPath = reservationPath;
            LastProcessId = processId;
            return true;
        }

        private bool TryReservePort(out ushort port)
        {
            for (int offset = 0; offset < maximumActiveMatches; offset++)
            {
                int candidate = firstPort + offset;
                if (candidate > ushort.MaxValue) break;
                ushort value = (ushort)candidate;
                if (!allocatedPorts.Add(value)) continue;
                port = value;
                return true;
            }
            port = 0;
            return false;
        }
    }
}
