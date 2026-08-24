using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "PuttResultCinematicTuning", menuName = "SwingPop/Putt Result Cinematic Tuning")]
    public sealed class PuttResultCinematicTuningData : ScriptableObject
    {
        [Header("Putt Address")]
        [SerializeField] private Vector3 puttAddressOffset = new(5.4f, 3.15f, -7.2f);
        [SerializeField, Range(0f, 1f)] private float puttAddressCupBias = 0.48f;
        [SerializeField, Min(0f)] private float puttAddressLookHeight = 0.65f;
        [SerializeField, Range(20f, 90f)] private float puttAddressFieldOfView = 47f;

        [Header("Putt Rolling")]
        [SerializeField] private Vector3 puttRollingOffset = new(4.8f, 2.55f, -6.2f);
        [SerializeField, Range(0f, 1f)] private float puttRollingCupBias = 0.58f;
        [SerializeField, Min(0f)] private float puttRollingLookHeight = 0.24f;
        [SerializeField, Range(20f, 90f)] private float puttRollingFieldOfView = 43f;

        [Header("Cup Approach")]
        [Tooltip("Hole-In 판정과 무관한 presentation-only 거리입니다.")]
        [SerializeField, Min(0.1f)] private float approachDistance = 1.6f;
        [SerializeField] private Vector3 approachOffset = new(4.5f, 2.2f, -5.6f);
        [SerializeField, Min(0f)] private float approachLookHeight = 0.18f;
        [SerializeField, Range(20f, 90f)] private float approachFieldOfView = 42f;

        [Header("Hole-In / Result Camera")]
        [SerializeField] private Vector3 holeInOffset = new(4.7f, 2.8f, -5.8f);
        [SerializeField, Min(0f)] private float holeInLookHeight = 0.28f;
        [SerializeField, Range(20f, 90f)] private float holeInFieldOfView = 43f;
        [SerializeField] private Vector3 resultOffset = new(7.2f, 4.45f, -7.4f);
        [SerializeField, Range(0f, 1f)] private float resultCupBias = 0.56f;
        [SerializeField, Min(0f)] private float resultLookHeight = 0.95f;
        [SerializeField, Range(20f, 90f)] private float resultFieldOfView = 49f;

        [Header("Cinematic Timing")]
        [SerializeField, Min(0f)] private float celebrationDelay = 0.34f;
        [SerializeField, Min(0f)] private float resultRevealDelay = 0.74f;
        [SerializeField, Min(0f)] private float resultFrameDuration = 0.28f;
        [SerializeField, Min(0f)] private float resultScoreDelay = 0.09f;
        [SerializeField, Min(0f)] private float resultDetailDelay = 0.2f;

        [Header("Hole-In VFX Timing")]
        [SerializeField, Min(0f)] private float holeRingDelay = 0.06f;
        [SerializeField, Min(0f)] private float holeCelebrationDelay = 0.22f;
        [SerializeField, Min(0.1f)] private float holeVfxIntensity = 1.2f;

        [Header("Result Layout")]
        [SerializeField] private Vector2 resultPanelOffset = new(335f, -8f);

        public Vector3 PuttAddressOffset => puttAddressOffset;
        public float PuttAddressCupBias => puttAddressCupBias;
        public float PuttAddressLookHeight => puttAddressLookHeight;
        public float PuttAddressFieldOfView => puttAddressFieldOfView;
        public Vector3 PuttRollingOffset => puttRollingOffset;
        public float PuttRollingCupBias => puttRollingCupBias;
        public float PuttRollingLookHeight => puttRollingLookHeight;
        public float PuttRollingFieldOfView => puttRollingFieldOfView;
        public float ApproachDistance => approachDistance;
        public Vector3 ApproachOffset => approachOffset;
        public float ApproachLookHeight => approachLookHeight;
        public float ApproachFieldOfView => approachFieldOfView;
        public Vector3 HoleInOffset => holeInOffset;
        public float HoleInLookHeight => holeInLookHeight;
        public float HoleInFieldOfView => holeInFieldOfView;
        public Vector3 ResultOffset => resultOffset;
        public float ResultCupBias => resultCupBias;
        public float ResultLookHeight => resultLookHeight;
        public float ResultFieldOfView => resultFieldOfView;
        public float CelebrationDelay => celebrationDelay;
        public float ResultRevealDelay => Mathf.Max(resultRevealDelay, celebrationDelay);
        public float ResultFrameDuration => resultFrameDuration;
        public float ResultScoreDelay => resultScoreDelay;
        public float ResultDetailDelay => Mathf.Max(resultDetailDelay, resultScoreDelay);
        public float HoleRingDelay => holeRingDelay;
        public float HoleCelebrationDelay => Mathf.Max(holeCelebrationDelay, holeRingDelay);
        public float HoleVfxIntensity => holeVfxIntensity;
        public Vector2 ResultPanelOffset => resultPanelOffset;
    }
}
