using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SwingPop.Data;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using ProcessWindowStyle = System.Diagnostics.ProcessWindowStyle;

namespace SwingPop.Editor
{
    public static class M18ConnectivityTools
    {
        private const string LobbyScene = "Assets/_Game/Scenes/Lobby_Development.unity";
        private const string HoleScene = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M18ConnectivityDevelopmentSettings.asset";
        private const string LobbyBuild = "Builds/M18Lobby/SwingPopLobby.exe";
        private const string RelayBuild = "Builds/M18Relay/SwingPopRelay.exe";
        private const string ServerBuild = "Builds/M18MatchServer/SwingPopServer.exe";
        private const string ClientBuild = "Builds/M18Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/M18/Build Foundation")]
        public static void BuildFoundation()
        {
            M17LobbyTools.BuildFoundation();
            ConnectivityDevelopmentSettings settings =
                AssetDatabase.LoadAssetAtPath<ConnectivityDevelopmentSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ConnectivityDevelopmentSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            settings.Configure(MatchConnectivityMode.Direct, "127.0.0.1", 20817,
                ConnectivityProtocol.LocalRelayProvider, "local", 4, 30f, 1800f, 3, 1f);
            EditorUtility.SetDirty(settings);

            Scene scene = EditorSceneManager.OpenScene(LobbyScene, OpenSceneMode.Single);
            LobbyDevelopmentController controller = UnityEngine.Object.FindAnyObjectByType<LobbyDevelopmentController>();
            Require(controller != null, "M17 Lobby controller is missing.");
            SerializedObject serialized = new(controller);
            serialized.FindProperty("connectivitySettings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, LobbyScene);
            AssetDatabase.SaveAssets();
            Debug.Log("M18 CONNECTIVITY FOUNDATION BUILD COMPLETE | default=Direct provider=local-relay-proxy");
        }

        [MenuItem("SwingPop/Online/Validate M18 Connectivity")]
        public static void Validate()
        {
            BuildFoundation();
            ConnectivityDevelopmentSettings settings =
                AssetDatabase.LoadAssetAtPath<ConnectivityDevelopmentSettings>(SettingsPath);
            Require(settings != null && settings.DefaultMode == MatchConnectivityMode.Direct,
                "Direct must remain the default development mode.");
            Require(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ConnectivityRequest)
                    && NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ConnectivityAccepted)
                    && NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ConnectivityRejected),
                "Connectivity message directions are invalid.");

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string secret = ConnectivitySecurity.CreateCredential();
            MatchConnectivityDescriptor relay = new(MatchConnectivityMode.Relay,
                ConnectivityProtocol.LocalRelayProvider, "127.0.0.1", 20817,
                "allocation-validator", secret, now + 60_000L);
            Require(relay.IsValidAt(now), "Relay descriptor validation failed.");
            ConnectivityCredentialRegistry connectivity = new(relay.AllocationId, relay.Credential,
                relay.ExpiresAtUnixMilliseconds);
            Require(connectivity.Validate(new ConnectivityRequestMessage(relay), now).Accepted,
                "Relay credential validation failed.");
            MatchConnectivityDescriptor wrong = new(MatchConnectivityMode.Relay,
                ConnectivityProtocol.LocalRelayProvider, relay.Address, relay.Port,
                relay.AllocationId, ConnectivitySecurity.CreateCredential(), relay.ExpiresAtUnixMilliseconds);
            Require(connectivity.Validate(new ConnectivityRequestMessage(wrong), now).Reason
                    == ConnectivityRejectReason.InvalidCredential, "Wrong relay credential was accepted.");

            string resultPath = ProjectPath("Library/M18/M18Validation.result");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            File.WriteAllText(resultPath,
                "PASS | modeDefault=Direct provider=local-relay-proxy credentialBoundary=PASS " +
                "messageDirections=PASS productionRelay=NO-GO crossNat=NOT_VERIFIED");
            Debug.Log("M18 CONNECTIVITY VALIDATION PASS | Direct preserved; relay credential boundary PASS; " +
                      "production relay NO-GO; cross-NAT NOT VERIFIED");
        }

        [MenuItem("SwingPop/Online/M18/Build All")]
        public static void BuildAll()
        {
            BuildFoundation();
            BuildPlayer(LobbyBuild, new[] { LobbyScene }, "M18 LOBBY");
            BuildPlayer(RelayBuild, new[] { LobbyScene }, "M18 RELAY PROXY");
            BuildPlayer(ServerBuild, new[] { HoleScene }, "M18 MATCH SERVER");
            BuildPlayer(ClientBuild, new[] { LobbyScene, HoleScene }, "M18 CLIENT");
        }

        [MenuItem("SwingPop/Online/M18/Launch Direct Regression")]
        public static void LaunchDirectRegression() => Launch(MatchConnectivityMode.Direct);

        [MenuItem("SwingPop/Online/M18/Launch Relay Test")]
        public static void LaunchRelayTest() => Launch(MatchConnectivityMode.Relay);

        private static void Launch(MatchConnectivityMode mode)
        {
            Require(File.Exists(ProjectPath(LobbyBuild)), "Run M18 > Build All first.");
            Require(File.Exists(ProjectPath(ServerBuild)), "M18 match server build is missing.");
            Require(File.Exists(ProjectPath(ClientBuild)), "M18 client build is missing.");
            if (mode == MatchConnectivityMode.Relay)
                Require(File.Exists(ProjectPath(RelayBuild)), "M18 relay build is missing.");
            M17LobbyTools.GenerateDevelopmentCredentials();
            Dictionary<string, string> credentials = File.ReadAllLines(ProjectPath("Library/M17/M17CredentialPaths.txt"))
                .Select(line => line.Split(new[] { '=' }, 2)).Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
            string evidence = ProjectPath("Library/M18/" + mode.ToString().ToLowerInvariant());
            string captures = ProjectPath("docs/review-captures/m18-relay");
            Directory.CreateDirectory(evidence);
            Directory.CreateDirectory(captures);
            string shared = $"-swingpopAutomatedLobbyTest -swingpopProbeDuration=180 " +
                            $"-swingpopCaptureDirectory=\"{captures}\" ";
            if (mode == MatchConnectivityMode.Relay) shared += "-swingpopM18RelayReconnect ";
            Start(ProjectPath(LobbyBuild),
                $"-swingpopLobbyService -swingpopConnectivityMode={mode} " +
                $"-swingpopAuthKeyFile=\"{credentials["serverKey"]}\" " +
                $"-swingpopMatchServerExecutable=\"{ProjectPath(ServerBuild)}\" " +
                $"-swingpopRelayExecutable=\"{ProjectPath(RelayBuild)}\" " +
                $"-swingpopM17EvidenceDirectory=\"{evidence}\" {shared}" +
                $"-swingpopM17Role=Service -swingpopProbeLog=\"{Path.Combine(evidence, "service.probe.log")}\" " +
                $"-logFile \"{Path.Combine(evidence, "service.log")}\"");
            Start(ProjectPath(ClientBuild),
                $"-swingpopLobbyClient -swingpopAuthCredentialFile=\"{credentials["clientA"]}\" {shared}" +
                $"-swingpopM17Role=A -swingpopProbeLog=\"{Path.Combine(evidence, "client-a.probe.log")}\" " +
                $"-logFile \"{Path.Combine(evidence, "client-a.log")}\"", false);
            Start(ProjectPath(ClientBuild),
                $"-swingpopLobbyClient -swingpopAuthCredentialFile=\"{credentials["clientB"]}\" {shared}" +
                $"-swingpopM17Role=B -swingpopProbeLog=\"{Path.Combine(evidence, "client-b.probe.log")}\" " +
                $"-logFile \"{Path.Combine(evidence, "client-b.log")}\"", false);
            Debug.Log($"M18 {mode} TEST LAUNCHED | evidence={evidence}");
        }

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

        private static void Start(string executable, string arguments, bool hidden = true)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = hidden,
                WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
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
