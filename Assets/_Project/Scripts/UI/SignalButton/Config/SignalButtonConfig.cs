using UnityEngine;

namespace Project.UI.SignalButton.Config
{
    /// <summary>
    /// Defines the movement pattern for the gauge arrow.
    /// </summary>
    public enum ArrowMovementPattern
    {
        Linear,
        EaseInOut,
        Random
    }

    /// <summary>
    /// Central configuration for the signal button system.
    /// Contains timing, visual, and arrow movement parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "SignalButtonConfig", menuName = "UI/SignalButton/Config")]
    public class SignalButtonConfig : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Range(1f, 10f), Tooltip("Time in seconds before button can be clicked again")]
        private float _cooldownDuration = 5f;
        
        [SerializeField, Range(0.1f, 2f), Tooltip("Duration of hover scale animation in seconds")]
        private float _hoverAnimationDuration = 0.2f;
        
        [SerializeField, Range(0.05f, 1f), Tooltip("Duration of click scale animation in seconds")]
        private float _clickAnimationDuration = 0.1f;

        [Header("Visual")]
        [SerializeField, Range(1f, 1.5f), Tooltip("Scale multiplier when button is hovered")]
        private float _hoverScaleMultiplier = 1.1f;
        
        [SerializeField, Tooltip("Color of the outline that appears on hover")]
        private Color _outlineColor = Color.cyan;
        
        [SerializeField, Range(1f, 10f), Tooltip("Width of the outline in pixels")]
        private float _outlineWidth = 2f;

        [Header("Arrow Movement")]
        [SerializeField, Range(10f, 360f), Tooltip("Arrow rotation speed in degrees per second")]
        private float _arrowRotationSpeed = 90f;
        
        [SerializeField, Tooltip("Movement pattern of the arrow")]
        private ArrowMovementPattern _movementPattern = ArrowMovementPattern.Linear;

        // Public properties with getters (read-only access)
        public float CooldownDuration => _cooldownDuration;
        public float HoverAnimationDuration => _hoverAnimationDuration;
        public float ClickAnimationDuration => _clickAnimationDuration;
        public float HoverScaleMultiplier => _hoverScaleMultiplier;
        public Color OutlineColor => _outlineColor;
        public float OutlineWidth => _outlineWidth;
        public float ArrowRotationSpeed => _arrowRotationSpeed;
        public ArrowMovementPattern MovementPattern => _movementPattern;

        /// <summary>
        /// Validates configuration values to ensure they are within reasonable bounds.
        /// Called automatically by Unity Editor.
        /// </summary>
        private void OnValidate()
        {
            _cooldownDuration = Mathf.Max(0.1f, _cooldownDuration);
            _hoverAnimationDuration = Mathf.Max(0.01f, _hoverAnimationDuration);
            _clickAnimationDuration = Mathf.Max(0.01f, _clickAnimationDuration);
            _hoverScaleMultiplier = Mathf.Clamp(_hoverScaleMultiplier, 1f, 2f);
            _outlineWidth = Mathf.Max(0f, _outlineWidth);
            _arrowRotationSpeed = Mathf.Max(0f, _arrowRotationSpeed);
        }
    }
}