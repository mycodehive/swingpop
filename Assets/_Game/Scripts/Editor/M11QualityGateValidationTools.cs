using System.Collections.Generic;
using System.Reflection;
using SwingPop.CameraSystem;
using SwingPop.Gameplay.Course;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M11QualityGateValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";

        [MenuItem("SwingPop/M11/Validate Quality Gate Structure")]
        public static void ValidateQualityGateStructure()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject polishRoot = GameObject.Find("M11 Visual Polish");
            Require(polishRoot != null, "M11 Visual Polish root is missing.");
            Require(polishRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "M11 presentation art contains a Collider and can alter gameplay physics.");
            Require(polishRoot.GetComponentsInChildren<Renderer>(true).Length >= 12,
                "M11 presentation layer is incomplete.");

            GameObject oldCourseVisuals = FindInScene(scene, "Course Visual Layers");
            Require(oldCourseVisuals != null && !oldCourseVisuals.activeSelf,
                "The superseded rectangular course visual layer must stay disabled.");
            Require(AssetDatabase.GetAssetPath(RenderSettings.skybox) ==
                    "Assets/_Game/Materials/Environment/CourseEnvironmentPass/CEP_Skybox.mat",
                "Hole01 must use the isolated Course Environment Pass skybox material.");

            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            Require(cameraDirector != null, "CameraDirector is missing.");
            SerializedObject cameraSerialized = new(cameraDirector);
            Object cameraTuning = cameraSerialized.FindProperty("tuning")?.objectReferenceValue;
            string cameraTuningPath = AssetDatabase.GetAssetPath(cameraTuning);
            Require(
                cameraTuningPath == "Assets/_Game/ScriptableObjects/Polish/M11CameraTuning.asset" ||
                cameraTuningPath == "Assets/_Game/ScriptableObjects/Presentation/PuttResultCameraTuning.asset",
                "CameraDirector is not using an approved M11-or-later camera tuning asset.");

            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            Require(hud != null, "GameplayHudPresenter is missing.");
            GameObject action = FindInScene(scene, "Bottom Right - Primary Action");
            Require(action != null, "Primary action HUD is missing.");
            RectTransform actionRect = action.GetComponent<RectTransform>();
            Require(actionRect != null && actionRect.anchorMin == new Vector2(1f, 0f) &&
                    actionRect.anchorMax == new Vector2(1f, 0f),
                "Primary action HUD is not anchored to the lower-right safe area.");

            int missingScripts = 0;
            int gameObjects = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                gameObjects += transforms.Length;
                foreach (Transform transform in transforms)
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }
            Require(missingScripts == 0, $"Scene has {missingScripts} Missing Script component(s).");

            HashSet<TerrainSurfaceType> surfaces = new();
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
            {
                Require(surface.Data != null, $"{surface.name} has no TerrainSurfaceData.");
                surfaces.Add(surface.SurfaceType);
            }
            foreach (TerrainSurfaceType type in new[]
                     {
                         TerrainSurfaceType.Tee, TerrainSurfaceType.Fairway, TerrainSurfaceType.Rough,
                         TerrainSurfaceType.Bunker, TerrainSurfaceType.Green, TerrainSurfaceType.Water,
                         TerrainSurfaceType.OutOfBounds
                     })
            {
                Require(surfaces.Contains(type), $"Gameplay surface {type} is missing.");
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            HashSet<Material> materials = new();
            int transparentSlots = 0;
            int shadowCasters = 0;
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    materials.Add(material);
                    if (material.renderQueue >= 3000) transparentSlots++;
                }
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowCasters++;
            }

            int updateBehaviours = 0;
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour != null && behaviour.GetType().GetMethod(
                        "Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                {
                    updateBehaviours++;
                }
            }

            Require(shadowCasters <= 145, $"Shadow caster count regressed above the M10 baseline: {shadowCasters}.");
            // The VFX Hero Pass replaces the old minimal graph with 15 fixed reusable systems.
            Require(Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length <= 16,
                "Particle system count exceeds the vertical-slice budget.");
            Require(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length <= 6,
                "Audio source count exceeds the vertical-slice budget.");

            Debug.Log(
                $"M11 QUALITY GATE STRUCTURE PASS | GameObjects={gameObjects}, Renderers={renderers.Length}, " +
                $"SharedMaterials={materials.Count}, TransparentRendererSlots={transparentSlots}, " +
                $"ShadowCasters={shadowCasters}, Colliders={Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length}, " +
                $"PolishArtColliders=0, ParticleSystems={Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length}, " +
                $"AudioSources={Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length}, " +
                $"UpdateBehaviours={updateBehaviours}, MissingScripts=0.");
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name) return transform.gameObject;
                }
            }
            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException(message);
        }
    }
}
