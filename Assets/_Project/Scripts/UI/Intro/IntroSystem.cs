// Path: Assets/_Project/Scripts/UI/Intro/IntroSystem.cs

using System;
using Common.Runtime.Components;
using Core;
using DG.Tweening;
using Project.Audio;
using Project.Core;
using Project.UI.Intro;
using UnityEngine;

namespace Project.UI.Intro
{
    /// <summary>
    /// Finite states for the intro sequence.
    /// </summary>
    public enum IntroState
    {
        /// <summary> System is idle, waiting for start. </summary>
        Idle,
        /// <summary> First text is fading in. </summary>
        FadeInText1,
        /// <summary> Pause after first text is fully visible. </summary>
        PauseAfterText1,
        /// <summary> First text is fading out. </summary>
        FadeOutText1,
        /// <summary> Second text is fading in. </summary>
        FadeInText2,
        /// <summary> Pause after second text is fully visible. </summary>
        PauseAfterText2,
        /// <summary> Second text is fading out. </summary>
        FadeOutText2,
        /// <summary> Background is fading out. </summary>
        FadeOutBackground,
        /// <summary> Intro sequence completed, transitioning to gameplay. </summary>
        Complete
    }

    /// <summary>
    /// Main controller for the intro cinematic sequence.
    /// Orchestrates text fades, camera shake, audio effects, and skip functionality.
    /// Follows the G service locator pattern.
    /// </summary>
    [RequireComponent(typeof(IntroCanvas))]
    public class IntroSystem : MonoBehaviour
    {
        [Header("Sequence Timing")]
        [SerializeField, Tooltip("Duration of text fade in (seconds).")]
        [Range(0.1f, 10f)]
        private float _fadeInDuration = 1.5f;

        [SerializeField, Tooltip("Duration of text fade out (seconds).")]
        [Range(0.1f, 10f)]
        private float _fadeOutDuration = 1.0f;

        [SerializeField, Tooltip("Duration of pause between fade in and fade out (seconds).")]
        [Range(0.1f, 10f)]
        private float _pauseDuration1 = 2.0f;
        [SerializeField, Tooltip("Duration of pause between fade in and fade out (seconds).")]
        [Range(0.1f, 10f)]
        private float _pauseDuration2 = 2.0f;

        [SerializeField, Tooltip("Duration of background fade out (seconds).")]
        [Range(0.1f, 10f)]
        private float _backgroundFadeDuration = 2.0f;

        [Header("Effects")]
        [SerializeField, Tooltip("Intensity of camera shake (0-10).")]
        [Range(0f, 10f)]
        private float _shakeIntensity = 0.5f;

        [Space]
        [SerializeField, Tooltip("Audio key for text fade in effect.")]
        private string _audioKeyFadeIn = "intro_text_fade_in";

        [SerializeField, Tooltip("Audio key for text fade out effect.")]
        private string _audioKeyFadeOut = "intro_text_fade_out";

        [Header("References")]
        [SerializeField, Tooltip("Reference to the intro canvas component.")]
        private IntroCanvas _introCanvas;

        [Header("Development")]
        [SerializeField, Tooltip("If enabled, automatically skip intro when running in Unity Editor.")]
        private bool _autoSkipInEditor = false;

        // State machine
        private IntroState _currentState = IntroState.Idle;
        private float _stateTimer;
        private bool _isSkipping;

        // Track current alpha values for tweening
        private float _text1Alpha = 0f;
        private float _text2Alpha = 0f;
        private float _backgroundAlpha = 1f;
        private Tween _currentTween;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration with G service locator
            if (G.Intro != null && G.Intro != this)
            {
                Debug.LogWarning("[IntroSystem] Multiple IntroSystem instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.Intro = this;
            G.EnsureSystem("Intro", G.Intro);

            // Cache references
            if (_introCanvas == null)
                _introCanvas = GetComponent<IntroCanvas>();

            // Ensure canvas is in correct initial state
            InitializeCanvas();

            // Subscribe to skip events from canvas
            if (_introCanvas != null)
                _introCanvas.OnSkipRequested += HandleSkipRequested;
        }

        private void Start()
        {
            // Start intro automatically if game state is Intro
            if (G.HasGameState() && G.GameState.CurrentState == GameState.Intro)
            {
                StartIntro();
            }
            else
            {
                // Wait for GameState change event
                if (G.HasGameState())
                    G.GameState.OnStateEnter += OnGameStateEnter;
            }
        }

        private void OnDestroy()
        {
            // Unregister from G
            if (G.Intro == this)
                G.Intro = null;

            // Unsubscribe from events
            if (G.HasGameState())
                G.GameState.OnStateEnter -= OnGameStateEnter;

            // Unsubscribe from canvas skip event
            if (_introCanvas != null)
                _introCanvas.OnSkipRequested -= HandleSkipRequested;

            // Kill any active tweens
            _currentTween?.Kill();
        }

        private void Update()
        {
            // Zero‑allocation state machine update
            switch (_currentState)
            {
                case IntroState.Idle:
                case IntroState.Complete:
                    // Nothing to do
                    break;

                case IntroState.PauseAfterText1:
                case IntroState.PauseAfterText2:
                    UpdatePauseState();
                    break;

                // Other states are driven by tween callbacks, no need for timer updates
            }

            // Skip detection (mouse click) in any active state except Idle and Complete
            if (_currentState != IntroState.Idle && _currentState != IntroState.Complete)
            {
                CheckForSkip();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start the intro sequence manually.
        /// </summary>
        public void StartIntro()
        {
            if (_currentState != IntroState.Idle)
            {
                Debug.LogWarning("[IntroSystem] Intro already started.");
                return;
            }

            // Auto-skip in Unity Editor if enabled
#if UNITY_EDITOR
            if (_autoSkipInEditor)
            {
                Debug.Log("[IntroSystem] Auto-skip enabled in Unity Editor. Skipping intro.");
                TransitionToState(IntroState.Complete);
                return;
            }
#endif

            Debug.Log("[IntroSystem] Starting intro sequence.");
            _isSkipping = false;
            TransitionToState(IntroState.FadeInText1);
        }

        /// <summary>
        /// Skip the current intro step, moving to fade out of the currently displayed text.
        /// </summary>
        public void Skip()
        {
            if (_currentState == IntroState.Idle || _currentState == IntroState.Complete)
                return;

            Debug.Log("[IntroSystem] Skip requested.");
            _isSkipping = true;

            // Based on current state, determine the appropriate skip action
            switch (_currentState)
            {
                case IntroState.FadeInText1:
                case IntroState.PauseAfterText1:
                    // Skip to fade out of text 1
                    _currentTween?.Kill();
                    TransitionToState(IntroState.FadeOutText1);
                    break;

                case IntroState.FadeOutText1:
                    // Already fading out, let it continue
                    break;

                case IntroState.FadeInText2:
                case IntroState.PauseAfterText2:
                    // Skip to fade out of text 2
                    _currentTween?.Kill();
                    TransitionToState(IntroState.FadeOutText2);
                    break;

                case IntroState.FadeOutText2:
                    // Already fading out, let it continue
                    break;

                case IntroState.FadeOutBackground:
                    // Skip background fade, go directly to complete
                    _currentTween?.Kill();
                    TransitionToState(IntroState.Complete);
                    break;

                default:
                    // Unexpected state, force completion
                    TransitionToState(IntroState.Complete);
                    break;
            }
        }

        #endregion

        #region Private State Machine

        private void TransitionToState(IntroState newState)
        {
            if (_currentState == newState)
                return;

            var oldState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;

            Debug.Log($"[IntroSystem] State transition: {oldState} -> {newState}");

            // Exit old state (if needed)
            OnStateExit(oldState);

            // Enter new state
            OnStateEnter(newState);
        }

        private void OnStateEnter(IntroState state)
        {
            switch (state)
            {
                case IntroState.FadeInText1:
                    StartFadeInText(1);
                    break;

                case IntroState.PauseAfterText1:
                    StartPause(_pauseDuration1);
                    break;

                case IntroState.FadeOutText1:
                    StartFadeOutText(1);
                    break;

                case IntroState.FadeInText2:
                    StartFadeInText(2);
                    break;

                case IntroState.PauseAfterText2:
                    StartPause(_pauseDuration2);
                    break;

                case IntroState.FadeOutText2:
                    StartFadeOutText(2);
                    break;

                case IntroState.FadeOutBackground:
                    StartFadeOutBackground();
                    break;

                case IntroState.Complete:
                    CompleteIntro();
                    break;
            }
        }

        private void OnStateExit(IntroState state)
        {
            // Clean up any state‑specific resources
        }

        #endregion

        #region State Implementations

        private void InitializeCanvas()
        {
            if (_introCanvas == null) return;

            // Ensure background visible, texts invisible
            _introCanvas.SetBackgroundAlpha(1f);
            _introCanvas.SetText1Alpha(0f);
            _introCanvas.SetText2Alpha(0f);
            _introCanvas.SetText1Visible(true);
            _introCanvas.SetText2Visible(true);

            // Sync tracked alpha values
            _text1Alpha = 0f;
            _text2Alpha = 0f;
            _backgroundAlpha = 1f;
        }

        private void StartFadeInText(int textIndex)
        {
            if (_introCanvas == null) return;

            // Trigger camera shake and sound effect
            TriggerCameraShake();
            PlayAudioEffect(_audioKeyFadeIn);

            // Determine target alpha (1) and duration
            float targetAlpha = 1f;
            float duration = _fadeInDuration;

            // Create tween
            _currentTween = DOTween.To(
                () => textIndex == 1 ? _text1Alpha : _text2Alpha,
                alpha =>
                {
                    if (textIndex == 1)
                    {
                        _text1Alpha = alpha;
                        _introCanvas.SetText1Alpha(alpha);
                    }
                    else
                    {
                        _text2Alpha = alpha;
                        _introCanvas.SetText2Alpha(alpha);
                    }
                },
                targetAlpha,
                duration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    // Transition to pause state after fade in
                    if (textIndex == 1)
                        TransitionToState(IntroState.PauseAfterText1);
                    else
                        TransitionToState(IntroState.PauseAfterText2);
                });
        }

        private void StartPause(float duration)
        {
            // Pause state just waits for timer; skip is handled in Update
            _stateTimer = duration;
        }

        private void UpdatePauseState()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f || _isSkipping)
            {
                if (_currentState == IntroState.PauseAfterText1)
                    TransitionToState(IntroState.FadeOutText1);
                else if (_currentState == IntroState.PauseAfterText2)
                    TransitionToState(IntroState.FadeOutText2);
            }
        }

        private void StartFadeOutText(int textIndex)
        {
            if (_introCanvas == null) return;

            // Trigger camera shake and sound effect
            TriggerCameraShake();
            PlayAudioEffect(_audioKeyFadeOut);

            float duration = _fadeOutDuration;

            _currentTween = DOTween.To(
                () => textIndex == 1 ? _text1Alpha : _text2Alpha,
                alpha =>
                {
                    if (textIndex == 1)
                    {
                        _text1Alpha = alpha;
                        _introCanvas.SetText1Alpha(alpha);
                    }
                    else
                    {
                        _text2Alpha = alpha;
                        _introCanvas.SetText2Alpha(alpha);
                    }
                },
                0f,
                duration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    // After text 1 fade out, move to text 2 fade in
                    // After text 2 fade out, move to background fade out
                    if (textIndex == 1)
                        TransitionToState(IntroState.FadeInText2);
                    else
                        TransitionToState(IntroState.FadeOutBackground);
                });
        }

        private void StartFadeOutBackground()
        {
            if (_introCanvas == null) return;

            _currentTween = DOTween.To(
                () => _backgroundAlpha,
                alpha =>
                {
                    _backgroundAlpha = alpha;
                    _introCanvas.SetBackgroundAlpha(alpha);
                },
                0f,
                _backgroundFadeDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    TransitionToState(IntroState.Complete);
                });
        }

        private void CompleteIntro()
        {
            Debug.Log("[IntroSystem] Intro sequence completed.");

            // Notify Main to start the game
            if (G.HasMain())
                G.Main.StartGame();
            else
                Debug.LogError("[IntroSystem] G.Main is null, cannot start game.");

            // Optionally deactivate canvas
            if (_introCanvas != null)
                _introCanvas.gameObject.SetActive(false);
        }

        #endregion

        #region Helper Methods

        private void TriggerCameraShake()
        {
            // Fallback to G.ScreenShake if registered
            if (G.HasCamera())
            {
                G.Camera.UpdateScreenShakeSetting(_shakeIntensity);
                return;
            }

            Debug.LogWarning("[IntroSystem] ScreenShake component not found.");
        }

        private void HandleSkipRequested()
        {
            Skip();
        }

        private void PlayAudioEffect(string key)
        {
            if (G.HasAudio())
                G.Audio.PlaySFX(key);
            else
                Debug.LogWarning($"[IntroSystem] AudioManager not available, cannot play SFX: {key}");
        }

        private void CheckForSkip()
        {
            // Use InputManager for click detection (assuming G.Input provides WasClickPressed)
            if (G.HasInput() && G.Input.WasClickPressed())
            {
                Skip();
            }
        }

        private void OnGameStateEnter(GameState state)
        {
            if (state == GameState.Intro)
            {
                StartIntro();
            }
        }

        #endregion
    }
}