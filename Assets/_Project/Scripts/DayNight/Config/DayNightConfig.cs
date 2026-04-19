// Path: Assets/_Project/Scripts/DayNight/Config/DayNightConfig.cs

using Core;
using UnityEngine;

namespace Project.DayNight.Config
{
    /// <summary>
    /// ScriptableObject configuration for day/night cycle visual effects.
    /// Contains day length, start time, and AnimationCurves for URP volume parameters.
    /// Registers itself into G.DayNightConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "Game/Day Night Config", order = 102)]
    public class DayNightConfig : ScriptableObject
    {
        [Header("Time Settings")]
        [SerializeField]
        [Tooltip("Real‑time seconds for a full day cycle (0→1). Default is 300 seconds (5 minutes).")]
        private float _dayLengthSeconds = 300f;

        [SerializeField]
        [Tooltip("Normalized start time (0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk). Default is 0.25 (dawn).")]
        [Range(0f, 1f)]
        private float _startTimeNormalized = 0.25f;

        [SerializeField]
        [Tooltip("If true, the day/night cycle starts automatically when the scene loads.")]
        private bool _autoStart = true;

        [Header("URP Volume Curves")]
        [SerializeField]
        [Tooltip("Vignete curve (x = normalized time, y = exposure value in EV).")]
        private AnimationCurve _vignetteIntensityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [SerializeField]
        [Tooltip("Color temperature curve (x = normalized time, y = temperature in Kelvin).")]
        private AnimationCurve _temperatureCurve = AnimationCurve.Linear(0f, 8000f, 1f, 2000f);

        [SerializeField]
        [Tooltip("Tint curve (x = normalized time, y = tint value).")]
        private AnimationCurve _tintCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [SerializeField]
        [Tooltip("Saturation curve (x = normalized time, y = saturation multiplier).")]
        private AnimationCurve _saturationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.8f);

        [SerializeField]
        [Tooltip("Optional gradient for sky tint (could be used for skybox or ambient light).")]
        private Gradient _skyTintGradient;

        [Header("Sun Orbit Configuration")]
        [SerializeField]
        [Tooltip("Center point of the sun's circular orbit in world space.")]
        private Vector2 _sunOrbitCenter = Vector2.zero;

        [SerializeField]
        [Tooltip("Radius of the sun's circular orbit in world units.")]
        private float _sunOrbitRadius = 10f;

        [SerializeField]
        [Tooltip("Angle offset in degrees (0 = east, 90 = north, 180 = west, 270 = south).")]
        [Range(0f, 360f)]
        private float _sunStartAngleOffset = 0f;

        [SerializeField]
        [Tooltip("If true, the sun's intensity will change based on time of day (brighter at noon).")]
        private bool _sunAdjustIntensity = true;

        [SerializeField]
        [Tooltip("Curve mapping normalized time (0-1) to sun light intensity (0-1). Default peaks at noon (0.5).")]
        private AnimationCurve _sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);

        /// <summary>
        /// Real‑time seconds for a full day cycle (0→1).
        /// </summary>
        public float DayLengthSeconds => _dayLengthSeconds;

        /// <summary>
        /// Normalized start time (0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk).
        /// </summary>
        public float StartTimeNormalized => _startTimeNormalized;

        /// <summary>
        /// If true, the day/night cycle starts automatically when the scene loads.
        /// </summary>
        public bool AutoStart => _autoStart;

        /// <summary>
        /// Exposure curve (x = normalized time, y = exposure value in EV).
        /// </summary>
        public AnimationCurve VignetteIntensityCurve => _vignetteIntensityCurve;

        /// <summary>
        /// Color temperature curve (x = normalized time, y = temperature in Kelvin).
        /// </summary>
        public AnimationCurve TemperatureCurve => _temperatureCurve;

        /// <summary>
        /// Tint curve (x = normalized time, y = tint value).
        /// </summary>
        public AnimationCurve TintCurve => _tintCurve;

        /// <summary>
        /// Saturation curve (x = normalized time, y = saturation multiplier).
        /// </summary>
        public AnimationCurve SaturationCurve => _saturationCurve;

        /// <summary>
        /// Optional gradient for sky tint (could be used for skybox or ambient light).
        /// </summary>
        public Gradient SkyTintGradient => _skyTintGradient;

        /// <summary>
        /// Center point of the sun's circular orbit in world space.
        /// </summary>
        public Vector2 SunOrbitCenter => _sunOrbitCenter;

        /// <summary>
        /// Radius of the sun's circular orbit in world units.
        /// </summary>
        public float SunOrbitRadius => _sunOrbitRadius;

        /// <summary>
        /// Angle offset in degrees (0 = east, 90 = north, 180 = west, 270 = south).
        /// </summary>
        public float SunStartAngleOffset => _sunStartAngleOffset;

        /// <summary>
        /// If true, the sun's intensity will change based on time of day (brighter at noon).
        /// </summary>
        public bool SunAdjustIntensity => _sunAdjustIntensity;

        /// <summary>
        /// Curve mapping normalized time (0-1) to sun light intensity (0-1). Default peaks at noon (0.5).
        /// </summary>
        public AnimationCurve SunIntensityCurve => _sunIntensityCurve;

        #region Self‑Registration

        private void OnEnable()
        {
            // Register this instance as the global day‑night config if not already set
            if (G.DayNightConfig == null)
            {
                G.DayNightConfig = this;
                G.EnsureSystem(nameof(DayNightConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.DayNightConfig == this)
                G.DayNightConfig = null;
        }

        #endregion
    }
}