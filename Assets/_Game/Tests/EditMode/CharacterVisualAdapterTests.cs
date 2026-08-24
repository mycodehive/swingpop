using NUnit.Framework;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class CharacterVisualAdapterTests
    {
        [Test]
        public void Configure_ExposesReplacementSeamWithoutGameplayState()
        {
            GameObject root = new("CharacterRoot");
            CharacterVisualProfile profile = ScriptableObject.CreateInstance<CharacterVisualProfile>();
            try
            {
                Transform visual = Child(root.transform, "VisualRoot");
                Transform club = Child(root.transform, "ClubSocket");
                Transform hand = Child(root.transform, "HandSocket");
                Transform leftHand = Child(root.transform, "LeftHandSocket");
                Transform rightHand = Child(root.transform, "RightHandSocket");
                Transform impact = Child(root.transform, "ImpactAnchor");
                Transform look = Child(root.transform, "HeadLookTarget");
                CharacterVisualAdapter adapter = root.AddComponent<CharacterVisualAdapter>();

                adapter.Configure(
                    root.transform,
                    visual,
                    null,
                    null,
                    club,
                    hand,
                    leftHand,
                    rightHand,
                    impact,
                    look,
                    profile);

                Assert.That(adapter.HasRequiredReferences, Is.True);
                Assert.That(adapter.GameplayRoot, Is.SameAs(root.transform));
                Assert.That(adapter.VisualRoot, Is.SameAs(visual));
                Assert.That(adapter.ClubSocket, Is.SameAs(club));
                Assert.That(adapter.HandSocket, Is.SameAs(hand));
                Assert.That(adapter.LeftHandSocket, Is.SameAs(leftHand));
                Assert.That(adapter.RightHandSocket, Is.SameAs(rightHand));
                Assert.That(adapter.ImpactAnchor, Is.SameAs(impact));
                Assert.That(adapter.HeadLookTarget, Is.SameAs(look));
                Assert.That(adapter.Profile, Is.SameAs(profile));
                Assert.That(adapter.HasValidHumanoidAvatar, Is.False);
                Assert.That(profile.HasValidDimensions, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void LegacyHandSocket_IsRightHandFallback()
        {
            GameObject root = new("CharacterRoot");
            CharacterVisualProfile profile = ScriptableObject.CreateInstance<CharacterVisualProfile>();
            try
            {
                Transform visual = Child(root.transform, "VisualRoot");
                Transform club = Child(root.transform, "ClubSocket");
                Transform hand = Child(root.transform, "HandSocket");
                Transform impact = Child(root.transform, "ImpactAnchor");
                Transform look = Child(root.transform, "HeadLookTarget");
                CharacterVisualAdapter adapter = root.AddComponent<CharacterVisualAdapter>();

                adapter.Configure(root.transform, visual, null, club, hand, impact, look, profile);

                Assert.That(adapter.RightHandSocket, Is.SameAs(hand));
                Assert.That(adapter.LeftHandSocket, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }
    }
}
