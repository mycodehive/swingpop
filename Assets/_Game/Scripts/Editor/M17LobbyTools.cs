using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SwingPop.Data;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using ProcessWindowStyle = System.Diagnostics.ProcessWindowStyle;

namespace SwingPop.Editor
{
    public static class M17LobbyTools
    {
        private const string LobbyScenePath = "Assets/_Game/Scenes/Lobby_Development.unity";
        private const string HoleScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string FoundationScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string LobbySettingsPath = "Assets/_Game/ScriptableObjects/Online/M17LobbyDevelopmentSettings.asset";
        private const string MultiplayerSettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private const string LobbyBuildPath = "Builds/M17Lobby/SwingPopLobby.exe";
        private const string MatchServerBuildPath = "Builds/M17MatchServer/SwingPopServer.exe";
        private const string ClientBuildPath = "Builds/M17Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/Build M17 Lobby Foundation")]
        public static void BuildFoundation()
        {
            Require(AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(MultiplayerSettingsPath) != null,
                "M16 development settings are missing. Complete M16 before building M17.");
            LobbyDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<LobbyDevelopmentSettings>(LobbySettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LobbyDevelopmentSettings>();
                AssetDatabase.CreateAsset(settings, LobbySettingsPath);
            }
            settings.Configure("127.0.0.1", 18817, 16, 32, false);
            settings.ConfigureAllocator(MatchServerBuildPath, "127.0.0.1", 19817, 4, 15f, 90f);
            EditorUtility.SetDirty(settings);
            BuildLobbyScene(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("M17 LOBBY FOUNDATION BUILD COMPLETE | OfflineSingle remains default; Foundation unchanged.");
        }

        [MenuItem("SwingPop/Online/Validate M17 Lobby")]
        public static void Validate()
        {
            BuildFoundation();
            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            LobbyDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<LobbyDevelopmentSettings>(LobbySettingsPath);
            Require(scene.IsValid(), "Lobby_Development scene failed to load.");
            Require(settings != null, "M17 Lobby settings are missing.");
            Require(Object.FindObjectsByType<LobbyDevelopmentController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one LobbyDevelopmentController.");
            Require(Object.FindObjectsByType<LobbyNetworkTransport>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one LobbyNetworkTransport.");
            Require(Object.FindObjectsByType<LobbyDevelopmentView>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one LobbyDevelopmentView.");
            Require(Object.FindObjectsByType<MatchSessionController>(FindObjectsInactive.Include).Length == 0,
                "Lobby scene must not contain gameplay MatchSessionController.");
            Require(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length == 0,
                "Lobby scene must not contain gameplay colliders.");
            Require(settings.MaximumActiveMatches <= 8 && settings.MaximumRooms <= 128,
                "Lobby registry/allocation limits must be bounded.");
            Require(LobbyNetworkRules.IsAllowedFromClient(LobbyWireMessageType.CreateMatch)
                    && !LobbyNetworkRules.IsAllowedFromClient(LobbyWireMessageType.AdmissionGranted)
                    && LobbyNetworkRules.IsAllowedFromService(LobbyWireMessageType.AdmissionGranted)
                    && !LobbyNetworkRules.IsAllowedFromService(LobbyWireMessageType.StartMatch),
                "Lobby message directions are invalid.");
            Require(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.MatchAdmissionRequest)
                    && NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.MatchAdmissionRejected),
                "Dedicated match admission directions are invalid.");

            PrelaunchedGameServerAllocator allocator = new("127.0.0.1", 19817);
            InMemoryLobbyService lobby = new(allocator, 4);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LobbyPlayerSession a = new(new PlayerAccountId("validator-a"), new AuthSessionId("session-a"), now + 60_000L);
            LobbyPlayerSession b = new(new PlayerAccountId("validator-b"), new AuthSessionId("session-b"), now + 60_000L);
            LobbyMatchSnapshot room = lobby.CreateMatch(a,
                new CreateMatchRequest("create", "Validator Room", 2, LobbyProtocol.SupportedHoleId,
                    LobbyVisibility.Public), now).Value;
            Require(room != null && lobby.JoinMatch(b, new LobbyMatchRequest("join", room.LobbyMatchId), now).Accepted,
                "Create/join validation failed.");
            lobby.SetReady(a, new SetReadyRequest("ready-a", room.LobbyMatchId, true), now);
            lobby.SetReady(b, new SetReadyRequest("ready-b", room.LobbyMatchId, true), now);
            MatchReservation reservation = lobby.StartMatch(a,
                new LobbyMatchRequest("start", room.LobbyMatchId), now).Value;
            Require(reservation != null && reservation.Grants.Length == 2,
                "Ready/start reservation validation failed.");
            Require(allocator.LastAdmissionRegistry.ValidateAndConsume(reservation.GameMatchId,
                    a.PlayerAccountId, reservation.Grants[0].JoinTicket.Secret, now, false).Accepted,
                "Account-bound MatchJoinTicket validation failed.");

            string temporary = Path.Combine(Path.GetTempPath(), "SwingPop", "M17", "validator.reservation.json");
            MatchReservationFile.Write(temporary,
                MatchReservationFile.Create(reservation, allocator.LastAdmissionRegistry));
            string serializedReservation = File.ReadAllText(temporary);
            Require(!serializedReservation.Contains(reservation.Grants[1].JoinTicket.Secret, StringComparison.Ordinal),
                "Reservation file contains a plaintext MatchJoinTicket.");
            Require(MatchReservationFile.TryLoad(temporary, out MatchReservationFileDocument document,
                    out DevelopmentMatchAdmissionRegistry loaded) && document.GameMatchId == reservation.GameMatchId
                && loaded.Count == 2, "Reservation file round-trip failed.");

            JsonMatchMessageSerializer serializer = new();
            int createBytes = SerializedBytes(serializer, new CreateMatchRequest("create", "SwingPop Room 01", 2,
                LobbyProtocol.SupportedHoleId, LobbyVisibility.Public));
            int listBytes = SerializedBytes(serializer, new LobbyMatchListMessage("list", new[] { room }));
            int joinBytes = SerializedBytes(serializer, new LobbyMatchRequest("join", room.LobbyMatchId));
            int readyBytes = SerializedBytes(serializer, new SetReadyRequest("ready", room.LobbyMatchId, true));
            int startBytes = SerializedBytes(serializer, new LobbyMatchRequest("start", room.LobbyMatchId));
            int ticketBytes = SerializedBytes(serializer, reservation.Grants[1]);
            int reservationBytes = SerializedBytes(serializer, reservation);

            M16AuthenticationTools.Validate();
            string resultPath = ProjectPath("Library/M17/M17Validation.result");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            string report = "lobbyProtocol=1 gameplayProtocol=3 scene=PASS registry=PASS auth=PASS " +
                            "capacity=2 atomic=LOCK admission=PASS ticketPlaintext=NO m16=PASS " +
                            $"payloadBytes=create:{createBytes},list1:{listBytes},join:{joinBytes},ready:{readyBytes}," +
                            $"start:{startBytes},grant:{ticketBytes},reservation:{reservationBytes}";
            File.WriteAllText(resultPath, "PASS | " + report);
            Debug.Log("M17 LOBBY VALIDATION PASS | " + report);
        }

        [MenuItem("SwingPop/Online/M17/Build Lobby Service")]
        public static void BuildLobbyService()
        {
            BuildFoundation();
            BuildWindowsPlayer(LobbyBuildPath, new[] { LobbyScenePath }, "M17 LOBBY SERVICE BUILD PASS");
        }

        [MenuItem("SwingPop/Online/M17/Build Match Server")]
        public static void BuildMatchServer()
        {
            BuildFoundation();
            BuildWindowsPlayer(MatchServerBuildPath, new[] { HoleScenePath }, "M17 MATCH SERVER BUILD PASS");
        }

        [MenuItem("SwingPop/Online/M17/Build Client")]
        public static void BuildClient()
        {
            BuildFoundation();
            BuildWindowsPlayer(ClientBuildPath, new[] { LobbyScenePath, HoleScenePath }, "M17 CLIENT BUILD PASS");
        }

        [MenuItem("SwingPop/Online/M17/Generate Development Credentials")]
        public static void GenerateDevelopmentCredentials()
        {
            MultiplayerDevelopmentSettings settings =
                AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(MultiplayerSettingsPath);
            Require(settings != null, "M16 development settings are missing.");
            string directory = Path.Combine(Path.GetTempPath(), "SwingPop", "M17",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(directory);
            byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
            DevelopmentAuthenticationProvider provider = new(key, settings.DevelopmentAuthenticationIssuer);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string keyPath = Path.Combine(directory, "server-auth-key.txt");
            File.WriteAllText(keyPath, Convert.ToBase64String(key));
            string clientA = Path.Combine(directory, "client-a.credential");
            string clientB = Path.Combine(directory, "client-b.credential");
            string clientC = Path.Combine(directory, "client-c.credential");
            File.WriteAllText(clientA, provider.IssueCredential(new PlayerAccountId("dev-player-a"), now, 900_000L));
            File.WriteAllText(clientB, provider.IssueCredential(new PlayerAccountId("dev-player-b"), now, 900_000L));
            File.WriteAllText(clientC, provider.IssueCredential(new PlayerAccountId("dev-player-c"), now, 900_000L));
            string manifest = ProjectPath("Library/M17/M17CredentialPaths.txt");
            File.WriteAllLines(manifest, new[]
            {
                "directory=" + directory,
                "serverKey=" + keyPath,
                "clientA=" + clientA,
                "clientB=" + clientB,
                "clientC=" + clientC
            });
            Debug.Log("M17 DEVELOPMENT CREDENTIALS GENERATED | directory=" + directory
                      + " | key fingerprint=" + DevelopmentAuthenticationProvider.Fingerprint(Convert.ToBase64String(key))
                      + " | plaintext credentials were not logged");
        }

        [MenuItem("SwingPop/Online/M17/Launch Local Lobby Test")]
        public static void LaunchLocalLobbyTest()
        {
            Require(File.Exists(ProjectPath(LobbyBuildPath)), "Build Lobby Service first.");
            Require(File.Exists(ProjectPath(MatchServerBuildPath)), "Build Match Server first.");
            Require(File.Exists(ProjectPath(ClientBuildPath)), "Build Client first.");
            GenerateDevelopmentCredentials();
            Dictionary<string, string> credentials = File.ReadAllLines(ProjectPath("Library/M17/M17CredentialPaths.txt"))
                .Select(line => line.Split(new[] { '=' }, 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
            string evidence = ProjectPath("Library/M17/manual");
            Directory.CreateDirectory(evidence);
            StartProcess(ProjectPath(LobbyBuildPath), $"-swingpopLobbyService -swingpopAuthKeyFile=\"{credentials["serverKey"]}\" " +
                $"-swingpopMatchServerExecutable=\"{ProjectPath(MatchServerBuildPath)}\" -swingpopM17EvidenceDirectory=\"{evidence}\" " +
                $"-logFile \"{Path.Combine(evidence, "lobby-service.log")}\"");
            StartProcess(ProjectPath(ClientBuildPath),
                $"-swingpopLobbyClient -swingpopAuthCredentialFile=\"{credentials["clientA"]}\" " +
                $"-logFile \"{Path.Combine(evidence, "client-a.log")}\"", false);
            StartProcess(ProjectPath(ClientBuildPath),
                $"-swingpopLobbyClient -swingpopAuthCredentialFile=\"{credentials["clientB"]}\" " +
                $"-logFile \"{Path.Combine(evidence, "client-b.log")}\"", false);
            Debug.Log("M17 LOCAL LOBBY TEST LAUNCHED | Lobby Service + Client A + Client B. Use the development UI manually.");
        }

        private static void BuildLobbyScene(LobbyDevelopmentSettings lobbySettings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lobby_Development";
            Camera camera = new GameObject("Lobby Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.09f, 0.16f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;

            GameObject root = new("M17 Lobby Runtime");
            LobbyNetworkTransport transport = root.AddComponent<LobbyNetworkTransport>();
            LobbyDevelopmentController controller = root.AddComponent<LobbyDevelopmentController>();
            LobbyDevelopmentView view = root.AddComponent<LobbyDevelopmentView>();
            view.Configure(controller);
            MultiplayerDevelopmentSettings authSettings =
                AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(MultiplayerSettingsPath);
            SerializedObject serialized = new(controller);
            serialized.FindProperty("settings").objectReferenceValue = lobbySettings;
            serialized.FindProperty("authenticationSettings").objectReferenceValue = authSettings;
            serialized.FindProperty("transport").objectReferenceValue = transport;
            serialized.FindProperty("view").objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static void BuildWindowsPlayer(string path, string[] scenes, string label)
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
                throw new InvalidOperationException($"M17 build failed: {report.summary.result}");
            Debug.Log($"{label} | {path} | {report.summary.totalSize} bytes");
        }

        private static void StartProcess(string executable, string arguments, bool hidden = true)
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

        private static int SerializedBytes(JsonMatchMessageSerializer serializer, object payload) =>
            Encoding.UTF8.GetByteCount(serializer.Serialize(payload));

        private static string ProjectPath(string relative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
