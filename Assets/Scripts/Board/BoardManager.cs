using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Board
{
    /// <summary>
    /// Manages the ordered list of board tiles, content reshuffling, and movement lookups.
    ///
    /// SETUP: Place all sticky-note tile GameObjects in the scene manually, add a BoardTile
    /// component to each one, assign unique sequential indexes, then drag them all into the
    /// _tiles list here in the Inspector. Order in the list doesn't matter – they are sorted
    /// by index at runtime.
    ///
    /// This script does NOT procedurally create or position tiles.
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        [Header("Tile References")]
        [Tooltip("Drag every BoardTile in the scene into this list. Order doesn't matter.")]
        [SerializeField] private List<BoardTile> _tiles = new();

        [Header("Content Shuffle – Risk Level 0")]
        [Tooltip("Positive tiles placed at risk 0.")]
        [SerializeField] private int _basePositiveCount = 3;

        [Tooltip("Negative tiles placed at risk 0.")]
        [SerializeField] private int _baseNegativeCount = 1;

        [Header("Content Shuffle – Per Risk Level")]
        [Tooltip("Extra positive tiles added for each risk level above 0.")]
        [SerializeField] private int _positivePerRiskLevel = 2;

        [Tooltip("Extra negative tiles added for each risk level above 0.")]
        [SerializeField] private int _negativePerRiskLevel = 1;

        // Sorted, validated list built at runtime from _tiles.
        private readonly List<BoardTile> _sortedTiles = new();

        public int TileCount => _sortedTiles.Count;
        public IReadOnlyList<BoardTile> Tiles => _sortedTiles;

        protected override void Awake()
        {
            base.Awake();
            BuildSortedList();
        }

        // ── Public query API ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the tile at the given index, wrapping around if index is out of range.
        /// </summary>
        public BoardTile GetTileAtIndex(int index)
        {
            if (_sortedTiles.Count == 0) return null;
            // Double-mod handles negative values safely.
            int wrapped = ((index % _sortedTiles.Count) + _sortedTiles.Count) % _sortedTiles.Count;
            return _sortedTiles[wrapped];
        }

        /// <summary>
        /// Sorted index of the checkpoint tile, or -1 if none.
        /// </summary>
        public int CheckpointIndex
        {
            get
            {
                for (int i = 0; i < _sortedTiles.Count; i++)
                    if (_sortedTiles[i].IsCheckpoint) return i;
                return -1;
            }
        }

        public BoardTile CheckpointTile
        {
            get
            {
                foreach (var tile in _sortedTiles)
                    if (tile.IsCheckpoint) return tile;
                return null;
            }
        }

        // ── Content shuffling ─────────────────────────────────────────────────────

        /// <summary>
        /// Clears all tile content to Empty, then randomly distributes Positive and
        /// Negative tiles based on the current risk level. The checkpoint tile is never
        /// given random content.
        ///
        /// Call this at game start and after every checkpoint decision.
        /// </summary>
        public void ShuffleContents(int riskLevel)
        {
            // Reset everything.
            foreach (var tile in _sortedTiles)
                tile.SetContent(TileContent.Empty);

            // Build a pool of tiles that can receive content (excluding checkpoint).
            List<BoardTile> pool = new();
            foreach (var tile in _sortedTiles)
                if (!tile.IsCheckpoint)
                    pool.Add(tile);

            // Fisher-Yates shuffle.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int positiveCount = _basePositiveCount + riskLevel * _positivePerRiskLevel;
            int negativeCount = _baseNegativeCount + riskLevel * _negativePerRiskLevel;

            // Guard: never request more tiles than are available.
            int total = positiveCount + negativeCount;
            if (total > pool.Count)
            {
                Debug.LogWarning($"[BoardManager] Risk {riskLevel} wants {total} content tiles but only {pool.Count} non-checkpoint tiles exist. Clamping.");
                float scale   = (float)pool.Count / total;
                positiveCount = Mathf.FloorToInt(positiveCount * scale);
                negativeCount = pool.Count - positiveCount;
            }

            int idx = 0;
            for (int i = 0; i < positiveCount; i++, idx++) pool[idx].SetContent(TileContent.Positive);
            for (int i = 0; i < negativeCount; i++, idx++) pool[idx].SetContent(TileContent.Negative);
        }

        // ── Editor helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Re-sorts and re-validates the tile list. Call from the Editor validator
        /// or if you add tiles dynamically.
        /// </summary>
        public void RefreshTileList() => BuildSortedList();

        // ── Private ───────────────────────────────────────────────────────────────

        private void BuildSortedList()
        {
            _sortedTiles.Clear();
            _sortedTiles.AddRange(_tiles);
            _sortedTiles.Sort((a, b) => a.Index.CompareTo(b.Index));
            ValidateList();
        }

        private void ValidateList()
        {
            // Duplicate index check.
            for (int i = 0; i < _sortedTiles.Count - 1; i++)
            {
                if (_sortedTiles[i].Index == _sortedTiles[i + 1].Index)
                    Debug.LogError($"[BoardManager] Duplicate tile index {_sortedTiles[i].Index}. Each tile needs a unique index.");
            }

            // Exactly one checkpoint check.
            int count = 0;
            foreach (var t in _sortedTiles)
                if (t.IsCheckpoint) count++;

            if (count == 0)
                Debug.LogError("[BoardManager] No tile is marked IsCheckpoint. One tile must be the checkpoint/start.");
            else if (count > 1)
                Debug.LogError($"[BoardManager] {count} tiles are marked IsCheckpoint. Only one is allowed.");
        }
    }
}
