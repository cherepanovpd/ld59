// Path: Assets/_Project/Scripts/DayNight/DayNightVolumeController.cs

using Core;
using Project.Core;
using Project.Core.Events;
using Project.DayNight.Config;
using Project.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Project.DayNight
{
    /// <summary>
    /// Applies URP Volume overrides based on the current day/night time.
    /// Listens to DayNightTimeChangedEvent and updates post‑processing parameters.
    /// Zero allocations in Update loop.
    /// </summary>
    public class DayNightVolumeController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Reference to the DayNightConfig ScriptableObject. If null, will try to use G.DayNightConfig.")]
        private DayNightConfig _config;

        [Header("Volume References")]
        [SerializeField]
        [Tooltip("The Volume component to control. If null, will use GetComponent<Volume>().")]
        private Volume _volume;

        [Header("Update Settings")]
        [SerializeField]
        [Tooltip("Minimum normalized time change required to trigger a volume update (reduces CPU).")]
        private float _timeChangeThreshold = 0.001f;

        [SerializeField]
        [Tooltip("If true, updates volume every frame even without events (for debugging).")]
        private bool _forceContinuousUpdate;

        // Cached volume components
        private ColorAdjustments _colorAdjustments;
        private WhiteBalance _whiteBalance;
        private Vignette _vignette;

        // Cached parameter values to avoid unnecessary overrides
        private float _lastAppliedTime = -1f;
        private bool _hasColorAdjustments;
        private bool _hasWhiteBalance;
        private bool _hasVignette;
        
        // Game state tracking
        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheVolumeComponents();
            EnsureConfig();
            RegisterWithG();
        }

        private void Start()
        {
            SubscribeToEvents();
            SubscribeToGameState();
            
            // If game is already playing, initialize immediately
            if (G.GameState != null && G.GameState.CurrentState == GameState.Playing)
            {
                InitializeVolume();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            UnsubscribeFromGameState();
            UnregisterFromG();
        }

        private void Update()
        {
            if (_forceContinuousUpdate)
            {
                // For debugging: update every frame with a simulated time
                // In production, this should be driven by events.
                UpdateVolume(GetCurrentTimeNormalized());
            }
        }

        #endregion

        #region Volume Setup

        private void CacheVolumeComponents()
        {
            if (_volume == null)
                _volume = GetComponent<Volume>();

            if (_volume == null || _volume.profile == null)
            {
                Debug.LogWarning("[DayNightVolumeController] No Volume or VolumeProfile found. Disabling.", this);
                enabled = false;
                return;
            }

            // Try to get existing overrides; if they don't exist, we'll add them.
            _hasColorAdjustments = _volume.profile.TryGet(out _colorAdjustments);
            _hasWhiteBalance = _volume.profile.TryGet(out _whiteBalance);
            _hasVignette = _volume.profile.TryGet(out _vignette);

            // If any component is missing, log a warning but continue (they may be added later).
            if (!_hasColorAdjustments)
                Debug.LogWarning("[DayNightVolumeController] Volume profile missing ColorAdjustments. Exposure and saturation will not be applied.", this);
            if (!_hasWhiteBalance)
                Debug.LogWarning("[DayNightVolumeController] Volume profile missing WhiteBalance. Temperature and tint will not be applied.", this);
        }

        private void EnsureConfig()
        {
            if (_config == null)
                _config = G.DayNightConfig;

            if (_config == null)
                Debug.LogWarning("[DayNightVolumeController] No DayNightConfig assigned and G.DayNightConfig is null. Volume overrides will be disabled.", this);
        }

        #endregion

        #region Time Updates

        /// <summary>
        /// Public method to manually set the normalized time (0‑1).
        /// Useful for testing or if events are not used.
        /// </summary>
        public void SetTimeNormalized(float normalizedTime)
        {
            UpdateVolume(normalizedTime);
        }

        private void UpdateVolume(float normalizedTime)
        {
            if (_config == null)
                return;

            // Skip if time hasn't changed enough
            if (Mathf.Abs(normalizedTime - _lastAppliedTime) < _timeChangeThreshold)
                return;

            _lastAppliedTime = normalizedTime;

            float vignetteIntensity = _config.VignetteIntensityCurve.Evaluate(normalizedTime);
            float temperature = _config.TemperatureCurve.Evaluate(normalizedTime);
            float tint = _config.TintCurve.Evaluate(normalizedTime);
            float saturation = _config.SaturationCurve.Evaluate(normalizedTime);

            if (_hasWhiteBalance)
            {
                _whiteBalance.temperature.value = temperature;
                _whiteBalance.tint.value = tint;
            }

            if (_hasColorAdjustments)
                _colorAdjustments.saturation.value = (saturation - 1f) * 100f; // Convert multiplier to percentage
            
            if (_hasVignette)
                _vignette.intensity.value = vignetteIntensity;

            // Optional: apply sky tint gradient to a global shader property
            if (_config.SkyTintGradient != null)
            {
                Color skyTint = _config.SkyTintGradient.Evaluate(normalizedTime);
                Shader.SetGlobalColor("_DayNightSkyTint", skyTint);
            }
        }

        private float GetCurrentTimeNormalized()
        {
            // Priority 1: Use DayNightSystem if registered
            if (G.DayNightSystem != null)
            {
                return G.DayNightSystem.NormalizedTime;
            }

            // Fallback: cycle every 10 seconds for visualization (debugging only)
            return Mathf.PingPong(Time.time * 0.1f, 1f);
        }

        #endregion

        #region Event System Integration

        private void SubscribeToEvents()
        {
            if (G.Events == null)
            {
                Debug.LogWarning("[DayNightVolumeController] EventSystem not found. Time updates will not be received.", this);
                return;
            }

            // Subscribe to DayNightTimeChangedEvent
            G.Events.Subscribe<DayNightTimeChangedEvent>(OnTimeChanged);
        }

        private void UnsubscribeFromEvents()
        {
            if (G.Events == null)
                return;

            G.Events.Unsubscribe<DayNightTimeChangedEvent>(OnTimeChanged);
        }

        private void OnTimeChanged(DayNightTimeChangedEvent evt)
        {
            // Update volume with the new normalized time
            UpdateVolume(evt.NormalizedTime);
        }

        private void SubscribeToGameState()
        {
            if (G.GameState == null)
            {
                Debug.LogWarning("[DayNightVolumeController] GameStateManager not found. Will not receive game start events.", this);
                return;
            }

            G.GameState.OnStateChanged += OnGameStateChanged;
        }

        private void UnsubscribeFromGameState()
        {
            if (G.GameState == null)
                return;

            G.GameState.OnStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // Initialize volume when game starts (transition to Playing state)
            if (newState == GameState.Playing && !_isInitialized)
            {
                InitializeVolume();
            }
        }

        private void InitializeVolume()
        {
            if (_isInitialized)
                return;

            // Initial update with current time (if available)
            UpdateVolume(GetCurrentTimeNormalized());
            _isInitialized = true;
            
            Debug.Log("[DayNightVolumeController] Volume initialized (game started).");
        }

        #endregion

        #region G Class Registration

        private void RegisterWithG()
        {
            // Register this controller as the global day‑night visual effects controller
            if (G.DayNight == null)
                G.DayNight = this;
        }

        private void UnregisterFromG()
        {
            if (G.DayNight == this)
                G.DayNight = null;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Find Volume Automatically")]
        private void FindVolumeAutomatically()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null)
                _volume = gameObject.AddComponent<Volume>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfiguration()
        {
            CacheVolumeComponents();
            EnsureConfig();
            if (_config == null)
                Debug.LogError("[DayNightVolumeController] No DayNightConfig assigned.", this);
            if (_volume == null)
                Debug.LogError("[DayNightVolumeController] No Volume component found.", this);
        }
#endif

        #endregion
    }
}