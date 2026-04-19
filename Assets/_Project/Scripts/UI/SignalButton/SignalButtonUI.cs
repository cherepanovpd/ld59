// Path: Assets/_Project/Scripts/UI/SignalButton/SignalButtonUI.cs

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Project.UI.SignalButton.Config;
using Project.UI.SignalButton.Events;
using Core;

namespace Project.UI.SignalButton
{
    /// <summary>
    /// Main controller class that orchestrates the entire signal button system with cooldown logic.
    /// Integrates GaugeController, ButtonAnimator, and cooldown logic to provide a complete
    /// signal button experience with zone-based signal strength multipliers.
    /// Follows all project architectural rules: zero allocations, G service locator registration,
    /// ScriptableObject configuration, and SOLID principles.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(GaugeController))]
    [RequireComponent(typeof(ButtonAnimator))]
    public class SignalButtonUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("Unity Button component for interactivity control")]
        private Button _button;
        
        [SerializeField, Tooltip("GaugeController component for zone detection and arrow movement")]
        private GaugeController _gaugeController;
        
        [SerializeField, Tooltip("ButtonAnimator component for hover and click animations")]
        private ButtonAnimator _buttonAnimator;
        
        [Header("Cooldown Visualization")]
        [SerializeField, Tooltip("Optional Image for cooldown progress (radial fill)")]
        private Image _cooldownFillImage;
        
        [SerializeField, Tooltip("Optional TextMeshProUGUI for cooldown timer display")]
        private TMPro.TextMeshProUGUI _cooldownTimerText;
        
        [SerializeField, Tooltip("Optional GameObject that overlays the button during cooldown")]
        private GameObject _cooldownOverlay;
        
        [Header("Configuration")]
        [SerializeField, Tooltip("Signal button configuration containing cooldown duration and other parameters")]
        private SignalButtonConfig _signalConfig;
        
        [SerializeField, Tooltip("Gauge zone configuration for zone ranges and multipliers")]
        private GaugeZoneConfig _zoneConfig;
        
        [SerializeField, Tooltip("Animation configuration for visual feedback")]
        private AnimationConfig _animationConfig;
        
        [Header("Settings")]
        [SerializeField, Tooltip("If true, gauge arrow continues moving during cooldown")]
        private bool _allowGaugeMovementDuringCooldown = false;
        
        [SerializeField, Tooltip("If true, button will automatically restart gauge movement after cooldown")]
        private bool _restartGaugeAfterCooldown = true;
        
        // State variables
        private bool _isInCooldown = false;
        private float _cooldownTimer = 0f;
        private float _cooldownDuration = 5f;
        private int _lastDisplayedCooldownInt = -1; // Cache last displayed integer to avoid string allocations
        
        // Cached references for performance
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        
        // Events
        public event Action<float> SignalSent; // Parameter: strength multiplier
        
        #region Public Properties
        
        /// <summary>
        /// Whether the button is currently in cooldown state.
        /// </summary>
        public bool IsInCooldown => _isInCooldown;
        
        /// <summary>
        /// Current cooldown progress (0 to 1).
        /// </summary>
        public float CooldownProgress => _isInCooldown ? (_cooldownTimer / _cooldownDuration) : 0f;
        
        /// <summary>
        /// Remaining cooldown time in seconds.
        /// </summary>
        public float CooldownRemaining => _isInCooldown ? _cooldownTimer : 0f;
        
        /// <summary>
        /// Reference to the GaugeController component.
        /// </summary>
        public GaugeController GaugeController => _gaugeController;
        
        /// <summary>
        /// Reference to the ButtonAnimator component.
        /// </summary>
        public ButtonAnimator ButtonAnimator => _buttonAnimator;
        
        /// <summary>
        /// Reference to the SignalButtonConfig.
        /// </summary>
        public SignalButtonConfig SignalConfig => _signalConfig;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheReferences();
            ValidateComponents();
            InitializeFromConfig();
            
            // Register with global service locator
            RegisterWithServiceLocator();
        }
        
        private void Start()
        {
            // Ensure button is interactable and gauge is moving at start
            SetButtonInteractable(true);
            StartGaugeMovement();
        }
        
        private void Update()
        {
            // Zero-allocation update: only update cooldown timer if in cooldown
            if (!_isInCooldown) return;
            
            _cooldownTimer -= Time.deltaTime;
            
            // Update cooldown visualization
            UpdateCooldownVisualization();
            
            // Check if cooldown completed
            if (_cooldownTimer <= 0f)
            {
                EndCooldown();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            SignalSent = null;
            
            // Unregister from service locator if needed
            UnregisterFromServiceLocator();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Sends a signal based on current gauge zone and starts cooldown.
        /// Called when button is clicked or programmatically.
        /// </summary>
        public void SendSignal()
        {
            if (_isInCooldown)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nameof(SignalButtonUI)}: Cannot send signal while in cooldown.");
#endif
                return;
            }
            
            // Stop gauge arrow movement
            StopGaugeMovement();
            
            // Get current zone and multiplier from GaugeController
            GaugeZone currentZone = _gaugeController.CurrentZone;
            float multiplier = _gaugeController.CurrentMultiplier;
            
            // Emit SignalSent event with multiplier
            OnSignalSent(multiplier);
            
            // Start cooldown
            StartCooldown();
            
            // Show cooldown visual feedback
            ShowCooldownVisualFeedback();
            
            // Trigger click animation
            TriggerClickAnimation();
        }
        
        /// <summary>
        /// Starts the cooldown period (default 5 seconds, configurable).
        /// </summary>
        public void StartCooldown()
        {
            if (_isInCooldown) return;
            
            _isInCooldown = true;
            _cooldownTimer = _cooldownDuration;
            
            // Disable button interactivity
            SetButtonInteractable(false);
            
            // Optionally stop gauge movement during cooldown
            if (!_allowGaugeMovementDuringCooldown)
            {
                StopGaugeMovement();
            }
            
            // Show cooldown overlay
            SetCooldownOverlayVisible(true);
            
            // Update cooldown visualization
            UpdateCooldownVisualization();
            
            // Trigger cooldown start event
            if (G.HasEvents() && G.Events != null)
            {
                G.Events.Trigger(new SignalButtonCooldownStartEvent(_cooldownDuration));
            }
        }
        
        /// <summary>
        /// Immediately ends the cooldown and resets button state.
        /// </summary>
        public void EndCooldown()
        {
            if (!_isInCooldown) return;
            
            _isInCooldown = false;
            _cooldownTimer = 0f;
            
            // Re-enable button
            SetButtonInteractable(true);
            
            // Restart gauge arrow movement if configured
            if (_restartGaugeAfterCooldown)
            {
                StartGaugeMovement();
            }
            
            // Hide cooldown overlay
            SetCooldownOverlayVisible(false);
            
            // Reset cooldown visualization
            ResetCooldownVisualization();
            
            // Trigger cooldown complete event
            if (G.HasEvents() && G.Events != null)
            {
                G.Events.Trigger(new SignalButtonCooldownCompleteEvent());
            }
            
            // Trigger cooldown complete feedback
            OnCooldownComplete();
        }
        
        /// <summary>
        /// Resets the button to its initial state (cancels cooldown, restarts gauge).
        /// </summary>
        public void ResetButton()
        {
            // End any active cooldown
            if (_isInCooldown)
            {
                EndCooldown();
            }
            
            // Ensure button is interactable
            SetButtonInteractable(true);
            
            // Start gauge movement
            StartGaugeMovement();
            
            // Reset visual state
            ResetCooldownVisualization();
            SetCooldownOverlayVisible(false);
        }
        
        #endregion
        
        #region Private Helpers
        
        private void CacheReferences()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_gaugeController == null) _gaugeController = GetComponent<GaugeController>();
            if (_buttonAnimator == null) _buttonAnimator = GetComponent<ButtonAnimator>();
            
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        private void ValidateComponents()
        {
            if (_button == null)
                Debug.LogError($"{nameof(SignalButtonUI)}: Button component is missing.", this);
            
            if (_gaugeController == null)
                Debug.LogError($"{nameof(SignalButtonUI)}: GaugeController component is missing.", this);
            
            if (_buttonAnimator == null)
                Debug.LogError($"{nameof(SignalButtonUI)}: ButtonAnimator component is missing.", this);
            
            if (_signalConfig == null)
                Debug.LogWarning($"{nameof(SignalButtonUI)}: SignalButtonConfig is not assigned. Using default values.", this);
        }
        
        private void InitializeFromConfig()
        {
            if (_signalConfig != null)
            {
                _cooldownDuration = _signalConfig.CooldownDuration;
            }
            else
            {
                _cooldownDuration = 5f; // Default fallback
            }
        }
        
        private void RegisterWithServiceLocator()
        {
            // Register with G.SignalButton service locator
            if (G.SignalButton == null)
            {
                G.SignalButton = this;
                G.EnsureSystem(nameof(SignalButtonUI), this);
#if UNITY_EDITOR
                Debug.Log($"{nameof(SignalButtonUI)}: Registered with G.SignalButton service locator.");
#endif
            }
            else if (G.SignalButton != this)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nameof(SignalButtonUI)}: G.SignalButton is already registered with another instance. This instance will not be registered.");
#endif
            }
        }
        
        private void UnregisterFromServiceLocator()
        {
            // Unregister from G.SignalButton if this instance is registered
            if (G.SignalButton == this)
            {
                G.SignalButton = null;
#if UNITY_EDITOR
                Debug.Log($"{nameof(SignalButtonUI)}: Unregistered from G.SignalButton service locator.");
#endif
            }
        }
        
        private void SetButtonInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
            
            // Also update canvas group alpha for visual feedback
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = interactable ? 1f : 0.7f;
            }
        }
        
        private void StartGaugeMovement()
        {
            if (_gaugeController != null)
            {
                _gaugeController.StartArrowMovement();
            }
        }
        
        private void StopGaugeMovement()
        {
            if (_gaugeController != null)
            {
                _gaugeController.StopArrowMovement();
            }
        }
        
        private void TriggerClickAnimation()
        {
            // ButtonAnimator handles click animation via IPointerClickHandler
            // We can also trigger additional feedback here
        }
        
        private void UpdateCooldownVisualization()
        {
            // Update cooldown fill image
            if (_cooldownFillImage != null)
            {
                _cooldownFillImage.fillAmount = CooldownProgress;
            }
            
            // Update cooldown timer text - avoid string allocations by only updating when integer value changes
            if (_cooldownTimerText != null)
            {
                int currentInt = Mathf.CeilToInt(_cooldownTimer);
                if (currentInt != _lastDisplayedCooldownInt)
                {
                    _cooldownTimerText.text = currentInt.ToString();
                    _lastDisplayedCooldownInt = currentInt;
                }
            }
        }
        
        private void ResetCooldownVisualization()
        {
            if (_cooldownFillImage != null)
            {
                _cooldownFillImage.fillAmount = 0f;
            }
            
            if (_cooldownTimerText != null)
            {
                _cooldownTimerText.text = "";
                _lastDisplayedCooldownInt = -1;
            }
        }
        
        private void SetCooldownOverlayVisible(bool visible)
        {
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.SetActive(visible);
            }
        }
        
        private void ShowCooldownVisualFeedback()
        {
            // Additional visual feedback when cooldown starts
            // Could play particle effect, sound, etc.
            // This is a placeholder for actual implementation
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles pointer click events from the button.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            SendSignal();
        }
        
        protected virtual void OnSignalSent(float strengthMultiplier)
        {
            SignalSent?.Invoke(strengthMultiplier);
            
            // Broadcast through global event system if available
            if (G.HasEvents() && G.Events != null)
            {
                var clickEvent = new SignalButtonClickEvent(
                    strengthMultiplier,
                    _gaugeController.CurrentZone,
                    _gaugeController.CurrentAngle
                );
                G.Events.Trigger(clickEvent);
            }
            
#if UNITY_EDITOR
            Debug.Log($"{nameof(SignalButtonUI)}: Signal sent with strength multiplier {strengthMultiplier}x");
#endif
        }
        
        protected virtual void OnCooldownComplete()
        {
            // Placeholder for cooldown complete event
            // Could trigger visual/audio feedback
#if UNITY_EDITOR
            Debug.Log($"{nameof(SignalButtonUI)}: Cooldown complete.");
#endif
        }
        
        #endregion
        
        #region Editor Integration
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign components in editor for convenience
            if (_button == null) _button = GetComponent<Button>();
            if (_gaugeController == null) _gaugeController = GetComponent<GaugeController>();
            if (_buttonAnimator == null) _buttonAnimator = GetComponent<ButtonAnimator>();
        }
#endif
        
        #endregion
    }
}