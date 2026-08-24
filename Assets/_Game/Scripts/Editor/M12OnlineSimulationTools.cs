using SwingPop.Online;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class M12OnlineSimulationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.M12.RunLocal2P";

        static M12OnlineSimulationTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("SwingPop/Online/Run Local 2P Simulation")]
        public static void RunLocalTwoPlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            if (session == null)
            {
                Debug.LogError("[M12][Match] MatchSessionController is missing. Run Build M12 Foundation first.");
                return;
            }
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 200);
            Debug.Log("[M12][Match] Local 2P simulation started with 200 ms one-way loopback latency.");
        }
    }
}
