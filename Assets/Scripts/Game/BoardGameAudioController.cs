using System.Collections;
using Audio;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Central hub for all gameplay audio events.
    ///
    /// SETUP:
    ///   1. Add this component to the GameManager GameObject.
    ///   2. Ensure an AudioWrapper GameObject exists in the scene (it is a singleton).
    ///   3. For each event you can set:
    ///        • Single key  – legacy string field, used when the array is empty.
    ///        • Keys array  – one or more keys chosen randomly; takes priority over single key.
    ///        • Delay ms    – optional millisecond delay before the sound plays (0 = immediate).
    ///   4. Leave both key fields empty to skip that sound silently.
    ///
    /// ADDING SOUNDS:
    ///   AudioWrapper GameObject → Inspector → Sound List → add a SoundData entry whose
    ///   Name matches the key string you type below.
    /// </summary>
    public class BoardGameAudioController : MonoBehaviour
    {
        // ── Gameplay ──────────────────────────────────────────────────────────────

        [Header("Gameplay")]

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _startGameSound       = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _startGameSounds    = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _startGameDelayMs      = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _diceRollSound        = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _diceRollSounds     = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _diceRollDelayMs       = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _playerHopSound       = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _playerHopSounds    = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _playerHopDelayMs      = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _positiveCollectSound = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _positiveCollectSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _positiveCollectDelayMs = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _negativeHitSound     = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _negativeHitSounds  = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _negativeHitDelayMs    = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _gameOverSound        = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _gameOverSounds     = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _gameOverDelayMs       = 0f;

        // ── Checkpoint ────────────────────────────────────────────────────────────

        [Header("Checkpoint")]

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _checkpointReachedSound = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _checkpointReachedSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _checkpointReachedDelayMs = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _checkpointBankSound    = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _checkpointBankSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _checkpointBankDelayMs   = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _checkpointSkipSound    = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _checkpointSkipSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _checkpointSkipDelayMs   = 0f;

        // ── Score & Risk ──────────────────────────────────────────────────────────

        [Header("Score & Risk")]

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _riskIncreaseSound    = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _riskIncreaseSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _riskIncreaseDelayMs   = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _multiplierPopSound   = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _multiplierPopSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _multiplierPopDelayMs  = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _boardShuffleSound    = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _boardShuffleSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _boardShuffleDelayMs   = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _highScoreSavedSound    = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _highScoreSavedSounds = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _highScoreSavedDelayMs  = 0f;

        // ── UI ────────────────────────────────────────────────────────────────────

        [Header("UI")]

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _buttonClickSound     = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _buttonClickSounds  = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _buttonClickDelayMs    = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _panelOpenSound       = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _panelOpenSounds    = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _panelOpenDelayMs      = 0f;

        [Tooltip("Single sound key (legacy). Ignored when the array below has entries.")]
        [SerializeField] private string _panelCloseSound      = "";
        [Tooltip("Add one or more AudioWrapper sound keys. One is chosen randomly each time.")]
        [SerializeField] private string[] _panelCloseSounds   = {};
        [Tooltip("Delay in milliseconds before this sound plays. 0 = immediate.")]
        [SerializeField] private float _panelCloseDelayMs     = 0f;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _warnedMissingWrapper;

        // ── Public API ────────────────────────────────────────────────────────────

        public void PlayStartGame()         => PlayEvent(_startGameSounds,         _startGameSound,         _startGameDelayMs);
        public void PlayDiceRoll()          => PlayEvent(_diceRollSounds,          _diceRollSound,          _diceRollDelayMs);
        public void PlayPlayerHop()         => PlayEvent(_playerHopSounds,         _playerHopSound,         _playerHopDelayMs);
        public void PlayPositiveCollect()   => PlayEvent(_positiveCollectSounds,   _positiveCollectSound,   _positiveCollectDelayMs);
        public void PlayNegativeHit()       => PlayEvent(_negativeHitSounds,       _negativeHitSound,       _negativeHitDelayMs);
        public void PlayCheckpointReached() => PlayEvent(_checkpointReachedSounds, _checkpointReachedSound, _checkpointReachedDelayMs);
        public void PlayCheckpointBank()    => PlayEvent(_checkpointBankSounds,    _checkpointBankSound,    _checkpointBankDelayMs);
        public void PlayCheckpointSkip()    => PlayEvent(_checkpointSkipSounds,    _checkpointSkipSound,    _checkpointSkipDelayMs);
        public void PlayRiskIncrease()      => PlayEvent(_riskIncreaseSounds,      _riskIncreaseSound,      _riskIncreaseDelayMs);
        public void PlayMultiplierPop()     => PlayEvent(_multiplierPopSounds,     _multiplierPopSound,     _multiplierPopDelayMs);
        public void PlayBoardShuffle()      => PlayEvent(_boardShuffleSounds,      _boardShuffleSound,      _boardShuffleDelayMs);
        public void PlayButtonClick()       => PlayEvent(_buttonClickSounds,       _buttonClickSound,       _buttonClickDelayMs);
        public void PlayGameOver()          => PlayEvent(_gameOverSounds,          _gameOverSound,          _gameOverDelayMs);
        public void PlayHighScoreSaved()    => PlayEvent(_highScoreSavedSounds,    _highScoreSavedSound,    _highScoreSavedDelayMs);
        public void PlayPanelOpen()         => PlayEvent(_panelOpenSounds,         _panelOpenSound,         _panelOpenDelayMs);
        public void PlayPanelClose()        => PlayEvent(_panelCloseSounds,        _panelCloseSound,        _panelCloseDelayMs);

        // ── Private ───────────────────────────────────────────────────────────────

        private void PlayEvent(string[] keys, string fallback, float delayMs)
        {
            if (delayMs > 0f)
                StartCoroutine(PlayEventDelayed(keys, fallback, delayMs / 1000f));
            else
                PlayEventNow(keys, fallback);
        }

        private IEnumerator PlayEventDelayed(string[] keys, string fallback, float delaySec)
        {
            yield return new WaitForSeconds(delaySec);
            PlayEventNow(keys, fallback);
        }

        // Tries the array first; falls back to the single key if the array has no valid entries.
        private void PlayEventNow(string[] keys, string fallback)
        {
            if (keys != null)
            {
                int validCount = CountValid(keys);
                if (validCount > 0) { PlayRandom(keys, validCount); return; }
            }
            Play(fallback);
        }

        // Picks uniformly at random among non-blank entries in keys.
        private void PlayRandom(string[] keys, int validCount)
        {
            int pick = Random.Range(0, validCount);
            int seen = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(keys[i]))
                {
                    if (seen == pick) { Play(keys[i]); return; }
                    seen++;
                }
            }
        }

        private static int CountValid(string[] keys)
        {
            int count = 0;
            for (int i = 0; i < keys.Length; i++)
                if (!string.IsNullOrWhiteSpace(keys[i])) count++;
            return count;
        }

        private void Play(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            if (AudioWrapper.Instance == null)
            {
                if (!_warnedMissingWrapper)
                {
                    Debug.LogWarning("[BoardGameAudioController] AudioWrapper.Instance is null. " +
                                     "Add an AudioWrapper GameObject to the scene.");
                    _warnedMissingWrapper = true;
                }
                return;
            }

            Debug.Log($"[BoardGameAudioController] Playing '{soundName}'");
            AudioWrapper.Instance.PlaySound(soundName);
        }
    }
}
