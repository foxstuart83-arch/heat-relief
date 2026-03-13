using System;
using System.Collections.Generic;
using UnityEngine;
using TNG.Audio;
using TNG.Viewscreen;

namespace TNG.Missions
{
    /// <summary>
    /// ScriptableObject defining a complete scripted mission with branching beats.
    /// Create via: Assets > Create > TNG > Mission Data
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_001_NewMission", menuName = "TNG/Mission Data", order = 2)]
    public class MissionData : ScriptableObject
    {
        [Header("Mission Identity")]
        /// <summary>Display title shown in the mission UI (e.g. "The Neutral Zone Incident").</summary>
        public string missionTitle;

        /// <summary>Stardate string displayed when the mission begins (e.g. "47634.2").</summary>
        public string stardate;

        /// <summary>Captain's log briefing text shown before the mission starts.</summary>
        [TextArea(4, 8)]
        public string briefingText;

        [Header("Mission Beats")]
        /// <summary>All beats that make up this mission. The first beat in the list is always the opening beat.</summary>
        public List<MissionBeat> beats = new List<MissionBeat>();

        /// <summary>
        /// Finds a beat by its unique beatID string.
        /// Returns null if no match is found.
        /// </summary>
        public MissionBeat FindBeat(string beatID)
        {
            return beats.Find(b => b.beatID == beatID);
        }
    }

    /// <summary>
    /// One narrative moment in a mission. Sets scene state and offers player choices.
    /// </summary>
    [Serializable]
    public class MissionBeat
    {
        /// <summary>Unique identifier used to link choices to this beat (e.g. "BEAT_01", "BEAT_03A").</summary>
        public string beatID;

        /// <summary>Narrative text displayed with typewriter effect in the mission panel.</summary>
        [TextArea(4, 10)]
        public string narrativeText;

        /// <summary>Viewscreen state to apply when this beat begins.</summary>
        public ViewscreenState viewscreenState;

        /// <summary>Audio/alert state to apply when this beat begins.</summary>
        public AudioState audioState;

        /// <summary>Player choices presented after the narrative completes. Max 3.</summary>
        public List<MissionChoice> choices = new List<MissionChoice>();
    }

    /// <summary>
    /// One choice the player can make at a mission decision point.
    /// </summary>
    [Serializable]
    public class MissionChoice
    {
        /// <summary>Short text shown on the LCARS choice button.</summary>
        public string choiceText;

        /// <summary>Outcome text briefly shown after selecting this choice.</summary>
        [TextArea(2, 5)]
        public string outcomeText;

        /// <summary>The beatID of the next beat to load. Use "MISSION_END" to end the mission.</summary>
        public string nextBeatID;

        /// <summary>Whether this choice leads to a successful outcome. Used for mission complete screen.</summary>
        public bool isSuccess;
    }
}
