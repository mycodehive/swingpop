using SwingPop.Data;
using SwingPop.Gameplay.Hole;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class HudResultView : MonoBehaviour
    {
        [SerializeField] private HudSkinData skin;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text holeText;
        [SerializeField] private Text parText;
        [SerializeField] private Text strokesText;
        [SerializeField] private Text resultText;
        [SerializeField] private Image accentImage;
        [SerializeField] private Image emblemImage;
        [SerializeField] private CanvasGroup scoreGroup;
        [SerializeField] private CanvasGroup detailGroup;

        private float elapsed;
        private float showDuration = 0.42f;
        private bool showing;
        private float scoreDelay;
        private float detailDelay;
        private Vector3 resultBaseScale = Vector3.one;

        public bool IsVisible => root != null && root.activeSelf;
        public string ResultLabel => resultText != null ? resultText.text : string.Empty;
        public float RevealProgress => showDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / showDuration);
        public bool HasStagedGroups => scoreGroup != null && detailGroup != null;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(int holeNumber, ScoreResult result, float animationDuration)
        {
            Show(holeNumber, result, animationDuration, 0f, 0f);
        }

        public void Show(
            int holeNumber,
            ScoreResult result,
            float animationDuration,
            float scoreRevealDelay,
            float detailRevealDelay)
        {
            holeText.text = $"HOLE {holeNumber}";
            parText.text = $"PAR {result.Par}";
            strokesText.text = $"STROKES {result.Strokes}";
            resultText.text = HudPresentationMapper.FormatResultRelative(result);
            if (skin != null)
            {
                Color accent = skin.Resolve(HudSkinStyleMapper.ForResult(result));
                resultText.color = accent;
                if (accentImage != null) accentImage.color = accent;
                if (emblemImage != null)
                {
                    emblemImage.color = new Color(accent.r, accent.g, accent.b, 0.2f);
                }
            }
            showDuration = Mathf.Max(0.05f, animationDuration);
            scoreDelay = Mathf.Max(0f, scoreRevealDelay);
            detailDelay = Mathf.Max(scoreDelay, detailRevealDelay);
            elapsed = 0f;
            showing = true;
            root.SetActive(true);
            canvasGroup.alpha = 0f;
            panel.localScale = Vector3.one * 0.84f;
            resultBaseScale = resultText != null ? resultText.rectTransform.localScale : Vector3.one;
            SetAlpha(scoreGroup, scoreDelay <= 0f ? 1f : 0f);
            SetAlpha(detailGroup, detailDelay <= 0f ? 1f : 0f);
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
            float scoreT = RevealAfter(scoreDelay, showDuration * 0.62f);
            float detailT = RevealAfter(detailDelay, showDuration * 0.7f);
            SetAlpha(scoreGroup, scoreT);
            SetAlpha(detailGroup, detailT);
            if (resultText != null)
            {
                float pop = 1f + Mathf.Sin(scoreT * Mathf.PI) * 0.12f;
                resultText.rectTransform.localScale = resultBaseScale * pop;
            }
            showing = t < 1f || scoreT < 1f || detailT < 1f;
        }

        public void HideImmediate()
        {
            showing = false;
            elapsed = 0f;
            if (root != null)
            {
                root.SetActive(false);
            }
            SetAlpha(scoreGroup, 0f);
            SetAlpha(detailGroup, 0f);
        }

        private float RevealAfter(float delay, float duration)
        {
            return Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.05f, duration));
        }

        private static void SetAlpha(CanvasGroup group, float value)
        {
            if (group != null)
            {
                group.alpha = value;
            }
        }
    }
}
