using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Presentation
{
    public sealed class SkyIslandEnvironmentMotion : MonoBehaviour
    {
        [SerializeField] private SkyIslandEnvironmentTuningData tuning;
        [SerializeField] private Transform windmillRotor;
        [SerializeField] private Transform[] driftingClouds;
        [SerializeField] private Transform waterHighlight;

        private Vector3[] cloudOrigins;
        private Vector3 waterHighlightOrigin;

        public bool HasTuning => tuning != null;
        public bool HasWindmillRotor => windmillRotor != null;
        public int DriftingCloudCount => driftingClouds?.Length ?? 0;
        public bool HasWaterHighlight => waterHighlight != null;

        private void Awake()
        {
            driftingClouds ??= System.Array.Empty<Transform>();
            cloudOrigins = new Vector3[driftingClouds.Length];
            for (int index = 0; index < driftingClouds.Length; index++)
            {
                if (driftingClouds[index] != null)
                {
                    cloudOrigins[index] = driftingClouds[index].position;
                }
            }
            if (waterHighlight != null)
            {
                waterHighlightOrigin = waterHighlight.localPosition;
            }
        }

        private void Update()
        {
            if (tuning == null)
            {
                return;
            }

            if (windmillRotor != null)
            {
                windmillRotor.Rotate(Vector3.forward, tuning.WindmillDegreesPerSecond * Time.deltaTime, Space.Self);
            }

            float loopDistance = Mathf.Max(10f, tuning.CloudLoopDistance);
            for (int index = 0; index < driftingClouds.Length; index++)
            {
                Transform cloud = driftingClouds[index];
                if (cloud == null)
                {
                    continue;
                }

                float stagger = loopDistance * index / Mathf.Max(1, driftingClouds.Length);
                float offset = Mathf.Repeat(Time.time * tuning.CloudDriftSpeed + stagger, loopDistance) - loopDistance * 0.5f;
                cloud.position = cloudOrigins[index] + Vector3.right * offset;
            }

            if (waterHighlight != null)
            {
                float offset = Mathf.Sin(Time.time * tuning.WaterHighlightSpeed) * tuning.WaterHighlightDistance;
                waterHighlight.localPosition = waterHighlightOrigin + Vector3.right * offset;
            }
        }
    }
}
