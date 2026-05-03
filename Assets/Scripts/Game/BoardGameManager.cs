using System.Collections;
using System.Collections.Generic;
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
            PreStartPreview,  // board visible, start panel shown; no dice input
            WaitingForRoll,
            Rolling,
            Moving,
            ResolvingTile,
            CheckpointPreview, // board reshuffled + zoomed out; decision UI not yet shown
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

        [Header("Dice Roller")]
        [Tooltip("DiceRoller component in the scene. Leave empty to use instant random dice (jam fallback).")]
        [SerializeField] private DiceRoller _diceRoller;

        [Header("Audio")]
        [Tooltip("BoardGameAudioController on this GameObject. Leave empty to play no sounds.")]
        [SerializeField] private BoardGameAudioController _audioController;

        [Header("Timing")]
        [Tooltip("Seconds to display the dice result before the token starts moving.")]
        [SerializeField] private float _diceDisplayDuration = 0.8f;

        [Tooltip("Seconds to wait after the camera finishes zooming out before showing the checkpoint panel.")]
        [SerializeField] private float _checkpointRevealDelay = 0.5f;


        // ── Runtime state ─────────────────────────────────────────────────────────

        public GameState CurrentState { get; private set; } = GameState.WaitingForRoll;

        // Remembered so ResolveLandedTile knows whether to apply the two-dice bonus.
        private bool _lastRollWasTwoDice;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            InitGame();
        }

        // ── Public API – wire these to UI buttons ─────────────────────────────────

        /// <summary>
        /// Called by the Start button during PreStartPreview.
        /// Hides the start panel, zooms in, and begins normal play.
        /// </summary>
        public void StartGame()
        {
            if (CurrentState != GameState.PreStartPreview) return;
            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            SetState(GameState.Moving); // blocks roll input while zooming in
            _ui?.HideStartPanel();
            _audioController?.PlayStartGame();
            if (_camera != null) _camera.SetFollowMode();
            if (_camera != null) yield return StartCoroutine(_camera.WaitForZoomComplete());
            _ui?.ShowHud();
            RefreshHUD();
            SetState(GameState.WaitingForRoll);
        }

        /// <summary>Roll one d6 and move. No-op if not waiting for a roll.</summary>
        public void RollOneDie()
        {
            if (CurrentState != GameState.WaitingForRoll) return;
            _audioController?.PlayButtonClick();
            _lastRollWasTwoDice = false;
            StartCoroutine(RollAndMove(1));
        }

        /// <summary>Roll two d6 and move. No-op if not waiting for a roll.</summary>
        public void RollTwoDice()
        {
            if (CurrentState != GameState.WaitingForRoll) return;
            _audioController?.PlayButtonClick();
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
            StartCoroutine(ResolveCheckpointDecisionRoutine(tookCheckpoint: true));
        }

        /// <summary>
        /// Player chose to Skip the checkpoint: increase risk by 1 and reshuffle the board.
        /// No-op if not at the checkpoint decision.
        /// </summary>
        public void SkipCheckpoint()
        {
            if (CurrentState != GameState.AtCheckpoint) return;
            _scoreManager.SkipCheckpoint();
            StartCoroutine(ResolveCheckpointDecisionRoutine(tookCheckpoint: false));
        }

        /// <summary>Restarts the game from the beginning. Safe to call from any state.</summary>
        public void RestartGame()
        {
            StopAllCoroutines();
            InitGame();
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
            _audioController?.PlayHighScoreSaved();
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

        // Initialises a fresh game session and enters PreStartPreview.
        // Board is shuffled and zoomed out so the player can inspect before pressing Start.
        private void InitGame()
        {
            Debug.Log("[BoardGameManager] Initialising game – entering board preview.");
            _scoreManager.ResetSession();
            _player.PlaceAtCheckpoint();
            _boardManager.ShuffleContents(0);
            _audioController?.PlayBoardShuffle();

            if (_camera != null) _camera.SetCheckpointMode(); // zoomed out from the start

            RefreshHUD();
            if (_ui != null)
            {
                _ui.HideHud();
                _ui.HideCheckpointPanel();
                _ui.SetGameOverPanelVisible(false);
                _ui.ShowStartPanel();
                _ui.UpdateDiceResult(string.Empty);
                RefreshHighScoreUI();
            }

            SetState(GameState.PreStartPreview);
        }

        private IEnumerator RollAndMove(int diceCount)
        {
            SetState(GameState.Rolling);
            _audioController?.PlayDiceRoll();

            // ── Roll dice ──────────────────────────────────────────────────────────
            int total;
            IReadOnlyList<int> individuals;

            if (_diceRoller != null)
            {
                // Physics roll – waits for dice to spawn, tumble, and settle.
                yield return StartCoroutine(_diceRoller.RollDiceRoutine(diceCount));
                total       = _diceRoller.LastRollTotal;
                individuals = _diceRoller.LastRollIndividuals;
            }
            else
            {
                // Instant random fallback – used when no DiceRoller is assigned.
                var results = new List<int>();
                for (int i = 0; i < diceCount; i++)
                    results.Add(Random.Range(1, _dieFaces + 1));
                individuals = results.AsReadOnly();
                total = 0;
                foreach (var v in individuals) total += v;
            }

            Debug.Log($"[BoardGameManager] Dice roll – {diceCount} die/dice, total: {total}, moving {total} step(s).");
            if (_ui != null) _ui.ShowDiceResult(total, individuals);

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
                      $"at-risk: {_scoreManager.RunScore}, risk: {_scoreManager.RiskLevel}, " +
                      $"multiplier: {_scoreManager.CurrentMultiplier:F2}x.");
            SetState(GameState.CheckpointPreview);
            _ui?.HideDiceControls();

            // Reshuffle at current risk immediately so the zoomed-out board shows the
            // actual layout the player will be rolling into, informing their Take/Skip decision.
            _boardManager.ShuffleContents(_scoreManager.RiskLevel);
            _audioController?.PlayBoardShuffle();

            if (_camera != null) _camera.SetCheckpointMode();
            if (_camera != null) yield return StartCoroutine(_camera.WaitForZoomComplete());

            yield return new WaitForSeconds(_checkpointRevealDelay);
            _audioController?.PlayCheckpointReached();

            SetState(GameState.AtCheckpoint);

            if (_ui != null)
            {
                int   nextRisk  = Mathf.Min(_scoreManager.RiskLevel + 1, _scoreManager.MaxRiskLevel);
                float nextMult  = _scoreManager.GetMultiplier(nextRisk);
                bool  atMaxRisk = _scoreManager.RiskLevel >= _scoreManager.MaxRiskLevel;
                _ui.ShowCheckpointPanel(
                    _scoreManager.RunScore,
                    _scoreManager.BankedScore,
                    _scoreManager.CurrentMultiplier,
                    nextMult,
                    atMaxRisk);
                _audioController?.PlayPanelOpen();
            }
            // State stays AtCheckpoint – waiting for TakeCheckpoint() or SkipCheckpoint().
        }

        private IEnumerator ResolveCheckpointDecisionRoutine(bool tookCheckpoint)
        {
            _ui?.HideCheckpointPanel();
            _audioController?.PlayPanelClose();

            // Board is NOT reshuffled here. The shuffle happened before the decision so the
            // player could make an informed choice. They now play on that same board.
            if (tookCheckpoint)
            {
                Debug.Log($"[BoardGameManager] Checkpoint taken. Risk reset to 0. Banked: {_scoreManager.BankedScore}.");
                _audioController?.PlayCheckpointBank();
                _ui?.AnimateBankedScoreTransferred();
            }
            else
            {
                Debug.Log($"[BoardGameManager] Checkpoint skipped. Risk: {_scoreManager.RiskLevel}, " +
                          $"multiplier: {_scoreManager.CurrentMultiplier:F2}x.");
                _audioController?.PlayCheckpointSkip();
                _audioController?.PlayRiskIncrease();
                _audioController?.PlayMultiplierPop();
                _ui?.AnimateRiskChanged();
                _ui?.AnimateMultiplierChanged();
            }

            if (_camera != null) _camera.SetFollowMode();
            if (_camera != null) yield return StartCoroutine(_camera.WaitForZoomComplete());

            _ui?.ShowDiceControls();
            RefreshHUD();
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

                        _audioController?.PlayPositiveCollect();
                        if (_ui != null) _ui.ShowScoreGain(awarded, _player.transform.position);
                        RefreshHUD();
                        yield return new WaitForSeconds(0.6f);
                        break;

                    case TileContent.Negative:
                        Debug.Log("[BoardGameManager] Negative tile hit!");
                        _audioController?.PlayNegativeHit();
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
            _audioController?.PlayGameOver();

            if (_ui != null)
            {
                _ui.ShowGameOver(_scoreManager.BankedScore, _scoreManager.RunScore);
                RefreshHighScoreUI();
            }

            yield return null;
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
