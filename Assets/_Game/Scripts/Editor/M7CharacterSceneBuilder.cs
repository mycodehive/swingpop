using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M7CharacterSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Characters";
        private const string PrefabPath = PrefabFolder + "/PlaceholderGolfer.prefab";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/Character";
        private const string TuningPath = DataFolder + "/M7CharacterTuning.asset";
        private const string MaterialFolder = "Assets/_Game/Materials/Character";

        [MenuItem("SwingPop/M7/Build Character Animation Scene")]
        public static void BuildCharacterAnimationScene()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            if (ball == null || shotFlow == null || holeFlow == null)
            {
                Debug.LogError("M7 builder requires the completed M6 scene with Ball, ShotFlow, and HoleFlow.");
                return;
            }

            CharacterTuningData tuning = LoadOrCreateTuning();
            CreateOrReplacePrefab(tuning);

            GameObject existing = FindInScene(scene, "Placeholder Golfer");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Transform presentationRoot = FindInScene(scene, "Presentation")?.transform;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("M7 builder could not instantiate PlaceholderGolfer.prefab.");
                return;
            }
            instance.name = "Placeholder Golfer";
            if (presentationRoot != null)
            {
                instance.transform.SetParent(presentationRoot, true);
            }

            CharacterGolfController controller = instance.GetComponent<CharacterGolfController>();
            SetObjectReference(controller, "ball", ball);
            SetObjectReference(controller, "shotFlow", shotFlow);
            SetObjectReference(controller, "holeFlow", holeFlow);
            SetObjectReference(controller, "tuning", tuning);

            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            if (overlay != null)
            {
                SetObjectReference(overlay, "character", controller);
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("overlaySize").vector2Value = new Vector2(690f, 820f);
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject systems = FindInScene(scene, "M6 Camera Director Systems");
            if (systems != null)
            {
                systems.name = "M7 Character Animation Systems";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = instance;
            Debug.Log("SwingPop M7 Character / Animation scene wiring completed with PlaceholderGolfer.prefab.");
        }

        private static CharacterTuningData LoadOrCreateTuning()
        {
            CharacterTuningData data = AssetDatabase.LoadAssetAtPath<CharacterTuningData>(TuningPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterTuningData>();
                AssetDatabase.CreateAsset(data, TuningPath);
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateOrReplacePrefab(CharacterTuningData tuning)
        {
            GameObject root = new("Placeholder Golfer");
            CharacterPresentation presentation = root.AddComponent<CharacterPresentation>();
            CharacterAnimationController animationController = root.AddComponent<CharacterAnimationController>();
            CharacterGolfController golfController = root.AddComponent<CharacterGolfController>();

            Transform visualRoot = CreatePivot(root.transform, "Visual Root", Vector3.zero);
            Transform body = CreatePivot(visualRoot, "Body Pivot", new Vector3(0f, 1.2f, 0f));
            Transform head = CreatePivot(visualRoot, "Head Pivot", new Vector3(0f, 2.18f, 0f));
            Transform leftArm = CreatePivot(visualRoot, "Arm L Pivot", new Vector3(-0.48f, 1.67f, 0f));
            Transform rightArm = CreatePivot(visualRoot, "Arm R Pivot", new Vector3(0.48f, 1.67f, 0f));
            Transform leftLeg = CreatePivot(visualRoot, "Leg L Pivot", new Vector3(-0.23f, 0.73f, 0f));
            Transform rightLeg = CreatePivot(visualRoot, "Leg R Pivot", new Vector3(0.23f, 0.73f, 0f));
            Transform clubSocket = CreatePivot(visualRoot, "ClubSocket", tuning.ClubSocketOffset);

            Material skin = LoadOrCreateMaterial("PlaceholderSkin", new Color(1f, 0.72f, 0.55f));
            Material outfit = LoadOrCreateMaterial("PlaceholderOutfit", new Color(0.14f, 0.78f, 0.92f));
            Material accent = LoadOrCreateMaterial("PlaceholderAccent", new Color(1f, 0.28f, 0.58f));
            Material dark = LoadOrCreateMaterial("PlaceholderDark", new Color(0.07f, 0.1f, 0.18f));
            Material metal = LoadOrCreateMaterial("PlaceholderClub", new Color(0.78f, 0.86f, 0.95f));

            CreatePart("Torso", PrimitiveType.Capsule, body, Vector3.zero, new Vector3(0.44f, 0.62f, 0.32f), outfit);
            CreatePart("Belt", PrimitiveType.Cube, body, new Vector3(0f, -0.34f, 0f), new Vector3(0.82f, 0.12f, 0.55f), accent);
            CreatePart("Head", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.62f, 0.72f, 0.62f), skin);
            CreatePart("Hair", PrimitiveType.Sphere, head, new Vector3(0f, 0.18f, -0.03f), new Vector3(0.66f, 0.42f, 0.66f), dark);
            CreateLimb("Arm L", leftArm, skin);
            CreateLimb("Arm R", rightArm, skin);
            CreateLeg("Leg L", leftLeg, dark);
            CreateLeg("Leg R", rightLeg, dark);

            GameObject driver = CreatePivot(clubSocket, "Driver Visual", Vector3.zero).gameObject;
            CreatePart("Driver Shaft", PrimitiveType.Cylinder, driver.transform, new Vector3(0f, -0.62f, 0f), new Vector3(0.035f, 0.62f, 0.035f), metal);
            CreatePart("Driver Head", PrimitiveType.Cube, driver.transform, new Vector3(0.12f, -1.24f, 0f), new Vector3(0.34f, 0.16f, 0.22f), accent);

            GameObject putter = CreatePivot(clubSocket, "Putter Visual", Vector3.zero).gameObject;
            CreatePart("Putter Shaft", PrimitiveType.Cylinder, putter.transform, new Vector3(0f, -0.58f, 0f), new Vector3(0.028f, 0.58f, 0.028f), metal);
            CreatePart("Putter Head", PrimitiveType.Cube, putter.transform, new Vector3(0.16f, -1.16f, 0f), new Vector3(0.42f, 0.08f, 0.13f), dark);
            putter.SetActive(false);

            SetObjectReference(presentation, "visualRoot", visualRoot);
            SetObjectReference(presentation, "bodyPivot", body);
            SetObjectReference(presentation, "headPivot", head);
            SetObjectReference(presentation, "leftArmPivot", leftArm);
            SetObjectReference(presentation, "rightArmPivot", rightArm);
            SetObjectReference(presentation, "leftLegPivot", leftLeg);
            SetObjectReference(presentation, "rightLegPivot", rightLeg);
            SetObjectReference(presentation, "clubSocket", clubSocket);
            SetObjectReference(presentation, "driverVisual", driver);
            SetObjectReference(presentation, "putterVisual", putter);
            SetObjectReference(animationController, "presentation", presentation);
            SetObjectReference(animationController, "tuning", tuning);
            SetObjectReference(golfController, "animationController", animationController);
            SetObjectReference(golfController, "presentation", presentation);
            SetObjectReference(golfController, "tuning", tuning);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Transform CreatePivot(Transform parent, string name, Vector3 localPosition)
        {
            GameObject pivot = new(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private static void CreateLimb(string name, Transform parent, Material material)
        {
            CreatePart(name, PrimitiveType.Capsule, parent, new Vector3(0f, -0.39f, 0f), new Vector3(0.17f, 0.42f, 0.17f), material);
        }

        private static void CreateLeg(string name, Transform parent, Material material)
        {
            CreatePart(name, PrimitiveType.Capsule, parent, new Vector3(0f, -0.38f, 0f), new Vector3(0.22f, 0.42f, 0.22f), material);
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static Material LoadOrCreateMaterial(string assetName, Color color)
        {
            string path = $"{MaterialFolder}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            }
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindRecursive(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform match = FindRecursive(parent.GetChild(index), name);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} is missing serialized property '{propertyName}'.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
