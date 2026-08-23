using System.Collections.Generic;
using System.Reflection;
using SwingPop.CameraSystem;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M10VerticalSliceValidationTools
    {
        [MenuItem("SwingPop/M10/Validate Vertical Slice Structure")]
        public static void ValidateVerticalSliceStructure()
        {
            Scene scene = EditorSceneManager.OpenScene(M10VerticalSliceSceneBuilder.ScenePath, OpenSceneMode.Single);
            GameObject artRoot = GameObject.Find("M10 Sky Island Art");
            Require(artRoot != null, "M10 Sky Island Art root is missing.");
            Require(Object.FindAnyObjectByType<GolfBallController>() != null, "GolfBallController is missing.");
            Require(Object.FindAnyObjectByType<ShotFlowController>() != null, "ShotFlowController is missing.");
            Require(Object.FindAnyObjectByType<HoleFlowController>() != null, "HoleFlowController is missing.");
            Require(Object.FindAnyObjectByType<CameraDirector>() != null, "CameraDirector is missing.");
            Require(Object.FindAnyObjectByType<GameplayHudPresenter>() != null, "GameplayHudPresenter is missing.");
            Require(Object.FindAnyObjectByType<ShotPresentationController>() != null, "ShotPresentationController is missing.");
            Require(Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>() != null, "SkyIslandEnvironmentMotion is missing.");

            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }
            Require(missingScripts == 0, $"Scene has {missingScripts} Missing Script component(s).");
            Require(artRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Decorative art contains a Collider and can interfere with gameplay.");

            HashSet<TerrainSurfaceType> surfaceTypes = new();
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
            {
                Require(surface.Data != null, $"{surface.name} has no TerrainSurfaceData.");
                surfaceTypes.Add(surface.SurfaceType);
            }
            foreach (TerrainSurfaceType type in new[]
                     {
                         TerrainSurfaceType.Tee, TerrainSurfaceType.Fairway, TerrainSurfaceType.Rough,
                         TerrainSurfaceType.Bunker, TerrainSurfaceType.Green, TerrainSurfaceType.Water,
                         TerrainSurfaceType.OutOfBounds
                     })
            {
                Require(surfaceTypes.Contains(type), $"Gameplay surface {type} is missing.");
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            HashSet<Material> materials = new();
            int transparentRenderers = 0;
            int shadowCasters = 0;
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null) materials.Add(material);
                    if (material != null && material.renderQueue >= 3000) transparentRenderers++;
                }
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowCasters++;
            }

            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            int updateBehaviours = 0;
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().GetMethod(
                        "Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                {
                    updateBehaviours++;
                }
            }

            int gameObjects = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                gameObjects += root.GetComponentsInChildren<Transform>(true).Length;
            }

            Debug.Log(
                $"M10 STRUCTURE PASS | GameObjects={gameObjects}, Renderers={renderers.Length}, " +
                $"SharedMaterials={materials.Count}, TransparentRendererSlots={transparentRenderers}, " +
                $"ShadowCasters={shadowCasters}, Colliders={Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length}, " +
                $"ArtColliders=0, ParticleSystems={Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length}, " +
                $"AudioSources={Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length}, " +
                $"UpdateBehaviours={updateBehaviours}, MissingScripts=0.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }
    }
}
