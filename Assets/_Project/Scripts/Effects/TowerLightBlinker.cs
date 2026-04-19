// Path: Assets/_Project/Scripts/Effects/TowerLightBlinker.cs

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Project.Effects
{
    /// <summary>
    /// Controls blinking light effect for towers using Light2D component.
    /// Supports both continuous automatic blinking and on‑demand single blinks.
    /// Uses DOTween for smooth intensity transitions.
    /// </summary>
    public class TowerLightBlinker : MonoBehaviour
    {
        [Header("Light References")]
        [SerializeField, Tooltip("The Light2D component to control. Automatically fetched if not assigned.")]
        private Light2D _targetLight;

        [Header("Blink Timing")]
        [SerializeField, Tooltip("Duration of the 'on' transition (light intensity from off to on).")]
        [Min(0.01f)]
        private float _onDuration = 0.3f;
        
        [SerializeField, Tooltip("Duration of the 'off' transition (light intensity from on to off).")]
        [Min(0.01f)]
        private float _offDuration = 0.7f;
        
        [SerializeField, Tooltip("Interval between consecutive blinks in continuous mode.")]
        [Min(0.01f)]
        private float _blinkInterval = 1.5f;

        [Header("Intensity Values")]
        [SerializeField, Tooltip("Light intensity when the light is fully on.")]
        [Range(0f, 10f)]
        private float _onIntensity = 1f;
        
        [SerializeField, Tooltip("Light intensity when the light is fully off.")]
        [Range(0f, 10f)]
        private float _offIntensity = 0f;

        [Header("Behaviour")]
        [SerializeField, Tooltip("If true, continuous blinking starts automatically on Awake.")]
        private bool _startAutomatically = true;

        [SerializeField, Tooltip("Easing curve for the 'on' transition.")]
        private Ease _onEase = Ease.OutQuad;
        
        [SerializeField, Tooltip("Easing curve for the 'off' transition.")]
        private Ease _offEase = Ease.InQuad;

        // Runtime state
        private Sequence _blinkSequence;
        private bool _isContinuousBlinking;
        private float _currentIntensity;

        /// <summary>
        /// Cached reference to the Light2D component.
        /// </summary>
        public Light2D TargetLight => _targetLight;

        /// <summary>
        /// Whether the light is currently in continuous blinking mode.
        /// </summary>
        public bool IsBlinkingContinuously => _isContinuousBlinking;

        /// <summary>
        /// Invoked when a blink starts (both single and continuous).
        /// </summary>
        public event Action OnBlinkStart;

        /// <summary>
        /// Invoked when a blink completes (both single and continuous).
        /// </summary>
        public event Action OnBlinkComplete;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheLightReference();
            InitializeIntensity();
        }

        private void Start()
        {
            if (_startAutomatically)
            {
                StartContinuousBlinking();
            }
        }

        private void OnDestroy()
        {
            StopAllTweens();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts automatic continuous blinking with the currently configured parameters.
        /// If already blinking, this method does nothing.
        /// </summary>
        public void StartContinuousBlinking()
        {
            if (_isContinuousBlinking)
            {
                return;
            }

            _isContinuousBlinking = true;
            CreateAndPlayBlinkSequence(true);
        }

        /// <summary>
        /// Stops automatic continuous blinking.
        /// The light will stay at its current intensity.
        /// </summary>
        public void StopContinuousBlinking()
        {
            if (!_isContinuousBlinking)
            {
                return;
            }

            _isContinuousBlinking = false;
            StopAllTweens();
        }

        /// <summary>
        /// Triggers a single blink on‑demand, regardless of continuous mode.
        /// The blink consists of a smooth on‑transition followed by an off‑transition.
        /// If continuous blinking is active, it will be temporarily interrupted and resumed afterwards.
        /// </summary>
        public void TriggerSingleBlink()
        {
            bool wasContinuous = _isContinuousBlinking;
            
            if (wasContinuous)
            {
                StopAllTweens();
            }

            OnBlinkStart?.Invoke();

            Sequence singleBlink = DOTween.Sequence();
            singleBlink.Append(DOTween.To(
                    () => _currentIntensity,
                    value => SetLightIntensity(value),
                    _onIntensity,
                    _onDuration)
                .SetEase(_onEase))
                .Append(DOTween.To(
                    () => _currentIntensity,
                    value => SetLightIntensity(value),
                    _offIntensity,
                    _offDuration)
                .SetEase(_offEase))
                .OnComplete(() =>
                {
                    OnBlinkComplete?.Invoke();
                    if (wasContinuous)
                    {
                        CreateAndPlayBlinkSequence(false);
                    }
                });

            singleBlink.Play();
        }

        /// <summary>
        /// Updates the timing parameters for future blinks.
        /// If continuous blinking is active, the new parameters will be used from the next blink cycle.
        /// </summary>
        /// <param name="onDuration">New duration for the 'on' transition (seconds).</param>
        /// <param name="offDuration">New duration for the 'off' transition (seconds).</param>
        /// <param name="interval">New interval between blinks in continuous mode (seconds).</param>
        public void SetBlinkParameters(float onDuration, float offDuration, float interval)
        {
            _onDuration = Mathf.Max(0.01f, onDuration);
            _offDuration = Mathf.Max(0.01f, offDuration);
            _blinkInterval = Mathf.Max(0.01f, interval);

            if (_isContinuousBlinking)
            {
                StopAllTweens();
                CreateAndPlayBlinkSequence(false);
            }
        }

        #endregion

        #region Private Helpers

        private void CacheLightReference()
        {
            if (_targetLight == null)
            {
                _targetLight = GetComponent<Light2D>();
            }

            if (_targetLight == null)
            {
                Debug.LogError($"{nameof(TowerLightBlinker)} requires a {nameof(Light2D)} component.", this);
            }
        }

        private void InitializeIntensity()
        {
            if (_targetLight != null)
            {
                _currentIntensity = _targetLight.intensity;
            }
        }

        private void SetLightIntensity(float intensity)
        {
            _currentIntensity = intensity;
            if (_targetLight != null)
            {
                _targetLight.intensity = intensity;
            }
        }

        private void CreateAndPlayBlinkSequence(bool includeInitialDelay)
        {
            if (_targetLight == null)
            {
                return;
            }

            StopAllTweens();

            _blinkSequence = DOTween.Sequence();

            if (includeInitialDelay && _blinkInterval > 0f)
            {
                _blinkSequence.AppendInterval(_blinkInterval);
            }

            // Create a sub‑sequence for one blink cycle (on → off)
            Sequence blinkCycle = DOTween.Sequence();
            blinkCycle
                .AppendCallback(() => OnBlinkStart?.Invoke())
                .Append(DOTween.To(
                    () => _currentIntensity,
                    SetLightIntensity,
                    _onIntensity,
                    _onDuration)
                    .SetEase(_onEase))
                .Append(DOTween.To(
                    () => _currentIntensity,
                    SetLightIntensity,
                    _offIntensity,
                    _offDuration)
                    .SetEase(_offEase))
                .AppendCallback(() => OnBlinkComplete?.Invoke())
                .AppendInterval(_blinkInterval);

            _blinkSequence
                .Append(blinkCycle)
                .SetLoops(-1, LoopType.Restart);

            _blinkSequence.Play();
        }

        private void StopAllTweens()
        {
            if (_blinkSequence != null && _blinkSequence.IsActive())
            {
                _blinkSequence.Kill();
                _blinkSequence = null;
            }
        }

        #endregion

        #region Editor Debugging

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/Light2D/Add Tower Light Blinker")]
        private static void AddBlinkerToLight(UnityEditor.MenuCommand command)
        {
            var light = command.context as Light2D;
            if (light != null && light.GetComponent<TowerLightBlinker>() == null)
            {
                light.gameObject.AddComponent<TowerLightBlinker>();
            }
        }
#endif

        #endregion
    }
}