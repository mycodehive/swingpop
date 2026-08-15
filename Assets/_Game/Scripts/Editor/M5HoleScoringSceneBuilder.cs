using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M5HoleScoringSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string HoleFolder = "Assets/_Game/ScriptableObjects/Holes";
        private const string ClubFolder = "Assets/_Game/ScriptableObjects/Clubs";
        private const string MaterialFolder = "Assets/_Game/Materials";
        private const string HolePath = HoleFolder + "/Hole01.asset";
        private const string DriverPath = ClubFolder + "/TemporaryDriver.asset";
        private const string PutterPath = ClubFolder + "/Putter.asset";
        private const string TeeSurfacePath = "Assets/_Game/ScriptableObjects/Terrain/Tee.asset";

        [MenuItem("SwingPop/M5/Build Hole Scoring Scene _F11")]
        public static void BuildHoleScoringScene()
        {
            EnsureFolder(HoleFolder);
            EnsureFolder(ClubFolder);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GameObject systems = FindInScene(scene, "M4 Shot Wind Terrain Systems");
            if (ball == null || shotFlow == null || systems == null)
            {
                Debug.LogError("M5 builder requires the completed M4 Foundation scene.");
                return;
            }

            systems.name = "M5 Hole Scoring Systems";
            HoleData hole = LoadOrCreateHole();
            ClubData driver = LoadOrCreateClub(
                DriverPath,
                "Temporary Driver",
                ClubType.Driver,
                18f,
                35f,
                1f,
                1f);
            ClubData putter = LoadOrCreateClub(
                PutterPath,
                "Putter",
                ClubType.Putter,
                6.5f,
                1f,
                1f,
                1.15f);
            TerrainSurfaceData tee = AssetDatabase.LoadAssetAtPath<TerrainSurfaceData>(TeeSurfacePath);

            HoleFlowController holeFlow = systems.GetComponent<HoleFlowController>();
            if (holeFlow == null)
            {
                holeFlow = systems.AddComponent<HoleFlowController>();
            }

            SetObjectReference(holeFlow, "hole", hole);
            SetObjectReference(holeFlow, "ball", ball);
            SetObjectReference(holeFlow, "shotFlow", shotFlow);
            SetObjectReference(holeFlow, "normalClub", driver);
            SetObjectReference(holeFlow, "putter", putter);
            SetObjectReference(holeFlow, "teeSurface", tee);
            SetObjectReference(shotFlow, "defaultClub", driver);

            ShotDebugOverlay overlay = systems.GetComponent<ShotDebugOverlay>();
            if (overlay != null)
            {
                SetObjectReference(overlay, "holeFlow", holeFlow);
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("overlaySize").vector2Value = new Vector2(600f, 700f);
                serializedOverlay.FindProperty("aimLineLength").floatValue = 90f;
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject course = FindInScene(scene, "M4 Test Course")
                                ?? FindInScene(scene, "M5 Hole 1 Placeholder Course");
            if (course == null)
            {
                Debug.LogError("M5 builder could not find the M4 placeholder course.");
                return;
            }

            course.name = "M5 Hole 1 Placeholder Course";
            GameObject previousCup = FindChild(course.transform, "Cup Target");
            if (previousCup != null)
            {
                Object.DestroyImmediate(previousCup);
            }
            CreateCup(course.transform, hole, holeFlow);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = systems;
            Debug.Log("SwingPop M5 Hole / Scoring / Continuous Shot Flow scene wiring completed.");
        }

        private static HoleData LoadOrCreateHole()
        {
            HoleData data = AssetDatabase.LoadAssetAtPath<HoleData>(HolePath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HoleData>();
                AssetDatabase.CreateAsset(data, HolePath);
            }

            SerializedObject serialized = new(data);
            serialized.FindProperty("holeNumber").intValue = 1;
            serialized.FindProperty("par").intValue = 4;
            serialized.FindProperty("displayName").stringValue = "Sky Island Opening";
            serialized.FindProperty("teePosition").vector3Value = new Vector3(0f, 0.15f, 0f);
            serialized.FindProperty("cupPosition").vector3Value = new Vector3(0f, 0.05f, 78f);
            serialized.FindProperty("holeLength").floatValue = 78f;
            serialized.FindProperty("captureRadius").floatValue = 0.55f;
            serialized.FindProperty("maximumCaptureSpeed").floatValue = 2.4f;
            serialized.FindProperty("maximumHeightDifference").floatValue = 0.45f;
            serialized.FindProperty("assistRadius").floatValue = 1.35f;
            serialized.FindProperty("assistMaximumSpeed").floatValue = 4.5f;
            serialized.FindProperty("assistAcceleration").floatValue = 2.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static ClubData LoadOrCreateClub(
            string path,
            string displayName,
            ClubType type,
            float basePower,
            float loft,
            float carry,
            float roll)
        {
            ClubData data = AssetDatabase.LoadAssetAtPath<ClubData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ClubData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("clubType").enumValueIndex = (int)type;
            serialized.FindProperty("basePower").floatValue = basePower;
            serialized.FindProperty("loftDegrees").floatValue = loft;
            serialized.FindProperty("carryModifier").floatValue = carry;
            serialized.FindProperty("rollModifier").floatValue = roll;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void CreateCup(Transform parent, HoleData hole, HoleFlowController holeFlow)
        {
            GameObject cup = new("Cup Target", typeof(SphereCollider), typeof(CupCaptureController));
            cup.transform.SetParent(parent, false);
            cup.transform.position = hole.CupPosition;
            SphereCollider trigger = cup.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = hole.AssistRadius;
            CupCaptureController capture = cup.GetComponent<CupCaptureController>();
            SetObjectReference(capture, "hole", hole);
            SetObjectReference(capture, "holeFlow", holeFlow);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Cup Marker";
            marker.transform.SetParent(cup.transform, false);
            marker.transform.localPosition = new Vector3(0f, -0.09f, 0f);
            marker.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial("M5Cup", new Color(0.035f, 0.035f, 0.055f));

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Flag Pole";
            pole.transform.SetParent(cup.transform, false);
            pole.transform.localPosition = new Vector3(0.48f, 2.4f, 0f);
            pole.transform.localScale = new Vector3(0.035f, 2.4f, 0.035f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial("M5FlagPole", Color.white);

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(cup.transform, false);
            flag.transform.localPosition = new Vector3(0.95f, 4.25f, 0f);
            flag.transform.localScale = new Vector3(0.9f, 0.55f, 0.04f);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            flag.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial("M5Flag", new Color(1f, 0.25f, 0.55f));
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

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform match = FindRecursive(parent, name);
            return match != null ? match.gameObject : null;
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
