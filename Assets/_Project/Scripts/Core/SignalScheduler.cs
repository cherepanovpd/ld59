// Path: Assets/_Project/Scripts/Core/SignalScheduler.cs

using Core;
using Project.Tower;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Manages periodic signal bursts according to GameParameters.
    /// Uses two timers: periodicity (between bursts) and burst (between signals within a burst).
    /// Follows zero‑allocation principles in Update loops.
    /// </summary>
    [RequireComponent(typeof(Tower))]
    public class SignalScheduler : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField, Tooltip("Reference to the Tower component. Auto‑fetched if not assigned.")]
        private Tower _tower;

        [Header("Configuration")]
        [SerializeField, Tooltip("If true, signals are emitted automatically according to GameParameters. If false, you must trigger bursts manually.")]
        private bool _autoEmit = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Current scheduler state.")]
        private State _currentState = State.Idle;

        [SerializeField, Tooltip("Time elapsed since last burst start.")]
        private float _periodicityTimer;

        [SerializeField, Tooltip("Time elapsed since last signal within the current burst.")]
        private float _burstTimer;

        [SerializeField, Tooltip("Number of signals already emitted in the current burst.")]
        private int _signalsEmittedInBurst;

        // Cached configuration
        private GameParameters _gameParameters;
        private bool _hasValidConfiguration;
        private bool _hasTower;
        private int _signalCount;
        private float _signalDelay;
        private float _signalPeriodicity;

        /// <summary>
        /// Scheduler internal state.
        /// </summary>
        private enum State
        {
            Idle,
            InBurst
        }

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            ValidateConfiguration();
            SelfRegister();
        }

        private void Start()
        {
            // If auto‑emit is enabled, start the periodicity timer immediately
            if (_autoEmit && _hasValidConfiguration)
            {
                ResetPeriodicityTimer();
            }
        }

        private void Update()
        {
            if (!_hasValidConfiguration) return;
            if (!_autoEmit) return;

            float deltaTime = Time.deltaTime;

            switch (_currentState)
            {
                case State.Idle:
                    UpdateIdle(deltaTime);
                    break;
                case State.InBurst:
                    UpdateBurst(deltaTime);
                    break;
            }
        }

        private void OnDestroy()
        {
            SelfUnregister();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Immediately trigger a signal burst, regardless of auto‑emit and periodicity timer.
        /// If already in a burst, this will restart it.
        /// </summary>
        public void TriggerBurst()
        {
            if (!_hasValidConfiguration) return;

            _currentState = State.InBurst;
            _burstTimer = 0f;
            _signalsEmittedInBurst = 0;

            // Emit the first signal immediately
            EmitSignal();
        }

        /// <summary>
        /// Stop any ongoing burst and return to idle.
        /// </summary>
        public void StopBurst()
        {
            _currentState = State.Idle;
            ResetPeriodicityTimer();
        }

        /// <summary>
        /// Enable or disable automatic signal emission.
        /// </summary>
        public void SetAutoEmit(bool enable)
        {
            _autoEmit = enable;
            if (!enable)
            {
                StopBurst();
            }
            else if (_hasValidConfiguration)
            {
                ResetPeriodicityTimer();
            }
        }

        #endregion

        #region Private Logic

        private void UpdateIdle(float deltaTime)
        {
            _periodicityTimer += deltaTime;

            if (_periodicityTimer >= _signalPeriodicity)
            {
                StartBurst();
            }
        }

        private void UpdateBurst(float deltaTime)
        {
            _burstTimer += deltaTime;

            if (_burstTimer >= _signalDelay)
            {
                if (_signalsEmittedInBurst >= _signalCount)
                {
                    FinishBurst();
                    return;
                }
                
                EmitSignal();
                _burstTimer = 0f;
                _signalsEmittedInBurst++;
            }
        }

        private void StartBurst()
        {
            _currentState = State.InBurst;
            _burstTimer = 0f;
            _signalsEmittedInBurst = 1;

            EmitSignal();
        }

        private void FinishBurst()
        {
            _currentState = State.Idle;
            ResetPeriodicityTimer();
        }

        private void EmitSignal()
        {
            if (!_hasTower)
            {
                // Only log in editor to avoid allocations in builds
#if UNITY_EDITOR
                Debug.LogWarning($"{nameof(SignalScheduler)}: Tower reference is missing. Cannot emit signal.", this);
#endif
                return;
            }

            _tower.TriggerBlinkWithRing();
        }

        private void ResetPeriodicityTimer()
        {
            _periodicityTimer = 0f;
        }

        #endregion

        #region Configuration & Validation

        private void CacheComponents()
        {
            if (_tower == null)
            {
                _tower = GetComponent<Tower>();
            }

            _gameParameters = G.GameParameters;
        }

        private void ValidateConfiguration()
        {
            _hasTower = _tower != null;
            _hasValidConfiguration = _hasTower && _gameParameters != null;

            if (!_hasValidConfiguration)
            {
                Debug.LogWarning($"{nameof(SignalScheduler)}: Missing required dependencies. " +
                    $"Tower: {_tower}, GameParameters: {_gameParameters}.", this);
                return;
            }

            // Cache parameters with safe defaults
            _signalCount = Mathf.Max(1, _gameParameters.SignalCount);
            _signalDelay = Mathf.Max(0f, _gameParameters.SignalDelay);
            _signalPeriodicity = Mathf.Max(0.1f, _gameParameters.SignalPeriodicity);
        }

        private void SelfRegister()
        {
            if (G.SignalScheduler != null && G.SignalScheduler != this)
            {
                Debug.LogWarning($"[{nameof(SignalScheduler)}] Multiple instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            G.SignalScheduler = this;
            G.EnsureSystem(nameof(SignalScheduler), G.SignalScheduler);
        }

        private void SelfUnregister()
        {
            if (G.SignalScheduler == this)
            {
                G.SignalScheduler = null;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tower == null)
            {
                _tower = GetComponent<Tower>();
            }
        }

        [ContextMenu("Test Trigger Burst")]
        private void TestTriggerBurst()
        {
            TriggerBurst();
        }

        [ContextMenu("Test Stop Burst")]
        private void TestStopBurst()
        {
            StopBurst();
        }
#endif

        #endregion
    }
}