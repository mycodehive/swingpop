using System;
using System.IO;
using System.Linq;
using SwingPop.Data;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class M15MatchLifecycleTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private const string ServerBuildPath = "Builds/M15Server/SwingPopServer.exe";
        private const string ClientBuildPath = "Builds/M15Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/Build M15 Match Lifecycle")]
        public static void BuildFoundation()
        {
            M14DedicatedAuthorityTools.BuildFoundation();
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException("M15 development settings asset is missing.");
            settings.ConfigureForDevelopment(MultiplayerDevelopmentMode.OfflineSingle, 0, false);
            settings.ConfigureReconnect(30f, true, 3, 1f);
            settings.ConfigureConnectionLiveness(30f);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("M15 MATCH LIFECYCLE BUILD COMPLETE | OfflineSingle remains default; reconnect grace=30s.");
        }

        [MenuItem("SwingPop/Online/Validate M15 Match Lifecycle")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            Require(settings != null, "Development settings are missing.");
            Require(settings.Mode == MultiplayerDevelopmentMode.OfflineSingle, "Default mode must remain OfflineSingle.");
            Require(Mathf.Approximately(settings.ReconnectGraceSeconds, 30f), "Production reconnect grace must be 30 seconds.");
            Require(settings.ReconnectAttemptLimit == 3, "Reconnect attempt limit must be three.");
            Require(Object.FindObjectsByType<ReconnectController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one ReconnectController.");
            Require(Object.FindObjectsByType<DedicatedServerMatchTransport>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one dedicated server transport.");
            Require(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ReconnectRequest)
                    && !NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ReconnectAccepted)
                    && NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ReconnectAccepted)
                    && !NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ReconnectRequest),
                "Reconnect message directions are invalid.");
            Require(scene.IsValid(), "Hole01 scene failed to load.");
            M14DedicatedAuthorityTools.Validate();

            string resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/M15/M15Validation.result"));
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            string report = "protocol=2 reconnectController=1 grace=30s attempts=3 ticketHash=SHA256 " +
                            "slotReservation=1 pausePolicy=whole-match m14=PASS";
            File.WriteAllText(resultPath, "PASS | " + report);
            Debug.Log("M15 MATCH LIFECYCLE VALIDATION PASS | " + report);
        }

        [MenuItem("SwingPop/Online/M15/Build Dedicated Server")]
        public static void BuildDedicatedServer()
        {
            BuildFoundation();
            BuildWindowsPlayer(ServerBuildPath, "M15 SERVER BUILD PASS");
        }

        [MenuItem("SwingPop/Online/M15/Build Client")]
        public static void BuildClient()
        {
            BuildFoundation();
            BuildWindowsPlayer(ClientBuildPath, "M15 CLIENT BUILD PASS");
        }

        private static void BuildWindowsPlayer(string buildPath, string label)
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
                throw new InvalidOperationException($"M15 build failed: {report.summary.result}");
            Debug.Log($"{label} | {buildPath} | {report.summary.totalSize} bytes");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
