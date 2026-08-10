using SwingPop.Gameplay.Ball;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Gameplay.Shot
{
    /// <summary>
    /// Fixed M1 debug commands only. Aim, power, and impact belong to M2.
    /// </summary>
    public sealed class TemporaryBallInput : MonoBehaviour
    {
        [SerializeField] private GolfBallController ball;

        private InputAction launchAction;
        private InputAction resetAction;

        private void Awake()
        {
            launchAction = new InputAction("M1 Launch", InputActionType.Button);
            launchAction.AddBinding("<Keyboard>/space");
            launchAction.AddBinding("<Gamepad>/buttonSouth");

            resetAction = new InputAction("M1 Reset", InputActionType.Button);
            resetAction.AddBinding("<Keyboard>/r");
            resetAction.AddBinding("<Gamepad>/buttonNorth");
        }

        private void OnEnable()
        {
            launchAction.performed += OnLaunchPerformed;
            resetAction.performed += OnResetPerformed;
            launchAction.Enable();
            resetAction.Enable();
        }

        private void OnDisable()
        {
            launchAction.performed -= OnLaunchPerformed;
            resetAction.performed -= OnResetPerformed;
            launchAction.Disable();
            resetAction.Disable();
        }

        private void OnDestroy()
        {
            launchAction?.Dispose();
            resetAction?.Dispose();
        }

        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            if (ball != null)
            {
                ball.Launch();
            }
        }

        private void OnResetPerformed(InputAction.CallbackContext context)
        {
            if (ball != null)
            {
                ball.ResetBall();
            }
        }
    }
}
