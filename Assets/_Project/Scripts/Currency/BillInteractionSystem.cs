// Path: Assets/_Project/Scripts/Currency/BillInteractionSystem.cs

using Core;
using Project.Currency;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Currency
{
    /// <summary>
    /// Centralized system for detecting mouse interactions with bills.
    /// Solves the overlapping collider problem by detecting ALL bills under the cursor.
    /// </summary>
    public class BillInteractionSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Maximum number of bills that can be detected at once")]
        private int _maxDetectedBills = 10;

        [SerializeField, Tooltip("Layer mask for bill detection")]
        private LayerMask _billLayerMask = 1 << 0; // Default layer

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // Cached components
        private Camera _mainCamera;
        private Collider2D[] _detectedColliders; // Reused array to avoid allocations
        private BillController[] _detectedBills; // Reused array for current frame
        private BillController[] _previousBills; // Reused array for previous frame

        // State
        private int _currentDetectedCount = 0;
        private int _previousDetectedCount = 0;

        #region Unity Lifecycle

        private void Awake()
        {
            // Self-registration
            if (G.BillInteraction != null && G.BillInteraction != this)
            {
                Debug.LogWarning("[BillInteractionSystem] Multiple instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            G.BillInteraction = this;
            G.EnsureSystem("BillInteraction", G.BillInteraction);

            // Cache camera
            _mainCamera = Camera.main;
            if (_mainCamera == null && G.HasCamera())
                _mainCamera = G.Camera.GetComponent<Camera>();

            if (_mainCamera == null)
                Debug.LogWarning("[BillInteractionSystem] No main camera found. Mouse detection may not work.");

            // Pre-allocate arrays to avoid allocations in Update
            _detectedColliders = new Collider2D[_maxDetectedBills];
            _detectedBills = new BillController[_maxDetectedBills];
            _previousBills = new BillController[_maxDetectedBills];
        }

        private void OnDestroy()
        {
            // Unregister
            if (G.BillInteraction == this)
                G.BillInteraction = null;
        }

        private void Update()
        {
            DetectBillsUnderCursor();
            UpdateBillStates();
        }

        #endregion

        #region Detection

        /// <summary>
        /// Detect all bills under the current mouse cursor position.
        /// Uses Physics2D.OverlapPointNonAlloc for zero allocations.
        /// </summary>
        private void DetectBillsUnderCursor()
        {
            if (_mainCamera == null)
                return;

            // Get mouse position in world coordinates
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(mousePosition);

            // Detect all colliders at the mouse position
            _currentDetectedCount = Physics2D.OverlapPointNonAlloc(
                worldPoint,
                _detectedColliders,
                _billLayerMask
            );

            if (_currentDetectedCount > _maxDetectedBills)
            {
                Debug.LogWarning($"[BillInteractionSystem] Detected {_currentDetectedCount} bills, but max is {_maxDetectedBills}. Some bills may be ignored.");
                _currentDetectedCount = _maxDetectedBills;
            }

            // Extract BillController components
            for (int i = 0; i < _currentDetectedCount; i++)
            {
                Collider2D collider = _detectedColliders[i];
                if (collider == null)
                    continue;

                BillController bill = collider.GetComponent<BillController>();
                if (bill == null)
                {
                    // Try parent if collider is on a child object
                    bill = collider.GetComponentInParent<BillController>();
                }

                _detectedBills[i] = bill;
            }

            // Clear remaining slots
            for (int i = _currentDetectedCount; i < _maxDetectedBills; i++)
            {
                _detectedBills[i] = null;
            }

            if (_debugLog && _currentDetectedCount > 0)
            {
                Debug.Log($"[BillInteractionSystem] Detected {_currentDetectedCount} bill(s) under cursor.");
            }
        }

        /// <summary>
        /// Update hover states for all detected bills.
        /// Compares with previous frame to handle enter/exit events.
        /// </summary>
        private void UpdateBillStates()
        {
            // Handle bills that are no longer under cursor (exit)
            for (int i = 0; i < _previousDetectedCount; i++)
            {
                BillController bill = _previousBills[i];
                if (bill == null)
                    continue;

                // Check if this bill is still detected
                bool stillDetected = false;
                for (int j = 0; j < _currentDetectedCount; j++)
                {
                    if (_detectedBills[j] == bill)
                    {
                        stillDetected = true;
                        break;
                    }
                }

                if (!stillDetected)
                {
                    // Mouse exited this bill
                    OnBillMouseExit(bill);
                }
            }

            // Handle newly detected bills (enter)
            for (int i = 0; i < _currentDetectedCount; i++)
            {
                BillController bill = _detectedBills[i];
                if (bill == null)
                    continue;

                // Check if this bill was already detected last frame
                bool wasAlreadyDetected = false;
                for (int j = 0; j < _previousDetectedCount; j++)
                {
                    if (_previousBills[j] == bill)
                    {
                        wasAlreadyDetected = true;
                        break;
                    }
                }

                if (!wasAlreadyDetected)
                {
                    // Mouse entered this bill
                    OnBillMouseEnter(bill);
                }
            }

            // Swap arrays for next frame
            BillController[] temp = _previousBills;
            _previousBills = _detectedBills;
            _detectedBills = temp;

            _previousDetectedCount = _currentDetectedCount;
        }

        #endregion

        #region Event Handlers

        private void OnBillMouseEnter(BillController bill)
        {
            if (bill == null)
                return;

            // Call the bill's hover start method
            // We'll add a public method to BillController for this
            bill.SetHovered(true);

            if (_debugLog)
                Debug.Log($"[BillInteractionSystem] Mouse entered bill (Value: {bill.Value})");
        }

        private void OnBillMouseExit(BillController bill)
        {
            if (bill == null)
                return;

            // Call the bill's hover end method
            bill.SetHovered(false);

            if (_debugLog)
                Debug.Log($"[BillInteractionSystem] Mouse exited bill (Value: {bill.Value})");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get all bills currently under the mouse cursor.
        /// </summary>
        public BillController[] GetBillsUnderCursor(out int count)
        {
            count = _currentDetectedCount;
            return _detectedBills;
        }

        /// <summary>
        /// Check if any bill is under the mouse cursor.
        /// </summary>
        public bool HasBillsUnderCursor()
        {
            return _currentDetectedCount > 0;
        }

        /// <summary>
        /// Get the topmost bill under cursor (for backward compatibility).
        /// </summary>
        public BillController GetTopmostBillUnderCursor()
        {
            if (_currentDetectedCount == 0)
                return null;

            // Return the first valid bill (Physics2D.OverlapPoint returns colliders in z-order)
            return _detectedBills[0];
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxDetectedBills < 1)
                _maxDetectedBills = 1;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _mainCamera == null)
                return;

            // Draw mouse position
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(mousePosition);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldPoint, 0.1f);

            // Draw detected bills
            for (int i = 0; i < _currentDetectedCount; i++)
            {
                if (_detectedBills[i] != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(_detectedBills[i].transform.position, 0.2f);
                }
            }
        }
#endif

        #endregion
    }
}