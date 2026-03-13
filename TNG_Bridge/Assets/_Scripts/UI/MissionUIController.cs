using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TNG.Missions;

namespace TNG.UI
{
    /// <summary>
    /// Controls the mission panel UI — overlays the dialogue panel area during
    /// active mission beats.
    ///
    /// Features:
    ///   - Top bar: mission title and stardate
    ///   - Narrative body: typewriter effect at 30 characters/second
    ///   - Choice buttons: LCARS style, fade in after typewriter completes
    ///   - Mission complete screen: shown at end of all mission paths
    ///
    /// Singleton. Attach to a persistent [UIManager] or standalone [MissionUI] GameObject.
    /// </summary>
    public class MissionUIController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static MissionUIController Instance { get; private set; }

        // ── Inspector References — Mission Panel ──────────────────────────────

        [Header("Mission Panel Root")]
        [Tooltip("Root CanvasGroup of the mission panel — fade in/out.")]
        public CanvasGroup missionPanelGroup;

        [Header("Mission Header")]
        [Tooltip("Mission title label (top bar left).")]
        public TextMeshProUGUI missionTitleText;

        [Tooltip("Stardate label (top bar right). Driven by MissionData.stardate.")]
        public TextMeshProUGUI missionStardateText;

        [Header("Narrative")]
        [Tooltip("Main body text where narrative is typed out.")]
        public TextMeshProUGUI narrativeText;

        [Tooltip("Characters per second for the typewriter effect.")]
        public float typewriterSpeed = 30f;

        [Header("Choice Buttons")]
        [Tooltip("Array of 3 LCARS choice buttons (hidden until narrative completes).")]
        public Button[] choiceButtons = new Button[3];

        [Tooltip("Text labels for each choice button.")]
        public TextMeshProUGUI[] choiceButtonTexts = new TextMeshProUGUI[3];

        [Tooltip("CanvasGroup on the choice button container — used for fade-in.")]
        public CanvasGroup choiceButtonGroup;

        [Tooltip("Seconds to fade in the choice buttons after typewriter finishes.")]
        public float choiceFadeDuration = 0.5f;

        [Header("Mission Complete Screen")]
        [Tooltip("Root CanvasGroup of the mission complete overlay.")]
        public CanvasGroup missionCompleteGroup;

        [Tooltip("'MISSION COMPLETE' or 'MISSION FAILED' heading.")]
        public TextMeshProUGUI missionCompleteHeading;

        [Tooltip("Outcome summary text.")]
        public TextMeshProUGUI missionOutcomeText;

        [Tooltip("'Return to Bridge' button on the complete screen.")]
        public Button returnToBridgeButton;

        // ── Private State ─────────────────────────────────────────────────────

        private Coroutine _typewriterCoroutine;
        private Coroutine _fadeCoroutine;
        private MissionBeat _currentBeat;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            HideMissionPanel(instant: true);
            HideMissionComplete(instant: true);

            if (returnToBridgeButton != null)
                returnToBridgeButton.onClick.AddListener(OnReturnToBridge);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Displays the mission panel with the given beat's content.
        /// Starts the typewriter effect, hides choice buttons until it completes.
        /// </summary>
        public void ShowBeat(MissionBeat beat, string missionTitle, string stardate)
        {
            _currentBeat = beat;

            // Header
            if (missionTitleText != null)
                missionTitleText.text = missionTitle.ToUpper();
            if (missionStardateText != null)
                missionStardateText.text = $"STARDATE {stardate}";

            // Hide choices while narrative plays
            if (choiceButtonGroup != null)
            {
                choiceButtonGroup.alpha          = 0f;
                choiceButtonGroup.interactable   = false;
                choiceButtonGroup.blocksRaycasts = false;
            }

            // Show panel
            ShowMissionPanel();

            // Start typewriter
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterNarrative(beat.narrativeText));
        }

        /// <summary>
        /// Displays the mission complete screen.
        /// Called by MissionManager when the player reaches a terminal beat.
        /// </summary>
        public void ShowMissionComplete(string outcomeText, bool success)
        {
            HideMissionPanel(instant: false);
            StartCoroutine(ShowMissionCompleteCoroutine(outcomeText, success));
        }

        /// <summary>Hides the mission panel immediately (used on scene load).</summary>
        public void HideMissionPanel(bool instant)
        {
            if (missionPanelGroup == null) return;
            if (instant)
            {
                missionPanelGroup.alpha          = 0f;
                missionPanelGroup.interactable   = false;
                missionPanelGroup.blocksRaycasts = false;
            }
            else
            {
                StartCoroutine(FadeCanvasGroup(missionPanelGroup, 0f, panelAnimDuration));
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        [Header("Panel Animation")]
        public float panelAnimDuration = 0.4f;

        private void ShowMissionPanel()
        {
            if (missionPanelGroup == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(missionPanelGroup, 1f, panelAnimDuration));
        }

        private void HideMissionComplete(bool instant)
        {
            if (missionCompleteGroup == null) return;
            missionCompleteGroup.alpha          = instant ? 0f : missionCompleteGroup.alpha;
            missionCompleteGroup.interactable   = false;
            missionCompleteGroup.blocksRaycasts = false;
            if (!instant) StartCoroutine(FadeCanvasGroup(missionCompleteGroup, 0f, panelAnimDuration));
        }

        /// <summary>
        /// Coroutine: reveals narrative text character by character.
        /// After completion, fades in the choice buttons.
        /// </summary>
        private IEnumerator TypewriterNarrative(string fullText)
        {
            if (narrativeText == null) yield break;

            narrativeText.text = "";
            float delay = 1f / typewriterSpeed;

            foreach (char c in fullText)
            {
                narrativeText.text += c;
                yield return new WaitForSeconds(delay);
            }

            // Narrative complete — fade in choices
            yield return StartCoroutine(PopulateAndShowChoices());
        }

        private IEnumerator PopulateAndShowChoices()
        {
            if (_currentBeat == null || _currentBeat.choices == null) yield break;

            // Wire up buttons
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;

                if (i < _currentBeat.choices.Count)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    if (choiceButtonTexts[i] != null)
                        choiceButtonTexts[i].text = _currentBeat.choices[i].choiceText;

                    int capturedIndex = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => MissionManager.Instance?.SelectChoice(capturedIndex));
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }

            // Fade the button group in
            if (choiceButtonGroup != null)
            {
                choiceButtonGroup.interactable   = true;
                choiceButtonGroup.blocksRaycasts = true;
                yield return StartCoroutine(FadeCanvasGroup(choiceButtonGroup, 1f, choiceFadeDuration));
            }
        }

        private IEnumerator ShowMissionCompleteCoroutine(string outcomeText, bool success)
        {
            yield return new WaitForSeconds(0.5f);

            if (missionCompleteHeading != null)
            {
                missionCompleteHeading.text  = success ? "MISSION COMPLETE" : "MISSION FAILED";
                missionCompleteHeading.color = success ? LCARSColors.StatusGreen : LCARSColors.StatusRed;
            }

            if (missionOutcomeText != null)
                missionOutcomeText.text = outcomeText;

            if (missionCompleteGroup != null)
            {
                missionCompleteGroup.interactable   = true;
                missionCompleteGroup.blocksRaycasts = true;
                yield return StartCoroutine(FadeCanvasGroup(missionCompleteGroup, 1f, panelAnimDuration));
            }
        }

        private void OnReturnToBridge()
        {
            HideMissionComplete(instant: false);
            MissionManager.Instance?.EndMission();
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
        {
            float start   = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, targetAlpha, elapsed / duration);
                yield return null;
            }

            group.alpha = targetAlpha;
        }
    }
}
