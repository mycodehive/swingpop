using NUnit.Framework;
using SwingPop.Data;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.UI;

namespace SwingPop.Tests
{
    public sealed class HudSkinStyleMapperTests
    {
        [TestCase(ImpactGrade.Perfect, HudSkinTone.Gold)]
        [TestCase(ImpactGrade.Great, HudSkinTone.Cyan)]
        [TestCase(ImpactGrade.Good, HudSkinTone.Mint)]
        [TestCase(ImpactGrade.Miss, HudSkinTone.Coral)]
        public void ImpactGrade_MapsToPresentationTone(ImpactGrade grade, HudSkinTone expected)
        {
            Assert.That(HudSkinStyleMapper.ForImpact(grade), Is.EqualTo(expected));
        }

        [TestCase(TerrainSurfaceType.Tee, HudSkinTone.Fairway)]
        [TestCase(TerrainSurfaceType.Fairway, HudSkinTone.Fairway)]
        [TestCase(TerrainSurfaceType.Rough, HudSkinTone.Rough)]
        [TestCase(TerrainSurfaceType.Bunker, HudSkinTone.Bunker)]
        [TestCase(TerrainSurfaceType.Green, HudSkinTone.Green)]
        [TestCase(TerrainSurfaceType.Water, HudSkinTone.Coral)]
        [TestCase(TerrainSurfaceType.OutOfBounds, HudSkinTone.Coral)]
        public void Lie_MapsToPresentationTone(TerrainSurfaceType lie, HudSkinTone expected)
        {
            Assert.That(HudSkinStyleMapper.ForLie(lie), Is.EqualTo(expected));
        }

        [TestCase(ShotFlowState.Aiming, HudSkinTone.Cyan)]
        [TestCase(ShotFlowState.PowerSelecting, HudSkinTone.Mint)]
        [TestCase(ShotFlowState.ImpactSelecting, HudSkinTone.Gold)]
        public void ShotState_MapsToActionTone(ShotFlowState state, HudSkinTone expected)
        {
            Assert.That(HudSkinStyleMapper.ForAction(state), Is.EqualTo(expected));
        }

        [Test]
        public void Result_MapsBetterThanParToGold_ParToCyan_AndOverParToCoral()
        {
            Assert.That(HudSkinStyleMapper.ForResult(new ScoreResult(3, 4, -1, "Birdie")), Is.EqualTo(HudSkinTone.Gold));
            Assert.That(HudSkinStyleMapper.ForResult(new ScoreResult(4, 4, 0, "Par")), Is.EqualTo(HudSkinTone.Cyan));
            Assert.That(HudSkinStyleMapper.ForResult(new ScoreResult(5, 4, 1, "Bogey")), Is.EqualTo(HudSkinTone.Coral));
        }
    }
}
