using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TNG.Interactions
{
    /// <summary>
    /// A single unit of dialogue spoken by one character, with optional response choices.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        /// <summary>Display name of the character speaking this line.</summary>
        public string speakerName;

        /// <summary>The spoken dialogue text.</summary>
        [TextArea(2, 5)]
        public string dialogueText;

        /// <summary>Player-selectable responses shown after this line. Max 3.</summary>
        public List<DialogueOption> options = new List<DialogueOption>();
    }

    /// <summary>
    /// One choice the player can make in response to a dialogue line.
    /// </summary>
    [Serializable]
    public class DialogueOption
    {
        /// <summary>Short label shown on the LCARS button.</summary>
        public string optionText;

        /// <summary>Character's response after this option is selected.</summary>
        [TextArea(2, 5)]
        public string responseText;

        /// <summary>Optional Unity event fired when this option is selected (e.g. trigger mission).</summary>
        public UnityEvent onSelected;
    }
}
