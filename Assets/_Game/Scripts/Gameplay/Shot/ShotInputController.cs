using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Gameplay.Shot
{
    public sealed class ShotInputController : MonoBehaviour
    {
        [SerializeField] private ShotFlowController shotFlow;

        private InputAction aimAction;
        private InputAction confirmAction;
        private InputAction resetAction;
        private InputAction cancelAction;
        private InputAction forcePerfectAction;
        private InputAction noSpinAction;
        private InputAction topSpinAction;
        private InputAction backSpinAction;
        private InputAction leftSideSpinAction;
        private InputAction rightSideSpinAction;

        private void Awake()
        {
            aimAction = new InputAction("M2 Aim", InputActionType.Value, expectedControlType: "Axis");
            aimAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            aimAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            aimAction.AddBinding("<Gamepad>/leftStick/x");

            confirmAction = new InputAction("M2 Confirm", InputActionType.Button, "<Keyboard>/space");
            confirmAction.AddBinding("<Gamepad>/buttonSouth");
            resetAction = new InputAction("M2 Reset", InputActionType.Button, "<Keyboard>/r");
            resetAction.AddBinding("<Gamepad>/buttonNorth");
            cancelAction = new InputAction("M2 Cancel", InputActionType.Button, "<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");
            forcePerfectAction = new InputAction("M2 Force Perfect", InputActionType.Button, "<Keyboard>/p");
            noSpinAction = CreateKeyboardAction("M3 No Spin", "<Keyboard>/1", "<Keyboard>/numpad1");
            topSpinAction = CreateKeyboardAction("M3 Top Spin", "<Keyboard>/2", "<Keyboard>/numpad2");
            backSpinAction = CreateKeyboardAction("M3 Back Spin", "<Keyboard>/3", "<Keyboard>/numpad3");
            leftSideSpinAction = CreateKeyboardAction("M3 Left Side Spin", "<Keyboard>/4", "<Keyboard>/numpad4");
            rightSideSpinAction = CreateKeyboardAction("M3 Right Side Spin", "<Keyboard>/5", "<Keyboard>/numpad5");
        }

        private void OnEnable()
        {
            confirmAction.performed += OnConfirmPerformed;
            resetAction.performed += OnResetPerformed;
            cancelAction.performed += OnCancelPerformed;
            forcePerfectAction.performed += OnForcePerfectPerformed;
            noSpinAction.performed += OnNoSpinPerformed;
            topSpinAction.performed += OnTopSpinPerformed;
            backSpinAction.performed += OnBackSpinPerformed;
            leftSideSpinAction.performed += OnLeftSideSpinPerformed;
            rightSideSpinAction.performed += OnRightSideSpinPerformed;
            aimAction.Enable();
            confirmAction.Enable();
            resetAction.Enable();
            cancelAction.Enable();
            forcePerfectAction.Enable();
            noSpinAction.Enable();
            topSpinAction.Enable();
            backSpinAction.Enable();
            leftSideSpinAction.Enable();
            rightSideSpinAction.Enable();
        }

        private void Update()
        {
            if (shotFlow != null)
            {
                shotFlow.SetAimInput(aimAction.ReadValue<float>());
            }
        }

        private void OnDisable()
        {
            confirmAction.performed -= OnConfirmPerformed;
            resetAction.performed -= OnResetPerformed;
            cancelAction.performed -= OnCancelPerformed;
            forcePerfectAction.performed -= OnForcePerfectPerformed;
            noSpinAction.performed -= OnNoSpinPerformed;
            topSpinAction.performed -= OnTopSpinPerformed;
            backSpinAction.performed -= OnBackSpinPerformed;
            leftSideSpinAction.performed -= OnLeftSideSpinPerformed;
            rightSideSpinAction.performed -= OnRightSideSpinPerformed;
            aimAction.Disable();
            confirmAction.Disable();
            resetAction.Disable();
            cancelAction.Disable();
            forcePerfectAction.Disable();
            noSpinAction.Disable();
            topSpinAction.Disable();
            backSpinAction.Disable();
            leftSideSpinAction.Disable();
            rightSideSpinAction.Disable();
            shotFlow?.SetAimInput(0f);
        }

        private void OnDestroy()
        {
            aimAction?.Dispose();
            confirmAction?.Dispose();
            resetAction?.Dispose();
            cancelAction?.Dispose();
            forcePerfectAction?.Dispose();
            noSpinAction?.Dispose();
            topSpinAction?.Dispose();
            backSpinAction?.Dispose();
            leftSideSpinAction?.Dispose();
            rightSideSpinAction?.Dispose();
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.ConfirmCurrentStep();
        }

        private void OnResetPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.ResetShot();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.CancelToAiming();
        }

        private void OnForcePerfectPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.ForcePerfectImpactAndCommit();
        }

        private void OnNoSpinPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.SetSpinPreset(SpinPreset.NoSpin);
        }

        private void OnTopSpinPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.SetSpinPreset(SpinPreset.TopSpin);
        }

        private void OnBackSpinPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.SetSpinPreset(SpinPreset.BackSpin);
        }

        private void OnLeftSideSpinPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.SetSpinPreset(SpinPreset.LeftSideSpin);
        }

        private void OnRightSideSpinPerformed(InputAction.CallbackContext context)
        {
            shotFlow?.SetSpinPreset(SpinPreset.RightSideSpin);
        }

        private static InputAction CreateKeyboardAction(string name, string primaryBinding, string secondaryBinding)
        {
            InputAction action = new InputAction(name, InputActionType.Button, primaryBinding);
            action.AddBinding(secondaryBinding);
            return action;
        }
    }
}
