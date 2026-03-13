# TNG Bridge — Build Notes

## Project Configuration

| Setting | Value |
|---|---|
| Unity Version | 6.0 (6000.x LTS) |
| Render Pipeline | Universal Render Pipeline (URP) 17.x |
| Target Platform | iOS (iPad) |
| Bundle ID | com.stu.tngbridge |
| Version | 0.1.0 |
| Min iOS | 16.0 |
| Target Resolution | 2732 × 2048 (iPad Pro 12.9") |
| Orientation | Landscape Left + Right only |
| Architecture | ARM64 |

---

## Packages Installed

| Package | Version | Purpose |
|---|---|---|
| Universal RP | 17.0.3 | Rendering pipeline |
| Cinemachine | 3.1.2 | Camera system (future use Phase 2) |
| TextMeshPro | 3.2.0-pre.10 | All UI text |
| Input System | 1.11.2 | Touch input (new Input System) |

> **Note:** DOTween is referenced in the spec but is not available via UPM (Asset Store only).
> All animations use Unity coroutines with `Mathf.SmoothStep` for equivalent easing.
> If DOTween is later added via Asset Store, replace smoothstep lerps in:
> - `PlayerController.cs` → `MoveToPosition()`
> - `UIManager.cs` → `SlidePanelCoroutine()`
> - `MissionUIController.cs` → `FadeCanvasGroup()`

---

## Manual Setup Steps After Opening Project

### Step 1 — Run Scene Builder
1. Open Unity 6 and wait for package import to complete
2. Open menu: **TNG Bridge > Setup > Build Bridge Scene**
3. Confirm the dialog — scene will be created at `Assets/_Scenes/Bridge_Main.unity`
4. Also run: **TNG Bridge > Setup > Create Mission 001 Asset** if not already created

### Step 2 — Input System Backend
1. Go to **Edit > Project Settings > Player > Other Settings**
2. Set **Active Input Handling** to `Input System Package (New)`
3. Restart Unity if prompted

### Step 3 — URP Asset
1. Go to **Edit > Project Settings > Graphics**
2. Assign a URP Asset (create one via **Assets > Create > Rendering > URP Asset**)
3. Assign the same asset in **Quality Settings** for iOS

### Step 4 — TextMeshPro Setup
1. Go to **Window > TextMeshPro > Import TMP Essential Resources**
2. This is required for all TMPro text to render correctly

### Step 5 — Assign Audio Clips
1. Add audio files to `Assets/_Audio/` (see `README_AUDIO.md`)
2. Select `[AudioManager]` in the Bridge_Main scene hierarchy
3. Assign each clip in the Inspector

### Step 6 — Wire UI Inspector References
After the scene is built, open `Bridge_Main.unity` and manually wire:
- `UIManager` → all Panel, Text, Button references
- `MissionUIController` → all Panel, Text, Button references
- `AlertManager` → alertOverlayImage, alertLabel
- `PlayerController` → cameraRig (already set by script, verify)
- `MissionManager` → missionLibrary array (drag Mission_001 asset in)
- Picard's "Begin Mission" dialogue option → `MissionManager.LoadMission()`

### Step 7 — Configure iOS Build Settings
1. **File > Build Settings > Switch Platform > iOS**
2. **Player Settings > Other Settings:**
   - Bundle Identifier: `com.stu.tngbridge`
   - Version: `0.1.0`
   - Target minimum iOS: 16.0
3. **Player Settings > Resolution and Presentation:**
   - Default orientation: Landscape Left
   - Disable Portrait, Portrait Upside Down
   - Enable Landscape Left, Landscape Right
4. **Player Settings > Target Device:** iPad only

### Step 8 — Xcode
1. Click **Build** — Unity generates an Xcode project
2. Open in Xcode
3. Set Team / provisioning profile
4. Deploy to iPad

---

## Known Issues and Placeholders

| Item | Status | Notes |
|---|---|---|
| Bridge geometry | PLACEHOLDER — primitives | Replace with modelled Enterprise-D bridge mesh |
| Crew character models | PLACEHOLDER — capsules | Replace with rigged humanoid models + animations |
| Viewscreen materials | PLACEHOLDER — solid colour | Replace with star field RenderTexture + material |
| LCARS border radius | APPROXIMATED | True LCARS asymmetric radius requires custom UI shader or sprite nine-slice |
| Audio clips | MISSING | Source and assign per README_AUDIO.md |
| Character portraits | MISSING (null) | Assign Sprite in each CharacterData asset |
| Inspector wiring | MANUAL | UI references must be wired after scene creation |
| DOTween | NOT USED | Replaced with SmoothStep coroutines |
| Cinemachine | INSTALLED, NOT USED | Reserved for Phase 2 exterior camera |

---

## Folder Structure

```
Assets/
  _Scenes/
    Bridge_Main.unity          ← Main game scene
  _Scripts/
    Camera/
      PlayerController.cs      ← Touch input, movement, raycasting
    Characters/
      CharacterData.cs         ← ScriptableObject for crew data
      TNGCharacter.cs          ← Runtime crew member behaviour
    Interactions/
      DialogueData.cs          ← DialogueLine + DialogueOption structs
      StationMarker.cs         ← Marks interactable stations
      InteractionManager.cs    ← Dialogue orchestration
    Missions/
      MissionData.cs           ← ScriptableObject for missions/beats
      MissionManager.cs        ← Mission playback logic
    UI/
      LCARSColors.cs           ← Colour palette constants
      StardateDisplay.cs       ← Incrementing stardate label
      AlertManager.cs          ← Red/Yellow alert visual management
      UIManager.cs             ← Main HUD: status bar, dialogue panel
      MissionUIController.cs   ← Mission beat UI + typewriter
    Audio/
      AudioManager.cs          ← Singleton audio layer manager
    Viewscreen/
      ViewscreenController.cs  ← Viewscreen state machine
  _Prefabs/
    CharacterData/             ← 7 CharacterData .asset files
    Missions/
      Mission_001_NeutralZoneIncident.asset
  _Materials/                  ← Auto-generated placeholder materials
  _Audio/                      ← Place audio files here (gitignored)
  _UI/                         ← Future: UI sprite sheets, LCARS panels
  _Animations/                 ← Future: character animations
  _Models/                     ← Future: bridge mesh, character models
  Editor/
    BridgeSceneSetup.cs        ← One-click scene builder
  README_AUDIO.md
  BUILD_NOTES.md
  PHASE1_COMPLETE.md
```

---

*Director: Stu | Build Agent: Claude Code | Phase 1*
