// Path: Assets/_Project/Scripts/Core/Events/WindowLightingEvents.cs

namespace Project.Core.Events
{
    /// <summary>
    /// Raised when the overall window lighting state changes (lights turn on/off).
    /// This event is triggered by WindowLightManager when the number of windows with lights on
    /// changes from zero to non-zero or vice versa, or when the lighting configuration changes.
    /// </summary>
    public class WindowLightsChangedEvent
    {
        /// <summary>
        /// Whether lights are now ON (true) or OFF (false).
        /// </summary>
        public bool LightsOn { get; }

        /// <summary>
        /// Current hour when the change occurred.
        /// </summary>
        public float Hour { get; }

        /// <summary>
        /// Number of windows that are currently lit (if LightsOn is true).
        /// </summary>
        public int LitWindowCount { get; }

        public WindowLightsChangedEvent(bool lightsOn, float hour, int litWindowCount = 0)
        {
            LightsOn = lightsOn;
            Hour = hour;
            LitWindowCount = litWindowCount;
        }
    }
}