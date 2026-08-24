using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Presentation;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class FinalGraphicsQualityGateValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private static readonly string CaptureDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/final-graphics-quality-gate"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/FinalGraphicsQualityGate/Validation.result"));

        [MenuItem("SwingPop/Quality Gate/Run Final Graphics Structural Gate")]
        public static void ValidateFinalGraphicsGate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            try
            {
                // Execute every validator explicitly required by the final gate.
                M10VerticalSliceValidationTools.ValidateVerticalSliceStructure();
                M11QualityGateValidationTools.ValidateQualityGateStructure();
                CourseEnvironmentValidationTools.ValidateCourseArt();
                HudSkinValidationTools.ValidateGameplayHud();
                VfxHeroValidationTools.ValidateHeroVfx();
                CharacterSetupValidationTools.ValidateCharacterSetup();
                PuttResultCinematicValidationTools.Validate();

                string report = ValidateSceneAndCaptureMetrics();
                File.WriteAllText(ResultPath, "PASS\n" + report);
                Debug.Log("FINAL GRAPHICS STRUCTURAL GATE PASS | " + report);
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
        }

        private static string ValidateSceneAndCaptureMetrics()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int gameObjects = 0;
            int missingScripts = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    gameObjects++;
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            HashSet<Material> materials = new();
            int activeRenderers = 0;
            int transparentRenderers = 0;
            int activeTransparentRenderers = 0;
            int transparentSlots = 0;
            int shadowCasters = 0;
            int activeShadowCasters = 0;
            int missingMaterialSlots = 0;
            foreach (Renderer renderer in renderers)
            {
                bool active = renderer.gameObject.activeInHierarchy && renderer.enabled;
                if (active) activeRenderers++;
                bool transparent = false;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missingMaterialSlots++;
                        continue;
                    }
                    materials.Add(material);
                    if (material.renderQueue >= (int)RenderQueue.Transparent)
                    {
                        transparent = true;
                        transparentSlots++;
                    }
                }
                if (transparent)
                {
                    transparentRenderers++;
                    if (active) activeTransparentRenderers++;
                }
                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    shadowCasters++;
                    if (active) activeShadowCasters++;
                }
            }

            int updateBehaviours = 0;
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour != null && behaviour.GetType().GetMethod(
                        "Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                    updateBehaviours++;
            }

            int colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length;
            int particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            int canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length;
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
            int eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length;
            int hudPresenters = Object.FindObjectsByType<GameplayHudPresenter>(FindObjectsInactive.Include).Length;
            int audioControllers = Object.FindObjectsByType<GameplayAudioController>(FindObjectsInactive.Include).Length;
            int cinematicControllers = Object.FindObjectsByType<PuttResultCinematicController>(FindObjectsInactive.Include).Length;
            int characterControllers = Object.FindObjectsByType<CharacterGolfController>(FindObjectsInactive.Include).Length;

            Require(missingScripts == 0, $"Missing Scripts found: {missingScripts}.");
            Require(missingMaterialSlots == 0, $"Missing material slots found: {missingMaterialSlots}.");
            Require(colliders == 10, $"Gameplay collider count changed: {colliders}.");
            Require(particles == 15, $"ParticleSystem graph changed: {particles}.");
            Require(audioSources == 5, $"AudioSource graph changed: {audioSources}.");
            Require(canvases == 1, $"Expected one Canvas, found {canvases}.");
            Require(cameras == 1, $"Expected one Camera, found {cameras}.");
            Require(eventSystems == 1, $"Expected one EventSystem, found {eventSystems}.");
            Require(hudPresenters == 1, $"Expected one GameplayHudPresenter, found {hudPresenters}.");
            Require(audioControllers == 1, $"Expected one GameplayAudioController, found {audioControllers}.");
            Require(cinematicControllers == 1, $"Expected one PuttResultCinematicController, found {cinematicControllers}.");
            Require(characterControllers == 1, $"Expected one CharacterGolfController, found {characterControllers}.");
            Require(activeShadowCasters <= 100, $"Active shadow caster budget exceeded: {activeShadowCasters}.");

            ValidateCapture("A2-Clean-Address.png", 1920, 1080);
            ValidateCapture("B1-Power.png", 1920, 1080);
            ValidateCapture("E1-Green-Putter-Address.png", 1920, 1080);
            ValidateCapture("F3-Result.png", 1920, 1080);
            ValidateCapture("R1-Address-1600x900.png", 1600, 900);
            ValidateCapture("R2-Address-1280x720.png", 1280, 720);
            ValidateCapture("R3-Power-1600x900.png", 1600, 900);
            ValidateCapture("R4-Power-1280x720.png", 1280, 720);
            ValidateCapture("R5-Putt-1600x900.png", 1600, 900);
            ValidateCapture("R6-Putt-1280x720.png", 1280, 720);
            ValidateCapture("R7-Result-1600x900.png", 1600, 900);
            ValidateCapture("R8-Result-1280x720.png", 1280, 720);

            return
                $"GameObjects={gameObjects}, Renderers={renderers.Length}, ActiveRenderers={activeRenderers}, " +
                $"Materials={materials.Count}, TransparentRenderers={transparentRenderers}, " +
                $"ActiveTransparentRenderers={activeTransparentRenderers}, TransparentSlots={transparentSlots}, " +
                $"ShadowCasters={shadowCasters}, ActiveShadowCasters={activeShadowCasters}, Colliders={colliders}, " +
                $"ParticleSystems={particles}, AudioSources={audioSources}, Canvas={canvases}, Cameras={cameras}, " +
                $"EventSystems={eventSystems}, UpdateBehaviours={updateBehaviours}, MissingScripts={missingScripts}, " +
                $"MissingMaterialSlots={missingMaterialSlots}, DuplicateHUD=0, DuplicateCamera=0, DuplicateEventSystem=0, " +
                $"DuplicateAudioController=0, RequiredValidators=7/7, ResolutionCaptures=12/12.";
        }

        private static void ValidateCapture(string fileName, int width, int height)
        {
            string path = Path.Combine(CaptureDirectory, fileName);
            Require(File.Exists(path), $"Required capture is missing: {fileName}");
            Texture2D texture = new(2, 2, TextureFormat.RGB24, false);
            try
            {
                Require(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false),
                    $"Capture could not be decoded: {fileName}");
                Require(texture.width == width && texture.height == height,
                    $"Capture resolution mismatch for {fileName}: {texture.width}x{texture.height}.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
