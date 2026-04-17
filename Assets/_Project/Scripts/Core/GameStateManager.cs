// Path: Assets/_Project/Scripts/Core/GameStateManager.cs
using System;

using Core;

using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Simplified Finite State Machine for game states (Intro, Playing, Paused).
    /// Provides events for state changes and integrates with Main.cs.
    /// Registers itself as G.GameState.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [Header("Initial State")]
        [SerializeField] private GameState _initialState = GameState.Intro;

        // Current and previous states
        private GameState _currentState;
        private GameState _previousState;

        // Events
        public event Action<GameState> OnStateEnter;
        public event Action<GameState> OnStateExit;
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary> Current game state (read-only). </summary>
        public GameState CurrentState => _currentState;

        /// <summary> Previous game state before the last transition. </summary>
        public GameState PreviousState => _previousState;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.GameState != null && G.GameState != this)
            {
                Debug.LogWarning("[GameStateManager] Multiple GameStateManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.GameState = this;
            G.EnsureSystem("GameState", G.GameState);

            // Initialize state
            _currentState = _initialState;
            _previousState = _initialState;

            // Make persistent across scenes if needed
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Trigger initial state entry
            OnStateEnter?.Invoke(_currentState);
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.GameState == this)
                G.GameState = null;
        }

        // Zero allocations in Update
        private void Update()
        {
            // State-specific updates can be added here if needed
            // For now, we rely on external systems (like Main) to drive state changes.
        }

        #endregion

        #region Public API

        /// <summary>
        /// Transition to a new state.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState)
                return;

            _previousState = _currentState;
            GameState oldState = _currentState;
            _currentState = newState;

            // Exit old state
            OnStateExit?.Invoke(oldState);

            // Enter new state
            OnStateEnter?.Invoke(newState);

            // Notify change
            OnStateChanged?.Invoke(oldState, newState);

            Debug.Log($"[GameStateManager] State changed: {oldState} -> {newState}");
        }

        /// <summary>
        /// Check if the current state matches the given state.
        /// </summary>
        public bool IsState(GameState state) => _currentState == state;

        /// <summary>
        /// Get the current state.
        /// </summary>
        public GameState GetState() => _currentState;

        /// <summary>
        /// Return to the previous state.
        /// </summary>
        public void RevertToPreviousState()
        {
            if (_previousState == _currentState)
                return;

            SetState(_previousState);
        }

        /// <summary>
        /// Check if the current state is one of the given states.
        /// </summary>
        public bool IsAnyState(params GameState[] states)
        {
            foreach (GameState s in states)
            {
                if (_currentState == s)
                    return true;
            }
            return false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if the game is in intro (loading) state.
        /// </summary>
        public bool IsIntro() => _currentState == GameState.Intro;

        /// <summary>
        /// Check if the game is in playing state.
        /// </summary>
        public bool IsPlaying() => _currentState == GameState.Playing;

        /// <summary>
        /// Check if the game is paused.
        /// </summary>
        public bool IsPaused() => _currentState == GameState.Paused;

        #endregion
    }
}