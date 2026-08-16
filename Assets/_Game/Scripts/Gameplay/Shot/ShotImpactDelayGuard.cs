using UnityEngine;

namespace SwingPop.Gameplay.Shot
{
    public sealed class ShotImpactDelayGuard
    {
        private float delay;
        private float elapsed;

        public bool HasExpired { get; private set; }

        public void Begin(float delaySeconds)
        {
            delay = Mathf.Max(0.01f, delaySeconds);
            elapsed = 0f;
            HasExpired = false;
        }

        public bool Tick(float deltaTime)
        {
            if (HasExpired)
            {
                return false;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed < delay)
            {
                return false;
            }

            HasExpired = true;
            return true;
        }
    }
}
