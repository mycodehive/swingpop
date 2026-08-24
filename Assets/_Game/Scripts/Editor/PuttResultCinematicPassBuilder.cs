using System;
using System.IO;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class PuttResultCinematicPassBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string HudSourcePath = "Assets/_Game/Prefabs/UI/GameplayHUD_SwingPopSkin.prefab";
        private const string HudPrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD_PuttResult.prefab";
        private const string PresentationPrefabPath = "Assets/_Game/Prefabs/Presentation/PuttResultCinematic.prefab";
        private const string TuningPath = "Assets/_Game/ScriptableObjects/Presentation/PuttResultCinematicTuning.asset";
        private const string CameraSourcePath = "Assets/_Game/ScriptableObjects/Polish/M11CameraTuning.asset";
        private const string CameraTuningPath = "Assets/_Game/ScriptableObjects/Presentation/PuttResultCameraTuning.asset";
        private const string PresentationName = "Putt Result Cinematic";

        [MenuItem("SwingPop/Presentation/Build Putt Result Cinematic")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Prefabs/Presentation");
            EnsureFolder("Assets/_Game/ScriptableObjects/Presentation");

            PuttResultCinematicTuningData cinematic = LoadOrCreateCinematicTuning();
            CameraTuningData cameraTuning = LoadOrCreateCameraTuning();
            BuildHudPrefab(cinematic);
            BuildPresentationPrefab(cinematic);
            WireScene(cinematic, cameraTuning);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PUTT / RESULT CINEMATIC PASS BUILD COMPLETE | Hole01 presentation graph updated. Foundation unchanged.");
        }

        [MenuItem("SwingPop/Presentation/Preview Putt Result")]
        public static void PreviewPuttResult()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Open Hole01_SkyIsland and enter Play Mode before previewing the result sequence.");
            }

            PuttResultCinematicController controller = Object.FindAnyObjectByType<PuttResultCinematicController>();
            if (controller == null)
            {
                throw new InvalidOperationException("Putt Result Cinematic controller is not present in the active scene.");
            }

            controller.PreviewHoleInSequence(ScoreCalculator.Calculate(4, 3));
        }

        private static PuttResultCinematicTuningData LoadOrCreateCinematicTuning()
        {
            PuttResultCinematicTuningData tuning = AssetDatabase.LoadAssetAtPath<PuttResultCinematicTuningData>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<PuttResultCinematicTuningData>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }

            SerializedObject serialized = new(tuning);
            SetVector(serialized, "puttAddressOffset", new Vector3(5.4f, 3.15f, -7.2f));
            SetFloat(serialized, "puttAddressCupBias", 0.48f);
            SetFloat(serialized, "puttAddressLookHeight", 0.65f);
            SetFloat(serialized, "puttAddressFieldOfView", 47f);
            SetVector(serialized, "puttRollingOffset", new Vector3(4.8f, 2.55f, -6.2f));
            SetFloat(serialized, "puttRollingCupBias", 0.58f);
            SetFloat(serialized, "puttRollingLookHeight", 0.24f);
            SetFloat(serialized, "puttRollingFieldOfView", 43f);
            SetFloat(serialized, "approachDistance", 1.6f);
            SetVector(serialized, "approachOffset", new Vector3(4.5f, 2.2f, -5.6f));
            SetFloat(serialized, "approachLookHeight", 0.18f);
            SetFloat(serialized, "approachFieldOfView", 42f);
            SetVector(serialized, "holeInOffset", new Vector3(4.7f, 2.8f, -5.8f));
            SetFloat(serialized, "holeInLookHeight", 0.28f);
            SetFloat(serialized, "holeInFieldOfView", 43f);
            SetVector(serialized, "resultOffset", new Vector3(7.2f, 4.45f, -7.4f));
            SetFloat(serialized, "resultCupBias", 0.56f);
            SetFloat(serialized, "resultLookHeight", 0.95f);
            SetFloat(serialized, "resultFieldOfView", 49f);
            SetFloat(serialized, "celebrationDelay", 0.34f);
            SetFloat(serialized, "resultRevealDelay", 0.74f);
            SetFloat(serialized, "resultFrameDuration", 0.28f);
            SetFloat(serialized, "resultScoreDelay", 0.09f);
            SetFloat(serialized, "resultDetailDelay", 0.2f);
            SetFloat(serialized, "holeRingDelay", 0.06f);
            SetFloat(serialized, "holeCelebrationDelay", 0.22f);
            SetFloat(serialized, "holeVfxIntensity", 1.2f);
            SerializedProperty panelOffset = serialized.FindProperty("resultPanelOffset");
            if (panelOffset != null) panelOffset.vector2Value = new Vector2(335f, -8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
            return tuning;
        }

        private static CameraTuningData LoadOrCreateCameraTuning()
        {
            CameraTuningData source = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraSourcePath);
            if (source == null) throw new InvalidOperationException($"Missing camera source asset: {CameraSourcePath}");

            CameraTuningData tuning = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraTuningPath);
            if (tuning == null)
            {
                tuning = Object.Instantiate(source);
                tuning.name = Path.GetFileNameWithoutExtension(CameraTuningPath);
                AssetDatabase.CreateAsset(tuning, CameraTuningPath);
            }

            SerializedObject serialized = new(tuning);
            SetFloat(serialized, "defaultTransitionDuration", 0.32f);
            SetFloat(serialized, "followPositionSharpness", 7.5f);
            SetFloat(serialized, "followRotationSharpness", 10.5f);
            SetFloat(serialized, "followFovSharpness", 8.5f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
            return tuning;
        }

        private static void BuildHudPrefab(PuttResultCinematicTuningData tuning)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudSourcePath);
            try
            {
                root.name = "GameplayHUD_PuttResult";
                GameplayHudPresenter presenter = root.GetComponent<GameplayHudPresenter>();
                HudResultView result = root.GetComponentInChildren<HudResultView>(true);
                if (presenter == null || result == null)
                {
                    throw new InvalidOperationException("HUD source is missing GameplayHudPresenter or HudResultView.");
                }

                SetObjectReference(presenter, "cinematicTuning", tuning);
                Transform panel = FindRecursive(root.transform, "Result Panel");
                Transform resultLabel = FindRecursive(root.transform, "Result Label");
                if (panel == null || resultLabel == null)
                {
                    throw new InvalidOperationException("HUD source result hierarchy is incomplete.");
                }

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(600f, 420f);
                panelRect.anchoredPosition = tuning.ResultPanelOffset;

                CanvasGroup scoreGroup = resultLabel.GetComponent<CanvasGroup>();
                if (scoreGroup == null) scoreGroup = resultLabel.gameObject.AddComponent<CanvasGroup>();
                Text scoreText = resultLabel.GetComponent<Text>();
                if (scoreText != null) scoreText.fontSize = 54;
                RectTransform scoreRect = resultLabel.GetComponent<RectTransform>();
                scoreRect.anchoredPosition = new Vector2(0f, 64f);
                scoreRect.sizeDelta = new Vector2(520f, 90f);

                Transform detailRoot = panel.Find("Result Details Group");
                if (detailRoot == null)
                {
                    GameObject details = new("Result Details Group", typeof(RectTransform), typeof(CanvasGroup));
                    detailRoot = details.transform;
                    detailRoot.SetParent(panel, false);
                    RectTransform detailRect = (RectTransform)detailRoot;
                    detailRect.anchorMin = Vector2.zero;
                    detailRect.anchorMax = Vector2.one;
                    detailRect.offsetMin = Vector2.zero;
                    detailRect.offsetMax = Vector2.zero;
                }

                foreach (string name in new[] { "Hole", "Par", "Strokes" })
                {
                    Transform text = FindRecursive(panel, name);
                    if (text != null && text.parent != detailRoot) ReparentRect(text, detailRoot);
                }
                SetAnchoredY(detailRoot, "Hole", -24f);
                SetAnchoredY(detailRoot, "Par", -70f);
                SetAnchoredY(detailRoot, "Strokes", -112f);

                CanvasGroup detailGroup = detailRoot.GetComponent<CanvasGroup>();
                SetObjectReference(result, "scoreGroup", scoreGroup);
                SetObjectReference(result, "detailGroup", detailGroup);
                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildPresentationPrefab(PuttResultCinematicTuningData tuning)
        {
            GameObject root = new(PresentationName);
            PuttResultCinematicController controller = root.AddComponent<PuttResultCinematicController>();
            SetObjectReference(controller, "tuning", tuning);
            PrefabUtility.SaveAsPrefabAsset(root, PresentationPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void WireScene(PuttResultCinematicTuningData cinematic, CameraTuningData cameraTuning)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            WindController wind = Object.FindAnyObjectByType<WindController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>();
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>();
            HoleInVfxController holeInVfx = Object.FindAnyObjectByType<HoleInVfxController>();
            if (shotFlow == null || ball == null || wind == null || holeFlow == null || cameraDirector == null
                || character == null || audio == null || holeInVfx == null)
            {
                throw new InvalidOperationException("Hole01 is missing a required completed vertical-slice dependency.");
            }

            GameplayHudPresenter oldHud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            Camera worldCamera = ReadReference<Camera>(oldHud, "worldCamera") ?? Camera.main;
            HudTuningData hudTuning = ReadReference<HudTuningData>(oldHud, "tuning");
            Transform presentationParent = oldHud != null ? oldHud.transform.parent : FindInScene(scene, "Presentation")?.transform;
            if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject hudObject = PrefabUtility.InstantiatePrefab(hudPrefab, scene) as GameObject;
            if (hudObject == null) throw new InvalidOperationException("Could not instantiate putt/result HUD prefab.");
            hudObject.name = "Gameplay HUD";
            if (presentationParent != null) hudObject.transform.SetParent(presentationParent, false);
            GameplayHudPresenter hud = hudObject.GetComponent<GameplayHudPresenter>();
            SetObjectReference(hud, "shotFlow", shotFlow);
            SetObjectReference(hud, "ball", ball);
            SetObjectReference(hud, "wind", wind);
            SetObjectReference(hud, "holeFlow", holeFlow);
            SetObjectReference(hud, "worldCamera", worldCamera);
            SetObjectReference(hud, "tuning", hudTuning);
            SetObjectReference(hud, "cinematicTuning", cinematic);

            SetObjectReference(cameraDirector, "tuning", cameraTuning);
            SetObjectReference(cameraDirector, "characterTransform", character.transform);
            SetObjectReference(cameraDirector, "cinematicTuning", cinematic);
            SetObjectReference(character, "cinematicTuning", cinematic);
            SetObjectReference(audio, "cinematicTuning", cinematic);
            SetObjectReference(holeInVfx, "cinematicTuning", cinematic);

            PuttResultCinematicController existing = Object.FindAnyObjectByType<PuttResultCinematicController>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            GameObject presentationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPrefabPath);
            GameObject presentationObject = PrefabUtility.InstantiatePrefab(presentationPrefab, scene) as GameObject;
            if (presentationObject == null) throw new InvalidOperationException("Could not instantiate cinematic coordinator prefab.");
            presentationObject.name = PresentationName;
            if (presentationParent != null) presentationObject.transform.SetParent(presentationParent, false);
            PuttResultCinematicController coordinator = presentationObject.GetComponent<PuttResultCinematicController>();
            SetObjectReference(coordinator, "ball", ball);
            SetObjectReference(coordinator, "shotFlow", shotFlow);
            SetObjectReference(coordinator, "holeFlow", holeFlow);
            SetObjectReference(coordinator, "cameraDirector", cameraDirector);
            SetObjectReference(coordinator, "character", character);
            SetObjectReference(coordinator, "hud", hud);
            SetObjectReference(coordinator, "audioController", audio);
            SetObjectReference(coordinator, "tuning", cinematic);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = presentationObject;
        }

        private static void ReparentRect(Transform child, Transform parent)
        {
            RectTransform rect = child as RectTransform;
            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            Vector2 pivot = rect.pivot;
            Vector2 size = rect.sizeDelta;
            Vector2 position = rect.anchoredPosition;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetAnchoredY(Transform root, string name, float y)
        {
            RectTransform rect = FindRecursive(root, name) as RectTransform;
            if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private static T ReadReference<T>(Object target, string propertyName) where T : Object
        {
            if (target == null) return null;
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Missing serialized field '{propertyName}' on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetVector(SerializedObject serialized, string propertyName, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.vector3Value = value;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform value = FindRecursive(root.transform, name);
                if (value != null) return value.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform value = FindRecursive(parent.GetChild(index), name);
                if (value != null) return value;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
