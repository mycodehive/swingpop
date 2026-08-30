using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SwingPop.Editor
{
    public static class M19ProductionRelayTools
    {
        private const string LobbyScene = "Assets/_Game/Scenes/Lobby_Development.unity";
        private const string HoleScene = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M18ConnectivityDevelopmentSettings.asset";
        private const string LobbyBuild = "Builds/M19Lobby/SwingPopLobby.exe";
        private const string ServerBuild = "Builds/M19MatchServer/SwingPopServer.exe";
        private const string ClientBuild = "Builds/M19Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/M19/Validate Production Relay Foundation")]
        public static void Validate()
        {
            string manifest = File.ReadAllText(ProjectPath("Packages/manifest.json"));
            string packageLock = File.ReadAllText(ProjectPath("Packages/packages-lock.json"));
            Require(manifest.Contains("\"com.unity.services.multiplayer\": \"2.3.1\""),
                "Multiplayer Services 2.3.1 is not pinned in manifest.json.");
            Require(packageLock.Contains("\"com.unity.services.multiplayer\"")
                    && packageLock.Contains("\"version\": \"2.3.1\""),
                "Multiplayer Services 2.3.1 is not resolved in packages-lock.json.");
            ConnectivityDevelopmentSettings settings =
                AssetDatabase.LoadAssetAtPath<ConnectivityDevelopmentSettings>(SettingsPath);
            Require(settings != null && settings.DefaultMode == MatchConnectivityMode.Direct,
                "Direct must remain the default mode.");
            Require(!settings.EnableRealRelayTests,
                "Real cloud Relay tests must remain opt-in by default.");
            string[] gameplayReferences = typeof(GolfBallController).Assembly.GetReferencedAssemblies()
                .Select(value => value.Name).ToArray();
            Require(!gameplayReferences.Any(value => value.StartsWith("Unity.Services", StringComparison.Ordinal)),
                "Unity Services leaked into the gameplay runtime assembly.");

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ProductionRelayServerPayload payload = TestPayload();
            MatchConnectivityDescriptor descriptor = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, payload.Host, payload.Port, "validator-allocation",
                "validator-join-code", now + 60_000L,
                ConnectivityProtocol.ProductionDescriptorVersion, payload.Region);
            Require(descriptor.IsValidAt(now), "Production Relay descriptor validation failed.");
            Require(ProductionRelayServerPayload.TryDeserialize(payload.Serialize(), out _),
                "Production Relay opaque payload round-trip failed.");
            Require(!descriptor.SafeLabel.Contains(descriptor.Credential, StringComparison.Ordinal),
                "Safe connectivity label exposed the provider credential.");

            string resultPath = ProjectPath("Library/M19/M19Validation.result");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            File.WriteAllText(resultPath,
                "PASS | package=2.3.1 default=Direct modes=Direct,LocalRelay,ProductionRelay " +
                "sdkIsolation=PASS cloudTests=OPT_IN realProvider=REQUIRES_EXTERNAL_VALIDATION");
            Debug.Log("M19 PRODUCTION RELAY FOUNDATION VALIDATION PASS | package=2.3.1 | " +
                      "Direct default preserved | provider SDK isolated | cloud test opt-in");
        }

        [MenuItem("SwingPop/Online/M19/Build All")]
        public static void BuildAll()
        {
            Validate();
            BuildPlayer(LobbyBuild, new[] { LobbyScene }, "M19 LOBBY");
            BuildPlayer(ServerBuild, new[] { HoleScene }, "M19 MATCH SERVER");
            BuildPlayer(ClientBuild, new[] { LobbyScene, HoleScene }, "M19 CLIENT");
        }

        [MenuItem("SwingPop/Online/M19/Launch Production Relay (Opt-In)")]
        public static void LaunchProductionRelay()
        {
            if (!EditorUtility.DisplayDialog("M19 Production Relay",
                    "This starts a real Unity Relay allocation and can consume UGS quota. " +
                    "Continue only after linking the project and enabling Relay in the Unity Dashboard.",
                    "Start opt-in test", "Cancel")) return;
            Require(File.Exists(ProjectPath(LobbyBuild)), "Run M19 > Build All first.");
            Require(File.Exists(ProjectPath(ServerBuild)), "M19 match server build is missing.");
            Require(File.Exists(ProjectPath(ClientBuild)), "M19 client build is missing.");
            M17LobbyTools.GenerateDevelopmentCredentials();
            Dictionary<string, string> credentials = File.ReadAllLines(ProjectPath("Library/M17/M17CredentialPaths.txt"))
                .Select(line => line.Split(new[] { '=' }, 2)).Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
            string evidence = ProjectPath("Library/M19/production-relay");
            string captures = ProjectPath("docs/review-captures/m19-wan-quality");
            Directory.CreateDirectory(evidence);
            Directory.CreateDirectory(captures);
            string shared = $"-swingpopAutomatedLobbyTest -swingpopProbeDuration=180 " +
                            $"-swingpopCaptureDirectory=\"{captures}\" -swingpopM18RelayReconnect " +
                            "-swingpopUnityEnvironment=production -swingpopRelayConnectionType=dtls ";
            Start(ProjectPath(LobbyBuild),
                $"-swingpopLobbyService -swingpopConnectivityMode=ProductionRelay " +
                $"-swingpopEnableRealRelayTests -swingpopAuthKeyFile=\"{credentials["serverKey"]}\" " +
                $"-swingpopMatchServerExecutable=\"{ProjectPath(ServerBuild)}\" " +
                $"-swingpopM17EvidenceDirectory=\"{evidence}\" {shared}" +
                $"-swingpopM17Role=Service -swingpopProbeLog=\"{Path.Combine(evidence, "service.probe.log")}\" " +
                $"-logFile \"{Path.Combine(evidence, "service.log")}\"");
            StartClient(credentials["clientA"], "A", evidence, shared);
            StartClient(credentials["clientB"], "B", evidence, shared);
            Debug.Log($"M19 PRODUCTION RELAY OPT-IN TEST LAUNCHED | evidence={evidence}");
        }

        private static void StartClient(string credential, string role, string evidence, string shared) =>
            Start(ProjectPath(ClientBuild),
                $"-swingpopLobbyClient -swingpopAuthCredentialFile=\"{credential}\" {shared}" +
                $"-swingpopM17Role={role} -swingpopProbeLog=\"{Path.Combine(evidence, $"client-{role.ToLowerInvariant()}.probe.log")}\" " +
                $"-logFile \"{Path.Combine(evidence, $"client-{role.ToLowerInvariant()}.log")}\"");

        private static ProductionRelayServerPayload TestPayload() => new("relay.invalid", 9999,
            Enumerable.Repeat((byte)1, 16).ToArray(), Enumerable.Repeat((byte)2, 255).ToArray(),
            Enumerable.Repeat((byte)3, 255).ToArray(), Enumerable.Repeat((byte)4, 64).ToArray(),
            true, false, "validation");

        private static void BuildPlayer(string path, string[] scenes, string label)
        {
            string absolute = ProjectPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absolute,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"{label} build failed: {report.summary.result}");
            Debug.Log($"{label} BUILD PASS | {path} | {report.summary.totalSize} bytes");
        }

        private static void Start(string executable, string arguments)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = ProjectPath(string.Empty)
            });
        }

        private static string ProjectPath(string relative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
