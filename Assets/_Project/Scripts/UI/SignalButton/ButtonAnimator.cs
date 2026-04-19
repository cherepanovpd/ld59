// Path: Assets/_Project/Scripts/UI/SignalButton/ButtonAnimator.cs

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Project.UI.SignalButton.Config;

namespace Project.UI.SignalButton
{
    /// <summary>
    /// Handles visual animations for the signal button UI system.
    /// Provides smooth hover scaling, outline activation, and click feedback.
    /// Follows strict performance rules: zero allocations in Update loop, cached references,
    /// and interruptible animations using DOTween.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("RectTransform of the button to scale (defaults to this object)")]
        private RectTransform _buttonTransform;
        
        [SerializeField, Tooltip("Outline GameObject that will be activated/deactivated on hover")]
        private GameObject _outlineObject;
        
        [SerializeField, Tooltip("Optional Image component for color changes (e.g., tint on click)")]
        private Image _buttonImage;
        
        [SerializeField, Tooltip("Unity Button component for click events (defaults to this object)")]
        private Button _unityButton;

        [Header("Configuration")]
        [SerializeField, Tooltip("Signal button configuration containing timing and visual parameters")]
        private SignalButtonConfig _signalConfig;
        
        [SerializeField, Tooltip("Animation configuration containing curves for all transitions")]
        private AnimationConfig _animationConfig;

        [Header("Animation Settings")]
        [SerializeField, Range(0.01f, 2f), Tooltip("Duration of hover scale animation in seconds (overrides config if >0)")]
        private float _hoverDurationOverride = 0f;
        
        [SerializeField, Range(0.01f, 2f), Tooltip("Duration of click scale animation in seconds (overrides config if >0)")]
        private float _clickDurationOverride = 0f;
        
        [SerializeField, Tooltip("If true, outline will fade in/out using alpha animation instead of toggle")]
        private bool _useOutlineFade = true;

        // Cached references for performance
        private Vector3 _originalScale;
        private CanvasGroup _outlineCanvasGroup;
        private Sequence _hoverSequence;
        private Sequence _clickSequence;
        private bool _isHovered;
        private bool _isAnimatingClick;

        // Cached animation curves to avoid repeated allocations
        private AnimationCurve _cachedHoverScaleCurve;
        private AnimationCurve _cachedOutlineFadeCurve;
        private AnimationCurve _cachedClickScaleCurve;

        // Pre-allocated color for click flash to avoid GC allocations
        private static readonly Color ClickFlashColor = new Color(0.8f, 0.8f, 1f);

        // Public properties
        public bool IsHovered => _isHovered;
        public SignalButtonConfig SignalConfig => _signalConfig;
        public AnimationConfig AnimationConfig => _animationConfig;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
            ValidateComponents();
            StoreOriginalState();
            SetupOutlineComponent();
            CacheAnimationCurves();
        }

        private void OnEnable()
        {
            // Ensure animations are reset when object becomes active
            ResetToOriginalState();
        }

        private void OnDisable()
        {
            // Kill running tweens to prevent memory leaks and unexpected behavior
            KillAnimations();
            ResetToOriginalState();
        }

        private void OnDestroy()
        {
            KillAnimations();
            // Clean up cached curves (they are not managed resources, but we null them for clarity)
            _cachedHoverScaleCurve = null;
            _cachedOutlineFadeCurve = null;
            _cachedClickScaleCurve = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually trigger hover enter animation (e.g., for programmatic hover).
        /// </summary>
        public void HandleHoverEnter()
        {
            if (!isActiveAndEnabled) return;
            
            _isHovered = true;
            PlayHoverEnterAnimation();
        }

        /// <summary>
        /// Manually trigger hover exit animation (e.g., for programmatic hover).
        /// </summary>
        public void HandleHoverExit()
        {
            if (!isActiveAndEnabled) return;
            
            _isHovered = false;
            PlayHoverExitAnimation();
        }

        /// <summary>
        /// Manually trigger click animation (e.g., for programmatic clicks).
        /// </summary>
        public void HandleClick()
        {
            if (!isActiveAndEnabled) return;
            
            PlayClickAnimation();
        }

        /// <summary>
        /// Immediately reset all visual properties to their original state.
        /// </summary>
        public void ResetToOriginalState()
        {
            KillAnimations();
            
            if (_buttonTransform != null)
            {
                _buttonTransform.localScale = _originalScale;
            }
            
            SetOutlineActive(false, immediate: true);
            
            if (_buttonImage != null)
            {
                _buttonImage.color = Color.white;
            }
        }

        #endregion

        #region Pointer Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            HandleHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HandleHoverExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick();
        }

        #endregion

        #region Animation Implementation

        private void PlayHoverEnterAnimation()
        {
            // Kill any ongoing hover animation to ensure interruptibility
            _hoverSequence?.Kill();
            
            float duration = _hoverDurationOverride > 0 ? _hoverDurationOverride : _signalConfig.HoverAnimationDuration;
            float targetScale = _signalConfig.HoverScaleMultiplier;
            
            _hoverSequence = DOTween.Sequence();
            _hoverSequence.SetRecyclable(true);
            _hoverSequence.SetAutoKill(false);
            
            // Scale animation using cached curve
            _hoverSequence.Append(
                _buttonTransform.DOScale(_originalScale * targetScale, duration)
                    .SetEase(_cachedHoverScaleCurve)
            );
            
            // Outline activation
            if (_outlineObject != null)
            {
                if (_useOutlineFade && _outlineCanvasGroup != null)
                {
                    _outlineCanvasGroup.alpha = 0f;
                    _outlineObject.SetActive(true);
                    _hoverSequence.Join(
                        _outlineCanvasGroup.DOFade(1f, duration)
                            .SetEase(_cachedOutlineFadeCurve)
                    );
                }
                else
                {
                    _outlineObject.SetActive(true);
                }
            }
            
            _hoverSequence.SetUpdate(true);
            _hoverSequence.OnKill(() => _hoverSequence = null);
            _hoverSequence.Play();
        }

        private void PlayHoverExitAnimation()
        {
            // Kill any ongoing hover animation to ensure interruptibility
            _hoverSequence?.Kill();
            
            float duration = _hoverDurationOverride > 0 ? _hoverDurationOverride : _signalConfig.HoverAnimationDuration;
            
            _hoverSequence = DOTween.Sequence();
            _hoverSequence.SetRecyclable(true);
            _hoverSequence.SetAutoKill(false);
            
            // Scale back to original
            _hoverSequence.Append(
                _buttonTransform.DOScale(_originalScale, duration)
                    .SetEase(_cachedHoverScaleCurve)
            );
            
            // Outline deactivation
            if (_outlineObject != null)
            {
                if (_useOutlineFade && _outlineCanvasGroup != null)
                {
                    _hoverSequence.Join(
                        _outlineCanvasGroup.DOFade(0f, duration)
                            .SetEase(_cachedOutlineFadeCurve)
                            .OnComplete(() => _outlineObject.SetActive(false))
                    );
                }
                else
                {
                    _outlineObject.SetActive(false);
                }
            }
            
            _hoverSequence.SetUpdate(true);
            _hoverSequence.OnKill(() => _hoverSequence = null);
            _hoverSequence.Play();
        }

        private void PlayClickAnimation()
        {
            // Prevent multiple click animations from overlapping
            if (_isAnimatingClick) return;
            
            _isAnimatingClick = true;
            
            // Kill any ongoing click animation
            _clickSequence?.Kill();
            
            float duration = _clickDurationOverride > 0 ? _clickDurationOverride : _signalConfig.ClickAnimationDuration;
            
            _clickSequence = DOTween.Sequence();
            _clickSequence.SetRecyclable(true);
            _clickSequence.SetAutoKill(false);
            
            // Scale pulse: down then back to original (or hover scale if hovered)
            Vector3 targetScale = _isHovered ? 
                _originalScale * _signalConfig.HoverScaleMultiplier : 
                _originalScale;
            
            _clickSequence.Append(
                _buttonTransform.DOScale(_originalScale * 0.9f, duration * 0.5f)
                    .SetEase(_cachedClickScaleCurve)
            );
            _clickSequence.Append(
                _buttonTransform.DOScale(targetScale, duration * 0.5f)
                    .SetEase(_cachedClickScaleCurve)
            );
            
            // Optional color flash using pre-allocated color
            if (_buttonImage != null)
            {
                _clickSequence.Join(
                    _buttonImage.DOColor(ClickFlashColor, duration * 0.25f)
                        .SetLoops(2, LoopType.Yoyo)
                );
            }
            
            _clickSequence.SetUpdate(true);
            _clickSequence.OnComplete(() => _isAnimatingClick = false);
            _clickSequence.OnKill(() => {
                _isAnimatingClick = false;
                _clickSequence = null;
            });
            _clickSequence.Play();
        }

        #endregion

        #region Helper Methods

        private void CacheReferences()
        {
            if (_buttonTransform == null)
                _buttonTransform = GetComponent<RectTransform>();
            
            if (_unityButton == null)
                _unityButton = GetComponent<Button>();
            
            // Ensure we have a button component for click events
            if (_unityButton != null)
            {
                // Subscribe to Unity button's onClick event as a fallback
                _unityButton.onClick.AddListener(HandleClick);
            }
        }

        private void ValidateComponents()
        {
            if (_buttonTransform == null)
            {
                Debug.LogError($"[ButtonAnimator] ButtonTransform is not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }
            
            if (_signalConfig == null)
            {
                Debug.LogError($"[ButtonAnimator] SignalButtonConfig is not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }
            
            if (_animationConfig == null)
            {
                Debug.LogError($"[ButtonAnimator] AnimationConfig is not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }
        }

        private void StoreOriginalState()
        {
            _originalScale = _buttonTransform.localScale;
        }

        private void SetupOutlineComponent()
        {
            if (_outlineObject == null) return;
            
            if (_useOutlineFade)
            {
                _outlineCanvasGroup = _outlineObject.GetComponent<CanvasGroup>();
                if (_outlineCanvasGroup == null)
                {
                    _outlineCanvasGroup = _outlineObject.AddComponent<CanvasGroup>();
                }
                _outlineCanvasGroup.alpha = 0f;
                _outlineObject.SetActive(false);
            }
            else
            {
                _outlineObject.SetActive(false);
            }
        }

        private void CacheAnimationCurves()
        {
            if (_animationConfig == null) return;
            
            _cachedHoverScaleCurve = _animationConfig.GetCopyOfCurve(_animationConfig.HoverScaleCurve);
            _cachedOutlineFadeCurve = _animationConfig.GetCopyOfCurve(_animationConfig.OutlineFadeCurve);
            _cachedClickScaleCurve = _animationConfig.GetCopyOfCurve(_animationConfig.ClickScaleCurve);
        }

        private void SetOutlineActive(bool active, bool immediate = false)
        {
            if (_outlineObject == null) return;
            
            if (_useOutlineFade && _outlineCanvasGroup != null)
            {
                if (immediate)
                {
                    _outlineCanvasGroup.alpha = active ? 1f : 0f;
                    _outlineObject.SetActive(active);
                }
                // Otherwise handled by animation
            }
            else
            {
                _outlineObject.SetActive(active);
            }
        }

        private void KillAnimations()
        {
            _hoverSequence?.Kill();
            _hoverSequence = null;
            
            _clickSequence?.Kill();
            _clickSequence = null;
        }

        #endregion

        #region Editor-Only Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure positive durations
            _hoverDurationOverride = Mathf.Max(0f, _hoverDurationOverride);
            _clickDurationOverride = Mathf.Max(0f, _clickDurationOverride);
            
            // Auto-assign components if not set
            if (_buttonTransform == null)
                _buttonTransform = GetComponent<RectTransform>();
            
            if (_unityButton == null)
                _unityButton = GetComponent<Button>();
        }
#endif

        #endregion
    }
}