// Path: Assets/_Project/Scripts/Currency/HouseCurrencyGenerator.cs

using Core;
using DG.Tweening;
using Project.Currency.Config;
using UnityEngine;

namespace Project.Currency
{
    /// <summary>
    /// Attached to each house prefab. Listens for signal ring collisions,
    /// triggers house animation, and requests bill generation.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HouseCurrencyGenerator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Transform _houseVisual; // The transform to animate (usually the house root)

        [Header("Settings")]
        [SerializeField, Tooltip("If true, house can generate currency multiple times")]
        private bool _canGenerateMultipleTimes = true;

        [SerializeField, Tooltip("Cooldown between generations (seconds)")]
        private float _generationCooldown = 2f;

        [SerializeField, Tooltip("Layer mask for signal ring detection")]
        private LayerMask _signalRingLayer = 1 << 8; // Default to layer 8

        // Runtime state
        private bool _hasGenerated = false;
        private float _lastGenerationTime = -999f;
        private CurrencyConfig _config;
        private Vector3 _originalScale;

        // Cached references
        private Transform _cachedTransform;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            _cachedTransform = transform;

            // Store original scale for animation
            if (_houseVisual != null)
                _originalScale = _houseVisual.localScale;
            else
                _originalScale = _cachedTransform.localScale;

            // Ensure collider is trigger
            if (_collider != null)
                _collider.isTrigger = true;

            // Get config reference
            _config = G.CurrencyConfig;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if collider is on signal ring layer
            if (((1 << other.gameObject.layer) & _signalRingLayer) == 0)
                return;

            // Check if this is a TowerSignalRing (optional tag check)
            if (!other.CompareTag("SignalRing"))
                return;

            // Check cooldown
            if (Time.time - _lastGenerationTime < _generationCooldown)
                return;

            // Check if can generate multiple times
            if (!_canGenerateMultipleTimes && _hasGenerated)
                return;

            // Generate currency
            GenerateCurrency();
        }

        #endregion

        #region Currency Generation

        private void GenerateCurrency()
        {
            Debug.Log($"[HouseCurrencyGenerator] Generating currency at {_cachedTransform.position}");

            // Update state
            _hasGenerated = true;
            _lastGenerationTime = Time.time;

            // Play house animation
            PlayHouseAnimation();

            // Generate bills via BillManager
            if (G.BillManager != null)
            {
                G.BillManager.GenerateBills(_cachedTransform.position, _config);
            }
            else
            {
                Debug.LogWarning("[HouseCurrencyGenerator] BillManager not available in G.");
            }

            // Play sound if available
            if (G.HasAudio())
                G.Audio.PlaySFX("HouseGenerate");
        }

        private void PlayHouseAnimation()
        {
            if (_houseVisual == null)
                _houseVisual = _cachedTransform;

            if (_config == null)
                return;

            // Create spring animation sequence
            Sequence springSequence = DOTween.Sequence();

            // Scale up quickly
            springSequence.Append(_houseVisual.DOScale(
                _originalScale * _config.HouseSpringScale,
                _config.HouseSpringDuration * 0.3f
            ).SetEase(Ease.OutBack));

            // Bounce back with overshoot
            springSequence.Append(_houseVisual.DOScale(
                _originalScale,
                _config.HouseSpringDuration * 0.7f
            ).SetEase(Ease.OutBounce));

            // Optional: add slight rotation wobble
            springSequence.Join(_houseVisual.DORotate(
                new Vector3(0, 0, 5f),
                _config.HouseSpringDuration * 0.2f
            ).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
        }

        #endregion

        #region Helper Methods

        private void CacheComponents()
        {
            if (_collider == null)
                _collider = GetComponent<Collider2D>();

            if (_houseVisual == null)
                _houseVisual = transform;
        }

        /// <summary>
        /// Manually trigger currency generation (for testing).
        /// </summary>
        public void TriggerGeneration()
        {
            GenerateCurrency();
        }

        /// <summary>
        /// Reset generation state (e.g., when restarting level).
        /// </summary>
        public void ResetGenerator()
        {
            _hasGenerated = false;
            _lastGenerationTime = -999f;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheComponents();
        }

        [ContextMenu("Test Generation")]
        private void TestGeneration()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test generation can only be executed in Play mode.");
                return;
            }

            TriggerGeneration();
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize generation range
            if (_collider != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                if (_collider is CircleCollider2D circle)
                {
                    Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                }
                else if (_collider is BoxCollider2D box)
                {
                    Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
                }
            }
        }
#endif

        #endregion
    }
}