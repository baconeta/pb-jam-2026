using UnityEngine;

namespace Game
{
    /// <summary>
    /// Tracks score and risk level for a single game session.
    ///
    /// Risk / Reward logic:
    ///   - Risk level starts at 0. Taking the checkpoint resets it to 0.
    ///     Skipping the checkpoint adds 1.
    ///   - Score multiplier = 1.0 + (riskLevel × _riskBonusPerLevel)
    ///     Example: risk 2, bonus 0.5 per level → multiplier 2.0
    ///   - Higher risk also tells BoardManager to place more positive AND negative tiles,
    ///     so the board becomes both more rewarding and more dangerous.
    ///   - Negative tile behaviour (currently game over) lives in BoardGameManager.
    ///     Change it there without touching ScoreManager.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score Settings")]
        [Tooltip("Base score for landing on a Positive tile at risk 0 (multiplier 1.0).")]
        [SerializeField] private int _basePositiveScore = 100;

        [Tooltip("Multiplier increase added per risk level. Risk 0 = 1.0x, risk 1 = 1.0 + bonus, etc.")]
        [SerializeField] private float _riskBonusPerLevel = 0.5f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private int _score;
        private int _riskLevel;

        public int   Score     => _score;
        public int   RiskLevel => _riskLevel;

        /// <summary>Current score multiplier based on risk level.</summary>
        public float CurrentMultiplier => 1f + _riskLevel * _riskBonusPerLevel;

        // ── Session control ───────────────────────────────────────────────────────

        public void ResetSession()
        {
            _score     = 0;
            _riskLevel = 0;
        }

        // ── Score events ──────────────────────────────────────────────────────────

        /// <summary>
        /// Awards score for landing on a Positive tile.
        /// Returns the amount awarded so the UI can show a delta (e.g. "+150").
        /// </summary>
        public int AwardPositiveScore()
        {
            int awarded = Mathf.RoundToInt(_basePositiveScore * CurrentMultiplier);
            _score += awarded;
            return awarded;
        }

        // ── Checkpoint decisions ──────────────────────────────────────────────────

        /// <summary>
        /// Player chose Take Checkpoint: risk resets to 0, removing the score multiplier
        /// but also reducing future board danger.
        /// </summary>
        public void TakeCheckpoint()
        {
            _riskLevel = 0;
        }

        /// <summary>
        /// Player chose Skip Checkpoint: risk increases by 1, raising the score multiplier
        /// and the number of positive AND negative tiles placed on the next shuffle.
        /// </summary>
        public void SkipCheckpoint()
        {
            _riskLevel++;
        }
    }
}
