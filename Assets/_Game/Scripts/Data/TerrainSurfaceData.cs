using SwingPop.Gameplay.Course;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "TerrainSurface", menuName = "SwingPop/Terrain Surface")]
    public sealed class TerrainSurfaceData : ScriptableObject
    {
        [SerializeField] private TerrainSurfaceType surfaceType = TerrainSurfaceType.Fairway;
        [SerializeField, Min(0f)] private float powerModifier = 1f;
        [SerializeField, Min(0f)] private float friction = 1f;
        [SerializeField, Min(0f)] private float bounceModifier = 1f;
        [SerializeField, Min(0f)] private float spinResponse = 1f;
        [SerializeField, Min(0f)] private float rollingResistance = 1f;

        public TerrainSurfaceType SurfaceType => surfaceType;
        public float PowerModifier => powerModifier;
        public float Friction => friction;
        public float BounceModifier => bounceModifier;
        public float SpinResponse => spinResponse;
        public float RollingResistance => rollingResistance;
        public bool IsHazard => surfaceType is TerrainSurfaceType.Water or TerrainSurfaceType.OutOfBounds;
    }
}
