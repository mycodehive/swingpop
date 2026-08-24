using SwingPop.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public sealed class CharacterStatePreviewWindow : EditorWindow
    {
        [MenuItem("SwingPop/Character/Preview Character States")]
        public static void Open()
        {
            GetWindow<CharacterStatePreviewWindow>(false, "Character Preview");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Hole01 Play Mode에서만 사용합니다. 이 창의 Swing/Putt preview는 animation state만 요청하며 " +
                "Ball launch나 Shot flow를 실행하지 않습니다.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Idle")) Preview(CharacterState.Idle);
                if (GUILayout.Button("Address")) Preview(CharacterState.Address);
                if (GUILayout.Button("BackSwing")) Preview(CharacterState.BackSwing);
                if (GUILayout.Button("Swing (Impact Disabled)")) Preview(CharacterState.Swing);
                if (GUILayout.Button("FollowThrough")) Preview(CharacterState.FollowThrough);
                if (GUILayout.Button("Putt Address")) Preview(CharacterState.PuttAddress);
                if (GUILayout.Button("Putt Swing (Impact Disabled)")) Preview(CharacterState.PuttSwing);
                if (GUILayout.Button("Birdie Celebration")) Preview(CharacterState.BirdieCelebration);
            }
        }

        private static void Preview(CharacterState state)
        {
            CharacterAnimationController controller = Object.FindAnyObjectByType<CharacterAnimationController>();
            if (controller == null)
            {
                Debug.LogWarning("Character preview requires a playing scene with CharacterAnimationController.");
                return;
            }

            controller.PreviewState(state);
            Debug.Log($"CHARACTER PREVIEW | {state} | Mode={controller.AnimationMode} | Impact disabled");
        }
    }
}
