using System;
using System.IO;
using SwingPop.Data;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M20PublicControlPlaneTools
    {
        private const string LobbyScene = "Assets/_Game/Scenes/Lobby_Development.unity";
        private const string HoleScene = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M17LobbyDevelopmentSettings.asset";
        private const string LinuxLobby = "Builds/M20Staging/Linux/Lobby/SwingPopLobby.x86_64";
        private const string LinuxServer = "Builds/M20Staging/Linux/Server/SwingPopServer.x86_64";
        private const string WindowsClient = "Builds/M20Staging/WindowsClient/SwingPop.exe";
        private const string WindowsLobby = "Builds/M20Staging/WindowsValidation/Lobby/SwingPopLobby.exe";
        private const string WindowsServer = "Builds/M20Staging/WindowsValidation/Server/SwingPopServer.exe";

        [MenuItem("SwingPop/Online/M20/Validate Public Control Plane Foundation")]
        public static void Validate()
        {
            LobbyDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<LobbyDevelopmentSettings>(SettingsPath);
            Require(settings != null, "M17 Lobby settings are missing.");
            Require(ControlPlaneEndpoint.TryParse("wss://lobby.example.com/lobby", true,
                out ControlPlaneEndpoint endpoint, out _), "WSS endpoint validation failed.");
            Require(endpoint.IsSecure && endpoint.Path == "/lobby", "WSS endpoint mapping failed.");
            Require(!ControlPlaneEndpoint.TryParse("ws://lobby.example.com/lobby", true, out _, out _),
                "Staging accepted plaintext WebSocket.");
            Require(settings.MaximumConnections <= 64 && settings.MaximumRooms <= 128
                    && settings.MaximumActiveMatches <= 8, "Control-plane caps are not bounded.");
            Require(MatchServerLaunchPolicy.Staging(3600f).BindToAllocatorParent == false,
                "Staging match server is still bound to Lobby process lifetime.");
            Require(typeof(ILobbyService).GetMethod("SubmitShot") == null,
                "Lobby control plane must not own gameplay authority.");

            string result = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Library/M20/M20FoundationValidation.result"));
            Directory.CreateDirectory(Path.GetDirectoryName(result));
            File.WriteAllText(result, "PASS | endpoint=wss caps=PASS authoritySeparation=PASS " +
                "stagingParentBound=false publicDeployment=NOT_VERIFIED");
            Debug.Log("[M20][ControlPlane] FOUNDATION VALIDATION PASS | public deployment remains a separate gate.");
        }

        [MenuItem("SwingPop/Online/M20/Build Linux Staging Lobby + Server")]
        public static void BuildLinuxStaging()
        {
            Validate();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
                throw new InvalidOperationException("Install Unity Hub > Installs > 6000.5.7f1 > Add modules > " +
                                                    "Linux Build Support (Mono) before building M20 staging.");
            Build(LinuxLobby, BuildTarget.StandaloneLinux64, new[] { LobbyScene }, "Linux Lobby");
            Build(LinuxServer, BuildTarget.StandaloneLinux64, new[] { HoleScene }, "Linux Match Server");
        }

        [MenuItem("SwingPop/Online/M20/Build Windows WAN Client")]
        public static void BuildWindowsWanClient()
        {
            Validate();
            Build(WindowsClient, BuildTarget.StandaloneWindows64, new[] { LobbyScene, HoleScene }, "Windows WAN Client");
        }

        [MenuItem("SwingPop/Online/M20/Build Windows Staging Validation Services")]
        public static void BuildWindowsValidationServices()
        {
            Validate();
            Build(WindowsLobby, BuildTarget.StandaloneWindows64, new[] { LobbyScene }, "Windows Lobby Validation");
            Build(WindowsServer, BuildTarget.StandaloneWindows64, new[] { HoleScene }, "Windows Server Validation");
        }

        private static void Build(string relativePath, BuildTarget target, string[] scenes, string label)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"M20 {label} build failed: {report.summary.result}");
            Debug.Log($"[M20][ControlPlane] {label} build PASS | {relativePath} | " +
                      $"{report.summary.totalSize} bytes");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
