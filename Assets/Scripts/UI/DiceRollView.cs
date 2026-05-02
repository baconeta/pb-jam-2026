using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Controls the screen-space dice panel that displays the DiceCamera RenderTexture.
    ///
    /// Attach to the DiceRollPanel GameObject (child of your main Canvas).
    ///
    /// INSPECTOR SETUP:
    ///   _dicePanelCanvasGroup – CanvasGroup on DiceRollPanel (add one if missing).
    ///   _dicePanelRoot        – RectTransform of DiceRollPanel.
    ///   _diceRenderImage      – RawImage inside the panel; assign RT_DiceRoll as its texture.
    ///   _juice                – optional UIJuiceAnimator for animated entry/exit.
    ///
    /// The panel is hidden in Awake. DiceRoller calls Show()/Hide() automatically
    /// when _showDicePanelDuringRoll is enabled on the DiceRoller.
    /// </summary>
    public class DiceRollView : MonoBehaviour
    {
        [Header("Panel References")]
        [Tooltip("CanvasGroup on this panel. Used for instant alpha toggle (and juice fade if assigned).")]
        [SerializeField] private CanvasGroup _dicePanelCanvasGroup;

        [Tooltip("RectTransform of this panel. Used for juice animation.")]
        [SerializeField] private RectTransform _dicePanelRoot;

        [Tooltip("RawImage that displays the DiceCamera RenderTexture.")]
        [SerializeField] private RawImage _diceRenderImage;

        [Header("Animation (optional)")]
        [Tooltip("UIJuiceAnimator for pop-in/out. Leave empty for instant show/hide.")]
        [SerializeField] private UIJuiceAnimator _juice;

        private void Awake()
        {
            SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Show()
        {
            SetActive(true);

            if (_juice != null && _dicePanelRoot != null)
                _juice.PopIn(_dicePanelRoot);
            else if (_dicePanelCanvasGroup != null)
                _dicePanelCanvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            // Instant hide – no linger animation so the panel doesn't overlap gameplay.
            SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible) Show(); else Hide();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void SetActive(bool active)
        {
            if (_dicePanelRoot        != null) _dicePanelRoot.gameObject.SetActive(active);
            if (_dicePanelCanvasGroup != null) _dicePanelCanvasGroup.alpha = active ? 1f : 0f;
        }
    }
}
