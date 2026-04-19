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
    /// Displays in‑game time with both analog dial (rotating RectTransform) and digital clock (TMP text).
    /// Analog dial rotates according to normalized time (0 = midnight, 0.5 = noon).
    /// Digital clock supports 24‑hour or 12‑hour with AM/PM format.
    /// Zero allocations in Update loop, supports event‑based updates for efficiency.
    /// </summary>
    public class ClockDisplay : MonoBehaviour
    {
        #region Configuration Enums

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
                s_cachedHour12Strings[i] = i == 0 ? "12" : i.ToString("00");
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField]
        [Tooltip("How often the clock should update its display.")]
        private UpdateFrequency _updateFrequency = UpdateFrequency.OnTimeChangeEvent;

        [SerializeField]
        [Tooltip("For digital display: 24‑hour format (14:30) or 12‑hour with AM/PM (2:30 PM).")]
        private TimeFormat _timeFormat = TimeFormat.Hour24;

        [Header("Analog Clock References")]
        [SerializeField]
        [Tooltip("RectTransform of the analog dial (the whole clock face). Will be rotated around its Z axis.")]
        private RectTransform _analogDialRectTransform;

        [SerializeField]
        [Tooltip("Z rotation (degrees) at midnight (normalized time = 0).")]
        private float _analogDialMidnightRotation = -180f;

        [SerializeField]
        [Tooltip("Z rotation (degrees) at noon (normalized time = 0.5).")]
        private float _analogDialNoonRotation = 0f;

        [SerializeField]
        [Tooltip("If true, the dial rotates continuously (full 360° per day). If false, uses linear interpolation between midnight and noon.")]
        private bool _analogDialContinuousRotation = true;

        [Header("Digital Clock References")]
        [SerializeField]
        [Tooltip("TextMeshPro component to display digital time.")]
        private TMP_Text _digitalText;

        [SerializeField]
        [Tooltip("Optional TextMeshPro component for AM/PM indicator (used only in 12‑hour format).")]
        private TMP_Text _amPmText;

        [SerializeField]
        [Tooltip("Optional background Image behind digital clock. Its color will be evaluated from the time gradient.")]
        private Image _digitalBackgroundImage;

        [SerializeField]
        [Tooltip("Gradient mapping normalized time (0 = midnight, 0.5 = noon) to background color.")]
        private Gradient _timeGradient = new Gradient();

        [Header("Advanced")]
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

        // Background color caching
        private float _lastNormalizedTimeForBackground = -1f;
        private Color _lastBackgroundColor;

        // Performance flags
        private bool _isSubscribedToEvents;
        private bool _hasAnalogDial;
        private bool _hasDigitalText;
        private bool _hasAmPmText;
        private bool _hasBackgroundImage;

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
            _hasAnalogDial = _analogDialRectTransform != null;
            _hasDigitalText = _digitalText != null;
            _hasAmPmText = _amPmText != null;
            _hasBackgroundImage = _digitalBackgroundImage != null;

            if (!_hasAnalogDial)
            {
                Debug.LogWarning("[ClockDisplay] Analog dial RectTransform is not assigned.", this);
            }

            if (!_hasDigitalText)
            {
                Debug.LogError("[ClockDisplay] Digital text component (TMP_Text) is not assigned.", this);
            }

            if (_timeFormat == TimeFormat.Hour12WithAmPm && !_hasAmPmText)
            {
                Debug.LogWarning("[ClockDisplay] AM/PM text component is not assigned but 12‑hour format is selected.", this);
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

            // Update analog dial
            UpdateAnalogDial(normalizedTime);

            // Update digital display
            UpdateDigitalDisplay(hour24);

            // Update background color
            UpdateBackgroundColor(normalizedTime);
        }

        private void UpdateAnalogDial(float normalizedTime)
        {
            if (!_hasAnalogDial)
                return;

            float rotationZ;
            if (_analogDialContinuousRotation)
            {
                // Continuous rotation: midnight rotation + 360° per day
                rotationZ = _analogDialMidnightRotation + normalizedTime * 360f;
            }
            else
            {
                // Linear interpolation between midnight and noon, then noon and next midnight
                if (normalizedTime <= 0.5f)
                {
                    // Midnight → noon
                    rotationZ = Mathf.Lerp(_analogDialMidnightRotation, _analogDialNoonRotation, normalizedTime * 2f);
                }
                else
                {
                    // Noon → next midnight (midnight rotation + 360° for continuity)
                    float nextMidnightRotation = _analogDialMidnightRotation + 360f;
                    rotationZ = Mathf.Lerp(_analogDialNoonRotation, nextMidnightRotation, (normalizedTime - 0.5f) * 2f);
                }
            }

            _analogDialRectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private void UpdateDigitalDisplay(float hour24)
        {
            if (!_hasDigitalText)
                return;

            // Extract hour and minute
            int hour = Mathf.FloorToInt(hour24);
            int minute = Mathf.FloorToInt((hour24 % 1f) * 60f);

            // Check if we need to update the string (avoid unnecessary allocations)
            bool hourChanged = hour != _lastHour;
            bool minuteChanged = minute != _lastMinute;

            if (!hourChanged && !minuteChanged)
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
            _digitalText.text = _cachedTimeString;

            if (_timeFormat == TimeFormat.Hour12WithAmPm && _hasAmPmText)
            {
                _amPmText.text = _cachedAmPmString;
            }
        }

        private void UpdateBackgroundColor(float normalizedTime)
        {
            if (!_hasBackgroundImage)
                return;

            // Only update if normalized time changed significantly (threshold 0.001)
            if (Mathf.Abs(normalizedTime - _lastNormalizedTimeForBackground) < 0.001f)
                return;

            _lastNormalizedTimeForBackground = normalizedTime;
            Color newColor = _timeGradient.Evaluate(normalizedTime);

            // Avoid setting the same color (tiny performance)
            if (newColor != _lastBackgroundColor)
            {
                _digitalBackgroundImage.color = newColor;
                _lastBackgroundColor = newColor;
            }
        }

        #endregion

        #region Public API

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

            if (G.DayNightSystem != null)
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

        /// <summary>
        /// Set the analog dial rotation mapping directly.
        /// </summary>
        /// <param name="midnightRotation">Z rotation at midnight (normalized time = 0)</param>
        /// <param name="noonRotation">Z rotation at noon (normalized time = 0.5)</param>
        public void SetAnalogDialRotation(float midnightRotation, float noonRotation)
        {
            _analogDialMidnightRotation = midnightRotation;
            _analogDialNoonRotation = noonRotation;
            if (G.DayNightSystem != null)
            {
                UpdateAnalogDial(G.DayNightSystem.NormalizedTime);
            }
        }

        /// <summary>
        /// Set the time gradient for background color.
        /// </summary>
        /// <param name="gradient">New gradient (will be copied)</param>
        public void SetTimeGradient(Gradient gradient)
        {
            _timeGradient = gradient;
            _lastNormalizedTimeForBackground = -1f; // Force update
            if (G.DayNightSystem != null)
            {
                UpdateBackgroundColor(G.DayNightSystem.NormalizedTime);
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