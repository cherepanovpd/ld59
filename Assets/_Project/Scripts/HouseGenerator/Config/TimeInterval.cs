// Path: Assets/_Project/Scripts/HouseGenerator/Config/TimeInterval.cs

using UnityEngine;

namespace Project.HouseGenerator.Config
{
    /// <summary>
    /// Represents a time interval with start and end hours (0-24).
    /// </summary>
    [System.Serializable]
    public struct TimeInterval
    {
        [SerializeField]
        [Tooltip("Start hour (0-24).")]
        [Range(0f, 24f)]
        private float _startHour;

        [SerializeField]
        [Tooltip("End hour (0-24). Must be greater than or equal to start hour.")]
        [Range(0f, 24f)]
        private float _endHour;

        /// <summary>
        /// Start hour (0-24).
        /// </summary>
        public float StartHour => _startHour;

        /// <summary>
        /// End hour (0-24).
        /// </summary>
        public float EndHour => _endHour;

        /// <summary>
        /// Creates a new time interval.
        /// </summary>
        /// <param name="startHour">Start hour (0-24).</param>
        /// <param name="endHour">End hour (0-24).</param>
        public TimeInterval(float startHour, float endHour)
        {
            _startHour = Mathf.Clamp(startHour, 0f, 24f);
            _endHour = Mathf.Clamp(endHour, 0f, 24f);
        }

        /// <summary>
        /// Checks if the given hour (0-24) falls within this interval.
        /// Handles intervals that cross midnight (e.g., 22-2).
        /// </summary>
        /// <param name="currentHour">Current hour (0-24).</param>
        /// <returns>True if the hour is inside the interval.</returns>
        public bool IsTimeInInterval(float currentHour)
        {
            // Normalize current hour to 0-24
            currentHour = Mathf.Repeat(currentHour, 24f);

            // If start <= end, it's a normal interval within the same day
            if (_startHour <= _endHour)
            {
                return currentHour >= _startHour && currentHour <= _endHour;
            }
            else
            {
                // Interval crosses midnight (e.g., 22-2)
                return currentHour >= _startHour || currentHour <= _endHour;
            }
        }

        /// <summary>
        /// Returns a formatted string representation.
        /// </summary>
        public override string ToString()
        {
            return $"{_startHour:0.##}-{_endHour:0.##}";
        }
    }
}