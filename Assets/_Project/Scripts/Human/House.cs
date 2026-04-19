// Path: Assets/_Project/Scripts/Human/House.cs

using UnityEngine;

namespace Project.Human
{
    /// <summary>
    /// Placeholder for House class until the actual House implementation is created.
    /// Represents a house that humans belong to.
    /// </summary>
    [DisallowMultipleComponent]
    public class House : MonoBehaviour
    {
        [Header("House Identification")]
        [SerializeField] private string _houseId;
        [SerializeField] private int _maxResidents = 3;
        
        [Header("Spawn Area")]
        [SerializeField] private float _spawnRadius = 5f;
        
        /// <summary>
        /// Unique identifier for this house.
        /// </summary>
        public string HouseId => _houseId;
        
        /// <summary>
        /// Maximum number of humans that can live in this house.
        /// </summary>
        public int MaxResidents => _maxResidents;
        
        /// <summary>
        /// Radius around the house where humans can spawn.
        /// </summary>
        public float SpawnRadius => _spawnRadius;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Generate a unique ID if not set
            if (string.IsNullOrEmpty(_houseId))
            {
                _houseId = $"House_{GetInstanceID()}";
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get a random spawn position around this house.
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
            return transform.position + new Vector3(randomCircle.x, randomCircle.y, 0f);
        }
        
        /// <summary>
        /// Check if a position is within this house's spawn radius.
        /// </summary>
        public bool IsWithinSpawnRadius(Vector3 position)
        {
            float distance = Vector3.Distance(transform.position, position);
            return distance <= _spawnRadius;
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
            
            // Draw house center
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
#endif
        
        #endregion
    }
}