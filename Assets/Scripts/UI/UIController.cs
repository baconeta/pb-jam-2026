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
        [Header("HUD – Score")]
        [Tooltip("Displays banked (safe) score. Leave empty to skip.")]
        [SerializeField] private TMP_Text _bankedScoreText;

        [Tooltip("Displays run (at-risk) score. Leave empty to skip.")]
        [SerializeField] private TMP_Text _runScoreText;

        [Tooltip("Displays total score (banked + run). Leave empty to skip.")]
        [SerializeField] private TMP_Text _scoreText;

        [Header("HUD – Risk")]
        [Tooltip("Displays current risk level number.")]
        [SerializeField] private TMP_Text _riskText;

        [Tooltip("Displays current score multiplier (e.g. '2.0×'). Leave empty to skip.")]
        [SerializeField] private TMP_Text _multiplierText;

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
            SetCheckpointPanelVisible(false);
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

        // ── Checkpoint panel ──────────────────────────────────────────────────────

        public void SetCheckpointPanelVisible(bool visible)
        {
            if (_checkpointPanel != null) _checkpointPanel.SetActive(visible);
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
    }
}
