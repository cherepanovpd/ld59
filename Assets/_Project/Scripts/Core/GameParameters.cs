// Path: Assets/_Project/Scripts/Core/GameParameters.cs

using UnityEngine;

namespace Core
{
    /// <summary>
    /// ScriptableObject configuration for game-wide parameters, especially signal scheduling.
    /// Contains signal periodicity, count, and delay settings.
    /// Registers itself into G.GameParameters on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "GameParameters", menuName = "Game/Game Parameters", order = 101)]
    public class GameParameters : ScriptableObject
    {
        [Header("Signal Scheduling")]
        [SerializeField, Tooltip("Time between consecutive signal bursts (seconds).")]
        [Min(0.1f)]
        private float _signalPeriodicity = 5.0f;

        [SerializeField, Tooltip("Number of signals emitted per burst.")]
        [Min(1)]
        private int _signalCount = 3;

        [SerializeField, Tooltip("Delay between individual signals within a burst (seconds).")]
        [Min(0f)]
        private float _signalDelay = 1.0f;

        [Space]
        [Header("Optional Settings")]
        [SerializeField, Tooltip("If true, signals are emitted continuously; otherwise only when triggered.")]
        private bool _autoEmitSignals = true;

        /// <summary>
        /// Time between consecutive signal bursts (seconds).
        /// </summary>
        public float SignalPeriodicity => _signalPeriodicity;

        /// <summary>
        /// Number of signals emitted per burst.
        /// </summary>
        public int SignalCount => _signalCount;

        /// <summary>
        /// Delay between individual signals within a burst (seconds).
        /// </summary>
        public float SignalDelay => _signalDelay;

        /// <summary>
        /// If true, signals are emitted continuously; otherwise only when triggered.
        /// </summary>
        public bool AutoEmitSignals => _autoEmitSignals;

        #region Self-Registration

        private void OnEnable()
        {
            // Register this instance as the global game parameters if not already set
            if (G.GameParameters == null)
            {
                G.GameParameters = this;
                G.EnsureSystem(nameof(GameParameters), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.GameParameters == this)
                G.GameParameters = null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get total duration of a full signal burst (including delays).
        /// </summary>
        public float GetBurstDuration()
        {
            if (_signalCount <= 1)
                return 0f;
            return (_signalCount - 1) * _signalDelay;
        }

        /// <summary>
        /// Get the time between the start of one burst and the start of the next.
        /// </summary>
        public float GetCycleDuration()
        {
            return _signalPeriodicity;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            _signalPeriodicity = 5.0f;
            _signalCount = 3;
            _signalDelay = 1.0f;
            _autoEmitSignals = true;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion
    }
}