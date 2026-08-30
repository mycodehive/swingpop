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
        bool TryStop(int processId);
    }

    public sealed class MatchServerLaunchPolicy
    {
        public MatchServerLaunchPolicy(bool bindToAllocatorParent, float maximumLifetimeSeconds,
            float completionShutdownSeconds = 15f)
        {
            BindToAllocatorParent = bindToAllocatorParent;
            MaximumLifetimeSeconds = Math.Clamp(maximumLifetimeSeconds, 300f, 14_400f);
            CompletionShutdownSeconds = Math.Clamp(completionShutdownSeconds, 5f, 60f);
        }

        public bool BindToAllocatorParent { get; }
        public float MaximumLifetimeSeconds { get; }
        public float CompletionShutdownSeconds { get; }
        public static MatchServerLaunchPolicy Development => new(true, 3600f, 15f);
        public static MatchServerLaunchPolicy Staging(float maximumLifetimeSeconds,
            float completionShutdownSeconds = 15f) => new(false, maximumLifetimeSeconds, completionShutdownSeconds);
    }

    public sealed class LocalMatchServerProcessLauncher : ILocalMatchServerLauncher
    {
        private readonly MatchServerLaunchPolicy policy;

        public LocalMatchServerProcessLauncher(MatchServerLaunchPolicy policy = null) =>
            this.policy = policy ?? MatchServerLaunchPolicy.Development;

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
                List<string> values = new()
                {
                    "-swingpopServer", "-batchmode", "-nographics",
                    "-swingpopAddress=" + Quote(address),
                    "-swingpopPort=" + port.ToString(CultureInfo.InvariantCulture),
                    "-swingpopAuthKeyFile=" + Quote(authenticationKeyPath),
                    "-swingpopMatchReservationFile=" + Quote(reservationPath),
                    "-swingpopServerReadyFile=" + Quote(readyPath),
                    DedicatedServerBootstrap.MaximumLifetimeArgument +
                    policy.MaximumLifetimeSeconds.ToString(CultureInfo.InvariantCulture),
                    DedicatedServerBootstrap.CompletionShutdownArgument +
                    policy.CompletionShutdownSeconds.ToString(CultureInfo.InvariantCulture),
                    "-logFile", Quote(Path.ChangeExtension(readyPath, ".server.log"))
                };
                if (policy.BindToAllocatorParent)
                    values.Insert(values.Count - 2, DedicatedServerBootstrap.ParentProcessArgument +
                        Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
                string arguments = string.Join(" ", values);
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
                TryStop(process.Id);
                return false;
            }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public bool TryStop(int processId)
        {
            if (processId <= 0) return false;
            try
            {
                using Process process = Process.GetProcessById(processId);
                if (process.HasExited) return true;
                process.Kill();
                return process.WaitForExit(3000);
            }
            catch (ArgumentException) { return true; }
            catch (Exception) { return false; }
        }

        private static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "") + "\"";
    }

    /// <summary>Local-development allocator. It is bounded and is not cloud orchestration.</summary>
    public sealed class DevelopmentGameServerAllocator : IGameServerAllocator
    {
        private sealed class AllocationRecord
        {
            public string AllocationId;
            public int ProcessId;
            public long ExpiresAt;
            public string ReservationPath;
            public string ReadyPath;
        }

        private readonly string executablePath;
        private readonly string address;
        private readonly ushort firstPort;
        private readonly int maximumActiveMatches;
        private readonly long ticketLifetimeMilliseconds;
        private readonly float readyTimeoutSeconds;
        private readonly string authenticationKeyPath;
        private readonly string evidenceDirectory;
        private readonly ILocalMatchServerLauncher launcher;
        private readonly IMatchConnectivityProvider connectivityProvider;
        private readonly MatchServerLaunchPolicy launchPolicy;
        private readonly HashSet<ushort> allocatedPorts = new();
        private readonly Dictionary<string, ushort> connectivityPorts = new();
        private readonly Dictionary<string, AllocationRecord> processes = new();
        private long sequence;

        public DevelopmentGameServerAllocator(string executablePath, string address, ushort firstPort,
            int maximumActiveMatches, long ticketLifetimeMilliseconds, float readyTimeoutSeconds,
            string authenticationKeyPath, string evidenceDirectory, ILocalMatchServerLauncher launcher = null,
            IMatchConnectivityProvider connectivityProvider = null, MatchServerLaunchPolicy launchPolicy = null)
        {
            this.executablePath = executablePath ?? string.Empty;
            this.address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            this.firstPort = firstPort;
            this.maximumActiveMatches = Math.Max(1, maximumActiveMatches);
            this.ticketLifetimeMilliseconds = Math.Max(10_000L, ticketLifetimeMilliseconds);
            this.readyTimeoutSeconds = Math.Max(1f, readyTimeoutSeconds);
            this.authenticationKeyPath = authenticationKeyPath ?? string.Empty;
            this.evidenceDirectory = evidenceDirectory ?? Path.Combine(Path.GetTempPath(), "SwingPop", "M17");
            this.launchPolicy = launchPolicy ?? MatchServerLaunchPolicy.Development;
            this.launcher = launcher ?? new LocalMatchServerProcessLauncher(this.launchPolicy);
            this.connectivityProvider = connectivityProvider ?? new DirectMatchConnectivityProvider();
        }

        public int ActiveAllocationCount => allocatedPorts.Count;
        public int ActiveProcessCount => processes.Count;
        public DevelopmentMatchAdmissionRegistry LastAdmissionRegistry { get; private set; }
        public string LastReservationPath { get; private set; } = string.Empty;
        public int LastProcessId { get; private set; }
        public MatchConnectivityAllocation LastConnectivityAllocation { get; private set; }

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
            if (!connectivityProvider.TryAllocate(gameMatchId, address, port, nowMilliseconds,
                    out MatchConnectivityAllocation connectivity, out failure))
            {
                allocatedPorts.Remove(port);
                return false;
            }
            DevelopmentMatchAdmissionRegistry registry = new(gameMatchId);
            long expiry = nowMilliseconds + ticketLifetimeMilliseconds;
            MatchAdmissionGrant[] grants = new MatchAdmissionGrant[match.Members.Length];
            for (int index = 0; index < match.Members.Length; index++)
            {
                LobbyMatchMember member = match.Members[index];
                MatchPlayerId playerId = new(index == 0 ? "player-a" : "player-b");
                MatchJoinTicket ticket = registry.Register(member.AccountId, playerId, expiry);
                grants[index] = new MatchAdmissionGrant(match.LobbyMatchId, gameMatchId,
                    member.AccountId, playerId, connectivity.Client, ticket);
            }
            reservation = new MatchReservation(match.LobbyMatchId, gameMatchId, connectivity.Server,
                expiry, connectivity.Client, grants);
            Directory.CreateDirectory(evidenceDirectory);
            string safeId = gameMatchId.Value.Replace(':', '-').Replace('/', '-');
            string reservationPath = Path.Combine(evidenceDirectory, safeId + ".reservation.json");
            string readyPath = Path.Combine(evidenceDirectory, safeId + ".ready");
            MatchReservationFile.Write(reservationPath, MatchReservationFile.Create(reservation, registry));

            if (!launcher.TryLaunch(executablePath, address, port, authenticationKeyPath,
                    reservationPath, readyPath, readyTimeoutSeconds, out int processId, out failure))
            {
                if (processId > 0) launcher.TryStop(processId);
                connectivityProvider.Release(connectivity.AllocationId);
                allocatedPorts.Remove(port);
                Delete(reservationPath);
                Delete(readyPath);
                reservation = null;
                return false;
            }
            if (!connectivityProvider.MarkServerReady(connectivity.AllocationId))
            {
                launcher.TryStop(processId);
                connectivityProvider.Release(connectivity.AllocationId);
                allocatedPorts.Remove(port);
                Delete(reservationPath);
                Delete(readyPath);
                reservation = null;
                failure = "Connectivity allocation could not enter the server-ready state.";
                return false;
            }
            LastAdmissionRegistry = registry;
            LastReservationPath = reservationPath;
            LastProcessId = processId;
            LastConnectivityAllocation = connectivity;
            connectivityPorts[connectivity.AllocationId] = port;
            processes[connectivity.AllocationId] = new AllocationRecord
            {
                AllocationId = connectivity.AllocationId,
                ProcessId = processId,
                ExpiresAt = nowMilliseconds + (long)(launchPolicy.MaximumLifetimeSeconds * 1000f),
                ReservationPath = reservationPath,
                ReadyPath = readyPath
            };
            return true;
        }

        public bool Release(string allocationId)
        {
            if (string.IsNullOrWhiteSpace(allocationId)) return false;
            bool processReleased = false;
            if (processes.Remove(allocationId, out AllocationRecord record))
            {
                processReleased = launcher.TryStop(record.ProcessId);
                DeleteTemporaryFiles(record);
            }
            bool released = connectivityProvider.Release(allocationId);
            if (connectivityPorts.Remove(allocationId, out ushort port)) allocatedPorts.Remove(port);
            return released || processReleased;
        }

        public int Reap(long nowMilliseconds)
        {
            List<string> expired = new();
            foreach (KeyValuePair<string, AllocationRecord> pair in processes)
                if (nowMilliseconds >= pair.Value.ExpiresAt || !IsProcessAlive(pair.Value.ProcessId))
                    expired.Add(pair.Key);
            foreach (string allocationId in expired) Release(allocationId);
            return expired.Count;
        }

        private static bool IsProcessAlive(int processId)
        {
            if (processId <= 0) return false;
            try
            {
                using Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException) { return false; }
            catch (Exception) { return true; }
        }

        private static void DeleteTemporaryFiles(AllocationRecord record)
        {
            Delete(record.ReservationPath);
            Delete(record.ReadyPath);
        }

        private static void Delete(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
