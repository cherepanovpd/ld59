// Path: Assets/_Project/Scripts/Human/HumanCharacter.cs

using Core;
using Project.Human.Config;
using UnityEngine;
using Utilities;

namespace Project.Human
{
    /// <summary>
    /// Main MonoBehaviour that orchestrates a human character.
    /// Manages HumanVisual, HumanMovement, and HumanState components.
    /// </summary>
    [RequireComponent(typeof(HumanVisual))]
    [DisallowMultipleComponent]
    public class HumanCharacter : MonoBehaviour, IPoolable
    {
        [Header("Component References")]
        [SerializeField] private HumanVisual _visual;
        [SerializeField] private HumanMovement _movement;
        [SerializeField] private HumanState _state;
        
        [Header("Character Data")]
        [SerializeField] private HumanData _humanData;
        [SerializeField] private House _homeHouse;
        
        [Header("Configuration")]
        [SerializeField] private HumanConfig _config;
        
        // Cached references for performance
        private Transform _cachedTransform;
        
        /// <summary>
        /// The visual component of this character.
        /// </summary>
        public HumanVisual Visual => _visual;
        
        /// <summary>
        /// The movement component of this character.
        /// </summary>
        public HumanMovement Movement => _movement;
        
        /// <summary>
        /// The state component of this character.
        /// </summary>
        public HumanState State => _state;
        
        /// <summary>
        /// The data defining this character's appearance.
        /// </summary>
        public HumanData Data => _humanData;
        
        /// <summary>
        /// The house this character belongs to.
        /// </summary>
        public House HomeHouse => _homeHouse;
        
        /// <summary>
        /// Cached transform for performance.
        /// </summary>
        public Transform CachedTransform => _cachedTransform;
        
        #region IPoolable Implementation
        
        /// <summary>
        /// Called when the object is spawned from the pool.
        /// </summary>
        public void OnSpawn()
        {
            // Ensure components are cached
            CacheComponents();
            
            // Reset animation state
            if (_visual != null)
                _visual.ResetAnimation();
            
            // Register with systems
            RegisterWithSystems();
            
            // Ensure the object is active and ready
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public void OnDespawn()
        {
            // Unregister from systems
            UnregisterFromSystems();
            
            // Reset state
            if (_movement != null)
                _movement.Stop();
            
            if (_visual != null)
                _visual.ResetAnimation();
            
            // Clear references to prevent memory leaks
            _homeHouse = null;
            _humanData = null;
            
            // Deactivate the object
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _cachedTransform = transform;
            CacheComponents();
            
            // Register with global systems if needed
            RegisterWithSystems();
        }
        
        private void Start()
        {
            // Initialize if not already initialized
            if (_humanData == null && _config != null)
            {
                InitializeWithRandomData();
            }
        }
        
        private void Update()
        {
            // Update character state and behavior
            UpdateCharacter(Time.deltaTime);
        }
        
        private void OnDestroy()
        {
            // Unregister from global systems
            UnregisterFromSystems();
        }
        
        private void OnValidate()
        {
            // Auto-assign components if they're null
            CacheComponents();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initialize the character with specific data and home house.
        /// </summary>
        public void Initialize(HumanData data, House homeHouse, HumanConfig config = null)
        {
            _humanData = data;
            _homeHouse = homeHouse;
            _config = config ?? G.HumanConfig;
            
            // Ensure components are cached
            CacheComponents();
            
            // Initialize visual component
            if (_visual != null)
                _visual.Initialize(data);
            
            // Initialize movement component if it exists
            if (_movement != null)
                _movement.Initialize(_config);
            
            // Initialize state component if it exists
            if (_state != null)
                _state.Initialize(_config, homeHouse);
            
            // Set initial position near home house if specified
            if (homeHouse != null)
            {
                Vector3 homePosition = homeHouse.transform.position;
                Vector3 randomOffset = Random.insideUnitCircle * _config.SpawnRadius;
                _cachedTransform.position = homePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
            }
        }
        
        /// <summary>
        /// Initialize with random data using the provided config.
        /// </summary>
        public void InitializeWithRandomData(House homeHouse = null, HumanConfig config = null)
        {
            _config = config ?? G.HumanConfig;
            _homeHouse = homeHouse;
            
            if (_config == null)
            {
                Debug.LogError("HumanConfig is null. Cannot initialize character with random data.");
                return;
            }
            
            // Generate random human data
            _humanData = HumanData.CreateRandom(_config);
            
            // Initialize with the generated data
            Initialize(_humanData, homeHouse, _config);
        }
        
        /// <summary>
        /// Update the character's state and behavior.
        /// </summary>
        public void UpdateCharacter(float deltaTime)
        {
            // Update state machine
            if (_state != null)
                _state.UpdateState(deltaTime);
            
            // Update movement based on state
            if (_movement != null)
                _movement.UpdateMovement(deltaTime);
            
            // Update visual animations based on state
            UpdateVisuals(deltaTime);
        }
        
        /// <summary>
        /// Set the behavior state of this character.
        /// </summary>
        public void SetBehaviorState(BehaviorState newState)
        {
            if (_state != null)
                _state.TransitionTo(newState);
        }
        
        /// <summary>
        /// Get the current behavior state.
        /// </summary>
        public BehaviorState GetCurrentBehaviorState()
        {
            return _state != null ? _state.CurrentState : BehaviorState.Idle;
        }
        
        /// <summary>
        /// Check if the character has reached its current destination.
        /// </summary>
        public bool HasReachedDestination()
        {
            return _movement != null && _movement.HasReachedDestination();
        }
        
        /// <summary>
        /// Move to a specific position.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            if (_movement != null)
                _movement.MoveTo(position);
        }
        
        /// <summary>
        /// Return to home position.
        /// </summary>
        public void ReturnHome()
        {
            if (_homeHouse != null && _movement != null)
            {
                Vector3 homePosition = _homeHouse.transform.position;
                _movement.MoveTo(homePosition);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CacheComponents()
        {
            // Get or add required components
            if (_visual == null)
                _visual = GetComponent<HumanVisual>();
            
            if (_movement == null)
                _movement = GetComponent<HumanMovement>();
            
            if (_state == null)
                _state = GetComponent<HumanState>();
            
            // Ensure we have a HumanVisual (it's required by the attribute)
            if (_visual == null)
                _visual = gameObject.AddComponent<HumanVisual>();
        }
        
        private void UpdateVisuals(float deltaTime)
        {
            if (_visual == null || _config == null)
                return;
            
            // Apply slime jump animation during wandering state
            if (_state != null && _state.CurrentState == BehaviorState.Wandering)
            {
                // Calculate animation time based on config frequency
                float jumpTime = Time.time * _config.SlimeJumpFrequency;
                _visual.ApplySlimeJump(jumpTime);
            }
            else
            {
                // Reset animation for other states
                _visual.ResetAnimation();
            }
        }
        
        private void RegisterWithSystems()
        {
            // Register with HumanManager if it exists
            if (G.HumanManager != null)
                G.HumanManager.RegisterHuman(this);
        }
        
        private void UnregisterFromSystems()
        {
            // Unregister from HumanManager if it exists
            if (G.HumanManager != null)
                G.HumanManager.UnregisterHuman(this);
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Initialize With Random Data")]
        private void InitializeWithRandomDataEditor()
        {
            if (Application.isPlaying)
                return;
                
            // Get config from G if available
            var config = G.HumanConfig;
            if (config == null)
            {
                Debug.LogWarning("No HumanConfig found in G. Cannot initialize with random data.");
                return;
            }
            
            InitializeWithRandomData(null, config);
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [ContextMenu("Setup Required Components")]
        private void SetupRequiredComponentsEditor()
        {
            if (Application.isPlaying)
                return;
                
            CacheComponents();
            
            // Ensure we have all required components
            if (_movement == null)
                _movement = gameObject.AddComponent<HumanMovement>();
            
            if (_state == null)
                _state = gameObject.AddComponent<HumanState>();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        
        #endregion
    }
}