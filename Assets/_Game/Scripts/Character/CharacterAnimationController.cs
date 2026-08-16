using System;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.CharacterSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterPresentation))]
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CharacterPresentation presentation;
        [SerializeField] private CharacterTuningData tuning;
        [Tooltip("최종 Animator Controller가 연결되면 이 wrapper가 동일 상태를 CrossFade합니다. Placeholder는 procedural pose를 사용합니다.")]
        [SerializeField] private Animator animator;

        private readonly ImpactEventGate impactGate = new();
        private CharacterState state = CharacterState.Idle;
        private float stateElapsed;
        private float stateDuration;
        private bool completionRaised;

        public event Action<CharacterState, CharacterState> StateChanged;
        public event Action ImpactReached;
        public event Action<CharacterState> StateFinished;

        public CharacterState State => state;
        public float StateElapsed => stateElapsed;
        public bool ImpactEventFired => impactGate.HasFired;
        public bool UsesAnimator => animator != null && animator.runtimeAnimatorController != null;

        private void Awake()
        {
            presentation ??= GetComponent<CharacterPresentation>();
        }

        private void Update()
        {
            stateElapsed += Time.deltaTime;
            if ((state is CharacterState.Swing or CharacterState.PuttSwing) && impactGate.Tick(Time.deltaTime))
            {
                ImpactReached?.Invoke();
            }

            float normalized = stateDuration > 0f ? Mathf.Clamp01(stateElapsed / stateDuration) : 0f;
            presentation?.ApplyProceduralPose(state, normalized, stateElapsed);
            if (!completionRaised && stateDuration > 0f && stateElapsed >= stateDuration)
            {
                completionRaised = true;
                StateFinished?.Invoke(state);
            }
        }

        public void PlayAddress(bool isPutter)
        {
            Play(isPutter ? CharacterState.PuttAddress : CharacterState.Address, 0f);
        }

        public void PlayBackSwing(bool isPutter)
        {
            Play(
                isPutter ? CharacterState.PuttBackSwing : CharacterState.BackSwing,
                isPutter ? tuning.PuttBackSwingDuration : tuning.BackSwingDuration);
        }

        public void PlaySwing(bool isPutter)
        {
            float duration = isPutter ? tuning.PuttSwingDuration : tuning.SwingDuration;
            float impactTime = isPutter ? tuning.PuttImpactNormalizedTime : tuning.SwingImpactNormalizedTime;
            Play(isPutter ? CharacterState.PuttSwing : CharacterState.Swing, duration);
            impactGate.Begin(duration, impactTime);
        }

        public void PlayFollowThrough(bool isPutter)
        {
            Play(
                isPutter ? CharacterState.PuttFollowThrough : CharacterState.FollowThrough,
                isPutter ? tuning.PuttFollowThroughDuration : tuning.FollowThroughDuration);
        }

        public void PlayWatchBall()
        {
            Play(CharacterState.WatchBall, 0f);
        }

        public void PlayCelebration(CharacterState celebration)
        {
            Play(celebration, 0f);
        }

        /// <summary>Future Animation Clips can call this method from a single Animation Event.</summary>
        public void NotifyImpactAnimationEvent()
        {
            if (impactGate.TryFire())
            {
                ImpactReached?.Invoke();
            }
        }

        private void Play(CharacterState next, float duration)
        {
            CharacterState previous = state;
            state = next;
            stateElapsed = 0f;
            stateDuration = Mathf.Max(0f, duration);
            completionRaised = false;
            TryCrossFadeAnimator(next);
            if (previous != next)
            {
                StateChanged?.Invoke(previous, next);
            }
        }

        private void TryCrossFadeAnimator(CharacterState next)
        {
            if (!UsesAnimator)
            {
                return;
            }

            int stateHash = Animator.StringToHash(next.ToString());
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, 0.08f, 0, 0f);
            }
        }
    }
}
