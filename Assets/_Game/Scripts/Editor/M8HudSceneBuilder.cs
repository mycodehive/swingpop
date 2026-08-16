using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SwingPop.Editor
{
    public static class M8HudSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/GameplayHUD.prefab";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/UI";
        private const string TuningPath = DataFolder + "/M8HudTuning.asset";

        private static readonly Color PanelColor = new(0.025f, 0.12f, 0.2f, 0.88f);
        private static readonly Color PanelLightColor = new(0.035f, 0.2f, 0.29f, 0.9f);
        private static readonly Color Cyan = new(0.2f, 0.9f, 1f, 1f);
        private static readonly Color Mint = new(0.28f, 1f, 0.72f, 1f);
        private static readonly Color Gold = new(1f, 0.84f, 0.2f, 1f);
        private static readonly Color SoftWhite = new(0.93f, 0.98f, 1f, 1f);

        private static Font font;
        private static Sprite backgroundSprite;
        private static Sprite circleSprite;

        [MenuItem("SwingPop/M8/Build Gameplay HUD")]
        public static void BuildGameplayHud()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            WindController wind = Object.FindAnyObjectByType<WindController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            Camera worldCamera = Camera.main;
            if (shotFlow == null || ball == null || wind == null || holeFlow == null || worldCamera == null)
            {
                Debug.LogError("M8 builder requires the completed M7 scene with Shot, Ball, Wind, Hole, and Main Camera.");
                return;
            }

            HudTuningData tuning = LoadOrCreateTuning();
            CreateOrReplacePrefab(tuning);

            GameObject existing = FindInScene(scene, "Gameplay HUD");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Transform presentation = FindInScene(scene, "Presentation")?.transform;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("M8 builder could not instantiate GameplayHUD.prefab.");
                return;
            }
            instance.name = "Gameplay HUD";
            if (presentation != null)
            {
                instance.transform.SetParent(presentation, false);
            }

            GameplayHudPresenter presenter = instance.GetComponent<GameplayHudPresenter>();
            SetObjectReference(presenter, "shotFlow", shotFlow);
            SetObjectReference(presenter, "ball", ball);
            SetObjectReference(presenter, "wind", wind);
            SetObjectReference(presenter, "holeFlow", holeFlow);
            SetObjectReference(presenter, "worldCamera", worldCamera);
            SetObjectReference(presenter, "tuning", tuning);

            EnsureEventSystem(scene, presentation);

            GameObject systems = FindInScene(scene, "M7 Character Animation Systems");
            if (systems != null)
            {
                systems.name = "M8 Gameplay Systems";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = instance;
            Debug.Log("SwingPop M8 Gameplay HUD scene wiring completed with uGUI Canvas and Input System pointer support.");
        }

        private static HudTuningData LoadOrCreateTuning()
        {
            HudTuningData data = AssetDatabase.LoadAssetAtPath<HudTuningData>(TuningPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HudTuningData>();
                AssetDatabase.CreateAsset(data, TuningPath);
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateOrReplacePrefab(HudTuningData tuning)
        {
            GameObject root = CreateRect(null, "Gameplay HUD", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            GameplayHudView view = root.AddComponent<GameplayHudView>();
            GameplayHudPresenter presenter = root.AddComponent<GameplayHudPresenter>();

            RectTransform safeArea = CreateRect(root.transform, "Safe Area", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero).GetComponent<RectTransform>();

            BuildPlayerHud(safeArea, out Text playerName, out Text stroke, out Text penalty);
            BuildHoleHud(safeArea, out Text hole, out Text par, out Text score);
            BuildWindHud(safeArea, out RectTransform windArrow, out Text windPreset, out Text windStrength);
            BuildAimHud(safeArea, out GameObject aimRoot, out RectTransform aimMarker, out Text distance, out Text height);
            BuildClubHud(safeArea, out Text clubInitial, out Text club, out Text lie, out GameObject spinRoot, out Text spin);
            BuildTimingHud(safeArea, out HudGaugeView gaugeView);
            BuildActionHud(safeArea, out GameObject actionRoot, out Button actionButton, out Text actionText);
            BuildFeedbackHud(safeArea, out HudPopupView impactPopup, out HudPopupView hazardPopup, out HudPopupView liePopup, out HudResultView resultView);

            SetObjectReference(view, "playerNameText", playerName);
            SetObjectReference(view, "strokeText", stroke);
            SetObjectReference(view, "penaltyText", penalty);
            SetObjectReference(view, "holeText", hole);
            SetObjectReference(view, "parText", par);
            SetObjectReference(view, "scoreText", score);
            SetObjectReference(view, "windArrow", windArrow);
            SetObjectReference(view, "windPresetText", windPreset);
            SetObjectReference(view, "windStrengthText", windStrength);
            SetObjectReference(view, "aimRoot", aimRoot);
            SetObjectReference(view, "aimMarker", aimMarker);
            SetObjectReference(view, "distanceText", distance);
            SetObjectReference(view, "heightText", height);
            SetObjectReference(view, "clubInitialText", clubInitial);
            SetObjectReference(view, "clubText", club);
            SetObjectReference(view, "lieText", lie);
            SetObjectReference(view, "spinRoot", spinRoot);
            SetObjectReference(view, "spinText", spin);
            SetObjectReference(view, "gaugeView", gaugeView);
            SetObjectReference(view, "actionRoot", actionRoot);
            SetObjectReference(view, "actionButton", actionButton);
            SetObjectReference(view, "actionText", actionText);
            SetObjectReference(view, "impactPopup", impactPopup);
            SetObjectReference(view, "hazardPopup", hazardPopup);
            SetObjectReference(view, "liePopup", liePopup);
            SetObjectReference(view, "resultView", resultView);

            SetObjectReference(presenter, "view", view);
            SetObjectReference(presenter, "tuning", tuning);
            SetObjectReference(presenter, "hudCanvas", canvas);
            SetObjectReference(presenter, "safeArea", safeArea);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildPlayerHud(RectTransform parent, out Text playerName, out Text stroke, out Text penalty)
        {
            GameObject panel = CreatePanel(parent, "Top Left - Player HUD", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(370f, 142f), new Vector2(32f, -28f), PanelColor);
            GameObject portrait = CreateImage(panel.transform, "Portrait Placeholder", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(94f, 94f), new Vector2(24f, 0f), Cyan, circleSprite);
            AddOutline(portrait, new Color(0.1f, 0.95f, 1f, 0.9f), new Vector2(3f, -3f));
            CreateText(portrait.transform, "Portrait Initial", "P", 36, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            playerName = CreateText(panel.transform, "Player Name", "PLAYER", 27, FontStyle.Bold, TextAnchor.MiddleLeft, SoftWhite, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, 40f), new Vector2(132f, -20f));
            stroke = CreateText(panel.transform, "Stroke", "STROKE  0", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Mint, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, 34f), new Vector2(132f, -62f));
            penalty = CreateText(panel.transform, "Penalty", "PENALTY  +0", 17, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.48f, 0.3f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, 28f), new Vector2(132f, -101f));
        }

        private static void BuildHoleHud(RectTransform parent, out Text hole, out Text par, out Text score)
        {
            GameObject panel = CreatePanel(parent, "Top Center - Hole HUD", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(410f, 132f), new Vector2(0f, -22f), PanelColor);
            hole = CreateText(panel.transform, "Hole", "HOLE 1", 31, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 44f), new Vector2(0f, -15f));
            par = CreateText(panel.transform, "Par", "PAR 4", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Cyan, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(0f, -5f));
            score = CreateText(panel.transform, "Live Score", "STROKE 0", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Gold, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(0f, -5f));
        }

        private static void BuildWindHud(RectTransform parent, out RectTransform arrow, out Text preset, out Text strength)
        {
            GameObject panel = CreatePanel(parent, "Top Right - Wind HUD", Vector2.one, Vector2.one, Vector2.one, new Vector2(350f, 142f), new Vector2(-32f, -28f), PanelColor);
            CreateText(panel.transform, "Label", "WIND", 18, FontStyle.Bold, TextAnchor.MiddleLeft, Cyan, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(180f, 28f), new Vector2(126f, -18f));
            GameObject arrowObject = CreateRect(panel.transform, "Wind Arrow", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), new Vector2(62f, -3f));
            arrow = arrowObject.GetComponent<RectTransform>();
            CreateImage(arrow, "Shaft", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(12f, 54f), new Vector2(0f, -10f), Gold, backgroundSprite);
            GameObject head = CreateImage(arrow, "Head", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(31f, 31f), new Vector2(0f, -12f), Gold, backgroundSprite);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            preset = CreateText(panel.transform, "Preset", "CALM", 22, FontStyle.Bold, TextAnchor.MiddleLeft, SoftWhite, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-132f, 34f), new Vector2(126f, 6f));
            strength = CreateText(panel.transform, "Strength", "0.0 m/s", 29, FontStyle.Bold, TextAnchor.MiddleLeft, Mint, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-132f, 42f), new Vector2(126f, 10f));
        }

        private static void BuildAimHud(RectTransform parent, out GameObject aimRoot, out RectTransform marker, out Text distance, out Text height)
        {
            aimRoot = CreateRect(parent, "Center - Aim HUD", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            GameObject markerObject = CreatePanel(aimRoot.transform, "Aim Target Marker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(236f, 112f), new Vector2(0f, 110f), new Color(0.02f, 0.15f, 0.22f, 0.72f));
            marker = markerObject.GetComponent<RectTransform>();
            CreateImage(marker, "Target Vertical", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(4f, 26f), new Vector2(0f, 12f), Cyan, backgroundSprite);
            CreateImage(marker, "Target Horizontal", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(26f, 4f), new Vector2(0f, 12f), Cyan, backgroundSprite);
            distance = CreateText(marker, "Remaining Distance", "78.0 m", 30, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, -9f));
            height = CreateText(marker, "Height Difference", "LEVEL  0.0 m", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Gold, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 4f));
        }

        private static void BuildClubHud(RectTransform parent, out Text clubInitial, out Text club, out Text lie, out GameObject spinRoot, out Text spin)
        {
            GameObject panel = CreatePanel(parent, "Bottom Left - Club HUD", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(410f, 178f), new Vector2(32f, 30f), PanelColor);
            GameObject icon = CreateImage(panel.transform, "Club Icon Placeholder", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 92f), new Vector2(24f, 16f), Cyan, circleSprite);
            clubInitial = CreateText(icon.transform, "Club Initial", "D", 40, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.02f, 0.15f, 0.2f), Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            club = CreateText(panel.transform, "Club", "TEMPORARY DRIVER", 27, FontStyle.Bold, TextAnchor.MiddleLeft, SoftWhite, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(-136f, -8f), new Vector2(136f, -3f));
            lie = CreateText(panel.transform, "Lie", "TEE", 20, FontStyle.Bold, TextAnchor.MiddleLeft, Mint, new Vector2(0f, 0.25f), new Vector2(1f, 0.64f), new Vector2(0f, 0.5f), new Vector2(-136f, 0f), new Vector2(136f, 0f));
            spinRoot = CreatePanel(panel.transform, "Spin Status", new Vector2(0f, 0f), new Vector2(1f, 0.28f), new Vector2(0.5f, 0f), new Vector2(-28f, 0f), new Vector2(14f, 10f), PanelLightColor);
            spin = CreateText(spinRoot.transform, "Spin", "NO SPIN  --", 18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, Vector2.zero, Vector2.one, Vector2.one * 0.5f, new Vector2(-10f, -4f), new Vector2(5f, 2f));
        }

        private static void BuildTimingHud(RectTransform parent, out HudGaugeView gaugeView)
        {
            GameObject timing = CreateRect(parent, "Bottom Center - Timing HUD", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(780f, 160f), new Vector2(0f, 26f));
            gaugeView = timing.AddComponent<HudGaugeView>();

            GameObject power = CreatePanel(timing.transform, "Power Gauge", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, PanelColor);
            Text powerPercent = CreateText(power.transform, "Power Percent", "0%", 29, FontStyle.Bold, TextAnchor.MiddleRight, SoftWhite, new Vector2(0.78f, 0.58f), new Vector2(0.97f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateText(power.transform, "Power Label", "POWER", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Mint, new Vector2(0.04f, 0.6f), new Vector2(0.28f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            GameObject powerTrack = CreatePanel(power.transform, "Power Track", new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.52f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.02f, 0.08f, 0.12f, 1f));
            Image powerFill = CreateImage(powerTrack.transform, "Power Fill", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, Mint, backgroundSprite).GetComponent<Image>();
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Horizontal;
            powerFill.fillOrigin = 0;
            GameObject maxZone = CreateImage(powerTrack.transform, "100 Percent Highlight", new Vector2(0.91f, 0f), Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(1f, 0.75f, 0.12f, 0.6f), backgroundSprite);
            maxZone.transform.SetAsLastSibling();
            RectTransform powerCursor = CreateImage(powerTrack.transform, "Power Cursor", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.one * 0.5f, new Vector2(8f, 64f), Vector2.zero, SoftWhite, backgroundSprite).GetComponent<RectTransform>();
            CanvasGroup glow = CreateImage(power.transform, "Power Active Glow", new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.96f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.2f, 0.95f, 1f, 0.08f), backgroundSprite).AddComponent<CanvasGroup>();
            glow.blocksRaycasts = false;

            GameObject impact = CreatePanel(timing.transform, "Impact Gauge", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, PanelColor);
            CreateText(impact.transform, "Impact Label", "IMPACT", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Cyan, new Vector2(0.04f, 0.6f), new Vector2(0.28f, 0.92f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Text impactPreview = CreateText(impact.transform, "Impact Preview", "MISS", 28, FontStyle.Bold, TextAnchor.MiddleRight, Gold, new Vector2(0.74f, 0.58f), new Vector2(0.96f, 0.94f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            GameObject impactTrack = CreatePanel(impact.transform, "Impact Track", new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.52f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.72f, 0.2f, 0.14f, 1f));
            RectTransform good = CreateImage(impactTrack.transform, "GOOD Zone", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.2f, 0.55f, 0.9f, 1f), backgroundSprite).GetComponent<RectTransform>();
            RectTransform great = CreateImage(impactTrack.transform, "GREAT Zone", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, new Color(0.18f, 0.88f, 0.72f, 1f), backgroundSprite).GetComponent<RectTransform>();
            RectTransform perfect = CreateImage(impactTrack.transform, "PERFECT Zone", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, Gold, backgroundSprite).GetComponent<RectTransform>();
            RectTransform impactCursor = CreateImage(impactTrack.transform, "Impact Cursor", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.one * 0.5f, new Vector2(9f, 68f), Vector2.zero, SoftWhite, backgroundSprite).GetComponent<RectTransform>();

            SetObjectReference(gaugeView, "powerRoot", power);
            SetObjectReference(gaugeView, "powerFill", powerFill);
            SetObjectReference(gaugeView, "powerCursor", powerCursor);
            SetObjectReference(gaugeView, "powerPercentText", powerPercent);
            SetObjectReference(gaugeView, "powerGlow", glow);
            SetObjectReference(gaugeView, "impactRoot", impact);
            SetObjectReference(gaugeView, "impactCursor", impactCursor);
            SetObjectReference(gaugeView, "goodZone", good);
            SetObjectReference(gaugeView, "greatZone", great);
            SetObjectReference(gaugeView, "perfectZone", perfect);
            SetObjectReference(gaugeView, "impactPreviewText", impactPreview);
        }

        private static void BuildActionHud(RectTransform parent, out GameObject actionRoot, out Button button, out Text actionText)
        {
            actionRoot = CreateRect(parent, "Bottom Right - Primary Action", Vector2.one, Vector2.one, Vector2.one, new Vector2(330f, 174f), new Vector2(-32f, -28f));
            GameObject buttonObject = CreatePanel(actionRoot.transform, "Shot Button", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f, new Vector2(304f, 126f), Vector2.zero, new Color(0.08f, 0.78f, 0.88f, 0.98f));
            AddOutline(buttonObject, new Color(0.65f, 1f, 1f, 0.95f), new Vector2(4f, -4f));
            button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.95f, 1f, 1f);
            colors.disabledColor = new Color(0.45f, 0.52f, 0.56f, 0.55f);
            button.colors = colors;
            actionText = CreateText(buttonObject.transform, "Action Label", "START SHOT", 31, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, Vector2.zero, Vector2.one, Vector2.one * 0.5f, new Vector2(-16f, -14f), new Vector2(8f, 8f));
            CreateText(buttonObject.transform, "Keyboard Hint", "SPACE / CLICK", 15, FontStyle.Bold, TextAnchor.LowerCenter, new Color(0.8f, 1f, 1f), Vector2.zero, Vector2.one, Vector2.one * 0.5f, new Vector2(-14f, 8f), new Vector2(7f, 0f));
        }

        private static void BuildFeedbackHud(RectTransform parent, out HudPopupView impact, out HudPopupView hazard, out HudPopupView lie, out HudResultView result)
        {
            GameObject popups = CreateRect(parent, "Popups", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            impact = CreatePopup(popups.transform, "Impact Feedback", new Vector2(0f, 185f), new Vector2(370f, 100f), 42);
            hazard = CreatePopup(popups.transform, "Hazard Feedback", new Vector2(0f, 190f), new Vector2(500f, 138f), 34);
            lie = CreatePopup(popups.transform, "Lie Feedback", new Vector2(0f, 98f), new Vector2(280f, 72f), 26);

            GameObject resultRoot = CreateRect(popups.transform, "Result", Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            result = resultRoot.AddComponent<HudResultView>();
            GameObject panel = CreatePanel(resultRoot.transform, "Result Panel", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(560f, 430f), Vector2.zero, new Color(0.025f, 0.12f, 0.2f, 0.97f));
            AddOutline(panel, new Color(0.25f, 0.95f, 1f, 0.95f), new Vector2(5f, -5f));
            CanvasGroup group = resultRoot.AddComponent<CanvasGroup>();
            Text resultHole = CreateText(panel.transform, "Hole", "HOLE 1", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Cyan, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Text resultPar = CreateText(panel.transform, "Par", "PAR 4", 23, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.76f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Text resultStrokes = CreateText(panel.transform, "Strokes", "STROKES 4", 26, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.62f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Text resultLabel = CreateText(panel.transform, "Result Label", "PAR  EVEN", 45, FontStyle.Bold, TextAnchor.MiddleCenter, Gold, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.44f), Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            SetObjectReference(result, "root", resultRoot);
            SetObjectReference(result, "canvasGroup", group);
            SetObjectReference(result, "panel", panel.GetComponent<RectTransform>());
            SetObjectReference(result, "holeText", resultHole);
            SetObjectReference(result, "parText", resultPar);
            SetObjectReference(result, "strokesText", resultStrokes);
            SetObjectReference(result, "resultText", resultLabel);
        }

        private static HudPopupView CreatePopup(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            GameObject root = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, size, position);
            HudPopupView popup = root.AddComponent<HudPopupView>();
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            Image background = root.AddComponent<Image>();
            background.sprite = backgroundSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.02f, 0.11f, 0.18f, 0.94f);
            AddOutline(root, new Color(0.2f, 0.9f, 1f, 0.75f), new Vector2(3f, -3f));
            Text message = CreateText(root.transform, "Message", name.ToUpperInvariant(), fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite, Vector2.zero, Vector2.one, Vector2.one * 0.5f, new Vector2(-18f, -14f), new Vector2(9f, 7f));
            SetObjectReference(popup, "canvasGroup", group);
            SetObjectReference(popup, "panel", root.GetComponent<RectTransform>());
            SetObjectReference(popup, "messageText", message);
            return popup;
        }

        private static void EnsureEventSystem(Scene scene, Transform parent)
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy);
                }
                InputSystemUIInputModule current = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (current == null)
                {
                    current = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    current.AssignDefaultActions();
                }
                return;
            }

            GameObject eventObject = new("HUD Event System");
            SceneManager.MoveGameObjectToScene(eventObject, scene);
            if (parent != null)
            {
                eventObject.transform.SetParent(parent, false);
            }
            eventObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Color color)
        {
            GameObject panel = CreateRect(parent, name, anchorMin, anchorMax, pivot, size, position);
            Image image = panel.AddComponent<Image>();
            image.sprite = backgroundSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static GameObject CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Color color, Sprite sprite)
        {
            GameObject imageObject = CreateRect(parent, name, anchorMin, anchorMax, pivot, size, position);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite == backgroundSprite ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return imageObject;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle style, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            GameObject textObject = CreateRect(parent, name, anchorMin, anchorMax, pivot, size, position);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.03f, 0.07f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
            return text;
        }

        private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            GameObject result = new(name, typeof(RectTransform));
            RectTransform rect = result.GetComponent<RectTransform>();
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            return result;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
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
