using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "SkyIslandEnvironmentTuning", menuName = "SwingPop/Sky Island Environment Tuning")]
    public sealed class SkyIslandEnvironmentTuningData : ScriptableObject
    {
        [Header("Ambient Motion")]
        [SerializeField, Min(0f)] private float cloudDriftSpeed = 0.65f;
        [SerializeField, Min(10f)] private float cloudLoopDistance = 85f;
        [SerializeField] private float windmillDegreesPerSecond = 22f;

        [Header("Ambient Audio")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.12f;
        [SerializeField] private AudioClip ambientLoop;

        public float CloudDriftSpeed => cloudDriftSpeed;
        public float CloudLoopDistance => cloudLoopDistance;
        public float WindmillDegreesPerSecond => windmillDegreesPerSecond;
        public float AmbientVolume => ambientVolume;
        public AudioClip AmbientLoop => ambientLoop;
    }
}
