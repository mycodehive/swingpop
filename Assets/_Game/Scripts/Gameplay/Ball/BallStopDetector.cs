namespace SwingPop.Gameplay.Ball
{
    /// <summary>
    /// Pure stop evaluation kept independent from Unity lifecycle for deterministic tests.
    /// </summary>
    public sealed class BallStopDetector
    {
        private float stableTime;

        public float StableTime => stableTime;

        public bool Sample(
            bool isGrounded,
            float linearSpeed,
            float angularSpeed,
            float deltaTime,
            float maximumLinearSpeed,
            float maximumAngularSpeed,
            float requiredStableDuration)
        {
            bool isStable = isGrounded
                && linearSpeed <= maximumLinearSpeed
                && angularSpeed <= maximumAngularSpeed;

            stableTime = isStable ? stableTime + deltaTime : 0f;
            return isStable && stableTime >= requiredStableDuration;
        }

        public void Reset()
        {
            stableTime = 0f;
        }
    }
}
