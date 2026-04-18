// Path: Assets/_Project/Scripts/DayNight/Examples/DayNightEventDemo.cs

using Core;
using Project.Core.Events;
using Project.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Project.DayNight.Examples
{
    /// <summary>
    /// Demonstration component that listens to day/night cycle events.
    /// Shows how to subscribe/unsubscribe and log event data.
    /// Useful for debugging and documentation.
    /// </summary>
    public class DayNightEventDemo : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        [Tooltip("If true, logs event details to the Console.")]
        private bool _enableLogging = true;

        [SerializeField]
        [Tooltip("If true, also logs the normalized time and hour on every time change (may be spammy).")]
        private bool _logTimeChanges = true;

        [Header("UI Display (Optional)")]
        [SerializeField]
        [Tooltip("Optional UI Text component to display the last received event.")]
        private Text _uiText;

        [SerializeField]
        [Tooltip("Maximum number of lines to keep in the UI log.")]
        private int _maxUILines = 5;

        // State
        private int _daysPassed;
        private string _lastEventLog = "No events yet.";
        private System.Collections.Generic.Queue<string> _uiLogLines = new System.Collections.Generic.Queue<string>();

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToEvents();
            AddLog("DayNightEventDemo enabled and subscribed.");
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            AddLog("DayNightEventDemo disabled and unsubscribed.");
        }

        private void OnDestroy()
        {
            // Safety cleanup
            UnsubscribeFromEvents();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            var eventSystem = G.Events;
            if (eventSystem == null)
            {
                Debug.LogWarning("[DayNightEventDemo] G.Events is null. Cannot subscribe.");
                return;
            }

            // Subscribe to all three day/night events
            eventSystem.Subscribe<DayNightTimeChangedEvent>(OnTimeChanged, owner: this);
            eventSystem.Subscribe<DayEndEvent>(OnDayEnd, owner: this);
            eventSystem.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged, owner: this);

            if (_enableLogging)
                Debug.Log("[DayNightEventDemo] Subscribed to day/night events.");
        }

        private void UnsubscribeFromEvents()
        {
            var eventSystem = G.Events;
            if (eventSystem == null)
                return;

            // Unsubscribe using the owner parameter (EventSystem.UnsubscribeAll will handle it)
            eventSystem.UnsubscribeAll(this);

            if (_enableLogging)
                Debug.Log("[DayNightEventDemo] Unsubscribed from day/night events.");
        }

        #endregion

        #region Event Handlers

        private void OnTimeChanged(DayNightTimeChangedEvent evt)
        {
            _lastEventLog = $"Time changed: {evt.NormalizedTime:F3} (Hour {evt.Hour:F2})";

            if (_enableLogging && _logTimeChanges)
                Debug.Log($"[DayNightEventDemo] {_lastEventLog}");

            UpdateUI($"🕒 {evt.Hour:F2}h ({evt.NormalizedTime:F3})");
        }

        private void OnDayEnd(DayEndEvent evt)
        {
            _daysPassed = evt.DayCount;
            _lastEventLog = $"Day ended! Total days: {_daysPassed}";

            if (_enableLogging)
                Debug.Log($"[DayNightEventDemo] {_lastEventLog}");

            UpdateUI($"🌅 Day {_daysPassed} ended");
        }

        private void OnPhaseChanged(DayPhaseChangedEvent evt)
        {
            string phaseName = evt.NewPhase.ToString();
            _lastEventLog = $"Phase changed to {phaseName}";

            if (_enableLogging)
                Debug.Log($"[DayNightEventDemo] {_lastEventLog}");

            UpdateUI($"🌙 {phaseName}");
        }

        #endregion

        #region UI Helpers

        private void AddLog(string message)
        {
            if (!_enableLogging)
                return;

            Debug.Log($"[DayNightEventDemo] {message}");
        }

        private void UpdateUI(string line)
        {
            if (_uiText == null)
                return;

            // Add new line to the queue
            _uiLogLines.Enqueue($"[{System.DateTime.Now:HH:mm:ss}] {line}");
            while (_uiLogLines.Count > _maxUILines)
                _uiLogLines.Dequeue();

            // Build display text
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Day/Night Event Demo");
            sb.AppendLine("────────────────────");
            foreach (var logLine in _uiLogLines)
                sb.AppendLine(logLine);
            sb.AppendLine();
            sb.AppendLine($"Last event: {_lastEventLog}");
            sb.AppendLine($"Days passed: {_daysPassed}");

            _uiText.text = sb.ToString();
        }

        #endregion

        #region Public API (for testing)

        /// <summary>
        /// Returns the total number of day‑end events received.
        /// </summary>
        public int GetDaysPassed() => _daysPassed;

        /// <summary>
        /// Returns the last event log string.
        /// </summary>
        public string GetLastEventLog() => _lastEventLog;

        /// <summary>
        /// Clears the UI log lines.
        /// </summary>
        public void ClearUILog()
        {
            _uiLogLines.Clear();
            if (_uiText != null)
                _uiText.text = "Day/Night Event Demo\n────────────────────\nLog cleared.";
        }

        #endregion
    }
}