// Path: Assets/_Project/Scripts/DayNight/UI/ClockDisplay.cs

using Core;

using Project.Core.Events;
using Project.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.DayNight.UI
{
    /// <summary>
    /// Displays in‑game time as either an analog clock (rotating hands) or a digital clock (text).
    /// Demonstrates how to use DayNightSystem time parameters for clock sprite integration.
    /// Zero allocations in Update loop, supports event‑based updates for efficiency.
    /// </summary>
    public class ClockDisplay : MonoBehaviour
    {
        #region Configuration Enums

        public enum DisplayMode
        {
            Analog,
            Digital,
            Both
        }

        public enum UpdateFrequency
        {
            EveryFrame,
            OnTimeChangeEvent,
            Manual
        }

        public enum TimeFormat
        {
            Hour24,
            Hour12WithAmPm
        }

        #endregion

        #region Static Caches (Zero Allocation)

        private static readonly string[] s_cachedHourStrings = new string[24];
        private static readonly string[] s_cachedMinuteStrings = new string[60];
        private static readonly string[] s_cachedHour12Strings = new string[13]; // 1-12

        static ClockDisplay()
        {
            // Pre‑generate formatted strings for hours (00‑23) and minutes (00‑59)
            for (int i = 0; i < 24; i++)
            {
                s_cachedHourStrings[i] = i.ToString("00");
            }

            for (int i = 0; i < 60; i++)
            {
                s_cachedMinuteStrings[i] = i.ToString("00");
            }

            // 12‑hour format strings (1‑12)
            for (int i = 0; i <= 12; i++)
            {
                s_cachedHour12Strings[i] = i == 0 ? "12" : i.ToString();
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField]
        [Tooltip("Whether to show analog hands, digital text, or both.")]
        private DisplayMode _displayMode = DisplayMode.Analog;

        [SerializeField]
        [Tooltip("How often the clock should update its display.")]
        private UpdateFrequency _updateFrequency = UpdateFrequency.OnTimeChangeEvent;

        [SerializeField]
        [Tooltip("For digital display: 24‑hour format (14:30) or 12‑hour with AM/PM (2:30 PM).")]
        private TimeFormat _timeFormat = TimeFormat.Hour24;

        [Header("Analog Clock References")]
        [SerializeField]
        [Tooltip("Transform representing the hour hand. Will be rotated around its local Z axis.")]
        private Transform _hourHandTransform;

        [SerializeField]
        [Tooltip("Transform representing the minute hand. Will be rotated around its local Z axis.")]
        private Transform _minuteHandTransform;

        [SerializeField]
        [Tooltip("Optional second hand transform (if present).")]
        private Transform _secondHandTransform;

        [Header("Digital Clock References")]
        [SerializeField]
        [Tooltip("TextMeshPro component to display digital time. If using Unity UI Text, assign a Text component and we'll fall back.")]
        private TMP_Text _digitalText;

        [SerializeField]
        [Tooltip("Optional TextMeshPro component for AM/PM indicator (used only in 12‑hour format).")]
        private TMP_Text _amPmText;

        // Fallback for Unity UI Text (legacy support)
        private Text _legacyDigitalText;
        private Text _legacyAmPmText;

        [Header("Advanced")]
        [SerializeField]
        [Tooltip("If true, analog hour hand moves smoothly (continuous rotation). If false, snaps to discrete hours.")]
        private bool _smoothAnalogHands = true;

        [SerializeField]
        [Tooltip("Manual override for testing: set a specific normalized time (0‑1). Only works in Manual update frequency.")]
        [Range(0f, 1f)]
        private float _manualNormalizedTime = 0.25f;

        #endregion

        #region Private Fields

        // Digital display caching
        private System.Text.StringBuilder _stringBuilder;
        private string _cachedTimeString;
        private string _cachedAmPmString;
        private int _lastHour = -1;
        private int _lastMinute = -1;
        private bool _lastWasPm = false;

        // Performance flags
        private bool _isSubscribedToEvents;
        private bool _hasAnalogComponents;
        private bool _hasDigitalComponents;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateComponents();
            InitializeStringBuilder();
        }

        private void Start()
        {
            // Ensure we have a DayNightSystem reference
            if (G.DayNightSystem == null)
            {
                Debug.LogWarning("[ClockDisplay] DayNightSystem not found in G. Clock will not update.", this);
                return;
            }

            // Subscribe to events if needed
            if (_updateFrequency == UpdateFrequency.OnTimeChangeEvent)
            {
                SubscribeToEvents();
            }

            // Perform initial update
            UpdateDisplay(G.DayNightSystem.NormalizedTime, G.DayNightSystem.Hour24);
        }

        private void Update()
        {
            if (_updateFrequency != UpdateFrequency.EveryFrame)
                return;

            if (G.DayNightSystem == null)
                return;

            UpdateDisplay(G.DayNightSystem.NormalizedTime, G.DayNightSystem.Hour24);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void ValidateComponents()
        {
            _hasAnalogComponents = _hourHandTransform != null && _minuteHandTransform != null;
            
            // Check for digital text components (TMP_Text or legacy Text)
            bool hasTmpDigital = _digitalText != null;
            bool hasLegacyDigital = false;
            
            // If TMP_Text is null, try to find a legacy Text component on the same GameObject
            if (!hasTmpDigital && _digitalText == null)
            {
                _legacyDigitalText = GetComponent<Text>();
                if (_legacyDigitalText == null && transform.parent != null)
                    _legacyDigitalText = transform.parent.GetComponentInChildren<Text>();
                hasLegacyDigital = _legacyDigitalText != null;
            }
            
            _hasDigitalComponents = hasTmpDigital || hasLegacyDigital;

            // Similarly for AM/PM text
            if (_amPmText == null)
            {
                _legacyAmPmText = GetComponentInChildren<Text>(true);
                // Could be a separate search but we'll keep it simple
            }

            if (_displayMode == DisplayMode.Analog && !_hasAnalogComponents)
            {
                Debug.LogError("[ClockDisplay] Analog mode selected but hour/minute hand transforms are not assigned.", this);
            }

            if (_displayMode == DisplayMode.Digital && !_hasDigitalComponents)
            {
                Debug.LogError("[ClockDisplay] Digital mode selected but digital text component is not assigned.", this);
            }

            if (_displayMode == DisplayMode.Both && (!_hasAnalogComponents || !_hasDigitalComponents))
            {
                Debug.LogWarning("[ClockDisplay] Both mode selected but some components are missing.", this);
            }
        }

        private void InitializeStringBuilder()
        {
            _stringBuilder = new System.Text.StringBuilder(16); // Enough for "14:30" or "2:30 PM"
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            if (G.Events == null || _isSubscribedToEvents)
                return;

            G.Events.Subscribe<DayNightTimeChangedEvent>(OnTimeChanged, owner: this);
            _isSubscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (G.Events == null || !_isSubscribedToEvents)
                return;

            G.Events.Unsubscribe<DayNightTimeChangedEvent>(OnTimeChanged);
            _isSubscribedToEvents = false;
        }

        private void OnTimeChanged(DayNightTimeChangedEvent evt)
        {
            UpdateDisplay(evt.NormalizedTime, evt.Hour);
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Public method to manually trigger a display update with custom time values.
        /// Useful for testing or when using Manual update frequency.
        /// </summary>
        /// <param name="normalizedTime">Normalized time (0‑1)</param>
        /// <param name="hour24">Hour in 24‑hour format (0‑24)</param>
        public void UpdateDisplay(float normalizedTime, float hour24)
        {
            if (_updateFrequency == UpdateFrequency.Manual)
            {
                // Use manual override if set
                normalizedTime = _manualNormalizedTime;
                hour24 = normalizedTime * 24f;
            }

            switch (_displayMode)
            {
                case DisplayMode.Analog:
                    UpdateAnalogDisplay(normalizedTime, hour24);
                    break;
                case DisplayMode.Digital:
                    UpdateDigitalDisplay(hour24);
                    break;
                case DisplayMode.Both:
                    UpdateAnalogDisplay(normalizedTime, hour24);
                    UpdateDigitalDisplay(hour24);
                    break;
            }
        }

        private void UpdateAnalogDisplay(float normalizedTime, float hour24)
        {
            if (!_hasAnalogComponents)
                return;

            // Convert to 12‑hour format for traditional analog clock
            float hour12 = hour24 % 12f;
            float minutes = (hour24 % 1f) * 60f;
            float seconds = (minutes % 1f) * 60f;

            // Calculate rotations with proper smooth progression
            // Hour hand: 360° per 12 hours, but also moves with minutes (0.5° per minute)
            // Total minutes in 12 hours = 720
            float hourAngle = ((hour12 * 60f) + minutes) / 720f * 360f;

            // Minute hand: 360° per 60 minutes, also moves with seconds
            float minuteAngle = minutes / 60f * 360f;

            // Second hand: 360° per 60 seconds
            float secondAngle = seconds / 60f * 360f;

            // Apply rotations (negative for clockwise rotation)
            if (_smoothAnalogHands)
            {
                // Continuous rotation (smooth movement)
                _hourHandTransform.localRotation = Quaternion.Euler(0f, 0f, -hourAngle);
                _minuteHandTransform.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle);
                if (_secondHandTransform != null)
                    _secondHandTransform.localRotation = Quaternion.Euler(0f, 0f, -secondAngle);
            }
            else
            {
                // Discrete steps (snap to whole minutes/hours)
                int discreteHour = Mathf.FloorToInt(hour12);
                int discreteMinute = Mathf.FloorToInt(minutes);
                int discreteSecond = Mathf.FloorToInt(seconds);

                // Recalculate angles for discrete positions
                float discreteHourAngle = ((discreteHour * 60f) + discreteMinute) / 720f * 360f;
                float discreteMinuteAngle = discreteMinute / 60f * 360f;
                float discreteSecondAngle = discreteSecond / 60f * 360f;

                _hourHandTransform.localRotation = Quaternion.Euler(0f, 0f, -discreteHourAngle);
                _minuteHandTransform.localRotation = Quaternion.Euler(0f, 0f, -discreteMinuteAngle);
                if (_secondHandTransform != null)
                    _secondHandTransform.localRotation = Quaternion.Euler(0f, 0f, -discreteSecondAngle);
            }
        }

        private void SetDigitalText(string text)
        {
            if (_digitalText != null)
            {
                _digitalText.text = text;
            }
            else if (_legacyDigitalText != null)
            {
                _legacyDigitalText.text = text;
            }
        }

        private void SetAmPmText(string text)
        {
            if (_amPmText != null)
            {
                _amPmText.text = text;
            }
            else if (_legacyAmPmText != null)
            {
                _legacyAmPmText.text = text;
            }
        }

        private void UpdateDigitalDisplay(float hour24)
        {
            if (!_hasDigitalComponents)
                return;

            // Extract hour and minute
            int hour = Mathf.FloorToInt(hour24);
            int minute = Mathf.FloorToInt((hour24 % 1f) * 60f);

            // Check if we need to update the string (avoid unnecessary allocations)
            bool hourChanged = hour != _lastHour;
            bool minuteChanged = minute != _lastMinute;
            bool formatChanged = false;

            if (!hourChanged && !minuteChanged && !formatChanged)
                return; // No change, skip update

            _lastHour = hour;
            _lastMinute = minute;

            // Build time string based on format (zero allocation using cached strings)
            _stringBuilder.Clear();

            if (_timeFormat == TimeFormat.Hour24)
            {
                // 24‑hour format: "14:30"
                _stringBuilder.Append(s_cachedHourStrings[hour]);
                _stringBuilder.Append(':');
                _stringBuilder.Append(s_cachedMinuteStrings[minute]);
                _cachedAmPmString = string.Empty;
            }
            else
            {
                // 12‑hour format with AM/PM: "2:30 PM"
                bool isPm = hour >= 12;
                int hour12 = hour % 12; // 0‑11

                _stringBuilder.Append(s_cachedHour12Strings[hour12]);
                _stringBuilder.Append(':');
                _stringBuilder.Append(s_cachedMinuteStrings[minute]);
                _stringBuilder.Append(' ');
                _stringBuilder.Append(isPm ? "PM" : "AM");

                _cachedAmPmString = isPm ? "PM" : "AM";
                _lastWasPm = isPm;
            }

            _cachedTimeString = _stringBuilder.ToString();

            // Update UI components
            SetDigitalText(_cachedTimeString);

            if (_timeFormat == TimeFormat.Hour12WithAmPm)
            {
                SetAmPmText(_cachedAmPmString);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Switch display mode at runtime.
        /// </summary>
        public void SetDisplayMode(DisplayMode newMode)
        {
            _displayMode = newMode;
            ValidateComponents();

            if (G.DayNightSystem != null)
            {
                UpdateDisplay(G.DayNightSystem.NormalizedTime, G.DayNightSystem.Hour24);
            }
        }

        /// <summary>
        /// Switch update frequency at runtime.
        /// Will subscribe/unsubscribe from events as needed.
        /// </summary>
        public void SetUpdateFrequency(UpdateFrequency newFrequency)
        {
            if (_updateFrequency == UpdateFrequency.OnTimeChangeEvent && newFrequency != UpdateFrequency.OnTimeChangeEvent)
            {
                UnsubscribeFromEvents();
            }
            else if (_updateFrequency != UpdateFrequency.OnTimeChangeEvent && newFrequency == UpdateFrequency.OnTimeChangeEvent)
            {
                SubscribeToEvents();
            }

            _updateFrequency = newFrequency;
        }

        /// <summary>
        /// Switch time format at runtime (affects digital display only).
        /// </summary>
        public void SetTimeFormat(TimeFormat newFormat)
        {
            _timeFormat = newFormat;
            _lastHour = -1; // Force refresh
            _lastMinute = -1;

            if (G.DayNightSystem != null && (_displayMode == DisplayMode.Digital || _displayMode == DisplayMode.Both))
            {
                UpdateDigitalDisplay(G.DayNightSystem.Hour24);
            }
        }

        /// <summary>
        /// Manually set the time to display (only works when UpdateFrequency is Manual).
        /// </summary>
        /// <param name="normalizedTime">Normalized time (0‑1)</param>
        public void SetManualTime(float normalizedTime)
        {
            _manualNormalizedTime = Mathf.Clamp01(normalizedTime);
            if (_updateFrequency == UpdateFrequency.Manual)
            {
                UpdateDisplay(_manualNormalizedTime, _manualNormalizedTime * 24f);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure manual time is clamped
            _manualNormalizedTime = Mathf.Clamp01(_manualNormalizedTime);

            // Update in editor preview
            if (Application.isPlaying && G.DayNightSystem != null)
            {
                UpdateDisplay(G.DayNightSystem.NormalizedTime, G.DayNightSystem.Hour24);
            }
        }
#endif

        #endregion
    }
}