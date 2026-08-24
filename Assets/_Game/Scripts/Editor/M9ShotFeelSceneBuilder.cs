using SwingPop.AudioSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M9ShotFeelSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Foundation.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs/VFX";
        private const string PrefabPath = PrefabFolder + "/ShotFeelPresentation.prefab";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/Presentation";
        private const string TuningPath = DataFolder + "/M9ShotPresentationTuning.asset";
        private const string MaterialFolder = "Assets/_Game/Materials/VFX";

        [MenuItem("SwingPop/M9/Build VFX Audio Shot Feel")]
        public static void BuildShotFeelPresentation()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CharacterAnimationController characterAnimation = Object.FindAnyObjectByType<CharacterAnimationController>();
            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>();
            if (shotFlow == null || ball == null || holeFlow == null || characterAnimation == null || character == null)
            {
                Debug.LogError("M9 builder requires the completed M8 scene with Shot, Ball, Hole, Character, Camera, and HUD.");
                return;
            }

            ShotPresentationTuningData tuning = LoadOrCreateTuning();
            CreateOrReplacePrefab(tuning);

            GameObject existing = FindInScene(scene, "M9 Shot Feel Presentation");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Transform presentation = FindInScene(scene, "Presentation")?.transform;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("M9 builder could not instantiate ShotFeelPresentation.prefab.");
                return;
            }
            instance.name = "M9 Shot Feel Presentation";
            if (presentation != null)
            {
                instance.transform.SetParent(presentation, false);
            }

            ShotPresentationController controller = instance.GetComponent<ShotPresentationController>();
            BallTrailController trail = instance.GetComponentInChildren<BallTrailController>(true);
            GameplayAudioController audio = instance.GetComponentInChildren<GameplayAudioController>(true);
            SetObjectReference(controller, "shotFlow", shotFlow);
            SetObjectReference(controller, "ball", ball);
            SetObjectReference(controller, "holeFlow", holeFlow);
            SetObjectReference(trail, "ball", ball);
            SetObjectReference(audio, "shotFlow", shotFlow);
            SetObjectReference(audio, "ball", ball);
            SetObjectReference(audio, "holeFlow", holeFlow);
            SetObjectReference(audio, "characterAnimation", characterAnimation);
            SetObjectReference(audio, "characterTransform", character.transform);

            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            if (overlay != null)
            {
                SetObjectReference(overlay, "shotPresentation", controller);
                SetObjectReference(overlay, "gameplayAudio", audio);
            }

            GameObject systems = FindInScene(scene, "M8 Gameplay Systems");
            if (systems != null)
            {
                systems.name = "M9 Gameplay Systems";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = instance;
            Debug.Log("SwingPop M9 VFX / Audio / Shot Feel scene wiring completed with reusable Particle Systems, Trail Renderers, and Audio Sources.");
        }

        private static ShotPresentationTuningData LoadOrCreateTuning()
        {
            ShotPresentationTuningData data = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(TuningPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ShotPresentationTuningData>();
                AssetDatabase.CreateAsset(data, TuningPath);
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateOrReplacePrefab(ShotPresentationTuningData tuning)
        {
            Material particleMaterial = LoadOrCreateMaterial(
                "M9ParticlePlaceholder",
                "Universal Render Pipeline/Particles/Unlit");
            Material trailMaterial = LoadOrCreateMaterial(
                "M9TrailPlaceholder",
                "Universal Render Pipeline/Particles/Unlit");
            Material accentTrailMaterial = LoadOrCreateMaterial(
                "M9TrailAccentPlaceholder",
                "Universal Render Pipeline/Particles/Unlit");

            GameObject root = new("M9 Shot Feel Presentation");
            ShotPresentationController controller = root.AddComponent<ShotPresentationController>();

            GameObject impactRoot = new("Impact VFX");
            impactRoot.transform.SetParent(root.transform, false);
            ImpactVfxController impact = impactRoot.AddComponent<ImpactVfxController>();
            ParticleSystem flash = CreateParticle(impactRoot.transform, "Impact Flash", particleMaterial, 0.13f, 0f, 0.4f, 8, ParticleSystemShapeType.Sphere, 0.03f);
            ParticleSystem burst = CreateParticle(impactRoot.transform, "Radial Energy Burst", particleMaterial, 0.38f, 3.2f, 0.08f, 64, ParticleSystemShapeType.Sphere, 0.07f);
            ParticleSystem streak = CreateParticle(impactRoot.transform, "Directional Streak", particleMaterial, 0.24f, 6f, 0.06f, 48, ParticleSystemShapeType.Cone, 0.04f);
            ParticleSystem.ShapeModule streakShape = streak.shape;
            streakShape.angle = 13f;
            SetObjectReference(impact, "tuning", tuning);
            SetObjectReference(impact, "coreFlash", flash);
            SetObjectReference(impact, "radialBurst", burst);
            SetObjectReference(impact, "directionalStreak", streak);

            GameObject trailRoot = new("Gameplay Ball Trail");
            trailRoot.transform.SetParent(root.transform, false);
            BallTrailController trail = trailRoot.AddComponent<BallTrailController>();
            TrailRenderer coreTrail = CreateTrail(trailRoot.transform, "Core Trail", trailMaterial, 0.08f);
            TrailRenderer accentTrail = CreateTrail(trailRoot.transform, "Spin Accent Trail", accentTrailMaterial, 0.04f);
            accentTrail.transform.localPosition = new Vector3(0.025f, 0.015f, 0f);
            SetObjectReference(trail, "tuning", tuning);
            SetObjectReference(trail, "coreTrail", coreTrail);
            SetObjectReference(trail, "accentTrail", accentTrail);

            GameObject landingRoot = new("Landing VFX");
            landingRoot.transform.SetParent(root.transform, false);
            LandingVfxController landing = landingRoot.AddComponent<LandingVfxController>();
            ParticleSystem ground = CreateParticle(landingRoot.transform, "Ground Puff", particleMaterial, 0.52f, 1.8f, 0.1f, 64, ParticleSystemShapeType.Hemisphere, 0.13f);
            ParticleSystem.MainModule groundMain = ground.main;
            groundMain.gravityModifier = 0.35f;
            ParticleSystem splash = CreateParticle(landingRoot.transform, "Water Splash", particleMaterial, 0.68f, 3.4f, 0.16f, 80, ParticleSystemShapeType.Cone, 0.18f);
            ParticleSystem.ShapeModule splashShape = splash.shape;
            splashShape.angle = 48f;
            ParticleSystem.MainModule splashMain = splash.main;
            splashMain.gravityModifier = 0.65f;
            SetObjectReference(landing, "tuning", tuning);
            SetObjectReference(landing, "groundBurst", ground);
            SetObjectReference(landing, "waterSplash", splash);

            GameObject holeRoot = new("Hole In VFX");
            holeRoot.transform.SetParent(root.transform, false);
            HoleInVfxController holeIn = holeRoot.AddComponent<HoleInVfxController>();
            ParticleSystem sparkles = CreateParticle(holeRoot.transform, "Upward Sparkles", particleMaterial, 0.9f, 2.6f, 0.1f, 96, ParticleSystemShapeType.Cone, 0.12f);
            ParticleSystem.ShapeModule sparkleShape = sparkles.shape;
            sparkleShape.angle = 24f;
            ParticleSystem ring = CreateParticle(holeRoot.transform, "Cup Ring Burst", particleMaterial, 0.48f, 2.2f, 0.08f, 64, ParticleSystemShapeType.Circle, 0.06f);
            ParticleSystem.ShapeModule ringShape = ring.shape;
            ringShape.radiusThickness = 0f;
            SetObjectReference(holeIn, "tuning", tuning);
            SetObjectReference(holeIn, "upwardSparkles", sparkles);
            SetObjectReference(holeIn, "ringBurst", ring);

            GameObject audioRoot = new("Gameplay Audio");
            audioRoot.transform.SetParent(root.transform, false);
            GameplayAudioController audio = audioRoot.AddComponent<GameplayAudioController>();
            AudioSource swingSource = CreateAudioSource(audioRoot.transform, "Swing Source", 1f);
            AudioSource impactSource = CreateAudioSource(audioRoot.transform, "Impact Source", 1f);
            AudioSource terrainSource = CreateAudioSource(audioRoot.transform, "Terrain Hazard Hole Source", 1f);
            AudioSource uiSource = CreateAudioSource(audioRoot.transform, "UI Result Source", 0f);
            SetObjectReference(audio, "tuning", tuning);
            SetObjectReference(audio, "swingSource", swingSource);
            SetObjectReference(audio, "impactSource", impactSource);
            SetObjectReference(audio, "terrainSource", terrainSource);
            SetObjectReference(audio, "uiResultSource", uiSource);

            SetObjectReference(controller, "impactVfx", impact);
            SetObjectReference(controller, "ballTrail", trail);
            SetObjectReference(controller, "landingVfx", landing);
            SetObjectReference(controller, "holeInVfx", holeIn);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static ParticleSystem CreateParticle(
            Transform parent,
            string name,
            Material material,
            float lifetime,
            float speed,
            float size,
            int maxParticles,
            ParticleSystemShapeType shapeType,
            float radius)
        {
            GameObject effectObject = new(name, typeof(ParticleSystem));
            effectObject.transform.SetParent(parent, false);
            ParticleSystem effect = effectObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effect.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.maxParticles = maxParticles;
            main.stopAction = ParticleSystemStopAction.None;
            ParticleSystem.EmissionModule emission = effect.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = radius;
            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = 4;
            return effect;
        }

        private static TrailRenderer CreateTrail(Transform parent, string name, Material material, float width)
        {
            GameObject trailObject = new(name, typeof(TrailRenderer));
            trailObject.transform.SetParent(parent, false);
            TrailRenderer trail = trailObject.GetComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.4f;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.08f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.Clear();
            return trail;
        }

        private static AudioSource CreateAudioSource(Transform parent, string name, float spatialBlend)
        {
            GameObject sourceObject = new(name);
            sourceObject.transform.SetParent(parent, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 55f;
            return source;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            }
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindRecursive(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform match = FindRecursive(parent.GetChild(index), name);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} is missing serialized property '{propertyName}'.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
