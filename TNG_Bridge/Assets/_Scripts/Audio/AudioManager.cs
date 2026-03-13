using System.Collections;
using UnityEngine;

namespace TNG.Audio
{
    /// <summary>
    /// Audio state enum used by AudioManager and MissionData beats.
    /// </summary>
    public enum AudioState
    {
        Normal,
        YellowAlert,
        RedAlert
    }

    /// <summary>
    /// Singleton AudioManager. Persists across scenes.
    /// Manages layered audio: ambient hum, console beeps, alert klaxon, dialogue chime.
    ///
    /// PLACEHOLDER NOTES: All AudioClips are referenced by filename.
    /// Source audio files and assign them in the Inspector.
    /// Required files are documented in Assets/README_AUDIO.md
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── Inspector References ─────────────────────────────────────────────

        [Header("Audio Clips — assign in Inspector (see README_AUDIO.md)")]
        [Tooltip("Continuous low hum of the Enterprise bridge. File: bridge_ambient.wav")]
        public AudioClip bridgeAmbientClip;

        [Tooltip("Short LCARS beep sound. File: console_beep.wav")]
        public AudioClip consolBeepClip;

        [Tooltip("Red Alert klaxon loop. File: red_alert_klaxon.wav")]
        public AudioClip redAlertKlaxonClip;

        [Tooltip("Yellow Alert tone. File: yellow_alert_tone.wav")]
        public AudioClip yellowAlertToneClip;

        [Tooltip("Short chime when dialogue opens. File: dialogue_chime.wav")]
        public AudioClip dialogueChimeClip;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float ambientVolume    = 0.30f;
        [Range(0f, 1f)] public float consoleBeopVolume = 0.15f;
        [Range(0f, 1f)] public float klaxonVolume      = 0.60f;
        [Range(0f, 1f)] public float chimeVolume        = 0.50f;

        [Header("Console Beep Timing")]
        [Tooltip("Minimum seconds between random console beeps.")]
        public float beepIntervalMin = 8f;
        [Tooltip("Maximum seconds between random console beeps.")]
        public float beepIntervalMax = 15f;

        // ── Private Audio Sources ────────────────────────────────────────────

        private AudioSource _ambientSource;
        private AudioSource _klaxonSource;
        private AudioSource _oneShotSource;

        private AudioState _currentState = AudioState.Normal;
        private Coroutine  _beepCoroutine;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitAudioSources();
        }

        private void Start()
        {
            StartAmbient();
            _beepCoroutine = StartCoroutine(ConsoleBeepLoop());
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Transitions the bridge to the specified audio state.
        /// Starts or stops klaxon, adjusts ambient accordingly.
        /// </summary>
        public void SetAudioState(AudioState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;

            switch (newState)
            {
                case AudioState.Normal:
                    StopKlaxon();
                    SetAmbientVolume(ambientVolume);
                    break;

                case AudioState.YellowAlert:
                    StopKlaxon();
                    PlayOneShot(yellowAlertToneClip, klaxonVolume);
                    SetAmbientVolume(ambientVolume * 0.6f);
                    break;

                case AudioState.RedAlert:
                    StartKlaxon();
                    SetAmbientVolume(ambientVolume * 0.4f);
                    break;
            }
        }

        /// <summary>
        /// Plays the short dialogue open chime. Call when the dialogue panel opens.
        /// </summary>
        public void PlayDialogueChime()
        {
            PlayOneShot(dialogueChimeClip, chimeVolume);
        }

        /// <summary>
        /// Returns the current audio/alert state.
        /// </summary>
        public AudioState CurrentState => _currentState;

        // ── Private Helpers ──────────────────────────────────────────────────

        private void InitAudioSources()
        {
            _ambientSource = CreateSource("Ambient", true,  ambientVolume);
            _klaxonSource  = CreateSource("Klaxon",  true,  klaxonVolume);
            _oneShotSource = CreateSource("OneShot",  false, 1f);
        }

        private AudioSource CreateSource(string label, bool loop, float vol)
        {
            var go  = new GameObject($"AudioSource_{label}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop         = loop;
            src.volume       = vol;
            src.playOnAwake  = false;
            src.spatialBlend = 0f; // 2D — bridge audio is non-positional for ambience
            return src;
        }

        private void StartAmbient()
        {
            if (bridgeAmbientClip == null)
            {
                Debug.LogWarning("[AudioManager] bridgeAmbientClip not assigned. " +
                                 "See README_AUDIO.md for required files.");
                return;
            }
            _ambientSource.clip = bridgeAmbientClip;
            _ambientSource.Play();
        }

        private void StartKlaxon()
        {
            if (redAlertKlaxonClip == null)
            {
                Debug.LogWarning("[AudioManager] redAlertKlaxonClip not assigned.");
                return;
            }
            _klaxonSource.clip = redAlertKlaxonClip;
            _klaxonSource.Play();
        }

        private void StopKlaxon()
        {
            _klaxonSource.Stop();
        }

        private void SetAmbientVolume(float vol)
        {
            _ambientSource.volume = vol;
        }

        private void PlayOneShot(AudioClip clip, float vol)
        {
            if (clip == null) return;
            _oneShotSource.PlayOneShot(clip, vol);
        }

        /// <summary>
        /// Coroutine: plays a random console beep at a random interval.
        /// When 3D positional audio is added, pass a position from a random console transform.
        /// </summary>
        private IEnumerator ConsoleBeepLoop()
        {
            while (true)
            {
                float waitTime = Random.Range(beepIntervalMin, beepIntervalMax);
                yield return new WaitForSeconds(waitTime);

                if (_currentState != AudioState.RedAlert)
                    PlayOneShot(consolBeepClip, consoleBeopVolume);
            }
        }
    }
}
