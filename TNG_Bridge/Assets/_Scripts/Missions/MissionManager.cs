using UnityEngine;
using TNG.UI;
using TNG.Audio;
using TNG.Viewscreen;

namespace TNG.Missions
{
    /// <summary>
    /// Singleton that drives mission playback on the bridge.
    ///
    /// Workflow:
    ///   1. A crew member interaction fires LoadMission(MissionData) via UnityEvent.
    ///   2. MissionManager shows the briefing, then GotoBeat("BEAT_01").
    ///   3. Each beat sets the viewscreen state, audio state, and shows narrative.
    ///   4. Player selects a choice → SelectChoice(index) → GotoBeat(nextBeatID).
    ///   5. "MISSION_END" nextBeatID triggers EndMission().
    ///
    /// The "MISSION_END" sentinel string ends the mission gracefully.
    /// Attach to a persistent [MissionManager] GameObject in the Bridge_Main scene.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static MissionManager Instance { get; private set; }

        // ── Inspector References ─────────────────────────────────────────────

        [Header("Mission Library")]
        [Tooltip("Drag all MissionData ScriptableObjects here for reference. " +
                 "Missions are also loaded dynamically via LoadMission().")]
        public MissionData[] missionLibrary;

        // ── Private State ─────────────────────────────────────────────────────

        private MissionData  _activeMission;
        private MissionBeat  _currentBeat;
        private bool         _missionRunning;
        private bool         _lastChoiceSuccess;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a mission is actively playing.</summary>
        public bool IsMissionRunning => _missionRunning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Loads and begins a mission. Called via UnityEvent from a dialogue option
        /// (e.g. Picard's "Begin Mission" interaction option).
        ///
        /// Shows the mission briefing text, then proceeds to the first beat.
        /// </summary>
        public void LoadMission(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogError("[MissionManager] LoadMission called with null MissionData.");
                return;
            }

            if (_missionRunning)
            {
                Debug.LogWarning("[MissionManager] A mission is already running. End it first.");
                return;
            }

            _activeMission     = mission;
            _missionRunning    = true;
            _lastChoiceSuccess = false;

            Debug.Log($"[MissionManager] Starting mission: {mission.missionTitle}");

            // Set stardate on the status bar display
            UIManager.Instance?.stardateDisplay?.SetMissionStardate(mission.stardate);

            // Proceed to first beat
            if (mission.beats != null && mission.beats.Count > 0)
                GotoBeat(mission.beats[0].beatID);
            else
                Debug.LogError($"[MissionManager] Mission '{mission.missionTitle}' has no beats.");
        }

        /// <summary>
        /// Loads a mission by name from the missionLibrary array.
        /// Convenience method for UnityEvent hookups that don't hold a direct reference.
        /// </summary>
        public void LoadMissionByTitle(string missionTitle)
        {
            foreach (var m in missionLibrary)
            {
                if (m != null && m.missionTitle == missionTitle)
                {
                    LoadMission(m);
                    return;
                }
            }
            Debug.LogError($"[MissionManager] No mission with title '{missionTitle}' found in library.");
        }

        /// <summary>
        /// Transitions the mission to the beat with the given ID.
        /// Applies viewscreen and audio states, then shows narrative in MissionUIController.
        /// </summary>
        public void GotoBeat(string beatID)
        {
            if (!_missionRunning || _activeMission == null) return;

            if (beatID == "MISSION_END")
            {
                FinishMission();
                return;
            }

            MissionBeat beat = _activeMission.FindBeat(beatID);
            if (beat == null)
            {
                Debug.LogError($"[MissionManager] Beat ID '{beatID}' not found in mission '{_activeMission.missionTitle}'.");
                return;
            }

            _currentBeat = beat;

            // Apply environment states
            ViewscreenController.Instance?.SetViewscreenState(beat.viewscreenState);
            AlertManager.Instance?.SetAlertState(beat.audioState);

            // Drive mission UI
            MissionUIController.Instance?.ShowBeat(beat, _activeMission.missionTitle, _activeMission.stardate);
        }

        /// <summary>
        /// Called by MissionUIController when the player selects a choice button.
        /// Validates, records success state, shows outcome text briefly, then advances beat.
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (!_missionRunning || _currentBeat == null) return;
            if (choiceIndex >= _currentBeat.choices.Count) return;

            MissionChoice choice = _currentBeat.choices[choiceIndex];
            _lastChoiceSuccess = choice.isSuccess;

            Debug.Log($"[MissionManager] Choice selected: '{choice.choiceText}' → Beat: {choice.nextBeatID}");

            // Show outcome text in narrative area before advancing
            // A brief pause lets the player read it before the next beat loads
            StartCoroutine(ShowOutcomeThenAdvance(choice));
        }

        private System.Collections.IEnumerator ShowOutcomeThenAdvance(MissionChoice choice)
        {
            // Show the outcome text as a mini-beat (reuse narrative display, no choices)
            if (!string.IsNullOrEmpty(choice.outcomeText))
            {
                MissionBeat outcomeBeat = new MissionBeat
                {
                    beatID        = "OUTCOME",
                    narrativeText = choice.outcomeText,
                    viewscreenState = _currentBeat.viewscreenState,
                    audioState    = _currentBeat.audioState
                };
                MissionUIController.Instance?.ShowBeat(outcomeBeat, _activeMission.missionTitle, _activeMission.stardate);
                yield return new WaitForSeconds(3f);
            }

            GotoBeat(choice.nextBeatID);
        }

        /// <summary>
        /// Called when the player taps "Return to Bridge" on the mission complete screen.
        /// Resets all mission state.
        /// </summary>
        public void EndMission()
        {
            _missionRunning = false;
            _activeMission  = null;
            _currentBeat    = null;

            // Restore environment to defaults
            ViewscreenController.Instance?.SetViewscreenState(ViewscreenState.Space);
            AlertManager.Instance?.SetAlertState(AudioState.Normal);
            UIManager.Instance?.stardateDisplay?.ClearMissionOverride();
            MissionUIController.Instance?.HideMissionPanel(instant: false);

            Debug.Log("[MissionManager] Mission ended. Bridge returned to normal operations.");
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void FinishMission()
        {
            string outcomeText = _lastChoiceSuccess
                ? "The Enterprise continues on her mission. Well done, Captain."
                : "The situation has been resolved — though perhaps not in the Federation's best interest.";

            MissionUIController.Instance?.ShowMissionComplete(outcomeText, _lastChoiceSuccess);
        }
    }
}
