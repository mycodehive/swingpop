using System;
using System.Collections.Generic;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class M12OnlineFoundationBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string SettingsFolder = "Assets/_Game/ScriptableObjects/Online";
        private const string SettingsPath = SettingsFolder + "/M12MultiplayerDevelopmentSettings.asset";
        private const string RootName = "M12 Online Foundation";

        [MenuItem("SwingPop/Online/Build M12 Foundation")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureFolder(SettingsFolder);
            MultiplayerDevelopmentSettings settings = AssetDatabase.LoadAssetAtPath<MultiplayerDevelopmentSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MultiplayerDevelopmentSettings>();
                settings.ConfigureForDevelopment(MultiplayerDevelopmentMode.OfflineSingle, 0, false);
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            GameObject existing = FindInScene(scene, RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject stalePanel;
            while ((stalePanel = FindInScene(scene, "M12 Turn Panel")) != null)
                Object.DestroyImmediate(stalePanel);

            ShotFlowController shotFlow = Require<ShotFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            HoleFlowController holeFlow = Require<HoleFlowController>();
            GameplayHudView hudView = Require<GameplayHudView>();

            GameObject root = new(RootName);
            LocalMatchAuthority authority = root.AddComponent<LocalMatchAuthority>();
            LocalLoopbackTransport transport = root.AddComponent<LocalLoopbackTransport>();
            UnityTransportMatchTransport networkTransport = root.AddComponent<UnityTransportMatchTransport>();
            DedicatedServerMatchTransport dedicatedServerTransport = root.AddComponent<DedicatedServerMatchTransport>();
            DedicatedServerBootstrap dedicatedServerBootstrap = root.AddComponent<DedicatedServerBootstrap>();
            ReconnectController reconnectController = root.AddComponent<ReconnectController>();
            AuthenticationController authenticationController = root.AddComponent<AuthenticationController>();
            MatchSessionController session = root.AddComponent<MatchSessionController>();
            MultiplayerTurnPresenter presenter = root.AddComponent<MultiplayerTurnPresenter>();
            MultiplayerDebugOverlay debug = root.AddComponent<MultiplayerDebugOverlay>();

            SetReference(transport, "authority", authority);
            SetReference(networkTransport, "authority", authority);
            SetReference(session, "settings", settings);
            SetReference(session, "authority", authority);
            SetReference(session, "transport", transport);
            SetReference(session, "networkTransport", networkTransport);
            SetReference(session, "dedicatedServerTransport", dedicatedServerTransport);
            SetReference(session, "reconnectController", reconnectController);
            SetReference(session, "authenticationController", authenticationController);
            SetReference(session, "shotFlow", shotFlow);
            SetReference(session, "ball", ball);
            SetReference(session, "holeFlow", holeFlow);
            SetObjectArray(session, "surfaces", CollectSurfaceData());
            SetReference(dedicatedServerBootstrap, "settings", settings);
            SetReference(reconnectController, "settings", settings);
            SetReference(reconnectController, "networkTransport", networkTransport);
            SetReference(authenticationController, "settings", settings);
            SetReference(authenticationController, "networkTransport", networkTransport);

            RectTransform safeArea = FindInScene(scene, "Safe Area")?.GetComponent<RectTransform>();
            if (safeArea == null) throw new InvalidOperationException("Gameplay HUD Safe Area is missing.");
            GameObject panel = CreateTurnPanel(safeArea, hudView.Skin, out Text turnLabel, out Text playerA, out Text playerB);
            SetReference(presenter, "session", session);
            SetReference(presenter, "root", panel);
            SetReference(presenter, "turnLabel", turnLabel);
            SetReference(presenter, "playerAScore", playerA);
            SetReference(presenter, "playerBScore", playerB);
            SetReference(presenter, "gameplayActionButton", hudView.ActionButton);
            SetReference(debug, "session", session);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("M12 ONLINE FOUNDATION BUILD COMPLETE | OfflineSingle default, LocalTwoPlayer development mode available.");
        }

        private static GameObject CreateTurnPanel(RectTransform parent, HudSkinData skin, out Text turn, out Text playerA, out Text playerB)
        {
            GameObject panel = new("M12 Turn Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f, -176f);
            rect.sizeDelta = new Vector2(340f, 104f);
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.025f, 0.11f, 0.17f, 0.9f);
            image.sprite = skin != null ? skin.RoundedPanel : null;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            turn = CreateText(panel.transform, "Active Turn", new Vector2(16f, -10f), new Vector2(308f, 30f),
                20, FontStyle.Bold, new Color(0.35f, 0.95f, 1f, 1f));
            playerA = CreateText(panel.transform, "Player A Score", new Vector2(16f, -44f), new Vector2(150f, 22f),
                14, FontStyle.Bold, Color.white);
            playerB = CreateText(panel.transform, "Player B Score", new Vector2(174f, -44f), new Vector2(150f, 22f),
                14, FontStyle.Bold, new Color(0.75f, 0.84f, 0.9f, 1f));
            panel.SetActive(false);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size,
            int fontSize, FontStyle style, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static TerrainSurfaceData[] CollectSurfaceData()
        {
            Dictionary<TerrainSurfaceType, TerrainSurfaceData> byType = new();
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
                if (surface.Data != null) byType[surface.SurfaceType] = surface.Data;
            List<TerrainSurfaceData> result = new();
            foreach (TerrainSurfaceType type in Enum.GetValues(typeof(TerrainSurfaceType)))
                if (byType.TryGetValue(type, out TerrainSurfaceData data)) result.Add(data);
            return result.ToArray();
        }

        private static T Require<T>() where T : Object
        {
            T value = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return value != null ? value : throw new InvalidOperationException($"Required {typeof(T).Name} is missing.");
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.name == objectName) return transform.gameObject;
            return null;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} is missing.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} is missing.");
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
