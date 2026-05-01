using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// A single high score entry. Must be serializable for JsonUtility.
    /// </summary>
    [Serializable]
    public class HighScoreEntry
    {
        public string playerName;
        public int    score;
    }

    // JsonUtility cannot serialize a root List<T> directly – wrap it.
    [Serializable]
    internal class HighScoreList
    {
        public List<HighScoreEntry> entries = new();
    }

    /// <summary>
    /// Static helper for reading and writing local high scores via PlayerPrefs + JsonUtility.
    /// Stores the top <see cref="MaxEntries"/> entries, sorted by score descending.
    ///
    /// Usage:
    ///   HighScoreManager.SaveScore("Alice", 1500);
    ///   List&lt;HighScoreEntry&gt; top = HighScoreManager.GetTopScores();
    /// </summary>
    public static class HighScoreManager
    {
        private const string PrefKey    = "SkipCheckpoint_HighScores";
        public  const int    MaxEntries = 5;

        /// <summary>
        /// Adds a new score, keeps only the top <see cref="MaxEntries"/>, and persists.
        /// </summary>
        public static void SaveScore(string playerName, int score)
        {
            HighScoreList list = Load();
            list.entries.Add(new HighScoreEntry { playerName = playerName, score = score });

            // Sort descending.
            list.entries.Sort((a, b) => b.score.CompareTo(a.score));

            // Trim to max.
            if (list.entries.Count > MaxEntries)
                list.entries.RemoveRange(MaxEntries, list.entries.Count - MaxEntries);

            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();

            // Find the rank of the entry we just saved (1-based).
            int rank = list.entries.FindIndex(e => e.playerName == playerName && e.score == score) + 1;
            Debug.Log($"[HighScoreManager] Saved score – name: '{playerName}', score: {score}, rank: #{rank} of {list.entries.Count}.");
        }

        /// <summary>
        /// Returns the stored top scores. May return fewer than MaxEntries if not enough
        /// games have been played yet.
        /// </summary>
        public static List<HighScoreEntry> GetTopScores() => Load().entries;

        /// <summary>
        /// Erases all stored high scores. Useful for testing.
        /// </summary>
        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            Debug.Log("[HighScoreManager] All high scores cleared.");
        }

        private static HighScoreList Load()
        {
            string json = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new HighScoreList();

            try
            {
                return JsonUtility.FromJson<HighScoreList>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HighScoreManager] Could not parse saved scores ({e.Message}). Starting fresh.");
                return new HighScoreList();
            }
        }
    }
}
