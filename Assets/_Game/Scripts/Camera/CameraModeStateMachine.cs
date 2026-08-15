using UnityEngine;

namespace SwingPop.CameraSystem
{
    public sealed class CameraModeStateMachine
    {
        private float elapsed;
        private float duration;

        public CameraMode Current { get; private set; } = CameraMode.HoleIntro;
        public CameraMode Previous { get; private set; } = CameraMode.HoleIntro;
        public bool IsTransitioning => elapsed < duration;
        public float TransitionProgress => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        public bool Request(CameraMode next, float transitionDuration)
        {
            if (Current == next)
            {
                return false;
            }

            Previous = Current;
            Current = next;
            elapsed = 0f;
            duration = Mathf.Max(0f, transitionDuration);
            return true;
        }

        public void Reset(CameraMode mode)
        {
            Current = mode;
            Previous = mode;
            elapsed = 0f;
            duration = 0f;
        }

        public void Tick(float deltaTime)
        {
            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaTime));
        }
    }
}
