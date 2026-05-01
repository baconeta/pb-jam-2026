using UnityEngine;

namespace Board
{
    /// <summary>
    /// What kind of content is currently displayed on this tile.
    /// Assigned at runtime by BoardManager.ShuffleContents(); not serialized.
    /// </summary>
    public enum TileContent
    {
        Empty,
        Positive,
        Negative,
        Checkpoint
    }

    /// <summary>
    /// A single tile on the board loop.
    /// Attach one of these to each sticky-note tile GameObject you place in the scene.
    ///
    /// Index:        Determines order in the loop. Must be unique across all tiles.
    /// IsCheckpoint: Mark exactly one tile as the start/checkpoint. It will never
    ///               receive random Positive or Negative content.
    /// Icon refs:    Assign child GameObjects for each icon type. The tile will show/hide
    ///               them as content changes. Leave unassigned until you have art.
    /// </summary>
    public class BoardTile : MonoBehaviour
    {
        [Header("Tile Identity")]
        [Tooltip("Order of this tile in the board loop. Must be unique. Start tile is conventionally 0.")]
        [SerializeField] private int _index;

        [Tooltip("Mark true on exactly one tile – the start/checkpoint tile.")]
        [SerializeField] private bool _isCheckpoint;

        [Header("Icon References (assign child GameObjects; leave empty until you have art)")]
        [Tooltip("Shown when this tile has Positive content.")]
        [SerializeField] private GameObject _positiveIcon;

        [Tooltip("Shown when this tile has Negative content.")]
        [SerializeField] private GameObject _negativeIcon;

        [Tooltip("Always shown on the checkpoint tile (independent of content icons).")]
        [SerializeField] private GameObject _checkpointIcon;

        // Set at runtime by BoardManager. Not serialized – recomputed each session.
        private TileContent _currentContent;

        public int Index => _index;
        public bool IsCheckpoint => _isCheckpoint;
        public TileContent CurrentContent => _currentContent;

        /// <summary>
        /// Sets tile content and refreshes icon visibility.
        /// Checkpoint tiles ignore the argument and always display as Checkpoint.
        /// </summary>
        public void SetContent(TileContent content)
        {
            _currentContent = _isCheckpoint ? TileContent.Checkpoint : content;
            RefreshIcons();
        }

        private void Start()
        {
            // Ensure icons match initial state when the scene first runs.
            RefreshIcons();
        }

        private void RefreshIcons()
        {
            if (_positiveIcon  != null) _positiveIcon.SetActive(_currentContent  == TileContent.Positive);
            if (_negativeIcon  != null) _negativeIcon.SetActive(_currentContent  == TileContent.Negative);
            // Checkpoint icon follows the IsCheckpoint flag, not the content enum.
            if (_checkpointIcon != null) _checkpointIcon.SetActive(_isCheckpoint);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep icons correct while editing in the Inspector.
            RefreshIcons();
        }
#endif
    }
}
