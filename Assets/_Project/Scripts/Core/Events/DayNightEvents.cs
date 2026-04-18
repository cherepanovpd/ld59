// Path: Assets/_Project/Scripts/Core/Events/DayNightEvents.cs

namespace Project.Core.Events
{
    /// <summary>
    /// Raised when the normalized time changes (usually every frame while time is advancing).
    /// </summary>
    public class DayNightTimeChangedEvent
    {
        /// <summary>
        /// Normalized time (0‑1) where 0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk.
        /// </summary>
        public float NormalizedTime { get; }

        /// <summary>
        /// Time of day in 24‑hour format (0‑24).
        /// </summary>
        public float Hour { get; }

        public DayNightTimeChangedEvent(float normalizedTime, float hour)
        {
            NormalizedTime = normalizedTime;
            Hour = hour;
        }
    }

    /// <summary>
    /// Raised when a full day cycle completes (time wraps from 1 back to 0).
    /// </summary>
    public class DayEndEvent
    {
        /// <summary>
        /// Total number of days that have passed since game start (incremented each wrap).
        /// </summary>
        public int DayCount { get; }

        public DayEndEvent(int dayCount)
        {
            DayCount = dayCount;
        }
    }

    /// <summary>
    /// Raised when the day phase changes (Dawn → Day → Dusk → Night).
    /// </summary>
    public class DayPhaseChangedEvent
    {
        public enum Phase
        {
            Dawn,   // 0.2‑0.3
            Day,    // 0.3‑0.7
            Dusk,   // 0.7‑0.8
            Night   // 0.8‑0.2 (wrapping)
        }

        /// <summary>
        /// The new phase that just started.
        /// </summary>
        public Phase NewPhase { get; }

        public DayPhaseChangedEvent(Phase newPhase)
        {
            NewPhase = newPhase;
        }
    }
}