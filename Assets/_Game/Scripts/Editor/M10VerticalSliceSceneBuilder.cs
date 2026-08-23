using System.Collections.Generic;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Course;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using SwingPop.AudioSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M10VerticalSliceSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string FoundationPath = "Assets/_Game/Scenes/Foundation.unity";
        private const string MaterialFolder = "Assets/_Game/Materials/Environment";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Environment";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/Environment";
        private const string CameraSourcePath = "Assets/_Game/ScriptableObjects/Camera/M6CameraTuning.asset";
        private const string HudSourcePath = "Assets/_Game/ScriptableObjects/UI/M8HudTuning.asset";
        private const string PresentationSourcePath = "Assets/_Game/ScriptableObjects/Presentation/M9ShotPresentationTuning.asset";

        [MenuItem("SwingPop/M10/Build Sky Island Vertical Slice")]
        public static void BuildVerticalSlice()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);

            Scene scene = EditorSceneManager.OpenScene(FoundationPath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, true))
            {
                Debug.LogError("M10 builder could not create Hole01_SkyIsland.unity.");
                return;
            }

            scene = SceneManager.GetActiveScene();
            MaterialPalette palette = CreatePalette();
            Dictionary<string, GameObject> prefabs = CreateEnvironmentPrefabs(palette);
            ConfigureGameplayCourse(scene, palette);
            BuildEnvironment(scene, palette, prefabs);
            ConfigureScenePresentation(scene, palette);
            ConfigureLighting(scene, palette);
            ConfigureBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SwingPop M10 Hole01 Sky Island vertical slice scene built. Foundation.unity remains unchanged.");
        }

        private static void ConfigureGameplayCourse(Scene scene, MaterialPalette palette)
        {
            GameObject course = FindInScene(scene, "M5 Hole 1 Placeholder Course");
            if (course == null)
            {
                Debug.LogError("M10 builder requires the completed M9 Foundation scene.");
                return;
            }

            course.name = "M10 Hole 1 Gameplay Course";
            foreach (TerrainSurface surface in course.GetComponentsInChildren<TerrainSurface>(true))
            {
                Renderer renderer = surface.GetComponent<Renderer>();
                if (renderer == null)
                {
                    renderer = surface.GetComponentInChildren<Renderer>(true);
                }

                if (renderer != null)
                {
                    renderer.sharedMaterial = surface.SurfaceType switch
                    {
                        TerrainSurfaceType.Tee => palette.Tee,
                        TerrainSurfaceType.Fairway => palette.Fairway,
                        TerrainSurfaceType.Rough => palette.Rough,
                        TerrainSurfaceType.Bunker => palette.Sand,
                        TerrainSurfaceType.Green => palette.Green,
                        TerrainSurfaceType.Water => palette.Water,
                        TerrainSurfaceType.OutOfBounds => palette.Cliff,
                        _ => renderer.sharedMaterial
                    };
                }

                if (surface.SurfaceType == TerrainSurfaceType.OutOfBounds && renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            GameObject cup = FindInScene(scene, "Cup Target");
            if (cup != null)
            {
                foreach (Renderer renderer in cup.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = renderer.name == "Flag" ? palette.Flag : palette.Pole;
                }
            }

            GameObject visualRoot = new("Course Visual Layers");
            visualRoot.transform.SetParent(course.transform, false);
            CreatePrimitive(visualRoot.transform, "Fairway Highlight A", PrimitiveType.Cube,
                new Vector3(-2.5f, 0.012f, 19f), new Vector3(13f, 0.02f, 19f), palette.FairwayHighlight);
            CreatePrimitive(visualRoot.transform, "Fairway Highlight B", PrimitiveType.Cube,
                new Vector3(2.3f, 0.014f, 42f), new Vector3(14f, 0.02f, 19f), palette.FairwayHighlight);
            CreatePrimitive(visualRoot.transform, "Green Inner", PrimitiveType.Cylinder,
                new Vector3(0f, 0.018f, 72f), new Vector3(9.4f, 0.012f, 8.1f), palette.GreenHighlight);
        }

        private static void BuildEnvironment(Scene scene, MaterialPalette palette, Dictionary<string, GameObject> prefabs)
        {
            GameObject environment = FindRoot(scene, "Environment") ?? new GameObject("Environment");
            GameObject existing = FindInScene(scene, "M10 Sky Island Art");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject art = new("M10 Sky Island Art");
            art.transform.SetParent(environment.transform, false);

            Transform island = new GameObject("Main Floating Island Silhouette").transform;
            island.SetParent(art.transform, false);
            CreatePrimitive(island, "Grass Rim", PrimitiveType.Cube, new Vector3(0f, -0.72f, 38f), new Vector3(48f, 1.25f, 88f), palette.Rough);
            CreatePrimitive(island, "Stone Core", PrimitiveType.Cube, new Vector3(0f, -4f, 38f), new Vector3(44f, 6f, 84f), palette.Cliff);
            CreatePrimitive(island, "Tapered Underside", PrimitiveType.Sphere, new Vector3(0f, -15f, 38f), new Vector3(41f, 24f, 76f), palette.CliffDark);
            AddRock(island, new Vector3(-20f, -6f, 12f), new Vector3(7f, 12f, 10f), palette.CliffLight);
            AddRock(island, new Vector3(19f, -7f, 49f), new Vector3(9f, 15f, 13f), palette.CliffLight);
            AddRock(island, new Vector3(-16f, -8f, 71f), new Vector3(8f, 17f, 11f), palette.CliffDark);

            Vector3[] treePositions =
            {
                new(-13f, 0f, 7f), new(12f, 0f, 12f), new(-14f, 0f, 23f), new(14f, 0f, 31f),
                new(-15f, 0f, 50f), new(14f, 0f, 55f), new(-12f, 0f, 69f), new(12f, 0f, 78f)
            };
            for (int index = 0; index < treePositions.Length; index++)
            {
                GameObject tree = InstantiatePrefab(prefabs["Tree"], art.transform, $"Stylized Tree {index + 1:00}", treePositions[index]);
                tree.transform.localScale = Vector3.one * (index % 3 == 0 ? 1.25f : 1f);
                tree.transform.localRotation = Quaternion.Euler(0f, index * 37f, 0f);
            }

            Vector3[] flowerPositions =
            {
                new(-7f, 0.08f, 5f), new(7f, 0.08f, 17f), new(-8f, 0.08f, 31f),
                new(9f, 0.08f, 49f), new(-8f, 0.08f, 63f), new(7f, 0.08f, 76f)
            };
            for (int index = 0; index < flowerPositions.Length; index++)
            {
                InstantiatePrefab(prefabs["Flowers"], art.transform, $"Flower Patch {index + 1:00}", flowerPositions[index]);
            }

            GameObject windmill = InstantiatePrefab(prefabs["Windmill"], art.transform, "Windmill Landmark", new Vector3(17f, 0f, 68f));
            windmill.transform.localScale = Vector3.one * 1.35f;
            windmill.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);

            Transform distantRoot = new GameObject("Distant Floating Islands").transform;
            distantRoot.SetParent(art.transform, false);
            Vector3[] islandPositions =
            {
                new(-58f, -8f, 32f), new(55f, -3f, 58f), new(-42f, 3f, 105f), new(45f, -12f, 126f)
            };
            for (int index = 0; index < islandPositions.Length; index++)
            {
                GameObject distant = InstantiatePrefab(prefabs["Island"], distantRoot, $"Distant Island {index + 1:00}", islandPositions[index]);
                distant.transform.localScale = Vector3.one * (index == 2 ? 1.45f : 1f);
            }

            GameObject waterfallIsland = InstantiatePrefab(prefabs["Island"], distantRoot, "Waterfall Island Landmark", new Vector3(-35f, 7f, 91f));
            waterfallIsland.transform.localScale = new Vector3(1.5f, 1.25f, 1.5f);
            CreatePrimitive(waterfallIsland.transform, "Waterfall", PrimitiveType.Cube,
                new Vector3(0f, -9f, -5.6f), new Vector3(3.3f, 20f, 0.32f), palette.Waterfall);

            Transform cloudRoot = new GameObject("Drifting Clouds").transform;
            cloudRoot.SetParent(art.transform, false);
            Vector3[] cloudPositions =
            {
                new(-34f, 20f, 14f), new(28f, 26f, 42f), new(-26f, 31f, 74f),
                new(36f, 18f, 104f), new(-5f, 38f, 124f)
            };
            Transform[] clouds = new Transform[cloudPositions.Length];
            for (int index = 0; index < cloudPositions.Length; index++)
            {
                GameObject cloud = InstantiatePrefab(prefabs["Cloud"], cloudRoot, $"Cloud Cluster {index + 1:00}", cloudPositions[index]);
                cloud.transform.localScale = Vector3.one * (0.85f + index * 0.12f);
                clouds[index] = cloud.transform;
            }

            SkyIslandEnvironmentTuningData tuning = LoadOrCreateEnvironmentTuning();
            SkyIslandEnvironmentMotion motion = art.AddComponent<SkyIslandEnvironmentMotion>();
            SetObjectReference(motion, "tuning", tuning);
            SetObjectReference(motion, "windmillRotor", FindRecursive(windmill.transform, "Rotor"));
            SetObjectArray(motion, "driftingClouds", clouds);

            GameObject ambienceObject = new("Sky Island Ambience", typeof(AudioSource), typeof(SkyIslandAmbienceController));
            ambienceObject.transform.SetParent(art.transform, false);
            AudioSource ambienceSource = ambienceObject.GetComponent<AudioSource>();
            SkyIslandAmbienceController ambience = ambienceObject.GetComponent<SkyIslandAmbienceController>();
            SetObjectReference(ambience, "tuning", tuning);
            SetObjectReference(ambience, "ambientSource", ambienceSource);
        }

        private static void ConfigureScenePresentation(Scene scene, MaterialPalette palette)
        {
            CameraTuningData cameraTuning = CloneAsset<CameraTuningData>(CameraSourcePath, DataFolder + "/M10CameraTuning.asset");
            HudTuningData hudTuning = CloneAsset<HudTuningData>(HudSourcePath, DataFolder + "/M10HudTuning.asset");
            ShotPresentationTuningData shotTuning = CloneAsset<ShotPresentationTuningData>(PresentationSourcePath, DataFolder + "/M10ShotPresentationTuning.asset");
            TuneCamera(cameraTuning);
            TuneHud(hudTuning);
            TuneShotPresentation(shotTuning);

            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            SetObjectReference(cameraDirector, "tuning", cameraTuning);
            SetObjectReference(hud, "tuning", hudTuning);
            GameObject keyboardHint = FindInScene(scene, "Keyboard Hint");
            Text keyboardHintText = keyboardHint != null ? keyboardHint.GetComponent<Text>() : null;
            if (keyboardHintText != null)
            {
                SerializedObject serializedHint = new(keyboardHintText);
                serializedHint.FindProperty("m_Text").stringValue = "A/D AIM   SPACE / CLICK SHOT   1-5 SPIN";
                serializedHint.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(keyboardHintText);
            }
            foreach (ImpactVfxController component in Object.FindObjectsByType<ImpactVfxController>(FindObjectsInactive.Include))
            {
                SetObjectReference(component, "tuning", shotTuning);
            }
            foreach (BallTrailController component in Object.FindObjectsByType<BallTrailController>(FindObjectsInactive.Include))
            {
                SetObjectReference(component, "tuning", shotTuning);
            }
            foreach (LandingVfxController component in Object.FindObjectsByType<LandingVfxController>(FindObjectsInactive.Include))
            {
                SetObjectReference(component, "tuning", shotTuning);
            }
            foreach (HoleInVfxController component in Object.FindObjectsByType<HoleInVfxController>(FindObjectsInactive.Include))
            {
                SetObjectReference(component, "tuning", shotTuning);
            }
            foreach (GameplayAudioController component in Object.FindObjectsByType<GameplayAudioController>(FindObjectsInactive.Include))
            {
                SetObjectReference(component, "tuning", shotTuning);
            }

            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            BallTrajectoryDebug trajectory = Object.FindAnyObjectByType<BallTrajectoryDebug>();
            SetObjectReference(overlay, "trajectoryDebug", trajectory);
            SetBoolean(overlay, "startHidden", true);
            SetBoolean(overlay, "syncTrajectoryVisibility", true);
            SetBoolean(overlay, "showOverlay", false);
            if (trajectory != null)
            {
                trajectory.SetTrajectoryVisible(false);
                EditorUtility.SetDirty(trajectory);
            }

            GameObject character = FindInScene(scene, "Placeholder Golfer");
            if (character != null)
            {
                foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>(true))
                {
                    string lower = renderer.name.ToLowerInvariant();
                    if (lower.Contains("hair") || lower.Contains("shoe")) renderer.sharedMaterial = palette.CharacterDark;
                    else if (lower.Contains("club")) renderer.sharedMaterial = palette.Pole;
                    else if (lower.Contains("head") || lower.Contains("hand")) renderer.sharedMaterial = palette.CharacterSkin;
                    else if (lower.Contains("accent") || lower.Contains("hat")) renderer.sharedMaterial = palette.CharacterAccent;
                    else renderer.sharedMaterial = palette.CharacterOutfit;
                }
            }

            GameObject systems = FindInScene(scene, "M9 Gameplay Systems");
            if (systems != null)
            {
                systems.name = "M10 Vertical Slice Gameplay Systems";
            }
        }

        private static void ConfigureLighting(Scene scene, MaterialPalette palette)
        {
            Light sunlight = Object.FindAnyObjectByType<Light>();
            if (sunlight != null)
            {
                sunlight.color = new Color(1f, 0.94f, 0.82f);
                sunlight.intensity = 1.35f;
                sunlight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                sunlight.shadows = LightShadows.Soft;
            }

            Material skybox = LoadOrCreateSkybox();
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.82f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.67f, 0.75f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.28f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.58f, 0.83f, 0.96f);
            RenderSettings.fogStartDistance = 85f;
            RenderSettings.fogEndDistance = 185f;

            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(0.45f, 0.75f, 1f);
                camera.farClipPlane = 260f;
            }
        }

        private static Dictionary<string, GameObject> CreateEnvironmentPrefabs(MaterialPalette p)
        {
            Dictionary<string, GameObject> result = new();

            GameObject tree = new("StylizedTree");
            CreatePrimitive(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.35f, 0f), new Vector3(0.55f, 1.35f, 0.55f), p.Trunk);
            CreatePrimitive(tree.transform, "Canopy A", PrimitiveType.Sphere, new Vector3(0f, 3.5f, 0f), new Vector3(2.7f, 2.2f, 2.7f), p.Foliage);
            CreatePrimitive(tree.transform, "Canopy B", PrimitiveType.Sphere, new Vector3(-1.1f, 3.1f, 0.25f), new Vector3(1.9f, 1.7f, 1.9f), p.FoliageLight);
            CreatePrimitive(tree.transform, "Canopy C", PrimitiveType.Sphere, new Vector3(1.1f, 3.2f, -0.15f), new Vector3(1.8f, 1.6f, 1.8f), p.Foliage);
            result["Tree"] = SavePrefab(tree, PrefabFolder + "/StylizedTree.prefab");

            GameObject flowers = new("FlowerPatch");
            for (int index = 0; index < 7; index++)
            {
                float angle = index * Mathf.PI * 2f / 7f;
                Material material = index % 2 == 0 ? p.FlowerPink : p.FlowerGold;
                CreatePrimitive(flowers.transform, $"Flower {index + 1}", PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * 0.65f, 0.18f, Mathf.Sin(angle) * 0.65f), new Vector3(0.22f, 0.3f, 0.22f), material);
            }
            result["Flowers"] = SavePrefab(flowers, PrefabFolder + "/FlowerPatch.prefab");

            GameObject cloud = new("CloudCluster");
            CreatePrimitive(cloud.transform, "Cloud A", PrimitiveType.Sphere, Vector3.zero, new Vector3(7f, 2.2f, 3f), p.Cloud);
            CreatePrimitive(cloud.transform, "Cloud B", PrimitiveType.Sphere, new Vector3(-2.8f, 0.8f, 0f), new Vector3(3.8f, 3f, 2.8f), p.Cloud);
            CreatePrimitive(cloud.transform, "Cloud C", PrimitiveType.Sphere, new Vector3(2.4f, 0.65f, 0.2f), new Vector3(4.3f, 3.2f, 3f), p.Cloud);
            result["Cloud"] = SavePrefab(cloud, PrefabFolder + "/CloudCluster.prefab");

            GameObject island = new("FloatingIsland");
            CreatePrimitive(island.transform, "Grass Top", PrimitiveType.Cylinder, Vector3.zero, new Vector3(8f, 0.8f, 8f), p.Rough);
            CreatePrimitive(island.transform, "Rock", PrimitiveType.Sphere, new Vector3(0f, -4.5f, 0f), new Vector3(12f, 9f, 12f), p.Cliff);
            CreatePrimitive(island.transform, "Crystal", PrimitiveType.Cylinder, new Vector3(2.4f, 1.8f, -0.5f), new Vector3(0.55f, 2f, 0.55f), p.FlowerGold).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            result["Island"] = SavePrefab(island, PrefabFolder + "/FloatingIsland.prefab");

            GameObject windmill = new("Windmill");
            CreatePrimitive(windmill.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 3.4f, 0f), new Vector3(2f, 3.4f, 2f), p.WindmillCream);
            CreatePrimitive(windmill.transform, "Roof", PrimitiveType.Cylinder, new Vector3(0f, 7.2f, 0f), new Vector3(2.6f, 1.5f, 2.6f), p.WindmillRoof).transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Transform rotor = new GameObject("Rotor").transform;
            rotor.SetParent(windmill.transform, false);
            rotor.localPosition = new Vector3(0f, 5f, -2.15f);
            CreatePrimitive(rotor, "Hub", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.65f, p.FlowerGold);
            for (int index = 0; index < 4; index++)
            {
                GameObject blade = CreatePrimitive(rotor, $"Blade {index + 1}", PrimitiveType.Cube,
                    new Vector3(0f, 2.1f, 0f), new Vector3(0.45f, 3.5f, 0.18f), p.WindmillBlade);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, index * 90f);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, 2.1f, 0f);
            }
            result["Windmill"] = SavePrefab(windmill, PrefabFolder + "/Windmill.prefab");
            return result;
        }

        private static MaterialPalette CreatePalette()
        {
            return new MaterialPalette
            {
                Tee = Material("Tee", new Color(0.33f, 0.92f, 0.54f), 0.25f),
                Fairway = Material("Fairway", new Color(0.22f, 0.82f, 0.34f), 0.2f),
                FairwayHighlight = Material("FairwayHighlight", new Color(0.36f, 0.92f, 0.44f), 0.2f),
                Rough = Material("Rough", new Color(0.09f, 0.48f, 0.24f), 0.12f),
                Green = Material("Green", new Color(0.38f, 0.95f, 0.48f), 0.35f),
                GreenHighlight = Material("GreenHighlight", new Color(0.54f, 1f, 0.62f), 0.38f),
                Sand = Material("Sand", new Color(1f, 0.75f, 0.33f), 0.08f),
                Water = Material("Water", new Color(0.08f, 0.68f, 0.96f), 0.68f, true),
                Waterfall = Material("Waterfall", new Color(0.35f, 0.9f, 1f, 0.72f), 0.72f, true),
                Cliff = Material("Cliff", new Color(0.34f, 0.28f, 0.52f), 0.1f),
                CliffDark = Material("CliffDark", new Color(0.2f, 0.17f, 0.34f), 0.08f),
                CliffLight = Material("CliffLight", new Color(0.48f, 0.4f, 0.66f), 0.12f),
                Trunk = Material("Trunk", new Color(0.35f, 0.19f, 0.1f), 0.1f),
                Foliage = Material("Foliage", new Color(0.04f, 0.54f, 0.32f), 0.18f),
                FoliageLight = Material("FoliageLight", new Color(0.18f, 0.8f, 0.48f), 0.18f),
                FlowerPink = Material("FlowerPink", new Color(1f, 0.25f, 0.65f), 0.32f),
                FlowerGold = Material("FlowerGold", new Color(1f, 0.78f, 0.15f), 0.38f),
                Cloud = Material("Cloud", new Color(0.95f, 0.98f, 1f), 0.15f),
                WindmillCream = Material("WindmillCream", new Color(1f, 0.9f, 0.66f), 0.2f),
                WindmillRoof = Material("WindmillRoof", new Color(0.08f, 0.58f, 0.62f), 0.2f),
                WindmillBlade = Material("WindmillBlade", new Color(0.96f, 0.57f, 0.2f), 0.16f),
                Flag = Material("Flag", new Color(1f, 0.24f, 0.45f), 0.25f),
                Pole = Material("Pole", new Color(1f, 0.94f, 0.76f), 0.6f),
                CharacterSkin = Material("CharacterSkin", new Color(1f, 0.69f, 0.52f), 0.32f),
                CharacterOutfit = Material("CharacterOutfit", new Color(0.04f, 0.72f, 0.82f), 0.28f),
                CharacterAccent = Material("CharacterAccent", new Color(1f, 0.24f, 0.62f), 0.3f),
                CharacterDark = Material("CharacterDark", new Color(0.07f, 0.08f, 0.16f), 0.22f)
            };
        }

        private static Material Material(string name, Color color, float smoothness, bool transparent = false)
        {
            string path = $"{MaterialFolder}/M10{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "M10" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateSkybox()
        {
            string path = MaterialFolder + "/M10Skybox.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Skybox/Procedural");
            if (material == null)
            {
                material = new Material(shader) { name = "M10Skybox" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_SunSize", 0.045f);
            material.SetFloat("_AtmosphereThickness", 0.78f);
            material.SetColor("_SkyTint", new Color(0.24f, 0.72f, 1f));
            material.SetColor("_GroundColor", new Color(0.24f, 0.29f, 0.44f));
            material.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static SkyIslandEnvironmentTuningData LoadOrCreateEnvironmentTuning()
        {
            string path = DataFolder + "/M10SkyIslandEnvironmentTuning.asset";
            SkyIslandEnvironmentTuningData data = AssetDatabase.LoadAssetAtPath<SkyIslandEnvironmentTuningData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SkyIslandEnvironmentTuningData>();
                AssetDatabase.CreateAsset(data, path);
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static T CloneAsset<T>(string sourcePath, string destinationPath) where T : ScriptableObject
        {
            T destination = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
            T source = AssetDatabase.LoadAssetAtPath<T>(sourcePath);
            if (destination == null)
            {
                destination = Object.Instantiate(source);
                destination.name = System.IO.Path.GetFileNameWithoutExtension(destinationPath);
                AssetDatabase.CreateAsset(destination, destinationPath);
            }
            else
            {
                EditorUtility.CopySerialized(source, destination);
            }
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static void TuneCamera(CameraTuningData data)
        {
            SerializedObject serialized = new(data);
            SetIfFound(serialized, "holeIntroDuration", 3f);
            SetIfFound(serialized, "addressFieldOfView", 46f);
            SetIfFound(serialized, "followFieldOfView", 52f);
            SetIfFound(serialized, "landingFieldOfView", 47f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TuneHud(HudTuningData data)
        {
            SerializedObject serialized = new(data);
            SetIfFound(serialized, "aimScreenMargin", 112f);
            SetIfFound(serialized, "impactFeedbackDuration", 1.15f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TuneShotPresentation(ShotPresentationTuningData data)
        {
            SerializedObject serialized = new(data);
            SetIfFound(serialized, "normalTrailWidth", 0.085f);
            SetIfFound(serialized, "perfectTrailWidth", 0.15f);
            SetIfFound(serialized, "normalTrailTime", 0.5f);
            SetIfFound(serialized, "perfectTrailTime", 0.78f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIfFound(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            Object.DestroyImmediate(value.GetComponent<Collider>());
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return value;
        }

        private static void AddRock(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject rock = CreatePrimitive(parent, "Cliff Facet", PrimitiveType.Sphere, position, scale, material);
            rock.transform.localRotation = Quaternion.Euler(12f, position.z * 3f, 18f);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            return instance;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new()
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(FoundationPath, true)
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath && existing.path != FoundationPath)
                {
                    scenes.Add(existing);
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;
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

        private static void SetBoolean(Object target, string propertyName, bool value)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArray(Object target, string propertyName, Transform[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform result = FindRecursive(root.transform, name);
                if (result != null) return result.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform result = FindRecursive(parent.GetChild(index), name);
                if (result != null) return result;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private sealed class MaterialPalette
        {
            public Material Tee, Fairway, FairwayHighlight, Rough, Green, GreenHighlight, Sand, Water, Waterfall;
            public Material Cliff, CliffDark, CliffLight, Trunk, Foliage, FoliageLight, FlowerPink, FlowerGold, Cloud;
            public Material WindmillCream, WindmillRoof, WindmillBlade, Flag, Pole;
            public Material CharacterSkin, CharacterOutfit, CharacterAccent, CharacterDark;
        }
    }
}
