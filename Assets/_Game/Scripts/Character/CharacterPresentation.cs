using SwingPop.Gameplay.Club;
using UnityEngine;

namespace SwingPop.CharacterSystem
{
    [DisallowMultipleComponent]
    public sealed class CharacterPresentation : MonoBehaviour
    {
        [Header("Placeholder Rig")]
        [SerializeField] private CharacterVisualAdapter visualAdapter;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform bodyPivot;
        [SerializeField] private Transform headPivot;
        [SerializeField] private Transform leftArmPivot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform leftLegPivot;
        [SerializeField] private Transform rightLegPivot;
        [SerializeField] private Transform clubSocket;

        [Header("Replaceable Club Visuals")]
        [SerializeField] private GameObject driverVisual;
        [SerializeField] private GameObject putterVisual;

        private Vector3 visualRootBasePosition;
        private Quaternion bodyBaseRotation;
        private Quaternion headBaseRotation;
        private Quaternion leftArmBaseRotation;
        private Quaternion rightArmBaseRotation;
        private Quaternion leftLegBaseRotation;
        private Quaternion rightLegBaseRotation;
        private Quaternion clubSocketBaseRotation;

        public string CurrentClubVisual { get; private set; } = "None";
        public CharacterVisualAdapter VisualAdapter => visualAdapter;

        private void Awake()
        {
            visualAdapter ??= GetComponent<CharacterVisualAdapter>();
            if (visualAdapter != null)
            {
                visualRoot ??= visualAdapter.VisualRoot;
                clubSocket ??= visualAdapter.ClubSocket;
                visualAdapter.ApplyProfileToVisualRoot();
            }
            CaptureNeutralPose();
        }

        public void CaptureNeutralPose()
        {
            visualRootBasePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            bodyBaseRotation = ReadRotation(bodyPivot);
            headBaseRotation = ReadRotation(headPivot);
            leftArmBaseRotation = ReadRotation(leftArmPivot);
            rightArmBaseRotation = ReadRotation(rightArmPivot);
            leftLegBaseRotation = ReadRotation(leftLegPivot);
            rightLegBaseRotation = ReadRotation(rightLegPivot);
            clubSocketBaseRotation = ReadRotation(clubSocket);
        }

        public void ConfigureClubSocket(Vector3 localPosition)
        {
            if (clubSocket != null)
            {
                clubSocket.localPosition = localPosition;
            }
        }

        public void SetClub(ClubType clubType)
        {
            bool putter = CharacterFlowResolver.ResolveClubVisual(clubType) == CharacterClubVisualType.Putter;
            if (driverVisual != null)
            {
                driverVisual.SetActive(!putter);
            }
            if (putterVisual != null)
            {
                putterVisual.SetActive(putter);
            }
            CurrentClubVisual = putter ? "Putter" : "Driver";
        }

        public void ApplyProceduralPose(CharacterState state, float normalizedTime, float elapsedTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime));
            Vector3 bodyEuler = Vector3.zero;
            Vector3 headEuler = Vector3.zero;
            Vector3 leftArmEuler = Vector3.zero;
            Vector3 rightArmEuler = Vector3.zero;
            Vector3 leftLegEuler = Vector3.zero;
            Vector3 rightLegEuler = Vector3.zero;
            Vector3 socketEuler = Vector3.zero;
            float bob = 0f;

            switch (state)
            {
                case CharacterState.Idle:
                    bob = Mathf.Sin(elapsedTime * 2f) * 0.018f;
                    leftArmEuler = new Vector3(4f, 0f, -5f);
                    rightArmEuler = new Vector3(4f, 0f, 5f);
                    break;
                case CharacterState.Address:
                    bodyEuler = new Vector3(10f, 0f, 0f);
                    headEuler = new Vector3(-7f, 0f, 0f);
                    leftArmEuler = new Vector3(34f, 0f, -24f);
                    rightArmEuler = new Vector3(34f, 0f, 24f);
                    socketEuler = new Vector3(12f, 0f, 0f);
                    break;
                case CharacterState.PuttAddress:
                    bodyEuler = new Vector3(15f, 0f, 0f);
                    headEuler = new Vector3(-10f, 0f, 0f);
                    leftArmEuler = new Vector3(42f, 0f, -17f);
                    rightArmEuler = new Vector3(42f, 0f, 17f);
                    socketEuler = new Vector3(22f, 0f, 0f);
                    break;
                case CharacterState.BackSwing:
                    bodyEuler = new Vector3(8f, Mathf.Lerp(0f, -48f, t), 0f);
                    headEuler = new Vector3(-6f, Mathf.Lerp(0f, 18f, t), 0f);
                    leftArmEuler = new Vector3(Mathf.Lerp(34f, -62f, t), 0f, Mathf.Lerp(-24f, -48f, t));
                    rightArmEuler = new Vector3(Mathf.Lerp(34f, -62f, t), 0f, Mathf.Lerp(24f, 48f, t));
                    socketEuler = new Vector3(Mathf.Lerp(12f, -78f, t), 0f, 0f);
                    break;
                case CharacterState.PuttBackSwing:
                    bodyEuler = new Vector3(15f, Mathf.Lerp(0f, -8f, t), 0f);
                    headEuler = new Vector3(-10f, 0f, 0f);
                    leftArmEuler = new Vector3(Mathf.Lerp(42f, 32f, t), 0f, -17f);
                    rightArmEuler = new Vector3(Mathf.Lerp(42f, 32f, t), 0f, 17f);
                    socketEuler = new Vector3(Mathf.Lerp(22f, -13f, t), 0f, 0f);
                    break;
                case CharacterState.Swing:
                    bodyEuler = new Vector3(7f, Mathf.Lerp(-48f, 52f, t), 0f);
                    headEuler = new Vector3(-4f, Mathf.Lerp(18f, -16f, t), 0f);
                    leftArmEuler = new Vector3(Mathf.Lerp(-62f, 18f, t), 0f, Mathf.Lerp(-48f, -12f, t));
                    rightArmEuler = new Vector3(Mathf.Lerp(-62f, 18f, t), 0f, Mathf.Lerp(48f, 12f, t));
                    socketEuler = new Vector3(Mathf.Lerp(-78f, 68f, t), 0f, 0f);
                    break;
                case CharacterState.PuttSwing:
                    bodyEuler = new Vector3(15f, Mathf.Lerp(-8f, 9f, t), 0f);
                    headEuler = new Vector3(-10f, 0f, 0f);
                    leftArmEuler = new Vector3(Mathf.Lerp(32f, 52f, t), 0f, -17f);
                    rightArmEuler = new Vector3(Mathf.Lerp(32f, 52f, t), 0f, 17f);
                    socketEuler = new Vector3(Mathf.Lerp(-13f, 26f, t), 0f, 0f);
                    break;
                case CharacterState.FollowThrough:
                    bodyEuler = new Vector3(4f, 52f, -5f);
                    headEuler = new Vector3(-4f, -18f, 0f);
                    leftArmEuler = new Vector3(-12f, 0f, -58f);
                    rightArmEuler = new Vector3(-12f, 0f, 58f);
                    socketEuler = new Vector3(76f, 0f, 0f);
                    break;
                case CharacterState.PuttFollowThrough:
                    bodyEuler = new Vector3(14f, 9f, 0f);
                    headEuler = new Vector3(-8f, -5f, 0f);
                    leftArmEuler = new Vector3(52f, 0f, -17f);
                    rightArmEuler = new Vector3(52f, 0f, 17f);
                    socketEuler = new Vector3(28f, 0f, 0f);
                    break;
                case CharacterState.WatchBall:
                    bodyEuler = new Vector3(2f, 18f, 0f);
                    headEuler = new Vector3(-8f, 22f, 0f);
                    leftArmEuler = new Vector3(10f, 0f, -8f);
                    rightArmEuler = new Vector3(10f, 0f, 8f);
                    break;
                case CharacterState.Sad:
                    bodyEuler = new Vector3(18f, 0f, 0f);
                    headEuler = new Vector3(18f, 0f, 0f);
                    leftArmEuler = new Vector3(18f, 0f, -5f);
                    rightArmEuler = new Vector3(18f, 0f, 5f);
                    break;
                case CharacterState.Happy:
                case CharacterState.BirdieCelebration:
                case CharacterState.EagleCelebration:
                case CharacterState.HoleInOneCelebration:
                    float energy = state == CharacterState.HoleInOneCelebration ? 1f
                        : state == CharacterState.EagleCelebration ? 0.85f
                        : state == CharacterState.BirdieCelebration ? 0.7f
                        : 0.55f;
                    bob = Mathf.Abs(Mathf.Sin(elapsedTime * (5f + energy * 2f))) * 0.12f * energy;
                    bodyEuler = new Vector3(0f, Mathf.Sin(elapsedTime * 2.2f) * 18f * energy, 0f);
                    leftArmEuler = new Vector3(-125f * energy, 0f, -48f);
                    rightArmEuler = new Vector3(-125f * energy, 0f, 48f);
                    leftLegEuler = new Vector3(Mathf.Sin(elapsedTime * 4f) * 8f * energy, 0f, 0f);
                    rightLegEuler = -leftLegEuler;
                    break;
            }

            if (visualRoot != null)
            {
                visualRoot.localPosition = visualRootBasePosition + Vector3.up * bob;
            }
            ApplyRotation(bodyPivot, bodyBaseRotation, bodyEuler);
            ApplyRotation(headPivot, headBaseRotation, headEuler);
            ApplyRotation(leftArmPivot, leftArmBaseRotation, leftArmEuler);
            ApplyRotation(rightArmPivot, rightArmBaseRotation, rightArmEuler);
            ApplyRotation(leftLegPivot, leftLegBaseRotation, leftLegEuler);
            ApplyRotation(rightLegPivot, rightLegBaseRotation, rightLegEuler);
            ApplyRotation(clubSocket, clubSocketBaseRotation, socketEuler);
        }

        private static Quaternion ReadRotation(Transform target)
        {
            return target != null ? target.localRotation : Quaternion.identity;
        }

        private static void ApplyRotation(Transform target, Quaternion neutral, Vector3 euler)
        {
            if (target != null)
            {
                target.localRotation = neutral * Quaternion.Euler(euler);
            }
        }
    }
}
