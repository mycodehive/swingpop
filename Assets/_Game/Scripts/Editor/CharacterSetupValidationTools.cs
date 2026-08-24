using System.Collections.Generic;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class CharacterSetupValidationTools
    {
        private const string PlaceholderPrefabPath = "Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab";
        private const string TemplatePrefabPath = "Assets/_Game/Prefabs/Characters/HumanoidGolferTemplate.prefab";

        [MenuItem("SwingPop/Character/Validate Character Setup")]
        public static void ValidateCharacterSetup()
        {
            string report = ValidateAndGetReport(true);
            EditorUtility.DisplayDialog("SwingPop Character Validation", "PASS\n\n" + report.Replace(" | ", "\n"), "OK");
        }

        public static string ValidateDefaultAssetsAndGetReport()
        {
            return ValidateAndGetReport(false);
        }

        private static string ValidateAndGetReport(bool useSelection)
        {
            List<string> notes = new();
            CharacterVisualAdapter selected = useSelection ? FindSelectedAdapter() : null;
            if (selected != null)
            {
                ValidateAdapter(selected, selected.name, notes);
            }
            else
            {
                ValidatePrefab(PlaceholderPrefabPath, notes);
                ValidatePrefab(TemplatePrefabPath, notes);
            }

            string report = string.Join(" | ", notes);
            Debug.Log("CHARACTER SETUP PASS | " + report);
            return report;
        }

        private static void ValidatePrefab(string path, List<string> notes)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, $"Character prefab is missing: {path}");
            CharacterVisualAdapter adapter = prefab.GetComponent<CharacterVisualAdapter>();
            ValidateAdapter(adapter, prefab.name, notes);
            Require(prefab.GetComponentsInChildren<Collider>(true).Length == 0,
                $"{prefab.name} visual hierarchy contains a Collider.");
        }

        private static void ValidateAdapter(CharacterVisualAdapter adapter, string owner, List<string> notes)
        {
            Require(adapter != null, $"{owner}: CharacterVisualAdapter is missing.");
            Require(adapter.GameplayRoot != null, $"{owner}: GameplayRoot is missing.");
            Require(adapter.VisualRoot != null, $"{owner}: VisualRoot is missing.");
            Require(adapter.Profile != null, $"{owner}: CharacterVisualProfile is missing.");
            Require(adapter.ClubSocket != null, $"{owner}: ClubSocket is missing.");
            Require(adapter.HandSocket != null || adapter.RightHandSocket != null,
                $"{owner}: HandSocket/RightHandSocket is missing.");
            Require(adapter.ImpactAnchor != null, $"{owner}: ImpactAnchor is missing.");
            Require(adapter.HeadLookTarget != null, $"{owner}: HeadLookTarget is missing.");

            CharacterVisualProfile profile = adapter.Profile;
            Require(profile.HasValidDimensions, $"{owner}: Profile height, bounds, or CharacterScale is invalid.");
            Require(IsFinite(profile.GroundOffset), $"{owner}: GroundOffset is not finite.");
            Require(Mathf.Abs(profile.GroundOffset) <= profile.VisualHeight,
                $"{owner}: GroundOffset exceeds the declared VisualHeight.");
            Require(IsFinite(profile.CameraFramingOffset), $"{owner}: CameraFramingOffset is not finite.");

            Animator animator = adapter.Animator;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Require(adapter.HasValidHumanoidAvatar,
                    $"{owner}: Animator Controller is assigned but Avatar is missing, invalid, or not Humanoid.");
                notes.Add($"{owner}: Humanoid Animator ready");
            }
            else
            {
                notes.Add($"{owner}: procedural fallback ready; final Humanoid Avatar/Controller not assigned");
            }

            notes.Add(
                $"{owner}: height {profile.VisualHeight:0.00}m, scale {profile.CharacterScale:0.00}, " +
                $"ground {profile.GroundOffset:+0.00;-0.00;0.00}, sockets ready");
        }

        private static CharacterVisualAdapter FindSelectedAdapter()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return null;
            }
            return selected.GetComponent<CharacterVisualAdapter>()
                ?? selected.GetComponentInParent<CharacterVisualAdapter>()
                ?? selected.GetComponentInChildren<CharacterVisualAdapter>(true);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }
    }
}
