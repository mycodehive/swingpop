using SwingPop.CameraSystem;
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
    public static class M6CameraDirectorSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string CameraDataFolder = "Assets/_Game/ScriptableObjects/Camera";
        private const string CameraTuningPath = CameraDataFolder + "/M6CameraTuning.asset";

        [MenuItem("SwingPop/M6/Build Camera Director Scene _F5")]
        public static void BuildCameraDirectorScene()
        {
            EnsureFolder(CameraDataFolder);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (ball == null || shotFlow == null || holeFlow == null || mainCamera == null)
            {
                Debug.LogError("M6 builder requires the completed M5 scene with Ball, ShotFlow, HoleFlow, and Main Camera.");
                return;
            }

            BallFollowCamera legacyFollow = mainCamera.GetComponent<BallFollowCamera>();
            if (legacyFollow != null)
            {
                legacyFollow.enabled = false;
            }

            CameraDirector director = mainCamera.GetComponent<CameraDirector>();
            if (director == null)
            {
                director = mainCamera.gameObject.AddComponent<CameraDirector>();
            }

            CameraTuningData tuning = LoadOrCreateTuning();
            SetObjectReference(director, "controlledCamera", mainCamera);
            SetObjectReference(director, "ball", ball);
            SetObjectReference(director, "shotFlow", shotFlow);
            SetObjectReference(director, "holeFlow", holeFlow);
            SetObjectReference(director, "tuning", tuning);

            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            if (overlay != null)
            {
                SetObjectReference(overlay, "cameraDirector", director);
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("overlaySize").vector2Value = new Vector2(650f, 750f);
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject systems = FindInScene(scene, "M5 Hole Scoring Systems");
            if (systems != null)
            {
                systems.name = "M6 Camera Director Systems";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = mainCamera.gameObject;
            Debug.Log("SwingPop M6 Camera Director scene wiring completed. Legacy BallFollowCamera is retained but disabled.");
        }

        private static CameraTuningData LoadOrCreateTuning()
        {
            CameraTuningData data = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraTuningPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CameraTuningData>();
                AssetDatabase.CreateAsset(data, CameraTuningPath);
            }
            EditorUtility.SetDirty(data);
            return data;
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
                Transform result = FindRecursive(root.transform, name);
                if (result != null)
                {
                    return result.gameObject;
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
                Transform result = FindRecursive(parent.GetChild(index), name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
