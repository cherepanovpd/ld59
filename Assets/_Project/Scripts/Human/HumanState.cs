// Path: Assets/_Project/Scripts/Human/HumanState.cs

using Core;

using Project.Core.Events;
using Project.Human.Config;
using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Behavior states for human characters.
    /// </summary>
    public enum BehaviorState
    {
        Idle,           // Initial state
        Wandering,      // Day: Moving randomly with slime jump
        ReturningHome,  // Night: Running back to home
        AtHome,         // At home position (night)
        ExitingHome     // Morning: Coming out of home
    }
    
    /// <summary>
    /// Manages behavior state machine for human characters.
    /// Handles state transitions and timing.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanState : MonoBehaviour
    {
        [Header("Current State")]
        [SerializeField] private BehaviorState _currentState = BehaviorState.Idle;
        [SerializeField] private float _stateTimer = 0f;
        
        [Header("References")]
        [SerializeField] private House _homeHouse;
        [SerializeField] private HumanConfig _config;
        
        // Cached components for performance
        private HumanMovement _movement;
        private HumanCharacter _character;
        
        /// <summary>
        /// Current behavior state.
        /// </summary>
        public BehaviorState CurrentState => _currentState;
        
        /// <summary>
        /// Time spent in current state.
        /// </summary>
        public float StateTimer => _stateTimer;
        
        /// <summary>
        /// The home house this character belongs to.
        /// </summary>
        public House HomeHouse => _homeHouse;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
        }
        
        private void Start()
        {
            // Subscribe to day/night events if we have an event system
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initialize with configuration and home house.
        /// </summary>
        public void Initialize(HumanConfig config, House homeHouse)
        {
            _config = config;
            _homeHouse = homeHouse;
            
            CacheComponents();
            
            // Start in idle state
            TransitionTo(BehaviorState.Idle);
        }
        
        /// <summary>
        /// Update the state machine.
        /// </summary>
        public void UpdateState(float deltaTime)
        {
            _stateTimer += deltaTime;
            
            // State-specific updates
            switch (_currentState)
            {
                case BehaviorState.Wandering:
                    UpdateWanderingState(deltaTime);
                    break;
                    
                case BehaviorState.ReturningHome:
                    UpdateReturningHomeState(deltaTime);
                    break;
                    
                case BehaviorState.ExitingHome:
                    UpdateExitingHomeState(deltaTime);
                    break;
                    
                case BehaviorState.AtHome:
                    UpdateAtHomeState(deltaTime);
                    break;
                case BehaviorState.Idle:
                    // No special updates needed
                    break;
            }
            
            // Check for automatic state transitions
            CheckAutomaticTransitions();
        }

        private void UpdateAtHomeState(float deltaTime)
        {
            this.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Transition to a new behavior state.
        /// </summary>
        public void TransitionTo(BehaviorState newState)
        {
            if (_currentState == newState)
                return;
            
            // Exit current state
            OnStateExit(_currentState);
            
            // Update state
            BehaviorState previousState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;
            
            // Enter new state
            OnStateEnter(newState, previousState);
        }
        
        /// <summary>
        /// Force return home (for night time).
        /// </summary>
        public void ForceReturnHome()
        {
            if (_currentState != BehaviorState.AtHome && _currentState != BehaviorState.ReturningHome)
            {
                TransitionTo(BehaviorState.ReturningHome);
            }
        }
        
        /// <summary>
        /// Force exit home (for morning).
        /// </summary>
        public void ForceExitHome()
        {
            if (_currentState == BehaviorState.AtHome)
            {
                TransitionTo(BehaviorState.ExitingHome);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CacheComponents()
        {
            if (_movement == null)
                _movement = GetComponent<HumanMovement>();
            
            if (_character == null)
                _character = GetComponent<HumanCharacter>();
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to day/night events via G.Events if available
            if (G.Events != null)
            {
                // Note: Event subscription would be implemented when EventSystem is available
                // G.Events.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
                // G.Events.Subscribe<WindowLightsChangedEvent>(OnWindowLightsChanged);
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from events
            if (G.Events != null)
            {
                // G.Events.Unsubscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
                // G.Events.Unsubscribe<WindowLightsChangedEvent>(OnWindowLightsChanged);
            }
        }
        
        private void OnStateEnter(BehaviorState state, BehaviorState previousState)
        {
            switch (state)
            {
                case BehaviorState.Wandering:
                    OnEnterWandering();
                    break;
                    
                case BehaviorState.ReturningHome:
                    OnEnterReturningHome();
                    break;
                    
                case BehaviorState.AtHome:
                    OnEnterAtHome();
                    break;
                    
                case BehaviorState.ExitingHome:
                    OnEnterExitingHome();
                    break;
                    
                case BehaviorState.Idle:
                    OnEnterIdle();
                    break;
            }
        }
        
        private void OnStateExit(BehaviorState state)
        {
            switch (state)
            {
                case BehaviorState.Wandering:
                    OnExitWandering();
                    break;
                    
                case BehaviorState.ReturningHome:
                    OnExitReturningHome();
                    break;
                    
                case BehaviorState.AtHome:
                    OnExitAtHome();
                    break;
                    
                case BehaviorState.ExitingHome:
                    OnExitExitingHome();
                    break;
                    
                case BehaviorState.Idle:
                    OnExitIdle();
                    break;
            }
        }
        
        private void OnEnterWandering()
        {
            // Set movement speed for day time
            if (_movement != null && _config != null)
            {
                _movement.SetSpeed(_config.DayMovementSpeed);
            }
            
            // Start wandering immediately
            if (_movement != null)
            {
                Vector3 wanderPos = _movement.GetRandomWanderPosition();
                _movement.MoveTo(wanderPos);
            }
        }
        
        private void OnExitWandering()
        {
            // Stop movement
            if (_movement != null)
                _movement.Stop();
        }
        
        private void OnEnterReturningHome()
        {
            // Set movement speed for night time (faster)
            if (_movement != null && _config != null)
            {
                _movement.SetSpeed(_config.NightMovementSpeed);
            }
            
            // Move towards home
            if (_homeHouse != null && _movement != null)
            {
                Vector3 homePosition = _homeHouse.transform.position;
                _movement.MoveTo(homePosition);
            }
        }
        
        private void OnExitReturningHome()
        {
            // Nothing special
        }
        
        private void OnEnterAtHome()
        {
            // Stop movement
            if (_movement != null)
                _movement.Stop();
            
            // Position at home
            if (_homeHouse != null)
            {
                transform.position = _homeHouse.transform.position;
            }
        }
        
        private void OnExitAtHome()
        {
            // Nothing special
        }
        
        private void OnEnterExitingHome()
        {
            // Set movement speed for day time
            if (_movement != null && _config != null)
            {
                _movement.SetSpeed(_config.DayMovementSpeed);
            }
            
            // Move away from home
            if (_homeHouse != null && _movement != null)
            {
                Vector3 homePosition = _homeHouse.transform.position;
                Vector3 exitDirection = Random.insideUnitCircle.normalized;
                Vector3 exitPosition = homePosition + new Vector3(exitDirection.x, exitDirection.y, 0f) * 2f;
                _movement.MoveTo(exitPosition);
            }
        }
        
        private void OnExitExitingHome()
        {
            // Nothing special
        }
        
        private void OnEnterIdle()
        {
            // Stop movement
            if (_movement != null)
                _movement.Stop();
        }
        
        private void OnExitIdle()
        {
            // Nothing special
        }
        
        private void UpdateWanderingState(float deltaTime)
        {
            if (_movement == null)
                return;
            
            // Update wandering behavior
            _movement.UpdateWandering(deltaTime);
            
            // Check if we should get a new wander target
            if (_movement.HasReachedDestination())
            {
                Vector3 wanderPos = _movement.GetRandomWanderPosition();
                _movement.MoveTo(wanderPos);
            }
        }
        
        private void UpdateReturningHomeState(float deltaTime)
        {
            if (_movement == null || _homeHouse == null)
                return;
            
            // Check if we've reached home
            if (_movement.HasReachedDestination())
            {
                TransitionTo(BehaviorState.AtHome);
            }
        }
        
        private void UpdateExitingHomeState(float deltaTime)
        {
            if (_movement == null)
                return;
            
            this.transform.localScale = Vector3.one;
            
            // Check if we've exited far enough from home
            if (_movement.HasReachedDestination())
            {
                TransitionTo(BehaviorState.Wandering);
            }
        }
        
        private void CheckAutomaticTransitions()
        {
            // This would check time of day and other conditions
            // For now, we rely on external events to trigger transitions
            
            // Example: If we're returning home and reached destination, go to AtHome
            if (_currentState == BehaviorState.ReturningHome && _movement != null)
            {
                if (_movement.HasReachedDestination())
                {
                    TransitionTo(BehaviorState.AtHome);
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        // These would be connected to the EventSystem
        private void OnDayPhaseChanged(DayPhaseChangedEvent e)
        {
            // Implement based on day/night system
            // For example:
            // if (e.NewPhase == Phase.Dawn) TransitionTo(BehaviorState.ExitingHome);
            // else if (e.NewPhase == Phase.Dusk) TransitionTo(BehaviorState.ReturningHome);
        }
        
        private void OnWindowLightsChanged(WindowLightsChangedEvent e)
        {
            // When lights turn on at night, return home
            if (e.LightsOn)
            {
                ForceReturnHome();
            }
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        [ContextMenu("Transition to Wandering")]
        private void TransitionToWanderingEditor()
        {
            if (Application.isPlaying)
                TransitionTo(BehaviorState.Wandering);
        }
        
        [ContextMenu("Transition to Returning Home")]
        private void TransitionToReturningHomeEditor()
        {
            if (Application.isPlaying)
                TransitionTo(BehaviorState.ReturningHome);
        }
        
        [ContextMenu("Transition to At Home")]
        private void TransitionToAtHomeEditor()
        {
            if (Application.isPlaying)
                TransitionTo(BehaviorState.AtHome);
        }
#endif
        
        #endregion
    }
}