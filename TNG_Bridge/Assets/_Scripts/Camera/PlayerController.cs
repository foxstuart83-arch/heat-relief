using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TNG.Interactions;
using TNG.Characters;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace TNG.Camera
{
    /// <summary>
    /// Controls the player's presence on the Enterprise bridge.
    ///
    /// Behaviour:
    ///   Single-finger swipe  → Look around (horizontal 360°, vertical ±30°/-20°)
    ///   Single tap           → Move to tapped station or interact with crew
    ///   Double-tap           → Return to bridge centre
    ///
    /// The [Player] GameObject acts as the horizontal pivot.
    /// A child [CameraRig] handles vertical tilt.
    /// Main Camera lives under [CameraRig].
    ///
    /// Attach this script to the [Player] GameObject in the Bridge_Main scene.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector Properties ─────────────────────────────────────────────

        [Header("Look Settings")]
        [Tooltip("Degrees turned per pixel of swipe. Expose for tuning per device.")]
        public float lookSensitivity = 0.2f;

        [Tooltip("Smoothing applied to camera rotation (higher = snappier).")]
        public float smoothSpeed = 8f;

        [Tooltip("Lowest vertical angle the player can tilt the camera (degrees below horizon).")]
        public float minVerticalAngle = -20f;

        [Tooltip("Highest vertical angle the player can tilt the camera (degrees above horizon).")]
        public float maxVerticalAngle = 30f;

        [Header("Movement")]
        [Tooltip("Time in seconds to move from current position to a station.")]
        public float stationMoveTime = 0.8f;

        [Tooltip("Time in seconds to return to the bridge centre position.")]
        public float returnToCentreTime = 1.0f;

        [Header("Layer Masks")]
        [Tooltip("Layer used for all interactable objects (stations, crew). Set this to layer 'Interactable'.")]
        public LayerMask interactableLayer;

        [Header("References")]
        [Tooltip("Child GameObject that holds the vertical camera tilt. " +
                 "Must be a direct child of [Player].")]
        public Transform cameraRig;

        // ── Touch Detection Thresholds ────────────────────────────────────────

        private const float SwipePixelThreshold = 20f;  // min pixels to classify as a swipe
        private const float TapDurationMax      = 0.25f; // max seconds to classify as a tap
        private const float DoubleTapWindowSec  = 0.30f; // window for second tap
        private const float DoubleTapPixelDist  = 60f;   // max pixel distance for double-tap

        // ── Private State ─────────────────────────────────────────────────────

        private Vector3  _centrePosition;      // bridge centre — used for double-tap return
        private float    _targetYaw;           // desired horizontal rotation
        private float    _targetPitch;         // desired vertical rotation
        private bool     _isMoving;            // true while travelling to a station
        private bool     _inputLocked;         // true during movement or interaction

        // Touch tracking
        private Vector2  _touchStartPos;
        private float    _touchStartTime;
        private Vector2  _lastTapPos;
        private float    _lastTapTime    = -999f;
        private bool     _touchIsSwipe;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _centrePosition = transform.position;
            _targetYaw      = transform.eulerAngles.y;

            if (cameraRig == null)
                Debug.LogError("[PlayerController] CameraRig is not assigned. " +
                               "Drag the [CameraRig] child into the Inspector.");
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            HandleTouchInput();
            ApplySmoothRotation();

#if UNITY_EDITOR
            HandleEditorMouseInput();
#endif
        }

        // ── Input Handling ────────────────────────────────────────────────────

        private void HandleTouchInput()
        {
            var touches = Touch.activeTouches;
            if (touches.Count == 0) return;

            // Use only the first touch for look/move; ignore additional fingers
            var touch = touches[0];

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    OnTouchBegan(touch.screenPosition);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    OnTouchMoved(touch.delta);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    OnTouchEnded(touch.screenPosition);
                    break;
            }
        }

        private void OnTouchBegan(Vector2 screenPos)
        {
            _touchStartPos  = screenPos;
            _touchStartTime = Time.time;
            _touchIsSwipe   = false;
        }

        private void OnTouchMoved(Vector2 delta)
        {
            if (_inputLocked) return;

            float distMoved = Vector2.Distance(
                Touchscreen.current.primaryTouch.position.ReadValue(),
                _touchStartPos);

            if (distMoved > SwipePixelThreshold)
                _touchIsSwipe = true;

            if (_touchIsSwipe)
            {
                _targetYaw   += delta.x * lookSensitivity;
                _targetPitch -= delta.y * lookSensitivity;
                _targetPitch  = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
            }
        }

        private void OnTouchEnded(Vector2 screenPos)
        {
            float duration  = Time.time - _touchStartTime;
            float distance  = Vector2.Distance(screenPos, _touchStartPos);

            bool isTap = !_touchIsSwipe
                      && duration  <= TapDurationMax
                      && distance  <= SwipePixelThreshold;

            if (!isTap) return;

            // Double-tap detection
            float timeSinceLast = Time.time - _lastTapTime;
            float distFromLast  = Vector2.Distance(screenPos, _lastTapPos);

            if (timeSinceLast <= DoubleTapWindowSec && distFromLast <= DoubleTapPixelDist)
            {
                // Double tap — return to centre
                _lastTapTime = -999f;
                HandleDoubleTap();
            }
            else
            {
                // Potential first tap of a double — also fire single tap after delay
                _lastTapTime = Time.time;
                _lastTapPos  = screenPos;
                StartCoroutine(DeferredSingleTap(screenPos));
            }
        }

        /// <summary>
        /// Waits a double-tap window then fires the single-tap action if
        /// no second tap arrived in the meantime.
        /// </summary>
        private IEnumerator DeferredSingleTap(Vector2 screenPos)
        {
            yield return new WaitForSeconds(DoubleTapWindowSec + 0.01f);

            // If _lastTapTime was reset to -999 we got a double tap — skip
            if (_lastTapTime < 0f) yield break;

            HandleSingleTap(screenPos);
        }

        private void HandleSingleTap(Vector2 screenPos)
        {
            if (_inputLocked) return;

            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
            {
                var station   = hit.collider.GetComponentInParent<StationMarker>();
                var character = hit.collider.GetComponentInParent<TNGCharacter>();

                if (station != null && station.isAccessible)
                {
                    StartCoroutine(MoveToStation(station));
                }
                else if (character != null)
                {
                    StartCoroutine(MoveToCharacter(character));
                }
            }
        }

        private void HandleDoubleTap()
        {
            if (_isMoving) return;
            StartCoroutine(MoveToPosition(_centrePosition, returnToCentreTime, null));
        }

        // ── Movement Coroutines ───────────────────────────────────────────────

        /// <summary>
        /// Moves the player smoothly to the station's stand position then faces
        /// the station's facing target. Notifies InteractionManager if there is
        /// a character at the station.
        /// </summary>
        private IEnumerator MoveToStation(StationMarker station)
        {
            Vector3 destination = station.GetStandPosition();
            destination.y = _centrePosition.y; // maintain eye height

            yield return StartCoroutine(MoveToPosition(destination, stationMoveTime, station.GetFacingPosition()));

            // If a character is attached to this station, trigger interaction
            var character = station.GetComponentInChildren<TNGCharacter>();
            if (character != null)
                InteractionManager.Instance?.Interact(character);
        }

        private IEnumerator MoveToCharacter(TNGCharacter character)
        {
            // Stand one unit in front of the character's position
            Vector3 toChar   = (character.transform.position - transform.position).normalized;
            Vector3 standPos = character.transform.position - toChar * 1.5f;
            standPos.y       = _centrePosition.y;

            yield return StartCoroutine(MoveToPosition(standPos, stationMoveTime, character.transform.position));

            InteractionManager.Instance?.Interact(character);
        }

        /// <summary>
        /// Core smooth movement coroutine. Locks input during transit.
        /// After reaching the destination, rotates to face facingPoint if provided.
        /// </summary>
        private IEnumerator MoveToPosition(Vector3 destination, float duration, Vector3? facingPoint)
        {
            _isMoving   = true;
            _inputLocked = true;

            Vector3 startPos = transform.position;
            float   elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, destination, t);
                yield return null;
            }

            transform.position = destination;

            // Rotate to face the target if provided
            if (facingPoint.HasValue)
            {
                Vector3 dir = facingPoint.Value - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float targetAngle = Quaternion.LookRotation(dir).eulerAngles.y;
                    _targetYaw = targetAngle;

                    // Wait for the smooth rotation to catch up
                    yield return new WaitForSeconds(0.3f);
                }
            }

            _isMoving    = false;
            _inputLocked = false;
        }

        // ── Rotation Application ──────────────────────────────────────────────

        private void ApplySmoothRotation()
        {
            float currentYaw   = transform.eulerAngles.y;
            float currentPitch = cameraRig != null ? cameraRig.localEulerAngles.x : 0f;

            // Unwrap pitch from 0-360 to signed range
            if (currentPitch > 180f) currentPitch -= 360f;

            float smoothYaw   = Mathf.LerpAngle(currentYaw,   _targetYaw,   smoothSpeed * Time.deltaTime);
            float smoothPitch = Mathf.LerpAngle(currentPitch, _targetPitch, smoothSpeed * Time.deltaTime);

            transform.localEulerAngles = new Vector3(0f, smoothYaw, 0f);

            if (cameraRig != null)
                cameraRig.localEulerAngles = new Vector3(smoothPitch, 0f, 0f);
        }

        // ── Editor Mouse Simulation ───────────────────────────────────────────

#if UNITY_EDITOR
        private Vector2 _lastMousePos;
        private bool    _mouseWasDown;

        private void HandleEditorMouseInput()
        {
            if (Mouse.current == null) return;

            bool  mouseDown = Mouse.current.rightButton.isPressed;
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (mouseDown)
            {
                if (_mouseWasDown)
                {
                    Vector2 delta = mousePos - _lastMousePos;
                    _targetYaw   += delta.x * lookSensitivity;
                    _targetPitch -= delta.y * lookSensitivity;
                    _targetPitch  = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
                }
            }
            else if (_mouseWasDown)
            {
                // Released — treat as tap if movement was small
                float dist = Vector2.Distance(mousePos, _lastMousePos);
                if (dist < SwipePixelThreshold && Mouse.current.leftButton.wasPressedThisFrame == false)
                    HandleSingleTap(mousePos);
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
                HandleSingleTap(mousePos);

            _lastMousePos = mousePos;
            _mouseWasDown = mouseDown;
        }
#endif

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Lock or unlock all player input (used by InteractionManager during dialogue).</summary>
        public void SetInputLocked(bool locked) => _inputLocked = locked;

        /// <summary>Returns true if the player is currently mid-movement.</summary>
        public bool IsMoving => _isMoving;
    }
}
