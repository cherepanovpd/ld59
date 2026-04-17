// Path: Assets/_Project/Scripts/UI/UIManager.cs

using Core;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// UI screen base class for all screens.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0.99f && _canvasGroup.interactable;

        /// <summary>
        /// Show the screen with optional fade.
        /// </summary>
        public virtual void Show(bool instant = false)
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (instant)
            {
                _canvasGroup.alpha = 1f;
            }
            else
            {
                // Using DOTween would require DOTween module; for simplicity we use linear interpolation.
                // If DOTween is available, you can replace with DOFade.
                _canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Hide the screen with optional fade.
        /// </summary>
        public virtual void Hide(bool instant = false)
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (instant)
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                _canvasGroup.alpha = 0f;
            }
        }
    }

    /// <summary>
    /// Manages only the pause menu UI.
    /// Registers itself as G.UI.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Pause Screen")]
        [SerializeField] private UIScreen _pauseScreen;

        [Header("Volume Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;

        // Cached references
        private Transform _cachedTransform;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.UI != null && G.UI != this)
            {
                Debug.LogWarning("[UIManager] Multiple UIManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.UI = this;
            G.EnsureSystem("UI", G.UI);

            _cachedTransform = transform;

            // Hide pause screen initially
            if (_pauseScreen != null)
                _pauseScreen.Hide(true);

            // Setup button listeners
            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);

            // Setup slider listeners
            if (_masterVolumeSlider != null)
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (_musicVolumeSlider != null)
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // Load saved volume values
            LoadVolumeSettings();

            // Make persistent across scenes if needed
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // No automatic screen showing; pause menu is triggered by input
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.UI == this)
                G.UI = null;
        }

        // Zero allocations in Update
        private void Update()
        {
            // Handle pause input
            if (G.HasInput() && G.Input.WasPausePressed())
            {
                TogglePauseMenu();
            }
        }

        #endregion

        #region Public API - Pause Menu

        /// <summary>
        /// Show the pause menu.
        /// </summary>
        public void ShowPauseMenu()
        {
            if (_pauseScreen == null)
                return;

            _pauseScreen.Show();
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Hide the pause menu.
        /// </summary>
        public void HidePauseMenu()
        {
            if (_pauseScreen == null)
                return;

            _pauseScreen.Hide();
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Toggle pause menu visibility.
        /// </summary>
        public void TogglePauseMenu()
        {
            if (_pauseScreen == null)
                return;

            if (_pauseScreen.IsVisible)
                HidePauseMenu();
            else
                ShowPauseMenu();
        }

        #endregion

        #region Private Helpers

        private void OnContinueClicked()
        {
            HidePauseMenu();
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (G.HasAudio())
                G.Audio.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (G.HasAudio())
                G.Audio.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (G.HasAudio())
                G.Audio.SetSFXVolume(value);
        }

        private void LoadVolumeSettings()
        {
            if (G.HasAudio())
            {
                if (_masterVolumeSlider != null)
                    _masterVolumeSlider.value = G.Audio.GetMasterVolume();
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.value = G.Audio.GetMusicVolume();
                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.value = G.Audio.GetSFXVolume();
            }
            else
            {
                // Default values
                if (_masterVolumeSlider != null)
                    _masterVolumeSlider.value = 1f;
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.value = 0.8f;
                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.value = 1f;
            }
        }

        #endregion
    }
}