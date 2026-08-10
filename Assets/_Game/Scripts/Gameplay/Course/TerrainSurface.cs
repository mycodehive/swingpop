using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Gameplay.Course
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TerrainSurface : MonoBehaviour
    {
        [SerializeField] private TerrainSurfaceData data;

        public TerrainSurfaceData Data => data;
        public TerrainSurfaceType SurfaceType => data != null ? data.SurfaceType : TerrainSurfaceType.Fairway;
        public bool IsHazard => data != null && data.IsHazard;
    }
}
