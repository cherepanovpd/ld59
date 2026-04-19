// Path: Assets/_Project/Scripts/Currency/BillController.cs

using Core;
using DG.Tweening;
using Project.Currency.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace Project.Currency
{
    /// <summary>
    /// MonoBehaviour attached to each bill prefab.
    /// Manages bill state, animations, and interactions.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BillController : MonoBehaviour
    {
        public enum BillState
        {
            Idle,       // Waiting for mouse hover
            Hovered,    // Mouse is over, preparing to fly
            Flying,     // Flying to currency counter
            Collected   // Arrived at counter, about to be destroyed
        }

        [Header("Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private CircleCollider2D _collider;
        [SerializeField] private Light2D _light2D;

        [Header("Settings")]
        [SerializeField, Tooltip("Multiplier for collider radius relative to sprite size")]
        private float _colliderRadiusMultiplier = 1.5f;

        [SerializeField, Tooltip("Rotation speed during flight (degrees per second)")]
        private float _rotationSpeed = 180f;

        // Runtime state
        private BillState _currentState = BillState.Idle;
        private int _value = 10;
        private Vector3 _spawnPosition;
        private CurrencyConfig _config;
        private bool _isUsingInteractionSystem = false; // Track if we're using the new BillInteractionSystem

        // Animation references
        private Sequence _idleSequence;
        private Tween _flightTween;
        private Tween _rotationTween;

        // Cached references
        private Transform _cachedTransform;
        private Vector3 _cachedScale;

        #region Public Properties

        /// <summary>
        /// Current state of the bill.
        /// </summary>
        public BillState CurrentState => _currentState;

        /// <summary>
        /// Monetary value of this bill.
        /// </summary>
        public int Value => _value;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureRaycaster();
            CacheComponents();
            _cachedTransform = transform;
            _cachedScale = _cachedTransform.localScale;

            // Ensure collider is trigger
            if (_collider != null)
                _collider.isTrigger = true;

            // Get config reference
            _config = G.CurrencyConfig;
        }

        private void Start()
        {
            if (_currentState == BillState.Idle)
                StartIdleAnimation();
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        private void OnMouseEnter()
        {
            // If the new interaction system is active, ignore legacy mouse events
            // to prevent double triggering
            if (_isUsingInteractionSystem)
                return;

            if (_currentState == BillState.Idle)
            {
                SetState(BillState.Hovered);
                StartFlightToCounter();
            }
        }

        /// <summary>
        /// Called by BillInteractionSystem when mouse enters/exits this bill.
        /// </summary>
        public void SetHovered(bool isHovered)
        {
            if (!_isUsingInteractionSystem)
                _isUsingInteractionSystem = true; // Auto-detect that we're using the new system

            if (isHovered && _currentState == BillState.Idle)
            {
                SetState(BillState.Hovered);
                StartFlightToCounter();
            }
            // Note: We don't handle "unhover" because once a bill starts flying,
            // it can't be unhovered (it's already collected)
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the bill with value and spawn position.
        /// </summary>
        public void Initialize(int value, Vector3 spawnPosition)
        {
            _value = value;
            _spawnPosition = spawnPosition;

            // Apply random sprite if available
            if (_config != null && _spriteRenderer != null)
            {
                Sprite randomSprite = _config.GetRandomBillSprite();
                if (randomSprite != null)
                    _spriteRenderer.sprite = randomSprite;
            }
            
            _cachedTransform.localScale = _cachedScale;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, 1f);
            }

            // Scale collider
            if (_collider != null && _spriteRenderer != null)
            {
                Bounds bounds = _spriteRenderer.bounds;
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) * _colliderRadiusMultiplier;
                _collider.radius = radius;
            }

            // Set initial light intensity
            if (_light2D != null && _config != null)
            {
                _light2D.intensity = _config.MaxLightIntensity;
            }

            SetState(BillState.Idle);
        }

        private void EnsureRaycaster()
        {
            // OnMouseEnter requires a Physics2DRaycaster on the camera.
            // This validation ensures at least one camera has it.
            // We'll check the main camera and G.Camera (if available).
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // If no main camera, try to get from G.Camera
                if (G.Camera != null)
                    mainCam = G.Camera.GetComponent<Camera>();
            }

            if (mainCam != null)
            {
                var raycaster = mainCam.GetComponent<Physics2DRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogWarning($"[BillController] Physics2DRaycaster missing on camera '{mainCam.name}'. Adding one automatically.");
                    mainCam.gameObject.AddComponent<Physics2DRaycaster>();
                }
            }
            else
            {
                Debug.LogWarning("[BillController] No main camera found. OnMouseEnter may not work.");
            }
        }

        private void CacheComponents()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_collider == null)
                _collider = GetComponent<CircleCollider2D>();

            if (_light2D == null)
                _light2D = GetComponent<Light2D>();
        }

        #endregion

        #region State Management

        private void SetState(BillState newState)
        {
            if (_currentState == newState)
                return;

            BillState previousState = _currentState;
            _currentState = newState;

            OnStateChanged(previousState, newState);
        }

        private void OnStateChanged(BillState previousState, BillState newState)
        {
            // Stop animations from previous state
            switch (previousState)
            {
                case BillState.Idle:
                    StopIdleAnimation();
                    break;
                case BillState.Flying:
                    StopFlightAnimation();
                    break;
            }

            // Start animations for new state
            switch (newState)
            {
                case BillState.Idle:
                    StartIdleAnimation();
                    break;
                case BillState.Flying:
                    // Flight animation started by StartFlightToCounter
                    break;
                case BillState.Collected:
                    OnCollected();
                    break;
            }
        }

        #endregion

        #region Animations

        private void StartIdleAnimation()
        {
            if (_config == null || _spriteRenderer == null)
                return;

            // Pulsating scale animation
            _idleSequence = DOTween.Sequence();
            _idleSequence
                .Append(_cachedTransform.DOScale(_cachedScale * _config.PulseScale, _config.PulseDuration / 2f).SetEase(Ease.InOutSine))
                .Append(_cachedTransform.DOScale(_cachedScale, _config.PulseDuration / 2f).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);

            // Light flicker animation
            if (_light2D != null)
            {
                float randomOffset = Random.Range(0f, Mathf.PI * 2f);
                _idleSequence.Join(
                    DOTween.To(
                        () => _light2D.intensity,
                        x => _light2D.intensity = x,
                        _config.MinLightIntensity,
                        _config.PulseDuration / 2f
                    )
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                );
            }
        }

        private void StopIdleAnimation()
        {
            _idleSequence?.Kill();
            _idleSequence = null;
        }

        private void StartFlightToCounter()
        {
            if (_config == null || G.Currency == null)
            {
                // Fallback: just destroy
                SetState(BillState.Collected);
                return;
            }

            SetState(BillState.Flying);

            // Get target position from CurrencyCounter
            Vector3 targetPosition = G.Currency.GetCounterPosition();
            if (targetPosition == Vector3.zero)
                targetPosition = Camera.main.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, 10f));

            // Calculate flight path with curve
            Vector3 startPos = _cachedTransform.position;
            Vector3 controlPoint = (startPos + targetPosition) / 2f + Vector3.up * _config.FlightHeight;

            // Create flight tween using Bezier (simplified with DOTween path)
            float duration = _config.FlightDuration;
            _flightTween = DOTween.To(
                t => {
                    // Quadratic Bezier
                    float u = 1f - t;
                    float tt = t * t;
                    float uu = u * u;
                    Vector3 position = uu * startPos + 2f * u * t * controlPoint + tt * targetPosition;
                    _cachedTransform.position = position;
                },
                0f, 1f, duration
            )
            .SetEase(_config.FlightEase)
            .OnComplete(() => SetState(BillState.Collected));

            // Rotation during flight
            _rotationTween = _cachedTransform.DORotate(new Vector3(0, 0, 360f), duration / 2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(2, LoopType.Restart);
        }

        private void StopFlightAnimation()
        {
            _flightTween?.Kill();
            _rotationTween?.Kill();
            _flightTween = null;
            _rotationTween = null;
        }

        private void StopAllAnimations()
        {
            StopIdleAnimation();
            StopFlightAnimation();
        }

        #endregion

        #region Collection

        private void OnCollected()
        {
            // Notify CurrencyCounter to add value
            if (G.Currency != null)
                G.Currency.AddCurrency(_value);

            // Visual feedback before destruction
            Sequence collectSequence = DOTween.Sequence();
            collectSequence
                .Append(_cachedTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
                .Join(_spriteRenderer.DOFade(0f, 0.2f))
                .OnComplete(() => {
                    // Return to pool or destroy
                    if (G.BillManager != null)
                        G.BillManager.ReturnBill(gameObject);
                    else
                        Destroy(gameObject);
                });
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheComponents();
        }

        [ContextMenu("Test Flight")]
        private void TestFlight()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test flight can only be executed in Play mode.");
                return;
            }

            if (_currentState == BillState.Idle)
            {
                SetState(BillState.Hovered);
                StartFlightToCounter();
            }
        }
#endif

        #endregion
    }
}