# Skip the Checkpoint – CLAUDE.md

This is a Unity 2D board-game-style jam project. Read this before making any changes.

## Core Rules

- **Do not edit scene files, prefab YAML, ScriptableObjects, animation controllers, or other
  serialized Unity assets** unless explicitly asked. Always prefer creating or editing C# scripts.
- **Do not rename serialized fields** once they exist – renaming breaks Inspector-assigned values
  in scenes and prefabs silently.
- **Prefer small, focused changes** over large rewrites. One file at a time where possible.
- **After any code change, provide Inspector setup instructions** so the developer knows what to
  wire up in Unity.
- **Bias toward shippability.** Game jams have deadlines. Correct and shippable beats elegant and
  incomplete.
- **When unsure about scope or approach, propose a plan first** and wait for approval before editing.

## Project Layout

```
Assets/
  Scripts/
    Board/          – BoardTile, BoardManager, BoardPlayer
    Game/           – BoardGameManager (state machine), ScoreManager, HighScoreManager
    Camera/         – CameraController
    UI/             – UIController (game UI) + existing StateSwitcher framework
    Managers/       – AudioManager (existing, do not replace)
    Utils/          – Singleton<T>, EverlastingSingleton<T>, helpers (existing)
    Audio/          – Audio system (existing, do not replace)
    Analytics/      – Analytics (existing)
    Achievements/   – Achievement system (existing)
  Editor/           – BoardValidator + existing editor tools
```

## Existing Systems – Do Not Replace

- `Utils.Singleton<T>` – scene-local singleton base class
- `Utils.EverlastingSingleton<T>` – DontDestroyOnLoad singleton base class
- `Managers.AudioManager` – audio playback, use this for all sound
- `Audio.AudioWrapper` – named-sound dictionary wrapper around AudioManager
- `Achievements.AchievementController` – PlayerPrefs achievement tracking
- `Managers.SceneLoader` – async scene management
- `Utils.FileUtils` – JSON file I/O

## Coding Style

- Serialized private fields (`[SerializeField] private`) over public fields.
- No comments on obvious lines. Comments only for non-obvious WHY, hidden constraints, or
  workarounds.
- No heavy abstraction. No interfaces unless clearly necessary.
- Coroutines for movement and transitions. No async/await unless forced.
- TextMeshPro (`TMP_Text`, `TMP_InputField`) is available – use it for UI text.
- All game scripts must compile without final art or audio assets assigned.

## Key Design Decisions

- Board tiles are placed manually in the scene. No procedural generation.
- `BoardGameManager` is the single coordinator. UI buttons call its public methods directly.
- Negative tile behaviour is currently "game over" but is isolated in `BoardGameManager` so it can
  be changed to "return to checkpoint" without touching other systems.
- High scores use PlayerPrefs + JsonUtility (top 5, local only).

## Game Flow & Board Preview

The board must be **shuffled and visible before the player rolls or makes decisions.**

- **Start of game** (`InitGame`): board is shuffled at risk 0, camera is zoomed out,
  start panel appears. `StartGame()` (wired to the Start button) zooms in and begins play.
- **Checkpoint reached** (`HandleCheckpointReached`): board reshuffles at current risk,
  camera zooms out and waits for zoom to complete, then the checkpoint decision panel appears.
  The player makes their decision while seeing the full board layout.
- **After Take**: board reshuffles at risk 0 (cleaner board), camera zooms in, play resumes.
- **After Skip**: board reshuffles at new (higher) risk, stays zoomed out for
  `_boardPreviewDuration` seconds so the player sees the result, then camera zooms in.

`CameraController.WaitForZoomComplete()` is a public coroutine that GameManager yields on
to avoid showing decision UI before the zoom settles.

## Audio Hooks

All gameplay audio is routed through `BoardGameAudioController` (attached to the GameManager
GameObject). It calls `AudioWrapper.Instance.PlaySound(string)` by name.

- **All audio fields are optional** — leave any string empty to skip that sound silently.
- If `AudioWrapper` is missing from the scene, a single warning is logged and all sounds are
  suppressed. The game continues to function normally.
- **Do not hardcode audio clip paths or add AudioClip assets through code.** Sounds are
  configured entirely in the Inspector via AudioWrapper's SoundData list.
- `BoardPlayer` has a direct `_audio` reference for per-hop sounds to avoid routing all
  movement audio through GameManager.

## Banked vs Run Score System

`ScoreManager` tracks two score buckets:
- **`bankedScore`** – safe score locked in when the player Takes the checkpoint.
- **`runScore`** – at-risk score accumulated since the last checkpoint.
- **`totalScore`** = `bankedScore + runScore`. This is what high scores record.

On **Take Checkpoint**: `runScore` banks into `bankedScore`, risk resets to 0.
On **Skip Checkpoint**: risk increases, `runScore` stays at risk (not banked).
On **Game Over**: high score = `bankedScore + runScore` (all score counts, even the at-risk portion).

## Multiplier System

The score multiplier is driven by a **serialized `AnimationCurve`** on `ScoreManager`:
- Field: `_riskMultiplierCurve` (Inspector: Risk Settings → Risk Multiplier Curve)
- **X axis** = risk level (integer). **Y axis** = multiplier applied to base tile score.
- Result is always clamped to >= 1.0.
- Add/drag keyframes to adjust progression without any code changes.
- Default keyframes: risk 0→1×, 1→2×, 2→4×, 3→7×, 4→10×.

**Do not hardcode multiplier logic.** Always route through `ScoreManager.GetMultiplier(riskLevel)`.

**Do not rename `_riskMultiplierCurve`** – renaming a serialized field silently resets it to defaults
in any existing scene, losing the designer's tuning.

## Serialization Safety

- Never rename existing `[SerializeField]` fields – it loses Inspector-assigned values.
- Adding new serialized fields is always safe (they start at their C# default).
- Removing a serialized field is safe (Unity silently drops the value on next save).
- Private non-serialized fields (runtime state) can be renamed freely.
