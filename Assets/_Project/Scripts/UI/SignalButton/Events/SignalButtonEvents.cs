// Path: Assets/_Project/Scripts/UI/SignalButton/Events/SignalButtonEvents.cs

using Project.UI.SignalButton.Config;

namespace Project.UI.SignalButton.Events
{
    /// <summary>
    /// Event raised when the signal button is clicked.
    /// Contains the signal strength multiplier based on the gauge zone.
    /// </summary>
    public class SignalButtonClickEvent
    {
        /// <summary>
        /// Signal strength multiplier (0.5x for red, 1x for yellow, 3x for green).
        /// </summary>
        public float StrengthMultiplier { get; }
        
        /// <summary>
        /// The zone where the arrow was when the button was clicked.
        /// </summary>
        public GaugeZone Zone { get; }
        
        /// <summary>
        /// The exact angle of the arrow when the button was clicked (0-180 degrees).
        /// </summary>
        public float Angle { get; }
        
        public SignalButtonClickEvent(float strengthMultiplier, GaugeZone zone, float angle)
        {
            StrengthMultiplier = strengthMultiplier;
            Zone = zone;
            Angle = angle;
        }
    }
    
    /// <summary>
    /// Event raised when the signal button cooldown starts.
    /// </summary>
    public class SignalButtonCooldownStartEvent
    {
        /// <summary>
        /// Cooldown duration in seconds.
        /// </summary>
        public float Duration { get; }
        
        public SignalButtonCooldownStartEvent(float duration)
        {
            Duration = duration;
        }
    }
    
    /// <summary>
    /// Event raised when the signal button cooldown completes.
    /// </summary>
    public class SignalButtonCooldownCompleteEvent
    {
        public SignalButtonCooldownCompleteEvent()
        {
        }
    }
    
    /// <summary>
    /// Event raised when the signal button is hovered over.
    /// </summary>
    public class SignalButtonHoverEvent
    {
        public bool IsHovered { get; }
        
        public SignalButtonHoverEvent(bool isHovered)
        {
            IsHovered = isHovered;
        }
    }
}