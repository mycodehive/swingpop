using SwingPop.Gameplay.Shot;

namespace SwingPop.Gameplay.Ball
{
    public sealed class BallSpinState
    {
        public float VerticalSpin { get; private set; }
        public float SideSpin { get; private set; }
        public ShotSpin Current => new(VerticalSpin, SideSpin);

        public void Set(ShotSpin spin)
        {
            VerticalSpin = spin.VerticalSpin;
            SideSpin = spin.SideSpin;
        }

        public void Decay(float verticalDecayPerSecond, float sideDecayPerSecond, float deltaTime)
        {
            VerticalSpin = BallFlightModel.DecaySpin(VerticalSpin, verticalDecayPerSecond, deltaTime);
            SideSpin = BallFlightModel.DecaySpin(SideSpin, sideDecayPerSecond, deltaTime);
        }

        public void Reset()
        {
            VerticalSpin = 0f;
            SideSpin = 0f;
        }
    }
}
