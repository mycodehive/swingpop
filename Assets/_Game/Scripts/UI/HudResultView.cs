using SwingPop.Gameplay.Hole;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class HudResultView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text holeText;
        [SerializeField] private Text parText;
        [SerializeField] private Text strokesText;
        [SerializeField] private Text resultText;

        private float elapsed;
        private float showDuration = 0.42f;
        private bool showing;

        public bool IsVisible => root != null && root.activeSelf;
        public string ResultLabel => resultText != null ? resultText.text : string.Empty;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(int holeNumber, ScoreResult result, float animationDuration)
        {
            holeText.text = $"HOLE {holeNumber}";
            parText.text = $"PAR {result.Par}";
            strokesText.text = $"STROKES {result.Strokes}";
            resultText.text = HudPresentationMapper.FormatResultRelative(result);
            showDuration = Mathf.Max(0.05f, animationDuration);
            elapsed = 0f;
            showing = true;
            root.SetActive(true);
            canvasGroup.alpha = 0f;
            panel.localScale = Vector3.one * 0.84f;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!showing)
            {
                return;
            }

            elapsed += unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / showDuration);
            float eased = 1f - (1f - t) * (1f - t);
            canvasGroup.alpha = eased;
            panel.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, eased);
            showing = t < 1f;
        }

        public void HideImmediate()
        {
            showing = false;
            elapsed = 0f;
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
