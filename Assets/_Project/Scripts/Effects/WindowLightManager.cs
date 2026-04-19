// Path: Assets/_Project/Scripts/Effects/WindowLightManager.cs

using System.Collections.Generic;
using Core;
using Project.Core.Events;
using Project.Utilities;
using UnityEngine;

namespace Project.Effects
{
    /// <summary>
    /// Central manager that coordinates all window lighting based on time.
    /// Maintains a list of registered WindowLightController instances and updates them
    /// when the in‑game time changes.
    /// Registers itself in the G service locator and subscribes to DayNightTimeChangedEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public class WindowLightManager : MonoBehaviour
    {
        [Header("Performance")]
        [SerializeField, Tooltip("If true, windows are updated every time change event. If false, only when hour changes by at least this threshold.")]
        private bool _updateEveryEvent = true;

        [SerializeField, Tooltip("Hour change threshold for updates when _updateEveryEvent is false.")]
        private float _hourChangeThreshold = 0.1f;

        // Registered windows
        private readonly List<WindowLightController> _windows = new List<WindowLightController>();
        private float _lastProcessedHour = -1f;
        private int _previousLitCount = -1; // Tracks how many windows were lit in the previous update

        #region Unity Lifecycle

        private void Awake()
        {
            RegisterWithG();
        }

        private void Start()
        {
            SubscribeToTimeEvents();
            // Initial update if DayNightSystem is already available
            if (G.HasDayNightSystem())
            {
                UpdateAllWindows(G.DayNightSystem.Hour24);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromTimeEvents();
            UnregisterFromG();
            ClearWindows();
        }

        #endregion

        #region Public API (Registration)

        /// <summary>
        /// Register a window controller to receive time‑based updates.
        /// Called by WindowLightController.OnEnable().
        /// </summary>
        public void RegisterWindow(WindowLightController window)
        {
            if (window == null)
                return;

            if (!_windows.Contains(window))
            {
                _windows.Add(window);
                // Immediately update the window if we already have a valid hour
                if (G.HasDayNightSystem())
                {
                    window.UpdateLightBasedOnTime(G.DayNightSystem.Hour24);
                }
            }
        }

        /// <summary>
        /// Unregister a window controller.
        /// Called by WindowLightController.OnDisable().
        /// </summary>
        public void UnregisterWindow(WindowLightController window)
        {
            if (window == null)
                return;

            _windows.Remove(window);
        }

        /// <summary>
        /// Returns the number of currently registered windows.
        /// </summary>
        public int GetWindowCount() => _windows.Count;

        #endregion

        #region Time Event Handling

        private void SubscribeToTimeEvents()
        {
            if (G.HasEvents())
            {
                G.Events.Subscribe<DayNightTimeChangedEvent>(OnTimeChanged, owner: this);
            }
            else
            {
                Debug.LogWarning("[WindowLightManager] EventSystem not available. Will not receive time updates.", this);
            }
        }

        private void UnsubscribeFromTimeEvents()
        {
            if (G.HasEvents())
            {
                G.Events.UnsubscribeAll(this);
            }
        }

        private void OnTimeChanged(DayNightTimeChangedEvent evt)
        {
            if (!_updateEveryEvent)
            {
                // Only update if the hour changed beyond the threshold
                if (Mathf.Abs(evt.Hour - _lastProcessedHour) < _hourChangeThreshold)
                    return;
            }

            UpdateAllWindows(evt.Hour);
            _lastProcessedHour = evt.Hour;
        }

        #endregion

        #region Batch Update

        /// <summary>
        /// Update all registered windows with the current hour.
        /// Zero‑allocation iteration; windows may be null (destroyed) and are skipped.
        /// </summary>
        private void UpdateAllWindows(float currentHour)
        {
            int litCount = 0;
            
            // Iterate backwards to allow safe removal if we ever decide to auto‑remove nulls
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                WindowLightController window = _windows[i];
                if (window == null)
                {
                    // Clean up null entries (should be rare; windows unregister themselves on disable)
                    _windows.RemoveAt(i);
                    continue;
                }

                window.UpdateLightBasedOnTime(currentHour);
                
                // Count lit windows
                if (window.IsLightOn)
                    litCount++;
            }
            
            // Emit event if lighting state changed
            if (litCount != _previousLitCount)
            {
                EmitWindowLightsChangedEvent(currentHour, litCount);
                _previousLitCount = litCount;
            }
        }

        /// <summary>
        /// Emits a WindowLightsChangedEvent when the overall lighting state changes.
        /// </summary>
        /// <param name="currentHour">Current hour when the change occurred.</param>
        /// <param name="litCount">Number of windows currently lit.</param>
        private void EmitWindowLightsChangedEvent(float currentHour, int litCount)
        {
            if (!G.HasEvents())
                return;

            bool lightsOn = litCount > 0;
            var evt = new WindowLightsChangedEvent(lightsOn, currentHour, litCount);
            G.Events.Trigger(evt);
            
            #if UNITY_EDITOR
            Debug.Log($"[WindowLightManager] Window lights changed: LightsOn={lightsOn}, LitCount={litCount}, Hour={currentHour:F2}");
            #endif
        }

        /// <summary>
        /// Force an immediate update of all windows using the current hour from DayNightSystem.
        /// If DayNightSystem is not available, logs a warning and does nothing.
        /// </summary>
        public void ForceUpdateAllWindows()
        {
            if (!G.HasDayNightSystem())
            {
                Debug.LogWarning("[WindowLightManager] Cannot force update: DayNightSystem not available.", this);
                return;
            }

            UpdateAllWindows(G.DayNightSystem.Hour24);
        }

        #endregion

        #region G Service Locator Integration

        private void RegisterWithG()
        {
            if (G.WindowLightManager == null)
            {
                G.WindowLightManager = this;
                G.EnsureSystem(nameof(WindowLightManager), this);
            }
            else
            {
                Debug.LogWarning("[WindowLightManager] G.WindowLightManager already occupied by another instance. Not registering.", this);
            }
        }

        private void UnregisterFromG()
        {
            if (G.WindowLightManager == this)
                G.WindowLightManager = null;
        }

        #endregion

        #region Cleanup

        private void ClearWindows()
        {
            _windows.Clear();
        }

        #endregion

        #region Editor Only

#if UNITY_EDITOR
        private void OnValidate()
        {
            _hourChangeThreshold = Mathf.Max(0f, _hourChangeThreshold);
        }
#endif

        #endregion
    }
}