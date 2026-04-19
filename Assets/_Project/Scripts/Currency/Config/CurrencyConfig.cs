// Path: Assets/_Project/Scripts/Currency/Config/CurrencyConfig.cs

using Core;
using UnityEngine;

namespace Project.Currency.Config
{
    /// <summary>
    /// ScriptableObject configuration for currency generation.
    /// Contains bill generation parameters, flight settings, and visual effects.
    /// Registers itself into G.CurrencyConfig on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyConfig", menuName = "Game/Currency Config", order = 102)]
    public class CurrencyConfig : ScriptableObject
    {
        [Header("Bill Generation")]
        [SerializeField, Tooltip("Minimum and maximum number of bills generated per house")]
        private Vector2Int _billCountRange = new Vector2Int(1, 3);

        [SerializeField, Tooltip("Minimum and maximum value of each bill")]
        private Vector2Int _billValueRange = new Vector2Int(10, 100);

        [Header("Flight Settings")]
        [SerializeField, Tooltip("Duration of flight from bill to currency counter (seconds)")]
        [Min(0.1f)]
        private float _flightDuration = 0.8f;

        [SerializeField, Tooltip("Maximum height of the flight curve (world units)")]
        [Min(0f)]
        private float _flightHeight = 2f;

        [SerializeField, Tooltip("Curve defining the vertical motion (0=start, 1=end)")]
        private AnimationCurve _flightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField, Tooltip("Easing function for horizontal movement")]
        private DG.Tweening.Ease _flightEase = DG.Tweening.Ease.InOutCubic;

        [Header("Visual Settings")]
        [SerializeField, Tooltip("Prefab for bill GameObject")]
        private GameObject _billPrefab;

        [SerializeField, Tooltip("Optional sprite variants for visual variety")]
        private Sprite[] _billSprites;

        [SerializeField, Tooltip("Scale multiplier for pulse animation")]
        [Min(1f)]
        private float _pulseScale = 1.1f;

        [SerializeField, Tooltip("Duration of one pulse cycle (seconds)")]
        [Min(0.1f)]
        private float _pulseDuration = 0.5f;

        [Header("Light2D Settings")]
        [SerializeField, Tooltip("Minimum light intensity during flicker")]
        [Min(0f)]
        private float _minLightIntensity = 0.7f;

        [SerializeField, Tooltip("Maximum light intensity during flicker")]
        [Min(0f)]
        private float _maxLightIntensity = 1.3f;

        [SerializeField, Tooltip("Speed of flicker animation (cycles per second)")]
        [Min(0.1f)]
        private float _flickerSpeed = 3f;

        [Header("House Animation")]
        [SerializeField, Tooltip("Scale multiplier for house spring animation")]
        [Min(1f)]
        private float _houseSpringScale = 1.1f;

        [SerializeField, Tooltip("Duration of house spring animation (seconds)")]
        [Min(0.1f)]
        private float _houseSpringDuration = 0.3f;

        [Header("UI Animation")]
        [SerializeField, Tooltip("Duration for currency counter to increment (seconds per 1000 units)")]
        [Min(0.1f)]
        private float _counterIncrementDuration = 1.5f;

        [SerializeField, Tooltip("Maximum duration for any increment (caps large amounts)")]
        [Min(0.1f)]
        private float _maxIncrementDuration = 3f;

        // Public properties with getters
        public Vector2Int BillCountRange => _billCountRange;
        public Vector2Int BillValueRange => _billValueRange;
        public float FlightDuration => _flightDuration;
        public float FlightHeight => _flightHeight;
        public AnimationCurve FlightCurve => _flightCurve;
        public DG.Tweening.Ease FlightEase => _flightEase;
        public GameObject BillPrefab => _billPrefab;
        public Sprite[] BillSprites => _billSprites;
        public float PulseScale => _pulseScale;
        public float PulseDuration => _pulseDuration;
        public float MinLightIntensity => _minLightIntensity;
        public float MaxLightIntensity => _maxLightIntensity;
        public float FlickerSpeed => _flickerSpeed;
        public float HouseSpringScale => _houseSpringScale;
        public float HouseSpringDuration => _houseSpringDuration;
        public float CounterIncrementDuration => _counterIncrementDuration;
        public float MaxIncrementDuration => _maxIncrementDuration;

        #region Self-Registration

        private void OnEnable()
        {
            // Register this instance as the global currency config if not already set
            if (G.CurrencyConfig == null)
            {
                G.CurrencyConfig = this;
                G.EnsureSystem(nameof(CurrencyConfig), this);
            }
        }

        private void OnDisable()
        {
            // If this instance is the registered config, clear it
            if (G.CurrencyConfig == this)
                G.CurrencyConfig = null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get a random bill count within the configured range.
        /// </summary>
        public int GetRandomBillCount()
        {
            return Random.Range(_billCountRange.x, _billCountRange.y + 1);
        }

        /// <summary>
        /// Get a random bill value within the configured range.
        /// </summary>
        public int GetRandomBillValue()
        {
            return Random.Range(_billValueRange.x, _billValueRange.y + 1);
        }

        /// <summary>
        /// Get a random bill sprite from the available variants, or null if none.
        /// </summary>
        public Sprite GetRandomBillSprite()
        {
            if (_billSprites == null || _billSprites.Length == 0)
                return null;

            return _billSprites[Random.Range(0, _billSprites.Length)];
        }

        /// <summary>
        /// Calculate the duration for incrementing a given amount, capped by max duration.
        /// </summary>
        public float GetIncrementDuration(int amount)
        {
            float baseDuration = (amount / 1000f) * _counterIncrementDuration;
            return Mathf.Min(baseDuration, _maxIncrementDuration);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            _billCountRange = new Vector2Int(1, 3);
            _billValueRange = new Vector2Int(10, 100);
            _flightDuration = 0.8f;
            _flightHeight = 2f;
            _flightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            _flightEase = DG.Tweening.Ease.InOutCubic;
            _pulseScale = 1.1f;
            _pulseDuration = 0.5f;
            _minLightIntensity = 0.7f;
            _maxLightIntensity = 1.3f;
            _flickerSpeed = 3f;
            _houseSpringScale = 1.1f;
            _houseSpringDuration = 0.3f;
            _counterIncrementDuration = 1.5f;
            _maxIncrementDuration = 3f;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion
    }
}