using SwingPop.Data;
using UnityEngine;

namespace SwingPop.CharacterSystem
{
    /// <summary>
    /// Character gameplay과 교체 가능한 visual prefab 사이의 reference seam입니다.
    /// Gameplay code는 mesh hierarchy나 Humanoid bone 이름을 탐색하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisualAdapter : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform gameplayRoot;
        [SerializeField] private Transform visualRoot;

        [Header("Replaceable Rig")]
        [SerializeField] private Animator animator;
        [Tooltip("Humanoid Avatar를 명시적으로 연결합니다. 비어 있으면 Animator.avatar를 사용합니다.")]
        [SerializeField] private Avatar avatar;
        [SerializeField] private Transform clubSocket;
        [Tooltip("기존/단일 손 attachment 호환 reference입니다. Bone 이름을 검색하지 않습니다.")]
        [SerializeField] private Transform handSocket;
        [Tooltip("선택 사항입니다. 향후 two-hand constraint/IK seam이며 이번 pass에서는 IK를 실행하지 않습니다.")]
        [SerializeField] private Transform leftHandSocket;
        [Tooltip("선택 사항입니다. 비어 있으면 HandSocket을 오른손 socket으로 사용합니다.")]
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform impactAnchor;
        [SerializeField] private Transform headLookTarget;

        [Header("Composition")]
        [SerializeField] private CharacterVisualProfile profile;

        private bool baselineCaptured;
        private Vector3 visualRootBasePosition;
        private Vector3 visualRootBaseScale;

        public Transform GameplayRoot => gameplayRoot != null ? gameplayRoot : transform;
        public Transform VisualRoot => visualRoot;
        public Animator Animator => animator;
        public Avatar Avatar => avatar != null ? avatar : animator != null ? animator.avatar : null;
        public Transform ClubSocket => clubSocket;
        public Transform HandSocket => handSocket;
        public Transform LeftHandSocket => leftHandSocket;
        public Transform RightHandSocket => rightHandSocket != null ? rightHandSocket : handSocket;
        public Transform ImpactAnchor => impactAnchor;
        public Transform HeadLookTarget => headLookTarget;
        public CharacterVisualProfile Profile => profile;
        public float VisualHeight => profile != null ? profile.VisualHeight : CalculateRendererHeight();
        public Bounds LocalBounds => profile != null ? profile.LocalBounds : CalculateLocalRendererBounds();
        public Vector3 PresentationOffset => profile != null ? profile.VisualRootOffset : Vector3.zero;
        public Vector3 CameraFramingPoint => (VisualRoot != null ? VisualRoot : GameplayRoot).TransformPoint(
            profile != null ? profile.CameraFramingOffset : LocalBounds.center);
        public bool HasRequiredReferences => VisualRoot != null
            && ClubSocket != null
            && ImpactAnchor != null
            && HeadLookTarget != null
            && profile != null;
        public bool HasValidHumanoidAvatar => Avatar != null && Avatar.isValid && Avatar.isHuman;

        private void Awake()
        {
            ApplyProfileToVisualRoot();
        }

        /// <summary>
        /// Gameplay root는 그대로 두고 visual scale/pivot만 한 번 정규화합니다.
        /// 반복 호출해도 offset이나 scale이 누적되지 않습니다.
        /// </summary>
        public void ApplyProfileToVisualRoot()
        {
            if (visualRoot == null || profile == null)
            {
                return;
            }

            if (!baselineCaptured)
            {
                visualRootBasePosition = visualRoot.localPosition;
                visualRootBaseScale = visualRoot.localScale;
                baselineCaptured = true;
            }

            visualRoot.localPosition = visualRootBasePosition + profile.VisualRootOffset;
            visualRoot.localScale = visualRootBaseScale * profile.CharacterScale;
        }

        public void Configure(
            Transform newGameplayRoot,
            Transform newVisualRoot,
            Animator newAnimator,
            Transform newClubSocket,
            Transform newHandSocket,
            Transform newImpactAnchor,
            Transform newHeadLookTarget,
            CharacterVisualProfile newProfile)
        {
            Configure(
                newGameplayRoot,
                newVisualRoot,
                newAnimator,
                newAnimator != null ? newAnimator.avatar : null,
                newClubSocket,
                newHandSocket,
                null,
                newHandSocket,
                newImpactAnchor,
                newHeadLookTarget,
                newProfile);
        }

        public void Configure(
            Transform newGameplayRoot,
            Transform newVisualRoot,
            Animator newAnimator,
            Avatar newAvatar,
            Transform newClubSocket,
            Transform newHandSocket,
            Transform newLeftHandSocket,
            Transform newRightHandSocket,
            Transform newImpactAnchor,
            Transform newHeadLookTarget,
            CharacterVisualProfile newProfile)
        {
            gameplayRoot = newGameplayRoot;
            visualRoot = newVisualRoot;
            animator = newAnimator;
            avatar = newAvatar;
            clubSocket = newClubSocket;
            handSocket = newHandSocket;
            leftHandSocket = newLeftHandSocket;
            rightHandSocket = newRightHandSocket;
            impactAnchor = newImpactAnchor;
            headLookTarget = newHeadLookTarget;
            profile = newProfile;
            baselineCaptured = false;
        }

        private float CalculateRendererHeight()
        {
            Bounds bounds = CalculateLocalRendererBounds();
            return Mathf.Max(0.1f, bounds.size.y);
        }

        private Bounds CalculateLocalRendererBounds()
        {
            if (visualRoot == null)
            {
                return new Bounds(Vector3.up, new Vector3(1f, 2f, 1f));
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.up, new Vector3(1f, 2f, 1f));
            }

            Matrix4x4 worldToLocal = visualRoot.worldToLocalMatrix;
            Bounds result = TransformBounds(worldToLocal, renderers[0].bounds);
            for (int index = 1; index < renderers.Length; index++)
            {
                Bounds local = TransformBounds(worldToLocal, renderers[index].bounds);
                result.Encapsulate(local.min);
                result.Encapsulate(local.max);
            }
            return result;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
            Vector3 extents = bounds.extents;
            Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
            Vector3 localExtents = new(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, localExtents * 2f);
        }
    }
}
