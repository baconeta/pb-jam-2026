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
    ///   3. For each sound key field, enter the name that matches a SoundData entry in
    ///      AudioWrapper's inspector list.
    ///   4. Leave any field empty to skip that sound silently.
    ///
    /// HOW TO ADD SOUNDS:
    ///   Select the AudioWrapper GameObject → Inspector → Sound List → add a SoundData entry
    ///   with a Name that matches the string you type into the fields below.
    /// </summary>
    public class BoardGameAudioController : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private string _startGameSound       = "";
        [SerializeField] private string _diceRollSound        = "";
        [SerializeField] private string _playerHopSound       = "";
        [SerializeField] private string _positiveCollectSound = "";
        [SerializeField] private string _negativeHitSound     = "";
        [SerializeField] private string _gameOverSound        = "";

        [Header("Checkpoint")]
        [SerializeField] private string _checkpointReachedSound = "";
        [SerializeField] private string _checkpointBankSound    = "";
        [SerializeField] private string _checkpointSkipSound    = "";

        [Header("Score & Risk")]
        [SerializeField] private string _riskIncreaseSound   = "";
        [SerializeField] private string _multiplierPopSound  = "";
        [SerializeField] private string _boardShuffleSound   = "";
        [SerializeField] private string _highScoreSavedSound = "";

        [Header("UI")]
        [SerializeField] private string _buttonClickSound = "";
        [SerializeField] private string _panelOpenSound   = "";
        [SerializeField] private string _panelCloseSound  = "";

        // Fired once so the console isn't spammed if AudioWrapper is missing.
        private bool _warnedMissingWrapper;

        public void PlayStartGame()         => Play(_startGameSound);
        public void PlayDiceRoll()          => Play(_diceRollSound);
        public void PlayPlayerHop()         => Play(_playerHopSound);
        public void PlayPositiveCollect()   => Play(_positiveCollectSound);
        public void PlayNegativeHit()       => Play(_negativeHitSound);
        public void PlayCheckpointReached() => Play(_checkpointReachedSound);
        public void PlayCheckpointBank()    => Play(_checkpointBankSound);
        public void PlayCheckpointSkip()    => Play(_checkpointSkipSound);
        public void PlayRiskIncrease()      => Play(_riskIncreaseSound);
        public void PlayMultiplierPop()     => Play(_multiplierPopSound);
        public void PlayBoardShuffle()      => Play(_boardShuffleSound);
        public void PlayButtonClick()       => Play(_buttonClickSound);
        public void PlayGameOver()          => Play(_gameOverSound);
        public void PlayHighScoreSaved()    => Play(_highScoreSavedSound);
        public void PlayPanelOpen()         => Play(_panelOpenSound);
        public void PlayPanelClose()        => Play(_panelCloseSound);

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

            AudioWrapper.Instance.PlaySound(soundName);
        }
    }
}
