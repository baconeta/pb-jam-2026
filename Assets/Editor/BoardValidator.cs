using Board;
using Game;
using UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only tool to catch common board setup mistakes before entering Play Mode.
/// Run from the menu:  Skip The Checkpoint ▸ Validate Board Setup
///
/// Checks:
///   - At least one BoardManager exists in the scene
///   - No duplicate tile indexes
///   - Exactly one checkpoint tile
///   - BoardPlayer exists
///   - CameraController exists
///   - BoardGameManager exists
///   - UIController exists (warning, not error)
/// </summary>
public static class BoardValidator
{
    [MenuItem("Skip The Checkpoint/Validate Board Setup")]
    private static void Validate()
    {
        int errors   = 0;
        int warnings = 0;

        // ── BoardManager ───────────────────────────────────────────────────────────
        BoardManager boardManager = Object.FindAnyObjectByType<BoardManager>();

        if (boardManager == null)
        {
            Debug.LogError("[Validator] ✗ No BoardManager found in the scene. Add one and assign your tiles.");
            errors++;
        }
        else
        {
            boardManager.RefreshTileList();

            BoardTile[] allTiles = Object.FindObjectsByType<BoardTile>();

            if (allTiles.Length == 0)
            {
                Debug.LogError("[Validator] ✗ No BoardTile components found in the scene. Add tiles and assign them to BoardManager.");
                errors++;
            }

            // Duplicate index check.
            System.Collections.Generic.HashSet<int> seen = new();
            foreach (var tile in allTiles)
            {
                if (!seen.Add(tile.Index))
                {
                    Debug.LogError($"[Validator] ✗ Duplicate tile index {tile.Index} on '{tile.name}'. Each tile needs a unique index.");
                    errors++;
                }
            }

            // Checkpoint count check.
            int cpCount = 0;
            foreach (var tile in allTiles)
                if (tile.IsCheckpoint) cpCount++;

            if (cpCount == 0)
            {
                Debug.LogError("[Validator] ✗ No tile is marked IsCheckpoint = true. The start/checkpoint tile must be marked.");
                errors++;
            }
            else if (cpCount > 1)
            {
                Debug.LogError($"[Validator] ✗ {cpCount} tiles are marked IsCheckpoint. Only one is allowed.");
                errors++;
            }
        }

        // ── BoardPlayer ────────────────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<BoardPlayer>() == null)
        {
            Debug.LogError("[Validator] ✗ No BoardPlayer found in the scene.");
            errors++;
        }

        // ── CameraController ───────────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<CameraController>() == null)
        {
            Debug.LogError("[Validator] ✗ No CameraController found. Add it to the Main Camera.");
            errors++;
        }

        // ── BoardGameManager ───────────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<BoardGameManager>() == null)
        {
            Debug.LogError("[Validator] ✗ No BoardGameManager found in the scene.");
            errors++;
        }

        // ── UIController (non-fatal) ───────────────────────────────────────────────
        if (Object.FindAnyObjectByType<UIController>() == null)
        {
            Debug.LogWarning("[Validator] ⚠ No UIController found. The game will run but no UI will update.");
            warnings++;
        }

        // ── Summary ────────────────────────────────────────────────────────────────
        if (errors == 0 && warnings == 0)
            Debug.Log("[Validator] ✓ Board setup looks good – no issues found.");
        else
            Debug.Log($"[Validator] Done. {errors} error(s), {warnings} warning(s). See Console for details.");
    }
}
