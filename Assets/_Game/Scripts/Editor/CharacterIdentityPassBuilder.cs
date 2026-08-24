using System.IO;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    internal static class CharacterIdentityPassBuildRequestRunner
    {
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCharacterIdentityBuild.request"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCharacterIdentityBuild.result"));

        static CharacterIdentityPassBuildRequestRunner()
        {
            EditorApplication.update -= TryRun;
            EditorApplication.update += TryRun;
        }

        [DidReloadScripts]
        private static void AfterScriptsReload()
        {
            EditorApplication.update -= TryRun;
            EditorApplication.update += TryRun;
        }

        private static void TryRun()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= TryRun;
            File.Delete(RequestPath);
            try
            {
                CharacterIdentityPassBuilder.Build();
                File.WriteAllText(ResultPath, "PASS");
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
            }
        }
    }

    public static class CharacterIdentityPassBuilder
    {
        // This pass only authors presentation assets and integration metadata.
        private const string PlaceholderPrefabPath = "Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab";
        private const string TemplatePrefabPath = "Assets/_Game/Prefabs/Characters/HumanoidGolferTemplate.prefab";
        private const string ProfilePath = "Assets/_Game/ScriptableObjects/Character/ArtPass1CharacterVisualProfile.asset";
        private const string CameraTuningPath = "Assets/_Game/ScriptableObjects/Polish/M11CameraTuning.asset";
        private const string PresentationTuningPath = "Assets/_Game/ScriptableObjects/Polish/ArtPass1ShotPresentationTuning.asset";

        [MenuItem("SwingPop/Character/Build Character Identity Pass")]
        public static void Build()
        {
            CharacterVisualProfile profile = ConfigureProfile();
            ConfigurePlaceholder(profile);
            BuildHumanoidTemplate(profile);
            TuneCameraFraming();
            TunePerfectTrail();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CharacterSetupValidationTools.ValidateDefaultAssetsAndGetReport();
            Debug.Log(
                "CHARACTER IDENTITY PASS BUILT | Humanoid-ready adapter/profile/template, procedural fallback, " +
                "and presentation-only framing/trail tuning are ready. Gameplay systems were not modified.");
        }

        private static CharacterVisualProfile ConfigureProfile()
        {
            CharacterVisualProfile profile = AssetDatabase.LoadAssetAtPath<CharacterVisualProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CharacterVisualProfile>();
                profile.name = "ArtPass1CharacterVisualProfile";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            SerializedObject serialized = new(profile);
            serialized.FindProperty("displayName").stringValue = "Mira — SwingPop Placeholder Golfer";
            serialized.FindProperty("visualHeight").floatValue = 3.15f;
            serialized.FindProperty("localBoundsCenter").vector3Value = new Vector3(0f, 1.48f, 0f);
            serialized.FindProperty("localBoundsSize").vector3Value = new Vector3(1.55f, 3.15f, 1.15f);
            serialized.FindProperty("presentationOffset").vector3Value = Vector3.zero;
            serialized.FindProperty("characterScale").floatValue = 1f;
            serialized.FindProperty("groundOffset").floatValue = 0f;
            serialized.FindProperty("addressOffset").vector3Value = Vector3.zero;
            serialized.FindProperty("cameraFramingOffset").vector3Value = new Vector3(0f, 1.55f, 0.1f);
            serialized.FindProperty("clubSocketOffset").vector3Value = new Vector3(0.45f, 1.15f, 0.25f);
            serialized.FindProperty("impactAnchorOffset").vector3Value = new Vector3(0.62f, 0.16f, 0.58f);
            serialized.FindProperty("headLookOffset").vector3Value = new Vector3(0f, 2.2f, 2f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigurePlaceholder(CharacterVisualProfile profile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlaceholderPrefabPath);
            try
            {
                Transform visualRoot = RequireChild(root.transform, "Visual Root");
                Transform leftArm = RequireChild(root.transform, "Arm L Pivot");
                Transform rightArm = RequireChild(root.transform, "Arm R Pivot");
                Transform clubSocket = RequireChild(root.transform, "ClubSocket");
                Transform impactAnchor = RequireChild(root.transform, "AP1 Impact Anchor");
                Transform headLookTarget = RequireChild(root.transform, "AP1 Head Look Target");
                Transform rightHandSocket = FindRecursive(root.transform, "AP1 Hand Socket")
                    ?? CreateMarker(rightArm, "AP1 Hand Socket", new Vector3(0f, -0.92f, 0.03f));
                Transform leftHandSocket = FindRecursive(root.transform, "AP1 Left Hand Socket")
                    ?? CreateMarker(leftArm, "AP1 Left Hand Socket", new Vector3(0f, -0.92f, 0.03f));

                clubSocket.localPosition = profile.ClubSocketOffset;
                impactAnchor.localPosition = profile.ImpactAnchorOffset;
                headLookTarget.localPosition = profile.HeadLookOffset;

                Animator animator = root.GetComponentInChildren<Animator>(true);
                CharacterVisualAdapter adapter = root.GetComponent<CharacterVisualAdapter>();
                if (adapter == null)
                {
                    adapter = root.AddComponent<CharacterVisualAdapter>();
                }
                adapter.Configure(
                    root.transform,
                    visualRoot,
                    animator,
                    animator != null ? animator.avatar : null,
                    clubSocket,
                    rightHandSocket,
                    leftHandSocket,
                    rightHandSocket,
                    impactAnchor,
                    headLookTarget,
                    profile);

                CharacterPresentation presentation = root.GetComponent<CharacterPresentation>();
                if (presentation != null)
                {
                    SerializedObject serializedPresentation = new(presentation);
                    serializedPresentation.FindProperty("visualAdapter").objectReferenceValue = adapter;
                    serializedPresentation.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlaceholderPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildHumanoidTemplate(CharacterVisualProfile profile)
        {
            GameObject root = new("HumanoidGolferTemplate");
            try
            {
                Transform visualRoot = CreateMarker(root.transform, "VisualRoot", Vector3.zero);
                Animator animator = visualRoot.gameObject.AddComponent<Animator>();
                animator.applyRootMotion = false;

                Transform handSocket = CreateMarker(visualRoot, "HandSocket", Vector3.zero);
                Transform rightHandSocket = CreateMarker(handSocket, "RightHandSocket", Vector3.zero);
                Transform leftHandSocket = CreateMarker(visualRoot, "LeftHandSocket", Vector3.zero);
                Transform clubSocket = CreateMarker(rightHandSocket, "ClubSocket", Vector3.zero);
                CreateMarker(clubSocket, "ClubVisual", Vector3.zero);
                Transform impactAnchor = CreateMarker(visualRoot, "ImpactAnchor", profile.ImpactAnchorOffset);
                Transform headLookTarget = CreateMarker(visualRoot, "HeadLookTarget", profile.HeadLookOffset);

                CharacterVisualAdapter adapter = root.AddComponent<CharacterVisualAdapter>();
                adapter.Configure(
                    root.transform,
                    visualRoot,
                    animator,
                    null,
                    clubSocket,
                    handSocket,
                    leftHandSocket,
                    rightHandSocket,
                    impactAnchor,
                    headLookTarget,
                    profile);

                PrefabUtility.SaveAsPrefabAsset(root, TemplatePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void TuneCameraFraming()
        {
            CameraTuningData tuning = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraTuningPath);
            if (tuning == null)
            {
                throw new System.InvalidOperationException($"Camera tuning asset is missing: {CameraTuningPath}");
            }

            tuning.name = "M11CameraTuning";
            SerializedObject serialized = new(tuning);
            serialized.FindProperty("puttOffset").vector3Value = new Vector3(5.2f, 2.75f, -6.8f);
            serialized.FindProperty("puttFieldOfView").floatValue = 48f;
            serialized.FindProperty("puttDistanceScale").floatValue = 0.5f;
            serialized.FindProperty("puttHeightScale").floatValue = 0.1f;
            serialized.FindProperty("puttFovPerMeter").floatValue = 0.62f;
            serialized.FindProperty("puttMaximumFieldOfView").floatValue = 64f;
            serialized.FindProperty("resultOffset").vector3Value = new Vector3(9.2f, 6.1f, -10f);
            serialized.FindProperty("resultLookOffset").vector3Value = new Vector3(3.2f, 0.75f, 0f);
            serialized.FindProperty("resultFieldOfView").floatValue = 56f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
        }

        private static void TunePerfectTrail()
        {
            ShotPresentationTuningData tuning = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(PresentationTuningPath);
            if (tuning == null)
            {
                throw new System.InvalidOperationException($"Presentation tuning asset is missing: {PresentationTuningPath}");
            }

            SerializedObject serialized = new(tuning);
            serialized.FindProperty("perfectTrailTime").floatValue = 0.72f;
            serialized.FindProperty("perfectTrailWidth").floatValue = 0.095f;
            serialized.FindProperty("perfectTrailColor").colorValue = new Color(1f, 0.92f, 0.56f, 0.92f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
        {
            GameObject marker = new(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            return marker.transform;
        }

        private static Transform RequireChild(Transform root, string name)
        {
            Transform value = FindRecursive(root, name);
            return value != null
                ? value
                : throw new System.InvalidOperationException($"Required placeholder transform is missing: {name}");
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }
            for (int index = 0; index < root.childCount; index++)
            {
                Transform value = FindRecursive(root.GetChild(index), name);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }
    }
}
