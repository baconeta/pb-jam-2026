using System.Collections;
using Board;
using UI;
using UnityEngine;
using Utils;

namespace Game
{
    /// <summary>
    /// Central coordinator for the board game.
    /// Runs a simple coroutine-driven state machine.
    ///
    /// State flow:
    ///   WaitingForRoll → (player rolls) → Rolling → Moving
    ///       → checkpoint hit?  → AtCheckpoint → (Take/Skip) → WaitingForRoll
    ///       → normal land?     → ResolvingTile
    ///           → Positive → WaitingForRoll
    ///           → Negative → GameOver
    ///           → Empty    → WaitingForRoll
    ///
    /// INSPECTOR SETUP:
    ///   1. Add this script to a GameManager GameObject in the scene.
    ///   2. Assign all [SerializeField] references in the Inspector.
    ///   3. Wire UI buttons:
    ///        Roll 1 Die button  → BoardGameManager.RollOneDie()
    ///        Roll 2 Dice button → BoardGameManager.RollTwoDice()
    ///        Take button        → BoardGameManager.TakeCheckpoint()
    ///        Skip button        → BoardGameManager.SkipCheckpoint()
    ///        Restart button     → BoardGameManager.RestartGame()
    ///        Submit Score button→ BoardGameManager.SubmitHighScore()
    /// </summary>
    public class BoardGameManager : Singleton<BoardGameManager>
    {
        // ── State enum ────────────────────────────────────────────────────────────

        public enum GameState
        {
            WaitingForRoll,
            Rolling,
            Moving,
            ResolvingTile,
            AtCheckpoint,
            GameOver
        }

        // ── Inspector references ──────────────────────────────────────────────────

        [Header("Scene References")]
        [SerializeField] private BoardManager    _boardManager;
        [SerializeField] private BoardPlayer     _player;
        [SerializeField] private CameraController _camera;
        [SerializeField] private UIController    _ui;
        [SerializeField] private ScoreManager    _scoreManager;

        [Header("Dice")]
        [Tooltip("Number of faces on each die (default: d6).")]
        [SerializeField] private int _dieFaces = 6;

        [Tooltip("When true, rolling two dice awards a flat score bonus on Positive tile landing.")]
        [SerializeField] private bool _twoDiceBonus = false;

        [Tooltip("Score added when _twoDiceBonus is true and the player rolls two dice.")]
        [SerializeField] private int _twoDiceBonusAmount = 50;

        [Header("Timing")]
        [Tooltip("Seconds to display the dice result before the token starts moving.")]
        [SerializeField] private float _diceDisplayDuration = 0.8f;

        [Tooltip("Seconds to wait after the camera zooms out before showing the checkpoint panel.")]
        [SerializeField] private float _checkpointRevealDelay = 0.5f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        public GameState CurrentState { get; private set; } = GameState.WaitingForRoll;

        // Remembered so ResolveLandedTile knows whether to apply the two-dice bonus.
        private bool _lastRollWasTwoDice;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            StartGame();
        }

        // ── Public API – wire these to UI buttons ─────────────────────────────────

        /// <summary>Roll one d6 and move. No-op if not waiting for a roll.</summary>
        public void RollOneDie()
        {
            if (CurrentState != GameState.WaitingForRoll) return;
            _lastRollWasTwoDice = false;
            StartCoroutine(RollAndMove(1));
        }

        /// <summary>Roll two d6 and move. No-op if not waiting for a roll.</summary>
        public void RollTwoDice()
        {
            if (CurrentState != GameState.WaitingForRoll) return;
            _lastRollWasTwoDice = true;
            StartCoroutine(RollAndMove(2));
        }

        /// <summary>
        /// Player chose to Take the checkpoint: reset risk to 0 and reshuffle the board.
        /// No-op if not at the checkpoint decision.
        /// </summary>
        public void TakeCheckpoint()
        {
            if (CurrentState != GameState.AtCheckpoint) return;
            _scoreManager.TakeCheckpoint();
            ResolveCheckpointDecision();
        }

        /// <summary>
        /// Player chose to Skip the checkpoint: increase risk by 1 and reshuffle the board.
        /// No-op if not at the checkpoint decision.
        /// </summary>
        public void SkipCheckpoint()
        {
            if (CurrentState != GameState.AtCheckpoint) return;
            _scoreManager.SkipCheckpoint();
            ResolveCheckpointDecision();
        }

        /// <summary>Restarts the game from the beginning. Safe to call from any state.</summary>
        public void RestartGame()
        {
            StopAllCoroutines();
            StartGame();
        }

        /// <summary>
        /// Reads the player's name from the UI, saves the score to the high score table,
        /// and refreshes the high score display. Call from a Submit button on the game over screen.
        /// </summary>
        public void SubmitHighScore()
        {
            if (_ui == null) return;
            string name  = _ui.GetEnteredName();
            int    score = _scoreManager.GetTotalScore();
            Debug.Log($"[BoardGameManager] Submitting high score – name: '{name}', score: {score} " +
                      $"(banked {_scoreManager.BankedScore} + run {_scoreManager.RunScore}).");
            HighScoreManager.SaveScore(name, score);
            RefreshHighScoreUI();
        }

        /// <summary>
        /// Shows or hides the standalone high score panel (if one is assigned in UIController).
        /// Wire this to a "View Scores" button anywhere in your UI.
        /// </summary>
        public void ToggleHighScorePanel()
        {
            RefreshHighScoreUI();
            _ui?.ToggleStandaloneHighScorePanel();
        }

        // ── Game flow ─────────────────────────────────────────────────────────────

        private void StartGame()
        {
            Debug.Log("[BoardGameManager] Game starting.");
            _scoreManager.ResetSession();
            _player.PlaceAtCheckpoint();
            _boardManager.ShuffleContents(_scoreManager.RiskLevel);

            if (_camera != null) _camera.SetFollowMode();

            RefreshHUD();
            if (_ui != null)
            {
                _ui.SetCheckpointPanelVisible(false);
                _ui.SetGameOverPanelVisible(false);
                _ui.UpdateDiceResult(string.Empty);
                RefreshHighScoreUI(); // Show existing scores on the game over panel from the start.
            }

            SetState(GameState.WaitingForRoll);
        }

        private IEnumerator RollAndMove(int diceCount)
        {
            SetState(GameState.Rolling);

            // ── Roll dice ──────────────────────────────────────────────────────────
            int    total;
            string displayText;

            if (diceCount == 1)
            {
                int roll = Random.Range(1, _dieFaces + 1);
                total       = roll;
                displayText = $"Rolled: {roll}";
            }
            else
            {
                int r1 = Random.Range(1, _dieFaces + 1);
                int r2 = Random.Range(1, _dieFaces + 1);
                total       = r1 + r2;
                displayText = $"Rolled: {r1} + {r2} = {total}";
            }

            Debug.Log($"[BoardGameManager] Dice roll – {diceCount} die/dice, result: {displayText}, moving {total} step(s).");
            if (_ui != null) _ui.UpdateDiceResult(displayText);

            // Brief pause so the player can read the roll result.
            yield return new WaitForSeconds(_diceDisplayDuration);

            // ── Move ───────────────────────────────────────────────────────────────
            SetState(GameState.Moving);
            if (_camera != null) _camera.SetFollowMode();

            _player.StartMovement(total);
            yield return new WaitUntil(() => !_player.IsMoving);

            // ── Resolve ────────────────────────────────────────────────────────────
            if (_player.StoppedAtCheckpoint)
            {
                yield return StartCoroutine(HandleCheckpointReached());
            }
            else
            {
                yield return StartCoroutine(ResolveLandedTile());
            }
        }

        private IEnumerator HandleCheckpointReached()
        {
            Debug.Log($"[BoardGameManager] Checkpoint reached. Banked: {_scoreManager.BankedScore}, " +
                      $"at-risk run: {_scoreManager.RunScore}, risk level: {_scoreManager.RiskLevel}, " +
                      $"multiplier: {_scoreManager.CurrentMultiplier:F2}x.");
            SetState(GameState.AtCheckpoint);

            if (_camera != null) _camera.SetCheckpointMode();

            // Reshuffle now using the pre-decision risk level so the player can see
            // what the board looks like before they commit to Take or Skip.
            _boardManager.ShuffleContents(_scoreManager.RiskLevel);

            yield return new WaitForSeconds(_checkpointRevealDelay);

            if (_ui != null) _ui.SetCheckpointPanelVisible(true);
            // State stays AtCheckpoint – waiting for TakeCheckpoint() or SkipCheckpoint().
        }

        private void ResolveCheckpointDecision()
        {
            Debug.Log($"[BoardGameManager] Checkpoint decision resolved. Risk now: {_scoreManager.RiskLevel}, multiplier: {_scoreManager.CurrentMultiplier:F2}x.");
            if (_ui != null) _ui.SetCheckpointPanelVisible(false);

            // Reshuffle again now that the risk level has changed.
            _boardManager.ShuffleContents(_scoreManager.RiskLevel);

            RefreshHUD();
            if (_camera != null) _camera.SetFollowMode();
            SetState(GameState.WaitingForRoll);
        }

        private IEnumerator ResolveLandedTile()
        {
            SetState(GameState.ResolvingTile);

            BoardTile tile = _player.CurrentTile;
            if (tile != null)
            {
                Debug.Log($"[BoardGameManager] Landed on tile index {tile.Index} – content: {tile.CurrentContent}.");

                switch (tile.CurrentContent)
                {
                    case TileContent.Positive:
                        int awarded = _scoreManager.AwardPositiveScore();

                        // Optional bonus for choosing to roll two dice.
                        // Controlled by the _twoDiceBonus flag in the Inspector (default: off).
                        // AddFlatBonus() applies the points to the actual score – not just the display.
                        if (_twoDiceBonus && _lastRollWasTwoDice)
                        {
                            _scoreManager.AddFlatBonus(_twoDiceBonusAmount);
                            awarded += _twoDiceBonusAmount;
                        }

                        if (_ui != null) _ui.UpdateDiceResult($"+{awarded}!");
                        RefreshHUD();
                        yield return new WaitForSeconds(0.6f);
                        break;

                    case TileContent.Negative:
                        Debug.Log("[BoardGameManager] Negative tile hit!");
                        // Currently triggers game over.
                        // TO CHANGE: replace TriggerGameOver() here with e.g. ReturnToCheckpoint().
                        // ScoreManager and BoardManager don't need to change – only this block.
                        yield return StartCoroutine(TriggerGameOver());
                        yield break; // Don't fall through to WaitingForRoll.

                    case TileContent.Empty:
                    default:
                        // Nothing happens on empty tiles.
                        break;
                }
            }

            SetState(GameState.WaitingForRoll);
        }

        private IEnumerator TriggerGameOver()
        {
            Debug.Log($"[BoardGameManager] Game over. Banked: {_scoreManager.BankedScore}, " +
                      $"at-risk: {_scoreManager.RunScore}, total: {_scoreManager.GetTotalScore()}, " +
                      $"risk level was: {_scoreManager.RiskLevel}.");
            SetState(GameState.GameOver);

            if (_ui != null)
            {
                _ui.ShowGameOver(_scoreManager.BankedScore, _scoreManager.RunScore);
                RefreshHighScoreUI();
            }

            yield return null; // Placeholder – add a delay or animation here if needed.
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetState(GameState newState)
        {
            Debug.Log($"[BoardGameManager] State: {CurrentState} → {newState}.");
            CurrentState = newState;
            // Roll buttons are interactable only when waiting for player input.
            if (_ui != null)
                _ui.SetRollButtonsInteractable(newState == GameState.WaitingForRoll);
        }

        private void RefreshHUD()
        {
            if (_ui == null) return;
            _ui.UpdateScoreUI(_scoreManager.BankedScore, _scoreManager.RunScore);
            _ui.UpdateRiskUI(_scoreManager.RiskLevel, _scoreManager.CurrentMultiplier);
        }

        private void RefreshHighScoreUI()
        {
            _ui?.UpdateHighScoreDisplay(HighScoreManager.GetTopScores());
        }
    }
}
