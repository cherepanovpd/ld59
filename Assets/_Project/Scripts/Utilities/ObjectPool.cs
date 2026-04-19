// Path: Assets/_Project/Scripts/Utilities/ObjectPool.cs

using System.Collections.Generic;

using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Generic GameObject pooling system with zero allocations during gameplay.
    /// Supports pre-instantiation, auto-expand, and statistics.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 10;
        [SerializeField] private int _maxSize = 100;
        [SerializeField] private bool _autoExpand = true;
        [SerializeField] private bool _poolOnAwake = false;

        [Header("Debug")]
        [SerializeField] private bool _showStatistics = false;

        private Stack<GameObject> _inactiveObjects;
        private List<GameObject> _activeObjects;
        private Transform _poolContainer;

        public int InactiveCount => _inactiveObjects?.Count ?? 0;
        public int ActiveCount => _activeObjects?.Count ?? 0;
        public int TotalCount => InactiveCount + ActiveCount;
        public bool IsFull => TotalCount >= _maxSize;

        private void Awake()
        {
            if (_poolOnAwake)
                Initialize();
        }

        /// <summary>
        /// Initialize the pool with the configured prefab and size.
        /// </summary>
        public void Initialize()
        {
            if (_prefab == null)
            {
                Debug.LogError("[ObjectPool] Prefab is not assigned.");
                return;
            }

            _inactiveObjects = new Stack<GameObject>(_initialSize);
            _activeObjects = new List<GameObject>(_initialSize);

            // Create a container to keep the hierarchy clean
            _poolContainer = new GameObject($"{_prefab.name}_Pool").transform;
            _poolContainer.SetParent(transform);
            _poolContainer.localPosition = Vector3.zero;

            Preload(_initialSize);
        }

        /// <summary>
        /// Set the prefab for this pool at runtime (must be called before Initialize).
        /// </summary>
        public void SetPrefab(GameObject prefab)
        {
            if (_prefab != null)
            {
                Debug.LogWarning("[ObjectPool] Prefab already set. Clearing existing pool.");
                Clear();
            }
            _prefab = prefab;
        }

        /// <summary>
        /// Preload a specific number of objects into the pool.
        /// </summary>
        public void Preload(int count)
        {
            if (_prefab == null) return;

            int toCreate = Mathf.Min(count, _maxSize - TotalCount);
            for (int i = 0; i < toCreate; i++)
            {
                GameObject obj = CreateNewObject();
                Return(obj);
            }

            if (_showStatistics)
                Debug.Log($"[ObjectPool] Preloaded {toCreate} objects. Total: {TotalCount}");
        }

        /// <summary>
        /// Get an object from the pool. Returns null if pool is empty and cannot expand.
        /// </summary>
        public GameObject Get()
        {
            return Get(Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Get an object from the pool with specific position and rotation.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            if (_inactiveObjects == null)
                Initialize();

            GameObject obj = null;

            if (_inactiveObjects.Count > 0)
            {
                obj = _inactiveObjects.Pop();
            }
            else if (_autoExpand && TotalCount < _maxSize)
            {
                obj = CreateNewObject();
            }
            else
            {
                if (_showStatistics)
                    Debug.LogWarning($"[ObjectPool] Pool is empty and cannot expand. Max size: {_maxSize}");
                return null;
            }

            // Setup object
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _activeObjects.Add(obj);

            // Notify components
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawn();

            if (_showStatistics)
                Debug.Log($"[ObjectPool] Get. Active: {ActiveCount}, Inactive: {InactiveCount}");

            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            // Ensure the object belongs to this pool
            if (!_activeObjects.Contains(obj) && !_inactiveObjects.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] Object {obj.name} does not belong to this pool.");
                return;
            }

            _activeObjects.Remove(obj);
            _inactiveObjects.Push(obj);

            obj.SetActive(false);
            obj.transform.SetParent(_poolContainer);
            obj.transform.localPosition = Vector3.zero;

            // Notify components
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            if (_showStatistics)
                Debug.Log($"[ObjectPool] Return. Active: {ActiveCount}, Inactive: {InactiveCount}");
        }

        /// <summary>
        /// Clear all objects in the pool (including active ones).
        /// </summary>
        public void Clear()
        {
            if (_inactiveObjects != null)
            {
                while (_inactiveObjects.Count > 0)
                {
                    GameObject obj = _inactiveObjects.Pop();
                    if (obj != null)
                        Destroy(obj);
                }
                _inactiveObjects.Clear();
            }

            if (_activeObjects != null)
            {
                foreach (GameObject obj in _activeObjects)
                {
                    if (obj != null)
                        Destroy(obj);
                }
                _activeObjects.Clear();
            }

            if (_poolContainer != null)
                Destroy(_poolContainer.gameObject);

            if (_showStatistics)
                Debug.Log("[ObjectPool] Cleared all objects.");
        }

        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            if (_activeObjects == null) return;

            // Iterate backwards because list will be modified
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeObjects[i];
                if (obj != null)
                    Return(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = Instantiate(_prefab, _poolContainer);
            obj.name = $"{_prefab.name}_{TotalCount + 1}";
            obj.SetActive(false);

            // Attach a PoolMember component for automatic return tracking (optional)
            var member = obj.GetComponent<PoolMember>() ?? obj.AddComponent<PoolMember>();
            member.Pool = this;

            return obj;
        }

        private void OnDestroy()
        {
            Clear();
        }

        #region Statistics

        public void PrintStatistics()
        {
            Debug.Log($"[ObjectPool] '{_prefab?.name}' - Active: {ActiveCount}, Inactive: {InactiveCount}, Total: {TotalCount}, Max: {_maxSize}");
        }

        #endregion
    }

    /// <summary>
    /// Interface for objects that need custom spawn/despawn logic.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>
    /// Component attached to pooled objects to track their pool.
    /// </summary>
    public class PoolMember : MonoBehaviour, IPoolable
    {
        public ObjectPool Pool { get; set; }

        public void OnSpawn()
        {
            // Override in derived classes for custom behavior
        }

        public void OnDespawn()
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Return this object to its pool.
        /// </summary>
        public void ReturnToPool()
        {
            if (Pool != null)
                Pool.Return(gameObject);
        }
    }
}