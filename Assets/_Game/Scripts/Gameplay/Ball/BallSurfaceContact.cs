using System;
using SwingPop.Gameplay.Course;
using UnityEngine;

namespace SwingPop.Gameplay.Ball
{
    [Serializable]
    public readonly struct BallSurfaceContact
    {
        public BallSurfaceContact(
            Vector3 position,
            TerrainSurfaceType surfaceType,
            float impactSpeed,
            int sequence,
            bool isFirstLanding)
        {
            Position = position;
            SurfaceType = surfaceType;
            ImpactSpeed = Mathf.Max(0f, impactSpeed);
            Sequence = Mathf.Max(1, sequence);
            IsFirstLanding = isFirstLanding;
        }

        public Vector3 Position { get; }
        public TerrainSurfaceType SurfaceType { get; }
        public float ImpactSpeed { get; }
        public int Sequence { get; }
        public bool IsFirstLanding { get; }
    }
}
