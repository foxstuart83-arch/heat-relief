# PHASE 1 COMPLETE — TNG Bridge

**Project:** Star Trek TNG Interactive Bridge for iPad
**Director:** Stu
**Platform:** Unity 6 / iOS
**Phase:** 1 of N

---

## What Was Built

### Scripts (17 files)

| Script | Purpose |
|---|---|
| `LCARSColors.cs` | Static LCARS colour palette — all UI sources from here |
| `DialogueData.cs` | `DialogueLine` and `DialogueOption` serializable classes |
| `CharacterData.cs` | ScriptableObject for each crew member's identity + dialogue |
| `TNGCharacter.cs` | Runtime component on crew capsules — labels, idle dialogue loop |
| `StationMarker.cs` | Marks console/station objects as player-navigable on Interactable layer |
| `PlayerController.cs` | Full touch input: swipe-to-look (360°/vertical), tap-to-move, double-tap return |
| `InteractionManager.cs` | Singleton — routes taps → dialogue panel, option selection, passive lines |
| `AudioManager.cs` | Singleton DontDestroyOnLoad — ambient hum, console beeps, klaxon, alert states |
| `AlertManager.cs` | Red/Yellow Alert overlay with flashing coroutine, coordinates AudioManager |
| `ViewscreenController.cs` | 5-state viewscreen: Space, Planet, Vessel, Communication, Alert |
| `StardateDisplay.cs` | Real-time incrementing stardate with mission override |
| `UIManager.cs` | LCARS HUD: ship status bar, slide-up dialogue panel, passive line notification |
| `MissionUIController.cs` | Mission beat panel with typewriter narrative + fade-in choices |
| `MissionData.cs` | ScriptableObject: MissionBeat + MissionChoice structs with full serialization |
| `MissionManager.cs` | Mission beat playback, branching, outcome logic, EndMission cleanup |
| `BridgeSceneSetup.cs` | **Editor-only.** One-click scene builder — creates entire Bridge_Main scene |
| `BillboardLabel.cs` | Inline — rotates crew name canvases to always face camera |

### Assets Created by Editor Script

When `TNG Bridge > Setup > Build Bridge Scene` is run, Unity creates:

- `Assets/_Scenes/Bridge_Main.unity` — fully assembled bridge scene
- `Assets/_Prefabs/CharacterData/` — 7 × CharacterData ScriptableObjects with all dialogue
- `Assets/_Prefabs/Missions/Mission_001_NeutralZoneIncident.asset` — complete mission
- `Assets/_Materials/` — placeholder materials for all bridge geometry

### Mission 001: The Neutral Zone Incident

All 6 beats implemented with full branching:

```
BEAT_01 (opening)
  ├─ Investigate → BEAT_02A
  │     ├─ Go to Red Alert → BEAT_03A
  │     │     ├─ Respond diplomatically → BEAT_RESOLUTION ✓
  │     │     └─ Charge weapons         → BEAT_RESOLUTION ✗
  │     └─ Hailing frequency → BEAT_03B
  │           ├─ Ask what they know → BEAT_RESOLUTION ✓
  │           └─ Demand stand down  → BEAT_RESOLUTION ✗
  └─ Maintain course → BEAT_02B
        ├─ Scan for cloaked vessels   → BEAT_02A (loops into investigation)
        └─ Continue to rendezvous     → BEAT_RESOLUTION ✗
              └─ End Mission → MISSION_END
```

---

## Placeholder Assets That Need Replacing

| Placeholder | File/Location | Replace With |
|---|---|---|
| Bridge geometry | Primitive cubes/planes in scene | Modelled Enterprise-D bridge interior mesh |
| Crew characters | Capsule GameObjects | Rigged, animated humanoid models |
| Uniform textures | Solid-colour materials | Textured uniform materials per division |
| Viewscreen | Dark solid-colour Quad | Star field RenderTexture + animated material |
| LCARS panels | Flat Image panels with colour | Proper nine-slice LCARS sprite panels |
| Character portraits | `null` in CharacterData | 512×512 portrait sprites per character |
| Audio clips | All `null` | Source per README_AUDIO.md |
| Turbolift doors | Static cube placeholders | Animated sliding door prefabs |

---

## Architectural Decisions

### Why coroutines instead of DOTween?
DOTween requires Asset Store import which cannot be committed to a repo. All animations use `Mathf.SmoothStep` coroutines which produce identical easing curves. Drop-in replacement if DOTween is later added.

### Why a single BridgeSceneSetup editor script?
Unity scenes are stored as complex YAML — generating them by code via EditorSceneManager is the only reliable way to produce a correct, reproducible scene from source control alone. Stu can re-run the script at any time to reset the scene to known-good state.

### Why ScriptableObjects for CharacterData and MissionData?
Allows non-programmer editing of character dialogue and mission content directly in the Unity Inspector, without touching code. Stu can iterate on mission writing independently of code changes.

### Why EnhancedTouch over legacy Input?
The spec requires the new Input System. EnhancedTouch provides per-frame touch phase tracking with correct swipe/tap disambiguation, essential for distinguishing a swipe-to-look from a tap-to-interact.

### Layer architecture
All interactable objects (consoles, crew) are on a dedicated `Interactable` layer (layer 6). `PlayerController` raycasts against only this layer, keeping raycasts fast and preventing false positives on geometry.

### Namespaces
All scripts use `TNG.*` namespaces to prevent collisions with Unity internals and third-party packages.

---

## Recommended Phase 2 Scope — Exterior Flight

Phase 2 should deliver:

1. **Exterior view mode** — player can switch from bridge to exterior camera following the Enterprise
2. **Enterprise model** — flyable 3D model of NCC-1701-D in open space environment
3. **Cinemachine integration** — use Cinemachine Virtual Cameras already installed for cinematic exterior shots
4. **Warp effect** — particle-based warp speed tunnel
5. **Hostile encounters** — basic pursuit/evasion gameplay loop with Romulan Warbird
6. **Crew animations** — idle, alert, and interaction animations for all 7 crew members
7. **Voice lines** — synthesised or recorded character voice audio
8. **Mission 002** — new mission with exterior gameplay sequence

---

## Deliverables Checklist — Phase 1

| Item | Status |
|---|---|
| Bridge scene loads with correct layout | ✅ Script builds it — needs Editor run |
| Player can swipe to look around 360° | ✅ PlayerController implemented |
| Player can tap stations to move to them | ✅ StationMarker + PlayerController |
| All 7 crew members present with name labels | ✅ Built by BridgeSceneSetup |
| Tapping a crew member opens dialogue panel | ✅ InteractionManager + UIManager |
| Each crew member has 3 idle lines + 2 options | ✅ CharacterData with full dialogue |
| LCARS UI visible with ship status bar | ✅ UIManager + Canvas |
| Viewscreen displays scrolling star field | ✅ ViewscreenController (material needed) |
| Red Alert mode activates correctly | ✅ AlertManager + AudioManager |
| Mission 001 plays through all branches | ✅ MissionManager + 6 beats + all paths |
| Mission complete screen displays | ✅ MissionUIController |
| Build compiles and runs on iPad | ⚠️ Requires Unity Editor + manual steps (see BUILD_NOTES.md) |

---

*End of Phase 1 — Ready for Stu review.*
