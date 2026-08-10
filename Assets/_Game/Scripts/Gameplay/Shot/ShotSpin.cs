using System;
using UnityEngine;

namespace SwingPop.Gameplay.Shot
{
    [Serializable]
    public struct ShotSpin
    {
        [SerializeField] private float verticalSpin;
        [SerializeField] private float sideSpin;

        public ShotSpin(float verticalSpin, float sideSpin)
        {
            this.verticalSpin = Mathf.Clamp(verticalSpin, -1f, 1f);
            this.sideSpin = Mathf.Clamp(sideSpin, -1f, 1f);
        }

        public static ShotSpin None => new(0f, 0f);

        public float VerticalSpin => verticalSpin;
        public float SideSpin => sideSpin;

        public static ShotSpin FromPreset(SpinPreset preset)
        {
            return preset switch
            {
                SpinPreset.TopSpin => new ShotSpin(1f, 0f),
                SpinPreset.BackSpin => new ShotSpin(-1f, 0f),
                SpinPreset.LeftSideSpin => new ShotSpin(0f, -1f),
                SpinPreset.RightSideSpin => new ShotSpin(0f, 1f),
                _ => None
            };
        }

        public override string ToString()
        {
            return $"Vertical {verticalSpin:+0.00;-0.00;0.00}, Side {sideSpin:+0.00;-0.00;0.00}";
        }
    }
}
