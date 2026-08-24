using System.Collections.Generic;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Presentation;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SwingPop.Editor
{
    public static class M11PolishSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string MeshFolder = "Assets/_Game/Art/Courses/M11";
        private const string MaterialFolder = "Assets/_Game/Materials/Polish";
        private const string DataFolder = "Assets/_Game/ScriptableObjects/Polish";
        private const string CameraSourcePath = "Assets/_Game/ScriptableObjects/Environment/M10CameraTuning.asset";

        [MenuItem("SwingPop/M11/Build Visual Polish")]
        public static void BuildVisualPolish()
        {
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(DataFolder);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject existing = FindInScene(scene, "M11 Visual Polish");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            PolishPalette palette = CreatePalette();
            ConfigureGameplaySurfaceLayout(scene);
            GameObject polishRoot = BuildVisualCourse(scene, palette);
            RecomposeEnvironment(scene);
            PolishCharacter(scene, palette);
            PolishCamera(palette);
            PolishHud(scene);
            PolishAimAndBall(palette);
            PolishLighting(palette);
            OptimizeShadows(scene, polishRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SwingPop M11 visual polish applied without adding gameplay features. Foundation.unity remains unchanged.");
        }

        private static void ConfigureGameplaySurfaceLayout(Scene scene)
        {
            GameObject oldVisuals = FindInScene(scene, "Course Visual Layers");
            if (oldVisuals != null)
            {
                oldVisuals.SetActive(false);
            }

            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
            {
                foreach (Renderer renderer in surface.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }

                switch (surface.SurfaceType)
                {
                    case TerrainSurfaceType.Rough:
                        surface.transform.position = new Vector3(0f, -0.38f, 39f);
                        surface.transform.localScale = new Vector3(34f, 0.5f, 84f);
                        break;
                    case TerrainSurfaceType.Bunker:
                        surface.transform.position = new Vector3(7.5f, -0.25f, 54f);
                        surface.transform.localScale = new Vector3(9f, 0.5f, 13f);
                        break;
                    case TerrainSurfaceType.Water:
                        surface.transform.position = new Vector3(-11.5f, 2f, 34f);
                        BoxCollider waterTrigger = surface.GetComponent<BoxCollider>();
                        if (waterTrigger != null)
                        {
                            waterTrigger.size = new Vector3(10f, 6f, 16f);
                        }
                        break;
                }
            }
        }

        private static GameObject BuildVisualCourse(Scene scene, PolishPalette palette)
        {
            GameObject environment = FindInScene(scene, "Environment");
            GameObject root = new("M11 Visual Polish");
            root.transform.SetParent(environment != null ? environment.transform : null, false);

            Vector2[] islandOutline =
            {
                new(-13f, -7f), new(-5f, -9f), new(8f, -7f), new(15f, 2f),
                new(17f, 18f), new(16f, 35f), new(18f, 52f), new(15f, 70f),
                new(13f, 87f), new(6f, 94f), new(-7f, 93f), new(-14f, 85f),
                new(-16f, 66f), new(-15f, 48f), new(-18f, 30f), new(-17f, 10f)
            };
            CreateMeshObject(root.transform, "Organic Sky Island Shell", CreateExtrudedIslandMesh(islandOutline, -0.04f, -8f),
                "M11OrganicIsland", new[] { palette.Rough, palette.CliffSide }, true, true);
            CreateMeshObject(root.transform, "Cliff Grass Rim", CreateOutlineRingMesh(islandOutline, 0.045f, 0.9f),
                "M11CliffRim", new[] { palette.CliffRim }, false, true);

            Vector3[] fairwayCenters =
            {
                new(0f, 0.055f, -1f), new(-0.8f, 0.058f, 5f), new(-2.1f, 0.06f, 12f),
                new(-3f, 0.064f, 20f), new(-2.4f, 0.06f, 28f), new(-0.3f, 0.058f, 36f),
                new(2.2f, 0.062f, 44f), new(3.1f, 0.066f, 52f), new(2.2f, 0.07f, 59f),
                new(0.7f, 0.074f, 65f), new(0f, 0.078f, 69f)
            };
            float[] fairwayWidths = { 6.5f, 6.8f, 7.3f, 7.9f, 8.4f, 8.8f, 8.4f, 7.9f, 7.4f, 6.9f, 6.5f };
            float[] fringeWidths = new float[fairwayWidths.Length];
            for (int index = 0; index < fringeWidths.Length; index++) fringeWidths[index] = fairwayWidths[index] + 0.65f;
            CreateMeshObject(root.transform, "Fairway Fringe", CreateRibbonMesh(fairwayCenters, fringeWidths, false),
                "M11FairwayFringe", new[] { palette.Fringe }, false, true);
            CreateMeshObject(root.transform, "Curved Fairway", CreateRibbonMesh(fairwayCenters, fairwayWidths, true),
                "M11CurvedFairway", new[] { palette.Fairway, palette.FairwayAlternate }, false, true);

            CreateMeshObject(root.transform, "Raised Tee Rim", CreateEllipseRingMesh(new Vector3(0f, 0.07f, 0f), 7.2f, 5.1f, 5.9f, 4.1f, 28),
                "M11TeeRim", new[] { palette.Fringe }, false, true);
            CreateMeshObject(root.transform, "Organic Tee", CreateEllipseMesh(new Vector3(0f, 0.085f, 0f), 5.9f, 4.1f, 28, 0.04f),
                "M11OrganicTee", new[] { palette.Tee }, false, true);

            CreateMeshObject(root.transform, "Green Fringe", CreateEllipseRingMesh(new Vector3(0f, 0.075f, 76f), 11.2f, 9.4f, 9.7f, 8f, 36),
                "M11GreenFringe", new[] { palette.GreenFringe }, false, true);
            CreateMeshObject(root.transform, "Organic Green", CreateEllipseMesh(new Vector3(0f, 0.092f, 76f), 9.7f, 8f, 36, 0.05f),
                "M11OrganicGreen", new[] { palette.Green }, false, true);

            CreateMeshObject(root.transform, "Bunker Raised Rim", CreateEllipseRingMesh(new Vector3(7.5f, 0.075f, 54f), 5.1f, 7.4f, 4.3f, 6.4f, 30),
                "M11BunkerRim", new[] { palette.BunkerRim }, false, true);
            CreateMeshObject(root.transform, "Bunker Sand Depression", CreateEllipseMesh(new Vector3(7.5f, 0.025f, 54f), 4.3f, 6.4f, 30, 0.08f),
                "M11BunkerSand", new[] { palette.Sand }, false, true);

            CreateMeshObject(root.transform, "Water Deep Edge", CreateEllipseMesh(new Vector3(-11.5f, 0.01f, 34f), 5.2f, 8.5f, 32, 0.04f),
                "M11WaterDeep", new[] { palette.WaterDeep }, false, true);
            CreateMeshObject(root.transform, "Water Shallow Highlight", CreateEllipseMesh(new Vector3(-11.2f, 0.025f, 33.5f), 4.3f, 7.3f, 32, 0.05f),
                "M11WaterShallow", new[] { palette.WaterShallow }, false, false);

            CreateEdgeAccent(root.transform, "Tee Flower Accent Left", new Vector3(-5.4f, 0.11f, 1.3f), palette.FlowerPink);
            CreateEdgeAccent(root.transform, "Tee Flower Accent Right", new Vector3(5.3f, 0.11f, 2.7f), palette.FlowerGold);
            CreateEdgeAccent(root.transform, "Green Flower Accent", new Vector3(-9f, 0.12f, 72f), palette.FlowerPink);
            return root;
        }

        private static void RecomposeEnvironment(Scene scene)
        {
            GameObject oldIsland = FindInScene(scene, "Main Floating Island Silhouette");
            if (oldIsland != null) oldIsland.SetActive(false);

            Vector3[] treePositions =
            {
                new(-13.5f, 0f, 12f), new(-15.4f, 0f, 16f), new(-12.8f, 0f, 19.5f),
                new(13.3f, 0f, 28f), new(15.2f, 0f, 32f), new(12.5f, 0f, 36f),
                new(-12.5f, 0f, 67f), new(12.5f, 0f, 78f)
            };
            for (int index = 0; index < treePositions.Length; index++)
            {
                GameObject tree = FindInScene(scene, $"Stylized Tree {index + 1:00}");
                if (tree == null) continue;
                tree.transform.position = treePositions[index];
                float scale = index is 1 or 4 ? 1.2f : index >= 6 ? 0.9f : 1f;
                tree.transform.localScale = Vector3.one * scale;
                tree.transform.rotation = Quaternion.Euler(0f, 22f + index * 31f, 0f);
            }

            Vector3[] flowerPositions =
            {
                new(-5.5f, 0.1f, 2f), new(5f, 0.1f, 3.5f), new(-9f, 0.1f, 24f),
                new(9.2f, 0.1f, 44f), new(-9f, 0.1f, 68f), new(8f, 0.1f, 72f)
            };
            for (int index = 0; index < flowerPositions.Length; index++)
            {
                GameObject flowers = FindInScene(scene, $"Flower Patch {index + 1:00}");
                if (flowers == null) continue;
                flowers.transform.position = flowerPositions[index];
                flowers.transform.localScale = Vector3.one * 0.75f;
            }

            GameObject windmill = FindInScene(scene, "Windmill Landmark");
            if (windmill != null)
            {
                windmill.transform.position = new Vector3(11f, 0f, 82f);
                windmill.transform.localScale = Vector3.one * 1.05f;
                windmill.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            }

            GameObject waterfall = FindInScene(scene, "Waterfall Island Landmark");
            if (waterfall != null)
            {
                waterfall.transform.position = new Vector3(-28f, 5f, 103f);
                waterfall.transform.localScale = new Vector3(1.35f, 1.15f, 1.35f);
            }

            Vector3[] distantPositions =
            {
                new(-48f, -9f, 44f), new(47f, -7f, 70f), new(-34f, 0f, 125f), new(38f, -12f, 142f)
            };
            for (int index = 0; index < distantPositions.Length; index++)
            {
                GameObject island = FindInScene(scene, $"Distant Island {index + 1:00}");
                if (island != null) island.transform.position = distantPositions[index];
            }

            Vector3[] cloudPositions =
            {
                new(-46f, 24f, 34f), new(36f, 30f, 58f), new(-30f, 36f, 92f),
                new(48f, 22f, 122f), new(0f, 42f, 155f)
            };
            for (int index = 0; index < cloudPositions.Length; index++)
            {
                GameObject cloud = FindInScene(scene, $"Cloud Cluster {index + 1:00}");
                if (cloud != null) cloud.transform.position = cloudPositions[index];
            }
        }

        private static void PolishCharacter(Scene scene, PolishPalette palette)
        {
            GameObject character = FindInScene(scene, "Placeholder Golfer");
            if (character == null) return;

            Transform visualRoot = FindRecursive(character.transform, "Visual Root");
            if (visualRoot != null) visualRoot.localScale = Vector3.one * 1.27f;
            SetLocalScale(character.transform, "Torso", new Vector3(0.5f, 0.66f, 0.36f));
            SetLocalScale(character.transform, "Head", new Vector3(0.7f, 0.78f, 0.68f));
            SetLocalScale(character.transform, "Hair", new Vector3(0.74f, 0.45f, 0.72f));
            SetLocalScale(character.transform, "Arm L", new Vector3(0.22f, 0.43f, 0.22f));
            SetLocalScale(character.transform, "Arm R", new Vector3(0.22f, 0.43f, 0.22f));
            SetLocalScale(character.transform, "Leg L", new Vector3(0.29f, 0.45f, 0.27f));
            SetLocalScale(character.transform, "Leg R", new Vector3(0.29f, 0.45f, 0.27f));

            foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>(true))
            {
                string lower = renderer.name.ToLowerInvariant();
                if (lower.Contains("hair")) renderer.sharedMaterial = palette.CharacterHair;
                else if (lower.Contains("head") && !lower.Contains("driver") && !lower.Contains("putter")) renderer.sharedMaterial = palette.CharacterSkin;
                else if (lower.Contains("leg")) renderer.sharedMaterial = palette.CharacterBottom;
                else if (lower.Contains("belt")) renderer.sharedMaterial = palette.CharacterAccent;
                else if (lower.Contains("shaft")) renderer.sharedMaterial = palette.ClubShaft;
                else if (lower.Contains("driver") || lower.Contains("putter")) renderer.sharedMaterial = palette.ClubHead;
                else renderer.sharedMaterial = palette.CharacterOutfit;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            GameObject existing = FindChild(character.transform, "M11 Character Silhouette Additions");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject additions = new("M11 Character Silhouette Additions");
            additions.transform.SetParent(visualRoot, false);

            DestroyNamedChild(character.transform, "Hand L");
            DestroyNamedChild(character.transform, "Hand R");
            DestroyNamedChild(character.transform, "Shoe L");
            DestroyNamedChild(character.transform, "Shoe R");
            DestroyNamedChild(character.transform, "Hair Tuft 1");
            DestroyNamedChild(character.transform, "Hair Tuft 2");
            DestroyNamedChild(character.transform, "Hair Tuft 3");

            Transform armL = FindRecursive(character.transform, "Arm L");
            Transform armR = FindRecursive(character.transform, "Arm R");
            Transform legL = FindRecursive(character.transform, "Leg L");
            Transform legR = FindRecursive(character.transform, "Leg R");
            Transform headPivot = FindRecursive(character.transform, "Head Pivot");
            if (armL != null) CreatePrimitive(armL.parent, "Hand L", PrimitiveType.Sphere, new Vector3(0f, -0.82f, 0f), new Vector3(0.18f, 0.22f, 0.18f), palette.CharacterSkin, true);
            if (armR != null) CreatePrimitive(armR.parent, "Hand R", PrimitiveType.Sphere, new Vector3(0f, -0.82f, 0f), new Vector3(0.18f, 0.22f, 0.18f), palette.CharacterSkin, true);
            if (legL != null) CreatePrimitive(legL.parent, "Shoe L", PrimitiveType.Cube, new Vector3(0f, -0.9f, 0.12f), new Vector3(0.34f, 0.16f, 0.48f), palette.CharacterShoe, true);
            if (legR != null) CreatePrimitive(legR.parent, "Shoe R", PrimitiveType.Cube, new Vector3(0f, -0.9f, 0.12f), new Vector3(0.34f, 0.16f, 0.48f), palette.CharacterShoe, true);
            if (headPivot != null)
            {
                for (int index = -1; index <= 1; index++)
                {
                    GameObject tuft = CreatePrimitive(headPivot, $"Hair Tuft {index + 2}", PrimitiveType.Sphere,
                        new Vector3(index * 0.22f, 0.47f, -0.2f), new Vector3(0.28f, 0.42f, 0.3f), palette.CharacterHair, true);
                    tuft.transform.localRotation = Quaternion.Euler(15f, 0f, index * -16f);
                }
            }
        }

        private static void PolishCamera(PolishPalette palette)
        {
            CameraTuningData source = AssetDatabase.LoadAssetAtPath<CameraTuningData>(CameraSourcePath);
            CameraTuningData tuning = CloneAsset(source, DataFolder + "/M11CameraTuning.asset");
            SerializedObject serialized = new(tuning);
            SetVector(serialized, "addressOffset", new Vector3(5.1f, 3.15f, -7.1f));
            SetVector(serialized, "addressLookOffset", new Vector3(0f, 0.7f, 9.5f));
            SetFloat(serialized, "addressFieldOfView", 44f);
            SetVector(serialized, "swingOffset", new Vector3(4.7f, 3f, -6.6f));
            SetFloat(serialized, "swingFieldOfView", 43f);
            SetFloat(serialized, "followFieldOfView", 54f);
            SetFloat(serialized, "landingFieldOfView", 46f);
            SetVector(serialized, "puttOffset", new Vector3(5.2f, 2.75f, -6.8f));
            SetFloat(serialized, "puttFieldOfView", 48f);
            SetFloat(serialized, "puttDistanceScale", 0.5f);
            SetFloat(serialized, "puttHeightScale", 0.1f);
            SetFloat(serialized, "puttFovPerMeter", 0.62f);
            SetFloat(serialized, "puttMaximumFieldOfView", 64f);
            SetVector(serialized, "resultOffset", new Vector3(9.2f, 6.1f, -10f));
            SetVector(serialized, "resultLookOffset", new Vector3(3.2f, 0.75f, 0f));
            SetFloat(serialized, "resultFieldOfView", 56f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetObjectReference(Object.FindAnyObjectByType<CameraDirector>(), "tuning", tuning);
        }

        private static void PolishHud(Scene scene)
        {
            SetPanelScale(scene, "Top Left - Player HUD", 0.78f, new Vector2(24f, -20f), new Color(0.025f, 0.11f, 0.16f, 0.82f));
            SetPanelScale(scene, "Top Center - Hole HUD", 0.8f, new Vector2(0f, -18f), new Color(0.025f, 0.11f, 0.16f, 0.78f));
            SetPanelScale(scene, "Top Right - Wind HUD", 0.78f, new Vector2(-24f, -20f), new Color(0.025f, 0.11f, 0.16f, 0.78f));
            SetPanelScale(scene, "Bottom Left - Club HUD", 0.78f, new Vector2(24f, 24f), new Color(0.025f, 0.11f, 0.16f, 0.8f));

            GameObject action = FindInScene(scene, "Bottom Right - Primary Action");
            if (action != null)
            {
                RectTransform rect = action.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-24f, 28f);
                rect.sizeDelta = new Vector2(310f, 148f);
                rect.localScale = Vector3.one * 0.78f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
            SetPanelImage(scene, "Shot Button", new Color(0.04f, 0.62f, 0.75f, 0.94f), true);

            GameObject shotButton = FindInScene(scene, "Shot Button");
            if (shotButton != null)
            {
                RectTransform shotRect = shotButton.GetComponent<RectTransform>();
                shotRect.sizeDelta = new Vector2(292f, 112f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(shotRect);
            }

            GameObject keyboardHint = FindInScene(scene, "Keyboard Hint");
            if (keyboardHint != null)
            {
                RectTransform hintRect = keyboardHint.GetComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.5f, 0f);
                hintRect.anchorMax = new Vector2(0.5f, 0f);
                hintRect.pivot = new Vector2(0.5f, 0f);
                hintRect.anchoredPosition = new Vector2(0f, 8f);
                hintRect.sizeDelta = new Vector2(180f, 22f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(hintRect);
                Text hint = keyboardHint.GetComponent<Text>();
                if (hint != null)
                {
                    hint.text = "SPACE / CLICK";
                    hint.fontSize = 12;
                    hint.color = new Color(0.72f, 0.96f, 1f, 0.9f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(hint);
                }
            }

            GameObject aimMarker = FindInScene(scene, "Aim Target Marker");
            if (aimMarker != null)
            {
                aimMarker.transform.localScale = Vector3.one * 0.84f;
                SetPanelImage(scene, "Aim Target Marker", new Color(0.02f, 0.12f, 0.17f, 0.62f), false);
            }

            GameObject timing = FindInScene(scene, "Bottom Center - Timing HUD");
            if (timing != null) timing.transform.localScale = Vector3.one * 0.92f;
            GameObject result = FindInScene(scene, "Result Panel");
            if (result != null) result.transform.localScale = Vector3.one * 0.9f;
        }

        private static void PolishAimAndBall(PolishPalette palette)
        {
            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            if (overlay != null)
            {
                SerializedObject serialized = new(overlay);
                LineRenderer aimLine = serialized.FindProperty("aimLine")?.objectReferenceValue as LineRenderer;
                if (aimLine != null)
                {
                    aimLine.sharedMaterial = palette.AimGuide;
                    aimLine.startWidth = 0.055f;
                    aimLine.endWidth = 0.018f;
                    aimLine.startColor = new Color(0.2f, 0.95f, 1f, 0.72f);
                    aimLine.endColor = new Color(0.35f, 1f, 0.82f, 0.35f);
                    aimLine.textureMode = LineTextureMode.Tile;
                    aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    aimLine.receiveShadows = false;
                }
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            if (ball != null)
            {
                Renderer renderer = ball.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.sharedMaterial = palette.Ball;
            }
        }

        private static void PolishLighting(PolishPalette palette)
        {
            Light light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.93f, 0.82f);
                light.intensity = 1.08f;
                light.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
                light.shadowStrength = 0.72f;
                light.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientSkyColor = new Color(0.38f, 0.62f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.27f, 0.39f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.15f, 0.2f);
            RenderSettings.fogColor = new Color(0.48f, 0.72f, 0.87f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 215f;

            Material sourceSkybox = RenderSettings.skybox;
            if (sourceSkybox != null)
            {
                string skyboxPath = MaterialFolder + "/M11Skybox.mat";
                Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                if (skybox == null)
                {
                    skybox = new Material(sourceSkybox) { name = "M11Skybox" };
                    AssetDatabase.CreateAsset(skybox, skyboxPath);
                }
                else if (sourceSkybox != skybox)
                {
                    EditorUtility.CopySerialized(sourceSkybox, skybox);
                }
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", new Color(0.2f, 0.55f, 0.86f));
                if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", new Color(0.14f, 0.2f, 0.32f));
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 1.02f);
                EditorUtility.SetDirty(skybox);
                RenderSettings.skybox = skybox;
            }
        }

        private static void OptimizeShadows(Scene scene, GameObject polishRoot)
        {
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                string path = GetHierarchyPath(renderer.transform);
                bool decorativeNoShadow = path.Contains("Flower Patch")
                                          || path.Contains("Drifting Clouds")
                                          || path.Contains("Distant Island")
                                          || path.Contains("Waterfall")
                                          || path.Contains("Flower Accent")
                                          || path.Contains("Water Shallow")
                                          || path.Contains("Water Deep")
                                          || path.Contains("Fairway")
                                          || path.Contains("Green Fringe")
                                          || path.Contains("Organic Green")
                                          || path.Contains("Organic Tee");
                if (decorativeNoShadow)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = !path.Contains("Cloud");
                }
            }
        }

        private static PolishPalette CreatePalette()
        {
            return new PolishPalette
            {
                Rough = Material("Rough", new Color(0.075f, 0.34f, 0.16f), 0.1f),
                Fringe = Material("Fringe", new Color(0.11f, 0.46f, 0.21f), 0.12f),
                Fairway = Material("Fairway", new Color(0.2f, 0.66f, 0.29f), 0.2f),
                FairwayAlternate = Material("FairwayAlternate", new Color(0.25f, 0.72f, 0.32f), 0.22f),
                Tee = Material("Tee", new Color(0.3f, 0.76f, 0.42f), 0.26f),
                GreenFringe = Material("GreenFringe", new Color(0.16f, 0.55f, 0.25f), 0.2f),
                Green = Material("Green", new Color(0.35f, 0.78f, 0.38f), 0.34f),
                Sand = Material("Sand", new Color(0.9f, 0.68f, 0.34f), 0.08f),
                BunkerRim = Material("BunkerRim", new Color(0.14f, 0.43f, 0.2f), 0.1f),
                WaterDeep = Material("WaterDeep", new Color(0.025f, 0.32f, 0.56f), 0.62f),
                WaterShallow = Material("WaterShallow", new Color(0.05f, 0.62f, 0.76f), 0.72f, true),
                CliffSide = Material("CliffSide", new Color(0.26f, 0.23f, 0.39f), 0.08f),
                CliffRim = Material("CliffRim", new Color(0.39f, 0.34f, 0.51f), 0.12f),
                FlowerPink = Material("FlowerPink", new Color(0.92f, 0.28f, 0.58f), 0.3f),
                FlowerGold = Material("FlowerGold", new Color(0.95f, 0.7f, 0.18f), 0.36f),
                CharacterSkin = Material("CharacterSkin", new Color(0.95f, 0.66f, 0.51f), 0.32f),
                CharacterHair = Material("CharacterHair", new Color(0.055f, 0.065f, 0.13f), 0.24f),
                CharacterOutfit = Material("CharacterOutfit", new Color(0.04f, 0.48f, 0.68f), 0.28f),
                CharacterAccent = Material("CharacterAccent", new Color(0.92f, 0.19f, 0.52f), 0.34f),
                CharacterBottom = Material("CharacterBottom", new Color(0.08f, 0.12f, 0.22f), 0.2f),
                CharacterShoe = Material("CharacterShoe", new Color(0.84f, 0.89f, 0.91f), 0.3f),
                ClubShaft = Material("ClubShaft", new Color(0.72f, 0.78f, 0.82f), 0.6f),
                ClubHead = Material("ClubHead", new Color(0.08f, 0.72f, 0.76f), 0.5f),
                Ball = Material("Ball", new Color(0.98f, 0.99f, 1f), 0.78f),
                AimGuide = Material("AimGuide", new Color(0.25f, 0.95f, 1f, 0.7f), 0f, true, "Universal Render Pipeline/Unlit")
            };
        }

        private static Material Material(string name, Color color, float smoothness, bool transparent = false, string shaderName = "Universal Render Pipeline/Lit")
        {
            string path = $"{MaterialFolder}/M11{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = "M11" + name };
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

        private static Mesh CreateExtrudedIslandMesh(Vector2[] outline, float topY, float bottomY)
        {
            int count = outline.Length;
            Vector3[] vertices = new Vector3[2 + count * 2];
            vertices[0] = new Vector3(0f, topY, 43f);
            vertices[1] = new Vector3(0f, bottomY, 43f);
            for (int index = 0; index < count; index++)
            {
                vertices[2 + index] = new Vector3(outline[index].x, topY, outline[index].y);
                float depthVariation = bottomY - Mathf.Sin(index * 1.7f) * 1.2f;
                vertices[2 + count + index] = new Vector3(outline[index].x * 0.72f, depthVariation, Mathf.Lerp(43f, outline[index].y, 0.82f));
            }

            List<int> top = new();
            List<int> sides = new();
            for (int index = 0; index < count; index++)
            {
                int next = (index + 1) % count;
                top.Add(0); top.Add(2 + next); top.Add(2 + index);
                int topCurrent = 2 + index;
                int topNext = 2 + next;
                int bottomCurrent = 2 + count + index;
                int bottomNext = 2 + count + next;
                sides.Add(topCurrent); sides.Add(topNext); sides.Add(bottomCurrent);
                sides.Add(topNext); sides.Add(bottomNext); sides.Add(bottomCurrent);
                sides.Add(1); sides.Add(bottomCurrent); sides.Add(bottomNext);
            }
            Mesh mesh = new() { name = "M11 Organic Island" };
            mesh.vertices = vertices;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0);
            mesh.SetTriangles(sides, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOutlineRingMesh(Vector2[] outline, float y, float innerScale)
        {
            int count = outline.Length;
            Vector3[] vertices = new Vector3[count * 2];
            List<int> triangles = new();
            Vector2 center = new(0f, 43f);
            for (int index = 0; index < count; index++)
            {
                Vector2 outer = outline[index];
                Vector2 inner = Vector2.Lerp(center, outer, innerScale);
                vertices[index * 2] = new Vector3(outer.x, y, outer.y);
                vertices[index * 2 + 1] = new Vector3(inner.x, y + 0.01f, inner.y);
                int next = (index + 1) % count;
                triangles.Add(index * 2); triangles.Add(next * 2); triangles.Add(index * 2 + 1);
                triangles.Add(next * 2); triangles.Add(next * 2 + 1); triangles.Add(index * 2 + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "M11 Cliff Rim");
        }

        private static Mesh CreateRibbonMesh(Vector3[] centers, float[] widths, bool alternating)
        {
            Vector3[] vertices = new Vector3[centers.Length * 2];
            for (int index = 0; index < centers.Length; index++)
            {
                Vector3 previous = centers[Mathf.Max(0, index - 1)];
                Vector3 next = centers[Mathf.Min(centers.Length - 1, index + 1)];
                Vector3 direction = (next - previous).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                vertices[index * 2] = centers[index] - right * widths[index];
                vertices[index * 2 + 1] = centers[index] + right * widths[index];
            }
            List<int> even = new();
            List<int> odd = new();
            for (int index = 0; index < centers.Length - 1; index++)
            {
                List<int> target = alternating && index % 2 == 1 ? odd : even;
                int current = index * 2;
                int next = (index + 1) * 2;
                target.Add(current); target.Add(next); target.Add(current + 1);
                target.Add(next); target.Add(next + 1); target.Add(current + 1);
            }
            return MeshFrom(vertices, alternating ? new[] { even, odd } : new[] { even }, "M11 Organic Ribbon");
        }

        private static Mesh CreateEllipseMesh(Vector3 center, float radiusX, float radiusZ, int segments, float variation)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            vertices[0] = center;
            List<int> triangles = new();
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                float organic = 1f + Mathf.Sin(angle * 3f + 0.4f) * variation;
                vertices[index + 1] = center + new Vector3(Mathf.Cos(angle) * radiusX * organic, 0f, Mathf.Sin(angle) * radiusZ * organic);
            }
            for (int index = 0; index < segments; index++)
            {
                int next = (index + 1) % segments;
                triangles.Add(0); triangles.Add(next + 1); triangles.Add(index + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "M11 Organic Ellipse");
        }

        private static Mesh CreateEllipseRingMesh(Vector3 center, float outerX, float outerZ, float innerX, float innerZ, int segments)
        {
            Vector3[] vertices = new Vector3[segments * 2];
            List<int> triangles = new();
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                float organic = 1f + Mathf.Sin(angle * 3f + 0.4f) * 0.045f;
                vertices[index * 2] = center + new Vector3(Mathf.Cos(angle) * outerX * organic, 0f, Mathf.Sin(angle) * outerZ * organic);
                vertices[index * 2 + 1] = center + new Vector3(Mathf.Cos(angle) * innerX * organic, 0.01f, Mathf.Sin(angle) * innerZ * organic);
                int next = (index + 1) % segments;
                triangles.Add(index * 2); triangles.Add(next * 2); triangles.Add(index * 2 + 1);
                triangles.Add(next * 2); triangles.Add(next * 2 + 1); triangles.Add(index * 2 + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "M11 Organic Ring");
        }

        private static Mesh MeshFrom(Vector3[] vertices, IReadOnlyList<List<int>> subMeshes, string name)
        {
            Mesh mesh = new() { name = name, vertices = vertices, subMeshCount = subMeshes.Count };
            for (int index = 0; index < subMeshes.Count; index++) mesh.SetTriangles(subMeshes[index], index);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, string assetName, Material[] materials, bool castShadows, bool receiveShadows)
        {
            Mesh asset = SaveMesh(mesh, $"{MeshFolder}/{assetName}.asset");
            GameObject value = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            value.transform.SetParent(parent, false);
            value.GetComponent<MeshFilter>().sharedMesh = asset;
            MeshRenderer renderer = value.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            return value;
        }

        private static Mesh SaveMesh(Mesh source, string path)
        {
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(source, path);
                return source;
            }
            EditorUtility.CopySerialized(source, asset);
            Object.DestroyImmediate(source);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateEdgeAccent(Transform parent, string name, Vector3 position, Material material)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            for (int index = 0; index < 5; index++)
            {
                float angle = index * Mathf.PI * 2f / 5f;
                CreatePrimitive(root.transform, $"Accent {index + 1}", PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * 0.45f, 0.14f, Mathf.Sin(angle) * 0.45f), new Vector3(0.16f, 0.25f, 0.16f), material, false);
            }
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, bool castShadow)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            Object.DestroyImmediate(value.GetComponent<Collider>());
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return value;
        }

        private static void SetPanelScale(Scene scene, string name, float scale, Vector2 position, Color color)
        {
            GameObject panel = FindInScene(scene, name);
            if (panel == null) return;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            SetPanelImage(scene, name, color, true);
        }

        private static void SetPanelImage(Scene scene, string name, Color color, bool outline)
        {
            GameObject panel = FindInScene(scene, name);
            if (panel == null) return;
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }
            if (outline && panel.GetComponent<Outline>() == null)
            {
                Outline value = panel.AddComponent<Outline>();
                value.effectColor = new Color(0.18f, 0.78f, 0.86f, 0.35f);
                value.effectDistance = new Vector2(2f, -2f);
            }
        }

        private static T CloneAsset<T>(T source, string path) where T : ScriptableObject
        {
            T destination = AssetDatabase.LoadAssetAtPath<T>(path);
            if (destination == null)
            {
                destination = Object.Instantiate(source);
                destination.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(destination, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, destination);
            }
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static void SetVector(SerializedObject serialized, string propertyName, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.vector3Value = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetLocalScale(Transform root, string name, Vector3 value)
        {
            Transform transform = FindRecursive(root, name);
            if (transform != null) transform.localScale = value;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform value = FindRecursive(parent, name);
            return value != null ? value.gameObject : null;
        }

        private static void DestroyNamedChild(Transform parent, string name)
        {
            Transform value = FindRecursive(parent, name);
            if (value != null && value != parent) Object.DestroyImmediate(value.gameObject);
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform value = FindRecursive(root.transform, name);
                if (value != null) return value.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform value = FindRecursive(parent.GetChild(index), name);
                if (value != null) return value;
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

        private sealed class PolishPalette
        {
            public Material Rough, Fringe, Fairway, FairwayAlternate, Tee, GreenFringe, Green;
            public Material Sand, BunkerRim, WaterDeep, WaterShallow, CliffSide, CliffRim;
            public Material FlowerPink, FlowerGold;
            public Material CharacterSkin, CharacterHair, CharacterOutfit, CharacterAccent, CharacterBottom, CharacterShoe;
            public Material ClubShaft, ClubHead, Ball, AimGuide;
        }
    }
}
