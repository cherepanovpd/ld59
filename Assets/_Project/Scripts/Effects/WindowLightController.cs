// Path: Assets/_Project/Scripts/Effects/WindowLightController.cs

using Core;
using Project.HouseGenerator.Config;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Project.Effects
{
    /// <summary>
    /// Controls the visual state of an individual window (sprite color + Light2D).
    /// Managed by WindowLightManager; does not subscribe to time events directly.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class WindowLightController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField, Tooltip("SpriteRenderer that displays the window sprite. Automatically fetched if not assigned.")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("Optional Light2D component that provides illumination. If not assigned, will be fetched from this GameObject.")]
        private Light2D _light2D;

        [Header("State")]
        [SerializeField, Tooltip("Current light state (editor visibility only).")]
        private bool _isLightOn;

        // Cached colors from config
        private Color _lightOnColor;
        private Color _lightOffColor;

        // Gradual lighting
        private float _randomOffsetMinutes; // 0 ≤ offset < gradual duration
        private bool _hasGradualOffset = false;

        /// <summary>
        /// Whether the window light is currently ON.
        /// </summary>
        public bool IsLightOn => _isLightOn;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponentReferences();
            CacheColorsFromConfig();
            GenerateRandomOffset();
            // Set initial visual state based on serialized _isLightOn
            ApplyVisualState(_isLightOn);
        }

        private void OnEnable()
        {
            RegisterWithManager();
        }

        private void OnDisable()
        {
            UnregisterFromManager();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the window light state to ON or OFF and updates visuals immediately.
        /// </summary>
        /// <param name="isOn">True to turn the light ON, false to turn it OFF.</param>
        public void SetLightState(bool isOn)
        {
            if (_isLightOn == isOn)
                return;

            _isLightOn = isOn;
            ApplyVisualState(_isLightOn);
        }

        /// <summary>
        /// Updates the window light state based on the current hour.
        /// This method is called by WindowLightManager; the controller does not subscribe to time events directly.
        /// </summary>
        /// <param name="currentHour">Current hour in 24‑hour format (0‑24).</param>
        public void UpdateLightBasedOnTime(float currentHour)
        {
            if (!G.HasHouseConfig())
            {
                Debug.LogWarning($"[WindowLightController] HouseConfig not available. Cannot update light for hour {currentHour}.", this);
                return;
            }

            WindowLightingConfig config = G.HouseConfig.WindowLightingConfig;
            bool shouldBeOn;
            
            if (_hasGradualOffset && (config.GradualOnDurationMinutes > 0f || config.GradualOffDurationMinutes > 0f))
            {
                shouldBeOn = config.IsLightOnWithGradual(currentHour, _randomOffsetMinutes);
            }
            else
            {
                shouldBeOn = config.IsLightOn(currentHour); // Fallback to original
            }
            
            SetLightState(shouldBeOn);
        }

        #endregion

        #region Private Helpers

        private void CacheComponentReferences()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_light2D == null)
                _light2D = GetComponent<Light2D>();

            if (_spriteRenderer == null)
                Debug.LogError($"[WindowLightController] No SpriteRenderer found on {gameObject.name}.", this);
        }

        private void CacheColorsFromConfig()
        {
            if (!G.HasHouseConfig())
            {
                Debug.LogWarning($"[WindowLightController] HouseConfig not available. Using default colors.", this);
                _lightOnColor = new Color(1f, 0.9f, 0.6f); // Warm yellow
                _lightOffColor = new Color(0.1f, 0.1f, 0.1f); // Dark gray
                return;
            }

            WindowLightingConfig config = G.HouseConfig.WindowLightingConfig;
            _lightOnColor = config.LightOnColor;
            _lightOffColor = config.LightOffColor;
        }

        private void GenerateRandomOffset()
        {
            if (!G.HasHouseConfig())
            {
                _hasGradualOffset = false;
                return;
            }

            WindowLightingConfig config = G.HouseConfig.WindowLightingConfig;
            float maxDuration = Mathf.Max(config.GradualOnDurationMinutes, config.GradualOffDurationMinutes);

            if (maxDuration <= 0f)
            {
                _hasGradualOffset = false;
                return;
            }

            // Deterministic hash from instance ID and position
            int hash = GetInstanceID() ^ transform.position.GetHashCode();
            // Ensure positive value
            int positiveHash = Mathf.Abs(hash);
            // Generate offset in range [0, maxDuration)
            _randomOffsetMinutes = (positiveHash % 1000) / 1000f * maxDuration;
            _hasGradualOffset = true;
        }

        private void ApplyVisualState(bool isOn)
        {
            // Update sprite color
            if (_spriteRenderer != null)
                _spriteRenderer.color = isOn ? _lightOnColor : _lightOffColor;

            // Update Light2D activity
            if (_light2D != null)
                _light2D.enabled = isOn;
        }

        private void RegisterWithManager()
        {
            // Use the global service locator; if WindowLightManager is not yet registered, this is a no‑op.
            if (G.HasWindowLightManager())
                G.WindowLightManager.RegisterWindow(this);
            else
                Debug.LogWarning($"[WindowLightController] WindowLightManager not available. Window {gameObject.name} will not receive time updates.", this);
        }

        private void UnregisterFromManager()
        {
            if (G.HasWindowLightManager())
                G.WindowLightManager.UnregisterWindow(this);
        }

        #endregion

        #region Editor Only

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep the serialized field in sync with the visual state in the editor
            if (Application.isPlaying)
                return;

            // If we have a SpriteRenderer, preview the color based on _isLightOn
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                // Use default colors for editor preview
                Color previewOn = new Color(1f, 0.9f, 0.6f);
                Color previewOff = new Color(0.1f, 0.1f, 0.1f);
                _spriteRenderer.color = _isLightOn ? previewOn : previewOff;
            }

            // Light2D preview
            if (_light2D == null)
                _light2D = GetComponent<Light2D>();

            if (_light2D != null)
                _light2D.enabled = _isLightOn;
        }
#endif

        #endregion
    }
}