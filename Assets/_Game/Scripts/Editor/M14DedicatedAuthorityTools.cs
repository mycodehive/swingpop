using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SwingPop.Data;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class M14DedicatedAuthorityTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private const string ServerBuildPath = "Builds/M14Server/SwingPopServer.exe";
        private const string ClientBuildPath = "Builds/M14Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/Build M14 Dedicated Authority")]
        public static void BuildFoundation()
        {
            M13NetworkPrototypeTools.BuildSceneFoundation();
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException("M14 development settings asset is missing.");
            settings.ConfigureForDevelopment(MultiplayerDevelopmentMode.OfflineSingle, 0, false);
            settings.ConfigureNetwork("127.0.0.1", 7777, 8f);
            settings.ConfigureDedicatedServer(2, true, 0.25f);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("M14 DEDICATED AUTHORITY BUILD COMPLETE | Default remains OfflineSingle.");
        }

        [MenuItem("SwingPop/Online/Validate M14 Dedicated Authority")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            Require(settings != null, "Development settings are missing.");
            Require(settings.Mode == MultiplayerDevelopmentMode.OfflineSingle, "Default mode must remain OfflineSingle.");
            Require(settings.DedicatedServerMaxPlayers == 2, "Dedicated player capacity must be 2.");
            Require(settings.DisableServerPresentation, "Dedicated presentation disable policy must be enabled.");
            Require(OnlineProtocol.CurrentVersion >= 2, "M14 dedicated envelope requires protocol version 2 or newer.");
            Require(Object.FindObjectsByType<MatchSessionController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one MatchSessionController.");
            Require(Object.FindObjectsByType<LocalMatchAuthority>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one authority component.");
            Require(Object.FindObjectsByType<UnityTransportMatchTransport>(FindObjectsInactive.Include).Length == 1,
                "M13 UnityTransportMatchTransport must remain available.");
            Require(Object.FindObjectsByType<DedicatedServerMatchTransport>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one DedicatedServerMatchTransport.");
            Require(Object.FindObjectsByType<DedicatedServerBootstrap>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one DedicatedServerBootstrap.");
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>(FindObjectsInactive.Include);
            Require(session != null && session.IsConfigured, "M14 scene references are incomplete.");
            Require(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Count(value => value.enabled) >= 8,
                "Dedicated scene must retain gameplay colliders.");
            Require(typeof(ShotFlowController).GetMethod(nameof(ShotFlowController.TryExecuteAuthoritativeShot),
                        BindingFlags.Instance | BindingFlags.Public) != null,
                "Animator-independent authoritative shot entry point is missing.");
            Require(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ShotSubmission)
                    && !NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.MatchStarted),
                "Client/server message direction rules are invalid.");
            JsonMatchMessageSerializer serializer = new();
            ClientHelloMessage hello = new("m14-validator", ClientRequestedRole.Player);
            Require(serializer.Deserialize<ClientHelloMessage>(serializer.Serialize(hello)).RequestedRole
                    == ClientRequestedRole.Player, "M14 ClientHello serializer round trip failed.");
            Require(scene.IsValid(), "Hole01 scene failed to load.");
            M13NetworkPrototypeTools.Validate();

            int renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).Length;
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
            int canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length;
            int audio = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            string report = $"protocol={OnlineProtocol.CurrentVersion} capacity=2 authority=1 serverTransport=1 m13Transport=1 " +
                            $"colliders>=8 sharedScene=1 presentationPolicy=typed cameraRequired=0 canvasRequired=0 " +
                            $"audioRequired=0 sceneRenderers={renderers} sceneCameras={cameras} sceneCanvases={canvases} " +
                            $"sceneAudio={audio} dedicatedTargetInstalled={IsDedicatedServerTargetInstalled()} m13=PASS";
            string resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/M14/M14Validation.result"));
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            File.WriteAllText(resultPath, "PASS | " + report);
            Debug.Log("M14 DEDICATED AUTHORITY VALIDATION PASS | " + report);
        }

        [MenuItem("SwingPop/Online/M14/Build Dedicated Server")]
        public static void BuildDedicatedServer()
        {
            BuildFoundation();
            BuildWindowsPlayer(ServerBuildPath, "M14 SERVER BUILD PASS");
            if (!IsDedicatedServerTargetInstalled())
                Debug.LogWarning("M14 dedicated-server build target/module was not found. Built the documented " +
                                 "Windows Development Player fallback; launch it with -swingpopServer -batchmode -nographics.");
        }

        [MenuItem("SwingPop/Online/M14/Build Client")]
        public static void BuildClient()
        {
            BuildFoundation();
            BuildWindowsPlayer(ClientBuildPath, "M14 CLIENT BUILD PASS");
        }

        public static bool IsDedicatedServerTargetInstalled()
        {
            string variations = Path.Combine(EditorApplication.applicationContentsPath,
                "PlaybackEngines/windowsstandalonesupport/Variations");
            return Directory.Exists(variations)
                   && Directory.GetDirectories(variations).Any(path =>
                       Path.GetFileName(path).IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void BuildWindowsPlayer(string buildPath, string successLabel)
        {
            string directory = Path.GetDirectoryName(buildPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            BuildPlayerOptions options = new()
            {
                scenes = EditorBuildSettings.scenes.Where(value => value.enabled).Select(value => value.path).ToArray(),
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"M14 build failed: {report.summary.result}");
            Debug.Log($"{successLabel} | {buildPath} | {report.summary.totalSize} bytes");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
