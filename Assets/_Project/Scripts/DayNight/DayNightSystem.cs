// Path: Assets/_Project/Scripts/DayNight/DayNightSystem.cs

using Core;
using Project.Core;
using Project.Core.Events;
using Project.DayNight.Config;
using Project.Utilities;
using UnityEngine;

namespace Project.DayNight
{
    /// <summary>
    /// Core MonoBehaviour that tracks in‑game time and fires time‑change events.
    /// Zero allocations in Update loop, respects game pause, configurable day length.
    /// Registers itself as G.DayNightSystem.
    /// </summary>
    public class DayNightSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Optional DayNightConfig ScriptableObject. If null, will use G.DayNightConfig.")]
        private DayNightConfig _config;

        [SerializeField]
        [Tooltip("Manual override for day length (seconds per full cycle). If zero, uses config value.")]
        private float _dayLengthSecondsOverride;

        [SerializeField]
        [Tooltip("Manual override for start time (normalized 0‑1). If negative, uses config value.")]
        [Range(-0.1f, 1f)]
        private float _startTimeNormalizedOverride = -0.1f;

        [Header("Time Control")]
        [SerializeField]
        [Tooltip("Time scale multiplier (1 = normal speed).")]
        private float _timeScale = 1f;

        [SerializeField]
        [Tooltip("If true, time progression is paused regardless of game state.")]
        private bool _manuallyPaused;

        // Time state
        private float _normalizedTime;          // 0‑1
        private int _dayCount;                  // total days passed
        private DayPhaseChangedEvent.Phase _currentPhase;

        // Event objects (reused to avoid allocations)
        private DayNightTimeChangedEvent _cachedTimeChangedEvent;
        private DayEndEvent _cachedDayEndEvent;
        private DayPhaseChangedEvent _cachedPhaseChangedEvent;

        // Performance caches
        private float _lastHourEmitted = -1f;
        private float _lastNormalizedTimeEmitted = -1f;
        private const float TimeEmitThreshold = 0.001f;

        #region Public Properties

        /// <summary>
        /// Current normalized time (0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk).
        /// </summary>
        public float NormalizedTime => _normalizedTime;

        /// <summary>
        /// Current time of day in 24‑hour format (0‑24).
        /// </summary>
        public float Hour24 => _normalizedTime * 24f;

        /// <summary>
        /// Total number of full day cycles completed since start.
        /// </summary>
        public int DayCount => _dayCount;

        /// <summary>
        /// Current day phase (Dawn, Day, Dusk, Night).
        /// </summary>
        public DayPhaseChangedEvent.Phase CurrentPhase => _currentPhase;

        /// <summary>
        /// Whether time progression is paused (by manual toggle or game state).
        /// </summary>
        public bool IsPaused => _manuallyPaused || !IsGamePlaying();

        /// <summary>
        /// Time scale multiplier (1 = normal speed). Does not affect pause state.
        /// </summary>
        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Real‑time seconds per full day cycle (read‑only).
        /// </summary>
        public float DayLengthSeconds { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
            DetermineConfiguration();
            InitializeTimeState();
            RegisterWithG();
        }

        private void Start()
        {
            SubscribeToGameState();
            FireInitialEvents();
        }

        private void Update()
        {
            if (IsPaused)
                return;

            float delta = Time.deltaTime * _timeScale;
            AdvanceTime(delta);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameState();
            UnregisterFromG();
        }

        #endregion

        #region Initialization

        private void CacheReferences()
        {
            G.GameState = G.GameState;
            G.Events = G.Events;
        }

        private void DetermineConfiguration()
        {
            // Use config if provided, otherwise fall back to G.DayNightConfig
            DayNightConfig config = _config != null ? _config : G.DayNightConfig;

            if (config == null)
            {
                Debug.LogWarning("[DayNightSystem] No DayNightConfig assigned. Using default values.");
                DayLengthSeconds = _dayLengthSecondsOverride > 0 ? _dayLengthSecondsOverride : 300f;
                _normalizedTime = _startTimeNormalizedOverride >= 0 ? _startTimeNormalizedOverride : 0.25f;
            }
            else
            {
                DayLengthSeconds = _dayLengthSecondsOverride > 0 ? _dayLengthSecondsOverride : config.DayLengthSeconds;
                _normalizedTime = _startTimeNormalizedOverride >= 0 ? _startTimeNormalizedOverride : config.StartTimeNormalized;
            }

            // Clamp normalized time
            _normalizedTime = Mathf.Clamp01(_normalizedTime);
        }

        private void InitializeTimeState()
        {
            _dayCount = 0;
            _currentPhase = CalculatePhase(_normalizedTime);

            // Pre‑allocate event objects (zero allocations in Update)
            _cachedTimeChangedEvent = new DayNightTimeChangedEvent(_normalizedTime, Hour24);
            _cachedDayEndEvent = new DayEndEvent(_dayCount);
            _cachedPhaseChangedEvent = new DayPhaseChangedEvent(_currentPhase);
        }

        private void RegisterWithG()
        {
            // Register as G.DayNightSystem (core time management system)
            if (G.DayNightSystem == null)
            {
                G.DayNightSystem = this;
                G.EnsureSystem(nameof(DayNightSystem), this);
            }
            else
            {
                Debug.LogWarning("[DayNightSystem] G.DayNightSystem already occupied by another instance. Not registering.", this);
            }
        }

        private void UnregisterFromG()
        {
            if (G.DayNightSystem == this)
                G.DayNightSystem = null;
        }

        #endregion

        #region Time Progression

        private void AdvanceTime(float deltaSeconds)
        {
            if (DayLengthSeconds <= 0)
                return;

            float deltaNormalized = deltaSeconds / DayLengthSeconds;
            float newTime = _normalizedTime + deltaNormalized;

            // Detect day wrap
            if (newTime >= 1f)
            {
                newTime %= 1f;
                _dayCount++;
                FireDayEndEvent();
            }

            // Update internal state
            _normalizedTime = newTime;

            // Emit time‑changed event if change exceeds threshold
            if (Mathf.Abs(_normalizedTime - _lastNormalizedTimeEmitted) >= TimeEmitThreshold)
            {
                FireTimeChangedEvent();
                _lastNormalizedTimeEmitted = _normalizedTime;
            }

            // Check phase change
            DayPhaseChangedEvent.Phase newPhase = CalculatePhase(_normalizedTime);
            if (newPhase != _currentPhase)
            {
                _currentPhase = newPhase;
                FirePhaseChangedEvent();
            }
        }

        private DayPhaseChangedEvent.Phase CalculatePhase(float normalizedTime)
        {
            // Phase boundaries (matching DayPhaseChangedEvent.Phase documentation)
            if (normalizedTime >= 0.2f && normalizedTime < 0.3f)
                return DayPhaseChangedEvent.Phase.Dawn;
            if (normalizedTime >= 0.3f && normalizedTime < 0.7f)
                return DayPhaseChangedEvent.Phase.Day;
            if (normalizedTime >= 0.7f && normalizedTime < 0.8f)
                return DayPhaseChangedEvent.Phase.Dusk;
            return DayPhaseChangedEvent.Phase.Night; // 0.8‑1.0 and 0.0‑0.2 (wrapping)
        }

        #endregion

        #region Event System Integration

        private void SubscribeToGameState()
        {
            if (G.GameState != null)
            {
                G.GameState.OnStateChanged += OnGameStateChanged;
            }
        }

        private void UnsubscribeFromGameState()
        {
            if (G.GameState != null)
            {
                G.GameState.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // Time automatically pauses/resumes based on IsPaused property
            // No need to do anything besides logging for debugging
            Debug.Log($"[DayNightSystem] Game state changed from {oldState} to {newState}. IsPaused = {IsPaused}");
        }

        private bool IsGamePlaying()
        {
            return G.GameState != null && G.GameState.CurrentState == GameState.Playing;
        }

        private void FireInitialEvents()
        {
            // Ensure listeners know the initial time and phase
            FireTimeChangedEvent();
            FirePhaseChangedEvent();
        }

        private void FireTimeChangedEvent()
        {
            if (G.Events == null)
                return;

            // Update cached event with current values
            _cachedTimeChangedEvent = new DayNightTimeChangedEvent(_normalizedTime, Hour24);
            G.Events.Trigger(_cachedTimeChangedEvent);
        }

        private void FireDayEndEvent()
        {
            if (G.Events == null)
                return;

            _cachedDayEndEvent = new DayEndEvent(_dayCount);
            G.Events.Trigger(_cachedDayEndEvent);
        }

        private void FirePhaseChangedEvent()
        {
            if (G.Events == null)
                return;

            _cachedPhaseChangedEvent = new DayPhaseChangedEvent(_currentPhase);
            G.Events.Trigger(_cachedPhaseChangedEvent);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the current normalized time (0‑1) and fire a time‑changed event.
        /// </summary>
        public void SetTimeNormalized(float normalizedTime)
        {
            float clamped = Mathf.Clamp01(normalizedTime);
            if (Mathf.Approximately(_normalizedTime, clamped))
                return;

            _normalizedTime = clamped;
            FireTimeChangedEvent();

            // Recalculate phase
            DayPhaseChangedEvent.Phase newPhase = CalculatePhase(_normalizedTime);
            if (newPhase != _currentPhase)
            {
                _currentPhase = newPhase;
                FirePhaseChangedEvent();
            }
        }

        /// <summary>
        /// Set the current time of day in 24‑hour format (0‑24).
        /// </summary>
        public void SetHour24(float hour)
        {
            float normalized = (hour % 24f) / 24f;
            SetTimeNormalized(normalized);
        }

        /// <summary>
        /// Advance time by a given amount of real‑time seconds (respects time scale).
        /// </summary>
        public void AdvanceTimeSeconds(float seconds)
        {
            if (IsPaused)
                return;

            AdvanceTime(seconds);
        }

        /// <summary>
        /// Pause time progression (manual override).
        /// </summary>
        public void Pause()
        {
            _manuallyPaused = true;
        }

        /// <summary>
        /// Resume time progression (manual override).
        /// </summary>
        public void Resume()
        {
            _manuallyPaused = false;
        }

        /// <summary>
        /// Toggle manual pause state.
        /// </summary>
        public void TogglePause()
        {
            _manuallyPaused = !_manuallyPaused;
        }

        /// <summary>
        /// Reset time to start configuration (keeps day count).
        /// </summary>
        public void ResetTime()
        {
            DetermineConfiguration();
            FireTimeChangedEvent();
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Log Current Time")]
        private void LogCurrentTime()
        {
            Debug.Log($"[DayNightSystem] Normalized: {_normalizedTime:F3}, Hour: {Hour24:F1}, Day: {_dayCount}, Phase: {_currentPhase}");
        }
#endif

        #endregion
    }
}