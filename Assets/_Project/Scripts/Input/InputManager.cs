// Path: Assets/_Project/Scripts/Input/InputManager.cs

using System;

using Core;

using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSystem
{
    /// <summary>
    /// Minimal input manager for Ludum Dare 59.
    /// Only handles pause (Esc) input.
    /// Registers itself as G.Input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Asset")]
        [SerializeField] private InputActionAsset _inputAsset;
        [SerializeField] private string _uiMapName = "UI";

        // Cached action map
        private InputActionMap _uiMap;
        private InputAction _pauseAction;

        // Event
        public event Action OnPausePressed;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.Input != null && G.Input != this)
            {
                Debug.LogWarning("[InputManager] Multiple InputManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.Input = this;
            G.EnsureSystem("Input", G.Input);

            // Ensure we have an input asset
            if (_inputAsset == null)
            {
                Debug.LogError("[InputManager] No InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            // Cache UI map
            _uiMap = _inputAsset.FindActionMap(_uiMapName);
            if (_uiMap == null)
            {
                Debug.LogWarning($"[InputManager] UI action map '{_uiMapName}' not found.", this);
                enabled = false;
                return;
            }

            // Cache pause action
            _pauseAction = _uiMap.FindAction("Pause");
            if (_pauseAction == null)
            {
                Debug.LogWarning("[InputManager] Pause action not found.", this);
                enabled = false;
                return;
            }

            // Make this object persistent across scenes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Subscribe to pause action
            if (_pauseAction != null)
                _pauseAction.performed += OnPausePerformed;
        }

        private void OnEnable()
        {
            // Enable UI map when component is enabled
            if (_uiMap != null && _uiMap.enabled == false)
                _uiMap.Enable();
        }

        private void OnDisable()
        {
            // Disable UI map when component is disabled
            if (_uiMap != null)
                _uiMap.Disable();
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.Input == this)
                G.Input = null;

            // Unsubscribe
            if (_pauseAction != null)
                _pauseAction.performed -= OnPausePerformed;
        }

        #endregion

        #region Public API

        /// <summary> Check if the pause button was pressed this frame. </summary>
        public bool WasPausePressed() => _pauseAction?.WasPressedThisFrame() ?? false;

        #endregion

        #region Private Helpers

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            OnPausePressed?.Invoke();
        }

        #endregion
    }
}