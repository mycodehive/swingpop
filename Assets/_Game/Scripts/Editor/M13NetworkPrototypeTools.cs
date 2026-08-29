using System;
using System.IO;
using System.Linq;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class M13NetworkPrototypeTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private const string BuildPath = "Builds/M13/SwingPopM13.exe";
        private const string PendingModeKey = "SwingPop.M13.PendingMode";

        static M13NetworkPrototypeTools()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("SwingPop/Online/Build M13 Network Prototype")]
        public static void BuildSceneFoundation()
        {
            M12OnlineFoundationBuilder.Build();
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException("M13 development settings asset is missing.");
            settings.ConfigureForDevelopment(MultiplayerDevelopmentMode.OfflineSingle, 0, false);
            settings.ConfigureNetwork("127.0.0.1", 7777, 8f);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("M13 NETWORK PROTOTYPE BUILD COMPLETE | Default remains OfflineSingle.");
        }

        [MenuItem("SwingPop/Online/Validate M13 Network Prototype")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            Require(settings != null, "Development settings are missing.");
            Require(settings.Mode == MultiplayerDevelopmentMode.OfflineSingle, "Default mode must remain OfflineSingle.");
            Require(settings.HostAddress == "127.0.0.1" && settings.Port == 7777, "Default localhost endpoint must be 127.0.0.1:7777.");
            Require(OnlineProtocol.CurrentVersion >= 2, "M13 protocol envelope requires version 2 or newer.");
            Require(OnlineProtocol.MaximumPayloadBytes == 65536, "Payload cap must be 64KB.");
            Require(Object.FindObjectsByType<MatchSessionController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one MatchSessionController.");
            Require(Object.FindObjectsByType<LocalLoopbackTransport>(FindObjectsInactive.Include).Length == 1,
                "M12 LocalLoopbackTransport must remain available.");
            Require(Object.FindObjectsByType<UnityTransportMatchTransport>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one UnityTransportMatchTransport.");
            Require(Object.FindObjectsByType<LocalMatchAuthority>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one LocalMatchAuthority.");
            Require(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one Camera.");
            Require(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one Canvas.");
            Require(Object.FindObjectsByType<MultiplayerDebugOverlay>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one F2 network overlay.");
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>(FindObjectsInactive.Include);
            Require(session != null && session.IsConfigured, "M13 scene references are incomplete.");
            Require(EditorBuildSettings.scenes.Length >= 2
                    && EditorBuildSettings.scenes[0].path == ScenePath
                    && EditorBuildSettings.scenes[1].path == "Assets/_Game/Scenes/Foundation.unity",
                "Build Settings must keep Hole01 first and Foundation second.");
            string manifest = File.ReadAllText(Path.Combine(Application.dataPath, "../Packages/manifest.json"));
            Require(manifest.Contains("\"com.unity.transport\": \"6.5.0\""),
                "Unity Transport 6.5.0 must be explicit in the manifest.");
            Require(scene.IsValid(), "Hole01 scene failed to load.");
            JsonMatchMessageSerializer serializer = new();
            NetworkMessageEnvelope envelope = new(NetworkMessageType.ClientHello, default, 1,
                serializer.Serialize(new ClientHelloMessage("validator")));
            Require(serializer.Deserialize<NetworkMessageEnvelope>(serializer.Serialize(envelope)).Sequence == 1,
                "Network envelope serializer round trip failed.");
            M12OnlineFoundationValidationTools.Validate();

            string report = $"transport=UnityTransport6.5 protocol={OnlineProtocol.CurrentVersion} payload=65536 default=OfflineSingle " +
                            "sceneSession=1 localLoopback=1 realTransport=1 authority=1 camera=1 canvas=1 buildScenes=2 m12=PASS";
            string resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/M13/M13Validation.result"));
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            File.WriteAllText(resultPath, "PASS | " + report);
            Debug.Log("M13 NETWORK PROTOTYPE VALIDATION PASS | " + report);
        }

        [MenuItem("SwingPop/Online/M13/Start Host")]
        public static void StartHost() => StartEditorNetworkMode(MultiplayerDevelopmentMode.NetworkHost);

        [MenuItem("SwingPop/Online/M13/Start Client")]
        public static void StartClient() => StartEditorNetworkMode(MultiplayerDevelopmentMode.NetworkClient);

        [MenuItem("SwingPop/Online/M13/Build Development Prototype")]
        public static void BuildDevelopmentPrototype()
        {
            BuildSceneFoundation();
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            string[] scenes = EditorBuildSettings.scenes.Where(value => value.enabled).Select(value => value.path).ToArray();
            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"M13 development build failed: {report.summary.result}");
            Debug.Log($"M13 DEVELOPMENT BUILD PASS | {BuildPath} | {report.summary.totalSize} bytes");
        }

        private static void StartEditorNetworkMode(MultiplayerDevelopmentMode mode)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before selecting an M13 role.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SessionState.SetInt(PendingModeKey, (int)mode);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            int rawMode = SessionState.GetInt(PendingModeKey, -1);
            if (rawMode < 0) return;
            SessionState.EraseInt(PendingModeKey);
            MultiplayerDevelopmentMode mode = (MultiplayerDevelopmentMode)rawMode;
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            if (session == null)
            {
                Debug.LogError("M13 MatchSessionController is missing. Run Build M13 Network Prototype first.");
                return;
            }
            session.StartNetworkMatch(mode, "127.0.0.1", 7777);
            Debug.Log($"M13 EDITOR {mode} STARTED | 127.0.0.1:7777");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
