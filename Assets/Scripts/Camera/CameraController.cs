using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Controls camera follow and orthographic zoom for the board game.
    /// Attach this script directly to the Main Camera GameObject.
    ///
    /// Two modes:
    ///   Follow mode  – camera tracks the player token; zoomed in. Active during normal play.
    ///   Checkpoint mode – camera pans to the board centre and zooms out. Active at checkpoint.
    ///
    /// Call SetFollowMode() when movement starts, SetCheckpointMode() when the player lands
    /// on the checkpoint. The GameManager drives these calls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("Transform to follow (the player token). Assign in Inspector or call SetFollowTarget().")]
        [SerializeField] private Transform _followTarget;

        [Tooltip("Lerp speed toward the follow target. Higher = snappier.")]
        [SerializeField] private float _followSpeed = 6f;

        [Header("Zoom")]
        [Tooltip("Orthographic size when following the player.")]
        [SerializeField] private float _zoomedInSize = 4f;

        [Tooltip("Orthographic size when showing the whole board at checkpoint.")]
        [SerializeField] private float _zoomedOutSize = 10f;

        [Tooltip("Speed of orthographic size transitions.")]
        [SerializeField] private float _zoomSpeed = 3f;

        [Header("Board Centre")]
        [Tooltip("World position to look at when zoomed out. Set this to the centre of your board layout.")]
        [SerializeField] private Vector3 _boardCentrePosition = Vector3.zero;

        // ── State ─────────────────────────────────────────────────────────────────

        private Camera _cam;
        private bool   _following = true;
        private float  _targetSize;

        private void Awake()
        {
            _cam        = GetComponent<Camera>();
            _targetSize = _zoomedInSize;
        }

        private void LateUpdate()
        {
            // Smooth zoom toward target size every frame.
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, Time.deltaTime * _zoomSpeed);

            if (_following && _followTarget != null)
            {
                // Follow on X/Y; preserve camera Z so it doesn't fly into the scene.
                Vector3 target = new Vector3(_followTarget.position.x, _followTarget.position.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * _followSpeed);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Switches to follow-player mode and zooms in. Call when movement begins.</summary>
        public void SetFollowMode()
        {
            _following   = true;
            _targetSize  = _zoomedInSize;
        }

        /// <summary>
        /// Stops following, zooms out, and pans to the board centre.
        /// Call when the player reaches the checkpoint.
        /// </summary>
        public void SetCheckpointMode()
        {
            _following  = false;
            _targetSize = _zoomedOutSize;
            StartCoroutine(PanToCentre());
        }

        /// <summary>Changes the follow target at runtime (e.g. after spawning the player).</summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private IEnumerator PanToCentre()
        {
            Vector3 start  = transform.position;
            Vector3 end    = new Vector3(_boardCentrePosition.x, _boardCentrePosition.y, transform.position.z);
            // Drive pan duration from zoom speed so they feel synchronised.
            float duration = Mathf.Max(0.1f, 1f / _zoomSpeed);
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed           += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            transform.position = end;
        }
    }
}
