using System;
using System.Collections.Generic;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    public static class PuttResultCinematicValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string TuningPath = "Assets/_Game/ScriptableObjects/Presentation/PuttResultCinematicTuning.asset";
        private const string CameraPath = "Assets/_Game/ScriptableObjects/Presentation/PuttResultCameraTuning.asset";

        [MenuItem("SwingPop/Presentation/Validate Putt Result Cinematic")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> failures = new();
            PuttResultCinematicTuningData tuning = AssetDatabase.LoadAssetAtPath<PuttResultCinematicTuningData>(TuningPath);
            CameraTuningData cameraTuning = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraPath);
            Require(tuning != null, "Cinematic tuning asset exists", failures);
            Require(cameraTuning != null, "Camera tuning asset exists", failures);

            PuttResultCinematicController[] coordinators = Object.FindObjectsByType<PuttResultCinematicController>(
                FindObjectsInactive.Include);
            Require(coordinators.Length == 1, $"Exactly one cinematic coordinator (found {coordinators.Length})", failures);
            if (coordinators.Length == 1)
            {
                Require(coordinators[0].IsConfigured, "Cinematic coordinator dependencies are connected", failures);
                Require(coordinators[0].Tuning == tuning, "Coordinator uses pass tuning", failures);
            }

            CameraDirector camera = Object.FindAnyObjectByType<CameraDirector>(FindObjectsInactive.Include);
            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>(FindObjectsInactive.Include);
            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>(FindObjectsInactive.Include);
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>(FindObjectsInactive.Include);
            HoleInVfxController holeVfx = Object.FindAnyObjectByType<HoleInVfxController>(FindObjectsInactive.Include);
            HudResultView result = Object.FindAnyObjectByType<HudResultView>(FindObjectsInactive.Include);
            Require(camera != null && camera.CinematicTuning == tuning, "CameraDirector uses cinematic tuning", failures);
            Require(camera != null && ReadReference<CameraTuningData>(camera, "tuning") == cameraTuning,
                "CameraDirector uses pass camera tuning", failures);
            Require(character != null && character.CinematicTuning == tuning, "Character reaction is coordinator-managed", failures);
            Require(hud != null && hud.CinematicTuning == tuning, "HUD result reveal is coordinator-managed", failures);
            Require(audio != null && audio.CinematicTuning == tuning, "Audio result cues are coordinator-managed", failures);
            Require(holeVfx != null && holeVfx.CinematicTuning == tuning, "Hole VFX uses cinematic timing", failures);
            Require(result != null && result.HasStagedGroups, "Result UI has score/detail reveal groups", failures);

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            Require(canvases.Length == 1, $"One HUD Canvas (found {canvases.Length})", failures);
            Require(eventSystems.Length == 1, $"One EventSystem (found {eventSystems.Length})", failures);
            ValidateMissingScripts(scene, failures);

            int particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
            string performance = $"cameras={cameras}, canvases={canvases.Length}, particles={particles}, audioSources={audioSources}";
            if (failures.Count > 0)
            {
                throw new InvalidOperationException("PUTT / RESULT CINEMATIC VALIDATION FAILED\n- " + string.Join("\n- ", failures));
            }

            Debug.Log($"PUTT / RESULT CINEMATIC VALIDATION PASSED | {performance} | no missing scripts; one coordinator; staged result graph connected.");
        }

        private static void ValidateMissingScripts(Scene scene, ICollection<string> failures)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    {
                        failures.Add($"Missing script: {HierarchyPath(transform)}");
                    }
                }
            }
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static T ReadReference<T>(Object target, string propertyName) where T : Object
        {
            if (target == null) return null;
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void Require(bool condition, string message, ICollection<string> failures)
        {
            if (!condition) failures.Add(message);
        }
    }
}
