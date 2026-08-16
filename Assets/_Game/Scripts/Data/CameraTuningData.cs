using SwingPop.CameraSystem;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "CameraTuning", menuName = "SwingPop/Camera Tuning")]
    public sealed class CameraTuningData : ScriptableObject
    {
        [Header("Hole Intro")]
        [SerializeField, Min(0f)] private float holeIntroDuration = 2.8f;
        [SerializeField] private Vector3 introTeeOffset = new(-14f, 11f, -16f);
        [SerializeField] private Vector3 introCupOffset = new(11f, 8f, -12f);
        [SerializeField, Range(20f, 90f)] private float introFieldOfView = 58f;

        [Header("Address / Aim")]
        [SerializeField] private Vector3 addressOffset = new(6.5f, 3.8f, -8.5f);
        [SerializeField] private Vector3 addressLookOffset = new(0f, 0.55f, 7f);
        [SerializeField, Range(20f, 90f)] private float addressFieldOfView = 48f;
        [SerializeField, Min(0f)] private float addressHoldDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float aimYawResponse = 0.35f;

        [Header("Swing / Impact")]
        [SerializeField] private Vector3 swingOffset = new(5.5f, 3.2f, -7f);
        [SerializeField, Range(20f, 90f)] private float swingFieldOfView = 45f;
        [SerializeField, Range(0f, 20f)] private float impactFovKick = 6f;
        [SerializeField, Min(0.01f)] private float impactHoldDuration = 0.24f;
        [SerializeField, Min(0f)] private float normalImpactShake = 0.06f;
        [SerializeField, Min(0f)] private float perfectImpactShake = 0.13f;
        [SerializeField, Min(0f)] private float impactShakeFrequency = 34f;

        [Header("Ball Follow")]
        [SerializeField] private Vector3 followOffset = new(6.5f, 4.5f, -9.5f);
        [SerializeField, Min(0f)] private float followLookAheadSeconds = 0.28f;
        [SerializeField, Min(0f)] private float speedDistanceScale = 0.12f;
        [SerializeField, Min(0f)] private float maximumDistanceExtension = 7f;
        [SerializeField, Range(20f, 100f)] private float followFieldOfView = 58f;
        [SerializeField, Range(20f, 100f)] private float maximumFollowFieldOfView = 68f;
        [SerializeField, Min(0.01f)] private float fovSpeedReference = 34f;
        [SerializeField, Min(0f)] private float apexWideHeight = 9f;
        [SerializeField, Min(0f)] private float apexHeightExtension = 3f;

        [Header("Landing / Next Shot")]
        [SerializeField] private Vector3 landingOffset = new(6f, 3.2f, -8f);
        [SerializeField, Range(20f, 90f)] private float landingFieldOfView = 50f;
        [SerializeField] private Vector3 nextShotOffset = new(7f, 4.2f, -9f);
        [SerializeField, Min(0f)] private float nextShotHoldDuration = 0.55f;

        [Header("Putt / Hole Complete")]
        [Tooltip("Ball-Cup midpoint 기준의 기본 side / height / backward offset입니다.")]
        [SerializeField] private Vector3 puttOffset = new(4.2f, 2.25f, -5.4f);
        [SerializeField, Range(20f, 90f)] private float puttFieldOfView = 43f;
        [Tooltip("Putt 거리 1m당 카메라가 추가로 뒤로 물러나는 거리입니다.")]
        [SerializeField, Min(0f)] private float puttDistanceScale = 0.42f;
        [SerializeField, Min(0f)] private float puttMaximumDistanceExtension = 12f;
        [Tooltip("긴 Putt에서 Ball과 Cup을 함께 보기 위한 거리당 추가 높이입니다.")]
        [SerializeField, Min(0f)] private float puttHeightScale = 0.08f;
        [SerializeField, Min(0f)] private float puttFovPerMeter = 0.55f;
        [SerializeField, Range(20f, 100f)] private float puttMaximumFieldOfView = 60f;
        [SerializeField] private Vector3 holeCompleteOffset = new(4.5f, 2.6f, -4.5f);
        [SerializeField, Range(20f, 90f)] private float holeCompleteFieldOfView = 42f;
        [SerializeField, Min(0f)] private float holeCompleteHoldDuration = 1.1f;
        [SerializeField] private Vector3 resultOffset = new(8f, 5.5f, -8f);
        [SerializeField, Range(20f, 90f)] private float resultFieldOfView = 52f;
        [SerializeField, Min(0f)] private float holeCompleteShake = 0.04f;

        [Header("Blending")]
        [SerializeField, Min(0f)] private float defaultTransitionDuration = 0.45f;
        [SerializeField, Min(0f)] private float introTransitionDuration = 0.8f;
        [SerializeField, Min(0f)] private float impactTransitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float nextShotTransitionDuration = 0.55f;
        [SerializeField, Min(0f)] private float followPositionSharpness = 6f;
        [SerializeField, Min(0f)] private float followRotationSharpness = 9f;
        [SerializeField, Min(0f)] private float followFovSharpness = 7f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = 1;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.28f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.18f;
        [SerializeField, Min(0f)] private float minimumTargetDistance = 1.4f;

        public float HoleIntroDuration => holeIntroDuration;
        public Vector3 IntroTeeOffset => introTeeOffset;
        public Vector3 IntroCupOffset => introCupOffset;
        public float IntroFieldOfView => introFieldOfView;
        public Vector3 AddressOffset => addressOffset;
        public Vector3 AddressLookOffset => addressLookOffset;
        public float AddressFieldOfView => addressFieldOfView;
        public float AddressHoldDuration => addressHoldDuration;
        public float AimYawResponse => aimYawResponse;
        public Vector3 SwingOffset => swingOffset;
        public float SwingFieldOfView => swingFieldOfView;
        public float ImpactFovKick => impactFovKick;
        public float ImpactHoldDuration => impactHoldDuration;
        public float NormalImpactShake => normalImpactShake;
        public float PerfectImpactShake => perfectImpactShake;
        public float ImpactShakeFrequency => impactShakeFrequency;
        public Vector3 FollowOffset => followOffset;
        public float FollowLookAheadSeconds => followLookAheadSeconds;
        public float SpeedDistanceScale => speedDistanceScale;
        public float MaximumDistanceExtension => maximumDistanceExtension;
        public float FollowFieldOfView => followFieldOfView;
        public float MaximumFollowFieldOfView => maximumFollowFieldOfView;
        public float FovSpeedReference => fovSpeedReference;
        public float ApexWideHeight => apexWideHeight;
        public float ApexHeightExtension => apexHeightExtension;
        public Vector3 LandingOffset => landingOffset;
        public float LandingFieldOfView => landingFieldOfView;
        public Vector3 NextShotOffset => nextShotOffset;
        public float NextShotHoldDuration => nextShotHoldDuration;
        public Vector3 PuttOffset => puttOffset;
        public float PuttFieldOfView => puttFieldOfView;
        public float PuttDistanceScale => puttDistanceScale;
        public float PuttMaximumDistanceExtension => puttMaximumDistanceExtension;
        public float PuttHeightScale => puttHeightScale;
        public float PuttFovPerMeter => puttFovPerMeter;
        public float PuttMaximumFieldOfView => puttMaximumFieldOfView;
        public Vector3 HoleCompleteOffset => holeCompleteOffset;
        public float HoleCompleteFieldOfView => holeCompleteFieldOfView;
        public float HoleCompleteHoldDuration => holeCompleteHoldDuration;
        public Vector3 ResultOffset => resultOffset;
        public float ResultFieldOfView => resultFieldOfView;
        public float HoleCompleteShake => holeCompleteShake;
        public float FollowPositionSharpness => followPositionSharpness;
        public float FollowRotationSharpness => followRotationSharpness;
        public float FollowFovSharpness => followFovSharpness;
        public LayerMask CollisionLayers => collisionLayers;
        public float CollisionRadius => collisionRadius;
        public float CollisionPadding => collisionPadding;
        public float MinimumTargetDistance => minimumTargetDistance;

        public float GetTransitionDuration(CameraMode mode)
        {
            return mode switch
            {
                CameraMode.HoleIntro => introTransitionDuration,
                CameraMode.Impact => impactTransitionDuration,
                CameraMode.NextShot => nextShotTransitionDuration,
                _ => defaultTransitionDuration
            };
        }
    }
}
