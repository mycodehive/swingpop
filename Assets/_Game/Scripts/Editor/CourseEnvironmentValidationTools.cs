using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SwingPop.Presentation;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class CourseEnvironmentValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string RootName = "Course Environment Pass";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/CourseEnvironmentPass";
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCourseEnvironmentValidation.request"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCourseEnvironmentValidation.result"));

        [DidReloadScripts]
        private static void RunPendingValidationAfterReload()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            try
            {
                File.WriteAllText(ResultPath, "PASS\n" + ValidateAndGetReport());
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
            }
        }

        [MenuItem("SwingPop/Environment/Validate Course Art")]
        public static void ValidateCourseArt()
        {
            ValidateAndGetReport();
        }

        private static string ValidateAndGetReport()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = RequireObject(scene, RootName);
            Require(root.activeInHierarchy, "Course Environment Pass root is inactive.");
            Require(root.GetComponentsInChildren<Collider>(true).Length == 0,
                "Course Environment Pass contains a Collider and can alter gameplay.");

            foreach (string required in new[]
                     {
                         "Layered Main Island", "Broad Fairway Mowing", "Irregular Fairway Fringe", "Green Fine Mowing",
                         "Bunker Layered Sand", "Water Deep Body", "Water Shallow Band", "Water Highlight Motion",
                         "Course Windmill Landmark", "Course Waterfall Landmark", "Stylized Flag Cloth"
                     })
            {
                RequireObject(scene, required);
            }

            GameObject oldEnvironment = RequireObject(scene, "Art Pass 1 Environment");
            GameObject oldDetails = RequireObject(scene, "Art Pass 1 Course Details");
            Require(!oldEnvironment.activeSelf && !oldDetails.activeSelf,
                "Superseded ART PASS 1 environment visuals must stay inactive.");
            for (int index = 1; index <= 4; index++)
            {
                Require(!RequireObject(scene, $"Distant Island {index:00}").activeSelf,
                    $"Superseded M10 distant island {index:00} must stay inactive.");
            }

            string[] prefabNames =
            {
                "CEP_Tree_A", "CEP_Tree_B", "CEP_Tree_C", "CEP_Cloud_A", "CEP_Cloud_B", "CEP_Cloud_C",
                "CEP_Island_A", "CEP_Island_B", "CEP_Island_C", "CEP_FlowerPatch", "CEP_Windmill", "CEP_WaterfallIsland"
            };
            foreach (string name in prefabNames)
            {
                Require(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab") != null,
                    $"Course environment prefab reference is missing: {name}");
            }

            int missingMeshes = 0;
            int missingMaterialSlots = 0;
            int nonAssetMaterials = 0;
            int duplicateMaterialAssets = 0;
            int unexpectedShadowCasters = 0;
            Dictionary<string, string> materialPathsByName = new();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) missingMeshes++;
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        missingMaterialSlots++;
                        continue;
                    }
                    if (!AssetDatabase.Contains(material)) nonAssetMaterials++;
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    if (materialPathsByName.TryGetValue(material.name, out string existingPath)
                        && existingPath != materialPath)
                    {
                        duplicateMaterialAssets++;
                    }
                    else
                    {
                        materialPathsByName[material.name] = materialPath;
                    }
                }
                if (renderer.shadowCastingMode != ShadowCastingMode.Off && !IsAllowedShadowCaster(renderer.transform))
                {
                    unexpectedShadowCasters++;
                }
            }
            Require(missingMeshes == 0, $"Course Environment Pass has {missingMeshes} missing mesh reference(s).");
            Require(missingMaterialSlots == 0, $"Course Environment Pass has {missingMaterialSlots} missing material slot(s).");
            Require(nonAssetMaterials == 0, $"Course Environment Pass has {nonAssetMaterials} non-persistent material reference(s).");
            Require(duplicateMaterialAssets == 0, $"Course Environment Pass has {duplicateMaterialAssets} duplicate-named material asset(s).");
            Require(unexpectedShadowCasters == 0, $"Course Environment Pass has {unexpectedShadowCasters} unexpected shadow caster(s).");

            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>(FindObjectsInactive.Include);
            Require(motion != null && motion.HasTuning && motion.HasWindmillRotor && motion.DriftingCloudCount == 6 && motion.HasWaterHighlight,
                "Central environment motion references are incomplete.");

            int gameObjects = 0;
            int missingScripts = 0;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform transform in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    gameObjects++;
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }
            Require(missingScripts == 0, $"Scene has {missingScripts} Missing Script component(s).");

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            HashSet<Material> materials = new();
            int activeRenderers = 0;
            int transparentSlots = 0;
            int activeTransparentSlots = 0;
            int shadowCasters = 0;
            int activeShadowCasters = 0;
            foreach (Renderer renderer in renderers)
            {
                bool active = renderer.gameObject.activeInHierarchy && renderer.enabled;
                if (active) activeRenderers++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    materials.Add(material);
                    if (material.renderQueue >= (int)RenderQueue.Transparent)
                    {
                        transparentSlots++;
                        if (active) activeTransparentSlots++;
                    }
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
                if (behaviour != null && behaviour.GetType().GetMethod("Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null)
                {
                    updateBehaviours++;
                }
            }

            int colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length;
            int particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            Require(colliders == 10, $"Gameplay collider count changed from the established baseline: {colliders}.");
            // The VFX Hero Pass uses a fixed, reusable 15-system graph. This budget guards
            // against accidental scene growth while allowing the authored impact/landing/hole layers.
            Require(particleSystems <= 16, $"Particle system budget exceeded: {particleSystems}.");
            Require(audioSources <= 6, $"Audio source budget exceeded: {audioSources}.");
            Require(activeShadowCasters <= 100, $"Active shadow caster budget exceeded: {activeShadowCasters}.");

            string report =
                $"GameObjects={gameObjects}, Renderers={renderers.Length}, ActiveRenderers={activeRenderers}, SharedMaterials={materials.Count}, " +
                $"TransparentRendererSlots={transparentSlots}, ActiveTransparentRendererSlots={activeTransparentSlots}, " +
                $"ShadowCasters={shadowCasters}, ActiveShadowCasters={activeShadowCasters}, " +
                $"Colliders={colliders}, CourseArtColliders=0, ParticleSystems={particleSystems}, AudioSources={audioSources}, " +
                $"UpdateBehaviours={updateBehaviours}, MissingScripts={missingScripts}, MissingMeshes={missingMeshes}, " +
                $"MissingMaterialSlots={missingMaterialSlots}, NonAssetMaterials={nonAssetMaterials}, DuplicateMaterialAssets={duplicateMaterialAssets}, " +
                $"UnexpectedShadowCasters={unexpectedShadowCasters}.";
            Debug.Log("COURSE ART VALIDATION PASS | " + report);
            return report;
        }

        private static bool IsAllowedShadowCaster(Transform transform)
        {
            string path = HierarchyPath(transform);
            return path.Contains("Course Tree 01") || path.Contains("Course Tree 02")
                   || path.Contains("Course Tree 03") || path.Contains("Course Tree 04")
                   || path.Contains("Course Tree 05") || path.Contains("Course Tree 06")
                   || path.Contains("Course Windmill Landmark");
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
