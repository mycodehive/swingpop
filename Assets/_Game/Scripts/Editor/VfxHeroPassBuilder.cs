using System.IO;
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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class VfxHeroPassBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PrefabPath = "Assets/_Game/Prefabs/VFX/ShotFeelPresentation_Hero.prefab";
        private const string TuningPath = "Assets/_Game/ScriptableObjects/Presentation/VfxHeroShotPresentationTuning.asset";
        private const string ArtFolder = "Assets/_Game/Art/VFX/HeroPass";
        private const string MaterialFolder = "Assets/_Game/Materials/VFX/HeroPass";
        private const string PresentationRootName = "VFX Hero Presentation";
        private const int TextureSize = 128;

        [MenuItem("SwingPop/VFX/Build Hero VFX Pass")]
        public static void BuildHeroVfxPass()
        {
            EnsureFolder("Assets/_Game/Art/VFX");
            EnsureFolder(ArtFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Game/ScriptableObjects/Presentation");
            EnsureFolder("Assets/_Game/Prefabs/VFX");

            GenerateSpriteFamily();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            MaterialSet materials = CreateMaterials();
            ShotPresentationTuningData tuning = LoadOrCreateTuning();
            CreateHeroPrefab(tuning, materials);
            WireHole01Scene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VFX HERO PASS BUILD COMPLETE | Hole01 uses reusable layered impact/trail/landing/water/hole VFX. Foundation unchanged.");
        }

        private static void GenerateSpriteFamily()
        {
            WriteSprite("VFX_SoftGlow", DrawSoftGlow());
            WriteSprite("VFX_Streak", DrawStreak());
            WriteSprite("VFX_Ring", DrawRing());
            WriteSprite("VFX_Sparkle", DrawSparkle());
            WriteSprite("VFX_Dust", DrawDust());
            WriteSprite("VFX_Splash", DrawSplash());
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet(
                LoadOrCreateMaterial("HeroSoftGlow_Add", "VFX_SoftGlow", true),
                LoadOrCreateMaterial("HeroStreak_Add", "VFX_Streak", true),
                LoadOrCreateMaterial("HeroRing_Add", "VFX_Ring", true),
                LoadOrCreateMaterial("HeroSparkle_Add", "VFX_Sparkle", true),
                LoadOrCreateMaterial("HeroDust_Alpha", "VFX_Dust", false),
                LoadOrCreateMaterial("HeroSplash_Alpha", "VFX_Splash", false));
        }

        private static ShotPresentationTuningData LoadOrCreateTuning()
        {
            ShotPresentationTuningData tuning = AssetDatabase.LoadAssetAtPath<ShotPresentationTuningData>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<ShotPresentationTuningData>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }

            SerializedObject serialized = new(tuning);
            SetColor(serialized, "normalImpactColor", new Color(0.28f, 0.9f, 1f, 1f));
            SetColor(serialized, "greatImpactColor", new Color(0.5f, 1f, 0.86f, 1f));
            SetColor(serialized, "perfectImpactColor", new Color(0.82f, 1f, 1f, 1f));
            SetColor(serialized, "normalImpactAccentColor", new Color(0.2f, 0.65f, 1f, 1f));
            SetColor(serialized, "greatImpactAccentColor", new Color(0.96f, 0.86f, 0.38f, 1f));
            SetColor(serialized, "perfectImpactAccentColor", new Color(1f, 0.74f, 0.14f, 1f));
            SetFloat(serialized, "normalImpactScale", 0.72f);
            SetFloat(serialized, "greatImpactScale", 1.08f);
            SetFloat(serialized, "perfectImpactScale", 1.55f);
            SetInt(serialized, "normalImpactParticles", 9);
            SetInt(serialized, "greatImpactParticles", 15);
            SetInt(serialized, "perfectImpactParticles", 24);
            SetFloat(serialized, "putterImpactScaleMultiplier", 0.28f);
            SetFloat(serialized, "putterImpactParticleMultiplier", 0.22f);
            SetFloat(serialized, "coreFlashSizeMultiplier", 0.58f);
            SetFloat(serialized, "radialRingSizeMultiplier", 1.05f);
            SetFloat(serialized, "radialBurstSizeMultiplier", 0.1f);
            SetFloat(serialized, "directionalStreakSizeMultiplier", 0.085f);
            SetFloat(serialized, "accentSparkSizeMultiplier", 0.06f);
            SetFloat(serialized, "directionalParticleRatio", 0.45f);
            SetFloat(serialized, "accentParticleRatio", 0.35f);

            SetFloat(serialized, "normalTrailTime", 0.42f);
            SetFloat(serialized, "normalTrailWidth", 0.08f);
            SetFloat(serialized, "greatTrailTime", 0.53f);
            SetFloat(serialized, "greatTrailWidth", 0.105f);
            SetFloat(serialized, "perfectTrailTime", 0.65f);
            SetFloat(serialized, "perfectTrailWidth", 0.13f);
            SetColor(serialized, "normalTrailColor", new Color(0.2f, 0.88f, 1f, 0.88f));
            SetColor(serialized, "greatTrailColor", new Color(0.48f, 1f, 0.88f, 0.92f));
            SetColor(serialized, "perfectTrailColor", new Color(0.72f, 1f, 1f, 0.96f));
            SetColor(serialized, "perfectTrailAccentColor", new Color(1f, 0.8f, 0.25f, 0.9f));
            SetFloat(serialized, "trailCoreWidthMultiplier", 0.42f);
            SetFloat(serialized, "trailSpinWidthMultiplier", 0.24f);
            SetFloat(serialized, "trailOuterAlpha", 0.4f);
            SetFloat(serialized, "trailCoreAlpha", 0.96f);
            SetFloat(serialized, "trailSpinAlpha", 0.54f);
            SetFloat(serialized, "minimumTrailSpeed", 0.45f);
            SetFloat(serialized, "speedStreakMinimumSpeed", 18f);
            SetFloat(serialized, "speedStreakEmissionRate", 14f);

            SetInt(serialized, "grassParticleCount", 9);
            SetInt(serialized, "roughParticleCount", 14);
            SetInt(serialized, "sandParticleCount", 24);
            SetInt(serialized, "waterParticleCount", 30);
            SetColor(serialized, "grassColor", new Color(0.48f, 0.94f, 0.42f, 0.9f));
            SetColor(serialized, "roughColor", new Color(0.2f, 0.58f, 0.18f, 0.92f));
            SetColor(serialized, "sandColor", new Color(1f, 0.76f, 0.3f, 0.94f));
            SetColor(serialized, "waterColor", new Color(0.2f, 0.82f, 1f, 0.94f));
            SetFloat(serialized, "grassLandingSize", 0.12f);
            SetFloat(serialized, "roughLandingSize", 0.16f);
            SetFloat(serialized, "sandLandingSize", 0.28f);
            SetFloat(serialized, "waterLandingSize", 0.3f);
            SetFloat(serialized, "secondaryBounceIntensity", 0.28f);
            SetFloat(serialized, "minimumSecondaryBounceSpeed", 3.2f);

            SetInt(serialized, "holeSparkleCount", 22);
            SetFloat(serialized, "subduedCelebrationScale", 0.72f);
            SetFloat(serialized, "normalCelebrationScale", 1f);
            SetFloat(serialized, "strongCelebrationScale", 1.28f);
            SetFloat(serialized, "strongestCelebrationScale", 1.55f);
            SetFloat(serialized, "perfectAccentVolume", 0.64f);
            SetFloat(serialized, "greatImpactVolumeMultiplier", 1.08f);
            SetFloat(serialized, "putterImpactVolumeMultiplier", 0.34f);
            SetFloat(serialized, "terrainVolume", 0.46f);
            SetFloat(serialized, "hazardVolume", 0.64f);
            SetFloat(serialized, "holeVolume", 0.72f);
            SetFloat(serialized, "resultVolume", 0.58f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);
            return tuning;
        }

        private static void CreateHeroPrefab(ShotPresentationTuningData tuning, MaterialSet materials)
        {
            GameObject root = new(PresentationRootName);
            ShotPresentationController controller = root.AddComponent<ShotPresentationController>();

            GameObject impactRoot = CreateChild(root.transform, "Impact Hero VFX");
            ImpactVfxController impact = impactRoot.AddComponent<ImpactVfxController>();
            ParticleSystem coreFlash = CreateParticle(impactRoot.transform, "Core Flash", materials.Glow,
                0.11f, 0f, 0.5f, 4, ParticleSystemShapeType.Sphere, 0.01f);
            ConfigurePulse(coreFlash, 0.55f, 1.1f);
            ParticleSystem radialRing = CreateParticle(impactRoot.transform, "Radial Ring", materials.Ring,
                0.27f, 0f, 0.8f, 4, ParticleSystemShapeType.Circle, 0.01f);
            ConfigurePulse(radialRing, 0.18f, 1.18f);
            ParticleSystem radialBurst = CreateParticle(impactRoot.transform, "Radial Burst", materials.Sparkle,
                0.32f, 3.4f, 0.1f, 64, ParticleSystemShapeType.Sphere, 0.025f);
            ParticleSystem directional = CreateParticle(impactRoot.transform, "Directional Streak", materials.Streak,
                0.25f, 7.8f, 0.08f, 64, ParticleSystemShapeType.Cone, 0.025f);
            ParticleSystem.ShapeModule directionalShape = directional.shape;
            directionalShape.angle = 10f;
            ConfigureStretchRenderer(directional, 0.46f, 0.08f);
            ParticleSystem accents = CreateParticle(impactRoot.transform, "Accent Sparkles", materials.Sparkle,
                0.42f, 2.4f, 0.06f, 48, ParticleSystemShapeType.Sphere, 0.035f);
            SetObjectReference(impact, "tuning", tuning);
            SetObjectReference(impact, "coreFlash", coreFlash);
            SetObjectReference(impact, "radialRing", radialRing);
            SetObjectReference(impact, "radialBurst", radialBurst);
            SetObjectReference(impact, "directionalStreak", directional);
            SetObjectReference(impact, "accentSparkles", accents);

            GameObject trailRoot = CreateChild(root.transform, "Hero Ball Trail");
            BallTrailController trail = trailRoot.AddComponent<BallTrailController>();
            TrailRenderer outerTrail = CreateTrail(trailRoot.transform, "Soft Outer Trail", materials.Glow, 2);
            TrailRenderer coreTrail = CreateTrail(trailRoot.transform, "Bright Core Trail", materials.Streak, 4);
            TrailRenderer spinTrail = CreateTrail(trailRoot.transform, "Spin Accent Trail", materials.Streak, 5);
            spinTrail.transform.localPosition = new Vector3(0.018f, 0.012f, 0f);
            ParticleSystem speedStreaks = CreateParticle(trailRoot.transform, "Speed Streaks", materials.Streak,
                0.18f, -1.5f, 0.05f, 36, ParticleSystemShapeType.Cone, 0.035f);
            ParticleSystem.MainModule speedMain = speedStreaks.main;
            speedMain.loop = true;
            ParticleSystem.ShapeModule speedShape = speedStreaks.shape;
            speedShape.angle = 8f;
            ParticleSystem.EmissionModule speedEmission = speedStreaks.emission;
            speedEmission.enabled = true;
            speedEmission.rateOverTime = 0f;
            ConfigureStretchRenderer(speedStreaks, 0.32f, 0.05f);
            SetObjectReference(trail, "tuning", tuning);
            SetObjectReference(trail, "outerTrail", outerTrail);
            SetObjectReference(trail, "coreTrail", coreTrail);
            SetObjectReference(trail, "accentTrail", spinTrail);
            SetObjectReference(trail, "speedStreaks", speedStreaks);

            GameObject landingRoot = CreateChild(root.transform, "Surface Landing VFX");
            LandingVfxController landing = landingRoot.AddComponent<LandingVfxController>();
            ParticleSystem ground = CreateParticle(landingRoot.transform, "Grass Sand Puff", materials.Dust,
                0.5f, 1.7f, 0.14f, 72, ParticleSystemShapeType.Hemisphere, 0.12f);
            ground.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.MainModule groundMain = ground.main;
            groundMain.gravityModifier = 0.32f;
            ParticleSystem groundRing = CreateParticle(landingRoot.transform, "Ground Contact Ring", materials.Ring,
                0.38f, 0f, 0.45f, 4, ParticleSystemShapeType.Circle, 0.01f);
            groundRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ConfigurePulse(groundRing, 0.2f, 1.15f);
            ParticleSystem splash = CreateParticle(landingRoot.transform, "Vertical Water Splash", materials.Splash,
                0.66f, 3.3f, 0.22f, 96, ParticleSystemShapeType.Cone, 0.14f);
            splash.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.ShapeModule splashShape = splash.shape;
            splashShape.angle = 30f;
            ParticleSystem.MainModule splashMain = splash.main;
            splashMain.gravityModifier = 0.72f;
            ParticleSystem waterRing = CreateParticle(landingRoot.transform, "Water Outward Ring", materials.Ring,
                0.52f, 0f, 0.55f, 4, ParticleSystemShapeType.Circle, 0.01f);
            waterRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ConfigurePulse(waterRing, 0.16f, 1.28f);
            ParticleSystem droplets = CreateParticle(landingRoot.transform, "Water Droplets", materials.Glow,
                0.72f, 2.7f, 0.07f, 48, ParticleSystemShapeType.Cone, 0.1f);
            droplets.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.ShapeModule dropletShape = droplets.shape;
            dropletShape.angle = 42f;
            ParticleSystem.MainModule dropletMain = droplets.main;
            dropletMain.gravityModifier = 0.82f;
            SetObjectReference(landing, "tuning", tuning);
            SetObjectReference(landing, "groundBurst", ground);
            SetObjectReference(landing, "groundRing", groundRing);
            SetObjectReference(landing, "waterSplash", splash);
            SetObjectReference(landing, "waterRing", waterRing);
            SetObjectReference(landing, "waterDroplets", droplets);

            GameObject holeRoot = CreateChild(root.transform, "Hole In Hero VFX");
            HoleInVfxController holeIn = holeRoot.AddComponent<HoleInVfxController>();
            ParticleSystem cupFlash = CreateParticle(holeRoot.transform, "Cup Flash", materials.Glow,
                0.16f, 0f, 0.42f, 4, ParticleSystemShapeType.Sphere, 0.01f);
            ConfigurePulse(cupFlash, 0.55f, 1.12f);
            ParticleSystem upward = CreateParticle(holeRoot.transform, "Upward Sparkles", materials.Sparkle,
                0.92f, 2.7f, 0.1f, 96, ParticleSystemShapeType.Cone, 0.11f);
            upward.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.ShapeModule upwardShape = upward.shape;
            upwardShape.angle = 23f;
            ParticleSystem ring = CreateParticle(holeRoot.transform, "Cup Ring", materials.Ring,
                0.55f, 0f, 0.45f, 24, ParticleSystemShapeType.Circle, 0.01f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ConfigurePulse(ring, 0.16f, 1.25f);
            ParticleSystem celebration = CreateParticle(holeRoot.transform, "Celebration Sparkles", materials.Sparkle,
                1.05f, 1.45f, 0.075f, 64, ParticleSystemShapeType.Sphere, 0.32f);
            ParticleSystem.MainModule celebrationMain = celebration.main;
            celebrationMain.gravityModifier = -0.04f;
            SetObjectReference(holeIn, "tuning", tuning);
            SetObjectReference(holeIn, "cupFlash", cupFlash);
            SetObjectReference(holeIn, "upwardSparkles", upward);
            SetObjectReference(holeIn, "ringBurst", ring);
            SetObjectReference(holeIn, "celebrationSparkles", celebration);

            GameObject audioRoot = CreateChild(root.transform, "Gameplay Audio");
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

        private static void WireHole01Scene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CharacterAnimationController characterAnimation = Object.FindAnyObjectByType<CharacterAnimationController>();
            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>();
            if (shotFlow == null || ball == null || holeFlow == null || characterAnimation == null || character == null)
            {
                throw new System.InvalidOperationException("VFX Hero builder requires the completed Hole01 gameplay graph.");
            }

            ShotPresentationController existing = Object.FindAnyObjectByType<ShotPresentationController>();
            Transform parent = existing != null ? existing.transform.parent : FindInScene(scene, "Presentation")?.transform;
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("VFX Hero prefab could not be instantiated.");
            }
            instance.name = PresentationRootName;
            if (parent != null) instance.transform.SetParent(parent, false);

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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = instance;
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
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 6;
            return effect;
        }

        private static TrailRenderer CreateTrail(Transform parent, string name, Material material, int sortingOrder)
        {
            GameObject trailObject = new(name, typeof(TrailRenderer));
            trailObject.transform.SetParent(parent, false);
            TrailRenderer trail = trailObject.GetComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.4f;
            trail.widthMultiplier = 0.08f;
            trail.minVertexDistance = 0.05f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = sortingOrder;
            trail.emitting = false;
            trail.Clear();
            return trail;
        }

        private static void ConfigurePulse(ParticleSystem effect, float startScale, float endScale)
        {
            ParticleSystem.SizeOverLifetimeModule size = effect.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, startScale), new Keyframe(1f, endScale)));
            ParticleSystem.ColorOverLifetimeModule color = effect.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.72f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = fade;
        }

        private static void ConfigureStretchRenderer(ParticleSystem effect, float lengthScale, float velocityScale)
        {
            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = lengthScale;
            renderer.velocityScale = velocityScale;
        }

        private static AudioSource CreateAudioSource(Transform parent, string name, float spatialBlend)
        {
            GameObject sourceObject = CreateChild(parent, name);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 75f;
            return source;
        }

        private static Material LoadOrCreateMaterial(string materialName, string spriteName, bool additive)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) throw new System.InvalidOperationException("URP Particles/Unlit shader was not found.");
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/{spriteName}.png");
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void WriteSprite(string name, Color32[] pixels)
        {
            string assetPath = $"{ArtFolder}/{name}.png";
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = TextureSize;
            importer.SaveAndReimport();
        }

        private static Color32[] DrawSoftGlow()
        {
            return Draw((x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
            });
        }

        private static Color32[] DrawRing()
        {
            return Draw((x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float band = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.68f) / 0.09f);
                return band * Mathf.Clamp01((0.98f - d) / 0.15f);
            });
        }

        private static Color32[] DrawStreak()
        {
            return Draw((x, y) =>
            {
                float horizontal = Mathf.Clamp01((x + 1f) * 0.5f);
                float vertical = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) * 3.1f), 1.7f);
                float head = Mathf.Clamp01((1f - x) * 2.2f);
                return Mathf.Pow(horizontal, 1.35f) * vertical * head;
            });
        }

        private static Color32[] DrawSparkle()
        {
            return Draw((x, y) =>
            {
                float vertical = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x) * 4.8f), 2f)
                                 * Mathf.Clamp01(1f - Mathf.Abs(y));
                float horizontal = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y) * 4.8f), 2f)
                                   * Mathf.Clamp01(1f - Mathf.Abs(x));
                float core = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 5f), 1.4f);
                return Mathf.Clamp01(vertical + horizontal + core);
            });
        }

        private static Color32[] DrawDust()
        {
            return Draw((x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float wobble = 0.08f * Mathf.Sin((x * 7f + y * 11f) * Mathf.PI);
                return Mathf.Pow(Mathf.Clamp01(1f - (d + wobble)), 1.7f) * 0.82f;
            });
        }

        private static Color32[] DrawSplash()
        {
            return Draw((x, y) =>
            {
                float shiftedY = y + 0.18f;
                float width = Mathf.Lerp(0.22f, 0.7f, Mathf.Clamp01((shiftedY + 0.8f) * 0.62f));
                float body = Mathf.Clamp01(1f - Mathf.Abs(x) / width)
                             * Mathf.Clamp01(1f - Mathf.Abs(shiftedY) / 0.92f);
                float tip = Mathf.Clamp01((0.98f - y) * 3f);
                return Mathf.Pow(body * tip, 1.35f);
            });
        }

        private static Color32[] Draw(System.Func<float, float, float> alpha)
        {
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = ((x + 0.5f) / TextureSize) * 2f - 1f;
                    float ny = ((y + 0.5f) / TextureSize) * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(nx, ny)) * 255f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, a);
                }
            }
            return pixels;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindRecursive(root.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new System.InvalidOperationException($"{target.GetType().Name} is missing '{propertyName}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name)
                ?? throw new System.InvalidOperationException($"Tuning is missing '{name}'.");
            property.floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name)
                ?? throw new System.InvalidOperationException($"Tuning is missing '{name}'.");
            property.intValue = value;
        }

        private static void SetColor(SerializedObject serialized, string name, Color value)
        {
            SerializedProperty property = serialized.FindProperty(name)
                ?? throw new System.InvalidOperationException($"Tuning is missing '{name}'.");
            property.colorValue = value;
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(Material glow, Material streak, Material ring, Material sparkle, Material dust, Material splash)
            {
                Glow = glow;
                Streak = streak;
                Ring = ring;
                Sparkle = sparkle;
                Dust = dust;
                Splash = splash;
            }

            public Material Glow { get; }
            public Material Streak { get; }
            public Material Ring { get; }
            public Material Sparkle { get; }
            public Material Dust { get; }
            public Material Splash { get; }
        }
    }
}
