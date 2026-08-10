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
        }

        private void OnEnable()
        {
            confirmAction.performed += OnConfirmPerformed;
            resetAction.performed += OnResetPerformed;
            cancelAction.performed += OnCancelPerformed;
            forcePerfectAction.performed += OnForcePerfectPerformed;
            aimAction.Enable();
            confirmAction.Enable();
            resetAction.Enable();
            cancelAction.Enable();
            forcePerfectAction.Enable();
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
            aimAction.Disable();
            confirmAction.Disable();
            resetAction.Disable();
            cancelAction.Disable();
            forcePerfectAction.Disable();
            shotFlow?.SetAimInput(0f);
        }

        private void OnDestroy()
        {
            aimAction?.Dispose();
            confirmAction?.Dispose();
            resetAction?.Dispose();
            cancelAction?.Dispose();
            forcePerfectAction?.Dispose();
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
    }
}
