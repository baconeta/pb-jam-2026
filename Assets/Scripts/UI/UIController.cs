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
        [Header("HUD")]
        [Tooltip("Displays the player's current score.")]
        [SerializeField] private TMP_Text _scoreText;

        [Tooltip("Displays the current risk level.")]
        [SerializeField] private TMP_Text _riskText;

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
        }

        // ── HUD updates ───────────────────────────────────────────────────────────

        public void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = $"Score: {score}";
        }

        public void UpdateRiskLevel(int riskLevel)
        {
            if (_riskText != null) _riskText.text = $"Risk: {riskLevel}";
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

        /// <summary>Shows the game over panel and displays the final score.</summary>
        public void ShowGameOver(int finalScore)
        {
            SetGameOverPanelVisible(true);
            if (_gameOverScoreText != null) _gameOverScoreText.text = $"Final Score: {finalScore}";
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

        /// <summary>Builds and displays a formatted high score table from the provided entries.</summary>
        public void UpdateHighScoreDisplay(List<HighScoreEntry> scores)
        {
            if (_highScoreText == null) return;

            if (scores == null || scores.Count == 0)
            {
                _highScoreText.text = "No scores yet!";
                return;
            }

            System.Text.StringBuilder sb = new();
            sb.AppendLine("── HIGH SCORES ──");
            for (int i = 0; i < scores.Count; i++)
                sb.AppendLine($"{i + 1}.  {scores[i].playerName,-12}  {scores[i].score}");

            _highScoreText.text = sb.ToString();
        }
    }
}
