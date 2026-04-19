// Path: Assets/_Project/Scripts/Utilities/SortingGroupByYPosition.cs
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Utilities
{
    /// <summary>
    /// Dynamically updates a SortingGroup's order based on the GameObject's Y position.
    /// Higher Y values result in lower order (appear behind), lower Y values result in higher order (appear in front).
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class SortingGroupByYPosition : MonoBehaviour
    {
        [Header("Y Position Range")]
        [Tooltip("Maximum Y value (top of range). Objects at this Y will have Order = -MaxY.")]
        [SerializeField] private float _maxY = 10f;
        
        [Tooltip("Minimum Y value (bottom of range). Objects at this Y will have Order = -MinY.")]
        [SerializeField] private float _minY = -10f;
        
        [Header("Debug")]
        [Tooltip("If enabled, logs the calculated order in the console.")]
        [SerializeField] private bool _debugLog = false;
        
        private SortingGroup _sortingGroup;
        private float _previousY;
        private int _previousOrder;
        
        /// <summary>
        /// Cached reference to the SortingGroup component.
        /// </summary>
        public SortingGroup SortingGroup => _sortingGroup;
        
        /// <summary>
        /// Current calculated order based on Y position.
        /// </summary>
        public int CurrentOrder { get; private set; }
        
        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null)
            {
                Debug.LogError($"{name}: SortingGroup component missing!", this);
                enabled = false;
                return;
            }
            
            // Ensure minY is less than maxY
            if (_minY > _maxY)
            {
                float temp = _minY;
                _minY = _maxY;
                _maxY = temp;
            }
            
            UpdateOrder();
        }
        
        private void Update()
        {
            // Only update if Y position changed beyond a small threshold
            float currentY = transform.position.y;
            if (Mathf.Abs(currentY - _previousY) < 0.001f)
                return;
            
            UpdateOrder();
        }
        
        /// <summary>
        /// Recalculates the order based on current Y position and applies it to the SortingGroup.
        /// </summary>
        public void UpdateOrder()
        {
            float y = transform.position.y;
            _previousY = y;
            
            // Clamp Y to the defined range
            float clampedY = Mathf.Clamp(y, _minY, _maxY);
            
            // Map clamped Y to order: Order = -Y
            // When Y = maxY → Order = -maxY (lowest order, appears behind)
            // When Y = minY → Order = -minY (highest order, appears in front)
            int order = Mathf.RoundToInt(-clampedY);
            
            // Only update if order changed
            if (order == _previousOrder)
                return;
            
            _previousOrder = order;
            CurrentOrder = order;
            _sortingGroup.sortingOrder = order;
            
            if (_debugLog)
            {
                Debug.Log($"{name}: Y={y:F2}, ClampedY={clampedY:F2}, Order={order}", this);
            }
        }
        
        /// <summary>
        /// Forces an immediate order update (useful if range parameters changed at runtime).
        /// </summary>
        public void ForceUpdate()
        {
            UpdateOrder();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure minY <= maxY in the Inspector
            if (_minY > _maxY)
            {
                float temp = _minY;
                _minY = _maxY;
                _maxY = temp;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualize the Y range in the Scene view
            Vector3 position = transform.position;
            Vector3 maxPos = new Vector3(position.x, _maxY, position.z);
            Vector3 minPos = new Vector3(position.x, _minY, position.z);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(maxPos, minPos);
            Gizmos.DrawWireSphere(maxPos, 0.2f);
            Gizmos.DrawWireSphere(minPos, 0.2f);
            
            // Draw current Y position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, 0.15f);
        }
#endif
    }
}