using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Lightweight, reusable UI animation helper driven entirely by coroutines.
    /// No paid plugins required.
    ///
    /// SETUP:
    ///   Add this component to any persistent GameObject in the scene (e.g. the UIController
    ///   object). Assign the reference in UIController's _juice field.
    ///
    /// SAFETY:
    ///   - Every animation ends at a clean resting state (scale 1, rotation 0, original position).
    ///   - Starting a new animation on an element automatically cancels the previous one first.
    ///   - Null targets are silently ignored.
    ///   - Uses Time.unscaledDeltaTime so animations play correctly if Time.timeScale is 0.
    ///
    /// HANDMADE STYLE GUIDE (suggested Inspector values for a stationery/sticky-note feel):
    ///   Wobble angle  :  4–8 °   (feels hand-drawn, not mechanical)
    ///   Pop overshoot :  1.12–1.2 (sticky-note slap, not a cartoon bounce)
    ///   Bump peak     :  1.15–1.25 (score ping)
    ///   Shake strength:  4–8 px  (rubber-stamp thud)
    ///   Slap start rot:  -6 to -10 ° (card dropped at a slight angle)
    /// </summary>
    public class UIJuiceAnimator : MonoBehaviour
    {
        [Header("Pop In / Out")]
        [Tooltip("Duration of PopIn and PopOut animations in seconds.")]
        [SerializeField] private float _popDuration = 0.22f;

        [Tooltip("Peak scale the element reaches before settling at 1.0. " +
                 "1.12 = subtle sticky-note slap. 1.25 = cartoon bounce.")]
        [SerializeField] private float _popOvershoot = 1.14f;

        [Header("Bump (score ping)")]
        [Tooltip("Duration of the Bump scale-up-and-back animation.")]
        [SerializeField] private float _bumpDuration = 0.18f;

        [Tooltip("Peak scale during bump. 1.2 = clear score ping.")]
        [SerializeField] private float _bumpPeak = 1.2f;

        [Header("Punch Text (multiplier hit)")]
        [Tooltip("Peak scale for PunchText – more dramatic than Bump.")]
        [SerializeField] private float _punchPeak = 1.4f;

        [Tooltip("Duration of the PunchText animation.")]
        [SerializeField] private float _punchDuration = 0.28f;

        [Header("Wobble (handmade wiggle)")]
        [Tooltip("Total duration of the wobble animation.")]
        [SerializeField] private float _wobbleDuration = 0.45f;

        [Tooltip("Maximum rotation angle in degrees. 4–8 degrees feels hand-written.")]
        [SerializeField] private float _wobbleAngle = 5f;

        [Tooltip("Number of left-right swings during the wobble.")]
        [SerializeField] private int _wobbleSwings = 3;

        [Header("Fade")]
        [Tooltip("Duration of FadeIn / FadeOut.")]
        [SerializeField] private float _fadeDuration = 0.18f;

        [Header("Shake")]
        [Tooltip("Default position shake strength in pixels. Override per-call if needed.")]
        [SerializeField] private float _shakeStrength = 6f;

        [Tooltip("Duration of the shake animation.")]
        [SerializeField] private float _shakeDuration = 0.22f;

        [Header("Slap-In (checkpoint panel)")]
        [Tooltip("Starting rotation in degrees. Negative = tilted left, like a card dropped casually.")]
        [SerializeField] private float _slapStartRotation = -7f;

        [Tooltip("Starting scale for the slap-in. 0 = appear from nothing. 0.8 = subtle scale-up.")]
        [SerializeField] private float _slapStartScale = 0.82f;

        [Tooltip("Total duration of the slap-in (scale + rotate + settle) before the residual wobble.")]
        [SerializeField] private float _slapDuration = 0.28f;

        // ── Active coroutine tracking ─────────────────────────────────────────────
        // Keyed by InstanceID so we can cancel and restart on the same element.

        private readonly Dictionary<int, Coroutine> _activeAnims      = new();
        private readonly Dictionary<int, Coroutine> _activeGroupAnims = new();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Scales in from zero with an overshoot, like a sticky note being slapped down.
        /// Ends at localScale (1,1,1).
        /// </summary>
        public void PopIn(RectTransform target)
        {
            if (target == null) return;
            KillAnim(target);
            RegisterAnim(target, StartCoroutine(PopInRoutine(target)));
        }

        /// <summary>
        /// Scales out to zero. Call SetActive(false) on completion yourself,
        /// or use HideAfterPopOut() if you need that automatically.
        /// Ends at localScale (0,0,0).
        /// </summary>
        public void PopOut(RectTransform target)
        {
            if (target == null) return;
            KillAnim(target);
            RegisterAnim(target, StartCoroutine(PopOutRoutine(target)));
        }

        /// <summary>
        /// Quick scale-up then settle back to 1. Use for score text when a value changes.
        /// Ends at localScale (1,1,1).
        /// </summary>
        public void Bump(RectTransform target)
        {
            if (target == null) return;
            KillAnim(target);
            RegisterAnim(target, StartCoroutine(BumpRoutine(target, _bumpPeak, _bumpDuration)));
        }

        /// <summary>
        /// More dramatic than Bump — a sharp punch with a slight elastic snap-back.
        /// Use for multiplier increases or exciting events.
        /// Ends at localScale (1,1,1).
        /// </summary>
        public void PunchText(RectTransform target)
        {
            if (target == null) return;
            KillAnim(target);
            RegisterAnim(target, StartCoroutine(PunchTextRoutine(target)));
        }

        /// <summary>
        /// Rocks left-right with decaying amplitude, like a wobbly handwritten label.
        /// Ends at original localEulerAngles.
        /// </summary>
        public void Wobble(RectTransform target)
        {
            if (target == null) return;
            KillAnim(target);
            RegisterAnim(target, StartCoroutine(WobbleRoutine(target, _wobbleAngle, _wobbleDuration, _wobbleSwings)));
        }

        /// <summary>Fades CanvasGroup alpha from 0 to 1.</summary>
        public void FadeIn(CanvasGroup group)
        {
            if (group == null) return;
            KillGroupAnim(group);
            RegisterGroupAnim(group, StartCoroutine(FadeInRoutine(group)));
        }

        /// <summary>Fades CanvasGroup alpha from its current value to 0. Does not disable the GameObject.</summary>
        public void FadeOut(CanvasGroup group)
        {
            if (group == null) return;
            KillGroupAnim(group);
            RegisterGroupAnim(group, StartCoroutine(FadeOutRoutine(group)));
        }

        /// <summary>
        /// Shakes the element's anchored position and returns it to its original position.
        /// <paramref name="strengthOverride"/> overrides the default Inspector value when > 0.
        /// </summary>
        public void Shake(RectTransform target, float strengthOverride = -1f)
        {
            if (target == null) return;
            KillAnim(target);
            float strength = strengthOverride > 0f ? strengthOverride : _shakeStrength;
            RegisterAnim(target, StartCoroutine(ShakeRoutine(target, strength)));
        }

        /// <summary>
        /// Combined entrance animation: fades in while scaling from a tilted, shrunken state.
        /// Feels like a card being casually dropped onto the board.
        /// Ends at localScale (1,1,1), localEulerAngles (0,0,0), alpha 1.
        /// <paramref name="group"/> can be null if no fade is needed.
        /// </summary>
        public void SlapIn(RectTransform target, CanvasGroup group = null)
        {
            if (target == null) return;
            KillAnim(target);
            if (group != null) KillGroupAnim(group);
            RegisterAnim(target, StartCoroutine(SlapInRoutine(target, group)));
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator PopInRoutine(RectTransform rt)
        {
            rt.localScale = Vector3.zero;

            for (float e = 0f; e < _popDuration; e += Time.unscaledDeltaTime)
            {
                float t     = e / _popDuration;
                float scale = PopCurve(t, _popOvershoot, from: 0f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }

            rt.localScale = Vector3.one;
        }

        private IEnumerator PopOutRoutine(RectTransform rt)
        {
            float startScale = rt.localScale.x;

            for (float e = 0f; e < _popDuration; e += Time.unscaledDeltaTime)
            {
                float t = e / _popDuration;
                rt.localScale = Vector3.one * Mathf.Lerp(startScale, 0f, t * t);
                yield return null;
            }

            rt.localScale = Vector3.zero;
        }

        private IEnumerator BumpRoutine(RectTransform rt, float peak, float duration)
        {
            rt.localScale = Vector3.one;

            float half = duration * 0.5f;

            for (float e = 0f; e < half; e += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * Mathf.Lerp(1f, peak, Mathf.SmoothStep(0f, 1f, e / half));
                yield return null;
            }
            for (float e = 0f; e < half; e += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * Mathf.Lerp(peak, 1f, Mathf.SmoothStep(0f, 1f, e / half));
                yield return null;
            }

            rt.localScale = Vector3.one;
        }

        private IEnumerator PunchTextRoutine(RectTransform rt)
        {
            rt.localScale = Vector3.one;

            float upTime   = _punchDuration * 0.25f;
            float downTime = _punchDuration * 0.75f;

            // Sharp scale up.
            for (float e = 0f; e < upTime; e += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * Mathf.Lerp(1f, _punchPeak, e / upTime);
                yield return null;
            }

            // Elastic-ish snap back with a small overshoot at the end.
            for (float e = 0f; e < downTime; e += Time.unscaledDeltaTime)
            {
                float t     = e / downTime;
                // Lerp back to 1, then add a tiny sine ripple that decays.
                float scale = Mathf.Lerp(_punchPeak, 1f, Mathf.SmoothStep(0f, 1f, t))
                              + Mathf.Sin(t * Mathf.PI) * 0.06f * (1f - t);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }

            rt.localScale = Vector3.one;
        }

        private IEnumerator WobbleRoutine(RectTransform rt, float angle, float duration, int swings)
        {
            Vector3 restEuler = rt.localEulerAngles;
            // Normalise to [-180, 180] range so we can add small offsets cleanly.
            float baseZ = restEuler.z > 180f ? restEuler.z - 360f : restEuler.z;

            for (float e = 0f; e < duration; e += Time.unscaledDeltaTime)
            {
                float t     = e / duration;
                float decay = 1f - t;
                float z     = Mathf.Sin(t * swings * Mathf.PI * 2f) * angle * decay;
                rt.localEulerAngles = new Vector3(restEuler.x, restEuler.y, baseZ + z);
                yield return null;
            }

            rt.localEulerAngles = restEuler;
        }

        private IEnumerator FadeInRoutine(CanvasGroup group)
        {
            group.alpha = 0f;

            for (float e = 0f; e < _fadeDuration; e += Time.unscaledDeltaTime)
            {
                group.alpha = e / _fadeDuration;
                yield return null;
            }

            group.alpha = 1f;
        }

        private IEnumerator FadeOutRoutine(CanvasGroup group)
        {
            float startAlpha = group.alpha;

            for (float e = 0f; e < _fadeDuration; e += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(startAlpha, 0f, e / _fadeDuration);
                yield return null;
            }

            group.alpha = 0f;
        }

        private IEnumerator ShakeRoutine(RectTransform rt, float strength)
        {
            Vector2 origin = rt.anchoredPosition;

            for (float e = 0f; e < _shakeDuration; e += Time.unscaledDeltaTime)
            {
                float decay = 1f - (e / _shakeDuration);
                rt.anchoredPosition = origin + Random.insideUnitCircle * (strength * decay);
                yield return null;
            }

            rt.anchoredPosition = origin;
        }

        private IEnumerator SlapInRoutine(RectTransform rt, CanvasGroup group)
        {
            // Set starting state.
            rt.localScale       = Vector3.one * _slapStartScale;
            rt.localEulerAngles = new Vector3(0f, 0f, _slapStartRotation);
            if (group != null) group.alpha = 0f;

            for (float e = 0f; e < _slapDuration; e += Time.unscaledDeltaTime)
            {
                float t = e / _slapDuration;

                // Scale: start → 1.0 with overshoot.
                rt.localScale = Vector3.one * PopCurve(t, _popOvershoot, from: _slapStartScale);

                // Rotation: tilt → 0 with SmoothStep easing (snappy landing).
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(_slapStartRotation, 0f, Mathf.SmoothStep(0f, 1f, t)));

                // Fade in: complete in the first 60% of the duration.
                if (group != null) group.alpha = Mathf.Clamp01(t / 0.6f);

                yield return null;
            }

            // Clean landing state.
            rt.localScale       = Vector3.one;
            rt.localEulerAngles = Vector3.zero;
            if (group != null) group.alpha = 1f;

            // Residual wobble after landing — inlined to avoid nested coroutine tracking issues.
            float wobbleDur    = _wobbleDuration * 0.6f;
            float wobbleAngle  = _wobbleAngle * 0.7f;
            int   wobbleSwings = _wobbleSwings;
            for (float e = 0f; e < wobbleDur; e += Time.unscaledDeltaTime)
            {
                float t = e / wobbleDur;
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * wobbleSwings * Mathf.PI * 2f) * wobbleAngle * (1f - t));
                yield return null;
            }

            rt.localEulerAngles = Vector3.zero;
        }

        // ── Easing helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Animates from <paramref name="from"/> to 1.0, peaking at <paramref name="peakScale"/>
        /// around t=0.65 before settling back to 1.0.
        ///
        /// This gives the characteristic overshoot-and-settle feel without requiring
        /// an AnimationCurve asset.
        /// </summary>
        private static float PopCurve(float t, float peakScale, float from = 0f)
        {
            const float peakTime = 0.65f;

            float scale = t < peakTime
                ? Mathf.SmoothStep(from, peakScale, t / peakTime)
                : Mathf.SmoothStep(peakScale, 1f, (t - peakTime) / (1f - peakTime));

            return scale;
        }

        // ── Coroutine tracking ────────────────────────────────────────────────────

        private void KillAnim(RectTransform rt)
        {
            int id = rt.GetInstanceID();
            if (_activeAnims.TryGetValue(id, out Coroutine c) && c != null)
                StopCoroutine(c);
            _activeAnims.Remove(id);
        }

        private void RegisterAnim(RectTransform rt, Coroutine c) =>
            _activeAnims[rt.GetInstanceID()] = c;

        private void KillGroupAnim(CanvasGroup g)
        {
            int id = g.GetInstanceID();
            if (_activeGroupAnims.TryGetValue(id, out Coroutine c) && c != null)
                StopCoroutine(c);
            _activeGroupAnims.Remove(id);
        }

        private void RegisterGroupAnim(CanvasGroup g, Coroutine c) =>
            _activeGroupAnims[g.GetInstanceID()] = c;
    }
}
