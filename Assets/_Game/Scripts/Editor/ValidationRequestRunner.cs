using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    /// <summary>
    /// Editor-only one-shot bridge used by automated local validation when the Game view owns keyboard focus.
    /// Create Temp/SwingPopValidation.request with M1 through M9. The request enters Play Mode when needed.
    /// </summary>
    [InitializeOnLoad]
    public static class ValidationRequestRunner
    {
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopValidation.request"));

        static ValidationRequestRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += TryRunRequest;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += TryRunRequest;
            }
        }

        private static void TryRunRequest()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = true;
                }
                return;
            }

            string milestone = File.ReadAllText(RequestPath).Trim().ToUpperInvariant();
            File.Delete(RequestPath);
            ClearConsole();
            switch (milestone)
            {
                case "M1":
                    M1ValidationTools.RunPlayPhysicsValidation();
                    break;
                case "M2":
                    M1ValidationTools.RunM2ShotIntegrationValidation();
                    break;
                case "M3":
                    M3ValidationTools.RunSpinComparisonValidation();
                    break;
                case "M4":
                    M4ValidationTools.RunWindHazardValidation();
                    break;
                case "M5":
                    M5ValidationTools.RunHoleFlowValidation();
                    break;
                case "M6":
                    M6ValidationTools.RunCameraFlowValidation();
                    break;
                case "M7":
                    M7ValidationTools.RunCharacterFlowValidation();
                    break;
                case "M8":
                    M8ValidationTools.RunHudFlowValidation();
                    break;
                case "M9":
                    M9ValidationTools.RunShotFeelValidation();
                    break;
                default:
                    Debug.LogError($"Unknown SwingPop validation request '{milestone}'.");
                    break;
            }
        }

        private static void ClearConsole()
        {
            System.Type logEntries = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            logEntries?.GetMethod(
                    "Clear",
                    System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(null, null);
        }
    }
}
