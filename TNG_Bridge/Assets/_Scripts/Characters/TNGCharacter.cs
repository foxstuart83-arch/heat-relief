using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TNG.Interactions;

namespace TNG.Characters
{
    /// <summary>
    /// Runtime component for a TNG crew member on the bridge.
    ///
    /// Holds a reference to the character's CharacterData ScriptableObject
    /// and drives idle behaviour (random line selection, name label display).
    ///
    /// Attach to each crew member's root GameObject (capsule placeholder or mesh).
    /// Place the GameObject on the Interactable layer so the PlayerController can raycast it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TNGCharacter : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────

        [Header("Character Data")]
        [Tooltip("ScriptableObject containing this character's data. " +
                 "Create via Assets > Create > TNG > Character Data.")]
        public CharacterData data;

        [Header("Name Label")]
        [Tooltip("World-space TextMeshPro above the character's head.")]
        public TextMeshPro nameLabel;

        [Tooltip("Rank + role shown below the name label.")]
        public TextMeshPro rankLabel;

        [Header("Idle Behaviour")]
        [Tooltip("Minimum seconds between idle dialogue plays.")]
        public float idleIntervalMin = 20f;

        [Tooltip("Maximum seconds between idle dialogue plays.")]
        public float idleIntervalMax = 60f;

        // ── Private State ─────────────────────────────────────────────────────

        private int   _lastIdleIndex = -1;
        private bool  _isInteracting;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            ApplyDataToLabels();
            ApplyUniformColour();

            if (data != null && data.idleDialogue.Count > 0)
                StartCoroutine(IdleDialogueLoop());
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a random idle dialogue line that was not the most recently played.
        /// Returns null if this character has no idle dialogue defined.
        /// </summary>
        public DialogueLine GetRandomIdleLine()
        {
            if (data == null || data.idleDialogue.Count == 0) return null;

            List<DialogueLine> lines = data.idleDialogue;
            if (lines.Count == 1) return lines[0];

            int idx;
            do { idx = Random.Range(0, lines.Count); }
            while (idx == _lastIdleIndex);
            _lastIdleIndex = idx;
            return lines[idx];
        }

        /// <summary>
        /// Mark this character as currently in an interaction.
        /// Prevents idle lines from firing during dialogue.
        /// </summary>
        public void SetInteracting(bool interacting)
        {
            _isInteracting = interacting;
        }

        /// <summary>Returns true if the player is currently engaged with this character.</summary>
        public bool IsInteracting => _isInteracting;

        // ── Private Helpers ───────────────────────────────────────────────────

        private void ApplyDataToLabels()
        {
            if (data == null) return;

            if (nameLabel != null)
            {
                nameLabel.text  = data.characterName.ToUpper();
                nameLabel.color = data.uniformColor;
            }

            if (rankLabel != null)
            {
                rankLabel.text  = $"{data.rank}\n{data.role}".ToUpper();
                rankLabel.color = Color.Lerp(data.uniformColor, Color.white, 0.4f);
            }
        }

        private void ApplyUniformColour()
        {
            if (data == null) return;

            // Apply uniform colour to the character's main renderer (capsule placeholder)
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                // Clone the material so shared materials aren't affected
                rend.material = new Material(rend.sharedMaterial);
                rend.material.color = data.uniformColor;
            }
        }

        /// <summary>
        /// Coroutine: at random intervals fires an idle dialogue line via InteractionManager
        /// if the player is not already in a conversation.
        /// </summary>
        private IEnumerator IdleDialogueLoop()
        {
            while (true)
            {
                float wait = Random.Range(idleIntervalMin, idleIntervalMax);
                yield return new WaitForSeconds(wait);

                if (!_isInteracting && InteractionManager.Instance != null && !InteractionManager.Instance.IsInteracting)
                {
                    DialogueLine line = GetRandomIdleLine();
                    if (line != null)
                        InteractionManager.Instance.ShowPassiveLine(line, this);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (data == null) return;
            Gizmos.color = data.uniformColor;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.1f, 0.35f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.6f,
                data.characterName);
        }
#endif
    }
}
