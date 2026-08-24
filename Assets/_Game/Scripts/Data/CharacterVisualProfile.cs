using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "CharacterVisualProfile", menuName = "SwingPop/Character Visual Profile")]
    public sealed class CharacterVisualProfile : ScriptableObject
    {
        [Header("Identity Metadata")]
        [SerializeField] private string displayName = "Placeholder Golfer";
        [Tooltip("HUD portrait를 나중에 연결하기 위한 presentation hook입니다. Gameplay에는 사용하지 않습니다.")]
        [SerializeField] private Sprite portraitReference;

        [Header("Visual Normalization")]
        [Tooltip("VisualRoot 기준 로컬 공간에서 측정한 기본 캐릭터 높이입니다.")]
        [SerializeField, Min(0.1f)] private float visualHeight = 3f;
        [SerializeField] private Vector3 localBoundsCenter = new(0f, 1.45f, 0f);
        [SerializeField] private Vector3 localBoundsSize = new(1.45f, 3f, 1.05f);
        [Tooltip("모델 pivot 차이를 보정하는 presentation-only 로컬 위치입니다.")]
        [SerializeField] private Vector3 presentationOffset = Vector3.zero;
        [Tooltip("Gameplay root scale과 분리된 VisualRoot 배율입니다.")]
        [SerializeField, Min(0.01f)] private float characterScale = 1f;
        [Tooltip("발바닥과 gameplay ground 사이의 presentation-only 높이 보정입니다.")]
        [SerializeField] private float groundOffset;

        [Header("Composition Metadata")]
        [Tooltip("Address에서 모델 pivot/stance 차이를 보정하는 VisualRoot 로컬 오프셋입니다.")]
        [SerializeField] private Vector3 addressOffset = Vector3.zero;
        [Tooltip("Camera가 캐릭터 bounds를 참조할 때 사용할 composition 기준점입니다.")]
        [SerializeField] private Vector3 cameraFramingOffset = new(0f, 1.45f, 0f);

        [Header("Attachment Metadata")]
        [Tooltip("최종 모델용 ClubSocket 권장 로컬 위치입니다. Bone 경로 대신 Inspector reference와 함께 사용합니다.")]
        [SerializeField] private Vector3 clubSocketOffset = new(0.45f, 1.15f, 0.25f);
        [SerializeField] private Vector3 impactAnchorOffset = new(0.62f, 0.16f, 0.58f);
        [SerializeField] private Vector3 headLookOffset = new(0f, 2.2f, 2f);

        public string DisplayName => displayName;
        public Sprite PortraitReference => portraitReference;
        public float VisualHeight => visualHeight;
        public Bounds LocalBounds => new(localBoundsCenter, localBoundsSize);
        public Vector3 VisualCenterOffset => localBoundsCenter;
        public Vector3 PresentationOffset => presentationOffset;
        public float CharacterScale => characterScale;
        public float GroundOffset => groundOffset;
        public Vector3 AddressOffset => addressOffset;
        public Vector3 CameraFramingOffset => cameraFramingOffset;
        public Vector3 ClubSocketOffset => clubSocketOffset;
        public Vector3 ImpactAnchorOffset => impactAnchorOffset;
        public Vector3 HeadLookOffset => headLookOffset;
        public Vector3 VisualRootOffset => presentationOffset + addressOffset + Vector3.up * groundOffset;
        public bool HasValidDimensions => visualHeight > 0f
            && localBoundsSize.x > 0f
            && localBoundsSize.y > 0f
            && localBoundsSize.z > 0f
            && characterScale > 0f;
    }
}
