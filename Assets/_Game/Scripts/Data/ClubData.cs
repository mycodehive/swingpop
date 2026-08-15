using SwingPop.Gameplay.Club;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "Club", menuName = "SwingPop/Club")]
    public sealed class ClubData : ScriptableObject
    {
        [SerializeField] private string displayName = "Club";
        [SerializeField] private ClubType clubType = ClubType.Driver;

        [Header("Shot")]
        [SerializeField, Min(0.1f)] private float basePower = 18f;
        [SerializeField, Range(0f, 89f)] private float loftDegrees = 35f;
        [SerializeField, Min(0f)] private float carryModifier = 1f;
        [SerializeField, Min(0.01f)] private float rollModifier = 1f;

        public string DisplayName => displayName;
        public ClubType ClubType => clubType;
        public float BasePower => basePower;
        public float LoftDegrees => loftDegrees;
        public float CarryModifier => carryModifier;
        public float RollModifier => rollModifier;
        public bool IsPutter => clubType == ClubType.Putter;
    }
}
