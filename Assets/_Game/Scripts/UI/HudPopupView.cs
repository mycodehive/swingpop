using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class HudPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text messageText;

        private float remaining;
        private float duration;
        private float fadeDuration = 0.18f;
        private float startScale = 0.86f;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;
        public string Message => messageText != null ? messageText.text : string.Empty;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(string message, Color color, float visibleDuration, float fadeSeconds, bool strong)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = color;
            }

            duration = Mathf.Max(0.1f, visibleDuration);
            remaining = duration;
            fadeDuration = Mathf.Clamp(fadeSeconds, 0.01f, duration);
            startScale = strong ? 1.22f : 0.9f;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            if (panel != null)
            {
                panel.localScale = Vector3.one * startScale;
            }
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (remaining <= 0f)
            {
                return;
            }

            remaining = Mathf.Max(0f, remaining - unscaledDeltaTime);
            float elapsed = duration - remaining;
            if (panel != null)
            {
                float settle = 1f - Mathf.Exp(-elapsed * 13f);
                panel.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, settle);
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = remaining < fadeDuration ? remaining / fadeDuration : 1f;
            }
        }

        public void HideImmediate()
        {
            remaining = 0f;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }
    }
}
