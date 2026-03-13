using System.Collections.Generic;
using UnityEngine;
using TNG.Interactions;

namespace TNG.Characters
{
    /// <summary>
    /// ScriptableObject holding all static data for a TNG crew member.
    /// Create via: Assets > Create > TNG > Character Data
    /// </summary>
    [CreateAssetMenu(fileName = "CharData_NewCharacter", menuName = "TNG/Character Data", order = 1)]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Full name of the crew member (e.g. "Jean-Luc Picard").</summary>
        public string characterName;

        /// <summary>Starfleet rank (e.g. "Captain", "Lieutenant Commander").</summary>
        public string rank;

        /// <summary>Role or position (e.g. "Commanding Officer").</summary>
        public string role;

        /// <summary>Home station on the bridge (e.g. "Command Chair").</summary>
        public string station;

        [Header("Appearance")]
        /// <summary>
        /// Division uniform colour.
        /// Red = Command (#CC0000), Gold = Operations (#CCAA00), Teal = Sciences (#006666)
        /// </summary>
        public Color uniformColor = Color.white;

        /// <summary>Portrait sprite displayed in the dialogue panel. Assign once art is available.</summary>
        public Sprite portrait;

        [Header("Dialogue")]
        /// <summary>
        /// Ambient lines the character says while idle on the bridge.
        /// Displayed when the player approaches without initiating a mission.
        /// </summary>
        public List<DialogueLine> idleDialogue = new List<DialogueLine>();
    }
}
