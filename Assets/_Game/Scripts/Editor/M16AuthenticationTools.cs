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
    public static class M16AuthenticationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private const string ServerBuildPath = "Builds/M16Server/SwingPopServer.exe";
        private const string ClientBuildPath = "Builds/M16Client/SwingPop.exe";

        [MenuItem("SwingPop/Online/Build M16 Authentication Foundation")]
        public static void BuildFoundation()
        {
            M15MatchLifecycleTools.BuildFoundation();
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException("M16 development settings asset is missing.");
            settings.ConfigureAuthentication(true, "swingpop-development", 900f, 1800f, 8f, false);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("M16 AUTHENTICATION FOUNDATION BUILD COMPLETE | Development provider enabled; no signing key serialized.");
        }

        [MenuItem("SwingPop/Online/Validate M16 Authentication")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            Require(settings != null, "Development settings are missing.");
            Require(settings.DevelopmentAuthenticationEnabled, "Development authentication must be enabled.");
            Require(settings.AuthenticationTokenLifetimeSeconds >= 60f, "Token lifetime is invalid.");
            Require(settings.AuthenticationSessionLifetimeSeconds >= settings.AuthenticationTokenLifetimeSeconds,
                "Auth session lifetime must not be shorter than token lifetime.");
            Require(Object.FindObjectsByType<AuthenticationController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one AuthenticationController.");
            Require(Object.FindObjectsByType<MatchSessionController>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one MatchSessionController.");
            Require(Object.FindObjectsByType<DedicatedServerMatchTransport>(FindObjectsInactive.Include).Length == 1,
                "Expected exactly one DedicatedServerMatchTransport.");
            Require(OnlineProtocol.CurrentVersion == 3, "M16 requires protocol version 3.");
            Require(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.AuthRequest)
                    && !NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.AuthAccepted)
                    && NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.AuthAccepted)
                    && !NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.AuthRequest),
                "Authentication message directions are invalid.");
            Require(!AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(NetworkMessageType.ShotSubmission),
                "Unauthenticated ShotSubmission must be denied.");

            byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
            DevelopmentAuthenticationProvider provider = new(key, settings.DevelopmentAuthenticationIssuer);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string credential = provider.IssueCredential(new PlayerAccountId("dev-validator"), now, 60_000L);
            Require(provider.ValidateCredential(credential, now).Accepted, "Generated development credential was rejected.");
            string tampered = (credential[0] == 'A' ? "B" : "A") + credential.Substring(1);
            Require(provider.ValidateCredential(tampered, now).Reason == AuthenticationRejectReason.InvalidSignature,
                "Tampered credential was not rejected by MAC validation.");
            Require(scene.IsValid(), "Hole01 scene failed to load.");
            Require(!File.ReadAllText(SettingsPath).Contains("signingKey", StringComparison.OrdinalIgnoreCase),
                "A signing key field must not be serialized in the settings asset.");
            M15MatchLifecycleTools.Validate();

            string resultPath = ProjectPath("Library/M16/M16Validation.result");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            string report = "protocol=3 authController=1 HMAC-SHA256=PASS tamper=REJECT " +
                            "unauthenticatedShot=REJECT secretSerialized=NO m15=PASS";
            File.WriteAllText(resultPath, "PASS | " + report);
            Debug.Log("M16 AUTHENTICATION VALIDATION PASS | " + report);
        }

        [MenuItem("SwingPop/Online/M16/Generate Development Credentials")]
        public static void GenerateDevelopmentCredentials()
        {
            BuildFoundation();
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            string directory = Path.Combine(Path.GetTempPath(), "SwingPop", "M16", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(directory);
            byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
            DevelopmentAuthenticationProvider provider = new(key, settings.DevelopmentAuthenticationIssuer);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string keyPath = Path.Combine(directory, "server-auth-key.txt");
            File.WriteAllText(keyPath, Convert.ToBase64String(key));
            WriteCredential(provider, directory, "client-a.credential", "dev-player-a", now, 900_000L);
            WriteCredential(provider, directory, "client-b.credential", "dev-player-b", now, 900_000L);
            WriteCredential(provider, directory, "client-c.credential", "dev-player-c", now, 900_000L);
            string invalid = provider.IssueCredential(new PlayerAccountId("dev-invalid"), now, 900_000L);
            invalid = (invalid[0] == 'A' ? "B" : "A") + invalid.Substring(1);
            File.WriteAllText(Path.Combine(directory, "client-tampered.credential"), invalid);

            string manifestPath = ProjectPath("Library/M16/M16CredentialPaths.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.WriteAllLines(manifestPath, new[]
            {
                "directory=" + directory,
                "serverKey=" + keyPath,
                "clientA=" + Path.Combine(directory, "client-a.credential"),
                "clientB=" + Path.Combine(directory, "client-b.credential"),
                "clientC=" + Path.Combine(directory, "client-c.credential"),
                "tampered=" + Path.Combine(directory, "client-tampered.credential")
            });
            Debug.Log("M16 DEVELOPMENT CREDENTIALS GENERATED | temp directory=" + directory +
                      " | key fingerprint=" + DevelopmentAuthenticationProvider.Fingerprint(Convert.ToBase64String(key)) +
                      " | plaintext credentials were not logged");
        }

        [MenuItem("SwingPop/Online/M16/Build Dedicated Server")]
        public static void BuildDedicatedServer()
        {
            BuildFoundation();
            BuildWindowsPlayer(ServerBuildPath, "M16 SERVER BUILD PASS");
        }

        [MenuItem("SwingPop/Online/M16/Build Client")]
        public static void BuildClient()
        {
            BuildFoundation();
            BuildWindowsPlayer(ClientBuildPath, "M16 CLIENT BUILD PASS");
        }

        private static void WriteCredential(DevelopmentAuthenticationProvider provider, string directory,
            string fileName, string accountId, long now, long lifetime)
        {
            File.WriteAllText(Path.Combine(directory, fileName),
                provider.IssueCredential(new PlayerAccountId(accountId), now, lifetime));
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
                throw new InvalidOperationException($"M16 build failed: {report.summary.result}");
            Debug.Log($"{label} | {buildPath} | {report.summary.totalSize} bytes");
        }

        private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
