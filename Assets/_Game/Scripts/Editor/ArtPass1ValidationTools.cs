using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SwingPop.CharacterSystem;
using SwingPop.Presentation;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class ArtPass1ValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string CharacterPrefabPath = "Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab";
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Validation.request"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Validation.result"));

        [DidReloadScripts]
        private static void RunPendingValidationAfterReload()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            File.Delete(RequestPath);
            try
            {
                string report = ValidateAndGetReport();
                File.WriteAllText(ResultPath, "PASS\n" + report);
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
            }
        }

        [MenuItem("SwingPop/Art Pass 1/Validate Character and Environment Structure")]
        public static void ValidateArtPass1Structure()
        {
            ValidateAndGetReport();
        }

        private static string ValidateAndGetReport()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject environmentRoot = RequireObject(scene, "Art Pass 1 Environment");
            GameObject courseRoot = RequireObject(scene, "Art Pass 1 Course Details");
            Require(environmentRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Art Pass 1 environment visuals contain a Collider and can alter gameplay.");
            Require(courseRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Art Pass 1 course details contain a Collider and can alter gameplay.");

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            Require(characterPrefab != null, "PlaceholderGolfer prefab is missing.");
            CharacterVisualAdapter prefabAdapter = characterPrefab.GetComponent<CharacterVisualAdapter>();
            ValidateAdapter(prefabAdapter, "PlaceholderGolfer prefab");
            Require(characterPrefab.GetComponentsInChildren<Collider>(true).Length == 0,
                "Character visual prefab contains a Collider.");

            CharacterVisualAdapter sceneAdapter = Object.FindAnyObjectByType<CharacterVisualAdapter>(FindObjectsInactive.Include);
            ValidateAdapter(sceneAdapter, "Hole01 character instance");
            Require(sceneAdapter.Profile.VisualHeight >= 3f,
                "Character visual profile height is below the Art Pass 1 silhouette target.");

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>(FindObjectsInactive.Include);
            Require(motion != null && motion.HasWaterHighlight,
                "The environment motion coordinator is not driving the water highlight.");

            Require(RequireObject(scene, "Stylized Tree 01").activeSelf == false,
                "Superseded M10 trees must stay disabled.");
            Require(RequireObject(scene, "Flower Patch 01").activeSelf == false,
                "Superseded M10 flower patches must stay disabled.");
            Require(RequireObject(scene, "Cloud Cluster 01").activeSelf == false,
                "Superseded M10 cloud clusters must stay disabled.");

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
            Require(missingScripts == 0, $"Scene has {missingScripts} Missing Script component(s).");

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            Require(environmentRoot.GetComponentsInChildren<Renderer>(true).Length >= 35,
                "Art Pass 1 environment prefab composition is incomplete.");
            HashSet<Material> materials = new();
            int missingMaterialSlots = 0;
            int transparentSlots = 0;
            int shadowCasters = 0;
            int activeRenderers = 0;
            int activeShadowCasters = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.activeInHierarchy) activeRenderers++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missingMaterialSlots++;
                        continue;
                    }
                    materials.Add(material);
                    if (material.renderQueue >= 3000) transparentSlots++;
                }
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    shadowCasters++;
                    if (renderer.gameObject.activeInHierarchy) activeShadowCasters++;
                }
            }
            Require(missingMaterialSlots == 0, $"Scene has {missingMaterialSlots} missing material slot(s).");

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

            int colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length;
            int particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            Require(colliders == 10, $"Gameplay collider count changed from the M11 baseline: {colliders}.");
            Require(particleSystems <= 8, $"Particle system budget exceeded: {particleSystems}.");
            Require(audioSources <= 6, $"Audio source budget exceeded: {audioSources}.");
            Require(shadowCasters <= 145, $"Shadow caster count regressed above the M10 guardrail: {shadowCasters}.");

            string report =
                $"GameObjects={gameObjects}, Renderers={renderers.Length}, ActiveRenderers={activeRenderers}, SharedMaterials={materials.Count}, " +
                $"TransparentRendererSlots={transparentSlots}, ShadowCasters={shadowCasters}, Colliders={colliders}, " +
                $"ActiveShadowCasters={activeShadowCasters}, " +
                $"ArtColliders=0, ParticleSystems={particleSystems}, AudioSources={audioSources}, " +
                $"UpdateBehaviours={updateBehaviours}, MissingScripts={missingScripts}, MissingMaterialSlots={missingMaterialSlots}.";
            Debug.Log("ART PASS 1 STRUCTURE PASS | " + report);
            return report;
        }

        private static void ValidateAdapter(CharacterVisualAdapter adapter, string owner)
        {
            Require(adapter != null, $"{owner} has no CharacterVisualAdapter.");
            Require(adapter.HasRequiredReferences, $"{owner} adapter has a missing required reference.");
            Require(adapter.GameplayRoot != null && adapter.VisualRoot != null && adapter.ClubSocket != null,
                $"{owner} adapter roots are incomplete.");
            Require(adapter.ImpactAnchor != null && adapter.HeadLookTarget != null,
                $"{owner} adapter anchors are incomplete.");
            Require(adapter.Profile != null, $"{owner} adapter has no CharacterVisualProfile.");
        }

        private static GameObject RequireObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name) return transform.gameObject;
                }
            }
            throw new System.InvalidOperationException($"Required scene object is missing: {name}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException(message);
        }
    }
}
