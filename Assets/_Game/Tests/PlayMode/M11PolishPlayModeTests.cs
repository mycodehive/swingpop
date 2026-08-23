using System.Collections;
using NUnit.Framework;
using SwingPop.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M11PolishPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_PolishLayer_IsPresentationOnlyAndHudFitsAddressState()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameObject polishRoot = GameObject.Find("M11 Visual Polish");
            Assert.That(polishRoot, Is.Not.Null);
            Assert.That(polishRoot.GetComponentsInChildren<Collider>(true), Is.Empty,
                "M11 course polish must remain presentation-only.");
            Assert.That(polishRoot.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(12));

            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.View.ClubLabel, Is.EqualTo("DRIVER"));
            Assert.That(hud.View.ActionLabel, Is.EqualTo("START SHOT"));

            GameObject action = GameObject.Find("Bottom Right - Primary Action");
            Assert.That(action, Is.Not.Null);
            RectTransform actionRect = action.GetComponent<RectTransform>();
            Assert.That(actionRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(actionRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(actionRect.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(actionRect.anchoredPosition.y, Is.GreaterThan(0f));

            GameObject oldCourseVisuals = FindInScene("Course Visual Layers");
            Assert.That(oldCourseVisuals, Is.Not.Null);
            Assert.That(oldCourseVisuals.activeSelf, Is.False);
        }

        private static GameObject FindInScene(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name) return transform.gameObject;
                }
            }
            return null;
        }
    }
}
