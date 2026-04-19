// Path: Assets/_Project/Scripts/Currency/CurrencyCounter.cs

using Core;
using DG.Tweening;
using Project.Currency.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Currency
{
    /// <summary>
    /// Tracks total currency, handles smooth increment animations, and provides UI updates.
    /// Self-registers as G.Currency.
    /// </summary>
    public class CurrencyCounter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _currencyText;
        [SerializeField] private Image _currencyIcon;
        [SerializeField] private RectTransform _counterPosition; // UI position where bills fly to

        [Header("Animation Settings")]
        [SerializeField, Tooltip("Scale multiplier for icon pulse")]
        private float _iconPulseScale = 1.3f;

        [SerializeField, Tooltip("Duration of icon pulse animation")]
        private float _iconPulseDuration = 0.3f;

        [SerializeField, Tooltip("Color change when currency increases")]
        private Color _increaseColor = new Color(0.2f, 1f, 0.2f, 1f);

        // Runtime state
        private int _currentAmount = 0;
        private int _displayAmount = 0;
        private CurrencyConfig _config;
        private Tween _incrementTween;
        private Tween _iconPulseTween;
        private Tween _textColorTween;

        // Cached references
        private Transform _cachedTransform;
        private Color _originalTextColor;
        private Color _originalIconColor;
        private Vector3 _originalIconScale;

        /// <summary>
        /// Current total currency amount.
        /// </summary>
        public int CurrentAmount => _currentAmount;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.Currency != null && G.Currency != this)
            {
                Debug.LogWarning("[CurrencyCounter] Multiple CurrencyCounter instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.Currency = this;
            G.EnsureSystem(nameof(CurrencyCounter), this);

            // Cache references
            _cachedTransform = transform;
            _config = G.CurrencyConfig;

            // Cache original values
            if (_currencyText != null)
                _originalTextColor = _currencyText.color;

            if (_currencyIcon != null)
            {
                _originalIconColor = _currencyIcon.color;
                _originalIconScale = _currencyIcon.transform.localScale;
            }

            // Initialize display
            UpdateDisplay();
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.Currency == this)
                G.Currency = null;

            // Clean up tweens
            _incrementTween?.Kill();
            _iconPulseTween?.Kill();
            _textColorTween?.Kill();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Add currency with smooth animation.
        /// </summary>
        public void AddCurrency(int amount)
        {
            if (amount <= 0)
                return;

            int previousAmount = _currentAmount;
            _currentAmount += amount;

            Debug.Log($"[CurrencyCounter] Adding {amount} currency. Total: {_currentAmount}");

            // Start smooth increment animation
            StartIncrementAnimation(previousAmount, _currentAmount);

            // Visual feedback
            PulseIcon();
            FlashTextColor();

            // Play sound if available
            if (G.HasAudio())
                G.Audio.PlaySFX("CurrencyCollect");
        }

        /// <summary>
        /// Set currency directly (without animation).
        /// </summary>
        public void SetCurrency(int amount)
        {
            if (amount < 0)
                amount = 0;

            _currentAmount = amount;
            _displayAmount = amount;
            UpdateDisplay();
        }

        /// <summary>
        /// Get the world position where bills should fly to.
        /// </summary>
        public Vector3 GetCounterPosition()
        {
            if (_counterPosition != null)
            {
                // Convert UI position to world position
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _counterPosition,
                    _counterPosition.position,
                    Camera.main,
                    out Vector3 worldPos
                );
                return worldPos;
            }

            // Fallback: use this GameObject's position
            return _cachedTransform.position;
        }

        /// <summary>
        /// Reset currency to zero.
        /// </summary>
        public void ResetCurrency()
        {
            SetCurrency(0);
        }

        #endregion

        #region Animation

        private void StartIncrementAnimation(int fromAmount, int toAmount)
        {
            // Kill existing tween
            _incrementTween?.Kill();

            // Calculate duration based on config
            float duration = _config != null ? _config.GetIncrementDuration(toAmount - fromAmount) : 1f;

            // Create smooth increment tween
            _incrementTween = DOTween.To(
                () => _displayAmount,
                x => {
                    _displayAmount = x;
                    UpdateDisplay();
                },
                toAmount,
                duration
            )
            .SetEase(Ease.OutCubic)
            .OnComplete(() => _displayAmount = toAmount);
        }

        private void PulseIcon()
        {
            if (_currencyIcon == null)
                return;

            // Kill existing pulse
            _iconPulseTween?.Kill();

            // Create pulse sequence
            Transform iconTransform = _currencyIcon.transform;
            _iconPulseTween = iconTransform.DOScale(_originalIconScale * _iconPulseScale, _iconPulseDuration / 2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    iconTransform.DOScale(_originalIconScale, _iconPulseDuration / 2f)
                        .SetEase(Ease.InOutSine);
                });
        }

        private void FlashTextColor()
        {
            if (_currencyText == null)
                return;

            // Kill existing color tween
            _textColorTween?.Kill();

            // Create flash sequence
            _textColorTween = DOTween.Sequence()
                .Append(_currencyText.DOColor(_increaseColor, 0.1f).SetEase(Ease.OutSine))
                .Append(_currencyText.DOColor(_originalTextColor, 0.3f).SetEase(Ease.InOutSine));
        }

        #endregion

        #region UI Updates

        private void UpdateDisplay()
        {
            if (_currencyText != null)
            {
                // Format with commas for thousands
                _currencyText.text = FormatCurrency(_displayAmount);
            }
        }

        private string FormatCurrency(int amount)
        {
            // Simple formatting - can be enhanced with localization
            return amount.ToString("N0");
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Add 100 Currency")]
        private void AddTestCurrency()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test currency can only be added in Play mode.");
                return;
            }

            AddCurrency(100);
        }

        [ContextMenu("Reset to Zero")]
        private void EditorResetCurrency()
        {
            if (!Application.isPlaying)
                return;

            ResetCurrency();
        }

        private void OnValidate()
        {
            // Auto-find references if null
            if (_currencyText == null)
                _currencyText = GetComponentInChildren<TextMeshProUGUI>();

            if (_currencyIcon == null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image img in images)
                {
                    if (img.gameObject != gameObject)
                    {
                        _currencyIcon = img;
                        break;
                    }
                }
            }
        }
#endif

        #endregion
    }
}