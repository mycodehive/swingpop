using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Wind;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M4WindTerrainSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string WindTuningPath = "Assets/_Game/ScriptableObjects/M4WindTuning.asset";
        private const string SurfaceDataFolder = "Assets/_Game/ScriptableObjects/Terrain";
        private const string MaterialFolder = "Assets/_Game/Materials";
        private const string PhysicsMaterialPath = "Assets/_Game/Materials/GolfBallPhysics.asset";
        private const string CourseRootName = "M4 Test Course";

        [MenuItem("SwingPop/M4/Build Wind Terrain Scene")]
        public static void BuildWindTerrainScene()
        {
            EnsureAssetFolder(SurfaceDataFolder);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            GameObject systems = FindInScene(scene, "M4 Shot Wind Terrain Systems")
                                 ?? FindInScene(scene, "M3 Shot and Flight Systems");
            if (ball == null || systems == null)
            {
                Debug.LogError("M4 builder requires the completed M3 Foundation scene.");
                return;
            }

            systems.name = "M4 Shot Wind Terrain Systems";
            WindTuningData windTuning = LoadOrCreateWindTuning();
            TerrainSurfaceData tee = LoadOrCreateSurface("Tee", TerrainSurfaceType.Tee, 1f, 1f, 0.9f, 1f, 1f);
            TerrainSurfaceData fairway = LoadOrCreateSurface("Fairway", TerrainSurfaceType.Fairway, 1f, 1f, 1f, 1f, 1f);
            TerrainSurfaceData rough = LoadOrCreateSurface("Rough", TerrainSurfaceType.Rough, 0.9f, 1.35f, 0.65f, 0.7f, 1.35f);
            TerrainSurfaceData bunker = LoadOrCreateSurface("Bunker", TerrainSurfaceType.Bunker, 0.65f, 2f, 0.25f, 0.3f, 1.8f);
            TerrainSurfaceData green = LoadOrCreateSurface("Green", TerrainSurfaceType.Green, 1f, 0.7f, 0.2f, 1.25f, 0.65f);
            TerrainSurfaceData water = LoadOrCreateSurface("Water", TerrainSurfaceType.Water, 0f, 0f, 0f, 0f, 0f);
            TerrainSurfaceData outOfBounds = LoadOrCreateSurface("OutOfBounds", TerrainSurfaceType.OutOfBounds, 0f, 0f, 0f, 0f, 0f);
            PhysicsMaterial physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicsMaterialPath);

            DisableLegacyGround(scene);
            GameObject oldCourse = FindInScene(scene, CourseRootName);
            if (oldCourse != null)
            {
                Object.DestroyImmediate(oldCourse);
            }

            GameObject environmentRoot = FindRoot(scene, "Environment") ?? new GameObject("Environment");
            GameObject courseRoot = new(CourseRootName);
            courseRoot.transform.SetParent(environmentRoot.transform, false);

            CreateSolidSurface(courseRoot.transform, "Tee", new Vector3(0f, -0.25f, 0f), new Vector3(14f, 0.5f, 8f), tee, new Color(0.16f, 0.88f, 0.68f), physicsMaterial);
            CreateSolidSurface(courseRoot.transform, "Fairway", new Vector3(0f, -0.25f, 32f), new Vector3(18f, 0.5f, 56f), fairway, new Color(0.3f, 0.82f, 0.35f), physicsMaterial);
            CreateSolidSurface(courseRoot.transform, "Rough", new Vector3(-16f, -0.25f, 32f), new Vector3(12f, 0.5f, 56f), rough, new Color(0.08f, 0.42f, 0.16f), physicsMaterial);
            CreateSolidSurface(courseRoot.transform, "Bunker", new Vector3(16f, -0.25f, 32f), new Vector3(12f, 0.5f, 56f), bunker, new Color(0.9f, 0.71f, 0.34f), physicsMaterial);
            CreateSolidSurface(courseRoot.transform, "Green", new Vector3(0f, -0.25f, 72f), new Vector3(26f, 0.5f, 24f), green, new Color(0.45f, 1f, 0.45f), physicsMaterial);
            CreateHazardZone(courseRoot.transform, "Water", new Vector3(-27f, 2f, 45f), new Vector3(14f, 8f, 34f), water, new Color(0.05f, 0.4f, 0.95f));
            CreateHazardZone(courseRoot.transform, "Out Of Bounds", new Vector3(27f, 2f, 45f), new Vector3(14f, 8f, 34f), outOfBounds, new Color(0.95f, 0.18f, 0.22f));

            WindController wind = systems.GetComponent<WindController>();
            if (wind == null)
            {
                wind = systems.AddComponent<WindController>();
            }
            SetObjectReference(wind, "tuning", windTuning);

            WindDebugInputController windInput = systems.GetComponent<WindDebugInputController>();
            if (windInput == null)
            {
                windInput = systems.AddComponent<WindDebugInputController>();
            }
            SetObjectReference(windInput, "wind", wind);

            ConfigureWindVisualizer(systems.transform, ball, wind);
            SetObjectReference(ball, "wind", wind);
            SetObjectReference(ball, "defaultSurface", tee);

            ShotDebugOverlay overlay = systems.GetComponent<ShotDebugOverlay>();
            if (overlay != null)
            {
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("wind").objectReferenceValue = wind;
                serializedOverlay.FindProperty("overlaySize").vector2Value = new Vector2(560f, 520f);
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = systems;
            Debug.Log("SwingPop M4 Wind / Terrain scene wiring completed.");
        }

        private static WindTuningData LoadOrCreateWindTuning()
        {
            WindTuningData data = AssetDatabase.LoadAssetAtPath<WindTuningData>(WindTuningPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WindTuningData>();
                AssetDatabase.CreateAsset(data, WindTuningPath);
            }

            SerializedObject serialized = new(data);
            serialized.FindProperty("tailwindStrength").floatValue = 5f;
            serialized.FindProperty("headwindStrength").floatValue = 5f;
            serialized.FindProperty("crosswindStrength").floatValue = 5f;
            serialized.FindProperty("windForceMultiplier").floatValue = 0.32f;
            serialized.FindProperty("headTailMultiplier").floatValue = 1f;
            serialized.FindProperty("crosswindMultiplier").floatValue = 1.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static TerrainSurfaceData LoadOrCreateSurface(
            string assetName,
            TerrainSurfaceType type,
            float power,
            float friction,
            float bounce,
            float spin,
            float rollingResistance)
        {
            string path = $"{SurfaceDataFolder}/{assetName}.asset";
            TerrainSurfaceData data = AssetDatabase.LoadAssetAtPath<TerrainSurfaceData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TerrainSurfaceData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new(data);
            serialized.FindProperty("surfaceType").enumValueIndex = (int)type;
            serialized.FindProperty("powerModifier").floatValue = power;
            serialized.FindProperty("friction").floatValue = friction;
            serialized.FindProperty("bounceModifier").floatValue = bounce;
            serialized.FindProperty("spinResponse").floatValue = spin;
            serialized.FindProperty("rollingResistance").floatValue = rollingResistance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void CreateSolidSurface(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            TerrainSurfaceData data,
            Color color,
            PhysicsMaterial physicsMaterial)
        {
            GameObject surfaceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surfaceObject.name = name;
            surfaceObject.transform.SetParent(parent, false);
            surfaceObject.transform.position = position;
            surfaceObject.transform.localScale = scale;
            surfaceObject.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial($"M4{name.Replace(" ", string.Empty)}", color);
            BoxCollider collider = surfaceObject.GetComponent<BoxCollider>();
            collider.sharedMaterial = physicsMaterial;
            TerrainSurface surface = surfaceObject.AddComponent<TerrainSurface>();
            SetObjectReference(surface, "data", data);
        }

        private static void CreateHazardZone(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            TerrainSurfaceData data,
            Color color)
        {
            GameObject zone = new(name, typeof(BoxCollider));
            zone.transform.SetParent(parent, false);
            zone.transform.position = position;
            BoxCollider trigger = zone.GetComponent<BoxCollider>();
            trigger.size = size;
            trigger.isTrigger = true;
            TerrainSurface surface = zone.AddComponent<TerrainSurface>();
            SetObjectReference(surface, "data", data);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(zone.transform, false);
            visual.transform.localPosition = new Vector3(0f, -2.2f, 0f);
            visual.transform.localScale = new Vector3(size.x, 0.1f, size.z);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial($"M4{name.Replace(" ", string.Empty)}", color);
        }

        private static void ConfigureWindVisualizer(Transform systems, GolfBallController ball, WindController wind)
        {
            GameObject oldVector = FindChild(systems, "Wind Vector");
            if (oldVector != null)
            {
                Object.DestroyImmediate(oldVector);
            }

            GameObject vectorObject = new("Wind Vector", typeof(LineRenderer), typeof(WindDebugVisualizer));
            vectorObject.transform.SetParent(systems, false);
            LineRenderer line = vectorObject.GetComponent<LineRenderer>();
            line.sharedMaterial = LoadOrCreateMaterial("M4WindVector", new Color(1f, 0.15f, 0.85f));
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.14f;
            line.endWidth = 0.04f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            WindDebugVisualizer visualizer = vectorObject.GetComponent<WindDebugVisualizer>();
            SetObjectReference(visualizer, "wind", wind);
            SetObjectReference(visualizer, "anchor", ball.transform);
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

        private static void DisableLegacyGround(Scene scene)
        {
            GameObject ground = FindInScene(scene, "FoundationGround");
            if (ground != null)
            {
                ground.SetActive(false);
            }
        }

        private static void EnsureAssetFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Terrain");
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
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
