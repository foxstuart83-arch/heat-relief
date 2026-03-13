# TNG Bridge — Audio File Requirements

All audio clips must be sourced and added manually.
Place files in: `Assets/_Audio/`
Then assign them in the **AudioManager** Inspector in the Bridge_Main scene.

---

## Required Audio Files

| Inspector Field | Filename | Description | Loop? | Target Volume |
|---|---|---|---|---|
| `bridgeAmbientClip` | `bridge_ambient.wav` | Low continuous hum of ship systems — warp core, life support, consoles | YES | 0.30 |
| `consolBeepClip` | `console_beep.wav` | Short LCARS console beep (0.3–0.8 sec) | NO | 0.15 |
| `redAlertKlaxonClip` | `red_alert_klaxon.wav` | Red Alert klaxon loop | YES | 0.60 |
| `yellowAlertToneClip` | `yellow_alert_tone.wav` | Single Yellow Alert tone (one-shot) | NO | 0.60 |
| `dialogueChimeClip` | `dialogue_chime.wav` | Short chime when dialogue panel opens | NO | 0.50 |

---

## Recommended Sources

> These are fan project suggestions. Verify licensing before use.

- **Star Trek sound effects archives** — widely archived online
- **Free Sound (freesound.org)** — search "sci-fi hum", "computer beep", "klaxon"
- **Trek Core / TrekSounds** — dedicated Trek SFX archives
- For authentic TNG sounds, the original show's audio is the reference

---

## Audio Specifications

For best results on iPad, export audio as:
- **Format**: WAV (uncompressed) or OGG (for file size)
- **Sample rate**: 44100 Hz
- **Bit depth**: 16-bit
- **Channels**: Mono for positional SFX, Stereo for ambient/music

Unity will compress audio on import per platform. Set iOS compression override to:
- Ambient loops: Streaming (saves memory)
- Short SFX: Compressed in memory (fastest)

---

## Future Audio (Phase 2+)

| File | Description |
|---|---|
| `captain_log_intro.wav` | Captain's log opening strum + speech |
| `warp_engage.wav` | Warp speed engaging sound |
| `transporter.wav` | Transporter effect |
| `phaser_fire.wav` | Phaser shot sound |
| `shields_hit.wav` | Shield impact |
| `door_slide.wav` | Turbolift door open/close |
| `comm_badge_chirp.wav` | Communicator badge tap |

---

*Last updated: Phase 1*
