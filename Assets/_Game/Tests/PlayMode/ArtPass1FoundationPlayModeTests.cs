using System.Collections;
using NUnit.Framework;
using SwingPop.CharacterSystem;
using SwingPop.Presentation;
using SwingPop.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class ArtPass1FoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_ArtPass1_IsConnectedAndPresentationOnly()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameObject environment = FindInScene("Art Pass 1 Environment");
            GameObject courseDetails = FindInScene("Art Pass 1 Course Details");
            Assert.That(environment, Is.Not.Null);
            Assert.That(courseDetails, Is.Not.Null);
            Assert.That(environment.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(courseDetails.GetComponentsInChildren<Collider>(true), Is.Empty);

            CharacterVisualAdapter adapter = Object.FindAnyObjectByType<CharacterVisualAdapter>(FindObjectsInactive.Include);
            Assert.That(adapter, Is.Not.Null);
            Assert.That(adapter.HasRequiredReferences, Is.True);
            Assert.That(adapter.Profile, Is.Not.Null);
            Assert.That(adapter.Profile.VisualHeight, Is.GreaterThanOrEqualTo(3f));
            Assert.That(adapter.Profile.HasValidDimensions, Is.True);
            Assert.That(adapter.LeftHandSocket, Is.Not.Null);
            Assert.That(adapter.RightHandSocket, Is.Not.Null);
            Assert.That(adapter.gameObject.GetComponentsInChildren<Collider>(true), Is.Empty);

            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null, $"{renderer.name} has a missing material slot.");
                }
            }

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>(FindObjectsInactive.Include);
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.HasWaterHighlight, Is.True);
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length, Is.EqualTo(10));

            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.View.ClubLabel, Is.EqualTo("DRIVER"));
            Assert.That(hud.View.ActionLabel, Is.EqualTo("START SHOT"));
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
