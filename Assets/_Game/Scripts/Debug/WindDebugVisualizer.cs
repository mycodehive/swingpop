using SwingPop.Gameplay.Wind;
using UnityEngine;

namespace SwingPop.Debugging
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class WindDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private WindController wind;
        [SerializeField] private Transform anchor;
        [SerializeField, Min(0f)] private float metersPerStrength = 1.2f;
        [SerializeField] private Vector3 worldOffset = new(0f, 2.5f, 0f);

        private LineRenderer windLine;

        private void Awake()
        {
            windLine = GetComponent<LineRenderer>();
        }

        private void LateUpdate()
        {
            if (wind == null || windLine == null)
            {
                return;
            }

            Vector3 origin = (anchor != null ? anchor.position : transform.position) + worldOffset;
            Vector3 vector = wind.Direction * (wind.Strength * metersPerStrength);
            windLine.enabled = vector.sqrMagnitude > Mathf.Epsilon;
            if (!windLine.enabled)
            {
                return;
            }

            windLine.SetPosition(0, origin);
            windLine.SetPosition(1, origin + vector);
            Debug.DrawRay(origin, vector, Color.magenta);
        }
    }
}
