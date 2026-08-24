using System;
using System.Collections.Generic;
using System.Reflection;
using SwingPop.Data;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SwingPop.Editor
{
    public static class HudSkinValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string ExpectedPrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD_SwingPopSkin.prefab";
        private const string PuttResultPrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD_PuttResult.prefab";
        private const string BaselinePrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD.prefab";

        [MenuItem("SwingPop/UI/Validate Gameplay HUD")]
        public static void ValidateGameplayHud()
        {
            string report = ValidateAndGetReport();
            Debug.Log("HUD SKIN VALIDATION PASS | " + report);
        }

        private static string ValidateAndGetReport()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hud = RequireObject(scene, "Gameplay HUD");
            GameplayHudPresenter presenter = RequireComponent<GameplayHudPresenter>(hud);
            GameplayHudView view = RequireComponent<GameplayHudView>(hud);
            Canvas canvas = RequireComponent<Canvas>(hud);
            CanvasScaler scaler = RequireComponent<CanvasScaler>(hud);
            GraphicRaycaster raycaster = RequireComponent<GraphicRaycaster>(hud);
            Require(raycaster != null, "Gameplay HUD has no GraphicRaycaster.");

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud);
            Require(prefabPath == ExpectedPrefabPath || prefabPath == PuttResultPrefabPath,
                $"Hole01 uses unexpected HUD prefab: {prefabPath}");
            Require(view.Skin != null, "GameplayHudView is missing HudSkinData.");
            RequireAllSkinSprites(view.Skin);

            Require(canvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD Canvas must remain ScreenSpaceOverlay.");
            Require(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "CanvasScaler must use ScaleWithScreenSize.");
            Require(scaler.referenceResolution == new Vector2(1920f, 1080f), "CanvasScaler reference must be 1920x1080.");
            Require(Mathf.Abs(scaler.matchWidthOrHeight - 0.5f) < 0.001f, "CanvasScaler width/height match must remain 0.5.");

            RectTransform safeArea = RequireObject(scene, "Safe Area").GetComponent<RectTransform>();
            Require(safeArea != null, "Safe Area has no RectTransform.");
            Require(safeArea.anchorMin == Vector2.zero && safeArea.anchorMax == Vector2.one,
                "Safe Area must stretch to the Canvas.");

            foreach (string field in new[] { "shotFlow", "ball", "wind", "holeFlow", "view", "tuning", "hudCanvas", "safeArea", "worldCamera" })
            {
                SerializedProperty property = new SerializedObject(presenter).FindProperty(field);
                Require(property != null && property.objectReferenceValue != null, $"GameplayHudPresenter.{field} is missing.");
            }

            foreach (string required in new[]
                     {
                         "Top Left - Player HUD", "Top Center - Hole HUD", "Top Right - Wind HUD",
                         "Aim Target Marker", "Bottom Left - Club HUD", "Power Gauge", "Impact Gauge",
                         "Shot Button", "Impact Feedback", "Hazard Feedback", "Lie Feedback", "Result Panel",
                         "Player Silhouette", "Wind Arrow Icon", "Club Silhouette", "Spin Direction Icon",
                         "Target Emblem", "Action Accent", "Result Emblem"
                     })
            {
                Require(Find(hud.transform, required) != null, $"Required HUD skin element is missing: {required}");
            }

            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            Require(eventSystems.Length == 1, $"Expected one EventSystem, found {eventSystems.Length}.");

            Button actionButton = view.ActionButton;
            Require(actionButton != null && actionButton.targetGraphic != null, "Shot Button command target is missing.");
            Require(actionButton.targetGraphic.raycastTarget, "Shot Button must receive pointer raycasts.");

            int raycastTargets = 0;
            int missingSprites = 0;
            HashSet<Sprite> sprites = new();
            HashSet<Material> materials = new();
            Graphic[] graphics = hud.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.raycastTarget) raycastTargets++;
                if (graphic.material != null) materials.Add(graphic.material);
                if (graphic is Image image)
                {
                    if (image.sprite == null) missingSprites++;
                    else sprites.Add(image.sprite);
                }
            }
            Require(raycastTargets == 1, $"Only the Shot Button may receive raycasts; found {raycastTargets} targets.");
            Require(missingSprites == 0, $"HUD contains {missingSprites} Image(s) without a sprite.");

            int missingScripts = 0;
            foreach (Transform transform in hud.GetComponentsInChildren<Transform>(true))
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            Require(missingScripts == 0, $"HUD contains {missingScripts} Missing Script component(s).");

            int canvases = hud.GetComponentsInChildren<Canvas>(true).Length;
            int layoutGroups = hud.GetComponentsInChildren<LayoutGroup>(true).Length;
            int contentFitters = hud.GetComponentsInChildren<ContentSizeFitter>(true).Length;
            int outlines = hud.GetComponentsInChildren<Outline>(true).Length;
            int shadows = hud.GetComponentsInChildren<Shadow>(true).Length - outlines;
            int updateBehaviours = 0;
            foreach (MonoBehaviour behaviour in hud.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().GetMethod("Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                    updateBehaviours++;
            }
            Require(canvases == 1, $"HUD Canvas count changed unexpectedly: {canvases}.");
            Require(layoutGroups == 0 && contentFitters == 0, "HUD skin must not add runtime layout rebuild components.");

            HudMetrics baseline = MeasurePrefab(BaselinePrefabPath);
            HudMetrics current = Measure(hud);

            return $"BaselineGameObjects={baseline.GameObjects}, BaselineGraphics={baseline.Graphics}, "
                   + $"BaselineRaycastTargets={baseline.RaycastTargets}, BaselineOutlines={baseline.Outlines}, "
                   + $"GameObjects={current.GameObjects}, Canvas={canvases}, Graphics={graphics.Length}, RaycastTargets={raycastTargets}, "
                   + $"Sprites={sprites.Count}, Materials={materials.Count}, Outlines={outlines}, Shadows={shadows}, "
                   + $"LayoutGroups={layoutGroups}, ContentSizeFitters={contentFitters}, UpdateBehaviours={updateBehaviours}, "
                   + $"EventSystems={eventSystems.Length}, MissingSprites={missingSprites}, MissingScripts={missingScripts}.";
        }

        private static HudMetrics MeasurePrefab(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                return Measure(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static HudMetrics Measure(GameObject root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            int raycastTargets = 0;
            foreach (Graphic graphic in graphics)
                if (graphic.raycastTarget) raycastTargets++;
            return new HudMetrics(
                root.GetComponentsInChildren<Transform>(true).Length,
                graphics.Length,
                raycastTargets,
                root.GetComponentsInChildren<Outline>(true).Length);
        }

        private static void RequireAllSkinSprites(HudSkinData skin)
        {
            foreach (Sprite sprite in new[]
                     {
                         skin.RoundedPanel, skin.Capsule, skin.Circle, skin.Diamond, skin.Triangle,
                         skin.PlayerIcon, skin.WindIcon, skin.DriverIcon, skin.PutterIcon,
                         skin.SpinNoneIcon, skin.SpinTopIcon, skin.SpinBackIcon,
                         skin.SpinLeftIcon, skin.SpinRightIcon, skin.TargetIcon
                     })
                Require(sprite != null, "HudSkinData contains a missing sprite reference.");
        }

        private static T RequireComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            Require(component != null, $"{target.name} is missing {typeof(T).Name}.");
            return component;
        }

        private static GameObject RequireObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = Find(root.transform, name);
                if (match != null) return match.gameObject;
            }
            throw new InvalidOperationException($"Scene object is missing: {name}");
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform match = Find(root.GetChild(index), name);
                if (match != null) return match;
            }
            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct HudMetrics
        {
            public HudMetrics(int gameObjects, int graphics, int raycastTargets, int outlines)
            {
                GameObjects = gameObjects;
                Graphics = graphics;
                RaycastTargets = raycastTargets;
                Outlines = outlines;
            }

            public int GameObjects { get; }
            public int Graphics { get; }
            public int RaycastTargets { get; }
            public int Outlines { get; }
        }
    }
}
