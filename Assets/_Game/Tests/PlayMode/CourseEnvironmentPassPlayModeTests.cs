using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class CourseEnvironmentPassPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_CourseEnvironmentPass_IsPresentationOnlyAndGameplaySurfacesRemainConnected()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameObject passRoot = FindInScene("Course Environment Pass");
            Assert.That(passRoot, Is.Not.Null);
            Assert.That(passRoot.activeInHierarchy, Is.True);
            Assert.That(passRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(FindInScene("Art Pass 1 Environment").activeSelf, Is.False);
            Assert.That(FindInScene("Art Pass 1 Course Details").activeSelf, Is.False);

            TerrainSurface[] surfaces = Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include);
            Assert.That(surfaces.Length, Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length, Is.EqualTo(10));

            foreach (string required in new[]
                     {
                         "Broad Fairway Mowing", "Green Fine Mowing", "Bunker Layered Sand", "Water Deep Body",
                         "Layered Main Island", "Course Windmill Landmark", "Course Waterfall Landmark"
                     })
            {
                Assert.That(FindInScene(required), Is.Not.Null, required);
            }

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>(FindObjectsInactive.Include);
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.HasWindmillRotor, Is.True);
            Assert.That(motion.HasWaterHighlight, Is.True);
            Assert.That(motion.DriftingCloudCount, Is.EqualTo(6));
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
