using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class M8BuildRequestRunner
    {
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopBuild.request"));

        static M8BuildRequestRunner()
        {
            EditorApplication.delayCall += TryRun;
        }

        private static void TryRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath))
            {
                return;
            }

            string request = File.ReadAllText(RequestPath).Trim().ToUpperInvariant();
            File.Delete(RequestPath);
            if (request == "M8")
            {
                M8HudSceneBuilder.BuildGameplayHud();
            }
        }
    }
}
