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
        private bool impactArmed;
        private CharacterAnimationMode animationMode;
        private CharacterImpactSource lastImpactSource;

        public event Action<CharacterState, CharacterState> StateChanged;
        public event Action ImpactReached;
        public event Action<CharacterState> StateFinished;

        public CharacterState State => state;
        public float StateElapsed => stateElapsed;
        public bool ImpactEventFired => impactGate.HasFired;
        public CharacterAnimationMode AnimationMode => animationMode;
        public CharacterImpactSource LastImpactSource => lastImpactSource;
        public bool UsesAnimator => animationMode == CharacterAnimationMode.HumanoidAnimator;

        private void Awake()
        {
            presentation ??= GetComponent<CharacterPresentation>();
            animator ??= presentation != null && presentation.VisualAdapter != null
                ? presentation.VisualAdapter.Animator
                : null;
            animationMode = ResolveAnimationMode(animator);
        }

        private void Update()
        {
            stateElapsed += Time.deltaTime;
            if (impactArmed
                && (state is CharacterState.Swing or CharacterState.PuttSwing)
                && impactGate.Tick(Time.deltaTime))
            {
                impactArmed = false;
                lastImpactSource = CharacterImpactSource.NormalizedFallback;
                ImpactReached?.Invoke();
            }

            float normalized = stateDuration > 0f ? Mathf.Clamp01(stateElapsed / stateDuration) : 0f;
            if (!UsesAnimator)
            {
                presentation?.ApplyProceduralPose(state, normalized, stateElapsed);
            }
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
            lastImpactSource = CharacterImpactSource.None;
            impactGate.Begin(duration, impactTime);
            impactArmed = true;
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
            if (impactArmed && impactGate.TryFire())
            {
                impactArmed = false;
                lastImpactSource = CharacterImpactSource.AnimationEvent;
                ImpactReached?.Invoke();
            }
        }

        /// <summary>Editor preview 전용. Gameplay command나 Ball launch를 요청하지 않습니다.</summary>
        public void PreviewState(CharacterState previewState)
        {
            impactArmed = false;
            lastImpactSource = CharacterImpactSource.None;
            Play(previewState, ResolvePreviewDuration(previewState));
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

            int stateHash = CharacterAnimatorContract.GetStateHash(next);
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, 0.08f, 0, 0f);
            }
        }

        public static CharacterAnimationMode ResolveAnimationMode(Animator candidate)
        {
            if (candidate == null || candidate.runtimeAnimatorController == null)
            {
                return CharacterAnimationMode.ProceduralFallback;
            }

            Avatar candidateAvatar = candidate.avatar;
            if (candidateAvatar == null || !candidateAvatar.isValid || !candidateAvatar.isHuman)
            {
                Debug.LogWarning(
                    "Character Animator Controller is assigned, but its Avatar is missing, invalid, or not Humanoid. " +
                    "SwingPop will use the procedural fallback until a valid Humanoid Avatar is assigned.",
                    candidate);
                return CharacterAnimationMode.ProceduralFallback;
            }

            return CharacterAnimationMode.HumanoidAnimator;
        }

        private float ResolvePreviewDuration(CharacterState previewState)
        {
            if (tuning == null)
            {
                return 0f;
            }

            return previewState switch
            {
                CharacterState.BackSwing => tuning.BackSwingDuration,
                CharacterState.Swing => tuning.SwingDuration,
                CharacterState.FollowThrough => tuning.FollowThroughDuration,
                CharacterState.PuttBackSwing => tuning.PuttBackSwingDuration,
                CharacterState.PuttSwing => tuning.PuttSwingDuration,
                CharacterState.PuttFollowThrough => tuning.PuttFollowThroughDuration,
                _ => 0f
            };
        }
    }
}
