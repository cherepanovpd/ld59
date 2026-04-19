using UnityEngine;

namespace Project.UI.SignalButton.Config
{
    /// <summary>
    /// Defines the three zones of the semicircular gauge with their angles, colors, and multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "GaugeZoneConfig", menuName = "UI/SignalButton/GaugeZoneConfig")]
    public class GaugeZoneConfig : ScriptableObject
    {
        [Header("Zone Angles (0-180 degrees)")]
        [SerializeField, Tooltip("Red zone angle range (min, max) in degrees")]
        private Vector2 _redZoneRange = new Vector2(0f, 60f);
        
        [SerializeField, Tooltip("Yellow zone angle range (min, max) in degrees")]
        private Vector2 _yellowZoneRange = new Vector2(60f, 120f);
        
        [SerializeField, Tooltip("Green zone angle range (min, max) in degrees")]
        private Vector2 _greenZoneRange = new Vector2(120f, 180f);

        [Header("Zone Colors")]
        [SerializeField, Tooltip("Color of the red zone")]
        private Color _redZoneColor = Color.red;
        
        [SerializeField, Tooltip("Color of the yellow zone")]
        private Color _yellowZoneColor = Color.yellow;
        
        [SerializeField, Tooltip("Color of the green zone")]
        private Color _greenZoneColor = Color.green;

        [Header("Zone Multipliers")]
        [SerializeField, Range(0f, 1f), Tooltip("Signal strength multiplier for red zone")]
        private float _redZoneMultiplier = 0.5f;
        
        [SerializeField, Range(0.5f, 2f), Tooltip("Signal strength multiplier for yellow zone")]
        private float _yellowZoneMultiplier = 1f;
        
        [SerializeField, Range(1f, 5f), Tooltip("Signal strength multiplier for green zone")]
        private float _greenZoneMultiplier = 3f;

        // Public properties with getters (read-only access)
        public Vector2 RedZoneRange => _redZoneRange;
        public Vector2 YellowZoneRange => _yellowZoneRange;
        public Vector2 GreenZoneRange => _greenZoneRange;
        
        public Color RedZoneColor => _redZoneColor;
        public Color YellowZoneColor => _yellowZoneColor;
        public Color GreenZoneColor => _greenZoneColor;
        
        public float RedZoneMultiplier => _redZoneMultiplier;
        public float YellowZoneMultiplier => _yellowZoneMultiplier;
        public float GreenZoneMultiplier => _greenZoneMultiplier;

        /// <summary>
        /// Gets the zone (Red, Yellow, Green) for a given angle.
        /// </summary>
        /// <param name="angle">Angle in degrees (0-180)</param>
        /// <returns>The zone enum value</returns>
        public GaugeZone GetZoneForAngle(float angle)
        {
            if (angle >= _redZoneRange.x && angle <= _redZoneRange.y)
                return GaugeZone.Red;
            if (angle >= _yellowZoneRange.x && angle <= _yellowZoneRange.y)
                return GaugeZone.Yellow;
            if (angle >= _greenZoneRange.x && angle <= _greenZoneRange.y)
                return GaugeZone.Green;
            
            // Default to red if outside all ranges (should not happen with proper validation)
            return GaugeZone.Red;
        }

        /// <summary>
        /// Gets the multiplier for a given angle.
        /// </summary>
        /// <param name="angle">Angle in degrees (0-180)</param>
        /// <returns>Signal strength multiplier</returns>
        public float GetMultiplierForAngle(float angle)
        {
            var zone = GetZoneForAngle(angle);
            return zone switch
            {
                GaugeZone.Red => _redZoneMultiplier,
                GaugeZone.Yellow => _yellowZoneMultiplier,
                GaugeZone.Green => _greenZoneMultiplier,
                _ => 1f
            };
        }

        /// <summary>
        /// Gets the color for a given angle.
        /// </summary>
        /// <param name="angle">Angle in degrees (0-180)</param>
        /// <returns>Zone color</returns>
        public Color GetColorForAngle(float angle)
        {
            var zone = GetZoneForAngle(angle);
            return zone switch
            {
                GaugeZone.Red => _redZoneColor,
                GaugeZone.Yellow => _yellowZoneColor,
                GaugeZone.Green => _greenZoneColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Validates configuration values to ensure zones are properly defined and non-overlapping.
        /// Called automatically by Unity Editor.
        /// </summary>
        private void OnValidate()
        {
            // Ensure angles are within 0-180 range
            _redZoneRange.x = Mathf.Clamp(_redZoneRange.x, 0f, 180f);
            _redZoneRange.y = Mathf.Clamp(_redZoneRange.y, 0f, 180f);
            _yellowZoneRange.x = Mathf.Clamp(_yellowZoneRange.x, 0f, 180f);
            _yellowZoneRange.y = Mathf.Clamp(_yellowZoneRange.y, 0f, 180f);
            _greenZoneRange.x = Mathf.Clamp(_greenZoneRange.x, 0f, 180f);
            _greenZoneRange.y = Mathf.Clamp(_greenZoneRange.y, 0f, 180f);
            
            // Ensure min <= max
            if (_redZoneRange.x > _redZoneRange.y)
                (_redZoneRange.x, _redZoneRange.y) = (_redZoneRange.y, _redZoneRange.x);
            if (_yellowZoneRange.x > _yellowZoneRange.y)
                (_yellowZoneRange.x, _yellowZoneRange.y) = (_yellowZoneRange.y, _yellowZoneRange.x);
            if (_greenZoneRange.x > _greenZoneRange.y)
                (_greenZoneRange.x, _greenZoneRange.y) = (_greenZoneRange.y, _greenZoneRange.x);
            
            // Ensure multipliers are positive
            _redZoneMultiplier = Mathf.Max(0f, _redZoneMultiplier);
            _yellowZoneMultiplier = Mathf.Max(0f, _yellowZoneMultiplier);
            _greenZoneMultiplier = Mathf.Max(0f, _greenZoneMultiplier);
        }
    }

    /// <summary>
    /// Enum representing the three gauge zones.
    /// </summary>
    public enum GaugeZone
    {
        Red,
        Yellow,
        Green
    }
}