using System.Collections.Generic;
using SwingPop.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace SwingPop.Editor
{
    /// <summary>
    /// Builds the presentation-only Course Material & Environment Pass for Hole01.
    /// Gameplay colliders and all gameplay/state/data components remain untouched.
    /// </summary>
    public static class CourseEnvironmentPassBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string RootName = "Course Environment Pass";
        private const string MeshFolder = "Assets/_Game/Art/Courses/CourseEnvironmentPass";
        private const string MaterialFolder = "Assets/_Game/Materials/Environment/CourseEnvironmentPass";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/CourseEnvironmentPass";

        private static readonly Vector2[] IslandOutline =
        {
            new(-13f, -7f), new(-5f, -9f), new(8f, -7f), new(15f, 2f),
            new(17f, 18f), new(16f, 35f), new(18f, 52f), new(15f, 70f),
            new(13f, 87f), new(6f, 94f), new(-7f, 93f), new(-14f, 85f),
            new(-16f, 66f), new(-15f, 48f), new(-18f, 30f), new(-17f, 10f)
        };

        private static readonly Vector3[] FairwayCenters =
        {
            new(0f, 0.1f, -1f), new(-0.8f, 0.103f, 5f), new(-2.1f, 0.105f, 12f),
            new(-3f, 0.109f, 20f), new(-2.4f, 0.105f, 28f), new(-0.3f, 0.103f, 36f),
            new(2.2f, 0.107f, 44f), new(3.1f, 0.111f, 52f), new(2.2f, 0.115f, 59f),
            new(0.7f, 0.119f, 65f), new(0f, 0.123f, 69f)
        };

        private static readonly float[] FairwayWidths =
            { 6.5f, 6.8f, 7.3f, 7.9f, 8.4f, 8.8f, 8.4f, 7.9f, 7.4f, 6.9f, 6.5f };

        [MenuItem("SwingPop/Environment/Build Course Material and Environment Pass")]
        public static void BuildCourseEnvironmentPass()
        {
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyObject(scene, RootName);

            Palette palette = CreatePalette();
            EnvironmentPrefabs prefabs = CreateEnvironmentPrefabs(palette);
            GameObject environment = FindInScene(scene, "Environment");
            GameObject root = new(RootName);
            root.transform.SetParent(environment != null ? environment.transform : null, false);

            DisableSupersededVisuals(scene);
            BuildCourseSurfaces(root.transform, palette);
            BuildVegetationAndLandmarks(root.transform, palette, prefabs, out Transform rotor, out Transform[] clouds);
            PolishCupAndFlag(scene, root.transform, palette);
            ConnectCentralMotion(rotor, clouds, FindRecursive(root.transform, "Water Highlight Motion"));
            TuneLighting();
            ApplyShadowBudget(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("COURSE ENVIRONMENT PASS BUILD COMPLETE | presentation-only meshes/materials connected; gameplay graph unchanged.");
        }

        private static Palette CreatePalette()
        {
            return new Palette
            {
                Rough = Material("Rough", new Color(0.085f, 0.35f, 0.17f), 0.055f),
                Fringe = Material("Fringe", new Color(0.135f, 0.47f, 0.22f), 0.1f),
                FairwayA = Material("FairwayA", new Color(0.235f, 0.625f, 0.285f), 0.18f),
                FairwayB = Material("FairwayB", new Color(0.25f, 0.645f, 0.3f), 0.21f),
                FairwayC = Material("FairwayC", new Color(0.225f, 0.605f, 0.275f), 0.16f),
                GreenA = Material("GreenA", new Color(0.35f, 0.755f, 0.39f), 0.34f),
                GreenB = Material("GreenB", new Color(0.37f, 0.78f, 0.41f), 0.39f),
                Sand = Material("Sand", new Color(0.89f, 0.69f, 0.42f), 0.075f),
                SandLight = Material("SandLight", new Color(0.96f, 0.79f, 0.52f), 0.1f),
                SandShade = Material("SandShade", new Color(0.72f, 0.49f, 0.275f), 0.04f),
                WaterDeep = Material("WaterDeep", new Color(0.02f, 0.31f, 0.55f), 0.63f),
                WaterShallow = Material("WaterShallow", new Color(0.065f, 0.61f, 0.76f), 0.72f),
                WaterHighlight = Material("WaterHighlight", new Color(0.62f, 0.96f, 1f, 0.62f), 0.88f, true),
                Shoreline = Material("Shoreline", new Color(0.66f, 0.9f, 0.78f), 0.26f),
                CliffUpper = Material("CliffUpper", new Color(0.43f, 0.37f, 0.52f), 0.09f),
                CliffMid = Material("CliffMid", new Color(0.29f, 0.255f, 0.405f), 0.055f),
                CliffDark = Material("CliffDark", new Color(0.145f, 0.14f, 0.25f), 0.025f),
                Trunk = Material("Trunk", new Color(0.27f, 0.145f, 0.09f), 0.055f),
                FoliageDark = Material("FoliageDark", new Color(0.045f, 0.31f, 0.16f), 0.065f),
                FoliageLight = Material("FoliageLight", new Color(0.13f, 0.53f, 0.255f), 0.11f),
                FlowerPink = Material("FlowerPink", new Color(0.96f, 0.3f, 0.6f), 0.3f),
                FlowerGold = Material("FlowerGold", new Color(1f, 0.72f, 0.19f), 0.32f),
                White = Material("SoftWhite", new Color(0.91f, 0.97f, 1f), 0.15f),
                CloudShade = Material("CloudShade", new Color(0.72f, 0.87f, 0.96f), 0.09f),
                PropCream = Material("PropCream", new Color(0.86f, 0.75f, 0.54f), 0.16f),
                PropAccent = Material("PropAccent", new Color(0.31f, 0.18f, 0.25f), 0.13f),
                PropCyan = Material("PropCyan", new Color(0.07f, 0.57f, 0.67f), 0.38f),
                Stone = Material("Stone", new Color(0.39f, 0.42f, 0.49f), 0.07f),
                Hole = Material("CupHole", new Color(0.025f, 0.055f, 0.075f), 0f)
            };
        }

        private static void DisableSupersededVisuals(Scene scene)
        {
            SetActive(scene, "Art Pass 1 Environment", false);
            SetActive(scene, "Art Pass 1 Course Details", false);
            SetActive(scene, "Tee Flower Accent Left", false);
            SetActive(scene, "Tee Flower Accent Right", false);
            SetActive(scene, "Green Flower Accent", false);
            for (int index = 1; index <= 4; index++)
            {
                SetActive(scene, $"Distant Island {index:00}", false);
            }

            foreach (string name in new[]
                     {
                         "Organic Sky Island Shell", "Cliff Grass Rim", "Fairway Fringe", "Curved Fairway",
                         "Raised Tee Rim", "Organic Tee", "Green Fringe", "Organic Green", "Bunker Raised Rim",
                         "Bunker Sand Depression", "Water Deep Edge", "Water Shallow Highlight"
                     })
            {
                GameObject value = FindInScene(scene, name);
                Renderer renderer = value != null ? value.GetComponent<Renderer>() : null;
                if (renderer != null) renderer.enabled = false;
            }
        }

        private static void BuildCourseSurfaces(Transform parent, Palette palette)
        {
            Transform course = Marker(parent, "Course Surfaces", Vector3.zero);

            CreateMeshObject(course, "Layered Main Island",
                CreateLayeredIslandMesh(IslandOutline, new Vector2(0f, 43f), -0.04f, -2.5f, -5.4f, -9.3f),
                "CEP_LayeredMainIsland", new[] { palette.Rough, palette.CliffUpper, palette.CliffMid, palette.CliffDark }, false, true);
            CreateMeshObject(course, "Grass Cliff Rim", CreateOutlineRingMesh(IslandOutline, new Vector2(0f, 43f), 0.055f, 0.905f, 1f),
                "CEP_GrassCliffRim", new[] { palette.Fringe }, false, true);
            CreateMeshObject(course, "Upper Rock Ledge", CreateOutlineRingMesh(IslandOutline, new Vector2(0f, 43f), -2.28f, 0.91f, 0.965f),
                "CEP_UpperRockLedge", new[] { palette.CliffUpper }, false, true);
            CreateMeshObject(course, "Lower Rock Ledge", CreateOutlineRingMesh(IslandOutline, new Vector2(0f, 43f), -5.05f, 0.75f, 0.81f),
                "CEP_LowerRockLedge", new[] { palette.CliffMid }, false, true);

            float[] fringeWidths = new float[FairwayWidths.Length];
            for (int i = 0; i < fringeWidths.Length; i++) fringeWidths[i] = FairwayWidths[i] + 0.78f + Mathf.Sin(i * 1.7f) * 0.16f;
            CreateMeshObject(course, "Irregular Fairway Fringe", CreateRibbonBorderMesh(FairwayCenters, FairwayWidths, fringeWidths),
                "CEP_FairwayFringe", new[] { palette.Fringe, palette.Rough }, false, true);
            CreateMeshObject(course, "Broad Fairway Mowing", CreateRibbonMesh(FairwayCenters, FairwayWidths, 3),
                "CEP_FairwayMowing", new[] { palette.FairwayA, palette.FairwayB, palette.FairwayC }, false, true);

            CreateMeshObject(course, "Tee Fringe", CreateEllipseRingMesh(new Vector3(0f, 0.112f, 0f), 7.15f, 5.05f, 5.95f, 4.12f, 32, 0.035f),
                "CEP_TeeFringe", new[] { palette.Fringe }, false, true);
            CreateMeshObject(course, "Tee Mowing", CreateConcentricEllipseMesh(new Vector3(0f, 0.126f, 0f), 5.95f, 4.12f, 32, new[] { 0.42f, 0.72f, 1f }),
                "CEP_TeeMowing", new[] { palette.FairwayB, palette.FairwayA, palette.FairwayB }, false, true);

            CreateMeshObject(course, "Green Fringe Pass", CreateEllipseRingMesh(new Vector3(0f, 0.128f, 76f), 11.25f, 9.45f, 9.75f, 8.05f, 40, 0.025f),
                "CEP_GreenFringe", new[] { palette.Fringe }, false, true);
            CreateMeshObject(course, "Green Fine Mowing", CreateConcentricEllipseMesh(new Vector3(0f, 0.143f, 76f), 9.75f, 8.05f, 40, new[] { 0.34f, 0.63f, 0.82f, 1f }),
                "CEP_GreenMowing", new[] { palette.GreenB, palette.GreenA, palette.GreenB, palette.GreenA }, false, true);

            CreateMeshObject(course, "Bunker Grass Rim Pass", CreateEllipseRingMesh(new Vector3(7.5f, 0.164f, 54f), 5.15f, 7.45f, 4.5f, 6.7f, 36, 0.07f),
                "CEP_BunkerGrassRim", new[] { palette.Fringe }, false, true);
            CreateMeshObject(course, "Bunker Layered Sand", CreateConcentricEllipseMesh(new Vector3(7.5f, 0.146f, 54f), 4.5f, 6.7f, 36, new[] { 0.38f, 0.78f, 1f }),
                "CEP_BunkerLayeredSand", new[] { palette.SandShade, palette.Sand, palette.SandLight }, false, true);
            CreateMeshObject(course, "Bunker Grain", CreateBunkerGrainMesh(), "CEP_BunkerGrain", new[] { palette.SandShade, palette.SandLight }, false, false);

            CreateMeshObject(course, "Water Shoreline", CreateEllipseRingMesh(new Vector3(-11.5f, 0.085f, 34f), 5.65f, 8.95f, 5.16f, 8.46f, 40, 0.035f),
                "CEP_WaterShoreline", new[] { palette.Shoreline }, false, true);
            CreateMeshObject(course, "Water Deep Body", CreateEllipseMesh(new Vector3(-11.5f, 0.092f, 34f), 5.18f, 8.48f, 40, 0.035f),
                "CEP_WaterDeep", new[] { palette.WaterDeep }, false, false);
            CreateMeshObject(course, "Water Shallow Band", CreateEllipseRingMesh(new Vector3(-11.5f, 0.11f, 34f), 5f, 8.25f, 4.15f, 7.05f, 40, 0.035f),
                "CEP_WaterShallow", new[] { palette.WaterShallow }, false, false);
            Transform waterMotion = Marker(course, "Water Highlight Motion", Vector3.zero);
            CreateMeshObject(waterMotion, "Water Soft Highlight", CreateWaterHighlightMesh(), "CEP_WaterHighlight",
                new[] { palette.WaterHighlight }, false, false);

            CreateMeshObject(course, "Selective Grass Clumps", CreateGrassAccentMesh(), "CEP_GrassAccents",
                new[] { palette.FoliageDark, palette.FoliageLight }, false, true);
            CreateMeshObject(course, "Shore Stones", CreateStoneAccentMesh(), "CEP_ShoreStones",
                new[] { palette.Stone, palette.CliffUpper }, false, true);
        }

        private static EnvironmentPrefabs CreateEnvironmentPrefabs(Palette palette)
        {
            EnvironmentPrefabs result = new();
            for (int variant = 0; variant < 3; variant++)
            {
                MeshComposer tree = new(3);
                float trunkHeight = 1.55f + variant * 0.25f;
                tree.AddCylinder(new Vector3(0f, trunkHeight * 0.5f, 0f), 0.28f + variant * 0.035f, trunkHeight, 7, 0);
                int blobs = 4 + variant;
                for (int index = 0; index < blobs; index++)
                {
                    float angle = (index * 2f * Mathf.PI / blobs) + variant * 0.44f;
                    Vector3 center = new(Mathf.Cos(angle) * (0.58f + variant * 0.08f), trunkHeight + 0.58f + Mathf.Sin(index * 1.45f) * 0.2f,
                        Mathf.Sin(angle) * (0.48f + variant * 0.05f));
                    Vector3 size = new(0.9f + (index % 2) * 0.22f, 0.72f + ((index + variant) % 2) * 0.18f, 0.82f);
                    tree.AddBlob(center, size, index % 3 == 0 ? 2 : 1, index + variant * 7);
                }
                result.Trees[variant] = SaveSingleMeshPrefab($"CEP Tree {(char)('A' + variant)}", tree.Build($"CEP Tree {(char)('A' + variant)}"),
                    $"CEP_Tree_{(char)('A' + variant)}", new[] { palette.Trunk, palette.FoliageDark, palette.FoliageLight }, true);

                MeshComposer cloud = new(2);
                for (int index = 0; index < 3 + variant; index++)
                {
                    float x = (index - (2 + variant) * 0.5f) * 1.05f;
                    cloud.AddBlob(new Vector3(x, Mathf.Sin(index * 1.8f + variant) * 0.2f, (index % 2) * 0.2f),
                        new Vector3(1.3f, 0.72f + (index % 2) * 0.16f, 0.92f), index == 0 ? 1 : 0, index + 13);
                }
                result.Clouds[variant] = SaveSingleMeshPrefab($"CEP Cloud {(char)('A' + variant)}", cloud.Build($"CEP Cloud {(char)('A' + variant)}"),
                    $"CEP_Cloud_{(char)('A' + variant)}", new[] { palette.White, palette.CloudShade }, false);

                Mesh island = CreateFloatingIslandMesh(variant);
                result.Islands[variant] = SaveSingleMeshPrefab($"CEP Island {(char)('A' + variant)}", island,
                    $"CEP_Island_{(char)('A' + variant)}", new[] { palette.Rough, palette.CliffUpper, palette.CliffMid, palette.CliffDark, palette.FoliageDark }, false);
            }
            result.Flowers = CreateFlowerPrefab(palette);
            result.Windmill = CreateWindmillPrefab(palette);
            result.Waterfall = CreateWaterfallPrefab(palette);
            return result;
        }

        private static GameObject CreateFlowerPrefab(Palette palette)
        {
            MeshComposer mesh = new(5);
            Vector3[] centers =
            {
                new(-0.45f, 0f, -0.18f), new(0f, 0f, 0.18f), new(0.48f, 0f, -0.05f),
                new(-0.18f, 0f, 0.48f), new(0.3f, 0f, 0.5f)
            };
            for (int index = 0; index < centers.Length; index++)
            {
                Vector3 c = centers[index];
                mesh.AddBlade(c + new Vector3(-0.08f, 0f, 0f), 0.24f, 0.12f, index * 37f, 0);
                mesh.AddBlade(c + new Vector3(0.09f, 0f, 0.03f), 0.19f, 0.1f, index * 37f + 74f, 1);
                mesh.AddCylinder(c + new Vector3(0f, 0.18f, 0f), 0.025f, 0.34f, 5, 0);
                mesh.AddBlob(c + new Vector3(0f, 0.39f + (index % 2) * 0.05f, 0f), new Vector3(0.12f, 0.09f, 0.12f), 2 + index % 3, index + 31);
            }
            return SaveSingleMeshPrefab("CEP Flower Patch", mesh.Build("CEP Flower Patch"), "CEP_FlowerPatch",
                new[] { palette.FoliageDark, palette.FoliageLight, palette.FlowerPink, palette.FlowerGold, palette.White }, false);
        }

        private static GameObject CreateWindmillPrefab(Palette palette)
        {
            GameObject root = new("CEP Windmill");
            MeshComposer tower = new(4);
            tower.AddTaperedBox(new Vector3(0f, 2.5f, 0f), new Vector3(2.25f, 5f, 2.05f), new Vector3(1.45f, 5f, 1.45f), Quaternion.identity, 0);
            tower.AddBox(new Vector3(0f, 0.28f, 0f), new Vector3(2.45f, 0.36f, 2.25f), Quaternion.identity, 1);
            tower.AddCone(new Vector3(0f, 5.3f, 0f), 1.5f, 1.15f, 8, 1);
            tower.AddBox(new Vector3(0f, 1.05f, -1.08f), new Vector3(0.62f, 1.35f, 0.08f), Quaternion.identity, 1);
            tower.AddBox(new Vector3(0f, 3.2f, -0.87f), new Vector3(0.48f, 0.5f, 0.08f), Quaternion.identity, 2);
            tower.AddBox(new Vector3(0f, 4.2f, -0.77f), new Vector3(1.75f, 0.14f, 0.12f), Quaternion.identity, 1);
            CreateUnsavedMeshObject(root.transform, "Authored Tower", SaveMesh(tower.Build("CEP Windmill Tower"), MeshFolder + "/CEP_WindmillTower.asset"),
                new[] { palette.PropCream, palette.PropAccent, palette.PropCyan, palette.White }, true, true);

            Transform rotor = Marker(root.transform, "Rotor", new Vector3(0f, 4.1f, -1.13f));
            MeshComposer blades = new(2);
            blades.AddCylinder(Vector3.zero, 0.3f, 0.32f, 8, 1, Quaternion.Euler(90f, 0f, 0f));
            for (int index = 0; index < 4; index++)
            {
                float angle = index * 90f;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                blades.AddTaperedBox(rotation * Vector3.up * 1.35f, new Vector3(0.25f, 1.45f, 0.09f),
                    new Vector3(0.52f, 1.45f, 0.09f), rotation, 0);
            }
            CreateUnsavedMeshObject(rotor, "Blade Assembly", SaveMesh(blades.Build("CEP Windmill Blades"), MeshFolder + "/CEP_WindmillBlades.asset"),
                new[] { palette.White, palette.PropAccent }, true, true);
            return SaveComplexPrefab(root, "CEP_Windmill");
        }

        private static GameObject CreateWaterfallPrefab(Palette palette)
        {
            GameObject root = new("CEP Waterfall Island");
            Vector2[] outline = new Vector2[12];
            for (int i = 0; i < outline.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / outline.Length;
                float organic = 1f + Mathf.Sin(angle * 3f + 0.6f) * 0.1f;
                outline[i] = new Vector2(Mathf.Cos(angle) * 5.5f * organic, Mathf.Sin(angle) * 4.4f * organic);
            }
            CreateUnsavedMeshObject(root.transform, "Layered Waterfall Rock",
                SaveMesh(CreateLayeredIslandMesh(outline, Vector2.zero, 1.58f, 0.2f, -1.65f, -3.8f), MeshFolder + "/CEP_WaterfallRock.asset"),
                new[] { palette.Rough, palette.CliffUpper, palette.CliffMid, palette.CliffDark }, false, true);
            CreateUnsavedMeshObject(root.transform, "Waterfall Source Pool",
                SaveMesh(CreateEllipseMesh(new Vector3(0f, 1.63f, -2.85f), 1.25f, 1.35f, 20, 0.03f), MeshFolder + "/CEP_WaterfallSource.asset"),
                new[] { palette.WaterShallow }, false, false);

            MeshComposer topTree = new(3);
            topTree.AddCylinder(new Vector3(-2.15f, 2.15f, 0.1f), 0.18f, 1.25f, 6, 0);
            topTree.AddBlob(new Vector3(-2.15f, 3.1f, 0.1f), new Vector3(0.82f, 0.7f, 0.78f), 1, 53);
            topTree.AddBlob(new Vector3(-1.7f, 3.05f, 0f), new Vector3(0.58f, 0.52f, 0.56f), 2, 54);
            CreateUnsavedMeshObject(root.transform, "Waterfall Top Tree",
                SaveMesh(topTree.Build("CEP Waterfall Top Tree"), MeshFolder + "/CEP_WaterfallTopTree.asset"),
                new[] { palette.Trunk, palette.FoliageDark, palette.FoliageLight }, false, true);
            CreateUnsavedMeshObject(root.transform, "Tapered Waterfall", SaveMesh(CreateWaterfallRibbonMesh(), MeshFolder + "/CEP_WaterfallRibbon.asset"),
                new[] { palette.WaterShallow, palette.WaterHighlight }, false, false);
            MeshComposer mist = new(2);
            for (int i = -2; i <= 2; i++)
            {
                mist.AddBlob(new Vector3(i * 0.62f, -4.65f + Mathf.Abs(i) * 0.07f, -4.75f),
                    new Vector3(0.78f, 0.32f, 0.52f), i % 2 == 0 ? 0 : 1, i + 70);
            }
            CreateUnsavedMeshObject(root.transform, "Waterfall Mist", SaveMesh(mist.Build("CEP Waterfall Mist"), MeshFolder + "/CEP_WaterfallMist.asset"),
                new[] { palette.White, palette.CloudShade }, false, false);
            return SaveComplexPrefab(root, "CEP_WaterfallIsland");
        }

        private static void BuildVegetationAndLandmarks(Transform parent, Palette palette, EnvironmentPrefabs prefabs,
            out Transform rotor, out Transform[] clouds)
        {
            Transform vegetation = Marker(parent, "Vegetation", Vector3.zero);
            Vector3[] treePositions =
            {
                new(-13.6f, 0.08f, 9.5f), new(-15.2f, 0.08f, 14.4f), new(-12.9f, 0.08f, 19.2f),
                new(13.5f, 0.08f, 26.5f), new(15.1f, 0.08f, 31.2f), new(12.9f, 0.08f, 36.8f),
                new(-12.4f, 0.08f, 61.5f), new(-14f, 0.08f, 67.5f), new(12.3f, 0.08f, 74f),
                new(10.8f, 0.08f, 80.5f)
            };
            for (int index = 0; index < treePositions.Length; index++)
            {
                GameObject tree = InstantiatePrefab(prefabs.Trees[(index * 2 + index / 3) % 3], vegetation,
                    $"Course Tree {index + 1:00}");
                tree.transform.position = treePositions[index];
                float scale = 0.88f + (index % 4) * 0.09f;
                tree.transform.localScale = new Vector3(scale * (index % 3 == 0 ? 1.12f : 1f), scale, scale);
                tree.transform.rotation = Quaternion.Euler(0f, 17f + index * 41f, 0f);
                Renderer renderer = tree.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.shadowCastingMode = index < 6 ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            Vector3[] flowerPositions =
            {
                new(-5.7f, 0.13f, 1.4f), new(5.35f, 0.13f, 2.6f), new(-9.3f, 0.13f, 22f),
                new(9.7f, 0.13f, 41f), new(-9.7f, 0.13f, 65f), new(9f, 0.13f, 70.5f),
                new(-8f, 0.13f, 75.5f), new(8.2f, 0.13f, 82.5f)
            };
            for (int index = 0; index < flowerPositions.Length; index++)
            {
                GameObject flowers = InstantiatePrefab(prefabs.Flowers, vegetation, $"Course Flower Patch {index + 1:00}");
                flowers.transform.position = flowerPositions[index];
                flowers.transform.localScale = Vector3.one * (0.82f + (index % 3) * 0.1f);
                flowers.transform.rotation = Quaternion.Euler(0f, index * 47f, 0f);
            }

            Transform background = Marker(parent, "Background Islands and Clouds", Vector3.zero);
            Vector3[] islandPositions =
            {
                new(-52f, -9f, 58f), new(53f, -10f, 88f), new(-49f, 6f, 151f), new(52f, -14f, 174f)
            };
            for (int index = 0; index < islandPositions.Length; index++)
            {
                GameObject island = InstantiatePrefab(prefabs.Islands[index % 3], background, $"Course Floating Island {index + 1:00}");
                island.transform.position = islandPositions[index];
                float scale = new[] { 0.86f, 0.7f, 0.72f, 0.9f }[index];
                island.transform.localScale = Vector3.one * scale;
                island.transform.rotation = Quaternion.Euler(0f, index * 37f - 18f, 0f);
            }

            Vector3[] cloudPositions =
            {
                new(-45f, 24f, 35f), new(36f, 30f, 61f), new(-29f, 36f, 94f),
                new(49f, 23f, 124f), new(2f, 43f, 158f), new(-60f, 32f, 145f)
            };
            clouds = new Transform[cloudPositions.Length];
            for (int index = 0; index < cloudPositions.Length; index++)
            {
                GameObject cloud = InstantiatePrefab(prefabs.Clouds[index % 3], background, $"Course Cloud {index + 1:00}");
                cloud.transform.position = cloudPositions[index];
                float depthScale = index < 2 ? 3.1f : index < 4 ? 2.55f : 2.05f;
                cloud.transform.localScale = Vector3.one * depthScale;
                cloud.transform.rotation = Quaternion.Euler(0f, index * 29f, index % 2 == 0 ? -2.5f : 2.5f);
                clouds[index] = cloud.transform;
            }

            Transform landmarks = Marker(parent, "Landmarks", Vector3.zero);
            GameObject windmill = InstantiatePrefab(prefabs.Windmill, landmarks, "Course Windmill Landmark");
            windmill.transform.position = new Vector3(14.5f, 0.08f, 87f);
            windmill.transform.localScale = Vector3.one * 0.88f;
            windmill.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            rotor = FindRecursive(windmill.transform, "Rotor");

            GameObject waterfall = InstantiatePrefab(prefabs.Waterfall, landmarks, "Course Waterfall Landmark");
            waterfall.transform.position = new Vector3(-47f, 3f, 132f);
            waterfall.transform.localScale = Vector3.one * 0.82f;
            waterfall.transform.rotation = Quaternion.Euler(0f, -5f, 0f);
        }

        private static void PolishCupAndFlag(Scene scene, Transform parent, Palette palette)
        {
            Transform detail = Marker(parent, "Cup and Flag Polish", Vector3.zero);
            CreateMeshObject(detail, "Cup Dark Interior", CreateEllipseMesh(new Vector3(0f, 0.151f, 78f), 0.31f, 0.31f, 24, 0f),
                "CEP_CupInterior", new[] { palette.Hole }, false, false);
            CreateMeshObject(detail, "Cup Bright Rim", CreateEllipseRingMesh(new Vector3(0f, 0.158f, 78f), 0.46f, 0.46f, 0.32f, 0.32f, 24, 0f),
                "CEP_CupRim", new[] { palette.White }, false, true);

            GameObject cupMarker = FindInScene(scene, "Cup Marker");
            if (cupMarker != null && cupMarker.TryGetComponent(out Renderer cupRenderer)) cupRenderer.enabled = false;
            GameObject pole = FindInScene(scene, "Flag Pole");
            if (pole != null && pole.TryGetComponent(out Renderer poleRenderer))
            {
                poleRenderer.sharedMaterial = palette.PropCyan;
                poleRenderer.shadowCastingMode = ShadowCastingMode.On;
            }

            GameObject oldFlag = FindInScene(scene, "Flag");
            if (oldFlag == null) return;
            Renderer oldRenderer = oldFlag.GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            Mesh flagMesh = CreateFlagMesh();
            GameObject flag = CreateMeshObject(detail, "Stylized Flag Cloth", flagMesh, "CEP_FlagCloth",
                new[] { palette.FlowerPink, palette.White }, false, true);
            flag.transform.position = oldFlag.transform.position;
            flag.transform.rotation = oldFlag.transform.rotation;
        }

        private static void ConnectCentralMotion(Transform rotor, Transform[] clouds, Transform waterHighlight)
        {
            SkyIslandEnvironmentMotion motion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>(FindObjectsInactive.Include);
            if (motion == null) return;
            SerializedObject serialized = new(motion);
            serialized.FindProperty("windmillRotor").objectReferenceValue = rotor;
            SerializedProperty driftingClouds = serialized.FindProperty("driftingClouds");
            driftingClouds.arraySize = clouds.Length;
            for (int index = 0; index < clouds.Length; index++)
            {
                driftingClouds.GetArrayElementAtIndex(index).objectReferenceValue = clouds[index];
            }
            serialized.FindProperty("waterHighlight").objectReferenceValue = waterHighlight;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(motion);
        }

        private static void TuneLighting()
        {
            Light light = Object.FindAnyObjectByType<Light>(FindObjectsInactive.Include);
            if (light != null)
            {
                light.color = new Color(1f, 0.945f, 0.855f);
                light.intensity = 1.16f;
                light.transform.rotation = Quaternion.Euler(48f, -29f, 0f);
                light.shadowStrength = 0.62f;
                light.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientSkyColor = new Color(0.43f, 0.67f, 0.83f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.49f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.13f, 0.19f, 0.23f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.61f, 0.82f, 0.92f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 84f;
            RenderSettings.fogEndDistance = 205f;

            Material source = RenderSettings.skybox;
            if (source == null) return;
            string path = MaterialFolder + "/CEP_Skybox.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(source) { name = "CEP Skybox" };
                AssetDatabase.CreateAsset(sky, path);
            }
            else if (sky != source)
            {
                EditorUtility.CopySerialized(source, sky);
            }
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.16f, 0.52f, 0.86f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.27f, 0.43f, 0.53f));
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.86f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.08f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        private static void ApplyShadowBudget(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string path = HierarchyPath(renderer.transform);
                bool allowed = path.Contains("Course Tree 01") || path.Contains("Course Tree 02")
                               || path.Contains("Course Tree 03") || path.Contains("Course Tree 04")
                               || path.Contains("Course Tree 05") || path.Contains("Course Tree 06")
                               || path.Contains("Course Windmill Landmark");
                renderer.shadowCastingMode = allowed ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = !path.Contains("Cloud") && !path.Contains("Waterfall Mist");
            }
        }

        private static Mesh CreateLayeredIslandMesh(Vector2[] outline, Vector2 center, float topY, float upperY, float midY, float bottomY)
        {
            int count = outline.Length;
            Vector3[] vertices = new Vector3[1 + count * 4 + 1];
            vertices[0] = new Vector3(center.x, topY, center.y);
            for (int ring = 0; ring < 4; ring++)
            {
                float scale = new[] { 1f, 0.93f, 0.79f, 0.43f }[ring];
                float y = new[] { topY, upperY, midY, bottomY }[ring];
                for (int index = 0; index < count; index++)
                {
                    Vector2 point = Vector2.Lerp(center, outline[index], scale);
                    float variation = ring == 0 ? 0f : Mathf.Sin(index * 1.63f + ring) * (0.18f + ring * 0.11f);
                    vertices[1 + ring * count + index] = new Vector3(point.x, y + variation, point.y);
                }
            }
            int bottomCenter = vertices.Length - 1;
            vertices[bottomCenter] = new Vector3(center.x, bottomY - 1.25f, center.y + 1.5f);
            List<int>[] triangles = { new(), new(), new(), new() };
            for (int index = 0; index < count; index++)
            {
                int next = (index + 1) % count;
                triangles[0].Add(0); triangles[0].Add(1 + next); triangles[0].Add(1 + index);
                for (int ring = 0; ring < 3; ring++)
                {
                    int a = 1 + ring * count + index;
                    int b = 1 + ring * count + next;
                    int c = 1 + (ring + 1) * count + index;
                    int d = 1 + (ring + 1) * count + next;
                    triangles[ring + 1].Add(a); triangles[ring + 1].Add(b); triangles[ring + 1].Add(c);
                    triangles[ring + 1].Add(b); triangles[ring + 1].Add(d); triangles[ring + 1].Add(c);
                }
                triangles[3].Add(1 + 3 * count + index);
                triangles[3].Add(1 + 3 * count + next);
                triangles[3].Add(bottomCenter);
            }
            return MeshFrom(vertices, triangles, "CEP Layered Island");
        }

        private static Mesh CreateFloatingIslandMesh(int variant)
        {
            int segments = 10 + variant * 2;
            float radiusX = 5.4f + variant * 0.9f;
            float radiusZ = 4f + (2 - variant) * 0.45f;
            Vector2[] outline = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float organic = 1f + Mathf.Sin(angle * (3 + variant) + variant) * 0.11f;
                outline[i] = new Vector2(Mathf.Cos(angle) * radiusX * organic, Mathf.Sin(angle) * radiusZ * organic);
            }
            Mesh baseMesh = CreateLayeredIslandMesh(outline, Vector2.zero, 0f, -1.5f - variant * 0.2f, -3.4f - variant * 0.35f, -5.8f - variant * 0.55f);
            return baseMesh;
        }

        private static Mesh CreateRibbonMesh(Vector3[] centers, float[] widths, int materialCount)
        {
            Vector3[] vertices = RibbonVertices(centers, widths);
            List<int>[] triangles = new List<int>[materialCount];
            for (int i = 0; i < materialCount; i++) triangles[i] = new List<int>();
            for (int index = 0; index < centers.Length - 1; index++)
            {
                List<int> target = triangles[index % materialCount];
                int current = index * 2;
                int next = current + 2;
                target.Add(current); target.Add(next); target.Add(current + 1);
                target.Add(next); target.Add(next + 1); target.Add(current + 1);
            }
            return MeshFrom(vertices, triangles, "CEP Broad Mowing Ribbon");
        }

        private static Mesh CreateRibbonBorderMesh(Vector3[] centers, float[] innerWidths, float[] outerWidths)
        {
            Vector3[] inner = RibbonVertices(centers, innerWidths);
            Vector3[] outer = RibbonVertices(centers, outerWidths);
            Vector3[] vertices = new Vector3[inner.Length + outer.Length];
            inner.CopyTo(vertices, 0);
            outer.CopyTo(vertices, inner.Length);
            List<int>[] triangles = { new(), new() };
            int offset = inner.Length;
            for (int index = 0; index < centers.Length - 1; index++)
            {
                List<int> target = triangles[index % 2];
                int i0 = index * 2;
                int i1 = i0 + 2;
                int o0 = offset + i0;
                int o1 = offset + i1;
                target.Add(o0); target.Add(o1); target.Add(i0);
                target.Add(o1); target.Add(i1); target.Add(i0);
                target.Add(i0 + 1); target.Add(i1 + 1); target.Add(o0 + 1);
                target.Add(i1 + 1); target.Add(o1 + 1); target.Add(o0 + 1);
            }
            return MeshFrom(vertices, triangles, "CEP Irregular Fringe");
        }

        private static Vector3[] RibbonVertices(Vector3[] centers, float[] widths)
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
            return vertices;
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
                int next = (index + 1) % segments;
                triangles.Add(0); triangles.Add(next + 1); triangles.Add(index + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "CEP Ellipse");
        }

        private static Mesh CreateEllipseRingMesh(Vector3 center, float outerX, float outerZ, float innerX, float innerZ,
            int segments, float variation)
        {
            Vector3[] vertices = new Vector3[segments * 2];
            List<int> triangles = new();
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                float organic = 1f + Mathf.Sin(angle * 3f + 0.4f) * variation;
                vertices[index * 2] = center + new Vector3(Mathf.Cos(angle) * outerX * organic, 0f, Mathf.Sin(angle) * outerZ * organic);
                vertices[index * 2 + 1] = center + new Vector3(Mathf.Cos(angle) * innerX * organic, 0.008f, Mathf.Sin(angle) * innerZ * organic);
                int next = (index + 1) % segments;
                triangles.Add(index * 2); triangles.Add(index * 2 + 1); triangles.Add(next * 2);
                triangles.Add(next * 2); triangles.Add(index * 2 + 1); triangles.Add(next * 2 + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "CEP Ellipse Ring");
        }

        private static Mesh CreateConcentricEllipseMesh(Vector3 center, float radiusX, float radiusZ, int segments, float[] radii)
        {
            List<Vector3> vertices = new() { center };
            foreach (float radius in radii)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float organic = 1f + Mathf.Sin(angle * 3f + 0.35f) * 0.025f;
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * radiusX * radius * organic, radius * 0.004f,
                        Mathf.Sin(angle) * radiusZ * radius * organic));
                }
            }
            List<int>[] triangles = new List<int>[radii.Length];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[0].Add(0); triangles[0].Add(1 + next); triangles[0].Add(1 + i);
            }
            for (int ring = 1; ring < radii.Length; ring++)
            {
                int inner = 1 + (ring - 1) * segments;
                int outer = 1 + ring * segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles[ring].Add(inner + i); triangles[ring].Add(outer + next); triangles[ring].Add(outer + i);
                    triangles[ring].Add(inner + i); triangles[ring].Add(inner + next); triangles[ring].Add(outer + next);
                }
            }
            return MeshFrom(vertices.ToArray(), triangles, "CEP Concentric Ellipse");
        }

        private static Mesh CreateOutlineRingMesh(Vector2[] outline, Vector2 center, float y, float innerScale, float outerScale)
        {
            Vector3[] vertices = new Vector3[outline.Length * 2];
            List<int> triangles = new();
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 outer = Vector2.Lerp(center, outline[i], outerScale);
                Vector2 inner = Vector2.Lerp(center, outline[i], innerScale);
                vertices[i * 2] = new Vector3(outer.x, y, outer.y);
                vertices[i * 2 + 1] = new Vector3(inner.x, y + 0.012f, inner.y);
                int next = (i + 1) % outline.Length;
                triangles.Add(i * 2); triangles.Add(i * 2 + 1); triangles.Add(next * 2);
                triangles.Add(next * 2); triangles.Add(i * 2 + 1); triangles.Add(next * 2 + 1);
            }
            return MeshFrom(vertices, new[] { triangles }, "CEP Outline Ledge");
        }

        private static Mesh CreateBunkerGrainMesh()
        {
            MeshComposer mesh = new(2);
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 2.39996f;
                float radius = 0.7f + (i % 7) * 0.42f;
                Vector3 center = new(7.5f + Mathf.Cos(angle) * radius, 0.121f, 54f + Mathf.Sin(angle) * radius * 1.35f);
                mesh.AddFlatDiamond(new Vector3(center.x, 0.158f, center.z), 0.06f + (i % 3) * 0.018f, i % 2);
            }
            return mesh.Build("CEP Bunker Grain");
        }

        private static Mesh CreateWaterHighlightMesh()
        {
            MeshComposer mesh = new(1);
            for (int i = -2; i <= 2; i++)
            {
                float z = 34f + i * 2.15f;
                mesh.AddQuad(new Vector3(-14.3f + Mathf.Abs(i) * 0.15f, 0.132f, z - 0.12f),
                    new Vector3(-8.7f - Mathf.Abs(i) * 0.15f, 0.132f, z - 0.12f),
                    new Vector3(-8.95f, 0.132f, z + 0.14f), new Vector3(-14.05f, 0.132f, z + 0.14f), 0);
            }
            return mesh.Build("CEP Water Highlights");
        }

        private static Mesh CreateGrassAccentMesh()
        {
            MeshComposer mesh = new(2);
            Vector3[] anchors =
            {
                new(-6.2f, 0.12f, 0.7f), new(6.1f, 0.12f, 2.1f), new(-8f, 0.12f, 17f),
                new(8.9f, 0.12f, 30f), new(-9f, 0.12f, 46f), new(9.2f, 0.12f, 64f),
                new(-8.4f, 0.12f, 72f), new(8.8f, 0.12f, 78f), new(-16f, 0.12f, 36f)
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                for (int blade = 0; blade < 4; blade++)
                {
                    Vector3 offset = new((blade - 1.5f) * 0.12f, 0f, Mathf.Sin(blade * 2f) * 0.1f);
                    mesh.AddBlade(anchors[i] + offset, 0.28f + blade * 0.045f, 0.09f, i * 31f + blade * 47f, (i + blade) % 2);
                }
            }
            return mesh.Build("CEP Grass Accents");
        }

        private static Mesh CreateStoneAccentMesh()
        {
            MeshComposer mesh = new(2);
            Vector3[] positions =
            {
                new(-16.3f, 0.12f, 29f), new(-16.6f, 0.12f, 34f), new(-16f, 0.12f, 39.5f),
                new(-6.2f, 0.12f, 3.2f), new(6.5f, 0.12f, 3.8f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                mesh.AddBlob(positions[i], new Vector3(0.38f + (i % 2) * 0.16f, 0.19f, 0.3f), i % 2, i + 91);
            }
            return mesh.Build("CEP Shore Stones");
        }

        private static Mesh CreateWaterfallRibbonMesh()
        {
            Vector3[] vertices =
            {
                new(-0.6f, 1.4f, -4.58f), new(0.6f, 1.4f, -4.58f), new(-0.52f, -0.8f, -4.7f), new(0.52f, -0.8f, -4.7f),
                new(-0.78f, -0.8f, -4.71f), new(0.78f, -0.8f, -4.71f), new(-1.05f, -4.3f, -4.82f), new(1.05f, -4.3f, -4.82f),
                new(-0.12f, 1.35f, -4.6f), new(0.12f, 1.35f, -4.6f), new(-0.2f, -4.25f, -4.84f), new(0.2f, -4.25f, -4.84f)
            };
            List<int>[] triangles =
            {
                new() { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 },
                new() { 8, 10, 9, 9, 10, 11 }
            };
            return MeshFrom(vertices, triangles, "CEP Tapered Waterfall");
        }

        private static Mesh CreateFlagMesh()
        {
            Vector3[] vertices =
            {
                new(-0.82f, 0.28f, 0f), new(0.76f, 0.18f, 0.03f), new(0.98f, -0.2f, -0.01f), new(-0.82f, -0.28f, 0f),
                new(-0.2f, 0.12f, -0.012f), new(0.18f, 0.08f, 0.02f), new(0.24f, -0.1f, 0f), new(-0.2f, -0.12f, -0.012f)
            };
            List<int>[] triangles =
            {
                new() { 0, 1, 3, 1, 2, 3, 3, 1, 0, 3, 2, 1 },
                new() { 4, 5, 7, 5, 6, 7, 7, 5, 4, 7, 6, 5 }
            };
            return MeshFrom(vertices, triangles, "CEP Stylized Flag");
        }

        private static Material Material(string name, Color color, float smoothness, bool transparent = false)
        {
            string path = $"{MaterialFolder}/CEP_{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = "CEP " + name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.One);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject SaveSingleMeshPrefab(string name, Mesh mesh, string assetName, Material[] materials, bool castShadows)
        {
            GameObject root = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            root.GetComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, $"{MeshFolder}/{assetName}.asset");
            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return SaveComplexPrefab(root, assetName);
        }

        private static GameObject SaveComplexPrefab(GameObject root, string assetName)
        {
            string path = $"{PrefabFolder}/{assetName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
        {
            GameObject value = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (value == null) throw new System.InvalidOperationException($"Could not instantiate environment prefab: {prefab.name}");
            value.name = name;
            return value;
        }

        private static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, string assetName, Material[] materials,
            bool castShadows, bool receiveShadows)
        {
            Mesh asset = SaveMesh(mesh, $"{MeshFolder}/{assetName}.asset");
            return CreateUnsavedMeshObject(parent, name, asset, materials, castShadows, receiveShadows);
        }

        private static GameObject CreateUnsavedMeshObject(Transform parent, string name, Mesh mesh, Material[] materials,
            bool castShadows, bool receiveShadows)
        {
            GameObject value = new(name, typeof(MeshFilter), typeof(MeshRenderer));
            value.transform.SetParent(parent, false);
            value.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = value.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
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

        private static Mesh MeshFrom(Vector3[] vertices, IReadOnlyList<List<int>> triangles, string name)
        {
            Mesh mesh = new() { name = name, vertices = vertices, subMeshCount = triangles.Count };
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Transform Marker(Transform parent, string name, Vector3 localPosition)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            return value.transform;
        }

        private static void DestroyObject(Scene scene, string name)
        {
            GameObject value = FindInScene(scene, name);
            if (value != null) Object.DestroyImmediate(value);
        }

        private static void SetActive(Scene scene, string name, bool active)
        {
            GameObject value = FindInScene(scene, name);
            if (value != null) value.SetActive(active);
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
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform value = FindRecursive(parent.GetChild(i), name);
                if (value != null) return value;
            }
            return null;
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private sealed class EnvironmentPrefabs
        {
            public readonly GameObject[] Trees = new GameObject[3];
            public readonly GameObject[] Clouds = new GameObject[3];
            public readonly GameObject[] Islands = new GameObject[3];
            public GameObject Flowers;
            public GameObject Windmill;
            public GameObject Waterfall;
        }

        private sealed class Palette
        {
            public Material Rough, Fringe, FairwayA, FairwayB, FairwayC, GreenA, GreenB;
            public Material Sand, SandLight, SandShade, WaterDeep, WaterShallow, WaterHighlight, Shoreline;
            public Material CliffUpper, CliffMid, CliffDark, Trunk, FoliageDark, FoliageLight;
            public Material FlowerPink, FlowerGold, White, CloudShade, PropCream, PropAccent, PropCyan, Stone, Hole;
        }

        private sealed class MeshComposer
        {
            private readonly List<Vector3> vertices = new();
            private readonly List<int>[] triangles;

            public MeshComposer(int subMeshCount)
            {
                triangles = new List<int>[subMeshCount];
                for (int i = 0; i < subMeshCount; i++) triangles[i] = new List<int>();
            }

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles[material].Add(start); triangles[material].Add(start + 1); triangles[material].Add(start + 3);
                triangles[material].Add(start + 1); triangles[material].Add(start + 2); triangles[material].Add(start + 3);
            }

            public void AddFlatDiamond(Vector3 center, float radius, int material)
            {
                AddQuad(center + new Vector3(-radius, 0f, 0f), center + new Vector3(0f, 0f, radius),
                    center + new Vector3(radius, 0f, 0f), center + new Vector3(0f, 0f, -radius), material);
            }

            public void AddBlade(Vector3 basePosition, float height, float width, float yaw, int material)
            {
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 right = rotation * Vector3.right * width;
                Vector3 lean = rotation * Vector3.forward * width * 0.4f;
                Vector3 top = basePosition + Vector3.up * height + lean;
                AddQuad(basePosition - right, basePosition + right, top + right * 0.25f, top - right * 0.25f, material);
                AddQuad(basePosition + right, basePosition - right, top - right * 0.25f, top + right * 0.25f, material);
            }

            public void AddBox(Vector3 center, Vector3 size, Quaternion rotation, int material)
            {
                AddTaperedBox(center, size, size, rotation, material);
            }

            public void AddTaperedBox(Vector3 center, Vector3 bottomSize, Vector3 topSize, Quaternion rotation, int material)
            {
                float bottomY = -bottomSize.y * 0.5f;
                float topY = topSize.y * 0.5f;
                Vector3[] local =
                {
                    new(-bottomSize.x * .5f, bottomY, -bottomSize.z * .5f), new(bottomSize.x * .5f, bottomY, -bottomSize.z * .5f),
                    new(bottomSize.x * .5f, bottomY, bottomSize.z * .5f), new(-bottomSize.x * .5f, bottomY, bottomSize.z * .5f),
                    new(-topSize.x * .5f, topY, -topSize.z * .5f), new(topSize.x * .5f, topY, -topSize.z * .5f),
                    new(topSize.x * .5f, topY, topSize.z * .5f), new(-topSize.x * .5f, topY, topSize.z * .5f)
                };
                int start = vertices.Count;
                foreach (Vector3 point in local) vertices.Add(center + rotation * point);
                int[] indices =
                {
                    0,2,1, 0,3,2, 4,5,6, 4,6,7,
                    0,1,5, 0,5,4, 1,2,6, 1,6,5,
                    2,3,7, 2,7,6, 3,0,4, 3,4,7
                };
                foreach (int index in indices) triangles[material].Add(start + index);
            }

            public void AddCylinder(Vector3 center, float radius, float height, int segments, int material, Quaternion? rotation = null)
            {
                Quaternion q = rotation ?? Quaternion.identity;
                int start = vertices.Count;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    Vector3 radial = new(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius);
                    vertices.Add(center + q * radial);
                    vertices.Add(center + q * new Vector3(radial.x, height * 0.5f, radial.z));
                }
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    int a = start + i * 2;
                    int b = start + next * 2;
                    triangles[material].Add(a); triangles[material].Add(b); triangles[material].Add(a + 1);
                    triangles[material].Add(b); triangles[material].Add(b + 1); triangles[material].Add(a + 1);
                }
            }

            public void AddCone(Vector3 center, float radius, float height, int segments, int material)
            {
                int top = vertices.Count;
                vertices.Add(center + Vector3.up * height * 0.5f);
                int ring = vertices.Count;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius));
                }
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles[material].Add(top); triangles[material].Add(ring + i); triangles[material].Add(ring + next);
                }
            }

            public void AddBlob(Vector3 center, Vector3 size, int material, int seed)
            {
                const int segments = 8;
                int top = vertices.Count;
                vertices.Add(center + Vector3.up * size.y);
                int upperRing = vertices.Count;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float irregular = 0.91f + Mathf.Sin(i * 2.13f + seed) * 0.08f;
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * size.x * 0.82f * irregular, size.y * 0.38f,
                        Mathf.Sin(angle) * size.z * 0.82f * irregular));
                }
                int lowerRing = vertices.Count;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float irregular = 0.92f + Mathf.Sin(i * 1.77f + seed * 0.7f) * 0.08f;
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * size.x * irregular, -size.y * 0.28f,
                        Mathf.Sin(angle) * size.z * irregular));
                }
                int bottom = vertices.Count;
                vertices.Add(center - Vector3.up * size.y * 0.82f);
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles[material].Add(top); triangles[material].Add(upperRing + i); triangles[material].Add(upperRing + next);
                    triangles[material].Add(upperRing + i); triangles[material].Add(lowerRing + i); triangles[material].Add(upperRing + next);
                    triangles[material].Add(upperRing + next); triangles[material].Add(lowerRing + i); triangles[material].Add(lowerRing + next);
                    triangles[material].Add(bottom); triangles[material].Add(lowerRing + next); triangles[material].Add(lowerRing + i);
                }
            }

            public Mesh Build(string name)
            {
                return MeshFrom(vertices.ToArray(), triangles, name);
            }
        }
    }
}
