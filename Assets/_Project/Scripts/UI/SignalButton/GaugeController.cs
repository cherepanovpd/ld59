// Path: Assets/_Project/Scripts/UI/SignalButton/GaugeController.cs

using UnityEngine;
using UnityEngine.UI;
using Project.UI.SignalButton.Config;

namespace Project.UI.SignalButton
{
    /// <summary>
    /// Controls a semicircular gauge with a moving arrow that moves from left to right (0-180 degrees).
    /// Uses GaugeZoneConfig to define red, yellow, and green zones and calculates signal strength multipliers.
    /// Follows strict performance rules: zero allocations in Update loop, cached references.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GaugeController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("RectTransform of the arrow that rotates around the gauge center")]
        private RectTransform _arrowTransform;
        
        [SerializeField, Tooltip("Optional image of the arrow for color changes based on zone")]
        private Image _arrowImage;
        
        [SerializeField, Tooltip("Background image of the gauge for zone visualization")]
        private Image _gaugeBackground;
        
        [Header("Configuration")]
        [SerializeField, Tooltip("Zone configuration defining red, yellow, green zones with angles and multipliers")]
        private GaugeZoneConfig _zoneConfig;
        
        [SerializeField, Tooltip("Signal button configuration containing arrow movement speed and pattern")]
        private SignalButtonConfig _signalConfig;
        
        [SerializeField, Tooltip("Animation configuration containing movement curve")]
        private AnimationConfig _animationConfig;
        
        [Header("Movement Settings")]
        [SerializeField, Range(0f, 180f), Tooltip("Starting angle of the arrow (0 = left, 180 = right)")]
        private float _startAngle = 0f;
        
        [SerializeField, Tooltip("If true, arrow movement starts automatically on Awake")]
        private bool _autoStart = false;
        
        // Cached references for performance
        private RectTransform _cachedRectTransform;
        private AnimationCurve _cachedMovementCurve;
        
        // Movement state
        private float _currentAngle;
        private float _movementDirection = 1f; // 1 for increasing angle, -1 for decreasing
        private bool _isMoving = false;
        
        // Performance: cached values to avoid allocations
        private float _deltaTime;
        private float _rotationSpeed;
        private GaugeZone _currentZone;
        private float _currentMultiplier;
        
        #region Public Properties
        
        /// <summary>
        /// Current arrow angle in degrees (0-180).
        /// </summary>
        public float CurrentAngle => _currentAngle;
        
        /// <summary>
        /// Whether the arrow is currently moving.
        /// </summary>
        public bool IsMoving => _isMoving;
        
        /// <summary>
        /// Current zone the arrow is in (Red, Yellow, Green).
        /// </summary>
        public GaugeZone CurrentZone => _currentZone;
        
        /// <summary>
        /// Current signal strength multiplier based on zone.
        /// </summary>
        public float CurrentMultiplier => _currentMultiplier;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheReferences();
            ValidateComponents();
            
            _currentAngle = Mathf.Clamp(_startAngle, 0f, 180f);
            UpdateArrowRotation();
            UpdateZoneAndMultiplier();
            
            if (_autoStart)
            {
                StartArrowMovement();
            }
        }
        
        private void Update()
        {
            if (!_isMoving) return;
            
            // Zero‑allocation update: store Time.deltaTime once
            _deltaTime = Time.deltaTime;
            
            // Calculate angle change based on speed and direction
            _rotationSpeed = _signalConfig != null ? _signalConfig.ArrowRotationSpeed : 90f;
            float angleDelta = _rotationSpeed * _deltaTime * _movementDirection;
            _currentAngle += angleDelta;
            
            // Handle boundaries with ping‑pong behavior
            if (_currentAngle >= 180f)
            {
                _currentAngle = 180f;
                _movementDirection = -1f;
            }
            else if (_currentAngle <= 0f)
            {
                _currentAngle = 0f;
                _movementDirection = 1f;
            }
            
            // Apply rotation to arrow
            UpdateArrowRotation();
            
            // Update zone and multiplier (cached to avoid repeated calculations)
            UpdateZoneAndMultiplier();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts arrow movement with current configuration.
        /// </summary>
        public void StartArrowMovement()
        {
            if (_signalConfig == null)
            {
                Debug.LogWarning($"{nameof(GaugeController)}: SignalButtonConfig is missing, movement will not start.");
                return;
            }
            
            _isMoving = true;
        }
        
        /// <summary>
        /// Stops arrow movement.
        /// </summary>
        public void StopArrowMovement()
        {
            _isMoving = false;
        }
        
        /// <summary>
        /// Gets the current zone the arrow is in.
        /// </summary>
        /// <returns>Current GaugeZone (Red, Yellow, Green)</returns>
        public GaugeZone GetCurrentZone()
        {
            return _currentZone;
        }
        
        /// <summary>
        /// Gets the signal strength multiplier for the current arrow position.
        /// </summary>
        /// <returns>Multiplier value</returns>
        public float GetCurrentMultiplier()
        {
            return _currentMultiplier;
        }
        
        /// <summary>
        /// Sets the arrow to a specific angle (0-180 degrees) and updates zone/multiplier.
        /// </summary>
        /// <param name="angle">Target angle in degrees</param>
        public void SetAngle(float angle)
        {
            _currentAngle = Mathf.Clamp(angle, 0f, 180f);
            UpdateArrowRotation();
            UpdateZoneAndMultiplier();
        }
        
        /// <summary>
        /// Toggles arrow movement on/off.
        /// </summary>
        public void ToggleMovement()
        {
            _isMoving = !_isMoving;
        }
        
        #endregion
        
        #region Private Helpers
        
        private void CacheReferences()
        {
            if (_arrowTransform == null)
                _arrowTransform = GetComponent<RectTransform>();
            
            if (_arrowImage == null && _arrowTransform != null)
                _arrowImage = _arrowTransform.GetComponent<Image>();
            
            if (_gaugeBackground == null && transform.parent != null)
                _gaugeBackground = transform.parent.GetComponentInChildren<Image>();
            
            _cachedRectTransform = _arrowTransform;
            
            // Cache movement curve if available
            if (_animationConfig != null)
                _cachedMovementCurve = _animationConfig.ArrowMovementCurve;
        }
        
        private void ValidateComponents()
        {
            if (_arrowTransform == null)
                Debug.LogError($"{nameof(GaugeController)}: Arrow Transform is not assigned and cannot be found automatically.", this);
            
            if (_zoneConfig == null)
                Debug.LogError($"{nameof(GaugeController)}: Zone Config is not assigned.", this);
            
            if (_signalConfig == null)
                Debug.LogWarning($"{nameof(GaugeController)}: Signal Button Config is not assigned, using default speed 90°/s.", this);
        }
        
        private void UpdateArrowRotation()
        {
            if (_cachedRectTransform == null) return;
            
            // Rotate around the pivot (assuming pivot is at the bottom center of the arrow)
            // For a semicircular gauge, we rotate around the Z axis (2D UI)
            _cachedRectTransform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
            
            // Optional: update arrow color based on zone
            if (_arrowImage != null && _zoneConfig != null)
            {
                _arrowImage.color = _zoneConfig.GetColorForAngle(_currentAngle);
            }
        }
        
        private void UpdateZoneAndMultiplier()
        {
            if (_zoneConfig == null) return;
            
            _currentZone = _zoneConfig.GetZoneForAngle(_currentAngle);
            _currentMultiplier = _zoneConfig.GetMultiplierForAngle(_currentAngle);
        }
        
        #endregion
        
        #region Editor Utilities
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp start angle in editor
            _startAngle = Mathf.Clamp(_startAngle, 0f, 180f);
            
            // Auto‑assign missing references in editor for convenience
            if (_arrowTransform == null)
                _arrowTransform = GetComponent<RectTransform>();
        }
        
        private void Reset()
        {
            // Set default values when component is added via Inspector
            _arrowTransform = GetComponent<RectTransform>();
            _startAngle = 0f;
            _autoStart = false;
        }
#endif
        
        #endregion
    }
}