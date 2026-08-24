using System.Collections.Generic;
using SwingPop.AudioSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Presentation;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    internal static class ArtPass1BuildRequestRunner
    {
        private static readonly string RequestPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Build.request"));
        private static readonly string ResultPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Build.result"));

        static ArtPass1BuildRequestRunner()
        {
            // Use the editor update loop instead of a one-shot delay call so an open
            // Editor can consume a request even while asset refreshes are still settling.
            EditorApplication.update -= TryRun;
            EditorApplication.update += TryRun;
        }

        internal static void TryRun()
        {
            if (!System.IO.File.Exists(RequestPath))
            {
                EditorApplication.update -= TryRun;
                return;
            }
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (SceneManager.GetActiveScene().isDirty)
            {
                EditorApplication.update -= TryRun;
                System.IO.File.Delete(RequestPath);
                System.IO.File.WriteAllText(ResultPath, "BLOCKED_DIRTY_SCENE");
                Debug.LogWarning("ART PASS 1 build request was blocked because the active scene has unsaved changes.");
                return;
            }

            EditorApplication.update -= TryRun;
            System.IO.File.Delete(RequestPath);
            try
            {
                ArtPass1SceneBuilder.BuildArtPass1();
                System.IO.File.WriteAllText(ResultPath, "PASS");
            }
            catch (System.Exception exception)
            {
                System.IO.File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
            }
        }
    }

    public static class ArtPass1SceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string CharacterPrefabPath = "Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab";
        private const string CharacterProfilePath = "Assets/_Game/ScriptableObjects/Character/ArtPass1CharacterVisualProfile.asset";
        private const string CharacterMeshFolder = "Assets/_Game/Art/Characters/ArtPass1";
        private const string CourseMeshFolder = "Assets/_Game/Art/Courses/ArtPass1";
        private const string EnvironmentPrefabFolder = "Assets/_Game/Prefabs/Environment/ArtPass1";
        private const string PresentationTuningPath = "Assets/_Game/ScriptableObjects/Polish/ArtPass1ShotPresentationTuning.asset";
        private const string PresentationSourcePath = "Assets/_Game/ScriptableObjects/Environment/M10ShotPresentationTuning.asset";

        [DidReloadScripts]
        private static void ConsumePendingBuildRequestAfterReload()
        {
            ArtPass1BuildRequestRunner.TryRun();
        }

        [MenuItem("SwingPop/Art Pass 1/Build Character and Environment Foundation")]
        public static void BuildArtPass1()
        {
            EnsureFolder(CharacterMeshFolder);
            EnsureFolder(CourseMeshFolder);
            EnsureFolder(EnvironmentPrefabFolder);

            Palette palette = LoadAndTunePalette();
            CharacterVisualProfile profile = LoadOrCreateCharacterProfile();
            BuildCharacterPrefab(profile, palette);
            EnvironmentPrefabs prefabs = BuildEnvironmentPrefabs(palette);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CleanupM11CharacterAdditions(scene);
            BuildCourseDetails(scene, palette);
            ReplaceEnvironment(scene, prefabs);
            ApplyCharacterScenePolish(scene);
            TuneLighting(scene);
            TunePerfectPresentation(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SwingPop ART PASS 1 character/environment foundation built. Gameplay systems were not modified.");
        }

        private static CharacterVisualProfile LoadOrCreateCharacterProfile()
        {
            CharacterVisualProfile profile = AssetDatabase.LoadAssetAtPath<CharacterVisualProfile>(CharacterProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CharacterVisualProfile>();
                profile.name = "ArtPass1CharacterVisualProfile";
                AssetDatabase.CreateAsset(profile, CharacterProfilePath);
            }

            SerializedObject serialized = new(profile);
            serialized.FindProperty("displayName").stringValue = "SwingPop Art Pass 1 Placeholder";
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
            return profile;
        }

        private static void BuildCharacterPrefab(CharacterVisualProfile profile, Palette palette)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CharacterPrefabPath);
            try
            {
                Transform visualRoot = RequireChild(root.transform, "Visual Root");
                Transform bodyPivot = RequireChild(root.transform, "Body Pivot");
                Transform headPivot = RequireChild(root.transform, "Head Pivot");
                Transform leftArmPivot = RequireChild(root.transform, "Arm L Pivot");
                Transform rightArmPivot = RequireChild(root.transform, "Arm R Pivot");
                Transform leftLegPivot = RequireChild(root.transform, "Leg L Pivot");
                Transform rightLegPivot = RequireChild(root.transform, "Leg R Pivot");
                Transform clubSocket = RequireChild(root.transform, "ClubSocket");

                DestroyPrefixedChildren(root.transform, "AP1 ");
                SetRendererEnabled(root.transform, "Torso", false);
                SetRendererEnabled(root.transform, "Hair", false);
                SetRendererEnabled(root.transform, "Arm L", false);
                SetRendererEnabled(root.transform, "Arm R", false);
                SetRendererEnabled(root.transform, "Leg L", false);
                SetRendererEnabled(root.transform, "Leg R", false);

                Mesh torsoMesh = SaveMesh(CreateTaperedPrismMesh(0.92f, 0.64f, 1.2f, 0.58f), CharacterMeshFolder + "/AP1TaperedTorso.asset");
                CreateMeshVisual(bodyPivot, "AP1 Tapered Jacket", torsoMesh, Vector3.zero, Vector3.one, palette.CharacterOutfit, true);
                CreatePrimitive(bodyPivot, "AP1 Pelvis", PrimitiveType.Capsule, new Vector3(0f, -0.57f, 0f), new Vector3(0.34f, 0.18f, 0.29f), palette.CharacterBottom, true);
                CreatePrimitive(bodyPivot, "AP1 Collar L", PrimitiveType.Cube, new Vector3(-0.2f, 0.5f, 0.28f), new Vector3(0.18f, 0.07f, 0.08f), palette.CharacterShoe, true);
                CreatePrimitive(bodyPivot, "AP1 Collar R", PrimitiveType.Cube, new Vector3(0.2f, 0.5f, 0.28f), new Vector3(0.18f, 0.07f, 0.08f), palette.CharacterShoe, true);
                CreatePrimitive(bodyPivot, "AP1 Jacket Accent", PrimitiveType.Cube, new Vector3(0f, -0.34f, 0.3f), new Vector3(0.3f, 0.08f, 0.04f), palette.CharacterAccent, true);

                CreatePrimitive(headPivot, "AP1 Neck", PrimitiveType.Cylinder, new Vector3(0f, -0.55f, 0f), new Vector3(0.16f, 0.16f, 0.16f), palette.CharacterSkin, true);
                CreateHair(headPivot, palette);
                CreateFace(headPivot, palette);
                CreateArm(leftArmPivot, "L", -1f, palette);
                CreateArm(rightArmPivot, "R", 1f, palette);
                CreateLeg(leftLegPivot, "L", palette);
                CreateLeg(rightLegPivot, "R", palette);

                Transform handSocket = CreateMarker(rightArmPivot, "AP1 Hand Socket", new Vector3(0f, -0.92f, 0.03f));
                Transform leftHandSocket = CreateMarker(leftArmPivot, "AP1 Left Hand Socket", new Vector3(0f, -0.92f, 0.03f));
                Transform impactAnchor = CreateMarker(root.transform, "AP1 Impact Anchor", profile.ImpactAnchorOffset);
                Transform headLookTarget = CreateMarker(root.transform, "AP1 Head Look Target", profile.HeadLookOffset);
                clubSocket.localPosition = profile.ClubSocketOffset;

                PolishClub(root.transform, "Driver Visual", "Driver Shaft", "Driver Head", false, palette);
                PolishClub(root.transform, "Putter Visual", "Putter Shaft", "Putter Head", true, palette);

                CharacterVisualAdapter adapter = root.GetComponent<CharacterVisualAdapter>();
                if (adapter == null) adapter = root.AddComponent<CharacterVisualAdapter>();
                Animator animator = root.GetComponentInChildren<Animator>(true);
                adapter.Configure(
                    root.transform,
                    visualRoot,
                    animator,
                    animator != null ? animator.avatar : null,
                    clubSocket,
                    handSocket,
                    leftHandSocket,
                    handSocket,
                    impactAnchor,
                    headLookTarget,
                    profile);

                CharacterPresentation presentation = root.GetComponent<CharacterPresentation>();
                SerializedObject presentationSerialized = new(presentation);
                presentationSerialized.FindProperty("visualAdapter").objectReferenceValue = adapter;
                presentationSerialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureRenderer(root.transform, "Head", palette.CharacterSkin, true);
                ConfigureRenderer(root.transform, "Belt", palette.CharacterAccent, true);
                PrefabUtility.SaveAsPrefabAsset(root, CharacterPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateHair(Transform headPivot, Palette palette)
        {
            CreatePrimitive(headPivot, "AP1 Hair Back", PrimitiveType.Sphere, new Vector3(0f, 0.18f, -0.18f), new Vector3(0.75f, 0.58f, 0.66f), palette.CharacterHair, true);
            for (int index = -1; index <= 1; index++)
            {
                GameObject fringe = CreatePrimitive(headPivot, $"AP1 Hair Fringe {index + 2}", PrimitiveType.Sphere,
                    new Vector3(index * 0.24f, 0.42f - Mathf.Abs(index) * 0.05f, 0.38f),
                    new Vector3(0.24f, 0.4f, 0.2f), palette.CharacterHair, true);
                fringe.transform.localRotation = Quaternion.Euler(18f, 0f, index * -18f);
            }
            CreatePrimitive(headPivot, "AP1 Hair Side L", PrimitiveType.Sphere, new Vector3(-0.55f, 0.02f, -0.02f), new Vector3(0.2f, 0.48f, 0.25f), palette.CharacterHair, true);
            CreatePrimitive(headPivot, "AP1 Hair Side R", PrimitiveType.Sphere, new Vector3(0.55f, 0.02f, -0.02f), new Vector3(0.2f, 0.48f, 0.25f), palette.CharacterHair, true);
        }

        private static void CreateFace(Transform headPivot, Palette palette)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(headPivot, side < 0 ? "AP1 Eye L" : "AP1 Eye R", PrimitiveType.Sphere,
                    new Vector3(side * 0.2f, 0.04f, 0.62f), new Vector3(0.07f, 0.13f, 0.035f), palette.CharacterHair, false);
                CreatePrimitive(headPivot, side < 0 ? "AP1 Brow L" : "AP1 Brow R", PrimitiveType.Cube,
                    new Vector3(side * 0.2f, 0.2f, 0.64f), new Vector3(0.1f, 0.025f, 0.02f), palette.CharacterHair, false);
            }
            CreatePrimitive(headPivot, "AP1 Mouth", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.64f), new Vector3(0.1f, 0.02f, 0.02f), palette.CharacterAccent, false);
        }

        private static void CreateArm(Transform pivot, string suffix, float side, Palette palette)
        {
            CreatePrimitive(pivot, $"AP1 Shoulder {suffix}", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f), new Vector3(0.23f, 0.2f, 0.24f), palette.CharacterOutfit, true);
            CreatePrimitive(pivot, $"AP1 Upper Arm {suffix}", PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f), new Vector3(0.18f, 0.29f, 0.18f), palette.CharacterOutfit, true);
            CreatePrimitive(pivot, $"AP1 Lower Arm {suffix}", PrimitiveType.Capsule, new Vector3(side * 0.015f, -0.68f, 0.04f), new Vector3(0.15f, 0.25f, 0.15f), palette.CharacterOutfit, true);
            CreatePrimitive(pivot, $"AP1 Cuff {suffix}", PrimitiveType.Cylinder, new Vector3(0f, -0.86f, 0.04f), new Vector3(0.17f, 0.06f, 0.17f), palette.CharacterShoe, true);
            CreatePrimitive(pivot, $"AP1 Hand {suffix}", PrimitiveType.Sphere, new Vector3(0f, -0.98f, 0.04f), new Vector3(0.17f, 0.21f, 0.16f), palette.CharacterSkin, true);
        }

        private static void CreateLeg(Transform pivot, string suffix, Palette palette)
        {
            CreatePrimitive(pivot, $"AP1 Upper Leg {suffix}", PrimitiveType.Capsule, new Vector3(0f, -0.25f, 0f), new Vector3(0.23f, 0.3f, 0.22f), palette.CharacterBottom, true);
            CreatePrimitive(pivot, $"AP1 Lower Leg {suffix}", PrimitiveType.Capsule, new Vector3(0f, -0.68f, 0.02f), new Vector3(0.19f, 0.27f, 0.18f), palette.CharacterBottom, true);
            CreatePrimitive(pivot, $"AP1 Sock {suffix}", PrimitiveType.Cylinder, new Vector3(0f, -0.88f, 0.03f), new Vector3(0.2f, 0.07f, 0.19f), palette.CharacterShoe, true);
            CreatePrimitive(pivot, $"AP1 Shoe {suffix}", PrimitiveType.Cube, new Vector3(0f, -0.98f, 0.14f), new Vector3(0.28f, 0.14f, 0.43f), palette.CharacterShoe, true);
            CreatePrimitive(pivot, $"AP1 Shoe Sole {suffix}", PrimitiveType.Cube, new Vector3(0f, -1.06f, 0.15f), new Vector3(0.3f, 0.035f, 0.45f), palette.CharacterBottom, true);
        }

        private static void PolishClub(Transform root, string visualName, string shaftName, string headName, bool putter, Palette palette)
        {
            Transform visual = RequireChild(root, visualName);
            Transform shaft = RequireChild(root, shaftName);
            Transform head = RequireChild(root, headName);
            shaft.localPosition = new Vector3(0f, putter ? -0.78f : -1.02f, 0f);
            shaft.localScale = new Vector3(0.045f, putter ? 0.82f : 1.08f, 0.045f);
            head.localPosition = new Vector3(0f, putter ? -1.62f : -2.13f, putter ? 0.1f : 0.15f);
            head.localScale = putter ? new Vector3(0.42f, 0.08f, 0.17f) : new Vector3(0.3f, 0.16f, 0.42f);
            ConfigureRenderer(root, shaftName, palette.ClubShaft, true);
            ConfigureRenderer(root, headName, palette.ClubHead, true);
            CreatePrimitive(visual, putter ? "AP1 Putter Grip" : "AP1 Driver Grip", PrimitiveType.Cylinder,
                new Vector3(0f, 0.08f, 0f), new Vector3(0.07f, 0.28f, 0.07f), palette.CharacterBottom, true);
        }

        private static EnvironmentPrefabs BuildEnvironmentPrefabs(Palette palette)
        {
            EnvironmentPrefabs result = new();
            for (int variant = 0; variant < 3; variant++)
            {
                string path = $"{EnvironmentPrefabFolder}/ArtPass1Tree_{(char)('A' + variant)}.prefab";
                GameObject tree = new($"ArtPass1Tree_{(char)('A' + variant)}");
                float trunkHeight = 1.05f + variant * 0.16f;
                CreatePrimitive(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, trunkHeight, 0f), new Vector3(0.34f + variant * 0.03f, trunkHeight, 0.34f + variant * 0.03f), palette.Trunk, true);
                int blobs = 4 + variant;
                for (int index = 0; index < blobs; index++)
                {
                    float angle = Mathf.PI * 2f * index / blobs + variant * 0.35f;
                    Vector3 position = new(Mathf.Cos(angle) * (0.55f + variant * 0.08f), 2.3f + Mathf.Sin(index * 1.7f) * 0.25f + variant * 0.22f, Mathf.Sin(angle) * 0.45f);
                    Vector3 scale = new(0.8f + (index % 2) * 0.18f, 0.72f + ((index + variant) % 2) * 0.16f, 0.78f);
                    CreatePrimitive(tree.transform, $"Canopy {index + 1}", PrimitiveType.Sphere, position, scale,
                        index % 3 == 0 ? palette.FoliageLight : variant == 2 ? palette.FoliageMedium : palette.Foliage, index < 2);
                }
                result.Trees[variant] = SavePrefab(tree, path);
            }

            Mesh flowerMesh = SaveMesh(CreateFlowerPatchMesh(), CourseMeshFolder + "/ArtPass1FlowerPatch.asset");
            GameObject flower = new("ArtPass1FlowerPatch", typeof(MeshFilter), typeof(MeshRenderer));
            flower.GetComponent<MeshFilter>().sharedMesh = flowerMesh;
            flower.GetComponent<MeshRenderer>().sharedMaterials = new[] { palette.FoliageMedium, palette.FlowerPink, palette.FlowerGold };
            flower.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            result.Flowers = SavePrefab(flower, EnvironmentPrefabFolder + "/ArtPass1FlowerPatch.prefab");

            for (int variant = 0; variant < 3; variant++)
            {
                GameObject cloud = new($"ArtPass1Cloud_{(char)('A' + variant)}");
                int blobs = 3 + variant;
                for (int index = 0; index < blobs; index++)
                {
                    float x = (index - (blobs - 1) * 0.5f) * 1.05f;
                    CreatePrimitive(cloud.transform, $"Cloud Blob {index + 1}", PrimitiveType.Sphere,
                        new Vector3(x, Mathf.Sin(index * 1.9f + variant) * 0.22f, (index % 2) * 0.18f),
                        new Vector3(1.15f, 0.65f + (index % 2) * 0.15f, 0.85f), palette.Cloud, false);
                }
                result.Clouds[variant] = SavePrefab(cloud, $"{EnvironmentPrefabFolder}/ArtPass1Cloud_{(char)('A' + variant)}.prefab");
            }

            result.Windmill = BuildWindmillPrefab(palette);
            result.Waterfall = BuildWaterfallPrefab(palette);
            return result;
        }

        private static GameObject BuildWindmillPrefab(Palette palette)
        {
            Mesh towerMesh = SaveMesh(CreateTaperedPrismMesh(1.45f, 2.2f, 5f, 2.05f), CourseMeshFolder + "/ArtPass1WindmillTower.asset");
            GameObject root = new("ArtPass1Windmill");
            CreateMeshVisual(root.transform, "Tower", towerMesh, new Vector3(0f, 2.5f, 0f), Vector3.one, palette.WindmillBody, true);
            CreatePrimitive(root.transform, "Roof", PrimitiveType.Sphere, new Vector3(0f, 5.05f, 0f), new Vector3(1.25f, 0.42f, 1.2f), palette.WindmillRoof, true);
            CreatePrimitive(root.transform, "Door", PrimitiveType.Cube, new Vector3(0f, 1.05f, -1.06f), new Vector3(0.48f, 0.86f, 0.06f), palette.WindmillRoof, true);
            CreatePrimitive(root.transform, "Window", PrimitiveType.Cube, new Vector3(0f, 3.25f, -1.05f), new Vector3(0.34f, 0.34f, 0.06f), palette.WaterDeep, true);
            Transform rotor = CreateMarker(root.transform, "Rotor", new Vector3(0f, 3.85f, -1.22f));
            CreatePrimitive(rotor, "Hub", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.28f, palette.WindmillRoof, true);
            for (int index = 0; index < 4; index++)
            {
                float angle = index * 90f;
                Vector3 bladePosition = Quaternion.Euler(0f, 0f, angle) * Vector3.up * 1.15f;
                GameObject blade = CreatePrimitive(rotor, $"Blade {index + 1}", PrimitiveType.Cube,
                    bladePosition, new Vector3(0.17f, 1.25f, 0.07f), palette.WindmillBlade, true);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            return SavePrefab(root, EnvironmentPrefabFolder + "/ArtPass1Windmill.prefab");
        }

        private static GameObject BuildWaterfallPrefab(Palette palette)
        {
            GameObject root = new("ArtPass1WaterfallIsland");
            CreatePrimitive(root.transform, "Rock Core", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(5.7f, 2.3f, 4.8f), palette.CliffSide, false);
            CreatePrimitive(root.transform, "Grass Crown", PrimitiveType.Sphere, new Vector3(0f, 1.65f, 0f), new Vector3(5.25f, 0.72f, 4.4f), palette.Rough, false);
            CreatePrimitive(root.transform, "Waterfall Upper", PrimitiveType.Cube, new Vector3(0f, 0.1f, -4.55f), new Vector3(0.78f, 2.35f, 0.08f), palette.WaterShallow, false);
            CreatePrimitive(root.transform, "Waterfall Lower", PrimitiveType.Cube, new Vector3(0f, -2.9f, -4.65f), new Vector3(1.12f, 1.75f, 0.07f), palette.WaterHighlight, false);
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(root.transform, side < 0 ? "Waterfall Frame L" : "Waterfall Frame R", PrimitiveType.Sphere,
                    new Vector3(side * 1.35f, -0.6f, -4.25f), new Vector3(1.1f, 2.15f, 0.72f), palette.CliffRim, false);
            }
            for (int index = -1; index <= 1; index++)
            {
                CreatePrimitive(root.transform, $"Mist {index + 2}", PrimitiveType.Sphere,
                    new Vector3(index * 0.75f, -4.6f, -4.8f), new Vector3(0.9f, 0.38f, 0.55f), palette.Cloud, false);
            }
            CreatePrimitive(root.transform, "Top Tree Trunk", PrimitiveType.Cylinder, new Vector3(-2.2f, 2.4f, 0f), new Vector3(0.22f, 0.75f, 0.22f), palette.Trunk, false);
            CreatePrimitive(root.transform, "Top Tree Canopy", PrimitiveType.Sphere, new Vector3(-2.2f, 3.45f, 0f), new Vector3(0.95f, 0.82f, 0.9f), palette.FoliageLight, false);
            return SavePrefab(root, EnvironmentPrefabFolder + "/ArtPass1WaterfallIsland.prefab");
        }

        private static void CleanupM11CharacterAdditions(Scene scene)
        {
            GameObject character = FindInScene(scene, "Placeholder Golfer");
            if (character == null) return;
            foreach (string name in new[]
                     {
                         "M11 Character Silhouette Additions", "Hand L", "Hand R", "Shoe L", "Shoe R",
                         "Hair Tuft 1", "Hair Tuft 2", "Hair Tuft 3"
                     })
            {
                DestroyNamedChildren(character.transform, name);
            }
        }

        private static void ApplyCharacterScenePolish(Scene scene)
        {
            GameObject character = FindInScene(scene, "Placeholder Golfer");
            if (character == null) return;
            Transform visualRoot = FindRecursive(character.transform, "Visual Root");
            if (visualRoot != null) visualRoot.localScale = Vector3.one * 1.28f;
            CharacterVisualAdapter adapter = character.GetComponent<CharacterVisualAdapter>();
            if (adapter != null) EditorUtility.SetDirty(adapter);
        }

        private static void BuildCourseDetails(Scene scene, Palette palette)
        {
            GameObject polishRoot = FindInScene(scene, "M11 Visual Polish");
            if (polishRoot == null) throw new System.InvalidOperationException("M11 Visual Polish root is missing.");
            GameObject old = FindInScene(scene, "Art Pass 1 Course Details");
            if (old != null) Object.DestroyImmediate(old);
            GameObject root = new("Art Pass 1 Course Details");
            root.transform.SetParent(polishRoot.transform, false);

            Mesh sandShade = SaveMesh(CreateEllipseMesh(new Vector3(7.5f, 0.035f, 54f), 3.2f, 4.8f, 28, 0.07f), CourseMeshFolder + "/AP1BunkerInnerShade.asset");
            CreateMeshVisual(root.transform, "Bunker Inner Shade", sandShade, Vector3.zero, Vector3.one, palette.SandShade, false);

            Mesh waterHighlight = SaveMesh(CreateEllipseRingMesh(new Vector3(-11.2f, 0.055f, 33.5f), 3.7f, 6.4f, 3.3f, 5.75f, 32), CourseMeshFolder + "/AP1WaterHighlight.asset");
            GameObject water = CreateMeshVisual(root.transform, "Water Moving Highlight", waterHighlight, Vector3.zero, Vector3.one, palette.WaterHighlight, false);

            Mesh cupRing = SaveMesh(CreateEllipseRingMesh(new Vector3(0f, 0.12f, 78f), 0.52f, 0.52f, 0.38f, 0.38f, 24), CourseMeshFolder + "/AP1CupReadabilityRing.asset");
            CreateMeshVisual(root.transform, "Cup Readability Ring", cupRing, Vector3.zero, Vector3.one, palette.FlowerGold, false);

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>();
            if (motion != null)
            {
                SerializedObject serialized = new(motion);
                serialized.FindProperty("waterHighlight").objectReferenceValue = water.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssignMaterial(FindInScene(scene, "Organic Sky Island Shell"), new[] { palette.Rough, palette.CliffSide });
            AssignMaterial(FindInScene(scene, "Cliff Grass Rim"), new[] { palette.CliffRim });
            AssignMaterial(FindInScene(scene, "Fairway Fringe"), new[] { palette.Fringe });
            AssignMaterial(FindInScene(scene, "Curved Fairway"), new[] { palette.Fairway, palette.FairwayAlternate });
            AssignMaterial(FindInScene(scene, "Organic Tee"), new[] { palette.Tee });
            AssignMaterial(FindInScene(scene, "Green Fringe"), new[] { palette.GreenFringe });
            AssignMaterial(FindInScene(scene, "Organic Green"), new[] { palette.Green });
            AssignMaterial(FindInScene(scene, "Bunker Sand Depression"), new[] { palette.Sand });
            AssignMaterial(FindInScene(scene, "Water Deep Edge"), new[] { palette.WaterDeep });
            AssignMaterial(FindInScene(scene, "Water Shallow Highlight"), new[] { palette.WaterShallow });
        }

        private static void ReplaceEnvironment(Scene scene, EnvironmentPrefabs prefabs)
        {
            GameObject old = FindInScene(scene, "Art Pass 1 Environment");
            if (old != null) Object.DestroyImmediate(old);
            GameObject environment = FindInScene(scene, "Environment");
            GameObject root = new("Art Pass 1 Environment");
            root.transform.SetParent(environment != null ? environment.transform : null, false);

            SetNamedObjectsActive(scene, "Stylized Tree ", false);
            SetNamedObjectsActive(scene, "Flower Patch ", false);
            SetNamedObjectsActive(scene, "Cloud Cluster ", false);
            SetObjectActive(scene, "Windmill Landmark", false);
            SetObjectActive(scene, "Waterfall Island Landmark", false);

            Vector3[] treePositions =
            {
                new(-13.5f, 0f, 11f), new(-15.3f, 0f, 16.5f), new(-12.6f, 0f, 21f),
                new(13.2f, 0f, 27f), new(15.2f, 0f, 32f), new(12.6f, 0f, 37f),
                new(-12.2f, 0f, 66f), new(12.4f, 0f, 79f)
            };
            for (int index = 0; index < treePositions.Length; index++)
            {
                GameObject tree = InstantiatePrefab(prefabs.Trees[index % 3], root.transform, $"Art Pass 1 Tree {index + 1:00}");
                tree.transform.position = treePositions[index];
                float scale = index is 1 or 4 ? 1.12f : index >= 6 ? 0.82f : 0.94f;
                tree.transform.localScale = Vector3.one * scale;
                tree.transform.rotation = Quaternion.Euler(0f, index * 37f + 14f, 0f);
            }

            Vector3[] flowerPositions =
            {
                new(-5.6f, 0.1f, 1.6f), new(5.2f, 0.1f, 2.9f), new(-8.9f, 0.1f, 24f),
                new(9.4f, 0.1f, 44f), new(-9f, 0.1f, 69f), new(8f, 0.1f, 72.5f)
            };
            for (int index = 0; index < flowerPositions.Length; index++)
            {
                GameObject flowers = InstantiatePrefab(prefabs.Flowers, root.transform, $"Art Pass 1 Flowers {index + 1:00}");
                flowers.transform.position = flowerPositions[index];
                flowers.transform.localScale = Vector3.one * (0.78f + (index % 3) * 0.08f);
                flowers.transform.rotation = Quaternion.Euler(0f, index * 51f, 0f);
            }

            Vector3[] cloudPositions =
            {
                new(-46f, 24f, 34f), new(36f, 30f, 58f), new(-30f, 36f, 92f),
                new(48f, 22f, 122f), new(0f, 42f, 155f)
            };
            Transform[] clouds = new Transform[cloudPositions.Length];
            for (int index = 0; index < cloudPositions.Length; index++)
            {
                GameObject cloud = InstantiatePrefab(prefabs.Clouds[index % 3], root.transform, $"Art Pass 1 Cloud {index + 1:00}");
                cloud.transform.position = cloudPositions[index];
                cloud.transform.localScale = Vector3.one * (2.6f + index * 0.18f);
                cloud.transform.rotation = Quaternion.Euler(0f, index * 29f, index % 2 == 0 ? -3f : 3f);
                clouds[index] = cloud.transform;
            }

            GameObject windmill = InstantiatePrefab(prefabs.Windmill, root.transform, "Art Pass 1 Windmill Landmark");
            windmill.transform.position = new Vector3(11f, 0f, 82f);
            windmill.transform.localScale = Vector3.one * 1.08f;
            windmill.transform.rotation = Quaternion.Euler(0f, -18f, 0f);

            GameObject waterfall = InstantiatePrefab(prefabs.Waterfall, root.transform, "Art Pass 1 Waterfall Landmark");
            waterfall.transform.position = new Vector3(-28f, 5f, 103f);
            waterfall.transform.localScale = Vector3.one * 1.25f;
            waterfall.transform.rotation = Quaternion.Euler(0f, -5f, 0f);

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>();
            if (motion != null)
            {
                SerializedObject serialized = new(motion);
                serialized.FindProperty("windmillRotor").objectReferenceValue = FindRecursive(windmill.transform, "Rotor");
                SerializedProperty cloudProperty = serialized.FindProperty("driftingClouds");
                cloudProperty.arraySize = clouds.Length;
                for (int index = 0; index < clouds.Length; index++) cloudProperty.GetArrayElementAtIndex(index).objectReferenceValue = clouds[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void TuneLighting(Scene scene)
        {
            Light light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.94f, 0.86f);
                light.intensity = 1.12f;
                light.transform.rotation = Quaternion.Euler(49f, -33f, 0f);
                light.shadowStrength = 0.68f;
                light.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.66f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.45f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.13f, 0.19f, 0.22f);
            RenderSettings.fogColor = new Color(0.56f, 0.78f, 0.9f);
            RenderSettings.fogStartDistance = 92f;
            RenderSettings.fogEndDistance = 205f;
            Material skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", new Color(0.22f, 0.6f, 0.9f));
                if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", new Color(0.18f, 0.28f, 0.38f));
                if (skybox.HasProperty("_AtmosphereThickness")) skybox.SetFloat("_AtmosphereThickness", 0.78f);
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 1.06f);
                EditorUtility.SetDirty(skybox);
            }
        }

        private static void TunePerfectPresentation(Scene scene)
        {
            ShotPresentationTuningData source = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(PresentationSourcePath);
            ShotPresentationTuningData tuning = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(PresentationTuningPath);
            if (tuning == null)
            {
                tuning = Object.Instantiate(source);
                tuning.name = "ArtPass1ShotPresentationTuning";
                AssetDatabase.CreateAsset(tuning, PresentationTuningPath);
            }
            else
            {
                EditorUtility.CopySerialized(source, tuning);
            }
            SerializedObject serialized = new(tuning);
            serialized.FindProperty("perfectImpactScale").floatValue = 1.38f;
            serialized.FindProperty("perfectImpactParticles").intValue = 24;
            serialized.FindProperty("perfectTrailTime").floatValue = 0.72f;
            serialized.FindProperty("perfectTrailWidth").floatValue = 0.095f;
            serialized.FindProperty("perfectImpactColor").colorValue = new Color(1f, 0.84f, 0.2f, 1f);
            serialized.FindProperty("perfectTrailColor").colorValue = new Color(1f, 0.92f, 0.56f, 0.92f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssignTuning(Object.FindAnyObjectByType<ImpactVfxController>(), tuning);
            AssignTuning(Object.FindAnyObjectByType<BallTrailController>(), tuning);
            AssignTuning(Object.FindAnyObjectByType<LandingVfxController>(), tuning);
            AssignTuning(Object.FindAnyObjectByType<HoleInVfxController>(), tuning);
            AssignTuning(Object.FindAnyObjectByType<GameplayAudioController>(), tuning);
        }

        private static void AssignTuning(Object target, ShotPresentationTuningData tuning)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty("tuning");
            if (property != null)
            {
                property.objectReferenceValue = tuning;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Palette LoadAndTunePalette()
        {
            Palette palette = new()
            {
                Rough = Tune("Assets/_Game/Materials/Polish/M11Rough.mat", new Color(0.07f, 0.29f, 0.14f), 0.08f),
                Fringe = Tune("Assets/_Game/Materials/Polish/M11Fringe.mat", new Color(0.1f, 0.43f, 0.2f), 0.1f),
                Fairway = Tune("Assets/_Game/Materials/Polish/M11Fairway.mat", new Color(0.19f, 0.59f, 0.25f), 0.18f),
                FairwayAlternate = Tune("Assets/_Game/Materials/Polish/M11FairwayAlternate.mat", new Color(0.25f, 0.68f, 0.3f), 0.2f),
                Tee = Tune("Assets/_Game/Materials/Polish/M11Tee.mat", new Color(0.3f, 0.73f, 0.38f), 0.24f),
                GreenFringe = Tune("Assets/_Game/Materials/Polish/M11GreenFringe.mat", new Color(0.14f, 0.5f, 0.23f), 0.16f),
                Green = Tune("Assets/_Game/Materials/Polish/M11Green.mat", new Color(0.37f, 0.78f, 0.4f), 0.3f),
                Sand = Tune("Assets/_Game/Materials/Polish/M11Sand.mat", new Color(0.91f, 0.7f, 0.4f), 0.08f),
                CliffSide = Tune("Assets/_Game/Materials/Polish/M11CliffSide.mat", new Color(0.25f, 0.22f, 0.36f), 0.06f),
                CliffRim = Tune("Assets/_Game/Materials/Polish/M11CliffRim.mat", new Color(0.4f, 0.35f, 0.5f), 0.1f),
                WaterDeep = Tune("Assets/_Game/Materials/Polish/M11WaterDeep.mat", new Color(0.03f, 0.3f, 0.54f), 0.58f),
                WaterShallow = Tune("Assets/_Game/Materials/Polish/M11WaterShallow.mat", new Color(0.08f, 0.65f, 0.78f, 0.7f), 0.7f),
                CharacterSkin = Tune("Assets/_Game/Materials/Polish/M11CharacterSkin.mat", new Color(0.98f, 0.69f, 0.54f), 0.28f),
                CharacterHair = Tune("Assets/_Game/Materials/Polish/M11CharacterHair.mat", new Color(0.035f, 0.055f, 0.12f), 0.12f),
                CharacterOutfit = Tune("Assets/_Game/Materials/Polish/M11CharacterOutfit.mat", new Color(0.035f, 0.47f, 0.68f), 0.2f),
                CharacterAccent = Tune("Assets/_Game/Materials/Polish/M11CharacterAccent.mat", new Color(0.94f, 0.2f, 0.5f), 0.28f),
                CharacterBottom = Tune("Assets/_Game/Materials/Polish/M11CharacterBottom.mat", new Color(0.055f, 0.09f, 0.18f), 0.12f),
                CharacterShoe = Tune("Assets/_Game/Materials/Polish/M11CharacterShoe.mat", new Color(0.91f, 0.94f, 0.94f), 0.24f),
                ClubShaft = Tune("Assets/_Game/Materials/Polish/M11ClubShaft.mat", new Color(0.68f, 0.74f, 0.78f), 0.62f),
                ClubHead = Tune("Assets/_Game/Materials/Polish/M11ClubHead.mat", new Color(0.04f, 0.62f, 0.68f), 0.55f),
                FlowerPink = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Polish/M11FlowerPink.mat"),
                FlowerGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Polish/M11FlowerGold.mat"),
                Trunk = Tune("Assets/_Game/Materials/Environment/M10Trunk.mat", new Color(0.24f, 0.12f, 0.08f), 0.06f),
                Foliage = Tune("Assets/_Game/Materials/Environment/M10Foliage.mat", new Color(0.04f, 0.35f, 0.18f), 0.08f),
                FoliageLight = Tune("Assets/_Game/Materials/Environment/M10FoliageLight.mat", new Color(0.12f, 0.52f, 0.25f), 0.1f),
                Cloud = Tune("Assets/_Game/Materials/Environment/M10Cloud.mat", new Color(0.91f, 0.97f, 1f), 0.12f),
                WindmillBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/M10WindmillCream.mat"),
                WindmillRoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/M10WindmillRoof.mat"),
                WindmillBlade = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/M10WindmillBlade.mat")
            };
            palette.FoliageMedium = palette.Fringe;
            palette.SandShade = Material("AP1SandShade", new Color(0.76f, 0.53f, 0.27f), 0.05f);
            palette.WaterHighlight = Material("AP1WaterHighlight", new Color(0.45f, 0.92f, 1f, 0.46f), 0.78f, true);
            return palette;
        }

        private static Material Tune(string path, Color color, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new System.InvalidOperationException($"Required material is missing: {path}");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Material(string name, Color color, float smoothness, bool transparent = false)
        {
            string path = $"Assets/_Game/Materials/Polish/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateTaperedPrismMesh(float topWidth, float bottomWidth, float height, float depth)
        {
            float top = height * 0.5f;
            float bottom = -top;
            Vector3[] vertices =
            {
                new(-bottomWidth * 0.5f, bottom, -depth * 0.5f), new(bottomWidth * 0.5f, bottom, -depth * 0.5f),
                new(bottomWidth * 0.5f, bottom, depth * 0.5f), new(-bottomWidth * 0.5f, bottom, depth * 0.5f),
                new(-topWidth * 0.5f, top, -depth * 0.5f), new(topWidth * 0.5f, top, -depth * 0.5f),
                new(topWidth * 0.5f, top, depth * 0.5f), new(-topWidth * 0.5f, top, depth * 0.5f)
            };
            int[] triangles =
            {
                0,1,2, 0,2,3, 4,6,5, 4,7,6,
                0,4,5, 0,5,1, 1,5,6, 1,6,2,
                2,6,7, 2,7,3, 3,7,4, 3,4,0
            };
            Mesh mesh = new() { name = "AP1 Tapered Prism", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFlowerPatchMesh()
        {
            List<Vector3> vertices = new();
            List<int>[] triangles = { new(), new(), new() };
            AddHorizontalDiamond(vertices, triangles[0], new Vector3(-0.25f, 0.03f, 0f), 0.48f, 0.2f);
            AddHorizontalDiamond(vertices, triangles[0], new Vector3(0.25f, 0.04f, 0.1f), 0.4f, 0.22f);
            Vector3[] centers =
            {
                new(-0.4f, 0.24f, -0.05f), new(0f, 0.32f, 0.12f), new(0.42f, 0.23f, -0.08f),
                new(-0.12f, 0.2f, -0.28f), new(0.22f, 0.18f, 0.34f)
            };
            for (int index = 0; index < centers.Length; index++)
            {
                AddFlowerDiamond(vertices, triangles[index % 2 == 0 ? 1 : 2], centers[index], 0.18f);
            }
            Mesh mesh = new() { name = "AP1 Flower Patch", vertices = vertices.ToArray(), subMeshCount = 3 };
            for (int index = 0; index < triangles.Length; index++) mesh.SetTriangles(triangles[index], index);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddHorizontalDiamond(List<Vector3> vertices, List<int> triangles, Vector3 center, float width, float depth)
        {
            int start = vertices.Count;
            vertices.Add(center + Vector3.left * width);
            vertices.Add(center + Vector3.forward * depth);
            vertices.Add(center + Vector3.right * width);
            vertices.Add(center + Vector3.back * depth);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private static void AddFlowerDiamond(List<Vector3> vertices, List<int> triangles, Vector3 center, float radius)
        {
            int start = vertices.Count;
            vertices.Add(center + new Vector3(-radius, 0f, 0f));
            vertices.Add(center + new Vector3(0f, radius * 0.5f, radius));
            vertices.Add(center + new Vector3(radius, 0f, 0f));
            vertices.Add(center + new Vector3(0f, radius * 0.5f, -radius));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private static Mesh CreateEllipseMesh(Vector3 center, float radiusX, float radiusZ, int segments, float variation)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            vertices[0] = center;
            List<int> triangles = new();
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                float organic = 1f + Mathf.Sin(angle * 3f + 0.4f) * variation;
                vertices[index + 1] = center + new Vector3(Mathf.Cos(angle) * radiusX * organic, 0f, Mathf.Sin(angle) * radiusZ * organic);
            }
            for (int index = 0; index < segments; index++)
            {
                int next = (index + 1) % segments;
                triangles.Add(0); triangles.Add(next + 1); triangles.Add(index + 1);
            }
            Mesh mesh = new() { name = "AP1 Organic Ellipse", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateEllipseRingMesh(Vector3 center, float outerX, float outerZ, float innerX, float innerZ, int segments)
        {
            Vector3[] vertices = new Vector3[segments * 2];
            List<int> triangles = new();
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                vertices[index * 2] = center + new Vector3(Mathf.Cos(angle) * outerX, 0f, Mathf.Sin(angle) * outerZ);
                vertices[index * 2 + 1] = center + new Vector3(Mathf.Cos(angle) * innerX, 0.01f, Mathf.Sin(angle) * innerZ);
                int next = (index + 1) % segments;
                triangles.Add(index * 2); triangles.Add(next * 2); triangles.Add(index * 2 + 1);
                triangles.Add(next * 2); triangles.Add(next * 2 + 1); triangles.Add(index * 2 + 1);
            }
            Mesh mesh = new() { name = "AP1 Organic Ring", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateMeshVisual(Transform parent, string name, Mesh mesh, Vector3 localPosition, Vector3 localScale, Material material, bool castShadow)
        {
            GameObject value = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            value.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = value.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return value;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, bool castShadow)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            Object.DestroyImmediate(value.GetComponent<Collider>());
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = castShadow;
            return value;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
        {
            GameObject marker = new(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            return marker.transform;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
        {
            GameObject value = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            value.name = name;
            return value;
        }

        private static Mesh SaveMesh(Mesh source, string path)
        {
            source.name = System.IO.Path.GetFileNameWithoutExtension(path);
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(source, path);
                return source;
            }
            EditorUtility.CopySerialized(source, asset);
            Object.DestroyImmediate(source);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetRendererEnabled(Transform root, string name, bool enabled)
        {
            Transform target = FindRecursive(root, name);
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer != null) renderer.enabled = enabled;
        }

        private static void ConfigureRenderer(Transform root, string name, Material material, bool castShadow)
        {
            Transform target = FindRecursive(root, name);
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer == null) return;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void AssignMaterial(GameObject target, Material[] materials)
        {
            if (target == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterials = materials;
        }

        private static void DestroyPrefixedChildren(Transform root, string prefix)
        {
            List<GameObject> targets = new();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root && child.name.StartsWith(prefix)) targets.Add(child.gameObject);
            }
            targets.Sort((left, right) => GetDepth(right.transform).CompareTo(GetDepth(left.transform)));
            foreach (GameObject target in targets) if (target != null) Object.DestroyImmediate(target);
        }

        private static void DestroyNamedChildren(Transform root, string name)
        {
            List<GameObject> targets = new();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root && child.name == name) targets.Add(child.gameObject);
            }
            foreach (GameObject target in targets) Object.DestroyImmediate(target);
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            while (transform.parent != null) { depth++; transform = transform.parent; }
            return depth;
        }

        private static void SetNamedObjectsActive(Scene scene, string prefix, bool active)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name.StartsWith(prefix)) transform.gameObject.SetActive(active);
                }
            }
        }

        private static void SetObjectActive(Scene scene, string name, bool active)
        {
            GameObject value = FindInScene(scene, name);
            if (value != null) value.SetActive(active);
        }

        private static Transform RequireChild(Transform root, string name)
        {
            Transform value = FindRecursive(root, name);
            if (value == null) throw new System.InvalidOperationException($"Required character transform is missing: {name}");
            return value;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = FindRecursive(root.transform, name);
                if (result != null) return result.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform result = FindRecursive(root.GetChild(index), name);
                if (result != null) return result;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private sealed class EnvironmentPrefabs
        {
            public readonly GameObject[] Trees = new GameObject[3];
            public readonly GameObject[] Clouds = new GameObject[3];
            public GameObject Flowers;
            public GameObject Windmill;
            public GameObject Waterfall;
        }

        private sealed class Palette
        {
            public Material Rough, Fringe, Fairway, FairwayAlternate, Tee, GreenFringe, Green, Sand, SandShade;
            public Material CliffSide, CliffRim, WaterDeep, WaterShallow, WaterHighlight;
            public Material CharacterSkin, CharacterHair, CharacterOutfit, CharacterAccent, CharacterBottom, CharacterShoe;
            public Material ClubShaft, ClubHead, FlowerPink, FlowerGold;
            public Material Trunk, Foliage, FoliageMedium, FoliageLight, Cloud;
            public Material WindmillBody, WindmillRoof, WindmillBlade;
        }
    }
}
