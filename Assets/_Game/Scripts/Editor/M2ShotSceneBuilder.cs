using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M2ShotSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string BallTuningPath = "Assets/_Game/ScriptableObjects/M1BallTuning.asset";
        private const string ShotTuningPath = "Assets/_Game/ScriptableObjects/M2ShotTuning.asset";
        private const string AimLineMaterialPath = "Assets/_Game/Materials/M2AimLine.mat";

        [MenuItem("SwingPop/M2/Build Aim Power Impact Scene")]
        public static void BuildM2ShotScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BallTuningData ballTuning = AssetDatabase.LoadAssetAtPath<BallTuningData>(BallTuningPath);
            ShotTuningData shotTuning = LoadOrCreateShotTuning();
            Material aimLineMaterial = LoadOrCreateAimLineMaterial();

            GolfBallController ball = Object.FindFirstObjectByType<GolfBallController>();
            if (ball == null || ballTuning == null)
            {
                Debug.LogError("M2 scene builder requires the completed M1 GolfBall and M1BallTuning assets.");
                return;
            }

            GameObject gameplayRoot = FindRoot(scene, "Gameplay") ?? new GameObject("Gameplay");
            RemoveGeneratedSystems(scene);
            ConfigureGround(scene);

            GameObject aimDirectionObject = FindInScene(scene, "LaunchDirection");
            if (aimDirectionObject == null)
            {
                aimDirectionObject = new GameObject("AimDirection");
                aimDirectionObject.transform.SetParent(gameplayRoot.transform, false);
                aimDirectionObject.transform.position = ball.transform.position;
            }
            else
            {
                aimDirectionObject.name = "AimDirection";
            }

            GameObject systems = new("M2 Shot Systems");
            systems.transform.SetParent(gameplayRoot.transform, false);

            ShotFlowController shotFlow = systems.AddComponent<ShotFlowController>();
            SetObjectReference(shotFlow, "ball", ball);
            SetObjectReference(shotFlow, "ballTuning", ballTuning);
            SetObjectReference(shotFlow, "shotTuning", shotTuning);
            SetObjectReference(shotFlow, "aimDirectionReference", aimDirectionObject.transform);

            ShotInputController input = systems.AddComponent<ShotInputController>();
            SetObjectReference(input, "shotFlow", shotFlow);

            GameObject aimGuide = new("Aim Guide", typeof(LineRenderer));
            aimGuide.transform.SetParent(systems.transform, false);
            LineRenderer lineRenderer = aimGuide.GetComponent<LineRenderer>();
            lineRenderer.sharedMaterial = aimLineMaterial;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.startColor = new Color(0.1f, 1f, 0.95f, 1f);
            lineRenderer.endColor = new Color(0.1f, 0.8f, 1f, 0.25f);
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            ShotDebugOverlay overlay = systems.AddComponent<ShotDebugOverlay>();
            SetObjectReference(overlay, "shotFlow", shotFlow);
            SetObjectReference(overlay, "ball", ball);
            SetObjectReference(overlay, "aimLine", lineRenderer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = systems;

            Debug.Log("SwingPop M2 Aim/Power/Impact scene wiring completed.");
        }

        private static ShotTuningData LoadOrCreateShotTuning()
        {
            ShotTuningData tuning = AssetDatabase.LoadAssetAtPath<ShotTuningData>(ShotTuningPath);
            if (tuning != null)
            {
                return tuning;
            }

            tuning = ScriptableObject.CreateInstance<ShotTuningData>();
            AssetDatabase.CreateAsset(tuning, ShotTuningPath);
            return tuning;
        }

        private static Material LoadOrCreateAimLineMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AimLineMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "M2AimLine" };
            material.SetColor("_BaseColor", new Color(0.1f, 1f, 0.95f, 1f));
            AssetDatabase.CreateAsset(material, AimLineMaterialPath);
            return material;
        }

        private static void RemoveGeneratedSystems(Scene scene)
        {
            string[] generatedNames = { "M1 Systems", "M2 Shot Systems" };
            foreach (string generatedName in generatedNames)
            {
                GameObject generatedObject = FindInScene(scene, generatedName);
                if (generatedObject != null)
                {
                    Object.DestroyImmediate(generatedObject);
                }
            }
        }

        private static void ConfigureGround(Scene scene)
        {
            GameObject ground = FindInScene(scene, "FoundationGround");
            if (ground != null)
            {
                ground.transform.localScale = new Vector3(120f, 0.5f, 140f);
            }
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
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
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
