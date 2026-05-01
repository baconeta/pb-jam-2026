using System.Collections;
using UnityEngine;

namespace Board
{
    /// <summary>
    /// Moves the player token along the board one tile at a time with a small hop arc.
    /// Attach to the player token GameObject.
    ///
    /// Movement uses coroutines and no Rigidbody physics.
    /// The GameManager starts movement by calling StartMovement() and then
    /// waits for IsMoving to return false before continuing the game flow.
    /// </summary>
    public class BoardPlayer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The BoardManager in the scene.")]
        [SerializeField] private BoardManager _boardManager;

        [Header("Movement")]
        [Tooltip("Seconds to travel between adjacent tiles.")]
        [SerializeField] private float _tileMoveDuration = 0.18f;

        [Tooltip("Height of the hop arc. Set to 0 for a flat slide.")]
        [SerializeField] private float _hopHeight = 0.25f;

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>True while a movement coroutine is running. Disable input while this is true.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// True after movement ends if the run was cut short by landing on the checkpoint.
        /// Check this immediately after IsMoving becomes false.
        /// </summary>
        public bool StoppedAtCheckpoint { get; private set; }

        // Internal position on the sorted tile list (not world-space).
        private int _currentTileIndex;

        /// <summary>The tile the player is currently standing on.</summary>
        public BoardTile CurrentTile => _boardManager != null
            ? _boardManager.GetTileAtIndex(_currentTileIndex)
            : null;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Instantly places the token on the checkpoint/start tile.
        /// Call at game start and restart.
        /// </summary>
        public void PlaceAtCheckpoint()
        {
            if (_boardManager == null)
            {
                Debug.LogError("[BoardPlayer] BoardManager is not assigned.");
                return;
            }

            int idx = _boardManager.CheckpointIndex;
            if (idx < 0)
            {
                Debug.LogError("[BoardPlayer] No checkpoint tile found.");
                return;
            }

            _currentTileIndex = idx;
            transform.position = _boardManager.GetTileAtIndex(_currentTileIndex).transform.position;
            StoppedAtCheckpoint = false;
        }

        /// <summary>
        /// Starts the movement coroutine for <paramref name="steps"/> tiles.
        /// Movement stops early if the checkpoint tile is reached mid-move.
        ///
        /// Usage in GameManager:
        ///   _player.StartMovement(roll);
        ///   yield return new WaitUntil(() => !_player.IsMoving);
        ///   if (_player.StoppedAtCheckpoint) { ... }
        /// </summary>
        public void StartMovement(int steps)
        {
            if (IsMoving)
            {
                Debug.LogWarning("[BoardPlayer] StartMovement called while already moving.");
                return;
            }
            StartCoroutine(MoveSteps(steps));
        }

        // ── Private movement coroutines ───────────────────────────────────────────

        private IEnumerator MoveSteps(int steps)
        {
            if (_boardManager == null) yield break;

            IsMoving             = true;
            StoppedAtCheckpoint  = false;
            int checkpointIndex  = _boardManager.CheckpointIndex;

            for (int i = 0; i < steps; i++)
            {
                int nextIndex = (_currentTileIndex + 1) % _boardManager.TileCount;
                yield return StartCoroutine(HopToTile(nextIndex));
                _currentTileIndex = nextIndex;

                // Stop immediately on reaching the checkpoint, discarding remaining steps.
                if (nextIndex == checkpointIndex)
                {
                    StoppedAtCheckpoint = true;
                    break;
                }
            }

            IsMoving = false;
        }

        /// <summary>
        /// Smoothly hops from the current world position to the centre of the target tile.
        /// Uses SmoothStep easing with a sine-curve vertical arc.
        /// </summary>
        private IEnumerator HopToTile(int targetIndex)
        {
            BoardTile target = _boardManager.GetTileAtIndex(targetIndex);
            if (target == null) yield break;

            Vector3 startPos = transform.position;
            Vector3 endPos   = target.transform.position;
            float   elapsed  = 0f;

            while (elapsed < _tileMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / _tileMoveDuration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);
                // Arc peaks at t = 0.5 using a half-sine.
                float arc    = Mathf.Sin(t * Mathf.PI) * _hopHeight;

                transform.position = Vector3.Lerp(startPos, endPos, smooth) + Vector3.up * arc;
                yield return null;
            }

            // Snap to exact final position to avoid float drift.
            transform.position = endPos;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_boardManager == null)
                Debug.LogWarning("[BoardPlayer] BoardManager reference is not assigned.");
        }
#endif
    }
}
