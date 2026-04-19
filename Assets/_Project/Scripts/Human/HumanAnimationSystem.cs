// Path: Assets/_Project/Scripts/Human/HumanAnimationSystem.cs

using System.Collections.Generic;
using Core;

using Project.DayNight;
using Project.Human.Config;
using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Handles the "slime jump" pulsating animation for human characters.
    /// Manages batch animation updates for performance.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanAnimationSystem : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private HumanConfig _config;
        [SerializeField] private float _globalAnimationSpeed = 1f;
        [SerializeField] private bool _enableDaytimeAnimation = true;
        [SerializeField] private bool _enableNighttimeAnimation = false;
        
        [Header("Slime Jump Parameters")]
        [SerializeField] private float _baseFrequency = 1f;
        [SerializeField] private float _amplitude = 0.1f;
        [SerializeField] private float _verticalOffset = 0.1f;
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Performance")]
        [SerializeField] private int _maxBatchSize = 50;
        [SerializeField] private bool _useFixedUpdate = false;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _logAnimationEvents = false;
        [SerializeField] private bool _visualizeAnimation = false;
        
        // Animation state
        private float _globalTime = 0f;
        private bool _isAnimating = true;
        
        // Cached references for performance
        private HumanManager _humanManager;
        private DayNightSystem _dayNightSystem;
        private List<HumanCharacter> _animatingHumans = new List<HumanCharacter>();
        
        /// <summary>
        /// Whether the animation system is currently active.
        /// </summary>
        public bool IsAnimating => _isAnimating;
        
        /// <summary>
        /// Global animation time (can be used for synchronization).
        /// </summary>
        public float GlobalTime => _globalTime;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache references
            _humanManager = G.HumanManager;
            _dayNightSystem = G.DayNightSystem;
            
            // Ensure we have a config
            if (_config == null)
                _config = G.HumanConfig;
            
            if (_showDebugInfo)
                Debug.Log($"[HumanAnimationSystem] Initialized with config: {(_config != null ? _config.name : "null")}");
        }
        
        private void Start()
        {
            // Start with animation enabled
            _isAnimating = true;
        }
        
        private void Update()
        {
            if (!_isAnimating || _useFixedUpdate)
                return;
            
            UpdateAnimation(Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            if (!_isAnimating || !_useFixedUpdate)
                return;
            
            UpdateAnimation(Time.fixedDeltaTime);
        }
        
        private void OnDrawGizmos()
        {
            if (!_visualizeAnimation || !Application.isPlaying)
                return;
            
            DrawAnimationGizmos();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Start the animation system.
        /// </summary>
        public void StartAnimation()
        {
            _isAnimating = true;
            
            if (_logAnimationEvents)
                Debug.Log("[HumanAnimationSystem] Animation started.");
        }
        
        /// <summary>
        /// Stop the animation system.
        /// </summary>
        public void StopAnimation()
        {
            _isAnimating = false;
            
            // Reset all human animations
            ResetAllAnimations();
            
            if (_logAnimationEvents)
                Debug.Log("[HumanAnimationSystem] Animation stopped.");
        }
        
        /// <summary>
        /// Update animation for all humans.
        /// </summary>
        public void UpdateAnimation(float deltaTime)
        {
            // Update global time
            _globalTime += deltaTime * _globalAnimationSpeed;
            
            // Check if we should animate based on day/night cycle
            if (!ShouldAnimate())
                return;
            
            // Get all active humans
            UpdateAnimatingHumansList();
            
            // Batch update animations
            BatchUpdateAnimations(deltaTime);
        }
        
        /// <summary>
        /// Apply slime jump animation to a specific human.
        /// </summary>
        public void ApplySlimeJumpToHuman(HumanCharacter human, float timeOffset = 0f)
        {
            if (human == null || human.Visual == null)
                return;
            
            // Calculate animation time with optional offset
            float animationTime = _globalTime + timeOffset;
            
            // Apply animation via HumanVisual
            human.Visual.ApplySlimeJump(animationTime * _baseFrequency);
        }
        
        /// <summary>
        /// Reset animation for a specific human.
        /// </summary>
        public void ResetHumanAnimation(HumanCharacter human)
        {
            if (human == null || human.Visual == null)
                return;
            
            human.Visual.ResetAnimation();
        }
        
        /// <summary>
        /// Reset animations for all humans.
        /// </summary>
        public void ResetAllAnimations()
        {
            if (_humanManager == null)
                return;
            
            // We need to get all humans - for simplicity, we'll iterate through HumanManager's list
            // In a real implementation, we'd have a more efficient way
            UpdateAnimatingHumansList();
            
            foreach (HumanCharacter human in _animatingHumans)
            {
                ResetHumanAnimation(human);
            }
            
            if (_logAnimationEvents)
                Debug.Log($"[HumanAnimationSystem] Reset animations for {_animatingHumans.Count} humans.");
        }
        
        /// <summary>
        /// Set animation parameters.
        /// </summary>
        public void SetAnimationParameters(float frequency, float amplitude, float verticalOffset)
        {
            _baseFrequency = Mathf.Max(0.1f, frequency);
            _amplitude = Mathf.Max(0f, amplitude);
            _verticalOffset = Mathf.Max(0f, verticalOffset);
            
            if (_logAnimationEvents)
                Debug.Log($"[HumanAnimationSystem] Parameters updated: freq={_baseFrequency}, amp={_amplitude}, offset={_verticalOffset}");
        }
        
        /// <summary>
        /// Enable or disable daytime animation.
        /// </summary>
        public void SetDaytimeAnimationEnabled(bool enabled)
        {
            _enableDaytimeAnimation = enabled;
            
            if (_logAnimationEvents)
                Debug.Log($"[HumanAnimationSystem] Daytime animation {(enabled ? "enabled" : "disabled")}.");
        }
        
        /// <summary>
        /// Enable or disable nighttime animation.
        /// </summary>
        public void SetNighttimeAnimationEnabled(bool enabled)
        {
            _enableNighttimeAnimation = enabled;
            
            if (_logAnimationEvents)
                Debug.Log($"[HumanAnimationSystem] Nighttime animation {(enabled ? "enabled" : "disabled")}.");
        }
        
        #endregion
        
        #region Private Methods
        
        private bool ShouldAnimate()
        {
            return true;
        }
        
        private void UpdateAnimatingHumansList()
        {
            if (_humanManager == null)
                return;
            
            // Clear current list
            _animatingHumans.Clear();
            
            // Get all humans in wandering state from HumanManager
            List<HumanCharacter> wanderingHumans = _humanManager.GetHumansByState(BehaviorState.Wandering);
            _animatingHumans.AddRange(wanderingHumans);
            
            // Log if debugging
            if (_logAnimationEvents && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[HumanAnimationSystem] Found {_animatingHumans.Count} wandering humans.");
            }
        }
        
        private void BatchUpdateAnimations(float deltaTime)
        {
            if (_humanManager == null || _animatingHumans.Count == 0)
                return;
            
            // Calculate animation parameters
            float frequency = _baseFrequency;
            if (_config != null)
                frequency = _config.SlimeJumpFrequency;
            
            // Update each human's animation
            int batchSize = Mathf.Min(_animatingHumans.Count, _maxBatchSize);
            for (int i = 0; i < batchSize; i++)
            {
                HumanCharacter human = _animatingHumans[i];
                if (human == null || !human.gameObject.activeSelf)
                    continue;
                
                // Check if human is in wandering state (for daytime animation)
                if (human.GetCurrentBehaviorState() == BehaviorState.Wandering)
                {
                    // Apply animation with slight offset for variety
                    float timeOffset = i * 0.1f;
                    ApplySlimeJumpToHuman(human, timeOffset);
                }
                else
                {
                    // Reset animation for non-wandering humans
                    ResetHumanAnimation(human);
                }
            }
            
            // Log performance info occasionally
            if (_showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[HumanAnimationSystem] Updated {batchSize} human animations.");
            }
        }
        
        private void DrawAnimationGizmos()
        {
            if (_animatingHumans.Count == 0)
                return;
            
            Gizmos.color = Color.cyan;
            
            // Draw animation influence radius around each animating human
            foreach (HumanCharacter human in _animatingHumans)
            {
                if (human == null)
                    continue;
                
                Vector3 position = human.transform.position;
                Gizmos.DrawWireSphere(position, 0.5f);
                
                // Draw animation direction indicator
                Vector3 direction = Vector3.up * Mathf.Sin(_globalTime * _baseFrequency) * 0.3f;
                Gizmos.DrawLine(position, position + direction);
            }
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Toggle Animation")]
        private void ToggleAnimationEditor()
        {
            if (!Application.isPlaying)
                return;
            
            if (_isAnimating)
                StopAnimation();
            else
                StartAnimation();
            
            Debug.Log($"[HumanAnimationSystem] Animation {(_isAnimating ? "started" : "stopped")}.");
        }
        
        [ContextMenu("Reset All Animations")]
        private void ResetAllAnimationsEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[HumanAnimationSystem] Reset only works in Play mode.");
                return;
            }
            
            ResetAllAnimations();
        }
        
        [ContextMenu("Log Animation Stats")]
        private void LogAnimationStatsEditor()
        {
            if (!Application.isPlaying)
                return;
            
            Debug.Log($"[HumanAnimationSystem] Stats:\n" +
                     $"Global Time: {_globalTime:F2}\n" +
                     $"Is Animating: {_isAnimating}\n" +
                     $"Animating Humans: {_animatingHumans.Count}\n" +
                     $"Should Animate: {ShouldAnimate()}");
        }
        
        [ContextMenu("Increase Animation Speed")]
        private void IncreaseAnimationSpeedEditor()
        {
            _globalAnimationSpeed += 0.5f;
            Debug.Log($"[HumanAnimationSystem] Animation speed increased to {_globalAnimationSpeed}.");
        }
        
        [ContextMenu("Decrease Animation Speed")]
        private void DecreaseAnimationSpeedEditor()
        {
            _globalAnimationSpeed = Mathf.Max(0.1f, _globalAnimationSpeed - 0.5f);
            Debug.Log($"[HumanAnimationSystem] Animation speed decreased to {_globalAnimationSpeed}.");
        }
#endif
        
        #endregion
    }
}