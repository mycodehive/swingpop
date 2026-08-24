using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "MultiplayerDevelopmentSettings", menuName = "SwingPop/Online/Development Settings")]
    public sealed class MultiplayerDevelopmentSettings : ScriptableObject
    {
        [SerializeField] private MultiplayerDevelopmentMode mode = MultiplayerDevelopmentMode.OfflineSingle;
        [SerializeField, Range(0, 1000)] private int simulatedLatencyMs;
        [SerializeField] private bool verboseLogging;
        [SerializeField, Min(0.1f)] private float simulatedRemoteShotDelay = 1.2f;
        [SerializeField, Range(0.1f, 1f)] private float simulatedRemotePower = 0.62f;

        public MultiplayerDevelopmentMode Mode => mode;
        public int SimulatedLatencyMs => simulatedLatencyMs;
        public bool VerboseLogging => verboseLogging;
        public float SimulatedRemoteShotDelay => simulatedRemoteShotDelay;
        public float SimulatedRemotePower => simulatedRemotePower;

        public void ConfigureForDevelopment(
            MultiplayerDevelopmentMode developmentMode,
            int latencyMs,
            bool verbose,
            float remoteDelay = 1.2f,
            float remotePower = 0.62f)
        {
            mode = developmentMode;
            simulatedLatencyMs = Mathf.Clamp(latencyMs, 0, 1000);
            verboseLogging = verbose;
            simulatedRemoteShotDelay = Mathf.Max(0.1f, remoteDelay);
            simulatedRemotePower = Mathf.Clamp(remotePower, 0.1f, 1f);
        }
    }
}
