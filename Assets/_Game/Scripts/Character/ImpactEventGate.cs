using UnityEngine;

namespace SwingPop.CharacterSystem
{
    public sealed class ImpactEventGate
    {
        private float triggerTime;
        private float elapsed;

        public bool HasFired { get; private set; }

        public void Begin(float duration, float normalizedImpactTime)
        {
            triggerTime = Mathf.Max(0.001f, duration) * Mathf.Clamp01(normalizedImpactTime);
            elapsed = 0f;
            HasFired = false;
        }

        public bool Tick(float deltaTime)
        {
            if (HasFired)
            {
                return false;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            return elapsed >= triggerTime && TryFire();
        }

        public bool TryFire()
        {
            if (HasFired)
            {
                return false;
            }

            HasFired = true;
            return true;
        }
    }
}
