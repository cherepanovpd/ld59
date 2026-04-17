// Path: Assets/_Project/Scripts/Core/Main.cs

using Project.Core;

using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    /// <summary>
    /// Core gameplay orchestrator for Ludum Dare 59.
    /// Manages game state and high-level flow (Intro, Playing, Paused).
    /// Registers itself as G.Main.
    /// </summary>
    public class Main : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.Intro;
        [SerializeField] private GameState _previousState = GameState.Intro;

        [Header("Events")]
        [SerializeField] private UnityEvent _onGameStart = new UnityEvent();
        [SerializeField] private UnityEvent _onGamePause = new UnityEvent();
        [SerializeField] private UnityEvent _onGameResume = new UnityEvent();

        // Cached references
        private Transform _cachedTransform;

        /// <summary> Current game state (read-only). </summary>
        public GameState CurrentState => _currentState;

        /// <summary> Previous game state before the last transition. </summary>
        public GameState PreviousState => _previousState;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.Main != null && G.Main != this)
            {
                Debug.LogWarning("[Main] Multiple Main instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.Main = this;
            G.EnsureSystem("Main", G.Main);

            // Cache references
            _cachedTransform = transform;

            // Ensure this object persists across scenes if needed
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Initialize based on starting state
            ApplyState(_currentState);
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.Main == this)
                G.Main = null;
        }

        private void Update()
        {
            // Zero‑allocation state‑specific updates
            switch (_currentState)
            {
                case GameState.Intro:
                    UpdateIntro();
                    break;
                case GameState.Playing:
                    UpdatePlaying();
                    break;
                case GameState.Paused:
                    UpdatePaused();
                    break;
            }
        }

        #endregion

        #region Public API

        /// <summary> Transition from Intro to Playing state. </summary>
        public void StartGame()
        {
            if (_currentState == GameState.Playing)
                return;

            Debug.Log("[Main] Game started.");
            SetState(GameState.Playing);
            _onGameStart?.Invoke();

            // Notify other systems via G
        }

        /// <summary> Pause the game (sets time scale to 0). </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing)
                return;

            Debug.Log("[Main] Game paused.");
            SetState(GameState.Paused);
            Time.timeScale = 0f;
            _onGamePause?.Invoke();

            if (G.HasUI())
                G.UI.ShowPauseMenu();
        }

        /// <summary> Resume from pause. </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused)
                return;

            Debug.Log("[Main] Game resumed.");
            SetState(_previousState);
            Time.timeScale = 1f;
            _onGameResume?.Invoke();

            if (G.HasUI())
                G.UI.HidePauseMenu();
        }

        /// <summary> Quit the application (works in builds). </summary>
        public void QuitGame()
        {
            Debug.Log("[Main] Quitting game.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Private Helpers

        private void SetState(GameState newState)
        {
            if (_currentState == newState)
                return;

            _previousState = _currentState;
            _currentState = newState;
            ApplyState(newState);

            // Sync with GameStateManager if available
            if (G.HasGameState() && G.GameState.CurrentState != newState)
            {
                G.GameState.SetState(newState);
            }
        }

        private void ApplyState(GameState state)
        {
            // Apply state‑specific logic
            switch (state)
            {
                case GameState.Intro:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    break;
            }
        }

        // Zero‑allocation update methods
        private void UpdateIntro()
        {
            // Intro-specific updates (e.g., wait for any key to start)
            if (G.HasInput() && G.Input.WasPausePressed())
            {
                // Treat pause button as start during intro
                StartGame();
            }
        }

        private void UpdatePlaying()
        {
            // Check for pause input
            if (G.HasInput() && G.Input.WasPausePressed())
            {
                PauseGame();
            }
        }

        private void UpdatePaused()
        {
            // Check for pause input to resume
            if (G.HasInput() && G.Input.WasPausePressed())
            {
                ResumeGame();
            }
        }

        #endregion
    }
}