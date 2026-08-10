using System;
using SwingPop.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class FoundationSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string ProbePrefabPath = "Assets/_Game/Prefabs/Golf/FoundationInputProbe.prefab";
        private const string GroundMaterialPath = "Assets/_Game/Materials/FoundationGround.mat";
        private const string ProbeMaterialPath = "Assets/_Game/Materials/FoundationProbe.mat";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";

        [MenuItem("SwingPop/M0/Build Foundation Scene")]
        public static void BuildFoundationScene()
        {
            EnsureDirectories();

            Material groundMaterial = LoadOrCreateMaterial(
                GroundMaterialPath,
                new Color(0.22f, 0.68f, 0.32f, 1f));
            Material probeMaterial = LoadOrCreateMaterial(
                ProbeMaterialPath,
                new Color(0.15f, 0.85f, 0.75f, 1f));

            GameObject probePrefab = CreateProbePrefab(probeMaterial);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Foundation";

            GameObject environmentRoot = new("Environment");
            CreateGround(environmentRoot.transform, groundMaterial);
            CreateLighting(environmentRoot.transform);

            GameObject gameplayRoot = new("Gameplay");
            GameObject probeInstance = (GameObject)PrefabUtility.InstantiatePrefab(probePrefab, scene);
            probeInstance.transform.SetParent(gameplayRoot.transform, false);

            GameObject cameraRoot = new("Presentation");
            CreateCamera(cameraRoot.transform, probeInstance.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = probeInstance;
            Debug.Log($"SwingPop M0 Foundation scene created at {ScenePath}.");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildFoundationScene();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void EnsureDirectories()
        {
            EnsureDirectory("Assets/_Game/Scenes");
            EnsureDirectory("Assets/_Game/Materials");
            EnsureDirectory("Assets/_Game/Prefabs/Golf");
        }

        private static void EnsureDirectory(string path)
        {
            string[] parts = path.Split('/');
            string currentPath = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("The URP Lit shader is unavailable. Verify the active render pipeline before rebuilding M0.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateProbePrefab(Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "FoundationInputProbe";
            root.transform.position = Vector3.up;
            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "InputIndicator";
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            indicator.transform.localScale = Vector3.one * 0.25f;
            indicator.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(indicator.GetComponent<Collider>());

            FoundationInputProbe probe = root.AddComponent<FoundationInputProbe>();
            SerializedObject serializedProbe = new(probe);
            serializedProbe.FindProperty("inputIndicator").objectReferenceValue = indicator.transform;
            serializedProbe.FindProperty("probeRenderer").objectReferenceValue = renderer;
            serializedProbe.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ProbePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateGround(Transform parent, Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "FoundationGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(20f, 0.5f, 20f);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new("Directional Light", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.96f, 0.86f, 1f);
            light.shadows = LightShadows.Soft;

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                GameObject volumeObject = new("Global Volume", typeof(Volume));
                volumeObject.transform.SetParent(parent, false);
                Volume volume = volumeObject.GetComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }
        }

        private static void CreateCamera(Transform parent, Transform target)
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(8f, 6f, -8f);
            cameraObject.transform.LookAt(target.position);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
