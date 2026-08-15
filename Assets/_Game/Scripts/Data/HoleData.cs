using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "Hole", menuName = "SwingPop/Hole")]
    public sealed class HoleData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int holeNumber = 1;
        [SerializeField, Min(1)] private int par = 4;
        [SerializeField] private string displayName = "Sky Island Opening";

        [Header("Layout")]
        [SerializeField] private Vector3 teePosition = new(0f, 0.15f, 0f);
        [SerializeField] private Vector3 cupPosition = new(0f, 0f, 78f);
        [SerializeField, Min(0f)] private float holeLength = 78f;

        [Header("Cup Capture")]
        [SerializeField, Min(0.05f)] private float captureRadius = 0.55f;
        [SerializeField, Min(0f)] private float maximumCaptureSpeed = 2.4f;
        [SerializeField, Min(0f)] private float maximumHeightDifference = 0.45f;
        [SerializeField, Min(0.05f)] private float assistRadius = 1.35f;
        [SerializeField, Min(0f)] private float assistMaximumSpeed = 4.5f;
        [SerializeField, Min(0f)] private float assistAcceleration = 2.2f;

        public int HoleNumber => holeNumber;
        public int Par => par;
        public string DisplayName => displayName;
        public Vector3 TeePosition => teePosition;
        public Vector3 CupPosition => cupPosition;
        public float HoleLength => holeLength;
        public float CaptureRadius => captureRadius;
        public float MaximumCaptureSpeed => maximumCaptureSpeed;
        public float MaximumHeightDifference => maximumHeightDifference;
        public float AssistRadius => Mathf.Max(assistRadius, captureRadius);
        public float AssistMaximumSpeed => assistMaximumSpeed;
        public float AssistAcceleration => assistAcceleration;
    }
}
