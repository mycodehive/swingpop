using NUnit.Framework;
using SwingPop.Gameplay.Ball;

namespace SwingPop.Tests.EditMode
{
    public sealed class BallStopDetectorTests
    {
        [Test]
        public void Sample_ReturnsTrueAfterRequiredStableDuration()
        {
            BallStopDetector detector = new();

            Assert.That(detector.Sample(true, 0.05f, 0.2f, 0.3f, 0.1f, 0.5f, 0.6f), Is.False);
            Assert.That(detector.Sample(true, 0.05f, 0.2f, 0.3f, 0.1f, 0.5f, 0.6f), Is.True);
        }

        [Test]
        public void Sample_ResetsStableTimeWhenBallLeavesGround()
        {
            BallStopDetector detector = new();

            detector.Sample(true, 0.05f, 0.2f, 0.4f, 0.1f, 0.5f, 0.6f);
            detector.Sample(false, 0.05f, 0.2f, 0.1f, 0.1f, 0.5f, 0.6f);

            Assert.That(detector.StableTime, Is.Zero);
        }

        [Test]
        public void Sample_RejectsMotionAboveEitherThreshold()
        {
            BallStopDetector detector = new();

            Assert.That(detector.Sample(true, 0.2f, 0.2f, 1f, 0.1f, 0.5f, 0.6f), Is.False);
            Assert.That(detector.Sample(true, 0.05f, 0.8f, 1f, 0.1f, 0.5f, 0.6f), Is.False);
        }
    }
}
