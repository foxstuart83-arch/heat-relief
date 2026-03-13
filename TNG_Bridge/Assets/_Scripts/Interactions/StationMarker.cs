using UnityEngine;

namespace TNG.Interactions
{
    /// <summary>
    /// Marks a point on the bridge as a player-navigable station.
    /// Attach this component to any GameObject on the Interactable layer
    /// that the player should be able to tap and walk to.
    ///
    /// The PlayerController performs a raycast against the Interactable layer
    /// and checks for this component on the hit object to determine valid destinations.
    /// </summary>
    public class StationMarker : MonoBehaviour
    {
        // ── Inspector Properties ─────────────────────────────────────────────

        [Header("Station Identity")]
        [Tooltip("Human-readable name displayed in debug and tooltips (e.g. 'Helm Console').")]
        public string stationName = "Unnamed Station";

        [Tooltip("Where the player's [Player] GameObject moves to when this station is selected. " +
                 "If null, the player moves to this GameObject's position.")]
        public Transform playerStandTarget;

        [Tooltip("Direction the player camera should face after arriving. " +
                 "If null, player faces the station object itself.")]
        public Transform facingTarget;

        [Header("Highlight")]
        [Tooltip("The subtle highlight ring shown on the floor when the player's tap ray hovers over this station. " +
                 "Assign a child ring mesh here. Hidden by default; shown via OnHighlight().")]
        public GameObject highlightRing;

        [Header("Accessibility")]
        [Tooltip("If false, the player cannot navigate to this station (used to lock stations during missions).")]
        public bool isAccessible = true;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure the highlight ring starts hidden
            if (highlightRing != null)
                highlightRing.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the world position where the player should stand when at this station.
        /// Falls back to this object's position if no explicit target is set.
        /// </summary>
        public Vector3 GetStandPosition()
        {
            return playerStandTarget != null ? playerStandTarget.position : transform.position;
        }

        /// <summary>
        /// Returns the world position the player camera should face after arrival.
        /// Falls back to this object's position if no explicit target is set.
        /// </summary>
        public Vector3 GetFacingPosition()
        {
            return facingTarget != null ? facingTarget.position : transform.position;
        }

        /// <summary>
        /// Activates the floor highlight ring to indicate a valid tap target.
        /// Called by PlayerController during hover/tap detection.
        /// </summary>
        public void OnHighlight()
        {
            if (highlightRing != null && isAccessible)
                highlightRing.SetActive(true);
        }

        /// <summary>
        /// Deactivates the floor highlight ring.
        /// </summary>
        public void OnUnhighlight()
        {
            if (highlightRing != null)
                highlightRing.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw a cyan sphere at the stand target so it's visible in the editor
            Gizmos.color = isAccessible ? Color.cyan : Color.gray;
            Vector3 pos = playerStandTarget != null ? playerStandTarget.position : transform.position;
            Gizmos.DrawWireSphere(pos, 0.2f);

            // Draw a line from station to stand position
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawLine(transform.position, pos);

            // Label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f,
                $"[STATION] {stationName}");
        }
#endif
    }
}
