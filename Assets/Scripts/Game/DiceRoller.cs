using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Spawns die prefabs into a dedicated 3D dice tray, applies physics forces,
    /// waits for them to settle, reads the top-face values, then cleans up.
    ///
    /// SCREEN-SPACE SETUP (recommended):
    ///   Dice physics live in a separate Dice3D layer/world so they are never mixed
    ///   with the 2D board. A dedicated DiceCamera renders that layer to a RenderTexture
    ///   displayed by a UI RawImage (see full setup instructions in the output doc).
    ///
    ///   _diceContainer  – child of Dice3DWorld, positioned above the tray floor.
    ///   Spawn positions  – X/Z spread above the tray (gravity pulls -Y onto the floor).
    ///   _diceRollView   – optional UI panel controller; shown on roll, hidden on cleanup.
    ///
    /// FALLBACK:
    ///   If _diePrefab is not assigned the roller produces a random result instantly
    ///   so the game stays playable while art is pending.
    /// </summary>
    public class DiceRoller : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Spawn origin. Place this as a child of Dice3DWorld, above the tray floor.")]
        [SerializeField] private Transform _diceContainer;

        [Tooltip("Die prefab. Must have: Rigidbody, Collider, DieResultReader. Assign to Dice3D layer.")]
        [SerializeField] private GameObject _diePrefab;

        [Tooltip("Optional UI panel that shows the render texture during a roll. Null = no panel.")]
        [SerializeField] private UI.DiceRollView _diceRollView;

        [Header("Spawn")]
        [Tooltip("Height above _diceContainer to spawn dice (world Y).")]
        [SerializeField] private float _spawnHeight = 0.6f;

        [Tooltip("Random X/Z spread radius around _diceContainer when spawning multiple dice.")]
        [SerializeField] private float _spawnSpread = 0.12f;

        [Header("Physics")]
        [Tooltip("Random lateral (X/Z) velocity magnitude applied on spawn.")]
        [SerializeField] private float _launchForce = 1.5f;

        [Tooltip("Upward (Y) velocity applied on spawn so dice bounce off the floor.")]
        [SerializeField] private float _upwardForce = 2f;

        [Tooltip("Rotational impulse when useRandomTorqueRange is off.")]
        [SerializeField] private float _torqueImpulse = 16f;

        [Tooltip("Minimum rotational impulse when useRandomTorqueRange is on.")]
        [SerializeField] private float _minTorqueImpulse = 10f;

        [Tooltip("Maximum rotational impulse when useRandomTorqueRange is on.")]
        [SerializeField] private float _maxTorqueImpulse = 24f;

        [Tooltip("When true the torque magnitude is randomised between min and max each roll.")]
        [SerializeField] private bool _useRandomTorqueRange = true;

        [Tooltip("Extra angular velocity added directly to rb.angularVelocity after the impulse. " +
                 "Increase this if dice still feel sluggish after raising torque.")]
        [SerializeField] private float _initialAngularVelocity = 6f;

        [Header("Settle Detection")]
        [Tooltip("Linear velocity below which a die is considered still.")]
        [SerializeField] private float _settleVelocityThreshold = 0.05f;

        [Tooltip("Angular velocity below which a die is considered still.")]
        [SerializeField] private float _settleAngularVelocityThreshold = 0.05f;

        [Tooltip("Seconds all dice must remain below thresholds before reading values.")]
        [SerializeField] private float _requiredStillTime = 0.4f;

        [Tooltip("Minimum seconds the settle loop must run before a still die can be accepted. " +
                 "Prevents a slow-spinning die from being read on the very first frame.")]
        [SerializeField] private float _minimumRollDuration = 1.0f;

        [Tooltip("Hard timeout: read values even if dice have not settled after this many seconds.")]
        [SerializeField] private float _maxSettleTime = 8f;

        [Header("Cleanup")]
        [Tooltip("Seconds after reading values before destroying dice and hiding the panel.")]
        [SerializeField] private float _cleanupDelay = 1.5f;

        [Header("Panel")]
        [Tooltip("When true, DiceRollView.Show() is called at roll start and Hide() after cleanup.")]
        [SerializeField] private bool _showDicePanelDuringRoll = true;

        [Header("Limits")]
        [SerializeField] private int _maxDiceCount = 2;

        // ── State ─────────────────────────────────────────────────────────────────

        public bool               IsRolling           { get; private set; }
        public int                LastRollTotal       { get; private set; }
        public IReadOnlyList<int> LastRollIndividuals { get; private set; } = new List<int>();

        private readonly List<GameObject> _activeDice = new();

        // ── Public coroutine API ──────────────────────────────────────────────────

        /// <summary>
        /// Full dice roll sequence: spawn → physics → settle → read → schedule cleanup.
        /// Results are available on <see cref="LastRollTotal"/> and
        /// <see cref="LastRollIndividuals"/> immediately after this coroutine completes.
        ///
        /// Usage in GameManager:
        ///   yield return StartCoroutine(_diceRoller.RollDiceRoutine(count));
        ///   int total = _diceRoller.LastRollTotal;
        /// </summary>
        public IEnumerator RollDiceRoutine(int count)
        {
            if (IsRolling)
            {
                Debug.LogWarning("[DiceRoller] Roll requested while already rolling – ignoring.");
                yield break;
            }

            count     = Mathf.Clamp(count, 1, _maxDiceCount);
            IsRolling = true;

            if (_showDicePanelDuringRoll) _diceRollView?.Show();

            // Destroy any leftover dice from a previous roll.
            CleanupImmediate();

            // ── Fallbacks ─────────────────────────────────────────────────────────
            if (_diePrefab == null)
            {
                Debug.LogWarning("[DiceRoller] _diePrefab not assigned – using instant random fallback.");
                InstantRandomFallback(count);
                yield break;
            }

            if (_diceContainer == null)
            {
                Debug.LogWarning("[DiceRoller] _diceContainer not assigned – using instant random fallback.");
                InstantRandomFallback(count);
                yield break;
            }

            // ── Spawn ─────────────────────────────────────────────────────────────
            var rigidbodies = new List<Rigidbody>();

            for (int i = 0; i < count; i++)
            {
                // Spread on the X/Z plane so dice land on the horizontal tray floor.
                Vector3 spawnPos = _diceContainer.position + new Vector3(
                    Random.Range(-_spawnSpread, _spawnSpread),
                    _spawnHeight,
                    Random.Range(-_spawnSpread, _spawnSpread));

                // Spawn without a parent to avoid scale-inherited physics artifacts.
                GameObject die = Instantiate(_diePrefab, spawnPos, Random.rotation);
                _activeDice.Add(die);

                Rigidbody rb = die.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rigidbodies.Add(rb);
                    // X/Z lateral velocity gives the die a random skid across the tray.
                    // Y upward component ensures it bounces off the floor at least once.
                    rb.linearVelocity = new Vector3(
                        Random.Range(-1f, 1f) * _launchForce,
                        _upwardForce,
                        Random.Range(-1f, 1f) * _launchForce);
                    float   tMag   = _useRandomTorqueRange
                                         ? Random.Range(_minTorqueImpulse, _maxTorqueImpulse)
                                         : _torqueImpulse;
                    rb.AddTorque(Random.onUnitSphere * tMag, ForceMode.Impulse);
                    rb.angularVelocity += Random.onUnitSphere * _initialAngularVelocity;
                }
                else
                {
                    Debug.LogWarning($"[DiceRoller] Prefab '{_diePrefab.name}' is missing a Rigidbody.");
                }
            }

            // ── Wait for settle ───────────────────────────────────────────────────
            yield return StartCoroutine(WaitForSettle(rigidbodies));

            // ── Read values ───────────────────────────────────────────────────────
            var results = new List<int>();
            foreach (var die in _activeDice)
            {
                if (die == null) { results.Add(1); continue; }
                var reader = die.GetComponentInChildren<DieResultReader>();
                if (reader != null)
                {
                    results.Add(reader.GetTopFaceValue());
                }
                else
                {
                    Debug.LogWarning($"[DiceRoller] '{die.name}' missing DieResultReader – defaulting to 1.");
                    results.Add(1);
                }
            }

            SetResults(results);
            IsRolling = false;

            // Cleanup runs independently so GameManager proceeds immediately.
            List<GameObject> toClean = new(_activeDice);
            _activeDice.Clear();
            StartCoroutine(CleanupAfterDelay(toClean));
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private IEnumerator WaitForSettle(List<Rigidbody> rigidbodies)
        {
            float stillTimer   = 0f;
            float timeoutTimer = 0f;

            while (stillTimer < _requiredStillTime && timeoutTimer < _maxSettleTime)
            {
                // Never accept a "still" reading before the minimum roll time has elapsed.
                // This prevents a die that spawns on a flat face from being read immediately.
                bool pastMinimum = timeoutTimer >= _minimumRollDuration;

                bool allStill = true;
                foreach (var rb in rigidbodies)
                {
                    if (rb == null) continue;
                    if (rb.linearVelocity.magnitude  > _settleVelocityThreshold ||
                        rb.angularVelocity.magnitude > _settleAngularVelocityThreshold)
                    {
                        allStill = false;
                        break;
                    }
                }

                if (allStill && pastMinimum) stillTimer += Time.deltaTime;
                else                         stillTimer  = 0f;
                timeoutTimer += Time.deltaTime;
                yield return null;
            }

            if (timeoutTimer >= _maxSettleTime)
                Debug.LogWarning("[DiceRoller] Max settle time reached – reading values early.");
        }

        private IEnumerator CleanupAfterDelay(List<GameObject> dice)
        {
            yield return new WaitForSeconds(_cleanupDelay);
            foreach (var die in dice)
                if (die != null) Destroy(die);
            if (_showDicePanelDuringRoll) _diceRollView?.Hide();
        }

        private void CleanupImmediate()
        {
            foreach (var die in _activeDice)
                if (die != null) Destroy(die);
            _activeDice.Clear();
        }

        private void InstantRandomFallback(int count)
        {
            var results = new List<int>();
            for (int i = 0; i < count; i++) results.Add(Random.Range(1, 7));
            SetResults(results);
            IsRolling = false;
            // Hide panel immediately on fallback since there's nothing to show.
            if (_showDicePanelDuringRoll) _diceRollView?.Hide();
        }

        private void SetResults(List<int> results)
        {
            int total = 0;
            foreach (var v in results) total += v;
            LastRollTotal       = total;
            LastRollIndividuals = results.AsReadOnly();
            Debug.Log($"[DiceRoller] Results: [{string.Join(", ", results)}] → total {total}.");
        }
    }
}
