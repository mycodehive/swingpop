using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Debugging
{
    /// <summary>
    /// M0-only visual probe proving that the configured Input System receives input.
    /// It deliberately contains no golf or shot behavior.
    /// </summary>
    public sealed class FoundationInputProbe : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Scene References")]
        [SerializeField] private Transform inputIndicator;
        [SerializeField] private Renderer probeRenderer;

        [Header("Presentation")]
        [SerializeField, Min(0.05f)] private float indicatorRange = 0.75f;
        [SerializeField] private Color idleColor = new(0.15f, 0.85f, 0.75f, 1f);
        [SerializeField] private Color confirmColor = new(1f, 0.75f, 0.15f, 1f);

        private InputAction aimAction;
        private InputAction confirmAction;
        private MaterialPropertyBlock materialProperties;
        private Vector3 indicatorOrigin;

        private void Awake()
        {
            aimAction = new InputAction("Foundation Aim", InputActionType.Value, expectedControlType: "Vector2");
            aimAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            aimAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            aimAction.AddBinding("<Gamepad>/leftStick");

            confirmAction = new InputAction("Foundation Confirm", InputActionType.Button);
            confirmAction.AddBinding("<Keyboard>/space");
            confirmAction.AddBinding("<Gamepad>/buttonSouth");

            materialProperties = new MaterialPropertyBlock();
            if (inputIndicator != null)
            {
                indicatorOrigin = inputIndicator.localPosition;
            }
        }

        private void OnEnable()
        {
            aimAction?.Enable();
            confirmAction?.Enable();
        }

        private void Update()
        {
            Vector2 aim = aimAction.ReadValue<Vector2>();
            if (inputIndicator != null)
            {
                inputIndicator.localPosition = indicatorOrigin + new Vector3(aim.x, 0f, aim.y) * indicatorRange;
            }

            SetProbeColor(confirmAction.IsPressed() ? confirmColor : idleColor);
        }

        private void OnDisable()
        {
            aimAction?.Disable();
            confirmAction?.Disable();
        }

        private void OnDestroy()
        {
            aimAction?.Dispose();
            confirmAction?.Dispose();
        }

        private void SetProbeColor(Color color)
        {
            if (probeRenderer == null)
            {
                return;
            }

            probeRenderer.GetPropertyBlock(materialProperties);
            materialProperties.SetColor(BaseColorId, color);
            probeRenderer.SetPropertyBlock(materialProperties);
        }
    }
}
