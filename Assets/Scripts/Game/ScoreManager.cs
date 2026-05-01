using UnityEngine;

namespace Game
{
    /// <summary>
    /// Tracks banked score, run (at-risk) score, and risk level for a single game session.
    ///
    /// SCORE MODEL:
    ///   bankedScore  – safe score, locked in when the player Takes the checkpoint
    ///   runScore     – at-risk score accumulated since the last checkpoint
    ///   totalScore   = bankedScore + runScore
    ///
    /// RISK / MULTIPLIER:
    ///   - riskLevel starts at 0 and increases each time the checkpoint is skipped.
    ///   - riskLevel is clamped to _maxRiskLevel.
    ///   - The score multiplier is read from _riskMultiplierCurve (AnimationCurve).
    ///   - Curve X = risk level, Y = multiplier. Result is always >= 1.0.
    ///   - See GetMultiplier() for tuning instructions.
    ///
    /// CHECKPOINT DECISIONS:
    ///   Take → BankScore() (run → banked), risk resets to 0
    ///   Skip → risk += 1 (clamped), run score stays at risk
    ///
    /// NEGATIVE TILE:
    ///   Behaviour (currently game over) is handled in BoardGameManager, not here.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score Settings")]
        [Tooltip("Base score added to run score when landing on a Positive tile (before multiplier).")]
        [SerializeField] private int _basePositiveScore = 100;

        [Header("Risk Settings")]
        [Tooltip("Maximum risk level the player can reach. SkipCheckpoint() clamps to this value.")]
        [SerializeField] private int _maxRiskLevel = 10;

        // ── Multiplier curve ──────────────────────────────────────────────────────
        //
        // HOW TO TUNE IN THE INSPECTOR:
        //   Open ScoreManager → Risk Settings → Risk Multiplier Curve.
        //   X axis = risk level (integer, 0 = safe).
        //   Y axis = score multiplier applied to _basePositiveScore.
        //   Add or drag keyframes to define the progression.
        //   The result is always clamped to >= 1.0, so the curve can never reduce score.
        //
        // SUGGESTED STARTING VALUES (match the default keyframes below):
        //   Risk 0 → 1×   (baseline, no reward for risk)
        //   Risk 1 → 2×
        //   Risk 2 → 4×
        //   Risk 3 → 7×
        //   Risk 4 → 10×
        //
        // Interpolation between keyframes is handled by Unity's AnimationCurve.
        // To extend beyond risk 4, add more keyframes on the right side of the curve.
        //
        [Tooltip("X = risk level, Y = score multiplier. See code comments for tuning guide.")]
        [SerializeField] private AnimationCurve _riskMultiplierCurve = new AnimationCurve(
            new Keyframe(0,  1f),
            new Keyframe(1,  2f),
            new Keyframe(2,  4f),
            new Keyframe(3,  7f),
            new Keyframe(4, 10f)
        );

        // ── Runtime state (not serialized – recomputed each session) ──────────────

        private int _bankedScore;
        private int _runScore;
        private int _riskLevel;

        // ── Properties ────────────────────────────────────────────────────────────

        public int BankedScore  => _bankedScore;
        public int RunScore     => _runScore;
        public int RiskLevel    => _riskLevel;
        public int MaxRiskLevel => _maxRiskLevel;

        /// <summary>bankedScore + runScore. Use this for display and high score submission.</summary>
        public int GetTotalScore() => _bankedScore + _runScore;

        /// <summary>Backward-compat alias for GetTotalScore().</summary>
        public int Score => GetTotalScore();

        /// <summary>Multiplier at the current risk level.</summary>
        public float CurrentMultiplier => GetMultiplier(_riskLevel);

        // ── Multiplier ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the score multiplier for the given risk level by evaluating the
        /// AnimationCurve. Result is always >= 1.0.
        ///
        /// Called by AwardPositiveScore() automatically. You can also call it directly
        /// to display the multiplier in the UI before the player lands on a tile.
        /// </summary>
        public float GetMultiplier(int riskLevel)
        {
            if (_riskMultiplierCurve == null || _riskMultiplierCurve.length == 0)
            {
                Debug.LogWarning("[ScoreManager] Risk multiplier curve is not configured – defaulting to 1.0. " +
                                 "Set keyframes in the Inspector under Risk Settings → Risk Multiplier Curve.");
                return 1f;
            }
            return Mathf.Max(1f, _riskMultiplierCurve.Evaluate(riskLevel));
        }

        // ── Session control ───────────────────────────────────────────────────────

        public void ResetSession()
        {
            _bankedScore = 0;
            _runScore    = 0;
            _riskLevel   = 0;
            Debug.Log("[ScoreManager] Session reset – banked 0, run 0, risk 0.");
        }

        // ── Run score ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes (baseScore × multiplier) and adds it to the run score.
        /// Called when the player lands on a Positive tile.
        /// Returns the amount added so the UI can display the "+X" delta.
        /// </summary>
        public int AwardPositiveScore()
        {
            float multiplier = GetMultiplier(_riskLevel);
            int   awarded    = Mathf.RoundToInt(_basePositiveScore * multiplier);
            _runScore += awarded;
            Debug.Log($"[ScoreManager] Positive tile – +{awarded} to run score " +
                      $"(base {_basePositiveScore} × {multiplier:F2}x). " +
                      $"Run: {_runScore}  Banked: {_bankedScore}  Total: {GetTotalScore()}.");
            return awarded;
        }

        /// <summary>Adds amount directly to run score. Low-level; bypasses multiplier.</summary>
        public void AddRunScore(int amount)
        {
            _runScore += amount;
        }

        /// <summary>
        /// Adds a flat bonus to the run score, bypassing the multiplier.
        /// Used by BoardGameManager for the optional two-dice bonus.
        /// </summary>
        public void AddFlatBonus(int amount)
        {
            _runScore += amount;
            Debug.Log($"[ScoreManager] Flat bonus +{amount} to run score. Run: {_runScore}  Total: {GetTotalScore()}.");
        }

        // ── Checkpoint decisions ──────────────────────────────────────────────────

        /// <summary>
        /// Moves all run score into banked score and resets risk to 0.
        /// Called internally by TakeCheckpoint(). Can also be called directly.
        /// </summary>
        public void BankScore()
        {
            Debug.Log($"[ScoreManager] Banking {_runScore} pts → banked {_bankedScore} + {_runScore} = {_bankedScore + _runScore}.");
            _bankedScore += _runScore;
            _runScore     = 0;
        }

        /// <summary>
        /// Discards the run score without banking it.
        /// Not called by default flow – available for penalty mechanics.
        /// </summary>
        public void ResetRunScore()
        {
            Debug.Log($"[ScoreManager] Run score cleared ({_runScore} pts discarded).");
            _runScore = 0;
        }

        /// <summary>
        /// Player chose Take Checkpoint:
        ///   • run score is banked (safe)
        ///   • risk resets to 0
        ///   • multiplier drops back to 1.0
        /// </summary>
        public void TakeCheckpoint()
        {
            int prevRisk = _riskLevel;
            BankScore();
            _riskLevel = 0;
            Debug.Log($"[ScoreManager] Checkpoint taken – risk {prevRisk} → 0. " +
                      $"Multiplier now {CurrentMultiplier:F2}x. Banked: {_bankedScore}.");
        }

        /// <summary>
        /// Player chose Skip Checkpoint:
        ///   • risk increases by 1 (clamped to _maxRiskLevel)
        ///   • run score stays at risk – not banked
        ///   • multiplier increases according to the curve
        /// </summary>
        public void SkipCheckpoint()
        {
            int prev   = _riskLevel;
            _riskLevel = Mathf.Min(_riskLevel + 1, _maxRiskLevel);

            if (_riskLevel == _maxRiskLevel && prev == _maxRiskLevel)
                Debug.LogWarning($"[ScoreManager] Risk level already at maximum ({_maxRiskLevel}). Cannot increase further.");

            Debug.Log($"[ScoreManager] Checkpoint skipped – risk {prev} → {_riskLevel} (max {_maxRiskLevel}). " +
                      $"Multiplier now {CurrentMultiplier:F2}x. Run score at risk: {_runScore}.");
        }
    }
}
