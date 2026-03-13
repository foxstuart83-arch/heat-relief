using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TNG.Characters;
using TNG.Interactions;

namespace TNG.UI
{
    /// <summary>
    /// Singleton UI orchestrator. Manages all persistent HUD elements:
    ///   - Ship status bar (top)
    ///   - Dialogue panel (slides up from bottom on interaction)
    ///   - Passive line notification (small, non-blocking)
    ///
    /// The Canvas is set to Screen Space – Overlay and persists across scenes.
    ///
    /// Assign all references in the Inspector. This script does not create
    /// UI objects — use the BridgeSceneSetup editor tool to build the Canvas.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── Inspector References — Ship Status Bar ────────────────────────────

        [Header("Ship Status Bar")]
        [Tooltip("TextMeshPro for the ship name (left side of status bar).")]
        public TextMeshProUGUI shipNameText;

        [Tooltip("StardateDisplay component on the stardate label (centre).")]
        public StardateDisplay stardateDisplay;

        [Tooltip("TextMeshPro for the warp core status indicator (right side).")]
        public TextMeshProUGUI warpCoreStatusText;

        // ── Inspector References — Dialogue Panel ─────────────────────────────

        [Header("Dialogue Panel")]
        [Tooltip("Root RectTransform of the dialogue panel — slides up from below screen.")]
        public RectTransform dialoguePanel;

        [Tooltip("Portrait Image on the left column of the dialogue panel.")]
        public Image characterPortraitImage;

        [Tooltip("Character name label in the dialogue panel.")]
        public TextMeshProUGUI characterNameText;

        [Tooltip("Rank and role shown below the character name.")]
        public TextMeshProUGUI characterRankText;

        [Tooltip("Main dialogue body text.")]
        public TextMeshProUGUI dialogueBodyText;

        [Tooltip("The three LCARS option buttons. Array of exactly 3.")]
        public Button[] optionButtons = new Button[3];

        [Tooltip("Text labels inside each option button.")]
        public TextMeshProUGUI[] optionButtonTexts = new TextMeshProUGUI[3];

        [Tooltip("Close/dismiss button shown when there are no options.")]
        public Button closeButton;

        // ── Inspector References — Passive Line ───────────────────────────────

        [Header("Passive Line Notification")]
        [Tooltip("Small non-blocking notification shown for ambient crew lines.")]
        public RectTransform passiveLinePanel;

        [Tooltip("Text in the passive line notification.")]
        public TextMeshProUGUI passiveLineText;

        // ── Panel Animation ───────────────────────────────────────────────────

        [Header("Panel Animation")]
        [Tooltip("Height of the dialogue panel in pixels — used for slide animation.")]
        public float dialoguePanelHeight = 700f;

        [Tooltip("Duration of the slide-up and slide-down animations.")]
        public float panelAnimDuration = 0.3f;

        // ── Private State ─────────────────────────────────────────────────────

        private DialogueLine   _displayedLine;
        private CharacterData  _displayedCharacter;
        private Coroutine      _slideCoroutine;
        private Coroutine      _passiveCoroutine;
        private bool           _panelOpen;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            InitStatusBar();
            SetPanelPosition(hidden: true);

            if (passiveLinePanel != null)
                passiveLinePanel.gameObject.SetActive(false);
        }

        // ── Public API — Dialogue Panel ───────────────────────────────────────

        /// <summary>
        /// Opens the dialogue panel with the given line and character data.
        /// Populates portrait, name, dialogue text, and option buttons.
        /// </summary>
        public void ShowDialoguePanel(DialogueLine line, CharacterData character)
        {
            if (line == null) return;

            _displayedLine      = line;
            _displayedCharacter = character;

            // Character portrait and identity
            if (character != null)
            {
                if (characterPortraitImage != null)
                {
                    characterPortraitImage.sprite  = character.portrait;
                    characterPortraitImage.color   = character.portrait != null
                        ? Color.white : character.uniformColor;
                }
                if (characterNameText != null)
                    characterNameText.text = character.characterName.ToUpper();
                if (characterRankText != null)
                    characterRankText.text = $"{character.rank}  ·  {character.role}".ToUpper();
            }

            // Dialogue body
            if (dialogueBodyText != null)
                dialogueBodyText.text = line.dialogueText;

            // Option buttons
            ConfigureOptionButtons(line);

            // Slide open
            if (!_panelOpen)
                SlidePanel(open: true);
        }

        /// <summary>
        /// Hides the dialogue panel with a slide-down animation.
        /// </summary>
        public void HideDialoguePanel()
        {
            if (_panelOpen)
                SlidePanel(open: false);
        }

        /// <summary>
        /// Shows a brief non-blocking ambient line from a crew member.
        /// Disappears automatically after 4 seconds.
        /// </summary>
        public void ShowPassiveLine(DialogueLine line, CharacterData character)
        {
            if (passiveLinePanel == null || passiveLineText == null) return;

            string speaker = character != null ? character.characterName : line.speakerName;
            passiveLineText.text = $"<b>{speaker.ToUpper()}</b>  \"{line.dialogueText}\"";

            if (_passiveCoroutine != null) StopCoroutine(_passiveCoroutine);
            _passiveCoroutine = StartCoroutine(ShowPassiveLineCoroutine());
        }

        // ── Public API — Status Bar ───────────────────────────────────────────

        /// <summary>Updates the warp core status indicator text and colour.</summary>
        public void SetWarpCoreStatus(string statusText, bool nominal = true)
        {
            if (warpCoreStatusText == null) return;
            warpCoreStatusText.text  = $"WARP CORE  {statusText}";
            warpCoreStatusText.color = nominal ? LCARSColors.StatusGreen : LCARSColors.StatusRed;
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void InitStatusBar()
        {
            if (shipNameText != null)
                shipNameText.text = "USS ENTERPRISE  NCC-1701-D";

            SetWarpCoreStatus("NOMINAL", nominal: true);
        }

        private void ConfigureOptionButtons(DialogueLine line)
        {
            bool hasOptions = line.options != null && line.options.Count > 0;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;

                if (hasOptions && i < line.options.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionButtonTexts[i] != null)
                        optionButtonTexts[i].text = line.options[i].optionText;

                    // Capture index for closure
                    int capturedIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(capturedIndex));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            // Show close button when there are no choices (response lines)
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(!hasOptions);
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => InteractionManager.Instance?.CloseDialogue());
            }
        }

        private void OnOptionSelected(int index)
        {
            if (_displayedLine == null) return;
            if (index >= _displayedLine.options.Count) return;

            DialogueOption opt = _displayedLine.options[index];
            InteractionManager.Instance?.SelectOption(opt);
        }

        private void SetPanelPosition(bool hidden)
        {
            if (dialoguePanel == null) return;
            Vector2 pos = dialoguePanel.anchoredPosition;
            pos.y = hidden ? -dialoguePanelHeight : 0f;
            dialoguePanel.anchoredPosition = pos;
        }

        private void SlidePanel(bool open)
        {
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlidePanelCoroutine(open));
        }

        private IEnumerator SlidePanelCoroutine(bool open)
        {
            _panelOpen = open;
            dialoguePanel.gameObject.SetActive(true);

            Vector2 startPos = dialoguePanel.anchoredPosition;
            Vector2 endPos   = startPos;
            endPos.y = open ? 0f : -dialoguePanelHeight;

            float elapsed = 0f;
            while (elapsed < panelAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / panelAnimDuration);
                dialoguePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            dialoguePanel.anchoredPosition = endPos;

            if (!open)
                dialoguePanel.gameObject.SetActive(false);
        }

        private IEnumerator ShowPassiveLineCoroutine()
        {
            passiveLinePanel.gameObject.SetActive(true);
            yield return new WaitForSeconds(4f);
            passiveLinePanel.gameObject.SetActive(false);
        }
    }
}
