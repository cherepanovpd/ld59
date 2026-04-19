// Path: Assets/_Project/Scripts/DayNight/SunMovementController.cs

using Core;
using Project.Core.Events;
using Project.DayNight.Config;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Project.DayNight
{
    /// <summary>
    /// Controls the movement of a sun (Light2D) along a circular path based on the day/night cycle time.
    /// Zero allocations in Update loop, configurable orbit parameters.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class SunMovementController : MonoBehaviour
    {
        [Header("Configuration Source")]
        [SerializeField]
        [Tooltip("If true, uses values from G.DayNightConfig. If false, uses local inspector values.")]
        private bool _useConfig = true;

        [Header("Orbit Configuration (Local Override)")]
        [SerializeField]
        [Tooltip("Center point of the sun's circular orbit in world space.")]
        private Vector2 _orbitCenter = Vector2.zero;

        [SerializeField]
        [Tooltip("Radius of the sun's circular orbit in world units.")]
        private float _orbitRadius = 10f;

        [SerializeField]
        [Tooltip("Angle offset in degrees (0 = east, 90 = north, 180 = west, 270 = south).")]
        [Range(0f, 360f)]
        private float _startAngleOffset = 0f;

        [SerializeField]
        [Tooltip("If true, the sun's intensity will change based on time of day (brighter at noon).")]
        private bool _adjustIntensity = true;

        [Header("Intensity Curve (Local Override)")]
        [SerializeField]
        [Tooltip("Curve mapping normalized time (0-1) to light intensity (0-1). Default peaks at noon (0.5).")]
        private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);

        [Header("Performance")]
        [SerializeField]
        [Tooltip("Update mode: EveryFrame, OnTimeChange (via events), or Threshold.")]
        private UpdateMode _updateMode = UpdateMode.OnTimeChange;

        [SerializeField]
        [Tooltip("Minimum normalized time change required to trigger an update in Threshold mode.")]
        [Range(0.001f, 0.1f)]
        private float _updateThreshold = 0.01f;

        // Cached components
        private Light2D _sunLight;
        private float _lastNormalizedTime = -1f;

        // Performance optimization: cache Mathf conversions
        private const float Deg2Rad = Mathf.PI / 180f;
        private const float FullCircleRad = Mathf.PI * 2f;

        private enum UpdateMode
        {
            EveryFrame,
            OnTimeChange,
            Threshold
        }

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            ApplyConfiguration();
        }

        private void Start()
        {
            SubscribeToEvents();
            // Initial positioning based on current time
            UpdateSunPosition();
        }

        private void Update()
        {
            if (_updateMode != UpdateMode.EveryFrame)
                return;

            UpdateSunPosition();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            _sunLight = GetComponent<Light2D>();
            if (_sunLight == null)
            {
                Debug.LogError("[SunMovementController] No Light2D component found on GameObject.", this);
            }
        }

        private void ApplyConfiguration()
        {
            if (!_useConfig || !G.HasDayNightConfig())
                return;

            var config = G.DayNightConfig;
            _orbitCenter = config.SunOrbitCenter;
            _orbitRadius = config.SunOrbitRadius;
            _startAngleOffset = config.SunStartAngleOffset;
            _adjustIntensity = config.SunAdjustIntensity;
            
            // Only override curve if it's not the default empty curve
            if (config.SunIntensityCurve != null && config.SunIntensityCurve.keys.Length > 0)
            {
                _intensityCurve = config.SunIntensityCurve;
            }
        }

        private void SubscribeToEvents()
        {
            if (_updateMode == UpdateMode.OnTimeChange && G.HasEvents())
            {
                G.Events.Subscribe<DayNightTimeChangedEvent>(OnTimeChanged);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (G.HasEvents())
            {
                G.Events.Unsubscribe<DayNightTimeChangedEvent>(OnTimeChanged);
            }
        }

        #endregion

        #region Update Logic

        private void OnTimeChanged(DayNightTimeChangedEvent evt)
        {
            // Event-based update - zero allocations in Update
            UpdateSunPosition(evt.NormalizedTime);
        }

        private void UpdateSunPosition()
        {
            if (!G.HasDayNightSystem())
            {
                // Fallback: use a simple time-based animation for testing
                Debug.LogWarning("[SunMovementController] DayNightSystem not available. Using fallback time.");
                UpdateWithFallbackTime();
                return;
            }

            float normalizedTime = G.DayNightSystem.NormalizedTime;
            
            // Threshold check for Threshold mode
            if (_updateMode == UpdateMode.Threshold)
            {
                float timeDelta = Mathf.Abs(normalizedTime - _lastNormalizedTime);
                if (timeDelta < _updateThreshold)
                    return;
            }

            UpdateSunPosition(normalizedTime);
        }

        private void UpdateSunPosition(float normalizedTime)
        {
            _lastNormalizedTime = normalizedTime;

            // Calculate position on circular path
            Vector2 position = CalculateOrbitPosition(normalizedTime);
            transform.position = new Vector3(position.x, position.y, transform.position.z);

            // Adjust light intensity if enabled
            if (_adjustIntensity && _sunLight != null)
            {
                float intensity = _intensityCurve.Evaluate(normalizedTime);
                _sunLight.intensity = intensity;
            }
        }

        private void UpdateWithFallbackTime()
        {
            // Fallback for when DayNightSystem is not available
            float normalizedTime = (Time.time % 60f) / 60f; // 60-second cycle for testing
            UpdateSunPosition(normalizedTime);
        }

        private Vector2 CalculateOrbitPosition(float normalizedTime)
        {
            // Convert normalized time (0-1) to angle in radians
            // 0 = midnight (sun at bottom), 0.25 = dawn (sun rising east), 0.5 = noon (sun at top), 0.75 = dusk (sun setting west)
            float angleRad = (normalizedTime * FullCircleRad) + (_startAngleOffset * Deg2Rad);

            // Calculate position on circle
            float x = _orbitCenter.x + Mathf.Cos(angleRad) * _orbitRadius;
            float y = _orbitCenter.y + Mathf.Sin(angleRad) * _orbitRadius;

            return new Vector2(x, y);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Forces an immediate update of the sun's position and intensity.
        /// Useful when orbit parameters are changed at runtime.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateSunPosition();
        }

        /// <summary>
        /// Sets the orbit center point.
        /// </summary>
        public void SetOrbitCenter(Vector2 center)
        {
            _orbitCenter = center;
            if (enabled && gameObject.activeInHierarchy)
                UpdateSunPosition();
        }

        /// <summary>
        /// Sets the orbit radius.
        /// </summary>
        public void SetOrbitRadius(float radius)
        {
            _orbitRadius = Mathf.Max(0f, radius);
            if (enabled && gameObject.activeInHierarchy)
                UpdateSunPosition();
        }

        /// <summary>
        /// Gets the current orbit center.
        /// </summary>
        public Vector2 GetOrbitCenter() => _orbitCenter;

        /// <summary>
        /// Gets the current orbit radius.
        /// </summary>
        public float GetOrbitRadius() => _orbitRadius;

        /// <summary>
        /// Refreshes configuration from G.DayNightConfig.
        /// </summary>
        public void RefreshConfiguration()
        {
            ApplyConfiguration();
            ForceUpdate();
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp radius to non-negative
            _orbitRadius = Mathf.Max(0f, _orbitRadius);

            // Update intensity curve if it's the default
            if (_intensityCurve == null || _intensityCurve.keys.Length == 0)
            {
                _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);
                _intensityCurve.AddKey(1f, 0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw orbit circle in scene view
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(_orbitCenter, Vector3.forward, _orbitRadius);

            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_orbitCenter, 0.2f);

            // Draw current position if we have a DayNightSystem
            if (Application.isPlaying && G.HasDayNightSystem())
            {
                Vector2 currentPos = CalculateOrbitPosition(G.DayNightSystem.NormalizedTime);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentPos, 0.3f);
            }
        }
#endif

        #endregion
    }
}