// Path: Assets/_Project/Scripts/Effects/TowerSignalRing.cs

using DG.Tweening;
using UnityEngine;

namespace Project.Effects
{
    /// <summary>
    /// Visual and collider ring that expands from a tower to signal a blink.
    /// The ring starts bright and fades out, while a CircleCollider2D moves with it
    /// to detect objects within the radius.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class TowerSignalRing : MonoBehaviour
    {
        [Header("Ring Configuration")]
        [SerializeField, Tooltip("Maximum radius the ring will expand to.")]
        [Min(0.1f)]
        private float _maxRadius = 5f;

        [SerializeField, Tooltip("Duration of the expansion from zero to max radius.")]
        [Min(0.01f)]
        private float _expandDuration = 1f;

        [SerializeField, Tooltip("Easing curve for the expansion.")]
        private Ease _expandEase = Ease.OutQuad;

        [Header("Visual Appearance")]
        [SerializeField, Tooltip("Color of the ring at the start of expansion.")]
        private Color _startColor = new Color(0.2f, 0.6f, 1f, 1f);

        [SerializeField, Tooltip("Color of the ring at the end of expansion (alpha should be zero).")]
        private Color _endColor = new Color(0.2f, 0.6f, 1f, 0f);

        [SerializeField, Tooltip("Width of the ring line.")]
        [Min(0.01f)]
        private float _lineWidth = 0.1f;

        [SerializeField, Tooltip("Number of segments in the circle (higher = smoother).")]
        [Range(4, 128)]
        private int _circleSegments = 64;

        [Header("Collider")]
        [SerializeField, Tooltip("If true, the collider will be enabled and move with the ring.")]
        private bool _enableCollider = true;

        [SerializeField, Tooltip("Collider will be a trigger.")]
        private bool _isTrigger = true;

        [SerializeField, Tooltip("Offset of collider radius relative to visual radius (e.g., 1 = same).")]
        [Min(0f)]
        private float _colliderRadiusMultiplier = 1f;

        // Component references
        private LineRenderer _lineRenderer;
        private CircleCollider2D _circleCollider;
        private MaterialPropertyBlock _propertyBlock;

        // Runtime state
        private float _currentRadius;
        private Sequence _expandSequence;
        private bool _isExpanding;

        /// <summary>
        /// Whether the ring is currently expanding.
        /// </summary>
        public bool IsExpanding => _isExpanding;

        /// <summary>
        /// Current radius of the ring (0 to maxRadius).
        /// </summary>
        public float CurrentRadius => _currentRadius;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            InitializeVisual();
            InitializeCollider();
        }

        private void OnDestroy()
        {
            StopAllTweens();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts the ring expansion from the given center position.
        /// If the ring is already expanding, it will be restarted.
        /// </summary>
        /// <param name="center">World position where the ring originates.</param>
        /// <param name="customRadius">Optional custom maximum radius. If zero or negative, uses the serialized _maxRadius.</param>
        /// <param name="customDuration">Optional custom expansion duration. If zero or negative, uses the serialized _expandDuration.</param>
        public void StartExpansion(Vector3 center, float customRadius = 0f, float customDuration = 0f)
        {
            StopAllTweens();

            transform.position = center;
            _currentRadius = 0f;

            float targetRadius = customRadius > 0f ? customRadius : _maxRadius;
            float duration = customDuration > 0f ? customDuration : _expandDuration;

            UpdateVisualRadius(0f);
            UpdateColliderRadius(0f);
            UpdateColorProgress(0f);

            _isExpanding = true;
            SetVisible(true);

            float colorProgress = 0f;
            _expandSequence = DOTween.Sequence();
            // Tween radius
            _expandSequence.Join(DOTween.To(
                    () => _currentRadius,
                    value =>
                    {
                        _currentRadius = value;
                        UpdateVisualRadius(value);
                        UpdateColliderRadius(value);
                    },
                    targetRadius,
                    duration)
                .SetEase(_expandEase));
            // Tween color progress
            _expandSequence.Join(DOTween.To(
                    () => colorProgress,
                    value =>
                    {
                        colorProgress = value;
                        UpdateColorProgress(value);
                    },
                    1f,
                    duration)
                .SetEase(_expandEase));
            
            _expandSequence.OnComplete(() =>
            {
                _isExpanding = false;
                OnExpansionComplete();
            });

            _expandSequence.Play();
        }

        /// <summary>
        /// Stops the expansion immediately and optionally hides the ring.
        /// </summary>
        /// <param name="hide">If true, disables the LineRenderer and Collider.</param>
        public void StopExpansion(bool hide = false)
        {
            StopAllTweens();
            _isExpanding = false;

            if (hide)
            {
                SetVisible(false);
            }
        }

        /// <summary>
        /// Instantly sets the ring to a specific radius (for debugging or manual control).
        /// </summary>
        /// <param name="radius">Desired radius.</param>
        public void SetRadius(float radius)
        {
            _currentRadius = Mathf.Clamp(radius, 0f, _maxRadius);
            UpdateVisualRadius(_currentRadius);
            UpdateColliderRadius(_currentRadius);
        }

        /// <summary>
        /// Changes the ring colors dynamically.
        /// </summary>
        /// <param name="start">Color at the inner edge (or start of expansion).</param>
        /// <param name="end">Color at the outer edge (or end of expansion).</param>
        public void SetColors(Color start, Color end)
        {
            _startColor = start;
            _endColor = end;
            UpdateVisualColors();
        }

        #endregion

        #region Private Helpers

        private void CacheComponents()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _circleCollider = GetComponent<CircleCollider2D>();

            if (_lineRenderer == null)
            {
                Debug.LogError($"{nameof(TowerSignalRing)} requires a {nameof(LineRenderer)} component.", this);
            }

            if (_circleCollider == null)
            {
                Debug.LogError($"{nameof(TowerSignalRing)} requires a {nameof(CircleCollider2D)} component.", this);
            }
        }

        private void InitializeVisual()
        {
            if (_lineRenderer == null) return;

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.positionCount = _circleSegments + 1; // +1 to close the circle

            UpdateVisualColors();
            UpdateVisualRadius(0f);
            SetVisible(false);
        }

        private void InitializeCollider()
        {
            if (_circleCollider == null) return;

            _circleCollider.isTrigger = _isTrigger;
            _circleCollider.enabled = _enableCollider;
            _circleCollider.radius = 0f;
        }

        private void UpdateVisualRadius(float radius)
        {
            if (_lineRenderer == null) return;

            float angleStep = 2f * Mathf.PI / _circleSegments;

            for (int i = 0; i <= _circleSegments; i++)
            {
                float angle = i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private void UpdateColorProgress(float t)
        {
            if (_lineRenderer == null) return;

            // Interpolate color between start and end
            Color color = Color.Lerp(_startColor, _endColor, t);
            // Create a simple gradient with the same color at both ends
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
            );
            _lineRenderer.colorGradient = gradient;
        }

        private void UpdateVisualColors()
        {
            // Set initial color (progress = 0)
            UpdateColorProgress(0f);
        }

        private void UpdateColliderRadius(float radius)
        {
            if (_circleCollider == null || !_enableCollider) return;

            _circleCollider.radius = radius * _colliderRadiusMultiplier;
        }

        private void SetVisible(bool visible)
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = visible;
            }

            if (_circleCollider != null && _enableCollider)
            {
                _circleCollider.enabled = visible;
            }
        }

        private void StopAllTweens()
        {
            if (_expandSequence != null && _expandSequence.IsActive())
            {
                _expandSequence.Kill();
                _expandSequence = null;
            }
        }

        private void OnExpansionComplete()
        {
            SetVisible(false);

            // Optionally disable visuals after a short delay, or keep them until manually hidden.
            // For now, we keep the ring visible at its final radius.
            // You can emit an event here if needed.
        }

        #endregion
    }
}