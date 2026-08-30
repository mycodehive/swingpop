using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SwingPop.Online
{
    public interface ILocalRelayProcessLauncher
    {
        bool TryLaunch(string executablePath, string listenAddress, ushort listenPort,
            string targetAddress, ushort targetPort, string readyPath, float timeoutSeconds,
            int parentProcessId, float lifetimeSeconds, out int processId, out string failure);
        bool TryStop(int processId);
    }

    public sealed class LocalRelayProcessLauncher : ILocalRelayProcessLauncher
    {
        public bool TryLaunch(string executablePath, string listenAddress, ushort listenPort,
            string targetAddress, ushort targetPort, string readyPath, float timeoutSeconds,
            int parentProcessId, float lifetimeSeconds, out int processId, out string failure)
        {
            processId = 0;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                failure = "Local relay executable was not found.";
                return false;
            }
            try
            {
                if (File.Exists(readyPath)) File.Delete(readyPath);
                string arguments = string.Join(" ", new[]
                {
                    LocalRelayProxyRuntime.RoleArgument, "-batchmode", "-nographics",
                    LocalRelayProxyRuntime.ListenAddressArgument + Quote(listenAddress),
                    LocalRelayProxyRuntime.ListenPortArgument + listenPort.ToString(CultureInfo.InvariantCulture),
                    LocalRelayProxyRuntime.TargetAddressArgument + Quote(targetAddress),
                    LocalRelayProxyRuntime.TargetPortArgument + targetPort.ToString(CultureInfo.InvariantCulture),
                    LocalRelayProxyRuntime.ReadyFileArgument + Quote(readyPath),
                    LocalRelayProxyRuntime.ParentProcessArgument + parentProcessId.ToString(CultureInfo.InvariantCulture),
                    LocalRelayProxyRuntime.LifetimeArgument + lifetimeSeconds.ToString(CultureInfo.InvariantCulture),
                    "-logFile", Quote(Path.ChangeExtension(readyPath, ".relay.log"))
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
                        failure = "Local relay exited before its ready handshake.";
                        return false;
                    }
                    Thread.Sleep(50);
                }
                TryStop(processId);
                failure = "Local relay ready handshake timed out.";
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
                if (!process.HasExited) process.Kill();
                return true;
            }
            catch (ArgumentException) { return true; }
            catch (Exception) { return false; }
        }

        private static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", string.Empty) + "\"";
    }

    /// <summary>Bounded local proxy provider used to verify the seam and real proxy traffic. It is not NAT traversal.</summary>
    public sealed class LocalRelayConnectivityProvider : IMatchConnectivityProvider
    {
        private readonly string executablePath;
        private readonly string relayAddress;
        private readonly ushort firstRelayPort;
        private readonly int maximumAllocations;
        private readonly float allocationTimeoutSeconds;
        private readonly long credentialLifetimeMilliseconds;
        private readonly string evidenceDirectory;
        private readonly ILocalRelayProcessLauncher launcher;
        private readonly Dictionary<string, MatchConnectivityAllocation> allocations = new();

        public LocalRelayConnectivityProvider(string executablePath, string relayAddress,
            ushort firstRelayPort, int maximumAllocations, float allocationTimeoutSeconds,
            long credentialLifetimeMilliseconds, string evidenceDirectory,
            ILocalRelayProcessLauncher launcher = null)
        {
            this.executablePath = executablePath ?? string.Empty;
            this.relayAddress = string.IsNullOrWhiteSpace(relayAddress) ? "127.0.0.1" : relayAddress.Trim();
            this.firstRelayPort = firstRelayPort;
            this.maximumAllocations = Mathf.Clamp(maximumAllocations, 1, 8);
            this.allocationTimeoutSeconds = Mathf.Clamp(allocationTimeoutSeconds, 1f, 60f);
            this.credentialLifetimeMilliseconds = Math.Max(60_000L, credentialLifetimeMilliseconds);
            this.evidenceDirectory = string.IsNullOrWhiteSpace(evidenceDirectory)
                ? Path.Combine(Path.GetTempPath(), "SwingPop", "M18") : evidenceDirectory;
            this.launcher = launcher ?? new LocalRelayProcessLauncher();
        }

        public MatchConnectivityMode Mode => MatchConnectivityMode.Relay;
        public int ActiveAllocationCount => allocations.Count;

        public bool TryAllocate(MatchId gameMatchId, string serverAddress, ushort serverPort,
            long nowMilliseconds, out MatchConnectivityAllocation allocation, out string failure)
        {
            allocation = null;
            failure = string.Empty;
            if (!gameMatchId.IsValid || allocations.Count >= maximumAllocations)
            {
                failure = "The bounded local relay allocation limit was reached.";
                return false;
            }
            if (!TryFindRelayPort(out ushort relayPort))
            {
                failure = "No local relay port is available.";
                return false;
            }
            string allocationId = "relay-" + gameMatchId.Value + "-" + Guid.NewGuid().ToString("N");
            string credential = ConnectivitySecurity.CreateCredential();
            long expiry = nowMilliseconds + credentialLifetimeMilliseconds;
            Directory.CreateDirectory(evidenceDirectory);
            string readyPath = Path.Combine(evidenceDirectory,
                ConnectivitySecurity.Fingerprint(allocationId) + ".relay.ready");
            bool launched = launcher.TryLaunch(executablePath, relayAddress, relayPort,
                serverAddress, serverPort, readyPath, allocationTimeoutSeconds,
                Process.GetCurrentProcess().Id, credentialLifetimeMilliseconds / 1000f,
                out int processId, out failure);
            if (!launched) return false;

            ServerConnectivityDescriptor server = new(serverAddress, serverPort);
            MatchConnectivityDescriptor client = new(MatchConnectivityMode.Relay,
                ConnectivityProtocol.LocalRelayProvider, relayAddress, relayPort,
                allocationId, credential, expiry);
            allocation = new MatchConnectivityAllocation(allocationId, server, client, expiry, processId);
            allocations.Add(allocationId, allocation);
            return true;
        }

        public bool MarkServerReady(string allocationId)
        {
            if (!allocations.TryGetValue(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            value.MarkServerReady();
            value.MarkInUse();
            return true;
        }

        public bool Release(string allocationId)
        {
            if (!allocations.Remove(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            bool stopped = launcher.TryStop(value.ResourceProcessId);
            value.MarkReleased();
            return stopped;
        }

        private bool TryFindRelayPort(out ushort port)
        {
            HashSet<ushort> used = new();
            foreach (MatchConnectivityAllocation value in allocations.Values) used.Add(value.Client.Port);
            for (int offset = 0; offset < maximumAllocations; offset++)
            {
                int candidate = firstRelayPort + offset;
                if (candidate <= ushort.MaxValue && !used.Contains((ushort)candidate))
                {
                    port = (ushort)candidate;
                    return true;
                }
            }
            port = 0;
            return false;
        }
    }

    /// <summary>Transparent standalone TCP proxy. UTP WebSocket bytes are forwarded unchanged.</summary>
    public sealed class LocalRelayProxyRuntime : MonoBehaviour
    {
        public const string RoleArgument = "-swingpopLocalRelayProxy";
        public const string ListenAddressArgument = "-swingpopRelayListenAddress=";
        public const string ListenPortArgument = "-swingpopRelayListenPort=";
        public const string TargetAddressArgument = "-swingpopRelayTargetAddress=";
        public const string TargetPortArgument = "-swingpopRelayTargetPort=";
        public const string ReadyFileArgument = "-swingpopRelayReadyFile=";
        public const string ParentProcessArgument = "-swingpopRelayParentProcess=";
        public const string LifetimeArgument = "-swingpopRelayLifetimeSeconds=";

        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool stopping;
        private int parentProcessId;
        private float lifetimeSeconds;
        private float elapsed;
        private string targetAddress;
        private ushort targetPort;
        private long forwardedBytes;
        private int acceptedConnections;
        private string telemetryPath = string.Empty;
        private float telemetryElapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArgument(args, RoleArgument)) return;
            GameObject root = new("M18 Local Relay Proxy");
            DontDestroyOnLoad(root);
            root.AddComponent<LocalRelayProxyRuntime>().Initialize(args);
        }

        private void Initialize(string[] args)
        {
            Application.runInBackground = true;
            string listenAddress = Read(args, ListenAddressArgument, "127.0.0.1");
            ushort listenPort = ReadPort(args, ListenPortArgument);
            targetAddress = Read(args, TargetAddressArgument, "127.0.0.1");
            targetPort = ReadPort(args, TargetPortArgument);
            parentProcessId = ReadInt(args, ParentProcessArgument, 0);
            lifetimeSeconds = Mathf.Max(60f, ReadFloat(args, LifetimeArgument, 1800f));
            if (listenPort == 0 || targetPort == 0 || !IPAddress.TryParse(listenAddress, out IPAddress ip))
            {
                Debug.LogError("[M18][Relay] Invalid local proxy arguments.", this);
                Application.Quit(2);
                return;
            }
            try
            {
                listener = new TcpListener(ip, listenPort);
                listener.Start(16);
                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "SwingPop Local Relay Accept" };
                acceptThread.Start();
                string ready = Read(args, ReadyFileArgument, string.Empty);
                if (!string.IsNullOrWhiteSpace(ready))
                {
                    string directory = Path.GetDirectoryName(ready);
                    if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                    File.WriteAllText(ready, $"READY relay={listenAddress}:{listenPort} target=private");
                    telemetryPath = Path.ChangeExtension(ready, ".traffic");
                    WriteTelemetry("READY");
                }
                Debug.Log($"[M18][Relay] READY endpoint={listenAddress}:{listenPort} target=private");
            }
            catch (Exception exception)
            {
                Debug.LogError("[M18][Relay] Startup failed: " + exception.GetType().Name, this);
                Application.Quit(3);
            }
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            telemetryElapsed += Time.unscaledDeltaTime;
            if (telemetryElapsed >= 1f)
            {
                telemetryElapsed = 0f;
                WriteTelemetry("ACTIVE");
            }
            if (elapsed >= lifetimeSeconds || parentProcessId > 0 && !ProcessExists(parentProcessId))
                Application.Quit(0);
        }

        private void OnDestroy()
        {
            stopping = true;
            try { listener?.Stop(); } catch (Exception) { }
            WriteTelemetry("STOP");
            Debug.Log($"[M18][Relay] STOP bytes={Interlocked.Read(ref forwardedBytes)}");
        }

        private void AcceptLoop()
        {
            while (!stopping)
            {
                try
                {
                    TcpClient inbound = listener.AcceptTcpClient();
                    Interlocked.Increment(ref acceptedConnections);
                    Thread worker = new(() => Forward(inbound)) { IsBackground = true, Name = "SwingPop Relay Connection" };
                    worker.Start();
                }
                catch (SocketException) when (stopping) { return; }
                catch (ObjectDisposedException) { return; }
                catch (Exception) { if (stopping) return; }
            }
        }

        private void Forward(TcpClient inbound)
        {
            using (inbound)
            using (TcpClient outbound = new())
            {
                try
                {
                    outbound.Connect(targetAddress, targetPort);
                    NetworkStream fromClient = inbound.GetStream();
                    NetworkStream toServer = outbound.GetStream();
                    Thread upstream = new(() => Copy(fromClient, toServer)) { IsBackground = true };
                    upstream.Start();
                    Copy(toServer, fromClient);
                    try { inbound.Client.Shutdown(SocketShutdown.Both); } catch (Exception) { }
                    try { outbound.Client.Shutdown(SocketShutdown.Both); } catch (Exception) { }
                }
                catch (Exception) { }
            }
        }

        private void Copy(NetworkStream source, NetworkStream destination)
        {
            byte[] buffer = new byte[16 * 1024];
            try
            {
                while (!stopping)
                {
                    int read = source.Read(buffer, 0, buffer.Length);
                    if (read <= 0) return;
                    destination.Write(buffer, 0, read);
                    destination.Flush();
                    Interlocked.Add(ref forwardedBytes, read);
                }
            }
            catch (Exception) { }
        }

        private static bool ProcessExists(int id)
        {
            try { using Process process = Process.GetProcessById(id); return !process.HasExited; }
            catch (Exception) { return false; }
        }

        private void WriteTelemetry(string state)
        {
            if (string.IsNullOrWhiteSpace(telemetryPath)) return;
            try
            {
                File.WriteAllText(telemetryPath,
                    $"state={state} connections={Volatile.Read(ref acceptedConnections)} " +
                    $"bytes={Interlocked.Read(ref forwardedBytes)} updated={DateTime.UtcNow:O}");
            }
            catch (Exception) { }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            foreach (string value in args) if (string.Equals(value, expected, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Read(string[] args, string prefix, string fallback)
        {
            foreach (string value in args)
                if (value != null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return value.Substring(prefix.Length).Trim().Trim('"');
            return fallback;
        }

        private static ushort ReadPort(string[] args, string prefix) =>
            ushort.TryParse(Read(args, prefix, string.Empty), NumberStyles.None,
                CultureInfo.InvariantCulture, out ushort value) ? value : (ushort)0;

        private static int ReadInt(string[] args, string prefix, int fallback) =>
            int.TryParse(Read(args, prefix, string.Empty), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int value) ? value : fallback;

        private static float ReadFloat(string[] args, string prefix, float fallback) =>
            float.TryParse(Read(args, prefix, string.Empty), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value) ? value : fallback;
    }
}
