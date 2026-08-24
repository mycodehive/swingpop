using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class M12OnlineFoundationValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/Online/M12MultiplayerDevelopmentSettings.asset";
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/M12/M12Validation.result"));

        [MenuItem("SwingPop/Online/Validate M12 Foundation")]
        public static void Validate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            try
            {
                string report = ValidateStructure();
                File.WriteAllText(ResultPath, "PASS\n" + report);
                Debug.Log("M12 ONLINE FOUNDATION VALIDATION PASS | " + report);
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
        }

        private static string ValidateStructure()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MatchSessionController[] sessions = Object.FindObjectsByType<MatchSessionController>(FindObjectsInactive.Include);
            LocalLoopbackTransport[] transports = Object.FindObjectsByType<LocalLoopbackTransport>(FindObjectsInactive.Include);
            LocalMatchAuthority[] authorities = Object.FindObjectsByType<LocalMatchAuthority>(FindObjectsInactive.Include);
            Require(sessions.Length == 1, $"Expected one MatchSessionController, found {sessions.Length}.");
            Require(transports.Length == 1, $"Expected one LocalLoopbackTransport, found {transports.Length}.");
            Require(authorities.Length == 1, $"Expected one LocalMatchAuthority, found {authorities.Length}.");
            Require(sessions[0].IsConfigured, "MatchSessionController dependencies are incomplete.");
            Require(transports[0].Authority == authorities[0], "Transport authority reference is invalid.");

            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            Require(settings != null, "M12 development settings asset is missing.");
            Require(settings.Mode == MultiplayerDevelopmentMode.OfflineSingle, "M12 default mode must remain OfflineSingle.");
            Require(OnlineProtocol.CurrentVersion >= 1, "Online protocol version must be positive.");

            Require(Object.FindObjectsByType<ShotFlowController>(FindObjectsInactive.Include).Length == 1,
                "Duplicate ShotFlowController found.");
            Require(Object.FindObjectsByType<GolfBallController>(FindObjectsInactive.Include).Length == 1,
                "Duplicate GolfBallController found.");
            Require(Object.FindObjectsByType<HoleFlowController>(FindObjectsInactive.Include).Length == 1,
                "Duplicate HoleFlowController found.");
            Require(Object.FindObjectsByType<CameraDirector>(FindObjectsInactive.Include).Length == 1,
                "Expected one CameraDirector.");
            Require(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length == 1,
                "Expected one Camera.");
            Require(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length == 1,
                "Expected one Canvas.");
            Require(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length == 1,
                "Expected one EventSystem.");
            Require(Object.FindObjectsByType<MultiplayerTurnPresenter>(FindObjectsInactive.Include).Length == 1,
                "Expected one MultiplayerTurnPresenter.");
            Require(CountInScene(scene, "M12 Turn Panel") == 1, "Expected exactly one M12 Turn Panel.");

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            Require(missingScripts == 0, $"Missing Scripts found: {missingScripts}.");

            ValidateNetworkDtos();
            JsonMatchMessageSerializer serializer = new();
            MatchSnapshot snapshot = CreateValidationSnapshot();
            ShotSubmission submission = CreateValidationSubmission(snapshot);
            string submissionJson = serializer.Serialize(submission);
            string snapshotJson = serializer.Serialize(snapshot);
            Require(serializer.Deserialize<ShotSubmission>(submissionJson).PlayerId == new MatchPlayerId("player-a"),
                "Shot serializer round trip failed.");
            Require(serializer.Deserialize<MatchSnapshot>(snapshotJson).PlayerCount == 2,
                "Snapshot serializer round trip failed.");

            int submissionBytes = Encoding.UTF8.GetByteCount(submissionJson);
            int snapshotBytes = Encoding.UTF8.GetByteCount(snapshotJson);
            Require(submissionBytes < 4096, $"ShotSubmission payload is unexpectedly large: {submissionBytes} bytes.");
            Require(snapshotBytes < 8192, $"MatchSnapshot payload is unexpectedly large: {snapshotBytes} bytes.");

            return $"Sessions=1, Transports=1, Authorities=1, Protocol={OnlineProtocol.CurrentVersion}, " +
                   $"Players=2, Cameras=1, Canvas=1, EventSystems=1, MissingScripts=0, " +
                   $"ShotSubmissionBytes={submissionBytes}, MatchSnapshotBytes={snapshotBytes}, DTOUnityObjectReferences=0.";
        }

        private static void ValidateNetworkDtos()
        {
            Type[] dtoTypes =
            {
                typeof(MatchId), typeof(MatchPlayerId), typeof(NetworkVector3), typeof(PlayerSnapshot),
                typeof(MatchSnapshot), typeof(ShotSubmission), typeof(ApprovedShot), typeof(ShotRejection),
                typeof(NetworkShotResult), typeof(PlayerResult), typeof(ShotCommand), typeof(ShotSpin)
            };
            HashSet<Type> visited = new();
            foreach (Type type in dtoTypes) ValidateType(type, visited);
        }

        private static void ValidateType(Type type, ISet<Type> visited)
        {
            if (type == null || visited.Contains(type) || type.IsPrimitive || type.IsEnum || type == typeof(string)) return;
            visited.Add(type);
            if (typeof(Object).IsAssignableFrom(type))
                throw new InvalidOperationException($"Network DTO contains UnityEngine.Object type: {type.FullName}");
            if (type.IsArray)
            {
                ValidateType(type.GetElementType(), visited);
                return;
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(Object).IsAssignableFrom(field.FieldType))
                    throw new InvalidOperationException($"Network DTO field references UnityEngine.Object: {type.Name}.{field.Name}");
                if (field.FieldType.Namespace != null && field.FieldType.Namespace.StartsWith("System", StringComparison.Ordinal))
                    continue;
                ValidateType(field.FieldType, visited);
            }
        }

        private static MatchSnapshot CreateValidationSnapshot()
        {
            NetworkVector3 tee = new(0f, 1f, 0f);
            PlayerSnapshot[] players =
            {
                new(new MatchPlayerId("player-a"), "PLAYER A", 0, 0, true, PlayerConnectionState.Connected,
                    0, 0, tee, tee, TerrainSurfaceType.Tee, false),
                new(new MatchPlayerId("player-b"), "PLAYER B", 1, 1, false, PlayerConnectionState.Connected,
                    0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            };
            return new MatchSnapshot(new MatchId("validation-match"), OnlineProtocol.CurrentVersion, 1,
                "hole-01", MatchPhase.Playing, TurnState.PreparingShot, 0, 0,
                new MatchPlayerId("player-a"), players);
        }

        private static ShotSubmission CreateValidationSubmission(MatchSnapshot snapshot)
        {
            ShotCommand command = new ShotCommand(Vector3.forward, Vector3.forward, 0f, 0.7f, 1f, 0f,
                ImpactGrade.Perfect, 0.7f, 0f, 22f, 35f, ShotSpin.None)
                .WithClub(ClubType.Driver, 22f, 35f, 1f, 1f);
            return new ShotSubmission(snapshot.MatchId, snapshot.CurrentTurnPlayer, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, command);
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == name) return transform.gameObject;
            return null;
        }

        private static int CountInScene(Scene scene, string name)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == name) count++;
            return count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
