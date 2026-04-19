// Path: Assets/_Project/Scripts/HouseGenerator/Config/WindowLightingConfig.cs

using System.Collections.Generic;
using UnityEngine;

namespace Project.HouseGenerator.Config
{
    /// <summary>
    /// Configuration for window lighting behavior.
    /// </summary>
    [System.Serializable]
    public class WindowLightingConfig
    {
        [Header("Colors")]
        [SerializeField]
        [Tooltip("Color when window light is ON.")]
        private Color _lightOnColor = new Color(1f, 0.9f, 0.6f); // Warm yellow

        [SerializeField]
        [Tooltip("Color when window light is OFF.")]
        private Color _lightOffColor = new Color(0.1f, 0.1f, 0.1f); // Dark gray

        [Header("Time Intervals")]
        [SerializeField]
        [Tooltip("List of time intervals when lights should be ON (e.g., 18:00-20:00).")]
        private List<TimeInterval> _lightOnIntervals = new List<TimeInterval>();

        [SerializeField]
        [Tooltip("List of time intervals when lights should be OFF (e.g., 00:00-02:00).")]
        private List<TimeInterval> _lightOffIntervals = new List<TimeInterval>();

        [Header("Gradual Lighting")]
        [SerializeField]
        [Tooltip("Duration in minutes over which windows gradually turn ON (0 = immediate).")]
        private float _gradualOnDurationMinutes = 0f;

        [SerializeField]
        [Tooltip("Duration in minutes over which windows gradually turn OFF (0 = immediate).")]
        private float _gradualOffDurationMinutes = 0f;

        [Header("Simple Mode (No Interval Arrays)")]
        [SerializeField]
        [Tooltip("If true, uses simple on/off start hours instead of interval arrays.")]
        private bool _useSimpleMode = false;

        [SerializeField]
        [Tooltip("Hour (0-24) after which lights start turning ON with gradual delay. Lights stay ON until lights off start hour.")]
        [Range(0f, 24f)]
        private float _lightsOnStartHour = 18f;

        [SerializeField]
        [Tooltip("Hour (0-24) after which lights start turning OFF with gradual delay. Lights stay OFF until lights on start hour.")]
        [Range(0f, 24f)]
        private float _lightsOffStartHour = 6f;

        /// <summary>
        /// Color when window light is ON.
        /// </summary>
        public Color LightOnColor => _lightOnColor;

        /// <summary>
        /// Color when window light is OFF.
        /// </summary>
        public Color LightOffColor => _lightOffColor;

        /// <summary>
        /// List of time intervals when lights should be ON.
        /// </summary>
        public IReadOnlyList<TimeInterval> LightOnIntervals => _lightOnIntervals;

        /// <summary>
        /// List of time intervals when lights should be OFF.
        /// </summary>
        public IReadOnlyList<TimeInterval> LightOffIntervals => _lightOffIntervals;

        /// <summary>
        /// Duration in minutes over which windows gradually turn ON.
        /// </summary>
        public float GradualOnDurationMinutes => _gradualOnDurationMinutes;

        /// <summary>
        /// Duration in minutes over which windows gradually turn OFF.
        /// </summary>
        public float GradualOffDurationMinutes => _gradualOffDurationMinutes;

        /// <summary>
        /// Determines whether the window light should be ON at the given hour in simple mode (no intervals).
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <returns>True if light should be ON, false if OFF.</returns>
        private bool IsLightOnSimple(float currentHour)
        {
            // Normalize current hour to 0-24
            currentHour = Mathf.Repeat(currentHour, 24f);
            
            // Lights are ON between lightsOnStartHour and lightsOffStartHour (with wrap-around at midnight)
            if (_lightsOnStartHour <= _lightsOffStartHour)
            {
                // Same-day interval (e.g., 6:00 to 18:00 - lights OFF during day, ON during night? Actually that's reversed)
                // Actually, if lightsOnStartHour <= lightsOffStartHour, ON period is within same day.
                // Example: lightsOnStartHour = 18, lightsOffStartHour = 6 would be crossing midnight, so this case is for same-day intervals.
                // For typical night lighting, lightsOnStartHour > lightsOffStartHour (crossing midnight).
                // This case handles scenarios where lights are ON during a portion of the same day.
                return currentHour >= _lightsOnStartHour && currentHour < _lightsOffStartHour;
            }
            else
            {
                // Interval crosses midnight (e.g., 18:00 to 6:00 next day)
                return currentHour >= _lightsOnStartHour || currentHour < _lightsOffStartHour;
            }
        }

        /// <summary>
        /// Determines whether the window light should be ON at the given hour.
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <returns>True if light should be ON, false if OFF.</returns>
        public bool IsLightOn(float currentHour)
        {
            if (_useSimpleMode)
            {
                return IsLightOnSimple(currentHour);
            }
            
            // First, check if any OFF interval matches (OFF takes precedence)
            foreach (var interval in _lightOffIntervals)
            {
                if (interval.IsTimeInInterval(currentHour))
                    return false;
            }

            // Then, check if any ON interval matches
            foreach (var interval in _lightOnIntervals)
            {
                if (interval.IsTimeInInterval(currentHour))
                    return true;
            }

            // Default: light is OFF
            return false;
        }

        /// <summary>
        /// Determines whether the window light should be ON at the given hour, considering gradual lighting.
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <param name="randomOffsetMinutes">Window-specific random offset in minutes (0 ≤ offset < gradual duration).</param>
        /// <returns>True if light should be ON, false if OFF.</returns>
        public bool IsLightOnWithGradual(float currentHour, float randomOffsetMinutes)
        {
            if (_useSimpleMode)
            {
                return IsLightOnWithGradualSimple(currentHour, randomOffsetMinutes);
            }
            
            float randomOffsetHours = randomOffsetMinutes / 60f;
            
            // First, check OFF intervals (they take precedence)
            foreach (var interval in _lightOffIntervals)
            {
                if (!interval.IsTimeInInterval(currentHour))
                    continue;
                    
                float start = interval.StartHour;
                float gradualHours = _gradualOffDurationMinutes / 60f;
                
                if (gradualHours <= 0f)
                {
                    // Immediate OFF
                    return false;
                }
                
                float transitionEnd = start + gradualHours;
                
                // Check if we're in the gradual period
                if (currentHour >= start && currentHour < transitionEnd)
                {
                    // Window turns OFF at start + randomOffsetHours
                    if (currentHour >= start + randomOffsetHours)
                        return false;
                    // Otherwise, still ON (hasn't reached its turn-off time yet)
                    return true;
                }
                else
                {
                    // Past gradual period, definitely OFF
                    return false;
                }
            }
            
            // Check ON intervals (only if no OFF interval forced OFF)
            foreach (var interval in _lightOnIntervals)
            {
                if (!interval.IsTimeInInterval(currentHour))
                    continue;
                    
                float start = interval.StartHour;
                float gradualHours = _gradualOnDurationMinutes / 60f;
                
                if (gradualHours <= 0f)
                {
                    // Immediate ON
                    return true;
                }
                
                float transitionEnd = start + gradualHours;
                
                if (currentHour >= start && currentHour < transitionEnd)
                {
                    // Window turns ON at start + randomOffsetHours
                    if (currentHour >= start + randomOffsetHours)
                        return true;
                    // Otherwise, still OFF
                    return false;
                }
                else
                {
                    // Past gradual period, definitely ON
                    return true;
                }
            }
            
            // No interval matches
            return false;
        }

        /// <summary>
        /// Determines whether the window light should be ON at the given hour in simple mode, considering gradual lighting.
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <param name="randomOffsetMinutes">Window-specific random offset in minutes (0 ≤ offset < gradual duration).</param>
        /// <returns>True if light should be ON, false if OFF.</returns>
        private bool IsLightOnWithGradualSimple(float currentHour, float randomOffsetMinutes)
        {
            float randomOffsetHours = randomOffsetMinutes / 60f;
            float currentHourNormalized = Mathf.Repeat(currentHour, 24f);
            
            // Determine if we're in the ON period (without gradual)
            bool isInOnPeriod = IsLightOnSimple(currentHour);
            
            // Determine which transition we're closer to
            // We need to know if we're near lightsOnStartHour (turning ON) or lightsOffStartHour (turning OFF)
            // For simplicity, we can treat lightsOnStartHour as the start of ON period with gradual ON,
            // and lightsOffStartHour as the start of OFF period with gradual OFF.
            // However, the ON period may cross midnight, so we need to handle wrap.
            
            // Calculate distances to the two transition points
            float distanceToOnStart = Mathf.Repeat(currentHourNormalized - _lightsOnStartHour, 24f);
            float distanceToOffStart = Mathf.Repeat(currentHourNormalized - _lightsOffStartHour, 24f);
            
            // If we're in ON period, we might be in gradual OFF at the end of ON period?
            // Actually, gradual OFF happens at lightsOffStartHour, which is the start of OFF period.
            // If we're in ON period and current hour is within gradualOffDuration after lightsOffStartHour,
            // we should be turning OFF gradually.
            // But lightsOffStartHour is the time when lights start turning OFF (i.e., end of ON period).
            // Similarly, lightsOnStartHour is when lights start turning ON (start of ON period).
            
            // Let's implement:
            // - Gradual ON: from lightsOnStartHour to lightsOnStartHour + gradualOnDuration
            // - Gradual OFF: from lightsOffStartHour to lightsOffStartHour + gradualOffDuration
            
            // Check if we're in gradual OFF period
            float gradualOffHours = _gradualOffDurationMinutes / 60f;
            if (gradualOffHours > 0f)
            {
                // Check if current hour is within gradual OFF period after lightsOffStartHour
                float offStart = _lightsOffStartHour;
                float offEnd = offStart + gradualOffHours;
                // Handle wrap-around
                if (offEnd <= 24f)
                {
                    if (currentHourNormalized >= offStart && currentHourNormalized < offEnd)
                    {
                        // In gradual OFF period
                        if (currentHourNormalized >= offStart + randomOffsetHours)
                            return false; // Turned OFF
                        else
                            return true; // Still ON (hasn't reached turn-off time)
                    }
                }
                else
                {
                    // Gradual OFF period crosses midnight
                    if (currentHourNormalized >= offStart || currentHourNormalized < offEnd - 24f)
                    {
                        // In gradual OFF period (wrap)
                        // Need to adjust random offset for wrap
                        float adjustedCurrent = currentHourNormalized < offEnd - 24f ? currentHourNormalized + 24f : currentHourNormalized;
                        float adjustedStart = offStart;
                        if (adjustedCurrent >= adjustedStart + randomOffsetHours)
                            return false;
                        else
                            return true;
                    }
                }
            }
            
            // Check if we're in gradual ON period
            float gradualOnHours = _gradualOnDurationMinutes / 60f;
            if (gradualOnHours > 0f)
            {
                float onStart = _lightsOnStartHour;
                float onEnd = onStart + gradualOnHours;
                if (onEnd <= 24f)
                {
                    if (currentHourNormalized >= onStart && currentHourNormalized < onEnd)
                    {
                        // In gradual ON period
                        if (currentHourNormalized >= onStart + randomOffsetHours)
                            return true; // Turned ON
                        else
                            return false; // Still OFF
                    }
                }
                else
                {
                    // Gradual ON period crosses midnight
                    if (currentHourNormalized >= onStart || currentHourNormalized < onEnd - 24f)
                    {
                        float adjustedCurrent = currentHourNormalized < onEnd - 24f ? currentHourNormalized + 24f : currentHourNormalized;
                        float adjustedStart = onStart;
                        if (adjustedCurrent >= adjustedStart + randomOffsetHours)
                            return true;
                        else
                            return false;
                    }
                }
            }
            
            // Not in any gradual period, return base state
            return isInOnPeriod;
        }

        /// <summary>
        /// Gets the appropriate color for the window light at the given hour.
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <returns>LightOnColor if light should be ON, otherwise LightOffColor.</returns>
        public Color GetColorForHour(float currentHour)
        {
            return IsLightOn(currentHour) ? _lightOnColor : _lightOffColor;
        }
    }
}