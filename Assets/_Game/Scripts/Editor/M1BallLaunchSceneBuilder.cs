using SwingPop.CameraSystem;
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
    public static class M1BallLaunchSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string BallPrefabPath = "Assets/_Game/Prefabs/Golf/GolfBall.prefab";
        private const string BallMaterialPath = "Assets/_Game/Materials/GolfBallPlaceholder.mat";
        private const string BallPhysicsMaterialPath = "Assets/_Game/Materials/GolfBallPhysics.asset";
        private const string BallTuningPath = "Assets/_Game/ScriptableObjects/M1BallTuning.asset";

        [MenuItem("SwingPop/M1/Build Ball Launch Scene")]
        public static void BuildM1BallLaunchScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BallTuningData tuning = LoadOrCreateTuning();
            Material visualMaterial = LoadOrCreateVisualMaterial();
            PhysicsMaterial physicsMaterial = LoadOrCreatePhysicsMaterial();
            GameObject ballPrefab = CreateBallPrefab(tuning, visualMaterial, physicsMaterial);

            RemoveExistingM1Objects(scene);
            ConfigureGround(scene, physicsMaterial);

            GameObject gameplayRoot = FindRoot(scene, "Gameplay") ?? new GameObject("Gameplay");
            GameObject ballInstance = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab, scene);
            ballInstance.name = "GolfBall";
            ballInstance.transform.SetParent(gameplayRoot.transform, false);
            ballInstance.transform.position = new Vector3(0f, 0.16f, 0f);

            GameObject launchDirection = new("LaunchDirection");
            launchDirection.transform.SetParent(gameplayRoot.transform, false);
            launchDirection.transform.position = ballInstance.transform.position;
            launchDirection.transform.rotation = Quaternion.identity;

            GolfBallController ball = ballInstance.GetComponent<GolfBallController>();
            SerializedObject serializedBall = new(ball);
            serializedBall.FindProperty("launchDirectionReference").objectReferenceValue = launchDirection.transform;
            serializedBall.ApplyModifiedPropertiesWithoutUndo();

            GameObject systems = new("M1 Systems");
            systems.transform.SetParent(gameplayRoot.transform, false);
            TemporaryBallInput input = systems.AddComponent<TemporaryBallInput>();
            SetObjectReference(input, "ball", ball);
            BallDebugTelemetry telemetry = systems.AddComponent<BallDebugTelemetry>();
            SetObjectReference(telemetry, "ball", ball);

            Camera mainCamera = Object.FindAnyObjectByType<Camera>();
            if (mainCamera == null)
            {
                GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.GetComponent<Camera>();
            }

            BallFollowCamera followCamera = mainCamera.GetComponent<BallFollowCamera>();
            if (followCamera == null)
            {
                followCamera = mainCamera.gameObject.AddComponent<BallFollowCamera>();
            }

            SetObjectReference(followCamera, "ball", ball);
            mainCamera.transform.position = ball.transform.position + new Vector3(7f, 4.5f, -9f);
            mainCamera.transform.LookAt(ball.transform.position + new Vector3(0f, 0.5f, 1.5f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = ballInstance;

            Debug.Log("SwingPop M1 Ball Launch scene wiring completed.");
        }

        private static BallTuningData LoadOrCreateTuning()
        {
            BallTuningData tuning = AssetDatabase.LoadAssetAtPath<BallTuningData>(BallTuningPath);
            if (tuning != null)
            {
                return tuning;
            }

            tuning = ScriptableObject.CreateInstance<BallTuningData>();
            AssetDatabase.CreateAsset(tuning, BallTuningPath);
            return tuning;
        }

        private static Material LoadOrCreateVisualMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BallMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = "GolfBallPlaceholder" };
                AssetDatabase.CreateAsset(material, BallMaterialPath);
            }

            material.SetColor("_BaseColor", new Color(0.92f, 0.98f, 1f, 1f));
            material.SetFloat("_Smoothness", 0.65f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial LoadOrCreatePhysicsMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BallPhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial("GolfBallPhysics");
                AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            }

            material.bounciness = 0.48f;
            material.dynamicFriction = 0.22f;
            material.staticFriction = 0.28f;
            material.bounceCombine = PhysicsMaterialCombine.Maximum;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateBallPrefab(
            BallTuningData tuning,
            Material visualMaterial,
            PhysicsMaterial physicsMaterial)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "GolfBall";
            root.transform.localScale = Vector3.one * 0.3f;

            SphereCollider sphereCollider = root.GetComponent<SphereCollider>();
            sphereCollider.sharedMaterial = physicsMaterial;
            root.GetComponent<Renderer>().sharedMaterial = visualMaterial;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            GolfBallController controller = root.AddComponent<GolfBallController>();
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("ballBody").objectReferenceValue = body;
            serializedController.FindProperty("ballCollider").objectReferenceValue = sphereCollider;
            serializedController.FindProperty("tuning").objectReferenceValue = tuning;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BallPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureGround(Scene scene, PhysicsMaterial physicsMaterial)
        {
            GameObject ground = FindInScene(scene, "FoundationGround");
            if (ground == null)
            {
                Debug.LogWarning("FoundationGround was not found; M1 builder left the scene without modifying a ground object.");
                return;
            }

            ground.transform.position = new Vector3(0f, -0.25f, 50f);
            ground.transform.localScale = new Vector3(30f, 0.5f, 140f);
            Collider groundCollider = ground.GetComponent<Collider>();
            if (groundCollider != null)
            {
                groundCollider.sharedMaterial = physicsMaterial;
            }
        }

        private static void RemoveExistingM1Objects(Scene scene)
        {
            FoundationInputProbe foundationProbe = Object.FindAnyObjectByType<FoundationInputProbe>();
            if (foundationProbe != null)
            {
                Object.DestroyImmediate(foundationProbe.gameObject);
            }

            string[] generatedObjectNames = { "GolfBall", "LaunchDirection", "M1 Systems" };
            foreach (string objectName in generatedObjectNames)
            {
                GameObject existingObject = FindInScene(scene, objectName);
                if (existingObject != null)
                {
                    Object.DestroyImmediate(existingObject);
                }
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
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
