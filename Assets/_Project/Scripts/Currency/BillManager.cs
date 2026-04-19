// Path: Assets/_Project/Scripts/Currency/BillManager.cs

using Core;
using Project.Currency.Config;
using UnityEngine;

using Utilities;

namespace Project.Currency
{
    /// <summary>
    /// Central orchestrator for bill lifecycle management.
    /// Uses object pooling for performance, generates bills based on config,
    /// and handles bill spawning, recycling, and cleanup.
    /// Self-registers as G.BillManager.
    /// </summary>
    public class BillManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField, Tooltip("Reference to the currency config. If null, uses G.CurrencyConfig.")]
        private CurrencyConfig _config;

        [Header("Pool Settings")]
        [SerializeField, Tooltip("Initial size of the bill pool")]
        private int _initialPoolSize = 20;

        [SerializeField, Tooltip("Maximum size of the bill pool")]
        private int _maxPoolSize = 100;

        [SerializeField, Tooltip("If true, preloads the pool on Awake")]
        private bool _preloadOnAwake = true;

        // Cached references
        private ObjectPool _billPool;
        private Transform _cachedTransform;

        /// <summary>
        /// Current active bill count (for debugging).
        /// </summary>
        public int ActiveBillCount => _billPool?.ActiveCount ?? 0;

        /// <summary>
        /// Current inactive bill count (for debugging).
        /// </summary>
        public int InactiveBillCount => _billPool?.InactiveCount ?? 0;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.BillManager != null && G.BillManager != this)
            {
                Debug.LogWarning("[BillManager] Multiple BillManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.BillManager = this;
            G.EnsureSystem(nameof(BillManager), this);

            // Cache references
            _cachedTransform = transform;

            // Ensure config is available
            if (_config == null)
                _config = G.CurrencyConfig;

            if (_config == null)
                Debug.LogError("[BillManager] No CurrencyConfig assigned and G.CurrencyConfig is null.");

            // Initialize pool
            InitializePool();

            if (_preloadOnAwake)
                PreloadPool();
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.BillManager == this)
                G.BillManager = null;
        }

        #endregion

        #region Pool Management

        private void InitializePool()
        {
            if (_config?.BillPrefab == null)
            {
                Debug.LogError("[BillManager] Cannot initialize pool: BillPrefab is null.");
                return;
            }

            // Create a child GameObject to hold the pool
            GameObject poolContainer = new GameObject("BillPool");
            poolContainer.transform.SetParent(_cachedTransform);
            poolContainer.transform.localPosition = Vector3.zero;

            // Add and configure ObjectPool component
            _billPool = poolContainer.AddComponent<ObjectPool>();
            _billPool.SetPrefab(_config.BillPrefab);
            _billPool.Initialize();
        }

        private void PreloadPool()
        {
            if (_billPool != null)
                _billPool.Preload(_initialPoolSize);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generate bills from a house at the specified position.
        /// </summary>
        /// <param name="housePosition">World position where bills should appear</param>
        /// <param name="customConfig">Optional custom config (uses default if null)</param>
        public void GenerateBills(Vector3 housePosition, CurrencyConfig customConfig = null)
        {
            CurrencyConfig config = customConfig ?? _config ?? G.CurrencyConfig;
            if (config == null)
            {
                Debug.LogError("[BillManager] Cannot generate bills: no CurrencyConfig available.");
                return;
            }

            int billCount = config.GetRandomBillCount();
            Debug.Log($"[BillManager] Generating {billCount} bills at {housePosition}");

            for (int i = 0; i < billCount; i++)
            {
                GenerateSingleBill(housePosition, config);
            }
        }

        /// <summary>
        /// Generate a single bill with random parameters.
        /// </summary>
        public void GenerateSingleBill(Vector3 housePosition, CurrencyConfig config)
        {
            if (config?.BillPrefab == null)
                return;

            // Calculate spawn position with random offset
            float maxRadius = 1.5f;
            Vector2 randomCircle = Random.insideUnitCircle * maxRadius;
            Vector3 spawnPosition = housePosition + new Vector3(randomCircle.x, randomCircle.y, 0);

            // Get bill from pool
            GameObject billObj = _billPool?.Get(spawnPosition, Quaternion.identity);
            if (billObj == null)
            {
                // Fallback to instantiation if pool fails
                billObj = Instantiate(config.BillPrefab, spawnPosition, Quaternion.identity, _cachedTransform);
                Debug.LogWarning("[BillManager] Pool returned null, instantiated fallback.");
            }
            
            // Configure bill
            BillController billController = billObj.GetComponent<BillController>();
            if (billController != null)
            {
                int value = config.GetRandomBillValue();
                billController.Initialize(value, spawnPosition);
            }
            else
            {
                Debug.LogWarning("[BillManager] Bill prefab missing BillController component.");
            }
        }

        /// <summary>
        /// Return a bill to the pool (called by BillController when collected).
        /// </summary>
        public void ReturnBill(GameObject bill)
        {
            if (_billPool != null)
            {
                _billPool.Return(bill);
            }
            else
            {
                // Fallback: destroy immediately
                Destroy(bill);
            }
        }

        /// <summary>
        /// Clear all active bills (e.g., when restarting level).
        /// </summary>
        public void ClearAllBills()
        {
            if (_billPool != null)
            {
                // The ObjectPool should handle this
                // For now, we'll just destroy all child bills
                foreach (Transform child in _cachedTransform)
                {
                    if (child.name.Contains("Bill"))
                        Destroy(child.gameObject);
                }
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Generate Test Bills")]
        private void GenerateTestBills()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test bills can only be generated in Play mode.");
                return;
            }

            GenerateBills(Vector3.zero);
        }

        [ContextMenu("Clear All Bills")]
        private void EditorClearAllBills()
        {
            ClearAllBills();
        }
#endif

        #endregion
    }
}