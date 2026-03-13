using UnityEngine;
using TMPro;

namespace TNG.UI
{
    /// <summary>
    /// Slowly increments and displays the Starfleet stardate in the ship status bar.
    ///
    /// Stardate format: XXXXX.X (five digits dot one decimal)
    /// Starts at the configurable baseStardate and increments in real time.
    ///
    /// Mission beats can override the displayed stardate temporarily via
    /// SetMissionStardate() and restore it via ClearMissionOverride().
    ///
    /// Attach to the ship status bar's stardate TextMeshPro object.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class StardateDisplay : MonoBehaviour
    {
        // ── Inspector Properties ─────────────────────────────────────────────

        [Tooltip("Starting stardate. Phase 1 begins at ~47634 (TNG Season 7 timeframe).")]
        public float baseStardate = 47634.2f;

        [Tooltip("How many stardate units pass per real-world second. " +
                 "At 0.005 this is ~18 per hour — slow and atmospheric.")]
        public float incrementPerSecond = 0.005f;

        // ── Private State ─────────────────────────────────────────────────────

        private TextMeshProUGUI _text;
        private float           _currentStardate;
        private float?          _missionOverride; // if set, display this instead

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _text            = GetComponent<TextMeshProUGUI>();
            _currentStardate = baseStardate;
        }

        private void Update()
        {
            if (_missionOverride.HasValue)
            {
                // During a mission beat, show the mission stardate (static)
                _text.text = $"STARDATE {_missionOverride.Value:F1}";
                return;
            }

            _currentStardate += incrementPerSecond * Time.deltaTime;
            _text.text = $"STARDATE {_currentStardate:F1}";
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Overrides the displayed stardate with a static mission value.
        /// Call at the start of each mission beat.
        /// </summary>
        public void SetMissionStardate(float stardate)
        {
            _missionOverride = stardate;
        }

        /// <summary>
        /// Parses and sets the mission stardate from the "47634.2" string format
        /// used in MissionData.
        /// </summary>
        public void SetMissionStardate(string stardateString)
        {
            if (float.TryParse(stardateString, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture,
                               out float value))
            {
                _missionOverride = value;
            }
            else
            {
                Debug.LogWarning($"[StardateDisplay] Could not parse stardate string: {stardateString}");
            }
        }

        /// <summary>
        /// Removes the mission override and resumes real-time stardate counting.
        /// Call when a mission ends.
        /// </summary>
        public void ClearMissionOverride()
        {
            _missionOverride = null;
        }

        /// <summary>Returns the current live stardate value.</summary>
        public float CurrentStardate => _currentStardate;
    }
}
