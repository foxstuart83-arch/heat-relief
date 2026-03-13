using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TNG.Audio;

namespace TNG.UI
{
    /// <summary>
    /// Manages the visual Red/Yellow Alert state on the bridge.
    /// Drives the full-screen tint overlay and coordinates with AudioManager.
    ///
    /// Attach to the persistent [UIManager] or a dedicated [AlertManager] GameObject.
    /// Requires a child Canvas with the alert overlay Image assigned.
    /// </summary>
    public class AlertManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static AlertManager Instance { get; private set; }

        // ── Inspector References ─────────────────────────────────────────────

        [Header("Alert Overlay")]
        [Tooltip("Full-screen Image used as the alert tint. Normally hidden.")]
        public Image alertOverlayImage;

        [Tooltip("TextMeshPro text for the RED ALERT / YELLOW ALERT label.")]
        public TMPro.TextMeshProUGUI alertLabel;

        [Header("Red Alert Settings")]
        [Tooltip("Base tint colour for Red Alert overlay.")]
        public Color redAlertColor = new Color(1f, 0f, 0f, 0.15f);

        [Tooltip("Flash interval in seconds for Red Alert.")]
        public float redAlertFlashInterval = 1.5f;

        [Header("Yellow Alert Settings")]
        public Color yellowAlertColor = new Color(1f, 0.8f, 0f, 0.10f);

        // ── Private State ────────────────────────────────────────────────────

        private AudioState  _currentAlertState = AudioState.Normal;
        private Coroutine   _flashCoroutine;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            HideAlert();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Sets the current alert level. Activates or deactivates the visual overlay.
        /// Also notifies AudioManager to update the audio state.
        /// </summary>
        public void SetAlertState(AudioState state)
        {
            if (_currentAlertState == state) return;
            _currentAlertState = state;

            // Stop any existing flash coroutine
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            AudioManager.Instance?.SetAudioState(state);

            switch (state)
            {
                case AudioState.Normal:
                    HideAlert();
                    break;

                case AudioState.YellowAlert:
                    ShowAlert(yellowAlertColor, "YELLOW ALERT", LCARSColors.YellowAlert);
                    _flashCoroutine = StartCoroutine(FlashOverlay(yellowAlertColor, redAlertFlashInterval * 1.5f));
                    break;

                case AudioState.RedAlert:
                    ShowAlert(redAlertColor, "RED ALERT", LCARSColors.RedAlert);
                    _flashCoroutine = StartCoroutine(FlashOverlay(redAlertColor, redAlertFlashInterval));
                    break;
            }
        }

        /// <summary>Returns the current alert level.</summary>
        public AudioState CurrentAlertState => _currentAlertState;

        // ── Private Helpers ──────────────────────────────────────────────────

        private void ShowAlert(Color overlayColor, string labelText, Color labelColor)
        {
            if (alertOverlayImage != null)
            {
                alertOverlayImage.gameObject.SetActive(true);
                alertOverlayImage.color = overlayColor;
            }

            if (alertLabel != null)
            {
                alertLabel.gameObject.SetActive(true);
                alertLabel.text  = labelText;
                alertLabel.color = labelColor;
            }
        }

        private void HideAlert()
        {
            if (alertOverlayImage != null)
                alertOverlayImage.gameObject.SetActive(false);

            if (alertLabel != null)
                alertLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Coroutine: pulses the overlay alpha between 0 and the target color's alpha
        /// at the specified interval to simulate the bridge lights flashing.
        /// </summary>
        private IEnumerator FlashOverlay(Color baseColor, float interval)
        {
            bool visible = true;
            while (true)
            {
                yield return new WaitForSeconds(interval * 0.5f);
                visible = !visible;

                if (alertOverlayImage != null)
                {
                    Color c = baseColor;
                    c.a = visible ? baseColor.a : 0f;
                    alertOverlayImage.color = c;
                }

                if (alertLabel != null)
                    alertLabel.gameObject.SetActive(visible);
            }
        }
    }
}
