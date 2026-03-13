using System.Collections;
using UnityEngine;
using TNG.Characters;
using TNG.UI;
using TNG.Audio;

namespace TNG.Interactions
{
    /// <summary>
    /// Central hub for all player-initiated and ambient interactions on the bridge.
    ///
    /// Responsible for:
    ///   - Opening and closing the dialogue panel
    ///   - Routing player dialogue choices to character responses
    ///   - Firing dialogue option UnityEvents (e.g. starting a mission)
    ///   - Playing passive (ambient) lines from crew without full interaction
    ///
    /// Singleton — place on a persistent [InteractionManager] GameObject in the scene.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static InteractionManager Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────

        private TNGCharacter  _currentCharacter;
        private DialogueLine  _currentLine;
        private bool          _isInteracting;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the player is in an active dialogue with a crew member.</summary>
        public bool IsInteracting => _isInteracting;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initiates a full interaction with the specified crew member.
        /// Opens the dialogue panel with their first idle line.
        /// Called by PlayerController on tap.
        /// </summary>
        public void Interact(TNGCharacter character)
        {
            if (_isInteracting) return;
            if (character == null || character.data == null) return;

            _currentCharacter = character;
            _isInteracting    = true;
            character.SetInteracting(true);

            DialogueLine line = character.GetRandomIdleLine();
            if (line == null)
            {
                // Character has no dialogue configured yet
                line = new DialogueLine
                {
                    speakerName  = character.data.characterName,
                    dialogueText = "(No dialogue assigned yet.)"
                };
            }

            _currentLine = line;
            AudioManager.Instance?.PlayDialogueChime();
            UIManager.Instance?.ShowDialoguePanel(line, character.data);
        }

        /// <summary>
        /// Displays an ambient (non-blocking) line in a small notification.
        /// Used when a crew member speaks an idle line without the player tapping them.
        /// Does not lock interaction or open the full panel.
        /// </summary>
        public void ShowPassiveLine(DialogueLine line, TNGCharacter character)
        {
            if (_isInteracting) return;
            UIManager.Instance?.ShowPassiveLine(line, character.data);
        }

        /// <summary>
        /// Called by the UI when the player selects a dialogue option.
        /// Plays the response text then closes the panel.
        /// Fires the option's UnityEvent (e.g. start a mission).
        /// </summary>
        public void SelectOption(DialogueOption option)
        {
            if (!_isInteracting) return;

            // Fire any attached Unity events (e.g. MissionManager.LoadMission)
            option.onSelected?.Invoke();

            if (!string.IsNullOrEmpty(option.responseText))
            {
                // Show response line — reuse current character data, no further choices
                DialogueLine responseLine = new DialogueLine
                {
                    speakerName  = _currentLine.speakerName,
                    dialogueText = option.responseText
                    // No nested options — response leads to panel close
                };
                UIManager.Instance?.ShowDialoguePanel(responseLine, _currentCharacter?.data);
                _currentLine = responseLine;
            }
            else
            {
                CloseDialogue();
            }
        }

        /// <summary>
        /// Closes the active dialogue panel and releases interaction state.
        /// Called by the UI close button or after a terminal response.
        /// </summary>
        public void CloseDialogue()
        {
            _isInteracting = false;

            if (_currentCharacter != null)
            {
                _currentCharacter.SetInteracting(false);
                _currentCharacter = null;
            }

            _currentLine = null;
            UIManager.Instance?.HideDialoguePanel();
        }
    }
}
