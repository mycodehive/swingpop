using SwingPop.Gameplay.Wind;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Debugging
{
    public sealed class WindDebugInputController : MonoBehaviour
    {
        [SerializeField] private WindController wind;

        private InputAction calmAction;
        private InputAction tailwindAction;
        private InputAction headwindAction;
        private InputAction leftCrosswindAction;
        private InputAction rightCrosswindAction;

        private void Awake()
        {
            calmAction = CreateAction("M4 Calm", "<Keyboard>/6", "<Keyboard>/numpad6");
            tailwindAction = CreateAction("M4 Tailwind", "<Keyboard>/7", "<Keyboard>/numpad7");
            headwindAction = CreateAction("M4 Headwind", "<Keyboard>/8", "<Keyboard>/numpad8");
            leftCrosswindAction = CreateAction("M4 Left Crosswind", "<Keyboard>/9", "<Keyboard>/numpad9");
            rightCrosswindAction = CreateAction("M4 Right Crosswind", "<Keyboard>/0", "<Keyboard>/numpad0");
        }

        private void OnEnable()
        {
            Enable(calmAction, OnCalm);
            Enable(tailwindAction, OnTailwind);
            Enable(headwindAction, OnHeadwind);
            Enable(leftCrosswindAction, OnLeftCrosswind);
            Enable(rightCrosswindAction, OnRightCrosswind);
        }

        private void OnDisable()
        {
            Disable(calmAction, OnCalm);
            Disable(tailwindAction, OnTailwind);
            Disable(headwindAction, OnHeadwind);
            Disable(leftCrosswindAction, OnLeftCrosswind);
            Disable(rightCrosswindAction, OnRightCrosswind);
        }

        private void OnDestroy()
        {
            calmAction?.Dispose();
            tailwindAction?.Dispose();
            headwindAction?.Dispose();
            leftCrosswindAction?.Dispose();
            rightCrosswindAction?.Dispose();
        }

        private void OnCalm(InputAction.CallbackContext context) => wind?.SetPreset(WindPreset.Calm);
        private void OnTailwind(InputAction.CallbackContext context) => wind?.SetPreset(WindPreset.Tailwind);
        private void OnHeadwind(InputAction.CallbackContext context) => wind?.SetPreset(WindPreset.Headwind);
        private void OnLeftCrosswind(InputAction.CallbackContext context) => wind?.SetPreset(WindPreset.LeftCrosswind);
        private void OnRightCrosswind(InputAction.CallbackContext context) => wind?.SetPreset(WindPreset.RightCrosswind);

        private static InputAction CreateAction(string name, string primaryBinding, string secondaryBinding)
        {
            InputAction action = new(name, InputActionType.Button, primaryBinding);
            action.AddBinding(secondaryBinding);
            return action;
        }

        private static void Enable(InputAction action, System.Action<InputAction.CallbackContext> callback)
        {
            action.performed += callback;
            action.Enable();
        }

        private static void Disable(InputAction action, System.Action<InputAction.CallbackContext> callback)
        {
            action.performed -= callback;
            action.Disable();
        }
    }
}
