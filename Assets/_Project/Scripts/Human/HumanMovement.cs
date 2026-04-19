// Path: Assets/_Project/Scripts/Human/HumanMovement.cs

using Project.Human.Config;
using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Handles movement logic for human characters.
    /// Supports wandering, returning home, and idle states.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _arrivalThreshold = 0.1f;
        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _wanderInterval = 3f;
        
        [Header("Current State")]
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private bool _hasTarget = false;
        [SerializeField] private float _wanderTimer = 0f;
        
        // Cached references for performance
        private Transform _cachedTransform;
        private HumanConfig _config;
        
        /// <summary>
        /// Current movement speed.
        /// </summary>
        public float Speed => _speed;
        
        /// <summary>
        /// Whether the character has a movement target.
        /// </summary>
        public bool HasTarget => _hasTarget;
        
        /// <summary>
        /// Current target position.
        /// </summary>
        public Vector3 TargetPosition => _targetPosition;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _cachedTransform = transform;
        }
        
        private void OnValidate()
        {
            // Clamp values
            _speed = Mathf.Max(0.1f, _speed);
            _arrivalThreshold = Mathf.Max(0.01f, _arrivalThreshold);
            _wanderRadius = Mathf.Max(0.1f, _wanderRadius);
            _wanderInterval = Mathf.Max(0.1f, _wanderInterval);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initialize with configuration.
        /// </summary>
        public void Initialize(HumanConfig config)
        {
            _config = config;
            
            if (_config != null)
            {
                _speed = _config.DayMovementSpeed;
                _wanderRadius = _config.WanderRadius;
            }
        }
        
        /// <summary>
        /// Update movement based on current state.
        /// </summary>
        public void UpdateMovement(float deltaTime)
        {
            if (!_hasTarget)
                return;
            
            // Calculate direction to target
            Vector3 direction = _targetPosition - _cachedTransform.position;
            float distance = direction.magnitude;
            
            // Check if we've arrived
            if (distance <= _arrivalThreshold)
            {
                _hasTarget = false;
                return;
            }
            
            // Normalize direction and move
            direction.Normalize();
            Vector3 movement = direction * _speed * deltaTime;
            
            // Move the character
            _cachedTransform.position += movement;
        }
        
        /// <summary>
        /// Move to a specific position.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            _targetPosition = position;
            _hasTarget = true;
        }
        
        /// <summary>
        /// Stop moving.
        /// </summary>
        public void Stop()
        {
            _hasTarget = false;
        }
        
        /// <summary>
        /// Check if the character has reached its destination.
        /// </summary>
        public bool HasReachedDestination()
        {
            if (!_hasTarget)
                return true;
            
            float distance = Vector3.Distance(_cachedTransform.position, _targetPosition);
            return distance <= _arrivalThreshold;
        }
        
        /// <summary>
        /// Get a random wander position within wander radius from current position.
        /// </summary>
        public Vector3 GetRandomWanderPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            return _cachedTransform.position + new Vector3(randomCircle.x, randomCircle.y, 0f);
        }
        
        /// <summary>
        /// Update wandering behavior.
        /// </summary>
        public void UpdateWandering(float deltaTime)
        {
            _wanderTimer -= deltaTime;
            
            if (_wanderTimer <= 0f)
            {
                // Get new random wander position
                Vector3 wanderPos = GetRandomWanderPosition();
                MoveTo(wanderPos);
                
                // Reset timer with some randomness
                _wanderTimer = _wanderInterval + Random.Range(-0.5f, 0.5f);
            }
        }
        
        /// <summary>
        /// Set movement speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            _speed = Mathf.Max(0.1f, speed);
        }
        
        /// <summary>
        /// Set arrival threshold.
        /// </summary>
        public void SetArrivalThreshold(float threshold)
        {
            _arrivalThreshold = Mathf.Max(0.01f, threshold);
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw wander radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _wanderRadius);
            
            // Draw target position if we have one
            if (_hasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_targetPosition, 0.2f);
                Gizmos.DrawLine(transform.position, _targetPosition);
            }
        }
#endif
        
        #endregion
    }
}