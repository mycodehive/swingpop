using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SwingPop.Tests
{
    public sealed class HudSkinPassPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_HudSkin_PreservesSafeAreaAndPrimaryActionCommandPath()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameplayHudPresenter presenter = Object.FindAnyObjectByType<GameplayHudPresenter>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.View, Is.Not.Null);
            Assert.That(presenter.View.Skin, Is.Not.Null);
            Assert.That(holeFlow, Is.Not.Null);
            Assert.That(shotFlow, Is.Not.Null);

            CanvasScaler scaler = presenter.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(presenter.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
            Assert.That(presenter.GetComponentsInChildren<LayoutGroup>(true), Is.Empty);
            Assert.That(presenter.GetComponentsInChildren<ContentSizeFitter>(true), Is.Empty);

            int raycastTargets = 0;
            foreach (Graphic graphic in presenter.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) raycastTargets++;
            Assert.That(raycastTargets, Is.EqualTo(1));
            Assert.That(presenter.View.ActionButton.targetGraphic.raycastTarget, Is.True);

            holeFlow.DebugResetHole();
            yield return null;
            Assert.That(shotFlow.State, Is.EqualTo(ShotFlowState.Aiming));
            Assert.That(presenter.View.ActionLabel, Is.EqualTo("START SHOT"));
            presenter.View.ActionButton.onClick.Invoke();
            yield return null;
            Assert.That(shotFlow.State, Is.EqualTo(ShotFlowState.PowerSelecting));
            Assert.That(presenter.View.ActionLabel, Is.EqualTo("SET POWER"));
            Assert.That(presenter.View.GaugeView.IsPowerVisible, Is.True);

            holeFlow.DebugResetHole();
        }
    }
}
