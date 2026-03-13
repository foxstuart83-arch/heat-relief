using System.Collections;
using UnityEngine;

namespace TNG.Viewscreen
{
    /// <summary>
    /// Viewscreen display states available to missions and interactions.
    /// </summary>
    public enum ViewscreenState
    {
        Space,
        Planet,
        Vessel,
        Communication,
        Alert
    }

    /// <summary>
    /// Controls the large forward viewscreen on the bridge.
    ///
    /// The viewscreen is a Plane/Quad mesh using a RenderTexture as its material.
    /// Each state swaps the displayed content.
    ///
    /// PLACEHOLDER: The scrolling star material uses UV offset animation.
    /// Replace materials with proper art assets when available.
    /// Attach this to the [Viewscreen] GameObject in the bridge scene.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ViewscreenController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static ViewscreenController Instance { get; private set; }

        // ── Inspector References ─────────────────────────────────────────────

        [Header("State Materials — assign in Inspector")]
        [Tooltip("Scrolling star field material. Applied in SPACE and ALERT states.")]
        public Material spaceMaterial;

        [Tooltip("Planet surface or orbital view material.")]
        public Material planetMaterial;

        [Tooltip("Enemy/ally vessel silhouette material.")]
        public Material vesselMaterial;

        [Tooltip("Communication channel split-screen material.")]
        public Material communicationMaterial;

        [Header("Star Field Scroll")]
        [Tooltip("UV scroll speed of the star field material.")]
        public float starScrollSpeed = 0.02f;

        [Header("Alert Tint")]
        [Tooltip("Red tint overlay object child of the viewscreen — enable on Alert state.")]
        public GameObject alertTintOverlay;

        // ── Private State ────────────────────────────────────────────────────

        private ViewscreenState _currentState = ViewscreenState.Space;
        private Renderer        _renderer;
        private Material        _activeMaterial;
        private Coroutine       _scrollCoroutine;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            _renderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            SetViewscreenState(ViewscreenState.Space);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Transitions the viewscreen to the specified state.
        /// Swaps the material and starts or stops any animation coroutines.
        /// </summary>
        public void SetViewscreenState(ViewscreenState state)
        {
            _currentState = state;

            // Stop any existing scroll animation
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
            }

            // Handle alert tint overlay
            if (alertTintOverlay != null)
                alertTintOverlay.SetActive(state == ViewscreenState.Alert);

            switch (state)
            {
                case ViewscreenState.Space:
                    ApplyMaterial(spaceMaterial);
                    _scrollCoroutine = StartCoroutine(ScrollStarField());
                    break;

                case ViewscreenState.Planet:
                    ApplyMaterial(planetMaterial);
                    break;

                case ViewscreenState.Vessel:
                    ApplyMaterial(vesselMaterial);
                    break;

                case ViewscreenState.Communication:
                    ApplyMaterial(communicationMaterial);
                    break;

                case ViewscreenState.Alert:
                    ApplyMaterial(spaceMaterial);
                    _scrollCoroutine = StartCoroutine(ScrollStarField());
                    break;
            }
        }

        /// <summary>Returns the current viewscreen display state.</summary>
        public ViewscreenState CurrentState => _currentState;

        // ── Private Helpers ──────────────────────────────────────────────────

        private void ApplyMaterial(Material mat)
        {
            if (mat == null)
            {
                // PLACEHOLDER: create a solid-colour stand-in so the scene doesn't
                // throw errors before real materials are assigned.
                Debug.LogWarning($"[ViewscreenController] Material for state {_currentState} " +
                                  "is not assigned. Using placeholder colour.");
                _renderer.material = CreatePlaceholderMaterial(_currentState);
                return;
            }
            _renderer.material = mat;
            _activeMaterial = mat;
        }

        private Material CreatePlaceholderMaterial(ViewscreenState state)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = state switch
            {
                ViewscreenState.Space         => new Color(0.02f, 0.02f, 0.08f),
                ViewscreenState.Planet        => new Color(0.1f,  0.4f,  0.8f),
                ViewscreenState.Vessel        => new Color(0.1f,  0.1f,  0.15f),
                ViewscreenState.Communication => new Color(0.0f,  0.05f, 0.1f),
                ViewscreenState.Alert         => new Color(0.15f, 0.0f,  0.0f),
                _                             => Color.black
            };
            return mat;
        }

        /// <summary>
        /// Coroutine: continuously scrolls the star field material UV offset
        /// to simulate the Enterprise moving through space.
        /// </summary>
        private IEnumerator ScrollStarField()
        {
            float offset = 0f;
            while (true)
            {
                offset += starScrollSpeed * Time.deltaTime;
                if (_activeMaterial != null)
                    _activeMaterial.SetTextureOffset("_MainTex", new Vector2(0f, offset));
                yield return null;
            }
        }
    }
}
