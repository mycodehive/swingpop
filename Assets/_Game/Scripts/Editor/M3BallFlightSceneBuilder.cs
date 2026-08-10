using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M3BallFlightSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string TrajectoryMaterialPath = "Assets/_Game/Materials/M3TrajectoryLine.mat";
        [MenuItem("SwingPop/M3/Build Arcade Ball Flight Scene")]
        public static void BuildM3BallFlightScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            GameObject systems = FindInScene(scene, "M3 Shot and Flight Systems")
                                 ?? FindInScene(scene, "M2 Shot Systems");
            if (ball == null || systems == null)
            {
                Debug.LogError("M3 builder requires the completed M2 Foundation scene.");
                return;
            }

            systems.name = "M3 Shot and Flight Systems";
            GameObject oldTrace = FindInScene(scene, "Trajectory Trace");
            if (oldTrace != null)
            {
                Object.DestroyImmediate(oldTrace);
            }

            GameObject traceObject = new("Trajectory Trace", typeof(LineRenderer));
            traceObject.transform.SetParent(systems.transform, false);
            LineRenderer line = traceObject.GetComponent<LineRenderer>();
            line.sharedMaterial = LoadOrCreateTrajectoryMaterial();
            line.useWorldSpace = true;
            line.startWidth = 0.08f;
            line.endWidth = 0.035f;
            line.startColor = new Color(1f, 0.35f, 0.95f, 0.95f);
            line.endColor = new Color(0.25f, 0.8f, 1f, 0.4f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            BallTrajectoryDebug trajectory = systems.GetComponent<BallTrajectoryDebug>();
            if (trajectory == null)
            {
                trajectory = systems.AddComponent<BallTrajectoryDebug>();
            }

            SetObjectReference(trajectory, "ball", ball);
            SetObjectReference(trajectory, "trajectoryLine", line);

            ShotDebugOverlay overlay = systems.GetComponent<ShotDebugOverlay>();
            if (overlay != null)
            {
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("overlaySize").vector2Value = new Vector2(500f, 360f);
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject ground = FindInScene(scene, "FoundationGround");
            if (ground != null)
            {
                ground.transform.localScale = new Vector3(180f, 0.5f, 220f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = systems;
            Debug.Log("SwingPop M3 Arcade Ball Flight scene wiring completed.");
        }

        private static Material LoadOrCreateTrajectoryMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(TrajectoryMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "M3TrajectoryLine" };
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, TrajectoryMaterialPath);
            return material;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindRecursive(root.transform, objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform match = FindRecursive(parent.GetChild(index), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void SetObjectReference(Object component, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(component);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
