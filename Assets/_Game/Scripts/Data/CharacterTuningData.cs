using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "CharacterTuning", menuName = "SwingPop/Character Tuning")]
    public sealed class CharacterTuningData : ScriptableObject
    {
        [Header("Address Placement")]
        [Tooltip("Aim direction 기준 오른쪽(+)/왼쪽(-) 배치 거리입니다.")]
        [SerializeField] private float addressLateralOffset = -0.9f;
        [Tooltip("Aim direction 기준 앞(+)/뒤(-) 배치 거리입니다.")]
        [SerializeField] private float addressBackwardOffset = -0.12f;
        [SerializeField] private float addressHeightOffset = -0.15f;
        [SerializeField, Min(0f)] private float positionSharpness = 10f;
        [SerializeField, Min(0f)] private float rotationSharpness = 12f;

        [Header("Driver Timeline")]
        [SerializeField, Min(0.01f)] private float backSwingDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float swingDuration = 0.34f;
        [SerializeField, Range(0.05f, 0.95f)] private float swingImpactNormalizedTime = 0.58f;
        [SerializeField, Min(0.01f)] private float followThroughDuration = 0.72f;

        [Header("Putter Timeline")]
        [SerializeField, Min(0.01f)] private float puttBackSwingDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float puttSwingDuration = 0.24f;
        [SerializeField, Range(0.05f, 0.95f)] private float puttImpactNormalizedTime = 0.5f;
        [SerializeField, Min(0.01f)] private float puttFollowThroughDuration = 0.42f;

        [Header("Safety")]
        [Tooltip("Animation impact signal이 누락될 때 Ball launch를 보장하는 최대 대기 시간입니다.")]
        [SerializeField, Min(0.05f)] private float fallbackImpactDelay = 0.65f;

        [Header("Attachment")]
        [SerializeField] private Vector3 clubSocketOffset = new(0.45f, 1.15f, 0.25f);

        public float AddressLateralOffset => addressLateralOffset;
        public float AddressBackwardOffset => addressBackwardOffset;
        public float AddressHeightOffset => addressHeightOffset;
        public float PositionSharpness => positionSharpness;
        public float RotationSharpness => rotationSharpness;
        public float BackSwingDuration => backSwingDuration;
        public float SwingDuration => swingDuration;
        public float SwingImpactNormalizedTime => swingImpactNormalizedTime;
        public float FollowThroughDuration => followThroughDuration;
        public float PuttBackSwingDuration => puttBackSwingDuration;
        public float PuttSwingDuration => puttSwingDuration;
        public float PuttImpactNormalizedTime => puttImpactNormalizedTime;
        public float PuttFollowThroughDuration => puttFollowThroughDuration;
        public float FallbackImpactDelay => fallbackImpactDelay;
        public Vector3 ClubSocketOffset => clubSocketOffset;
    }
}
