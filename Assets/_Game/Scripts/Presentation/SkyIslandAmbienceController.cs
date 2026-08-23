using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SkyIslandAmbienceController : MonoBehaviour
    {
        [SerializeField] private SkyIslandEnvironmentTuningData tuning;
        [SerializeField] private AudioSource ambientSource;

        public bool HasTuning => tuning != null;
        public bool HasAmbientSource => ambientSource != null;

        private void Awake()
        {
            ambientSource ??= GetComponent<AudioSource>();
            if (tuning == null || ambientSource == null)
            {
                return;
            }

            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = tuning.AmbientVolume;
            ambientSource.clip = tuning.AmbientLoop;
            if (ambientSource.clip != null)
            {
                ambientSource.Play();
            }
        }
    }
}
