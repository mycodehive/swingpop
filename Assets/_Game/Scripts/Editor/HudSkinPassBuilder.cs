using System;
using System.Collections.Generic;
using System.IO;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SwingPop.Editor
{
    public static class HudSkinPassBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SourcePrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD.prefab";
        private const string PrefabPath = "Assets/_Game/Prefabs/UI/GameplayHUD_SwingPopSkin.prefab";
        private const string ArtFolder = "Assets/_Game/Art/UI/HudSkinPass";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/UI";
        private const string SkinPath = DataFolder + "/SwingPopHudSkin.asset";

        private static readonly Color Panel = new(0.018f, 0.105f, 0.16f, 0.88f);
        private static readonly Color Raised = new(0.025f, 0.2f, 0.27f, 0.94f);
        private static readonly Color Cyan = new(0.18f, 0.92f, 1f, 1f);
        private static readonly Color Mint = new(0.33f, 1f, 0.72f, 1f);
        private static readonly Color Pink = new(1f, 0.27f, 0.62f, 1f);
        private static readonly Color Gold = new(1f, 0.79f, 0.16f, 1f);
        private static readonly Color White = new(0.96f, 0.99f, 1f, 1f);
        private static readonly Color Secondary = new(0.67f, 0.9f, 0.96f, 1f);

        [MenuItem("SwingPop/UI/Build HUD Skin Pass")]
        public static void BuildHudSkinPass()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(DataFolder);
            GenerateSprites();
            AssetDatabase.Refresh();

            HudSkinData skin = LoadOrCreateSkin();
            BuildSkinnedPrefab(skin);
            WireHoleScene(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HUD SKIN PASS BUILD COMPLETE | Hole01 uses the isolated SwingPop HUD skin prefab; gameplay bindings preserved.");
        }

        private static void GenerateSprites()
        {
            WriteSprite("HUD_RoundedPanel", DrawRoundedRect(64, 64, 14f), new Vector4(16f, 16f, 16f, 16f));
            WriteSprite("HUD_Capsule", DrawRoundedRect(64, 32, 15f), new Vector4(15f, 15f, 15f, 15f));
            WriteSprite("HUD_Circle", DrawCircle(64, false), Vector4.zero);
            WriteSprite("HUD_Diamond", DrawDiamond(64), Vector4.zero);
            WriteSprite("HUD_Triangle", DrawTriangle(64), Vector4.zero);
            WriteSprite("HUD_Player", DrawPlayer(64), Vector4.zero);
            WriteSprite("HUD_Wind", DrawArrow(64, IconDirection.Up, true), Vector4.zero);
            WriteSprite("HUD_Driver", DrawClub(64, false), Vector4.zero);
            WriteSprite("HUD_Putter", DrawClub(64, true), Vector4.zero);
            WriteSprite("HUD_SpinNone", DrawSpinNone(64), Vector4.zero);
            WriteSprite("HUD_SpinTop", DrawArrow(64, IconDirection.Up, false), Vector4.zero);
            WriteSprite("HUD_SpinBack", DrawArrow(64, IconDirection.Down, false), Vector4.zero);
            WriteSprite("HUD_SpinLeft", DrawArrow(64, IconDirection.Left, false), Vector4.zero);
            WriteSprite("HUD_SpinRight", DrawArrow(64, IconDirection.Right, false), Vector4.zero);
            WriteSprite("HUD_Target", DrawTarget(64), Vector4.zero);
        }

        private static HudSkinData LoadOrCreateSkin()
        {
            HudSkinData skin = AssetDatabase.LoadAssetAtPath<HudSkinData>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<HudSkinData>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            SerializedObject serialized = new(skin);
            SetColor(serialized, "panelColor", Panel);
            SetColor(serialized, "raisedPanelColor", Raised);
            SetColor(serialized, "borderColor", new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f));
            SetColor(serialized, "shadowColor", new Color(0.005f, 0.025f, 0.055f, 0.5f));
            SetColor(serialized, "cyan", Cyan);
            SetColor(serialized, "mint", Mint);
            SetColor(serialized, "pink", Pink);
            SetColor(serialized, "gold", Gold);
            SetColor(serialized, "coral", new Color(1f, 0.34f, 0.26f, 1f));
            SetColor(serialized, "disabled", new Color(0.43f, 0.57f, 0.66f, 1f));
            SetColor(serialized, "primaryText", White);
            SetColor(serialized, "secondaryText", Secondary);
            SetColor(serialized, "fairway", new Color(0.33f, 0.95f, 0.57f, 1f));
            SetColor(serialized, "rough", new Color(0.18f, 0.65f, 0.39f, 1f));
            SetColor(serialized, "bunker", new Color(1f, 0.72f, 0.3f, 1f));
            SetColor(serialized, "green", new Color(0.42f, 1f, 0.78f, 1f));

            string[] fields =
            {
                "roundedPanel", "capsule", "circle", "diamond", "triangle", "playerIcon", "windIcon",
                "driverIcon", "putterIcon", "spinNoneIcon", "spinTopIcon", "spinBackIcon", "spinLeftIcon",
                "spinRightIcon", "targetIcon"
            };
            string[] assets =
            {
                "HUD_RoundedPanel", "HUD_Capsule", "HUD_Circle", "HUD_Diamond", "HUD_Triangle", "HUD_Player", "HUD_Wind",
                "HUD_Driver", "HUD_Putter", "HUD_SpinNone", "HUD_SpinTop", "HUD_SpinBack", "HUD_SpinLeft",
                "HUD_SpinRight", "HUD_Target"
            };
            for (int index = 0; index < fields.Length; index++)
            {
                serialized.FindProperty(fields[index]).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{assets[index]}.png");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static void BuildSkinnedPrefab(HudSkinData skin)
        {
            GameObject source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
            try
            {
                source.name = "Gameplay HUD - SwingPop Skin";
                ApplySkin(source.transform, skin);
                PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(source);
            }
        }

        private static void WireHoleScene(HudSkinData skin)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject existing = FindInScene(scene, "Gameplay HUD");
            GameplayHudPresenter oldPresenter = existing != null ? existing.GetComponent<GameplayHudPresenter>() : null;

            ShotFlowController shotFlow = ReadReference<ShotFlowController>(oldPresenter, "shotFlow")
                                             ?? UnityEngine.Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = ReadReference<GolfBallController>(oldPresenter, "ball")
                                      ?? UnityEngine.Object.FindAnyObjectByType<GolfBallController>();
            WindController wind = ReadReference<WindController>(oldPresenter, "wind")
                                  ?? UnityEngine.Object.FindAnyObjectByType<WindController>();
            HoleFlowController holeFlow = ReadReference<HoleFlowController>(oldPresenter, "holeFlow")
                                          ?? UnityEngine.Object.FindAnyObjectByType<HoleFlowController>();
            Camera worldCamera = ReadReference<Camera>(oldPresenter, "worldCamera") ?? Camera.main;
            HudTuningData tuning = ReadReference<HudTuningData>(oldPresenter, "tuning");
            Transform parent = existing != null ? existing.transform.parent : FindInScene(scene, "Presentation")?.transform;

            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate the HUD skin prefab.");
            instance.name = "Gameplay HUD";
            if (parent != null) instance.transform.SetParent(parent, false);

            GameplayHudPresenter presenter = instance.GetComponent<GameplayHudPresenter>();
            SetObjectReference(presenter, "shotFlow", shotFlow);
            SetObjectReference(presenter, "ball", ball);
            SetObjectReference(presenter, "wind", wind);
            SetObjectReference(presenter, "holeFlow", holeFlow);
            SetObjectReference(presenter, "worldCamera", worldCamera);
            if (tuning != null) SetObjectReference(presenter, "tuning", tuning);
            SetObjectReference(instance.GetComponent<GameplayHudView>(), "skin", skin);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = instance;
        }

        private static void ApplySkin(Transform root, HudSkinData skin)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            GameplayHudView view = root.GetComponent<GameplayHudView>();
            HudGaugeView gauge = root.GetComponentInChildren<HudGaugeView>(true);
            HudResultView result = root.GetComponentInChildren<HudResultView>(true);
            SetObjectReference(view, "skin", skin);
            SetObjectReference(gauge, "skin", skin);
            SetObjectReference(result, "skin", skin);

            SkinPanel(root, "Top Left - Player HUD", new Vector2(330f, 116f), new Vector2(26f, -24f), skin);
            SkinPanel(root, "Top Center - Hole HUD", new Vector2(340f, 106f), new Vector2(0f, -20f), skin);
            SkinPanel(root, "Top Right - Wind HUD", new Vector2(300f, 116f), new Vector2(-26f, -24f), skin);
            SkinPanel(root, "Bottom Left - Club HUD", new Vector2(350f, 150f), new Vector2(26f, 26f), skin);
            SkinPanel(root, "Power Gauge", Vector2.zero, Vector2.zero, skin);
            SkinPanel(root, "Impact Gauge", Vector2.zero, Vector2.zero, skin);
            SkinPanel(root, "Result Panel", new Vector2(520f, 390f), Vector2.zero, skin);

            ConfigureRect(root, "Bottom Center - Timing HUD", new Vector2(720f, 138f), new Vector2(0f, 24f), Vector3.one);
            RectTransform actionRoot = Find(root, "Bottom Right - Primary Action") as RectTransform;
            if (actionRoot != null)
            {
                actionRoot.anchorMin = new Vector2(1f, 0f);
                actionRoot.anchorMax = new Vector2(1f, 0f);
                actionRoot.pivot = new Vector2(1f, 0f);
                actionRoot.sizeDelta = new Vector2(290f, 150f);
                actionRoot.anchoredPosition = new Vector2(-28f, 28f);
                actionRoot.localScale = Vector3.one;
            }
            ConfigureRect(root, "Shot Button", new Vector2(258f, 108f), Vector2.zero, Vector3.one);
            ConfigureRect(root, "Aim Target Marker", new Vector2(204f, 90f), new Vector2(0f, 106f), Vector3.one * 0.9f);

            AddPanelAccent(root, "Top Left - Player HUD", "Player Accent", Pink, HorizontalEdge.Top, skin);
            AddPanelAccent(root, "Top Center - Hole HUD", "Hole Accent", Cyan, HorizontalEdge.Bottom, skin);
            AddPanelAccent(root, "Top Right - Wind HUD", "Wind Accent", Gold, HorizontalEdge.Top, skin);
            AddPanelAccent(root, "Bottom Left - Club HUD", "Club Accent", Mint, HorizontalEdge.Bottom, skin);

            Image portraitFrame = Find(root, "Portrait Placeholder")?.GetComponent<Image>();
            if (portraitFrame != null)
            {
                portraitFrame.sprite = skin.Circle;
                portraitFrame.type = Image.Type.Simple;
                portraitFrame.color = Raised;
                ConfigureRect(root, "Portrait Placeholder", new Vector2(88f, 88f), new Vector2(18f, 0f), Vector3.one);
                Image portrait = EnsureImage(portraitFrame.transform, "Player Silhouette", skin.PlayerIcon, Cyan,
                    new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
                SetObjectReference(view, "playerPortraitImage", portrait);
                SetActive(root, "Portrait Initial", false);
            }

            Image clubFrame = Find(root, "Club Icon Placeholder")?.GetComponent<Image>();
            if (clubFrame != null)
            {
                clubFrame.sprite = skin.Circle;
                clubFrame.type = Image.Type.Simple;
                clubFrame.color = Raised;
                ConfigureRect(root, "Club Icon Placeholder", new Vector2(80f, 80f), new Vector2(20f, 14f), Vector3.one);
                Image clubIcon = EnsureImage(clubFrame.transform, "Club Silhouette", skin.DriverIcon, White,
                    new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f));
                SetObjectReference(view, "clubIconImage", clubIcon);
                SetActive(root, "Club Initial", false);
            }

            Transform spinRoot = Find(root, "Spin Status");
            if (spinRoot != null)
            {
                SkinImage(spinRoot.GetComponent<Image>(), skin.Capsule, new Color(0.02f, 0.14f, 0.2f, 0.88f));
                Image spinIcon = EnsureImage(spinRoot, "Spin Direction Icon", skin.SpinNoneIcon, Cyan,
                    new Vector2(0.035f, 0.18f), new Vector2(0.16f, 0.82f));
                SetObjectReference(view, "spinIconImage", spinIcon);
                RectTransform spinText = Find(root, "Spin") as RectTransform;
                if (spinText != null)
                {
                    spinText.anchorMin = new Vector2(0.17f, 0f);
                    spinText.offsetMin = Vector2.zero;
                }
            }

            Image lieAccent = EnsureImage(Find(root, "Bottom Left - Club HUD"), "Lie Accent", skin.Capsule, Mint,
                new Vector2(0.31f, 0.39f), new Vector2(0.35f, 0.58f));
            SetObjectReference(view, "lieAccentImage", lieAccent);

            ConfigureWind(root, skin);
            ConfigureAim(root, skin);
            ConfigureGauges(root, skin, gauge);
            ConfigureAction(root, skin, view);
            ConfigurePopups(root, skin);
            ConfigureResult(root, skin, result);
            ConfigureTypography(root, skin);

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void ConfigureWind(Transform root, HudSkinData skin)
        {
            Transform arrow = Find(root, "Wind Arrow");
            if (arrow == null) return;
            SetActive(root, "Shaft", false);
            SetActive(root, "Head", false);
            Image icon = EnsureImage(arrow, "Wind Arrow Icon", skin.WindIcon, Gold, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            icon.preserveAspect = true;
        }

        private static void ConfigureAim(Transform root, HudSkinData skin)
        {
            Transform marker = Find(root, "Aim Target Marker");
            if (marker == null) return;
            SkinImage(marker.GetComponent<Image>(), skin.RoundedPanel, new Color(Panel.r, Panel.g, Panel.b, 0.7f));
            Image icon = EnsureImage(marker, "Target Emblem", skin.TargetIcon, Cyan,
                new Vector2(0.41f, 0.76f), new Vector2(0.59f, 1.14f));
            icon.preserveAspect = true;
            SetActive(root, "Target Vertical", false);
            SetActive(root, "Target Horizontal", false);
        }

        private static void ConfigureGauges(Transform root, HudSkinData skin, HudGaugeView gauge)
        {
            foreach (string name in new[] { "Power Track", "Impact Track" })
            {
                Transform track = Find(root, name);
                if (track == null) continue;
                SkinImage(track.GetComponent<Image>(), skin.Capsule, new Color(0.008f, 0.045f, 0.075f, 0.98f));
                RectTransform rect = track.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.055f, 0.2f);
                rect.anchorMax = new Vector2(0.945f, 0.48f);
            }

            Image powerFill = Find(root, "Power Fill")?.GetComponent<Image>();
            SkinImage(powerFill, skin.Capsule, Cyan);
            if (powerFill != null)
            {
                powerFill.type = Image.Type.Filled;
                powerFill.fillMethod = Image.FillMethod.Horizontal;
                powerFill.fillOrigin = 0;
            }

            Image powerCursor = Find(root, "Power Cursor")?.GetComponent<Image>();
            SkinImage(powerCursor, skin.Diamond, White);
            ConfigureRect(root, "Power Cursor", new Vector2(24f, 46f), Vector2.zero, Vector3.one);
            SetObjectReference(gauge, "powerCursorImage", powerCursor);

            Image impactCursor = Find(root, "Impact Cursor")?.GetComponent<Image>();
            SkinImage(impactCursor, skin.Diamond, White);
            ConfigureRect(root, "Impact Cursor", new Vector2(24f, 48f), Vector2.zero, Vector3.one);
            SetObjectReference(gauge, "impactCursorImage", impactCursor);

            SkinImage(Find(root, "GOOD Zone")?.GetComponent<Image>(), skin.Capsule, new Color(0.22f, 0.55f, 0.92f, 1f));
            SkinImage(Find(root, "GREAT Zone")?.GetComponent<Image>(), skin.Capsule, Mint);
            Image perfect = Find(root, "PERFECT Zone")?.GetComponent<Image>();
            SkinImage(perfect, skin.Capsule, Gold);
            SetObjectReference(gauge, "perfectZoneImage", perfect);

            Transform max = Find(root, "100 Percent Highlight");
            SkinImage(max?.GetComponent<Image>(), skin.Capsule, new Color(Gold.r, Gold.g, Gold.b, 0.72f));
            if (max != null)
            {
                RectTransform rect = max.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.9f, 0f);
            }
        }

        private static void ConfigureAction(Transform root, HudSkinData skin, GameplayHudView view)
        {
            Transform action = Find(root, "Bottom Right - Primary Action");
            Transform buttonTransform = Find(root, "Shot Button");
            if (action == null || buttonTransform == null) return;

            Image accent = EnsureImage(action, "Action Accent", skin.RoundedPanel, Cyan,
                new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));
            accent.transform.SetAsFirstSibling();
            Image buttonImage = buttonTransform.GetComponent<Image>();
            SkinImage(buttonImage, skin.RoundedPanel, new Color(0.03f, 0.5f, 0.64f, 0.98f));
            buttonImage.raycastTarget = true;
            buttonTransform.SetAsLastSibling();

            Button button = buttonTransform.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.55f, 0.62f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            SetObjectReference(view, "actionButtonImage", buttonImage);
            SetObjectReference(view, "actionAccentImage", accent);
        }

        private static void ConfigurePopups(Transform root, HudSkinData skin)
        {
            foreach (string name in new[] { "Impact Feedback", "Hazard Feedback", "Lie Feedback" })
            {
                Transform popupTransform = Find(root, name);
                if (popupTransform == null) continue;
                Image panelImage = popupTransform.GetComponent<Image>();
                SkinImage(panelImage, skin.RoundedPanel, new Color(Panel.r, Panel.g, Panel.b, 0.94f));
                Image accent = EnsureImage(popupTransform, "Popup Accent", skin.Capsule, Cyan,
                    new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.09f));
                HudPopupView popup = popupTransform.GetComponent<HudPopupView>();
                SetObjectReference(popup, "panelImage", panelImage);
                SetObjectReference(popup, "accentImage", accent);
            }
        }

        private static void ConfigureResult(Transform root, HudSkinData skin, HudResultView result)
        {
            Transform panel = Find(root, "Result Panel");
            if (panel == null) return;
            SkinImage(panel.GetComponent<Image>(), skin.RoundedPanel, new Color(Panel.r, Panel.g, Panel.b, 0.96f));
            Image accent = EnsureImage(panel, "Result Accent", skin.Capsule, Gold,
                new Vector2(0.12f, 0.075f), new Vector2(0.88f, 0.095f));
            Image emblem = EnsureImage(panel, "Result Emblem", skin.Diamond, new Color(Gold.r, Gold.g, Gold.b, 0.2f),
                new Vector2(0.36f, 0.22f), new Vector2(0.64f, 0.58f));
            emblem.transform.SetAsFirstSibling();
            SetObjectReference(result, "accentImage", accent);
            SetObjectReference(result, "emblemImage", emblem);
        }

        private static void ConfigureTypography(Transform root, HudSkinData skin)
        {
            SetText(root, "Player Name", 22, White);
            SetText(root, "Stroke", 18, Mint);
            SetText(root, "Penalty", 14, new Color(1f, 0.42f, 0.3f));
            SetText(root, "Hole", 28, White);
            SetText(root, "Par", 17, Cyan);
            SetText(root, "Live Score", 17, Gold);
            SetText(root, "Label", 14, Cyan);
            SetText(root, "Preset", 16, Secondary);
            SetText(root, "Strength", 24, White);
            SetText(root, "Remaining Distance", 27, White);
            SetText(root, "Height Difference", 16, Gold);
            SetText(root, "Club", 24, White);
            SetText(root, "Lie", 16, Mint);
            SetText(root, "Spin", 15, Secondary);
            SetText(root, "Power Label", 18, Mint);
            SetText(root, "Power Percent", 25, White);
            SetText(root, "Impact Label", 18, Cyan);
            SetText(root, "Impact Preview", 24, Gold);
            SetText(root, "Action Label", 26, White);
            SetText(root, "Keyboard Hint", 12, Secondary);
            SetText(root, "Result Label", 44, Gold);
        }

        private static void SkinPanel(Transform root, string name, Vector2 size, Vector2 position, HudSkinData skin)
        {
            Transform panel = Find(root, name);
            if (panel == null) return;
            ConfigureRect(root, name, size, position, Vector3.one);
            SkinImage(panel.GetComponent<Image>(), skin.RoundedPanel, skin.PanelColor);
        }

        private static void AddPanelAccent(Transform root, string panelName, string accentName, Color color, HorizontalEdge edge, HudSkinData skin)
        {
            Transform panel = Find(root, panelName);
            if (panel == null) return;
            Vector2 min = edge == HorizontalEdge.Top ? new Vector2(0.08f, 0.94f) : new Vector2(0.08f, 0.035f);
            Vector2 max = edge == HorizontalEdge.Top ? new Vector2(0.92f, 0.98f) : new Vector2(0.92f, 0.075f);
            EnsureImage(panel, accentName, skin.Capsule, color, min, max);
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existing = FindDirect(parent, name);
            GameObject target = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            RectTransform rect = target.GetComponent<RectTransform>();
            if (existing == null) rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureRect(Transform root, string name, Vector2 size, Vector2 position, Vector3 scale)
        {
            RectTransform rect = Find(root, name) as RectTransform;
            if (rect == null) return;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = scale;
        }

        private static void SkinImage(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }

        private static void SetText(Transform root, string name, int size, Color color)
        {
            Text text = Find(root, name)?.GetComponent<Text>();
            if (text == null) return;
            text.fontSize = size;
            text.color = color;
            Outline outline = text.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0f, 0.035f, 0.07f, 0.72f);
                outline.effectDistance = new Vector2(1.25f, -1.25f);
            }
        }

        private static void SetActive(Transform root, string name, bool active)
        {
            Transform target = Find(root, name);
            if (target != null) target.gameObject.SetActive(active);
        }

        private static void WriteSprite(string name, Color32[] pixels, Vector4 border)
        {
            int width = name == "HUD_Capsule" ? 64 : 64;
            int height = name == "HUD_Capsule" ? 32 : 64;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            string assetPath = $"{ArtFolder}/{name}.png";
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 64f;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static Color32[] DrawRoundedRect(int width, int height, float radius)
        {
            Color32[] pixels = Blank(width, height);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (width - 1) * 0.5f) - (width * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - (height - 1) * 0.5f) - (height * 0.5f - radius), 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }
            return pixels;
        }

        private static Color32[] DrawCircle(int size, bool ring)
        {
            Color32[] pixels = Blank(size, size);
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float outer = size * 0.45f;
            float inner = ring ? size * 0.32f : 0f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= outer && distance >= inner) pixels[y * size + x] = Color.white;
            }
            return pixels;
        }

        private static Color32[] DrawDiamond(int size)
        {
            Color32[] pixels = Blank(size, size);
            float center = (size - 1) * 0.5f;
            float radius = size * 0.43f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (Mathf.Abs(x - center) + Mathf.Abs(y - center) <= radius) pixels[y * size + x] = Color.white;
            }
            return pixels;
        }

        private static Color32[] DrawTriangle(int size)
        {
            Color32[] pixels = Blank(size, size);
            FillTriangle(pixels, size, new Vector2(size * 0.5f, size * 0.88f), new Vector2(size * 0.12f, size * 0.18f), new Vector2(size * 0.88f, size * 0.18f));
            return pixels;
        }

        private static Color32[] DrawPlayer(int size)
        {
            Color32[] pixels = Blank(size, size);
            FillCircle(pixels, size, new Vector2(size * 0.5f, size * 0.68f), size * 0.17f);
            FillEllipse(pixels, size, new Vector2(size * 0.5f, size * 0.31f), size * 0.3f, size * 0.22f);
            return pixels;
        }

        private static Color32[] DrawClub(int size, bool putter)
        {
            Color32[] pixels = Blank(size, size);
            DrawThickLine(pixels, size, new Vector2(size * 0.35f, size * 0.82f), new Vector2(size * 0.58f, size * 0.2f), 4f);
            if (putter)
            {
                FillRect(pixels, size, new Rect(size * 0.48f, size * 0.13f, size * 0.32f, size * 0.12f));
            }
            else
            {
                FillEllipse(pixels, size, new Vector2(size * 0.66f, size * 0.2f), size * 0.2f, size * 0.12f);
            }
            return pixels;
        }

        private static Color32[] DrawSpinNone(int size)
        {
            Color32[] pixels = DrawCircle(size, true);
            FillRect(pixels, size, new Rect(size * 0.24f, size * 0.46f, size * 0.52f, size * 0.08f));
            return pixels;
        }

        private static Color32[] DrawArrow(int size, IconDirection direction, bool withStreaks)
        {
            Color32[] pixels = Blank(size, size);
            FillRect(pixels, size, new Rect(size * 0.44f, size * 0.2f, size * 0.12f, size * 0.45f));
            FillTriangle(pixels, size, new Vector2(size * 0.5f, size * 0.88f), new Vector2(size * 0.24f, size * 0.58f), new Vector2(size * 0.76f, size * 0.58f));
            if (withStreaks)
            {
                FillRect(pixels, size, new Rect(size * 0.18f, size * 0.16f, size * 0.2f, size * 0.06f));
                FillRect(pixels, size, new Rect(size * 0.1f, size * 0.29f, size * 0.24f, size * 0.06f));
            }
            RotatePixels(pixels, size, direction);
            return pixels;
        }

        private static Color32[] DrawTarget(int size)
        {
            Color32[] pixels = DrawCircle(size, true);
            DrawThickLine(pixels, size, new Vector2(size * 0.5f, size * 0.08f), new Vector2(size * 0.5f, size * 0.92f), 3f);
            DrawThickLine(pixels, size, new Vector2(size * 0.08f, size * 0.5f), new Vector2(size * 0.92f, size * 0.5f), 3f);
            FillCircle(pixels, size, Vector2.one * size * 0.5f, size * 0.08f);
            return pixels;
        }

        private static void RotatePixels(Color32[] pixels, int size, IconDirection direction)
        {
            int turns = direction switch
            {
                IconDirection.Right => 1,
                IconDirection.Down => 2,
                IconDirection.Left => 3,
                _ => 0
            };
            for (int turn = 0; turn < turns; turn++)
            {
                Color32[] copy = (Color32[])pixels.Clone();
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = copy[(size - 1 - x) * size + y];
            }
        }

        private static void FillCircle(Color32[] pixels, int size, Vector2 center, float radius)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (Vector2.Distance(new Vector2(x, y), center) <= radius) pixels[y * size + x] = Color.white;
        }

        private static void FillEllipse(Color32[] pixels, int size, Vector2 center, float radiusX, float radiusY)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                if (dx * dx + dy * dy <= 1f) pixels[y * size + x] = Color.white;
            }
        }

        private static void FillRect(Color32[] pixels, int size, Rect rect)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(rect.xMin), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(rect.xMax), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(rect.yMin), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(rect.yMax), 0, size - 1);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++) pixels[y * size + x] = Color.white;
        }

        private static void FillTriangle(Color32[] pixels, int size, Vector2 a, Vector2 b, Vector2 c)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float d1 = Sign(p, a, b);
                float d2 = Sign(p, b, c);
                float d3 = Sign(p, c, a);
                if (!((d1 < 0f || d2 < 0f || d3 < 0f) && (d1 > 0f || d2 > 0f || d3 > 0f)))
                    pixels[y * size + x] = Color.white;
            }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void DrawThickLine(Color32[] pixels, int size, Vector2 start, Vector2 end, float thickness)
        {
            Vector2 segment = end - start;
            float squaredLength = segment.sqrMagnitude;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new(x, y);
                float t = squaredLength > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / squaredLength) : 0f;
                if (Vector2.Distance(point, start + segment * t) <= thickness) pixels[y * size + x] = Color.white;
            }
        }

        private static Color32[] Blank(int width, int height)
        {
            return new Color32[width * height];
        }

        private static void EnsureFolder(string path)
        {
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform match = Find(root.GetChild(index), name);
                if (match != null) return match;
            }
            return null;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
                if (parent.GetChild(index).name == name) return parent.GetChild(index);
            return null;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = Find(root.transform, name);
                if (match != null) return match.gameObject;
            }
            return null;
        }

        private static T ReadReference<T>(UnityEngine.Object target, string propertyName) where T : UnityEngine.Object
        {
            if (target == null) return null;
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property?.objectReferenceValue as T;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name} has no '{propertyName}' field.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(SerializedObject serialized, string propertyName, Color value)
        {
            serialized.FindProperty(propertyName).colorValue = value;
        }

        private enum HorizontalEdge { Top, Bottom }
        private enum IconDirection { Up, Right, Down, Left }
    }
}
