using System.Collections.Generic;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Wires UI elements to the board game flow.
    /// Assign all references in the Inspector – this script does not contain game logic.
    /// The GameManager calls the public methods here to push state into the UI.
    ///
    /// Requires TextMeshPro. If TMP types cause compile errors, run:
    ///   Window > TextMeshPro > Import TMP Essential Resources
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Juice Animator")]
        [Tooltip("UIJuiceAnimator component (any persistent GameObject). Drives all UI animations.")]
        [SerializeField] private UIJuiceAnimator _juice;

        [Header("HUD Root")]
        [Tooltip("Parent GameObject for the entire HUD. Hidden on game over, shown on restart.")]
        [SerializeField] private GameObject _hudRoot;

        [Header("HUD – Score")]
        [Tooltip("Displays banked (safe) score. Leave empty to skip.")]
        [SerializeField] private TMP_Text _bankedScoreText;

        [Tooltip("RectTransform of the banked score label (used for bump animation).")]
        [SerializeField] private RectTransform _bankedScoreRect;

        [Tooltip("Displays run (at-risk) score. Leave empty to skip.")]
        [SerializeField] private TMP_Text _runScoreText;

        [Tooltip("RectTransform of the run score label (used for bump animation).")]
        [SerializeField] private RectTransform _runScoreRect;

        [Tooltip("Displays total score (banked + run). Leave empty to skip.")]
        [SerializeField] private TMP_Text _scoreText;

        [Header("HUD – Risk")]
        [Tooltip("Displays current risk level number.")]
        [SerializeField] private TMP_Text _riskText;

        [Tooltip("RectTransform of the risk label (used for wobble animation).")]
        [SerializeField] private RectTransform _riskRect;

        [Tooltip("Displays current score multiplier (e.g. '2.0×'). Leave empty to skip.")]
        [SerializeField] private TMP_Text _multiplierText;

        [Tooltip("RectTransform of the multiplier label (used for punch animation).")]
        [SerializeField] private RectTransform _multiplierRect;

        [Header("HUD – Dice")]
        [Tooltip("Shows the dice roll result (e.g. '5' or '3 + 4 = 7').")]
        [SerializeField] private TMP_Text _diceResultText;

        [Header("Roll Buttons")]
        [Tooltip("'Roll 1 Die' button – wire its onClick to BoardGameManager.RollOneDie().")]
        [SerializeField] private Button _rollOneDieButton;

        [Tooltip("'Roll 2 Dice' button – wire its onClick to BoardGameManager.RollTwoDice().")]
        [SerializeField] private Button _rollTwoDiceButton;

        [Header("Checkpoint Panel")]
        [Tooltip("Parent panel containing the Take / Skip buttons. Shown at checkpoint.")]
        [SerializeField] private GameObject _checkpointPanel;

        [Tooltip("CanvasGroup on the checkpoint panel (used for fade-in). Add one if missing.")]
        [SerializeField] private CanvasGroup _checkpointPanelGroup;

        [Tooltip("RectTransform of the checkpoint panel (used for slap-in animation).")]
        [SerializeField] private RectTransform _checkpointPanelRect;

        [Tooltip("Headline text on the checkpoint panel (e.g. 'CHECKPOINT!').")]
        [SerializeField] private TMP_Text _checkpointTitleText;

        [Tooltip("Description text on the checkpoint panel showing run score and multiplier info.")]
        [SerializeField] private TMP_Text _checkpointDescriptionText;

        [Tooltip("Warning text shown when at max risk level (e.g. 'Max risk reached!').")]
        [SerializeField] private TMP_Text _checkpointWarningText;

        [Tooltip("Text showing what the multiplier will be after skipping (e.g. 'Next: 4.0×').")]
        [SerializeField] private TMP_Text _nextMultiplierText;

        [Tooltip("The 'Take / Bank' button on the checkpoint panel.")]
        [SerializeField] private Button _bankButton;

        [Tooltip("The 'Skip' button on the checkpoint panel.")]
        [SerializeField] private Button _skipButton;

        [Header("Game Over Panel")]
        [Tooltip("Panel shown when the player hits a Negative tile.")]
        [SerializeField] private GameObject _gameOverPanel;

        [Header("Standalone High Score Panel (optional)")]
        [Tooltip("Optional separate panel just for viewing high scores (e.g. a main-menu leaderboard). " +
                 "Leave empty to skip. Wire a 'View Scores' button to BoardGameManager.ToggleHighScorePanel().")]
        [SerializeField] private GameObject _highScoreStandalonePanel;

        [Tooltip("Text inside the standalone panel (can be the same TMP_Text as _highScoreText if you prefer).")]
        [SerializeField] private TMP_Text _highScoreStandaloneText;

        [Tooltip("Displays the final score inside the game over panel.")]
        [SerializeField] private TMP_Text _gameOverScoreText;

        [Tooltip("Input field for the player to type their name before saving a high score.")]
        [SerializeField] private TMP_InputField _nameInputField;

        [Tooltip("Displays the top-5 high score table (usually inside the game over panel).")]
        [SerializeField] private TMP_Text _highScoreText;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Start with panels hidden; the GameManager controls their visibility.
            HideCheckpointPanel();
            SetGameOverPanelVisible(false);
            if (_highScoreStandalonePanel != null) _highScoreStandalonePanel.SetActive(false);
        }

        // ── HUD updates ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the split score display: banked (safe), run (at-risk), and total.
        /// Assign the three TMP_Text refs in the Inspector to see the breakdown.
        /// Any refs left unassigned are silently skipped.
        /// </summary>
        public void UpdateScoreUI(int banked, int run)
        {
            if (_bankedScoreText != null) _bankedScoreText.text = $"Banked: {banked}";
            if (_runScoreText    != null) _runScoreText.text    = $"At Risk: {run}";
            if (_scoreText       != null) _scoreText.text       = $"Total: {banked + run}";
        }

        /// <summary>
        /// Updates the risk level and multiplier display.
        /// Assign _riskText and/or _multiplierText in the Inspector.
        /// </summary>
        public void UpdateRiskUI(int riskLevel, float multiplier)
        {
            if (_riskText      != null) _riskText.text      = $"Risk: {riskLevel}";
            if (_multiplierText != null) _multiplierText.text = $"{multiplier:F1}×";
        }

        // ── Individual setters (kept for backward compat or targeted use) ─────────

        /// <summary>Updates total score text only. Prefer UpdateScoreUI() for the split display.</summary>
        public void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = $"Total: {score}";
        }

        /// <summary>Updates risk level text only. Prefer UpdateRiskUI() to also show multiplier.</summary>
        public void UpdateRiskLevel(int riskLevel)
        {
            if (_riskText != null) _riskText.text = $"Risk: {riskLevel}";
        }

        public void UpdateMultiplier(float multiplier)
        {
            if (_multiplierText != null) _multiplierText.text = $"{multiplier:F1}×";
        }

        public void UpdateBankedScore(int banked)
        {
            if (_bankedScoreText != null) _bankedScoreText.text = $"Banked: {banked}";
        }

        public void UpdateRunScore(int run)
        {
            if (_runScoreText != null) _runScoreText.text = $"At Risk: {run}";
        }

        public void UpdateDiceResult(string result)
        {
            if (_diceResultText != null) _diceResultText.text = result;
        }

        // ── Button state ──────────────────────────────────────────────────────────

        /// <summary>Enables or disables both roll buttons. Pass false while movement is happening.</summary>
        public void SetRollButtonsInteractable(bool interactable)
        {
            if (_rollOneDieButton  != null) _rollOneDieButton.interactable  = interactable;
            if (_rollTwoDiceButton != null) _rollTwoDiceButton.interactable = interactable;
        }

        // ── HUD show / hide ───────────────────────────────────────────────────────

        public void ShowHud()
        {
            if (_hudRoot != null) _hudRoot.SetActive(true);
        }

        public void HideHud()
        {
            if (_hudRoot != null) _hudRoot.SetActive(false);
        }

        // ── Checkpoint panel ──────────────────────────────────────────────────────

        /// <summary>Legacy setter kept for any direct callers; prefer ShowCheckpointPanel / HideCheckpointPanel.</summary>
        public void SetCheckpointPanelVisible(bool visible)
        {
            if (visible) ShowCheckpointPanel(0, 0, 1f, 1f, false);
            else         HideCheckpointPanel();
        }

        /// <summary>
        /// Populates and animates the checkpoint panel into view.
        /// </summary>
        public void ShowCheckpointPanel(int runScore, int banked, float currentMult, float nextMult, bool atMaxRisk)
        {
            if (_checkpointPanel == null) return;
            _checkpointPanel.SetActive(true);

            if (_checkpointTitleText != null)
                _checkpointTitleText.text = "CHECKPOINT!";

            if (_checkpointDescriptionText != null)
                _checkpointDescriptionText.text =
                    $"At-risk score: {runScore}  ×  {currentMult:F1} = +{Mathf.RoundToInt(runScore * currentMult)}\n" +
                    $"Banked: {banked}";

            if (_checkpointWarningText != null)
                _checkpointWarningText.gameObject.SetActive(atMaxRisk);

            if (_nextMultiplierText != null)
            {
                _nextMultiplierText.gameObject.SetActive(!atMaxRisk);
                if (!atMaxRisk)
                    _nextMultiplierText.text = $"Skip → {nextMult:F1}×";
            }

            if (_skipButton != null)
                _skipButton.interactable = !atMaxRisk;

            if (_juice != null && _checkpointPanelRect != null)
                _juice.SlapIn(_checkpointPanelRect, _checkpointPanelGroup);
            else if (_checkpointPanelGroup != null)
                _checkpointPanelGroup.alpha = 1f;
        }

        public void HideCheckpointPanel()
        {
            if (_checkpointPanel == null) return;
            _checkpointPanel.SetActive(false);
        }

        // ── Game over panel ───────────────────────────────────────────────────────

        public void SetGameOverPanelVisible(bool visible)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(visible);
        }

        /// <summary>
        /// Shows the game over panel with a banked/run score breakdown.
        /// Total = banked + run.
        /// </summary>
        public void ShowGameOver(int bankedScore, int runScore)
        {
            HideHud();
            SetGameOverPanelVisible(true);
            int total = bankedScore + runScore;
            if (_gameOverScoreText != null)
                _gameOverScoreText.text = $"Final Score: {total}\nBanked: {bankedScore}  +  At-Risk: {runScore}";
        }

        // ── Name input ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the name the player typed, trimmed. Falls back to "Player" if blank
        /// or if no input field is assigned.
        /// </summary>
        public string GetEnteredName()
        {
            if (_nameInputField == null) return "Player";
            string entered = _nameInputField.text.Trim();
            return string.IsNullOrEmpty(entered) ? "Player" : entered;
        }

        // ── High scores ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and displays a formatted high score table from the provided entries.
        /// Writes to _highScoreText (game over panel) and also to _highScoreStandaloneText
        /// if it is assigned, keeping both in sync automatically.
        /// </summary>
        public void UpdateHighScoreDisplay(List<HighScoreEntry> scores)
        {
            string text = BuildHighScoreText(scores);
            if (_highScoreText         != null) _highScoreText.text         = text;
            if (_highScoreStandaloneText != null) _highScoreStandaloneText.text = text;
        }

        /// <summary>
        /// Toggles the standalone high score panel (if assigned).
        /// BoardGameManager.ToggleHighScorePanel() calls this after refreshing the scores.
        /// </summary>
        public void ToggleStandaloneHighScorePanel()
        {
            if (_highScoreStandalonePanel == null) return;
            bool next = !_highScoreStandalonePanel.activeSelf;
            _highScoreStandalonePanel.SetActive(next);
            Debug.Log($"[UIController] Standalone high score panel {(next ? "shown" : "hidden")}.");
        }

        private static string BuildHighScoreText(List<HighScoreEntry> scores)
        {
            if (scores == null || scores.Count == 0)
                return "No scores yet!";

            System.Text.StringBuilder sb = new();
            sb.AppendLine("── HIGH SCORES ──");
            for (int i = 0; i < scores.Count; i++)
                sb.AppendLine($"{i + 1}.  {scores[i].playerName,-12}  {scores[i].score}");
            return sb.ToString();
        }

        // ── Juice helpers (called by GameManager after score/state changes) ────────

        public void AnimateBankedScoreChanged()
        {
            if (_juice == null || _bankedScoreRect == null) return;
            _juice.Bump(_bankedScoreRect);
        }

        public void AnimateRunScoreChanged()
        {
            if (_juice == null || _runScoreRect == null) return;
            _juice.Bump(_runScoreRect);
        }

        public void AnimateMultiplierChanged()
        {
            if (_juice == null || _multiplierRect == null) return;
            _juice.PunchText(_multiplierRect);
        }

        public void AnimateRiskChanged()
        {
            if (_juice == null || _riskRect == null) return;
            _juice.Wobble(_riskRect);
        }
    }
}
