using System.Collections.Generic;
using System.IO;
using SwingPop.AudioSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Presentation;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class VfxHeroValidationTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PrefabPath = "Assets/_Game/Prefabs/VFX/ShotFeelPresentation_Hero.prefab";
        private const string TuningPath = "Assets/_Game/ScriptableObjects/Presentation/VfxHeroShotPresentationTuning.asset";
        private const string ArtFolder = "Assets/_Game/Art/VFX/HeroPass";
        private const string MaterialFolder = "Assets/_Game/Materials/VFX/HeroPass";

        [MenuItem("SwingPop/VFX/Validate Hero VFX")]
        public static void ValidateHeroVfx()
        {
            Debug.Log(ValidateAndGetReport());
        }

        public static string ValidateAndGetReport()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ShotPresentationController[] presentations = Object.FindObjectsByType<ShotPresentationController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(presentations.Length == 1, $"Expected one ShotPresentationController, found {presentations.Length}.");
            ShotPresentationController presentation = presentations[0];
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(presentation.gameObject);
            Require(sourcePath == PrefabPath, $"Hole01 VFX source must be {PrefabPath}, got {sourcePath}.");
            Require(presentation.GetComponentsInChildren<Collider>(true).Length == 0,
                "VFX presentation hierarchy must not contain gameplay colliders.");

            ImpactVfxController impact = RequireSingle<ImpactVfxController>();
            BallTrailController trail = RequireSingle<BallTrailController>();
            LandingVfxController landing = RequireSingle<LandingVfxController>();
            HoleInVfxController holeIn = RequireSingle<HoleInVfxController>();
            GameplayAudioController audio = RequireSingle<GameplayAudioController>();
            ShotPresentationTuningData tuning = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(TuningPath);
            Require(tuning != null, "VFX Hero tuning asset is missing.");

            RequireReferences(presentation, "shotFlow", "ball", "holeFlow", "impactVfx", "ballTrail", "landingVfx", "holeInVfx");
            RequireReferences(impact, "tuning", "coreFlash", "radialRing", "radialBurst", "directionalStreak", "accentSparkles");
            RequireReferences(trail, "ball", "tuning", "outerTrail", "coreTrail", "accentTrail", "speedStreaks");
            RequireReferences(landing, "tuning", "groundBurst", "groundRing", "waterSplash", "waterRing", "waterDroplets");
            RequireReferences(holeIn, "tuning", "cupFlash", "upwardSparkles", "ringBurst", "celebrationSparkles");
            RequireReferences(audio, "shotFlow", "ball", "holeFlow", "characterAnimation", "characterTransform",
                "tuning", "swingSource", "impactSource", "terrainSource", "uiResultSource");
            Require(SerializedReference(impact, "tuning") == tuning, "Impact tuning is not the Hero asset.");
            Require(SerializedReference(trail, "tuning") == tuning, "Trail tuning is not the Hero asset.");
            Require(SerializedReference(landing, "tuning") == tuning, "Landing tuning is not the Hero asset.");
            Require(SerializedReference(holeIn, "tuning") == tuning, "Hole-In tuning is not the Hero asset.");
            Require(SerializedReference(audio, "tuning") == tuning, "Audio tuning is not the Hero asset.");

            Require(impact.LayerCount == 5, $"Impact requires 5 reusable layers, found {impact.LayerCount}.");
            Require(landing.LayerCount == 5, $"Landing requires 5 reusable layers, found {landing.LayerCount}.");
            Require(holeIn.LayerCount == 4, $"Hole-In requires 4 reusable layers, found {holeIn.LayerCount}.");

            foreach (string sprite in new[] { "SoftGlow", "Streak", "Ring", "Sparkle", "Dust", "Splash" })
            {
                string path = $"{ArtFolder}/VFX_{sprite}.png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Require(texture != null, $"Missing VFX sprite texture: {path}");
            }

            HashSet<Material> materials = new();
            ParticleSystemRenderer[] particleRenderers = presentation.GetComponentsInChildren<ParticleSystemRenderer>(true);
            TrailRenderer[] trails = presentation.GetComponentsInChildren<TrailRenderer>(true);
            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                Require(renderer.sharedMaterial != null, $"Particle renderer {renderer.name} has no material.");
                Require(AssetDatabase.GetAssetPath(renderer.sharedMaterial).StartsWith(MaterialFolder),
                    $"Particle renderer {renderer.name} does not use a shared Hero material.");
                materials.Add(renderer.sharedMaterial);
            }
            foreach (TrailRenderer renderer in trails)
            {
                Require(renderer.sharedMaterial != null, $"Trail renderer {renderer.name} has no material.");
                Require(AssetDatabase.GetAssetPath(renderer.sharedMaterial).StartsWith(MaterialFolder),
                    $"Trail renderer {renderer.name} does not use a shared Hero material.");
                materials.Add(renderer.sharedMaterial);
            }

            int missingScripts = CountMissingScripts(scene);
            Require(missingScripts == 0, $"Scene contains {missingScripts} missing scripts.");
            Require(particleRenderers.Length == 15, $"Expected 15 ParticleSystem renderers, found {particleRenderers.Length}.");
            Require(trails.Length == 3, $"Expected 3 TrailRenderers, found {trails.Length}.");
            Require(materials.Count == 6, $"Expected 6 shared Hero materials, found {materials.Count}.");

            int maxParticles = 0;
            foreach (ParticleSystem particle in presentation.GetComponentsInChildren<ParticleSystem>(true))
                maxParticles += particle.main.maxParticles;

            string report =
                "VFX HERO VALIDATION PASS | "
                + $"ParticleSystems={particleRenderers.Length}, Trails={trails.Length}, SharedMaterials={materials.Count}, "
                + $"ImpactLayers={impact.LayerCount}, LandingLayers={landing.LayerCount}, HoleLayers={holeIn.LayerCount}, "
                + $"ConfiguredMaxParticles={maxParticles}, Colliders=0, MissingScripts={missingScripts}, "
                + "DuplicateControllers=0, MissingReferences=0, MissingSprites=0.";
            return report;
        }

        [MenuItem("SwingPop/VFX/Preview/Normal Impact")]
        private static void PreviewNormal() => PreviewImpact(ShotPresentationLevel.Normal, false);

        [MenuItem("SwingPop/VFX/Preview/Great Impact")]
        private static void PreviewGreat() => PreviewImpact(ShotPresentationLevel.Great, false);

        [MenuItem("SwingPop/VFX/Preview/Perfect Impact")]
        private static void PreviewPerfect() => PreviewImpact(ShotPresentationLevel.Perfect, false);

        [MenuItem("SwingPop/VFX/Preview/Putter Impact")]
        private static void PreviewPutter() => PreviewImpact(ShotPresentationLevel.Perfect, true);

        [MenuItem("SwingPop/VFX/Preview/Fairway Landing")]
        private static void PreviewFairway() => PreviewLanding(LandingEffectType.Grass);

        [MenuItem("SwingPop/VFX/Preview/Rough Landing")]
        private static void PreviewRough() => PreviewLanding(LandingEffectType.Rough);

        [MenuItem("SwingPop/VFX/Preview/Bunker Landing")]
        private static void PreviewBunker() => PreviewLanding(LandingEffectType.Sand);

        [MenuItem("SwingPop/VFX/Preview/Water Splash")]
        private static void PreviewWater() => PreviewLanding(LandingEffectType.Water);

        [MenuItem("SwingPop/VFX/Preview/Hole In")]
        private static void PreviewHoleIn()
        {
            RequirePlayMode();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            HoleInVfxController effect = Object.FindAnyObjectByType<HoleInVfxController>();
            if (holeFlow == null || effect == null) throw new System.InvalidOperationException("Hole-In preview dependencies are missing.");
            effect.Play(holeFlow.Hole.CupPosition, CelebrationPresentationLevel.Strong);
        }

        private static void PreviewImpact(ShotPresentationLevel level, bool isPutter)
        {
            RequirePlayMode();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ImpactVfxController effect = Object.FindAnyObjectByType<ImpactVfxController>();
            if (ball == null || effect == null) throw new System.InvalidOperationException("Impact preview dependencies are missing.");
            effect.Play(ball.PhysicsPosition, ball.LaunchForward, level, isPutter);
        }

        private static void PreviewLanding(LandingEffectType type)
        {
            RequirePlayMode();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            LandingVfxController effect = Object.FindAnyObjectByType<LandingVfxController>();
            if (ball == null || effect == null) throw new System.InvalidOperationException("Landing preview dependencies are missing.");
            effect.Play(ball.PhysicsPosition, type, 1f);
        }

        private static void RequirePlayMode()
        {
            if (!EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Open Hole01_SkyIsland and enter Play Mode before previewing Hero VFX.");
        }

        private static T RequireSingle<T>() where T : Object
        {
            T[] values = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(values.Length == 1, $"Expected one {typeof(T).Name}, found {values.Length}.");
            return values[0];
        }

        private static void RequireReferences(Object target, params string[] propertyNames)
        {
            SerializedObject serialized = new(target);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                Require(property != null, $"{target.GetType().Name} is missing serialized field {propertyName}.");
                Require(property.objectReferenceValue != null,
                    $"{target.GetType().Name}.{propertyName} is not assigned.");
            }
        }

        private static Object SerializedReference(Object target, string propertyName)
        {
            return new SerializedObject(target).FindProperty(propertyName)?.objectReferenceValue;
        }

        private static int CountMissingScripts(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            return count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException("VFX HERO VALIDATION FAILED: " + message);
        }
    }
}
